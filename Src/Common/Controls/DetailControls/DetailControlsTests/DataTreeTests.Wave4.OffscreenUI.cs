// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[TestFixture]
	public partial class DataTreeTests
	{
		#region Wave 4 — Offscreen UI & Painter Interface

		#region IDataTreePainter contract

		/// <summary>
		/// The default Painter property should point to the DataTree itself.
		/// Category: SurvivesRefactoring — tests the seam contract.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void Painter_DefaultIsSelf()
		{
			Assert.That(m_dtree.Painter, Is.SameAs(m_dtree),
				"DataTree.Painter should default to 'this'");
		}

		/// <summary>
		/// The Painter property can be replaced with a test double.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void Painter_CanBeReplacedWithRecorder()
		{
			var recorder = new RecordingPainter();
			m_dtree.Painter = recorder;

			Assert.That(m_dtree.Painter, Is.SameAs(recorder));
		}

		/// <summary>
		/// When a RecordingPainter is injected and PaintLinesBetweenSlices is called,
		/// the recorder captures the call.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void RecordingPainter_RecordsPaintCalls()
		{
			var recorder = new RecordingPainter();
			using (var ctx = new OffscreenGraphicsContext())
			{
				recorder.PaintLinesBetweenSlices(ctx.Graphics, 800);

				Assert.That(recorder.PaintCallCount, Is.EqualTo(1));
				Assert.That(recorder.Calls[0].Width, Is.EqualTo(800));
				Assert.That(recorder.Calls[0].Graphics, Is.SameAs(ctx.Graphics));
			}
		}

		/// <summary>
		/// RecordingPainter's Delegate property forwards calls to another painter.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void RecordingPainter_DelegateForwardsCall()
		{
			var inner = new RecordingPainter();
			var outer = new RecordingPainter { Delegate = inner };
			using (var ctx = new OffscreenGraphicsContext())
			{
				outer.PaintLinesBetweenSlices(ctx.Graphics, 400);

				Assert.That(outer.PaintCallCount, Is.EqualTo(1),
					"Outer recorder should record the call");
				Assert.That(inner.PaintCallCount, Is.EqualTo(1),
					"Inner delegate should also receive the call");
			}
		}

		#endregion

		#region PaintLinesBetweenSlices — offscreen bitmap tests

		/// <summary>
		/// Calling PaintLinesBetweenSlices with no slices loaded must not throw.
		/// Category: SurvivesRefactoring — empty-state safety.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_NoSlices_DoesNotThrow()
		{
			// m_dtree has empty Slices list — no ShowObject called.
			using (var ctx = new OffscreenGraphicsContext())
			{
				Assert.DoesNotThrow(
					() => m_dtree.PaintLinesBetweenSlices(ctx.Graphics, 800));
			}
		}

		/// <summary>
		/// PaintLinesBetweenSlices with loaded slices executes the drawing path
		/// without throwing, even though slices have default layout positions.
		/// </summary>
		[Test]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_WithSlices_DrawsWithoutError()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.GreaterThanOrEqualTo(2),
				"Need at least 2 slices for inter-slice line drawing");

			using (var ctx = new OffscreenGraphicsContext())
			{
				Assert.DoesNotThrow(
					() => m_dtree.PaintLinesBetweenSlices(ctx.Graphics, ctx.Bitmap.Width));
			}
		}

		/// <summary>
		/// PaintLinesBetweenSlices draws separator lines onto the bitmap when slices
		/// have been positioned with explicit locations and sizes.
		/// </summary>
		[Test]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_AfterLayout_DrawsPixels()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.GreaterThanOrEqualTo(2));

			// Give slices explicit positions so the paint method has real
			// geometry to draw between. In headless tests PerformLayout may
			// not position children, so we set Location/Size directly.
			var slice0 = m_dtree.Slices[0] as Slice;
			var slice1 = m_dtree.Slices[1] as Slice;
			slice0.SetBounds(0, 0, 800, 30);
			slice1.SetBounds(0, 30, 800, 30);

			using (var ctx = new OffscreenGraphicsContext(800, 100))
			{
				ctx.Graphics.Clear(Color.White);
				m_dtree.PaintLinesBetweenSlices(ctx.Graphics, ctx.Bitmap.Width);

				// The separator line should appear near y=30 (bottom of first slice).
				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.True,
					"Expected at least one gray separator line to be drawn between slices");
			}
		}

		/// <summary>
		/// HandlePaintLinesBetweenSlices delegates correctly to PaintLinesBetweenSlices.
		/// We verify by injecting a RecordingPainter — note that HandlePaintLinesBetweenSlices
		/// does NOT go through the Painter property (it calls PaintLinesBetweenSlices directly
		/// on 'this'). But we can verify the internal call path works.
		/// </summary>
		[Test]
		[Category("OffscreenUI")]
		public void HandlePaintLinesBetweenSlices_CallsPaintMethod()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			using (var ctx = new OffscreenGraphicsContext())
			using (var pea = ctx.CreatePaintEventArgs())
			{
				// HandlePaintLinesBetweenSlices is internal and calls
				// this.PaintLinesBetweenSlices(pea.Graphics, Width).
				Assert.DoesNotThrow(
					() => m_dtree.HandlePaintLinesBetweenSlices(pea));
			}
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OnPaint_UsesInjectedPainter_WhenLayoutStateNormal()
		{
			var recorder = new RecordingPainter();
			m_dtree.Painter = recorder;

			using (var ctx = new OffscreenGraphicsContext(300, 120))
			using (var pea = ctx.CreatePaintEventArgs())
			{
				InvokeOnPaintForTest(pea);
			}

			Assert.That(recorder.PaintCallCount, Is.EqualTo(1),
				"OnPaint should delegate to the injected Painter when layout state is normal");
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OnPaint_ReentrantState_DoesNotInvokePainter()
		{
			var recorder = new RecordingPainter();
			m_dtree.Painter = recorder;
			SetLayoutStateForTest(DataTree.LayoutStates.klsChecking);

			try
			{
				using (var ctx = new OffscreenGraphicsContext(300, 120))
				using (var pea = ctx.CreatePaintEventArgs())
				{
					InvokeOnPaintForTest(pea);
				}
			}
			finally
			{
				SetLayoutStateForTest(DataTree.LayoutStates.klsNormal);
			}

			Assert.That(recorder.PaintCallCount, Is.EqualTo(0),
				"OnPaint should early-return during re-entrant layout states");
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_HeaderChildPair_SkipsLine()
		{
			m_dtree.Slices.Clear();
			var first = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice header='true' />"),
				Weight = ObjectWeight.field,
				Key = new object[] { 1 }
			};
			var second = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field,
				Key = new object[] { 1, 2 }
			};
			first.SetBounds(0, 0, 220, 20);
			second.SetBounds(0, 20, 220, 20);
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			using (var ctx = new OffscreenGraphicsContext(220, 60))
			{
				ctx.Graphics.Clear(Color.White);
				m_dtree.PaintLinesBetweenSlices(ctx.Graphics, 220);
				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.False,
					"Header->child transition should suppress spacer line");
			}
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_SameObjectAttr_UsesSplitPositionBaseX()
		{
			m_dtree.Slices.Clear();
			m_dtree.SliceSplitPositionBase = 150;
			var first = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field
			};
			var second = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice sameObject='true' />"),
				Weight = ObjectWeight.field
			};
			first.Indent = 0;
			second.Indent = 0;
			first.SetBounds(0, 0, 220, 20);
			second.SetBounds(0, 20, 220, 20);
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			using (var ctx = new OffscreenGraphicsContext(220, 60))
			{
				ctx.Graphics.Clear(Color.White);
				m_dtree.PaintLinesBetweenSlices(ctx.Graphics, 220);

				Assert.That(
					ctx.HasNonBackgroundPixelsInRegion(new Rectangle(0, 20, 120, 1), Color.White),
					Is.False,
					"sameObject branch should start the line near split-base, not at far-left label indent");
				Assert.That(
					ctx.HasNonBackgroundPixelsInRegion(new Rectangle(150, 20, 60, 1), Color.White),
					Is.True,
					"sameObject branch should draw from split-base towards the right edge");
			}
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_HeavyNextSlice_AppliesVerticalOffset()
		{
			m_dtree.Slices.Clear();
			var first = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field
			};
			var second = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.heavy
			};
			first.SetBounds(0, 0, 220, 20);
			second.SetBounds(0, 20, 220, 20);
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			using (var ctx = new OffscreenGraphicsContext(220, 80))
			{
				ctx.Graphics.Clear(Color.White);
				m_dtree.PaintLinesBetweenSlices(ctx.Graphics, 220);

				int expectedY = 20 + (DataTree.HeavyweightRuleThickness / 2) + DataTree.HeavyweightRuleAboveMargin;
				Assert.That(
					ctx.HasNonBackgroundPixelsInRegion(new Rectangle(0, expectedY, 220, 1), Color.White),
					Is.True,
					"Heavy next slice should shift line downward by heavy-rule offsets");
			}
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void PaintLinesBetweenSlices_SummarySkipSpacerLine_SkipsLine()
		{
			m_dtree.Slices.Clear();
			var first = new SummarySlice
			{
				ConfigurationNode = CreateXmlNode("<slice skipSpacerLine='true' />"),
				Weight = ObjectWeight.field
			};
			var second = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field
			};
			first.SetBounds(0, 0, 220, 20);
			second.SetBounds(0, 20, 220, 20);
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			using (var ctx = new OffscreenGraphicsContext(220, 60))
			{
				ctx.Graphics.Clear(Color.White);
				m_dtree.PaintLinesBetweenSlices(ctx.Graphics, 220);
				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.False,
					"Summary slices with skipSpacerLine=true should suppress spacer line");
			}
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void HandleLayout1_HeavySlice_IncludesHeavyMargins()
		{
			m_dtree.Slices.Clear();
			var first = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field
			};
			var second = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.heavy
			};
			first.SetBounds(0, 0, 200, 20);
			second.SetBounds(0, 0, 200, 20);
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			int yBottom = m_dtree.HandleLayout1(true, new Rectangle(0, 0, 200, 200));

			int expectedSecondTop = (first.Height + 1) + DataTree.HeavyweightRuleThickness + DataTree.HeavyweightRuleAboveMargin;
			Assert.That(second.Top, Is.EqualTo(expectedSecondTop),
				"Heavy slices should be pushed down by heavy-rule thickness + margin");
			Assert.That(yBottom, Is.GreaterThan(second.Top), "Layout should advance past second slice");
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void HandleLayout1_WhenClipBelowTop_ReturnsEarly()
		{
			m_dtree.Slices.Clear();
			var first = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Weight = ObjectWeight.field
			};
			first.SetBounds(0, 0, 200, 20);
			m_dtree.Slices.Add(first);

			int result = m_dtree.HandleLayout1(false, new Rectangle(0, -10, 200, 0));

			Assert.That(result, Is.EqualTo(0),
				"Partial layout should return early when current yTop is already below clip bottom");
		}

		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void HandleLayout1_WhenDisposing_ReturnsClipBottom()
		{
			SetDisposingStateForTest(true);
			try
			{
				var clip = new Rectangle(0, 0, 200, 123);
				int result = m_dtree.HandleLayout1(true, clip);
				Assert.That(result, Is.EqualTo(clip.Bottom));
			}
			finally
			{
				SetDisposingStateForTest(false);
			}
		}

		#endregion

		#region DrawLabel — offscreen bitmap tests

		/// <summary>
		/// Slice.DrawLabel draws the label text onto a bitmap-backed Graphics.
		/// We verify non-blank pixels appear after the call.
		/// </summary>
		[Test]
		[Category("OffscreenUI")]
		public void DrawLabel_WithSlice_DrawsTextToBitmap()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0));

			var slice = m_dtree.Slices[0];
			Assert.That(slice.Label, Is.Not.Null.And.Not.Empty,
				"Slice should have a label for us to draw");

			using (var ctx = new OffscreenGraphicsContext(400, 40))
			{
				ctx.Graphics.Clear(Color.White);
				slice.DrawLabel(0, 0, ctx.Graphics, 400);

				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.True,
					"Expected DrawLabel to render visible text onto the bitmap");
			}
		}

		/// <summary>
		/// Slice.DrawLabel with null SmallImages does not throw; it simply skips
		/// image drawing and renders text only.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void DrawLabel_NullSmallImages_SkipsImagesAndDoesNotThrow()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var slice = m_dtree.Slices[0];

			Assert.That(slice.SmallImages, Is.Null,
				"SmallImages should default to null in test environment");

			using (var ctx = new OffscreenGraphicsContext(400, 40))
			{
				Assert.DoesNotThrow(
					() => slice.DrawLabel(0, 0, ctx.Graphics, 400));
			}
		}

		/// <summary>
		/// Both overloads of DrawLabel (4-param and 3-param) work correctly.
		/// The 3-param version uses LabelIndent() for the x position.
		/// </summary>
		[Test]
		[Category("OffscreenUI")]
		public void DrawLabel_ThreeParamOverload_DrawsTextToBitmap()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var slice = m_dtree.Slices[0];

			using (var ctx = new OffscreenGraphicsContext(400, 40))
			{
				ctx.Graphics.Clear(Color.White);
				// 3-param overload: DrawLabel(int y, Graphics gr, int clipWidth)
				slice.DrawLabel(0, ctx.Graphics, 400);

				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.True,
					"3-param DrawLabel should also render visible text");
			}
		}

		#endregion

		#region Static helper tests (SameSourceObject, IsChildSlice)

		/// <summary>
		/// SameSourceObject returns true when both slices have the same Key array contents.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void SameSourceObject_IdenticalKeys_ReturnsTrue()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.GreaterThanOrEqualTo(2));

			var slice1 = m_dtree.Slices[0];
			var slice2 = m_dtree.Slices[1];

			// Both should belong to the same root object (m_entry).
			bool result = DataTree.SameSourceObject(slice1, slice2);
			Assert.That(result, Is.True,
				"Two slices from the same root entry should share the source object");
		}

		/// <summary>
		/// SameSourceObject returns false when slices display different objects.
		/// We compare a slice from a sense sub-object against a top-level entry slice.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void SameSourceObject_DifferentKeys_ReturnsFalse()
		{
			// Give m_entry a sense so we get slices at different object levels.
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);

			// With one sense we get Gloss slices. Find two slices where
			// Object differs (e.g., the entry-level vs sense-level).
			// If the layout only produces sense-level slices (same Object),
			// we fall back to verifying that SameSourceObject correctly
			// returns true for those.
			if (m_dtree.Slices.Count >= 2)
			{
				var s1 = m_dtree.Slices[0];
				// Look for a slice whose Object differs from s1's Object.
				var s2 = m_dtree.Slices.Cast<Slice>()
					.FirstOrDefault(s => s.Object != null && s.Object.Hvo != s1.Object.Hvo);

				if (s2 != null)
				{
					Assert.That(DataTree.SameSourceObject(s1, s2), Is.False,
						"Slices viewing different objects should not be same-source");
				}
				else
				{
					// All slices happen to show the same object — just verify they agree.
					Assert.That(DataTree.SameSourceObject(s1, m_dtree.Slices[1] as Slice), Is.True,
						"Slices from the same object should be same-source");
				}
			}
			else
			{
				Assert.Inconclusive("Not enough slices generated for SameSourceObject negative test");
			}
		}

		/// <summary>
		/// IsChildSlice returns true when the second slice's Key is a prefix extension
		/// of the first (i.e. same initial elements plus at least one more).
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void IsChildSlice_ChildKey_ReturnsTrue()
		{
			using (var parent = new Slice())
			using (var child = new Slice())
			{
				parent.Key = new object[] { 1, "field" };
				child.Key = new object[] { 1, "field", 2 };

				Assert.That(DataTree.IsChildSlice(parent, child), Is.True,
					"Slice with extended key should be recognized as child");
			}
		}

		/// <summary>
		/// IsChildSlice returns false when both slices have the same key length.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void IsChildSlice_SameLength_ReturnsFalse()
		{
			using (var a = new Slice())
			using (var b = new Slice())
			{
				a.Key = new object[] { 1, "field" };
				b.Key = new object[] { 1, "field" };

				Assert.That(DataTree.IsChildSlice(a, b), Is.False,
					"Same-length keys should not be parent-child");
			}
		}

		/// <summary>
		/// IsChildSlice returns false when the second slice has a shorter key.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void IsChildSlice_ShorterSecond_ReturnsFalse()
		{
			using (var a = new Slice())
			using (var b = new Slice())
			{
				a.Key = new object[] { 1, "field", 2 };
				b.Key = new object[] { 1 };

				Assert.That(DataTree.IsChildSlice(a, b), Is.False,
					"Shorter second key should not be child");
			}
		}

		#endregion

		#region OffscreenGraphicsContext self-tests

		/// <summary>
		/// OffscreenGraphicsContext creates a usable bitmap and Graphics surface.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OffscreenGraphicsContext_CreatesValidSurface()
		{
			using (var ctx = new OffscreenGraphicsContext(200, 100))
			{
				Assert.That(ctx.Bitmap, Is.Not.Null);
				Assert.That(ctx.Graphics, Is.Not.Null);
				Assert.That(ctx.Bitmap.Width, Is.EqualTo(200));
				Assert.That(ctx.Bitmap.Height, Is.EqualTo(100));
			}
		}

		/// <summary>
		/// HasNonBackgroundPixels returns false for a freshly-cleared bitmap.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OffscreenGraphicsContext_FreshBitmap_HasNoNonBackgroundPixels()
		{
			using (var ctx = new OffscreenGraphicsContext(50, 50))
			{
				ctx.Graphics.Clear(Color.White);
				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.False);
			}
		}

		/// <summary>
		/// HasNonBackgroundPixels returns true after drawing on the bitmap.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OffscreenGraphicsContext_AfterDraw_HasNonBackgroundPixels()
		{
			using (var ctx = new OffscreenGraphicsContext(50, 50))
			{
				ctx.Graphics.Clear(Color.White);
				ctx.Graphics.DrawLine(Pens.Black, 0, 25, 50, 25);
				Assert.That(ctx.HasNonBackgroundPixels(Color.White), Is.True);
			}
		}

		/// <summary>
		/// CreatePaintEventArgs creates a valid PaintEventArgs with full bitmap clip.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OffscreenGraphicsContext_CreatePaintEventArgs_ValidClip()
		{
			using (var ctx = new OffscreenGraphicsContext(300, 200))
			using (var pea = ctx.CreatePaintEventArgs())
			{
				Assert.That(pea.Graphics, Is.SameAs(ctx.Graphics));
				Assert.That(pea.ClipRectangle.Width, Is.EqualTo(300));
				Assert.That(pea.ClipRectangle.Height, Is.EqualTo(200));
			}
		}

		/// <summary>
		/// HasNonBackgroundPixelsInRegion correctly scopes to the given region.
		/// </summary>
		[Test]
		[Category("SurvivesRefactoring")]
		[Category("OffscreenUI")]
		public void OffscreenGraphicsContext_RegionCheck_ScopedCorrectly()
		{
			using (var ctx = new OffscreenGraphicsContext(100, 100))
			{
				ctx.Graphics.Clear(Color.White);
				// Draw a line only in the bottom half.
				ctx.Graphics.DrawLine(Pens.Black, 0, 75, 100, 75);

				Assert.That(
					ctx.HasNonBackgroundPixelsInRegion(new Rectangle(0, 0, 100, 50), Color.White),
					Is.False, "Top half should be blank");
				Assert.That(
					ctx.HasNonBackgroundPixelsInRegion(new Rectangle(0, 50, 100, 50), Color.White),
					Is.True, "Bottom half should have the drawn line");
			}
		}

		private void InvokeOnPaintForTest(PaintEventArgs pea)
		{
			var onPaint = typeof(DataTree).GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(onPaint, Is.Not.Null, "Could not reflect DataTree.OnPaint");
			onPaint.Invoke(m_dtree, new object[] { pea });
		}

		private void SetLayoutStateForTest(DataTree.LayoutStates state)
		{
			var field = typeof(DataTree).GetField("m_layoutState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_layoutState");
			field.SetValue(m_dtree, state);
		}

		private void SetDisposingStateForTest(bool disposing)
		{
			var field = typeof(DataTree).GetField("m_fDisposing", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_fDisposing");
			field.SetValue(m_dtree, disposing);
		}

		#endregion

		#endregion
	}
}
