// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test framework for migration from version 7000036 to 7000037.  This migration fixes a
	/// data conversion problem for externalLink attributes in Run elements coming from
	/// FieldWorks 6.0 into FieldWorks 7.0+.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DataMigration7000037Tests : DataMigrationTestsBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		///  Test the migration from version 7000036 to 7000037.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000037Test()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000037.xml");

			// Create all the Mock classes for the classes in my test data.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LexSense", "StPara" });
			mockMDC.AddClass(7, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(15, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMDC.AddClass(16, "StTxtPara", "StPara", new List<string>());

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000036, dtos, mockMDC,
				FileUtils.ChangePathToPlatform("C:\\WW\\DistFiles\\Projects\\TokPisin"), FwDirectoryFinder.FdoDirectories);
			// Check that the version is correct.
			Assert.AreEqual(7000036, dtoRepos.CurrentModelVersion, "Wrong original version.");
			// Collect the link values that shouldn't change.
			m_cLinks = 0;
			CollectionNonSilfwLinks(dtoRepos);
			int cLinksOrig = m_cLinks;
			Assert.AreEqual(12, m_cLinks, "Should have 12 externalLink attributes in the test data");

			// Do Migration
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000037, new DummyProgressDlg());

			// Check that the version was updated.
			Assert.AreEqual(7000037, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Check that all externalLink values are reasonable, and that we find the right
			// number of them.
			m_cLinks = 0;
			foreach (var dto in dtoRepos.AllInstancesWithValidClasses())
			{
				string xml = dto.Xml;
				Assert.IsTrue(xml.Contains("externalLink"), "Every object in the test has an externalLink");
				var dtoXML = XElement.Parse(xml);
				foreach (var run in dtoXML.XPathSelectElements("//Run"))
				{
					var externalLinkAttr = run.Attribute("externalLink");
					if (externalLinkAttr != null)
					{
						CheckForValidLinkValue(externalLinkAttr.Value);
						++m_cLinks;
					}
				}
			}
			Assert.AreEqual(cLinksOrig, m_cLinks, "Migration should not change the number of externalLink attributes");
		}

		int m_cLinks;
		List<string> m_rgsOtherLinks = new List<string>();

		private void CollectionNonSilfwLinks(IDomainObjectDTORepository dtoRepos)
		{
			foreach (var dto in dtoRepos.AllInstancesWithValidClasses())
			{
				string xml = dto.Xml;
				Assert.IsTrue(xml.Contains("externalLink"), "Every object in the test has an externalLink");
				var dtoXML = XElement.Parse(xml);
				foreach (var run in dtoXML.XPathSelectElements("//Run"))
				{
					var externalLinkAttr = run.Attribute("externalLink");
					if (externalLinkAttr != null)
					{
						string value = externalLinkAttr.Value;
						if (value.StartsWith("http://") || value.StartsWith("libronixdls:"))
							m_rgsOtherLinks.Add(value);
						++m_cLinks;
					}
				}
			}

		}

		private void CheckForValidLinkValue(string externalLink)
		{
			int idxColon = externalLink.IndexOf(':');
			Assert.Greater(idxColon, 0, "links must have a colon to separate off the type of link");
			string linkType = externalLink.Substring(0, idxColon);
			switch (linkType)
			{
				case "http":
				case "libronixdls":
					Assert.IsTrue(m_rgsOtherLinks.Contains(externalLink),
						"Migration should not affect http or libronix links");
					return;
				default:
					Assert.AreEqual("silfw", linkType);
					break;
			}
			Assert.IsTrue(externalLink.StartsWith(FwLinkArgs.kFwUrlPrefix),
				"silfw link should start with " + FwLinkArgs.kFwUrlPrefix);
			string query = HttpUtility.UrlDecode(externalLink.Substring(FwLinkArgs.kFwUrlPrefix.Length));
			string[] rgsProps = query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
			string tool = null;
			Guid guid = Guid.Empty;
			for (int i = 0; i < rgsProps.Length; ++i)
			{
				string[] propPair = rgsProps[i].Split('=');
				switch (propPair[0])
				{
					case "app":
						Assert.AreEqual("flex", propPair[1], "silfw link - app should equal \"flex\"");
						break;
					case "server":
						Assert.IsNullOrEmpty(propPair[1], "silfw link - server should be empty");
						break;
					case "database":
						Assert.AreEqual("this$", propPair[1], "silfw link - database should equal \"this$\"");
						break;
					case "tool":
						tool = propPair[1];
						break;
					case "guid":
						guid = new Guid(propPair[1]);
						break;
					default:
						break;
				}
			}
			Assert.IsNotNullOrEmpty(tool, "silfw link - tool should have a value");
			Assert.AreNotEqual(Guid.Empty, guid, "silfw link - guid should have a value");
		}
	}
}
