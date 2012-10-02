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
// File: EditingHelperTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.Common.RootSites
{
	#region DummyEditingHelper
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyEditingHelper : EditingHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text from clipboard.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString CallGetTextFromClipboard()
		{
			CheckDisposed();

			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			return base.GetTextFromClipboard(null, false, propsFact.MakeProps("bla", 0, 0));
		}
	}
	#endregion

	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for SimpleRootSite class.
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	[TestFixture]
	public class EditingHelperInMemoryTests : InMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting unicode characters from the clipboard (TE-4633)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteUnicode()
		{
			CheckDisposed();

			Clipboard.SetText("\u091C\u092E\u094D\u200D\u092E\u0947\u0906",
				TextDataFormat.UnicodeText);

			DummyEditingHelper helper = new DummyEditingHelper();
			ITsString str = helper.CallGetTextFromClipboard();

			Assert.AreEqual("\u091C\u092E\u094D\u200D\u092E\u0947\u0906", str.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a hyperlink to a stringbuilder using the AddHyperlink method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddHyperlink()
		{
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			DynamicMock mockStylesheet = new DynamicMock(typeof(FwStyleSheet));
			mockStylesheet.Strict = true;
			DynamicMock mockHyperlinkStyle = new DynamicMock(typeof(StStyle));
			mockHyperlinkStyle.Strict = true;
			mockHyperlinkStyle.Expect("InUse", true);
			mockStylesheet.ExpectAndReturn("FindStyle", (IStStyle)mockHyperlinkStyle.MockInstance, StStyle.Hyperlink);
			FwStyleSheet styleSheet = (FwStyleSheet)mockStylesheet.MockInstance;

			Assert.IsTrue(EditingHelper.AddHyperlink(strBldr, Cache.DefaultAnalWs, "Click Here",
				"www.google.com", styleSheet));
			Assert.AreEqual(1, strBldr.RunCount);
			Assert.AreEqual("Click Here", strBldr.get_RunText(0));
			ITsTextProps props = strBldr.get_Properties(0);
			StStyleTests.AssertHyperlinkPropsAreCorrect(props, Cache.DefaultAnalWs, "www.google.com");
			mockHyperlinkStyle.Verify();
			mockStylesheet.Verify();
		}
	}

	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for SimpleRootSite class.
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	[TestFixture]
	public class EditingHelperDbTests : InDatabaseFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the TextRepOfObj and MakeObjFromText methods (for copying and pasting pictures)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTextRepOfObj_CmPicture()
		{
			CheckDisposed();

			string internalPathOrig = null;
			string internalPathNew = null;
			try
			{
				using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
				{
					ITsStrFactory factory = TsStrFactoryClass.Create();
					EditingHelper editHelper = new EditingHelper(null);
					CmPicture pict = new CmPicture(m_fdoCache, filemaker.Filename,
						factory.MakeString("Test picture", m_fdoCache.DefaultVernWs),
						StringUtils.LocalPictures);
					Assert.IsNotNull(pict);
					Assert.IsTrue(pict.PictureFileRA.AbsoluteInternalPath == pict.PictureFileRA.InternalPath);
					Guid guid = Cache.GetGuidFromId(pict.Hvo);
					string sTextRepOfObject = editHelper.TextRepOfObj(m_fdoCache, guid);
					int objectDataType;
					guid = editHelper.MakeObjFromText(m_fdoCache, sTextRepOfObject, null,
						out objectDataType);
					CmPicture pictNew = new CmPicture(Cache, Cache.GetIdFromGuid(guid));
					Assert.IsTrue(pict != pictNew);
					internalPathOrig = pict.PictureFileRA.AbsoluteInternalPath;
					internalPathNew = pictNew.PictureFileRA.AbsoluteInternalPath;
					Assert.AreEqual(internalPathOrig, internalPathNew);
					Assert.AreEqual(internalPathOrig.IndexOf("junk"), internalPathNew.IndexOf("junk"));
					Assert.IsTrue(internalPathNew.EndsWith(".jpg"));
					AssertEx.AreTsStringsEqual(pict.Caption.VernacularDefaultWritingSystem.UnderlyingTsString,
						pictNew.Caption.VernacularDefaultWritingSystem.UnderlyingTsString);
					Assert.AreEqual(pict.PictureFileRA.OwnerHVO, pictNew.PictureFileRA.OwnerHVO);
				}
			}
			finally
			{
				// TODO: When Undo works right, these should get cleaned up automatically
				if (internalPathOrig != null)
					File.Delete(internalPathOrig);
				if (internalPathNew != null)
					File.Delete(internalPathNew);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the TextRepOfObj when the guid doesn't reference a real object in the database
		/// (TE-5012).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTextRepOfObj_InvalidObject()
		{
			CheckDisposed();

			EditingHelper editHelper = new EditingHelper(null);
			string sTextRepOfObject = editHelper.TextRepOfObj(m_fdoCache, Guid.Empty);
			Assert.IsNull(sTextRepOfObject);
		}
	}
}
