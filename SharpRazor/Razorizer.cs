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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using Microsoft.CSharp.RuntimeBinder;
using SharpRazor.CSharp;

namespace SharpRazor
{
    /// <summary>
    /// Provides methods for templating text in-memory using Razor.
    /// </summary>
    public class Razorizer
    {
        private const string DefaultNamespaceForCompiledPageTemplate = "SharpRazor.Dynamic";
        private readonly Dictionary<string, Type> generatedPageTemplateTypes = new Dictionary<string, Type>();
        private readonly HashSet<IRazorizerLanguageProvider> languageProviders;
        private string defaultFileExtension;

        /// <summary>
        /// Initializes a new instance of the <see cref="Razorizer"/> class.
        /// </summary>
        public Razorizer() : this(typeof (PageTemplate<>))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Razorizer"/> class.
        /// </summary>
        /// <param name="pageTemplateType">Type of the page template.</param>
        /// <exception cref="System.ArgumentNullException">pageTemplateType</exception>
        /// <exception cref="System.ArgumentException">Expecting type to subclass PageTemplate<></exception>
        public Razorizer(Type pageTemplateType)
        {
            if (pageTemplateType == null) throw new ArgumentNullException("pageTemplateType");

            // Check that the template is a type of PageTemplate<>
            if (!CompilerServicesUtility.IsImplementingGenericType(pageTemplateType, typeof (PageTemplate<>)))
            {
                throw new ArgumentException("Expecting type to subclass PageTemplate<>");
            }

            PageTemplateType = pageTemplateType;

            // Register default namespaces
            NameSpaces = new HashSet<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq"
            };

            // Register default assemblies
            Assemblies = new HashSet<Assembly>(
                AppDomain.CurrentDomain.GetAssemblies().Concat(new Assembly[] { typeof(RuntimeBinderException).Assembly })
                    .Where(a => !a.IsDynamic && File.Exists(a.Location))
                    .GroupBy(a => a.GetName().Name)
                    .Select(grp => grp.First(y => y.GetName().Version == grp.Max(x => x.GetName().Version))));

            // Register default extension
            defaultFileExtension = CSharpLanguageProvider.DefaultExtension;

            // Register default language providers
            languageProviders = new HashSet<IRazorizerLanguageProvider>
            {
                new CSharpLanguageProvider()
            };
        }

        /// <summary>
        /// Gets the type of the page template.
        /// </summary>
        /// <value>The type of the page template.</value>
        public Type PageTemplateType { get; private set; }

        /// <summary>
        /// Gets the assemblies to load when compiling a <see cref="PageTemplate"/>.
        /// </summary>
        /// <value>The assemblies.</value>
        public HashSet<Assembly> Assemblies { get; private set; }

        /// <summary>
        /// Gets the namespaces to use when compiling a <see cref="PageTemplate"/>.
        /// </summary>
        /// <value>The name spaces.</value>
        public HashSet<string> NameSpaces { get; private set; }

        /// <summary>
        /// Gets the language providers registered.
        /// </summary>
        /// <value>The language providers.</value>
        public HashSet<IRazorizerLanguageProvider> LanguageProviders
        {
            get { return languageProviders; }
        }

        /// <summary>
        /// Gets or sets the default file extension. Default is: .cshtml
        /// </summary>
        /// <value>The default file extension.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public string DefaultFileExtension
        {
            get { return defaultFileExtension; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("value");

                defaultFileExtension = NormalizeExtension(value);
            }
        }

        /// <summary>
        /// Finds a language provider for the specified file extension. Returns null if not found.
        /// </summary>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>A registered <see cref="IRazorizerLanguageProvider"/> or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">fileExtension</exception>
        public virtual IRazorizerLanguageProvider FindLanguageProvider(string fileExtension)
        {
            if (fileExtension == null) throw new ArgumentNullException("fileExtension");
            fileExtension = NormalizeExtension(fileExtension);

            return LanguageProviders.FirstOrDefault(razorLanguageProvider => razorLanguageProvider.CanHandle(fileExtension));
        }

        /// <summary>
        /// Finds a template already generated with the specified template name. See remarks.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <returns>PageTemplate.</returns>
        /// <remarks>
        /// A template can be pre-compiled using the various <see cref="Compile(string,string,System.Type)"/> methods that are accepting
        /// a template name.
        /// </remarks>
        public virtual PageTemplate FindTemplate(string templateName)
        {
            Type pageTemplateType = null;
            lock (generatedPageTemplateTypes)
            {
                generatedPageTemplateTypes.TryGetValue(templateName, out pageTemplateType);
            }

            return pageTemplateType != null ? NewPageTemplate(pageTemplateType) : null;
        }

        /// <summary>
        /// Parses the specified template and returns the result of executing the template.
        /// </summary>
        /// <param name="templateContent">Content of the template.</param>
        /// <returns>The result of executing the template.</returns>
        public string Parse(string templateContent)
        {
            return Parse(templateContent, (dynamic)new ExpandoObject());
        }

        /// <summary>
        /// Parses the specified template and returns the result of executing the template.
        /// </summary>
        /// <typeparam name="T">Type of the model to use with the template</typeparam>
        /// <param name="templateContent">Content of the template.</param>
        /// <param name="model">The model instance.</param>
        /// <param name="viewBag">The view bag used with layouts. May be null</param>
        /// <returns>The result of executing the template.</returns>
        public string Parse<T>(string templateContent, T model, dynamic viewBag = null)
        {
            return Parse(templateContent, model, new PageTemplateContext(viewBag));
        }

        /// <summary>
        /// Parses the specified template and returns the result of executing the template.
        /// </summary>
        /// <typeparam name="T">Type of the model to use with the template</typeparam>
        /// <param name="templateContent">Content of the template.</param>
        /// <param name="model">The model.</param>
        /// <param name="pageContext">The page context.</param>
        /// <returns>The result of executing the template.</returns>
        public string Parse<T>(string templateContent, T model, PageTemplateContext pageContext)
        {
            var template = Compile(templateContent, typeof(T));
            template.Model = model;
            return template.Run(pageContext);
        }

        /// <summary>
        /// Compiles a template with the specified content, name, filename and model type.
        /// </summary>
        /// <param name="templateContent">Content of the template.</param>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>A new instance of a <see cref="PageTemplate"/>.</returns>
        /// <exception cref="System.ArgumentNullException">templateContent</exception>
        public PageTemplate Compile(string templateContent, Type modelType = null)
        {
            return Compile(null, templateContent, modelType);
        }

        /// <summary>
        /// Compiles a template with the specified content, name, filename and model type.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="templateContent">Content of the template.</param>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>A new instance of a <see cref="PageTemplate"/>.</returns>
        /// <exception cref="System.ArgumentNullException">templateContent</exception>
        public PageTemplate Compile(string templateName, string templateContent, Type modelType = null)
        {
            return Compile(templateName, templateContent, null, modelType);
        }

        /// <summary>
        /// Compiles a template with the specified content, name, filename and model type.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="templateContent">Content of the template.</param>
        /// <param name="templateFileName">Name of the template file.</param>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>A new instance of a <see cref="PageTemplate"/>.</returns>
        /// <exception cref="System.ArgumentNullException">templateContent</exception>
        public PageTemplate Compile(string templateName, string templateContent, string templateFileName, Type modelType = null)
        {
            if (string.IsNullOrWhiteSpace(templateContent)) throw new ArgumentNullException("templateContent");

            return CompileOrCreatePageTemplate(templateName, templateContent, templateFileName, modelType);
        }

        /// <summary>
        /// Decorates the razor code generator for the current page template to generate.
        /// </summary>
        /// <param name="incomingCodeGenerator">The incoming code generator.</param>
        /// <returns>A modified version of the code generator.</returns>
        protected virtual RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            incomingCodeGenerator.GenerateLinePragmas = true;

            var host = (RazorizerEngineHost)incomingCodeGenerator.Host;

            string templateTypeName;
            if (!host.DefaultBaseTemplateType.IsGenericType)
            {
                templateTypeName = host.DefaultBaseTemplateType.FullName;
            }
            else
            {
                var modelName = CompilerServicesUtility.ResolveCSharpTypeName(host.DefaultModelType);
                templateTypeName = host.DefaultBaseClass + "<" + modelName + ">";
            }

            var baseType = new CodeTypeReference(templateTypeName);
            incomingCodeGenerator.Context.GeneratedClass.BaseTypes.Clear();
            incomingCodeGenerator.Context.GeneratedClass.BaseTypes.Add(baseType);

            return incomingCodeGenerator;
        }

        /// <summary>
        /// Creates the markup parser. Default is <see cref="HtmlMarkupParser"/>.
        /// </summary>
        /// <returns>A markup parser.</returns>
        protected virtual ParserBase CreateMarkupParser()
        {
            return new HtmlMarkupParser();
        }

        /// <summary>
        /// Called in case of compiler errors. By default throws a <see cref="InvalidOperationException"/>.
        /// Override this method to customize handling of compiler errors.
        /// </summary>
        /// <param name="errors">The errors.</param>
        protected virtual void OnCompilerErrors(List<CompilerError> errors)
        {
            var errorText = new StringBuilder();
            errorText.AppendLine("Error when compiling page:");
            foreach (var error in errors)
            {
                errorText.AppendFormat("{0}({1},{2}): error {3}: {4}", error.FileName, error.Line, error.Column, error.ErrorNumber, error.ErrorText);
                errorText.AppendLine();
            }
            throw new InvalidOperationException(errorText.ToString());
        }

        /// <summary>
        /// Called in case of razor code generator errors. By default throws a <see cref="InvalidOperationException"/>.
        /// Override this method to customize handling of razor code generator errors.
        /// </summary>
        /// <param name="templateFileName">Name of the template file being processed.</param>
        /// <param name="errors">The errors.</param>
        protected virtual void OnCodeGeneratorErrors(string templateFileName, List<RazorError> errors)
        {
            var errorText = new StringBuilder();
            errorText.AppendLine("Error when generating Razor code:");
            foreach (var error in errors)
            {
                errorText.AppendFormat("{0}({1},{2}): error {3}: {4}", templateFileName, error.Location.LineIndex + 1, error.Location.CharacterIndex + 1, "R0000", error.Message);
                errorText.AppendLine();
            }

            throw new InvalidOperationException(errorText.ToString());
        }

        /// <summary>
        /// Creates a new instance of <see cref="PageTemplate"/> based on the type. Override this method to customize how to instantiate generated <see cref="PageTemplate"/>.
        /// </summary>
        /// <param name="pageTemplateType">Type of the page template.</param>
        /// <returns>A new instance of <see cref="PageTemplate"/>.</returns>
        protected virtual PageTemplate NewPageTemplate(Type pageTemplateType)
        {
            var pageTemplate = (PageTemplate)Activator.CreateInstance(pageTemplateType);
            pageTemplate.Razorizer = this;
            return pageTemplate;
        }

        private Type GenerateAndCompilePageTemplateType(string templateContent, string templateFileName, Type modelType, IRazorizerLanguageProvider languageProvider)
        {
            string templateClassName;
            var result = GeneratePageTemplateCode(templateContent, templateFileName, modelType, languageProvider, out templateClassName);
            if (!result.Success)
            {
                OnCodeGeneratorErrors(templateFileName, result.ParserErrors.ToList());
            }
            else
            {
                // Add the dynamic model attribute if the type is an anonymous type.
                var type = result.GeneratedCode.Namespaces[0].Types[0];
                var hasDynamicModel = (modelType != null && CompilerServicesUtility.IsAnonymousType(modelType));

                // Generate any constructors required by the base template type.
                GenerateConstructors(CompilerServicesUtility.GetConstructors(PageTemplateType), type, hasDynamicModel);

                return CompilePageTemplateCode(templateClassName, result.GeneratedCode, languageProvider.CreateCodeDomProvider());
            }

            return null;
        }

        private GeneratorResults GeneratePageTemplateCode(string templateContent, string templateFileName, Type modelType, IRazorizerLanguageProvider languageProvider, out string templateFullClassName)
        {
            var host = new RazorizerEngineHost(languageProvider.CreateCodeLanguage(), CreateMarkupParser, DecorateCodeGenerator, parserBase => languageProvider.CreateCodeParser())
            {
                DefaultBaseTemplateType = PageTemplateType,
                DefaultModelType = modelType ?? typeof(object),
                DefaultBaseClass = GetPageTemplateTypeName(PageTemplateType),
                DefaultClassName = "template" + Guid.NewGuid().ToString("N"),
                DefaultNamespace = DefaultNamespaceForCompiledPageTemplate,
                GeneratedClassContext = new GeneratedClassContext("Execute",
                    "Write",
                    "WriteLiteral",
                    "WriteTo",
                    "WriteLiteralTo",
                    typeof(LambdaWriter).FullName,
                    "DefineSection")
                {
                    ResolveUrlMethodName = "ResolveUrl"
                }
            };

            templateFullClassName = host.DefaultNamespace + "." + host.DefaultClassName;

            var templateEngine = new RazorTemplateEngine(host);
            using (var reader = new StringReader(templateContent))
            {
                return templateEngine.GenerateCode(reader, null, null, templateFileName ?? "CustomTemplate");
            }
        }
        
        private Type CompilePageTemplateCode(string templateClassName, CodeCompileUnit codeCompileUnit, CodeDomProvider codeDomProvider)
        {
            // Generate the code and put it in the text box:
            // TODO: output this text somewhere?
            string code;
            using (StringWriter sw = new StringWriter())
            {
                codeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, sw, new CodeGeneratorOptions());
                code = sw.ToString();
            }

            // Filter assemblies: Remove dynamics, check location, remove duplicated versions (taking latest)
            var assemblyNames = Assemblies
                .Where(a => !a.IsDynamic && File.Exists(a.Location))
                .GroupBy(a => a.GetName().Name)
                .Select(grp => grp.First(y => y.GetName().Version == grp.Max(x => x.GetName().Version)))
                .Select(a => a.Location).ToArray();


            // Compile an assembly in-memory
            var compilerParameters = new CompilerParameters(assemblyNames) { GenerateInMemory = true };
            var results = codeDomProvider.CompileAssemblyFromDom(compilerParameters, codeCompileUnit);

            // Handle errors here.
            Type pageTemplateType = null;
            if (results.Errors.HasErrors)
            {
                OnCompilerErrors(results.Errors.OfType<CompilerError>().ToList());
            }
            else
            {
                var assembly = results.CompiledAssembly;
                pageTemplateType = assembly.GetType(templateClassName);
            }

            return pageTemplateType;
        }
        
        private PageTemplate CompileOrCreatePageTemplate(string templateName, string templateContent, string templateFileName, Type modelType)
        {
            var templateType = GetOrCompilePageTemplateType(templateName, templateContent, templateFileName, modelType);

            // Should not happen but in case OnCodeGeneratorErrors is overloaded and not generating 
            // any exception, we have to handle this here correctly
            if (templateType == null)
            {
                throw new InvalidOperationException("Unable to create a template type. Check errors");
            }

            return NewPageTemplate(templateType);
        }

        private Type GetOrCompilePageTemplateType(string templateName, string templateContent, string templateFileName, Type modelType)
        {
            if (string.IsNullOrWhiteSpace(templateFileName))
            {
                templateFileName = templateName == null ? DefaultFileExtension : templateName + DefaultFileExtension;
            }

            var fileExtension = Path.GetExtension(templateFileName);
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ArgumentException("File extension cannot be empty. Check also DefaultFileExtension.", "templateFileName");
            }

            var languageProvider = FindLanguageProvider(fileExtension);
            if (languageProvider == null)
            {
                throw new ArgumentException(string.Format("Unable to find a registered Language Provider for extension [{0}]", fileExtension), "templateFileName");
            }

            // Check that the model type can be used with the page template
            if (modelType != null && !PageTemplateType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(string.Format("Cannot use ModelType [{0}] when the PageTemplateType [{1}] is already setting the model type", modelType, PageTemplateType));
            }

            // Use an object type in case of a null model type
            if (modelType == null)
            {
                modelType = typeof(object);
            }

            // When template name is null, generate a generic template name
            if (templateName == null)
            {
                templateName = ComputeCacheKey(templateContent, templateFileName, modelType);
            }

            // Create a template type
            Type pageTemplateType;
            lock (generatedPageTemplateTypes)
            {
                if (!generatedPageTemplateTypes.TryGetValue(templateName, out pageTemplateType))
                {
                    pageTemplateType = GenerateAndCompilePageTemplateType(templateContent, templateFileName, modelType,
                        languageProvider);
                    if (pageTemplateType != null)
                    {
                        generatedPageTemplateTypes.Add(templateName, pageTemplateType);
                    }
                }
            }

            return pageTemplateType;
        }
        
        private static string ComputeCacheKey(string templateContent, string templateFileName, Type modelType)
        {
            var sha256 = SHA256.Create();
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 4096, true))
            {
                writer.Write(templateContent);
                writer.Write("+");
                writer.Write(templateFileName);
                writer.Write("+");
                writer.Write(modelType);
                writer.Flush();
            }
            memoryStream.Position = 0;
            return Convert.ToBase64String(sha256.ComputeHash(memoryStream));
        }

        private static string GetPageTemplateTypeName(Type templateType)
        {
            if (!templateType.IsGenericTypeDefinition || !templateType.IsGenericType)
                return templateType.FullName;
            return string.Format("{0}.{1}", templateType.Namespace, templateType.Name.Substring(0, templateType.Name.IndexOf('`')));
        }


        private static void GenerateConstructors(IEnumerable<ConstructorInfo> constructors, CodeTypeDeclaration codeType, bool hasDynamicModel)
        {
            if (constructors == null || !constructors.Any())
                return;

            var existingConstructors = codeType.Members.OfType<CodeConstructor>().ToArray();
            foreach (var existingConstructor in existingConstructors)
                codeType.Members.Remove(existingConstructor);

            foreach (var constructor in constructors)
            {
                var ctor = new CodeConstructor();
                ctor.Attributes = MemberAttributes.Public;

                foreach (var param in constructor.GetParameters())
                {
                    ctor.Parameters.Add(new CodeParameterDeclarationExpression(param.ParameterType, param.Name));
                    ctor.BaseConstructorArgs.Add(new CodeSnippetExpression(param.Name));
                }

                ctor.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "HasDynamicModel"), 
                    new CodePrimitiveExpression(hasDynamicModel)));

                codeType.Members.Add(ctor);
            }
        }

        private static string NormalizeExtension(string fileExtension)
        {
            if (fileExtension == null) throw new ArgumentNullException("fileExtension");
            return
                (fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension).ToLower(
                    CultureInfo.InvariantCulture);
        }
    }
}