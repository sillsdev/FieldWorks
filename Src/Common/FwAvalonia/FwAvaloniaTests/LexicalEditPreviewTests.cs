// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Preview;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Headless tests for the preview-host sample path over the shared lexical-edit region renderer.
	/// These keep the preview lane honest without preserving a second detached slice/editor stack.
	/// </summary>
	[TestFixture]
	public class LexicalEditPreviewTests
	{
		private static Window ShowPreview(string dataMode = "sample")
		{
			var provider = new LexicalEditPreviewDataProvider();
			var window = new LexicalEditPreviewWindow
			{
				DataContext = provider.CreateDataContext(dataMode)
			};
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return window;
		}

		private static T Find<T>(Window window, string automationId) where T : Control
			=> window.GetVisualDescendants().OfType<T>()
				.First(control => AutomationProperties.GetAutomationId(control) == automationId);

		[AvaloniaTest]
		public void Preview_RendersTheSharedRegionEditors()
		{
			var window = ShowPreview();
			Assert.That(Find<LexicalEditRegionView>(window, "LexicalEditRegionView"), Is.Not.Null);
			Assert.That(Find<TextBox>(window, "LexemeFormEditor.seh"), Is.Not.Null);
			Assert.That(Find<TextBox>(window, "LexemeFormEditor.en"), Is.Not.Null);
			Assert.That(Find<FwChooserField>(window, "MorphTypeChooser"), Is.Not.Null);
			Assert.That(Find<TextBox>(window, "SenseGlossEditor.en"), Is.Not.Null);
			Assert.That(Find<TextBox>(window, "SenseGlossEditor.pt"), Is.Not.Null);
		}

		[AvaloniaTest]
		public void Preview_WsText_UsesConfiguredFonts()
		{
			var window = ShowPreview();
			Assert.That(Find<TextBox>(window, "LexemeFormEditor.seh").FontFamily.Name, Is.EqualTo("Charis SIL"));
			Assert.That(Find<TextBox>(window, "LexemeFormEditor.en").FontFamily.Name, Is.EqualTo("Times New Roman"));
		}

		[AvaloniaTest]
		public void Preview_MorphTypeChooser_UsesSharedOptionPicker_AndReturnsFocus()
		{
			var window = ShowPreview();
			var chooser = Find<FwChooserField>(window, "MorphTypeChooser");
			Assert.That(chooser.ValueText, Is.EqualTo("stem"));

			chooser.Focus();
			var flyout = (Flyout)chooser.Flyout;
			flyout.ShowAt(chooser);
			Dispatcher.UIThread.RunJobs();

			var picker = (FwOptionPicker)flyout.Content;
			Assert.That(AutomationProperties.GetAutomationId(picker.FilterBox), Is.EqualTo("MorphTypeChooser.Search"));
			Assert.That(AutomationProperties.GetAutomationId(picker.OptionsList), Is.EqualTo("MorphTypeChooser.Options"));
			picker.OptionsList.SelectedIndex = 3;
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(chooser.SelectedKey, Is.EqualTo("suffix"));
			Assert.That(chooser.ValueText, Is.EqualTo("suffix"));
			Assert.That(chooser.IsFocused, Is.True, "focus returns to the chooser after choosing");
		}

		[AvaloniaTest]
		public void Preview_UsesStableAutomationMetadata()
		{
			var window = ShowPreview();
			Assert.That(AutomationProperties.GetAutomationId(Find<LexicalEditRegionView>(window, "LexicalEditRegionView")),
				Is.EqualTo("LexicalEditRegionView"));
			Assert.That(AutomationProperties.GetAutomationId(Find<TextBox>(window, "LexemeFormEditor.seh")),
				Is.EqualTo("LexemeFormEditor.seh"));
			Assert.That(AutomationProperties.GetAutomationId(Find<FwChooserField>(window, "MorphTypeChooser")),
				Is.EqualTo("MorphTypeChooser"));
		}
	}
}