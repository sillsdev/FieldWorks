// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CmPictureTests.cs
// Responsibility: TE Team

// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the CmPicture class
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmPictureTests: InMemoryFdoTestBase
	{
		#region Data members
		ICmPicture m_pict;
		ArrayList m_internalFilesToDelete = new ArrayList();
		string m_internalPath;
		ITsStrFactory m_factory = TsStrFactoryClass.Create();
		#endregion

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
				if (m_internalFilesToDelete != null)
				{
					foreach (string sFile in m_internalFilesToDelete)
					{
						File.Delete(Path.Combine(DirectoryFinder.FWDataDirectory, sFile));
					}
					m_internalFilesToDelete.Clear();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_pict = null;
			m_factory = null;
			m_internalPath = null;
			m_internalFilesToDelete = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create a CmPicture from the dummy file.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			// This is a bit odd, but because of the unpredictable timing of garbage collection,
			// it's possible for the junk file to be deleted by the GC from a previous test,
			// resulting in a FileNotFoundException in the CmPicture constructor. So we
			// just give it a few shots at it to make it more reliable.
			bool fSucceeded = false;
			for (int i = 0; i < 5 && !fSucceeded; i++)
			{
				try
				{
					using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
					{
						m_pict = new CmPicture(Cache, filemaker.Filename,
							m_factory.MakeString("Test picture", Cache.DefaultVernWs),
							StringUtils.LocalPictures);
						fSucceeded = true;
					}
				}
				catch (FileNotFoundException)
				{

				}
			}
			Assert.IsNotNull(m_pict);
			m_internalPath = m_pict.PictureFileRA.InternalPath;
			Assert.IsNotNull(m_internalPath, "Internal path not set correctly");
			Assert.IsTrue(m_pict.PictureFileRA.AbsoluteInternalPath == m_internalPath, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
			m_internalFilesToDelete.Add(m_pict.PictureFileRA.AbsoluteInternalPath);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any internal copies of the dummy file(s).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			base.Exit();

			foreach (string sFile in m_internalFilesToDelete)
				File.Delete(sFile);
			m_internalFilesToDelete.Clear();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the properties of a new picture created from the given a file, folder, etc.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void CmPictureConstructor_FromFile()
		{
			CheckDisposed();

			Assert.AreEqual("Test picture", m_pict.Caption.VernacularDefaultWritingSystem.Text);
			int ich = m_internalPath.IndexOf("junk");
			Assert.IsTrue(ich > 0);
			Assert.IsTrue(m_internalPath.EndsWith(".jpg"));
			Assert.IsTrue(m_pict.PictureFileRA.AbsoluteInternalPath == m_internalPath, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests whether undo really removes a CmPicture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-2664: Undo of Insert Picture command leaves orphaned CmPicture objects")]
		public void UndoOfCreateCmPicture()
		{
			CheckDisposed();

			int hvoPicture = Cache.CreateObject(CmPicture.kclsidCmPicture);
			Cache.Undo();
			Assert.IsFalse(Cache.IsRealObject(hvoPicture, CmPicture.kclsidCmPicture));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateFile when the original file is no longer present
		/// </summary>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		[Ignore("TE-2917: We no longer throw exceptions if the picture file cannot be found")]
		public void CmFileFinder_OrigFileMissing()
		{
			CheckDisposed();

			// Setup
			ICmFolder folder = CmFolder.FindOrCreateFolder(Cache, (int)LangProject.LangProjectTags.kflidPictures,
				StringUtils.LocalPictures);
			string origFile = m_pict.PictureFileRA.AbsoluteInternalPath;
			try
			{
				File.Delete(origFile);
			}
			catch
			{
			}
			Assert.IsFalse(File.Exists(origFile),
				"Test cannot proceed. Unable to delete Original file.");

			// Test
			ICmFile file = CmFile.FindOrCreateFile(folder, origFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateFile when CmFile already exists with identical original
		/// file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CmFileFinder_OrigFilesMatch()
		{
			CheckDisposed();

			// Setup
			ICmFolder folder = CmFolder.FindOrCreateFolder(Cache, (int)LangProject.LangProjectTags.kflidPictures,
				StringUtils.LocalPictures);
			using (DummyFileMaker maker = new DummyFileMaker("garbage.jpg", true))
			{
				ICmFile fileOrig = new CmFile();
				folder.FilesOC.Add(fileOrig);
				fileOrig.InternalPath = maker.Filename;

				ICmFile file = CmFile.FindOrCreateFile(folder, maker.Filename);
				Assert.AreEqual(fileOrig, file);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateFile when there is no other CmFile with the same orig
		/// file name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CmFileFinder_NoPreExistingCmFile()
		{
			CheckDisposed();

			// Setup
			ICmFolder folder = CmFolder.FindOrCreateFolder(Cache, (int)LangProject.LangProjectTags.kflidPictures,
				StringUtils.LocalPictures);
			using (DummyFileMaker maker = new DummyFileMaker("junk56.jpg", true))
			{
				ICmFile file = CmFile.FindOrCreateFile(folder, maker.Filename);
				Assert.IsNotNull(file, "null CmFile returned");
				Assert.IsNotNull(file.InternalPath, "Internal path not set correctly");
				Assert.IsTrue(file.AbsoluteInternalPath == file.InternalPath, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
				m_internalFilesToDelete.Add(file.AbsoluteInternalPath);
				Assert.IsTrue(m_pict.PictureFileRAHvo != file.Hvo);
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to Create a new picture, given a text representation (e.g., from the
		/// clipboard).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void CmPictureConstructor_FromTextRep()
		{
			CheckDisposed();

			ICmPicture pictNew = new CmPicture(Cache, ((CmPicture)m_pict).TextRepOfPicture,
				StringUtils.LocalPictures);
			Assert.IsTrue(pictNew != m_pict);
			string internalPathNew = pictNew.PictureFileRA.InternalPath;
			Assert.IsNotNull(internalPathNew, "Internal path not set correctly");
			Assert.IsTrue(pictNew.PictureFileRA.AbsoluteInternalPath == internalPathNew, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
			m_internalFilesToDelete.Add(pictNew.PictureFileRA.AbsoluteInternalPath);
			Assert.AreEqual(m_internalPath, internalPathNew);
			Assert.IsTrue(internalPathNew.EndsWith(".jpg"));
			AssertEx.AreTsStringsEqual(m_pict.Caption.VernacularDefaultWritingSystem.UnderlyingTsString,
				pictNew.Caption.VernacularDefaultWritingSystem.UnderlyingTsString);
			Assert.AreEqual(m_pict.PictureFileRA.OwnerHVO, pictNew.PictureFileRA.OwnerHVO);
			Assert.IsNull(pictNew.Description.AnalysisDefaultWritingSystem.Text);
			// REVIEW (TE-7745): What should the default PictureLayoutPosition value be?
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn, pictNew.LayoutPos);
			Assert.AreEqual(100, pictNew.ScaleFactor);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, pictNew.LocationRangeType);
			Assert.AreEqual(0, pictNew.LocationMin);
			Assert.AreEqual(0, pictNew.LocationMax);
			Assert.IsNull(pictNew.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor stores a filename which is not a full path as a
		/// relative path consisting of just that filename.
		/// </summary>
		/// <remarks>
		/// Consider whether this should throw an exception if the argument is not an
		/// absolute path.
		/// </remarks>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void CmPictureConstructor_FullParamsMultipleDescriptionVariants()
		{
			CheckDisposed();
			Dictionary<int, string> descriptions = new Dictionary<int,string>();
			descriptions[Cache.DefaultAnalWs] = "My picture.";
			descriptions[Cache.DefaultVernWs] = "Mi foto.";
			ICmPicture pictNew = new CmPicture(Cache, StringUtils.LocalPictures, 0, null, descriptions,
				m_pict.PictureFileRA.AbsoluteInternalPath, "left", "1-2",
				"Don't use this picture in your book!",
				m_pict.Caption.VernacularDefaultWritingSystem.UnderlyingTsString,
				PictureLocationRangeType.ParagraphRange, "62");
			Assert.IsTrue(pictNew != m_pict);
			string internalPathNew = pictNew.PictureFileRA.InternalPath;
			Assert.AreEqual(pictNew.PictureFileRA.AbsoluteInternalPath, internalPathNew, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
			m_internalFilesToDelete.Add(pictNew.PictureFileRA.AbsoluteInternalPath);
			Assert.AreEqual(m_internalPath, internalPathNew);
			AssertEx.AreTsStringsEqual(m_pict.Caption.VernacularDefaultWritingSystem.UnderlyingTsString,
				pictNew.Caption.VernacularDefaultWritingSystem.UnderlyingTsString);
			Assert.AreEqual(m_pict.PictureFileRA.OwnerHVO, pictNew.PictureFileRA.OwnerHVO);
			Assert.AreEqual("My picture.", pictNew.Description.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Mi foto.", pictNew.Description.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(PictureLayoutPosition.LeftAlignInColumn, pictNew.LayoutPos);
			Assert.AreEqual(62, pictNew.ScaleFactor);
			Assert.AreEqual(PictureLocationRangeType.ParagraphRange, pictNew.LocationRangeType);
			Assert.AreEqual(1, pictNew.LocationMin);
			Assert.AreEqual(2, pictNew.LocationMax);
			Assert.AreEqual("Don't use this picture in your book!",
				pictNew.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor throws the correct exception when given a text
		/// representation (e.g., from the clipboard or import) that contains too few parameters.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "The clipboard format for a Picture was invalid")]
		public void CmPictureConstructor_FromTextRep_TooFewParams()
		{
			CheckDisposed();
			new CmPicture(Cache, "CmPicture||c:\\whatever.jpg||", StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor throws the correct exception when given a text
		/// representation that does not begin with a "CmPicture" token.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "The clipboard format for a Picture was invalid")]
		public void CmPictureConstructor_FromTextRep_MissingCmPictureToken()
		{
			CheckDisposed();
			new CmPicture(Cache, "CmFile||c:\\whatever.jpg||||This is a caption||", StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor throws the correct exception when given a text
		/// representation that has an empty filename token.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "File path not specified.\r\nParameter name: srcFile")]
		public void CmPictureConstructor_FromTextRep_MissingFilename()
		{
			CheckDisposed();
			ICmPicture pictNew = new CmPicture(Cache, "CmPicture||||||This is a caption||",
				StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor throws the correct exception when given a text
		/// representation that has an invalid filename.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "File path (c:\\wha<>tever.jpg) contains at least one invalid character.\r\nParameter name: srcFile")]
		public void CmPictureConstructor_FromTextRep_InvalidFilename()
		{
			CheckDisposed();
			new CmPicture(Cache, "CmPicture||c:\\wha<>tever.jpg||||This is a caption||", StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CmPicture contructor stores a filename which is not a full path as a
		/// relative path consisting of just that filename.
		/// </summary>
		/// <remarks>
		/// Consider whether this should throw an exception if the argument is not an
		/// absolute path.
		/// </remarks>
		/// -------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "File does not have a rooted pathname: whatever.jpg\r\nParameter name: srcFile")]
		public void CmPictureConstructor_FromTextRep_FilenameNotFullPath()
		{
			CheckDisposed();
			new CmPicture(Cache, "CmPicture||whatever.jpg||||This is a caption||", StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the text representation of a picture.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void CmPicture_GetTextRepOfPicture()
		{
			CheckDisposed();

			m_pict.Description.SetAlternative("Your mom", Cache.DefaultAnalWs);
			m_inMemoryCache.ChangeDefaultAnalWs(m_inMemoryCache.SetupWs("es"));
			m_pict.Description.SetAlternative("Tu madre", Cache.DefaultAnalWs);
			m_inMemoryCache.ChangeDefaultAnalWs(m_inMemoryCache.SetupWs("de"));
			string textRep = (m_pict as CmPicture).GetTextRepOfPicture(false, "MyRef", null);
			string [] figParams = textRep.Split(new char[] {'|'}, StringSplitOptions.None);
			Assert.AreEqual("Your mom", figParams[0], "English Description should be exported.");
			Assert.IsTrue(figParams[1].EndsWith("junk.jpg"));
			Assert.IsTrue(Path.IsPathRooted(figParams[1]));
			Assert.AreEqual("col", figParams[2], "Layout position should be exported.");
			Assert.AreEqual(string.Empty, figParams[3], "Picture location should be empty.");
			Assert.AreEqual(string.Empty, figParams[4], "Copyright should be empty.");
			Assert.AreEqual("Test picture", figParams[5], "Caption (vernacular) should be exported.");
			Assert.AreEqual("MyRef", figParams[6], "Picture reference should be exported.");
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to update the properties of a picture, given a file, folder, etc.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void UpdateCmPicture()
		{
			CheckDisposed();

			using (DummyFileMaker fileMaker = new DummyFileMaker("junk1.gif", true))
			{
				((CmPicture)m_pict).UpdatePicture(fileMaker.Filename,
					m_factory.MakeString("Updated Picture", Cache.DefaultVernWs), StringUtils.LocalPictures);
				Assert.AreEqual("Updated Picture", m_pict.Caption.VernacularDefaultWritingSystem.Text);
				string internalPathUpdated = m_pict.PictureFileRA.InternalPath;
				Assert.IsNotNull(internalPathUpdated, "Internal path not set correctly");
				Assert.IsTrue(m_pict.PictureFileRA.AbsoluteInternalPath == internalPathUpdated, "Files outside LangProject.ExtLinkRootDir are stored as absolute paths");
				m_internalFilesToDelete.Add(m_pict.PictureFileRA.AbsoluteInternalPath);
				int ich = internalPathUpdated.IndexOf("junk1");
				Assert.IsTrue(ich > 0);
				Assert.IsTrue(internalPathUpdated.EndsWith(".gif"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a FileNotFoundException is thrown if user attempts to update a picture,
		/// with a file that is no longer accessible.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		//[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		[Ignore("TE-2917: We no longer throw exceptions if the picture file cannot be found")]
		public void UpdateCmPicture_FileNotFound()
		{
			CheckDisposed();

			((CmPicture)m_pict).UpdatePicture("c:\\IDontExist.arg",
				m_factory.MakeString("Updated Picture", Cache.DefaultVernWs), StringUtils.LocalPictures);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get a string representation of the picture suitable to put on the
		/// clipboard.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void TextRepOfPicture()
		{
			CheckDisposed();

			Assert.AreEqual("CmPicture||" +
				m_pict.PictureFileRA.AbsoluteInternalPath + "|col|||Test picture|AfterAnchor|100",
				((CmPicture)m_pict).TextRepOfPicture);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to Insert a CmPicture ORC into a TS string.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Test]
		public void InsertORCAt_Simple()
		{
			CheckDisposed();

			ILangProject lp = Cache.LangProject;
			lp.Description.AnalysisDefaultWritingSystem.Text = "This is my language project";
			ITsString tss = lp.Description.AnalysisDefaultWritingSystem.UnderlyingTsString;
			int ichInsert = 4;
			int cchOrigStringLength = tss.Length;

			((CmPicture)m_pict).InsertORCAt(tss, ichInsert, lp.Hvo,
				(int)CmProject.CmProjectTags.kflidDescription,
				lp.DefaultAnalysisWritingSystem);

			Guid guidPicture = Cache.GetGuidFromId(m_pict.Hvo);

			tss = lp.Description.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(cchOrigStringLength + 1, tss.Length);
			Assert.AreEqual(3, tss.RunCount, "ORC should split original run into 3 runs");
			Assert.AreEqual(0, tss.get_RunAt(ichInsert - 1), "First run should end before where we inserted the ORC");
			Assert.AreEqual(1, tss.get_RunAt(ichInsert), "Second run should be where we inserted the ORC");
			Assert.AreEqual(2, tss.get_RunAt(ichInsert + 1), "Third run should start after where we inserted the ORC");
			string strGuid = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
			Guid guid = MiscUtils.GetGuidFromObjData(strGuid.Substring(1));
			Assert.AreEqual(guidPicture, guid, "Wrong guid was inserted");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseLayoutPos method when given valid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseLayoutPos_Valid()
		{
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "col"));
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "CenterInColumn"));

			Assert.AreEqual(PictureLayoutPosition.CenterOnPage,
			(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "span"));
			Assert.AreEqual(PictureLayoutPosition.CenterOnPage,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "CenterOnPage"));

			Assert.AreEqual(PictureLayoutPosition.RightAlignInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "right"));
			Assert.AreEqual(PictureLayoutPosition.RightAlignInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "RightAlignInColumn"));

			Assert.AreEqual(PictureLayoutPosition.LeftAlignInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "left"));
			Assert.AreEqual(PictureLayoutPosition.LeftAlignInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "LeftAlignInColumn"));

			Assert.AreEqual(PictureLayoutPosition.FillColumnWidth,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "fillcol"));
			Assert.AreEqual(PictureLayoutPosition.FillColumnWidth,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "FillColumnWidth"));

			Assert.AreEqual(PictureLayoutPosition.FillPageWidth,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "fillspan"));
			Assert.AreEqual(PictureLayoutPosition.FillPageWidth,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "FillPageWidth"));

			Assert.AreEqual(PictureLayoutPosition.FullPage,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "fullpage"));
			Assert.AreEqual(PictureLayoutPosition.FullPage,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture), "ParseLayoutPosition", "FullPage"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseLayoutPos method when given invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseLayoutPos_Invalid()
		{
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn,
				(PictureLayoutPosition)ReflectionHelper.GetResult(typeof(CmPicture),
				"ParseLayoutPosition", "monkey brains"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given valid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_Normal()
		{
			Assert.AreEqual(34, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "34"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given no value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_Null()
		{
			Assert.AreEqual(100, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", new object[] {null}));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given no value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_Empty()
		{
			Assert.AreEqual(100, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", String.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given negative value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_Negative()
		{
			Assert.AreEqual(53, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "-53"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given a value that contains both
		/// text and a number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_TextAndNumber()
		{
			Assert.AreEqual(53, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "scale=53"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given a value that contains more
		/// than one number (we just take the first one)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_MultipleNumbers()
		{
			Assert.AreEqual(93, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "down 93, hut1, hut2, 34, 0, hike!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given a value of 0
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_Zero()
		{
			Assert.AreEqual(100, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "0"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given an excessively large value
		/// (we cap it at 1000%)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_HumongousNumber()
		{
			Assert.AreEqual(1000, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "100000"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToolboxPictureInfo.ParseScale method when given a number with an explicit
		/// percent sign.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseScale_PercentSign()
		{
			Assert.AreEqual(43, ReflectionHelper.GetIntResult(typeof(CmPicture),
				"ParseScaleFactor", "43%"));
		}
	}
}
