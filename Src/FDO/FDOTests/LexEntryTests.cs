// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LexEntryTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LexEntryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ISilDataAccess m_sda;
		ILexEntryFactory m_entryFactory;
		ILexSenseFactory m_senseFactory;
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
			m_entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			m_senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
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
		/// Test the named method.
		/// </summary>
		[Test]
		public void MoveSenseToCopy()
		{
			ILexEntry oldEntry = null;
			ILexSense complexSense = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
					{
						oldEntry = MakeEntryWithAllPropsSet();
						complexSense = AddSenseWithHierarchyOfSubsenses(oldEntry);
					});
			oldEntry.MoveSenseToCopy(complexSense); // makes its own UOW
			var newEntry = complexSense.Owner as ILexEntry;

			// Check we got a copy
			Assert.That(newEntry, Is.Not.Null);
			Assert.That(newEntry, Is.Not.EqualTo(oldEntry));

			// Check the senses were moved.
			Assert.That(newEntry.SensesOS, Has.Member(complexSense));
			Assert.That(newEntry.SensesOS, Has.Count.EqualTo(1));
			Assert.That(oldEntry.SensesOS, Has.Count.EqualTo(1));
			Assert.That(newEntry.AllSenses, Has.Count.EqualTo(4), "all senses were moved");

			// Check the other properties were copied (and not changed)
			VerifyCopiedProperties(newEntry);
			VerifyCopiedProperties(oldEntry);

			// Check MSAs were handled properly.
			var oldSense = oldEntry.SensesOS[0];
			Assert.That(oldEntry.MorphoSyntaxAnalysesOC, Has.Count.EqualTo(1), "other MSAs should be destroyed since not used by surviving sense");
			Assert.That(oldSense.MorphoSyntaxAnalysisRA, Is.EqualTo(oldEntry.MorphoSyntaxAnalysesOC.First()));
			Assert.That(newEntry.MorphoSyntaxAnalysesOC, Has.Count.EqualTo(2), "two msas are used by moved senses");
			var newSenseTop = newEntry.SensesOS[0];
			Assert.That(newEntry.MorphoSyntaxAnalysesOC, Has.Member(newSenseTop.MorphoSyntaxAnalysisRA));
			var sub1 = newSenseTop.SensesOS[0];
			Assert.That(newEntry.MorphoSyntaxAnalysesOC, Has.Member(sub1.MorphoSyntaxAnalysisRA));
			Assert.That(newSenseTop.MorphoSyntaxAnalysisRA, Is.Not.EqualTo(sub1.MorphoSyntaxAnalysisRA));
			var sub2 = newSenseTop.SensesOS[1];
			Assert.That(newSenseTop.MorphoSyntaxAnalysisRA, Is.EqualTo(sub2.MorphoSyntaxAnalysisRA));
			var sub11 = sub1.SensesOS[0];
			Assert.That(sub11.MorphoSyntaxAnalysisRA, Is.EqualTo(sub1.MorphoSyntaxAnalysisRA));
		}

		/// <summary>
		/// Consistent with MakeEntryWithAllPropsSet
		/// </summary>
		private void VerifyCopiedProperties(ILexEntry newEntry)
		{
			Assert.That(newEntry.CitationForm.AnalysisDefaultWritingSystem.Text, Is.EqualTo("cf"));
			Assert.That(newEntry.Bibliography.AnalysisDefaultWritingSystem.Text, Is.EqualTo("bib"));
			Assert.That(newEntry.Bibliography.VernacularDefaultWritingSystem.Text, Is.EqualTo("bibV"));
			Assert.That(newEntry.Comment.AnalysisDefaultWritingSystem.Text, Is.EqualTo("com"));
			Assert.That(newEntry.LiteralMeaning.AnalysisDefaultWritingSystem.Text, Is.EqualTo("lit"));
			Assert.That(newEntry.Restrictions.AnalysisDefaultWritingSystem.Text, Is.EqualTo("restrict"));
			Assert.That(newEntry.SummaryDefinition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("sum"));
			Assert.That(newEntry.DoNotUseForParsing, Is.True);
			Assert.That(newEntry.ExcludeAsHeadword, Is.True);
			Assert.That(newEntry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("lf"));
			Assert.That(newEntry.AlternateFormsOS, Has.Count.EqualTo(2));
			Assert.That(newEntry.AlternateFormsOS[0].Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("alt1"));
			Assert.That(newEntry.AlternateFormsOS[1].Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("alt2"));
			Assert.That(newEntry.PronunciationsOS[0].Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("pron"));
			Assert.That(newEntry.EntryRefsOS, Has.Count.EqualTo(1));
			Assert.That(newEntry.EtymologyOA, Is.Not.Null);
		}

		// Reflect changes in VerifyCopiedProperties
		ILexEntry MakeEntryWithAllPropsSet()
		{
			ILexEntry entry = MakeEntry();
			var sense = MakeSense(entry);

			entry.CitationForm.AnalysisDefaultWritingSystem = MakeAnalysisString("cf");
			// At least one property try with multiple WSs.
			entry.Bibliography.AnalysisDefaultWritingSystem = MakeAnalysisString("bib");
			entry.Bibliography.VernacularDefaultWritingSystem = MakeVernString("bibV");
			entry.Comment.AnalysisDefaultWritingSystem = MakeAnalysisString("com");
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = MakeAnalysisString("lit");
			entry.Restrictions.AnalysisDefaultWritingSystem = MakeAnalysisString("restrict");
			entry.SummaryDefinition.AnalysisDefaultWritingSystem = MakeAnalysisString("sum");
			entry.DoNotUseForParsing = true;
			entry.ExcludeAsHeadword = true;
			var form = MakeLexemeForm(entry);
			form.Form.VernacularDefaultWritingSystem = MakeVernString("lf");
			var alternate1 = MakeAllomorph(entry);
			alternate1.Form.VernacularDefaultWritingSystem = MakeVernString("alt1");
			var alternate2 = MakeAllomorph(entry);
			alternate2.Form.VernacularDefaultWritingSystem = MakeVernString("alt2");
			var pron = MakePronunciation(entry);
			pron.Form.VernacularDefaultWritingSystem = MakeVernString("pron");
			var er = MakeEntryRef(entry);
			MakeEtymology(entry);
			var pos1 = MakePartOfSpeech();
			var msa = MakeMsa(entry, pos1);
			sense.MorphoSyntaxAnalysisRA = msa;
			return entry;
		}

		private IMoStemMsa MakeMsa(ILexEntry entry, IPartOfSpeech pos)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = pos;
			return msa;
		}

		private ILexEntryRef MakeEntryRef(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entry.EntryRefsOS.Add(form);
			return form;
		}

		private ILexPronunciation MakePronunciation(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(form);
			return form;
		}

		private ILexEtymology MakeEtymology(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOA = form;
			return form;
		}
		private IMoStemAllomorph MakeLexemeForm(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			return form;
		}

		private IMoStemAllomorph MakeAllomorph(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(form);
			return form;
		}

		private ITsString MakeAnalysisString(string input)
		{
			return Cache.TsStrFactory.MakeString(input, Cache.DefaultAnalWs);
		}
		private ITsString MakeVernString(string input)
		{
			return Cache.TsStrFactory.MakeString(input, Cache.DefaultVernWs);
		}
		private ILexEntry MakeEntry()
		{
			var entry = m_entryFactory.Create();
			return entry;
		}

		private ILexSense MakeSense(ILexEntry owningEntry)
		{
			var sense = m_senseFactory.Create();
			owningEntry.SensesOS.Add(sense);
			return sense;
		}

		private ILexSense MakeSense(ILexSense owningSense)
		{
			var sense = m_senseFactory.Create();
			owningSense.SensesOS.Add(sense);
			return sense;
		}

		ILexSense AddSenseWithHierarchyOfSubsenses(ILexEntry entry)
		{
			var sense = MakeSense(entry);
			var partOfSpeech = MakePartOfSpeech();
			var msa1 = MakeMsa(entry, partOfSpeech);
			sense.MorphoSyntaxAnalysisRA = msa1;

			var sub1 = MakeSense(sense);
			var partOfSpeech2 = MakePartOfSpeech();
			var msa2 = MakeMsa(entry, partOfSpeech2);
			sub1.MorphoSyntaxAnalysisRA = msa2;
			var sub2 = MakeSense(sense);
			sub2.MorphoSyntaxAnalysisRA = msa1;
			var sub11 = MakeSense(sub1);
			sub11.MorphoSyntaxAnalysisRA = msa2;
			return sense;
		}

		private IPartOfSpeech MakePartOfSpeech()
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			return partOfSpeech;
		}

		/// <summary>
		/// Test the virtual property.
		/// </summary>
		[Test]
		public void NumberOfSenses()
		{
			ILexEntry entry;
			ILexSense sense2, sense2_2;
			ILexSense senseInserted;
			using (var helper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				entry = m_entryFactory.Create();
				var sense = m_senseFactory.Create();
				entry.SensesOS.Add(sense);
				Assert.AreEqual(1, entry.NumberOfSensesForEntry);
				sense2 = m_senseFactory.Create();
				entry.SensesOS.Add(sense2);
				Assert.AreEqual(2, entry.NumberOfSensesForEntry);
				var sense3 = m_senseFactory.Create();
				entry.SensesOS.Add(sense3);
				Assert.AreEqual(3, entry.NumberOfSensesForEntry);

				var sense2_1 = m_senseFactory.Create();
				sense2.SensesOS.Add(sense2_1);
				Assert.AreEqual(4, entry.NumberOfSensesForEntry);

				sense2_2 = m_senseFactory.Create();
				sense2.SensesOS.Add(sense2_2);
				Assert.AreEqual(5, entry.NumberOfSensesForEntry);

				var sense2_1_1 = m_senseFactory.Create();
				sense2_1.SensesOS.Add(sense2_1_1);
				Assert.AreEqual(6, entry.NumberOfSensesForEntry);

				var sense2_2_1 = m_senseFactory.Create();
				sense2_2.SensesOS.Add(sense2_2_1);
				Assert.AreEqual(7, entry.NumberOfSensesForEntry);

				helper.RollBack = false;
			}
			m_notifiee = new Notifiee();
			IFwMetaDataCache mdc = m_sda.MetaDataCache;
			int nosFlid = mdc.GetFieldId("LexEntry", "NumberOfSensesForEntry", false);
			m_sda.AddNotification(m_notifiee);
			using (var helper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				senseInserted = m_senseFactory.Create();
				entry.SensesOS.Insert(1, senseInserted);
				Assert.AreEqual(8, entry.NumberOfSensesForEntry); // Added one top-level sense
				helper.RollBack = false;
			}
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(entry.Hvo, LexEntryTags.kflidSenses, 1, 1, 0),
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
							}, "insert second sense in entry");
			m_sda.RemoveNotification(m_notifiee);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			using (var helper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				entry.SensesOS.Remove(senseInserted);
				helper.RollBack = false;
			}
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(entry.Hvo, LexEntryTags.kflidSenses, 1, 0, 1),
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
							 }, "delete second sense in entry");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual(7, entry.NumberOfSensesForEntry);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			using (var helper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				senseInserted = m_senseFactory.Create();
				sense2.SensesOS.Insert(1, senseInserted);
				Assert.AreEqual(8, entry.NumberOfSensesForEntry); // Added a subsense.
				helper.RollBack = false;
			}
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(sense2.Hvo, LexSenseTags.kflidSenses, 1, 1, 0),
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
							 }, "insert subsense in sense");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual(8, entry.NumberOfSensesForEntry);

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
			using (var helper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				sense2.SensesOS.Remove(senseInserted);
				helper.RollBack = false;
			}
			m_notifiee.CheckChangesWeaker(new[]
							 {
								 new ChangeInformationTest(sense2.Hvo, LexSenseTags.kflidSenses, 1, 0, 1),
								 new ChangeInformationTest(entry.Hvo, nosFlid, 0, 0, 0),
							 }, "remove subsense from sense");
			m_sda.RemoveNotification(m_notifiee);
			Assert.AreEqual(7, entry.NumberOfSensesForEntry);
		}
		/// <summary>
		/// Test the virtual property.
		/// </summary>
		[Test]
		public void HomographRenumbering()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				int vernWs = Cache.DefaultVernWs;
				var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var formFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
				var ldb = Cache.LangProject.LexDbOA;
				var tsf = Cache.TsStrFactory;
				var morphTypeRoot =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
				var entry1 = entryFactory.Create();
				var morph1 = formFactory.Create();
				entry1.LexemeFormOA = morph1;
				morph1.Form.set_String(vernWs, tsf.MakeString("rot", vernWs));

				Assert.AreEqual(0, entry1.HomographNumber, "sole entry should have homograph number zero.");

				var entry2 = entryFactory.Create();
				var morph2 = formFactory.Create();
				entry2.LexemeFormOA = morph2;
				morph2.Form.set_String(vernWs, tsf.MakeString("rot", vernWs));
				Assert.AreEqual(1, entry1.HomographNumber,
					"old entry with duplicate LF should have homograph number one.");
				Assert.AreEqual(2, entry2.HomographNumber,
					"new entry with duplicate LF should have homograph number one.");

				var entry3 = entryFactory.Create();
				var morph3 = formFactory.Create();
				entry3.LexemeFormOA = morph3;
				morph3.Form.set_String(vernWs, tsf.MakeString("blau", vernWs));
				Assert.AreEqual(0, entry3.HomographNumber, "new entry with unique LF should have homograph number zero.");
				Assert.AreEqual(1, entry1.HomographNumber,
					"old entry with duplicate LF should have homograph number one (unchanged).");
				Assert.AreEqual(2, entry2.HomographNumber,
					"new entry with duplicate LF should have homograph number one (unchanged).");

				// Since citation form takes precedence, changing this should make it a homograph.
				entry3.CitationForm.set_String(vernWs, tsf.MakeString("rot", vernWs));
				Assert.AreEqual(1, entry1.HomographNumber,
					"old entry with duplicate LF should have homograph number one (unchanged).");
				Assert.AreEqual(2, entry2.HomographNumber,
					"new entry with duplicate LF should have homograph number one (unchanged).");
				Assert.AreEqual(3, entry3.HomographNumber, "entry with changed CF should have homograph number three.");

				var entry4 = entryFactory.Create();
				var morph4 = formFactory.Create();
				entry4.LexemeFormOA = morph4;
				morph4.Form.set_String(vernWs, tsf.MakeString("blau", vernWs));
				Assert.AreEqual(0, entry4.HomographNumber,
					"new entry is not a homograph even though LF matches, since CF of other does not.");

				// Changing the CF of entry 1 should make it no longer a homograph of 'rot' but now a homograph of 'blau'.
				entry1.CitationForm.set_String(vernWs, tsf.MakeString("blau", vernWs));
				Assert.AreEqual(2, entry1.HomographNumber, "changed entry is now a homograph of blau).");
				Assert.AreEqual(1, entry2.HomographNumber,
					"old homograph 2 is now homograph 1, since old H1 is no longer 'rot').");
				Assert.AreEqual(2, entry3.HomographNumber, "old homograph 3 drops to 2 with loss of old first one.");
				Assert.AreEqual(1, entry4.HomographNumber, "old blau now has a homograph.");

				// pathologically, the HomographForm may be taken from the first allomorph.
				var entry5 = entryFactory.Create();
				var morph5 = formFactory.Create();
				entry5.LexemeFormOA = morph5;
				var morph5A = formFactory.Create();
				entry5.AlternateFormsOS.Add(morph5A);
				morph5A.Form.set_String(vernWs, tsf.MakeString("blau", vernWs));
				Assert.AreEqual(3, entry5.HomographNumber, "new blau homograph based on alternate form.");

				// At this point we have entry 2(rot1), entry3(rot2), entry1(blau1), entry4(blau2), and entry5(blau3).
				var entry6 = entryFactory.Create();
				var morph6 = formFactory.Create();
				entry6.LexemeFormOA = morph6;
				morph6.Form.set_String(vernWs, tsf.MakeString("blau", vernWs));
				Assert.AreEqual(4, entry6.HomographNumber, "one more blau homograph.");

				// Try changing the lexeme form to affect things.
				morph5.Form.set_String(vernWs, tsf.MakeString("grun", vernWs));
				Assert.AreEqual(0, entry5.HomographNumber, "changing lexeme form makes it no longer a homograph.");
				Assert.AreEqual(3, entry6.HomographNumber, "last blau homograph reduces number.");

				// At this point we have entry 2(rot1), entry3(rot2), entry1(blau1), entry4(blau2), and entry6(blau3), and entry5(grun0).
				entry4.Delete();
				Assert.AreEqual(1, entry1.HomographNumber, "deleting second homograph does not affect first.");
				Assert.AreEqual(2, entry6.HomographNumber, "deleting homograph renumbers subsequent ones.");

				entry6.Delete();
				Assert.AreEqual(0, entry1.HomographNumber, "deleting second (and last) homograph changes first to zero.");

				entry2.Delete();
				Assert.AreEqual(0, entry3.HomographNumber, "deleting first of two homographs changes second to zero.");

				helper.RollBack = false; // hopefully all our changes succeeded.
			}
		}

		/// <summary>
		/// This test ensures that when an Allomorph is created it has by default the morphType of
		/// the LexEntry's LexemeForm.
		/// </summary>
		[Test]
		public void AllomorphsHaveCorrectMorphTypes()
		{
			ILexEntry oldEntry = null;
			ILexSense complexSense = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
										() =>
											{
												oldEntry = MakeEntryWithLexemeFormAndAlternateForm();
											});
			Assert.AreEqual(oldEntry.LexemeFormOA.MorphTypeRA, oldEntry.AlternateFormsOS[0].MorphTypeRA, "The morphType of the allomorph should match that of the Lexical Entry.");
			Assert.AreNotEqual(oldEntry.LexemeFormOA.MorphTypeRA, oldEntry.AlternateFormsOS[1].MorphTypeRA, "The morphType of the allomorph should match that of the Lexical Entry.");
		}

		//Create a LexEntry with two Allomorphs. One we specifically set the morphtype to something
		//other than that of the LexEntry LexemeForm
		ILexEntry MakeEntryWithLexemeFormAndAlternateForm()
		{
			ILexEntry entry = MakeEntry();

			var form = MakeLexemeForm(entry);
			form.Form.VernacularDefaultWritingSystem = MakeVernString("lf");

			var morphTypeRep = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			form.MorphTypeRA = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphStem);

			var alternate1 = MakeAllomorph(entry);
			alternate1.Form.VernacularDefaultWritingSystem = MakeVernString("alt1");
			var alternate2 = MakeAllomorph(entry);
			alternate2.Form.VernacularDefaultWritingSystem = MakeVernString("alt2");
			alternate2.MorphTypeRA = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphSuffix);
			return entry;
		}

		/// <summary>
		/// Test the indicated method.
		/// </summary>
		[Test]
		public void FullSortKey()
		{
			LexEntry entry = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry = (LexEntry) MakeEntryWithForm("kick"));
			Assert.That(entry.FullSortKey(false, Cache.DefaultVernWs), Is.EqualTo("kick"));
			LexEntry entry2 = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry2 = (LexEntry) MakeEntryWithForm("kick"));
			Assert.That(entry.FullSortKey(false, Cache.DefaultVernWs), Is.EqualTo("kick 00000000001"));
			Assert.That(entry2.FullSortKey(false, Cache.DefaultVernWs), Is.EqualTo("kick 00000000002"));

			var morphTypeRep = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry.LexemeFormOA.MorphTypeRA = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphSuffix));
			// The number comes from the homograph number plus 1024 times the morph type index (6x1024).
			Assert.That(entry.FullSortKey(false, Cache.DefaultVernWs), Is.EqualTo("kick 00000006145"));
		}

		private ILexEntry MakeEntryWithForm(string form)
		{
			var entry = MakeEntry();
			var lexform = MakeLexemeForm(entry);
			lexform.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			return entry;
		}

		/// <summary>
		/// Tests the various paths through the indicated method.
		/// </summary>
		[Test]
		public void ReplaceObsoleteMsas()
		{
			LexEntry entry = null;
			IMoStemMsa msa1 = null;
			IPartOfSpeech pos1 = null;
			ILexSense sense1 = null;
			IWfiWordform wf = null;
			IWfiAnalysis anal = null;
			IWfiMorphBundle wmb = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
				{ entry = (LexEntry) MakeEntryWithForm("in");
					pos1 = MakePartOfSpeech();
					msa1 = MakeMsa(entry, pos1);
					sense1 = MakeSense(entry);
					sense1.MorphoSyntaxAnalysisRA = msa1;
					var servLoc = Cache.ServiceLocator;
					wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
					anal = servLoc.GetInstance<IWfiAnalysisFactory>().Create();
					wf.AnalysesOC.Add(anal);
					wmb = servLoc.GetInstance<IWfiMorphBundleFactory>().Create();
					anal.MorphBundlesOS.Add(wmb);
					wmb.MsaRA = msa1;
				});
			var oldMsas = new List<IMoMorphSynAnalysis>();
			oldMsas.Add(msa1);

			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry.ReplaceObsoleteMsas(oldMsas));
			Assert.That(msa1.IsValidObject, Is.False, "old MSA should be deleted by ReplaceObsoleteMsas");
			var msaAffix = entry.MorphoSyntaxAnalysesOC.First() as MoUnclassifiedAffixMsa;
			Assert.That(msaAffix, Is.Not.Null, "should have made one unclassified MSA");
			Assert.That(msaAffix.PartOfSpeechRA, Is.EqualTo(pos1),
				"Should have copied the POS from the stem MSA to the affix one");
			Assert.That(sense1.MorphoSyntaxAnalysisRA, Is.EqualTo(msaAffix), "should have adjusted sense MSA");
			Assert.That(wmb.MsaRA, Is.EqualTo(msaAffix), "should have fixed morph bundle reference");

			// Now flip it back, to test the opposite normal path.
			oldMsas[0] = msaAffix;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry.ReplaceObsoleteMsas(oldMsas));
			Assert.That(msaAffix.IsValidObject, Is.False, "old MSA should be deleted by ReplaceObsoleteMsas");
			var msaStem = entry.MorphoSyntaxAnalysesOC.First() as IMoStemMsa;
			Assert.That(msaStem, Is.Not.Null, "should have made one stem MSA");
			Assert.That(msaStem.PartOfSpeechRA, Is.EqualTo(pos1),
				"Should have copied the POS from the affix MSA to the stem one");
			Assert.That(sense1.MorphoSyntaxAnalysisRA, Is.EqualTo(msaStem), "should have adjusted sense MSA");
			Assert.That(wmb.MsaRA, Is.EqualTo(msaStem), "should have fixed morph bundle reference");

			// Now see if it works when there's no POS
			oldMsas[0] = msaStem;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
					{
						msaStem.PartOfSpeechRA = null;
						entry.ReplaceObsoleteMsas(oldMsas);
					});
			Assert.That(msaStem.IsValidObject, Is.False, "old MSA should be deleted by ReplaceObsoleteMsas");
			msaAffix = entry.MorphoSyntaxAnalysesOC.First() as MoUnclassifiedAffixMsa;
			Assert.That(msaAffix, Is.Not.Null, "should have made one unclassified MSA");
			Assert.That(msaAffix.PartOfSpeechRA, Is.Null, "Should have left the affix POS null");
			Assert.That(sense1.MorphoSyntaxAnalysisRA, Is.EqualTo(msaAffix), "should have adjusted sense MSA");
			Assert.That(wmb.MsaRA, Is.EqualTo(msaAffix), "should have fixed morph bundle reference");

			// And the reverse path with no POS
			oldMsas[0] = msaAffix;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry.ReplaceObsoleteMsas(oldMsas));
			Assert.That(msaAffix.IsValidObject, Is.False, "old MSA should be deleted by ReplaceObsoleteMsas");
			msaStem = entry.MorphoSyntaxAnalysesOC.First() as IMoStemMsa;
			Assert.That(msaStem, Is.Not.Null, "should have made one stem MSA");
			Assert.That(msaStem.PartOfSpeechRA, Is.Null, "Should have left POS on stem MSA null");
			Assert.That(sense1.MorphoSyntaxAnalysisRA, Is.EqualTo(msaStem), "should have adjusted sense MSA");
			Assert.That(wmb.MsaRA, Is.EqualTo(msaStem), "should have fixed morph bundle reference");

			// Enhance JohnT: the above tests only the most basic paths through this complex group of methods.
			// I was fixing a crash that happened when POS was null.
			// Ideally should also test cases where there is already a suitable MSA to reuse.
			// Also (many) cases where there is another MSA available, but for various reasons
			// (about 7 options) it is not suitable.
			// Also about four varieties of affix MSA can be converted to stem.

		}
	}
}
