// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrObjWrapperTests.cs
// Responsibility: TE Team

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ScrObjWrapper class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrObjWrapperTests : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the SO Wrapper can load a Paratext 5 project without crashing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatext5Project()
		{
			using (ScrObjWrapper wrapper = new ScrObjWrapper())
			{
				IScrImportSet settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext5, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
				settings.StartRef = new BCVRef(1, 1, 1);
				settings.EndRef = new BCVRef(66, 22, 21);
				TempSFFileMaker fileMaker = new TempSFFileMaker();

				string fileName = fileMaker.CreateFile("EXO",
				new string[] {@"\mt Exodus", @"\c 1", @"\v 1 This is fun!"});
				settings.AddFile(fileName, ImportDomain.Main, null, null);

				wrapper.LoadScriptureProject(settings);
				Assert.IsFalse(wrapper.BooksPresent.Contains(1));
				Assert.IsTrue(wrapper.BooksPresent.Contains(2));
				string sText, sMarker;
				ImportDomain domain;
				Assert.IsTrue(wrapper.GetNextSegment(out sText, out sMarker, out domain));
				Assert.AreEqual(fileName, wrapper.CurrentFileName);
				Assert.AreEqual(1, wrapper.CurrentLineNumber);
				Assert.AreEqual(new BCVRef(2, 1, 0), wrapper.SegmentFirstRef);
				Assert.AreEqual(new BCVRef(2, 1, 0), wrapper.SegmentLastRef);
				Assert.AreEqual(2, wrapper.ExternalPictureFolders.Count);
				Assert.AreEqual(Cache.LangProject.LinkedFilesRootDir, wrapper.ExternalPictureFolders[0]);
				Assert.AreEqual(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
				wrapper.ExternalPictureFolders[1]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the Patatext 6 project for annotation-only import with only Scripture project
		/// set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadP6ProjectForAnnotationOnlyImportWithOnlyScriptureProjectSet()
		{
			using (ScrObjWrapper wrapper = new ScrObjWrapper())
			{
				IScrImportSet settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);

				Unpacker.UnPackParatextTestProjects();
				settings.ParatextScrProj = "KAM";
				settings.StartRef = new BCVRef(1, 1, 1);
				settings.EndRef = new BCVRef(66, 22, 21);
				settings.ImportAnnotations = true;
				settings.ImportBackTranslation = false;
				settings.ImportTranslation = false;
				wrapper.LoadScriptureProject(settings);
				string sText, sMarker;
				ImportDomain domain;
				Assert.IsTrue(wrapper.GetNextSegment(out sText, out sMarker, out domain));
				Assert.AreEqual(3, wrapper.ExternalPictureFolders.Count);
				Assert.AreEqual(Path.Combine(Unpacker.PTProjectDirectory, @"KAM\Figures"), wrapper.ExternalPictureFolders[0]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the P6 project for annotation-only import with only BT project set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadP6ProjectForAnnotationOnlyImportWithOnlyBTProjectSet()
		{
			using (ScrObjWrapper wrapper = new ScrObjWrapper())
			{
				IScrImportSet settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);

				Unpacker.UnPackParatextTestProjects();
				settings.ParatextBTProj = "KAM";
				settings.StartRef = new BCVRef(1, 1, 1);
				settings.EndRef = new BCVRef(66, 22, 21);
				settings.ImportAnnotations = true;
				settings.ImportBackTranslation = false;
				settings.ImportTranslation = false;
				wrapper.LoadScriptureProject(settings);
				string sText, sMarker;
				ImportDomain domain;
				Assert.IsTrue(wrapper.GetNextSegment(out sText, out sMarker, out domain));
				Assert.AreEqual(Path.Combine(Unpacker.PTProjectDirectory, @"KAM\Figures"), wrapper.ExternalPictureFolders[0]);
			}
		}
	}
}
