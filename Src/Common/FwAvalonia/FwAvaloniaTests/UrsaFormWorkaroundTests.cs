// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using Ursa.Controls;

namespace FwAvaloniaTests
{
	/// <summary>
	/// DataTree overrides four things Ursa's Form/FormItem ControlThemes decide, all of them
	/// undocumented internals of a 1.x dependency. These assert the RESOLVED values on the live
	/// control tree rather than the constants fed in, so an Ursa upgrade that changes a default
	/// fails here instead of silently regressing detail-view layout.
	/// </summary>
	[TestFixture]
	public class UrsaFormWorkaroundTests
	{
		private static (Window window, DataTree view) Show(params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(),
				new List<ViewDiagnostic>());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 520, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, view);
		}

		private static DetailField TextField(string id) =>
			new DetailField(id, id, id, null, DetailFieldKind.Text, EditorClassification.Known,
				id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") },
				null, null, isEditable: true, indent: 0, isCollapsible: false,
				isInitiallyExpanded: true, menuId: null, contextMenuId: null, hotlinksId: null,
				objectHvo: 1234);

		/// <summary>
		/// Ursa's Form ControlTheme sets HorizontalAlignment=Left so the form sizes to content.
		/// The detail pane needs the value column to fill the width instead.
		/// </summary>
		[AvaloniaTest]
		public void Form_StretchesHorizontally_NotSizedToContent()
		{
			var (window, view) = Show(TextField("LexemeForm"));

			var form = view.GetVisualDescendants().OfType<Form>().Single();
			Assert.That(form.HorizontalAlignment, Is.EqualTo(HorizontalAlignment.Stretch),
				"Ursa's Form theme defaults this to Left, which makes the value column hug the "
				+ "left edge instead of filling the pane");
			Assert.That(form.Bounds.Width, Is.GreaterThan(view.Bounds.Width / 2),
				"a stretched form should span the pane, not shrink to its content");
		}

		/// <summary>
		/// Ursa's FormItem ControlTheme sets Margin="0 8" -- 16px of dead space per field. The
		/// scoped style has to beat it.
		/// </summary>
		[AvaloniaTest]
		public void FormItem_UsesTheDetailRowSpacing_NotUrsasDefaultMargin()
		{
			var (window, view) = Show(TextField("LexemeForm"), TextField("CitationForm"));

			var items = view.GetVisualDescendants().OfType<FormItem>().ToList();
			Assert.That(items, Is.Not.Empty, "expected the fields to render as Ursa FormItems");
			var expected = new Thickness(0, FwAvaloniaDensity.RowSpacing);
			foreach (var item in items)
			{
				Assert.That(item.Margin, Is.EqualTo(expected),
					"the scoped style must win over Ursa's 0,8 default; a resolved margin of "
					+ item.Margin + " means the override stopped applying");
			}
		}

		/// <summary>
		/// Ursa's FormItem template binds the label's FontWeight to a bold DynamicResource.
		/// Legacy detail labels are regular weight.
		/// </summary>
		[AvaloniaTest]
		public void Label_StaysRegularWeight_AgainstUrsasBoldBinding()
		{
			var (window, view) = Show(TextField("LexemeForm"));

			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => t.Text == "LexemeForm");
			Assert.That(label.FontWeight, Is.EqualTo(FontWeight.Normal),
				"a bold resolved weight means Ursa's DynamicResource binding is winning");
		}

		/// <summary>
		/// FormItem honors only an absolute LabelWidth, which is why the splitter mirrors the
		/// grid column into it. A relative or auto width silently stops the mirroring working.
		/// </summary>
		[AvaloniaTest]
		public void Form_LabelWidth_IsAbsolute_AndCoversTheSplitterColumn()
		{
			var (window, view) = Show(TextField("LexemeForm"));

			var form = view.GetVisualDescendants().OfType<Form>().Single();
			Assert.That(form.LabelWidth.IsAbsolute, Is.True,
				"FormItem ignores a non-absolute LabelWidth, so the splitter would stop moving "
				+ "the label column");
			Assert.That(form.LabelWidth.Value,
				Is.EqualTo(FwAvaloniaDensity.LabelColumnWidth + FwAvaloniaDensity.SplitterWidth),
				"the form spans the splitter column, so its label column has to cover it too or "
				+ "the value area starts underneath the splitter");
		}
	}
}
