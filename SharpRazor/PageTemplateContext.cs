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

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace SharpRazor
{
    /// <summary>
    /// The context used when rendering a <see cref="PageTemplate"/>. The creation of this context
    /// can be overriden from <see cref="Razorizer"/>.
    /// </summary>
    public class PageTemplateContext
    {
        private readonly Stack<LambdaWriter> lambdaWriters = new Stack<LambdaWriter>();
        private readonly IDictionary<string, Action> registeredSections = new Dictionary<string, Action>();
        private readonly dynamic viewBag;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageTemplateContext"/> class.
        /// </summary>
        public PageTemplateContext() : this(new ExpandoObject())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageTemplateContext"/> class.
        /// </summary>
        /// <param name="viewBag">The view bag.</param>
        public PageTemplateContext(dynamic viewBag)
        {
            this.viewBag = viewBag ?? new ExpandoObject();
        }


        /// <summary>
        /// Gets the view bag.
        /// </summary>
        /// <value>The view bag.</value>
        public dynamic ViewBag { get { return viewBag; } }

        /// <summary>
        /// Defines a section.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="action">The action to execute when using the section.</param>
        /// <exception cref="System.ArgumentException">
        /// Section cannot be null or empty;name
        /// or
        /// A section is already registered with name ' + name + ';name
        /// </exception>
        public virtual void DefineSection(string name, Action action)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Section cannot be null or empty", "name");

            if (registeredSections.ContainsKey(name))
                throw new ArgumentException("A section is already registered with name '" + name + "'", "name");

            registeredSections.Add(name, action);
        }

        /// <summary>
        /// Gets the section action for the specified section name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>An action or null if not found</returns>
        public virtual Action GetSectionAction(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            Action action;
            registeredSections.TryGetValue(name, out action);
            return action;
        }

        /// <summary>
        /// Determines whether a section with the specified name is defined.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if a section with the specified name is defined; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public virtual bool IsSectionDefined(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            return GetSectionAction(name) != null;
        }

        /// <summary>
        /// Pushes the body of a layout that will be rendered later (at <see cref="PopBody"/> time).
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public virtual void PushBody(LambdaWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            lambdaWriters.Push(writer);
        }

        /// <summary>
        /// Pops the body of a layout that is going to be rendered.
        /// </summary>
        /// <returns>LambdaWriter.</returns>
        public virtual LambdaWriter PopBody()
        {
            return lambdaWriters.Pop();
        }
    }
}