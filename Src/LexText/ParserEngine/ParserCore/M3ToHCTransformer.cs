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
// File: M3ParserTransformer.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml.Xsl;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by a parser.
	/// </summary>
	internal class M3ToHCTransformer
	{
		protected string m_outputDirectory;
		protected TaskReport m_topLevelTask;
		protected string m_database;
		protected TraceSwitch tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ParserTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToHCTransformer(string database)
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

		internal void MakeHCFiles(ref XmlDocument model, TaskReport parentTask, ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			using (m_topLevelTask = parentTask.AddSubTask(ParserCoreStrings.ksMakingXAmpleFiles))
			{
				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.LexiconOnly ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData)
				{
					DateTime startTime = DateTime.Now;
					TransformDomToFile("FxtM3ParserToHCInput.xsl", model, m_database + "HCInput.xml");
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "Lex XSLT took : " + ttlTicks.ToString());
				}

				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarOnly ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData)
				{
					DateTime startTime = DateTime.Now;
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
