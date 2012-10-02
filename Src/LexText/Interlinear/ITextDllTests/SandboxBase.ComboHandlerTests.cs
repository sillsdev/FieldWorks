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
// File: SandboxBase.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class SandboxEntryComboHandlerLogicTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private SetupMatchingMorphs m_matchingMorphs;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
			   DoSetupFixture);
		}

		public override void FixtureTeardown()
		{
			if (m_matchingMorphs != null)
				m_matchingMorphs.Dispose();
			m_matchingMorphs = null;
			base.FixtureTeardown();
		}

		void DoSetupFixture()
		{

			m_matchingMorphs = new SetupMatchingMorphs(this);
		}

		/// <summary>
		/// We initially tried to do SetupMatchingMorphs on a per test basis,
		/// but discovered that Undo wasn't undoing everything we expected.
		/// (See UndoAllIssueTest)
		/// </summary>
		internal class SetupMatchingMorphsDisabled : SetupMatchingMorphs
		{
			internal SetupMatchingMorphsDisabled(SandboxEntryComboHandlerLogicTests testFixture)
			{

			}

			protected override void DoSetup()
			{
				// don't do any data setup.
			}

		}

		// REVIEW (TomB): There is no reason to derive from FwDisposableBase because neither
		// Dispose method is being overriden. Either override one of those methods or get rid
		// of all the using statements where objects of this class are instantiated.
		internal class SetupMatchingMorphs : FwDisposableBase
		{
			internal SetupMatchingMorphs()
			{

			}
			internal SetupMatchingMorphs(SandboxEntryComboHandlerLogicTests testFixture)
			{
				Cache = testFixture.Cache;
				DoSetup();
			}

			protected virtual void DoSetup()
			{
				ILexEntry newEntry;
				ILexSense newSense;
				var msa1 = new SandboxGenericMSA();
				msa1.MsaType = MsaType.kInfl;
				SetupLexEntryAndSense(Cache, "ipr-", "inflectional prefix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-isu", "inflectional suffix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-ii-", "inflectional infix", msa1, out newEntry, out newSense);
				var msa2 = new SandboxGenericMSA();
				msa2.MsaType = MsaType.kStem;
				SetupLexEntryAndSense(Cache, "s", "stem1", msa1, out newEntry, out newSense);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "sa", newEntry.LexemeFormOA.MorphTypeRA);
				SetupLexEntryAndSense(Cache, "s", "stem2", msa1, out newEntry, out newSense);
				var morphTypeEnclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphEnclitic);
				var morphTypeProclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "pro", morphTypeProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "enc", morphTypeEnclitic);
			}

			FdoCache Cache { get; set; }

			private void SetupPartsOfSpeech()
			{
				// setup language project parts of speech
				var partOfSpeechFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				var adjunct = partOfSpeechFactory.Create();
				var noun = partOfSpeechFactory.Create();
				var verb = partOfSpeechFactory.Create();
				var transitiveVerb = partOfSpeechFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(adjunct);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
				verb.SubPossibilitiesOS.Add(transitiveVerb);
				adjunct.Name.set_String(Cache.DefaultAnalWs, "adjunct");
				noun.Name.set_String(Cache.DefaultAnalWs, "noun");
				verb.Name.set_String(Cache.DefaultAnalWs, "verb");
				transitiveVerb.Name.set_String(Cache.DefaultAnalWs, "transitive verb");
			}

			private void SetupLexEntryAndSense(FdoCache cache, string fullForm, string senseGloss, SandboxGenericMSA msa,
				out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
			{
				lexEntry1_Entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(fullForm, senseGloss, msa);
				lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];
			}

			private void AddAllomorph<TAllomorphFactory, TItem>(ILexEntry entry, string allomorphForm, IMoMorphType morphType)
				where TAllomorphFactory : IFdoFactory<TItem>
				where TItem : IMoForm
			{
				ITsString tssAllomorphForm = StringUtils.MakeTss(allomorphForm, Cache.DefaultVernWs);
				TAllomorphFactory stemFactory = Cache.ServiceLocator.GetInstance<TAllomorphFactory>();
				var allomorph = stemFactory.Create();
				entry.AlternateFormsOS.Add(allomorph);
				allomorph.MorphTypeRA = morphType;
				allomorph.Form.set_String(StringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);
			}
		}

		private void CheckExpected<TMorphExpected>(int expectedMatchCount,
			string prefixMarker, string form, string postfixMarker)
		{
			var targetForm = StringUtils.MakeTss(form, Cache.DefaultVernWs);
			var matches = MorphServices.GetMatchingMorphs(Cache,
				prefixMarker, targetForm, postfixMarker);
			Assert.AreEqual(expectedMatchCount, matches.Count());
			foreach (var match in matches)
				Assert.IsTrue(match is TMorphExpected, "Expected class " + typeof(TMorphExpected).Name);
		}

		private void CheckEmpty(string prefixMarker, string form, string postfixMarker)
		{
			ITsString tssForm = StringUtils.MakeTss(form, Cache.DefaultVernWs);
			var emptySet = MorphServices.GetMatchingMorphs(Cache,
																		prefixMarker, tssForm, postfixMarker);
			Assert.AreEqual(0, emptySet.Count());
		}

		[Test]
		public void MatchingMorphs_Stems()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stems = MorphServices.GetMatchingMorphs(Cache,
					"", StringUtils.MakeTss("s", Cache.DefaultVernWs), "");
				Assert.AreEqual(2, stems.Count());
				foreach (var stem in stems)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stems");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "s", "");
				CheckEmpty("",  "s", "-");
				CheckEmpty("-", "s", "-");
			}
		}

		[Test]
		public void MatchingMorphs_StemAllomorphs()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stemAllomorphs = MorphServices.GetMatchingMorphs(Cache,
					"", StringUtils.MakeTss("sa", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, stemAllomorphs.Count());
				foreach (var stem in stemAllomorphs)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stem allomorph");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "sa", "");
				CheckEmpty("", "sa", "-");
				CheckEmpty("-", "sa", "-");
			}
		}

		[Test]
		public void MatchingMorphs_Prefixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var prefixes = MorphServices.GetMatchingMorphs(Cache,
					"", StringUtils.MakeTss("ipr", Cache.DefaultVernWs), "-");
				Assert.AreEqual(1, prefixes.Count());
				foreach (var prefix in prefixes)
					Assert.IsTrue(prefix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "ipr", "");
				CheckEmpty("-", "ipr", "");
				CheckEmpty("-", "ipr", "-");
			}
		}

		[Test]
		public void MatchingMorphs_Suffixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var suffixes = MorphServices.GetMatchingMorphs(Cache,
					"-", StringUtils.MakeTss("isu", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, suffixes.Count());
				foreach (var suffix in suffixes)
					Assert.IsTrue(suffix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "isu", "");
				CheckEmpty("",  "isu", "-");
				CheckEmpty("-", "isu", "-");
			}
		}

		[Test]
		public void MatchingMorphs_Proclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var procliticForm = StringUtils.MakeTss("pro", Cache.DefaultVernWs);
				var proclitics = MorphServices.GetMatchingMorphs(Cache,
					"", procliticForm, "=");
				Assert.AreEqual(1, proclitics.Count());
				foreach (var proclitic in proclitics)
					Assert.IsTrue(proclitic is IMoStemAllomorph, "Expected proclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "pro", "");
				CheckEmpty("-", "pro", "-");
				CheckEmpty("-", "pro", "");
				CheckEmpty("",  "pro", "-");
			}
		}

		[Test]
		public void MatchingMorphs_Enclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var encliticForm = StringUtils.MakeTss("enc", Cache.DefaultVernWs);
				var enclitics = MorphServices.GetMatchingMorphs(Cache,
					"=", encliticForm, "");
				Assert.AreEqual(1, enclitics.Count());
				foreach (var enclitic in enclitics)
					Assert.IsTrue(enclitic is IMoStemAllomorph, "Expected enclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "enc", "");
				CheckEmpty("-", "enc", "-");
				CheckEmpty("-", "enc", "");
				CheckEmpty("", "enc", "-");
			}
		}

	}

	/// <summary>
	/// We expect Undo to undo all the data created during a test.
	/// To verify this, we run two tests, both creating a new entry and testing the same preconditions and postconditions
	/// </summary>
	[TestFixture]
	public class UndoAllIssueTest : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private void MakeSureEverythingIsUndoneAfterTests()
		{
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count, "Expected no initial entries");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count, "Expected no initial senses");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count, "Expected no initial moForms");

			ITsString tssFullForm = StringUtils.MakeTss("entryToUndo", Cache.DefaultVernWs);
			var entryComponents = MorphServices.BuildEntryComponents(Cache,
																			tssFullForm);
			entryComponents.GlossAlternatives.Add(StringUtils.MakeTss("senseToUndo", Cache.DefaultVernWs));
			ILexEntry newEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);

			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count);
		}

		[Test]
		public void NewEntry()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}

		[Test]
		public void NewEntry2()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}
	}
}
