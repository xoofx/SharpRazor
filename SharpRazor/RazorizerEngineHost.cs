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
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;

namespace SharpRazor
{
    /// <summary>
    /// Defines the custom razor engine host used by <see cref="Razorizer"/>.
    /// </summary>
    public class RazorizerEngineHost : System.Web.Razor.RazorEngineHost
    {
        private readonly Func<RazorCodeGenerator, RazorCodeGenerator> decorateCodeGenerator;
        private readonly Func<ParserBase, ParserBase> decorateCodeParser;
        /// <summary>
        /// Initializes a new instance of the <see cref="RazorizerEngineHost" /> class.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="markupParserFactory">The markup parser factory.</param>
        /// <param name="decorateCodeGenerator">The decorate code generator.</param>
        /// <param name="decorateCodeParser">The code parser factory.</param>
        /// <exception cref="System.ArgumentNullException">codeParser</exception>
        public RazorizerEngineHost(RazorCodeLanguage language, Func<ParserBase> markupParserFactory,  
            Func<RazorCodeGenerator, RazorCodeGenerator> decorateCodeGenerator, 
            Func<ParserBase, ParserBase> decorateCodeParser)
            : base(language, markupParserFactory)
        {
            if (decorateCodeGenerator == null) throw new ArgumentNullException("decorateCodeGenerator");
            if (decorateCodeParser == null) throw new ArgumentNullException("decorateCodeParser");
            this.decorateCodeGenerator = decorateCodeGenerator;
            this.decorateCodeParser = decorateCodeParser;
        }

        /// <summary>
        /// Gets or sets the default template type.
        /// </summary>
        public Type DefaultBaseTemplateType { get; set; }

        /// <summary>
        /// Gets or sets the default model type.
        /// </summary>
        public Type DefaultModelType { get; set; }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
           return decorateCodeGenerator(incomingCodeGenerator);
        }

        /// <summary>
        /// Decorates the code parser.
        /// </summary>
        /// <param name="incomingCodeParser">The code parser.</param>
        /// <returns>The decorated parser.</returns>
        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return decorateCodeParser(incomingCodeParser) ?? incomingCodeParser;
        }
    }
}