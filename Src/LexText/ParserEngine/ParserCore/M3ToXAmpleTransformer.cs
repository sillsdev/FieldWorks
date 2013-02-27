//#define DumpCleanedMode
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
// File: M3ToXAmpleTransformer.cs
// Responsibility: John Hatton
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.WordWorks.GAFAWS.PositionAnalysis;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by XAmple.
	/// </summary>
	internal class M3ToXAmpleTransformer
	{
		private readonly string m_outputDirectory;
		private readonly string m_database;
		private readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly TraceSwitch m_tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToXAmpleTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToXAmpleTransformer(string database, Action<TaskReport> taskUpdateHandler)
		{
			m_database = database;
			m_taskUpdateHandler = taskUpdateHandler;
			m_outputDirectory = Path.GetTempPath();
		}

		protected void TransformDomToFile(string transformName, XmlDocument inputDOM, string outputName, TaskReport task)
		{
			using (task.AddSubTask(String.Format(ParserCoreStrings.ksCreatingX, outputName)))
			{
				XmlUtils.TransformDomToFile(Path.Combine(DirectoryFinder.FWCodeDirectory + "/Language Explorer/Transforms/", transformName),
					inputDOM, Path.Combine(m_outputDirectory, outputName));
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		internal void PrepareTemplatesForXAmpleFiles(ref XmlDocument domModel, XmlDocument domTemplate)
		{
			using (var task = new TaskReport(ParserCoreStrings.ksPreparingTemplatesForXAmple, m_taskUpdateHandler))
			{
				// get top level POS that has at least one template with slots
				XmlNodeList templateNodeList = domTemplate.SelectNodes("//PartsOfSpeech/PartOfSpeech[descendant-or-self::MoInflAffixTemplate[PrefixSlots or SuffixSlots]]");
				foreach (XmlNode templateNode in templateNodeList)
				{
					// transform the POS that has templates to GAFAWS format
					string sGafawsFile = m_database + "gafawsData.xml";
					TransformPOSInfoToGafawsInputFormat(templateNode, sGafawsFile, task);
					string sResultFile = ApplyGafawsAlgorithm(sGafawsFile);
					//based on results of GAFAWS, modify the model dom by inserting orderclass in slots
					InsertOrderclassInfo(ref domModel, sResultFile);
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void InsertOrderclassInfo(ref XmlDocument domModel, string sResultFile)
		{
			// Check for a valid filename (see LT-6472).
			if (String.IsNullOrEmpty(sResultFile))
				return;
			var dom = new XmlDocument();
			dom.Load(sResultFile);
			XmlNodeList gafawsNodeList = dom.SelectNodes("//Morpheme");
			foreach (XmlNode gafawsNode in gafawsNodeList)
			{
				string sMorphemeId = gafawsNode.Attributes.GetNamedItem("MID").InnerText;
				if (sMorphemeId == "R")
					continue;  // skip the stem/root node
				string sXpathToMorphemeId = "//MoInflAffixSlot[@Id='" + sMorphemeId + "']";
				XmlNode modelNode = domModel.SelectSingleNode(sXpathToMorphemeId);
				StringBuilder sb;
				BuildOrderclassElementsString(out sb, gafawsNode);
				XmlElement orderclassNode = domModel.CreateElement("orderclass");
				orderclassNode.InnerXml = sb.ToString();
				modelNode.AppendChild(orderclassNode);
			}
		}

		private static void BuildOrderclassElementsString(out StringBuilder sb, XmlNode gafawsNode)
		{
			sb = new StringBuilder();
			sb.Append("<minValue>");
			sb.Append(gafawsNode.Attributes.GetNamedItem("StartCLIDREF").InnerText);
			sb.Append("</minValue> <maxValue>");
			sb.Append(gafawsNode.Attributes.GetNamedItem("EndCLIDREF").InnerText);
			sb.Append("</maxValue>");
		}

		protected string ApplyGafawsAlgorithm(string sGafawsFile)
		{
			var pa = new PositionAnalyzer();
			string sGafawsInputFile = Path.Combine(m_outputDirectory, sGafawsFile);
			return pa.Process(sGafawsInputFile);
		}
		/// <summary>
		/// transform the POS that has templates to GAFAWS format
		/// </summary>
		protected void TransformPOSInfoToGafawsInputFormat(XmlNode templateNode, string sGafawsFile, TaskReport task)
		{
			var dom = new XmlDocument();
			dom.CreateElement("GAFAWSData"); // create root element
			dom.InnerXml = templateNode.OuterXml;	 // copy in POS elements
			TransformDomToFile("FxtM3ParserToGAFAWS.xsl", dom, sGafawsFile, task);
		}

		internal void MakeAmpleFiles(XmlDocument model)
		{
			using (var task = new TaskReport(ParserCoreStrings.ksMakingXAmpleFiles, m_taskUpdateHandler))
			{
					DateTime startTime = DateTime.Now;
				TransformDomToFile("FxtM3ParserToXAmpleADCtl.xsl", model, m_database + "adctl.txt", task);
				TransformDomToFile("FxtM3ParserToToXAmpleGrammar.xsl", model, m_database + "gram.txt", task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Grammar XSLTs took : " + (DateTime.Now.Ticks - startTime.Ticks));
					// TODO: Putting this here is not necessarily efficient because it happens every time
					//       the parser is run.  It would be more efficient to run this only when the user
					//       is trying a word.  But we need the "model" to apply this transform an it is
					//       available here, so we're doing this for now.
					startTime = DateTime.Now;
					string sName = m_database + "XAmpleWordGrammarDebugger.xsl";
				TransformDomToFile("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl", model, sName, task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "WordGrammarDebugger XSLT took : " + (DateTime.Now.Ticks - startTime.Ticks));

				startTime = DateTime.Now;
				TransformDomToFile("FxtM3ParserToXAmpleLex.xsl", model, m_database + "lex.txt", task);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Lex XSLT took : " + (DateTime.Now.Ticks - startTime.Ticks));
			}
		}
	}
}
