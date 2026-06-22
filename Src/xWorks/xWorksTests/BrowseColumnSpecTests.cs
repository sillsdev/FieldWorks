// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

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
	}
}
