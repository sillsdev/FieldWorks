// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ManagedPictureFactoryTests.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;

using NUnit.Framework;
using System.IO;
using SIL.Utils.ComTypes;
using System.Drawing.Imaging;
using SIL.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary/>
	[TestFixture]
	public class ManagedPictureFactoryTests // can't derive from BaseTest because of dependencies
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
