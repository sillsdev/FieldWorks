// Copyright (c) 2003-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParserWorker.cs
// Responsibility:
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
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParserWorker.
	/// </summary>
	public class ParserWorker : FwDisposableBase
	{
		private readonly FdoCache m_cache;
		private readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly ParseFiler m_parseFiler;
		private int m_numberOfWordForms;
		private IParser m_parser;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserWorker"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserWorker(FdoCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, string dataDir)
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
					m_parser = new HCParser(cache, dataDir);
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
		public void TryAWord(string sForm, bool fDoTrace, string sSelectTraceMorphs)
		{
			CheckDisposed();

			if (sForm == null)
				throw new ArgumentNullException("sForm", "TryAWord cannot trace a Null string.");
			if (sForm == String.Empty)
				throw new ArgumentException("Can't try a word with no content.", "sForm");

			CheckNeedsUpdate();
			using (var task = new TaskReport(string.Format(ParserCoreStrings.ksTraceWordformX, sForm), m_taskUpdateHandler))
			{
				string normForm = Icu.Normalize(sForm, Icu.UNormalizationMode.UNORM_NFD);
				task.Details = fDoTrace ? m_parser.TraceWordXml(normForm, sSelectTraceMorphs) : m_parser.ParseWordXml(normForm);
			}
		}

		public bool UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			CheckDisposed();

			int wordformHash = 0;
			ITsString form = null;
			int hvo = 0;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (wordform.IsValidObject)
				{
					wordformHash = wordform.Checksum;
					form = wordform.Form.VernacularDefaultWritingSystem;
				}
			}
			// 'form' will now be null, if it could not find the wordform for whatever reason.
			// uiCRCWordform will also now be 0, if 'form' is null.
			if (form == null || string.IsNullOrEmpty(form.Text))
				return false;

			CheckNeedsUpdate();
			ParseResult result = m_parser.ParseWord(Icu.Normalize(form.Text.Replace(' ', '.'), Icu.UNormalizationMode.UNORM_NFD));
			if (wordformHash == result.GetHashCode())
				return false;

			return m_parseFiler.ProcessParse(wordform, priority, result);
		}

		private void CheckNeedsUpdate()
		{
			using (var task = new TaskReport(ParserCoreStrings.ksUpdatingGrammarAndLexicon, m_taskUpdateHandler))
			{
				if (!m_parser.IsUpToDate())
					m_parser.Update();
			}
		}

		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();

			m_parser.Reset();
			CheckNeedsUpdate();
		}
	}
}
