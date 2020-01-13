# C# Scripting in Legacy C++ Applications

## Why implement C# scripting?

Many developers who work on legacy applications, written in C++ for instance, wish to extend the capabilities of their project in a quick and simple way. Scripting is a feature that serves that purpose, and thus the possibility to load and run scripts has been implemented in various programs.

The most populare programming language for scripting seems to be Python at the moment, due to its huge ecosystem of libraries and its popularity in the field of AI. However, writing scripts in a more sophisticated language - i.e. C# - has many advantages:

- The scripts can be compiled upon loading, resulting in optimized code and faster execution speed.
- C# is quite well known among desktop and legacy developers, who are the target audience here.
- The amount of libraries and tools available for .NET is (almost) as big as for Python.
- C#, targeting the .NET Framework, does not need any additional rumtime, as .NET comes with Windows in general.

Especially the last item may be important when it comes to using any scripting features in an actual production environment.

## Implementation (details) of this project

### Loading and compiling a C# script

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

## Open issues

