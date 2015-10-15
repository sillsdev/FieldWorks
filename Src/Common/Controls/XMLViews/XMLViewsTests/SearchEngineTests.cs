using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;

namespace XMLViewsTests
{
	[TestFixture]
	public class SearchEngineTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void Search()
		{
			using (var searchEngine = new LexEntrySearchEngine(Cache))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				IMoMorphType stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				ILexEntry form1 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry form2 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry form3 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] {form1.Hvo, form2.Hvo, form3.Hvo}));

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("form1", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo }));

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("form4", Cache.DefaultVernWs)) }),
					Is.Empty);

				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));
			}
		}

		[Test]
		public void SearchFiltersResults()
		{
			using(var searchEngine = new LexEntrySearchEngine(Cache))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				IMoMorphType stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				ILexEntry form1 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry form2 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry form3 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();
				searchEngine.FilterThisHvo = form1.Hvo;
				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo }), "form1 entry not filtered out");

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("form1", Cache.DefaultVernWs)) }),
					Is.Empty, "form1 entry not filtered out");

				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo }), "form1 entry not filtered out");
			}
		}

		[Test]
		public void ResetIndex()
		{
			using (var searchEngine = new LexEntrySearchEngine(Cache))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				IMoMorphType stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				ILexEntry form1 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry form2 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry form3 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				ILexEntry form4 = entryFactory.Create(stem, Cache.TsStrFactory.MakeString("form4", Cache.DefaultVernWs), "gloss4", new SandboxGenericMSA());
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo, form4.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				form1.Delete();
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo, form4.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				form2.LexemeFormOA.Form.SetVernacularDefaultWritingSystem("other");
				form2.SensesOS[0].Gloss.SetAnalysisDefaultWritingSystem("other");
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, Cache.TsStrFactory.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, Cache.TsStrFactory.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form3.Hvo, form4.Hvo }));
			}
		}

		private class LexEntrySearchEngine : SearchEngine
		{
			public LexEntrySearchEngine(FdoCache cache)
				: base(cache, SearchType.Prefix)
			{
			}

			protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
			{
				var entry = (ILexEntry) obj;

				int ws = field.String.get_WritingSystemAt(0);
				switch (field.Flid)
				{
					case LexEntryTags.kflidLexemeForm:
						if (entry.LexemeFormOA != null)
						{
							var lf = entry.LexemeFormOA.Form.StringOrNull(ws);
							if (lf != null && lf.Length > 0)
								yield return lf;
						}
						break;

					case LexSenseTags.kflidGloss:
						foreach (ILexSense sense in entry.SensesOS)
						{
							var gloss = sense.Gloss.StringOrNull(ws);
							if (gloss != null && gloss.Length > 0)
								yield return gloss;
						}
						break;
				}
			}

			protected override IList<ICmObject> GetSearchableObjects()
			{
				return Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Cast<ICmObject>().ToArray();
			}

			protected override IEnumerable<int> FilterResults(IEnumerable<int> results)
			{
				return results.Where(hvo => hvo != FilterThisHvo);
			}

			public int FilterThisHvo { private get; set; }

			protected override bool IsIndexResetRequired(int hvo, int flid)
			{
				if (flid == Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
					return true;

				switch (flid)
				{
					case LexEntryTags.kflidLexemeForm:
					case LexEntryTags.kflidSenses:
					case MoFormTags.kflidForm:
					case LexSenseTags.kflidSenses:
					case LexSenseTags.kflidGloss:
						return true;
				}
				return false;
			}

			protected override bool IsFieldMultiString(SearchField field)
			{
				return true;
			}
		}
	}
}
