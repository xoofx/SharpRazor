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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace SharpRazor
{
    /// <summary>
    /// An exception occuring when compiling a razor template into target language.
    /// </summary>
    public class TemplateCompilationException : Exception
    {
        private readonly string sourceCode;
        private readonly List<CompilerError> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateCompilationException" /> class.
        /// </summary>
        /// <param name="sourceCode">The source code.</param>
        /// <param name="errors">The errors.</param>
        public TemplateCompilationException(string sourceCode, List<CompilerError> errors) : base(FormatErrors(errors))
        {
            this.sourceCode = sourceCode;
            this.errors = errors;
        }

        /// <summary>
        /// Gets the source code used for compilation.
        /// </summary>
        /// <value>The source code.</value>
        public string SourceCode
        {
            get { return sourceCode; }
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>The errors.</value>
        public List<CompilerError> Errors
        {
            get { return errors; }
        }

        private static string FormatErrors(IEnumerable<CompilerError> errors)
        {
            if (errors == null) throw new ArgumentNullException("errors");

            var errorText = new StringBuilder();
            errorText.AppendLine("Error when compiling page:");
            foreach (var error in errors)
            {
                errorText.AppendFormat("{0}({1},{2}): error {3}: {4}", error.FileName, error.Line, error.Column, error.ErrorNumber, error.ErrorText);
                errorText.AppendLine();
            }
            return errorText.ToString();
        }
    }
}