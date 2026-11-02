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
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// An input-method (IME) composition open in one writing-system row must finish in that row
	/// when the user clicks another row (LT-22887). The headless platform has no input method,
	/// so a fake one holds the composed text and delivers it as ordinary text input when asked
	/// to finish.
	/// </summary>
	[TestFixture]
	public class ImeCompositionGuardTests
	{
		private Action m_savedCompletion;

		[SetUp]
		public void SaveCompletion() => m_savedCompletion = ImeCompositionGuard.CompleteOpenComposition;

		[TearDown]
		public void RestoreCompletion() => ImeCompositionGuard.CompleteOpenComposition = m_savedCompletion;

		/// <summary>
		/// Stands in for an input method: it holds a composition, delivers it to whichever
		/// element has focus when it finishes, and finishes at most once.
		/// </summary>
		private sealed class FakeInputMethod
		{
			private readonly Window m_window;
			private string m_composition;

			public FakeInputMethod(Window window) => m_window = window;

			public int CompletionRequests { get; private set; }

			public void Compose(string text) => m_composition = text;

			public void Complete()
			{
				CompletionRequests++;
				FinishIfOpen();
			}

			// A real input method also finishes on its own once focus has moved, for example when
			// FieldWorks switches the keyboard for the clicked row's writing system.
			public void FinishIfOpen()
			{
				if (string.IsNullOrEmpty(m_composition))
					return;

				var text = m_composition;
				m_composition = null;
				m_window.KeyTextInput(text);
			}
		}

		private static (FwMultiWsTextField Field, Window Window, FakeDetailEditContext Context) ShowTwoRows()
		{
			var field = new DetailField(
				stableId: "LexExampleSentence/Example", label: "Example", field: "Example",
				writingSystem: null, kind: DetailFieldKind.Text,
				editorClassification: EditorClassification.Known,
				automationId: "LexExampleSentence_Example", localizationKey: null,
				routing: HostRouting.Product,
				values: new List<DetailWsValue>
				{
					new DetailWsValue("zh", string.Empty, wsTag: "zh"),
					new DetailWsValue("en", string.Empty, wsTag: "en")
				},
				options: null, selectedOptionKey: null, isEditable: true);
			var context = new FakeDetailEditContext();
			var control = new FwMultiWsTextField(field, field.AutomationId, context, null);
			var window = new Window { Content = control, Width = 360, Height = 160 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (control, window, context);
		}

		private static IReadOnlyList<TextBox> Boxes(FwMultiWsTextField control)
			=> control.GetVisualDescendants().OfType<TextBox>().ToList();

		private static void Click(Window window, Visual target)
		{
			var point = target.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the click target must be in the window");
			window.MouseDown(point.Value, MouseButton.Left);
			window.MouseUp(point.Value, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void ClickAnotherRow_WhileComposing_FinishesTheTextInTheComposingRow()
		{
			var (control, window, context) = ShowTwoRows();
			var ime = new FakeInputMethod(window);
			ImeCompositionGuard.CompleteOpenComposition = ime.Complete;
			var chinese = Boxes(control)[0];
			var english = Boxes(control)[1];
			chinese.Focus();
			Dispatcher.UIThread.RunJobs();

			ime.Compose("nihao");
			Click(window, english);
			ime.FinishIfOpen();

			Assert.That(english.IsFocused, Is.True, "the click still moves focus to the clicked row");
			Assert.That(chinese.Text, Is.EqualTo("nihao"), "the composed text stays in the row it was typed in");
			Assert.That(english.Text ?? string.Empty, Is.Empty, "nothing lands in the clicked row");
			Assert.That(context.TextEdits, Is.EqualTo(new[] { ("Example", "zh", "nihao") }),
				"the text is staged under the composing row's writing system only");
		}

		// Models an input method that leaves its composition open but passes the arrow key on, so
		// the row navigation sees a plain arrow key on the composing row's last line.
		[AvaloniaTest]
		public void ArrowToAnotherRow_WhileComposing_FinishesTheTextInTheComposingRow()
		{
			var (control, window, context) = ShowTwoRows();
			var ime = new FakeInputMethod(window);
			ImeCompositionGuard.CompleteOpenComposition = ime.Complete;
			var chinese = Boxes(control)[0];
			var english = Boxes(control)[1];
			chinese.Focus();
			Dispatcher.UIThread.RunJobs();

			ime.Compose("nihao");
			window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
			Dispatcher.UIThread.RunJobs();
			ime.FinishIfOpen();

			Assert.That(english.IsFocused, Is.True, "the arrow key still moves to the next row");
			Assert.That(chinese.Text, Is.EqualTo("nihao"), "the composed text stays in the row it was typed in");
			Assert.That(english.Text ?? string.Empty, Is.Empty, "nothing lands in the next row");
			Assert.That(context.TextEdits, Is.EqualTo(new[] { ("Example", "zh", "nihao") }),
				"the text is staged under the composing row's writing system only");
		}

		[AvaloniaTest]
		public void ImeProcessedKey_WhileComposing_DoesNotFinishTheComposition()
		{
			var (control, window, _) = ShowTwoRows();
			var ime = new FakeInputMethod(window);
			ImeCompositionGuard.CompleteOpenComposition = ime.Complete;
			Boxes(control)[0].Focus();
			Dispatcher.UIThread.RunJobs();

			ime.Compose("nihao");
			window.KeyPress(Key.ImeProcessed, RawInputModifiers.None, PhysicalKey.None, null);

			Assert.That(ime.CompletionRequests, Is.Zero,
				"a key the input method consumes is part of the composition");
		}

		[AvaloniaTest]
		public void ClickInsideTheComposingRow_DoesNotFinishTheComposition()
		{
			var (control, window, _) = ShowTwoRows();
			var ime = new FakeInputMethod(window);
			ImeCompositionGuard.CompleteOpenComposition = ime.Complete;
			var chinese = Boxes(control)[0];
			chinese.Focus();
			Dispatcher.UIThread.RunJobs();

			ime.Compose("nihao");
			Click(window, chinese);

			Assert.That(ime.CompletionRequests, Is.Zero,
				"a click inside the composing row is the input method's own business");
		}

		[AvaloniaTest]
		public void ClickWithNoTextBoxFocused_DoesNotAskTheInputMethod()
		{
			var (control, window, _) = ShowTwoRows();
			var ime = new FakeInputMethod(window);
			ImeCompositionGuard.CompleteOpenComposition = ime.Complete;

			Click(window, Boxes(control)[1]);

			Assert.That(ime.CompletionRequests, Is.Zero);
		}
	}
}
