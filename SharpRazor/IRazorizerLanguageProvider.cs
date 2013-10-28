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
using System.CodeDom.Compiler;
using System.Web.Razor;
using System.Web.Razor.Parser;

namespace SharpRazor
{
    /// <summary>
    /// Provides specific language parsing/compiling for <see cref="Razorizer"/>
    /// registered through the <see cref="Razorizer.LanguageProviders"/>.
    /// </summary>
    public interface IRazorizerLanguageProvider
    {
        /// <summary>
        /// Determines whether this instance can handle the specified file extension.
        /// </summary>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns><c>true</c> if this instance can handle the specified file extension; otherwise, <c>false</c>.</returns>
        bool CanHandle(string fileExtension);

        /// <summary>
        /// Creates the code language.
        /// </summary>
        /// <returns>A new instance of a <see cref="RazorCodeLanguage"/>.</returns>
        RazorCodeLanguage CreateCodeLanguage();

        /// <summary>
        /// Creates the code parser handled by this provider.
        /// </summary>
        /// <returns>A new instance of a <see cref="ParserBase"/> used for parsing the code specific to this language.</returns>
        ParserBase CreateCodeParser();

        /// <summary>
        /// Creates the code DOM provider handled by this provider in order to compile the generated language.
        /// </summary>
        /// <returns>CodeDomProvider.</returns>
        CodeDomProvider CreateCodeDomProvider();
    }
}