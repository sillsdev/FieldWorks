using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Xml;
using System.Collections.Generic;
using System;

namespace SIL.FieldWorks.FDO.FDOTests.LingTests
{
	/// <summary>
	/// Tests for the non-generated parts of LexSense.
	/// </summary>
	[TestFixture]
	public class LexSenseTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ISilDataAccess m_sda;
		private Notifiee m_notifiee;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_sda = Cache.DomainDataByFlid;
		}

		/// <summary>
		/// Undo everything and clean up.
		/// </summary>
		public override void FixtureTeardown()
		{
			m_sda = null;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Stop the Undo task the base class kicks off,
		/// since this test makes its own
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_actionHandler.EndUndoTask();
		}

		/// <summary>
		/// Start a UOW, since the base class will want one.
		/// </summary>
		public override void TestTearDown()
		{
			// Start a UOW, since the base class will try and end it.
			m_sda.BeginUndoTask("Undo something", "Redo something");
			base.TestTearDown();
		}

		/// <summary>
		/// Test the virtual properties LexSense.LexSenseOutline and LexEntry.NumberOfSensesForEntry.
		/// </summary>
		[Test]
		public void TestLexSenseSideEffects()
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			ILexEntry entry = null;
			ILexSense sense, sense2 = null, sense3 = null, sense2_1 = null, sense2_2 = null, sense2_1_1 = null, sense2_2_1 = null;
			UndoableUnitOfWorkHelper.Do("Undo add senses", "Redo add senses", m_actionHandler, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				sense = senseFactory.Create();
				entry.SensesOS.Add(sense);
				Assert.AreEqual("1", sense.LexSenseOutline.Text);
				sense2 = senseFactory.Create();
				entry.SensesOS.Add(sense2);
				Assert.AreEqual("2", sense2.LexSenseOutline.Text);
				sense3 = senseFactory.Create();
				entry.SensesOS.Add(sense3);
				Assert.AreEqual("3", sense3.LexSenseOutline.Text);

				sense2_1 = senseFactory.Create();
				sense2.SensesOS.Add(sense2_1);
				Assert.AreEqual("2.1", sense2_1.LexSenseOutline.Text);

				sense2_2 = senseFactory.Create();
				sense2.SensesOS.Add(sense2_2);
				Assert.AreEqual("2.2", sense2_2.LexSenseOutline.Text);

				sense2_1_1 = senseFactory.Create();
				sense2_1.SensesOS.Add(sense2_1_1);
				Assert.AreEqual("2.1.1", sense2_1_1.LexSenseOutline.Text);

				sense2_2_1 = senseFactory.Create();
				sense2_2.SensesOS.Add(sense2_2_1);
				Assert.AreEqual("2.2.1", sense2_2_1.LexSenseOutline.Text);
			});

			m_notifiee = new Notifiee();
			var mdc = m_sda.MetaDataCache;
			var lsoFlid = mdc.GetFieldId("LexSense", "LexSenseOutline", false);
			m_sda.AddNotification(m_notifiee);
			ILexSense senseInserted = null;
			UndoableUnitOfWorkHelper.Do("Undo add another sense", "Redo add another sense", m_actionHandler, () =>
			{
				senseInserted = senseFactory.Create();
				entry.SensesOS.Insert(1, senseInserted);
				Assert.AreEqual("2", senseInserted.LexSenseOutline.Text);
			});
			int nosFlid = mdc.GetFieldId("LexEntry", "NumberOfSensesForEntry", false);
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
								 new ChangeInformationTest(entry.Hvo, LexEntryTags.kflidSenses, 1, 1, 0),
								 new ChangeInformationTest(sense2.Hvo, lsoFlid, 0, "2".Length, "3".Length),
								 new ChangeInformationTest(sense3.Hvo, lsoFlid, 0, "3".Length, "4".Length),
								 new ChangeInformationTest(sense2_1.Hvo, lsoFlid, 0, "2.1".Length, "3.1".Length),
								 new ChangeInformationTest(sense2_2.Hvo, lsoFlid, 0, "2.1".Length, "3.1".Length),
								 new ChangeInformationTest(sense2_1_1.Hvo, lsoFlid, 0, "2.1.1".Length, "3.1.1".Length),
								 new ChangeInformationTest(sense2_2_1.Hvo, lsoFlid, 0, "2.2.1".Length, "3.2.1".Length),
							 }, "insert second sense in entry");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual("3.1.1", sense2_1_1.LexSenseOutline.Text);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			UndoableUnitOfWorkHelper.Do("Undo remove sense", "Redo remove sense", m_actionHandler, () =>
				entry.SensesOS.Remove(senseInserted));
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
								 new ChangeInformationTest(entry.Hvo, LexEntryTags.kflidSenses, 1, 0, 1),
								 new ChangeInformationTest(sense2.Hvo, lsoFlid, 0, "2".Length, "3".Length),
								 new ChangeInformationTest(sense3.Hvo, lsoFlid, 0, "3".Length, "4".Length),
								 new ChangeInformationTest(sense2_1.Hvo, lsoFlid, 0, "2.1".Length, "3.1".Length),
								 new ChangeInformationTest(sense2_2.Hvo, lsoFlid, 0, "2.1".Length, "3.1".Length),
								 new ChangeInformationTest(sense2_1_1.Hvo, lsoFlid, 0, "2.1.1".Length, "3.1.1".Length),
								 new ChangeInformationTest(sense2_2_1.Hvo, lsoFlid, 0, "2.2.1".Length, "3.2.1".Length),
							 }, "delete second sense in entry");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual("2.1.1", sense2_1_1.LexSenseOutline.Text);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			UndoableUnitOfWorkHelper.Do("Undo add another sense", "Redo add another sense", m_actionHandler, () =>
			{
				senseInserted = senseFactory.Create();
				sense2.SensesOS.Insert(1, senseInserted);
				Assert.AreEqual("2.2", senseInserted.LexSenseOutline.Text);
			});
			m_notifiee.CheckChangesWeaker(new[]
							{
								new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
								new ChangeInformationTest(sense2.Hvo, LexSenseTags.kflidSenses, 1, 1, 0),
								new ChangeInformationTest(sense2_2.Hvo, lsoFlid, 0, "2.2".Length, "2.3".Length),
								new ChangeInformationTest(sense2_2_1.Hvo, lsoFlid, 0, "2.2.1".Length, "2.3.1".Length),
							}, "insert subsense in sense");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual("2.3.1", sense2_2_1.LexSenseOutline.Text);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			UndoableUnitOfWorkHelper.Do("Undo get rid of sense", "Redo get rid of sense", m_actionHandler, () =>
				sense2.SensesOS.Remove(senseInserted));
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
								 new ChangeInformationTest(sense2.Hvo, LexSenseTags.kflidSenses, 1, 0, 1),
								 new ChangeInformationTest(sense2_2.Hvo, lsoFlid, 0, "2.2".Length, "2.3".Length),
								 new ChangeInformationTest(sense2_2_1.Hvo, lsoFlid, 0, "2.2.1".Length, "2.3.1".Length),
							 }, "remove subsense from sense");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual("2.2.1", sense2_2_1.LexSenseOutline.Text);
		}

	}

	/// <summary>
	/// More tests for LexSense methods.
	/// </summary>
	[TestFixture]
	public class MoreLexSenseTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Check the method for merging RDE senses. (RDEMergeSense)
		/// </summary>
		[Test]
		public void RdeMerge()
		{
			if (Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count < 5)
			{
				ICmSemanticDomainFactory factSemDom = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
				while (Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count < 5)
				{
					ICmSemanticDomain sem = factSemDom.Create();
					Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(sem);
				}
			}
			IFdoOwningSequence<ICmPossibility> seqSemDom = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS;
			ICmSemanticDomain semDom1 = seqSemDom[0] as ICmSemanticDomain;
			ICmSemanticDomain semDom2 = seqSemDom[1] as ICmSemanticDomain;
			ICmSemanticDomain semDom3 = seqSemDom[2] as ICmSemanticDomain;
			ICmSemanticDomain semDom4 = seqSemDom[4] as ICmSemanticDomain;

			// Create a LexEntry LE1 ("xyzTest1" defined as "xyzDefn1.1" in D1).
			// Attempt to merge it and verify that it survives.

			ILexEntry le1 = MakeLexEntry("xyzTest1", "xyzDefn1.1", semDom1);
			Set<int> newItems = new Set<int>();
			ILexSense sense1 = le1.SensesOS[0];
			newItems.Add(sense1.Hvo);
			// get an id for the 'list of senses' property (which isn't in the model)
			int fakeFlid = (int)CmObjectFields.kflidStartDummyFlids + 1;

			List<XmlNode> columns = new List<XmlNode>();
			bool fSenseDeleted = sense1.RDEMergeSense(semDom1.Hvo, columns, Cache, newItems);
			Assert.IsFalse(fSenseDeleted, "RDEMergeSense 1: sense should not be deleted");
			Assert.IsTrue(IsRealLexEntry(le1));
			Assert.IsTrue(le1.SensesOS[0].SemanticDomainsRC.Contains(semDom1));

			// Attempt to merge them both.
			// Verify that LE3 survives.
			// Verify that old LE1 survives and now has two senses; new sense has xyzDefn1.2.
			// Verify that LE2 is deleted and LE3 survives.
			ILexEntry le2 = MakeLexEntry("xyzTest1", "xyzDefn1.2", semDom2);
			Set<int> newItems2 = new Set<int>();
			ILexSense sense2 = le2.SensesOS[0];
			newItems2.Add(sense2.Hvo);

			ILexEntry le3 = MakeLexEntry("xyzTest3", "xyzDefn3.1", semDom2);
			ILexSense sense3 = le3.SensesOS[0];
			newItems2.Add(sense3.Hvo);

			fSenseDeleted = sense2.RDEMergeSense(semDom2.Hvo, columns, Cache, newItems2);
			Assert.IsFalse(fSenseDeleted, "RDEMergeSense 2: sense should not be deleted");
			fSenseDeleted = sense3.RDEMergeSense(semDom2.Hvo, columns, Cache, newItems2);
			Assert.IsFalse(fSenseDeleted, "RDEMergeSense 3: sense should not be deleted");
			Assert.IsTrue(IsRealLexEntry(le3));
			Assert.IsFalse(IsRealLexEntry(le2), "entry should be deleted");
			Assert.IsTrue(IsRealLexEntry(le1));
			Assert.AreEqual(2, le1.SensesOS.Count, "sense added to entry by merge");
			Assert.AreEqual(1, le3.SensesOS.Count, "should still be one sense");
			Assert.AreEqual("xyzDefn1.2", le1.SensesOS[1].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.IsTrue(le1.SensesOS[1].SemanticDomainsRC.Contains(semDom2));
			Assert.IsTrue(le3.SensesOS[0].SemanticDomainsRC.Contains(semDom2));

			// Create two more entries LE4("xyzTest1/xyzDefn1.2/D3" and LE5 ("xyzTest1/xyzDefn1.3/D3").
			// Verify that the second sense of LE1 gains a domain;
			// It also gains exactly one new sense;
			// And LE4 and LE5 are both deleted.
			ILexEntry le4 = MakeLexEntry("xyzTest1", "xyzDefn1.2", semDom3);
			Set<int> newItems3 = new Set<int>();
			ILexSense sense4 = le4.SensesOS[0];
			newItems3.Add(sense4.Hvo);

			ILexEntry le5 = MakeLexEntry("xyzTest1", "xyzDefn1.3", semDom3);
			ILexSense sense5 = le5.SensesOS[0];
			newItems3.Add(sense5.Hvo);

			fSenseDeleted = sense4.RDEMergeSense(semDom3.Hvo, columns, Cache, newItems3);
			Assert.IsTrue(fSenseDeleted, "RDEMergeSense 4: sense should be deleted");
			fSenseDeleted = sense5.RDEMergeSense(semDom3.Hvo, columns, Cache, newItems3);
			Assert.IsFalse(fSenseDeleted, "RDEMergeSense 5: sense should not be deleted");
			Assert.IsTrue(IsRealLexEntry(le3));
			Assert.IsFalse(IsRealLexEntry(le4));
			Assert.IsFalse(IsRealLexEntry(le5));
			Assert.IsTrue(IsRealLexEntry(le1));
			Assert.AreEqual(3, le1.SensesOS.Count, "one sense added to entry by merge");
			Assert.AreEqual("xyzDefn1.3", le1.SensesOS[2].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.IsTrue(le1.SensesOS[2].SemanticDomainsRC.Contains(semDom3));
			int[] sense2Domains = le1.SensesOS[1].SemanticDomainsRC.ToHvoArray();
			Assert.AreEqual(2, sense2Domains.Length, "got 2 semantic domains on sense 2");
			int minDom = Math.Min(semDom2.Hvo, semDom3.Hvo);  // smaller of expected domains.
			int maxDom = Math.Max(semDom2.Hvo, semDom3.Hvo);
			int minActual = Math.Min(sense2Domains[0], sense2Domains[1]);
			int maxActual = Math.Max(sense2Domains[0], sense2Domains[1]);
			Assert.AreEqual(minDom, minActual, "expected domains on merged sense");
			Assert.AreEqual(maxDom, maxActual, "expected domains on merged sense");

			// Try adding four senses, three for the same CF, but which doesn't pre-exist.
			// Also, the three are exact duplicates.
			ILexEntry le6 = MakeLexEntry("xyzTest6", "xyzDefn6.1", semDom4);
			Set<int> newItems4 = new Set<int>();
			ILexSense sense6 = le6.SensesOS[0];
			newItems4.Add(sense6.Hvo);

			ILexEntry le7 = MakeLexEntry("xyzTest6", "xyzDefn6.1", semDom4);
			ILexSense sense7 = le7.SensesOS[0];
			newItems4.Add(sense7.Hvo);

			ILexEntry le8 = MakeLexEntry("xyzTest6", "xyzDefn6.1", semDom4);
			ILexSense sense8 = le8.SensesOS[0];
			newItems4.Add(sense8.Hvo);

			fSenseDeleted = sense6.RDEMergeSense(semDom4.Hvo, columns, Cache, newItems4);
			Assert.IsFalse(fSenseDeleted, "RDEMergeSense 6: sense should not be deleted");
			fSenseDeleted = sense7.RDEMergeSense(semDom4.Hvo, columns, Cache, newItems4);
			Assert.IsTrue(fSenseDeleted, "RDEMergeSense 7: sense should be deleted");
			fSenseDeleted = sense8.RDEMergeSense(semDom4.Hvo, columns, Cache, newItems4);
			Assert.IsTrue(fSenseDeleted, "RDEMergeSense 8: sense should be deleted");

			Assert.IsTrue(IsRealLexEntry(le6));
			Assert.IsFalse(IsRealLexEntry(le7));
			Assert.IsFalse(IsRealLexEntry(le8));
			Assert.AreEqual(1, le6.SensesOS.Count, "one sense survives merge");
		}
		/// <summary>
		/// Check the method for merging RDE senses, in the more complex cases involving gloss and citation form as well as definition and lexeme form
		/// Also multiple writing systems.
		/// </summary>
		[Test]
		public void RdeMerge_GlossAndCf()
		{
			if (Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count < 5)
			{
				ICmSemanticDomainFactory factSemDom = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
				while (Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count < 5)
				{
					ICmSemanticDomain sem = factSemDom.Create();
					Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(sem);
				}
			}
			IFdoOwningSequence<ICmPossibility> seqSemDom = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS;
			ICmSemanticDomain semDom1 = seqSemDom[0] as ICmSemanticDomain;
			ICmSemanticDomain semDom2 = seqSemDom[1] as ICmSemanticDomain;
			ICmSemanticDomain semDom3 = seqSemDom[2] as ICmSemanticDomain;
			ICmSemanticDomain semDom4 = seqSemDom[4] as ICmSemanticDomain;

			// Create a LexEntry LE1 (cf "red" gloss "rot" in D1).
			// Attempt to merge it and verify that it survives.

			ILexEntry red = MakeLexEntry("red", "", "rot", "", semDom1);
			Set<int> newItems = new Set<int>();
			ILexSense sense1 = red.SensesOS[0];
			newItems.Add(sense1.Hvo);
			List<XmlNode> columns = new List<XmlNode>();
			bool fSenseDeleted = RunMergeSense(columns, red);
			Assert.IsFalse(fSenseDeleted, "Merging red when there is no similar item should not delete sense");
			Assert.IsTrue(IsRealLexEntry(red), "Merging with no similar entry should not delete entry");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove semantic domain");

			// After creating similar entry in another domain with same info, should merge.
			var red2 = MakeLexEntry("red", "", "rot", "", semDom2);
			fSenseDeleted = RunMergeSense(columns, red2);
			Assert.IsTrue(fSenseDeleted, "Merging red/rot with matching lf/gloss should merge and delete new sense");
			Assert.IsFalse(IsRealLexEntry(red2), "Merging with red/rot with matching lf/gloss should delete entry");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should add new semantic domain");

			// Another similar entry should merge, adding definition.
			var red3 = MakeLexEntry("red", "", "rot", "rot2", semDom3);
			fSenseDeleted = RunMergeSense(columns, red3);
			Assert.IsTrue(fSenseDeleted, "Merging red/rot with matching lf/gloss and new defn should merge and delete new sense");
			Assert.IsFalse(IsRealLexEntry(red3), "Merging with red/rot with matching lf/gloss and new defn should delete entry");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should not remove old semantic domain");
			Assert.IsTrue(red.SensesOS[0].SemanticDomainsRC.Contains(semDom3), "Merging should add new semantic domain");
			Assert.That(red.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("rot2"));

			// Similarly we should be able to start with a matching definition, and add a gloss.
			var blue = MakeLexEntry("blue", "", "", "blau", semDom1);
			var blue2 = MakeLexEntry("blue", "", "blauG", "blau", semDom2);
			fSenseDeleted = RunMergeSense(columns, blue2);
			Assert.IsTrue(fSenseDeleted, "Merging blue/blau with matching lf/defn and new gloss should merge and delete new sense");
			Assert.IsFalse(IsRealLexEntry(blue2), "Merging blue/blau with matching lf/defn and new gloss should delete entry");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should add new semantic domain");
			Assert.That(blue.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text, Is.EqualTo("blauG"));

			// Conflicts in another writing system should prevent merging
			var blue3 = MakeLexEntry("blue", "", "blauG", "blau", semDom3);
			var wsEs = Cache.WritingSystemFactory.GetWsFromStr("es");
			blue.SensesOS[0].Gloss.set_String(wsEs, "blueS");
			blue3.SensesOS[0].Gloss.set_String(wsEs, "blueS3");
			fSenseDeleted = RunMergeSense(columns, blue3);
			Assert.IsFalse(fSenseDeleted, "Merging blue/blau with matching lf/defn/gloss but different gloss in spanish should not delete sense");
			Assert.IsFalse(IsRealLexEntry(blue3), "Merging blue/blau with matching lf/defn/gloss but different gloss in spanish should delete entry");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should not remove old semantic domain");
			Assert.IsTrue(blue.SensesOS[1].SemanticDomainsRC.Contains(semDom3), "Merging should not remove old semantic domain");

			// A conflicting lex entry, even though a homograph in the current relevant writing system, should prevent merging,
			// even though the sense data all matches
			blue.CitationForm.set_String(Cache.DefaultVernWs, "blueCf");
			var blue4 = MakeLexEntry("blueForm2", "blueCf", "blauG", "blau", semDom4);
			fSenseDeleted = RunMergeSense(columns, blue4);
			Assert.IsFalse(fSenseDeleted, "Merging blueCf/blau with matching lf/defn/gloss but different lexeme form should not delete sense");
			Assert.IsTrue(IsRealLexEntry(blue4), "Merging blueCf/blau with matching lf/defn/gloss but different lexeme form should not delete entry");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(blue.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should not remove old semantic domain");
			Assert.IsTrue(blue4.SensesOS[0].SemanticDomainsRC.Contains(semDom4), "Merging should not remove old semantic domain");

			// This case demonstrates that we can merge when the existing entry has no lexeme form, filling in the one we have.
			// We can also fill in other WS alternatives.
			var green = MakeLexEntry("", "greenF", "grun", "color grun", semDom1);
			var green2 = MakeLexEntry("green", "greenF", "grun", "color grun", semDom2);
			green2.CitationForm.set_String(wsEs, "grunCfS");
			green2.LexemeFormOA.Form.set_String(wsEs, "grunS");
			fSenseDeleted = RunMergeSense(columns, green2);
			Assert.IsTrue(fSenseDeleted, "Merging green/grun with matching cf/defn/gloss and one missing LF should delete sense");
			Assert.IsFalse(IsRealLexEntry(green2), "Merging blueCf/blau with matching lf/defn/gloss but different lexeme form should not delete entry");
			Assert.IsTrue(green.SensesOS[0].SemanticDomainsRC.Contains(semDom1), "Merging should not remove old semantic domain");
			Assert.IsTrue(green.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should not remove old semantic domain");
			Assert.That(green.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("green"), "merge should fill in missing LF");
			Assert.That(green.LexemeFormOA.Form.get_String(wsEs).Text, Is.EqualTo("grunS"), "merge should fill in missing spanish LF");
			Assert.That(green.CitationForm.get_String(wsEs).Text, Is.EqualTo("grunCfS"), "merge should fill in missing spanish CF");

			// We should not merge when NO non-empty string matches.
			var brown = MakeLexEntry("brown", "", "braun", "", semDom1);
			var brown2 = MakeLexEntry("brown", "", "", "braun color", semDom2);
			fSenseDeleted = RunMergeSense(columns, brown2);
			Assert.IsFalse(fSenseDeleted, "Merging two forms of brown with no overlap between gloss and defn should not delete sense");

			// But as a special case we can merge if definition of one matches gloss of other.
			var brown3 = MakeLexEntry("brown", "", "", "braun", semDom2);
			fSenseDeleted = RunMergeSense(columns, brown3);
			Assert.IsTrue(fSenseDeleted, "Merging two forms of brown with no gloss of one equal to defn of other should delete sense");
			Assert.IsTrue(brown.SensesOS[0].SemanticDomainsRC.Contains(semDom2), "Merging should combine semantic domains");
			Assert.That(brown.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("braun"), "Merging should copy definition");

			// We want to match entries that have the same LexemeForm even if they are not homographs.
			// This is possible if one of them has an empty CF in the homograph writing system.
			var orange = MakeLexEntry("orange", "orangeCf", "orang", "", semDom1);
			var orange2 = MakeLexEntry("orange", "", "orang", "", semDom2);
			fSenseDeleted = RunMergeSense(columns, orange2);
			Assert.IsTrue(fSenseDeleted, "Merging two forms of orange with matching LF and new blank Cf should delete sense");

			var pink = MakeLexEntry("pink", "", "rose", "", semDom1);
			var pink2 = MakeLexEntry("pink", "pinkCf", "rose", "", semDom2);
			fSenseDeleted = RunMergeSense(columns, pink2);
			Assert.IsTrue(fSenseDeleted, "Merging two forms of pink with matching LF and old blank Cf should delete sense");
			Assert.That(pink.CitationForm.VernacularDefaultWritingSystem.Text, Is.EqualTo("pinkCf"), "merge should fill in missing CF");

			// An inexact match that moves a sense should still fill in missing info on the chosen entry.
			var yellow = MakeLexEntry("yellow", "", "flower", "", semDom1);
			var yellow2 = MakeLexEntry("yellow", "yellowCf", "floral", "", semDom2);
			fSenseDeleted = RunMergeSense(columns, yellow2);
			Assert.IsFalse(fSenseDeleted, "Merging two forms of yellow with matching LF and old blank Cf should delete sense");
			Assert.That(yellow.CitationForm.VernacularDefaultWritingSystem.Text, Is.EqualTo("yellowCf"), "merge should fill in missing CF");
		}

		private bool RunMergeSense(List<XmlNode> columns, ILexEntry entry)
		{
			var sense = entry.SensesOS[0];
			Set<int> newItems;
			bool fSenseDeleted;
			newItems = new Set<int>();
			newItems.Add(sense.Hvo);
			fSenseDeleted = sense.RDEMergeSense(sense.SemanticDomainsRC.ToArray()[0].Hvo, columns, Cache, newItems);
			return fSenseDeleted;
		}

		private ILexEntry MakeLexEntry(string lf, string cf, string gloss, string defn, ICmSemanticDomain domain)
		{
			var le = MakeLexEntry(cf, defn, domain);
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			le.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, lf);
			le.SensesOS[0].Gloss.set_String(Cache.DefaultAnalWs, gloss);
			return le;
		}

		private ILexEntry MakeLexEntry(string cf, string defn, ICmSemanticDomain domain)
		{
			var servLoc = Cache.ServiceLocator;
			var le = servLoc.GetInstance<ILexEntryFactory>().Create();

			var ws = Cache.DefaultVernWs;
			le.CitationForm.set_String(ws, Cache.TsStrFactory.MakeString(cf, ws));
			var ls = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			ws = Cache.DefaultAnalWs;
			ls.Definition.set_String(ws, Cache.TsStrFactory.MakeString(defn, ws));
			if (domain != null)
				ls.SemanticDomainsRC.Add(domain);
			var msa = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		private bool IsRealLexEntry(ILexEntry le)
		{
			return le != null &&
				Cache.ServiceLocator.GetInstance<ICmObjectRepository>().IsValidObjectId(le.Hvo) &&
				le.ClassID == LexEntryTags.kClassId;
		}

		/// <summary>
		/// So far this is a very minimal test, checking that the particular problem observed in FWR-2850 stays fixed.
		/// </summary>
		[Test]
		public void SetSandboxMSA()
		{
			var servLoc = Cache.ServiceLocator;
			var le = servLoc.GetInstance<ILexEntryFactory>().Create();
			var ls = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			var msaOld = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msaOld);
			ls.MorphoSyntaxAnalysisRA = msaOld;
			msaOld.MsFeaturesOA = servLoc.GetInstance<IFsFeatStrucFactory>().Create();
			msaOld.MsFeaturesOA.FeatureSpecsOC.Add(servLoc.GetInstance<IFsOpenValueFactory>().Create());
			var newMsa = new SandboxGenericMSA();
			((LexSense) ls).SandboxMSA = newMsa;
			Assert.That(((IMoStemMsa) ls.MorphoSyntaxAnalysisRA).MsFeaturesOA, Is.Not.Null);
			Assert.That(ls.MorphoSyntaxAnalysisRA, Is.Not.EqualTo(msaOld));

			// Check that we can use it to set an MSA that matches an existing one. This should result
			// in the two senses havingt the same MSA and the unused one being deleted (but not
			// a crash trying to delete it twice--LT-11195)
			var sense2 = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(sense2);
			sense2.MorphoSyntaxAnalysisRA = ls.MorphoSyntaxAnalysisRA;
			newMsa = new SandboxGenericMSA();
			var posList = Cache.LangProject.PartsOfSpeechOA;
			if (posList == null)
			{
				posList = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA = posList;
			}
			var newPos = servLoc.GetInstance<IPartOfSpeechFactory>().Create();
			posList.PossibilitiesOS.Add(newPos);
			newMsa.MainPOS = newPos;
			ls.SandboxMSA = newMsa;
			Assert.That(le.MorphoSyntaxAnalysesOC.Count, Is.EqualTo(2));
			sense2.SandboxMSA = newMsa;
			Assert.That(sense2.MorphoSyntaxAnalysisRA, Is.EqualTo(ls.MorphoSyntaxAnalysisRA));
			Assert.That(le.MorphoSyntaxAnalysesOC.Count, Is.EqualTo(1));
		}

		/// <summary>
		/// Test that we can make a new sense with specified lexeme form and definition
		/// </summary>
		[Test]
		public void RDENewSense_Handles_LexemeFormAndDefinition()
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<LexSenseFactory>();
			var mySd = MakeSemanticDomain();
			var nodes = MakeNodeList(new string[] { "Word (Lexeme Form)", "Meaning (Definition)" });
			ITsString[] data = new ITsString[] {MakeVernString("kick"), MakeAnalysisString("strike with foot")};

			int hvoSense = senseFactory.RDENewSense(mySd.Hvo, nodes, data, Cache, null);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSense);
			var entry = (ILexEntry) sense.Owner;
			Assert.That(entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("kick"));
			Assert.That(sense.Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("strike with foot"));
		}
		/// <summary>
		/// Test that we can make a new sense with specified lexeme form and definition
		/// </summary>
		[Test]
		public void RDENewSense_Handles_CitationFormAndGloss()
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<LexSenseFactory>();
			var mySd = MakeSemanticDomain();
			var nodes = MakeNodeList(new string[] { "Word (Citation Form)", "Meaning (Gloss)" });
			ITsString[] data = new ITsString[] { MakeVernString("kick"), MakeAnalysisString("strike with foot") };

			int hvoSense = senseFactory.RDENewSense(mySd.Hvo, nodes, data, Cache, null);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSense);
			var entry = (ILexEntry)sense.Owner;
			Assert.That(entry.CitationForm.VernacularDefaultWritingSystem.Text, Is.EqualTo("kick"));
			Assert.That(sense.Gloss.AnalysisDefaultWritingSystem.Text, Is.EqualTo("strike with foot"));
			var morphTypeRep = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			Assert.That(entry.LexemeFormOA, Is.Not.Null);
			Assert.That(entry.LexemeFormOA.MorphTypeRA, Is.EqualTo(morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphStem)));
		}

		/// <summary>
		/// Test that we can make a new sense with specified all four fields filled in. It's especially important to
		/// test citation form coming before lexeme form.
		/// </summary>
		[Test]
		public void RDENewSense_Handles_FourColumns()
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<LexSenseFactory>();
			var mySd = MakeSemanticDomain();
			var nodes = MakeNodeList(new string[] { "Word (Citation Form)", "Word (Lexeme Form)", "Meaning (Gloss)", "Meaning (Definition)"});
			ITsString[] data = new ITsString[] { MakeVernString("kick"), MakeVernString("kickL"),
				MakeAnalysisString("strike with foot"), MakeAnalysisString("strike sharply with foot") };

			int hvoSense = senseFactory.RDENewSense(mySd.Hvo, nodes, data, Cache, null);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSense);
			var entry = (ILexEntry)sense.Owner;
			Assert.That(entry.CitationForm.VernacularDefaultWritingSystem.Text, Is.EqualTo("kick"));
			Assert.That(entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("kickL"));
			Assert.That(sense.Gloss.AnalysisDefaultWritingSystem.Text, Is.EqualTo("strike with foot"));
			Assert.That(sense.Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("strike sharply with foot"));
			var morphTypeRep = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			Assert.That(entry.LexemeFormOA, Is.Not.Null);
			Assert.That(entry.LexemeFormOA.MorphTypeRA, Is.EqualTo(morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphStem)));
		}

		private List<XmlNode> MakeNodeList(string[] labels)
		{
			var doc = new XmlDocument();
			var result = new List<XmlNode>();
			foreach (var item in labels)
			{
				var node = doc.CreateElement("column");
				XmlUtils.SetAttribute(node, "label", item);
				result.Add(node);
			}
			return result;
		}

		private ITsString MakeVernString(string arg)
		{
			return Cache.TsStrFactory.MakeString(arg, Cache.DefaultVernWs);
		}

		private ITsString MakeAnalysisString(string arg)
		{
			return Cache.TsStrFactory.MakeString(arg, Cache.DefaultAnalWs);
		}

		private ICmSemanticDomain MakeSemanticDomain()
		{
			var sdList = Cache.LangProject.SemanticDomainListOA;
			if (sdList == null)
			{
				sdList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				Cache.LangProject.SemanticDomainListOA = sdList;
			}
			var mySd = Cache.ServiceLocator.GetInstance<CmSemanticDomainFactory>().Create();
			sdList.PossibilitiesOS.Add(mySd);
			return mySd;
		}
	}
}
