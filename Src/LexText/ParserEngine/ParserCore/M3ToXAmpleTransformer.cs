//#define DumpCleanedMode
// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: M3ToXAmpleTransformer.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.WordWorks.GAFAWS;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by XAmple.
	/// </summary>
	internal class M3ToXAmpleTransformer
	{
		protected string m_outputDirectory;
		protected TaskReport m_topLevelTask;
		protected string m_database;
		private TraceSwitch tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToXAmpleTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToXAmpleTransformer(string database)
		{
			m_database = database;
			m_outputDirectory = System.IO.Path.GetTempPath();
		}

		protected XmlDocument TransformDomToDom(string transformName, XmlDocument inputDOM)
		{
			XslCompiledTransform transformer = new XslCompiledTransform();
			transformer.Load(PathToWordworksTransforms() + transformName);
			MemoryStream ms = new MemoryStream();
			transformer.Transform(inputDOM, null, ms);
			ms.Seek(0, SeekOrigin.Begin);
			XmlDocument output = new XmlDocument();
			output.Load(ms); //ENHANCE: this line is SLOW
			return output;
		}

		protected void TransformDomToFile(string transformName, XmlDocument inputDOM, string outputName)
		{
			using (m_topLevelTask.AddSubTask(String.Format(ParserCoreStrings.ksCreatingX, outputName)))
			{
#if UsingDotNetAndNotUsingUtilityTool
				TextWriter writer = null;
				try
				{
					XslCompiledTransform transformer = new XslCompiledTransform();
					transformer.Load(PathToWordworksTransforms() + transformName);
					writer = File.CreateText(m_outputDirectory + "\\" + outputName);
					transformer.Transform(inputDOM, new XsltArgumentList(), writer, null);
				}
				finally
				{
					if (writer != null)
						writer.Close();
				}
#else
				SIL.Utils.XmlUtils.TransformDomToFile(Path.Combine(PathToWordworksTransforms(), transformName), inputDOM, Path.Combine(m_outputDirectory, outputName));
#endif
			}
		}

		internal void PrepareTemplatesForXAmpleFiles(ref XmlDocument domModel, XmlDocument domTemplate, TaskReport parentTask)
		{
			using (m_topLevelTask = parentTask.AddSubTask(ParserCoreStrings.ksPreparingTemplatesForXAmple))
			{
				// get top level POS that has at least one template with slots
				XmlNodeList templateNodeList = domTemplate.SelectNodes("//PartsOfSpeech/PartOfSpeech[descendant-or-self::MoInflAffixTemplate[PrefixSlots or SuffixSlots]]");
				foreach (XmlNode templateNode in templateNodeList)
				{
					// transform the POS that has templates to GAFAWS format
					string sGafawsFile = m_database + "gafawsData.xml";
					TransformPOSInfoToGafawsInputFormat(templateNode, sGafawsFile);
					string sResultFile = ApplyGafawsAlgorithm(sGafawsFile);
					//based on results of GAFAWS, modify the model dom by inserting orderclass in slots
					InsertOrderclassInfo(ref domModel, sResultFile);
				}
			}
		}

		protected void InsertOrderclassInfo(ref XmlDocument domModel, string sResultFile)
		{
			// Check for a valid filename (see LT-6472).
			if (String.IsNullOrEmpty(sResultFile))
				return;
			XmlDocument dom = new XmlDocument();
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

		private void BuildOrderclassElementsString(out StringBuilder sb, XmlNode gafawsNode)
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
			PositionAnalyzer pa = new PositionAnalyzer();
			string sGafawsInputFile = Path.Combine(m_outputDirectory, sGafawsFile);
			return pa.Process(sGafawsInputFile);
		}
		/// <summary>
		/// transform the POS that has templates to GAFAWS format
		/// </summary>
		/// <param name="templateNode"></param>
		protected void TransformPOSInfoToGafawsInputFormat(XmlNode templateNode, string sGafawsFile)
		{
			XmlDocument dom = new XmlDocument();
			dom.CreateElement("GAFAWSData"); // create root element
			dom.InnerXml = templateNode.OuterXml;	 // copy in POS elements
			TransformDomToFile("FxtM3ParserToGAFAWS.xsl", dom, sGafawsFile);
		}

		internal void MakeAmpleFiles(XmlDocument model, TaskReport parentTask, ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			using (m_topLevelTask = parentTask.AddSubTask(ParserCoreStrings.ksMakingXAmpleFiles))
			{
				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarOnly ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData)
				{
					DateTime startTime = DateTime.Now;
					TransformDomToFile("FxtM3ParserToXAmpleADCtl.xsl", model, m_database + "adctl.txt");
					TransformDomToFile("FxtM3ParserToToXAmpleGrammar.xsl", model, m_database + "gram.txt");
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "Grammar XSLTs took : " + ttlTicks.ToString());
					// TODO: Putting this here is not necessarily efficient because it happens every time
					//       the parser is run.  It would be more efficient to run this only when the user
					//       is trying a word.  But we need the "model" to apply this transform an it is
					//       available here, so we're doing this for now.
					startTime = DateTime.Now;
					string sName = m_database + "XAmpleWordGrammarDebugger.xsl";
					TransformDomToFile("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl", model, sName);
					ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "WordGrammarDebugger XSLT took : " + ttlTicks.ToString());
				}
				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.LexiconOnly ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData)
				{
					DateTime startTime = DateTime.Now;
					TransformDomToFile("FxtM3ParserToXAmpleLex.xsl", model, m_database + "lex.txt");
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "Lex XSLT took : " + ttlTicks.ToString());
				}
			}
		}

		/// <summary>
		/// The file system path to the XSLT transforms used by WordWorks.
		/// </summary>
		/// <returns></returns>
		protected string PathToWordworksTransforms()
		{
			return DirectoryFinder.FWCodeDirectory + @"\Language Explorer\Transforms\";
		}
	}
}
