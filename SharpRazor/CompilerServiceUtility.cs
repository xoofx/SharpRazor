// Copyright (c) 2013 SharpRazor - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from RazorEngine (https://github.com/Antaris/RazorEngine)
// with the following license:
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.
//--------------------------------------------------------------------

using System.Linq;

namespace SharpRazor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Dynamic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Collections;

    /// <summary>
    /// Provides service methods for compilation.
    /// </summary>
    internal static class CompilerServicesUtility
    {
        #region Fields
        private static readonly Type DynamicType = typeof(DynamicObject);
        private static readonly Type ExpandoType = typeof(ExpandoObject);
        private static readonly Type EnumerableType = typeof(IEnumerable);
        private static readonly Type EnumeratorType = typeof(IEnumerator);
        #endregion

        public static bool IsImplementingGenericType(Type type, Type genericType)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (genericType == null) throw new ArgumentNullException("genericType");

            if (!genericType.IsGenericTypeDefinition)
                throw new ArgumentException("Expecting a generic definition", "genericType");

            for (Type t = type; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
                    return true;

            return false;
        }

        #region Methods
        /// <summary>
        /// Determines if the specified type is an anonymous type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is an anonymous type, otherwise false.</returns>
        public static bool IsAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return (type.IsClass
                    && type.IsSealed
                    && type.BaseType == typeof(object)
                    && type.Name.StartsWith("<>", StringComparison.Ordinal)
                    && type.IsDefined(typeof(CompilerGeneratedAttribute), true));
        }

        /// <summary>
        /// Determines if the specified type is a dynamic type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is an anonymous type, otherwise false.</returns>
        public static bool IsDynamicType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return (DynamicType.IsAssignableFrom(type)
                    || ExpandoType.IsAssignableFrom(type)
                    || IsAnonymousType(type));
        }

        /// <summary>
        /// Determines if the specified type is a compiler generated iterator type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is an iterator type, otherwise false.</returns>
        public static bool IsIteratorType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return type.IsNestedPrivate
                && type.Name.StartsWith("<", StringComparison.Ordinal)
                && (EnumerableType.IsAssignableFrom(type) || EnumeratorType.IsAssignableFrom(type));
        }

        /// <summary>
        /// Gets the public or protected constructors of the specified type.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <returns>An enumerable of constructors.</returns>
        public static IEnumerable<ConstructorInfo> GetConstructors(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var constructors = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            return constructors;
        }

        public static string ResolveCSharpTypeName(Type type)
        {
            if (IsIteratorType(type))
                type = GetFirstGenericInterface(type);

            if (IsDynamicType(type))
                return "dynamic";

            if (!type.IsGenericType)
                return type.FullName;

            return type.Namespace
                  + "."
                  + type.Name.Substring(0, type.Name.IndexOf('`'))
                  + "<"
                  + string.Join(", ", type.GetGenericArguments().Select(ResolveCSharpTypeName))
                  + ">";
        }

        public static string ResolveVBTypeName(Type type)
        {
            if (IsIteratorType(type))
                type = GetFirstGenericInterface(type);

            if (IsDynamicType(type))
                return "Object";

            if (!type.IsGenericType)
                return type.FullName;

            return type.Namespace
                  + "."
                  + type.Name.Substring(0, type.Name.IndexOf('`'))
                  + "(Of"
                  + string.Join(", ", type.GetGenericArguments().Select(ResolveVBTypeName))
                  + ")";
        }

        /// <summary>
        /// Gets the first generic interface of the specified type if one exists.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <returns>The first generic interface if one exists, otherwise the first interface or the target type itself if there are no interfaces.</returns>
        public static Type GetFirstGenericInterface(Type type)
        {
            Type firstInterface = null;
            foreach (var @interface in type.GetInterfaces())
            {
                if (firstInterface == null)
                    firstInterface = @interface;

                if (@interface.IsGenericType)
                    return @interface;
            }
            return @firstInterface ?? type;
        }

        /// <summary>
        /// Gets an enumerable of all assemblies loaded in the current domain.
        /// </summary>
        /// <returns>An enumerable of loaded assemblies.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static IEnumerable<Assembly> GetLoadedAssemblies()
        {
            var domain = AppDomain.CurrentDomain;
            return domain.GetAssemblies();
        }
        #endregion
    }
}
