// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// End-to-end selection behaviour of the wired-up multistring value box (<see cref="FwMultiWsTextField"/>):
	/// the seam where our <c>RegionBidirectionalTextNavigation</c> override meets the live Avalonia
	/// <see cref="TextBox"/> through real headless keyboard/pointer input. The pure navigation math is covered
	/// in RegionModelTests; these drive the actual focused box so a regression in the handler wiring (routing,
	/// caret/selection ordering) is caught where the pure-function tests cannot see it. String literals with
	/// non-ASCII clusters use explicit \u escapes so the source keeps the exact code units under test.
	/// </summary>
	[TestFixture]
	public class FwFieldSelectionInputTests
	{
		private static RegionField LtrField(string text)
			=> new RegionField(
				"LexEntry/x/#0", "Lexeme Form", "Form", "vernacular", RegionFieldKind.Text,
				EditorClassification.Known, "SelEditor", null, SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("en", text, wsTag: "en") },
				null, null);

		private static RegionField RtlField(string text)
			=> new RegionField(
				"LexEntry/x/#0", "Lexeme Form", "Form", "vernacular", RegionFieldKind.Text,
				EditorClassification.Known, "SelEditor", null, SurfaceRouting.Product,
				new List<RegionWsValue>
				{
					new RegionWsValue("ar", text, "Scheherazade New", 0, rightToLeft: true, wsTag: "ar")
				},
				null, null);

		private static (TextBox box, Window window) ShowFocused(RegionField field)
		{
			var control = new FwMultiWsTextField(field, field.AutomationId, new FakeRegionEditContext(), null);
			var window = new Window { Content = control, Width = 420, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			var box = control.GetVisualDescendants().OfType<TextBox>().Single();
			box.Focus();
			Dispatcher.UIThread.RunJobs();
			return (box, window);
		}

		private static (int start, int end) Span(TextBox box)
			=> (Math.Min(box.SelectionStart, box.SelectionEnd), Math.Max(box.SelectionStart, box.SelectionEnd));

		// ---- D1: Shift+Arrow must EXTEND the selection through the wired-up box ----

		[AvaloniaTest]
		public void ShiftRight_FromStart_ExtendsSelectionByOneGrapheme()
		{
			var (box, window) = ShowFocused(LtrField("testes"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That(end - start, Is.EqualTo(1),
				$"Shift+Right must leave a one-grapheme selection; actual span [{start}..{end}], caret {box.CaretIndex}");
			Assert.That((start, end), Is.EqualTo((0, 1)),
				"Shift+Right from the start selects exactly the first grapheme");
			Assert.That(box.CaretIndex, Is.EqualTo(1), "the caret lands on the moving (right) edge");
		}

		[AvaloniaTest]
		public void ShiftRight_Twice_ExtendsSelectionToTwoGraphemes()
		{
			var (box, window) = ShowFocused(LtrField("testes"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();
			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That((start, end), Is.EqualTo((0, 2)),
				$"two Shift+Rights select the first two graphemes; actual span [{start}..{end}]");
		}

		[AvaloniaTest]
		public void ShiftLeft_FromEnd_ExtendsSelectionLeftwardByOneGrapheme()
		{
			var (box, window) = ShowFocused(LtrField("testes"));
			box.CaretIndex = 6;
			box.SelectionStart = 6;
			box.SelectionEnd = 6;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That((start, end), Is.EqualTo((5, 6)),
				$"Shift+Left from the end selects the last grapheme; actual span [{start}..{end}]");
			Assert.That(box.CaretIndex, Is.EqualTo(5), "the caret lands on the moving (left) edge");
		}

		[AvaloniaTest]
		public void ShiftLeft_RtlRun_ExtendsSelectionByOneCluster()
		{
			// A single Hebrew run (aleph-bet-gimel) flows RTL; the Left arrow moves logically forward inside
			// it, and Shift must extend rather than collapse — the same wiring seam, in the RTL direction.
			var (box, window) = ShowFocused(RtlField("אבג"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That(end - start, Is.EqualTo(1),
				$"Shift+Left inside the RTL run must leave a one-cluster selection; actual span [{start}..{end}]");
			Assert.That((start, end), Is.EqualTo((0, 1)));
		}

		// ---- Integration cluster safety: Shift+Arrow lands on WHOLE grapheme clusters end-to-end ----

		[AvaloniaTest]
		public void ShiftRight_AcrossCombiningMarkCluster_TakesTheClusterWhole()
		{
			// "a" + ("e" U+0065 + U+0301 combining acute = one grapheme) + "b": that cluster spans 1..3.
			var (box, window) = ShowFocused(LtrField("aéb"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift); // over "a"
			Dispatcher.UIThread.RunJobs();
			Assert.That(Span(box), Is.EqualTo((0, 1)), "first extend takes the base letter");

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift); // over the whole cluster
			Dispatcher.UIThread.RunJobs();
			var (start, end) = Span(box);
			Assert.That((start, end), Is.EqualTo((0, 3)),
				$"the combining-mark cluster is taken whole; span [{start}..{end}]");
			Assert.That(end, Is.Not.EqualTo(2), "the selection never lands between the base and its combining mark");
		}

		[AvaloniaTest]
		public void ShiftRight_AcrossSurrogatePairEmoji_TakesTheClusterWhole()
		{
			// "x" + U+1F600 grinning face (surrogate pair 😀, indices 1..3) + "y".
			var (box, window) = ShowFocused(LtrField("x😀y"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift); // over "x"
			Dispatcher.UIThread.RunJobs();
			Assert.That(Span(box), Is.EqualTo((0, 1)));

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift); // over the whole emoji
			Dispatcher.UIThread.RunJobs();
			var (start, end) = Span(box);
			Assert.That((start, end), Is.EqualTo((0, 3)),
				$"the surrogate pair is taken whole; span [{start}..{end}]");
			Assert.That(end, Is.Not.EqualTo(2), "the selection never lands between the surrogate halves");
		}

		[AvaloniaTest]
		public void ShiftRight_AcrossZwjEmojiSequence_TakesTheJoinedClusterWhole()
		{
			// U+1F468 U+200D U+1F4BB (man ZWJ laptop = "man technologist"): one grapheme, 5 UTF-16 units.
			const string zwj = "👨‍💻";
			var (box, window) = ShowFocused(LtrField(zwj + "z"));
			box.CaretIndex = 0;
			box.SelectionStart = 0;
			box.SelectionEnd = 0;
			Dispatcher.UIThread.RunJobs();

			window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That((start, end), Is.EqualTo((0, 5)),
				$"the ZWJ-joined emoji is one cluster; the first extend takes all 5 units, never a partial join. Span [{start}..{end}]");
		}

		// ---- D2 (mouse-drag over-grow): NOT headlessly reproducible — documented, left OPEN ----
		//
		// The reported defect is that a mouse DRAG grows the selection beyond the pointer. Drag-select is
		// Avalonia-native (our only pointer handler is the release-time cluster snap). Driving a headless
		// MouseDown/Move/Up over the value box produces NO selection at all (SelectionStart==SelectionEnd==0):
		// the headless backend has no real text hit-testing, so the native drag-to-caret mapping never runs
		// and the defect cannot be provoked or self-validated here. Per the repro-first rule the D2 code is
		// left UNCHANGED. Reproducing it needs a real rendered window / the specific layout transform suspected
		// in the region surface; this Ignored test records the limitation so the finding stays visible.
		[AvaloniaTest]
		[Ignore("D2 (mouse-drag selection over-grow) is not reproducible headlessly: the headless backend does no text hit-testing, so a driven drag yields no selection. Left open; needs a real rendered window.")]
		public void MouseDrag_SelectionOverGrow_NotHeadlesslyReproducible()
		{
			var (box, window) = ShowFocused(LtrField("testes"));
			var origin = box.TranslatePoint(new Point(0, box.Bounds.Height / 2), window);
			var startPt = origin.Value + new Vector(2, 0);
			var endPt = origin.Value + new Vector(24, 0);
			window.MouseDown(startPt, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();
			window.MouseMove(endPt);
			Dispatcher.UIThread.RunJobs();
			window.MouseUp(endPt, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();

			var (start, end) = Span(box);
			Assert.That(end, Is.GreaterThan(start), "a headless drag would need to select something to test over-grow");
		}
	}
}
