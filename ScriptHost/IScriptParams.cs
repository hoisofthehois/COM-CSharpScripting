using System;
using System.Runtime.InteropServices;

namespace csharpscripting
{
	[ComVisible(true)]
	[Guid("98340634-1CB5-4C1F-8DC9-5E4A7AAE0CFA")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IScriptParams
	{
		void SetImage([MarshalAs(UnmanagedType.BStr)]String key, int width, int height, int stride, IntPtr data);
		void SetParam([MarshalAs(UnmanagedType.BStr)]String key, [MarshalAs(UnmanagedType.BStr)]String value);
		[return: MarshalAs(UnmanagedType.BStr)] String GetResult([MarshalAs(UnmanagedType.BStr)]String key);
	}
}
