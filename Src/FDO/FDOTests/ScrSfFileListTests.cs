// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrSfFileListTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using Rhino.Mocks;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for ScrSfFileList
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrSfFileListTests: BaseTest
	{
		#region data members
		private IOverlappingFileResolver m_resolver;
		private ScrMappingList m_mappingList = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName);
		private ScrSfFileList m_fileList;
		private List<IScrImportFileInfo> m_expectedRemovedFiles;
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
			m_resolver = MockRepository.GenerateStrictMock<IOverlappingFileResolver>();
			m_expectedRemovedFiles = new List<IScrImportFileInfo>();
			m_callCountForVerifyFileRemoved = 0;
			m_fileList = new ScrSfFileList(m_resolver);
			m_fileList.FileRemoved += new ScrImportFileEventHandler(VerifyFileRemoved);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the Add method adds a file to the list (without crashing) even if it
		/// doesn't exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add_NonExistantFile()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1,3));

			IScrImportFileInfo f2 = factory.Create("IdontExist.blurb", m_mappingList,
				ImportDomain.Main, null, null, false);
			f2.Stub(x => x.IsReadable).Return(false);

			m_fileList.Add(f1);
			m_fileList.Add(f2);

			Assert.AreEqual(2, m_fileList.Count);
			Assert.AreEqual(f1, m_fileList[0]);
			Assert.AreEqual(f2, m_fileList[1]);
			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(0, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where the user elects to keep file 2 in the Overlapping Files
		/// dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_KeepFile2()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1,3));

			m_expectedRemovedFiles.Add(f1);

			IScrImportFileInfo f2 = CreateStubFileInfo(factory, "file2",
				new ScrReference(40, 3, 1, ScrVers.English), new ReferenceRange(40, 3, 5));

			IScrImportFileInfo f3 = CreateStubFileInfo(factory, "file3",
				new ScrReference(40, 6, 1, ScrVers.English), new ReferenceRange(40, 6, 7));

			m_resolver.Expect(x => x.ChooseFileToRemove(f2, f1)).Return(f1);

			m_fileList.Add(f1);
			m_fileList.Add(f2);
			m_fileList.Add(f3);

			Assert.AreEqual(2, m_fileList.Count);
			Assert.AreEqual(f2, m_fileList[0]);
			Assert.AreEqual(f3, m_fileList[1]);

			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where the user elects to keep file 1 in the Overlapping Files
		/// dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_KeepFile1()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1, 3));

			IScrImportFileInfo f2 = CreateStubFileInfo(factory, "file2",
				new ScrReference(40, 3, 1, ScrVers.English), new ReferenceRange(40, 3, 5));

			IScrImportFileInfo f3 = CreateStubFileInfo(factory, "file3",
				new ScrReference(40, 6, 1, ScrVers.English), new ReferenceRange(40, 6, 7));

			m_resolver.Expect(x => x.ChooseFileToRemove(f2, f1)).Return(f2);

			m_fileList.Add(f1);
			m_fileList.Add(f2);
			m_fileList.Add(f3);

			Assert.AreEqual(2, m_fileList.Count);
			Assert.AreEqual(f1, m_fileList[0]);
			Assert.AreEqual(f3, m_fileList[1]);

			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where three files overlap, and we keep the two that don't
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepTwoFiles()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1, 3));

			IScrImportFileInfo f2 = CreateStubFileInfo(factory, "file2",
				new ScrReference(40, 3, 1, ScrVers.English), new ReferenceRange(40, 3, 4));

			IScrImportFileInfo f3 = CreateStubFileInfo(factory, "file3",
				new ScrReference(40, 4, 1, ScrVers.English), new ReferenceRange(40, 4, 6));

			m_resolver.Expect(x => x.ChooseFileToRemove(f2, f1)).Return(f2);

			m_fileList.Add(f1);
			m_fileList.Add(f2);
			m_fileList.Add(f3);

			Assert.AreEqual(2, m_fileList.Count);
			Assert.AreEqual(f1, m_fileList[0]);
			Assert.AreEqual(f3, m_fileList[1]);

			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where three files overlap, and we keep the one that is doubly
		/// overlapped. The initially added file is removed, but the third file never gets
		/// added due to the conflict.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepOneFile_RemoveOne()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1, 3));

			m_expectedRemovedFiles.Add(f1);

			IScrImportFileInfo f2 = CreateStubFileInfo(factory, "file2",
				new ScrReference(40, 3, 1, ScrVers.English), new ReferenceRange(40, 3, 4));

			IScrImportFileInfo f3 = CreateStubFileInfo(factory, "file3",
				new ScrReference(40, 4, 1, ScrVers.English), new ReferenceRange(40, 4, 6));

			m_resolver.Expect(x => x.ChooseFileToRemove(f2, f1)).Return(f1);
			m_resolver.Expect(x => x.ChooseFileToRemove(f3, f2)).Return(f3);

			m_fileList.Add(f1);
			m_fileList.Add(f2);
			m_fileList.Add(f3);

			Assert.AreEqual(1, m_fileList.Count);
			Assert.AreEqual(f2, m_fileList[0]);

			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where three files overlap, and we keep the one that is doubly
		/// overlapped. The two files that ultimately get tossed are both initially added but
		/// then subsequently removed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCheckForOverlaps_MultipleOverlaps_KeepOneFile_RemoveTwo()
		{
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();

			IScrImportFileInfo f1 = CreateStubFileInfo(factory, "file1",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1, 3));

			IScrImportFileInfo f2 = CreateStubFileInfo(factory, "file2",
				new ScrReference(40, 4, 1, ScrVers.English), new ReferenceRange(40, 4, 6));

			m_expectedRemovedFiles.Add(f1);
			m_expectedRemovedFiles.Add(f2);

			IScrImportFileInfo f3 = CreateStubFileInfo(factory, "file3",
				new ScrReference(40, 1, 1, ScrVers.English), new ReferenceRange(40, 1, 6));

			m_resolver.Expect(x => x.ChooseFileToRemove(f3, f1)).Return(f1);
			m_resolver.Expect(x => x.ChooseFileToRemove(f3, f2)).Return(f2);

			m_fileList.Add(f1);
			m_fileList.Add(f2);
			m_fileList.Add(f3);

			Assert.AreEqual(1, m_fileList.Count);
			Assert.AreEqual(f3, m_fileList[0]);

			m_resolver.VerifyAllExpectations();
			Assert.AreEqual(m_expectedRemovedFiles.Count, m_callCountForVerifyFileRemoved);
		}
		#endregion

		#region helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// VerifyAllExpectations that notification was issued for the expected file removal
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CheckForOverlaps where the user elects to keep file 1 in the Overlapping Files
		/// dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScrImportFileInfo CreateStubFileInfo(DummyScrImportFileInfoFactory factory,
			string name, ScrReference startRef, ReferenceRange refRange)
		{
			IScrImportFileInfo f = factory.Create(name, m_mappingList, ImportDomain.Main,
						null, null, false);

			f.Stub(x => x.IsReadable).Return(true);
			f.Stub(x => x.IsStillReadable).Return(true);
			f.Stub(x => x.StartRef).Return(startRef);
			f.Stub(x => x.BookReferences).Return(new ReferenceRange[] { refRange });

			return f;
		}
		#endregion
	}
}
