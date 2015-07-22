using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;
using System.Xml.Xsl;
using SIL.CoreImpl;
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

		public string CreateResultPage(PropertyTable propertyTable, XDocument result, bool isTrace)
		{
			var args = new XsltArgumentList();
			args.AddParam("prmHCTraceLoadErrorFile", "", Path.Combine(Path.GetTempPath(), propertyTable.GetValue<FdoCache>("cache").ProjectId.Name + "HCLoadErrors.xml"));
			args.AddParam("prmShowTrace", "", isTrace.ToString().ToLowerInvariant());
			return TraceTransform.Transform(propertyTable, result, isTrace ? "HCTrace" : "HCParse", args);
		}
	}
}
