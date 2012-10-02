// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2004' to='2006' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrImportFileInfoTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ScrImportFileInfo.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrImportFileInfoTests
	{
		#region class DummyScrImportFileInfo
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special class for testing the ability to get reference info and SF mappings and to
		/// detect markup errors without actually having to create files. Uses StringReader
		/// instead of FileReader.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class DummyScrImportFileInfo : ScrImportFileInfo
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Construct a ScrImportFileInfo based on a filename. This is used to build an
			/// in-memory list of files.
			/// </summary>
			/// <param name="fileContents">String containing one or more "lines" of data, separated
			/// by \r\n</param>
			/// <param name="mappingList">Sorted list of mappings to which newly found mappings
			/// should (and will) be added</param>
			/// <param name="fParatext5"><c>true</c> to look for backslash markers
			/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
			/// must start on a new line, but Paratext considers all backslashes in the data to be
			/// SF markers.)</param>
			/// ------------------------------------------------------------------------------------
			public DummyScrImportFileInfo(string fileContents, ScrMappingList mappingList,
				bool fParatext5) :
				base(fileContents, mappingList, ImportDomain.Main, null, 0, fParatext5)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Does nothing because the "file" is actually the file contents
			/// </summary>
			/// --------------------------------------------------------------------------------
			protected override void GuessFileEncoding()
			{
				// no-op
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether this file is still readable.
			/// </summary>
			/// <value>Always <c>true</c> for this dummy test version</value>
			/// --------------------------------------------------------------------------------
			public override bool IsStillReadable
			{
				get
				{
					m_isReadable = true;
					return m_isReadable;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a reader for the file (for this dummy test object, the filename is actually the
			/// file contents).
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override TextReader GetReader()
			{
				return new StringReader(m_fileName);
			}
		}
		#endregion

		#region Member data
		private ScrMappingList m_mappingList;
		#endregion

		#region Test Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_mappingList = new ScrMappingList(MappingSet.Main, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does one-time initialization needed for the fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			BCVRefTests.InitializeVersificationTable();
		}
		#endregion

		#region Test of the ReferenceRange class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the book of the reference range is entirely
		/// before the start ref's book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyBeforeBook()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsFalse(range.OverlapsRange(new ScrReference(3, 1, 1, Paratext.ScrVers.English),
				new ScrReference(4, 1, 1, Paratext.ScrVers.English)));
			Assert.IsFalse(range.OverlapsRange(new ScrReference(3, 1, 1, Paratext.ScrVers.English),
				new ScrReference(3, 1, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the chapters of the reference range fall entirely
		/// before the start ref's first chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyBeforeFirstChapter()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsFalse(range.OverlapsRange(new ScrReference(2, 5, 1, Paratext.ScrVers.English),
				new ScrReference(4, 1, 1, Paratext.ScrVers.English)));
			Assert.IsFalse(range.OverlapsRange(new ScrReference(2, 5, 1, Paratext.ScrVers.English),
				new ScrReference(2, 6, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the book of the reference range is entirely
		/// after the end ref's book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyAfterBook()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsFalse(range.OverlapsRange(new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(1, 12, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the chapters of the reference range fall entirely
		/// after the end ref's last chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyAfterLastChapter()
		{
			ReferenceRange range = new ReferenceRange(2, 16, 20);
			Assert.IsFalse(range.OverlapsRange(new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 15, 1, Paratext.ScrVers.English)));
			Assert.IsFalse(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 15, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the book of the reference range is strictly
		/// between the start ref's book and the end ref's book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyContainedBook()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(4, 1, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the reference range exactly matches the start
		/// and end refs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_ExactlyEqual()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 4, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the start of the reference range exactly matches
		/// the start ref and the end of the reference range is before the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_StartEqual_EndsWithin()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 3);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 4, 1, Paratext.ScrVers.English)));
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(57, 1, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the start of the reference range is before
		/// the start ref and the end of the reference range is before the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_StartsBefore_EndsWithin()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 30);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 5, 1, Paratext.ScrVers.English),
				new ScrReference(2, 34, 1, Paratext.ScrVers.English)));
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 5, 1, Paratext.ScrVers.English),
				new ScrReference(57, 1, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the start of the reference range is after the
		/// start ref and the end of the reference range exactly matches the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_StartsWithin_EndEqual()
		{
			ReferenceRange range = new ReferenceRange(2, 3, 4);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 2, 1, Paratext.ScrVers.English),
				new ScrReference(2, 4, 1, Paratext.ScrVers.English)));
			Assert.IsTrue(range.OverlapsRange(new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 4, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the start of the reference range is before the
		/// start ref and the end of the reference range exactly matches the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_StartsBefore_EndEqual()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 2, 1, Paratext.ScrVers.English),
				new ScrReference(2, 4, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the start of the reference range exactly matches
		/// the start ref and the end of the reference range is after the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_StartEqual_EndsAfter()
		{
			ReferenceRange range = new ReferenceRange(2, 1, 4);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 3, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the chapters of the reference range are entirely
		/// contained in the chapters of the start ref book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyContainedChaptersInStartBook()
		{
			ReferenceRange range = new ReferenceRange(2, 2, 39);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(3, 3, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the chapters of the reference range are entirely
		/// contained in the chapters of the end ref book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyContainedChaptersInEndBook()
		{
			ReferenceRange range = new ReferenceRange(2, 2, 39);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 40, 1, Paratext.ScrVers.English)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OverlapsRange method when the chapters of the reference range are entirely
		/// contained in the book represented by the start and end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlapsRange_EntirelyContainedChaptersInStartAndEndBook()
		{
			ReferenceRange range = new ReferenceRange(2, 2, 39);
			Assert.IsTrue(range.OverlapsRange(new ScrReference(2, 1, 1, Paratext.ScrVers.English),
				new ScrReference(2, 40, 1, Paratext.ScrVers.English)));
		}
		#endregion

		#region reference range and overlap tests
		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Scan a file that has a single book of data fro references
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void ScanSingleBookFile()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\c 2\r\n\\c 3\r\n\\c 4";
			DummyScrImportFileInfo info = new DummyScrImportFileInfo(fileContents,
				m_mappingList, false);

			ReferenceRange[] range = info.BookReferences;
			Assert.IsNotNull(range, "No reference range was created");
			Assert.AreEqual(1, range.Length);
			Assert.AreEqual(40, range[0].Book);
			Assert.AreEqual(1, range[0].StartChapter);
			Assert.AreEqual(4, range[0].EndChapter);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Scan a file that has a two books of data
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void ScanMultiBookFile()
		{
			string fileContents = "\\id MAT\r\n\\c 12\r\n\\c 25\r\n\\c 34\r\n\\id REV\r\n\\c 1\r\n\\c 2";
			DummyScrImportFileInfo info = new DummyScrImportFileInfo(fileContents,
				m_mappingList, false);

			ReferenceRange[] range = info.BookReferences;
			Assert.IsNotNull(range, "No reference range was created");
			Assert.AreEqual(2, range.Length);

			Assert.AreEqual(40, range[0].Book);
			Assert.AreEqual(12, range[0].StartChapter);
			Assert.AreEqual(34, range[0].EndChapter);

			Assert.AreEqual(66, range[1].Book);
			Assert.AreEqual(1, range[1].StartChapter);
			Assert.AreEqual(2, range[1].EndChapter);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Scan a file that has out of order chapter numbers
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void ScanOutOfOrderChapterFile()
		{
			string fileContents = "\\id MAT\r\n\\c 4\r\n\\c 3\r\n\\c 2\r\n\\c 1";
			DummyScrImportFileInfo info = new DummyScrImportFileInfo(fileContents,
				m_mappingList, false);

			ReferenceRange[] range = info.BookReferences;
			Assert.IsNotNull(range, "No reference range was created");
			Assert.AreEqual(1, range.Length);
			Assert.AreEqual(40, range[0].Book);
			Assert.AreEqual(4, range[0].StartChapter);
			Assert.AreEqual(1, range[0].EndChapter);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Scan a file that has a single book of data with a single chapter
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void ScanSingleChapterFile()
		{
			string fileContents = "\\id MAT\r\n\\c 5";
			DummyScrImportFileInfo info = new DummyScrImportFileInfo(fileContents,
				m_mappingList, false);

			ReferenceRange[] range = info.BookReferences;
			Assert.IsNotNull(range, "No reference range was created");
			Assert.AreEqual(1, range.Length);
			Assert.AreEqual(40, range[0].Book);
			Assert.AreEqual(5, range[0].StartChapter);
			Assert.AreEqual(5, range[0].EndChapter);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Scan a file that has no chapters
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void ScanNoChapterFile()
		{
			string fileContents = "\\id PHM\r\n\blah haha - no chapters!";
			DummyScrImportFileInfo info = new DummyScrImportFileInfo(fileContents,
				m_mappingList, false);

			ReferenceRange[] range = info.BookReferences;
			Assert.IsNotNull(range, "No reference range was created");
			Assert.AreEqual(1, range.Length);
			Assert.AreEqual(57, range[0].Book);
			Assert.AreEqual(1, range[0].StartChapter);
			Assert.AreEqual(1, range[0].EndChapter);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Test overlap when there is no overlap between two different books
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void NoOverlapInTwoBooks()
		{
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				string fileName1 = filemaker.CreateFile("EXO", new string[] {@"\c 1", @"\c 2", @"\c 3", @"\c 25"});
				ScrImportFileInfo map1 = new ScrImportFileInfo(fileName1, m_mappingList,
					ImportDomain.Main, null, 0);

				string fileName2 = filemaker.CreateFile("ACT", new string[] {@"\c 1", @"\c 2", @"\c 3", @"\c 25"});
				ScrImportFileInfo map2 = new ScrImportFileInfo(fileName2, m_mappingList,
					ImportDomain.Main, null, 0);

				Assert.IsFalse(ScrImportFileInfo.CheckForOverlap(map1, map2));
				Assert.IsFalse(ScrImportFileInfo.CheckForOverlap(map2, map1));
			}
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Test overlap when there is no overlap in the same book
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void NoOverlapInOneBook()
		{
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				string fileName1 = filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3", @"\c 4"});
				ScrImportFileInfo map1 = new ScrImportFileInfo(fileName1, m_mappingList,
					ImportDomain.Main, null, 0);

				string fileName2 = filemaker.CreateFile("MAT", new string[] {@"\c 5", @"\c 6", @"\c 8", @"\c 18"});
				ScrImportFileInfo map2 = new ScrImportFileInfo(fileName2, m_mappingList,
					ImportDomain.Main, null, 0);

				Assert.IsFalse(ScrImportFileInfo.CheckForOverlap(map1, map2));
				Assert.IsFalse(ScrImportFileInfo.CheckForOverlap(map2, map1));
			}
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Test overlap when there is an overlap in the same book
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void OverlapInOneBook()
		{
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				string fileName1 = filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 5", @"\c 8"});
				ScrImportFileInfo map1 = new ScrImportFileInfo(fileName1, m_mappingList,
					ImportDomain.Main, null, 0);

				string fileName2 = filemaker.CreateFile("MAT", new string[] {@"\c 5", @"\c 6", @"\c 8", @"\c 18"});
				ScrImportFileInfo map2 = new ScrImportFileInfo(fileName2, m_mappingList,
					ImportDomain.Main, null, 0);

				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map1, map2));
				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map2, map1));
			}
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Test overlap when one file contains a subset of the chapters in the other
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void OverlapWhenOneFileContainsSubset()
		{
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				string fileName1 = filemaker.CreateFile("MAT", new string[] {@"\c 3",  @"\c 4", @"\c 5", @"\c 6"});
				ScrImportFileInfo map1 = new ScrImportFileInfo(fileName1, m_mappingList,
					ImportDomain.Main, null, 0);

				string fileName2 = filemaker.CreateFile("MAT", new string[] {@"\c 4", @"\c 5"});
				ScrImportFileInfo map2 = new ScrImportFileInfo(fileName2, m_mappingList,
					ImportDomain.Main, null, 0);

				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map1, map2));
				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map2, map1));
			}
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Test overlap when there is overlap between two files with multiple books
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void OverlapInTwoMaps()
		{
			using (TempSFFileMaker filemaker = new TempSFFileMaker())
			{
				string fileName1 = filemaker.CreateFile("MAT", new string[] {@"\c 1", @"\c 2", @"\c 3",
																			@"\id MRK", @"\c 1", @"\c 2", @"\c 3"});
				ScrImportFileInfo map1 = new ScrImportFileInfo(fileName1, m_mappingList,
					ImportDomain.Main, null, 0);

				string fileName2 = filemaker.CreateFile("MRK", new string[] {@"\c 3",
																				@"\id LUK", @"\c 1", @"\c 2", @"\c 3"});
				ScrImportFileInfo map2 = new ScrImportFileInfo(fileName2, m_mappingList,
					ImportDomain.Main, null, 0);

				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map1, map2));
				Assert.IsTrue(ScrImportFileInfo.CheckForOverlap(map2, map1));
			}
		}
		#endregion

		#region Error reporting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that ScrImportFileInfo constructor throws the proper exception when attempting
		/// to read a file that does not have a required chapter number. Jira number for this
		/// is TE-468 (part of TE-76).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_VerseNoChapter_required()
		{
			try
			{
				// Test Genesis (has multiple chapters)
				new DummyScrImportFileInfo("\\id GEN\r\n\\mt Genesis\r\n\\v 1 My verse",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.MissingChapterNumber, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that ScrImportFileInfo constructor does not throw an exception when attempting
		/// to read a file that does not have a non-required chapter number. Jira number for this
		/// is TE-468 (part of TE-76).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileValid_VerseNoChapter_optional()
		{
			// Test Jude (has only one chapter)
			new DummyScrImportFileInfo("\\id JUD\r\n\\mt Jude\r\n\\v 1", m_mappingList, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to load a file that has a chapter number before a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_ChapterNoBook()
		{
			try
			{
				// Test Genesis (has multiple chapters)
				new DummyScrImportFileInfo("\\c 1\r\n\\id GEN\r\n\\mt Genesis\r\n\\v 1 My verse",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.ChapterWithNoBook, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to load a file that has a verse number before a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_VerseNoBook()
		{
			try
			{
				new DummyScrImportFileInfo("\\v 1\r\n\\id GEN\r\n\\mt Genesis\r\n\\v 1 My verse",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.VerseWithNoBook, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to load a file that does not have a required chapter number.
		/// Jira number for this is TE-468 (part of TE-76).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_NoChapter_required()
		{
			try
			{
				// Test Genesis (has more than 1 chapter)
				new DummyScrImportFileInfo("\\id GEN\r\n\\mt Genesis\r\n\\hmmm what?!",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException sue)
			{
				Assert.AreEqual(SUE_ErrorCode.NoChapterNumber, sue.ErrorCode);
			}
			catch (Exception e)
			{
				Assert.Fail("Wrong exception detected: " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that ScrImportFileInfo constructor does not throw an exception when attempting
		/// to load a file that does not have a non-required chapter number.
		/// Jira number for this is TE-468 (part of TE-76).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileValid_NoChapter_optional()
		{
			// Test Jude (has only one chapter)
			new DummyScrImportFileInfo("\\id JUD\r\n\\mt Jude\r\n\\hmmm what?!", m_mappingList, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to load a file that does not have an ID marker.
		/// Jira number for this is TE-76.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_MissingIdMarker()
		{
			try
			{
				new DummyScrImportFileInfo(@"\_sh Genesis", m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.MissingBook, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for invalid data in markers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_InvalidMarkerCharacters()
		{
			try
			{
				new DummyScrImportFileInfo("\\id MAT\r\n\\mt Matthew\r\n\\a\u0100",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.InvalidCharacterInMarker, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exception expected if \v marker is followed by text instead of a verse number.
		/// Jira number for this is TE-511.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_BogusVerseNumbers_Text()
		{
			try
			{
				new DummyScrImportFileInfo("\\id GEN\r\n\\mt Genesis\r\n\\c 1\r\n\\v Bogus\r\n\\c 2\r\n\\v 1 Valid",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.InvalidVerseNumber, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exception expected if a verse number range goes from high to low, e.g. 4-2.
		/// Jira number for this is TE-511.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_BogusVerseNumbers_ReverseRange()
		{
			try
			{
				new DummyScrImportFileInfo("\\id GEN\r\n\\mt Genesis\r\n\\c 1\r\n\\v 4-1\r\n\\c 2\r\n\\v 1 Valid",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.InvalidVerseNumber, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that blank lines are handled gracefully
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileValid_BlankLines()
		{
			// Should not throw an exception on the blank lines.
			new DummyScrImportFileInfo("\\id GEN\r\n\\c 1\r\n\r\n\\v 1\r\n\r\n\\c 2\r\n\\v 1 Valid",
				m_mappingList, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for excluding all data before an ID line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_NonExcludedMarkersBeforeIdLine()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFileNoID(
					new string[] {@"\rem Ignore",
									 @"\mt Genesis",
									 @"\id GEN",
									 @"\c 1",
									 @"\v 1 Valid"});

				// Try it first without strict checking to make sure it does not throw an exception
				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList, ImportDomain.Main, null, 0);

				// Now use strict checking to make sure the error occurs.
				try
				{
					fileInfo.PerformStrictScan();
					Assert.Fail("Failed to throw an exception.");
				}
				catch (ScriptureUtilsException e)
				{
					Assert.AreEqual(SUE_ErrorCode.UnexcludedDataBeforeIdLine, e.ErrorCode);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for an invalid book code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_InvalidBook()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFileNoID(
					new string[] {@"\id Hmmm",
									 @"\mt Genesis",
									 @"\id GEN",
									 @"\c 1",
									 @"\v 1 Valid"});

				try
				{
					new ScrImportFileInfo(sFilename, m_mappingList, ImportDomain.Main, null, 0, true);
					Assert.Fail("Failed to throw an exception.");
				}
				catch (ScriptureUtilsException e)
				{
					Assert.AreEqual(SUE_ErrorCode.InvalidBookID, e.ErrorCode);
				}
				catch
				{
					Assert.Fail("Wrong exception detected.");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a chapter number without an ID
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_ChapterWithoutID()
		{
			try
			{
				new DummyScrImportFileInfo("\\c 1\r\n\\id GEN\r\n\\v 1 Valid",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.ChapterWithNoBook, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expected exception when chapter number is invalid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileInvalid_InvalidChapterNumber()
		{
			try
			{
				new DummyScrImportFileInfo("\\id GEN\r\n\\c One\r\n\\v 1 Valid",
					m_mappingList, false);
				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.InvalidChapterNumber, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}
		#endregion

		#region Figure Entries tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for good \fig entries where the \fig field has not end delimeter (delimited by
		/// the end of line)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_NoEndMarker()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {	 @"\mt Romans",
									 @"\c 1",
									 @"\v 1 Hello",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5|stuff6"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false,
					MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for good \fig entries where the \fig field is delimited by \fig*
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_WithEndMarker()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				// Note that ending \fig* is optional even if it is in mapping.
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {	 @"\mt Romans",
									 @"\c 1",
									 @"\v 1 Hello",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5|stuff6\fig* etc.",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5|stuff6"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", @"\fig*", false,
					MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for good \fig entries where the \fig field is delimited by \fig*, followed by
		/// other marker(s) and then another \fig
		/// Jira # is TE-7669
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoFigureEntries_good_NotScanningInline()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				// Note that ending \fig* is optional even if it is in mapping.
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {	 @"\c 1",
									 @"\v 1 Hello",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5|stuff6\fig*",
									 @"\new will I be seen?",
									 @"\v 2 I'm a verse, too.",
									 @"\btfig stuff1|stuff2|stuff3|stuff4|stuff5|stuff6\btfig*"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", @"\fig*", false,
					MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, false);

				m_mappingList.Delete(m_mappingList[@"\new"]);
				Assert.IsNull(m_mappingList[@"\new"]);

				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();

				Assert.IsNotNull(m_mappingList[@"\new"]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for bad \fig entries
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_bad_tooFewTokens()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {	 @"\mt Romans",
									 @"\c 1",
									 @"\v 1 Hello",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				// Try it first without strict checking to make sure it does not throw an exception
				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList, ImportDomain.Main, null, 0);

				try
				{
					// Use strict checking to make sure the error occurs.
					fileInfo.PerformStrictScan();
				}
				catch (ScriptureUtilsException e)
				{
					Assert.AreEqual(SUE_ErrorCode.BadFigure, e.ErrorCode);
					return;
				}
				Assert.Fail("The expected exception did not happen");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a \fig entry whose parameters are split across lines in the file (TE-7669).
		/// This case covers when the \fig is the last thing in the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_SplitAcrossLine_NothingFollowing()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {@"\mt Romans",
						@"\c 1",
						@"\v 1 Hello",
						@"\fig stuff1|stuff2|stuff3|stuff that",
						@"is for 4|stuff5|stuff6"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a \fig entry whose parameters are split across lines in the file (TE-7669).
		/// This case covers when the \fig is split across multiple lines.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_SplitAcrossLine_EachParamOnItsOwnLine()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {@"\mt Romans",
						@"\c 1",
						@"\v 1 Hello",
						@"\fig stuff1",
						@"|stuff2",
						@"|stuff3",
						@"|stuff4",
						@"|stuff5",
						@"|stuff6"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a \fig entry whose parameters are split across lines in the file (TE-7669).
		/// This case covers when the \fig parameters are immediately followed (on the same
		/// line) by an in-line marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_SplitAcrossLine_InlineMarkerFollowing()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {@"\mt Romans",
						@"\c 1",
						@"\v 1 Hello",
						@"\fig stuff1|stuff2|stuff3|stuff that",
						@"is for 4|stuff5|stuff6\em my child!\em*"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a \fig entry whose parameters are split across lines in the file (TE-7669).
		/// This case covers when the \fig parameters are immediately followed (on the same
		/// line) by a closing \fig* marker and additional data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_SplitAcrossLine_WithClosingMarker()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {@"\mt Romans",
						@"\c 1",
						@"\v 1 Hello",
						@"\fig stuff1|stuff2|stuff3|stuff that",
						@"is for 4|stuff5|stuff6\fig* my child\"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for a \fig entry whose parameters are split across lines in the file (TE-7669).
		/// This case covers when the \fig is followed by another line in the file that begins
		/// with a new marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_good_SplitAcrossLine_LineWithMarkerFollowing()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {@"\mt Romans",
						@"\c 1",
						@"\v 1 Hello",
						@"\fig stuff1|stuff2|stuff3|stuff that",
						@"is for 4|stuff5|stuff6",
						@"\v 2 This is Paul."},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				ScrImportFileInfo fileInfo = new ScrImportFileInfo(sFilename, m_mappingList,
					ImportDomain.Main, null, 0, true);
				// Use strict checking to make sure no error occurs.
				fileInfo.PerformStrictScan();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure Figure validation doesn't happen if \fig marker is excluded
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FigureEntries_excluded()
		{
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string sFilename = fileMaker.CreateFile("ROM",
					new string[] {	 @"\mt Romans",
									 @"\c 1",
									 @"\v 1 Hello",
									 @"\fig stuff1|stuff2|stuff3|stuff4|stuff5"},
					Encoding.UTF8, false);

				m_mappingList.Add(new ImportMappingInfo(@"\fig", null, true, MappingTargetType.Figure, MarkerDomain.Default, null, null));

				new ScrImportFileInfo(sFilename, m_mappingList, ImportDomain.Main, null, 0, true);
			}
		}
		#endregion

		#region Paratext5 in-line SF mapping detection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox and Paratext 5 work identically if no in-line backslash markers
		/// are present in the data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_NoInlineMappings_Baseline()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1\r\n\\v 2\r\n\\c 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(3, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_SingleInlineMapping()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\f footnote\r\n\\v 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			m_mappingList.Delete(m_mappingList[@"\f"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_MultipleInlineMappings()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\f + \\ft footnote\r\n\\v 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(5, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			m_mappingList.Delete(m_mappingList[@"\f"]);
			m_mappingList.Delete(m_mappingList[@"\ft"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_EndMarkers()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\f + \\ft footnote \\em text\\em* \\f*\r\n\\v 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(6, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			ImportMappingInfo mapping = m_mappingList[@"\f"];
			Assert.AreEqual(@"\f*", mapping.EndMarker);
			m_mappingList.Delete(mapping);
			m_mappingList.Delete(m_mappingList[@"\ft"]);
			mapping = m_mappingList[@"\em"];
			Assert.AreEqual(@"\em*", mapping.EndMarker);
			m_mappingList.Delete(mapping);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_AsteriskedMarkersWithNoBeginMarker()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\em* Dude!";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			m_mappingList.Delete(m_mappingList[@"\em*"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// In this test, we have a stray end marker that occurs before the corresponding
		/// begin marker. We should assume the data is just bad, and get the mapping info
		/// right. (Import can safely handle out-of-place end markers.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_AsteriskedMarkerPrecedesBeginMarker()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\em* Dude!\\em Food";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			ImportMappingInfo mapping = m_mappingList[@"\em"];
			Assert.AreEqual(@"\em*", mapping.EndMarker);
			m_mappingList.Delete(mapping);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// In this test, we have an end marker immediately followed by a comma, with no
		/// whitespace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_AsteriskedMarkerWithFollowingComma()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 The \\em Dude\\em*, but no";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			ImportMappingInfo mapping = m_mappingList[@"\em"];
			Assert.AreEqual(@"\em*", mapping.EndMarker);
			m_mappingList.Delete(mapping);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_InlineMappingAtEndOfLine()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 \\f\r\n\\v 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			m_mappingList.Delete(m_mappingList[@"\f"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Toolbox does not find in-line mappings and Paratext 5 does
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paratext5MappingDetection_InlineMappingInSecondLine()
		{
			string fileContents = "\\id MAT\r\n\\c 1\r\n\\v 1 Some more\r\ntext\\f footnote\r\n\\v 2";
			new DummyScrImportFileInfo(fileContents, m_mappingList, true);
			Assert.AreEqual(4, m_mappingList.Count);
			m_mappingList.Delete(m_mappingList[@"\id"]);
			m_mappingList.Delete(m_mappingList[@"\c"]);
			m_mappingList.Delete(m_mappingList[@"\v"]);
			m_mappingList.Delete(m_mappingList[@"\f"]);
			new DummyScrImportFileInfo(fileContents, m_mappingList, false);
			Assert.AreEqual(3, m_mappingList.Count);
		}
		#endregion
	}
}
