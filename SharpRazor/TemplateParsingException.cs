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
using System.Text;
using System.Web.Razor.Parser.SyntaxTree;

namespace SharpRazor
{
    /// <summary>
    /// An exception occuring when parsing a razor template.
    /// </summary>
    public class TemplateParsingException : Exception
    {
        private readonly List<RazorError> errors;
        private readonly string location;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateParsingException"/> class.
        /// </summary>
        /// <param name="templateFilePath">Path of the template file.</param>
        /// <param name="errors">The errors.</param>
        public TemplateParsingException(string templateFilePath, List<RazorError> errors)
            : base(FormatErrors(templateFilePath, errors))
        {
            this.location = templateFilePath;
            this.errors = errors;
        }

        /// <summary>
        /// Gets the location of the template where the error was encountered.
        /// </summary>
        /// <value>The location.</value>
        public string Location
        {
            get { return location; }
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>The errors.</value>
        public List<RazorError> Errors
        {
            get { return errors; }
        }

        private static string FormatErrors(string location, IEnumerable<RazorError> errors)
        {
            if (errors == null) throw new ArgumentNullException("errors");

            var errorText = new StringBuilder();
            errorText.AppendLine("Error when generating Razor code:");
            foreach (var error in errors)
            {
                errorText.AppendFormat("{0}({1},{2}): error {3}: {4}", location, error.Location.LineIndex + 1, error.Location.CharacterIndex + 1, "R0000", error.Message);
                errorText.AppendLine();
            }
            return errorText.ToString();
        }
    }
}