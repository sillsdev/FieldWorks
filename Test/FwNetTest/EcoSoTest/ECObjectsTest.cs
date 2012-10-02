// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ECObjectsTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using System.Diagnostics;
using System.Resources;
using System.IO;
using Microsoft.Win32;
using ECOBJECTSLib;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.ScrImportComponents
{
	#region ECObjectsTest class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for ECProjectClass.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ECObjectsTest : BaseTest
	{
		#region data members
		private IECProject m_sfProjectProxy;
		private RegistryData m_regData;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ECObjectsTest"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ECObjectsTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unpack test files and create registry entries
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			Unpacker.UnPackECTestProjects();
			Unpacker.UnPackSfTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_regData != null)
					m_regData.RestoreRegistryData();

				Unpacker.RemoveParatextTestProjects();
				Unpacker.RemoveSfTestProjects();
				Unpacker.RemoveECTestProjects();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_regData = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called before each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			CheckDisposed();

			m_sfProjectProxy = new ECProjectClass();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up called after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();

			m_sfProjectProxy = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the SfFilenames property persists in the ECProjectClass
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PersistSfFilenames()
		{
			CheckDisposed();

			m_sfProjectProxy.AddFile(Unpacker.SfProjectTestFolder + "01GEN.sfm");
			m_sfProjectProxy.AddFile(Unpacker.SfProjectTestFolder + "02EXO.sfm");
			m_sfProjectProxy.AddFile(Unpacker.SfProjectTestFolder + "58PHM.sfm");
			Assert.AreEqual(3, m_sfProjectProxy.NumberOfFiles);
			int c = 0;
			foreach (string sFile in (string[])m_sfProjectProxy.Files)
			{
				if ((sFile == Unpacker.SfProjectTestFolder + "01GEN.sfm") ||
					(sFile == Unpacker.SfProjectTestFolder + "02EXO.sfm") ||
					(sFile == Unpacker.SfProjectTestFolder + "58PHM.sfm"))
				{
					c++;
				}
				else
				{
					Assert.Fail("Unexpected filename in list");
				}
			}
			Assert.AreEqual(3, c);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the StyleMappings property persists in the ECProjectClass
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PersistStyleMappings()
		{
			CheckDisposed();

			m_sfProjectProxy.AddFile(Unpacker.SfProjectTestFolder + "3markers.sfm");
			Assert.AreEqual(3, m_sfProjectProxy.NumberOfMappings,
				"Before modifying mappings");

			ECMapping mapping;

			m_sfProjectProxy.NthECMapping(0, out mapping);
			Assert.AreEqual(0, mapping.IsConfirmed, "Marker 0 failed");

			m_sfProjectProxy.NthECMapping(1, out mapping);
			Assert.AreEqual(0, mapping.IsConfirmed, "Marker 1 failed");

			m_sfProjectProxy.NthECMapping(2, out mapping);
			Assert.AreEqual(0, mapping.IsConfirmed, "Marker 2 failed");

			ECMappingClass mappingClass = new ECMappingClass();

			mappingClass.BeginMarker = "\\imt";
			mappingClass.Domain = MarkerDomain.MD_Back;
			mappingClass.StyleName = "Background Section Heading";

//	Not a valid TECkit file...
//			mappingClass.DataEncoding = "French";
			m_sfProjectProxy.SetECMapping(mappingClass);

			mappingClass = new ECMappingClass();
			mappingClass.BeginMarker = "\\ist";
			mappingClass.Domain = MarkerDomain.MD_Note;
			mappingClass.StyleName = "Intro Heading";
//			mappingClass.DataEncoding = "English";
			m_sfProjectProxy.SetECMapping(mappingClass);

			// This should return the data with the confirmed flags set to non zero.
			byte[] blob = (byte[])m_sfProjectProxy.AsSafeArray;
			ECProjectClass tmpProjProxy = new ECProjectClass();
			tmpProjProxy.InitializeFromSafeArray(blob);

			Assert.AreEqual(3, tmpProjProxy.NumberOfMappings, "After reading blob");

			string commentImt = "Marker \\imt";
			string commentIst = "Marker \\ist";
			int count = 0;
			try
			{
				for (int i = 0; i < tmpProjProxy.NumberOfMappings; i++)
				{
					tmpProjProxy.NthECMapping(i, out mapping);

					if (mapping.BeginMarker == "\\imt")
					{
						Assert.AreEqual(MarkerDomain.MD_Back, mapping.Domain, commentImt);
						Assert.AreEqual("Background Section Heading", mapping.StyleName, commentImt);
//						Assert.AreEqual("French", mapping.DataEncoding, commentImt);
						Assert.IsTrue(mapping.IsConfirmed != 0, commentImt + ": fConfirmed != 0");
					}
					else if (mapping.BeginMarker == "\\ist")
					{
						Assert.AreEqual(MarkerDomain.MD_Note, mapping.Domain, commentIst);
						Assert.AreEqual("Intro Heading", mapping.StyleName, commentIst);
//						Assert.AreEqual("English", mapping.DataEncoding, commentIst);
						Assert.IsTrue(mapping.IsConfirmed != 0, commentIst + ": fConfirmed != 0");
					}
					count++;
				}
			}
			catch(ArgumentException)
			{
			}

			tmpProjProxy = null;
			Assert.AreEqual(3, count, "After reading back mappings");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ECProjectClass property provides proper default when
		/// uninitialized
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultECProject()
		{
			CheckDisposed();

			Assert.AreEqual(0, m_sfProjectProxy.NumberOfMappings, "Number of Mappings not zero");
			Assert.AreEqual(0, m_sfProjectProxy.NumberOfFiles, "Number of Files not zero");
			Assert.IsTrue(m_sfProjectProxy.Files == null ||
				((string[])m_sfProjectProxy.Files).Length == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ECProjectClass property provides proper default when
		/// uninitialized
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultParatextProject()
		{
			CheckDisposed();

			IParatextProjectProxy proj = new ParatextProjectProxyClass();

			Assert.AreEqual(string.Empty, proj.VernTransProj, "VernTransProj not empty.");
			Assert.AreEqual(string.Empty, proj.NotesTransProj, "NotesTransProj not empty.");
			Assert.AreEqual(string.Empty, proj.BackTransProj, "BackTransProj not empty.");
			Assert.AreEqual(0, proj.NumberOfMappings, "Number of Mappings not zero");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="SCScriptureTextClass.GetBooksForFile"/> method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test][Ignore("Until the test files are moved from the PtProjectTestFolder to the SfProjectTestFolder.")]
		public void VerifyGetScrBooksFromSFFile()
		{
			CheckDisposed();

			IECProject ecProject = new ECProjectClass();

			// Test with file with only one book in it
			// Romans is really book 45...
			string testFolder = Unpacker.PtProjectTestFolder;
			System.Array scrBooks = (System.Array)ecProject.GetBooksForFile(testFolder + "KAM\\46ROM.KAM");

			Assert.AreEqual(1, scrBooks.Length);
			Assert.AreEqual(45, (short)scrBooks.GetValue(0));

			scrBooks = (System.Array)ecProject.GetBooksForFile(testFolder + "SOTest.sfm");
			Assert.AreEqual(2, scrBooks.Length);
			Assert.AreEqual(56, (short)scrBooks.GetValue(0));
			Assert.AreEqual(65, (short)scrBooks.GetValue(1));
		}



		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test][Ignore("Until opening SSF files isn't done via the SO.")]
		public void DefaultTEStyles()
		{
			CheckDisposed();

			Unpacker.UnPackSfTestProjects();
			Unpacker.UnPackTEStyleFile();

			m_sfProjectProxy.DefaultSTYFileName = Unpacker.SfProjectTestFolder + "ECSOStyles.sty";
			m_sfProjectProxy.AddFile(Unpacker.SfProjectTestFolder + "01GEN.sfm");

			ECMapping mapping;

			for (int i = 0; i < m_sfProjectProxy.NumberOfMappings; i++)
			{
				m_sfProjectProxy.NthECMapping(i, out mapping);
				switch (mapping.BeginMarker)
				{
					case @"\h":
						Assert.AreEqual("Header & Footer", mapping.StyleName);
						break;
					case @"\ip":
						Assert.AreEqual("Background Paragraph", mapping.StyleName);
						break;
					case @"\v":
						Assert.AreEqual("Verse Number", mapping.StyleName);
						break;
					case @"\c":
						Assert.AreEqual("Chapter Number", mapping.StyleName);
						break;
					case @"\q":
						Assert.AreEqual("Poetry 1", mapping.StyleName);
						break;
					case @"\s":
						Assert.AreEqual("Section Heading", mapping.StyleName);
						break;
					default:
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTheRegistryDataClass()
		{
			CheckDisposed();

			RegistryData newPath, newElement, newValue, keyWithNoValue, partialTree, oneMore;
			newPath = newElement = newValue = keyWithNoValue = partialTree = oneMore = null;

			try
			{
				newPath = new RegistryData(Registry.LocalMachine,
					@"SOFTWARE\TEST_Undefined_Path",
					"TEST_NewPath",
					"asdfasdfasdfasdf" );

				newElement = new RegistryData( Registry.LocalMachine,
					"SOFTWARE",
					"TEST_NewElement",
					"asdfasdfasdfasdf" );

				newValue= new RegistryData( Registry.LocalMachine,
					@"SOFTWARE\SIL\FieldWorks",
					"RootDir",
					"asdfasdfasdfasdfasdfasdfasdfasdfasdfasdf" );

				keyWithNoValue = new RegistryData( Registry.LocalMachine,
					"SOFTWARE",
					"EmptyString",
					"SomeData...." );

				partialTree = new RegistryData( Registry.LocalMachine,
					@"SOFTWARE\SIL\FieldWorks\test1\test2\test3\test4",
					"DataKey",
					"DataValue" );

				oneMore = new RegistryData( Registry.LocalMachine,
					@"SOFTWARE\SIL\FieldWorks\test1",
					"Test1KeyItem",
					"DataValue" );
			}
			finally
			{
				if (newPath != null)
					newPath.RestoreRegistryData();

				if (newElement != null)
					newElement.RestoreRegistryData();

				if (newValue != null)
					newValue.RestoreRegistryData();

				if (keyWithNoValue != null)
					keyWithNoValue.RestoreRegistryData();

				if (partialTree != null)
					partialTree.RestoreRegistryData();

				if (oneMore != null)
					oneMore.RestoreRegistryData();
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDllRegistryPath()
		{
			CheckDisposed();

			string regPath1 = RegistryData.GetRegisteredDLLPath( "ECObjects.ECProject" );
			string regPath2 = RegistryData.GetRegisteredDLLPath( "ECObjects.ECProject.1" );
			Assert.AreEqual(regPath1, regPath2, "The Registered Paths aren't equal.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestScriptureReferenceBounds()
		{
			CheckDisposed();

			/// Check 5 different files for the start and end references including a file that is BAD and throws
			///  and exception.
			///
			string fileName1 = Unpacker.ECTestProjectTestFolder + "DEU01_08.TEV";
			string fileName2 = Unpacker.ECTestProjectTestFolder + "SingleVerse.TEV";
			string fileName3 = Unpacker.ECTestProjectTestFolder + "DualVerse.TEV";
			string fileName4 = Unpacker.ECTestProjectTestFolder + "multibooks.TEV";
			string fileName5 = Unpacker.ECTestProjectTestFolder + "multibooksBAD.TEV";

			string bookMarker = "\\id";
			string chapterMarker = "\\c";
			string verseMarker = "\\v";

			string startRef, endRef;

			ECLibrary comObj = new ECLibraryClass();
			comObj.GetScriptureRefBounds( fileName1, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );

			IECLibrary libObjPtr;
			libObjPtr = m_sfProjectProxy.LibraryObject;

			libObjPtr.GetScriptureRefBounds( fileName1, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );
			Assert.AreEqual("DEU 0:0", startRef, "Invalid Start Ref Found:");
			Assert.AreEqual("DEU 8:20", endRef, "Invalid End Ref Found:");

			libObjPtr.GetScriptureRefBounds( fileName2, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );
			Assert.AreEqual("DEU 0:0", startRef, "Invalid Start Ref Found:");
			Assert.AreEqual("DEU 1:1", endRef, "Invalid End Ref Found:");

			libObjPtr.GetScriptureRefBounds( fileName3, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );
			Assert.AreEqual("DEU 0:11", startRef, "Invalid Start Ref Found:");
			Assert.AreEqual("DEU 1:234", endRef, "Invalid End Ref Found:");

			libObjPtr.GetScriptureRefBounds( fileName4, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );
			Assert.AreEqual("1JN 0:11", startRef, "Invalid Start Ref Found:");
			Assert.AreEqual("2JN 1:99", endRef, "Invalid End Ref Found:");

			try
			{
				libObjPtr.GetScriptureRefBounds( fileName5, bookMarker, chapterMarker, verseMarker, out startRef, out endRef );
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("It was bad"); // wasbad = true;
			}
			// this is nolonger bad due to a change in the parser.
			// Assert.IsTrue(wasbad, "The BAD file wasn't BAD...");
			Assert.AreEqual("1JN 0:11", startRef, "Invalid Start Ref Found:");
			Assert.AreEqual("3JN 1:99", endRef, "Invalid End Ref Found:");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Test_LoadECObjectForSO()
		{
			CheckDisposed();

			RegistryData rootDir = null;

			try
			{
				rootDir = new RegistryData(Registry.LocalMachine,
					@"SOFTWARE\SIL\FieldWorks",
					"rootDir",
					Unpacker.RootDir);
			}
			finally
			{
				if (rootDir != null)
					rootDir.RestoreRegistryData();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Test_NewBeginAndNewEnd_Markers()
		{
			CheckDisposed();

			/// 4 different tests to check the new mapping properties :NewEndMarker, and NewBeginMarker.
			/// The rule is to make sure that the "new" properties all begin with a '\' and that all
			/// '*' characters are replaced with '+' except for the last char of the marker.  These are
			/// required rules due to the Scripture Object implementaion.
			///

			IECProject sfProject;
			IECMapping mappingObj;
			String endMarker;

			sfProject = new ECProjectClass();
			mappingObj = new ECMappingClass();

			// TEST 1
			mappingObj.BeginMarker = "\\b";							// create a \b marker
			endMarker = mappingObj.NewBeginMarker;			// retrieve the new Marker (or the current begin)
			Assert.AreEqual("\\b", endMarker, "Invalid New Begin Marker");

			// TEST 2
			mappingObj = new ECMappingClass();
			mappingObj.BeginMarker = "{b}";
			endMarker = mappingObj.NewBeginMarker;			// retrieve the new Marker (or the current begin)
			Assert.AreEqual("\\{b}", endMarker, "Invalid New Begin Marker");

			// TEST 3
			mappingObj = new ECMappingClass();
			mappingObj.EndMarker = "{b*}";
			endMarker = mappingObj.NewEndMarker;			// retrieve the new Marker (or the current begin)
			Assert.AreEqual("\\{b+}", endMarker, "Invalid New End Marker");

			// TEST 4
			mappingObj = new ECMappingClass();
			mappingObj.EndMarker = "|LastTest****";
			endMarker = mappingObj.NewEndMarker;			// retrieve the new Marker (or the current begin)
			Assert.AreEqual("\\|LastTest+++*", endMarker, "Invalid New End Marker");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test is using project files (\".ssf\") that don't exist in the temp data folder that is created.  TE team needs to investigate.")]
		public void Test_AreProjectsAccessible()
		{
			CheckDisposed();

			///	This test shows how to use the three new methods on the ParatextProjectProxy object.
			///	To effectivly run a test in this environment, a resource file is needed that has PT
			///	projects that can be added to the ParatextProjectProxy object.  With valid projects
			///	the put assignments will not raise an exception.
			///

			IParatextProjectProxy ptProject = new ParatextProjectProxyClass();

			MarkerDomain md = MarkerDomain.MD_Unknown;
			if( md == MarkerDomain.MD_Back )	// needed to remove the CS0219 warning...
				md = MarkerDomain.MD_Note;

			string projectName = "";

			try
			{
				md = MarkerDomain.MD_Vern;
				projectName = "PTVern";
				ptProject.VernTransProj = projectName;

				md = MarkerDomain.MD_Back;
				projectName = "PTBack";
				ptProject.VernTransProj = projectName;

				md = MarkerDomain.MD_Note;
				projectName = "PTNote";
				ptProject.VernTransProj = projectName;
			}
			catch
			{
				string msg;
				msg = "Unable to Add project: " + projectName;
				System.Diagnostics.Debug.WriteLine( msg );
			}

			int markerDomainsNotAccessible = ptProject.AreProjectsAccessible;
			if ( markerDomainsNotAccessible != 0 )
			{
				string projectFileName, msg;

				if ( ((byte)(markerDomainsNotAccessible) & (byte)(MarkerDomain.MD_Vern)) != 0 )
				{
					ptProject.GetProjectFilename( MarkerDomain.MD_Vern, out projectFileName);
					msg = "Project file not Accessible: " + projectFileName;
					System.Diagnostics.Debug.WriteLine(msg);
				}

				if ( ((byte)(markerDomainsNotAccessible) & (byte)(MarkerDomain.MD_Back)) != 0 )
				{
					ptProject.GetProjectFilename( MarkerDomain.MD_Back, out projectFileName);
					msg = "Project file not Accessible: " + projectFileName;
					System.Diagnostics.Debug.WriteLine(msg);
				}

				if ( ((byte)(markerDomainsNotAccessible) & (byte)(MarkerDomain.MD_Note)) != 0 )
				{
					ptProject.GetProjectFilename( MarkerDomain.MD_Note, out projectFileName);
					msg = "Project file not Accessible: " + projectFileName;
					System.Diagnostics.Debug.WriteLine(msg);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Test_AreProjectsAccessible_TheSequel()
		{
			CheckDisposed();

			// Set up a couple of Paratext projects and save the information in a blob.
			IParatextProjectProxy projectProxy = new ParatextProjectProxyClass();
			projectProxy.VernTransProj = "KAM";
			projectProxy.BackTransProj = "TEV";
			byte[] blob = (byte[])projectProxy.AsSafeArray;

			// Recreate the proxy and initialize it with the blob we just created.
			projectProxy = new ParatextProjectProxyClass();
			projectProxy.InitializeFromSafeArray(blob);

			// Verify the AreProjectsAccessible property works properly. All projects should
			// be accessible.
			int inaccessibleProjects = projectProxy.AreProjectsAccessible;
			Assert.AreEqual(0, inaccessibleProjects, "Projects were not accessible");

			// Verify the get_IsValidProject method works properly.
			Assert.AreEqual(1, projectProxy.get_IsValidProject(MarkerDomain.MD_Vern),
				"Vernacular invalid");
			Assert.AreEqual(1, projectProxy.get_IsValidProject(MarkerDomain.MD_Back),
				"Back Trans. invalid");

			try
			{
				// Now blow away the KAM.ssf (i.e. vernacular project file) settings file and
				// check if it is still accessible. It should not be.
				File.Delete(Unpacker.PtProjectTestFolder + "KAM.ssf");

				// Verify the AreProjectsAccessible property works properly. Only the
				// vernacular project should be inaccessible.
				inaccessibleProjects = projectProxy.AreProjectsAccessible;
				Assert.IsTrue((inaccessibleProjects & (int)MarkerDomain.MD_Vern) != 0,
					"Vernacular is accessible");
				Assert.AreEqual((inaccessibleProjects & (int)MarkerDomain.MD_Back), 0,
					"Back Trans is inaccessible");
				Assert.AreEqual((inaccessibleProjects & (int)MarkerDomain.MD_Note), 0,
					"Notes is inaccessible");

				// Verify the get_IsValidProject method works properly.
				Assert.AreEqual(0, projectProxy.get_IsValidProject(MarkerDomain.MD_Vern),
					"Vernacular valid");
				Assert.AreEqual(1, projectProxy.get_IsValidProject(MarkerDomain.MD_Back),
					"Back Trans. invalid");

				// Check that the inaccessible file is correctly named.
				string sInvalidFile;
				projectProxy.GetProjectFilename(MarkerDomain.MD_Vern, out sInvalidFile);
				Assert.IsTrue(sInvalidFile.EndsWith("KAM.ssf"));

				// Now check that AreProjectsAccessible works properly when a proxy is
				// initialized from a blob after the settings file was deleted.
				projectProxy = new ParatextProjectProxyClass();
				projectProxy.InitializeFromSafeArray(blob);

				inaccessibleProjects = projectProxy.AreProjectsAccessible;
				Assert.IsTrue((inaccessibleProjects & (int)MarkerDomain.MD_Vern) != 0,
					"Vernacular is accessible");
				Assert.IsTrue((inaccessibleProjects & (int)MarkerDomain.MD_Back) == 0,
					"Back Trans is inaccessible");
				Assert.IsTrue((inaccessibleProjects & (int)MarkerDomain.MD_Note) == 0,
					"Notes is inaccessible");

				// Check that the inaccessible file is correctly named.
				projectProxy.GetProjectFilename(MarkerDomain.MD_Vern, out sInvalidFile);
				Assert.IsTrue(sInvalidFile.EndsWith("KAM.ssf"));
			}
			finally
			{
				// Unzip the paratext projects again since we just deleted the KAM.ssf.
				Unpacker.UnPackParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RescanParatextProjectFiles()
		{
			CheckDisposed();

			// Set up a couple of Paratext projects and save the information in a blob.
			IParatextProjectProxy projectProxy = new ParatextProjectProxyClass();
			projectProxy.VernTransProj = "KAM";
			projectProxy.BackTransProj = "TEV";

			Assert.AreEqual(44, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Vern),
				"Wrong Number of Vernacular Markers");
			Assert.AreEqual(158, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Back),
				"Wrong Number of Back Translation Markers");

			byte[] blob = (byte[])projectProxy.AsSafeArray;

			// Recreate the proxy and initialize it with the blob we just created.
			projectProxy = new ParatextProjectProxyClass();
			projectProxy.InitializeFromSafeArray(blob);

			// Verify there are still the same number of mappings after loading blob.
			Assert.AreEqual(44, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Vern),
				"Wrong Number of Vernacular Markers");
			Assert.AreEqual(158, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Back),
				"Wrong Number of Back Translation Markers");

			try
			{
				// Unzip a couple of .sty files containing more markers than when the projects
				// were scanned before the blob above was saved.
				Unpacker.UnpackParatextStyWithExtraMarkers();

				// Recan the Vernacular domain to catch the extra 2 markers.
				projectProxy.RefreshDomainMarkers(MarkerDomain.MD_Vern);

				Assert.AreEqual(46, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Vern),
					"Wrong Number of Vernacular Markers after rescan");

				// Recan the Back Trans. domain to catch the extra 2 markers.
				projectProxy.RefreshDomainMarkers(MarkerDomain.MD_Back);

				Assert.AreEqual(161, projectProxy.get_NumberOfMappingsForDomain(MarkerDomain.MD_Back),
					"Wrong Number of Back Translation Markers after recan");
			}
			finally
			{
				// This will restore the original .sty files for other tests.
				Unpacker.UnPackParatextTestProjects();
			}
		}
	}
	#endregion
}
