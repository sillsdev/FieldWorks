//
// This helper class allows access to protected members of AxHost
// used to convert Image and Font objects to OLE objects as used
// in most ActiveX controls.
//
// This file (OLEConvert.cs) must be added to your project.
// Using Visual Studio.NET's menu command "Project, Add Existing Item..."
// add the file OLEConvert.cs to your project
//
// To use this helper class, add the following using statement
// to your project file(s).
//
// using OLEConvert
//
// EB/2009-08-20: The only method we currently use is OLECvt.ToOLE_IPictureDisp.
// If we need the other methods we should copy the interface definitions from
// stdole to COMInterfaces/ComWrapper.cs since stdole isn't available on Linux.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.Utils.ComTypes;

namespace SIL.Utils
{
	/// <summary>
	/// This helper class allows access to protected members of AxHost
	/// used to convert Image and Font objects to OLE objects
	/// </summary>
	public class OLECvt
#if !__MonoCS__
		: AxHost
#endif
	{
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Offending code compiles only on Windows")]
		private OLECvt()
#if !__MonoCS__
			: base("")
#endif
		{
		}

#if UNUSED
		/// <summary>
		///  convert an Image to an OLE Picture object
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static StdPicture ToOLEPic(Image i)
		{
			return AxHost.GetIPictureDispFromPicture(i) as StdPicture;
		}
#endif

		/// <summary>
		/// convert an Image to an OLE Picture IPictureDisp interface
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Offending code compiles only on Windows")]
		public static IPictureDisp ToOLE_IPictureDisp(Image image)
		{
#if !__MonoCS__
			return AxHost.GetIPictureDispFromPicture(image) as IPictureDisp;
#else
			return ImagePicture.FromImage(image);
#endif
		}

#if UNUSED
		/// <summary>
		/// convert a Font to an OLE Font object
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public static StdFont ToOLEFont(Font f)
		{
			return AxHost.GetIFontFromFont(f) as StdFont;
		}

		/// <summary>
		/// convert a Font to an OLE Font IFontDisp interface
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public static IFontDisp ToOLE_IFontDisp(Font f)
		{
			return AxHost.GetIFontFromFont(f) as IFontDisp;
		}
#endif
	}
}
