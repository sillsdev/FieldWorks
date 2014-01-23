// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ManagedPictureFactoryTests.cs
// Responsibility:

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
		/// <summary/>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			// Set stub for messagebox so that we don't pop up a message box when running tests.
			MessageBoxUtils.Manager.SetMessageBoxAdapter(new MessageBoxStub());
		}

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
