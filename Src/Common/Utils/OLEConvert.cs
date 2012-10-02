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

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// This helper class allows access to protected members of AxHost
	/// used to convert Image and Font objects to OLE objects
	/// </summary>
	public class OLECvt : System.Windows.Forms.AxHost
	{
		private OLECvt() : base("")
		{
		}
		/// <summary>
		///  convert an Image to an OLE Picture object
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static stdole.StdPicture ToOLEPic(Image i)
		{
			return AxHost.GetIPictureDispFromPicture(i) as stdole.StdPicture;
		}

		/// <summary>
		/// convert an Image to an OLE Picture IPictureDisp interface
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static stdole.IPictureDisp ToOLE_IPictureDisp(Image i)
		{
			return AxHost.GetIPictureDispFromPicture(i) as stdole.IPictureDisp;
		}

		/// <summary>
		/// convert a Font to an OLE Font object
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public static stdole.StdFont ToOLEFont(Font f)
		{
			return AxHost.GetIFontFromFont(f) as stdole.StdFont;
		}

		/// <summary>
		/// convert a Font to an OLE Font IFontDisp interface
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public static stdole.IFontDisp ToOLE_IFontDisp(Font f)
		{
			return AxHost.GetIFontFromFont(f) as stdole.IFontDisp;
		}
	}
}
