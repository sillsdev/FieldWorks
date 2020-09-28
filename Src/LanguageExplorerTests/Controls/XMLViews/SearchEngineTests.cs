// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Controls.XMLViews
{
	[TestFixture]
	public class SearchEngineTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private const string FauxPropertyName = "FauxPropertyName";

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			_flexComponentParameters.PropertyTable.SetProperty(FwUtilsConstants.cache, Cache);
		}

		public override void FixtureTeardown()
		{
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			base.FixtureTeardown();
		}

		[Test]
		public void Search()
		{
			using (var searchEngine = LexEntrySearchEngine.Get(_flexComponentParameters.PropertyTable, FauxPropertyName))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				var form1 = entryFactory.Create(stem, TsStringUtils.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				var form2 = entryFactory.Create(stem, TsStringUtils.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				var form3 = entryFactory.Create(stem, TsStringUtils.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("form1", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo }));

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("form4", Cache.DefaultVernWs)) }),
					Is.Empty);

				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));
			}
		}

		[Test]
		public void SearchFiltersResults()
		{
			using (var searchEngine = LexEntrySearchEngine.Get(_flexComponentParameters.PropertyTable, FauxPropertyName))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				var form1 = entryFactory.Create(stem, TsStringUtils.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				var form2 = entryFactory.Create(stem, TsStringUtils.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				var form3 = entryFactory.Create(stem, TsStringUtils.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();
				searchEngine.FilterThisHvo = form1.Hvo;
				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo }), "form1 entry not filtered out");

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("form1", Cache.DefaultVernWs)) }),
					Is.Empty, "form1 entry not filtered out");

				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo }), "form1 entry not filtered out");
			}
		}

		[Test]
		public void ResetIndex()
		{
			using (var searchEngine = LexEntrySearchEngine.Get(_flexComponentParameters.PropertyTable, FauxPropertyName))
			{
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				var form1 = entryFactory.Create(stem, TsStringUtils.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				var form2 = entryFactory.Create(stem, TsStringUtils.MakeString("form2", Cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				var form3 = entryFactory.Create(stem, TsStringUtils.MakeString("form3", Cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());

				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				var form4 = entryFactory.Create(stem, TsStringUtils.MakeString("form4", Cache.DefaultVernWs), "gloss4", new SandboxGenericMSA());
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form1.Hvo, form2.Hvo, form3.Hvo, form4.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				form1.Delete();
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form2.Hvo, form3.Hvo, form4.Hvo }));

				m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
				form2.LexemeFormOA.Form.SetVernacularDefaultWritingSystem("other");
				form2.SensesOS[0].Gloss.SetAnalysisDefaultWritingSystem("other");
				m_actionHandler.EndUndoTask();

				Assert.That(searchEngine.Search(new[] { new SearchField(LexEntryTags.kflidLexemeForm, TsStringUtils.MakeString("fo", Cache.DefaultVernWs)) }),
					Is.EquivalentTo(new[] { form3.Hvo, form4.Hvo }));
				Assert.That(searchEngine.Search(new[] { new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString("gl", Cache.DefaultAnalWs)) }),
					Is.EquivalentTo(new[] { form3.Hvo, form4.Hvo }));
			}
		}

		private sealed class LexEntrySearchEngine : SearchEngine
		{
			private LexEntrySearchEngine(LcmCache cache)
				: base(cache, SearchType.Prefix)
			{
			}

			internal static LexEntrySearchEngine Get(IPropertyTable propertyTable, string propName)
			{
				var searchEngine = new LexEntrySearchEngine(propertyTable.GetValue<LcmCache>(FwUtilsConstants.cache))
				{
					_propertyTable = propertyTable,
					_searchEnginePropertyName = propName
				};
				// Don't persist it, and if anyone ever cares about hearing that it changed,
				// then create a new override of this method that feeds the last bool parameter in as 'true'.
				// This default method can then feed that override 'false'.
				propertyTable.SetProperty(propName, searchEngine);
				propertyTable.SetPropertyDispose(propName);
				return searchEngine;
			}

			internal int FilterThisHvo { private get; set; }

			protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
			{
				var entry = (ILexEntry)obj;
				var ws = field.String.get_WritingSystemAt(0);
				switch (field.Flid)
				{
					case LexEntryTags.kflidLexemeForm:
						var lf = entry.LexemeFormOA?.Form.StringOrNull(ws);
						if (lf != null && lf.Length > 0)
						{
							yield return lf;
						}
						break;

					case LexSenseTags.kflidGloss:
						foreach (var sense in entry.SensesOS)
						{
							var gloss = sense.Gloss.StringOrNull(ws);
							if (gloss != null && gloss.Length > 0)
							{
								yield return gloss;
							}
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

			protected override bool IsIndexResetRequired(int hvo, int flid)
			{
				if (flid == Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
				{
					return true;
				}
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

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
				base.Dispose(disposing);
			}
		}
	}
}