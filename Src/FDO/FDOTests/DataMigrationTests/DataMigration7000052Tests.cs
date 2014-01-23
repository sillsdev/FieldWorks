// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests7000052.cs
// Responsibility: FW team

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000051 to 7000052.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000052 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000051 to 7000052.
		/// Copy MorphRA forms into WfiMorphBundle forms
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000052Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// Add WfiMorphBundle that already has a form.
			const string sGuid_wmbWithForm = "00b35f9f-86ce-4f07-bde7-b65c28503641";

			sb.AppendFormat("<rt class=\"WfiMorphBundle\" guid=\"{0}\">", sGuid_wmbWithForm);
			sb.Append("<Form>");
			sb.Append("<AStr ws=\"wsForm0\">");
			sb.AppendFormat("<Run ws=\"wsForm0\">{0}</Run>", "form1");
			sb.Append("</AStr>");
			sb.Append("</Form>");
			sb.Append("</rt>");
			var dtoWithForm = new DomainObjectDTO(sGuid_wmbWithForm, "WfiMorphBundle", sb.ToString());
			dtos.Add(dtoWithForm);
			sb.Length = 0;

			const string sGuid_moAffixAllomorph = "f2f9a52f-b07e-4a09-9259-5f6333445eb9";
			sb.AppendFormat("<rt class=\"MoAffixAllomorph\" guid=\"{0}\">", sGuid_moAffixAllomorph);
			sb.Append("<Form>");
			sb.AppendFormat("<AUni ws=\"wsMorph0\">{0}</AUni>", "morphForm&amp;0");
			sb.AppendFormat("<AUni ws=\"wsMorph1\">{0}</AUni>", "morphForm1");
			sb.Append("</Form>");
			sb.Append("</rt>");
			var dtoMoAffix = new DomainObjectDTO(sGuid_moAffixAllomorph, "MoAffixAllomorph", sb.ToString());
			dtos.Add(dtoMoAffix);
			sb.Length = 0;

			const string sGuid_wmbNoForm = "0110541a-c93e-4f01-8eec-9d24e1b08d3a";
			sb.AppendFormat("<rt class=\"WfiMorphBundle\" guid=\"{0}\">", sGuid_wmbNoForm);
			sb.Append("<Form>");
			sb.Append("<AStr ws=\"wsMorphPrexisting\">");
			sb.AppendFormat("<Run ws=\"wsMorphPrexisting\">{0}</Run>", "morphFormPrexisting&amp;0");
			sb.Append("</AStr>");
			sb.Append("</Form>");
			sb.Append("<Morph>");
			sb.AppendFormat("<objsur guid=\"{0}\" t=\"r\" />", sGuid_moAffixAllomorph);
			sb.Append("</Morph>");
			sb.Append("</rt>");
			var dtoNoForm = new DomainObjectDTO(sGuid_wmbNoForm, "WfiMorphBundle", sb.ToString());
			dtos.Add(dtoNoForm);
			sb.Length = 0;

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "WfiMorphBundle", "MoAffixAllomorph" }); // Not true, but no matter.
			mockMDC.AddClass(2, "WfiMorphBundle", "CmObject", new List<string>());
			mockMDC.AddClass(3, "MoAffixAllomorph", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000051, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000052, new DummyProgressDlg());
			Assert.AreEqual(7000052, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// check that MorphBundle with form still has its form.
			{
				DomainObjectDTO dtoWithFormTest;
				dtoRepos.TryGetValue(sGuid_wmbWithForm, out dtoWithFormTest);
				var eltWmbWithFormTest = XElement.Parse(dtoWithFormTest.Xml);
				var eltFormTest = eltWmbWithFormTest.Element("Form");
				// get form
				Assert.IsNotNull(eltFormTest);
				var eltRunTest = eltFormTest.Element("AStr").Element("Run");
				Assert.AreEqual("form1", eltRunTest.Value);

				// now check that ws of the new Form matches the Morph Form ws.
				var eltWsTest = eltFormTest.Element("AStr").Attribute("ws");
				Assert.AreEqual("wsForm0", eltWsTest.Value);
				eltWsTest = eltFormTest.Element("AStr").Element("Run").Attribute("ws");
				Assert.AreEqual("wsForm0", eltWsTest.Value);
			}


			// check that MorphBundle without form now has a new alternative forms,
			// identical to the Morph alternative Form.
			{
				DomainObjectDTO dtoNewFormTest;
				dtoRepos.TryGetValue(sGuid_wmbNoForm, out dtoNewFormTest);
				var eltWmbNewFormTest = XElement.Parse(dtoNewFormTest.Xml);
				var eltFormTest = eltWmbNewFormTest.Element("Form");
				// get form
				Assert.IsNotNull(eltFormTest);
				var eltRunTest = eltFormTest.Element("AStr").Element("Run");
				Assert.AreEqual("morphForm&0", eltRunTest.Value);

				// now check that ws of the new Form matches the Morph Form ws.
				var eltWsTest = eltFormTest.Element("AStr").Attribute("ws");
				Assert.AreEqual("wsMorph0", eltWsTest.Value);
				eltWsTest = eltFormTest.Element("AStr").Element("Run").Attribute("ws");
				Assert.AreEqual("wsMorph0", eltWsTest.Value);

				// prexisting form should have been deleted. should only have two ws strings now.
				Assert.AreEqual(2, eltFormTest.Elements("AStr").Count());
				var aStrTest = eltFormTest.Elements("AStr").ToList()[1];
				eltRunTest = aStrTest.Element("Run");
				Assert.AreEqual("morphForm1", eltRunTest.Value);

				eltWsTest = aStrTest.Attribute("ws");
				Assert.AreEqual("wsMorph1", eltWsTest.Value);
				eltWsTest = aStrTest.Element("Run").Attribute("ws");
				Assert.AreEqual("wsMorph1", eltWsTest.Value);
			}
		}
	}
}