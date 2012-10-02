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
// File: IPicture.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------

using System.Drawing;
using NUnit.Framework;

namespace SIL.Utils
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
					Assert.AreEqual(new HiMetric(width, i.DpiX).Value, i.Width, "A1");
					Assert.AreEqual(new HiMetric(height, i.DpiY).Value, i.Height, "A2");
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
			Assert.IsTrue(h2.Value == h1.Value, "A1");
			Assert.IsTrue(h2.GetPixels(dpi) == h1.GetPixels(dpi), "A2");

			Assert.IsTrue(h2.GetPixels(dpi) == pixels, "A3");
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
			Assert.IsTrue(h2.Value == h1.Value, "A1");
			Assert.IsTrue(h2.GetPixels(dpi) == h1.GetPixels(dpi), "A2");

			Assert.IsTrue(h2.GetPixels(dpi) == pixels, "A3");
		}
	}
}
