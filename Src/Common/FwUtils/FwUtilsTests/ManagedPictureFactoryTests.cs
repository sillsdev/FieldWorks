// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using NUnit.Framework;
using System.IO;
using System.Drawing.Imaging;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary/>
	[TestFixture]
	public class ManagedPictureFactoryTests
	{
		/// <summary></summary>
		[Test]
		public void ImageFromBytes_SimpleImage_Success()
		{
			const int width = 100;
			const int height = 110;

			// Create test data
			using (Bitmap testBitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb))
			{
				using (MemoryStream stream = new MemoryStream())
				{
					testBitmap.Save(stream, ImageFormat.Bmp);
					byte[] byteArray = stream.ToArray();

					// Perform the test.
					var factory = new ManagedPictureFactory();
					using (var pic = (ImagePicture)factory.ImageFromBytes(byteArray, byteArray.Length))
					{
						pic.ReferenceOwnedByNative = false;
					// Test the result.
					Assert.NotNull(pic, "ImageFromBytes returned null");
					Assert.AreEqual(new HiMetric(width, pic.DpiX).Value, pic.Width);
					Assert.AreEqual(new HiMetric(height, pic.DpiY).Value, pic.Height);
				}
			}
		}
	}
}
}
