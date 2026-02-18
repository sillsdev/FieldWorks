using HCSynthByGloss;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace SIL.HCSynthByGloss
{
	public class HCSynthByGlossDll
	{
		public bool DoTracing { get; set; } = false;
		public bool ShowTracing { get; set; } = false;
		public string HcXmlFile { get; set; } = "";
		public string GlossFile { get; set; } = "";
		public string OutputFile { get; set; } = "";
		public string kSuccess { get; } = "Success!";
		public string kError1 { get; } = "Could not find ";
		public string kHCXmlFile { get; } = "HC XML file";
		public string kGlossFile { get; } = "Gloss file";
		public string kError2 { get; } = " '";
		public string kError3 { get; } = "'";
		Language synLang;
		string glosses = "";

		public HCSynthByGlossDll(string output)
		{
			OutputFile = output;
		}

		public string SetHcXmlFile(string value)
		{
			if (!File.Exists(value))
			{
				return kError1 + kHCXmlFile + kError2 + value + kError3;
			}
			HcXmlFile = value;
			synLang = XmlLanguageLoader.Load(HcXmlFile);
			return kSuccess;
		}

		public string SetGlossFile(string value)
		{
			if (!File.Exists(value))
			{
				return kError1 + kGlossFile + kError2 + value + kError3;
			}
			GlossFile = value;
			glosses = File.ReadAllText(GlossFile, Encoding.UTF8);
			return kSuccess;
		}

		public string Process()
		{

			if (!File.Exists(HcXmlFile))
			{
				return kError1 + kHCXmlFile + kError2 + HcXmlFile + kError3;
			}
			if (!File.Exists(GlossFile))
			{
				return kError1 + kGlossFile + kError2 + GlossFile + kError3;
			}
			//Language synLang = XmlLanguageLoader.Load(HcXmlFile);
			var hcTraceManager = new HcXmlTraceManager();
			hcTraceManager.IsTracing = DoTracing;
			var srcMorpher = new Morpher(hcTraceManager, synLang);
			var synthesizer = Synthesizer.Instance;
			string synthesizedWordForms = synthesizer.SynthesizeGlosses(
				glosses,
				srcMorpher,
				synLang,
				hcTraceManager
			);
			synthesizedWordForms = synthesizedWordForms.Replace("#", " ");
			File.WriteAllText(OutputFile, synthesizedWordForms, Encoding.UTF8);
			if (hcTraceManager.IsTracing)
			{
				// we want to create a temp XML file and stuff synthesizer.Trace into it
				// then transform it to an html file and show the html file
				var tempXMlResult = CreateXmlFile(synthesizer);
				string tempHtmResult = CreateHtmResult(tempXMlResult, synthesizer);
				if (ShowTracing)
				{
					System.Diagnostics.Process.Start(tempHtmResult);
				}
			}

			return kSuccess;
		}

		private static string CreateHtmResult(string xmlFile, Synthesizer synthesizer)
		{
			string tempHtmResult = Path.Combine(Path.GetTempPath(), "HCSynthTrace.htm");
			Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var rootdir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			string basedir = rootdir;
			int i = rootdir.LastIndexOf("bin");
			if (i >= 0)
			{
				// rootdir is in development environment; adjust the value
				basedir = rootdir.Substring(0, i);
			}
			string iconPath = Path.Combine(
				basedir,
				"Language Explorer",
				"Configuration",
				"Words",
				"Analyses",
				"TraceParse"
			);
			var traceTransform = XmlUtils.CreateTransform("HCSynthByGlossFormatHCTrace", "PresentationTransforms");
			XPathDocument doc = new XPathDocument(xmlFile);

			StreamWriter result = new StreamWriter(tempHtmResult);
			XsltArgumentList argList = new XsltArgumentList();
			argList.AddParam("prmIconPath", "", iconPath);
			// we do not have access to any of the following; use defaults
			//argList.AddParam("prmAnalysisFont", "", m_language.NTFontFace);
			//argList.AddParam("prmAnalysisFontSize", "", m_language.NTFontSize.ToString());
			//argList.AddParam("prmVernacularFont", "", m_language.LexFontFace);
			//argList.AddParam("prmVernacularFontSize", "", m_language.LexFontSize.ToString());
			//argList.AddParam("prmVernacularRTL", "", m_language.NTColorName);
			argList.AddParam("prmShowTrace", "", "true");
			traceTransform.Transform(doc, argList, result);
			result.Close();
			return tempHtmResult;
		}

		private string CreateXmlFile(Synthesizer synthesizer)
		{
			string tempXmlResult = Path.Combine(Path.GetTempPath(), "HCSynthTrace.xml");
			File.WriteAllText(tempXmlResult, synthesizer.Trace.ToString());
			return tempXmlResult;
		}

	}
}
