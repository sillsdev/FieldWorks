// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Which slice a detail-menu request targets in the hidden command adapter. Sibling rows
	/// share one MoForm, so an object-only match returns whichever slice comes first, while
	/// legacy handlers key on <c>CurrentSlice.Flid</c>.
	/// </summary>
	[TestFixture]
	public class DetailMenuTargetingTests
	{
		// The Lexeme Form section's slice order: the Form row, then rows sharing its MoForm.
		private const int MorphHvo = 4242;
		private const int EntryHvo = 11;

		private static IReadOnlyList<(int Hvo, string FieldName)> LexemeFormSection()
			=> new List<(int, string)>
			{
				(EntryHvo, "CitationForm"),
				(MorphHvo, "Form"),
				(MorphHvo, "IsAbstract"),
				(MorphHvo, "MorphType"),
				(MorphHvo, "PhoneEnv")
			};

		[Test]
		public void FieldMatch_WinsOverEarlierSlicesOnTheSameObject()
		{
			var index = RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), MorphHvo, "MorphType");

			Assert.That(index, Is.EqualTo(3),
				"the Morph Type row must target its own slice, not the first slice on the same MoForm");
		}

		[Test]
		public void LexemeFormRow_TargetsTheFormSlice()
		{
			var index = RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), MorphHvo, "Form");

			Assert.That(index, Is.EqualTo(1),
				"GetGuidForJumpToTool requires CurrentSlice.Flid to be the MoForm Form field");
		}

		[Test]
		public void UnknownFieldName_FallsBackToTheObjectMatch()
		{
			// Rows with no slice counterpart (headers, ghosts) fall back to object-only.
			var index = RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), MorphHvo, "NoSuchField");

			Assert.That(index, Is.EqualTo(1), "falls back to the first slice bound to the object");
		}

		[Test]
		public void NoFieldName_FallsBackToTheObjectMatch()
		{
			Assert.That(RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), MorphHvo, null),
				Is.EqualTo(1));
			Assert.That(RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), MorphHvo, string.Empty),
				Is.EqualTo(1));
		}

		[Test]
		public void NoObjectMatch_ReportsNoTarget()
		{
			// The caller then clears CurrentSlice so handlers no-op rather than mis-target.
			Assert.That(RecordEditView.ChooseTargetSliceIndex(LexemeFormSection(), 999, "Form"),
				Is.EqualTo(-1));
			Assert.That(RecordEditView.ChooseTargetSliceIndex(null, MorphHvo, "Form"), Is.EqualTo(-1));
		}

		[Test]
		public void FieldNameMatch_IsScopedToTheRequestedObject()
		{
			// Another object's slice with the SAME field name must not be picked up.
			var candidates = new List<(int, string)>
			{
				(EntryHvo, "Form"),
				(MorphHvo, "Form")
			};

			Assert.That(RecordEditView.ChooseTargetSliceIndex(candidates, MorphHvo, "Form"), Is.EqualTo(1));
		}
	}
}
