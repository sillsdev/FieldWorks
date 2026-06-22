// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Unit coverage for the owned browse column model (rendering-cutover F1): the pure
	/// snapshot→view-definition projection that replaces the table's fabricated <c>"col{i}"</c> column
	/// tokens. Pins the stable-field fallback chain and the field/label/ws carried onto each view node
	/// (the live-viewer <c>Snapshot</c> loop is exercised by the browse integration path, not here).
	/// </summary>
	[TestFixture]
	public class BrowseColumnSpecTests
	{
		private static BrowseColumnSpec Col(int index, string label, string field, string ws, string transduce)
			=> new BrowseColumnSpec(index, label, field, ws, transduce, isEditable: false);

		[Test]
		public void StableField_PrefersField_ThenTransduce_ThenPositional()
		{
			Assert.That(Col(0, "Gloss", "Gloss", "analysis", "LexSense.Gloss").StableField, Is.EqualTo("Gloss"),
				"an explicit field wins");
			Assert.That(Col(1, "Lexeme Form", null, "vernacular", "LexEntry.LexemeForm.Form").StableField,
				Is.EqualTo("LexEntry.LexemeForm.Form"), "no field → the transduce target identifies the column");
			Assert.That(Col(2, "Custom", null, null, null).StableField, Is.EqualTo("col2"),
				"neither → a positional token, never empty");
		}

		[Test]
		public void ToFieldNode_CarriesLabelStableFieldAndWritingSystem()
		{
			var node = Col(3, "Headword", null, "vernacular", "LexEntry.CitationForm").ToFieldNode();
			Assert.That(node.Kind, Is.EqualTo(ViewNodeKind.Field));
			Assert.That(node.StableId, Is.EqualTo("browse/#3"));
			Assert.That(node.Label, Is.EqualTo("Headword"));
			Assert.That(node.Field, Is.EqualTo("LexEntry.CitationForm"), "the stable field token, not col3");
			Assert.That(node.WritingSystem, Is.EqualTo("vernacular"));
		}

		[Test]
		public void ToFieldNode_NullLabel_FallsBackToPositionalHeader()
			=> Assert.That(Col(4, null, "X", null, null).ToFieldNode().Label, Is.EqualTo("Column 5"));

		[Test]
		public void ToViewDefinition_ProjectsAllColumnsInOrder()
		{
			var columns = new List<BrowseColumnSpec>
			{
				Col(0, "Form", null, "vernacular", "LexEntry.LexemeForm.Form"),
				Col(1, "Gloss", "Gloss", "analysis", "LexSense.Gloss")
			};
			var model = BrowseColumnSpec.ToViewDefinition(columns);
			var fields = model.Roots.Where(n => n.Kind == ViewNodeKind.Field).ToList();
			Assert.That(fields.Select(n => n.Label), Is.EqualTo(new[] { "Form", "Gloss" }));
			Assert.That(fields.Select(n => n.Field),
				Is.EqualTo(new[] { "LexEntry.LexemeForm.Form", "Gloss" }));
		}

		// Task 18: Snapshot reads only IBrowseColumnSource members, so it widens from the concrete
		// BrowseViewer to the interface with no behavior change — proven here against a pure stub source
		// (the F2 viewer-free provider will satisfy the same seam).
		[Test]
		public void Snapshot_ReadsTheColumnSourceInterface_NoViewerNeeded()
		{
			var source = new FakeColumnSource(new[]
			{
				("Form", (string)null, "vernacular", "LexEntry.LexemeForm.Form", true),
				("Gloss", "Gloss", "analysis", "LexSense.Gloss", false)
			});

			var specs = BrowseColumnSpec.Snapshot(source);

			Assert.That(specs.Count, Is.EqualTo(2));
			Assert.That(specs[0].Label, Is.EqualTo("Form"));
			Assert.That(specs[0].StableField, Is.EqualTo("LexEntry.LexemeForm.Form"),
				"no field → transduce target is the stable token");
			Assert.That(specs[0].IsEditable, Is.True);
			Assert.That(specs[1].Field, Is.EqualTo("Gloss"));
			Assert.That(specs[1].WritingSystem, Is.EqualTo("analysis"));
			Assert.That(specs[1].IsEditable, Is.False);
		}

		// A minimal IBrowseColumnSource exposing only the column-metadata members Snapshot touches; the
		// cell/sort/filter members throw to prove Snapshot never reaches them.
		private sealed class FakeColumnSource : IBrowseColumnSource
		{
			private readonly (string label, string field, string ws, string transduce, bool editable)[] _cols;
			public FakeColumnSource((string, string, string, string, bool)[] cols) { _cols = cols; }
			public int ColumnCount => _cols.Length;
			public string GetColumnName(int icol) => _cols[icol].label;
			public void GetColumnEditAttributes(int icol, out string field, out string ws, out string transduce)
			{ field = _cols[icol].field; ws = _cols[icol].ws; transduce = _cols[icol].transduce; }
			public bool IsColumnEditable(int icol) => _cols[icol].editable;
			public IReadOnlyList<BrowseColumnInfo> GetAvailableColumns() => throw new NotSupportedException();
			public string GetColumnKey(int icol) => _cols[icol].field;
			public IReadOnlyList<string> GetRowCellStrings(IManyOnePathSortItem item) => throw new NotSupportedException();
			public ITsString GetRowCellTsString(IManyOnePathSortItem item, int icol) => throw new NotSupportedException();
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending) => throw new NotSupportedException();
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending, bool sortedFromEnd, bool sortedByLength) => throw new NotSupportedException();
			public RecordFilter MakeColumnFilter(int dataColumnIndex, BrowseColumnFilterKind kind, string text) => throw new NotSupportedException();
			public RecordFilter MakePatternColumnFilter(int dataColumnIndex, string pattern, BrowsePatternMatchType matchType, bool matchCase) => throw new NotSupportedException();
			public RecordFilter MakeStringListColumnFilter(int dataColumnIndex, string value, bool exclude) => throw new NotSupportedException();
			public string[] GetColumnStringList(int dataColumnIndex) => throw new NotSupportedException();
			public string GetColumnSpecAttribute(int icol, string attrName) => throw new NotSupportedException();
			public string GetBulkEditSpecAttribute(string attrName) => null;
			public RecordFilter MakeDateColumnFilter(int dataColumnIndex, BrowseDateMatchKind kind, System.DateTime start, System.DateTime end, bool handleGenDate) => throw new NotSupportedException();
			public IReadOnlyList<BrowseChooserItem> GetColumnChooserList(int dataColumnIndex) => throw new NotSupportedException();
			public RecordFilter MakeListChoiceColumnFilter(int dataColumnIndex, IReadOnlyList<string> chosenKeys) => throw new NotSupportedException();
			public bool ColumnSupportsSpellingFilter(int dataColumnIndex) => throw new NotSupportedException();
			public RecordFilter MakeSpellingErrorColumnFilter(int dataColumnIndex) => throw new NotSupportedException();
		}
	}
}
