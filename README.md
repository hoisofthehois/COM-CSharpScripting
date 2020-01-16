# C# Scripting in Legacy C++ Applications

## Why implement C# scripting?

Many developers who work on legacy applications, written in C++ for instance, wish to extend the capabilities of their project in a quick and simple way. Scripting is a feature that serves that purpose, and thus the possibility to load and run scripts has been implemented in various programs.

The most popular programming language for scripting seems to be Python at the moment, due to its huge ecosystem of libraries and its popularity in the field of AI. However, writing scripts in a more sophisticated language - that is, C# - has many advantages:

- The scripts can be compiled upon loading, resulting in optimized code and faster execution speed.
- C# is quite well known among desktop and legacy developers, who are the target audience here.
- The amount of libraries and tools available for .NET is (almost) as big as for Python.
- In Windows, there is rich interop possibilities in between .NET and C/C++ (e.g. P/Invoke, user-defined marshalling etc.).
- C#, targeting the .NET Framework, does not need any additional rumtime, as .NET comes with Windows in general.

Especially the last item may be crucial when it comes to deploying a scripting feature in an actual production environment.

This repository provides a working example of how scripting for C# (or any .NET language) can be implemented and made available to C++. It consists of a library *ScriptHost* that implements the actual script compilation and execution, a test script that applies a simple image processing operation to a `Bitmap`, and a C++ application running that script using the *ScriptHost* library.

## Implementation (details) of this project

The scripting example consists of three projects, as mentioned above: The *ScriptHost*, the client, and the actual script. Host and client collaborate in order to load and compile the script file, resolve any dependencies to other assemblies that the script may have, passing parameters to the script, executing it and retrieving the results.

In the following, each of these steps is described in detail.

### Loading and compiling a C# script

The most important functionality of C# scripting is to load and compile a user-specified file containing C# code. A lot of examples can be found of how to accomplish that task. A common approach, which is also implemented here, is to use the built-in compilation tools from .NET, that is `Microsoft.CSharp.CSharpCodeProvider` ([docs](https://docs.microsoft.com/en-us/dotnet/api/microsoft.csharp.csharpcodeprovider?view=netframework-4.6.2)) along with `System.CodeDom.Compiler.CompilerParameters` ([docs](https://docs.microsoft.com/en-us/dotnet/api/system.codedom.compiler.compilerparameters?view=netframework-4.6.2)).

The following piece of code will compile a script, specified as a `String`, into an assembly:
```csharp
Assembly compiledAssembly = null;
using (var provider = new CSharpCodeProvider())
{
  var parameters = new CompilerParameters
  {
    GenerateExecutable = false,
    GenerateInMemory = true,
    CompilerOptions = "/optimize /langversion:5"
  };
  var compilerResults = provider.CompileAssemblyFromSource(parameters, script);
  if (compilerResults.Errors.HasErrors)
    throw new Exception(compilerResults.Errors.Cast<CompilerError>().First().ErrorText);
  compiledAssembly = compilerResults.CompiledAssembly;
}
```
The script must contain valid code that is actually compilable, i.e. classes or structs, no free functions, all necessary `using namespace` declarations etc. As for resolving dependencies to other assemblies, see below.

If the compilation is not successful, the resulting `CompilerResults` object will contain a non-empty `CompilerErrorCollection`. For simplicity, the first error that occured is thrown as an `Exception` here. If it is sucessful, the compiled assembly can be investigated using reflection:
```csharp
var mainMethod = scriptAssembly
  .DefinedTypes
  .SelectMany(type => type.GetMethods())
  .FirstOrDefault(method => method.Name == entryFunction);
var mainObject = this.mainMethod?.DeclaringType?.CreateInstance();
```
Here, `entryFunction` is the name of a member function that will be called later to run the script; the `mainObject` is an instance of the class in which that function is declared. `CreateInstance` is just an extension method that wraps `Activator.CreateInstance` which was written in order to be able to use the null-conditional call syntax.

### Managing script dependencies

### Parameter input and output

### Exposing the C# script host to C++

## Example from image processing

### The C# script
```csharp
/// <remarks>
/// Dependencies are listed using the following syntax.
/// They are searched in the GAC as well as in the script directory.
/// </remarks>
// #require "System.dll"
// #require "System.Drawing.dll"
// #require "Accord.Imaging.dll"

using System;
using System.IO;
using System.Drawing;
using Accord.Imaging.Filters;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace TestScript
{
  /// <summary>
  /// Example script file; class name is arbitrary, and the class doesn't need not be public.
  /// </summary>
  public class Script
  {

    /// <summary>
    /// An <see cref="EventHandler<String>"/> event named "Debug" or "debug" is 
    /// automatically recognized for message output.
    /// </summary>
    public event EventHandler<String> Debug;

    /// <summary>
    /// Properties with private getter functions are considered as input parameters.
    /// </summary>
    public String OutDir { private get; set; }

    /// <summary>
    /// Parameters can have any type that is convertible from and to <see cref="String"/>.
    /// </summary>
    public int FilterSize { private get; set; }

    /// <summary>
    /// Images are transfered as <see cref="Bitmap"/>s and are always in/out parameters.
    /// </summary>
    public Bitmap WorkImage { private get; set; }

    /// <summary>
    /// Properties with private setter functions are considered output, i.e. result parameters.
    /// </summary>
    public double Elapsed { get; private set; }

    /// <summary>
    /// The script entry point can have an arbitrary name and needn't be public.
    /// It must be an parameter-less member function
    /// </summary>
    public void RunScript()
    {
      try
      {
        Debug(this, "Applying filter to WorkImage..."); // sending a debug message to the host
        var timer = Stopwatch.StartNew();
        this.ApplyFilter();
        this.Elapsed = timer.Elapsed.TotalSeconds;
        Debug(this, "Operation took " + this.Elapsed + "sec");
        this.SaveImage(DateTime.Now);
      }
      catch (Exception exc)
      {
        Debug(this, exc.Message);
        throw;  // Exceptions are eventually translated to appropriate HRESULT values in C++.
      }
    }

    /// <summary>
    /// The script can have public and private methods, fields etc. as any ordinary class.
    /// </summary>
    private void ApplyFilter()
    {
      var filter = new Median(this.FilterSize);
      filter.ApplyInPlace(this.WorkImage);  // Images are in/out parameters.
    }

    private void SaveImage(DateTime now)
    {
      var bmpFile = Path.Combine(this.OutDir, now.ToFileTime() + ".bmp");
      this.Debug(this, "Saving image to " + bmpFile);
      this.WorkImage.Save(bmpFile, ImageFormat.Bmp);
    }

    private readonly Font font = new Font("Arial", 36f);

    /// <summary>
    /// A <see cref="Main"/> method can be provided in order to be able to compile the script independently (e.g. for testing).
    /// </summary>
    [STAThread]
    static void Main()
    {
      var now = DateTime.Now;
      var prog = new Script { OutDir = @"\Test", FilterSize = 11 };
      prog.RunScript();
    }

  }
}
```

### The "legacy" client

---

## Current issues

## Next steps

- [ ] Error handling during script compilation should be improved; instead of a generic exception, a dedicated class with appropriate `HRESULT` (that is, deriving from `COMError`), could be implemented.
- [ ] `Microsoft.CSharp.CSharpCodeProvider` supports C# 5 only. In order to compile scripts written in a more recent language version, the *.NET Compiler Platform* / [*Roslyn*](https://github.com/dotnet/roslyn) should be used.
- [ ] Although passing BLOB-style objects like `Bitmap`s into and out of the script can be sufficient in certain areas, being with composed objects (collections, trees etc.) would be an even more handy feature. If these types were exposed to COM, it may be possible to use them in .NET, and thus within the script.

## References

- **CSharpCodeProvider Class**: https://docs.microsoft.com/en-us/dotnet/api/microsoft.csharp.csharpcodeprovider?view=netframework-4.6.2
- **CompilerParameters Class**: https://docs.microsoft.com/en-us/dotnet/api/system.codedom.compiler.compilerparameters?view=netframework-4.6.2
- **C# COM server and client example**: https://stackoverflow.com/q/19874230/2380654
- **How to: Configure .NET Framework-Based COM Components for Registration-Free Activation**: https://docs.microsoft.com/en-us/dotnet/framework/interop/configure-net-framework-based-com-components-for-reg
- **#import directive (C++)**: https://docs.microsoft.com/en-us/cpp/preprocessor/hash-import-directive-cpp?view=vs-2019
- **How to: Reference .NET Types from COM**: https://docs.microsoft.com/en-us/dotnet/framework/interop/how-to-reference-net-types-from-com
- **A quick guide to Registration-Free COM in .Net–and how to Unit Test it**: http://blog.functionalfun.net/2012/09/a-quick-guide-to-registration-free-com.html
- **IErrorInfo interface**: https://docs.microsoft.com/en-us/windows/win32/api/oaidl/nn-oaidl-ierrorinfo
- **\_com_error Class**: https://docs.microsoft.com/en-us/cpp/cpp/com-error-class?view=vs-2019
- **Image processing** (Accord.Net): https://github.com/accord-net/framework/wiki/Imaging
