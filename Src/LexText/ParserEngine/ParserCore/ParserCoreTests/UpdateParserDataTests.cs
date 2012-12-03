// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UpdateParserDataTests.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implements the UpdateParserDataTests unit tests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using System.Xml;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
#if WANTTESTPORT // Will we even want the updater stuff? Surely, we won't want it to use FXT.
	[TestFixture]
	public class UpdateParserDataTests : MemoryOnlyBackendProviderTestBase
	{
		private M3ParserModelRetriever m_retriever;
		private XmlDocument m_fxtResult;
		protected SqlConnection m_sqlConnection;
		private string m_sFxtResultFile;

		private string m_sFxtTemplatePath = Path.Combine(DirectoryFinder.FlexFolder,
														 Path.Combine("Configuration",
														 Path.Combine("Grammar", "FXTs"))));

		/// <summary>
		/// Location of test files
		/// </summary>

		private TimeStamp m_tsStartUp;

		public UpdateParserDataTests() : base()
		{

		}

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			BaseVirtualHandler.InstallVirtuals(Path.Combine(FwUtils.ksFlexAppName,
				Path.Combine("Configuration",
				Path.Combine("Grammar", "areaConfiguration.xml"))),
				new string[] { "SIL.FieldWorks.FDO." }, Cache, true);

			Guid appGuid = Guid.NewGuid();
			Cache.MakeDbSyncRecords(appGuid);

			SetUpDataFiles();
			m_tsStartUp = new TimeStamp(m_sqlConnection);
		}

		private void SetUpDataFiles()
		{
			string server = Environment.MachineName + "\\SILFW";
			string database = "TestLangProj";

			string cnxString = "Server=" + server
				+ "; Database=" + database
				+ "; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;";
			m_sqlConnection = new SqlConnection(cnxString);
			m_sqlConnection.Open();
//            SqlCommand command = m_sqlConnection.CreateCommand();
//            command.CommandText = "select top 1 Dst "
//                + "from LangProject_CurVernWss "
//                + "order by Ord";
//            m_vernacularWS = (int)command.ExecuteScalar();

			m_retriever = new M3ParserModelRetriever(SIL.FieldWorks.Common.Utils.BasicUtils.GetTestLangProjDataBaseName());
			using (SIL.FieldWorks.Common.FXT.XDumper fxtDumper = new XDumper(Cache))
			{
				m_sFxtResultFile = Path.Combine(Path.GetTempPath(), "TestLangProjParserFxtResult.xml");
				string ksFXTPath = Path.Combine(FwUtils.ksFlexAppName,
													Path.Combine("Configuration",
													Path.Combine("Grammar", "FXTs")));
				string sFxtFile = Path.Combine(ksFXTPath, "M3Parser.fxt");
				string sFxtPath = Path.Combine(DirectoryFinder.FWCodeDirectory, sFxtFile);
				fxtDumper.Go(Cache.LangProject as CmObject, sFxtPath, File.CreateText(m_sFxtResultFile),
							 new IFilterStrategy[] { new ConstraintFilterStrategy() });
			}


			m_fxtResult = new XmlDocument();
			m_fxtResult.Load(m_sFxtResultFile);
		}

		/// <summary>
		/// Test allomorph changes
		/// </summary>
		[Test]
		public void ParserDataChanges()
		{
			XmlNode node;
#if !ShowDumpResult
			m_fxtResult.Save(Path.Combine(System.IO.Path.GetTempPath(), "TestFxtUpdateBefore.xml"));
#endif
			// -------------
			// Make data changes
			// -------------
			// Make a change to stem allomorph
			ILangProject lp = Cache.LangProject;
			ILexDb lexdb = lp.LexDbOA;
			int[] aiLexEntries = lexdb.EntriesOC.HvoArray;
			int hvoLexEntry = aiLexEntries[0];
			ILexEntry lexEntry = CmObject.CreateFromDBObject(Cache, hvoLexEntry) as ILexEntry;
			Assert.IsNotNull(lexEntry);
			IMoStemAllomorph stemAllomorph = lexEntry.LexemeFormOA as IMoStemAllomorph;
			Assert.IsNotNull(stemAllomorph);
			stemAllomorph.Form.SetAlternative("bili-changed", Cache.DefaultVernWs);
			int hvoStemAllomorph = stemAllomorph.Hvo;
			stemAllomorph.IsAbstract = true;

			// Delete an affix allomorph
			hvoLexEntry = aiLexEntries[3];
			lexEntry = CmObject.CreateFromDBObject(Cache, hvoLexEntry) as ILexEntry;
			Assert.IsNotNull(lexEntry);
			IMoAffixAllomorph affixAllomorph = lexEntry.AlternateFormsOS[1] as IMoAffixAllomorph;
			Assert.IsNotNull(affixAllomorph);
			int hvoAffixAllomorph = affixAllomorph.Hvo;
			lexEntry.AlternateFormsOS.RemoveAt(1);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoLexEntry, (int)LexEntry.LexEntryTags.kflidAlternateForms, 1, 0, 1);

			// Add a new affix allomorph
			IMoAffixAllomorph newAffixAllomorph = new MoAffixAllomorph();
			lexEntry.AlternateFormsOS.Append(newAffixAllomorph);
			newAffixAllomorph.Form.SetAlternative("him-new", Cache.DefaultVernWs);
			int hvoNewAffixAllomorph = newAffixAllomorph.Hvo;
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoLexEntry, (int)LexEntry.LexEntryTags.kflidAlternateForms, lexEntry.AlternateFormsOS.Count - 1, 1, 0);

			// add a compound rule
			IMoMorphData morphData = lp.MorphologicalDataOA;
			IMoEndoCompound compRuleNew = new MoEndoCompound();
			morphData.CompoundRulesOS.Append(compRuleNew);
			string sCompRuleName = "new compound rule";
			compRuleNew.Name.AnalysisDefaultWritingSystem = sCompRuleName;
			compRuleNew.HeadLast = true;
			int hvoPOS = lp.PartsOfSpeechOA.PossibilitiesOS.FirstItem.Hvo;
			compRuleNew.LeftMsaOA.PartOfSpeechRAHvo = hvoPOS;
			compRuleNew.RightMsaOA.PartOfSpeechRAHvo = hvoPOS;
			compRuleNew.OverridingMsaOA.PartOfSpeechRAHvo = hvoPOS;
			// Change compound rule description
			const string ksCompRuleDescription = "new description";
			compRuleNew.Description.AnalysisDefaultWritingSystem.Text = ksCompRuleDescription;
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, morphData.Hvo, (int)MoMorphData.MoMorphDataTags.kflidCompoundRules, morphData.CompoundRulesOS.Count - 1, 1, 0);

			// delete a compound rule
			IMoExoCompound compRuleDeleted = morphData.CompoundRulesOS.FirstItem as IMoExoCompound;
			int hvoCompRuleDeletedLeftMsa = compRuleDeleted.LeftMsaOAHvo;
			int hvoCompRuleDeletedRightMsa = compRuleDeleted.RightMsaOAHvo;
			int hvoCompRuleDeletedToMsa = compRuleDeleted.ToMsaOAHvo;
			morphData.CompoundRulesOS.RemoveAt(0);

			// add an ad hoc co-prohibition
			IMoAlloAdhocProhib alloAdHoc = new MoAlloAdhocProhib();
			morphData.AdhocCoProhibitionsOC.Add(alloAdHoc);
			alloAdHoc.Adjacency = 2;
			alloAdHoc.FirstAllomorphRAHvo = hvoNewAffixAllomorph;
			alloAdHoc.RestOfAllosRS.Append(hvoNewAffixAllomorph);

			// change a "rest of allos" in extant ad hoc co-prohibition
			int[] hvosAdHocProhibs = morphData.AdhocCoProhibitionsOC.HvoArray;
			IMoAlloAdhocProhib alloAdHocOld =
				CmObject.CreateFromDBObject(Cache, hvosAdHocProhibs[9]) as IMoAlloAdhocProhib;
			IMoAffixAllomorph alloAdHicOldFirstRestOfAllos = alloAdHocOld.RestOfAllosRS.FirstItem as IMoAffixAllomorph;
			IMoAffixAllomorph affixAllomorph2 = lexEntry.AlternateFormsOS[0] as IMoAffixAllomorph;
			alloAdHocOld.RestOfAllosRS.Append(affixAllomorph2);
			alloAdHocOld.RestOfAllosRS.RemoveAt(0);
			alloAdHocOld.Adjacency = 2;

			//Add a new productivity restriction
			ICmPossibilityList prodRestricts = morphData.ProdRestrictOA;
			ICmPossibility prodRestriction = new CmPossibility();
			prodRestricts.PossibilitiesOS.Append(prodRestriction);
			string sNewProdRestrictName = "new exception feature";
			prodRestriction.Name.AnalysisDefaultWritingSystem = sNewProdRestrictName;
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, prodRestricts.Hvo, (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, prodRestricts.PossibilitiesOS.Count - 1, 1, 0);

			// Change a phonological enviroment string representation
			IPhPhonData phonData = lp.PhonologicalDataOA;
			IPhEnvironment env = phonData.EnvironmentsOS.FirstItem;
			const string ksEnvStringRep = "/ _ [C] [V] a e i o u";
			env.StringRepresentation.Text = ksEnvStringRep;

			// Add a new phonological enviroment string representation
			IPhEnvironment envNew = new PhEnvironment();
			phonData.EnvironmentsOS.Append(envNew);
			envNew.StringRepresentation.Text = "/ _ m";
			int hvoPhonData = phonData.Hvo;
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoPhonData, (int)PhPhonData.PhPhonDataTags.kflidEnvironments, phonData.EnvironmentsOS.Count - 1, 1, 0);

			// Change parser parameters (to test Unicode string field type)
			string sParserParameters = morphData.ParserParameters.Trim();
			int i = sParserParameters.IndexOf("</ParserParameters>");
			string sNewParserParameters = sParserParameters.Substring(0, i) + "<HermitCrab><stuff>1</stuff></HermitCrab>" + "</ParserParameters>";
			morphData.ParserParameters = sNewParserParameters;

			// Delete a lex entry
			int[] hvosEntries = lexdb.EntriesOC.HvoArray;
			int hvoEntryDeleted = hvosEntries[hvosEntries.Length - 4];
			ILexEntry entryDeleted = CmObject.CreateFromDBObject(Cache, hvoEntryDeleted) as ILexEntry;
			int hvoEntryDeletedLexemeForm = entryDeleted.LexemeFormOAHvo;
			int[] hvosEntryDeletedAlternateForms = entryDeleted.AlternateFormsOS.HvoArray;
			int[] hvosEntryDeletedMSAs = entryDeleted.MorphoSyntaxAnalysesOC.HvoArray;
			int[] hvosEntryDeletedSenses = entryDeleted.SensesOS.HvoArray;
			//entryDeleted.LexemeFormOA.DeleteUnderlyingObject();
			lexdb.EntriesOC.Remove(hvosEntries[hvosEntries.Length - 4]);
			//Cache.PropChanged(null, PropChangeType.kpctNotifyAll, morphData.Hvo, (int)MoMorphData.MoMorphDataTags.kflidParserParameters, 0, 0, 0);

			// Create a new lex entry
			ILexEntry entryNew = new LexEntry();
			lexdb.EntriesOC.Add(entryNew);

			IMoAffixAllomorph alloNew = new MoAffixAllomorph();
			entryNew.LexemeFormOA = alloNew;
			string sNewAlloForm = "dem";
			alloNew.Form.VernacularDefaultWritingSystem = sNewAlloForm;
			alloNew.MorphTypeRA = (IMoMorphType)lexdb.MorphTypesOA.LookupPossibilityByGuid(new Guid(MoMorphType.kguidMorphPrefix));

			IMoAffixAllomorph alloNew2 = new MoAffixAllomorph();
			entryNew.AlternateFormsOS.Append(alloNew2);
			string sNewAlloForm2 = "den";
			alloNew2.Form.VernacularDefaultWritingSystem = sNewAlloForm2;
			alloNew2.MorphTypeRA = (IMoMorphType)lexdb.MorphTypesOA.LookupPossibilityByGuid(new Guid(MoMorphType.kguidMorphPrefix));
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, entryNew.Hvo, (int)LexEntry.LexEntryTags.kflidAlternateForms, entryNew.AlternateFormsOS.Count - 1, 1, 0);

			ILexSense sense = new LexSense();
			entryNew.SensesOS.Append(sense);
			string sGloss = "MeToo";
			sense.Gloss.AnalysisDefaultWritingSystem = sGloss;

			IMoInflAffMsa inflAffixMsa = new MoInflAffMsa();
			entryNew.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			sense.MorphoSyntaxAnalysisRA = inflAffixMsa;
			int[] hvosPOSes = lp.PartsOfSpeechOA.PossibilitiesOS.HvoArray;
			int hvoVerb = hvosPOSes[12];
			inflAffixMsa.PartOfSpeechRAHvo = hvoVerb;
			IPartOfSpeech pos = CmObject.CreateFromDBObject(Cache, hvoVerb) as IPartOfSpeech;
			int hvoSlot = pos.AffixSlotsOC.HvoArray[2];
			inflAffixMsa.SlotsRC.Add(hvoSlot);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, entryNew.Hvo, (int)LexEntry.LexEntryTags.kflidSenses, entryNew.SensesOS.Count - 1, 1, 0);

			// Add an inflectional template
			int[] hvoVerbSubCats = pos.SubPossibilitiesOS.HvoArray;
			int hvoIntransVerb = hvoVerbSubCats[2];
			IPartOfSpeech posVI = CmObject.CreateFromDBObject(Cache, hvoIntransVerb) as IPartOfSpeech;
			IMoInflAffixTemplate affixTemplate = new MoInflAffixTemplate();
			posVI.AffixTemplatesOS.Append(affixTemplate);
			affixTemplate.Name.AnalysisDefaultWritingSystem = "derived verb";
			affixTemplate.Final = false;
			affixTemplate.SuffixSlotsRS.Append(hvoSlot);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, posVI.Hvo, (int)PartOfSpeech.PartOfSpeechTags.kflidAffixTemplates, posVI.AffixTemplatesOS.Count - 1, 1, 0);

			// add a phonological feature
			IFsClosedFeature consFeat = new FsClosedFeature();
			Cache.LangProject.PhFeatureSystemOA.FeaturesOC.Add(consFeat);
			consFeat.Name.AnalysisDefaultWritingSystem = "consonantal";
			consFeat.Abbreviation.AnalysisDefaultWritingSystem = "cons";
			IFsSymFeatVal consPlus = new FsSymFeatVal();
			consFeat.ValuesOC.Add(consPlus);
			consPlus.SimpleInit("+", "positive");
			IFsSymFeatVal consMinus = new FsSymFeatVal();
			consFeat.ValuesOC.Add(consMinus);
			consMinus.SimpleInit("-", "negative");
			IFsFeatStrucType fsType = null;
			if (Cache.LangProject.PhFeatureSystemOA.TypesOC.Count == 0)
			{
				fsType = new FsFeatStrucType();
				Cache.LangProject.PhFeatureSystemOA.TypesOC.Add(fsType);
				fsType.Abbreviation.AnalysisDefaultWritingSystem = "Phon";
			}
			else
			{
				foreach (IFsFeatStrucType type in Cache.LangProject.PhFeatureSystemOA.TypesOC)
				{
					fsType = type;
					break;
				}
			}
			fsType.FeaturesRS.Append(consFeat);

			// add a feature-based NC
			IPhNCFeatures featNC = new PhNCFeatures();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Append(featNC);
			featNC.Name.AnalysisDefaultWritingSystem = "Consonants (Features)";
			featNC.Abbreviation.AnalysisDefaultWritingSystem = "CF";
			IFsFeatStruc fs = new FsFeatStruc();
			featNC.FeaturesOA = fs;
			IFsClosedValue val = fs.FindOrCreateClosedValue(consFeat.Hvo);
			val.FeatureRA = consFeat;
			val.ValueRA = consPlus;
			featNC.NotifyNew();

			// add phonological rule
			IPhRegularRule regRule = new PhRegularRule();
			Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Append(regRule);
			regRule.NotifyNew();
			regRule.Name.AnalysisDefaultWritingSystem = "regular rule";
			IPhSimpleContextSeg segCtxt = new PhSimpleContextSeg();
			regRule.RightHandSidesOS[0].StrucChangeOS.Append(segCtxt);
			IPhPhoneme phoneme = null;
			foreach (IPhPhoneme phon in Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC)
			{
				phoneme = phon;
				break;
			}
			segCtxt.FeatureStructureRA = phoneme;
			segCtxt.NotifyNew();

			IPhSimpleContextNC ncCtxt = new PhSimpleContextNC();
			regRule.RightHandSidesOS[0].LeftContextOA = ncCtxt;
			ncCtxt.FeatureStructureRA = featNC;
			ncCtxt.NotifyNew();

			// add a morphological rule
			IMoAffixProcess affRule = new MoAffixProcess();
			entryNew.AlternateFormsOS.Append(affRule);
			affRule.NotifyNew();
			ncCtxt = new PhSimpleContextNC();
			affRule.InputOS.Append(ncCtxt);
			ncCtxt.FeatureStructureRA = featNC;
			ncCtxt.NotifyNew();
			IMoCopyFromInput copy = new MoCopyFromInput();
			affRule.OutputOS.Append(copy);
			copy.ContentRA = ncCtxt;
			copy.NotifyNew();

			// -----------
			// Update the FXT result
			// -----------
			XmlDocument updatedFxtResult = UpdateFXT();

			// -----------
			// Test the updated results
			// -----------

			// Test changed stem allomorph: checks on MultiUnicode and boolean
			node = updatedFxtResult.SelectSingleNode("//MoStemAllomorph[@Id='" + hvoStemAllomorph + "']");
			Assert.IsNotNull(node);
			Assert.AreEqual(stemAllomorph.Form.VernacularDefaultWritingSystem, node.InnerText, "stem allomorph form change failed");
			XmlNode contentNode = node.SelectSingleNode("@IsAbstract");
			Assert.AreEqual("1", contentNode.InnerText, "stem allomorph is abstract should be true (=1)");

			// Test deleted affix allomorph: checks on owning sequence
			node = updatedFxtResult.SelectSingleNode("//MoAffixAllomorph[@Id='" + hvoAffixAllomorph + "']");
			Assert.IsNull(node, "Deleted affix allomorph should be null");
			node =
				updatedFxtResult.SelectSingleNode("//LexEntry[@id='" + hvoLexEntry + "']/AlternateForms[@dst='" +
												  hvoAffixAllomorph + "']");
			Assert.IsNull(node, "LexEntry should no longer have deleted alternate form");

			// Test added new affix allomorph: checks on owning sequence owned by an item with an @Id; also checks on addition of MoAffixAllomorph via AllAllomorphs
			string sXPath = "//LexEntry[@Id='" + hvoLexEntry + "']/AlternateForms[@dst='" +
							hvoNewAffixAllomorph + "']";
			node = updatedFxtResult.SelectSingleNode(sXPath);
			Assert.IsNotNull(node, "LexEntry should have added alternate form");
			node = updatedFxtResult.SelectSingleNode("//MoAffixAllomorph[@Id='" + hvoNewAffixAllomorph + "']");
			Assert.IsNotNull(node, "Added affix allomorph should be present");
			sXPath = "//LexEntry[@Id='" + hvoLexEntry + "']";
			node = updatedFxtResult.SelectSingleNode(sXPath);
			XmlNodeList nodes = node.SelectNodes("AlternateForms");
			Assert.AreEqual(3, nodes.Count, "Expected three Alternate forms in lex entry.");

			//Test newly added compound rule: checks on owning sequence owned by an Id-less element; also on multistring
			node = updatedFxtResult.SelectSingleNode("//MoEndoCompound[@Id='" + compRuleNew.Hvo + "']");
			Assert.IsNotNull(node, "did not find newly added compound rule");
			contentNode = node.SelectSingleNode("@HeadLast");
			Assert.IsNotNull(contentNode, "missing headlast attribute for coompound rule");
			Assert.AreEqual("1", contentNode.InnerText, "compound rule headlast value differs");
			contentNode = node.SelectSingleNode("Name");
			Assert.IsNotNull(contentNode, "missing Name for compound rule");
			Assert.AreEqual(sCompRuleName, contentNode.InnerText, "compound rule name differs");
			// check on MultiString
			contentNode = node.SelectSingleNode("Description");
			Assert.AreEqual(ksCompRuleDescription, contentNode.InnerText, "compound rule description differs");
			// check on count
			node = updatedFxtResult.SelectSingleNode("//CompoundRules");
			nodes = node.SelectNodes("MoExoCompound | MoEndoCompound");
			Assert.AreEqual(6, nodes.Count, "Expected seven compound rules.");
			// check on owningAtom
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + compRuleNew.LeftMsaOAHvo + "']");
			Assert.IsNotNull(node, "missing real MoStemMsa for LeftMsa of newly added compound rule");
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + compRuleNew.RightMsaOAHvo + "']");
			Assert.IsNotNull(node, "missing real MoStemMsa for RightMsa of newly added compound rule");
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + compRuleNew.OverridingMsaOAHvo + "']");
			Assert.IsNotNull(node, "missing real MoStemMsa for OverridingMsa of newly added compound rule");

			// Test deleted compound rule
			node = updatedFxtResult.SelectSingleNode("//MoExoCompound[@Id='" + compRuleDeleted.Hvo + "']");
			Assert.IsNull(node, "compound rule should be deleted");
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + hvoCompRuleDeletedLeftMsa + "']");
			Assert.IsNull(node, "compound rule left MSA should be deleted");
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + hvoCompRuleDeletedRightMsa + "']");
			Assert.IsNull(node, "compound rule right MSA should be deleted");
			node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + hvoCompRuleDeletedToMsa + "']");
			Assert.IsNull(node, "compound rule to MSA should be deleted");

			//Test newly added allomorph ad hoc rule: checks on owning collection
			node = updatedFxtResult.SelectSingleNode("//MoAlloAdhocProhib[@Id='" + alloAdHoc.Hvo + "']");
			Assert.IsNotNull(node, "did not find newly added allo ad hoc rule");
			contentNode = node.SelectSingleNode("@Adjacency");
			Assert.IsNotNull(contentNode, "missing adjacency attribute for allo ad hoc rule");
			Assert.AreEqual("2",contentNode.InnerText, "allo ad hoc rule adjacency value differs");
			contentNode = node.SelectSingleNode("FirstAllomorph");
			Assert.IsNotNull(contentNode, "missing FirstAllomorph for allo ad hoc rule");
			contentNode = contentNode.SelectSingleNode("@dst");
			Assert.IsNotNull(contentNode, "missing dst attribute of FirstAllomorph for allo ad hoc rule");
			Assert.AreEqual(hvoNewAffixAllomorph.ToString(), contentNode.InnerText, "FirstAllomorph of allo ad hoc rule differs");
			contentNode = node.SelectSingleNode("RestOfAllos");
			Assert.IsNotNull(contentNode, "missing RestOfAllos for allo ad hoc rule");
			contentNode = contentNode.SelectSingleNode("@dst");
			Assert.IsNotNull(contentNode, "missing dst attribute of RestOfAllos for allo ad hoc rule");
			Assert.AreEqual(hvoNewAffixAllomorph.ToString(), contentNode.InnerText, "RestOfAllos of allo ad hoc rule differs");

			// test change of a "rest of allos" in extant ad hoc co-prohibition: check on reference sequence
			node = updatedFxtResult.SelectSingleNode("//MoAlloAdhocProhib[@Id='" + alloAdHocOld.Hvo + "']");
			Assert.IsNotNull(node, "did not find old allo ad hoc rule");
			contentNode = node.SelectSingleNode("RestOfAllos");
			Assert.IsNotNull(contentNode, "missing RestOfAllos for old allo ad hoc rule");
			contentNode = contentNode.SelectSingleNode("@dst");
			Assert.IsNotNull(contentNode, "missing dst attribute of RestOfAllos for old allo ad hoc rule");
			Assert.AreEqual(affixAllomorph2.Hvo.ToString(), contentNode.InnerText, "RestOfAllos of old allo ad hoc rule differs");
			nodes = node.SelectNodes("RestOfAllos");
			Assert.AreEqual(1, nodes.Count, "count of RestOfAllos of old allo ad hoc rule differs");
			// check on integer change
			contentNode = node.SelectSingleNode("@Adjacency");
			Assert.AreEqual("2", contentNode.InnerText, "Adjacency differs");
			node =
				updatedFxtResult.SelectSingleNode("//MoAffixAllomorph[@Id='" + alloAdHicOldFirstRestOfAllos.Hvo + "']");
			Assert.IsNotNull(node, "Original RestOfAllos allomorph should still be present");
			nodes = updatedFxtResult.SelectNodes("//MoAffixAllomorph[@Id='" + affixAllomorph2.Hvo + "']");
			Assert.AreEqual(1, nodes.Count, "Should only be one instance of new allomorph in RestOfAllos");


			// Test added productivity restriction: check on CmPossibilityList
			node = updatedFxtResult.SelectSingleNode("//ProdRestrict/CmPossibility");
			Assert.IsNotNull(node, "Did not find newly added productivity restriction");
			node = node.SelectSingleNode("Name");
			Assert.IsNotNull(node, "Expected Name node in productivity restrictioni");
			Assert.AreEqual(sNewProdRestrictName, node.InnerText, "name of productivity restriction differs");

			// Test phonological environment string representation: check on string
			node = updatedFxtResult.SelectSingleNode("//PhEnvironment[@Id='" + env.Hvo + "']/@StringRepresentation");
			Assert.AreEqual(ksEnvStringRep, node.InnerText, "phonological environment string differs");

			// Test adding a phonological environment string representation:
			// check on case where parent of owner has Id and is class name;
			// also check on case where there is a comment/text node within the result nodes
			node = updatedFxtResult.SelectSingleNode("//PhEnvironment[@Id='" + envNew.Hvo + "']");
			Assert.IsNotNull(node, "missing newly added phonological environment");
			nodes = updatedFxtResult.SelectNodes("//PhEnvironment");
			Assert.AreEqual(11, nodes.Count, "number of PhEnvironments differs");

			// Test Parser Parameters: check on unicode string
			node = updatedFxtResult.SelectSingleNode("//ParserParameters");
			string sResultParseParameters = node.OuterXml.Trim();
			Assert.AreEqual(sNewParserParameters, sResultParseParameters, "Parser Parameters content differs");

			// Test deletion of a lex entry: check on finding LexDb when there is no class LexDb in FXT file
			nodes = updatedFxtResult.SelectNodes("//LexEntry");
			Assert.AreEqual(61, nodes.Count, "number of LexEntries differs");
			node = updatedFxtResult.SelectSingleNode("//LexEntry[@Id='" + hvoEntryDeleted + "']");
			Assert.IsNull(node, "Deleted lex entry should be missing");
			foreach (int hvo in hvosEntryDeletedAlternateForms)
			{
				node = updatedFxtResult.SelectSingleNode("//MoStemAllomorph[@Id='" + hvo + "'] | //MoAffixAllomorph[@Id='" + hvo + "']");
				Assert.IsNull(node, "deleted entry's alternate form should also be gone");
			}
			foreach (int hvo in hvosEntryDeletedMSAs)
			{
				node = updatedFxtResult.SelectSingleNode("//MoStemMsa[@Id='" + hvo + "']");
				Assert.IsNull(node, "deleted entry's msa should also be gone");
			}
			foreach (int hvo in hvosEntryDeletedSenses)
			{
				node = updatedFxtResult.SelectSingleNode("//LexSense[@Id='" + hvo + "']");
				Assert.IsNull(node, "deleted entry's lexsense should also be gone");
			}
			node = updatedFxtResult.SelectSingleNode("//MoStemAllomorph[@Id='" + hvoEntryDeletedLexemeForm + "']");
			Assert.IsNull(node, "deleted entry's lexeme form should also be gone");

			// Test adding new entry
			node = updatedFxtResult.SelectSingleNode("//LexEntry[@Id='" + entryNew.Hvo + "']");
			Assert.IsNotNull(node, "new lex entry is missing");
			contentNode = node.SelectSingleNode("LexemeForm[@dst='" + alloNew.Hvo + "']");
			Assert.IsNotNull(contentNode, "missing lexeme form for new entry");
			contentNode = node.SelectSingleNode("AlternateForms[@dst='" + alloNew2.Hvo + "']");
			Assert.IsNotNull(contentNode, "missing alternate form in new lex entry");
			contentNode = node.SelectSingleNode("Sense[@dst='" + sense.Hvo + "']");
			Assert.IsNotNull(contentNode, "missing sense in new lex entry");
			contentNode = node.SelectSingleNode("MorphoSyntaxAnalysis[@dst='" + inflAffixMsa.Hvo + "']");
			Assert.IsNotNull(contentNode, "missing msa in new lex entry");
			contentNode = node.SelectSingleNode("AlternateForms[@dst='" + affRule.Hvo + "']");
			Assert.IsNotNull(contentNode, "missing affix process rule in new lex entry");

			node = updatedFxtResult.SelectSingleNode("//MoAffixAllomorph[@Id='" + alloNew.Hvo + "']");
			Assert.IsNotNull(node, "new lexeme form affix allomorph for new lex entry is missing");
			contentNode = node.SelectSingleNode("@MorphType");
			Assert.IsNotNull(contentNode, "@MorphType missing for new MoAffixAllomorph in lexeme form of new lex entry");
			IMoMorphType typeNew = MoMorphType.CreateFromDBObject(Cache, Convert.ToInt32(contentNode.InnerText));
			string sGuidNew = typeNew.Guid.ToString();
			Assert.AreEqual(MoMorphType.kguidMorphPrefix, sGuidNew, "morph type wrong for new MoAffixAllomorph in lexeme form of new lex entry");
			contentNode = node.SelectSingleNode("Form");
			Assert.IsNotNull(contentNode, "Form missing for new MoAffixAllomorph in lexeme form new lex entry");
			Assert.AreEqual(sNewAlloForm, contentNode.InnerText, "form wrong for new MoAffixAllomorph in lexeme form of new lex entry");

			node = updatedFxtResult.SelectSingleNode("//MoAffixAllomorph[@Id='" + alloNew2.Hvo + "']");
			Assert.IsNotNull(node, "new alternate form affix allomorph for new lex entry is missing");
			contentNode = node.SelectSingleNode("@MorphType");
			Assert.IsNotNull(contentNode, "@MorphType missing for new MoAffixAllomorph in alternate form of new lex entry");
			typeNew = MoMorphType.CreateFromDBObject(Cache, Convert.ToInt32(contentNode.InnerText));
			sGuidNew = typeNew.Guid.ToString();
			Assert.AreEqual(MoMorphType.kguidMorphPrefix, sGuidNew, "morph type wrong for new MoAffixAllomorph in lexeme form of new lex entry");
			contentNode = node.SelectSingleNode("Form");
			Assert.IsNotNull(contentNode, "Form missing for new MoAffixAllomorph in alternate form new lex entry");
			Assert.AreEqual(sNewAlloForm2, contentNode.InnerText, "form wrong for new MoAffixAllomorph in alternate form of new lex entry");

			node = updatedFxtResult.SelectSingleNode("//LexSense[@Id='" + sense.Hvo + "']");
			Assert.IsNotNull(node, "new sense for new lex entry is missing");
			contentNode = node.SelectSingleNode("Gloss");
			Assert.IsNotNull(contentNode, "Gloss missing for new LexSense in new lex entry");
			Assert.AreEqual(sGloss, contentNode.InnerText, "Gloss wrong for new LexSense in new lex entry");

			node = updatedFxtResult.SelectSingleNode("//MoInflAffMsa[@Id='" + inflAffixMsa.Hvo + "']");
			Assert.IsNotNull(node, "new infl affix msa for new lex entry is missing");
			contentNode = node.SelectSingleNode("@PartOfSpeech");
			Assert.IsNotNull(contentNode, "@PartOfSpeech missing for new MoInflAffMsa in new lex entry");
			Assert.AreEqual(hvoVerb.ToString(), contentNode.InnerText, "part of speech wrong for new MoInflAffMsa in new lex entry");
			contentNode = node.SelectSingleNode("Slots/@dst");
			Assert.IsNotNull(contentNode, "Slots missing for new MoInflAffMsa in new lex entry");
			Assert.AreEqual(hvoSlot.ToString(), contentNode.InnerText, "slot wrong for new MoInflAffMsa in new lex entry");

			// Test adding new template
			node = updatedFxtResult.SelectSingleNode("//MoInflAffixTemplate[@Id='" + affixTemplate.Hvo + "']");
			Assert.IsNotNull(node, "new affix template missing");
			node =
				updatedFxtResult.SelectSingleNode("//PartOfSpeech[@Id='" + hvoIntransVerb +
												  "']/AffixTemplates/MoInflAffixTemplate[@Id='" + affixTemplate.Hvo +
												  "']");
			Assert.IsNotNull(node, "new affix template is in intransitive verb");

			// Test adding new phonological feature
			node = updatedFxtResult.SelectSingleNode("//PhFeatureSystem/Features/FsClosedFeature[@Id='" + consFeat.Hvo + "']");
			Assert.IsNotNull(node, "new phonological feature is missing");
			contentNode = node.SelectSingleNode("Abbreviation");
			Assert.IsNotNull(contentNode, "Abbreviation missing from new phonological feature");
			Assert.AreEqual(contentNode.InnerText, consFeat.Abbreviation.AnalysisDefaultWritingSystem, "Abbreviation wrong for new phonological feature");
			nodes = node.SelectNodes("Values/FsSymFeatVal");
			Assert.IsNotNull(nodes, "values missing from new phonological feature");
			Assert.AreEqual(nodes.Count, 2, "incorrect number of values in new phonological feature");
			node = updatedFxtResult.SelectSingleNode("//PhFeatureSystem/Types/FsFeatStrucType/Features/Feature[@dst='" + consFeat.Hvo + "']");
			Assert.IsNotNull(node, "reference to new phonological feature is missing from phonological feature system");

			// Test adding new feature-based NC
			node = updatedFxtResult.SelectSingleNode("//PhNCFeatures[@Id='" + featNC.Hvo + "']");
			Assert.IsNotNull(node, "new feature-based NC is missing");
			contentNode = node.SelectSingleNode("Abbreviation");
			Assert.IsNotNull(contentNode, "Abbreviation missing from new feature-based NC");
			Assert.AreEqual(contentNode.InnerText, featNC.Abbreviation.AnalysisDefaultWritingSystem, "Abbreviation wrong for new feature-based NC");
			contentNode = node.SelectSingleNode("FsFeatStruc/FsClosedValue[@Id='" + val.Hvo + "']");
			Assert.IsNotNull(contentNode, "value missing from new feature-based NC");
			Assert.AreEqual((contentNode as XmlElement).GetAttribute("Feature"), consFeat.Hvo.ToString(), "closed value feature is wrong in new feature-based NC");
			Assert.AreEqual((contentNode as XmlElement).GetAttribute("Value"), consPlus.Hvo.ToString(), "closed value is wrong in new feature-based NC");

			// Test adding new phonological rule
			node = updatedFxtResult.SelectSingleNode("//PhRegularRule[@Id='" + regRule.Hvo + "']");
			Assert.IsNotNull(node, "new phonological rule is missing");
			nodes = node.SelectNodes("StrucDesc/*");
			Assert.AreEqual(nodes.Count, 0);
			contentNode = node.SelectSingleNode("RightHandSides/PhSegRuleRHS/StrucChange/PhSimpleContextSeg[@dst='" + phoneme.Hvo + "']");
			Assert.IsNotNull(contentNode, "phoneme simple context missing in new phonological rule");
			contentNode = node.SelectSingleNode("RightHandSides/PhSegRuleRHS/LeftContext/PhSimpleContextNC[@dst='" + featNC.Hvo + "']");
			Assert.IsNotNull(contentNode, "NC simple context missing in new phonological rule");

			// Test adding new morphological rule
			node = updatedFxtResult.SelectSingleNode("//Lexicon/Allomorphs/MoAffixProcess[@Id='" + affRule.Hvo + "']");
			Assert.IsNotNull(node, "new morphological rule is missing");
			contentNode = node.SelectSingleNode("Input/PhSimpleContextNC[@dst='" + featNC.Hvo + "']");
			Assert.IsNotNull(contentNode, "NC simple context missing in new morphological rule");
			contentNode = node.SelectSingleNode("Output/MoCopyFromInput/Content[@dst='" + ncCtxt.Hvo + "']");
			Assert.IsNotNull(contentNode, "copy from input missing in new morphological rule");

			// Modify a phonological rule
			segCtxt = new PhSimpleContextSeg();
			regRule.StrucDescOS.Append(segCtxt);
			segCtxt.FeatureStructureRA = phoneme;
			segCtxt.NotifyNew();
			regRule.RightHandSidesOS[0].StrucChangeOS[0].DeleteUnderlyingObject();
			IPhPhonContext oldCtxt = regRule.RightHandSidesOS[0].LeftContextOA;
			Cache.LangProject.PhonologicalDataOA.ContextsOS.Append(oldCtxt);
			IPhSequenceContext seqCtxt = new PhSequenceContext();
			regRule.RightHandSidesOS[0].LeftContextOA = seqCtxt;
			seqCtxt.MembersRS.Append(oldCtxt);
			seqCtxt.NotifyNew();
			IPhSimpleContextBdry bdryCtxt = new PhSimpleContextBdry();
			Cache.LangProject.PhonologicalDataOA.ContextsOS.Append(bdryCtxt);
			bdryCtxt.FeatureStructureRAHvo = Cache.GetIdFromGuid(LangProject.kguidPhRuleWordBdry);
			bdryCtxt.NotifyNew();
			seqCtxt.MembersRS.Append(bdryCtxt);

			// Modify a morphological rule
			entryNew.LexemeFormOA = affRule;
			IMoInsertPhones insertPhones = new MoInsertPhones();
			affRule.OutputOS.InsertAt(insertPhones, 0);
			insertPhones.ContentRS.Append(phoneme);
			insertPhones.NotifyNew();
			affRule.InputOS[1].DeleteUnderlyingObject();

			// change order of a sequence vector
			lexEntry.AlternateFormsOS.InsertAt(newAffixAllomorph, 0);

			updatedFxtResult = UpdateFXT();

			// Test modifying a phonological rule
			node = updatedFxtResult.SelectSingleNode("//PhRegularRule[@Id='" + regRule.Hvo + "']");
			contentNode = node.SelectSingleNode("StrucDesc/PhSimpleContextSeg[@dst='" + phoneme.Hvo + "']");
			Assert.IsNotNull(contentNode, "phoneme simple context missing from StrucDesc in modified phonological rule");
			contentNode = node.SelectSingleNode("RightHandSides/PhSegRuleRHS/StrucChange/PhSimpleContextSeg[@dst='" + phoneme.Hvo + "']");
			Assert.IsNull(contentNode, "phoneme simple context is not missing from StrucChange in modified phonological rule");
			contentNode = node.SelectSingleNode("RightHandSides/PhSegRuleRHS/LeftContext/PhSequenceContext[@Id='" + seqCtxt.Hvo + "']");
			Assert.IsNotNull(contentNode, "sequence context missing from modified phonological rule");
			contentNode = contentNode.SelectSingleNode("Members[@dst='" + bdryCtxt.Hvo + "']");
			Assert.IsNotNull(contentNode, "boundary context missing from sequence context in modified phonological rule");
			node = updatedFxtResult.SelectSingleNode("//PhPhonData/Contexts/PhSimpleContextBdry[@Id='" + bdryCtxt.Hvo + "']");
			Assert.IsNotNull(node, "boundary context missing from contexts in phonological data");

			// Test modifying a morphological rule
			node = updatedFxtResult.SelectSingleNode("//LexEntry[@Id='" + entryNew.Hvo + "']");
			contentNode = node.SelectSingleNode("LexemeForm[@dst='" + affRule.Hvo + "']");
			Assert.IsNotNull(contentNode, "affix process rule is not the lexeme form for the lex entry");
			node = updatedFxtResult.SelectSingleNode("//Lexicon/Allomorphs/MoAffixProcess[@Id='" + affRule.Hvo + "']");
			contentNode = node.SelectSingleNode("Input/PhSimpleContextNC[@dst='" + featNC.Hvo + "']");
			Assert.IsNull(contentNode, "NC simple context was not removed from morphological rule");
			nodes = node.SelectNodes("Output/*");
			Assert.AreEqual(nodes.Count, 2, "incorrect number of mappings in morphological rule");
			contentNode = node.SelectSingleNode("Output/*[position() = 1 and @Id='" + insertPhones.Hvo + "']");
			Assert.IsNotNull(contentNode, "insert phones missing from morphological rule");

			// Test changing order of a sequence vector
			node = updatedFxtResult.SelectSingleNode("//LexEntry[@Id='" + lexEntry.Hvo + "']");
			contentNode = node.SelectSingleNode("AlternateForms[@dst='" + lexEntry.AlternateFormsOS[0].Hvo + "']/@ord");
			Assert.AreEqual("0", contentNode.InnerText);
			contentNode = node.SelectSingleNode("AlternateForms[@dst='" + lexEntry.AlternateFormsOS[1].Hvo + "']/@ord");
			Assert.AreEqual("1", contentNode.InnerText);
			contentNode = node.SelectSingleNode("AlternateForms[@dst='" + lexEntry.AlternateFormsOS[2].Hvo + "']/@ord");
			Assert.AreEqual("2", contentNode.InnerText);
		}

		private void StoreChangedDataItems()
		{
			SqlCommand newcmd = m_sqlConnection.CreateCommand();
			newcmd.CommandType = System.Data.CommandType.Text;
			newcmd.CommandText = String.Format(ParserScheduler.m_ksChangedParserDataQuery, m_tsStartUp.Hex);
			using (SqlDataReader sqlreader = newcmd.ExecuteReader())
			{
				m_retriever.StoreChangedDataItems(sqlreader);
			}
		}

		private XmlDocument UpdateFXT()
		{
			StoreChangedDataItems();
			m_tsStartUp = new TimeStamp(m_sqlConnection);
			m_retriever.DoUpdate(Cache, Path.Combine(m_sFxtTemplatePath, "M3Parser.fxt"), ref m_fxtResult);
			// XmlDocument updatedFxtResult = m_retriever.ModelDom;
			XmlDocument updatedFxtResult = m_fxtResult;
#if !ShowDumpResult
			updatedFxtResult.PreserveWhitespace = false;
			updatedFxtResult.Save(Path.Combine(System.IO.Path.GetTempPath(), "TestFxtUpdateResult.xml"));
#endif
			return updatedFxtResult;
		}
	}
#endif
}
