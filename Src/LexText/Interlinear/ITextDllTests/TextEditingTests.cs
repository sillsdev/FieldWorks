using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.XWorks;

namespace SIL.FieldWorks.IText
{

	public class TextEditingTests : InterlinearTestBase
	{
		protected FDO.IText m_text1 = null;
		protected MockRawTextEditor m_rtp = null;

		#region Setup And TearDown

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			InstallVirtuals(Path.Combine(FwUtils.ksFlexAppName, Path.Combine("Configuration", "Main.xml")),
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_rtp != null)
					m_rtp.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rtp = null;
			m_text1 = null;

			base.Dispose(disposing);
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			if (m_rtp != null && !m_rtp.IsDisposed)
				m_rtp.Dispose();

			// UndoEverything before we clear our wordform table, so we can make sure
			// the real wordform list is what we want to start with the next time.
			base.Exit();
		}

		protected enum Text1ParaIndex { SimpleSegmentPara, ExtraPara /*, ComplexSegments */};
		/// <summary>
		/// The actual paragraphs are built from ParagraphParserTestTexts.xml. ParagraphContents is simply for auditing.
		/// </summary>
		protected string[] ParagraphContents = new string[] {
					// Paragraph 0 - simple segments
					"xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.",
					// Paragraph 1 - Extra segments
					"xxxsup xxxlayalo xxxranihimbili.",
					// Paragraph 2 - complex segments
					//"xxxMr. xxxZook xxxwants xxxa xxxvacation.  xxxCome  xxxHere xxxI xxxam.",
					};

		#endregion Setup and Teardown

		internal protected class MockRawTextEditor : RawTextPane
		{
			List<XmlNode> m_expectedTextAnnotationsDefnsAfterEdits = new List<XmlNode>();
			List<XmlNode> m_expectedTextAnnotationsDefnsAfterUndo = new List<XmlNode>();
			List<XmlNode> m_expectedTextAnnotationsDefnsAfterRedo = new List<XmlNode>();
			List<LinkedObjectInfo> m_allLinkedObjectsBeforeEdit = new List<LinkedObjectInfo>();

			TextBuilder m_tb;

			internal MockRawTextEditor(FdoCache cache, TextBuilder tb)
			{
				this.Cache = cache;
				m_tb = tb;
			}


			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					EditingHelper.ClearTsStringClipboard();
				}
				base.Dispose(disposing);
			}

			private class StateTransitionHelper : IDisposable
			{
#pragma warning disable 414
				List<XmlNode> m_statesBeforeEdit;
#pragma warning restore 414
				List<XmlNode> m_expectedStatesAfterEdits;

				MockRawTextEditor m_rtp;
				internal StateTransitionHelper(MockRawTextEditor rtp, List<XmlNode> statesBeforeEdit, List<XmlNode> expectedStatesAfterEdits)
				{
					m_rtp = rtp;
					m_statesBeforeEdit = statesBeforeEdit;
					m_expectedStatesAfterEdits = expectedStatesAfterEdits;
					XmlNode textSpecBeforeEdit = rtp.m_tb.SelectedNode;
					if (rtp.m_tb.ActualStText != null)
					{
						rtp.m_allLinkedObjectsBeforeEdit = rtp.m_tb.ActualStText.LinkedObjects;
					}
					if (textSpecBeforeEdit != null)
						statesBeforeEdit.Insert(0, (ParagraphBuilder.Snapshot(textSpecBeforeEdit)));
				}

				#region IDisposable Members

				public void Dispose()
				{
					// perform the transition
					if (m_expectedStatesAfterEdits.Count > 0)
					{
						m_rtp.m_tb.SelectedNode = m_expectedStatesAfterEdits[0];
						m_expectedStatesAfterEdits.RemoveAt(0);
					}
					GC.SuppressFinalize(this);
				}

				#endregion
			}

			public override void SetRoot(int hvo)
			{
				base.SetRoot(hvo);
				m_tb.HvoActualStText = hvo;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="textDefn"></param>
			/// <returns>a snapshot of the expected state</returns>
			internal XmlNode AddResultingAnnotationState(XmlNode textDefn)
			{
				XmlNode snapshot = ParagraphBuilder.Snapshot(textDefn);
				m_expectedTextAnnotationsDefnsAfterEdits.Add(snapshot);
				return snapshot;
			}

			internal void DeleteText()
			{
				using (new StateTransitionHelper(this, m_expectedTextAnnotationsDefnsAfterUndo, m_expectedTextAnnotationsDefnsAfterEdits))
				{
					m_tb.DeleteText();
				}
			}

			internal void ValidateStTextAnnotations()
			{
				ValidateStTextAnnotations(new List<int>());
			}

			internal void ValidateStTextAnnotations(int hvoParaModified)
			{
				ValidateStTextAnnotations(new List<int>(new int[] { hvoParaModified }));
			}

			/// <summary>
			/// validate all the annotations for the current StText, starting with
			/// the paragraphs that we know were modified.
			/// </summary>
			/// <param name="hvoParasModified"></param>
			internal void ValidateStTextAnnotations(List<int> hvoParasModified)
			{
				// validate the annotations for the modified paragraphs
				foreach (int hvoParaModified in hvoParasModified)
				{
					ValidateParagraphAnnotations(hvoParaModified);
				}

				if (m_tb.ActualStText != null)
				{
					// validate the annotations for remaining paragraphs
					foreach (IStPara para in m_tb.ActualStText.ParagraphsOS)
					{
						if (hvoParasModified.Contains(para.Hvo))
							continue;	// we've already validated this one.
						ValidateParagraphAnnotations(para.Hvo);
					}
				}
				// now validate that the annotations that we expected to be deleted/removed
				// are invalid.
				ValidateInvalidAnnotations();
			}


			/// <summary>
			/// Pastes the given string after copying it to the clipboard
			/// </summary>
			/// <param name="sIns"></param>
			/// <param name="tsiBeforeIns"></param>
			internal void OnInsert(string sIns, out TextSelInfo tsiBeforeIns)
			{
				IVwSelection vwsel = RootBox.Selection;
				Assert.IsTrue(vwsel != null);
				Commit(vwsel);
				ITsTextProps[] vttp;
				IVwPropertyStore[] vvps;
				int cttp;
				SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
				ITsTextProps ttpSel = vttp[0];
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsString tss = tsf.MakeStringWithPropsRgch(sIns, sIns.Length, ttpSel);
				using (new StateTransitionHelper(this, m_expectedTextAnnotationsDefnsAfterUndo, m_expectedTextAnnotationsDefnsAfterEdits))
				{
					tsiBeforeIns = this.CurrentSelectionInfo;
					EditingHelper.PasteCore(tss);
				}
			}

			internal void OnInsert(Keys key, out TextSelInfo tsiBeforeIns)
			{
				// modify the paragraph contents.
				OnKeyDownAndPress(key, out tsiBeforeIns);
			}

			internal void OnDelete(out TextSelInfo tsiBeforeDel)
			{
				// then modify the paragraph contents.
				OnKeyDownAndPress(Keys.Delete, out tsiBeforeDel);
				// adjust the expected result annotations for this paragraph.
			}

			internal void OnCut(out TextSelInfo tsiBeforeDel)
			{
				// then modify the paragraph contents.
				OnKeyDownAndPress(Keys.X | Keys.Control, out tsiBeforeDel);
				// adjust the expected result annotations for this paragraph.
			}

			internal void OnEnter(out TextSelInfo tsiBeforeEnter)
			{
				OnKeyDownAndPress(Keys.Enter, out tsiBeforeEnter);
			}

			internal void OnBackspace(out TextSelInfo tsiBeforeDel)
			{
				// then modify the paragraph contents.
				OnKeyDownAndPress(Keys.Back, out tsiBeforeDel);
				// adjust the expected result annotations for this paragraph.
			}

			internal void OnUndo()
			{
				TextSelInfo tsiBeforeUndo;
				OnUndo(out tsiBeforeUndo);
			}

			///// <summary>
			///// this version of Undo issues a prop change that is currently being skipped during undo (LTxxxx).
			///// </summary>
			//internal void OnUndo(StTxtPara paraToResync, int ivMin, int cvins, int cvdel)
			//{
			//    OnUndo();
			//    PropChangedForReplacingParaContents(paraToResync, ivMin, cvins, cvdel);
			//}

			internal void ResyncAllParaContents()
			{
				if (m_tb.ActualStText == null)
					return;
				foreach (IStPara para in m_tb.ActualStText.ParagraphsOS)
				{
					PropChangedForReplacingParaContents(para as StTxtPara, 0, 0, 0);
				}
			}

			private void PropChangedForReplacingParaContents(StTxtPara paraToResync, int ivMin, int cvins, int cvdel)
			{
				RootBox.PropChanged(paraToResync.Hvo,
									(int)StTxtPara.StTxtParaTags.kflidContents,
									ivMin,
									cvins,
									cvdel);
			}

			internal void OnUndo(out TextSelInfo tsiBeforeUndo)
			{
				UndoResult ures;
				OnUndo(out tsiBeforeUndo, out ures);
			}

			internal void OnUndo(out UndoResult ures)
			{
				TextSelInfo tsiBeforeUndo;
				OnUndo(out tsiBeforeUndo, out ures);
			}

			internal void OnUndo(out TextSelInfo tsiBeforeUndo, out UndoResult ures)
			{
				using (new StateTransitionHelper(this, m_expectedTextAnnotationsDefnsAfterRedo, m_expectedTextAnnotationsDefnsAfterUndo))
				{
					tsiBeforeUndo = this.CurrentSelectionInfo;
					Cache.Undo(out ures);
					if (ures == UndoResult.kuresRefresh)
						ResyncAllParaContents();
				}
			}

			internal void OnRedo()
			{
				TextSelInfo tsiBeforeRedo;
				OnRedo(out tsiBeforeRedo);
			}

			///// <summary>
			///// this version of Redo issues a prop change that is currently being skipped during undo because
			///// a refresh is pending. (LTxxxx)
			///// </summary>
			//internal void OnRedo(StTxtPara paraToResync, int ivMin, int cvins, int cvdel)
			//{
			//    OnRedo();
			//    PropChangedForReplacingParaContents(paraToResync, ivMin, cvins, cvdel);
			//    // we also need to restore the cursor to ivMin
			//    SetCursor(paraToResync, ivMin);
			//}

			internal void OnRedo(out TextSelInfo tsiBeforeRedo)
			{
				UndoResult ures;
				OnRedo(out tsiBeforeRedo, out ures);
			}

			internal void OnRedo(out TextSelInfo tsiBeforeRedo, out UndoResult ures)
			{
				using (new StateTransitionHelper(this, m_expectedTextAnnotationsDefnsAfterUndo, m_expectedTextAnnotationsDefnsAfterRedo))
				{
					tsiBeforeRedo = this.CurrentSelectionInfo;
					Cache.Redo(out ures);
					if (ures == UndoResult.kuresRefresh)
						ResyncAllParaContents();
				}
			}

			internal TextSelInfo CurrentSelectionInfo
			{
				get { return new TextSelInfo(RootBox.Selection); }
			}

			internal ParagraphBuilder GetParagraphBuilder(TextSelInfo tsi)
			{
				return GetParagraphBuilder(tsi.HvoAnchor);
			}

			internal ParagraphBuilder GetParagraphBuilder(int hvoPara)
			{
				if (m_tb != null)
					return m_tb.GetParagraphBuilder(hvoPara);
				return null;
			}



			internal void SetCursor(StTxtPara para, int ichMin)
			{
				this.MakeTextSelectionAndScrollToView(ichMin, ichMin, 0, para.IndexInOwner);
			}

			internal void SetSelection(StTxtPara para, int ichMin, int ichLim)
			{
				this.MakeTextSelectionAndScrollToView(ichMin, ichLim, 0, para.IndexInOwner);
			}

			internal void SetSelection(StTxtPara paraBegin, int ichMin, StTxtPara paraEnd, int ichLim)
			{
				this.MakeTextSelectionAndScrollToView(ichMin, ichLim, 0, paraBegin.IndexInOwner, paraEnd.IndexInOwner);
			}

			private void OnKeyDownAndPress(Keys key, out TextSelInfo tsiBeforeEdit)
			{
				using (new StateTransitionHelper(this, m_expectedTextAnnotationsDefnsAfterUndo, m_expectedTextAnnotationsDefnsAfterEdits))
				{
					tsiBeforeEdit = this.CurrentSelectionInfo;
					HandleKeyDownAndKeyPress(key);
				}
			}

			internal TextBuilder TextBuilder
			{
				get { return m_tb; }
			}

			internal void ValidateInsertedState(string ins, TextSelInfo tsiInitial)
			{
				int ichInsertionPointInitial = Math.Min(tsiInitial.IchAnchor, tsiInitial.IchEnd);
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				Assert.AreEqual(tsiInitial.AnchorText.Length + ins.Length, tsiAfterEdit.AnchorText.Length);
				// check our cursor has advanced
				Assert.AreEqual(ichInsertionPointInitial + ins.Length, tsiAfterEdit.IchAnchor);
				Assert.AreEqual(ins, tsiAfterEdit.AnchorText.Substring(ichInsertionPointInitial, ins.Length));

				// now validate against expected annotations.
				ValidateStTextAnnotations(tsiInitial.HvoAnchor);
			}

			/// <summary>
			/// Validate invalid annotations in the text.
			/// What do we do about
			/// </summary>
			private void ValidateInvalidAnnotations()
			{
				// make sure that the paragraph annotations existing only in the previous state
				// are now invalid or orphaned.
				List<LinkedObjectInfo> currentLinkedObjects = new List<LinkedObjectInfo>();
				if (m_tb.ActualStText != null)
					currentLinkedObjects = m_tb.ActualStText.LinkedObjects;
				foreach (LinkedObjectInfo prevLinkedObjInfo in m_allLinkedObjectsBeforeEdit)
				{
					// look through currentLinkedObjects, if we don't find a match
					// then we expect this to point to something that is invalid or an orphan
					LinkedObjectInfo matchingLinkedObjInfo = null;
					foreach (LinkedObjectInfo currentLinkedObjInfo in currentLinkedObjects)
					{
						if (ReflectionHelper.HaveSamePropertyValues(prevLinkedObjInfo, currentLinkedObjInfo))
						{
							matchingLinkedObjInfo = currentLinkedObjInfo;
							break;
						}
					}
					if (matchingLinkedObjInfo != null)
					{
						// remove the matching obj from our list, since we don't need to compare against it again.
						currentLinkedObjects.Remove(matchingLinkedObjInfo);
						continue;
					}
					// we found linkedObject information that is not found in our current state.
					// make sure its basic object information has been invalidated.


				}
			}

			private void ValidateParagraphAnnotations(int hvoPara)
			{
				ParagraphBuilder pb = GetParagraphBuilder(hvoPara);
				if (pb != null && pb.ParagraphDefinition != null)
				{
					// make sure our real annotation offsets match the actual text.
					pb.ReconstructSegmentsAndWordforms();
					ConceptualModelXmlParagraphValidator cmpv = new ConceptualModelXmlParagraphValidator(pb);
					cmpv.ValidateActualParagraphAgainstDefn();
				}
				else
				{
					// we aren't expecting any annotations for this paragraph. Make sure we don't
					// have anything referencing the paragraph.
					StTxtPara para = StTxtPara.CreateFromDBObject(Cache, hvoPara) as StTxtPara;
					Assert.AreEqual(0, para.LinkedObjects.Count);
				}
			}

			internal enum EditAction { Backspace, Delete, Undo, Redo, Insert, Replace };

			internal void ValidateBackspaceState(string del, TextSelInfo tsiInitial)
			{
				ValidateDeletedState(del, tsiInitial, EditAction.Backspace);
			}

			internal void ValidateDeletedState(string del, TextSelInfo tsiInitial)
			{
				ValidateDeletedState(del, tsiInitial, EditAction.Delete);
			}

			internal void ValidateDeletedState(string del, TextSelInfo tsiInitial, EditAction action)
			{
				int ichInsertionPointInitial = Math.Min(tsiInitial.IchAnchor, tsiInitial.IchEnd);
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				Assert.AreEqual(tsiInitial.AnchorLength - del.Length, tsiAfterEdit.AnchorLength);
				if (action == EditAction.Backspace)
				{
					// check our cursor has retreated
					Assert.AreEqual(ichInsertionPointInitial - del.Length, tsiAfterEdit.IchAnchor);
				}
				else if (action == EditAction.Delete)
				{
					// check our cursor is in the same location
					Assert.AreEqual(ichInsertionPointInitial, tsiAfterEdit.IchAnchor, "Expected same cursor position on a delete.");
				}

				// now validate against expected annotations.
				ValidateStTextAnnotations(tsiInitial.HvoAnchor);
			}

			internal void ValidateDeletedState(int[] rghvoDelParas)
			{
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				foreach (int hvoDelPara in rghvoDelParas)
				{
					Assert.IsFalse(Cache.IsValidObject(hvoDelPara),
						String.Format("Expected paragraph {0} to be deleted.", hvoDelPara));
				}

				// now validate against expected annotations.
				ValidateStTextAnnotations(tsiAfterEdit.HvoAnchor);
			}


			internal void ValidateReplacedState(string del, string ins, TextSelInfo tsiInitial)
			{
				int ichInsertionPointInitial = Math.Min(tsiInitial.IchAnchor, tsiInitial.IchEnd);
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				// calculate and validate the expected length of the edited text
				Assert.AreEqual(tsiInitial.AnchorLength - del.Length + ins.Length, tsiAfterEdit.AnchorLength);
				// validate our expected cursor is at the end of the pasted text.
				Assert.AreEqual(ichInsertionPointInitial + ins.Length, tsiAfterEdit.IchAnchor);
				string actualIns = tsiAfterEdit.AnchorLength > 0 ?
					tsiAfterEdit.AnchorText.Substring(ichInsertionPointInitial, ins.Length) : "";
				Assert.AreEqual(ins, actualIns);
				// now validate against expected annotations.
				ValidateStTextAnnotations(tsiInitial.HvoAnchor);
			}

			internal void ValidateUndoRedo(UndoResult uresExpected, UndoResult uresActual,
				TextSelInfo tsiExpectedAfterUndoRedo)
			{
				Assert.AreEqual(uresExpected, uresActual, "UndoResult");
				ValidateUndoRedo(tsiExpectedAfterUndoRedo);
			}

			internal void ValidateUndoRedo(TextSelInfo tsiExpectedAfterUndoRedo)
			{
				TextSelInfo tsiAfterEdit = ValidateUndoRedoRaw(tsiExpectedAfterUndoRedo);
				ValidateStTextAnnotations(tsiAfterEdit.HvoAnchor);
			}

			internal TextSelInfo ValidateUndoRedoRaw(TextSelInfo tsiExpectedAfterUndoRedo)
			{
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				Assert.AreEqual(tsiExpectedAfterUndoRedo.AnchorLength, tsiAfterEdit.AnchorLength);
				Assert.AreEqual(tsiExpectedAfterUndoRedo.IchAnchor, tsiAfterEdit.IchAnchor);
				Assert.AreEqual(tsiExpectedAfterUndoRedo.IchEnd, tsiAfterEdit.IchEnd);
				Assert.AreEqual(tsiExpectedAfterUndoRedo.AnchorText, tsiAfterEdit.AnchorText);
				Assert.AreEqual(tsiExpectedAfterUndoRedo.HvoAnchor, tsiAfterEdit.HvoAnchor);
				return tsiAfterEdit;
			}

			internal void ValidateParagraphContents(TextSelInfo tsi, string expectedContents)
			{
				Assert.AreEqual(expectedContents, tsi.AnchorLength > 0 ? tsi.AnchorText : "");
				ValidateParagraphAnnotations(tsi.HvoAnchor);
			}

			internal void ValidateParagraphContentsRaw(TextSelInfo tsi, string expectedContents)
			{
				Assert.AreEqual(expectedContents, tsi.AnchorLength > 0 ? tsi.AnchorText : "");
			}

			internal void ValidateParagraphContents(StTxtPara para, string expectedContents)
			{
				Assert.AreEqual(expectedContents, para.Contents.Length > 0 ? para.Contents.Text : "");
				ValidateParagraphAnnotations(para.Hvo);
			}

			internal void ValidateParagraphContentsRaw(StTxtPara para, string expectedContents)
			{
				Assert.AreEqual(expectedContents, para.Contents.Length > 0 ? para.Contents.Text : "");
			}

			internal void ValidateIP(StTxtPara paraExpected, int ipOffset)
			{
				TextSelInfo tsiAfterEdit = this.CurrentSelectionInfo;
				Assert.AreEqual(paraExpected.Hvo, tsiAfterEdit.HvoAnchor);
				Assert.AreEqual(ipOffset, tsiAfterEdit.IchAnchor);
				Assert.AreEqual(ipOffset, tsiAfterEdit.IchEnd);
			}
		}

		#region Tests


		#region EditingParagraphTests
		// Since there are no annotations to preserve, no need to adjust their offsets with the edits.
		// --- Edits that affect annotation Offsets
		// --- Edits that affect segments
		// --- Edits that affect occurrences

		// --- Edits that affect segments and occurrences
		// Delete entire text
		// Replace entire text
		// TODO: What do we do about scripture edited in TE?

		// Enhance: paragraph boundaries (merging and dividing paragraphs)
		// Enhance: copy, cut, and pasting.

		/// <summary>
		/// Changing a space at the end of a paragraph shouldn't affect any segment offsets
		/// </summary>

		#region EditsThatAffectOffsetsForSegmentsAndSegmentForms
		//
		// These next tests test various whitespace edits that only affect offsets for segments, but not substantially in length or content.
		//

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira. "
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtEndOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetCursor(para0, para0.Contents.Length);
			// save selection info.
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.Space, out tsiBeforeIns);
			// check our text has increased
			m_rtp.ValidateInsertedState(" ", tsiBeforeIns);
			string sAfterInsert = "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira. ";
			m_rtp.ValidateParagraphContents(para0, sAfterInsert);

			// Delete the space from the end of paragraph
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			m_rtp.ValidateBackspaceState(" ", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira ."
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceBeforePeriodAtEndOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetCursor(para0, para0.Contents.Text.LastIndexOf("."));
			// save selection info.
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.Space, out tsiBeforeIns);
			// check our text has increased
			m_rtp.ValidateInsertedState(" ", tsiBeforeIns);
			string sAfterInsert = "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira .";
			m_rtp.ValidateParagraphContents(para0, sAfterInsert);

			// Delete the space from the end of paragraph
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			m_rtp.ValidateBackspaceState(" ", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///            1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira . xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtEndOfFirstSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Insert cursor at end of first sentence and insert a space
			m_rtp.SetCursor(para0, para0.Contents.Text.IndexOf("."));
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.Space, out tsiBeforeIns);
			m_rtp.ValidateInsertedState(" ", tsiBeforeIns);
			string sAfterInsert = "xxxpus xxxyalola xxxnihimbilira . xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterInsert);

			// Delete space from end of first sentence
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			m_rtp.ValidateBackspaceState(" ", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///   xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtBeginningOfParagraph()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Insert cursor at beginning of paragraph and insert a space
			m_rtp.SetCursor(para0, 0);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.Space, out tsiBeforeIns);
			m_rtp.ValidateInsertedState(" ", tsiBeforeIns);
			string sAfterInsert = " xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterInsert);

			// Delete space from beginning of paragraph
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;

			m_rtp.ValidateBackspaceState(" ", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		#endregion

		#region EditsThatAffectSegmentNumber
		//
		// These tests test edits that affect Segments in number and content (but not occurrences).
		//

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus. xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSegmentBreakAfter1stWordform()
		{
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Insert cursor after first Wordform and insert a period (FT should be on first segment)
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			TextSelInfo tsiBeforeIns;
			InsertSegmentBreakAfter1stWord(para0, out tsiBeforeIns);

			// Delete new period (merge FT back)
			TextSelInfo tsiBeforeBackspace;
			TextSelInfo tsiAfterBackspace;
			DeleteSegmentBreakAfter1stWord(para0, out tsiBeforeBackspace, out tsiAfterBackspace);

			// Undo
			UndoDeleteSegmentBreakAfter1stWord(tsiBeforeBackspace);
			UndoInsertSegmentBreakAfter1stWord(tsiBeforeIns);

			// Redo
			RedoInsertSegmentBreakAfter1stWord(tsiBeforeBackspace);
			RedoDeleteSegmentBreakAfter1stWord(tsiAfterBackspace);
		}

		virtual protected void InsertSegmentBreakAfter1stWord(StTxtPara para0, out TextSelInfo tsiBeforeIns)
		{
			m_rtp.SetCursor(para0, "xxxpus".Length);
			m_rtp.OnInsert(".", out tsiBeforeIns);
			m_rtp.ValidateInsertedState(".", tsiBeforeIns);
			string sAfterInsert = "xxxpus. xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterInsert);
		}

		virtual protected void DeleteSegmentBreakAfter1stWord(StTxtPara para0, out TextSelInfo tsiBeforeBackspace, out TextSelInfo tsiAfterBackspace)
		{
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			tsiAfterBackspace = m_rtp.CurrentSelectionInfo;

			m_rtp.ValidateBackspaceState(".", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);
		}

		virtual protected void UndoDeleteSegmentBreakAfter1stWord(TextSelInfo tsiBeforeBackspace)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
		}

		virtual protected void UndoInsertSegmentBreakAfter1stWord(TextSelInfo tsiBeforeIns)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);
		}

		virtual protected void RedoInsertSegmentBreakAfter1stWord(TextSelInfo tsiBeforeBackspace)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
		}

		virtual protected void RedoDeleteSegmentBreakAfter1stWord(TextSelInfo tsiAfterBackspace)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void DeleteSegmentBreakToMerge2Sentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Delete another period (merge two FTs)
			m_rtp.SetCursor(para0, para0.Contents.Text.IndexOf("."));
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(".", tsiBeforeDelete);
			string sAfterDelete = "xxxpus xxxyalola xxxnihimbilira xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterDelete);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira? xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira, xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceSegBreakWithAnotherSegBreakThenWithNonSegBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Replacing segment break (Paste new segment break)
			TextSelInfo tsiBeforeIns1;
			TextSelInfo tsiAfterIns1;
			ReplaceSegBreakWithAnotherSegBreak(para0, out tsiBeforeIns1, out tsiAfterIns1);

			// Replacing segment break (Paste non-segment break)
			TextSelInfo tsiBeforeIns2;
			TextSelInfo tsiAfterIns2;
			ReplaceSegmentBreakWithNonSegmentBreak(para0, out tsiBeforeIns2, out tsiAfterIns2);

			// Undo
			UndoReplaceSegmentBreakwithNonSegmentBreak(tsiBeforeIns2);
			UndoReplaceSegBreakWithAnotherSegBreak(tsiBeforeIns1);

			// Redo
			RedoReplaceSegBreakWithAnotherSegBreak(tsiAfterIns1);
			RedoReplaceSegmentBreakWithNonSegmentBreak(tsiAfterIns2);
		}

		protected virtual void RedoReplaceSegmentBreakWithNonSegmentBreak(TextSelInfo tsiAfterIns2)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns2);
		}

		protected virtual void RedoReplaceSegBreakWithAnotherSegBreak(TextSelInfo tsiAfterIns1)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns1);
		}

		protected virtual void UndoReplaceSegBreakWithAnotherSegBreak(TextSelInfo tsiBeforeIns1)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns1);
		}

		protected virtual void UndoReplaceSegmentBreakwithNonSegmentBreak(TextSelInfo tsiBeforeIns2)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns2);
		}

		protected virtual void ReplaceSegBreakWithAnotherSegBreak(StTxtPara para0, out TextSelInfo tsiBeforeIns1, out TextSelInfo tsiAfterIns1)
		{
			int ichMin = para0.Contents.Text.IndexOf(".");
			m_rtp.SetSelection(para0, ichMin, ichMin + 1);

			m_rtp.OnInsert("?", out tsiBeforeIns1);
			tsiAfterIns1 = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState(".", "?", tsiBeforeIns1);
			string sAfterReplace1 = "xxxpus xxxyalola xxxnihimbilira? xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterReplace1);
		}

		protected virtual void ReplaceSegmentBreakWithNonSegmentBreak(StTxtPara para0, out TextSelInfo tsiBeforeIns2, out TextSelInfo tsiAfterIns2)
		{
			int ichMin = para0.Contents.Text.IndexOf("?");
			m_rtp.SetSelection(para0, ichMin, ichMin + 1);

			m_rtp.OnInsert(",", out tsiBeforeIns2);
			tsiAfterIns2 = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("?", ",", tsiBeforeIns2);
			string sAfterReplace2 = "xxxpus xxxyalola xxxnihimbilira, xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterReplace2);
		}

		#endregion EditsThatAffectSegmentNumber

		#region EditsThatAffectWordforms
		//
		// These next tests test edits that substantially affect Wordforms in length and number (but not segments)
		//

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxApus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteWordFormCharInside1stWordform()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Insert cursor inside first wordform and insert a wordforming character
			m_rtp.SetCursor(para0, 3);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.A, out tsiBeforeIns);
			m_rtp.ValidateInsertedState("A", tsiBeforeIns);
			m_rtp.ValidateParagraphContents(para0,
				"xxxApus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");

			// Delete new wordforming character
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateBackspaceState("A", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxx pus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceInside1stWordform()
		{
			// This tests wordform split and merge
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Insert cursor inside first wordform and insert a space
			m_rtp.SetCursor(para0, 3);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert(Keys.Space, out tsiBeforeIns);
			m_rtp.ValidateInsertedState(" ", tsiBeforeIns);
			m_rtp.ValidateParagraphContents(para0,
				"xxx pus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");

			// Delete space (wordform merge)
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateBackspaceState(" ", tsiBeforeBackspace);
			m_rtp.ValidateParagraphContents(para0, ParagraphContents[0]);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertNewWordIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetCursor(para0, 0);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxNewWord ", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateInsertedState("xxxNewWord ", tsiBeforeIns);
			string sAfterIns = "xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxsup xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceWordWithSameSizeWordform()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, "xxxpus".Length, 0);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxsup", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus", "xxxsup", tsiBeforeIns);
			string sAfterIns = "xxxsup xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceWordWithSameWordform()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, "xxxpus".Length);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxpus", out tsiBeforeIns);
#pragma warning disable 219
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
#pragma warning restore 219
			m_rtp.ValidateReplacedState("xxxpus", "xxxpus", tsiBeforeIns);
			string sAfterIns = "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Edits that result in no change of string, don't register as undo/redo actions.
			// Undo
			//m_rtp.OnUndo();
			//m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			//m_rtp.OnRedo();
			//m_rtp.ValidateUndoRedo(tsiAfterIns);
		}


		/// <summary>
		/// There are too many situations to try to guess what the user wants us to do when replacing
		/// a selection with words that match words. So, let's just treat those as new forms that need
		/// to have their analyses confirmed again.
		///
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceWordformsWithStringContainingSameWordforms()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, "xxxpus xxxyalola xxxnihimbilira".Length, 0);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxNewWord xxxpus xxxyalola xxxnihimbilira", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus xxxyalola xxxnihimbilira", "xxxNewWord xxxpus xxxyalola xxxnihimbilira", tsiBeforeIns);
			string sAfterIns = "xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxsup xxxspu xxxups xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceWordformWithWordforms()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, "xxxpus".Length);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxsup xxxspu xxxups", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus", "xxxsup xxxspu xxxups", tsiBeforeIns);
			string sAfterIns = "xxxsup xxxspu xxxups xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpusyalolanihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceWordformsWithWordform()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, "xxxpus xxxyalola xxxnihimbilira".Length);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxpusyalolanihimbilira", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus xxxyalola xxxnihimbilira", "xxxpusyalolanihimbilira", tsiBeforeIns);
			string sAfterIns = "xxxpusyalolanihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertDuplicateWordformIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetCursor(para0, 0);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxpus ", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateInsertedState("xxxpus ", tsiBeforeIns);
			string sAfterIns = "xxxpus xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterIns);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void DeleteLastWordformIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Delete last Word in first sentence
			int ichMin = para0.Contents.Text.IndexOf(" xxxnihimbilira");
			m_rtp.SetSelection(para0, ichMin + " xxxnihimbilira".Length, ichMin);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnCut(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(" xxxnihimbilira", tsiBeforeDelete);
			string sAfterDelete = "xxxpus xxxyalola. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterDelete);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}


		#endregion // EditingParagraphWithNoAnnotations_EditsThatAffectWordforms

		#region EditsThatAffectWordformsAndSegments
		// these tests substantially affect Wordforms and segments in number and content

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void MoveLatterDuplicateWordformToFormerOffset()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Delete last Word in first sentence
			int ichMin = para0.Contents.Text.IndexOf(" xxxnihimbilira.");
			m_rtp.SetSelection(para0, ichMin, ichMin + " xxxnihimbilira.".Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(" xxxnihimbilira.", tsiBeforeDelete);
			string sAfterDelete = "xxxpus xxxyalola xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterDelete);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void DeleteRangeSpanningSegmentBoundaryAndWholeWordforms()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			// Delete last Word in first sentence
			int ichMin = para0.Contents.Text.IndexOf(" xxxnihimbilira. xxxnihimbilira");
			m_rtp.SetSelection(para0, ichMin, ichMin + " xxxnihimbilira. xxxnihimbilira xxxpus".Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(" xxxnihimbilira. xxxnihimbilira xxxpus", tsiBeforeDelete);
			string sAfterDelete = "xxxpus xxxyalola xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterDelete);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}


		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void DeleteFirstTwoSentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. ".Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState("xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. ", tsiBeforeDelete);
			string sAfterDelete = "xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterDelete);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// ""
		/// </summary>
		[Test]
		public virtual void DeleteAllSentencesResultingInEmptyPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, ParagraphContents[0].Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(ParagraphContents[0], tsiBeforeDelete);
			m_rtp.ValidateParagraphContents(para0, "");

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira"
		/// ""
		/// </summary>
		[Test]
		public virtual void DeleteSentenceWithoutSegmentBreakChar()
		{
			// first modify the first paragraph so that we only have one sentence without a
			// paragraph marker.
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			para0.Contents.UnderlyingTsString = para0.Contents.UnderlyingTsString.GetSubstring(0,
				"xxxpus xxxyalola xxxnihimbilira".Length);
			m_rtp.SetRoot(m_text1.ContentsOAHvo);
			m_rtp.SetSelection(para0, 0, para0.Contents.Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
#pragma warning disable 219
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
#pragma warning restore 219
			m_rtp.ValidateDeletedState("xxxpus xxxyalola xxxnihimbilira", tsiBeforeDelete);
			m_rtp.ValidateParagraphContents(para0, "");
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  []
		/// </summary>
		[Test]
		public virtual void DeleteParagraph()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);
			StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;

			m_rtp.SetSelection(para0, 0, para1, 0);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(new int[] { para0.Hvo });
			m_rtp.ValidateParagraphContents(tsiAfterDelete, ParagraphContents[1]);

			// Undo/Redo no longer restores selection (LT-8639)
			// Undo
			//m_rtp.OnUndo();
			//m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			//m_rtp.OnRedo();
			//m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}


		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira. xxxsup xxxlayalo xxxranihimbili.
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);
			StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;

			m_rtp.SetCursor(para0, ParagraphContents[0].Length);
			TextSelInfo tsiBeforeDelete;
			m_rtp.OnDelete(out tsiBeforeDelete);
			TextSelInfo tsiAfterDelete = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateDeletedState(new int[] { para1.Hvo });
			m_rtp.ValidateParagraphContents(tsiAfterDelete, String.Concat(ParagraphContents[0], ParagraphContents[1]));

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeDelete);

			// Redo
			m_rtp.OnRedo();
			// Since this change involves deleting an object, it also involves a Refresh; which means the selection
			// is not restored until the system is idle.
			Application.RaiseIdle(new EventArgs());
			m_rtp.ValidateUndoRedo(tsiAfterDelete);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///1 xxxpus xxxyalola xxxnihimbilira.
		///2 xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteParagraphBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforeEnter;
			TextSelInfo tsiAfterEnter;
			InsertParagraphBreakAfterFirstSegment(para0, out tsiBeforeEnter, out tsiAfterEnter);

#pragma warning disable 219
			TextSelInfo tsiBeforeBackspace = tsiAfterEnter;
#pragma warning restore 219
			StTxtPara para1 = new StTxtPara(Cache, tsiAfterEnter.HvoAnchor);
			TextSelInfo tsiAfterBackspace;
			DeleteParagraphBreakViaBackspace(para1, para0, out tsiAfterBackspace);

			// Undo/Redo no longer restores selection (LT-8639)
			// Undo Backspace
			//UndoDeleteParagraphViaBackspace(para0, para1, tsiBeforeBackspace);

			// Undo Enter
			//UndoInsertParagraphBreakAfterFirstSegment(para1, para0, tsiBeforeEnter);

			// Redo Enter
			//RedoInsertParagraphBreakAfterFirstSegment(para0, para1, tsiAfterEnter);

			// Redo Backspace
			// This is commented out until passes LT7841_UndoDeleteParagraphBreakResultsInRefresh()
			//m_rtp.OnRedo();
			//m_rtp.ValidateUndoRedo(tsiAfterBackspace);
		}

		protected virtual void InsertParagraphBreakAfterFirstSegment(StTxtPara para0, out TextSelInfo tsiBeforeEnter, out TextSelInfo tsiAfterEnter)
		{
			m_rtp.SetCursor(para0, "xxxpus xxxyalola xxxnihimbilira. ".Length);

			m_rtp.OnEnter(out tsiBeforeEnter);
			tsiAfterEnter = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContents(para0, "xxxpus xxxyalola xxxnihimbilira. ");
			m_rtp.ValidateParagraphContents(tsiAfterEnter, "xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");
			StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;
			m_rtp.ValidateIP(para1, 0);
		}

		protected virtual void DeleteParagraphBreakViaBackspace(StTxtPara para1, StTxtPara para0, out TextSelInfo tsiAfterBackspace)
		{
			string para0BeforeMerge = para0.Contents.Text;
			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			tsiAfterBackspace = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContents(tsiAfterBackspace, ParagraphContents[0]);
			m_rtp.ValidateIP(para0, para0BeforeMerge.Length);
		}

		protected virtual void UndoDeleteParagraphViaBackspace(StTxtPara para0, StTxtPara para1, TextSelInfo tsiBeforeBackspace)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeBackspace);
		}

		protected virtual void UndoInsertParagraphBreakAfterFirstSegment(StTxtPara para1, StTxtPara para0, TextSelInfo tsiBeforeEnter)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeEnter);
		}

		protected virtual void RedoInsertParagraphBreakAfterFirstSegment(StTxtPara para0, StTxtPara para1, TextSelInfo tsiAfterEnter)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterEnter);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// paste xxxNew\r\n at start of para
		///1 xxxNew
		///2 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void PasteParaAtStartOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforePaste;
			TextSelInfo tsiAfterPaste;
			PasteParaAtStart(para0, out tsiBeforePaste, out tsiAfterPaste);
		}

		protected virtual void PasteParaAtStart(StTxtPara para0, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		{
			m_rtp.SetCursor(para0, 0);

			m_rtp.OnInsert(String.Format("xxxNew{0}", Environment.NewLine), out tsiBeforePaste);
			tsiAfterPaste = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContents(para0, "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");
			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[1].Hvo, para0.Hvo, "original paragraph moved to second");
			StTxtPara paraNew = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.ValidateParagraphContents(paraNew, "xxxNew");
			m_rtp.ValidateIP(para0, 0);
			m_rtp.ValidateStTextAnnotations();
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// paste xxxNew\r\nxxxMoreNew  at start of para
		///1 xxxNew
		///2 nxxxMoreNew xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void PasteParaPlusTextAtStartOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforePaste;
			TextSelInfo tsiAfterPaste;
			PasteParaPlusTextAtStart(para0, out tsiBeforePaste, out tsiAfterPaste);
		}
		protected virtual void PasteParaPlusTextAtStart(StTxtPara para0, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		{
			m_rtp.SetCursor(para0, 0);

			m_rtp.OnInsert(String.Format("xxxNew{0}xxxMoreNew ", Environment.NewLine), out tsiBeforePaste);
			tsiAfterPaste = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContents(para0, "xxxMoreNew xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");
			Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[1].Hvo, para0.Hvo, "original paragraph moved to second");
			StTxtPara paraNew = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.ValidateParagraphContents(paraNew, "xxxNew");
			m_rtp.ValidateIP(para0, "xxxMoreNew ".Length);
			m_rtp.ValidateStTextAnnotations();
		}

		/// <summary>
		/// This test covers some fairly unusual cases involving pasting multiple lines of text in the
		/// middle of a paragraph. We don't have any immediate plans to save the full annotation information
		/// in the surviving part of the paragraph that moves to a new paragraph, so this test is
		/// disabled.
		/// (To eable this test also uncomment the overrides of PasteParaPlusTextMid and RepBackwards.)
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// paste xxxNew\r\nxxxMoreNew after the second word.
		///1 xxxpus xxxyalola xxxNew
		///2 xxxMoreNew xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// Make a selection with anchor in the second paragraph back to the start of what we pasted
		/// and paste xxxRep over that.
		/// </summary>
		///1 xxxpus xxxyalola xxxRep xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		//[Test]
		//public virtual void PasteParaPlusTextMidParaAndRepBackwards()
		//{
		//    StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
		//    m_rtp.SetRoot(m_text1.ContentsOAHvo);

		//    TextSelInfo tsiBeforePaste;
		//    TextSelInfo tsiAfterPaste;
		//    PasteParaPlusTextMid(para0, out tsiBeforePaste, out tsiAfterPaste);
		//    StTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;
		//    RepBackwards(para0, para1, out tsiBeforePaste, out tsiAfterPaste);
		//}
		//protected virtual void PasteParaPlusTextMid(StTxtPara para0, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		//{
		//    m_rtp.SetCursor(para0, "xxxpus xxxyalola ".Length);

		//    m_rtp.OnInsert("xxxNew\r\nxxxMoreNew ", out tsiBeforePaste);
		//    tsiAfterPaste = m_rtp.CurrentSelectionInfo;
		//    m_rtp.ValidateParagraphContents(para0, "xxxpus xxxyalola xxxNew");
		//    Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[0].Hvo, para0.Hvo, "original paragraph not moved");
		//    StTxtPara paraNew = m_text1.ContentsOA.ParagraphsOS[1] as StTxtPara;
		//    m_rtp.ValidateParagraphContents(paraNew, "xxxMoreNew xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");
		//    m_rtp.ValidateIP(paraNew, "xxxMoreNew ".Length);
		//    m_rtp.ValidateStTextAnnotations();
		//}

		//protected virtual void RepBackwards(StTxtPara para0, StTxtPara para1, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		//{
		//    m_rtp.SetSelection(para1, "xxxMoreNew ".Length, para0, "xxxpus xxxyalola ".Length);
		//    int oldLength = m_text1.ContentsOA.ParagraphsOS.Count;
		//    m_rtp.OnInsert("xxxRep ", out tsiBeforePaste);
		//    tsiAfterPaste = m_rtp.CurrentSelectionInfo;
		//    m_rtp.ValidateParagraphContents(para0, "xxxpus xxxyalola xxxRep xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");
		//    Assert.AreEqual(m_text1.ContentsOA.ParagraphsOS[0].Hvo, para0.Hvo, "original paragraph not moved");
		//    Assert.AreEqual(oldLength - 1, m_text1.ContentsOA.ParagraphsOS.Count, "one of the paragraphs got deleted");
		//    m_rtp.ValidateIP(para0, "xxxpus xxxyalola xxxRep ".Length);
		//    m_rtp.ValidateStTextAnnotations();
		//}


		/// <summary>
		/// Reproduces the problem in LT-7841. This bug prevents us from passing certain tests
		/// when breaking or merging paragraphs via Undo/Redo.
		///
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///1 xxxpus xxxyalola xxxnihimbilira.
		///2 xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void LT7841_UndoDeleteParagraphBreakResultsInRefresh()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetCursor(para0, "xxxpus xxxyalola xxxnihimbilira. ".Length);
			TextSelInfo tsiBeforeEnter;
			m_rtp.OnEnter(out tsiBeforeEnter);
			TextSelInfo tsiAfterEnter = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContentsRaw(para0, "xxxpus xxxyalola xxxnihimbilira. ");
			m_rtp.ValidateParagraphContentsRaw(tsiAfterEnter, "xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.");

			TextSelInfo tsiBeforeBackspace;
			m_rtp.OnBackspace(out tsiBeforeBackspace);
			TextSelInfo tsiAfterBackspace = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateParagraphContentsRaw(tsiAfterBackspace, ParagraphContents[0]);

			// Undo Backspace
			UndoResult ures;
			TextSelInfo tsiBeforeUndoBackspace;
			m_rtp.OnUndo(out tsiBeforeUndoBackspace, out ures);
			Assert.AreEqual(UndoResult.kuresSuccess, ures);

			m_rtp.ValidateUndoRedoRaw(tsiBeforeBackspace);

			// Undo Enter
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedoRaw(tsiBeforeEnter);

			// Redo Enter
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedoRaw(tsiAfterEnter);

			// Redo Backspace
			TextSelInfo tsiBeforeRedoBackspace;
			m_rtp.OnRedo(out tsiBeforeRedoBackspace, out ures);
			Assert.AreEqual(UndoResult.kuresSuccess, ures);
			m_rtp.ValidateUndoRedoRaw(tsiAfterBackspace);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteText()
		{
			int hvoStText = m_text1.ContentsOAHvo;
			m_rtp.SetRoot(hvoStText);
			m_rtp.DeleteText();
			Assert.IsFalse(Cache.IsValidObject(hvoStText));
			m_rtp.ValidateStTextAnnotations();

			// Undo
			m_rtp.OnUndo();
			Assert.IsTrue(Cache.IsValidObject(hvoStText));
			m_rtp.SetRoot(hvoStText);
			m_rtp.ValidateStTextAnnotations();

			// Redo
			m_rtp.OnRedo();
			m_rtp.SetRoot(0);
			Assert.IsFalse(Cache.IsValidObject(hvoStText));
			m_rtp.ValidateStTextAnnotations();
		}

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxsup xxxalolay xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceSentenceWithSameSizeSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforeIns;
			TextSelInfo tsiAfterIns;
			ReplaceSentenceWithSameSizeSentence(para0, out tsiBeforeIns, out tsiAfterIns);

			// Undo
			UndoReplaceSentenceWithSameSizeSentence(tsiBeforeIns);

			// Redo
			RedoReplaceSentenceWithSameSizeSentence(tsiAfterIns);
		}

		protected virtual void RedoReplaceSentenceWithSameSizeSentence(TextSelInfo tsiAfterIns)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		protected virtual void UndoReplaceSentenceWithSameSizeSentence(TextSelInfo tsiBeforeIns)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);
		}

		protected virtual void ReplaceSentenceWithSameSizeSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			m_rtp.SetSelection(para0, 0, "xxxpus xxxyalola xxxnihimbilira.".Length);

			m_rtp.OnInsert("xxxsup xxxalolay xxxarilibmihin.", out tsiBeforeIns);
			tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus xxxyalola xxxnihimbilira.", "xxxsup xxxalolay xxxarilibmihin.", tsiBeforeIns);
			string sAfterReplace = "xxxsup xxxalolay xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterReplace);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxsup. xxxalolay. xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public virtual void ReplaceSentenceWithSentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			m_rtp.SetSelection(para0, 0, "xxxpus xxxyalola xxxnihimbilira.".Length);
			TextSelInfo tsiBeforeIns;
			m_rtp.OnInsert("xxxsup. xxxalolay. xxxarilibmihin.", out tsiBeforeIns);
			TextSelInfo tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus xxxyalola xxxnihimbilira.", "xxxsup. xxxalolay. xxxarilibmihin.", tsiBeforeIns);
			string sAfterReplace = "xxxsup. xxxalolay. xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterReplace);

			// Undo
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);

			// Redo
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxsup
		/// </summary>
		[Test]
		public virtual void ReplaceSentencesWithSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforeIns;
			TextSelInfo tsiAfterIns;
			ReplaceSentencesWithSentence(para0, out tsiBeforeIns, out tsiAfterIns);

			// Undo
			UndoReplaceSentencesWithSentence(tsiBeforeIns);

			// Redo
			RedoReplaceSentencesWithSentence(tsiAfterIns);
		}

		protected virtual void RedoReplaceSentencesWithSentence(TextSelInfo tsiAfterIns)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		protected virtual void UndoReplaceSentencesWithSentence(TextSelInfo tsiBeforeIns)
		{
			m_rtp.OnUndo();
			m_rtp.ValidateUndoRedo(tsiBeforeIns);
		}

		protected virtual void ReplaceSentencesWithSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			m_rtp.SetSelection(para0, 0, "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.".Length);

			m_rtp.OnInsert("xxxsup", out tsiBeforeIns);
			tsiAfterIns = m_rtp.CurrentSelectionInfo;
			m_rtp.ValidateReplacedState("xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.",
				"xxxsup", tsiBeforeIns);
			m_rtp.ValidateParagraphContents(para0, "xxxsup");
		}

		/// <summary>
		///            1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira. xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///
		///	 This addresses one of the comments in LT-5369.
		/// </summary>
		[Test]
		public virtual void InsertSentencesAfterFirstSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.SetRoot(m_text1.ContentsOAHvo);

			TextSelInfo tsiBeforeIns;
			TextSelInfo tsiAfterIns;
			InsertSentencesAfterFirstSentence(para0, out tsiBeforeIns, out tsiAfterIns);

			// Undo
			UndoInsertSentences(tsiBeforeIns);

			// Redo
			RedoInsertSentences(tsiAfterIns);
		}

		protected virtual void InsertSentencesAfterFirstSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			m_rtp.SetCursor(para0, "xxxpus xxxyalola xxxnihimbilira. ".Length);

			m_rtp.OnInsert("xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. ", out tsiBeforeIns);
			tsiAfterIns = m_rtp.CurrentSelectionInfo;
			ValidateInsertSentencesAfterFirstSentence(para0, tsiBeforeIns);
		}

		protected virtual void ValidateInsertSentencesAfterFirstSentence(StTxtPara para0, TextSelInfo tsiBeforeIns)
		{
			m_rtp.ValidateReplacedState("", "xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. ", tsiBeforeIns);
			string sAfterReplace = "xxxpus xxxyalola xxxnihimbilira. xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.";
			m_rtp.ValidateParagraphContents(para0, sAfterReplace);
		}

		protected virtual void UndoInsertSentences(TextSelInfo tsiBeforeIns)
		{
			UndoResult ures;
			m_rtp.OnUndo(out ures);
			m_rtp.ValidateUndoRedo(UndoResult.kuresSuccess, ures, tsiBeforeIns);
		}

		protected virtual void RedoInsertSentences(TextSelInfo tsiAfterIns)
		{
			m_rtp.OnRedo();
			m_rtp.ValidateUndoRedo(tsiAfterIns);
		}

		#endregion EditsThatAffectWordsAndSegments
		#endregion EditingParagraphTests

		#endregion Tests

	}

	/// <summary>
	/// enable 	[TestFixture] for debugging basic problems in these tests.
	/// </summary>
	//[TestFixture]
	public class TextEditingTestsWithNoAnnotations : TextEditingTests
	{
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			TextBuilder tb = new TextBuilder();
			UndoableUnitOfWorkHelper.Do("TextEditingTests - Undo Setup texts",
				"TextEditingTests - Redo Setup texts", m_actionHandler, () =>
			{
				// Create a mirrored text without annotations.
				m_text1 = tb.CreateText(true);
				IStTxtPara para0 = tb.AppendNewParagraph();
				IStTxtPara para1 = tb.AppendNewParagraph();
				// raw copy from the annotated text.
				//	          1         2         3         4         5         6         7         8         9
				//  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
				//	xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
				para0.Contents.UnderlyingTsString = TsStringUtils.MakeTss(ParagraphContents[0], Cache.DefaultVernWs);
				para1.Contents.UnderlyingTsString = TsStringUtils.MakeTss(ParagraphContents[1], Cache.DefaultVernWs);
			}
			m_rtp = new MockRawTextEditor(Cache, tb);
		}

	}

	/// <summary>
	/// enable for testing editing tests more basic than with real/analyzed annotations.
	/// [TestFixture]
	/// </summary>
	public class TextEditingTestsWithAnnotations : TextEditingTests
	{
		XmlDocument m_textsDefn = new XmlDocument();

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			TextBuilder tb;
			UndoableUnitOfWorkHelper.Do("TextEditingTests - Undo Setup texts",
				"TextEditingTests - Redo Setup texts", m_actionHandler, () =>
			{
				m_text1 = LoadTestText(@"LexText\Interlinear\ITextDllTests\TextEditingTestsTexts.xml", 1, m_textsDefn, out tb);
			}
			m_rtp = new MockRawTextEditor(Cache, tb);
			tb.GenerateAnnotationIdsFromDefn();
		}

		protected override void Dispose(bool disposing)
		{
			m_textsDefn = null;

			base.Dispose(disposing);
		}

		public override void Exit()
		{
			base.Exit();
			// clear the wordform table.
			Cache.LangProject.WordformInventoryOA.ResetAllWordformOccurrences();
		}

		#region SetupTest
		/// <summary>
		/// This checks that our TextBuilder and ParagraphBuilder build texts like we expect.
		/// </summary>
		[Test]
		public void BuildTextFromAnnotations()
		{
			CheckDisposed();

			Assert.IsNotNull(m_text1);
			Assert.AreEqual(m_text1.Name.VernacularDefaultWritingSystem, "Test Text1 for TextEditingTests");
			Assert.IsNotNull(m_text1.ContentsOA);
			Assert.AreEqual(Enum.GetValues(typeof(Text1ParaIndex)).Length, m_text1.ContentsOA.ParagraphsOS.Count);
			// Simple Segments.
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.SimpleSegmentPara],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.SimpleSegmentPara]).Contents.Text);
			// Extra paragraph
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.ExtraPara],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.ExtraPara]).Contents.Text);
			// Complex Punctuations
			//Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.ComplexSegments],
			//	((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.ComplexSegments]).Contents.Text);
			m_rtp.ValidateStTextAnnotations();
		}

		#endregion SetupTest

		#region EditingParagraphTests

		#region EditsThatAffectOffsetsForSegmentsAndSegmentForms

		/// <summary>
		///            1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira. "
		/// </summary>
		[Test]
		public override void InsertAndDeleteSpaceAtEndOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 2, 2);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert a whitespace after final "xxxnihimbilira."
			pb.ReplaceTrailingWhitepace(2, 2, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			XmlNode paraNodeAfterDelete = paraNodeInitial;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteSpaceAtEndOfPara();
		}

		/// <summary>
		///	          1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira ."
		/// </summary>
		[Test]
		public override void InsertAndDeleteSpaceBeforePeriodAtEndOfPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 2, 1);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert a whitespace after final "xxxnihimbilira"
			pb.ReplaceTrailingWhitepace(2, 1, 1);
#pragma warning disable 219
			XmlNode paraNodeAfterEdit = m_rtp.AddResultingAnnotationState(tb.SelectedNode);
			pb.ReplaceTrailingWhitepace(2, 1, 0);
			XmlNode paraNodeAfterDelete = m_rtp.AddResultingAnnotationState(tb.SelectedNode);
#pragma warning restore 219

			base.InsertAndDeleteSpaceBeforePeriodAtEndOfPara();
		}

		/// <summary>
		///            1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///  xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///  xxxpus xxxyalola xxxnihimbilira . xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteSpaceAtEndOfFirstSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0, 2);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			// preserve the initial state from being edited.
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert a whitespace after first "xxxnihimbilira"
			pb.ReplaceTrailingWhitepace(0, 2, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			XmlNode paraNodeAfterDelete = paraNodeInitial;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteSpaceAtEndOfFirstSentence();
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///	 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///   xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteSpaceAtBeginningOfParagraph()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			// preserve the initial state from being edited.
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert a whitespace after first "xxxnihimbilira"
			pb.ReplaceLeadingWhitepace(0, 0, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			XmlNode paraNodeAfterDelete = paraNodeInitial;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteSpaceAtBeginningOfParagraph();
		}

		#endregion EditsThatAffectOffsetsForSegmentsAndSegmentForms

		#region EditsThatAffectSegmentNumber


		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus. xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteSegmentBreakAfter1stWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			// preserve the initial state from being edited.
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert a period after first "xxxpus"
			pb.InsertSegmentBreak(0, 1, ".");
			pb.ReplaceTrailingWhitepace(0, 1, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			XmlNode paraNodeAfterDelete = paraNodeInitial;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteSegmentBreakAfter1stWord();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteSegmentBreakToMerge2Sentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			// preserve the initial state from being edited.
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// delete period after first "xxxnihimbilira"
			pb.DeleteSegmentBreak(0, 3);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			XmlNode paraNodeAfterDelete = paraNodeInitial;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.DeleteSegmentBreakToMerge2Sentences();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira? xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira, xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceSegBreakWithAnotherSegBreakThenWithNonSegBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

#pragma warning disable 219
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
#pragma warning restore 219
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// replace '.' with '?'
			pb.ReplaceSegmentForm(0, 3, "?", 0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			// Replace segment break with non segment breaking character.
			pb.DeleteSegmentBreak(0, 3);
			pb.InsertSegmentForm(0, 3, CmAnnotationDefnTags.kguidAnnPunctuationInContext, ",");
			pb.ReplaceTrailingWhitepace(0, 3, 1);
			XmlNode paraNodeAfterDelete = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.ReplaceSegBreakWithAnotherSegBreakThenWithNonSegBreak();
		}

		#endregion EditsThatAffectSegmentNumber

		#region EditsThatAffectWords

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxApus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteWordFormCharInside1stWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			// preserve the initial state from being edited.
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert "A" in first "xxxpus"
			pb.ReplaceSegmentForm(0, 0, "xxxApus", 0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			pb.ReplaceSegmentForm(0, 0, "xxxpus", 0);
			XmlNode paraNodeAfterDelete = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteWordFormCharInside1stWord();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxx pus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteSpaceInside1stWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			// insert space in first "xxxpus"
			pb.ReplaceSegmentForm(0, 0, "xxx", 0);
			pb.InsertSegmentForm(0, 1, CmAnnotationDefnTags.kguidAnnWordformInContext, "pus");
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			pb.ReplaceSegmentForm(0, 0, "xxxpus", 0);
			pb.RemoveSegmentForm(0, 1);
			XmlNode paraNodeAfterDelete = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterDelete);

			base.InsertAndDeleteSpaceInside1stWord();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertNewWordIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			// preserve the initial state from being edited.
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

#pragma warning disable 219
			XmlNode paraNodeInitial = ParagraphBuilder.Snapshot(tb.SelectedNode);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxNewWord");
			XmlNode paraNodeAfterEdit = m_rtp.AddResultingAnnotationState(tb.SelectedNode);
			pb.RemoveSegmentForm(0, 0);
			XmlNode paraNodeAfterDelete = m_rtp.AddResultingAnnotationState(tb.SelectedNode);
#pragma warning restore 219

			base.InsertNewWordIn1stSentence();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxsup xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceWordWithSameSizeWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.ReplaceSegmentForm(0, 0, "xxxsup", 0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceWordWithSameSizeWord();
		}

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceWordWithSameWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectedNode = m_rtp.GetParagraphBuilder(para0.Hvo).SegmentNodes()[0];
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			ParagraphAnnotatorForParagraphBuilder papb = new ParagraphAnnotatorForParagraphBuilder(pb);
			string wgloss;
			papb.SetDefaultWordGloss(0, 0, out wgloss);
			pb.ReplaceSegmentForm(0, 0, "xxxpus", 0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceWordWithSameWord();
		}

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxNewWord xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceWordsWithStringContainingSameWords()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			ParagraphAnnotatorForParagraphBuilder papb = new ParagraphAnnotatorForParagraphBuilder(
				m_rtp.TextBuilder.GetParagraphBuilder(para0.Hvo));
			string wgloss;
			papb.SetDefaultWordGloss(0, 0, out wgloss);

			// clone text builder to modify a different version of the document.
			// this will help Validation maintain the initial word gloss state.
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// basically replace all the existing Words in the selection with new ones.
			pb.ReplaceSegmentForm(0, 0, "xxxNewWord", 0);
			pb.ReplaceSegmentForm(0, 1, "xxxpus", 0);
			pb.ReplaceSegmentForm(0, 2, "xxxyalola", 0);
			pb.InsertSegmentForm(0, 3, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxnihimbilira");
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceWordsWithStringContainingSameWords();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxsup xxxspu xxxups xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceWordWithWords()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.ReplaceSegmentForm(0, 0, "xxxsup", 0);
			pb.InsertSegmentForm(0, 1, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxspu");
			pb.InsertSegmentForm(0, 2, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxups");
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceWordWithWords();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpusyalolanihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceWordsWithWord()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			// preserve the initial state from being edited.
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.ReplaceSegmentForm(0, 0, "xxxpusyalolanihimbilira", 0);
			pb.RemoveSegmentForm(0, 1);
			pb.RemoveSegmentForm(0, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceWordsWithWord();
		}


		/// <summary>
		/// NOTE: This currently fails because DataUpdateMonitor issues the wrong ivIns (10) due to
		/// determining the IP anchor via string comparison. PasteFromClipboard does vwsel.ReplaceWithTsString(tss)
		/// which loses the IP, thus Commit(vwsel) can have the wrong selection info queued up for doing the prop change.
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertDuplicateWordformIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxpus");
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.InsertDuplicateWordformIn1stSentence();
		}

		/// <summary>
		/// Override the main body of this test to adjust the expected annotations.
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// paste xxxNew\r\n at start of para
		///1 xxxNew
		///2 nxxxMoreNew xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		protected override void PasteParaPlusTextAtStart(StTxtPara para0, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		{
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxMoreNew");
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			base.PasteParaPlusTextAtStart(para0, out tsiBeforePaste, out tsiAfterPaste);
		}

		/// <summary>
		///	           1         2         3         4         5         6         7         8         9
		///  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		///1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// paste xxxNew\r\nxxxMoreNew after the second word.
		///1 xxxpus xxxyalola xxxNew
		///2 xxxMoreNew xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		//protected override void PasteParaPlusTextMid(StTxtPara para0, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		//{
		//    TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
		//    ParagraphBuilder pb0 = tb.GetParagraphBuilder(para0.Hvo);
		//    pb0.InsertSegmentForm(0, 2, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxNew");
		//    tb.SelectNode(para0, 0, 3); // break before xxxnihimbilira
		//    XmlNode newParaNodeAfterParagraphBreak = tb.InsertParagraphBreak(para0, tb.SelectedNode);
		//    ParagraphBuilder pbNew = tb.GetParagraphBuilderForNotYetExistingParagraph(newParaNodeAfterParagraphBreak);
		//    pbNew.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxMoreNew");
		//    m_rtp.AddResultingAnnotationState(newParaNodeAfterParagraphBreak);

		//    base.PasteParaPlusTextMid(para0, out tsiBeforePaste, out tsiAfterPaste);
		//}

		///// <summary>
		/////	           1         2         3         4         5         6         7         8         9
		/////  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/////1 xxxpus xxxyalola xxxNew
		/////2 xxxMoreNew xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///// Make a selection with anchor in the second paragraph back to the start of what we pasted
		///// and paste xxxRep over that.
		/////1 xxxpus xxxyalola xxxRep xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		///// </summary>
		//protected override void RepBackwards(StTxtPara para0, StTxtPara para1, out TextSelInfo tsiBeforePaste, out TextSelInfo tsiAfterPaste)
		//{
		//    TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
		//    ParagraphBuilder pb0 = tb.GetParagraphBuilder(para0.Hvo);
		//    pb0.ReplaceSegmentForm(0, 2, "xxxRep", 0);
		//    ParagraphBuilder pb1 = tb.GetParagraphBuilder(para1.Hvo);
		//    pb1.RemoveSegmentForm(0, 0);
		//    tb.DeleteParagraphBreak(para0);
		//    m_rtp.AddResultingAnnotationState(tb.SelectedNode);
		//    base.RepBackwards(para0, para1, out tsiBeforePaste, out tsiAfterPaste);
		//}

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteLastWordIn1stSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0, 0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);

			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
			pb.RemoveSegmentForm(0, 2);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteLastWordIn1stSentence();
		}

		#endregion EditsThatAffectWords

		#region EditsThatAffectWordsAndSegments

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void MoveLatterDuplicateWordformToFormerOffset()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// remove segment boundary
			pb.DeleteSegmentBreak(0, 3);
			// delete first "xxxnihimbilira" from first sentence
			pb.RemoveSegmentForm(0, 2);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.MoveLatterDuplicateWordformToFormerOffset();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteRangeSpanningSegmentBoundaryAndWholeWords()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// remove segment boundary
			pb.DeleteSegmentBreak(0, 3);
			// delete "xxxnihimbilira xxxnihimbilira xxxpus" from first sentence
			pb.RemoveSegmentForm(0, 2);	// (first) xxxnihimbilira
			pb.RemoveSegmentForm(0, 2); // (second) xxxnihimbilira
			pb.RemoveSegmentForm(0, 2); // (second) xxxpus

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteRangeSpanningSegmentBoundaryAndWholeWords();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteFirstTwoSentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// remove first two segments
			pb.RemoveSegment(1);
			pb.RemoveSegment(0);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteFirstTwoSentences();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira."
		/// ""
		/// </summary>
		[Test]
		public override void DeleteAllSentencesResultingInEmptyPara()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// remove all segments
			pb.RemoveSegment(2);
			pb.RemoveSegment(1);
			pb.RemoveSegment(0);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteAllSentencesResultingInEmptyPara();
		}

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// "xxxpus xxxyalola xxxnihimbilira"
		/// ""
		/// </summary>
		[Test]
		public override void DeleteSentenceWithoutSegmentBreakChar()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// Setup new initial state:
			// remove last two segments and last punctuation.
			pb.RemoveSegment(2);
			pb.RemoveSegment(1);
			pb.RemoveSegmentForm(0, 3); // last period
			m_rtp.TextBuilder.SetTextDefnFromSelectedNode(ParagraphBuilder.Snapshot(tb.SelectedNode));

			// remove remaining segment
			pb.RemoveSegment(0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);
			base.DeleteSentenceWithoutSegmentBreakChar();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// []
		/// </summary>
		[Test]
		public override void DeleteParagraph()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(null);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
#pragma warning disable 219
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
#pragma warning restore 219

			tb.DeleteParagraphDefn(para0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteParagraph();
		}

		/// <summary>
		/// This doesn't require testing on annotations. However, we don't override it, because
		/// currently the whole no-annotations suite is disabled, so it wouldn't be run at all if not done here.
		/// Review EricP (JohnT): is it really true we don't need to verify the annotations? The test definitely
		/// fails if we use the validation routines which DO check annotations...to get that to work we'd have
		/// to break the base method into the two separate edits that insert a CR and then delete it, and
		/// do all the stuff with text builder and paragraph builder to create a valid expected state...
		/// </summary>
		//public override void LT7841_UndoDeleteParagraphBreakResultsInRefresh()
		//{
		//    base.LT7841_UndoDeleteParagraphBreakResultsInRefresh();
		//}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.xxxsup xxxlayalo xxxranihimbili.
		/// </summary>
		[Test]
		public override void DeleteParagraphBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(null);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
#pragma warning disable 219
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);
#pragma warning restore 219

			tb.DeleteParagraphBreak(para0);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.DeleteParagraphBreak();
		}

		/// <summary>
		///	   		    1         2         3         4         5         6         7         8         9
		///   0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// 1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// 2 xxxsup xxxlayalo xxxranihimbili.
		/// 3 xxxups xxxloyala xxxliranihimbi.
		/// </summary>
		[Test]
		[Ignore("TODO for LT-8509")]
		public virtual void DeleteAllParagraphs()
		{
#pragma warning disable 219
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
#pragma warning restore 219
			m_rtp.TextBuilder.SelectNode(null);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			IStTxtPara newPara = tb.AppendNewParagraph();
			ParagraphBuilder pb = tb.GetParagraphBuilder(newPara.Hvo);
			pb.CreateSegmentNode();
			pb.CreateSegmentForms();
			pb.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnPunctuationInContext, ".");
			pb.InsertSegmentForm(0, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxups");
			pb.InsertSegmentForm(0, 1, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxloyala");
			pb.InsertSegmentForm(0, 2, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxliranihimbi");
			pb.RebuildParagraphContentFromAnnotations();

			m_rtp.SelectAll();
			TextSelInfo tsiBeforeDel;
			m_rtp.OnDelete(out tsiBeforeDel);
			m_rtp.ValidateStTextAnnotations();
		}


		/// <summary>
		///			    1         2         3         4         5         6         7         8         9
		///   012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012
		/// 1 xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// 1 xxxpus xxxyalola xxxnihimbilira.
		/// 2 xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void InsertAndDeleteParagraphBreak()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			tb.SelectNode(para0, 1);
			XmlNode newParaNodeAfterInsert = tb.InsertParagraphBreak(para0, tb.SelectedNode);
			m_rtp.AddResultingAnnotationState(newParaNodeAfterInsert);
			tb.DeleteParagraphBreak(para0);
			tb.SelectNode(para0, 1);
			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.InsertAndDeleteParagraphBreak();
		}

		/// <summary>
		/// </summary>
		[Test]
		[Ignore("LT-????")]
		public override void DeleteText()
		{
			// We probably need a way to validate all the annotations got deleted.
			base.DeleteText();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxsup
		/// </summary>
		[Test]
		public override void ReplaceSentencesWithSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// remove last two segments
			pb.RemoveSegment(2);
			pb.RemoveSegment(1);
			// remove all but one form in the sentence
			pb.RemoveSegmentForm(0, 3);
			pb.RemoveSegmentForm(0, 2);
			pb.RemoveSegmentForm(0, 1);
			pb.ReplaceSegmentForm(0, 0, "xxxsup", 0);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceSentencesWithSentence();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxsup xxxalolay xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceSentenceWithSameSizeSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			pb.ReplaceSegmentForm(0, 0, "xxxsup", 0);
			pb.ReplaceSegmentForm(0, 1, "xxxalolay", 0);
			pb.ReplaceSegmentForm(0, 2, "xxxarilibmihin", 0);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceSentenceWithSameSizeSentence();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxsup. xxxalolay. xxxarilibmihin. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceSentenceWithSentences()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// replace wordforms
			pb.ReplaceSegmentForm(0, 0, "xxxsup", 0);
			pb.ReplaceSegmentForm(0, 1, "xxxalolay", 0);
			pb.ReplaceSegmentForm(0, 2, "xxxarilibmihin", 0);
			// insert segment breaks
			pb.InsertSegmentBreak(0, 2, ".");
			pb.ReplaceTrailingWhitepace(0, 2, 1);
			pb.InsertSegmentBreak(0, 1, ".");
			pb.ReplaceTrailingWhitepace(0, 1, 1);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.ReplaceSentenceWithSentences();
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira. xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// This addresses one of the comments in LT-5369.
		/// </summary>
		[Test]
		public override void InsertSentencesAfterFirstSentence()
		{
			StTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara;
			m_rtp.TextBuilder.SelectNode(para0);
			TextBuilder tb = new TextBuilder(m_rtp.TextBuilder);
			ParagraphBuilder pb = tb.GetParagraphBuilder(para0.Hvo);

			// replace wordforms
			pb.InsertSegmentForm(1, 0, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxfirstnew");
			pb.InsertSegmentForm(1, 1, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxsentence");
			pb.InsertSegmentForm(1, 2, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxsecondnew");
			pb.InsertSegmentForm(1, 3, CmAnnotationDefnTags.kguidAnnWordformInContext, "xxxsentence");

			// insert segment breaks
			pb.InsertSegmentBreak(1, 4, ".");
			pb.InsertSegmentBreak(1, 2, ".");
			pb.ReplaceTrailingWhitepace(1, 2, 1);
			pb.ReplaceTrailingWhitepace(2, 2, 1);

			XmlNode paraNodeAfterEdit = tb.SelectedNode;
			m_rtp.AddResultingAnnotationState(paraNodeAfterEdit);

			base.InsertSentencesAfterFirstSentence();
		}

		#endregion EditsThatAffectWordsAndSegments

		#endregion EditingParagraphTests

	}

	[TestFixture]
	public class TextEditingTestsWithAnnotations_FullyAnalyzed : TextEditingTestsWithAnnotations
	{
#pragma warning disable 414
		SegmentFreeFormAnnotationValidationHelper m_segFFvalidationHelper;
#pragma warning restore 414

		# region SetUp/Teardown
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			// annotate all the Words in the text.
			foreach (IStPara para in m_rtp.TextBuilder.ActualStText.ParagraphsOS)
			{
				ParagraphBuilder pb = m_rtp.TextBuilder.GetParagraphBuilder(para.Hvo);
				ParagraphAnnotatorForParagraphBuilder papb = new ParagraphAnnotatorForParagraphBuilder(pb);
				papb.SetupDefaultWordGlosses();
			}
		}

		[TearDown]
		public override void Exit()
		{
			base.Exit();
		}

		#endregion SetUp/Teardown

		class SegmentFreeFormAnnotationValidationHelper : IDisposable
		{
			int m_vtagSegFF = 0;

			TextEditingTestsWithAnnotations_FullyAnalyzed m_tests = null;
			StTxtPara m_para;
			List<int> segmentsBeforeSegBreakEdit;
			List<int> segmentsAfterSegBreakEdit;

			/// <summary>
			/// each index refers to a segment containing a list of cloned ffs containing comments.
			/// </summary>
			List<List<int>> segmentFFCommentsBeforeSegBreakEdit = new List<List<int>>();
			/// <summary>
			/// keeps track of the corresponding real ff id that had been cloned into a dummy id.
			/// </summary>
			Dictionary<int, int> ffsClonedToReal = new Dictionary<int, int>();

			internal SegmentFreeFormAnnotationValidationHelper(TextEditingTestsWithAnnotations_FullyAnalyzed tests, StTxtPara para)
				: this(para)
			{
				m_tests = tests;
				m_tests.m_segFFvalidationHelper = this;
			}

			internal SegmentFreeFormAnnotationValidationHelper(StTxtPara para)
			{
				m_para = para;
				m_vtagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(para.Cache);
				EstablishInitialSegmentState();
			}

			internal void EstablishInitialSegmentState()
			{
				segmentsBeforeSegBreakEdit = m_para.Segments;
				LoadParagraphSegFFData(m_para);
				segmentFFCommentsBeforeSegBreakEdit.Clear();
				// for each segment, capture all its freeform annotation comments.
				for (int i = 0; i < segmentsBeforeSegBreakEdit.Count; i++)
				{
					FdoObjectSet<ICmIndirectAnnotation> segmentFFsBeforeSegBreakEdit = GetFFs(segmentsBeforeSegBreakEdit[i]);
					segmentFFCommentsBeforeSegBreakEdit.Add(CaptureFFComments(segmentFFsBeforeSegBreakEdit));
				}
			}

			private FdoObjectSet<ICmIndirectAnnotation> GetFFs(int hvoSeg)
			{
				int[] segFFs;
				segFFs = Cache.GetVectorProperty(hvoSeg, m_vtagSegFF, true);
				return new FdoObjectSet<ICmIndirectAnnotation>(Cache, segFFs, false, typeof(CmIndirectAnnotation));
			}

			private void LoadParagraphSegFFData(StTxtPara para)
			{
				Set<int> analWsIds = new Set<int>(para.Cache.LangProject.AnalysisWssRC.HvoArray);
				StTxtPara.LoadSegmentFreeformAnnotationData(Cache, para.Hvo, analWsIds);
			}

			/// <summary>
			/// </summary>
			/// <param name="segmentFFs"></param>
			/// <returns></returns>
			internal List<int> CaptureFFComments(FdoObjectSet<ICmIndirectAnnotation> segmentFFs)
			{
				List<int> ffsClonedWithComments = new List<int>();
				foreach (CmIndirectAnnotation cia in segmentFFs)
				{
					int hvoCloned = cia.CloneIntoDummy();
					ffsClonedWithComments.Add(hvoCloned);
					ffsClonedToReal.Add(hvoCloned, cia.Hvo);
				}
				return ffsClonedWithComments;
			}

			private FdoCache Cache
			{
				get { return m_para.Cache; }
			}

			#region IDisposable Members

			public void Dispose()
			{
				segmentFFCommentsBeforeSegBreakEdit = null;
				m_para = null;
				if (m_tests != null)
				{
					m_tests.m_segFFvalidationHelper = null;
					m_tests = null;
				}
				segmentsBeforeSegBreakEdit = null;
				segmentsAfterSegBreakEdit = null;
				GC.SuppressFinalize(this);
			}

			#endregion

			internal void ValidateMatchingFreeformAnnotations(int iSegBeforeSegBreakEdit, int iSegAfterSegBreakEdit)
			{
				ValidateMatchingFreeformAnnotations(GetFFs(segmentsBeforeSegBreakEdit[iSegBeforeSegBreakEdit]),
					GetFFs(segmentsAfterSegBreakEdit[iSegAfterSegBreakEdit]));
			}

			private void ValidateMatchingFreeformAnnotations(FdoObjectSet<ICmIndirectAnnotation> segmentFFs_a,
			FdoObjectSet<ICmIndirectAnnotation> segmentFFs_b)
			{
				List<ICmIndirectAnnotation> segFFs_a = segmentFFs_a.ToList();
				Assert.AreEqual(segmentFFs_a.Count, segmentFFs_b.Count, "Expected segments to have matching number of indirect annotations.");
				foreach (ICmIndirectAnnotation ann_b in segmentFFs_b)
				{
					int index = segFFs_a.IndexOf(ann_b);
					Assert.IsTrue(index >= 0, "Expected segment to have matching hvos for indirect annotations");
					// make sure the content hasn't changed.
					foreach (int ws in ann_b.Cache.LangProject.AnalysisWssRC.HvoArray)
					{
						Assert.AreEqual(segFFs_a[index].Comment.GetAlternativeTss(ws),
							ann_b.Comment.GetAlternativeTss(ws),
							"Expected indirect annotations to have same comment content.");
					}
				}
			}

			internal void ValidateMergedFreeformAnnotations(int iSegTarget, int iSegSrc)
			{
				FdoObjectSet<ICmIndirectAnnotation> segmentFFsAfterMerge = GetFFs(segmentsAfterSegBreakEdit[iSegTarget]);

				//helper.ValidateMergedFreeformAnnotations(segmentsBeforeSegBreakEdit[0], segmentsBeforeSegBreakEdit[1], segmentFFCommentsBeforeMerge_0, segmentFFCommentsBeforeMerge_1, segmentFFsAfterMerge_0);
				ValidateMergedFreeformAnnotations(segmentsBeforeSegBreakEdit[iSegTarget], segmentsBeforeSegBreakEdit[iSegSrc],
					segmentFFCommentsBeforeSegBreakEdit[iSegTarget], segmentFFCommentsBeforeSegBreakEdit[iSegSrc], segmentFFsAfterMerge);
			}

			/// <summary>
			/// </summary>
			/// <param name="cbaSegTargetHvo"></param>
			/// <param name="cbaSegSrcHvo"></param>
			/// <param name="segmentFFCommentsBeforeSegBreakEdit_begin"></param>
			/// <param name="segmentFFCommentsBeforeSegBreakEdit_end"></param>
			/// <param name="segmentFFsAfterMerge"></param>
			private void ValidateMergedFreeformAnnotations(int cbaSegTargetHvo, int cbaSegSrcHvo,
				List<int> segmentFFCommentsBeforeMerge_target,
				List<int> segmentFFCommentsBeforeMerge_src,
				FdoObjectSet<ICmIndirectAnnotation> segmentFFsAfterMerge)
			{
				int segDefn_note = Cache.GetIdFromGuid(CmAnnotationDefnTags.kguidAnnNote);
				Dictionary<int, List<ICmIndirectAnnotation>> typeToAnnBeforeMergeBegin =
					AnnotationAdjuster.MakeAnnTypeToAnnDictionary(Cache, segmentFFCommentsBeforeMerge_target);
				Dictionary<int, List<ICmIndirectAnnotation>> typeToAnnAfterMerge =
					AnnotationAdjuster.MakeAnnTypeToAnnDictionary(segmentFFsAfterMerge);
				List<ICmIndirectAnnotation> srcNotes = new List<ICmIndirectAnnotation>();
				foreach (int srcFreeformAnnHvo in segmentFFCommentsBeforeMerge_src)
				{
					ICmIndirectAnnotation srcFreeformAnn = new CmIndirectAnnotation(Cache, srcFreeformAnnHvo) as ICmIndirectAnnotation;
					ICmIndirectAnnotation realSrcFreeformAnn = new CmIndirectAnnotation(Cache, ffsClonedToReal[srcFreeformAnn.Hvo]);
					List<ICmIndirectAnnotation> targetAnns;
					List<ICmIndirectAnnotation> mergedAnns;
					Assert.IsTrue(typeToAnnAfterMerge.TryGetValue(srcFreeformAnn.AnnotationTypeRAHvo, out mergedAnns),
							"Expected src ff annotation to be in the merged set.");
					// see if we can find the annotation on the target segment matching the same type.
					// if so, we should have merged into its comment.
					if (srcFreeformAnn.AnnotationTypeRAHvo != segDefn_note &&
						typeToAnnBeforeMergeBegin.TryGetValue(srcFreeformAnn.AnnotationTypeRAHvo, out targetAnns))
					{
						Assert.IsFalse(realSrcFreeformAnn.IsRealObject, "Expected merged ff to have been deleted.");
						Assert.AreEqual(1, targetAnns.Count, "We only expect to have one annotation of this type.");
						int hvoTargetFF = targetAnns[0].Hvo;
						// see if we can find src comment in the same ws as the target.
						ICmIndirectAnnotation targetFreeformAnn = new CmIndirectAnnotation(Cache, hvoTargetFF) as ICmIndirectAnnotation;
						foreach (int wsTarget in Cache.LangProject.CurrentAnalysisAndVernWss)
						{
							// assume that the first ws-run indicates the ws of this multistring.
							ITsString tssTargetComment = targetFreeformAnn.Comment.GetAlternativeTss(wsTarget);
							ITsString tssSrcComment = srcFreeformAnn.Comment.GetAlternativeTss(wsTarget);
							string sTarget = "";
							string sSrc = "";
							if (tssTargetComment.Length > 0)
							{
								sTarget = tssTargetComment.Text + " ";
							}
							if (tssSrcComment.Length > 0)
							{
								sSrc = tssSrcComment.Text;
							}
							if (tssTargetComment.Text == null && tssSrcComment.Text == null)
							{
								Assert.AreEqual(null,
									typeToAnnAfterMerge[srcFreeformAnn.AnnotationTypeRAHvo][0].Comment.GetAlternativeTss(wsTarget).Text,
									"Expected annotation comments to be merged.");
							}
							else
							{
								Assert.AreEqual(sTarget + sSrc,
									typeToAnnAfterMerge[srcFreeformAnn.AnnotationTypeRAHvo][0].Comment.GetAlternativeTss(wsTarget).Text,
									"Expected annotation comments to be merged.");
							}
						}
					}
					else
					{
						Assert.IsTrue(mergedAnns.IndexOf(realSrcFreeformAnn) != -1);
						Assert.IsTrue(realSrcFreeformAnn.IsRealObject, "Expected merged ff to have been deleted.");
						if (srcFreeformAnn.AnnotationTypeRAHvo == segDefn_note)
						{
							srcNotes.Add(srcFreeformAnn);
							// find this Note in the merged list
						}
						foreach (int wsSrc in Cache.LangProject.CurrentAnalysisAndVernWss)
						{
							// assume that the first ws-run indicates the ws of this multistring.
							ITsString tssSrcComment = srcFreeformAnn.Comment.GetAlternativeTss(wsSrc);
							ITsString tssMovedComment = realSrcFreeformAnn.Comment.GetAlternativeTss(wsSrc);
							Assert.IsNotNull(tssMovedComment);
							Assert.AreEqual(tssSrcComment.Text, tssMovedComment.Text,
								"Expected annotation comments to be identical.");
						}
					}
				}
				// make sure all the merged ff annotations point to the target and none point to the old src segment
				// it's a note or not found on the target, just move the AppliesToRC item to the target.
				// make sure it's not pointing to the old segment.
				foreach (ICmIndirectAnnotation ciaMergedFF in segmentFFsAfterMerge)
				{
					Assert.IsFalse(ciaMergedFF.AppliesToRS.Contains(cbaSegSrcHvo),
						"Expected freeform annotation to no longer reference a merged annotation.");
					// make sure it's pointing to the new segment.
					Assert.IsTrue(ciaMergedFF.AppliesToRS.Contains(cbaSegTargetHvo),
						"Expected freeform annotation to reference target annotation.");
				}
				// make sure we have the expected number of notes.
				List<ICmIndirectAnnotation> mergedNotes;
				if (!typeToAnnAfterMerge.TryGetValue(segDefn_note, out mergedNotes))
					mergedNotes = new List<ICmIndirectAnnotation>();
				List<ICmIndirectAnnotation> targetNotes;
				if (!typeToAnnBeforeMergeBegin.TryGetValue(segDefn_note, out targetNotes))
					targetNotes = new List<ICmIndirectAnnotation>();
				Assert.AreEqual(targetNotes.Count + srcNotes.Count, mergedNotes.Count,
					"Expected the same number of freeform annotations.");
			}

			internal void CaptureAndValidateSegmentsAfterSegBreakEdit(int cExpectedNewSegmentCount)
			{
				StTxtPara para = m_para;
				CaptureAndValidateSegmentsAfterSegBreakEdit(cExpectedNewSegmentCount, para);
			}

			internal void CaptureAndValidateSegmentsAfterSegBreakEdit(int cExpectedNewSegmentCount, StTxtPara para)
			{
				segmentsAfterSegBreakEdit = para.Segments;
				Assert.AreEqual(cExpectedNewSegmentCount, segmentsAfterSegBreakEdit.Count);
				LoadParagraphSegFFData(para);
			}

			internal void ValidateSegFFCount(int iSeg, int cExpectedFFs)
			{
				StTxtPara para = m_para;
				ValidateSegFFCount(iSeg, cExpectedFFs, para);
			}

			internal void ValidateSegFFCount(int iSeg, int cExpectedFFs, StTxtPara para)
			{
				List<int> currentSegments = para.Segments;
				IVwVirtualHandler vh;
				if (Cache.TryGetVirtualHandler(m_vtagSegFF, out vh) &&
					!(vh as BaseFDOPropertyVirtualHandler).IsPropInCache(Cache.MainCacheAccessor,
						currentSegments[iSeg], 0))
				{
					// make sure we've cached the paragraph segment ff data
					LoadParagraphSegFFData(para);
				}
				int[] segFFs = Cache.GetVectorProperty(currentSegments[iSeg], m_vtagSegFF, true);
				Assert.AreEqual(cExpectedFFs, segFFs.Length, String.Format("Segment {0} missing expected number of freeform annotations.", iSeg));
			}

			internal void ValidateSegAndFFsWereDeleted(int iSegBeforeSegBreakEdit)
			{
				ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(Cache, segmentsBeforeSegBreakEdit[iSegBeforeSegBreakEdit],
					typeof(CmBaseAnnotation), false, false)
					as ICmBaseAnnotation;
				// if cba was a dummy, this should fail just like an invalid one.
				Assert.IsFalse(cba.IsRealObject, String.Format("segment {0} should be invalid", cba.Hvo));

				List<int> segmentFFComments = segmentFFCommentsBeforeSegBreakEdit[iSegBeforeSegBreakEdit];
				foreach (int hvoFF in segmentFFComments)
				{
					ICmIndirectAnnotation cia = CmIndirectAnnotation.CreateFromDBObject(Cache, ffsClonedToReal[hvoFF],
						typeof(CmIndirectAnnotation), false, false) as ICmIndirectAnnotation;
					Assert.IsFalse(cia.IsValidObject(), String.Format("segment ff annotation {0} should be invalid", cia.Hvo));
				}
			}

		}

		/// <summary>
		/// 1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteSegmentBreakToMerge2Sentences()
		{
			// get segment annotations for affected sentences.

			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara))
			{
				base.DeleteSegmentBreakToMerge2Sentences();

				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(2);
				helper.ValidateSegFFCount(0, 4); // merge will result in one extra ff, because we don't merge notes.
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateMergedFreeformAnnotations(0, 1);

				// test that the last sentence ff annotation was unaffected.
				helper.ValidateMatchingFreeformAnnotations(2, 1);

				// TODO: Check to make sure Freeform Annotation Content status gets flagged as Unfinished.
			}

		}

		[Test]
		public override void InsertAndDeleteSegmentBreakAfter1stWord()
		{
			base.InsertAndDeleteSegmentBreakAfter1stWord();
		}

		protected override void InsertSegmentBreakAfter1stWord(StTxtPara para0, out TextSelInfo tsiBeforeIns)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				helper.ValidateSegFFCount(0, 3);
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateSegFFCount(2, 3);

				base.InsertSegmentBreakAfter1stWord(para0, out tsiBeforeIns);
				// we split the first segment into two, resulting in one more segment.
				ValidateInsertSegmentBreakAfter1stWord(helper);
			}
		}

		private void ValidateInsertSegmentBreakAfter1stWord(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(4);
			// validate that the first segment is still the one that has the ff annotations.
			helper.ValidateSegFFCount(0, 3);
			helper.ValidateSegFFCount(1, 0);
			helper.ValidateSegFFCount(2, 3);
			helper.ValidateSegFFCount(3, 3);
			helper.ValidateMatchingFreeformAnnotations(0, 0);
			helper.ValidateMatchingFreeformAnnotations(1, 2);
			helper.ValidateMatchingFreeformAnnotations(2, 3);
		}

		protected override void DeleteSegmentBreakAfter1stWord(StTxtPara para0, out TextSelInfo tsiBeforeBackspace, out TextSelInfo tsiAfterBackspace)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.DeleteSegmentBreakAfter1stWord(para0, out tsiBeforeBackspace, out tsiAfterBackspace);

				// validate that the first segment is still the one that has the ff annotations.
				// we should be back to the initial state
				ValidateDeleteSegmentBreakAfter1stWord(helper);
			}
		}

		private static void ValidateDeleteSegmentBreakAfter1stWord(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
			helper.ValidateSegFFCount(0, 3);
			helper.ValidateSegFFCount(1, 3);
			helper.ValidateSegFFCount(2, 3);
			helper.ValidateMatchingFreeformAnnotations(0, 0);
			helper.ValidateMatchingFreeformAnnotations(2, 1);
			helper.ValidateMatchingFreeformAnnotations(3, 2);
		}

		protected override void UndoDeleteSegmentBreakAfter1stWord(TextSelInfo tsiBeforeBackspace)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeBackspace.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoDeleteSegmentBreakAfter1stWord(tsiBeforeBackspace);
				// we should be back to the state after we inserted the new segment break
				ValidateInsertSegmentBreakAfter1stWord(helper);
			}
		}

		protected override void UndoInsertSegmentBreakAfter1stWord(TextSelInfo tsiBeforeIns)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeIns.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoInsertSegmentBreakAfter1stWord(tsiBeforeIns);
				ValidateDeleteSegmentBreakAfter1stWord(helper);
			}
		}

		protected override void RedoInsertSegmentBreakAfter1stWord(TextSelInfo tsiBeforeBackspace)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeBackspace.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoInsertSegmentBreakAfter1stWord(tsiBeforeBackspace);
				ValidateInsertSegmentBreakAfter1stWord(helper);
			}
		}

		protected override void RedoDeleteSegmentBreakAfter1stWord(TextSelInfo tsiAfterBackspace)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiAfterBackspace.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoDeleteSegmentBreakAfter1stWord(tsiAfterBackspace);
				ValidateDeleteSegmentBreakAfter1stWord(helper);
			}
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void DeleteRangeSpanningSegmentBoundaryAndWholeWords()
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara))
			{
				base.DeleteRangeSpanningSegmentBoundaryAndWholeWords();

				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(2);
				helper.ValidateSegFFCount(0, 4); // merge will result in one extra ff, because we don't merge notes.
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateMergedFreeformAnnotations(0, 1);

				// test that the freeform annotations in segment 2 remained unchanged.
				helper.ValidateMatchingFreeformAnnotations(2, 1);

				// TODO: Check to make sure Freeform Annotation Content status gets flagged as Unfinished.
			}
		}

		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira? xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira, xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// </summary>
		[Test]
		public override void ReplaceSegBreakWithAnotherSegBreakThenWithNonSegBreak()
		{
			base.ReplaceSegBreakWithAnotherSegBreakThenWithNonSegBreak();
		}

		protected override void ReplaceSegBreakWithAnotherSegBreak(StTxtPara para0, out TextSelInfo tsiBeforeIns1, out TextSelInfo tsiAfterIns1)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.ReplaceSegBreakWithAnotherSegBreak(para0, out tsiBeforeIns1, out tsiAfterIns1);
				// shouldn't be any changes
				ValidateSegBreakEditDidNotChangeState(helper);
			}
		}

		private static void ValidateSegBreakEditDidNotChangeState(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
			helper.ValidateSegFFCount(0, 3);
			helper.ValidateSegFFCount(1, 3);
			helper.ValidateSegFFCount(2, 3);
			helper.ValidateMatchingFreeformAnnotations(0, 0);
			helper.ValidateMatchingFreeformAnnotations(1, 1);
			helper.ValidateMatchingFreeformAnnotations(2, 2);
		}

		protected override void ReplaceSegmentBreakWithNonSegmentBreak(StTxtPara para0, out TextSelInfo tsiBeforeIns2, out TextSelInfo tsiAfterIns2)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.ReplaceSegmentBreakWithNonSegmentBreak(para0, out tsiBeforeIns2, out tsiAfterIns2);
				ValidateReplaceSegmentBreakWithNonSegmentBreakResultedInMerge(helper);
			}
		}

		private static void ValidateReplaceSegmentBreakWithNonSegmentBreakResultedInMerge(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(2);
			helper.ValidateSegFFCount(0, 4); // merge will result in one extra ff, because we don't merge notes.
			helper.ValidateSegFFCount(1, 3);
			helper.ValidateMergedFreeformAnnotations(0, 1);
			helper.ValidateMatchingFreeformAnnotations(2, 1);
		}

		protected override void UndoReplaceSegmentBreakwithNonSegmentBreak(TextSelInfo tsiBeforeIns2)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeIns2.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoReplaceSegmentBreakwithNonSegmentBreak(tsiBeforeIns2);
				// we should be back to non-merged state.
				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
				helper.ValidateSegFFCount(0, 3);
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateSegFFCount(2, 3);
				// only the last segment should match
				helper.ValidateMatchingFreeformAnnotations(1, 2);
			}
		}

		protected override void UndoReplaceSegBreakWithAnotherSegBreak(TextSelInfo tsiBeforeIns1)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeIns1.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoReplaceSegBreakWithAnotherSegBreak(tsiBeforeIns1);
				// undoing from non-merged state to initial state, shouldn't result in any changes.
				ValidateSegBreakEditDidNotChangeState(helper);
			}
		}

		protected override void RedoReplaceSegBreakWithAnotherSegBreak(TextSelInfo tsiAfterIns1)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiAfterIns1.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoReplaceSegBreakWithAnotherSegBreak(tsiAfterIns1);
				ValidateSegBreakEditDidNotChangeState(helper);
			}
		}

		protected override void RedoReplaceSegmentBreakWithNonSegmentBreak(TextSelInfo tsiAfterIns2)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiAfterIns2.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoReplaceSegmentBreakWithNonSegmentBreak(tsiAfterIns2);
				ValidateReplaceSegmentBreakWithNonSegmentBreakResultedInMerge(helper);
			}
		}

		[Test]
		public override void DeleteFirstTwoSentences()
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara))
			{
				base.DeleteFirstTwoSentences();

				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(1);
				helper.ValidateSegFFCount(0, 3);
				// validate the last sentence ff annotation was unaffected.
				helper.ValidateMatchingFreeformAnnotations(2, 0);
				// validate the ffs of the first two segments have been deleted.
				helper.ValidateSegAndFFsWereDeleted(0);
				helper.ValidateSegAndFFsWereDeleted(1);
			}
		}

		[Test]
		public override void DeleteAllSentencesResultingInEmptyPara()
		{
			base.DeleteAllSentencesResultingInEmptyPara();
		}

		[Test]
		public override void ReplaceSentencesWithSentence()
		{
			base.ReplaceSentencesWithSentence();
		}

		protected override void ReplaceSentencesWithSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.ReplaceSentencesWithSentence(para0, out tsiBeforeIns, out tsiAfterIns);

				ValidateReplaceSentencesWithSentence(helper);
			}
		}

		private static void ValidateReplaceSentencesWithSentence(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(1);
			helper.ValidateSegFFCount(0, 0);
			// validate all previous segments and ffs were deleted
			helper.ValidateSegAndFFsWereDeleted(0);
			helper.ValidateSegAndFFsWereDeleted(1);
			helper.ValidateSegAndFFsWereDeleted(2);
		}

		protected override void UndoReplaceSentencesWithSentence(TextSelInfo tsiBeforeIns)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeIns.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoReplaceSentencesWithSentence(tsiBeforeIns);
				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
				helper.ValidateSegFFCount(0, 3);
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateSegFFCount(2, 3);
				// validate all previous segments and ffs were deleted
				helper.ValidateSegAndFFsWereDeleted(0);
			}
		}

		protected override void RedoReplaceSentencesWithSentence(TextSelInfo tsiAfterIns)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiAfterIns.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoReplaceSentencesWithSentence(tsiAfterIns);
				ValidateReplaceSentencesWithSentence(helper);
			}
		}

		[Test]
		public override void ReplaceSentenceWithSameSizeSentence()
		{
			base.ReplaceSentenceWithSameSizeSentence();
		}

		protected override void ReplaceSentenceWithSameSizeSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.ReplaceSentenceWithSameSizeSentence(para0, out tsiBeforeIns, out tsiAfterIns);
				ValidateReplaceSentenceWithSameSizeSentence(helper);
			}
		}

		private static void ValidateReplaceSentenceWithSameSizeSentence(SegmentFreeFormAnnotationValidationHelper helper)
		{
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
			helper.ValidateSegFFCount(0, 0);
			helper.ValidateSegFFCount(1, 3);
			helper.ValidateSegFFCount(2, 3);
			helper.ValidateSegAndFFsWereDeleted(0);
			helper.ValidateMatchingFreeformAnnotations(1, 1);
			helper.ValidateMatchingFreeformAnnotations(2, 2);
		}

		protected override void UndoReplaceSentenceWithSameSizeSentence(TextSelInfo tsiBeforeIns)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiBeforeIns.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoReplaceSentenceWithSameSizeSentence(tsiBeforeIns);
				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
				helper.ValidateSegFFCount(0, 3);
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateSegFFCount(2, 3);
				helper.ValidateSegAndFFsWereDeleted(0);
				helper.ValidateMatchingFreeformAnnotations(1, 1);
				helper.ValidateMatchingFreeformAnnotations(2, 2);
			}
		}

		protected override void RedoReplaceSentenceWithSameSizeSentence(TextSelInfo tsiAfterIns)
		{
			StTxtPara para0 = new StTxtPara(Cache, tsiAfterIns.HvoAnchor);
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoReplaceSentenceWithSameSizeSentence(tsiAfterIns);
				ValidateReplaceSentenceWithSameSizeSentence(helper);
			}
		}

		[Test]
		public override void InsertAndDeleteParagraphBreak()
		{
			base.InsertAndDeleteParagraphBreak();
		}

		protected override void InsertParagraphBreakAfterFirstSegment(StTxtPara para0, out TextSelInfo tsiBeforeEnter, out TextSelInfo tsiAfterEnter)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.InsertParagraphBreakAfterFirstSegment(para0, out tsiBeforeEnter, out tsiAfterEnter);
				ValidateInsertParagraphBreakAfterFirstSegment(helper, tsiAfterEnter);
			}
		}

		void ValidateInsertParagraphBreakAfterFirstSegment(SegmentFreeFormAnnotationValidationHelper helper, TextSelInfo tsiAfterEnter)
		{
			// validate the existing paragraph.
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(1);
			helper.ValidateSegFFCount(0, 3);
			helper.ValidateMatchingFreeformAnnotations(0, 0);
			// now validate against the new paragraph.
			StTxtPara para1 = new StTxtPara(Cache, tsiAfterEnter.HvoAnchor);
			helper.CaptureAndValidateSegmentsAfterSegBreakEdit(2, para1);
			helper.ValidateSegFFCount(0, 3, para1);
			helper.ValidateSegFFCount(1, 3, para1);
			helper.ValidateMatchingFreeformAnnotations(1, 0);
			helper.ValidateMatchingFreeformAnnotations(2, 1);
		}

		protected override void DeleteParagraphBreakViaBackspace(StTxtPara para1, StTxtPara para0, out TextSelInfo tsiAfterBackspace)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper0 = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				using (SegmentFreeFormAnnotationValidationHelper helper1 = new SegmentFreeFormAnnotationValidationHelper(para1))
				{
					base.DeleteParagraphBreakViaBackspace(para1, para0, out tsiAfterBackspace);
					ValidateDeleteParagraphBreakViaBackspace(para0, helper0, helper1);
				}
			}
		}

		private static void ValidateDeleteParagraphBreakViaBackspace(StTxtPara para0, SegmentFreeFormAnnotationValidationHelper helper0, SegmentFreeFormAnnotationValidationHelper helper1)
		{
			helper0.CaptureAndValidateSegmentsAfterSegBreakEdit(3);
			helper0.ValidateSegFFCount(0, 3);
			helper0.ValidateSegFFCount(1, 3);
			helper0.ValidateSegFFCount(2, 3);
			helper0.ValidateMatchingFreeformAnnotations(0, 0);

			// validate against the old paragraph
			helper1.CaptureAndValidateSegmentsAfterSegBreakEdit(3, para0);
			helper1.ValidateMatchingFreeformAnnotations(0, 1);
			helper1.ValidateMatchingFreeformAnnotations(1, 2);
		}

		protected override void UndoDeleteParagraphViaBackspace(StTxtPara para0, StTxtPara para1, TextSelInfo tsiBeforeBackspace)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.UndoDeleteParagraphViaBackspace(para0, para1, tsiBeforeBackspace);
				TextSelInfo tsiAfterUndoDeleteParagraphViaBackspace = m_rtp.CurrentSelectionInfo;
				ValidateInsertParagraphBreakAfterFirstSegment(helper, tsiAfterUndoDeleteParagraphViaBackspace);
			}
		}

		protected override void UndoInsertParagraphBreakAfterFirstSegment(StTxtPara para1, StTxtPara para0, TextSelInfo tsiBeforeEnter)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper0 = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				using (SegmentFreeFormAnnotationValidationHelper helper1 = new SegmentFreeFormAnnotationValidationHelper(para1))
				{
					base.UndoInsertParagraphBreakAfterFirstSegment(para1, para0, tsiBeforeEnter);
					ValidateDeleteParagraphBreakViaBackspace(para0, helper0, helper1);
				}
			}
		}

		protected override void RedoInsertParagraphBreakAfterFirstSegment(StTxtPara para0, StTxtPara para1, TextSelInfo tsiAfterEnter)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.RedoInsertParagraphBreakAfterFirstSegment(para0, para1, tsiAfterEnter);
				ValidateInsertParagraphBreakAfterFirstSegment(helper, tsiAfterEnter);
			}
		}

		[Test]
		public override void ReplaceSentenceWithSentences()
		{
			base.ReplaceSentenceWithSentences();
		}


		/// <summary>
		///			  1         2         3         4         5         6         7         8         9
		/// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira. xxxfirstnew xxxsentence. xxxsecondnew xxxsentence. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
		/// This partially addresses one of the comments in LT-5369.
		/// </summary>
		[Test]
		public override void InsertSentencesAfterFirstSentence()
		{
			base.InsertSentencesAfterFirstSentence();
		}

		protected override void InsertSentencesAfterFirstSentence(StTxtPara para0, out TextSelInfo tsiBeforeIns, out TextSelInfo tsiAfterIns)
		{
			using (SegmentFreeFormAnnotationValidationHelper helper = new SegmentFreeFormAnnotationValidationHelper(para0))
			{
				base.InsertSentencesAfterFirstSentence(para0, out tsiBeforeIns, out tsiAfterIns);
				helper.CaptureAndValidateSegmentsAfterSegBreakEdit(5);
				helper.ValidateSegFFCount(0, 3);
				helper.ValidateMatchingFreeformAnnotations(0, 0);
				// Note: Ideally, segment 1 should get pushed out to segment 3,
				// but currently we treat insertions at the beginning of a segment
				// as insertions into the segment (see LT-????).
				helper.ValidateSegFFCount(1, 3);
				helper.ValidateMatchingFreeformAnnotations(1, 1);
				helper.ValidateSegFFCount(2, 0);
				helper.ValidateSegFFCount(3, 0);
				helper.ValidateSegFFCount(4, 3);
				helper.ValidateMatchingFreeformAnnotations(2, 4);
			}
		}
	}
}
