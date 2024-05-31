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

using System.Linq;
using System.Xml.Linq;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for XAmpleTrace.
	/// </summary>
	public class XAmpleTrace : IParserTrace
	{
		private static ParserTraceUITransform s_traceTransform;
		private static ParserTraceUITransform TraceTransform
		{
			get
			{
				if (s_traceTransform == null)
					s_traceTransform = new ParserTraceUITransform("FormatXAmpleTrace");
				return s_traceTransform;
			}
		}

		private static ParserTraceUITransform s_parseTransform;
		private static ParserTraceUITransform ParseTransform
		{
			get
			{
				if (s_parseTransform == null)
					s_parseTransform = new ParserTraceUITransform("FormatXAmpleParse");
				return s_parseTransform;
			}
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <param name="isTrace"></param>
		/// <returns>URL of the resulting HTML page</returns>
		public string CreateResultPage(PropertyTable propertyTable, XDocument result, bool isTrace)
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
				result = FixAnyDoubleQuotes(result);
			}
			return transform.Transform(propertyTable, result, baseName);
		}

		/// <summary>
		/// Convert any &amp;quot; sequences in the form to &quot; so it displays correctly
		/// </summary>
		/// <param name="result">The XAmple XML result</param>
		/// <returns>Corrected result</returns>
		private static XDocument FixAnyDoubleQuotes(XDocument result)
		{
			var forms = result.Descendants("form");
			var elem = forms.FirstOrDefault();
			if (elem != null)
			{
				result = FixAnyDoubleQuotes(result, elem.Value);
			}
			return result;
		}

		public static XDocument FixAnyDoubleQuotes(XDocument result, string nodeValue)
		{
			if (nodeValue != null && nodeValue.Contains("&quot;"))
			{
				// The Contains method above compared to what is replaced below appears odd
				// but what is really "&amp;quot;" is treated as "&quot;" in the node.
				string fixedQuote = result.ToString().Replace("&amp;quot;", "&quot;");
				XDocument fixedResult = XDocument.Parse(fixedQuote);
				result = fixedResult;
			}

			return result;
		}
	}
}
