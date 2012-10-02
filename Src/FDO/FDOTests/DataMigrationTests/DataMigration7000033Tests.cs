using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;
using System.IO;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000030 to 7000031.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000033 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000032 to 7000033.
		/// Clean up uses of obsolete names "customN" in configuration files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000032Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000033.xml");

			IFwMetaDataCacheManaged mockMdc = DataMigrationTests7000020.SetupMdc();

			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000032, dtos, mockMdc,
				Path.GetTempPath());

			var projectFolder = repoDto.ProjectFolder;
			var projectName = Path.GetFileNameWithoutExtension(projectFolder);
			// This is equivalent to DirectoryFinder.GetConfigSettingsDir(projectFolder) at the time of creating
			// the migration, but could conceivably change later.
			var targetDir = Path.Combine(projectFolder, "ConfigurationSettings");
			Directory.CreateDirectory(targetDir);
			var testFilePath = Path.Combine(targetDir, "LexEntry_Layouts.xml");
			using (var writer = new StreamWriter(testFilePath))
			{
				writer.WriteLine(@"<LayoutInventory>" +
				"<layout class='LexEntry' type='jtview' name='publishStemPara' css='$fwstyle=Dictionary-Normal' version='9'>" +
				" <part ref='Headword' label='Headword' before='' sep=' ' after='  ' ws='vernacular' wsType='vernacular' style='Dictionary-Headword' css='headword' visibility='ifdata' comment='Headword is a smart field. It is the lexeme form unless there is a citation form. Includes Homograph number and affix marking.' />" +
				" <part ref='$child' label='testYYY' before='' after=' ' visibility='ifdata' ws='$ws=analysis' wsType='analysis vernacular' sep=' ' showLabels='false'>" +
				"   <configureMlString field='custom' class='LexEntry' />" +
				" </part>" +
				"<part ref='$child' label='TestXXX' before='' after=' ' visibility='ifdata'>" +
				" <string field='custom1' class='LexEntry' />" +
				"</part>" +
				" <part ref='$child' label='messed up' before='' after=' ' visibility='ifdata' ws='$ws=analysis' wsType='analysis vernacular' sep=' ' showLabels='false' originalLabel='testBB'>" +
				"   <configureMlString field='custom' class='LexEntry' />" +
				" </part>" +
				"<part ref='$child' label='modified' before='' after=' ' visibility='ifdata' originalLabel='TestZZZ'>" +
				" <string field='custom1' class='LexEntry' />" +
				"</part>" +
				" <part ref='$child' before='' after=' ' visibility='ifdata' ws='$ws=analysis' wsType='analysis vernacular' sep=' ' showLabels='false' originalLabel='testCC'>" +
				"   <configureMlString field='custom' class='LexEntry' />" +
				" </part>" +
				"<part ref='$child' before='' after=' ' visibility='ifdata' originalLabel='TestD'>" +
				" <string field='custom1' class='LexEntry' />" +
				"</part>" +
				"</layout>" +
				"</LayoutInventory>");
				writer.Close();
			}

			// Do the migration.
			m_dataMigrationManager.PerformMigration(repoDto, 7000033);

			Assert.AreEqual(7000033, repoDto.CurrentModelVersion, "Wrong updated version.");
			var root = XElement.Load(testFilePath); // layout inventory node
			var layout = root.Elements().ToList()[0];
			var children = layout.Elements().ToList();
			VerifyChild(children[1], "testYYY");
			VerifyChild(children[2], "TestXXX");
			VerifyChild(children[3], "testBB");
			VerifyChild(children[4], "TestZZZ");
			VerifyChild(children[5], "testCC");
			VerifyChild(children[6], "TestD");
		}
		void VerifyChild(XElement partRef, string fieldName)
		{
			var propElt = partRef.Elements().ToList()[0];
			var field = propElt.Attribute("field").Value;
			Assert.That(field, Is.EqualTo(fieldName));
		}
	}
}