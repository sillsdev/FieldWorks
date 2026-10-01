// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using NUnit.Framework;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>Checks the HTML trace presentation using synthetic callback XML.</summary>
	[TestFixture]
	public class HCTraceTransformTests
	{
		private XslCompiledTransform m_transform;

		/// <summary>Loads the source stylesheet and its includes from this worktree.</summary>
		[OneTimeSetUp]
		public void LoadTransform()
		{
			var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (directory != null && !File.Exists(Path.Combine(directory.FullName, "FieldWorks.proj")))
				directory = directory.Parent;
			Assert.That(directory, Is.Not.Null, "The tests must run within the FieldWorks worktree.");
			m_transform = new XslCompiledTransform();
			m_transform.Load(Path.Combine(directory.FullName,
				"Src", "Transforms", "Presentation", "FormatHCTrace.xsl"),
				new XsltSettings(true, false), new XmlUrlResolver());
		}

		/// <summary>Separates environment alternatives without punctuation for unrelated
		/// fields.</summary>
		[TestCase(false)]
		[TestCase(true)]
		public void EnvironmentFailure_DisplaysEachAlternative(bool rtl)
		{
			var node = new XElement("ParseCompleteTrace", new XAttribute("success", false),
				new XElement("Result", "a"),
				new XElement("FailureReason", new XAttribute("type", "environment"),
					new XElement("Environment", "/ p _"), new XElement("Environment", "/ _ s"),
					Allomorph()));

			var html = Transform(node, rtl);

			Assert.That(Text(html), Does.Contain("Environment incorrect for allomorph 'root': / p _, / _ s."));
			Assert.That(html, Does.Contain("id=\"42\""));
		}

		/// <summary>Displays prefix and suffix environments even without an allomorph
		/// lookup.</summary>
		[Test]
		public void AffixEnvironmentFailure_DisplaysBothHalves()
		{
			var node = new XElement("MorphologicalRuleSynthesisTrace",
				new XElement("MorphologicalRule", new XAttribute("type", "affix"), "circumfix"),
				new XElement("Output", "*None*"),
				new XElement("FailureReason", new XAttribute("type", "environment"),
					new XElement("Environment", "/ p _"), new XElement("Environment", "/ _ s")));

			Assert.That(Text(Transform(node)), Does.Contain("Environment incorrect: / p _, / _ s."));
		}

		/// <summary>Displays the lists of actual and required compounding exception
		/// features.</summary>
		[Test]
		public void CompoundingFailure_SeparatesEachRestriction()
		{
			var node = CompoundFailure("missingProdRestrict");
			node.Element("FailureReason").Add(
				new XElement("StemProdRestricts", new XElement("MprFeature", "actual1"),
					new XElement("MprFeature", "actual2")),
				new XElement("RuleProdRestricts", new XElement("MprFeature", "required1"),
					new XElement("MprFeature", "required2")));

			var text = Text(Transform(node));

			Assert.That(text, Does.Contain("actual1 and actual2,"));
			Assert.That(text, Does.Contain("required1 or required2."));
		}

		/// <summary>Displays an unknown compounding cause without guessing a failed
		/// gate.</summary>
		[TestCase("unknown")]
		[TestCase("unrecognized")]
		public void CompoundingFailure_UnknownCauseIsExplicit(string type)
		{
			Assert.That(Text(Transform(CompoundFailure(type))), Does.Contain("Reason: Unknown reason."));
		}

		/// <summary>Shows blocking and preserves access to the replacement's successful
		/// parse.</summary>
		[TestCase(false)]
		[TestCase(true)]
		public void Blocked_DisplaysEntryAndReplacementOutput(bool rtl)
		{
			var synthesis = new XElement("WordSynthesisTrace",
				new XElement("BlockedTrace", new XElement("Rule", "synthetic past"),
					new XElement("BlockingEntry", Allomorph()), new XElement("Output", "vem")),
				new XElement("ParseCompleteTrace", new XAttribute("success", true),
					new XElement("Result", "vem")));
			var lookup = new XElement("LexLookupTrace", synthesis);

			var html = Transform(lookup, rtl);
			var text = Text(html);

			Assert.That(text, Does.Contain("Blocked"));
			Assert.That(text, Does.Contain("synthetic past"));
			Assert.That(text, Does.Contain("root"));
			Assert.That(text, Does.Contain("Output = vem"));
			Assert.That(text, Does.Contain("Parse succeeded"));
			Assert.That(html, Does.Contain("id=\"43\""));
			Assert.That(text, Does.Not.Contain("duplicate parse"));
		}

		/// <summary>Keeps a blocking event expandable when it is the only synthesis
		/// child.</summary>
		[Test]
		public void Blocked_OnlyChildRemainsReachableWithUnknownEntry()
		{
			var node = new XElement("MorphologicalRuleSynthesisTrace",
				new XElement("MorphologicalRule", new XAttribute("type", "affix"), "synthetic past"),
				new XElement("Output", "a"),
				new XElement("BlockedTrace", new XElement("Rule", "synthetic past"),
					new XElement("BlockingEntry"), new XElement("Output", "vem")));

			var text = Text(Transform(node));

			Assert.That(text, Does.Contain("Unknown blocking entry"));
			Assert.That(text, Does.Contain("Output = vem"));
			Assert.That(text, Does.Not.Contain("duplicate parse"));
		}

		private static XElement Allomorph()
		{
			return new XElement("Allomorph", new XAttribute("id", 42),
				new XElement("LongName", "root"), new XElement("Form", "a"),
				new XElement("Morpheme", new XAttribute("id", 43),
					new XElement("HeadWord", "root")));
		}

		private static XElement CompoundFailure(string type)
		{
			return new XElement("CompoundingRuleAnalysisTrace",
				new XElement("MorphologicalRule", new XAttribute("type", "compound"), "compound"),
				new XElement("Output", "*None*"),
				new XElement("FailureReason", new XAttribute("type", type)));
		}

		private string Transform(XElement node, bool rtl = false)
		{
			var doc = new XDocument(new XElement("Wordform", new XAttribute("form", "a"),
				new XElement("Trace", new XElement("WordAnalysisTrace", node))));
			var args = new XsltArgumentList();
			args.AddParam("prmVernacularRTL", "", rtl ? "Y" : "N");
			using (var writer = new StringWriter())
			using (var reader = doc.CreateReader())
			{
				m_transform.Transform(reader, args, writer);
				return writer.ToString();
			}
		}

		private static string Text(string html)
		{
			html = Regex.Replace(html, "<(script|style)\\b[^>]*>.*?</\\1>", "",
				RegexOptions.Singleline | RegexOptions.IgnoreCase);
			return Regex.Replace(System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")),
				"\\s+", " ");
		}
	}
}
