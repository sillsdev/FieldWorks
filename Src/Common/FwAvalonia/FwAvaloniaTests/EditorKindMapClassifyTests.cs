// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Coverage for <see cref="EditorKindMap.Classify"/>: the editor-string ->
	/// classification mapping that drives the importer's diagnostics for dynamic/obsolete/unknown editors
	/// and the grouping-node decision. Pins null/empty, the known/dynamic/obsolete sets, case-insensitivity,
	/// and the whitespace/unknown boundary.
	/// </summary>
	[TestFixture]
	public class EditorKindMapClassifyTests
	{
		[TestCase(null)]
		[TestCase("")]
		public void NullOrEmpty_IsGroupingNone(string editor)
			=> Assert.That(EditorKindMap.Classify(editor), Is.EqualTo(EditorClassification.GroupingNone));

		[TestCase("multistring")]
		[TestCase("string")]
		[TestCase("morphtypeatomicreference")]
		[TestCase("summary")]
		[TestCase("lit")]
		[TestCase("picture")]
		[TestCase("image")]
		[TestCase("jtview")]
		public void KnownEditors_AreKnown(string editor)
			=> Assert.That(EditorKindMap.Classify(editor), Is.EqualTo(EditorClassification.Known));

		[TestCase("MULTISTRING")]
		[TestCase("MorphTypeAtomicReference")]
		public void KnownEditors_AreCaseInsensitive(string editor)
			=> Assert.That(EditorKindMap.Classify(editor), Is.EqualTo(EditorClassification.Known));

		[TestCase("custom")]
		[TestCase("customwithparams")]
		[TestCase("autocustom")]
		public void DynamicEditors_AreDynamic(string editor)
			=> Assert.That(EditorKindMap.Classify(editor), Is.EqualTo(EditorClassification.Dynamic));

		[Test]
		public void MessageEditor_IsObsolete()
			=> Assert.That(EditorKindMap.Classify("message"), Is.EqualTo(EditorClassification.Obsolete));

		[TestCase("   ")]      // whitespace is NOT empty → not a grouping node
		[TestCase("notreal")]
		[TestCase("frobnicate")]
		public void UnrecognizedNonEmpty_IsUnknown(string editor)
			=> Assert.That(EditorKindMap.Classify(editor), Is.EqualTo(EditorClassification.Unknown));

		// ----- ClassifyDetailFieldKind: the dispatch table the composer + mapper both consume -----

		[TestCase(null, DetailEditorCategory.Grouping)]
		[TestCase("", DetailEditorCategory.Grouping)]
		[TestCase("multistring", DetailEditorCategory.Text)]
		[TestCase("string", DetailEditorCategory.Text)]
		[TestCase("morphtypeatomicreference", DetailEditorCategory.MorphTypeChooser)]
		[TestCase("summary", DetailEditorCategory.Summary)]
		[TestCase("lit", DetailEditorCategory.Literal)]
		[TestCase("picture", DetailEditorCategory.Picture)]
		[TestCase("image", DetailEditorCategory.Picture)]
		[TestCase("jtview", DetailEditorCategory.EmbeddedView)]
		[TestCase("command", DetailEditorCategory.Command)]
		[TestCase("possatomicreference", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("defaultatomicreference", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("msareferencecombobox", DetailEditorCategory.MsaChooser)]
		[TestCase("derivmsareference", DetailEditorCategory.MsaChooser)]
		[TestCase("inflmsareference", DetailEditorCategory.MsaChooser)]
		[TestCase("MULTISTRING", DetailEditorCategory.Text)] // case-insensitive
		[TestCase("notreal", DetailEditorCategory.Other)]
		public void ClassifyDetailFieldKind_MapsEditorToCategory(string editor, DetailEditorCategory expected)
			=> Assert.That(EditorKindMap.ClassifyDetailFieldKind(editor), Is.EqualTo(expected));

		[Test]
		public void ClassifyDetailFieldKind_EnumCombo_IsClosedCombo_NotFreeFormText()
		{
			// Safety: a closed enum combo must NOT degrade to a free-form editor that could persist
			// invalid enum values -- this is the regression this dispatch arm guards against.
			Assert.That(EditorKindMap.ClassifyDetailFieldKind("enumcombobox"),
				Is.EqualTo(DetailEditorCategory.EnumCombo));
		}
	}
}
