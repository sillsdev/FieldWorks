// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
//  The name here, "worker" would lead one to think that this is the
//	class which is the top of the heap of the worker thread.
//	However, it is actually the "Scheduler" class which controls the thread and calls this.
// </remarks>
/*

throws exception:  * One way I recall is that they would create an inflectional template, but not put anything in it yet (i.e. no slots at all).
 * This causes XAmple to die because it produces a PC-PATR load error.
 * This could be fixed, of course, in the XSLT that generates the grammar file.
 * This one's on my TODO list (I've got the sticky note from Dallas)...


no exception: Try an adhoc prohibition with only one item in it

no exception: Create a compound with neither member specified or only one specified.

no exception:  Create an allomorph with an environment that is ill-formed.  (Presumably this will result in the same problem as breaking an environment for an existing allomorph.)

no exception: Create an infl affix slot with no affixes in it and then use this slot in a template (though this just might not cause the parser to fail - it would just be useless!).

*/

using System;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.ObjectModel;
using XCore;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParserWorker.
	/// </summary>
	public class ParserWorker : DisposableBase
	{
		private readonly LcmCache m_cache;
		private readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly ParseFiler m_parseFiler;
		private int m_numberOfWordForms;
		private IParser m_parser;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserWorker"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserWorker(LcmCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, string dataDir)
		{
			m_cache = cache;
			m_taskUpdateHandler = taskUpdateHandler;
			ICmAgent agent;
			switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
			{
				case "XAmple":
					m_parser = new XAmpleParser(cache, dataDir);
					agent = cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentXAmpleParser);
					break;
				case "HC":
					m_parser = new HCParser(cache);
					agent = cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentHermitCrabParser);
					break;
				default:
					throw new InvalidOperationException("The language project is set to use an unrecognized parser.");
			}
			m_parseFiler = new ParseFiler(cache, taskUpdateHandler, idleQueue, agent);
		}

		protected override void DisposeManagedResources()
		{
			if (m_parser != null)
			{
				m_parser.Dispose();
				m_parser = null;
			}
		}

		public ParseFiler ParseFiler
		{
			get
			{
				CheckDisposed();
				return m_parseFiler;
			}
		}

		/// <summary>
		/// Try parsing a wordform, optionally getting a trace of the parse
		/// </summary>
		/// <param name="sForm">the word form to parse</param>
		/// <param name="fDoTrace">whether or not to trace the parse</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		public void TryAWord(string sForm, bool fDoTrace, int[] sSelectTraceMorphs)
		{
			CheckDisposed();

			if (sForm == null)
				throw new ArgumentNullException("sForm", "TryAWord cannot trace a Null string.");
			if (sForm == String.Empty)
				throw new ArgumentException("Can't try a word with no content.", "sForm");

			CheckNeedsUpdate();
			using (var task = new TaskReport(string.Format(ParserCoreStrings.ksTraceWordformX, sForm), m_taskUpdateHandler))
			{
				// Assume that the user used the correct case.
				string normForm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sForm);
				task.Details = fDoTrace ? m_parser.TraceWordXml(normForm, sSelectTraceMorphs) : m_parser.ParseWordXml(normForm);
			}
		}

		public bool UpdateWordform(IWfiWordform wordform, ParserPriority priority, bool checkParser = false)
		{
			CheckDisposed();

			ITsString form = null;
			int hvo = 0;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (wordform.IsValidObject)
				{
					form = wordform.Form.VernacularDefaultWritingSystem;
				}
			}
			// 'form' will now be null, if it could not find the wordform for whatever reason.
			// uiCRCWordform will also now be 0, if 'form' is null.
			if (form == null || string.IsNullOrEmpty(form.Text))
			{
				// Call ProcessParse anyway to let clients know that the parser finished.
				ParseResult parseResult = new ParseResult(string.Format(ParserCoreStrings.ksHCInvalidWordform, "", 0, "", ""));
				m_parseFiler.ProcessParse(wordform, priority, parseResult, checkParser);
				return false;
			}

			CheckNeedsUpdate();
			var normalizer = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD);
			var word = normalizer.Normalize(form.Text.Replace(' ', '.'));
			ParseResult result = null;
			var stopWatch = System.Diagnostics.Stopwatch.StartNew();
			using (var task = new TaskReport(String.Format(ParserCoreStrings.ksParsingX, word), m_taskUpdateHandler))
			{
				result = m_parser.ParseWord(word);
			}
			stopWatch.Stop();
			result.ParseTime = stopWatch.ElapsedMilliseconds;

			// Try parsing the lowercase word if it is different from the original word.
			// Do this even if the uppercase word parsed successfully.
			var cf = new CaseFunctions(m_cache.ServiceLocator.WritingSystemManager.Get(form.get_WritingSystemAt(0)));
			string sLower = cf.ToLower(form.Text);

			if (sLower != form.Text)
			{
				var text = TsStringUtils.MakeString(sLower, form.get_WritingSystem(0));
				IWfiWordform lcWordform;
				// We cannot use WfiWordformServices.FindOrCreateWordform because of props change (LT-21810).
				// Only parse the lowercase version if it exists.
				if (m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(text, out lcWordform))
				{
					var lcWord = normalizer.Normalize(sLower.Replace(' ', '.'));
					ParseResult lcResult = null;
					stopWatch.Start();
					using (var task = new TaskReport(String.Format(ParserCoreStrings.ksParsingX, word), m_taskUpdateHandler))
					{
						lcResult = m_parser.ParseWord(lcWord);
					}
					stopWatch.Stop();
					lcResult.ParseTime = stopWatch.ElapsedMilliseconds;
					if (lcResult.Analyses.Count > 0 && lcResult.ErrorMessage == null)
					{
						m_parseFiler.ProcessParse(lcWordform, 0, lcResult, checkParser);
						m_parseFiler.ProcessParse(wordform, priority, result, checkParser);
						return true;
					}
				}
			}

			return m_parseFiler.ProcessParse(wordform, priority, result, checkParser);
		}

		private void CheckNeedsUpdate()
		{
			if (!m_parser.IsUpToDate())
				using (var task = new TaskReport(ParserCoreStrings.ksUpdatingGrammarAndLexicon, m_taskUpdateHandler))
				{
					m_parser.Update();
				}
		}

		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();

			m_parser.Reset();
			CheckNeedsUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be used for tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IParser Parser { set => m_parser = value; }

	}
}
