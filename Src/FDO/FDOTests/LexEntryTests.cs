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
using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
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
		private ICmPossibilityFactory m_possFact;
		private ICmPossibilityRepository m_possRepo;
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
			m_possFact = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			m_possRepo = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
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
		/// Test (incomplete as yet) of the MlHeadword property.
		/// </summary>
		[Test]
		public void MlHeadword()
		{
			LexEntry bank1 = null;
			LexEntry past = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					bank1 = (LexEntry)MakeEntryWithForm("bank");
					past = (LexEntry)MakeAffixWithForm("ed", MoMorphTypeTags.kguidMorphSuffix);
					var pos1 = MakePartOfSpeech();
					var msa = MakeAffixMsa(past, pos1, pos1);
				});
			Assert.That(bank1.MLHeadWord.VernacularDefaultWritingSystem.Text, Is.EqualTo("bank"));
			Assert.That(past.MLHeadWord.VernacularDefaultWritingSystem.Text, Is.EqualTo("-ed"));
			Assert.That(bank1.MLHeadWord.AnalysisDefaultWritingSystem.Text, Is.Null);
			Assert.That(past.MLHeadWord.AnalysisDefaultWritingSystem.Text, Is.Null,
				"where all forms are empty, no affix marker");
			LexEntry bank2 = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					bank2 = (LexEntry)MakeEntryWithForm("bank");
				});
			Assert.That(bank1.MLHeadWord.VernacularDefaultWritingSystem.Text, Is.EqualTo("bank1"), "homograph # part of name");
			Assert.That(bank1.MLHeadWord.AnalysisDefaultWritingSystem.Text, Is.Null,
				"homograph # not shown for empty alternative");
		}

		/// <summary>
		/// Tests many aspects of LexEntry.MoveSenseToCopy.
		/// </summary>
		[Test]
		public void MorphBundleRefsFixedWhenSenseMovedToNewEntry()
		{
			// All these null assignments are redundant, but the compiler does not know that the Do block will be executed.
			ILexEntry bankN = null;
			ILexEntry bankV = null;
			ILexSense bankMoney = null;
			ILexSense bankRiver = null;
			ILexSense bankTilt = null;
			ILexSense bankCreek = null;
			ILexSense bankStream = null;
			IPartOfSpeech posNoun = null;
			IPartOfSpeech posVerb = null;
			IPartOfSpeech posCommonNoun = null;
			IMoMorphSynAnalysis msaVerb = null;
			IMoMorphSynAnalysis msaNoun = null;
			IMoMorphSynAnalysis msaCommonNoun = null;
			IWfiWordform wfBank = null;
			IWfiAnalysis waBankMoney = null;
			IWfiAnalysis waBankRiver = null;
			IWfiAnalysis waBankTilt = null;
			IWfiAnalysis waBankCreek = null;
			IWfiAnalysis waBankStream = null;
			IMoForm baank = null;
			IMoForm baaank = null;

			UndoableUnitOfWorkHelper.Do("undoit", "doit", Cache.ActionHandlerAccessor,
				() =>
				{
					if (Cache.LangProject.PartsOfSpeechOA == null)
						Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
					posVerb = MakePartOfSpeech("Verb");
					posNoun = MakePartOfSpeech("Noun");
					posCommonNoun = MakePartOfSpeech("Common Noun"); // better perhaps to make owned by Noun, but doesn't affect this test

					bankN = MakeEntry("bank", "money", posNoun);
					bankV = MakeEntry("bank", "Tilt", posVerb);
					bankMoney = bankN.SensesOS[0];
					bankTilt = bankV.SensesOS[0];
					msaNoun = bankMoney.MorphoSyntaxAnalysisRA;
					bankRiver = MakeSense(bankN, "river");
					bankRiver.MorphoSyntaxAnalysisRA = msaNoun;
					bankCreek = MakeSenseWithNewMsa(bankRiver, "creek", posCommonNoun);
					bankStream = MakeSense(bankRiver, "stream");
					bankStream.MorphoSyntaxAnalysisRA = msaNoun;

					msaVerb = bankTilt.MorphoSyntaxAnalysisRA;
					msaCommonNoun = bankCreek.MorphoSyntaxAnalysisRA;

					baank = MakeAllomorph(bankN, "baank");
					baaank = MakeAllomorph(bankN, "baaank");

					// Todo:  possibly fill in more fields of bankN; move bankCreek to new entry; verify
					wfBank = MakeWordform("bank");
					waBankCreek = MakeAnalysis(wfBank, bankCreek);
					waBankRiver = MakeAnalysis(wfBank, bankRiver);
					waBankTilt = MakeAnalysis(wfBank, bankTilt);
					waBankMoney = MakeAnalysis(wfBank, bankMoney);
					waBankStream = MakeAnalysis(wfBank, bankStream);
					waBankStream.MorphBundlesOS[0].MorphRA = baaank;

					bankN.CitationForm.VernacularDefaultWritingSystem = MakeVernString("cf");
					bankN.Bibliography.AnalysisDefaultWritingSystem = MakeAnalysisString("biblio");
					bankN.Comment.AnalysisDefaultWritingSystem = MakeAnalysisString("comment");

				});
			// The task we're testing.
			bankN.MoveSenseToCopy(bankRiver);

			var newEntry = bankRiver.Entry;
			Assert.That(newEntry, Is.Not.EqualTo(bankN), "moved sense should have new owner");
			Assert.That(bankN.SensesOS.Count, Is.EqualTo(1), "old entry should have lost a sense");
			Assert.That(bankN.SensesOS[0], Is.EqualTo(bankMoney), "remaining sense should be the one that was not moved");
			Assert.That(bankRiver.SensesOS[0], Is.EqualTo(bankCreek), "the subsense should have moved too");
			Assert.That(bankN.MorphoSyntaxAnalysesOC.Count, Is.EqualTo(1), "the old entry should now only need one MSA");
			Assert.That(bankN.MorphoSyntaxAnalysesOC.First(), Is.EqualTo(msaNoun), "the old entry should keep the Noun MSA");

			var msaNewNoun = bankRiver.MorphoSyntaxAnalysisRA as MoStemMsa;
			Assert.That(msaNewNoun, Is.Not.EqualTo(msaNoun), "the moved sense should not be pointing to the old MSA");
			Assert.That(msaNewNoun.Owner, Is.EqualTo(newEntry), "River's msa's owner shoud be the right entry");
			Assert.That(msaNewNoun.PartOfSpeechRA, Is.EqualTo(posNoun), "the new MSA for River should point at the POS for Noun");

			var msaNewCommon = bankCreek.MorphoSyntaxAnalysisRA as MoStemMsa;
			Assert.That(msaNewCommon.PartOfSpeechRA, Is.EqualTo(posCommonNoun), "RiverCreek should have a corresponding MSA");
			Assert.That(msaNewCommon.Owner, Is.EqualTo(newEntry), "RiverCreek's msa's owner shoud be the right entry");

			var mbBankRiver = waBankRiver.MorphBundlesOS[0];
			Assert.That(mbBankRiver.MsaRA, Is.EqualTo(msaNewNoun), "The MSA for the river analysis should be the new one");
			Assert.That(mbBankRiver.MorphRA, Is.EqualTo(newEntry.LexemeFormOA), "The morph for the river analysis should be the new one");

			var mbBankCreek = waBankCreek.MorphBundlesOS[0];
			Assert.That(mbBankCreek.MsaRA, Is.EqualTo(msaNewCommon), "The MSA for the creek analysis should be the new one");
			Assert.That(mbBankCreek.MorphRA, Is.EqualTo(newEntry.LexemeFormOA), "The morph for the creek analysis should be the new one");

			var mbBankStream = waBankStream.MorphBundlesOS[0];
			Assert.That(mbBankStream.MsaRA, Is.EqualTo(msaNewNoun), "The MSA for the stream analysis should be the new one");
			Assert.That(mbBankStream.MorphRA, Is.EqualTo(newEntry.AlternateFormsOS[1]), "The morph for the stream analysis should be the new allomorph");
		}

		IWfiWordform MakeWordform(string form)
		{
			var result = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			result.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			return result;
		}
		IWfiAnalysis MakeAnalysis(IWfiWordform wf, ILexSense sense)
		{
			var result = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(result);
			var mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			result.MorphBundlesOS.Add(mb);
			mb.SenseRA = sense;
			mb.MsaRA = sense.MorphoSyntaxAnalysisRA;
			mb.MorphRA = sense.Entry.LexemeFormOA;
			return result;
		}

		/// <summary>
		/// Test PrimaryEntryRoots and the closely related NonTrivialEntryRoots.
		/// </summary>
		[Test]
		public void LexEntryRef_PrimaryEntryRoots()
		{
			ILexEntry star = null;
			ILexEntry allStar = null;
			ILexEntry allStarCast = null;
			ILexEntry cast = null;
			ILexEntryRef allStarComponents = null;
			ILexEntryRef allStarCastComponents = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					star = MakeEntry("star", "great performer");
					cast = MakeEntry("cast", "performers");
					allStar = MakeEntry("all-star", "everyone great");
					allStarCast = MakeEntry("all-star cast", "group of great performers");
					allStarComponents = MakeEntryRef(allStar);
					allStarComponents.RefType = LexEntryRefTags.krtComplexForm;
				});
			Assert.That(allStarComponents.PrimaryEntryRoots, Is.Empty);
			Assert.That(allStarComponents.NonTrivialEntryRoots, Is.Empty);

			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarComponents.ComponentLexemesRS.Add(star);
					allStarComponents.PrimaryLexemesRS.Add(star);
				});
			Assert.That(allStarComponents.PrimaryEntryRoots.Count(), Is.EqualTo(1));
			Assert.That(allStarComponents.PrimaryEntryRoots.FirstOrDefault(), Is.EqualTo(star));
			Assert.That(allStarComponents.NonTrivialEntryRoots, Is.Empty); // The one primary entry is 'trivial', matching the only component.

			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarCastComponents = MakeEntryRef(allStarCast);
					allStarCastComponents.RefType = LexEntryRefTags.krtComplexForm;
					allStarCastComponents.ComponentLexemesRS.Add(allStar.SensesOS[0]);
					allStarCastComponents.PrimaryLexemesRS.Add(allStar.SensesOS[0]);
					allStarCastComponents.ComponentLexemesRS.Add(cast.SensesOS[0]);
					allStarCastComponents.PrimaryLexemesRS.Add(cast.SensesOS[0]);
				});
			Assert.That(allStarCastComponents.PrimaryEntryRoots.Count(), Is.EqualTo(2)); // sense of alStar, sense of cast
			Assert.That(allStarCastComponents.PrimaryEntryRoots.FirstOrDefault(), Is.EqualTo(star)); // indirects through the sense to all-star, then to its root
			Assert.That(allStarCastComponents.PrimaryEntryRoots.Skip(1).FirstOrDefault(), Is.EqualTo(cast)); // simple indirection through sense
			Assert.That(allStarCastComponents.NonTrivialEntryRoots.FirstOrDefault(), Is.EqualTo(star)); // indirection makes components and PERs different
			Assert.That(allStarCastComponents.NonTrivialEntryRoots.Skip(1).FirstOrDefault(), Is.EqualTo(cast));
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarCastComponents.PrimaryLexemesRS.Remove(allStar.SensesOS[0]);
					allStarCastComponents.ComponentLexemesRS.Add(allStar);
					allStarCastComponents.PrimaryLexemesRS.Add(allStar);
				});
			Assert.That(allStarCastComponents.PrimaryEntryRoots.Count(), Is.EqualTo(2)); // sense of cast, entry of allStar
			Assert.That(allStarCastComponents.PrimaryEntryRoots.Skip(1).FirstOrDefault(), Is.EqualTo(star)); // indirection through entry
			Assert.That(allStarCastComponents.NonTrivialEntryRoots.Skip(1).FirstOrDefault(), Is.EqualTo(star)); // now actually 3 components
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarComponents.PrimaryLexemesRS.Clear();
					allStarComponents.ComponentLexemesRS.Clear();
					allStarComponents.ComponentLexemesRS.Add(star.SensesOS[0]);
					allStarComponents.PrimaryLexemesRS.Add(star.SensesOS[0]);
				});
			Assert.That(allStarComponents.PrimaryEntryRoots.Count(), Is.EqualTo(1)); // sense of star
			Assert.That(allStarComponents.PrimaryEntryRoots.FirstOrDefault(), Is.EqualTo(star));
			Assert.That(allStarComponents.NonTrivialEntryRoots, Is.Empty); // the only PrimaryRoot is the owner of the sense that is the only component.

			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarComponents.ComponentLexemesRS.Add(cast.SensesOS[0]);
					allStarComponents.PrimaryLexemesRS.Add(cast.SensesOS[0]);
				});
			Assert.That(allStarComponents.PrimaryEntryRoots.Count(), Is.EqualTo(2)); // sense of star and cast
			Assert.That(allStarComponents.PrimaryEntryRoots.FirstOrDefault(), Is.EqualTo(star));
			Assert.That(allStarComponents.NonTrivialEntryRoots, Is.Empty); // We can suppress two when they are both the same as the corresponding component.

			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					var kick = MakeEntry("kick", "strike with foot"); // ran out of even vaguely sane ideas.
					allStarComponents.ComponentLexemesRS.Add(kick.SensesOS[0]);
				});

			Assert.That(allStarComponents.PrimaryEntryRoots.Count(), Is.EqualTo(2)); // sense of star and cast, entry star (from allStar) is not primary
			Assert.That(allStarComponents.PrimaryEntryRoots.FirstOrDefault(), Is.EqualTo(star));
			Assert.That(allStarComponents.NonTrivialEntryRoots.Count(), Is.EqualTo(2)); // No suppression
		}

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void AddToPrimaryLexemesAddsToVisibleComplexForms()
		{
			ILexEntry star = null;
			ILexEntry all = null;
			ILexEntry allStar = null;
			ILexEntryRef allStarComponents = null;
			ILexEntryRef allStarCastComponents = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					star = MakeEntry("star", "great performer");
					all = MakeEntry("all", "every");
					allStar = MakeEntry("all-star", "everyone great");
					allStarComponents = MakeEntryRef(allStar);
					allStarComponents.RefType = LexEntryRefTags.krtComplexForm;
				});
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarComponents.PrimaryLexemesRS.Add(all);
				});
			Assert.That(allStarComponents.ShowComplexFormsInRS[0], Is.EqualTo(all));
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					allStarComponents.PrimaryLexemesRS.Add(star);
				});
			Assert.That(allStarComponents.ShowComplexFormsInRS[0], Is.EqualTo(all));
			Assert.That(allStarComponents.ShowComplexFormsInRS[1], Is.EqualTo(star));
		}

		/// <summary>
		/// Tests adding a component to an entry that doesn't have any components.
		/// </summary>
		[Test]
		public void AddComponentWhenEmpty()
		{
			ILexEntry component = null;
			ILexEntry complex = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				()=>
					{
						component = MakeEntryWithForm("kick");
						complex = MakeEntryWithForm("kick the bucket");
						complex.LexemeFormOA.MorphTypeRA =
							Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
						complex.AddComponent(component);
					});
			Assert.That(complex.EntryRefsOS.Count, Is.EqualTo(1));
			var entryRef = complex.EntryRefsOS[0];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComponentLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.ComponentLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0)); // LT-10928
			Assert.That(complex.LexemeFormOA.MorphTypeRA, Is.EqualTo(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem)),
				"a complex form cannot be a root");
		}

		/// <summary>
		/// Tests adding a component to an entry that already has it.
		/// </summary>
		[Test]
		public void AddComponentWhenAlreadyPresent()
		{
			ILexEntry component = null;
			ILexEntry complex = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					component = MakeEntryWithForm("kick");
					complex = MakeEntryWithForm("kick the bucket");
					complex.LexemeFormOA.MorphTypeRA =
						Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
					complex.AddComponent(component);
					complex.AddComponent(component);
				});
			Assert.That(complex.EntryRefsOS.Count, Is.EqualTo(1));
			var entryRef = complex.EntryRefsOS[0];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComponentLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.ComponentLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0));  // LT-10928
			Assert.That(complex.LexemeFormOA.MorphTypeRA, Is.EqualTo(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem)),
				"a complex form cannot be a root");
		}

		/// <summary>
		/// Tests adding a component to an entry that already has components.
		/// </summary>
		[Test]
		public void AddComponentWhenNotEmpty()
		{
			ILexEntry oldComponent = null;
			ILexEntry component = null;
			ILexEntry complex = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					oldComponent = MakeEntryWithForm("kick");
					component = MakeEntryWithForm("the");
					complex = MakeEntryWithForm("kick the bucket");
					complex.AddComponent(oldComponent);
					complex.EntryRefsOS[0].HideMinorEntry = 0;
					complex.AddComponent(component);
				});
			Assert.That(complex.EntryRefsOS.Count, Is.EqualTo(1));
			var entryRef = complex.EntryRefsOS[0];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComponentLexemesRS.Count, Is.EqualTo(2));
			Assert.That(entryRef.ComponentLexemesRS[0], Is.EqualTo(oldComponent));
			Assert.That(entryRef.ComponentLexemesRS[1], Is.EqualTo(component));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1)); // unchanged, since it already had one.
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(oldComponent));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0));
		}

		/// <summary>
		/// Tests the method that determines what displays in the Complex Forms field of an entry.
		/// This should depend on the Components field of LexEntry, NOT the PrimaryLexemes field.
		/// </summary>
		[Test]
		public void ComplexFormRefsWithComponent()
		{
			ILexEntry oldComponent = null;
			ILexEntry component = null;
			ILexEntry complex = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					oldComponent = MakeEntryWithForm("dog");
					component = MakeEntryWithForm("food");
					complex = MakeEntryWithForm("dogfood");
					complex.AddComponent(oldComponent);
					complex.EntryRefsOS[0].HideMinorEntry = 0;
					complex.AddComponent(component);
				});
			Assert.That(oldComponent.ComplexFormEntries.Count(), Is.EqualTo(1));
			Assert.That(component.ComplexFormEntries.Count(), Is.EqualTo(1));
			var entryRef = complex.EntryRefsOS[0];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComponentLexemesRS.Count, Is.EqualTo(2));
			Assert.That(entryRef.ComponentLexemesRS[0], Is.EqualTo(oldComponent));
			Assert.That(entryRef.ComponentLexemesRS[1], Is.EqualTo(component));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1)); // unchanged, since it already had one.
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(oldComponent));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0));
		}

		/// <summary>
		/// Reordering components should not clear PrimaryLexemes (e.g., as a side effect of deleting before re-inserting).
		/// </summary>
		[Test]
		public void ReorderComponentsDoesNotClearPrimaryLexemes()
		{
			ILexEntry kick = null;
			ILexEntry bucket = null;
			ILexEntry kickBucket = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					kick = MakeEntryWithForm("kick");
					bucket = MakeEntryWithForm("bucket");
					kickBucket = MakeEntryWithForm("kick the bucket");
					kickBucket.AddComponent(kick);
					kickBucket.AddComponent(bucket);
				});
			var entryRef = kickBucket.EntryRefsOS[0];
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(kick));
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() => entryRef.ComponentLexemesRS.Replace(0, 2, new [] {bucket, kick}));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1)); // unchanged, since it already had one.
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() => entryRef.ComponentLexemesRS.Replace(0, 2, new[] { bucket }));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(0)); // but now we really removed it from components.
		}

		/// <summary>
		/// Tests adding a component to an entry that doesn't have any components but is already a variant.
		/// </summary>
		[Test]
		public void AddComponentToVariant()
		{
			ILexEntry component = null;
			ILexEntry complex = null;
			ILexEntry variant = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
				{
					component = MakeEntryWithForm("kick");
					complex = MakeEntryWithForm("kick the bucket");
					variant = MakeEntryWithForm("kicked the bucket");
					complex.MakeVariantOf(variant, GetVariantTypeOrCreateOne("tense"));
					complex.LexemeFormOA.MorphTypeRA =
						Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
					complex.AddComponent(component);
				});
			Assert.That(complex.EntryRefsOS.Count, Is.EqualTo(2));
			var entryRef = complex.EntryRefsOS[1];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComponentLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.ComponentLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.PrimaryLexemesRS.Count, Is.EqualTo(1));
			Assert.That(entryRef.PrimaryLexemesRS[0], Is.EqualTo(component));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0));
			Assert.That(complex.LexemeFormOA.MorphTypeRA, Is.EqualTo(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem)),
				"a complex form cannot be a root");
		}

		/// <summary>
		/// Will find a variant entry type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the variant entry type in the Lexicon VariantEntryTypes list.
		/// </summary>
		/// <param name="variantTypeName"></param>
		/// <returns></returns>
		protected ILexEntryType GetVariantTypeOrCreateOne(string variantTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			var poss = m_possRepo.AllInstances().Where(
				someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == variantTypeName).FirstOrDefault();
			if (poss != null)
				return poss as ILexEntryType;
			// shouldn't get past here; they're already defined.
			var owningList = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.IsNotNull(owningList, "No VariantEntryTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, variantTypeName);
			return poss as ILexEntryType;
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
			Assert.That(newEntry.ShowMainEntryIn, Has.Count.EqualTo(1));
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
			var pub = MakeShowMainEntryInPub(entry);
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

		private ICmPossibility MakeShowMainEntryInPub(ILexEntry entry)
		{
			var publication = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			entry.ShowMainEntryIn.Add(publication);
			return publication;
		}

		private IMoStemMsa MakeMsa(ILexEntry entry, IPartOfSpeech pos)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = pos;
			return msa;
		}

		private IMoDerivAffMsa MakeAffixMsa(ILexEntry entry, IPartOfSpeech fromPos, IPartOfSpeech toPos)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.FromPartOfSpeechRA = fromPos;
			msa.ToPartOfSpeechRA = toPos;
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

		private IMoAffixAllomorph MakeAffixForm(ILexEntry entry, Guid morphType)
		{
			var form = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphType);
			return form;
		}

		private IMoStemAllomorph MakeAllomorph(ILexEntry entry, string form)
		{
			var result = MakeAllomorph(entry);
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);
			return result;
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

		private IPartOfSpeech MakePartOfSpeech(string name)
		{
			var partOfSpeech = MakePartOfSpeech();
			partOfSpeech.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(name, Cache.DefaultAnalWs);
			return partOfSpeech;
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
					"new entry with duplicate LF should have homograph number two.");

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
		/// If homographs have been given a non-standard set of numbers manually or by an import process,
		/// and a new one is added, it should be given the first unused number.
		/// </summary>
		[Test]
		public void CanInsertHomographIntoNonStandardSequence()
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
					{
						var morphTypeStem =
							Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
						// Where the previous state should have had HN 0
						var bankRiver = MakeStemEntry("bank", morphTypeStem);
						bankRiver.HomographNumber = 2;
						var bankMoney = MakeStemEntry("bank", morphTypeStem);
						Assert.That(bankMoney.HomographNumber, Is.EqualTo(1));
						Assert.That(bankRiver.HomographNumber, Is.EqualTo(2));

						// Insert in the middle of a sequence
						bankRiver.HomographNumber = 3;
						var bankPlane = MakeStemEntry("bank", morphTypeStem);
						Assert.That(bankMoney.HomographNumber, Is.EqualTo(1));
						Assert.That(bankPlane.HomographNumber, Is.EqualTo(2));
						Assert.That(bankRiver.HomographNumber, Is.EqualTo(3));

						// Insert at the start of the sequence.
						// Note that there is still a gap, and this is not corrected.
						// (For example: might be importing SFM with explicit HNs, and the larger numbers occurring earlier.
						// We don't want to renumber 4 and 5 when we import another.)
						bankMoney.HomographNumber = 4;
						bankPlane.HomographNumber = 5;
						var bankRelyOn = MakeStemEntry("bank", morphTypeStem);
						Assert.That(bankRelyOn.HomographNumber, Is.EqualTo(1)); // #1 was available
						Assert.That(bankMoney.HomographNumber, Is.EqualTo(4)); // last value set unchanged
						Assert.That(bankPlane.HomographNumber, Is.EqualTo(5)); // last value set unchanged
						Assert.That(bankRiver.HomographNumber, Is.EqualTo(3)); // last value set unchanged
					});
		}


		/// <summary>
		/// Inspired by LT-13615 - tests homograph renumbering following a change in lex forms with multiple ws.
		/// This test does not change the homograph ws when the vernacular ws changes.
		/// </summary>
		[Test]
		public void HomographsWithChangingVernWs()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				Cache.LanguageProject.VernacularWritingSystems.Clear();
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Clear();
				var frWs = Cache.WritingSystemFactory.get_Engine("fr");
				var spWs = Cache.WritingSystemFactory.get_Engine("es");
				Cache.LanguageProject.DefaultVernacularWritingSystem = (IWritingSystem)spWs;
				Cache.LanguageProject.DefaultVernacularWritingSystem = (IWritingSystem)frWs; // {fr es} order
				int frWsId = frWs.Handle;
				int spWsId = spWs.Handle;

				// ws order is "fr es"
				var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var morphTypeRoot = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphRoot);

				// 4) Switch to 2nd vWs: 2 hw with multi vWs lf with 1st vWs empty and 2nd with same forms -> hn 0 + 0 -> 1 + 2
				var lex1 = MakeStemEntryMultiWs("", frWsId, "k", spWsId, morphTypeRoot);
				var lex2 = MakeStemEntryMultiWs("", frWsId, "k", spWsId, morphTypeRoot);
				Assert.AreEqual(0, lex1.HomographNumber,
								"4) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"4) second entry should have homograph number 0.");
				SwapVernWs(false); // ws order is now "es fr", but hgws is still "fr"
				Assert.AreEqual(0, lex1.HomographNumber,
								"4) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"4) second entry should have homograph number 0.");

				// 5) Switch to 1st vWs: 2 hw with multi vWs lf with 1st vWs empty and 2nd with same forms -> hn 1 + 2 -> 0 + 0
				SwapVernWs(false); // ws order is now "fr es", and hgws is still "fr"
				Assert.AreEqual(0, lex1.HomographNumber,
								"5) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"5) second entry should have homograph number 0.");

				// 6) Switch to 2nd vWs: 4 hw with multi vWs lf with 1st vWs same and 2nd with 2 diff forms, 2 same -> hn 1234 -> 0012
				lex1 = MakeStemEntryMultiWs("x", frWsId, "s", spWsId, morphTypeRoot);
				lex2 = MakeStemEntryMultiWs("x", frWsId, "t", spWsId, morphTypeRoot);
				var lex3 = MakeStemEntryMultiWs("x", frWsId, "u", spWsId, morphTypeRoot);
				var lex4 = MakeStemEntryMultiWs("x", frWsId, "u", spWsId, morphTypeRoot);
				Assert.AreEqual(1, lex1.HomographNumber,
								"6) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"6) second entry should have homograph number 2.");
				Assert.AreEqual(3, lex3.HomographNumber,
								"6) third entry should have homograph number 3.");
				Assert.AreEqual(4, lex4.HomographNumber,
								"6) fourth entry should have homograph number 4.");
				SwapVernWs(false); // hgws is still "fr"
				Assert.AreEqual(1, lex1.HomographNumber,
								"6) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"6) second entry should have homograph number 2.");
				Assert.AreEqual(3, lex3.HomographNumber,
								"6) third entry should have homograph number 3.");
				Assert.AreEqual(4, lex4.HomographNumber,
								"6) fourth entry should have homograph number 4.");

				helper.RollBack = false; // hopefully all our changes succeeded.
			}
		}

		/// <summary>
		/// Inspired by LT-13615 - tests homograph renumbering following a change in lex forms with multiple ws.
		/// This test changes the homograph ws when the vernacular ws changes.
		/// </summary>
		[Test]
		public void HomographsChangingHgWs()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				Cache.LanguageProject.VernacularWritingSystems.Clear();
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Clear();
				var frWs = Cache.WritingSystemFactory.get_Engine("fr");
				var spWs = Cache.WritingSystemFactory.get_Engine("es");
				Cache.LanguageProject.DefaultVernacularWritingSystem = (IWritingSystem)spWs;
				Cache.LanguageProject.DefaultVernacularWritingSystem = (IWritingSystem)frWs; // {fr es} order
				int frWsId = frWs.Handle;
				int spWsId = spWs.Handle;

				// ws order is "fr es"
				var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var morphTypeRoot = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphRoot);

				// 1) Normal case: 2 hw with multi vWs lf with 1st vWs same forms and 2nd with diff forms -> hn 1 + 2
				var lex1 = MakeStemEntryMultiWs("a", frWsId, "b", spWsId, morphTypeRoot);
				var lex2 = MakeStemEntryMultiWs("a", frWsId, "c", spWsId, morphTypeRoot);
				Assert.AreEqual(1, lex1.HomographNumber,
								"1) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"1) second entry should have homograph number 2.");

				// 2) Empty case: both lf 1st vWs -> no hns 0 + 0
				lex1 = MakeStemEntryMultiWs("", frWsId, "b", spWsId, morphTypeRoot);
				lex2 = MakeStemEntryMultiWs("", frWsId, "c", spWsId, morphTypeRoot);
				Assert.AreEqual(0, lex1.HomographNumber,
								"2) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"2) second entry should have homograph number 0.");

				// 3) Edit case: Type a lf 1st vWs for both entries -> hns 1 + 2
				lex1.LexemeFormOA.Form.set_String(frWsId, Cache.TsStrFactory.MakeString("z", frWsId));
				lex2.LexemeFormOA.Form.set_String(frWsId, Cache.TsStrFactory.MakeString("z", frWsId));
				Assert.AreEqual(1, lex1.HomographNumber,
								"3) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"3) second entry should have homograph number 2.");

				// 4) Switch to 2nd vWs: 2 hw with multi vWs lf with 1st vWs empty and 2nd with same forms -> hn 0 + 0 -> 1 + 2
				lex1 = MakeStemEntryMultiWs("", frWsId, "k", spWsId, morphTypeRoot);
				lex2 = MakeStemEntryMultiWs("", frWsId, "k", spWsId, morphTypeRoot);
				Assert.AreEqual(0, lex1.HomographNumber,
								"4) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"4) second entry should have homograph number 0.");
				SwapVernWs(true); // ws order is now "es fr"
				Assert.AreEqual(1, lex1.HomographNumber,
								"4) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"4) second entry should have homograph number 2.");

				// 5) Switch to 1st vWs: 2 hw with multi vWs lf with 1st vWs empty and 2nd with same forms -> hn 1 + 2 -> 0 + 0
				SwapVernWs(true); // ws order is now "fr es"
				Assert.AreEqual(0, lex1.HomographNumber,
								"5) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"5) second entry should have homograph number 0.");

				// 6) Switch to 2nd vWs: 4 hw with multi vWs lf with 1st vWs same and 2nd with 2 diff forms, 2 same -> hn 1234 -> 0012
				lex1 = MakeStemEntryMultiWs("x", frWsId, "s", spWsId, morphTypeRoot);
				lex2 = MakeStemEntryMultiWs("x", frWsId, "t", spWsId, morphTypeRoot);
				var lex3 = MakeStemEntryMultiWs("x", frWsId, "u", spWsId, morphTypeRoot);
				var lex4 = MakeStemEntryMultiWs("x", frWsId, "u", spWsId, morphTypeRoot);
				Assert.AreEqual(1, lex1.HomographNumber,
								"6) first entry should have homograph number 1.");
				Assert.AreEqual(2, lex2.HomographNumber,
								"6) second entry should have homograph number 2.");
				Assert.AreEqual(3, lex3.HomographNumber,
								"6) third entry should have homograph number 3.");
				Assert.AreEqual(4, lex4.HomographNumber,
								"6) fourth entry should have homograph number 4.");
				SwapVernWs(true);
				Assert.AreEqual(0, lex1.HomographNumber,
								"6) first entry should have homograph number 0.");
				Assert.AreEqual(0, lex2.HomographNumber,
								"6) second entry should have homograph number 0.");
				Assert.AreEqual(1, lex3.HomographNumber,
								"6) third entry should have homograph number 1.");
				Assert.AreEqual(2, lex4.HomographNumber,
								"6) fourth entry should have homograph number 2.");

				helper.RollBack = false; // hopefully all our changes succeeded.
			}
		}

		/// <summary>
		/// Reverses the order of the vernacular wss - the top being the default
		/// </summary>
		/// <param name="changeHgWs">Make the homogragh ws match the new default vern ws when true</param>
		private void SwapVernWs(bool changeHgWs)
		{
			var newDvWs = Cache.LanguageProject.CurrentVernacularWritingSystems.ElementAt(1);
			Cache.LanguageProject.DefaultVernacularWritingSystem = newDvWs;
			if (changeHgWs)
			{
				Cache.LanguageProject.HomographWs = newDvWs.Id; // set homograph number ws to the new default vern ws
				Cache.ServiceLocator.GetInstance<ILexEntryRepository>().ResetHomographs(null);
			}
		}

		/// <summary>
		/// Makes a lex entry with lexeme form in 2 wss
		/// </summary>
		/// <param name="form1"></param>
		/// <param name="wsId1"> </param>
		/// <param name="form2"></param>
		/// <param name="wsId2"> </param>
		/// <param name="mt"></param>
		/// <returns></returns>
		private ILexEntry MakeStemEntryMultiWs(string form1, int wsId1, string form2, int wsId2, IMoMorphType mt)
		{
			var result = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			result.LexemeFormOA = morph;
			morph.MorphTypeRA = mt;
			morph.Form.set_String(wsId1, Cache.TsStrFactory.MakeString(form1, wsId1));
			morph.Form.set_String(wsId2, Cache.TsStrFactory.MakeString(form2, wsId2));
			return result;
		}

		/// <summary>
		/// Gets the ws id (int) from the ordered CurrentVernacularWritingSystems collection.
		/// </summary>
		/// <param name="seq">The first (0), second (1), etc..</param>
		/// <returns>The unique integer representing the ws</returns>
		private int GetVernWs(int seq)
		{
			var vernWsList = Cache.LanguageProject.CurrentVernacularWritingSystems;
			return Cache.WritingSystemFactory.GetWsFromStr(vernWsList.ElementAt(seq).Id);
		}

		/// <summary>
		/// Inspired by LT-13152 - tests homograph renumbering following a merge.
		/// </summary>
		[Test]
		public void HomographMerging()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var morphTypePrefix = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphPrefix);
				var entry1 = MakeAffixEntry("a", morphTypePrefix);
				var entry2 = MakeAffixEntry("a", morphTypePrefix);
				var entry3 = MakeAffixEntry("a", morphTypePrefix);
				var entry4 = MakeAffixEntry("a", morphTypePrefix);

				Assert.AreEqual(1, entry1.HomographNumber,
					"first entry should have homograph number 1.");
				Assert.AreEqual(2, entry2.HomographNumber,
					"second entry should have homograph number 2.");
				Assert.AreEqual(3, entry3.HomographNumber,
					"third entry should have homograph number 3.");
				Assert.AreEqual(4, entry4.HomographNumber,
					"fourth entry should have homograph number 4.");

				// We now have entry1 (a1); entry2 (a2); entry3 (a3); entry4 (a4)
				// Merging entry1 into entry3 should give us:
				// entry1 - obsolete; entry2 (a1); entry3 (a2); entry4 (a3)
				entry3.MergeObject(entry1);

				Assert.AreEqual(1, entry2.HomographNumber,
					"second entry should have changed to homograph number 1.");
				Assert.AreEqual(2, entry3.HomographNumber,
					"third entry should have changed to homograph number 2.");
				Assert.AreEqual(3, entry4.HomographNumber,
					"fourth entry should have changed to homograph number 3.");

				helper.RollBack = false; // hopefully all our changes succeeded.
			}
		}

		/// <summary>
		/// Things with the same LexemeForm and different morph types are not homographs.
		/// </summary>
		[Test]
		public void HomographsAndMorphTypes()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var morphTypeRoot = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphRoot);
				var morphTypeStem = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphStem);
				var morphTypePrefix = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphPrefix);
				var morphTypeSuffix = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphSuffix);
				var entry1 = MakeStemEntry("rot", morphTypeRoot);

				Assert.That(entry1.HomographNumber, Is.EqualTo(0), "sole entry should have homograph number zero.");

				var entry2 = MakeStemEntry("rot", morphTypeStem);
				Assert.That(entry1.HomographNumber, Is.EqualTo(1),
					"old entry with duplicate LF and similar morph type should have homograph number one.");
				Assert.That(entry2.HomographNumber, Is.EqualTo(2),
					"new entry with duplicate LF and similar morph type should have homograph number two.");

				var entry3 = MakeAffixEntry("rot", morphTypePrefix);
				Assert.That(entry3.HomographNumber, Is.EqualTo(0), "sole prefix with form should have homograph number zero.");
				var entry4 = MakeAffixEntry("rot", morphTypePrefix);
				Assert.That(entry3.HomographNumber, Is.EqualTo(1), "original prefix with form should have homograph number one.");
				Assert.That(entry4.HomographNumber, Is.EqualTo(2), "new prefix with form should have homograph number two.");

				entry1.LexemeFormOA.MorphTypeRA = morphTypePrefix;
				Assert.That(entry1.HomographNumber, Is.EqualTo(3), "old item changed to prefix should have homograph number three.");
				Assert.That(entry2.HomographNumber, Is.EqualTo(0), "last entry that is not a prefix is no longer a homograph");
				Assert.That(entry3.HomographNumber, Is.EqualTo(1), "original prefix with form should have homograph number one.");
				Assert.That(entry4.HomographNumber, Is.EqualTo(2), "second prefix with form should have homograph number two.");

				// Now we get into the hairy ones. An entry with no morph type is considered a stem.
				var entry5 = MakeEntry("rot", null);
				Assert.That(entry2.HomographNumber, Is.EqualTo(1), "stem entry becomes a homograph");
				Assert.That(entry5.HomographNumber, Is.EqualTo(2), "entry with no type is a homograph");

				// If we add an allomorph that is a prefix, that becomes the primary morph type and makes it a homograph
				// of the others. At this point entries 1, 3, 4, and 5 are prefixes.
				var newMorph5_1 = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry5.AlternateFormsOS.Add(newMorph5_1);
				newMorph5_1.MorphTypeRA = morphTypePrefix;
				Assert.That(entry1.HomographNumber, Is.EqualTo(3), "prefix group, no change");
				Assert.That(entry2.HomographNumber, Is.EqualTo(0), "changing entry5 to prefix leaves entry2 not a homograph");
				Assert.That(entry3.HomographNumber, Is.EqualTo(1), "prefix group, no change");
				Assert.That(entry4.HomographNumber, Is.EqualTo(2), "prefix group, no change");
				Assert.That(entry5.HomographNumber, Is.EqualTo(4), "entry5 is now a prefix homograph");

				// If we now add another allomorph that is a stem, that takes precedence.
				// Want to see the renumbering of the old homographs, so force entry5 out of last place
				entry5.HomographNumber = 3;
				entry1.HomographNumber = 4;
				var newMorph5_2 = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry5.AlternateFormsOS.Add(newMorph5_2);
				newMorph5_2.MorphTypeRA = morphTypeSuffix;
				Assert.That(entry1.HomographNumber, Is.EqualTo(3), "prefix group, number dropped as entry5 left group");
				Assert.That(entry2.HomographNumber, Is.EqualTo(0), "entry2 not a homograph");
				Assert.That(entry3.HomographNumber, Is.EqualTo(1), "prefix group, no change");
				Assert.That(entry4.HomographNumber, Is.EqualTo(2), "prefix group, no change");
				Assert.That(entry5.HomographNumber, Is.EqualTo(0), "entry5 is now the only suffix");

				// Make another suffix, so we can see the effect of the change.
				// We will do this in a trick way, by adding a suffix allomorph and then clearing the MT of the LF.
				var newMorph3_1 = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry3.AlternateFormsOS.Add(newMorph3_1);
				newMorph3_1.MorphTypeRA = morphTypeSuffix;
				entry3.LexemeFormOA.MorphTypeRA = null; // allomorph now wins, it is a suffix.
				Assert.That(entry1.HomographNumber, Is.EqualTo(2), "prefix group, number dropped as entry3 left group");
				Assert.That(entry2.HomographNumber, Is.EqualTo(0), "entry2 not a homograph (stem)");
				Assert.That(entry3.HomographNumber, Is.EqualTo(2), "now suffix group");
				Assert.That(entry4.HomographNumber, Is.EqualTo(1), "prefix group, number dropped as entry3 left group");
				Assert.That(entry5.HomographNumber, Is.EqualTo(1), "now there is another suffix");

				// Convert entry5 back to a stem, by setting the LF MT.
				entry5.LexemeFormOA.MorphTypeRA = morphTypeStem;
				Assert.That(entry1.HomographNumber, Is.EqualTo(2), "prefix group, no change");
				Assert.That(entry2.HomographNumber, Is.EqualTo(1), "entry2 now a homograph (stem)");
				Assert.That(entry3.HomographNumber, Is.EqualTo(0), "no longer a homograph as entry5 left suffix group");
				Assert.That(entry4.HomographNumber, Is.EqualTo(1), "prefix group, no change");
				Assert.That(entry5.HomographNumber, Is.EqualTo(2), "moved to second place in suffix group");

				helper.RollBack = false; // hopefully all our changes succeeded.
			}
		}

		ILexEntry MakeStemEntry(string form, IMoMorphType mt)
		{
			var result = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			result.LexemeFormOA = morph;
			var vernWs = Cache.DefaultVernWs;
			morph.Form.set_String(vernWs, Cache.TsStrFactory.MakeString(form, vernWs));
			morph.MorphTypeRA = mt;
			return result;
		}

		ILexEntry MakeAffixEntry(string form, IMoMorphType mt)
		{
			var result = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			result.LexemeFormOA = morph;
			var vernWs = Cache.DefaultVernWs;
			morph.Form.set_String(vernWs, Cache.TsStrFactory.MakeString(form, vernWs));
			morph.MorphTypeRA = mt;
			return result;
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

		void VerifyPair(string[] pair, string first, string second)
		{
			if (second == null)
				Assert.That(pair.Length, Is.EqualTo(1));
			else
			{
				Assert.That(pair.Length, Is.EqualTo(2));
				Assert.That(pair[1], Is.EqualTo(second));
			}
			Assert.That(pair[0], Is.EqualTo(first));
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
			VerifyPair(entry.FullSortKey(false, Cache.DefaultVernWs),"kick", null);
			LexEntry entry2 = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry2 = (LexEntry) MakeEntryWithForm("kick"));
			VerifyPair(entry.FullSortKey(false, Cache.DefaultVernWs), "kick", "00000000001");
			VerifyPair(entry2.FullSortKey(false, Cache.DefaultVernWs), "kick", "00000000002");

			var morphTypeRep = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			var morphType = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphSuffix);
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry.LexemeFormOA.MorphTypeRA = morphType);
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => entry2.LexemeFormOA.MorphTypeRA = morphType);
			// The number comes from the homograph number plus 1024 times the morph type index (6x1024).
			VerifyPair(entry.FullSortKey(false, Cache.DefaultVernWs), "kick", "00000006145");
		}

		private ILexEntry MakeEntry(string form, string gloss, IPartOfSpeech pos)
		{
			var result = MakeEntry(form, gloss);
			var msa = MakeMsa(result, pos);
			result.SensesOS[0].MorphoSyntaxAnalysisRA = msa;
			return result;
		}

		private ILexEntry MakeEntry(string form, string gloss)
		{
			var result = MakeEntryWithForm(form);
			MakeSense(result, gloss);
			return result;
		}

		/// <summary>
		/// Make a sense of the entry with the specified pos. Assumes a new MSA must be made.
		/// </summary>
		/// <returns></returns>
		private ILexSense MakeSenseWithNewMsa(ILexEntry entry, string gloss, IPartOfSpeech pos)
		{
			var result = MakeSense(entry, gloss);
			result.MorphoSyntaxAnalysisRA = MakeMsa(entry, pos);
			return result;
		}

		/// <summary>
		/// Make a subsense of the sense with the specified pos. Assumes a new MSA must be made.
		/// </summary>
		private ILexSense MakeSenseWithNewMsa(ILexSense sense, string gloss, IPartOfSpeech pos)
		{
			var result = MakeSense(sense, gloss);
			result.MorphoSyntaxAnalysisRA = MakeMsa(sense.Entry, pos);
			return result;
		}

		private ILexSense MakeSense(ILexEntry entry, string gloss)
		{
			var sense = MakeSense(entry);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return sense;
		}

		private ILexSense MakeSense(ILexSense parent, string gloss)
		{
			var sense = MakeSense(parent);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return sense;
		}

		private ILexEntry MakeEntryWithForm(string form)
		{
			var entry = MakeEntry();
			var lexform = MakeLexemeForm(entry);
			lexform.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			return entry;
		}

		private ILexEntry MakeAffixWithForm(string form, Guid morphType)
		{
			var entry = MakeEntry();
			var lexform = MakeAffixForm(entry, morphType);
			lexform.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			return entry;
		}

		/// <summary>
		/// The method should not crash if applied to a entry with a sense that lacks an MSA (or an adhocProhib.FirstMorphemeRA that is empty).
		/// This test basically just sets up the minimum preconditions so that if we forget to test for these things being null, it will crash.
		/// </summary>
		[Test]
		public void ReplaceObsoleteMsas_NullProps()
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
				{
					var entry = MakeEntryWithForm("in"); // We need an entry to be the owner of things
					MakeSense(entry); // We need a sense with null MSA (so don't set it), since not testing for null MSA is one point of the test.
					var pos = MakePartOfSpeech(); // We need this to make the MSA
					var msa = MakeMsa(entry, pos); // We need an msa, because the problem code is bypassed if there are no MSAs to replace
					// Another problem we are checking for is an MoMorphAdhocProhib with a FirstMorphemeRA null, so make one of those.
					var prohibition = Cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibFactory>().Create();
					Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(prohibition);
					// If the replace call does not crash we've proved that these edge cases are coped with.
					Assert.DoesNotThrow(() => entry.ReplaceObsoleteMsas(new List<IMoMorphSynAnalysis> { msa }));
				});
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
