// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;
using System.Xml.Xsl;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache and m_mediator are references")]
	public class HCTrace : IParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		private static ParserTraceUITransform TraceTransform
		{
			get
			{
				if (s_traceTransform == null)
					s_traceTransform = new ParserTraceUITransform("FormatHCTrace");
				return s_traceTransform;
			}
		}

		private readonly Mediator m_mediator;
		private readonly FdoCache m_cache;

		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public HCTrace(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
		}

		public string CreateResultPage(XDocument result, bool isTrace)
		{
			var args = new XsltArgumentList();
			args.AddParam("prmHCTraceLoadErrorFile", "", Path.Combine(Path.GetTempPath(), m_cache.ProjectId.Name + "HCLoadErrors.xml"));
			args.AddParam("prmShowTrace", "", isTrace.ToString().ToLowerInvariant());
			return TraceTransform.Transform(m_mediator, result, isTrace ? "HCTrace" : "HCParse", args);
		}
	}
}
