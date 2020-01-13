using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace csharpscripting
{
	[ComVisible(true)]
	[ProgId("CSharpScripting.ScriptParams")]
	[Guid("CFCB3B8E-8462-4FD7-AB27-04EBC255F9E7")]
	[ClassInterface(ClassInterfaceType.None)]
	public class ScriptParams : IScriptParams
	{
		private static PixelFormat CalcFormat(int width, int stride)
		{
			var ratio = stride / width;
			if (ratio == 1)
				return PixelFormat.Format8bppIndexed;
			else if (ratio == 3)
				return PixelFormat.Format24bppRgb;
			else if (ratio == 4)
				return PixelFormat.Format32bppRgb;
			else
				throw new ArgumentOutOfRangeException(nameof(stride), "Image width/stride mismatch");
		}

		public void SetImage(String key, int width, int height, int stride, IntPtr data)
		{
			try
			{
				var bmp = new Bitmap(width, height, stride, CalcFormat(width, stride), data);		
				this.images[key] = bmp;
			} catch (Exception exc)
			{
				Debug.WriteLine(exc.Message);
				throw;
			}
		}

		public void SetParam(String key, String value)
		{
			this.parameters[key] = value;
		}

		public String GetResult(String key)
		{
			return this.Results[key];
		}

		internal IReadOnlyDictionary<String, String> Parameters => this.parameters;
		internal IReadOnlyDictionary<String, Bitmap> Images => this.images;
		internal IDictionary<String, String> Results { get; } = new Dictionary<String, String>();

		private readonly Dictionary<String, String> parameters = new Dictionary<String, String>();
		private readonly Dictionary<String, Bitmap> images = new Dictionary<String, Bitmap>();

	}
}
