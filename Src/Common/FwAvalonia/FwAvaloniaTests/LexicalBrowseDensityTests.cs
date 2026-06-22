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
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// OpenSpec 2.5 — density parity for the integrated (editable-table) browse view: the virtualized
	/// rows must honor the compact FwAvaloniaDensity tokens rather than the bloated Fluent ListBoxItem
	/// defaults, so the table matches the legacy XMLViews row density.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseDensityTests
	{
		private sealed class StubRowSource : IBrowseRowSource
		{
			public int RowCount => 50;
			public IReadOnlyList<string> GetCellValues(int rowIndex) =>
				new[] { $"lexeme {rowIndex}", $"gloss {rowIndex}" };
		}

		private static ViewDefinitionModel TwoColumnDefinition() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "string",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "string",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void BrowseRows_UseCompactDensity_NotTheFluentDefault()
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), new StubRowSource());
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var item = view.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault();
			Assert.That(item, Is.Not.Null, "rows realize");
			Assert.That(item.Padding, Is.EqualTo(FwAvaloniaDensity.BrowseRowPadding),
				"rows use the compact density padding, not the Fluent default");
			Assert.That(item.MinHeight, Is.EqualTo(FwAvaloniaDensity.BrowseRowMinHeight),
				"rows use the compact density row height, not the Fluent min-height floor");
		}
	}
}
