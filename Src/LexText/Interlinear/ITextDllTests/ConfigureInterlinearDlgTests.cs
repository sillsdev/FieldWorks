using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	class ConfigureInterlinearDlgTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		[Test]
		public void InitRowChoices_MorphemesHaveOwnTable()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Analyze);
			morphemeRows.SetStandardState();
			// Verify preconditions
			Assert.That(morphemeRows.EnabledLineSpecs.Count, Is.EqualTo(8));
			Assert.That(morphemeRows.EnabledLineSpecs[0].WordLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[1].WordLevel && morphemeRows.EnabledLineSpecs[1].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[2].WordLevel && morphemeRows.EnabledLineSpecs[2].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[3].WordLevel && morphemeRows.EnabledLineSpecs[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[4].WordLevel && morphemeRows.EnabledLineSpecs[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[5].WordLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[6].WordLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[7].WordLevel, Is.False);
			// SUT
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(5));
		}

		[Test]
		public void InitRowChoices_CustomSegmentChoiceReturnsOnlyDefaultAnalysisWs()
		{
			CoreWritingSystemDefinition indonesian = null;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("add lang", "remove lang", Cache.ActionHandlerAccessor,
				() =>
				{
					Cache.ServiceLocator.WritingSystemManager.GetOrSet("id", out indonesian);
					Cache.LangProject.CurrentAnalysisWritingSystems.Add(indonesian);
				});
			try
			{
				using (var cf = new CustomFieldForTest(Cache,
					"Candy Apple Red",
					Cache.MetaDataCacheAccessor.GetClassId("Segment"),
					WritingSystemServices.kwsAnal,
					CellarPropertyType.String,
					Guid.Empty))
				{
					var customRow = new InterlinLineChoices(Cache,
						Cache.WritingSystemFactory.GetWsFromStr("fr"),
						Cache.WritingSystemFactory.GetWsFromStr("en"),
						InterlinLineChoices.InterlinMode.Analyze);
					customRow.Add(cf.Flid);
					Assert.That(customRow.EnabledLineSpecs.Count, Is.EqualTo(1));
					Assert.That(customRow.EnabledLineSpecs[0].WordLevel, Is.False);
					Assert.That(customRow.EnabledLineSpecs[0].ComboContent,
						Is.EqualTo(ColumnConfigureDialog.WsComboContent.kwccAnalysis));
					// Set up two column combo items for analysis, one with the default ws handle, and one with indonesian
					// the WritingSystemType and the WritingSystem(Handle) are used by the code to determine if a checkbox is needed
					var columns = new List<WsComboItem>
					{
						new WsComboItem("A Ok", Cache.LangProject.DefaultAnalysisWritingSystem.Id)
						{
							WritingSystem = Cache.LangProject.DefaultAnalysisWritingSystem.Handle,
							WritingSystemType = "analysis"
						},
						new WsComboItem("Begone", indonesian.Id)
						{
							WritingSystem = indonesian.Handle,
							WritingSystemType = "analysis"
						}
					};

					// Verify that only one checkbox is available
					// SUT
					var rowChoices = ConfigureInterlinDialog.InitRowChoices(customRow);
					using (var stringStream = new StringWriter())
					{
						using (var xmlWriter = XmlWriter.Create(stringStream))
						{
							rowChoices.First().GenerateRow(xmlWriter, columns, Cache, customRow);
						}

						var generatedRows = stringStream.ToString();
						var wsEn = Cache.DefaultAnalWs;
						Assert.That(generatedRows, Does.Contain($"{cf.Flid}%{wsEn}"));
						Assert.That(generatedRows,
							Does.Not.Contain($"{cf.Flid}%{indonesian.Id}"));
					}
				}
			}
			finally
			{
				m_actionHandler.Undo();
			}
		}

		[Test]
		public void InitRowChoices_CustomSegmentChoiceWSBothReturnsOnlyOneDefaultWs()
		{
			CoreWritingSystemDefinition indonesian = null;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("add lang", "remove lang", Cache.ActionHandlerAccessor,
				() =>
				{
					Cache.ServiceLocator.WritingSystemManager.GetOrSet("id", out indonesian);
					Cache.LangProject.CurrentAnalysisWritingSystems.Add(indonesian);
				});
			try
			{
				using (var cf = new CustomFieldForTest(Cache,
					"Candy Apple Red",
					Cache.MetaDataCacheAccessor.GetClassId("Segment"),
					WritingSystemServices.kwsAnal,
					CellarPropertyType.String,
					Guid.Empty))
				{
					var customRow = new InterlinLineChoices(Cache,
						Cache.WritingSystemFactory.GetWsFromStr("fr"),
						Cache.WritingSystemFactory.GetWsFromStr("en"),
						InterlinLineChoices.InterlinMode.Analyze);
					customRow.Add(cf.Flid);
					Assert.That(customRow.EnabledLineSpecs.Count, Is.EqualTo(1));
					Assert.That(customRow.EnabledLineSpecs[0].WordLevel, Is.False);
					Assert.That(customRow.EnabledLineSpecs[0].ComboContent,
						Is.EqualTo(ColumnConfigureDialog.WsComboContent.kwccAnalysis));
					// Set up three column combo items:
					//   One with the default analysis ws handle and a Type of "both" (both Analysis and Vernacular)
					//   One with the default vernacular ws handle and a Type of "both" (both Analysis and Vernacular)
					//   One with indonesian
					// The WritingSystemType and the WritingSystem(Handle) are used by the code to determine if a checkbox is needed.
					// For Type "both", the code looks at the the custom field ws to determine which column to display the checkbox.
					var columns = new List<WsComboItem>
					{
						new WsComboItem("A Ok", Cache.LangProject.DefaultAnalysisWritingSystem.Id)
						{
							WritingSystem = Cache.LangProject.DefaultAnalysisWritingSystem.Handle,
							WritingSystemType = "both"
						},
						new WsComboItem("FROk", Cache.LangProject.DefaultVernacularWritingSystem.Id)
						{
							WritingSystem = Cache.LangProject.DefaultVernacularWritingSystem.Handle,
							WritingSystemType = "both"
						},
						new WsComboItem("Begone", indonesian.Id)
						{
							WritingSystem = indonesian.Handle,
							WritingSystemType = "analysis"
						}

					};

					// Verify that only one checkbox is available
					// SUT
					var rowChoices = ConfigureInterlinDialog.InitRowChoices(customRow);
					using (var stringStream = new StringWriter())
					{
						using (var xmlWriter = XmlWriter.Create(stringStream))
						{
							rowChoices.First().GenerateRow(xmlWriter, columns, Cache, customRow);
						}

						var generatedRows = stringStream.ToString();
						var wsEn = Cache.DefaultAnalWs;
						var wsFr = Cache.DefaultVernWs;
						Assert.That(generatedRows, Does.Contain($"{cf.Flid}%{wsEn}"));
						Assert.That(generatedRows,
							Does.Not.Contain($"{cf.Flid}%{wsFr}"));
						Assert.That(generatedRows,
							Does.Not.Contain($"{cf.Flid}%{indonesian.Id}"));
					}
				}
			}
			finally
			{
				m_actionHandler.Undo();
			}
		}

		[Test]
		public void InitRowChoices_ChartChoices()
		{
			var morphemeRows = new InterlinLineChoices(Cache, Cache.WritingSystemFactory.GetWsFromStr("fr"), Cache.WritingSystemFactory.GetWsFromStr("en"), InterlinLineChoices.InterlinMode.Chart);
			morphemeRows.SetStandardChartState();
			// Verify preconditions
			Assert.That(morphemeRows.EnabledLineSpecs.Count, Is.EqualTo(6));
			Assert.That(morphemeRows.EnabledLineSpecs[0].WordLevel, Is.True); // row 1
			Assert.That(morphemeRows.EnabledLineSpecs[1].WordLevel, Is.True); // row 2
			Assert.That(morphemeRows.EnabledLineSpecs[2].WordLevel && morphemeRows.EnabledLineSpecs[2].MorphemeLevel, Is.True); // this and other morpheme combine to row 3
			Assert.That(morphemeRows.EnabledLineSpecs[3].WordLevel && morphemeRows.EnabledLineSpecs[3].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[4].WordLevel && morphemeRows.EnabledLineSpecs[4].MorphemeLevel, Is.True);
			Assert.That(morphemeRows.EnabledLineSpecs[5].WordLevel && morphemeRows.EnabledLineSpecs[5].MorphemeLevel, Is.True);
			// SUT
			var rowChoices = ConfigureInterlinDialog.InitRowChoices(morphemeRows);
			Assert.That(rowChoices.Count(), Is.EqualTo(3));
		}

		[Test]
		public void PreserveWSOrder()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			CoreWritingSystemDefinition deWs;
			wsManager.GetOrSet("de", out deWs);
			int wsGer = deWs.Handle;

			InterlinLineChoices choices = new InterlinLineChoices(Cache, wsFrn, wsEng);
			choices.Add(InterlinLineChoices.kflidWord, wsEng, true); // 0
			choices.Add(InterlinLineChoices.kflidMorphemes, wsEng, true); // 1
			choices.Add(InterlinLineChoices.kflidLexEntries, wsEng, true); // 2
			choices.Add(InterlinLineChoices.kflidLexGloss, wsEng, true); // 3
			choices.Add(InterlinLineChoices.kflidLexGloss, wsFrn, true); // 4
			choices.Add(InterlinLineChoices.kflidLexGloss, wsGer, true); // 5
			choices.Add(InterlinLineChoices.kflidLexPos, wsEng, true); // 6
			choices.Add(InterlinLineChoices.kflidWordGloss, wsEng, true); // 7
			choices.Add(InterlinLineChoices.kflidWordPos, wsEng, true); // 8
			choices.Add(InterlinLineChoices.kflidFreeTrans, wsGer, true); // 9
			choices.Add(InterlinLineChoices.kflidFreeTrans, wsFrn, true); // 10
			choices.Add(InterlinLineChoices.kflidFreeTrans, wsEng, true); // 11
			choices.Add(InterlinLineChoices.kflidLitTrans, wsEng, true); // 12
			choices.Add(InterlinLineChoices.kflidNote, wsEng, true); // 13

			// Simulate moving kflidWordPos above kflidWordGloss and kflidLitTrans above kflidFreeTrans.
			List<int> orderedFlids = new List<int>();
			orderedFlids.Add(InterlinLineChoices.kflidWord); // 0
			orderedFlids.Add(InterlinLineChoices.kflidMorphemes); // 1
			orderedFlids.Add(InterlinLineChoices.kflidLexEntries); // 2
			orderedFlids.Add(InterlinLineChoices.kflidLexGloss); // 3
			orderedFlids.Add(InterlinLineChoices.kflidLexPos); // 4
			orderedFlids.Add(InterlinLineChoices.kflidWordPos); // 5
			orderedFlids.Add(InterlinLineChoices.kflidWordGloss); // 6
			orderedFlids.Add(InterlinLineChoices.kflidLitTrans); // 7
			orderedFlids.Add(InterlinLineChoices.kflidFreeTrans); // 8
			orderedFlids.Add(InterlinLineChoices.kflidNote); // 9

			List<InterlinLineSpec> newLineSpecsUnordered = new List<InterlinLineSpec>();
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidWord, wsEng, true)); // 0
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidMorphemes, wsEng, true)); // 1
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLexEntries, wsEng, true)); // 2
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLexGloss, wsEng, true)); // 3
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLexGloss, wsFrn, true)); // 4
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLexGloss, wsGer, true)); // 5
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLexPos, wsEng, true)); // 6
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidWordPos, wsEng, true)); // 7
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidWordGloss, wsEng, true)); // 8
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidLitTrans, wsEng, true)); // 9
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidFreeTrans, wsEng, true)); // 10
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidFreeTrans, wsFrn, true)); // 11
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidFreeTrans, wsGer, true)); // 12
			newLineSpecsUnordered.Add(choices.CreateSpec(InterlinLineChoices.kflidNote, wsEng, true)); // 13

			ConfigureInterlinDialog.OrderAllSpecs(choices, orderedFlids, newLineSpecsUnordered);

			// Validate that the order of the flid's in choices is the new order.
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.AllLineSpecs[0].Flid); // 0
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.AllLineSpecs[1].Flid); // 1
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.AllLineSpecs[2].Flid); // 2
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[3].Flid); // 3
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[4].Flid); // 4
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[5].Flid); // 5
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices.AllLineSpecs[6].Flid); // 6
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.AllLineSpecs[7].Flid); // 7
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.AllLineSpecs[8].Flid); // 8
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices.AllLineSpecs[9].Flid); // 9
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.AllLineSpecs[10].Flid); // 10
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.AllLineSpecs[11].Flid); // 11
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.AllLineSpecs[12].Flid); // 12
			Assert.AreEqual(InterlinLineChoices.kflidNote, choices.AllLineSpecs[13].Flid); // 13

			// Valiate that the original order of the ws is preserved.
			Assert.AreEqual(wsEng, choices.AllLineSpecs[3].WritingSystem); // 3
			Assert.AreEqual(wsFrn, choices.AllLineSpecs[4].WritingSystem); // 4
			Assert.AreEqual(wsGer, choices.AllLineSpecs[5].WritingSystem); // 5
			Assert.AreEqual(wsGer, choices.AllLineSpecs[10].WritingSystem); // 10
			Assert.AreEqual(wsFrn, choices.AllLineSpecs[11].WritingSystem); // 11
			Assert.AreEqual(wsEng, choices.AllLineSpecs[12].WritingSystem); // 12
		}
	}
}
