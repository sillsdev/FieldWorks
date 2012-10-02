// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2005' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTxtParaTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for StTxtPara class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTxtParaTests : InMemoryFdoTestBase
	{
		#region Member data
		private IStText m_currentText;
		private IStText m_archivedText;
		private FdoOwningSequence<IStFootnote> m_archivedFootnotesOS;
		private FdoOwningSequence<IStFootnote> m_currentFootnotesOS;
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();

			m_currentText = m_inMemoryCache.CreateArbitraryStText();
			StTxtPara para = m_inMemoryCache.AddParaToMockedText(m_currentText.Hvo, "Normal");
			m_inMemoryCache.AddRunToMockedPara(para, "1", "CharacterStyle1");
			m_inMemoryCache.AddRunToMockedPara(para, "1", "CharacterStyle2");
			m_inMemoryCache.AddRunToMockedPara(para, "This text has no char style.", null);
			m_currentFootnotesOS = m_inMemoryCache.CreateArbitraryFootnoteSequence(m_currentText);

			m_archivedText = m_inMemoryCache.CreateArbitraryStText();
			para = m_inMemoryCache.AddParaToMockedText(m_archivedText.Hvo, "Normal");
			m_inMemoryCache.AddRunToMockedPara(para, "1", "CharacterStyle1");
			m_inMemoryCache.AddRunToMockedPara(para, "1", "CharacterStyle2");
			m_inMemoryCache.AddRunToMockedPara(para, "This is the previous version of the text.", null);
			m_archivedFootnotesOS = m_inMemoryCache.CreateArbitraryFootnoteSequence(m_archivedText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to set up the test paragraph in m_archivedText, including footnotes
		/// and back translations, plus any other fields deemed necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private StTxtPara SetUpParagraphInArchiveWithFootnotesAndBT()
		{
			// Prepare the Revision paragraph in m_archivedText
			// note: CreateTestData has already placed "11This is the previous version of the text."
			//  in paragraph in m_archivedText.

			StTxtPara paraRev = (StTxtPara)m_archivedText.ParagraphsOS[0];
			paraRev.StyleRules = StyleUtils.ParaStyleTextProps("Line 1");
			// add footnotes to existing paragraph.
			StFootnote footnote1 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, paraRev, 6, "Footnote1");
			StFootnote footnote2 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, paraRev, 10, "Footnote2");
			Assert.AreEqual(2, m_archivedFootnotesOS.Count);

			// Add two back translations of the para and footnotes
			int[] wsBt = new int[] { InMemoryFdoCache.s_wsHvos.En, InMemoryFdoCache.s_wsHvos.De };
			foreach (int ws in wsBt)
			{
				// add back translation of the para, and status
				ICmTranslation paraTrans = m_inMemoryCache.AddBtToMockedParagraph(paraRev, ws);
				m_inMemoryCache.AddRunToMockedTrans(paraTrans, ws, "BT of test paragraph" + ws.ToString(), null);
				paraTrans.Status.SetAlternative(BackTranslationStatus.Finished.ToString(), ws);
				// add BT footnotes, and status
				ICmTranslation footnoteTrans = m_inMemoryCache.AddFootnoteORCtoTrans(paraTrans, 2, ws, footnote1,
					"BT of footnote1 " + ws.ToString());
				footnoteTrans.Status.SetAlternative(BackTranslationStatus.Checked.ToString(), ws);
				footnoteTrans = m_inMemoryCache.AddFootnoteORCtoTrans(paraTrans, 6, ws, footnote2,
					"BT of footnote2 " + ws.ToString());
				footnoteTrans.Status.SetAlternative(BackTranslationStatus.Finished.ToString(), ws);
			}
			return paraRev;
		}
		#endregion

		#region Copy Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the .CopyTo method, copying a paragraph which has footnotes and back
		/// translation. Results should be identical to using other copy methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo()
		{
			StTxtPara paraRev = SetUpParagraphInArchiveWithFootnotesAndBT();
			StTxtPara newPara = m_inMemoryCache.AddParaToMockedText(m_currentText.Hvo, "Normal");

			// Now, call the method under test
			paraRev.CopyTo(newPara);

			VerifyCopiedPara(newPara);
			VerifyParagraphsAreDifferentObjects(paraRev, newPara);
		}
		#endregion

		#region RemoveOwnedObjectsForString Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing one owned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_Simple()
		{
			CheckDisposed();

			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps("Normal");
			paraBldr.AppendRun("Test Paragraph",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			StTxtPara para = paraBldr.CreateParagraph(m_currentText.Hvo);

			m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 10, null);
			Assert.AreEqual(1, m_currentFootnotesOS.Count);

			para.RemoveOwnedObjectsForString(5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing two footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_TwoFootnotes()
		{
			CheckDisposed();

			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps("Normal");
			paraBldr.AppendRun("Test Paragraph",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			StTxtPara para = paraBldr.CreateParagraph(m_currentText.Hvo);
			m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 8, null);
			m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 10, null);
			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			para.RemoveOwnedObjectsForString(5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing two footnotes which are referenced in the back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_FootnotesWithBT()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];
			// Add footnotes to existing paragraph.
			StFootnote footnote1 = m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 6, "Footnote1");
			StFootnote footnote2 = m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 10, "Footnote2");
			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			// add two back translations of the para and footnotes
			ICmTranslation trans;
			int[] wsBt = new int[] { InMemoryFdoCache.s_wsHvos.En, InMemoryFdoCache.s_wsHvos.De };
			foreach (int ws in wsBt)
			{
				// add back translation of the para
				trans = m_inMemoryCache.AddBtToMockedParagraph(para, ws);
				m_inMemoryCache.AddRunToMockedTrans(trans, ws, "BT of test paragraph", null);
				// add BT footnotes
				m_inMemoryCache.AddFootnoteORCtoTrans(trans, 2, ws, footnote1, "BT of footnote1");
				m_inMemoryCache.AddFootnoteORCtoTrans(trans, 6, ws, footnote2, "BT of footnote2");
				Assert.AreEqual("BT" + StringUtils.kchObject + " of" + StringUtils.kchObject + " test paragraph",
					trans.Translation.GetAlternative(ws).Text); // confirm that ORCs were inserted in BTs
			}

			para.RemoveOwnedObjectsForString(5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			// We expect that the ORCs would have also been removed from both back translations.
			trans = para.GetBT() as CmTranslation;
			Assert.IsNotNull(trans);
			foreach (int ws in wsBt)
				Assert.AreEqual("BT of test paragraph", trans.Translation.GetAlternative(ws).Text);
		}
		#endregion

		#region CreateOwnedObjects Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of one owned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_Footnote()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_archivedText.ParagraphsOS[0];
			StFootnote footnote = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 0, null);
			Cache.ChangeOwner(para.Hvo, m_currentText.Hvo, (int)StText.StTextTags.kflidParagraphs, 0);
			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 0, new object[] { para, 0 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			para.CreateOwnedObjects(0, 1,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();
			Assert.AreEqual(1, m_currentFootnotesOS.Count);
			VerifyFootnote((StFootnote)m_currentFootnotesOS[0], para, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple owned footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleFootnotesStartAt0()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_archivedText.ParagraphsOS[0];
			StFootnote footnote1 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 0, null);
			footnote1.DisplayFootnoteMarker = true;
			footnote1.DisplayFootnoteReference = false;
			StFootnote footnote2 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 1, null);
			footnote2.DisplayFootnoteMarker = false;
			footnote2.DisplayFootnoteReference = false;
			StFootnote footnote3 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 2, null);
			footnote3.DisplayFootnoteMarker = true;
			footnote3.DisplayFootnoteReference = true;
			Cache.ChangeOwner(para.Hvo, m_currentText.Hvo, (int)StText.StTextTags.kflidParagraphs, 0);
			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 0, new object[] { para, 0 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			para.CreateOwnedObjects(0, 3,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();
			Assert.AreEqual(3, m_currentFootnotesOS.Count);

			IStFootnote testFootnote = m_currentFootnotesOS[0];
			Assert.IsTrue(testFootnote.DisplayFootnoteMarker);
			Assert.IsFalse(testFootnote.DisplayFootnoteReference);
			VerifyFootnote(testFootnote, para, 0);

			testFootnote = m_currentFootnotesOS[1];
			Assert.IsFalse(testFootnote.DisplayFootnoteMarker);
			Assert.IsFalse(testFootnote.DisplayFootnoteReference);
			VerifyFootnote(testFootnote, para, 1);

			testFootnote = m_currentFootnotesOS[2];
			Assert.IsTrue(testFootnote.DisplayFootnoteMarker);
			Assert.IsTrue(testFootnote.DisplayFootnoteReference);
			VerifyFootnote(testFootnote, para, 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple owned footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleFootnotesStartInMiddle()
		{
			CheckDisposed();

			StTxtPara para = m_inMemoryCache.AddParaToMockedText(m_currentText.Hvo, "Normal");
			m_inMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the second section " +
				"of the first chapter of Genesis. This is here so that we have enough characters " +
				"to insert footnotes into it.", null);
			m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 0, null);
			m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 20, null);
			StFootnote footnotePrev = (StFootnote)m_currentFootnotesOS[0];
			StFootnote footnoteAfter = (StFootnote)m_currentFootnotesOS[1];

			para = (StTxtPara)m_currentText.ParagraphsOS[0];
			m_archivedFootnotesOS = m_inMemoryCache.CreateArbitraryFootnoteSequence(m_archivedText);
			StFootnote footnote1 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 4, null);
			footnote1.DisplayFootnoteMarker = true;
			footnote1.DisplayFootnoteReference = false;
			StFootnote footnote2 = m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 7, null);
			footnote2.DisplayFootnoteMarker = false;
			footnote2.DisplayFootnoteReference = true;
			Cache.ChangeOwner(para.Hvo, m_currentText.Hvo, (int)StText.StTextTags.kflidParagraphs, 0);
			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 1, new object[] { para, 0 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			para.CreateOwnedObjects(0, 10,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();
			Assert.AreEqual(4, m_currentFootnotesOS.Count);
			IStFootnote testFootnote = m_currentFootnotesOS[0];
			Assert.AreEqual(footnotePrev.Hvo, testFootnote.Hvo, "Previous footnote shouldn't have moved");

			testFootnote = m_currentFootnotesOS[1];
			VerifyFootnote(testFootnote, para, 4);
			Assert.IsTrue(testFootnote.DisplayFootnoteMarker);
			Assert.IsFalse(testFootnote.DisplayFootnoteReference);

			testFootnote = m_currentFootnotesOS[2];
			VerifyFootnote(testFootnote, para, 7);
			Assert.IsFalse(testFootnote.DisplayFootnoteMarker);
			Assert.IsTrue(testFootnote.DisplayFootnoteReference);

			testFootnote = m_currentFootnotesOS[3];
			Assert.AreEqual(footnoteAfter.Hvo, testFootnote.Hvo,
				"Following footnote should have gotten bumped two places");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of an owned picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_Picture()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];

			ITsString tss = para.Contents.UnderlyingTsString;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			using (DummyFileMaker fileMaker = new DummyFileMaker("junk.jpg", true))
			{
				CmPicture pict = new CmPicture(Cache, fileMaker.Filename,
					factory.MakeString("Test picture", Cache.DefaultVernWs),
					StringUtils.LocalPictures);
				pict.InsertORCAt(tss, 0, para.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0);
				tss = para.Contents.UnderlyingTsString;
				int cchOrigStringLength = tss.Length;

				NMock.DynamicMock mockIObjectMetaInfoProvider =
					new DynamicMock(typeof(IObjectMetaInfoProvider));
				mockIObjectMetaInfoProvider.Strict = true;
				mockIObjectMetaInfoProvider.ExpectAndReturn(1, "PictureFolder", StringUtils.LocalPictures);
				para.CreateOwnedObjects(0, 1,
					(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);
				mockIObjectMetaInfoProvider.Verify();

				tss = para.Contents.UnderlyingTsString;
				Assert.AreEqual(cchOrigStringLength, tss.Length);
				string sObjData = tss.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));

				byte odt = Convert.ToByte(sObjData[0]);
				Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
				Assert.IsTrue(Cache.GetGuidFromId(pict.Hvo) != guid, "New guid was not inserted");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of an ORC at the end of the paragraph (TE-3191)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_AtEnd()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];

			// We use m_archivedText with para from m_currentText to create a footnote.
			// This simulates a "paragraph with footnote" just copied from m_archivedText.
			// The important thing here is that we have a footnote that links to a
			// different owner.
			m_archivedFootnotesOS = m_inMemoryCache.CreateArbitraryFootnoteSequence(m_archivedText);
			m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, para.Contents.Length, null);
			int paraLen = para.Contents.Length;

			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 0, new object[] { para, paraLen - 1 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			para.CreateOwnedObjects(paraLen - 1, paraLen,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();

			Assert.AreEqual(1, m_currentFootnotesOS.Count);

			VerifyFootnote((StFootnote)m_currentFootnotesOS[0], para, paraLen - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple ORCs, one at the end of the paragraph (TE-3191)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleAtEnd()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];

			// We use m_archivedText with para from m_currentText to create a footnote.
			// This simulates a "paragraph with footnote" just copied from m_archivedText.
			// The important thing here is that we have a footnote that links to a
			// different owner.
			m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, 0, null);
			m_inMemoryCache.AddFootnote(m_archivedFootnotesOS, para, para.Contents.Length, null);
			int paraLen = para.Contents.Length;

			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 0, new object[] { para, 0 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			para.CreateOwnedObjects(0, paraLen,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();

			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			VerifyFootnote((StFootnote)m_currentFootnotesOS[0], para, 0);
			VerifyFootnote((StFootnote)m_currentFootnotesOS[1], para, paraLen - 1);
		}
		#endregion

		#region GetFootnoteOwnerAndFlid tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnoteOwnerAndFlid method when called on a paragraph owned by a
		/// ScrSection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnoteOwnerAndFlid()
		{
			CheckDisposed();

			IStTxtPara para = (IStTxtPara)m_currentText.ParagraphsOS[0];
			ICmObject owner;
			int flid;
			Assert.IsTrue(para.GetFootnoteOwnerAndFlid(out owner, out flid));
			Assert.AreEqual(m_currentText.Hvo, owner.Hvo);
			Assert.AreEqual(
				Cache.MetaDataCacheAccessor.GetFieldId("StText", "DummyFootnotesOS", false), flid);
		}
		#endregion

		#region GetFootnotes Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtPara.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_One()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];
			StFootnote footnote = m_inMemoryCache.AddFootnote(m_currentFootnotesOS, para, 0, null);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Anything");
			List<FootnoteInfo> footnotes = para.GetFootnotes();
			Assert.AreEqual(1, footnotes.Count);
			Assert.AreEqual(m_currentFootnotesOS[0].Hvo, ((FootnoteInfo)footnotes[0]).footnote.Hvo);
			Assert.AreEqual("Anything", ((FootnoteInfo)footnotes[0]).paraStylename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtPara.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_None()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];
			List<FootnoteInfo> footnotes = para.GetFootnotes();
			Assert.AreEqual(0, footnotes.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="StTxtPara.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_More()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_currentText.ParagraphsOS[0];
			FdoOwningSequence<IStFootnote> footnotesOS =
				m_inMemoryCache.CreateArbitraryFootnoteSequence(m_currentText);
			StFootnote footnote = m_inMemoryCache.AddFootnote(footnotesOS, para, 0, null);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Anything");
			footnote = m_inMemoryCache.AddFootnote(footnotesOS, para, 5, null);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Bla");
			footnote = m_inMemoryCache.AddFootnote(footnotesOS, para, 10, null);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "hing");
			List<FootnoteInfo> footnotes = para.GetFootnotes();
			Assert.AreEqual(3, footnotes.Count);
			Assert.AreEqual(m_currentFootnotesOS[0].Hvo, ((FootnoteInfo)footnotes[0]).footnote.Hvo);
			Assert.AreEqual("Anything", ((FootnoteInfo)footnotes[0]).paraStylename);
			Assert.AreEqual(m_currentFootnotesOS[1].Hvo, ((FootnoteInfo)footnotes[1]).footnote.Hvo);
			Assert.AreEqual("Bla", ((FootnoteInfo)footnotes[1]).paraStylename);
			Assert.AreEqual(m_currentFootnotesOS[2].Hvo, ((FootnoteInfo)footnotes[2]).footnote.Hvo);
			Assert.AreEqual("hing", ((FootnoteInfo)footnotes[2]).paraStylename);
		}
		#endregion

		#region Helper methods to verify results
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the cache.CopyObject method, copying a paragraph which has footnotes and back
		/// translation.  Results should be identical to using other copy methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyObject()
		{
			StTxtPara paraRev = SetUpParagraphInArchiveWithFootnotesAndBT();

			// Now, call the method under test!
			int hvoPara = Cache.CopyObject(paraRev.Hvo,
				m_currentText.Hvo, (int)StText.StTextTags.kflidParagraphs);
			StTxtPara newPara = new StTxtPara(Cache, hvoPara);

			VerifyCopiedPara(newPara);
			VerifyParagraphsAreDifferentObjects(paraRev, newPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method:
		/// Verify the given copied paragraph, including footnotes and back translation,
		/// plus any other fields deemed necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyCopiedPara(StTxtPara newPara)
		{
			// Verify the para StyleRules
			Assert.AreEqual("Line 1", newPara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the para Contents
			Assert.AreEqual("11This" + StringUtils.kchObject + " is" + StringUtils.kchObject + " the previous version of the text.",
				newPara.Contents.Text);
			ITsString tssNewParaContents = newPara.Contents.UnderlyingTsString;
			Assert.AreEqual(7, tssNewParaContents.RunCount);
			AssertEx.RunIsCorrect(tssNewParaContents, 0, "1", "CharacterStyle1", Cache.DefaultVernWs, true);
			AssertEx.RunIsCorrect(tssNewParaContents, 1, "1", "CharacterStyle2", Cache.DefaultVernWs, true);
			AssertEx.RunIsCorrect(tssNewParaContents, 2, "This", null, Cache.DefaultVernWs, true);
			// Run #3 is ORC for footnote, checked below...
			AssertEx.RunIsCorrect(tssNewParaContents, 4, " is", null, Cache.DefaultVernWs, true);
			// Run #5 is ORC for footnote, checked below...
			AssertEx.RunIsCorrect(tssNewParaContents, 6, " the previous version of the text.", null, Cache.DefaultVernWs, true);

			// note: At this point, having done the Copyxx() but not CreateOwnedObjects(),
			//  the ORCs still refer to footnote objects owned by m_archivedText...
			StFootnote footnote1 = (StFootnote)m_archivedFootnotesOS.FirstItem;
			VerifyFootnote(footnote1, newPara, 6);
			Assert.AreEqual("Footnote1", ((StTxtPara)footnote1.ParagraphsOS[0]).Contents.Text);
			StFootnote footnote2 = (StFootnote)m_archivedFootnotesOS[1];
			VerifyFootnote(footnote2, newPara, 10);
			Assert.AreEqual("Footnote2", ((StTxtPara)footnote2.ParagraphsOS[0]).Contents.Text);
			// ...thus the footnotes are not yet in m_currentFootnotesOS.
			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			// Verify the para translations
			Assert.AreEqual(1, newPara.TranslationsOC.Count); //only 1 translation, the BT
			ICmTranslation paraTrans = newPara.GetBT();
			// verify each alternate translation
			int[] wsBt = new int[] { InMemoryFdoCache.s_wsHvos.En, InMemoryFdoCache.s_wsHvos.De };
			foreach (int ws in wsBt)
			{
				ITsString tssBtParaContents = paraTrans.Translation.GetAlternativeTss(ws);
				Assert.AreEqual("BT" + StringUtils.kchObject + " of" + StringUtils.kchObject +
					" test paragraph" + ws.ToString(), tssBtParaContents.Text);
				Assert.AreEqual(5, tssBtParaContents.RunCount);
				// could check every run too, but we'll skip that
				Assert.AreEqual(BackTranslationStatus.Finished.ToString(),
					paraTrans.Status.GetAlternative(ws));
			}

			// Verify the footnote translations, their ORCs, and their status
			foreach (int ws in wsBt)
			{
				VerifyBtFootnote(footnote1, newPara, ws, 2);
				ICmTranslation footnoteTrans = ((StTxtPara)footnote1.ParagraphsOS[0]).GetBT();
				Assert.AreEqual("BT of footnote1 " + ws.ToString(),
					footnoteTrans.Translation.GetAlternativeTss(ws).Text);
				Assert.AreEqual(BackTranslationStatus.Checked.ToString(),
					footnoteTrans.Status.GetAlternative(ws));

				VerifyBtFootnote(footnote2, newPara, ws, 6);
				footnoteTrans = ((StTxtPara)footnote2.ParagraphsOS[0]).GetBT();
				Assert.AreEqual("BT of footnote2 " + ws.ToString(),
					footnoteTrans.Translation.GetAlternativeTss(ws).Text);
				Assert.AreEqual(BackTranslationStatus.Finished.ToString(),
					footnoteTrans.Status.GetAlternative(ws));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method  verifies that the copy was sufficiently "deep":
		/// copied paragraphs are different objects, and that owner and owned objects
		/// are different.
		/// </summary>
		/// <param name="srcPara">The para rev.</param>
		/// <param name="newPara">The new para.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyParagraphsAreDifferentObjects(StTxtPara srcPara, StTxtPara newPara)
		{
			Assert.AreNotEqual(srcPara.Hvo, newPara.Hvo);
			// owned by different StTexts
			Assert.AreNotEqual(srcPara.OwnerHVO, newPara.OwnerHVO);
			// owning different back translations
			Assert.AreNotEqual(srcPara.GetBT().Hvo, newPara.GetBT().Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the marker for a footnote exists in the back translation and refers to the
		/// footnote properly.
		/// </summary>
		/// <param name="footnote">given footnote whose marker we want to verify in the BT</param>
		/// <param name="para">vernacular paragraph which owns the back translation</param>
		/// <param name="ws">writing system of the back transltion</param>
		/// <param name="ich">Character position where ORC should be in the specified back
		/// translation</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyBtFootnote(IStFootnote footnote, IStTxtPara para, int ws, int ich)
		{
			Guid guid = footnote.Cache.GetGuidFromId(footnote.Hvo);
			ITsString tss = para.Contents.UnderlyingTsString;
			ICmTranslation trans = para.GetOrCreateBT();
			ITsString btTss = trans.Translation.GetAlternative(ws).UnderlyingTsString;

			int iRun = btTss.get_RunAt(ich);
			string sOrc = btTss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kchObject, sOrc[0]);
			ITsTextProps orcPropsParaFootnote = btTss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.IsNotNull(objData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newFootnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			Assert.AreEqual(guid, newFootnoteGuid);
			Assert.AreEqual(footnote.Hvo, footnote.Cache.GetIdFromGuid(newFootnoteGuid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure footnote exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="footnote"></param>
		/// <param name="para"></param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		{
			Guid guid = footnote.Cache.GetGuidFromId(footnote.Hvo);
			ITsString tss = para.Contents.UnderlyingTsString;
			int iRun = tss.get_RunAt(ich);
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newFootnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			Assert.AreEqual(guid, newFootnoteGuid);
			Assert.AreEqual(footnote.Hvo, footnote.Cache.GetIdFromGuid(newFootnoteGuid));
			string sOrc = tss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kchObject, sOrc[0]);
		}
		#endregion
	}
}
