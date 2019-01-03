// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml;
using System.Xml.Xsl;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	[TestFixture]
	public class InterlinearExporterTestsBase : InterlinearTestBase
	{
		protected IText m_text1;
		protected InterlinLineChoices m_choices;
		protected const string QaaXKal = "qaa-x-kal";

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_text1 = CreateText();
		}

		/// <summary>
		/// Override this method, if some other text is to be used, and then do not call this base method.
		/// </summary>
		/// <returns></returns>
		protected virtual IText CreateText()
		{
			return LoadTestText(Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Areas", "TextsAndWords", "Interlinear", "InterlinearExporterTests.xml"), 1, new XmlDocument());
		}

		public override void TestTearDown()
		{
			m_text1?.Delete();
			m_text1 = null;
			m_choices = null;
			base.TestTearDown();
		}

		protected XmlDocument ExportToXml(string mode)
		{
			var exportedXml = new XmlDocument();
			using (var vc = new InterlinVc(Cache))
			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, System.Text.Encoding.UTF8))
			{
				vc.LineChoices = m_choices;
				var exporter = InterlinearExporter.Create(mode, Cache, writer, m_text1.ContentsOA, m_choices, vc);
				exporter.WriteBeginDocument();
				exporter.ExportDisplay();
				exporter.WriteEndDocument();
				writer.Flush();
				stream.Seek(0, SeekOrigin.Begin);
				exportedXml.Load(stream);
			}
			return exportedXml;
		}


		public delegate T StreamFactory<T>();
		private delegate void ExtractStream<T>(T stream);

		private static void TransformDoc<T>(XmlDocument usxDocument, StreamFactory<T> createStream, ExtractStream<T> extractStream, string fileXsl)
			where T : Stream
		{
			const bool omitXmlDeclaration = true;
			var xslt = File.ReadAllText(fileXsl);
			var xform = new XslCompiledTransform(false);
			using (var stringReader = new StringReader(xslt))
			using (var xmlReader = XmlReader.Create(stringReader))
			{
				xform.Load(xmlReader);
			}
			using (var stream = createStream())
			{
				var writerSettings = new XmlWriterSettings
				{
					OmitXmlDeclaration = omitXmlDeclaration
				};
				if (writerSettings.OmitXmlDeclaration)
				{
					writerSettings.ConformanceLevel = ConformanceLevel.Auto;
				}
				using (var xmlWriter = XmlWriter.Create(stream, writerSettings))
				{
					xform.Transform(usxDocument.CreateNavigator(), new XsltArgumentList(), xmlWriter);
					xmlWriter.Flush();
					extractStream(stream);
				}
			}
		}

		protected static XmlDocument TransformDoc(XmlDocument usxDocument, string fileXsl)
		{
			StreamFactory<MemoryStream> createStream = () => new MemoryStream();
			var transformedDoc = new XmlDocument();
			ExtractStream<MemoryStream> extractStream = delegate (MemoryStream stream)
			{
				stream.Seek(0, SeekOrigin.Begin);
				transformedDoc.Load(stream);
			};
			TransformDoc(usxDocument, createStream, extractStream, fileXsl);
			return transformedDoc;
		}

		protected XmlDocument ExportToXml()
		{
			return ExportToXml("xml");
		}
	}
}