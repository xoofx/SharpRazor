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
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Net;

namespace SharpRazor
{
    /// <summary>
    /// Base class for rendering a page not intended for direct subclass. A client must subclass <see cref="PageTemplate{T}"/>
    /// </summary>
    public abstract class PageTemplate
    {
        private object model;

        protected PageTemplate()
        {
        }

        /// <summary>
        /// Gets the name of this page.
        /// </summary>
        /// <value>The name of the page.</value>
        public string PageName { get; internal set; }

        /// <summary>
        /// Gets the page source code. Only available when <see cref="SharpRazor.Razorizer.EnableDebug"/> is set to <c>true</c>.
        /// </summary>
        /// <value>The page source code.</value>
        public string PageSourceCode { get; internal set; }

        /// <summary>
        /// Gets or sets the currnt layout template.
        /// </summary>
        /// <value>The currnt layout template.</value>
        public string Layout { get; set; }

        /// <summary>
        /// Gets the <see cref="Razorizer"/> that creates this instance.
        /// </summary>
        /// <value>The razorizer.</value>
        public Razorizer Razorizer { get; internal set; }

        /// <summary>
        /// Gets the current context when rendering this page.
        /// </summary>
        /// <value>The current context when rendering this page.</value>
        public PageTemplateContext Context { get; private set; }

        /// <summary>
        /// Gets the current writer when rendering this page.
        /// </summary>
        /// <value>The writer.</value>
        public TextWriter Writer { get; private set; }

        /// <summary>
        /// This instance has a dynamic model. This is used internally.
        /// </summary>
        protected bool HasDynamicModel { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public object Model
        {
            get { return model; }
            set
            {
                if (HasDynamicModel && !(value is DynamicObject) && !(value is ExpandoObject))
                    model = new DynamicAnonymousObject { Model = value };
                else
                    model = value;
            }
        }

        /// <summary>
        /// Defines a section.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="action">The action associated with this section.</param>
        public virtual void DefineSection(string name, Action action)
        {
            Context.DefineSection(name, action);
        }

        /// <summary>
        /// Finds a template or null if not found. This method is redirecting calls 
        /// to <see cref="SharpRazor.Razorizer.FindTemplate"/>
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <returns>A new page template instance of null if not found.</returns>
        protected virtual PageTemplate FindTemplate(string templateName)
        {
            return Razorizer.FindTemplate(templateName);
        }

        /// <summary>
        /// Executes this instance, used by Razor.
        /// </summary>
        public virtual void Execute()
        {
        }

        /// <summary>
        /// Renders the body of a template.
        /// </summary>
        /// <returns>A <see cref="LambdaWriter"/> containing the body to render.</returns>
        public LambdaWriter RenderBody()
        {
            return Context.PopBody();
        }

        /// <summary>
        /// Renders a section.
        /// </summary>
        /// <param name="name">The name of the section to render.</param>
        /// <param name="isRequired">if set to <c>true</c> [is required].</param>
        /// <returns>A <see cref="LambdaWriter"/> containing the section to render.</returns>
        /// <exception cref="System.ArgumentNullException">name;Name of a section cannot be null</exception>
        /// <exception cref="System.ArgumentException">No section has been defined with name ' + name + ';name</exception>
        public virtual LambdaWriter RenderSection(string name, bool isRequired = true)
        {
            if (name == null) throw new ArgumentNullException("name", "Name of a section cannot be null");

            var action = Context.GetSectionAction(name);

            // If action is null, replace by an empty action, unless it is required.
            if (action == null)
            {
                if (isRequired)
                    throw new ArgumentException("No section has been defined with name '" + name + "'", "name");
                action = () => { };
            }

            return new LambdaWriter(tw => action());
        }

        /// <summary>
        /// Includes the template with the specified name.
        /// </summary>
        /// <param name="cacheName">The name of the template type in cache.</param>
        /// <param name="model">The model or NULL if there is no model for the template.</param>
        /// <returns>The template writer helper.</returns>
        public virtual LambdaWriter Include(string cacheName, object model = null)
        {
            var instance = Razorizer.FindTemplate(cacheName);
            if (instance == null)
                throw new ArgumentException("No template could be resolved with name '" + cacheName + "'");

            instance.Model = model ?? Model;
            return new LambdaWriter(tw => tw.Write(instance.Run()));
        }

        /// <summary>
        /// Runs the templating.
        /// </summary>
        /// <returns>The result of templating.</returns>
        public string Run()
        {
            return Run(new PageTemplateContext());
        }

        /// <summary>
        /// Runs the templating.
        /// </summary>
        /// <param name="viewBag">The view bag.</param>
        /// <returns>The result of templating.</returns>
        public string Run(dynamic viewBag)
        {
            return Run(new PageTemplateContext(viewBag));
        }

        /// <summary>
        /// Runs the templating.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The result of templating.</returns>
        /// <exception cref="System.InvalidOperationException">If a layout was not found</exception>
        public virtual string Run(PageTemplateContext context)
        {
            try
            {
                var writer = new StringWriter();

                // Execute this template
                Context = context;
                Writer = writer;
                Execute();
                Writer = null;
                Context = null;

                // If we are in a layout context, use the current layout
                if (Layout != null)
                {
                    // Get the layout template.
                    var layout = FindTemplate(Layout);

                    if (layout == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Layout [{0}] was not found in registered template", Layout));
                    }

                    // Push the current body instance onto the stack for later execution.
                    var layoutWriter = new LambdaWriter(tw => tw.Write(writer.ToString()));
                    context.PushBody(layoutWriter);

                    return layout.Run(context);
                }

                return writer.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Writes the specified value to the current <see cref="Writer"/>.
        /// </summary>
        /// <param name="val">The value.</param>
        public virtual void Write(object val)
        {
            WriteTo(Writer, val);
        }

        /// <summary>
        /// Writes to the current <see cref="Writer"/> using the specified lambda writer.
        /// </summary>
        /// <param name="lambdaWriter">The lambda writer.</param>
        public virtual void Write(LambdaWriter lambdaWriter)
        {
            if (lambdaWriter == null)
                return;

            WriteTo(Writer, lambdaWriter);
        }

        /// <summary>
        /// Writes the literal to the current <see cref="Writer"/>.
        /// </summary>
        /// <param name="val">The value.</param>
        public virtual void WriteLiteral(object val)
        {
            WriteLiteralTo(Writer, val);
        }

        /// <summary>
        /// Writes a value to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public virtual void WriteTo(TextWriter writer, object value)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (value == null) return;

            var htmlString = value as IHtmlString;
            if (htmlString != null)
            {
                writer.Write(htmlString.ToHtmlString());
            }
            else
            {
                var text = Convert.ToString(value, CultureInfo.CurrentCulture);
                WriteLiteralTo(writer, WebUtility.HtmlEncode(text));
            }
        }

        /// <summary>
        /// Converts a raw HTML to a <see cref="HtmlRawString"/>.
        /// </summary>
        /// <param name="rawHtml">The raw HTML.</param>
        /// <returns>An <see cref="HtmlRawString"/>.</returns>
        public static HtmlRawString Raw(string rawHtml)
        {
            return new HtmlRawString(rawHtml);
        }

        /// <summary>
        /// Writes a value to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="lambdaWriter">The lambda writer.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public virtual void WriteTo(TextWriter writer, LambdaWriter lambdaWriter)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (lambdaWriter == null) throw new ArgumentNullException("lambdaWriter");
            lambdaWriter.WriteTo(writer);
        }

        /// <summary>
        /// Writes a literal to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="val">The value.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public virtual void WriteLiteralTo(TextWriter writer, object val)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (val == null) return;

            writer.Write(val);
        }

        /// <summary>
        /// Writes an attribute to the current <see cref="Writer"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="suffix">The suffix.</param>
        /// <param name="values">The values.</param>
        public virtual void WriteAttribute(string name, PositionTagged<string> prefix, PositionTagged<string> suffix, params AttributeValue[] values)
        {
            WriteAttributeTo(Writer, name, prefix, suffix, values);
        }

        /// <summary>
        /// Writes an attribute to the current <see cref="Writer"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="name">The name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="suffix">The suffix.</param>
        /// <param name="values">The values.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public virtual void WriteAttributeTo(TextWriter writer, string name, PositionTagged<string> prefix, PositionTagged<string> suffix, params AttributeValue[] values)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            bool first = true;
            bool wroteSomething = false;
            if (values.Length == 0)
            {
                // Explicitly empty attribute, so write the prefix and suffix
                WritePositionTaggedLiteral(writer, prefix);
                WritePositionTaggedLiteral(writer, suffix);
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var attrVal = values[i];
                    var val = attrVal.Value;
                    var next = i == values.Length - 1 ?
                        suffix : // End of the list, grab the suffix
                        values[i + 1].Prefix; // Still in the list, grab the next prefix

                    bool? boolVal = null;
                    if (val.Value is bool)
                    {
                        boolVal = (bool)val.Value;
                    }

                    if (val.Value != null && (boolVal == null || boolVal.Value))
                    {
                        string valStr = val.Value as string;
                        if (valStr == null)
                        {
                            valStr = val.Value.ToString();
                        }
                        if (boolVal != null)
                        {
                            Debug.Assert(boolVal.Value);
                            valStr = name;
                        }

                        if (first)
                        {
                            WritePositionTaggedLiteral(writer, prefix);
                            first = false;
                        }
                        else
                        {
                            WritePositionTaggedLiteral(writer, attrVal.Prefix);
                        }

                        if (attrVal.Literal)
                        {
                            WriteLiteralTo(writer, valStr);
                        }
                        else
                        {
                            WriteTo(writer, valStr); // Write value
                        }
                        wroteSomething = true;
                    }
                }
                if (wroteSomething)
                {
                    WritePositionTaggedLiteral(writer, suffix);
                }
            }
        }

        /// <summary>
        /// Writes a position tagged literal.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="position">The position.</param>
        private void WritePositionTaggedLiteral(TextWriter writer, string value, int position)
        {
            WriteLiteralTo(writer, value);
        }

        /// <summary>
        /// Writes a position tagged literal.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        private void WritePositionTaggedLiteral(TextWriter writer, PositionTagged<string> value)
        {
            WritePositionTaggedLiteral(writer, value.Value, value.Position);
        }
    }

    /// <summary>
    /// Default base class for page templating.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    public class PageTemplate<T> : PageTemplate
    {
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public new T Model
        {
            get { return (T) base.Model; }
            set
            {
                base.Model = value;
            }
        }
    }
}