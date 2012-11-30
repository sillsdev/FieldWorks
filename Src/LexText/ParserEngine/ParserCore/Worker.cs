// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParserWorker.cs
// Responsibility:
//
// <remarks>
//  The name here, "worker" would lead one to think that this is the
//	class which is the top of the heap of the worker thread.
//	However, it is actually the "Scheduler" class which controls the thread and calls this.
// </remarks>
// --------------------------------------------------------------------------------------------
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
using System.Diagnostics;
using System.Xml;

using CodeProject.ReiMiyasaka;

using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParserWorker.
	/// </summary>
	public abstract class ParserWorker : FwDisposableBase
	{
		protected readonly FdoCache m_cache;
		protected readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly ParseFiler m_parseFiler;
		private long m_ticksParser;
		private int m_numberOfWordForms;
		protected readonly M3ParserModelRetriever m_retriever;

		protected string m_projectName;
		protected TraceSwitch m_tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserWorker"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected ParserWorker(FdoCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, ICmAgent agent)
		{
			m_cache = cache;
			m_taskUpdateHandler = taskUpdateHandler;
			m_parseFiler = new ParseFiler(cache, taskUpdateHandler, idleQueue, agent);
			m_projectName = cache.ProjectId.Name;
			m_retriever = new M3ParserModelRetriever(m_cache, m_taskUpdateHandler);
			Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "ParserWorker(): CurrentThreadId = " + Win32.GetCurrentThreadId());
		}

		protected override void DisposeManagedResources()
			{
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Total number of wordforms parsed = " + m_numberOfWordForms);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Total time for parser = " + m_ticksParser);

				if (m_numberOfWordForms != 0)
				{
					long lAvg = m_ticksParser/m_numberOfWordForms;
					Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Average time for parser = " + lAvg);
				}

				m_parseFiler.Dispose();
			m_retriever.Dispose();
		}

		public ParseFiler ParseFiler
		{
			get
			{
				CheckDisposed();
				return m_parseFiler;
			}
		}

		protected abstract string ParseWord(string form, int hvoWordform);
		protected abstract string TraceWord(string form, string selectTraceMorphs);

		private string GetOneWordformResult(int hvoWordform, string form)
		{
			Debug.Assert(hvoWordform > 0, "Wordform ID must be greater than zero.");
			Debug.Assert(form != null, "Wordform form must not be null.");

			Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "GetOneWordformResult(): CurrentThreadId = " + Win32.GetCurrentThreadId());
			var startTime = DateTime.Now;
			var results = ParseWord(Icu.Normalize(form, Icu.UNormalizationMode.UNORM_NFD), hvoWordform);
			long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
			m_ticksParser += ttlTicks;
			m_numberOfWordForms++;
			Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "ParseWord(" + form + ") took : " + ttlTicks);
			return Icu.Normalize(results, Icu.UNormalizationMode.UNORM_NFD);
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
				var normForm = Icu.Normalize(sForm, Icu.UNormalizationMode.UNORM_NFD);
				var result = fDoTrace ? TraceWord(normForm, sSelectTraceMorphs) : ParseWord(normForm, 0);
				if (fDoTrace)
					task.Details = result;
				else
					task.Details = Icu.Normalize(result, Icu.UNormalizationMode.UNORM_NFD);
			}
		}

		public bool UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			CheckDisposed();

			uint crcWordform = 0;
			ITsString form = null;
			int hvo = 0;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (wordform.IsValidObject)
				{
					crcWordform = (uint) wordform.Checksum;
					form = wordform.Form.VernacularDefaultWritingSystem;
					hvo = wordform.Hvo;
				}
			}
			// 'form' will now be null, if it could not find the wordform for whatever reason.
			// uiCRCWordform will also now be 0, if 'form' is null.
			if (form == null || string.IsNullOrEmpty(form.Text))
				return false;

			CheckNeedsUpdate();
			string result = GetOneWordformResult(hvo, form.Text.Replace(' ', '.')); // LT-7334 to allow for phrases
			uint crc = CrcStream.GetCrc(result);
			if (crcWordform == crc)
				return false;

			return m_parseFiler.ProcessParse(priority, result, crc);
		}

		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();

			Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "ParserWorker.ReloadGrammarAndLexicon");
			m_retriever.Reset();
			CheckNeedsUpdate();
			}

		private void CheckNeedsUpdate()
		{
			DateTime startTime = DateTime.Now;
			if (m_retriever.RetrieveModel())
			{
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Model retrieval took : " + (DateTime.Now.Ticks - startTime.Ticks));
				XmlDocument fxtResult = m_retriever.ModelDom;
				XmlDocument gafawsFxtResult = m_retriever.TemplateDom;
				LoadParser(ref fxtResult, gafawsFxtResult);
			}
		}

		protected abstract void LoadParser(ref XmlDocument model, XmlDocument template);
	}
}
