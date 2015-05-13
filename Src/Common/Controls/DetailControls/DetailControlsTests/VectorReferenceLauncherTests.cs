// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: VectorReferenceLauncherTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class VectorReferenceLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member variables

		private ILexEntryFactory m_leFact;
		private ILexEntryRefFactory m_lerFact;
		//private IMoStemAllomorphFactory m_moFact;
		private string m_wsAnalStr;
		private int m_wsAnalysis;
		private int m_wsVern;
		private IPartOfSpeech m_noun;
		private IMoMorphType m_stem;
		public MockVectorReferenceLauncher MockLauncher { get; set; }

		#endregion

		protected override void CreateTestData()
		{
			base.CreateTestData();
			var servLoc = Cache.ServiceLocator;
			m_leFact = servLoc.GetInstance<ILexEntryFactory>();
			m_lerFact = servLoc.GetInstance<ILexEntryRefFactory>();
			//m_moFact = servLoc.GetInstance<IMoStemAllomorphFactory>();
			MockLauncher = new MockVectorReferenceLauncher();
			m_wsAnalysis = Cache.DefaultAnalWs;
			m_wsVern = Cache.DefaultVernWs;
			m_wsAnalStr = Cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(Cache.DefaultAnalWs);
			m_noun = GetNounPOS();
			m_stem = GetStemMorphType();
		}

		public override void TestTearDown()
		{
			MockLauncher.Dispose();
			base.TestTearDown();
		}

		private IPartOfSpeech GetNounPOS()
		{
			return Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Where(
				pos => pos.Name.AnalysisDefaultWritingSystem.Text == "noun").Cast<IPartOfSpeech>().FirstOrDefault();
		}

		private IMoMorphType GetStemMorphType()
		{
			return Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Where(
				mt => mt.Name.AnalysisDefaultWritingSystem.Text == "stem").Cast<IMoMorphType>().FirstOrDefault();
		}

		private ILexEntry CreateSimpleEntry(string form, string gloss)
		{
			var lexAlt = TsStringUtils.MakeTss(form, m_wsVern);
			var glossAlt = TsStringUtils.MakeTss(gloss, m_wsAnalysis);
			var msa = new SandboxGenericMSA { MainPOS = m_noun, MsaType = MsaType.kStem };
			var leComp = new LexEntryComponents { MSA = msa, MorphType = m_stem };
			leComp.GlossAlternatives.Add(glossAlt);
			leComp.LexemeFormAlternatives.Add(lexAlt);
			var entry = m_leFact.Create(leComp);
			return entry;
		}

		private ILexEntryRef AddComponentEntryRef(ILexEntry mainEntry, ILexEntry secondaryEntry)
		{
			Assert.IsNotNull(secondaryEntry.EntryRefsOS,
							 "Entry is not set up correctly.");
			if (secondaryEntry.EntryRefsOS.Count > 0)
			{
				var existingLer = secondaryEntry.EntryRefsOS[0];
				if (mainEntry != null)
					existingLer.ComponentLexemesRS.Add(mainEntry);
				return existingLer;
			}
			var newLer = m_lerFact.Create();
			secondaryEntry.EntryRefsOS.Add(newLer);
			if (mainEntry != null)
				newLer.ComponentLexemesRS.Add(mainEntry);
			return newLer;
		}

		private ILexEntryRef AddPrimaryEntryRef(ILexEntry mainEntry, ILexEntry secondaryEntry)
		{
			Assert.IsNotNull(secondaryEntry.EntryRefsOS,
							 "Entry is not set up correctly.");
			if (secondaryEntry.EntryRefsOS.Count > 0)
			{
				var existingLer = secondaryEntry.EntryRefsOS[0];
				existingLer.PrimaryLexemesRS.Add(mainEntry);
				return existingLer;
			}
			var newLer = m_lerFact.Create();
			secondaryEntry.EntryRefsOS.Add(newLer);
			newLer.PrimaryLexemesRS.Add(mainEntry);
			return newLer;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a new Target item to the reference vector in the case where a list exists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddNewTargetToExistingList()
		{
			// Setup test
			var mainEntry = CreateSimpleEntry("form", "gloss");
			var secondaryEntry = CreateSimpleEntry("form2", "gloss2");
			var obj = AddComponentEntryRef(mainEntry, secondaryEntry);
			var testItem = CreateSimpleEntry("testform", "testgloss");

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// SUT
			// First close off task, since SUT has own UOW
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.AddItem(testItem);

			// Verify results
			Assert.AreEqual(2, obj.ComponentLexemesRS.Count,
				"Wrong number of ComponentLexemes.");
			Assert.IsTrue(obj.ComponentLexemesRS.ToHvoArray().Contains(testItem.Hvo),
				"testItem should be in ComponentLexemes property");
			Assert.AreEqual(0, mainEntry.EntryRefsOS.Count,
				"Shouldn't ever have any entry refs here.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a new Target item to the reference vector in the case where a list exists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddTwoNewTargetsToNonExistingList()
		{
			// Setup test
			ILexEntry mainEntry = null;
			var secondaryEntry = CreateSimpleEntry("form2", "gloss2");
			var obj = AddComponentEntryRef(mainEntry, secondaryEntry);
			var testItem = CreateSimpleEntry("testform", "testgloss");
			var testItem2 = CreateSimpleEntry("test2form", "test2gloss");

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// SUT
			// First close off task, since SUT has own UOW
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { testItem, testItem2 });

			// Verify results
			Assert.AreEqual(2, obj.ComponentLexemesRS.Count,
				"Wrong number of ComponentLexemes.");
			Assert.IsTrue(obj.ComponentLexemesRS.ToHvoArray().Contains(testItem.Hvo),
				"testItem should be in ComponentLexemes property");
			Assert.IsTrue(obj.ComponentLexemesRS.ToHvoArray().Contains(testItem2.Hvo),
				"testItem2 should be in ComponentLexemes property");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the reference vector leaving an empty list.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveTargetFromList_NowEmpty()
		{
			// Setup test
			var mainEntry = CreateSimpleEntry("form", "gloss");
			var secondaryEntry = CreateSimpleEntry("form2", "gloss2");
			var obj = AddComponentEntryRef(mainEntry, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject>());

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			Assert.AreEqual(0, secondaryEntry.EntryRefsOS[0].ComponentLexemesRS.Count,
				"Shouldn't have any ComponentLexemes left.");
			Assert.AreEqual(0, secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS.Count,
				"Shouldn't have any PrimaryLexemes left.");
			Assert.AreEqual(0, mainEntry.EntryRefsOS.Count,
				"Shouldn't ever have any entry refs here.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the reference vector in the case where the Target
		/// item is the second of a list of three items.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveTargetFromMiddleOfList()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			var obj = AddComponentEntryRef(entry3, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry1, entry3 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var result = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(2, result.Count,
				"Should have two ComponentLexemes left.");
			Assert.AreEqual(0, secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS.Count,
				"Shouldn't have any PrimaryLexemes.");
			Assert.False(result.ToHvoArray().Contains(entry2.Hvo),
				"The entry2 object should have been removed from ComponentLexemes.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the end of a reference vector in the case where it
		/// should remove the same item from a related vector.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveTargetFromEndOfListAffectingRelatedVector()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			AddComponentEntryRef(entry3, secondaryEntry);
			var obj = AddPrimaryEntryRef(entry3, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// Check pre-condition
			Assert.AreEqual(1, obj.PrimaryLexemesRS.Count,
				"There should be one PrimaryLexeme.");
			Assert.AreEqual(entry3.Hvo, obj.PrimaryLexemesRS[0].Hvo,
				"Wrong lexeme in PrimaryLexemes.");

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry1, entry2 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var compResult = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(2, compResult.Count,
				"Should have two ComponentLexemes left.");
			Assert.False(compResult.ToHvoArray().Contains(entry3.Hvo),
				"The entry3 object should have been removed from ComponentLexemes.");
			var primResult = secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS;
			Assert.AreEqual(0, primResult.Count,
				"Deleting entry3 object from ComponentLexemes, should remove it from PrimaryLexemes.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the end of a reference vector in the case where it
		/// should not remove another item from a related vector.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveTargetFromEndOfListNotAffectingRelatedVector()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			AddComponentEntryRef(entry3, secondaryEntry);
			var obj = AddPrimaryEntryRef(entry2, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// Check pre-condition
			Assert.AreEqual(1, obj.PrimaryLexemesRS.Count,
				"There should be one PrimaryLexeme.");
			Assert.AreEqual(entry2.Hvo, obj.PrimaryLexemesRS[0].Hvo,
				"Wrong lexeme in PrimaryLexemes.");

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry1, entry2 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var compResult = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(2, compResult.Count,
				"Should have two ComponentLexemes left.");
			Assert.False(compResult.ToHvoArray().Contains(entry3.Hvo),
				"The entry3 object should have been removed from ComponentLexemes.");
			var primResult = secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS;
			Assert.AreEqual(1, primResult.Count,
				"Deleting entry3 object from ComponentLexemes, should not remove existing PrimaryLexeme.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the reference vector and adding another item in the
		/// case where it should not affect the related vector.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveAndAddTargetsFromListNotAffectingRelatedVector()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			var obj = AddPrimaryEntryRef(entry2, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// Check pre-condition
			Assert.AreEqual(1, obj.PrimaryLexemesRS.Count,
				"There should be one PrimaryLexeme.");
			Assert.AreEqual(entry2.Hvo, obj.PrimaryLexemesRS[0].Hvo,
				"Wrong lexeme in PrimaryLexemes.");

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry2, entry3 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var compResult = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(2, compResult.Count,
				"Should have two ComponentLexemes left.");
			Assert.False(compResult.ToHvoArray().Contains(entry1.Hvo),
				"The entry1 object should have been removed from ComponentLexemes.");
			Assert.True(compResult.ToHvoArray().Contains(entry3.Hvo),
				"The entry3 object should have been added to ComponentLexemes.");
			var primResult = secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS;
			Assert.AreEqual(1, primResult.Count,
				"Modifications of ComponentLexemes, should not affect PrimaryLexemes.");
			Assert.AreEqual(entry2.Hvo, primResult[0].Hvo,
				"Entry2 object should be in PrimaryLexemes.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing the first Target item from the reference vector in the case where it
		/// should not affect the related vector.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveFirstTargetFromListNotAffectingRelatedVector()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			AddComponentEntryRef(entry3, secondaryEntry);
			var obj = AddPrimaryEntryRef(entry2, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// Check pre-condition
			Assert.AreEqual(1, obj.PrimaryLexemesRS.Count,
				"There should be one PrimaryLexeme.");
			Assert.AreEqual(entry2.Hvo, obj.PrimaryLexemesRS[0].Hvo,
				"Wrong lexeme in PrimaryLexemes.");

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry2, entry3 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var compResult = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(2, compResult.Count,
				"Should have two ComponentLexemes left.");
			Assert.False(compResult.ToHvoArray().Contains(entry1.Hvo),
				"The entry1 object should have been removed from ComponentLexemes.");
			var primResult = secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS;
			Assert.AreEqual(1, primResult.Count,
				"Deleting entry1 object from ComponentLexemes, should not affect PrimaryLexemes.");
			Assert.AreEqual(entry2.Hvo, primResult[0].Hvo,
				"Entry2 object should be in PrimaryLexemes.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a Target item from the reference vector and adding another item in the
		/// case where it should affect the related vector.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveAndAddTargetsFromListAffectingRelatedVector()
		{
			// Setup test
			var entry1 = CreateSimpleEntry("form1", "gloss1");
			var entry2 = CreateSimpleEntry("form2", "gloss2");
			var entry3 = CreateSimpleEntry("form3", "gloss3");
			var secondaryEntry = CreateSimpleEntry("phrase form", "phrase gloss");
			AddComponentEntryRef(entry1, secondaryEntry);
			AddComponentEntryRef(entry2, secondaryEntry);
			var obj = AddPrimaryEntryRef(entry2, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);

			// Check pre-condition
			Assert.AreEqual(1, obj.PrimaryLexemesRS.Count,
				"There should be one PrimaryLexeme.");
			Assert.AreEqual(entry2.Hvo, obj.PrimaryLexemesRS[0].Hvo,
				"Wrong lexeme in PrimaryLexemes.");

			// SUT
			Cache.ActionHandlerAccessor.EndUndoTask();
			MockLauncher.SetItems(new List<ICmObject> { entry3 });

			// Verify results
			Assert.AreEqual(1, secondaryEntry.EntryRefsOS.Count,
				"Should only have one entry ref object.");
			var compResult = secondaryEntry.EntryRefsOS[0].ComponentLexemesRS;
			Assert.AreEqual(1, compResult.Count,
				"Should only have one new ComponentLexeme left.");
			Assert.False(compResult.ToHvoArray().Contains(entry2.Hvo),
				"The entry2 object should have been removed from ComponentLexemes.");
			Assert.True(compResult.ToHvoArray().Contains(entry3.Hvo),
				"The entry3 object should have been added to ComponentLexemes.");
			var primResult = secondaryEntry.EntryRefsOS[0].PrimaryLexemesRS;
			Assert.AreEqual(0, primResult.Count,
				"Modifications of ComponentLexemes, should remove the one PrimaryLexeme.");
		}
	}

	public class MockVectorReferenceLauncher : VectorReferenceLauncher
	{
		#region overrides

		protected override VectorReferenceView CreateVectorReverenceView()
		{
			return new MockVectorReferenceView();
		}

		protected override int RootBoxHeight
		{
			get { return 16; }
		}

		protected override void AdjustFormScrollbars(bool displayScrollbars)
		{
			base.AdjustFormScrollbars(false);
		}

		protected override bool CanRaiseEvents
		{
			get { return false; }
		}

		#endregion

		public void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, string analysisWs)
		{
			Assert.IsNotNull(obj, "Must initialize with an object and flid.");
			Assert.Greater(flid, 0, "Must initialize with an object and flid.");
			Assert.IsNotNullOrEmpty(fieldName, "Must initialize with a field name.");
			Initialize(cache, obj, flid, fieldName, null, null, null, "", analysisWs);
		}
	}

	/// <summary>
	/// Functions with MockVectorReferenceLauncher to eliminate views from
	/// these VectorReferenceLauncher tests.
	/// </summary>
	public class MockVectorReferenceView : VectorReferenceView
	{
		#region overrides

		public override void MakeRoot()
		{
			//base.MakeRoot();
		}

		public override void ReloadVector()
		{

		}
		#endregion

	}
}
