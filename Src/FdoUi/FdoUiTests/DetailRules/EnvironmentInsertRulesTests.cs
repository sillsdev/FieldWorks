// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The environment insert rules, as PhoneEnvReferenceView and PhEnvStrRepresentationSlice
	/// answered them before the rules moved here, and as the Avalonia environment menus answer
	/// them now. A selection end below zero is "no selection".
	/// </summary>
	[TestFixture]
	public class EnvironmentInsertRulesTests
	{
		[TestCase(null, true, TestName = "Slash_AllowedInNoText")]
		[TestCase("", true, TestName = "Slash_AllowedInEmptyText")]
		[TestCase("a", true, TestName = "Slash_AllowedWhileTheTextHasNone")]
		[TestCase("/_", false, TestName = "Slash_RefusedOnceTheTextHasOne")]
		[TestCase("a/", false, TestName = "Slash_RefusedWhereverTheSlashSits")]
		public void CanInsertSlash(string text, bool expected)
		{
			Assert.That(EnvironmentInsertRules.CanInsertSlash(text), Is.EqualTo(expected));
		}

		[TestCase("/a", 1, 1, true, TestName = "Bar_AllowedAfterTheSlash")]
		[TestCase("/a", 2, 2, true, TestName = "Bar_AllowedAtTheEnd")]
		[TestCase("/a", 0, 0, false, TestName = "Bar_RefusedBeforeTheSlash")]
		[TestCase("/a", 0, 2, false, TestName = "Bar_RefusedWhenTheSelectionStartsBeforeTheSlash")]
		[TestCase("/a", 2, 0, false, TestName = "Bar_RefusedWhenAReversedSelectionReachesBeforeTheSlash")]
		[TestCase("/_", 2, 2, false, TestName = "Bar_RefusedOnceTheTextHasOne")]
		[TestCase("a", 1, 1, false, TestName = "Bar_RefusedWithoutASlash")]
		[TestCase("", 0, 0, false, TestName = "Bar_RefusedInEmptyText")]
		[TestCase("/a", -1, -1, false, TestName = "Bar_RefusedWithNoSelection")]
		public void CanInsertBar(string text, int anchor, int end, bool expected)
		{
			Assert.That(EnvironmentInsertRules.CanInsertBar(text, anchor, end), Is.EqualTo(expected));
		}

		// The recognizer's own rule: an item goes anywhere after the slash.
		[TestCase("/_", 1, 1, true, TestName = "Item_AllowedRightAfterTheSlash")]
		[TestCase("/_", 2, 2, true, TestName = "Item_AllowedAfterTheBar")]
		[TestCase("/a_b", 2, 2, true, TestName = "Item_AllowedInsideTheLeftContext")]
		[TestCase("/_", 0, 0, false, TestName = "Item_RefusedBeforeTheSlash")]
		[TestCase("a", 1, 1, false, TestName = "Item_RefusedWithoutASlash")]
		[TestCase("", 0, 0, false, TestName = "Item_RefusedInEmptyText")]
		[TestCase("/_", -1, -1, false, TestName = "Item_RefusedWithNoSelection")]
		public void CanInsertItem(string text, int anchor, int end, bool expected)
		{
			Assert.That(EnvironmentInsertRules.CanInsertItem(text, anchor, end), Is.EqualTo(expected));
		}

		// The recognizer's own rule: a word boundary goes right after the slash or at the end.
		[TestCase("/_", 1, 1, true, TestName = "HashMark_AllowedRightAfterTheSlash")]
		[TestCase("/_", 2, 2, true, TestName = "HashMark_AllowedAtTheEnd")]
		[TestCase("/a_b", 2, 2, false, TestName = "HashMark_RefusedInsideTheLeftContext")]
		[TestCase("/_#", 2, 2, false, TestName = "HashMark_RefusedBeforeAnExistingBoundary")]
		[TestCase("/_", 0, 0, false, TestName = "HashMark_RefusedBeforeTheSlash")]
		[TestCase("a", 1, 1, false, TestName = "HashMark_RefusedWithoutASlash")]
		[TestCase("/_", -1, -1, false, TestName = "HashMark_RefusedWithNoSelection")]
		public void CanInsertHashMark(string text, int anchor, int end, bool expected)
		{
			Assert.That(EnvironmentInsertRules.CanInsertHashMark(text, anchor, end), Is.EqualTo(expected));
		}

		[Test]
		public void TheInsertedTexts_AreTheCharactersTheWinFormsViewsType()
		{
			Assert.That(EnvironmentInsertRules.Slash, Is.EqualTo("/"));
			Assert.That(EnvironmentInsertRules.Bar, Is.EqualTo("_"));
			Assert.That(EnvironmentInsertRules.HashMark, Is.EqualTo("#"));
			Assert.That(EnvironmentInsertRules.OptionalItem, Is.EqualTo("()"));
			Assert.That(EnvironmentInsertRules.NaturalClass("C"), Is.EqualTo("[C]"));
		}
	}
}
