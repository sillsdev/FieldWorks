// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	internal sealed class XAmpleTrace : IParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		private static ParserTraceUITransform TraceTransform => s_traceTransform ?? (s_traceTransform = new ParserTraceUITransform("FormatXAmpleTrace"));
		private static ParserTraceUITransform s_parseTransform;
		private static ParserTraceUITransform ParseTransform => s_parseTransform ?? (s_parseTransform = new ParserTraceUITransform("FormatXAmpleParse"));

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		public string CreateResultPage(IPropertyTable propertyTable, XDocument result, bool isTrace)
		{
			ParserTraceUITransform transform;
			string baseName;
			if (isTrace)
			{
				transform = TraceTransform;
				baseName = "XAmpleTrace";
			}
			else
			{
				transform = ParseTransform;
				baseName = "XAmpleParse";
			}
			return transform.Transform(propertyTable, result, baseName);
		}
	}
}