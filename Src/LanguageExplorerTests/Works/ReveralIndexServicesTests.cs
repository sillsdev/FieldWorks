// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

#if RANDYTODO
using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.TestUtilities;
using SIL.LCModel.Utils;
using RIS = LanguageExplorer.Works.ReversalIndexServices;

namespace LanguageExplorerTests.Works
{
	/// <summary/>
	[TestFixture]
	public class ReversalIndexServicesTests : MemoryOnlyBackendProviderTestBase
	{
		private WritingSystemManager WSMgr => Cache.ServiceLocator.WritingSystemManager;

		/// <summary>
		/// Verifies that CreateOrRemoveReversalIndexConfigurationFiles
		///  - Deletes RevIdx Configs associated w/ nonexistent WS's
		///  - Does *not* delete RefIdx Configs associated w/ existent WS's, regardless of filename
		///  - Does not delete AllReversalIndexes
		///  - Creates RevIdx Configs for each Analysis WS
		/// </summary>
		[Test]
		public void CreateOrRemoveReversalIndexConfigurationFiles_DeletethNotValidConfigs()
		{
			const string nonExtantWs = "es";
			var analWss = new[] { "en", "fr", "de" };
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
				() => Cache.LangProject.AnalysisWss = string.Join(" ", analWss));
			using (var tfProject = new TemporaryFolder("ProjForDontDeleteRealReversals"))
			{
				var projectsDir = Path.GetDirectoryName(tfProject.Path);
				var projectName = Path.GetFileName(tfProject.Path);
				var riConfigDir = Path.Combine(tfProject.Path, RIS.ConfigDir, RIS.RevIndexDir);
				FileUtils.EnsureDirectoryExists(riConfigDir);

				var crazyFilename = Path.Combine(riConfigDir, "FilenameHasNoWS" + RIS.ConfigFileExtension);
				CreateDummyConfigForWS(crazyFilename, analWss[0]);
				var nonExtantWsFilename = GetFilenameForWs(riConfigDir, nonExtantWs);
				CreateDummyConfigForWS(nonExtantWsFilename, nonExtantWs);
				var wrongWsFilename = GetFilenameForWs(riConfigDir, analWss[1]);
				CreateDummyConfigForWS(wrongWsFilename, analWss[2]);
				var allReversalsFilename = Path.Combine(riConfigDir, RIS.AllIndexesFileName + RIS.ConfigFileExtension);
				CreateDummyConfigForWS(allReversalsFilename, "");
				// A file with the expected name that the user had modified
				var normalFilename = GetFilenameForWs(riConfigDir, analWss[2]);
				const string normalFileModified = "2015-03-14"; // arbitrary date in the past
				CreateDummyConfigForWS(normalFilename, analWss[2]);
				XAttribute normalFileModifiedAtt;
				var normalFile = GetLastModifiedAttributeFromFile(normalFilename, out normalFileModifiedAtt);
				normalFileModifiedAtt.Value = normalFileModified;
				normalFile.Save(normalFilename);


				RIS.CreateOrRemoveReversalIndexConfigurationFiles(WSMgr, Cache, FwDirectoryFinder.DefaultConfigurations,
					projectsDir, projectName);

				Assert.That(File.Exists(crazyFilename), crazyFilename + " should not have been deleted");
				Assert.AreEqual(analWss[0], GetWsFromFile(crazyFilename), "WS in custom-named file should not have been changed");
				Assert.That(!File.Exists(nonExtantWsFilename));
				Assert.That(File.Exists(wrongWsFilename));
				Assert.AreEqual(analWss[1], GetWsFromFile(wrongWsFilename),
					"WS in wrong ws-named file should have been changed (we think)");
				Assert.That(File.Exists(allReversalsFilename));
				Assert.AreEqual(string.Empty, GetWsFromFile(allReversalsFilename), "All reversals should not have a writing system");
				foreach (var ws in analWss)
				{
					var filename = GetFilenameForWs(riConfigDir, ws);
					Assert.That(File.Exists(filename), "No file for WS: " + ws);
					Assert.AreEqual(ws, GetWsFromFile(filename), "Incorrect WS attribute in file");
				}
				XAttribute modifiedAtt;
				GetLastModifiedAttributeFromFile(normalFilename, out modifiedAtt);
				Assert.AreEqual(normalFileModified, modifiedAtt.Value, "File with proper name and WS should not have been modified");
			}
		}

		private string GetFilenameForWs(string riConfigDir, string ws)
		{
			return Path.Combine(riConfigDir, WSMgr.Get(ws).LanguageTag + RIS.ConfigFileExtension);
		}

		private void CreateDummyConfigForWS(string filename, string ws)
		{
			var xmldoc = new XDocument();
			var xElement = new XElement(RIS.DictConfigElement);
			xElement.Add(new XAttribute("name", string.IsNullOrEmpty(ws) ? "All Reversal Indexes" : WSMgr.Get(ws).LanguageTag));
			xElement.Add(new XAttribute(RIS.WsAttribute, ws ?? String.Empty));
			xmldoc.Add(xElement);
			xmldoc.Save(filename);
		}

		private static string GetWsFromFile(string filename)
		{
			return XDocument.Load(filename).XPathSelectElement(RIS.DictConfigElement).Attribute(RIS.WsAttribute)?.Value;
		}

		private static XDocument GetLastModifiedAttributeFromFile(string filename, out XAttribute modifiedAtt)
		{
			var doc = XDocument.Load(filename);
			var config = doc.Element(RIS.DictConfigElement);
			if (config == null)
			{
				config = new XElement(RIS.DictConfigElement);
				doc.Add(config);
			}
			modifiedAtt = config.Attribute("lastModified");
			if (modifiedAtt == null)
			{
				modifiedAtt = new XAttribute("lastModified", string.Empty);
				config.Add(modifiedAtt);
			}
			return doc;
		}
	}
}
#endif