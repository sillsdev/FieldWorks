using System.IO;
using System.Xml.Linq;
using System.Xml.Xsl;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCTrace : ParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		private static ParserTraceUITransform TraceTransform
		{
			get
			{
				if (s_traceTransform == null)
					s_traceTransform = new ParserTraceUITransform("FormatHCTrace.xsl");
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

		public override string CreateResultPage(XDocument result, bool isTrace)
		{
			ParserTraceUITransform transform;
			string baseName;
			if (isTrace)
			{
				WordGrammarDebugger = new HCWordGrammarDebugger(m_mediator, result);
				transform = TraceTransform;
				baseName = "HCTrace";
			}
			else
			{
				transform = ParseTransform;
				baseName = "HCParse";
			}
			var args = new XsltArgumentList();
			args.AddParam("prmHCTraceLoadErrorFile", "", Path.Combine(Path.GetTempPath(), m_cache.ProjectId.Name + "HCLoadErrors.xml"));
			return transform.Transform(m_mediator, result, baseName, args);
		}
	}
}
