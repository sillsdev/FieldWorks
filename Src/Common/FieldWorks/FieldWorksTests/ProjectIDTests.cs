// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectIdTests.cs
// Responsibility: FW team

using System;
using System.IO;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ProjectId class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ProjectIDTests : BaseTest
	{
		#region Member variables
		private ILocalClientServerServices m_localCsSvcs;
		private FDOBackendProviderType m_defaultBepType;
		private MockFileOS m_mockFileOs;
		#endregion

		#region Setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void  FixtureSetup()
		{
			base.FixtureSetup();
			IClientServerServices clientSvcs = MockRepository.GenerateStub<IClientServerServices>();
			ReflectionHelper.SetProperty(typeof(ClientServerServices), "Current", clientSvcs);
			m_localCsSvcs = MockRepository.GenerateStub<ILocalClientServerServices>();
			clientSvcs.Stub(x => x.Local).Return(m_localCsSvcs);
			m_localCsSvcs.Stub(cs => cs.DefaultBackendType).Do(new Func<FDOBackendProviderType>( () => m_defaultBepType));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up default member values for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_defaultBepType = FDOBackendProviderType.kXML;
			m_mockFileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_mockFileOs);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			FileUtils.Manager.Reset();
		}
		#endregion

		#region Equality tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Equality()
		{
			var projA = new ProjectId("xml", "monkey", null);
			var projB = new ProjectId("xml", "monkey", null);
			Assert.AreEqual(FDOBackendProviderType.kXML, projA.Type);
			Assert.IsTrue(projA.Equals(projB));
			Assert.AreEqual(projA.GetHashCode(), projB.GetHashCode());
		}

		#endregion

		#region IsValid tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a bogus type string will result in an invalid project.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValid_BogusType()
		{
			ProjectId proj = new ProjectId("bogus", "rogus", null);
			Assert.AreEqual(FDOBackendProviderType.kInvalid, proj.Type);
			Assert.IsFalse(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a null type will attempt to get the type from the filename
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValid_NullType()
		{
			const string sProjectName = "monkey";
			string sFile = FdoFileHelper.GetXmlDataFileName(sProjectName);
			m_mockFileOs.AddExistingFile(GetXmlProjectFilename(sProjectName));
			ProjectId proj = new ProjectId(null, sFile, null);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsValid property on an XML project which doesn't exist
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValid_XML_False()
		{
			ProjectId proj = new ProjectId("xml", "notThere", null);
			Assert.IsFalse(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsValid property on an XML project which does exist
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValid_XML_True()
		{
			const string sProjectName = "monkey";
			string sFile = FdoFileHelper.GetXmlDataFileName(sProjectName);
			m_mockFileOs.AddExistingFile(GetXmlProjectFilename(sProjectName));
			ProjectId proj = new ProjectId("xml", sFile, null);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsValid property on an XML project when the project name
		/// is not specified
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValid_XML_NullProjectName()
		{
			ProjectId proj = new ProjectId("xml", null, null);
			Assert.IsFalse(proj.IsValid);
		}

		#endregion

		#region CleanUpNameForType tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will handle an empty name correctly.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_EmptyName()
		{
			var proj = new ProjectId(string.Empty, null);
			Assert.IsNull(proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsFalse(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the default extension and default directory to a base
		/// project name
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_Default_onlyName()
		{
			m_defaultBepType = FDOBackendProviderType.kDb4oClientServer;
			string expectedPath = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "ape"),
				FdoFileHelper.GetDb4oDataFileName("ape"));
			m_localCsSvcs.Stub(cs => cs.IdForLocalProject("ape")).Return(expectedPath);
			m_mockFileOs.AddExistingFile(expectedPath);

			ProjectId proj = new ProjectId("ape", null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kDb4oClientServer, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the default directory but not an extension to a base
		/// project name if the project name contains a period (i.e., has an extension) and the
		/// type is not specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_Default_NameWithPeriod_Exists()
		{
			string expectedPath = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "my.monkey"), "my.monkey");
			m_localCsSvcs.Stub(cs => cs.IdForLocalProject("my.monkey")).Return(expectedPath);
			m_mockFileOs.AddExistingFile(expectedPath);

			ProjectId proj = new ProjectId("my.monkey", null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the extension and the default directory to a base
		/// project name if there are two files with the same name as the project: one with and
		/// one without the default XML extension. The type is not specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_XML_NameWithPeriod_FilesWithAndWithoutExtensionExist()
		{
			string myMonkeyProjectFolder = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "my.monkey");
			string expectedPath = Path.Combine(myMonkeyProjectFolder, FdoFileHelper.GetXmlDataFileName("my.monkey"));
			m_mockFileOs.AddExistingFile(expectedPath);
			m_mockFileOs.AddExistingFile(Path.Combine(myMonkeyProjectFolder, "my.monkey"));

			var proj = new ProjectId("my.monkey", null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the extension and the default directory to a base
		/// project name if there is only one file with the same name as the project: one with
		/// an XML extension. Also, the type is specified as XML.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_XML_NameWithPeriod_WithXmlExtension()
		{
			const string projectName = "my.monkey";
			string expectedPath = GetXmlProjectFilename(projectName);
			m_mockFileOs.AddExistingFile(expectedPath);

			var proj = new ProjectId(FdoFileHelper.GetXmlDataFileName(projectName), null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the extension and the default directory to a base
		/// project name, even if the project name contains a period, if the project name as
		/// passed in could not be found.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_XML_NameWithPeriod_NotExist()
		{
			string expectedPath = GetXmlProjectFilename("my.monkey");
			m_mockFileOs.AddExistingFile(expectedPath);

			var proj = new ProjectId("my.monkey", null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will add the default directory to a project name with extension
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_XML_onlyNameWithExtension()
		{
			string expectedPath = GetXmlProjectFilename("monkey");
			m_mockFileOs.AddExistingFile(expectedPath);

			var proj = new ProjectId(FdoFileHelper.GetXmlDataFileName("monkey"), null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will not try to change the project file specification if a full
		/// path with extension is specified
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CleanUpNameForType_XML_FullPath()
		{
			string expectedPath = Path.Combine(FwDirectoryFinder.ProjectsDirectory, FdoFileHelper.GetXmlDataFileName("monkey"));
			m_mockFileOs.AddExistingFile(expectedPath);

			var proj = new ProjectId(expectedPath, null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ProjectId will handle a relative path by assumming it's in the FW data
		/// directory
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not sure what this would be useful for or if this would be the desired behavior.")]
		public void CleanUpNameForType_XML_RelativePath()
		{
			string relativePath = Path.Combine("primate", FdoFileHelper.GetXmlDataFileName("monkey"));
			string expectedPath = Path.Combine(FwDirectoryFinder.ProjectsDirectory, relativePath);
			m_mockFileOs.AddExistingFile(expectedPath);

			ProjectId proj = new ProjectId(relativePath, null);
			Assert.AreEqual(expectedPath, proj.Path);
			Assert.AreEqual(FDOBackendProviderType.kXML, proj.Type);
			Assert.IsTrue(proj.IsValid);
		}
		#endregion

		#region AssertValid tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AssertValid method on a valid ProjectId is valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssertValid_Valid()
		{
			string projFile = GetXmlProjectFilename("monkey");
			m_mockFileOs.AddExistingFile(projFile);

			var proj = new ProjectId(FdoFileHelper.GetXmlDataFileName("monkey"), null);
			proj.AssertValid(); // no exception should be thrown here for a valid project.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AssertValid method on ProjectId missing the Name is invalid, but does not
		/// need to be reported to the user.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssertValid_Invalid_NoName()
		{
			var proj = new ProjectId(string.Empty, null);
			try
			{
				proj.AssertValid();
				Assert.Fail("FwStartupException expected");
			}
			catch (StartupException exception)
			{
				Assert.IsFalse(exception.ReportToUser);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AssertValid method on ProjectId where the project file is not found
		/// (needs to be reported to the user).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssertValid_Invalid_FileNotFound()
		{
			var proj = new ProjectId(FdoFileHelper.GetXmlDataFileName("notfound"), null);
			try
			{
				proj.AssertValid();
				Assert.Fail("FwStartupException expected");
			}
			catch (StartupException exception)
			{
				Assert.IsTrue(exception.ReportToUser);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AssertValid method on ProjectId with a type that is invalid (needs to be
		/// reported to the user).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssertValid_InvalidProjectType()
		{
			var proj = new ProjectId(FDOBackendProviderType.kInvalid, FdoFileHelper.GetXmlDataFileName("invalid"), null);
			try
			{
				proj.AssertValid();
				Assert.Fail("FwStartupException expected");
			}
			catch (StartupException exception)
			{
				Assert.IsTrue(exception.ReportToUser);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AssertValid method on ProjectId where the shared project directory
		/// is not found (needs to be reported to the user).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssertValid_Invalid_SharedFolderNotFound()
		{
			var proj = new ProjectId(FdoFileHelper.GetDb4oDataFileName("monkey"), FwLinkArgs.kLocalHost);
			try
			{
				proj.AssertValid();
				Assert.Fail("FwStartupException expected");
			}
			catch (StartupException exception)
			{
				Assert.IsTrue(exception.ReportToUser);
			}
		}
		#endregion

		#region SimplePropertyTests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Properties contain expected values after creating a ProjectId with
		/// different constructors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckProperties()
		{
			string expectedProjectDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "SomeTest");
			m_mockFileOs.ExistingDirectories.Add(expectedProjectDir);

			const string type = "db4ocs";
			const string host = "127.0.0.1";
			string filename = FdoFileHelper.GetDb4oDataFileName("SomeTest");

			var proj = new ProjectId(type, filename, host);
			proj.AssertValid();
			Assert.AreEqual(Path.Combine(expectedProjectDir, filename), proj.Path);

			proj = new ProjectId(filename, host);
			proj.AssertValid();
			Assert.AreEqual(Path.Combine(expectedProjectDir, filename), proj.Path);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Name and Path properties when the project name contains periods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NameAndPath()
		{
			string myProjectFolder = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "My.Project");
			ProjectId projId = new ProjectId(FDOBackendProviderType.kXML, "My.Project", null);
			Assert.AreEqual(Path.Combine(myProjectFolder, FdoFileHelper.GetXmlDataFileName("My.Project")), projId.Path);
			Assert.AreEqual("My.Project", projId.Name);

			projId = new ProjectId(FDOBackendProviderType.kDb4oClientServer, "My.Project", null);
			Assert.AreEqual(Path.Combine(myProjectFolder, FdoFileHelper.GetDb4oDataFileName("My.Project")), projId.Path);
			Assert.AreEqual("My.Project", projId.Name);
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the XML project filename (in an appropriately named subfolder of the Projects
		/// folder).
		/// </summary>
		/// <param name="projectName">Name of the project.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetXmlProjectFilename(string projectName)
		{
			return Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, projectName),
				FdoFileHelper.GetXmlDataFileName(projectName));
		}
		#endregion
	}
}
