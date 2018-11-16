// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[TestFixture]
	public class ManagedPictureFactoryTests
	{
		/// <summary />
		[Test]
		public void ImageFromBytes_SimpleImage_Success()
		{
			const int width = 100;
			const int height = 110;

			// Create test data
			using (var testBitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb))
			using (var stream = new MemoryStream())
			{
				testBitmap.Save(stream, ImageFormat.Bmp);
				var byteArray = stream.ToArray();

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
