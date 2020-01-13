using System;
using System.Runtime.InteropServices;

namespace csharpscripting
{
	[ComVisible(true)]
	[Guid("9845390E-A748-4E84-8775-AE226C3729F0")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IScriptRunner
	{
		void LoadScript([MarshalAs(UnmanagedType.BStr)]String filename, [MarshalAs(UnmanagedType.BStr)]String entryFunction);
		bool Initialized();
		void Execute(IScriptParams scriptParams);
	}
}
