// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Covers which export templates the FLEX_AI_EXPORT opt-in applies to.
	/// </summary>
	[TestFixture]
	public class ExportDialogAiGateTests
	{
		[Test]
		public void IsAiExportTemplate_TrueForTheAiAnalysisTemplate()
		{
			Assert.That(ExportDialog.IsAiExportTemplate(
				Load("<template type=\"grammarTextsAI\"><FxtDocumentDescription /></template>")), Is.True);
		}

		[TestCase("<template format=\"htm\" type=\"phonology\" datatype=\"Phonology\" />")]
		[TestCase("<template type=\"LIFT\" />")]
		[TestCase("<template />")]
		[TestCase("<somethingElse />")]
		public void IsAiExportTemplate_FalseForEveryOtherTemplate(string xml)
		{
			Assert.That(ExportDialog.IsAiExportTemplate(Load(xml)), Is.False);
		}

		private static XmlDocument Load(string xml)
		{
			var document = new XmlDocument();
			document.LoadXml(xml);
			return document;
		}
	}
}
