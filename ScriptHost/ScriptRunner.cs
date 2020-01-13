using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;


namespace csharpscripting
{
	[ComVisible(true)]
	[ProgId("CSharpScripting.ScriptRunner")]
	[Guid("C8F31783-11F1-4177-B9DB-6899CA531DBA")]
	[ClassInterface(ClassInterfaceType.None)]
	public class ScriptRunner : IScriptRunner
	{
		private Type mainType = null;
		private Object mainObject = null;
		private MethodInfo mainMethod = null;
		private String scriptDirectory = null;
		private readonly List<String> debugMessages = new List<String>();

		public ScriptRunner()
		{
			AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
		}

		private Assembly OnAssemblyResolve(Object sender, ResolveEventArgs args)
		{
			if (!String.IsNullOrEmpty(this.scriptDirectory))
			{
				var assemblyName = new AssemblyName(args.Name);
				var dllPath = Path.Combine(this.scriptDirectory, assemblyName.Name + ".dll");
				if (File.Exists(dllPath))
					return Assembly.LoadFrom(dllPath);
			}
			return null;
		}

		private static Assembly Compile(StringBuilder scriptBuilder, IEnumerable<String> dependencies)
		{
			using (var provider = new CSharpCodeProvider())
			{
				var parameters = new CompilerParameters
				{
					GenerateExecutable = false,
					GenerateInMemory = true,
					CompilerOptions = "/optimize /langversion:5"
				};
				parameters.ReferencedAssemblies.AddRange(dependencies.ToArray());

				var compilerResults = provider.CompileAssemblyFromSource(parameters, scriptBuilder.ToString());
				if (compilerResults.Errors.HasErrors)
					throw new Exception(compilerResults.Errors.Cast<CompilerError>().First().ErrorText);	// TODO: Improve error reporting

				return compilerResults.CompiledAssembly;
			}
		}

		private IReadOnlyList<String> ProcessLines(StreamReader file, StringBuilder scriptBuilder)
		{
			var dependencies = new List<String>();
			while (!file.EndOfStream)
			{
				var line = file.ReadLine();
				var match = Regex.Match(line, "\\/\\/\\s*#require\\s*\"(?<dep>[\\w.]*)\"");				
				if (match.Success && match.Groups["dep"].Success)
				{
					var dep = match.Groups["dep"].Value;
					var dllFilename = dep.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) ? dep : dep + ".dll";
					var localDllFilename = Path.Combine(this.scriptDirectory, dllFilename);
					dependencies.Add(File.Exists(localDllFilename) ? localDllFilename : dllFilename);
				}
				scriptBuilder.AppendLine(line);
			}
			return dependencies;
		}

		public void LoadScript(String filename, String entryFunction)
		{
			this.scriptDirectory = Path.GetDirectoryName(filename);
			using (var file = File.OpenText(filename))
			{
				var scriptBuilder = new StringBuilder();
				var dependencies = this.ProcessLines(file, scriptBuilder);
				var scriptAssembly = Compile(scriptBuilder, dependencies);
				this.FindTypes(entryFunction, scriptAssembly);
			}
		}

		private void FindTypes(string entryFunction, Assembly scriptAssembly)
		{
			this.mainMethod = scriptAssembly
				.DefinedTypes
				.SelectMany(type => type.GetMethods())
				.FirstOrDefault(method => method.Name == entryFunction);
			this.mainType = this.mainMethod?.DeclaringType;
			this.mainObject = this.mainType?.CreateInstance();
			var dbgEvent = this.mainType
				?.GetEvents()
				?.Where(evInfo => evInfo.EventHandlerType == typeof(EventHandler<String>) && evInfo.Name.Like("Debug"))
				?.FirstOrDefault();
			dbgEvent?.AddEventHandler(this.mainObject, Delegate.CreateDelegate(dbgEvent.EventHandlerType, this, nameof(OnDebugMessage)));
		}

		public bool Initialized()
		{
			return this.mainObject != null && this.mainMethod != null;
		}

		private void OnDebugMessage(Object sender, String message)
		{
			this.debugMessages.Add(message);
		}

		public void Execute(IScriptParams scriptParams)
		{
			if (!this.Initialized())
				throw new InvalidOperationException("Script not initialized!");

			this.debugMessages.Clear();
			try
			{
				if (scriptParams is ScriptParams input)
				{
					input.Parameters.Apply(setProperty);
					input.Images.Apply(setProperty);
				}
				this.mainMethod.Invoke(this.mainObject, null);
				if (scriptParams is ScriptParams output)
				{
					this.mainType
						.GetProperties()
						.Select(prop => new { Property = prop, Getter = prop.GetGetMethod(), Setter = prop.GetSetMethod() })
						.Where(info => info.Getter != null && info.Setter == null)
						.Apply(info => output.Results.Add(info.Property.Name, info.Property.GetValue(this.mainObject).ToString()));
				}
			}
			catch (TargetInvocationException exc)
			{
				if (exc.InnerException != null)
					throw exc.InnerException;
				else
					throw new COMException(this.debugMessages.LastOrDefault() ?? exc.Message, exc.HResult);
			}

			void setProperty<T>(KeyValuePair<String, T> par)
			{
				var paramProperty = this.mainType.GetProperty(par.Key);
				if (paramProperty?.SetMethod != null)
				{
					var targetType = paramProperty.PropertyType;
					var value = Convert.ChangeType(par.Value, targetType);
					paramProperty.SetValue(this.mainObject, value);
				}
			}
		}

	}
}
