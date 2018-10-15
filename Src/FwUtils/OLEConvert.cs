using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Helper methods to convert a picture to/from OLE
	/// </summary>
	public class OLEConvert : AxHost
	{
		private OLEConvert() : base(string.Empty)
		{
			// this class is only intended to be used as static class. The only reason we
			// can't make this a static class is that we need to derive from AxHost to be able
			// to access the protected GetIPictureDispFromPicture method.
		}

		/// <summary>
		/// convert an Image to an OLE Picture IPictureDisp interface
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public static IPictureDisp ToOLE_IPictureDisp(Image image)
		{
			if (Platform.IsWindows)
				return AxHost.GetIPictureDispFromPicture(image) as IPictureDisp;
			return ImagePicture.FromImage(image);
		}

		/// <summary>
		/// Converts the image to (an OLECvt) IPicture picture and wraps it with a disposable object.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns></returns>
		public static ComPictureWrapper ConvertImageToComPicture(Image image)
		{
			return new ComPictureWrapper((IPicture)ToOLE_IPictureDisp(image));
		}
	}
}
