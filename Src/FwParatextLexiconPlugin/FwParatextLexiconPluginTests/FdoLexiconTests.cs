// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Paratext.LexicalContracts;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// FDO lexicon tests
	/// </summary>
	[TestFixture]
	public class FdoLexiconTests
	{
		private ThreadHelper m_threadHelper;
		private FdoLexicon m_lexicon;
		private LcmCache m_cache;

		/// <summary>
		/// Set up the unit tests
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			// Force initialization in ILRepacked SIL.WritingSystems assembly,
			// even if a referenced SIL.WritingSystems assembly somewhere down
			// the dependency chain, that we won't be using, was initialized.
			if (!Sldr.IsInitialized)
			{
				Sldr.Initialize();
			}

			FwRegistryHelper.Initialize();
			m_threadHelper = new ThreadHelper();
			var ui = new DummyLcmUI(m_threadHelper);
			var projectId = new ParatextLexiconPluginProjectId(BackendProviderType.kMemoryOnly, "Test.fwdata");
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(projectId, "en", "fr", "en", ui,
				ParatextLexiconPluginDirectoryFinder.LcmDirectories, new LcmSettings());
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(m_cache.ServiceLocator.WritingSystemManager.Get("fr"));
					m_cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(m_cache.ServiceLocator.WritingSystemManager.Get("en"));
					m_cache.LangProject.MorphologicalDataOA.ParserParameters = "<ParserParameters><XAmple><MaxNulls>1</MaxNulls><MaxPrefixes>5</MaxPrefixes><MaxInfixes>1</MaxInfixes><MaxSuffixes>5</MaxSuffixes><MaxInterfixes>0</MaxInterfixes><MaxAnalysesToReturn>10</MaxAnalysesToReturn></XAmple><ActiveParser>XAmple</ActiveParser></ParserParameters>";
				});
			m_lexicon = new FdoLexicon("Test", "FieldWorks:Test", m_cache, m_cache.DefaultVernWs);
		}

		/// <summary>
		/// Cleans up the unit tests
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			m_lexicon.Dispose();
			m_cache.Dispose();
			m_threadHelper.Dispose();
		}

		#region Tests
		/// <summary>
		/// Test multiple lexeme creates where the IDs match
		/// </summary>
		[Test]
		public void MultipleCreatesIdsMatch()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Assert.That(lex2.Id, Is.EqualTo(lex.Id));
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex.LexicalForm, Is.EqualTo("a"));
			Assert.That(lex2.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex2.LexicalForm, Is.EqualTo("a"));
		}

		/// <summary>
		/// Test multiple lexeme creates that refer to the same sense
		/// </summary>
		[Test]
		public void MultipleCreatesReferToSameSenses()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");

			m_lexicon.AddLexeme(lex);
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "test");

			Assert.That(lex2.Senses.Count(), Is.EqualTo(1));

			// Make sure the one that was added has the right sense now
			lex = m_lexicon[lex.Id];
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex.LexicalForm, Is.EqualTo("a"));
			Assert.That(lex.Senses.Count(), Is.EqualTo(1));
			Assert.That(lex.Senses.First().Glosses.First().Language, Is.EqualTo("en"));
			Assert.That(lex.Senses.First().Glosses.First().Text, Is.EqualTo("test"));
		}

		/// <summary>
		/// Test that adding sense also adds lexeme
		/// </summary>
		[Test]
		public void AddingSenseAddsLexeme()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a"); // Creates lexeme, but does not add it (verified in another test)
			LexiconSense sense = lex.AddSense(); // SUT: Lexeme is added by adding the Sense
			sense.AddGloss("en", "test");

			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));

			lex = m_lexicon[lex.Id]; // Make sure we're using the one stored in the lexicon
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex.LexicalForm, Is.EqualTo("a"));
			Assert.That(lex.Senses.Count(), Is.EqualTo(1));
			Assert.That(lex.Senses.First().Glosses.First().Language, Is.EqualTo("en"));
			Assert.That(lex.Senses.First().Glosses.First().Text, Is.EqualTo("test"));
		}

		/// <summary>
		/// Test that adding a Lexeme results in ImportResidue for the LexEntry that is created.
		/// </summary>
		[Test]
		public void AddingLexemeOrSenseSetsImportResidue()
		{
			var lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a"); // Creates lexeme, but does not add it (verified in another test)
			lex.AddSense();

			var lexEntryRepo = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances();
			var lexEntry = lexEntryRepo.FirstOrDefault(entry =>
				entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text == "a");
			var sense = lex.AddSense(); // SUT: Lexeme is added by adding the Sense
			sense.AddGloss("en", "test");

			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));

			lex = m_lexicon[lex.Id]; // Make sure we're using the one stored in the lexicon
			Assert.That(lex.LexicalForm, Is.EqualTo("a"), "Failure in test setup");
			Assert.That(lex.Senses.Count(), Is.EqualTo(1), "Failure in test setup");
			Assert.That(lexEntry.ImportResidue.Text, Is.EqualTo(FdoLexicon.AddedByParatext));
			Assert.That(lexEntry.SensesOS[0].ImportResidue.Text, Is.EqualTo(FdoLexicon.AddedByParatext));
		}

		/// <summary>
		/// Test that homograph increments
		/// </summary>
		[Test]
		public void HomographsIncrement()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");

			m_lexicon.AddLexeme(lex);
			// lex2 should be identical to lex since there aren't any in the cache yet
			Assert.That(lex2.Id, Is.EqualTo(lex.Id));

			// This lexeme should have a new homograph number since lex has been added to the cache
			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			Assert.That(lex.Id, Is.Not.EqualTo(lex3.Id));
			Assert.That(lex2.Id, Is.Not.EqualTo(lex3.Id));
			Assert.That(lex.HomographNumber, Is.EqualTo(1));
			Assert.That(lex2.HomographNumber, Is.EqualTo(1));
			Assert.That(lex3.HomographNumber, Is.EqualTo(2));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void HomographsFind()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex2);
			Assert.That(lex2.Id, Is.Not.EqualTo(lex.Id));

			List<Lexeme> found = new List<Lexeme>(m_lexicon.Lexemes);
			Assert.That(found.Count, Is.EqualTo(2));
			Assert.That(found[0].Id, Is.EqualTo(lex.Id));
			Assert.That(found[1].Id, Is.EqualTo(lex2.Id));
		}

		/// <summary>
		/// Test find or create lexeme
		/// </summary>
		[Test]
		public void FindOrCreate()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "monkey");

			Lexeme lex2 = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "a");
			Assert.That(lex2.Id, Is.EqualTo(lex.Id));
			Assert.That(lex2.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex2.LexicalForm, Is.EqualTo("a"));
			Assert.That(lex2.Senses.Count(), Is.EqualTo(1));
			Assert.That(lex2.Senses.First().Glosses.Count(), Is.EqualTo(1));
			Assert.That(lex2.Senses.First().Glosses.First().Language, Is.EqualTo("en"));
			Assert.That(lex2.Senses.First().Glosses.First().Text, Is.EqualTo("monkey"));

			Lexeme lex3 = m_lexicon.FindOrCreateLexeme(LexemeType.Suffix, "bob");
			Assert.That(lex3.Id, Is.Not.EqualTo(lex.Id));
			Assert.That(lex3.Id, Is.Not.EqualTo(lex2.Id));
			Assert.That(lex3.Type, Is.EqualTo(LexemeType.Suffix));
			Assert.That(lex3.LexicalForm, Is.EqualTo("bob"));
			Assert.That(lex3.Senses.Count(), Is.EqualTo(0));
		}

		/// <summary>
		/// Test indexer
		/// </summary>
		[Test]
		public void Indexer()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex);

			lex = m_lexicon[lex.Id];
			Assert.That(lex, Is.Not.Null);
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Stem));
			Assert.That(lex.LexicalForm, Is.EqualTo("a"));

			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Suffix, "monkey");
			Assert.That(m_lexicon[lex2.Id], Is.Null);
		}

		/// <summary>
		/// Test that creating a lexeme does not add it
		/// </summary>
		[Test]
		public void CreatingDoesNotAdd()
		{
			m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(0));
		}

		/// <summary>
		/// Test that getting a sense from a created lexeme does not add it
		/// </summary>
		[Test]
		public void GettingSensesDoesNotAdd()
		{
			Lexeme lexeme = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			lexeme.Senses.Count();
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(0));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void AddSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));

			lex = m_lexicon[lex.Id]; // Make sure we're using the one stored in the lexicon
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Word));
			Assert.That(lex.LexicalForm, Is.EqualTo("a"));
		}

		/// <summary>
		/// Test that adding the same lexeme multiple times fails
		/// </summary>
		[Test]
		public virtual void MultipleAddsFail()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Assert.Throws(typeof (ArgumentException), () => m_lexicon.AddLexeme(lex2));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void SensesRetained()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "glossen");
			sense.AddGloss("fr", "glossfr");

			Assert.That(lex.Senses.Count(), Is.EqualTo(1));
			Assert.That(lex.Senses.First().Glosses.Count(), Is.EqualTo(2));

			sense = m_lexicon[lex.Id].Senses.First(); // Make sure we're working with the one stored in the lexicon
			Assert.That(sense.Glosses.First().Language, Is.EqualTo("en"));
			Assert.That(sense.Glosses.First().Text, Is.EqualTo("glossen"));
			Assert.That(sense.Glosses.ElementAt(1).Language, Is.EqualTo("fr"));
			Assert.That(sense.Glosses.ElementAt(1).Text, Is.EqualTo("glossfr"));

			sense.RemoveGloss("en");

			sense = m_lexicon[lex.Id].Senses.First(); // Make sure we're working with the one stored in the lexicon
			Assert.That(sense.Glosses.Count(), Is.EqualTo(1));
			Assert.That(sense.Glosses.First().Language, Is.EqualTo("fr"));
			Assert.That(sense.Glosses.First().Text, Is.EqualTo("glossfr"));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MorphTypeRetained()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");
			m_lexicon.AddLexeme(lex2);
			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Suffix, "a");
			m_lexicon.AddLexeme(lex3);
			Lexeme lex4 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex4);

			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(4));
			Assert.That(m_lexicon.Lexemes.Contains(lex), Is.True);
			Assert.That(m_lexicon.Lexemes.Contains(lex2), Is.True);
			Assert.That(m_lexicon.Lexemes.Contains(lex3), Is.True);
			Assert.That(m_lexicon.Lexemes.Contains(lex4), Is.True);
		}

		/// <summary>
		/// Test removing lexemes
		/// </summary>
		[Test]
		public void RemoveLexemeSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");

			Assert.That(m_lexicon.Lexemes.Contains(lex), Is.True);
			Assert.That(m_lexicon.Lexemes.Contains(lex2), Is.False);

			m_lexicon.RemoveLexeme(lex);
			Assert.That(m_lexicon.Lexemes.Contains(lex), Is.False);

			m_lexicon.RemoveLexeme(lex2);
			Assert.That(m_lexicon.Lexemes.Contains(lex2), Is.False);

			m_lexicon.AddLexeme(lex2);
			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");
			m_lexicon.AddLexeme(lex3);

			m_lexicon.RemoveLexeme(lex2);
			Assert.That(m_lexicon.Lexemes.Contains(lex2), Is.False);
			Assert.That(m_lexicon.Lexemes.Contains(lex3), Is.True);
		}

		/// <summary>
		/// Test removing senses
		/// </summary>
		[Test]
		public void RemoveSenseSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);

			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "gloss1");

			LexiconSense sense2 = lex.AddSense();
			sense.AddGloss("en", "gloss1");

			// Test remove at
			lex.RemoveSense(sense2);

			Assert.That(lex.Senses.Count(), Is.EqualTo(1));
			Assert.That(lex.Senses.First(), Is.EqualTo(sense));
		}

		/// <summary>
		/// Test unusual characters in the lexical form
		/// </summary>
		[Test]
		public void UnusualCharactersSupported()
		{
			var stems = new[] { "a:b:c", "a:b:", "a:2", "123-4", "!@#$%^&*()" };

			foreach (string stem in stems)
			{
				Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Stem, stem);
				Assert.That(m_lexicon.Lexemes.Contains(lexeme), Is.False);
				m_lexicon.AddLexeme(lexeme);
				Assert.That(m_lexicon.Lexemes.Contains(lexeme), Is.True);

				// Add homomorph
				Lexeme lexeme2 = m_lexicon.CreateLexeme(LexemeType.Stem, stem);
				Assert.That(m_lexicon.Lexemes.Contains(lexeme2), Is.False);
				m_lexicon.AddLexeme(lexeme2);
				Assert.That(m_lexicon.Lexemes.Contains(lexeme2), Is.True);
			}
		}

		/// <summary>
		/// Tests that the lexicon correctly normalizes strings
		/// </summary>
		[Test]
		public void NormalizeStrings()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "Vacaci\u00f3n"); // Uses composed accented letter 'o'
			m_lexicon.AddLexeme(lex);

			lex = m_lexicon[new LexemeKey(LexemeType.Stem, "Vacaci\u00f3n").Id];
			Assert.That(lex, Is.Not.Null);
			Assert.That(lex.Type, Is.EqualTo(LexemeType.Stem));
			Assert.That(lex.LexicalForm, Is.EqualTo("Vacaci\u00f3n"));

			LexiconSense sense = lex.AddSense();
			Assert.That(sense, Is.Not.Null);

			LanguageText gloss = sense.AddGloss("en", "D\u00f3nde");

			Lexeme reGetLex = m_lexicon[lex.Id];
			Assert.That(reGetLex.Senses.First().Glosses.First().Text, Is.EqualTo(gloss.Text));
		}

		#region Lexicon Events

		/// <summary>
		/// Test lexeme added event
		/// </summary>
		[Test]
		public void LexemeAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) => glossAddedCount++;

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			Assert.That(lexemeAddedCount, Is.EqualTo(0));

			m_lexicon.AddLexeme(lexeme);
			Assert.That(lexemeAddedCount, Is.EqualTo(1));
			Assert.That(senseAddedCount, Is.EqualTo(0));
			Assert.That(glossAddedCount, Is.EqualTo(0));

			// Adding sense adds lexeme
			Lexeme lexeme2 = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word2");
			lexeme2.AddSense();
			Assert.That(lexemeAddedCount, Is.EqualTo(2));
		}

		/// <summary>
		/// Test sense added event
		/// </summary>
		[Test]
		public void SenseAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) => glossAddedCount++;

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			m_lexicon.AddLexeme(lexeme);
			lexeme.AddSense();
			Assert.That(senseAddedCount, Is.EqualTo(1));
		}

		/// <summary>
		/// Test gloss added event
		/// </summary>
		[Test]
		public void GlossAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			string glossText = "";
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) =>
				{
					glossAddedCount++;
					glossText = e.Gloss.Text;
				};

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			m_lexicon.AddLexeme(lexeme);
			LexiconSense sense = lexeme.AddSense();
			sense.AddGloss("en", "somegloss");

			Assert.That(glossAddedCount, Is.EqualTo(1));
			Assert.That(glossText, Is.EqualTo("somegloss"));
		}

		#endregion

		/// <summary>
		/// Test find matching lexemes
		/// </summary>
		[Test]
		public void FindMatchingLexemes()
		{
			Lexeme[] matchingLexemes = m_lexicon.FindMatchingLexemes("a").ToArray();
			Assert.That(matchingLexemes, Is.Empty);

			// Just the stem
			Lexeme lexemeA = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lexemeA);
			matchingLexemes = m_lexicon.FindMatchingLexemes("a").ToArray();
			Assert.That(matchingLexemes[0].LexicalForm, Is.EqualTo("a"));

			// Other parts, too
			Lexeme lexemePre = m_lexicon.CreateLexeme(LexemeType.Prefix, "pre");
			m_lexicon.AddLexeme(lexemePre);
			Lexeme lexemeSuf = m_lexicon.CreateLexeme(LexemeType.Suffix, "suf");
			m_lexicon.AddLexeme(lexemeSuf);
			m_lexicon.AddWordAnalysis(m_lexicon.CreateWordAnalysis("preasuf", new[] { lexemePre, lexemeA, lexemeSuf }));
			matchingLexemes = m_lexicon.FindMatchingLexemes("preasuf").ToArray();
			Assert.That(matchingLexemes.Length, Is.EqualTo(3));
			Assert.That(matchingLexemes.Contains(lexemePre), Is.True);
			Assert.That(matchingLexemes.Contains(lexemeA), Is.True);
			Assert.That(matchingLexemes.Contains(lexemeSuf), Is.True);
		}

		/// <summary>
		/// Test find closest matching lexeme
		/// </summary>
		[Test]
		public void FindClosestMatchingLexeme()
		{
			// Nothing found
			Lexeme matchingLexeme = m_lexicon.FindClosestMatchingLexeme("a");
			Assert.That(matchingLexeme, Is.Null);

			// Found by simple lexicon lookup
			Lexeme lexeme = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lexeme);
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("a");
			Assert.That(matchingLexeme.LexicalForm == "a", Is.True);

			// Found by parser
			lexeme = m_lexicon.CreateLexeme(LexemeType.Prefix, "pre");
			m_lexicon.AddLexeme(lexeme);
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("prea");
			Assert.That(matchingLexeme.LexicalForm == "a", Is.True);

			// Found by unsupervised stemmer
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "b"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "c"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "d"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "bpos"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "cpos"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "dpos"));
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("apos");
			Assert.That(matchingLexeme.LexicalForm == "a", Is.True);
		}

		/// <summary>
		/// Test when an entry has no morph type specified.
		/// </summary>
		[Test]
		public void NoMorphTypeSpecified()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.DefaultVernWs), "gloss", new SandboxGenericMSA());
				entry.LexemeFormOA.MorphTypeRA = null;
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form").Single();
			Assert.That(lexeme.LexicalForm, Is.EqualTo("form"));
			Assert.That(lexeme.Type, Is.EqualTo(LexemeType.Stem));
			lexeme = m_lexicon.Lexemes.Single();
			Assert.That(lexeme.LexicalForm, Is.EqualTo("form"));
			Assert.That(lexeme.Type, Is.EqualTo(LexemeType.Stem));
		}

		/// <summary>
		/// Test when an entry has a lexeme form with an empty default vernacular.
		/// </summary>
		[Test]
		public void EmptyDefaultVernLexemeForm()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.ElementAt(1).Handle), "gloss", new SandboxGenericMSA());
			});
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));
			Assert.That(m_lexicon.Lexemes.Single().LexicalForm, Is.EqualTo(string.Empty));
		}

		/// <summary>
		/// Test when an entry has a null lexeme form.
		/// </summary>
		[Test]
		public void NullLexemeForm()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.ElementAt(1).Handle), "gloss", new SandboxGenericMSA());
				entry.LexemeFormOA = null;
			});
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));
			Assert.That(m_lexicon.Lexemes.Single().LexicalForm, Is.EqualTo(string.Empty));
		}

		/// <summary>
		/// Test that the name for entry tree lexical relations are correct.
		/// </summary>
		[Test]
		public void EntryTreeLexicalRelationName()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry1 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry entry2 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form2", m_cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry entry3 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form3", m_cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());
				m_cache.LangProject.LexDbOA.ReferencesOA = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				ILexRefType entryLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(entryLexRefType);
				entryLexRefType.MappingType = (int) LexRefTypeTags.MappingTypes.kmtEntryTree;
				entryLexRefType.Name.SetAnalysisDefaultWritingSystem("Part");
				entryLexRefType.ReverseName.SetAnalysisDefaultWritingSystem("Whole");

				ILexReference entryLexRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
				entryLexRefType.MembersOC.Add(entryLexRef);
				entryLexRef.TargetsRS.Add(entry1);
				entryLexRef.TargetsRS.Add(entry2);
				entryLexRef.TargetsRS.Add(entry3);
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form1").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Part", "Part"}));

			lexeme = m_lexicon.FindMatchingLexemes("form2").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Whole", "Other"}));
		}

		/// <summary>
		/// Test that the name for sense tree lexical relations are correct.
		/// </summary>
		[Test]
		public void SenseTreeLexicalRelationName()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry1 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry entry2 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form2", m_cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry entry3 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form3", m_cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());
				m_cache.LangProject.LexDbOA.ReferencesOA = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				ILexRefType senseLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(senseLexRefType);
				senseLexRefType.MappingType = (int) LexRefTypeTags.MappingTypes.kmtSenseTree;
				senseLexRefType.Name.SetAnalysisDefaultWritingSystem("Part");
				senseLexRefType.ReverseName.SetAnalysisDefaultWritingSystem("Whole");

				ILexReference senseLexRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
				senseLexRefType.MembersOC.Add(senseLexRef);
				senseLexRef.TargetsRS.Add(entry1.SensesOS[0]);
				senseLexRef.TargetsRS.Add(entry2.SensesOS[0]);
				senseLexRef.TargetsRS.Add(entry3.SensesOS[0]);
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form1").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Part", "Part"}));

			lexeme = m_lexicon.FindMatchingLexemes("form2").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Whole", "Other"}));
		}

		/// <summary>
		/// The Paratext Open in Lexicon button should be enabled if FW is found.
		/// Part of LT-18529 and LT-18721.
		/// </summary>
		[Test]
		public void CanOpenInLexicon_Works()
		{
			var origFwDirEnviromentSetting = Environment.GetEnvironmentVariable("FIELDWORKSDIR");
			try
			{
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", null);
				// SUT
				Assert.That(m_lexicon.CanOpenInLexicon, Is.False,
					"Should not be able to open in Lexicon if FW location is not known.");

				var likeAPathToFw = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", likeAPathToFw);
				// SUT
				Assert.That(m_lexicon.CanOpenInLexicon, Is.True,
					"Should report ablity to open in Lexicon if FW location is known.");
			}
			finally
			{
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", origFwDirEnviromentSetting);
			}
		}

		/// <summary/>
		[Test]
		public void FindAllHomographs_ReturnsAll()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry1 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
						.GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss1",
					new SandboxGenericMSA());
				ILexEntry entry2 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
						.GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss2",
					new SandboxGenericMSA());
			});
			var entries = m_lexicon.FindAllHomographs(LexemeType.Stem, "form1");
			Assert.That(entries.Count(), Is.EqualTo(2));
		}

		/// <summary/>
		[Test]
		public void WordAnalysesV2_WordAnalyses_ReturnsWithGlosses()
		{
			Lexeme lexemeA = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lexemeA);
			Lexeme lexemePre = m_lexicon.CreateLexeme(LexemeType.Prefix, "pre");
			m_lexicon.AddLexeme(lexemePre);
			Lexeme lexemeSuf = m_lexicon.CreateLexeme(LexemeType.Suffix, "suf");
			m_lexicon.AddLexeme(lexemeSuf);
			var wordAnalysis = m_lexicon.CreateWordAnalysis("preasuf", new[] { lexemePre, lexemeA, lexemeSuf });
			m_lexicon.AddWordAnalysis(wordAnalysis);
			var otherAnalysis = m_lexicon.CreateWordAnalysis("preasuf", new Lexeme[] { lexemeA });
			m_lexicon.AddWordAnalysis(otherAnalysis);
			var analyses = m_lexicon.GetWordAnalyses("preasuf").ToArray();
			Assert.That(analyses.Count, Is.EqualTo(2));
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				var analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().First();
				var gloss = m_cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
				analysis.MeaningsOC.Add(gloss);
				gloss.Form.SetAnalysisDefaultWritingSystem("how glossy");
			});

			var lexiconAnalyses = ((WordAnalysesV2)m_lexicon).WordAnalyses;
			Assert.That(lexiconAnalyses.Count(), Is.EqualTo(2));
			Assert.That(()=>lexiconAnalyses.First().GetEnumerator(), Throws.Nothing);
			Assert.That(lexiconAnalyses.First().Glosses.Count(), Is.EqualTo(1));
			Assert.That(lexiconAnalyses.First().Glosses.First().Text, Is.EqualTo("how glossy"));
	  }

	  #endregion
   }
}
