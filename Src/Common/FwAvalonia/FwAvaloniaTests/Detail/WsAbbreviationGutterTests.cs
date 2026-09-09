// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
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
	/// Which rows draw the per-writing-system abbreviation gutter. Legacy draws it for the
	/// multi-alternative <c>MultiStringSlice</c> and not for the single-alternative
	/// <c>StringSlice</c>, so a plain string row -- including a custom field whose writing-system
	/// selector is singular -- must render its value with no abbreviation beside it.
	///
	/// These tests RENDER through <see cref="DataTree"/> rather than calling <see
	/// cref="SliceFactory"/> directly, because the gutter decision is the view's: the composed
	/// model already carries <see cref="DetailField.IsMultiStringRow"/>, and a model-level test
	/// cannot see the view ignoring it.
	/// </summary>
	[TestFixture]
	public class WsAbbreviationGutterTests
	{
		private static DetailField Row(string id, string label, bool isMultiString,
			params (string Abbrev, string Value)[] values)
		{
			var field = new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				values.Select(v => new DetailWsValue(v.Abbrev, v.Value)).ToList(),
				null, null, isEditable: true);
			field.IsMultiStringRow = isMultiString;
			return field;
		}

		private static DataTree Show(params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal",
				fields.ToList(), new List<ViewDiagnostic>());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static bool ShowsAbbreviation(DataTree view, string abbrev)
			=> view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == abbrev);

		[AvaloniaTest]
		public void MultiStringRow_DrawsTheAbbreviationGutter()
		{
			var view = Show(Row("f1", "Form", isMultiString: true,
				("Sen", "barigi"), ("seh", "barigi-seh")));

			Assert.That(ShowsAbbreviation(view, "Sen"), Is.True,
				"a multi-alternative row labels each writing system, like the legacy MultiStringSlice");
			Assert.That(ShowsAbbreviation(view, "seh"), Is.True,
				"every alternative is labelled, not just the first");
		}

		[AvaloniaTest]
		public void PlainStringRow_DrawsNoAbbreviationGutter()
		{
			var view = Show(Row("f1", "Custom Field 1", isMultiString: false, ("Por", "nota")));

			Assert.That(ShowsAbbreviation(view, "Por"), Is.False,
				"a single-alternative row renders like the legacy StringSlice: the value alone, with "
				+ "no writing-system label beside it");
		}

		[AvaloniaTest]
		public void PlainStringRow_StillRendersItsValue_WithoutTheGutter()
		{
			var view = Show(Row("f1", "Custom Field 1", isMultiString: false, ("Por", "nota")));

			var box = view.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
			Assert.That(box, Is.Not.Null, "the row still composes an editable value box");
			Assert.That(box.Text, Is.EqualTo("nota"),
				"suppressing the gutter must not suppress the value");
		}

		[AvaloniaTest]
		public void MixedRows_LabelOnlyTheMultiStringOne()
		{
			var view = Show(
				Row("f1", "Form", isMultiString: true, ("Sen", "barigi")),
				Row("f2", "Custom Field 1", isMultiString: false, ("Por", "nota")));

			Assert.That(ShowsAbbreviation(view, "Sen"), Is.True,
				"the multistring row keeps its gutter");
			Assert.That(ShowsAbbreviation(view, "Por"), Is.False,
				"the plain string row beside it does not gain one");
		}
	}
}
