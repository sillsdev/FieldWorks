// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.LCModel.Utils;
using SIL.WordWorks.GAFAWS.PositionAnalysis;

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

		/// <summary />
		public M3ToXAmpleTransformer(string database)
		{
			m_database = database;
		}

		private XslCompiledTransform AdctlTransform => m_adctlTransform ?? (m_adctlTransform = CreateTransform("FxtM3ParserToXAmpleADCtl"));

		private XslCompiledTransform GafawsTransform => m_gafawsTransform ?? (m_gafawsTransform = CreateTransform("FxtM3ParserToGAFAWS"));

		private XslCompiledTransform LexTransform => m_lexTransform ?? (m_lexTransform = CreateTransform("FxtM3ParserToXAmpleLex"));

		private XslCompiledTransform GrammarTransform => m_grammarTransform ?? (m_grammarTransform = CreateTransform("FxtM3ParserToToXAmpleGrammar"));

		private XslCompiledTransform GrammarDebuggingTransform => m_grammarDebuggingTransform ?? (m_grammarDebuggingTransform = CreateTransform("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT"));

		public void PrepareTemplatesForXAmpleFiles(XDocument domModel, XDocument domTemplate)
		{
			Debug.Assert(domTemplate.Root != null);
			// get top level POS that has at least one template with slots
			foreach (var templateElem in domTemplate.Root.Elements("PartsOfSpeech").Elements("PartOfSpeech")
				.Where(pe => pe.Elements("AffixTemplates").Elements("MoInflAffixTemplate").Any(te => te.Element("PrefixSlots") != null || te.Element("SuffixSlots") != null)))
			{
				// transform the POS that has templates to GAFAWS format
				var gafawsFile = m_database + "gafawsData.xml";
				TransformPosInfoToGafawsInputFormat(templateElem, gafawsFile);
				var resultFile = ApplyGafawsAlgorithm(gafawsFile);
				//based on results of GAFAWS, modify the model dom by inserting orderclass in slots
				InsertOrderclassInfo(domModel, resultFile);
			}
		}

		private void InsertOrderclassInfo(XDocument domModel, string resultFile)
		{
			// Check for a valid filename (see LT-6472).
			if (string.IsNullOrEmpty(resultFile))
			{
				return;
			}
			var dom = XDocument.Load(resultFile);
			foreach (var gafawsElem in dom.Elements("GAFAWSData").Elements("Morphemes").Elements("Morpheme"))
			{
				var morphemeID = (string)gafawsElem.Attribute("MID");
				if (morphemeID == "R")
				{
					continue; // skip the stem/root node
				}
				var modelElem = domModel.Descendants("MoInflAffixSlot").First(e => ((string)e.Attribute("Id")) == morphemeID);
				modelElem.Add(new XElement("orderclass",
					new XElement("minValue", (string)gafawsElem.Attribute("StartCLIDREF")),
					new XElement("maxValue", (string)gafawsElem.Attribute("EndCLIDREF"))));
			}
		}

		private string ApplyGafawsAlgorithm(string gafawsFile)
		{
			var gafawsInputFile = Path.Combine(Path.GetTempPath(), gafawsFile);
			var pa = new PositionAnalyzer();
			return pa.Process(gafawsInputFile);
		}

		/// <summary>
		/// transform the POS that has templates to GAFAWS format
		/// </summary>
		private void TransformPosInfoToGafawsInputFormat(XElement templateElem, string gafawsFile)
		{
			var dom = new XDocument(new XElement(templateElem));
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), gafawsFile)))
			{
				GafawsTransform.Transform(dom.CreateNavigator(), null, writer);
			}
		}

		public void MakeAmpleFiles(XDocument model)
		{
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "adctl.txt")))
			{
				AdctlTransform.Transform(model.CreateNavigator(), null, writer);
			}
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "gram.txt")))
			{
				GrammarTransform.Transform(model.CreateNavigator(), null, writer);
			}
			// TODO: Putting this here is not necessarily efficient because it happens every time
			//       the parser is run.  It would be more efficient to run this only when the user
			//       is trying a word.  But we need the "model" to apply this transform an it is
			//       available here, so we're doing this for now.
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "XAmpleWordGrammarDebugger.xsl")))
			{
				GrammarDebuggingTransform.Transform(model.CreateNavigator(), null, writer);
			}
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "lex.txt")))
			{
				LexTransform.Transform(model.CreateNavigator(), null, writer);
			}
		}

		private XslCompiledTransform CreateTransform(string xslName)
		{
			return CreateTransform(xslName, "ApplicationTransforms");
		}

		public static XslCompiledTransform CreateTransform(string xslName, string assemblyName)
		{
			var transform = new XslCompiledTransform();
			var type = Type.GetType($"{xslName},{assemblyName}");
			var libPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
			if (MiscUtils.IsDotNet)
			{
				// Assumes the XSL has been precompiled.  xslName is the name of the precompiled class
				Debug.Assert(type != null);
				transform.Load(type);
			}
			else
			{
				var transformAssembly = Assembly.LoadFrom(Path.Combine(libPath, assemblyName + ".dll"));
				using (var stream = transformAssembly.GetManifestResourceStream(xslName + ".xsl"))
				{
					Debug.Assert(stream != null);
					using (var reader = XmlReader.Create(stream))
					{
						transform.Load(reader, new XsltSettings(true, false), new XmlResourceResolver(transformAssembly));
					}
				}
			}
			return transform;
		}

		private sealed class XmlResourceResolver : XmlUrlResolver
		{
			private readonly Assembly m_assembly;

			internal XmlResourceResolver(Assembly assembly)
			{
				m_assembly = assembly;
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				return baseUri == null ? new Uri($"res://{relativeUri}") : base.ResolveUri(baseUri, relativeUri);
			}

			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				switch (absoluteUri.Scheme)
				{
					case "res":
						return m_assembly.GetManifestResourceStream(absoluteUri.OriginalString.Substring(6));

					default:
						// Handle file:// and http://
						// requests from the XmlUrlResolver base class
						return base.GetEntity(absoluteUri, role, ofObjectToReturn);
				}
			}
		}
	}
}