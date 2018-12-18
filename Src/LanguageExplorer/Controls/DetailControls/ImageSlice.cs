// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Drawing;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class ImageSlice : Slice
	{
		/// <summary />
		/// <param name="distFilesPath">the path to the distfiles Directory</param>
		/// <param name="relativeImagePath">they path to the image, relative to the distfiles Directory</param>
		/// <remarks>Will throw an exception if the image is not found.</remarks>
		public ImageSlice(string distFilesPath, string relativeImagePath) : base(new PictureBox())
		{
			var sPathname = System.IO.Path.Combine(distFilesPath, relativeImagePath);
			var controlAsPictureBox = (PictureBox)Control;
			controlAsPictureBox.Image = Image.FromFile(FileUtils.ActualFilePath(sPathname));
			controlAsPictureBox.Height = controlAsPictureBox.Image.Height;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Prevents memory leaks and also spurious continued locking of the file.
				var picBox = Control as PictureBox;
				if (picBox?.Image != null)
				{
					picBox.Image.Dispose();
					picBox.Image = null;
				}
			}
			base.Dispose(disposing);
		}
	}
}