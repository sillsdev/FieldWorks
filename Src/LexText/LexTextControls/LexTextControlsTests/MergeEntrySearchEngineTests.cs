// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextControlsTests
{
	/// <summary>
	/// The Merge Entry search engine must exclude the starting entry from its matches in BOTH
	/// full-text and substring modes, so an entry can never be offered as a target for merging
	/// into itself. The substring case matters because a typical multi-character merge-target
	/// query runs under substring matching (see <see cref="SubstringSearchPolicy"/>).
	/// </summary>
	[TestFixture]
	public class MergeEntrySearchEngineTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _language;
		private ILexEntry _languor;

		// The base opens a UOW in TestSetup and calls CreateTestData() inside it, so data is
		// created directly here with no UOW wrapper (a nested task would throw).
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_language = MakeEntry("language", "speech");
			_languor = MakeEntry("languor", "weariness");
		}

		private ILexEntry MakeEntry(string lexemeForm, string gloss)
		{
			var components = new LexEntryComponents
			{
				MorphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem)
			};
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexemeForm, Cache.DefaultVernWs));
			components.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
		}

		private IEnumerable<SearchField> LexemeFormQuery(string query)
		{
			var tss = TsStringUtils.MakeString(query, Cache.DefaultVernWs);
			return new[] { new SearchField(LexEntryTags.kflidLexemeForm, tss) };
		}

		[Test]
		public void FullTextEngine_ExcludesTheStartingEntry()
		{
			using (var engine = new MergeEntryDlg.MergeEntrySearchEngine(Cache, SearchType.FullText))
			{
				// "langu" is a word-prefix shared by both entries, so full-text matches both.
				var withoutExclusion = engine.Search(LexemeFormQuery("langu")).ToList();
				Assert.That(withoutExclusion, Does.Contain(_language.Hvo).And.Contain(_languor.Hvo),
					"both entries match the shared prefix before any entry is excluded");

				engine.CurrentEntryHvo = _language.Hvo;
				var withExclusion = engine.Search(LexemeFormQuery("langu")).ToList();
				Assert.That(withExclusion, Does.Not.Contain(_language.Hvo),
					"the starting entry is never offered as a target for merging into itself");
				Assert.That(withExclusion, Does.Contain(_languor.Hvo),
					"the other matching entry still appears");
			}
		}

		[Test]
		public void SubstringEngine_ExcludesTheStartingEntry()
		{
			using (var engine = new MergeEntryDlg.MergeEntrySearchEngine(Cache, SearchType.Substring))
			{
				// "angu" is an interior substring of both entries and a prefix of neither, so it
				// exercises the substring (match-anywhere) path specifically.
				var withoutExclusion = engine.Search(LexemeFormQuery("angu")).ToList();
				Assert.That(withoutExclusion, Does.Contain(_language.Hvo).And.Contain(_languor.Hvo),
					"both entries match the interior substring before any entry is excluded");

				engine.CurrentEntryHvo = _language.Hvo;
				var withExclusion = engine.Search(LexemeFormQuery("angu")).ToList();
				Assert.That(withExclusion, Does.Not.Contain(_language.Hvo),
					"substring results also exclude the starting entry (the self-merge guard)");
				Assert.That(withExclusion, Does.Contain(_languor.Hvo),
					"the other interior-substring match still appears");
			}
		}
	}
}
