**SharpRazor** is a *lightweight templating system* based on the powerful *Razor* templating stack.

## Usage

```csharp
var razorizer = new Razorizer();
var result = razorizer.Parse("<p>Hello @Model.Name!</p>", new { Name = "Razor" });
```
   
Output:

```html
	<p>Hello Razor!</p>
```

## Features

**SharpRazor** provides the following features:

 - Simple and easy interface, with a single entry point [Razorizer](T:SharpRazor.Razorizer) class and mainly two methods:
  - `Razorizer.Parse` to directly parse a template 
  - `Razorizer.Compile` to precompile template page
 - C# Code Language and HTML Markup Language (aka `cshtml` files)
 - [MVC3 Model](http://weblogs.asp.net/scottgu/archive/2010/10/19/asp-net-mvc-3-new-model-directive-support-in-razor.aspx) aka `@model` directive 
 - [MVC3 Layout](http://weblogs.asp.net/scottgu/archive/2010/10/22/asp-net-mvc-3-layouts.aspx) and [Sections](http://weblogs.asp.net/scottgu/archive/2010/12/30/asp-net-mvc-3-layouts-and-sections-with-razor.aspx)
 - Caching of generated page template types
 - Custom page template class inheriting from [PageTemplate<TModel>](T:SharpRazor.PageTemplate`1)
 - Allow debugging of template code (breakpoints, step-in...etc. from VS debugger) when loading template from a location on the disk (By specifying a `templateFilePath`).
 - Very lightweight, packed into an assembly weighting less than *40Kb* (without counting `System.Web.Razor` dependency)
 - Compatible with `.NET 4.5+` and `System.Web.Razor 3.0`

## Available from Nuget 
You can download **SharpRazor** binaries directly from [nuget](http://www.nuget.org/packages?q=sharprazor).

## License
MIT
