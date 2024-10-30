// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Linq;
using SIL.Utils;
using SIL.WordWorks.GAFAWS.PositionAnalysis;
using System.Collections.Generic;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by XAmple.
	/// </summary>
	internal class M3ToXAmpleTransformer
	{
		private XslCompiledTransform m_gafawsTransform;
		private XslCompiledTransform m_adctlTransform;
		private XslCompiledTransform m_lexTransform;
		private readonly string m_database;
		private XslCompiledTransform m_grammarTransform;
		private XslCompiledTransform m_grammarDebuggingTransform;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToXAmpleTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToXAmpleTransformer(string database)
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

		private XslCompiledTransform GrammarTransform
		{
			get
			{
				if (m_grammarTransform == null)
					m_grammarTransform = CreateTransform("FxtM3ParserToToXAmpleGrammar");
				return m_grammarTransform;
			}
		}

		private XslCompiledTransform GrammarDebuggingTransform
		{
			get
			{
				if (m_grammarDebuggingTransform == null)
					m_grammarDebuggingTransform = CreateTransform("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT");
				return m_grammarDebuggingTransform;
			}
		}

		public void PrepareTemplatesForXAmpleFiles(XDocument domModel, XDocument domTemplate)
		{
			Debug.Assert(domTemplate.Root != null);
			// get top level POS that has at least one template with slots
			foreach (XElement templateElem in domTemplate.Root.Elements("PartsOfSpeech").Elements("PartOfSpeech")
				.Where(pe => pe.DescendantsAndSelf().Elements("AffixTemplates").Elements("MoInflAffixTemplate").Any(te => te.Element("PrefixSlots") != null || te.Element("SuffixSlots") != null)))
			{
				DefineUndefinedSlots(templateElem);
				// transform the POS that has templates to GAFAWS format
				string gafawsFile = m_database + "gafawsData.xml";
				TransformPosInfoToGafawsInputFormat(templateElem, gafawsFile);
				string resultFile = ApplyGafawsAlgorithm(gafawsFile);
				//based on results of GAFAWS, modify the model dom by inserting orderclass in slots
				InsertOrderclassInfo(domModel, resultFile);
			}
		}

		/// <summary>
		/// Define undefined slots found in templateElem in AffixSlots.
		/// </summary>
		private void DefineUndefinedSlots(XElement templateElem)
		{
			ISet<string> undefinedSlots = new HashSet<string>();
			GetUndefinedSlots(templateElem, undefinedSlots);
			if (undefinedSlots.Count == 0)
				return;
			// Add undefined slots to AffixSlots.
			foreach (XElement elem in templateElem.Elements())
			{
				if (elem.Name == "AffixSlots")
				{
					foreach (string slotId in undefinedSlots)
					{
						XElement slot = new XElement("MoInflAffixSlot");
						slot.SetAttributeValue("Id", slotId);
						elem.Add(slot);
					}
					break;
				}
			}
		}

		/// <summary>
		/// Get slots that are not defined in the scope of their use.
		/// Slots are used in PrefixSlots and SuffixSlots.
		/// Slots are defined in AffixSlots.
		/// </summary>
		/// <param name="element"></param>
		/// <param name="undefinedSlots"></param>
		private void GetUndefinedSlots(XElement element, ISet<string> undefinedSlots)
		{
			// Get undefined slots recursively to handle scope correctly.
			foreach (XElement elem in element.Elements())
			{
				GetUndefinedSlots(elem, undefinedSlots);
			}
			// Record slots where they are used.
			if (element.Name == "PrefixSlots" || element.Name == "SuffixSlots")
			{
				undefinedSlots.Add((string) element.Attribute("dst"));
			}
			// Remove undefined slots from below that are defined at this level.
			// NB: This must happen after we recursively get undefined slots.
			XElement affixSlotsElem = element.Element("AffixSlots");
			if (affixSlotsElem != null)
			{
				foreach (XElement slot in affixSlotsElem.Elements())
				{
					undefinedSlots.Remove((string)slot.Attribute("Id"));
				}
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

		private XslCompiledTransform CreateTransform(string xslName)
		{
			return XmlUtils.CreateTransform(xslName, "ApplicationTransforms");
		}
	}
}
