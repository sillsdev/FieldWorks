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
// File: ExtraComInterfacesTests.cs
// Responsibility: Linux team
//
// <remarks>
// Currently the following VwGraphics methods/properties are tested (at least trivialy) by these unit tests
// ForeColor, BackColor, DrawRectangle, PushClipRect, PopClipRect, GetClipRect, GetTextExtent, DrawText
//
// The following VwGraphics methods/properties are not yes tested by these unit tests
// InvertRect, DrawHorzLine, DrawLine, DrawTextExt, GetTextLeadWidth,
// GetFontEmSquare, GetGlyphMetrics, GetFontData, GetFontDataRgch, FontAscent, FontDescent,
// FontCharProperties, XUnitsPerInch, YUnitsPerInch, GetSuperscriptHeightRatio, GetSuperscriptYOffsetRatio,
// GetSubscriptHeightRatio, GetSubscriptYOffsetRatio, SetupGraphics, DrawPolygon, RenderPicture, MakePicture
//
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary/>
	[TestFixture]
	public class VwGraphicsTests
	{
		internal class GraphicsObjectFromImage : IDisposable
		{
			readonly Bitmap m_bitmap;
			readonly Graphics m_graphics;

			public GraphicsObjectFromImage(int width, int height)
			{
				// create a graphics object from a 1000,1000 bitmap image.
				m_bitmap = new Bitmap(width, height);
				m_graphics = Graphics.FromImage(m_bitmap);
			}

			public GraphicsObjectFromImage()
			{
				// create a graphics object from a 1000,1000 bitmap image.
				m_bitmap = new Bitmap(1000, 1000);
				m_graphics = Graphics.FromImage(m_bitmap);
			}

			public Graphics Graphics
			{
				get { return m_graphics; }
			}

			public Bitmap Bitmap
			{
				get { return m_bitmap; }
			}

			#region IDisposable Members
			#if DEBUG
			/// <summary/>
			~GraphicsObjectFromImage()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_graphics.Dispose();
					m_bitmap.Dispose();
				}
				IsDisposed = true;
			}
			#endregion
		}

		/// <summary>
		/// Tests Initializing and releasing of a VwGraphics Objects from a hdc.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void SimpleCreationAndRelease()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				vwGraphics.Initialize(gr.Graphics.GetHdc());
				vwGraphics.ReleaseDC();
			}
		}

		/// <summary>
		/// Make sure our initialization of the character property engine works.
		/// (This test doesn't really belong here but it didn't seem worth creating a whole new test
		/// class which would have to register all the DLLs just for one test. And this one
		/// is important...it's the only one that verifies that our ICU overrides are working
		/// when the ICU directory is initialized from C#.)
		/// </summary>
		[Test]
		public void CharacterPropertyOverrides()
		{
			Icu.InitIcuDataDir();
			var cpe = LgIcuCharPropEngineClass.Create();
			var result = cpe.get_GeneralCategory('\xF171');
			Assert.That(result, Is.EqualTo(LgGeneralCharCategory.kccMn));
		}

		/// <summary>
		/// Compares R,G,B values of two Colors
		/// </summary>
		/// <returns>retur of R,G,B values of the two colors are the same</returns>
		internal bool ColorCompare(Color a, Color b)
		{
			return (a.R == b.R && a.B == b.B && a.G == b.G);
		}

		/// <summary>
		/// Convert a System.Drawing Color to a VwGraphics Color
		/// </summary>
		internal Int32 ConvertToVwGraphicsColor(Color color)
		{
			// VwGraphics colors seem to be BBGGRR
			var clone = Color.FromArgb(color.B, color.G, color.R);
			return clone.ToArgb();
		}

		/// <summary>
		/// an explicit tests of GetClipRect
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void GetClipRect()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				vwGraphics.Initialize(gr.Graphics.GetHdc());

				var rect1 = new Utils.Rect(0, 0, 1000, 1000);
				var rect2 = new Utils.Rect(500, 500, 700, 700);

				vwGraphics.PushClipRect(rect1);

				int left, top, right, bottom;
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);

				Assert.IsTrue(left == rect1.left, "First push failed: left");
				Assert.IsTrue(right == rect1.right, "First push failed: right");
				Assert.IsTrue(top == rect1.top, "First push failed: top");
				Assert.IsTrue(bottom == rect1.bottom, "First push failed: bottom");

				// try a second rectangle
				vwGraphics.PushClipRect(rect2);

				vwGraphics.GetClipRect(out left, out top, out right, out bottom);
				Assert.IsTrue(left == rect2.left, "Second push failed: left");
				Assert.IsTrue(right == rect2.right, "Second push failed: right");
				Assert.IsTrue(top == rect2.top, "Second push failed: top");
				Assert.IsTrue(bottom == rect2.bottom, "Second push failed: bottom");

				vwGraphics.PopClipRect();
				vwGraphics.PopClipRect();

				vwGraphics.ReleaseDC();
				gr.Graphics.ReleaseHdc();
			}

		}

		/// <summary>
		/// Tests that setting a clipping region prevents drawing outside of that clipping region.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void Clipping()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				// start with a blue background.
				using (var blueBrush = new SolidBrush(Color.Blue))
				{
					gr.Graphics.FillRectangle(blueBrush, new Rectangle(0, 0, 1000, 1000));

					// Check that filling with a blue brush worked.
					Assert.IsTrue(ColorCompare(gr.Bitmap.GetPixel(500, 500), Color.Blue));

					//
					// Check that drawing using a VwGraphics works.
					//

					vwGraphics.Initialize(gr.Graphics.GetHdc());

					vwGraphics.PushClipRect(new Utils.Rect(0, 0, 1000, 1000));
					vwGraphics.BackColor = ConvertToVwGraphicsColor(Color.Red);
					vwGraphics.DrawRectangle(0, 0, 1000, 1000);

					vwGraphics.PopClipRect();

					vwGraphics.ReleaseDC();
					gr.Graphics.ReleaseHdc();

					gr.Graphics.Flush();

					// Check that drawing a red rectangle using the VwGraphics Interface worked
					Assert.IsTrue(ColorCompare(gr.Bitmap.GetPixel(500, 500), Color.Red));

					/////
					// Check that VwGraphics doesn't draw outside its clip rect.
					/////

					vwGraphics.Initialize(gr.Graphics.GetHdc());
					// make the clip rect not include the area we are going to draw to.
					vwGraphics.PushClipRect(new Utils.Rect(100, 100, 200, 200));

					// attempt to draw off the clip rect.
					vwGraphics.BackColor = ConvertToVwGraphicsColor(Color.Green);
					vwGraphics.DrawRectangle(400, 400, 600, 600);

					vwGraphics.PopClipRect();

					vwGraphics.ReleaseDC();
					gr.Graphics.ReleaseHdc();

					gr.Graphics.Flush();

					// Check that the green rectangle didn't appear on screen.
					Assert.IsTrue(!ColorCompare(gr.Bitmap.GetPixel(500, 500), Color.Green));
				}
			}
		}

		/// <summary>
		/// Tests that SetClipRect works
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void SetClipRect()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				vwGraphics.Initialize(gr.Graphics.GetHdc());

				int left, top, right, bottom;

				var rect = new Utils.Rect(50,25,1000,1000);
				vwGraphics.SetClipRect(ref rect);
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);

				Assert.AreEqual(50, left, "Left doesn't match");
				Assert.AreEqual(25, top, "Top doesn't match");
				Assert.AreEqual(1000, right, "Right doesn't match");
				Assert.AreEqual(1000, bottom, "Bottom doesn't match");
			}
		}

		/// <summary>
		/// Tests that setting mulitple clip rects are merged correctly.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void ComplexClipping()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				vwGraphics.Initialize(gr.Graphics.GetHdc());

				// Test on a single push
				int left, top, right, bottom;

				vwGraphics.PushClipRect(new Utils.Rect(50, 60, 500, 510));
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);
				Assert.AreEqual(50, left, "Left doesn't match");
				Assert.AreEqual(60, top, "Top doesn't match");
				Assert.AreEqual(500, right, "Right doesn't match");
				Assert.AreEqual(510, bottom, "Bottom doesn't match");

				// Test on a second push
				vwGraphics.PushClipRect(new Utils.Rect(1, 1, 300, 310));
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);
				Assert.AreEqual(50, left, "Left doesn't match");
				Assert.AreEqual(60, top, "Top doesn't match");
				Assert.AreEqual(300, right, "Right doesn't match");
				Assert.AreEqual(310, bottom, "Bottom doesn't match");

				vwGraphics.PopClipRect();
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);
				Assert.AreEqual(50, left, "Left doesn't match");
				Assert.AreEqual(60, top, "Top doesn't match");
				Assert.AreEqual(500, right, "Right doesn't match");
				Assert.AreEqual(510, bottom, "Bottom doesn't match");
				vwGraphics.PopClipRect();
				vwGraphics.GetClipRect(out left, out top, out right, out bottom);
				Assert.AreEqual(0, left, "Left doesn't match");
				Assert.AreEqual(0, top, "Top doesn't match");
				Assert.AreEqual(1000, right, "Right doesn't match");
				Assert.AreEqual(1000, bottom, "Bottom doesn't match");

				vwGraphics.ReleaseDC();
				gr.Graphics.ReleaseHdc();
			}

		}

		/// <summary>
		/// Helper method that searches a Bitmap looking for the first non white pixel which is
		/// the bottom most !inside! the specified width/height in the Bitmap.
		/// </summary>
		/// <returns>-1 if no NonWhite pixel found</returns>
		internal int SearchForBottomMostNonWhitePixel(Bitmap bitmap, int width, int height)
		{
			for (int y = height -1 ; y >= 0; --y)
			{
				for (int x = width -1 ; x >= 0; --x)
				{
					if (!ColorCompare(bitmap.GetPixel(x, y), Color.White))
						return y;
				}
			}

			return -1;
		}

		/// <summary>
		/// Helper method that searches a Bitmap looking for the first non white pixel which is
		/// the right most !inside! the specified width/height in the Bitmap.
		/// </summary>
		/// <returns>-1 if no NonWhite pixel found</returns>
		internal int SearchForRightMostNonWhitePixel(Bitmap bitmap, int width, int height)
		{
			for (int x = width -1 ; x >= 0; --x)
			{
				for (int y = height -1 ; y >= 0; --y)
				{
					if (!ColorCompare(bitmap.GetPixel(x, y), Color.White))
						return x;
				}
			}

			return -1;
		}

		/// <summary>
		/// Helper method that searches a Bitmap looking for the first non white pixel which is
		/// the left most !inside! the specified width/height in the Bitmap.
		/// </summary>
		/// <returns>-1 if no NonWhite pixel found</returns>
		internal int SearchForLeftMostNonWhitePixel(Bitmap bitmap, int width, int height)
		{
			for (int x = 0; x < width; ++x)
			{
				for (int y = 0; y < height; ++y)
				{
					if (!ColorCompare(bitmap.GetPixel(x, y), Color.White))
						return x;
				}
			}

			return -1;
		}

		internal void TestGetTextExtentHelper(string testString)
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				const int areaWidth = 500;
				const int areaHeight = 500;

				// start with a White background.
				using (var blueBrush = new SolidBrush(Color.White))
				{
					gr.Graphics.FillRectangle(blueBrush, new Rectangle(0, 0, areaWidth, areaHeight));

					Assert.AreEqual(-1, SearchForBottomMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight), "Should all be white #1");
					Assert.AreEqual(-1, SearchForRightMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight), "Should all be white #2");

					vwGraphics.Initialize(gr.Graphics.GetHdc());

					vwGraphics.PushClipRect(new Utils.Rect(0, 0, areaWidth, areaHeight));
					vwGraphics.ForeColor = ConvertToVwGraphicsColor(Color.Black);

					int extentX;
					int extentY;

					vwGraphics.GetTextExtent(testString.Length, testString, out extentX, out extentY);

					Assert.That(extentX > 0, "extentX should be greater than 0");
					Assert.That(extentY > 0, "extentY should be greater than 0");

					vwGraphics.DrawText(0, 0, testString.Length, testString, 0);

					vwGraphics.PopClipRect();

					vwGraphics.ReleaseDC();
					gr.Graphics.ReleaseHdc();
					gr.Graphics.Flush();

					Assert.That(SearchForBottomMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight) <= extentY, String.Format("Should be <= {0}", extentY));
					Assert.That(SearchForRightMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight) <= extentX, String.Format("Should be <= {0}", extentX));
				}
			}
		}

		/// <summary>
		/// Test GetTextExtent reports are accurate value.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void GetTextExtent()
		{
			TestGetTextExtentHelper("A");
			TestGetTextExtentHelper("Hello World");

			TestGetTextExtentHelper("wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww");
			TestGetTextExtentHelper("mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm");

			// Test with non Roman strings

			// Chinese - PinYin
			TestGetTextExtentHelper("阿桑的到对");

			// Amharic - sera
			TestGetTextExtentHelper("ድድድውድ");
		}

		/// <summary>
		/// Test GetTextExtent reports are accurate value.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void GetTextExtentWithEmptyString()
		{
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				const int areaWidth = 1000;
				const int areaHeight = 1000;

				vwGraphics.Initialize(gr.Graphics.GetHdc());

				vwGraphics.PushClipRect(new Utils.Rect(0, 0, areaWidth, areaHeight));
				vwGraphics.ForeColor = ConvertToVwGraphicsColor(Color.Black);

				int extentX;
				int extentY;

				vwGraphics.GetTextExtent(0, String.Empty, out extentX, out extentY);

				Assert.That(extentX == 0, "extentX should equal 0");
				Assert.That(extentY > 0, "extentY should be greater than 0");

				vwGraphics.PopClipRect();

				vwGraphics.ReleaseDC();
				gr.Graphics.ReleaseHdc();
			}

		}

		/// <summary>
		/// FWNX-208: Test that DrawText text is clipped correctly even when part of the text is in the clipping region.
		/// Need to ensure the text that extents horizaontally out of the clipping region is not drawn.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void TextClipping()
		{
			const string longString = "abcdefghijklmnopqrstuvwzyzabcdefghijklmnopqrstuvwzyzabcdefghijklmnopqrstuvwzyz";

			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			using (var gr = new GraphicsObjectFromImage())
			{
				const int areaWidth = 500;
				const int areaHeight = 500;

				const int clipLeft = 300;
				const int clipRight = 400;

				// start with a White background.
				using (var blueBrush = new SolidBrush(Color.White))
				{
					gr.Graphics.FillRectangle(blueBrush, new Rectangle(0, 0, areaWidth, areaHeight));

					Assert.AreEqual(-1, SearchForBottomMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight), "Should all be white #1");
					Assert.AreEqual(-1, SearchForRightMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight), "Should all be white #2");

					vwGraphics.Initialize(gr.Graphics.GetHdc());

					vwGraphics.PushClipRect(new Utils.Rect(clipLeft, 0, clipRight, areaHeight));
					vwGraphics.ForeColor = ConvertToVwGraphicsColor(Color.Black);

					vwGraphics.DrawText(0, 0, longString.Length, longString, 0);

					vwGraphics.PopClipRect();

					vwGraphics.ReleaseDC();
					gr.Graphics.ReleaseHdc();
					gr.Graphics.Flush();

					Assert.That(SearchForLeftMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight) >= clipLeft, String.Format("Should be >= {0}", clipLeft));
					Assert.That(SearchForRightMostNonWhitePixel(gr.Bitmap, areaWidth, areaHeight) <= clipRight, String.Format("Should be <= {0}", clipRight));
				}
			}
		}

		/// <summary>
		/// Test that ensure we can draw to large rectangles see: FWNX-449
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void LargeRectangles()
		{
			// EB/2011-02-08: this test is failing for me on Linux. Not sure if it should work
			// or what (see FWNX-449). Test is failing in FillRectangle similar to the failure
			// in the following comment
			// https://www.jira.insitehome.org/browse/FWNX-449?focusedCommentId=108054&page=com.atlassian.jira.plugin.system.issuetabpanels:comment-tabpanel#comment-108054
			IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
			Assert.IsNotNull(vwGraphics);

			const int width = 1241;
			const int height = 56080; // something bigger than MAX_IMAGE_SIZE (32767)

			using (var gr = new GraphicsObjectFromImage(width, height))
			{
				// start with a blue background.
				using (var blueBrush = new SolidBrush(Color.Blue))
				{
					gr.Graphics.FillRectangle(blueBrush, new Rectangle(0, 0, width, height));

					// Check that filling with a blue brush worked.
					Assert.IsTrue(ColorCompare(gr.Bitmap.GetPixel(width - 1, height - 1), Color.Blue));

					/////
					// Check that drawing using a VwGraphics works.
					////

					vwGraphics.Initialize(gr.Graphics.GetHdc());

					vwGraphics.PushClipRect(new Utils.Rect(0, 0, width, height));
					vwGraphics.BackColor = ConvertToVwGraphicsColor(Color.Red);
					vwGraphics.DrawRectangle(0, 0, width, height);

					vwGraphics.PopClipRect();

					vwGraphics.ReleaseDC();
					gr.Graphics.ReleaseHdc();

					gr.Graphics.Flush();

					// Check that drawing a red rectangle using the VwGraphics Interface worked
					Assert.IsTrue(ColorCompare(gr.Bitmap.GetPixel(width - 1, height - 1), Color.Red));
				}
			}
		}
	}
}
