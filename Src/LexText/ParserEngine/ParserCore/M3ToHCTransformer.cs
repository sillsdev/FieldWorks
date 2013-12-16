//#define DumpCleanedMode
// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3ParserTransformer.cs
// Responsibility: John Hatton

using System;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by a parser.
	/// </summary>
	internal class M3ToHCTransformer : M3ToParserTransformerBase
	{

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToHCTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToHCTransformer(string database, Action<TaskReport> taskUpdateHandler)
			: base(database, taskUpdateHandler)
		{
		}

		public void MakeHCFiles(ref XmlDocument model)
		{
			using (var task = new TaskReport(ParserCoreStrings.ksMakingHCFiles, m_taskUpdateHandler))
			{
				var startTime = DateTime.Now;
				TransformDomToFile("FxtM3ParserToHCInput.xsl", model, m_database + "HCInput.xml", task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Lex XSLT took : " + (DateTime.Now.Ticks - startTime.Ticks));

				startTime = DateTime.Now;
				TransformDomToFile("FxtM3ParserToToXAmpleGrammar.xsl", model, m_database + "gram.txt", task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Grammar XSLTs took : " + (DateTime.Now.Ticks - startTime.Ticks));

				// TODO: Putting this here is not necessarily efficient because it happens every time
				//       the parser is run.  It would be more efficient to run this only when the user
				//       is trying a word.  But we need the "model" to apply this transform and it is
				//       available here, so we're doing this for now.
				startTime = DateTime.Now;
				string sName = m_database + "XAmpleWordGrammarDebugger.xsl";
				TransformDomToFile("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl", model, sName, task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "WordGrammarDebugger XSLT took : " + (DateTime.Now.Ticks - startTime.Ticks));
			}
		}
	}
}
