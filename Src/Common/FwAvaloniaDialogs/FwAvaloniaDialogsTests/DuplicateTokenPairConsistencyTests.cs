// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Guards the token-hygiene check's blind spot: a few value pairs must be kept numerically
	/// equal by COMMENT convention alone, because the check (Build/Agent/TokenHygiene.psm1) can
	/// never see both sides of the pair at once -- either both sides live in a file the check
	/// whole-file allowlists (<see cref="CompactDialogStyles"/>, since Avalonia's compiled XAML
	/// rejects <c>x:Static</c> as a resource declaration -- see that class's own doc comment),
	/// or one side is a Setter literal inside DialogTheme.axaml's declarative Styles, which the
	/// check's own x:Key-declaration skip does not re-examine as a usage.
	///
	/// Each test resolves BOTH sides through the real code path -- <see
	/// cref="CompactDialogStyles.Apply"/> realized on a live control for the
	/// CompactDialogStyles side, <see cref="DialogThemeBootstrap.Apply"/> + TryGetResource for
	/// the DialogTheme.axaml side, <see cref="FwAvaloniaDensity"/> for the DataTree side -- and
	/// asserts numeric equality, so an edit to one side that forgets its documented partner
	/// fails a real test instead of silently drifting.
	/// </summary>
	[TestFixture]
	public class DuplicateTokenPairConsistencyTests
	{
		// ----- CompactDialogStyles.Build() literals vs DialogTheme.axaml's Dialog*Padding keys
		// -----

		[AvaloniaTest]
		public void ButtonPadding_CompactDialogStylesMatchesDialogTheme()
		{
			var button = RealizeCompactStyled<Button>();
			var themePadding = ResolveDialogThemeThickness("DialogButtonPadding");
			Assert.That(button.Padding, Is.EqualTo(themePadding),
				"CompactDialogStyles' Button padding must stay numerically equal to DialogButtonPadding");
		}

		[AvaloniaTest]
		public void ComboBoxPadding_CompactDialogStylesMatchesDialogTheme()
		{
			var comboBox = RealizeCompactStyled<ComboBox>();
			var themePadding = ResolveDialogThemeThickness("DialogComboBoxPadding");
			Assert.That(comboBox.Padding, Is.EqualTo(themePadding),
				"CompactDialogStyles' ComboBox padding must stay numerically equal to DialogComboBoxPadding");
		}

		[AvaloniaTest]
		public void TextBoxPadding_CompactDialogStylesMatchesDialogTheme()
		{
			var textBox = RealizeCompactStyled<TextBox>();
			var themePadding = ResolveDialogThemeThickness("DialogTextBoxPadding");
			Assert.That(textBox.Padding, Is.EqualTo(themePadding),
				"CompactDialogStyles' TextBox padding must stay numerically equal to DialogTextBoxPadding");
		}

		[AvaloniaTest]
		public void TabItemPadding_CompactDialogStylesMatchesDialogTheme()
		{
			var tabItem = RealizeCompactStyled<TabItem>();
			var themePadding = ResolveDialogThemeThickness("DialogTabItemPadding");
			Assert.That(tabItem.Padding, Is.EqualTo(themePadding),
				"CompactDialogStyles' TabItem padding must stay numerically equal to DialogTabItemPadding");
		}

		[AvaloniaTest]
		public void ListBoxItemPadding_CompactDialogStylesMatchesDialogTheme()
		{
			var listBoxItem = RealizeCompactStyled<ListBoxItem>();
			var themePadding = ResolveDialogThemeThickness("DialogListBoxItemPadding");
			Assert.That(listBoxItem.Padding, Is.EqualTo(themePadding),
				"CompactDialogStyles' ListBoxItem padding must stay numerically equal to DialogListBoxItemPadding");
		}

		// ----- DataTree.ListRowPadding vs DialogListBoxItemPadding (a second, DataTree-side
		// pairing
		// of the same DialogListBoxItemPadding key -- see DataTree.ListRowPadding's own comment)
		// -----

		[AvaloniaTest]
		public void DataTreeListRowPadding_MatchesDialogListBoxItemPadding()
		{
			var themePadding = ResolveDialogThemeThickness("DialogListBoxItemPadding");
			Assert.That(FwAvaloniaDensity.ListRowPadding, Is.EqualTo(themePadding),
				"DataTree.ListRowPadding must stay numerically equal to DialogListBoxItemPadding");
		}

		// ----- DialogGroupSeparation vs DataTree.GroupSeparation -----

		[AvaloniaTest]
		public void DialogGroupSeparation_TopMatchesDataTreeGroupSeparation()
		{
			var themeMargin = ResolveDialogThemeThickness("DialogGroupSeparation");
			Assert.That(themeMargin.Top, Is.EqualTo(FwAvaloniaDensity.GroupSeparation),
				"DialogGroupSeparation's top component must stay numerically equal to DataTree.GroupSeparation");
		}

		// ----- DialogFieldBorderThickness vs DataTree.HairlineBorderThickness -----

		[AvaloniaTest]
		public void DialogFieldBorderThickness_MatchesDataTreeHairlineBorderThickness()
		{
			var themeThickness = ResolveDialogThemeThickness("DialogFieldBorderThickness");
			Assert.That(themeThickness, Is.EqualTo(FwAvaloniaDensity.HairlineBorderThickness),
				"DialogFieldBorderThickness must stay numerically equal to DataTree.HairlineBorderThickness");
		}

		// ----- helpers -----

		/// <summary>
		/// Builds a bare <typeparamref name="T"/>, applies <see cref="CompactDialogStyles"/> to
		/// its
		/// PARENT (Styles target descendants, not the styled element itself -- the same reason
		/// DialogThemeBootstrap applies the dialog theme to the dialog body rather than each
		/// field),
		/// realizes it in a headless window so the style selector actually matches, and returns
		/// the
		/// live control with its styled Padding resolved.
		/// </summary>
		private static T RealizeCompactStyled<T>() where T : Control, new()
		{
			var control = new T();
			var container = new StackPanel { Children = { control } };
			CompactDialogStyles.Apply(container);

			var window = new Window { Content = container };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			control.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();

			return control;
		}

		/// <summary>
		/// Resolves a DialogTheme.axaml-local <c>Dialog*</c> key the same way every real dialog
		/// view
		/// does -- <see cref="DialogThemeBootstrap.Apply"/> on a fresh probe control, then
		/// TryGetResource -- rather than re-parsing DialogTheme.axaml's markup.
		/// </summary>
		private static Thickness ResolveDialogThemeThickness(string key)
		{
			var probe = new Border();
			DialogThemeBootstrap.Apply(probe);
			var found = probe.TryGetResource(key, null, out var value);
			Assert.That(found, Is.True, $"DialogTheme.axaml must declare '{key}'");
			return (Thickness)value;
		}
	}
}
