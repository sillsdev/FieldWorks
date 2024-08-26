// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;
using System.Xml.Linq;
using SIL.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	[TestFixture]
	public class ParseWorkerTests : MemoryOnlyBackendProviderTestBase
	{
		#region Data Members
		private String m_taskDetailsString;
		private IdleQueue m_idleQueue;
		private CoreWritingSystemDefinition m_vernacularWS;
		#endregion Data Members

		#region Non-test methods
		private IWfiWordform FindOrCreateWordform(string form)
		{
			ILcmServiceLocator servLoc = Cache.ServiceLocator;
			IWfiWordform wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(m_vernacularWS.Handle, form);
			if (wf == null)
			{
				UndoableUnitOfWorkHelper.Do("Undo create", "Redo create", m_actionHandler,
					() => wf = servLoc.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeString(form, m_vernacularWS.Handle)));
			}
			return wf;
		}

		protected IWfiWordform CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			IWfiWordform wf = FindOrCreateWordform(form);
			int actualSize = wf.AnalysesOC.Count;
			string msg = String.Format("Wrong number of {0} analyses for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}

		protected void ExecuteIdleQueue()
		{
			foreach (var task in m_idleQueue)
				task.Delegate(task.Parameter);
			m_idleQueue.Clear();
		}

		private void HandleTaskUpdate(TaskReport task)
		{
			m_taskDetailsString = task?.Details?.ToString();
		}
		#endregion // Non-tests

		#region Setup and TearDown
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_vernacularWS = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_idleQueue = new IdleQueue {IsPaused = true};
		}

		public override void FixtureTeardown()
		{
			m_vernacularWS = null;
			m_idleQueue.Dispose();
			m_idleQueue = null;

			base.FixtureTeardown();
		}

		public override void TestTearDown()
		{
			UndoAll();
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undoable UOW and Undo everything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UndoAll()
		{
			// Undo the UOW (or more than one of them, if the test made new ones).
			while (m_actionHandler.CanUndo())
				m_actionHandler.Undo();

			// Need to 'Commit' to clear out redo stack,
			// since nothing is really saved.
			m_actionHandler.Commit();
		}
		#endregion Setup and TearDown

		#region Tests
		[Test]
		public void TryAWord()
		{
			XDocument lowerXDoc = new XDocument(new XComment("cats"));
			var parserWorker = new ParserWorker(Cache, HandleTaskUpdate, m_idleQueue, null);
			parserWorker.Parser = new TestParserClass(null, lowerXDoc);

			// SUT
			parserWorker.TryAWord("cats", false, null);
			Assert.AreEqual(m_taskDetailsString, lowerXDoc.ToString());
		}

		[Test]
		public void UpdateWordform()
		{
			IWfiWordform catsLowerTest = CheckAnalysisSize("cats", 0, true);
			IWfiWordform catsUpperTest = CheckAnalysisSize("Cats", 0, true);
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			ParseResult lowerResult = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Noun
				ILexEntry catN = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				IMoStemAllomorph catNForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				catN.AlternateFormsOS.Add(catNForm);
				catNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catn", m_vernacularWS.Handle);
				IMoStemMsa catNMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				catN.MorphoSyntaxAnalysesOC.Add(catNMsa);

				lowerResult = new ParseResult(new[]
				{
					new ParseAnalysis(new[]
					{
						new ParseMorph(catNForm, catNMsa),
					})
				});
			});

			var parserWorker = new ParserWorker(Cache, HandleTaskUpdate, m_idleQueue, null);
			parserWorker.Parser = new TestParserClass(lowerResult, null);

			// SUT
			// Parsing an uppercase wordform should cause the lowercase wordform to be parsed.
			// The uppercase wordform doesn't get a parse.
			var bVal = parserWorker.UpdateWordform(catsUpperTest, ParserPriority.Low);
			ExecuteIdleQueue();
			Assert.IsTrue(bVal);
			CheckAnalysisSize("Cats", 0, false);
			CheckAnalysisSize("cats", 1, false);

			// SUT
			// The lowercase wordform has already been parsed.
			bVal = parserWorker.UpdateWordform(catsLowerTest, ParserPriority.Low);
			ExecuteIdleQueue();
			Assert.IsTrue(bVal);
			CheckAnalysisSize("Cats", 0, false);
			CheckAnalysisSize("cats", 1, false);
		}
		#endregion // Tests
	}

	/// <summary>
	/// IParser class used for testing the ParseWorker.
	/// This test class only returns results for lowercase words. Uppercase
	/// words return empty results.
	/// </summary>
	public class TestParserClass : DisposableBase, IParser
	{
		private readonly ParseResult m_lowerResult;
		private readonly XDocument m_lowerXDoc;

		public TestParserClass(ParseResult lowerResult, XDocument lowerXDoc)
		{
			m_lowerResult = lowerResult;
			m_lowerXDoc = lowerXDoc;
		}

		public bool IsUpToDate() { return true; }

		public void Update() { }

		public void Reset() { }

		/// <summary>
		/// If the input word is lowercase then return the lowercase results.
		/// Else return empty results.
		/// </summary>
		public ParseResult ParseWord(string word)
		{
			if (word == word.ToLower())
			{
				return m_lowerResult;
			}
			else
			{
				return new ParseResult(Enumerable.Empty<ParseAnalysis>());
			}
		}

		/// <summary>
		/// If the input word is lowercase then return the lowercase results.
		/// Else return empty results.
		/// </summary>
		public XDocument ParseWordXml(string word)
		{
			if (word == word.ToLower())
			{
				return m_lowerXDoc;
			}
			else
			{
				return new XDocument();
			}
		}

		/// <summary>
		/// Not implemented.
		/// </summary>
		public XDocument TraceWordXml(string word, IEnumerable<int> selectTraceMorphs)
		{
			return null;
		}
	}
}
