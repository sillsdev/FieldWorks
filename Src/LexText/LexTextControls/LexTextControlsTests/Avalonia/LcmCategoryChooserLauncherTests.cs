// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;

namespace LexTextControlsTests
{
	/// <summary>
	/// The category chooser's input and result: the project's categories as a tree with a
	/// choice required, and the category a chosen key names. The modal loop itself is not
	/// run here.
	/// </summary>
	[TestFixture]
	public class LcmCategoryChooserLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_irregular;
		private IPartOfSpeech m_noun;

		// Verb with the subcategory Irregular, then Noun; the base's open unit of work takes
		// the edits.
		private void MakeCategories()
		{
			var categories = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			var list = Cache.LangProject.PartsOfSpeechOA;
			list.PossibilitiesOS.Add(m_verb = categories.Create());
			m_verb.Name.SetAnalysisDefaultWritingSystem("Verb");
			m_verb.SubPossibilitiesOS.Add(m_irregular = categories.Create());
			m_irregular.Name.SetAnalysisDefaultWritingSystem("Irregular");
			list.PossibilitiesOS.Add(m_noun = categories.Create());
			m_noun.Name.SetAnalysisDefaultWritingSystem("Noun");
		}

		[Test]
		public void BuildInput_OffersEveryCategoryAsATree_WithNothingSelected_AndRequiresAChoice()
		{
			MakeCategories();

			var input = LcmCategoryChooserLauncher.BuildInput(Cache);

			var mine = input.Candidates.Where(c => new[] { "Verb", "Irregular", "Noun" }.Contains(c.Name)).ToList();
			Assert.That(mine.Select(c => c.Name), Is.EqualTo(new[] { "Verb", "Irregular", "Noun" }), "list order");
			Assert.That(mine.Select(c => c.Depth), Is.EqualTo(new[] { 0, 1, 0 }));
			Assert.That(input.Hierarchical, Is.True);
			Assert.That(input.SelectionMode, Is.EqualTo(ChooserSelectionMode.Single));
			Assert.That(input.AllowEmpty, Is.False);
			Assert.That(input.ForbidEmptySelection, Is.True);
			Assert.That(input.InitialSelectedKeys, Is.Empty, "OK waits for a choice");
		}

		[Test]
		public void Resolve_ReturnsTheCategoryAKeyNames_AndNullForNoneOrAnother()
		{
			MakeCategories();
			var key = LcmCategoryChooserLauncher.BuildInput(Cache).Candidates.Single(c => c.Name == "Noun").Key;

			Assert.That(LcmCategoryChooserLauncher.Resolve(Cache, key), Is.SameAs(m_noun));
			Assert.That(LcmCategoryChooserLauncher.Resolve(Cache, null), Is.Null);
			Assert.That(LcmCategoryChooserLauncher.Resolve(Cache, "not a guid"), Is.Null);
			Assert.That(LcmCategoryChooserLauncher.Resolve(Cache, System.Guid.NewGuid().ToString()), Is.Null);
		}
	}
}
