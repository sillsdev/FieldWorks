// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AtomicReferenceLauncherTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AtomicReferenceLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member variables

		private ILexEntryFactory m_leFact;
		private ILexEntryRefFactory m_lerFact;
		private string m_wsAnalStr;
		private int m_wsAnalysis;
		private int m_wsVern;
		private IPartOfSpeech m_noun;
		private IMoMorphType m_stem;
		public MockAtomicReferenceLauncher MockLauncher { get; set; }

		#endregion

		protected override void CreateTestData()
		{
			base.CreateTestData();
			var servLoc = Cache.ServiceLocator;
			m_leFact = servLoc.GetInstance<ILexEntryFactory>();
			m_lerFact = servLoc.GetInstance<ILexEntryRefFactory>();
			//m_moFact = servLoc.GetInstance<IMoStemAllomorphFactory>();
			MockLauncher = new MockAtomicReferenceLauncher();
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
			var lexAlt = TsStringUtils.MakeString(form, m_wsVern);
			var glossAlt = TsStringUtils.MakeString(gloss, m_wsAnalysis);
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

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests checking launcher Targets when obj is invalid (hvo less than 1).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TargetReturnsNullIfObjectIsInvalid()
		{
			// Setup test
			var mainEntry = CreateSimpleEntry("form", "gloss");
			var secondaryEntry = CreateSimpleEntry("form2", "gloss2");
			var obj = AddComponentEntryRef(mainEntry, secondaryEntry);

			// and initialize launcher
			MockLauncher.Initialize(Cache, obj, LexEntryRefTags.kflidComponentLexemes,
				"ComponentLexemesRS", m_wsAnalStr);
			obj.Delete();

			// SUT
			// First close off task, since SUT has own UOW
			Cache.ActionHandlerAccessor.EndUndoTask();
			var target = MockLauncher.Target;

			// Verify results
			Assert.IsNull(target, "Target should be null.");
		}
	}

	public class MockAtomicReferenceLauncher : AtomicReferenceLauncher
	{
		#region overrides

		protected override AtomicReferenceView CreateAtomicReferenceView()
		{
			return new MockAtomicReferenceView();
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

		public void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, string analysisWs)
		{
			Assert.IsNotNull(obj, "Must initialize with an object and flid.");
			Assert.Greater(flid, 0, "Must initialize with an object and flid.");
			Assert.IsNotNullOrEmpty(fieldName, "Must initialize with a field name.");
			Initialize(cache, obj, flid, fieldName, null, null, null, "", analysisWs);
		}
	}

	/// <summary>
	/// Functions with MockAtomicReferenceLauncher to eliminate views from
	/// these AtomicReferenceLauncher tests.
	/// </summary>
	public class MockAtomicReferenceView : AtomicReferenceView
	{
		#region overrides

		public override void MakeRoot()
		{
			//base.MakeRoot();
		}

		#endregion

	}
}
