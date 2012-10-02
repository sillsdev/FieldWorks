using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.CommandBars;

namespace FwNantAddin2
{
	/// <summary>
	/// Summary description for ImageHelper.
	/// </summary>
	public class ImageHelper
	{
		/// <summary>
		/// prevent construction (only static methods)
		/// </summary>
		private ImageHelper()
		{
		}

		/// <summary>
		/// Sets an icon on a CommandBarButton
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="bitmap"></param>
		/// <param name="mask"></param>
		public static void SetPicture(CommandBarButton button, string bitmapName, string maskName)
		{
			Assembly assembly = Assembly.GetAssembly(typeof(ImageHelper));
			System.IO.Stream stream = assembly.GetManifestResourceStream(typeof(ImageHelper),
				bitmapName);
			Bitmap bitmap = (Bitmap)Bitmap.FromStream(stream);
			stream = assembly.GetManifestResourceStream(typeof(ImageHelper), maskName);
			Bitmap mask = (Bitmap)Bitmap.FromStream(stream);

			stdole.IPictureDisp picDisp = GetIPictureDispFromHandle(bitmap.GetHbitmap());
			button.Picture = (stdole.StdPicture)picDisp;
			picDisp = GetIPictureDispFromHandle(mask.GetHbitmap());
			button.Mask = (stdole.StdPicture)picDisp;
		}

		protected static stdole.IPictureDisp GetIPictureDispFromHandle(IntPtr hIntPtr)
		{
			object objIPictureDisp = null;
			Guid objGuid = new Guid("00020400-0000-0000-C000-000000000046");
			int result;
			Win32.PICTDESC pd = new Win32.PICTDESC(hIntPtr);
			result = Win32.OleCreatePictureIndirect(ref pd, ref objGuid, 1, ref objIPictureDisp);
			if( result != 0 )
			{
				System.Windows.Forms.MessageBox.Show("Conversion of bitmap failed");
			}
			return (stdole.IPictureDisp)objIPictureDisp;
		}
	}

	public class Win32
	{
		public struct PICTDESC
		{
			internal int cbSizeofstruct;
			internal int picType;
			internal IntPtr hbitmap;
			internal IntPtr hpal;
			internal int unused;
			public PICTDESC(IntPtr hBitmap)
			{
				this.cbSizeofstruct = Marshal.SizeOf(typeof(PICTDESC));
				this.picType = 1;
				this.hbitmap = hBitmap;
				this.hpal = IntPtr.Zero;
				this.unused = 0;
			}
		}
		[DllImport("olepro32.dll")]
		public static extern int OleCreatePictureIndirect(ref PICTDESC pPictDesc,
			ref Guid riid, int fOwn, [MarshalAs(UnmanagedType.IDispatch)] ref object
			ppvObj);
	}
}
