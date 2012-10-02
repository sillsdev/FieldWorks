// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrSfFileListTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Scripture;
using NMock;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for ScrSfFileList
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrSfFileListTests
	{
		#region data members
		private DynamicMock m_resolver;
		private ScrMappingList m_mappingList = new ScrMappingList(MappingSet.Main, null);
		private ScrSfFileList m_fileList;
		private ArrayList m_expectedRemovedFiles;
		private int m_callCountForVerifyFileRemoved;
		#endregion

		#region setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We should have named this with a GUID to avoid international conflicts with Sweden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_resolver = new DynamicMock(typeof(IOverlappingFileResolver));
			m_resolver.Strict = true;
			m_expectedRemovedFiles = new ArrayList();
			m_callCountForVerifyFileRemoved = 0;
			m_fileList = new ScrSfFileList((IOverlappingFileResolver)m_resolver.MockInstance);
			m_fileList.FileRemoved += new ScrImportFileEventHandler(VerifyFileRemoved);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CleanupOverlaps where the user elects to keep file 2 in the Overlapping Files
		/// dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_KeepFile2()
		{
			// Add temp file to the project
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				ScrImportFileInfo f1 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_expectedRemovedFiles.Add(f1);

				ScrImportFileInfo f2 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 3", @"\c 4", @"\c 5"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f3 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 6", @"\c 7"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f4 = new ScrImportFileInfo("c:\\Idontexist.blurb",
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_resolver.ExpectAndReturn("ChooseFileToRemove", f1, f2, f1);

				m_fileList.Add(f1);
				m_fileList.Add(f2);
				m_fileList.Add(f3);
				m_fileList.Add(f4);

				Assert.AreEqual(3, m_fileList.Count);
				Assert.AreEqual(f2, m_fileList[0]);
				Assert.AreEqual(f3, m_fileList[1]);
				Assert.AreEqual(f4, m_fileList[2]);
			}
			m_resolver.Verify();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CleanupOverlaps where the user elects to keep file 1 in the Overlapping Files
		/// dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_KeepFile1()
		{
			// Add temp file to the project
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				ScrImportFileInfo f1 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f2 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 3", @"\c 4", @"\c 5"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f3 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 6", @"\c 7"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f4 = new ScrImportFileInfo("c:\\Idontexist.blurb",
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_resolver.ExpectAndReturn("ChooseFileToRemove", f2, f2, f1);

				m_fileList.Add(f1);
				m_fileList.Add(f2);
				m_fileList.Add(f3);
				m_fileList.Add(f4);

				Assert.AreEqual(3, m_fileList.Count);
				Assert.AreEqual(f1, m_fileList[0]);
				Assert.AreEqual(f3, m_fileList[1]);
				Assert.AreEqual(f4, m_fileList[2]);
			}
			m_resolver.Verify();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CleanupOverlaps where three files overlap, and we keep the two that don't
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepTwoFiles()
		{
			// Add temp file to the project
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				ScrImportFileInfo f1 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f2 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 3", @"\c 4"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f3 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 4", @"\c 5", @"\c 6"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_resolver.ExpectAndReturn("ChooseFileToRemove", f2, f2, f1);

				m_fileList.Add(f1);
				m_fileList.Add(f2);
				m_fileList.Add(f3);

				Assert.AreEqual(2, m_fileList.Count);
				Assert.AreEqual(f1, m_fileList[0]);
				Assert.AreEqual(f3, m_fileList[1]);
			}
			m_resolver.Verify();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CleanupOverlaps where three files overlap, and we keep the one that is doubly
		/// overlapped. The initially added file is removed, but the third file never gets
		/// added due to the conflict.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepOneFile_RemoveOne()
		{
			// Add temp file to the project
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				ScrImportFileInfo f1 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_expectedRemovedFiles.Add(f1);

				ScrImportFileInfo f2 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 3", @"\c 4"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f3 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 4", @"\c 5", @"\c 6"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_resolver.ExpectAndReturn("ChooseFileToRemove", f1, f2, f1);
				m_resolver.ExpectAndReturn("ChooseFileToRemove", f3, f3, f2);

				m_fileList.Add(f1);
				m_fileList.Add(f2);
				m_fileList.Add(f3);

				Assert.AreEqual(1, m_fileList.Count);
				Assert.AreEqual(f2, m_fileList[0]);
			}
			m_resolver.Verify();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CleanupOverlaps where three files overlap, and we keep the one that is doubly
		/// overlapped. The two files that ultimately get tossed are both initially added but
		/// then subsequently removed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepOneFile_RemoveTwo()
		{
			// Add temp file to the project
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				ScrImportFileInfo f1 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				ScrImportFileInfo f2 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 4", @"\c 5", @"\c 6"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_expectedRemovedFiles.Add(f1);
				m_expectedRemovedFiles.Add(f2);

				ScrImportFileInfo f3 = new ScrImportFileInfo(
					filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3", @"\c 4", @"\c 5", @"\c 6"}),
					m_mappingList, ImportDomain.Main, null, 0, false);

				m_resolver.ExpectAndReturn("ChooseFileToRemove", f1, f3, f1);
				m_resolver.ExpectAndReturn("ChooseFileToRemove", f2, f3, f2);

				m_fileList.Add(f1);
				m_fileList.Add(f2);
				m_fileList.Add(f3);

				Assert.AreEqual(1, m_fileList.Count);
				Assert.AreEqual(f3, m_fileList[0]);
			}
			m_resolver.Verify();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}
		#endregion

		#region helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that notification was issued for the expected file removal
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyFileRemoved(object sender, ScrImportFileEventArgs e)
		{
			Assert.AreEqual(m_fileList, sender);
			int i = m_expectedRemovedFiles.IndexOf(e.FileInfo);
			if (i < 0)
				Assert.Fail("VerifyFileRemoved called with unexpected ScrImportFileEventArgs, Filename = " + e.FileInfo.FileName);
			m_expectedRemovedFiles[i] = null;
			m_callCountForVerifyFileRemoved++;
		}
		#endregion
	}
}
