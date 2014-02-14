// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// THIS NEEDS TO BE REFACTORED!!
//
// File: XAmpleTrace.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		XAmpleTrace - Deal with results of an XAmple trace
// </remarks>

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for XAmpleTrace.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_mediator is a reference")]
	public class XAmpleTrace : ParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		private static ParserTraceUITransform TraceTransform
		{
			get
			{
				if (s_traceTransform == null)
					s_traceTransform = new ParserTraceUITransform("FormatXAmpleTrace.xsl");
				return s_traceTransform;
			}
		}

		private readonly Mediator m_mediator;

		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public XAmpleTrace(Mediator mediator)
		{
			m_mediator = mediator;
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <param name="isTrace"></param>
		/// <returns>URL of the resulting HTML page</returns>
		public override string CreateResultPage(XDocument result, bool isTrace)
		{
			ParserTraceUITransform transform;
			string baseName;
			if (isTrace)
			{
				WordGrammarDebugger = new XAmpleWordGrammarDebugger(m_mediator, result);
				transform = TraceTransform;
				baseName = "XAmpleTrace";
			}
			else
			{
				transform = ParseTransform;
				baseName = "XAmpleParse";
			}
			return transform.Transform(m_mediator, result, baseName);
		}
	}
}
