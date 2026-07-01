// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Coverage for <see cref="EditorKindMap.Classify"/> (previously untested): the editor-string →
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

		// ----- ClassifyRegionFieldKind: the dispatch table the composer + mapper both consume -----

		[TestCase(null, RegionEditorCategory.Grouping)]
		[TestCase("", RegionEditorCategory.Grouping)]
		[TestCase("multistring", RegionEditorCategory.Text)]
		[TestCase("string", RegionEditorCategory.Text)]
		[TestCase("morphtypeatomicreference", RegionEditorCategory.MorphTypeChooser)]
		[TestCase("summary", RegionEditorCategory.Summary)]
		[TestCase("lit", RegionEditorCategory.Literal)]
		[TestCase("picture", RegionEditorCategory.Picture)]
		[TestCase("image", RegionEditorCategory.Picture)]
		[TestCase("jtview", RegionEditorCategory.EmbeddedView)]
		[TestCase("command", RegionEditorCategory.Command)]
		[TestCase("possatomicreference", RegionEditorCategory.AtomicReferenceChooser)]
		[TestCase("defaultatomicreference", RegionEditorCategory.AtomicReferenceChooser)]
		[TestCase("msareferencecombobox", RegionEditorCategory.MsaChooser)]
		[TestCase("derivmsareference", RegionEditorCategory.MsaChooser)]
		[TestCase("inflmsareference", RegionEditorCategory.MsaChooser)]
		[TestCase("MULTISTRING", RegionEditorCategory.Text)] // case-insensitive
		[TestCase("notreal", RegionEditorCategory.Other)]
		public void ClassifyRegionFieldKind_MapsEditorToCategory(string editor, RegionEditorCategory expected)
			=> Assert.That(EditorKindMap.ClassifyRegionFieldKind(editor), Is.EqualTo(expected));

		[Test]
		public void ClassifyRegionFieldKind_EnumCombo_IsClosedCombo_NotFreeFormText()
		{
			// Safety: a closed enum combo must NOT degrade to a free-form editor that could persist
			// invalid enum values — this is the regression this dispatch arm guards against.
			Assert.That(EditorKindMap.ClassifyRegionFieldKind("enumcombobox"),
				Is.EqualTo(RegionEditorCategory.EnumCombo));
		}
	}
}
