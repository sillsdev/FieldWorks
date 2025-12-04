// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ImagePicture class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImagePictureTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ImagePicture class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImagePictureClass()
		{
			const int width = 100;
			const int height = 200;
			using (Image testImage = new Bitmap(width, height))
			{
				using (ImagePicture i = ImagePicture.FromImage(testImage))
				{
					Assert.That(i.Width, Is.EqualTo(new HiMetric(width, i.DpiX).Value), "A1");
					Assert.That(i.Height, Is.EqualTo(new HiMetric(height, i.DpiY).Value), "A2");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests HiMetric with 96 dpi
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HimetricDpi96()
		{
			const int dpi = 96;
			const int pixels = 100;

			HiMetric h1 = new HiMetric(pixels, dpi);
			HiMetric h2 = new HiMetric(h1.Value);
			Assert.That(h2.Value == h1.Value, Is.True, "A1");
			Assert.That(h2.GetPixels(dpi) == h1.GetPixels(dpi), Is.True, "A2");

			Assert.That(h2.GetPixels(dpi) == pixels, Is.True, "A3");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests HiMetric with 200 dpi
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HimetricDpi200()
		{
			const int dpi = 200;
			const int pixels = 100;

			HiMetric h1 = new HiMetric(pixels, dpi);
			HiMetric h2 = new HiMetric(h1.Value);
			Assert.That(h2.Value == h1.Value, Is.True, "A1");
			Assert.That(h2.GetPixels(dpi) == h1.GetPixels(dpi), Is.True, "A2");

			Assert.That(h2.GetPixels(dpi) == pixels, Is.True, "A3");
		}
	}
}
