// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LexTextControlsTests
{
	/// <summary>
	/// Start of tests for MasterCategory. Very incomplete as yet.
	/// </summary>
	[TestFixture]
	public class MasterCategoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string WSEn = "en";
		private const string WSFr = "fr";
		private static readonly HashSet<IPartOfSpeech> POSEmptySet = new HashSet<IPartOfSpeech>();
		private static int[] s_wssOnlyEn;

		[OneTimeSetUp]
		public void TestFixtureSetup()
		{
			base.FixtureSetup();

			s_wssOnlyEn = new[] { Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(WSEn) };
		}

		[Test]
		public void MasterCategoryWithGuidNode_MakesPosWithRightGuid()
		{
			string input =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
   <item type='category' id='Adjective' guid='30d07580-5052-4d91-bc24-469b8b2d7df9'>
	  <abbrev ws='en'>adj</abbrev>
	  <term ws='en'>Adjective</term>
	  <def ws='en'>An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
   </item>
   <item type='category' id='Adposition' guid='ae115ea8-2cd7-4501-8ae7-dc638e4f17c5'>
	  <abbrev ws='en'>adp</abbrev>
	  <term ws='en'>Adposition</term>
	  <def ws='en'>An adposition is a part of speech whose members are of a closed set and occur before or after a complement composed of a noun phrase, noun, pronoun, or clause that functions as a noun phrase and forms a single structure with the complement to express its grammatical and semantic relation to another unit within a clause.</def>
	  <item type='category' id='Postposition' guid='18f1b2b8-0ce3-4889-90e9-003fed6a969f'>
		 <abbrev ws='en'>post</abbrev>
		 <term ws='en'>Postposition</term>
		 <def ws='en'>A postposition is an adposition that occurs after its complement.</def>
		  <item type='category' id='PPchild' guid='82B1250A-E64F-4AD8-8B8C-5ABBC732087A'>
			 <abbrev ws='en'>ppc</abbrev>
			 <term ws='en'>PPchild</term>
			 <def ws='en'>An imaginary POS to test another code path.</def>
		  </item>
	  </item>
	</item>
</eticPOSList>";
			m_actionHandler.EndUndoTask(); // AddToDatabase makes its own

			var posList = Cache.LangProject.PartsOfSpeechOA;
			Assert.That(posList, Is.Not.Null, "Test requires default init of cache to create POS list");
			CheckPosDoesNotExist("ae115ea8-2cd7-4501-8ae7-dc638e4f17c5");
			CheckPosDoesNotExist("18f1b2b8-0ce3-4889-90e9-003fed6a969f");
			CheckPosDoesNotExist("82B1250A-E64F-4AD8-8B8C-5ABBC732087A");

			var doc = new XmlDocument();
			doc.LoadXml(input);
			var rootItem = doc.DocumentElement.ChildNodes[1];

			var mc = MasterCategory.Create(POSEmptySet, rootItem, Cache);
			mc.AddToDatabase(Cache, posList, null, null);
			var adposition = CheckPos("ae115ea8-2cd7-4501-8ae7-dc638e4f17c5", posList);

			var childItem = rootItem.ChildNodes[3];
			var mcChild = MasterCategory.Create(new HashSet<IPartOfSpeech> {adposition}, childItem, Cache);
			mcChild.AddToDatabase(Cache, posList, null, adposition);
			var postPosition = CheckPos("18f1b2b8-0ce3-4889-90e9-003fed6a969f", adposition);

			var grandChildItem = childItem.ChildNodes[3];
			var mcGrandChild = MasterCategory.Create(new HashSet<IPartOfSpeech> {adposition, postPosition}, grandChildItem, Cache);
			mcGrandChild.AddToDatabase(Cache, posList, mcChild, null);
			CheckPos("82B1250A-E64F-4AD8-8B8C-5ABBC732087A", postPosition);
		}

		[Test]
		public void MasterCategoryWithGuidNode_ValidatePosInReversalGuid()
		{
			string input =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
				   <item type='category' id='Adjective' guid='30d07580-5052-4d91-bc24-469b8b2d7df9'>
					  <abbrev ws='en'>adj</abbrev>
					  <term ws='en'>Adjective</term>
					  <def ws='en'>An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
				   </item>
				   <item type='category' id='Adposition' guid='ae115ea8-2cd7-4501-8ae7-dc638e4f17c5'>
					  <abbrev ws='en'>adp</abbrev>
					  <term ws='en'>Adposition</term>
					  <def ws='en'>An adposition is a part of speech whose members are of a closed set and occur before or after a complement composed of a noun phrase, noun, pronoun, or clause that functions as a noun phrase and forms a single structure with the complement to express its grammatical and semantic relation to another unit within a clause.</def>
					  <item type='category' id='Postposition' guid='18f1b2b8-0ce3-4889-90e9-003fed6a969f'>
						 <abbrev ws='en'>post</abbrev>
						 <term ws='en'>Postposition</term>
						 <def ws='en'>A postposition is an adposition that occurs after its complement.</def>
						  <item type='category' id='PPchild' guid='82B1250A-E64F-4AD8-8B8C-5ABBC732087A'>
							 <abbrev ws='en'>ppc</abbrev>
							 <term ws='en'>PPchild</term>
							 <def ws='en'>An imaginary POS to test another code path.</def>
						  </item>
					  </item>
					</item>
				</eticPOSList>";

			var mRevIndexFactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			var index = mRevIndexFactory.Create();
			Cache.LangProject.LexDbOA.ReversalIndexesOC.Add(index);
			ICmPossibilityListFactory fact = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			index.PartsOfSpeechOA = fact.Create();
			var posList = index.PartsOfSpeechOA;
			var doc = new XmlDocument();
			doc.LoadXml(input);
			var rootItem = doc.DocumentElement.ChildNodes[1];
			var mc = MasterCategory.Create(POSEmptySet, rootItem, Cache);
			m_actionHandler.EndUndoTask();
			Assert.That(posList, Is.Not.Null, "Test requires default init of cache to create POS list");
			mc.AddToDatabase(Cache, posList, null, null);

			var childItem = rootItem.ChildNodes[3];
			var firstPos = (IPartOfSpeech) posList.PossibilitiesOS[0];
			var mcChild = MasterCategory.Create(new HashSet<IPartOfSpeech> { firstPos }, childItem, Cache);
			mcChild.AddToDatabase(Cache, posList, null, firstPos);

			Assert.That(firstPos.Guid, Is.Not.Null, "Item in the category should not be null Guid");
			Assert.That(firstPos.SubPossibilitiesOS[0].Guid, Is.Not.Null, "Sub-Item in the category should not be null Guid");
			Assert.IsFalse(firstPos.SubPossibilitiesOS[0].Guid == Guid.Empty, "Sub-Item in the category should not be Empty Guid");
		}

		[Test]
		public void GetBestWritingSystemForNamedNode_FallsThrough()
		{
			const string inputTemplate =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
   <item type='category' id='Adjective' guid='30d07580-5052-4d91-bc24-469b8b2d7df9'>
	  <abbrev ws='en'/> <!-- self-closing -->
	  <term ws='en'></term> <!-- empty -->
	  <def ws='en'>{0}</def> <!-- populated -->
	  <abbrev ws='fr'>{1}</abbrev>
	  <term ws='fr'>{2}</term>
	  <def ws='fr'>{3}</def>
   </item>
</eticPOSList>";
			const string defEn = "An adjective modifies a noun.";
			const string abbrFr = "adj";
			const string nameFr = "Adjectif";
			const string defFr = "Un adjectif est un modificateur du nom.";

			Cache.ServiceLocator.WritingSystemManager.GetOrSet(WSFr, out var wsDefFr);
			// NOT CurrentAWS. We need to be able fall through to English, even if the user has hidden it (LT-19115)
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsDefFr);
			// Commit WS changes
			m_actionHandler.EndUndoTask();

			var doc = new XmlDocument();
			doc.LoadXml(string.Format(inputTemplate, defEn, abbrFr, nameFr, defFr));
			var posNode = doc.DocumentElement.ChildNodes[0];

			// SUT
			var wsAbbrev = MasterCategory.GetBestWritingSystemForNamedNode(posNode, "abbrev", WSEn, Cache, out var outAbbrev);
			var wsTerm = MasterCategory.GetBestWritingSystemForNamedNode(posNode, "term", WSEn, Cache, out var outTerm);
			var wsDef = MasterCategory.GetBestWritingSystemForNamedNode(posNode, "def", WSEn, Cache, out var outDef);

			Assert.AreEqual(wsAbbrev, WSFr, "self-closing should fall through");
			Assert.AreEqual(abbrFr, outAbbrev);
			Assert.AreEqual(wsTerm, WSFr, "empty should fall through");
			Assert.AreEqual(nameFr, outTerm);
			Assert.AreEqual(wsDef, WSEn, "populated should be taken");
			Assert.AreEqual(defEn, outDef);
		}

		[Test]
		public void UpdatePOSStrings_UpdatesAllAnaWSs()
		{
			const string inputTemplate =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
   <item type='category' id='Adjective' guid='{0}'>
	  <abbrev ws='en'>adj</abbrev>
	  <term ws='en'>Adjective</term>
	  <def ws='en'>An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
	  <abbrev ws='fr'>{1}</abbrev>
	  <term ws='fr'>{2}</term>
	  <def ws='fr'>{3}</def>
   </item>
</eticPOSList>";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrFr = "adj";
			const string nameFr = "Adjectif";
			const string defFr = "Un adjectif est un modificateur du nom.";

			Cache.ServiceLocator.WritingSystemManager.GetOrSet(WSFr, out var wsDefFr);
			// NOT CurrentAWS. We need to be able fall through to English, even if the user has hidden it
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsDefFr);
			var wsIdFr = wsDefFr.Handle;
			// Commit WS changes; AddToDatabase makes its own UndoTask
			m_actionHandler.EndUndoTask();

			var posList = Cache.LangProject.PartsOfSpeechOA;
			Assert.That(posList, Is.Not.Null, "Test requires default init of cache to create POS list");


			var doc = new XmlDocument();
			doc.LoadXml(string.Format(inputTemplate, guid, string.Empty, string.Empty, string.Empty));
			var posNode = doc.DocumentElement.ChildNodes[0];

			var mcChild = MasterCategory.Create(POSEmptySet, posNode, Cache);
			mcChild.AddToDatabase(Cache, posList, null, null);

			// Verify the category has been added without French text (French will be added by SUT)
			var prePOS = CheckPos(guid, posList);
			CollectionAssert.AreEquivalent(s_wssOnlyEn, prePOS.Abbreviation.AvailableWritingSystemIds, "Abbrev should have only English");
			CollectionAssert.AreEquivalent(s_wssOnlyEn, prePOS.Name.AvailableWritingSystemIds, "Name should have only English");
			CollectionAssert.AreEquivalent(s_wssOnlyEn, prePOS.Description.AvailableWritingSystemIds, "Def should have only English");

			doc.LoadXml(string.Format(inputTemplate, guid, abbrFr, nameFr, defFr));
			posNode = doc.DocumentElement.ChildNodes[0];

			// SUT
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
				MasterCategory.UpdatePOSStrings(Cache, posNode, prePOS));

			var pos = CheckPos(guid, posList);
			Assert.AreEqual(abbrFr, pos.Abbreviation.GetAlternativeOrBestTss(wsIdFr, out var wsActual).Text);
			Assert.AreEqual(wsIdFr, wsActual, "Abbrev WS");
			Assert.AreEqual(nameFr, pos.Name.GetAlternativeOrBestTss(wsIdFr, out wsActual).Text);
			Assert.AreEqual(wsIdFr, wsActual, "Name WS");
			Assert.AreEqual(defFr, pos.Description.GetAlternativeOrBestTss(wsIdFr, out wsActual).Text);
			Assert.AreEqual(wsIdFr, wsActual, "Def WS");
		}

		[Test]
		public void ImportTranslatedPOSContent()
		{
			const string inputTemplate =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
   <item type='category' id='Adjective' guid='{0}'>
	  <abbrev ws='en'>adj</abbrev>
	  <term ws='en'>Adjective</term>
	  <def ws='en'>An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
	  <abbrev ws='fr'>{1}</abbrev>
	  <term ws='fr'>{2}</term>
	  <def ws='fr'>{3}</def>
   </item>
   <item type='category' id='Adposition' guid='{4}'>
	  <abbrev ws='en'>adp</abbrev>
	  <abbrev ws='fr'>{5}</abbrev>
	  <term ws='en'>Adposition</term>
	  <term ws='fr'>{6}</term>
	  <def ws='en'>An adposition is a part of speech whose members are of a closed set.</def>
	  <def ws='fr'>{7}</def>
	  <item type='category' id='Preposition' guid='{8}'>
		 <abbrev ws='en'>prep</abbrev>
		 <abbrev ws='fr'>{9}</abbrev>
		 <term ws='en'>Preposition</term>
		 <term ws='fr'>{10}</term>
		 <def ws='en'>A preposition is an adposition that occurs before its complement.</def>
		 <def ws='fr'>{11}</def>
	  </item>
	  <item type='category' id='Postposition' guid='{12}'>
		 <abbrev ws='en'>post</abbrev>
		 <abbrev ws='fr'>post</abbrev>
		 <term ws='en'>Postposition</term>
		 <term ws='fr'>Postposition</term>
		 <def ws='en'>A postposition is an adposition that occurs after its complement.</def>
		 <def ws='fr'>Un postposition est un adposition.</def>
	  </item>
   </item>
</eticPOSList>";
			const string ajGuid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string ajAbbrFr = "adjec";
			const string ajNameFr = "Adjectif";
			const string ajDffnFr = "Un adjectif est un modificateur du nom.";
			const string adGuid = "ae115ea8-2cd7-4501-8ae7-dc638e4f17c5";
			const string adAbbrFr = "adpos";
			const string adNameFr = "Adposition (fr)";
			const string adDffnFr = "Un adposition est un partie du discours.";
			const string prepGuid = "923e5aed-d84a-48b0-9b7a-331c1336864a";
			const string prepAbbrFr = "prép";
			const string prepNameFr = "Préposition";
			const string prepDffnFr = "Un préposition est un adposition.";
			const string postGuid = "18f1b2b8-0ce3-4889-90e9-003fed6a969f";
			var inputOnlyEn = string.Format(inputTemplate, ajGuid, string.Empty, string.Empty, string.Empty,
				adGuid, string.Empty, string.Empty, string.Empty,
				prepGuid, string.Empty, string.Empty, string.Empty,
				postGuid);
			var inputEnAndFr = string.Format(inputTemplate, ajGuid, ajAbbrFr, ajNameFr, ajDffnFr,
				adGuid, adAbbrFr, adNameFr, adDffnFr,
				prepGuid, prepAbbrFr, prepNameFr, prepDffnFr,
				postGuid);
			const string customPosName = "should not crash";
			const string customPosAbbr = "shouldn't crash";
			const string customPosDffn = "SUT shouldn't crash if custom Parts of Speech are present, nor should they be updated.";

			Cache.ServiceLocator.WritingSystemManager.GetOrSet(WSFr, out var wsDefFr);
			// NOT CurrentAWS. We need to be able fall through to English, even if the user has hidden it (LT-19115)
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsDefFr);
			var wsIdFr = wsDefFr.Handle;
			// Commit WS changes; AddToDatabase makes its own UndoTask
			m_actionHandler.EndUndoTask();

			var posList = Cache.LangProject.PartsOfSpeechOA;
			Assert.That(posList, Is.Not.Null, "Test requires default init of cache to create POS list");

			CheckPosDoesNotExist(postGuid);

			// Create cats without fr
			var doc = new XmlDocument();
			doc.LoadXml(inputOnlyEn);
			var topLevelNodes = doc.DocumentElement?.ChildNodes;
			Assert.NotNull(topLevelNodes, "keep ReSharper happy");
			var ajNode = topLevelNodes[0];
			var adNode = topLevelNodes[1];
			var prepNode = adNode.LastChild.PreviousSibling;

			MasterCategory.Create(POSEmptySet, ajNode, Cache).AddToDatabase(Cache, posList, null, null);
			CheckPosHasOnlyEnglish(CheckPos(ajGuid, posList));

			var adMCat = MasterCategory.Create(POSEmptySet, adNode, Cache);
			adMCat.AddToDatabase(Cache, posList, null, null);
			var adPos = CheckPos(adGuid, posList);
			CheckPosHasOnlyEnglish(adPos);

			MasterCategory.Create(POSEmptySet, prepNode, Cache).AddToDatabase(Cache, posList, adMCat, adPos);
			CheckPosHasOnlyEnglish(CheckPos(prepGuid, adPos));

			var customPosGuid = CreateCustomPos(customPosName, customPosAbbr, customPosDffn, wsIdFr, posList).Guid.ToString();

			CheckPosDoesNotExist(postGuid);

			doc.LoadXml(inputEnAndFr);

			// SUT
			MasterCategory.UpdatePOSStrings(Cache, doc);

			var ajPos = CheckPos(ajGuid, posList);
			adPos = CheckPos(adGuid, posList);
			var prepPos = CheckPos(prepGuid, adPos);
			var customPos = CheckPos(customPosGuid, posList);
			CheckPosDoesNotExist(postGuid);

			CheckMSA(ajAbbrFr, wsIdFr, ajPos.Abbreviation);
			CheckMSA(ajNameFr, wsIdFr, ajPos.Name);
			CheckMSA(ajDffnFr, wsIdFr, ajPos.Description);
			CheckMSA(adAbbrFr, wsIdFr, adPos.Abbreviation);
			CheckMSA(adNameFr, wsIdFr, adPos.Name);
			CheckMSA(adDffnFr, wsIdFr, adPos.Description);
			CheckMSA(prepAbbrFr, wsIdFr, prepPos.Abbreviation);
			CheckMSA(prepNameFr, wsIdFr, prepPos.Name);
			CheckMSA(prepDffnFr, wsIdFr, prepPos.Description);
			CheckMSA(customPosAbbr, wsIdFr, customPos.Abbreviation);
			CheckMSA(customPosName, wsIdFr, customPos.Name);
			CheckMSA(customPosDffn, wsIdFr, customPos.Description);
		}

		private IPartOfSpeech CheckPos(string guid, ICmObject owner)
		{
			Assert.True(Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(new Guid(guid), out var pos),
				"expected POS should be created with the right guid");
			Assert.That(pos.Owner, Is.EqualTo(owner), "POS should be created at the right place in the hierarchy");
			return pos;
		}

		private void CheckPosDoesNotExist(string id)
		{
			Assert.False(Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(new Guid(id), out _),
				"default possibility list should not already contain objects that this test creates");
		}

		private IPartOfSpeech CreateCustomPos(string name, string abbrev, string definition, int ws, ICmPossibilityList owner)
		{
			var guid = Guid.NewGuid();
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateCategory, LexTextControls.ksRedoCreateCategory,
				Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create(guid, owner);
					pos.Name.set_String(ws, TsStringUtils.MakeString(name, ws));
					pos.Abbreviation.set_String(ws, TsStringUtils.MakeString(abbrev, ws));
					pos.Description.set_String(ws, TsStringUtils.MakeString(definition, ws));
				});
			return CheckPos(guid.ToString(), owner);
		}

		private static void CheckPosHasOnlyEnglish(IPartOfSpeech pos)
		{
			CollectionAssert.AreEquivalent(s_wssOnlyEn, pos.Abbreviation.AvailableWritingSystemIds,
				$"Abbrev {pos.Abbreviation.BestAnalysisAlternative} should have only English");
			CollectionAssert.AreEquivalent(s_wssOnlyEn, pos.Name.AvailableWritingSystemIds,
				$"Name {pos.Name.BestAnalysisAlternative} should have only English");
			CollectionAssert.AreEquivalent(s_wssOnlyEn, pos.Description.AvailableWritingSystemIds,
				$"Def of {pos.Name.BestAnalysisAlternative} should have only English");
		}

		private static void CheckMSA(string expectedText, int expectedWs, IMultiStringAccessor actual)
		{
			var actualText = TsStringUtils.NormalizeToNFC(actual.GetAlternativeOrBestTss(expectedWs, out var actualWs).Text);
			Assert.AreEqual(expectedText, actualText, $"WS Handle\n{expectedWs} requested\n{actualWs} returned");
			Assert.AreEqual(expectedWs, actualWs, expectedText);
		}
	}
}
