// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrFdoTestBase.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// <summary>
	/// Base class for FDO tests that use a real FdoCache, or a fake one.
	/// </summary>
	public abstract class ScrFdoTestBase : FdoTestBase
	{
		/// <summary></summary>
		protected IScripture m_scr;

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_scr = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup that runs once for the entire fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			m_scr = Cache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo everything possible in the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			m_scr = null;
			base.Exit();
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Create a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set).
		///// </summary>
		///// <param name="book">Book to insert footnote into</param>
		///// <param name="para">Paragraph to insert footnote into</param>
		///// <param name="iFootnotePos">The 0-based index of the new footnote in the collection
		///// of footnotes owned by the book</param>
		///// <param name="ichPos">The 0-based character offset into the paragraph</param>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//protected StFootnote InsertTestFootnote(IScrBook book, IStTxtPara para,
		//    int iFootnotePos, int ichPos)
		//{
		//    // Create the footnote
		//    StFootnote footnote = new StFootnote();
		//    book.FootnotesOS.InsertAt(footnote, iFootnotePos);

		//    // Update the paragraph contents to include the footnote marker
		//    ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
		//    footnote.InsertOwningORCIntoPara(tsStrBldr, ichPos, 0); // Don't care about ws
		//    para.Contents.UnderlyingTsString = tsStrBldr.GetString();

		//    return footnote;
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Make sure footnote exists and is referred to properly in the paragraph contents
		///// </summary>
		///// <param name="footnote"></param>
		///// <param name="para"></param>
		///// <param name="ich">Character position where ORC should be</param>
		///// ------------------------------------------------------------------------------------
		//protected void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		//{
		//    Guid guid = Cache.GetGuidFromId(footnote.Hvo);
		//    ITsString tss = para.Contents.UnderlyingTsString;
		//    int iRun = tss.get_RunAt(ich);
		//    ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
		//    string objData = orcPropsParaFootnote.GetStrPropValue(
		//        (int)FwTextPropType.ktptObjData);
		//    Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
		//    // Send the objData string without the first character because the first character
		//    // is the object replacement character and the rest of the string is the GUID.
		//    Guid newFootnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
		//    Assert.AreEqual(guid, newFootnoteGuid);
		//    Assert.AreEqual(footnote.Hvo, Cache.GetIdFromGuid(newFootnoteGuid));
		//    string sOrc = tss.get_RunText(iRun);
		//    Assert.AreEqual(StringUtils.kchObject, sOrc[0]);
		//}
	}
}
