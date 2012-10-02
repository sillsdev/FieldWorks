using System;
using LiftIO;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.Fieldworks.LexText
{
	/// <summary>
	/// Test LIFT-format import
	/// </summary>
	[TestFixture]
	public class ImporterTests : InMemoryFdoTestBase, ILiftMergerTestSuite
	{
		protected SIL.FieldWorks.LexText.FlexLiftMerger _merger;
		private string _log;
		private int _germanId;
		private int _frenchId;
		private int _urduId;

		protected override void CreateTestData()
		{
			_merger = new SIL.FieldWorks.LexText.FlexLiftMerger(this.Cache);
			this.m_inMemoryCache.InitializeLexDb();
			m_inMemoryCache.InitializeWritingSystemEncodings();
			this.Cache.LangProject.WorldRegion.AnalysisDefaultWritingSystem = "moon";
			 _germanId = Cache.LanguageEncodings.GetWsFromIcuLocale("de");
			_frenchId = Cache.LanguageEncodings.GetWsFromIcuLocale("fr");
			_urduId = Cache.LanguageEncodings.GetWsFromIcuLocale("ur");

		}

		void OnMergeEvent(object sender, string description)
		{
			_log += description;
		}


		//------ lexEntry-level merging ------------


		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void ModifiedEntryFromLiftReplacesUntouchedFlexOne()
		{
			//           e.DateCreated = new DateTime(DateTime.Today.Ticks - 100);
		}

		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void ModifiedEntryFromFlexIgnoresUntouchedLiftOne()
		{
		}


		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void BothFlexAndLiftModifiedEntryCollisionNoted()
		{
		}


		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void NewEntryFromLiftAdded()
		{
		}

		//---------- sense-level merging -------------------//
		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void NewSensesFromLiftAdded()
		{
		 }

		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void RemovedSenseFromLiftRemoved()
		{
		}

		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void RemovedSenseFromFlexRemoved()
		{
		}


		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void AddedSensesFromBoth()
		{
		}


		//--------- string alternative merging ----------

		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void NewAlternativesToLexemeFormFromBoth()
		{
		}

		[Test, NUnit.Framework.Ignore("Not implemented")]
		public void LexemeFormCollisionNoted()
		{
			//what to do?  put in import residue?  Need flag or "merge residue".
		}


		public void LoadWeSay()
		{
//            Flex.LiftImport.Importer importer = new Importer(m_cache);
//            int count = m_cache.LangProject.LexDbOA.EntriesOC.Count;
//            importer.ImportLiftFile(GetSampleFilePath("WeSaySample1.xml"));
//            Assert.AreEqual(1+count, m_cache.LangProject.LexDbOA.EntriesOC.Count);
//
//            LexEntry e = importer.ImportOneWeSayEntry(GetSampleFilePath("ChangedEntry.xml"), "lexicon/entry[@testid='1']");
//            Assert.AreEqual(1+count, m_cache.LangProject.LexDbOA.EntriesOC.Count, "should have merged");
//            Assert.IsTrue(((LexSense)e.SensesOS[0]).Gloss.AnalysisDefaultWritingSystem.Contains("changed"),"gloss not merged.");
//            Assert.IsTrue(e.LexemeFormOA.Form.VernacularDefaultWritingSystem.Contains("changed"),"lexeme form not merged.");


	   }


//
//       protected string GetSampleFilePath(string file)
//       {
//           string dir = @"C:\WeSay\src\FieldWorksImport.Tests\TestData";
//           return Path.Combine(dir, file);
//       }




		#region ILiftMergerTestSuite Members

		[Test]
		public void NewerIncomingFormsWin()
		{
			Assert.Fail("not yet");
		}

		[Test]
		public void NewerExistingFormsWin()
		{
			Assert.Fail("not yet");
		}

		[Test]
		public void NewEntryWithGuid()
		{
			IdentifyingInfo idInfo = new IdentifyingInfo();
			idInfo.id = Guid.NewGuid().ToString();
			ILexEntry e = _merger.GetOrMakeEntry(idInfo);
			Assert.AreEqual(idInfo.id, e.Guid.ToString());
		}

		[Test] public void NewEntryWithTextIdIgnoresIt()
		{
			IdentifyingInfo idInfo = new IdentifyingInfo();
			idInfo.id = "hello";
			ILexEntry e = _merger.GetOrMakeEntry(idInfo);
			//no attempt is made to use that id
			Assert.IsNotNull(e.Guid);
			Assert.AreNotSame(Guid.Empty, e.Guid);
		}

		[Test]
		public void NewEntryTakesGivenDates()
		{
			IdentifyingInfo idInfo = new IdentifyingInfo();
			idInfo = AddDates(idInfo);

			ILexEntry e = _merger.GetOrMakeEntry(idInfo);
			Assert.AreEqual(idInfo.creationTime, e.DateCreated );
			Assert.AreEqual(idInfo.modificationTime, e.DateModified);;
		}


		private static IdentifyingInfo AddDates(IdentifyingInfo idInfo)
		{
			idInfo.creationTime = DateTime.Parse("2003-08-07T08:42:42+07:00");
			idInfo.modificationTime = DateTime.Parse("2005-01-01T01:11:11+01:00");
			return idInfo;
		}


//        [Test]
//        public void MorphTypesMissnamed()
//        {
//            Assert.AreEqual("MorphTypes", Cache.LangProject.LexDbOA.MorphTypesOA.ToString());
//        }

		[Test]
		public void NewEntryNoDatesUsesNow()
		{
			ILexEntry e = MakeSimpleEntry();
			Assert.IsTrue(TimeSpan.FromTicks(DateTime.UtcNow.Ticks - e.DateCreated.Ticks).Seconds < 2);
			Assert.IsTrue(TimeSpan.FromTicks(DateTime.UtcNow.Ticks - e.DateModified.Ticks).Seconds < 2);
		}
		private ILexEntry MakeSimpleEntry()
		{
			IdentifyingInfo idInfo = new IdentifyingInfo();
			return _merger.GetOrMakeEntry(idInfo);
		}

		[Test]
		public void EntryGetsEmptyLexemeForm()
		{
			ILexEntry e = MakeSimpleEntry();
			_merger.MergeInLexemeForm(e, new SimpleMultiText());
			Assert.IsNull(e.LexemeFormOA);
		}

		[Test]
		public void CorrectWritingSystemAlternativeUsed()
		{
			ILexEntry e = MakeSimpleEntry();
			SimpleMultiText forms = new SimpleMultiText();
			forms.Add("fr", "french_form");
			forms.Add("ur", "urdu_form");
			_merger.MergeInLexemeForm(e, forms);
			 Assert.AreEqual("french_form", e.LexemeFormOA.Form.GetAlternative(_frenchId));
		   Assert.AreEqual("urdu_form", e.LexemeFormOA.Form.GetAlternative(_urduId));
		}

		[Test]
		public void ConverWritingSystemLabels()
		{
			Assert.AreEqual(_frenchId, _merger.GetWsFromLiftLang("fr"));
			Assert.AreEqual(_frenchId,  _merger.GetWsFromLiftLang("x-fr"));
		}

		[Test]
		public void WritingSystemFromLiftLang()
		{
			ILexEntry e = MakeSimpleEntry();
			SimpleMultiText forms = new SimpleMultiText();
			forms.Add("fr", "french_form");
			forms.Add("ur", "urdu_form");
			_merger.MergeInLexemeForm(e, forms);
			Assert.AreEqual("french_form", e.LexemeFormOA.Form.GetAlternative(_frenchId));
			Assert.AreEqual("urdu_form", e.LexemeFormOA.Form.GetAlternative(_urduId));
		}

		[Test, Ignore("Not yet")]
		public void NewWritingSystemAlternativeHandled()
		{
			ILexEntry e = MakeSimpleEntry();
			SimpleMultiText forms = new SimpleMultiText();
			forms.Add("x99", "x99_form");
			_merger.MergeInLexemeForm(e, forms);
			int newWsId = Cache.LanguageEncodings.GetWsFromIcuLocale("x99");
			Assert.AreNotEqual(0, newWsId);

			Assert.AreEqual("x99_form", e.LexemeFormOA.Form.GetAlternative(newWsId));
		}

		[Test]
		public void NewEntryGetsLexemeForm()
		{
			ILexEntry e = MakeSimpleEntry();
			SimpleMultiText forms = new SimpleMultiText();
			forms.Add("fr", "hello");
			forms.Add("ur", "bye");
			_merger.MergeInLexemeForm(e, forms);
		   // Assert.AreEqual(2, e.LexemeFormOA.Count);
		}

		[Test]
		public void LexemeFormTakesAffixTypeOfFirstAlt()
		{
			ILexEntry e = MakeSimpleEntry();
			SimpleMultiText forms = new SimpleMultiText();
			forms.Add("fr", "-sfx");
			forms.Add("ur", "stem");
			_merger.MergeInLexemeForm(e, forms);
			Assert.AreEqual(typeof(MoAffixAllomorph), e.LexemeFormOA.GetType());
			Assert.AreEqual("-", e.LexemeFormOA.MorphTypeRA.Prefix);
			Assert.AreEqual("sfx", e.LexemeFormOA.Form.VernacularDefaultWritingSystem);
		}

		[Test, Ignore("Not Impl")] public void TryCompleteEntry()
		{
			throw new NotImplementedException();
		}

		[Test, Ignore("Not Impl")] public void ModifiedDatesRetained()
		{
			throw new NotImplementedException();
		}

		private static IdentifyingInfo CreateFullIdInfo(Guid g)
		{
			IdentifyingInfo idInfo = new IdentifyingInfo();
			idInfo.id = g.ToString();
			idInfo = AddDates(idInfo);
			return idInfo;
		}

		[Test] public void ChangedEntryFound()
		{
			Guid g = Guid.NewGuid();
			IdentifyingInfo idInfo = CreateFullIdInfo(g);

			LexEntry e = new LexEntry();
			this.Cache.LangProject.LexDbOA.EntriesOC.Add(e);
			e.Guid = g;
			e.DateCreated  = idInfo.creationTime;
			e.DateModified = new DateTime(e.DateCreated.Ticks + 100);

			ILexEntry found = _merger.GetOrMakeEntry(idInfo);
			Assert.AreEqual(idInfo.creationTime, found.DateCreated);
		}

		[Test, Ignore("FW needs utc dates first")]
		public void UnchangedEntryPruned()
		{
			LexEntry e = new LexEntry();
			this.Cache.LangProject.LexDbOA.EntriesOC.Add(e);
			IdentifyingInfo idInfo;
			idInfo.id = e.Guid.ToString();
			idInfo.creationTime = e.DateCreated;
			idInfo.modificationTime = e.DateModified;
			ILexEntry m= _merger.GetOrMakeEntry(idInfo);
			Assert.IsNull(m);//pruned
			//Assert.AreEqual("", _log);
		}

		[Test] public void EntryWithIncomingUnspecifiedModTimeNotPruned()
		{
			LexEntry e = new LexEntry();
			this.Cache.LangProject.LexDbOA.EntriesOC.Add(e);
			IdentifyingInfo idInfo;
			idInfo.id = e.Guid.ToString();
			idInfo.creationTime = e.DateCreated;
			idInfo.modificationTime = DateTime.Parse(e.DateModified.ToString(LiftIO.IdentifyingInfo.LiftDateOnlyFormat));
			ILexEntry m = _merger.GetOrMakeEntry(idInfo);
			Assert.IsNotNull(m);//not pruned
		}

		[Test, Ignore("Not Impl")] public void MergingSameEntryLackingGuidId_TwiceFindMatch()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
