// Copyright (c) 2003-2020 SIL International
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
using SIL.PlatformUtilities;
using SIL.WordWorks.GAFAWS.PositionAnalysis;

namespace SIL.FieldWorks.WordWorks.Parser.XAmple
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by XAmple.
	/// </summary>
	internal sealed class M3ToXAmpleTransformer
	{
		private XslCompiledTransform m_gafawsTransform;
		private XslCompiledTransform m_adctlTransform;
		private XslCompiledTransform m_lexTransform;
		private readonly string m_database;
		private XslCompiledTransform m_grammarTransform;
		private XslCompiledTransform m_grammarDebuggingTransform;

		/// <summary />
		internal M3ToXAmpleTransformer(string database)
		{
			m_database = database;
		}

		private XslCompiledTransform AdctlTransform => m_adctlTransform ?? (m_adctlTransform = CreateTransform("FxtM3ParserToXAmpleADCtl"));

		private XslCompiledTransform GafawsTransform => m_gafawsTransform ?? (m_gafawsTransform = CreateTransform("FxtM3ParserToGAFAWS"));

		private XslCompiledTransform LexTransform => m_lexTransform ?? (m_lexTransform = CreateTransform("FxtM3ParserToXAmpleLex"));

		private XslCompiledTransform GrammarTransform => m_grammarTransform ?? (m_grammarTransform = CreateTransform("FxtM3ParserToToXAmpleGrammar"));

		private XslCompiledTransform GrammarDebuggingTransform => m_grammarDebuggingTransform ?? (m_grammarDebuggingTransform = CreateTransform("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT"));

		internal void PrepareTemplatesForXAmpleFiles(XDocument domModel, XDocument domTemplate)
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

		private static string ApplyGafawsAlgorithm(string gafawsFile)
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

		internal void MakeAmpleFiles(XDocument model)
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

		private static XslCompiledTransform CreateTransform(string xslName)
		{
			return CreateTransform(xslName, "ApplicationTransforms");
		}

		internal static XslCompiledTransform CreateTransform(string xslName, string assemblyName)
		{
			// If we're running on Mono we enable debug for XslCompiledTransform. This works
			// around a crash somewhere deep in Mono (LT-20249). We could always pass true here,
			// but it's probably a little bit faster if we only do it where we need it.
			var transform = new XslCompiledTransform(Platform.IsMono);
			if (MiscUtils.IsDotNet)
			{
				// Assumes the XSL has been precompiled.  xslName is the name of the precompiled class
				var type = Type.GetType($"{xslName},{assemblyName}");
				Debug.Assert(type != null);
				transform.Load(type);
			}
			else
			{
				//var libPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
				//var transformAssembly = Assembly.LoadFrom(Path.Combine(libPath, assemblyName + ".dll"));
				var resolver = GetResourceResolver(assemblyName);
				var transformAssembly = ((XmlResourceResolver)resolver).Assembly;
				using (var stream = transformAssembly.GetManifestResourceStream(xslName + ".xsl"))
				{
					Debug.Assert(stream != null);
					using (var reader = XmlReader.Create(stream))
					{
						transform.Load(reader, new XsltSettings(true, false), resolver);
					}
				}
			}
			return transform;
		}

		internal static XmlResolver GetResourceResolver(string assemblyName)
		{
			var libPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
			var transformAssembly = Assembly.LoadFrom(Path.Combine(libPath, assemblyName + ".dll"));
			return new XmlResourceResolver(transformAssembly);
		}

		private sealed class XmlResourceResolver : XmlUrlResolver
		{
			internal Assembly Assembly { get; }

			internal XmlResourceResolver(Assembly assembly)
			{
				Assembly = assembly;
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				if (baseUri != null)
				{
					return base.ResolveUri(baseUri, relativeUri);
				}
#if JASONTODO
				// TODO: VS says "baseUri" is never null, so the following code is unreachable.
#endif
				var uri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);
				if (uri.IsAbsoluteUri)
				{
					if (uri.Scheme == "res")
					{
						return uri;
					}
					relativeUri = uri.AbsolutePath;
				}
				return new Uri($"res://{relativeUri}");
			}

			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				switch (absoluteUri.Scheme)
				{
					case "res":
						// strip off res://
						return Assembly.GetManifestResourceStream(absoluteUri.OriginalString.Substring(6));
					default:
						// Handle file:// and http://
						// requests from the XmlUrlResolver base class
						return base.GetEntity(absoluteUri, role, ofObjectToReturn);
				}
			}
		}
	}
}