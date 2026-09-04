// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// The boolean row control (<see cref="FwBooleanField"/>): the checkbox a legacy
	/// <c>CheckBoxSlice</c> drew, with the row's own label as its caption. These tests RENDER the
	/// control, because a row can compose correctly in the detail model and still draw nothing --
	/// the model-level composer tests cannot see that.
	/// </summary>
	[TestFixture]
	public class FwBooleanFieldTests
	{
		private static DetailField BooleanField(bool value, bool isEditable = true)
			=> new DetailField("test/IsAbstract", "Is Abstract Form", "IsAbstract", null,
				DetailFieldKind.Boolean, EditorClassification.Known, null, null, HostRouting.Inherit,
				null, null, value ? bool.TrueString : bool.FalseString, isEditable);

		private static (Control Editor, Window Window) Show(DetailField field,
			IDetailEditContext editContext, System.Action save = null)
		{
			var editor = SliceFactory.Build(field, "IsAbstract",
				new SliceFactoryContext(editContext: editContext, save: save));
			var window = new Window { Content = editor, Width = 420, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (editor, window);
		}

		[AvaloniaTest]
		public void Build_BooleanField_ProducesACheckbox()
		{
			var (editor, _) = Show(BooleanField(false), new FakeDetailEditContext());

			Assert.That(editor, Is.InstanceOf<FwBooleanField>(),
				"a boolean row dispatches to the checkbox control");
		}

		[AvaloniaTest]
		public void Render_BooleanField_AppliesACheckBoxTemplate()
		{
			var (editor, _) = Show(BooleanField(false), new FakeDetailEditContext());

			Assert.That(editor.IsVisible, Is.True, "the checkbox row is visible");

			// Neither bounds nor a non-empty visual tree prove a template: an untemplated control
			// still measures to its padding and still has children. The box border is what the
			// user
			// clicks, so assert that.
			Assert.That(editor.GetVisualDescendants().OfType<Border>().Any(), Is.True,
				"an untemplated row draws no box and cannot be clicked");
		}

		[AvaloniaTest]
		public void Render_BooleanField_ReflectsTheModelValue()
		{
			var (checkedEditor, _) = Show(BooleanField(true), new FakeDetailEditContext());
			var (clearEditor, _) = Show(BooleanField(false), new FakeDetailEditContext());

			Assert.That(((FwBooleanField)checkedEditor).IsChecked, Is.True,
				"a true row renders checked");
			Assert.That(((FwBooleanField)clearEditor).IsChecked, Is.False,
				"a false row renders clear");
		}

		[AvaloniaTest]
		public void Toggle_BooleanField_StagesTheNewValue()
		{
			var context = new FakeDetailEditContext();
			var (editor, _) = Show(BooleanField(false), context);
			var checkbox = (FwBooleanField)editor;

			Assert.That(checkbox.IsEnabled, Is.True, "an editable row with a context accepts input");

			checkbox.IsChecked = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.OptionEdits.Select(e => e.Key), Does.Contain(bool.TrueString),
				"toggling stages through the shared option path");
		}

		[AvaloniaTest]
		public void Toggle_BooleanField_RevertsWhenTheEditIsRefused()
		{
			var context = new FakeDetailEditContext { OptionResult = false };
			var (editor, _) = Show(BooleanField(false), context);
			var checkbox = (FwBooleanField)editor;

			checkbox.IsChecked = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(checkbox.IsChecked, Is.False,
				"a refused edit puts the box back rather than showing a state the model lacks");
		}

		/// <summary>
		/// A toggle COMMITS, it does not merely stage. Staging alone left the box ticked with
		/// nothing on the undo stack until focus happened to move off the row, so Ctrl+Z appeared
		/// to do nothing. Legacy's CheckBoxSlice writes the toggle as it lands.
		/// </summary>
		[AvaloniaTest]
		public void Toggle_BooleanField_CommitsImmediately_SoUndoWorksWithoutMovingFocus()
		{
			var commits = 0;
			var context = new FakeDetailEditContext();
			var (editor, _) = Show(BooleanField(false), context, () => commits++);

			((FwBooleanField)editor).IsChecked = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(commits, Is.EqualTo(1),
				"the gesture completes on the toggle -- not on some later focus change");
		}

		[AvaloniaTest]
		public void Toggle_BooleanField_DoesNotCommit_WhenTheEditIsRefused()
		{
			var commits = 0;
			var context = new FakeDetailEditContext { OptionResult = false };
			var (editor, _) = Show(BooleanField(false), context, () => commits++);

			((FwBooleanField)editor).IsChecked = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(commits, Is.Zero,
				"a refused edit commits nothing; committing here would push an empty step onto "
				+ "the undo stack");
		}

		[AvaloniaTest]
		public void Render_BooleanField_IsReadOnly_WhenNoEditContext()
		{
			var (editor, _) = Show(BooleanField(true), null);

			Assert.That(editor.IsEnabled, Is.False,
				"a null edit context yields read-only display, like every other row");
			Assert.That(((FwBooleanField)editor).IsChecked, Is.True,
				"a read-only row still shows the model's value");
		}
	}
}
