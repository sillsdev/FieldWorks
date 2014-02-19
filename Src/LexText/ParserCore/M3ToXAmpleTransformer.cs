//#define DumpCleanedMode
// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3ToXAmpleTransformer.cs
// Responsibility: John Hatton

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Linq;
using SIL.WordWorks.GAFAWS.PositionAnalysis;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by XAmple.
	/// </summary>
	internal class M3ToXAmpleTransformer : M3ToParserTransformerBase
	{
		private XslCompiledTransform m_gafawsTransform;
		private XslCompiledTransform m_adctlTransform;
		private XslCompiledTransform m_lexTransform;
		private readonly string m_database;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToXAmpleTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToXAmpleTransformer(string dataDir, string database)
			: base(dataDir)
		{
			m_database = database;
		}

		private XslCompiledTransform AdctlTransform
		{
			get
			{
				if (m_adctlTransform == null)
					m_adctlTransform = CreateTransform("FxtM3ParserToXAmpleADCtl");
				return m_adctlTransform;
			}
		}

		private XslCompiledTransform GafawsTransform
		{
			get
			{
				if (m_gafawsTransform == null)
					m_gafawsTransform = CreateTransform("FxtM3ParserToGAFAWS");
				return m_gafawsTransform;
			}
		}

		private XslCompiledTransform LexTransform
		{
			get
			{
				if (m_lexTransform == null)
					m_lexTransform = CreateTransform("FxtM3ParserToXAmpleLex");
				return m_lexTransform;
			}
		}

		public void PrepareTemplatesForXAmpleFiles(XDocument domModel, XDocument domTemplate)
		{
			Debug.Assert(domTemplate.Root != null);
			// get top level POS that has at least one template with slots
			foreach (XElement templateElem in domTemplate.Root.Elements("PartsOfSpeech").Elements("PartOfSpeech")
				.Where(pe => pe.Elements("AffixTemplates").Elements("MoInflAffixTemplate").Any(te => te.Element("PrefixSlots") != null || te.Element("SuffixSlots") != null)))
			{
				// transform the POS that has templates to GAFAWS format
				string gafawsFile = m_database + "gafawsData.xml";
				TransformPosInfoToGafawsInputFormat(templateElem, gafawsFile);
				string resultFile = ApplyGafawsAlgorithm(gafawsFile);
				//based on results of GAFAWS, modify the model dom by inserting orderclass in slots
				InsertOrderclassInfo(domModel, resultFile);
			}
		}

		private void InsertOrderclassInfo(XDocument domModel, string resultFile)
		{
			// Check for a valid filename (see LT-6472).
			if (String.IsNullOrEmpty(resultFile))
				return;
			XDocument dom = XDocument.Load(resultFile);
			foreach (XElement gafawsElem in dom.Elements("GAFAWSData").Elements("Morphemes").Elements("Morpheme"))
			{
				var morphemeID = (string) gafawsElem.Attribute("MID");
				if (morphemeID == "R")
					continue;  // skip the stem/root node
				XElement modelElem = domModel.Descendants("MoInflAffixSlot").First(e => ((string) e.Attribute("Id")) == morphemeID);
				modelElem.Add(new XElement("orderclass",
					new XElement("minValue", (string) gafawsElem.Attribute("StartCLIDREF")),
					new XElement("maxValue", (string) gafawsElem.Attribute("EndCLIDREF"))));
			}
		}

		private string ApplyGafawsAlgorithm(string gafawsFile)
		{
			var pa = new PositionAnalyzer();
			string gafawsInputFile = Path.Combine(Path.GetTempPath(), gafawsFile);
			return pa.Process(gafawsInputFile);
		}
		/// <summary>
		/// transform the POS that has templates to GAFAWS format
		/// </summary>
		private void TransformPosInfoToGafawsInputFormat(XElement templateElem, string gafawsFile)
		{
			var dom = new XDocument(new XElement(templateElem));
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), gafawsFile)))
				GafawsTransform.Transform(dom.CreateNavigator(), null, writer);
		}

		public void MakeAmpleFiles(XDocument model)
		{
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "adctl.txt")))
				AdctlTransform.Transform(model.CreateNavigator(), null, writer);

			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "gram.txt")))
				GrammarTransform.Transform(model.CreateNavigator(), null, writer);

			// TODO: Putting this here is not necessarily efficient because it happens every time
			//       the parser is run.  It would be more efficient to run this only when the user
			//       is trying a word.  But we need the "model" to apply this transform an it is
			//       available here, so we're doing this for now.
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "XAmpleWordGrammarDebugger.xsl")))
				GrammarDebuggingTransform.Transform(model.CreateNavigator(), null, writer);

			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "lex.txt")))
				LexTransform.Transform(model.CreateNavigator(), null, writer);
		}
	}
}
