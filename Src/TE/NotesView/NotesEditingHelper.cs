// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotesEditingHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NotesEditingHelper has methods needed by various notes views.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NotesEditingHelper : FwEditingHelper
	{
		private IScripture m_scr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The DB connection</param>
		/// <param name="callbacks">implementation of <see cref="IEditingCallbacks"/></param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public NotesEditingHelper(FdoCache cache, IEditingCallbacks callbacks, int filterInstance)
			: base(cache, callbacks)
		{
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
		}

		#region IDisposable override
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
			// Must not be run more than once.
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor for the currently edited rootbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal TeNotesVc CurrentNotesVc
		{
			get
			{
				if (Callbacks != null)
				{
					IVwViewConstructor vc;
					int hvo, frag;
					IVwStylesheet styleSheet;
					Callbacks.EditedRootBox.GetRootObject(out hvo, out vc, out frag, out styleSheet);
					return vc as TeNotesVc;
				}
				return null;
			}
		}
		#endregion

		#region Insert Note
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a note referencing the currently selected paragraph.
		/// </summary>
		/// <param name="noteType">Type of note</param>
		/// <param name="startRef">reference at beginning of selection</param>
		/// <param name="endRef">reference at end of selection</param>
		/// <param name="topObj">The object where quoted text begins.</param>
		/// <param name="bottomObj">The object where quoted text ends.</param>
		/// <param name="wsSelector">The writing system selector.</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="tssQuote">The text of the quote.</param>
		/// <returns>The inserted note</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrScriptureNote InsertNote(ICmAnnotationDefn noteType, BCVRef startRef,
			BCVRef endRef, CmObject topObj, CmObject bottomObj, int wsSelector,
			int startOffset, int endOffset, ITsString tssQuote)
		{
			CheckDisposed();
			TeNotesVc notesVc = CurrentNotesVc;

			IScrScriptureNote annotation;
			string sUndo, sRedo;
			int iPos;

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[startRef.Book - 1];

			TeResourceHelper.MakeUndoRedoLabels("kstidInsertAnnotation", out sUndo, out sRedo);
			string sType = noteType.Name.UserDefaultWritingSystem;
			sUndo = string.Format(sUndo, sType);
			sRedo = string.Format(sRedo, sType);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.MainCacheAccessor,
					   Control as IVwRootSite, sUndo, sRedo, false))
			{
				try
				{
					StTxtParaBldr quoteParaBldr = new StTxtParaBldr(m_cache);
					quoteParaBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
					quoteParaBldr.StringBuilder.ReplaceTsString(0, 0, tssQuote);
					annotation = annotations.InsertNote(startRef, endRef, topObj, bottomObj, noteType.Guid,
						wsSelector, startOffset, endOffset, quoteParaBldr, null, null, null,
						out iPos);

					if (notesVc != null)
					{
						// tell the VC that the newly inserted item should be expanded. That will cause
						// the view to be updated to show the new note.
						notesVc.ExpandItem(annotation.Hvo);
						notesVc.ExpandItem(annotation.DiscussionOAHvo);
					}
				}
				catch
				{
					undoTaskHelper.EndUndoTask = false;
					FwApp.App.RefreshAllViews(m_cache);
					throw;
				}
			}

			if (Control != null)
				Control.Focus();

			// Make a selection in the discussion so the user can start to type
			if (notesVc != null && notesVc.NotesSequenceHandler != null)
			{
				// Get the corresponding index in the virtual property.
				iPos = notesVc.NotesSequenceHandler.GetVirtualIndex(annotations.Hvo, iPos);
			}

			IVwRootSite rootSite = Control as IVwRootSite;
			MakeSelectionInNote(notesVc, startRef.Book - 1, iPos, rootSite, true);

			// REVIEW: Do we need to create a synch record?
			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the specified annotation (without scrolling the annotation in
		/// the view).
		/// </summary>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="iResponse">Index of the response (0 if setting the selection in one of
		/// the StJournalText fields rather than in a response.</param>
		/// <param name="noteTag">The tag indicating the field of the annotation where the
		/// selection is to be made.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNote(int bookIndex, int iAnnotation, int iResponse,
			ScrScriptureNote.ScrScriptureNoteTags noteTag)
		{
			MakeSelectionInNote(CurrentNotesVc, true, bookIndex, iAnnotation, iResponse,
				noteTag, Control as IVwRootSite, true);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the discussion of an annotation after first scrolling the
		/// annotation to near the top of the view.
		/// </summary>
		/// <param name="vc">The notes view constructor</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="fNoteIsExpanded">if <c>true</c> make a selection at the start and end so
		/// that the whole annotation can be scrolled into view. if set to <c>false</c> only
		/// make a selection at the start of the annotation.</param>
		/// --------------------------------------------------------------------------------
		internal void MakeSelectionInNote(TeNotesVc vc, int bookIndex, int iAnnotation,
			IVwRootSite rootSite, bool fNoteIsExpanded)
		{
			MakeSelectionInNote(vc, true, bookIndex, iAnnotation, 0,
				ScrScriptureNote.ScrScriptureNoteTags.kflidDiscussion, rootSite, fNoteIsExpanded);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the specified annotation (without scrolling the annotation in
		/// the view).
		/// </summary>
		/// <param name="vc">The notes view constructor</param>
		/// <param name="fScrollNearTop">if set to <c>true</c> scrolls the specified note to a
		/// position near the top of the view.</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="iResponse">Index of the response (0 if setting the selection in one of
		/// the StJournalText fields rather than in a response.</param>
		/// <param name="noteTag">The tag indicating the field of the annotation where the
		/// selection is to be made.</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="fNoteIsExpanded">if <c>true</c> make a selection at the start and end so
		/// that the whole annotation can be scrolled into view. if set to <c>false</c> only
		/// make a selection at the start of the annotation.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNote(TeNotesVc vc, bool fScrollNearTop, int bookIndex,
			int iAnnotation, int iResponse, ScrScriptureNote.ScrScriptureNoteTags noteTag,
			IVwRootSite rootSite, bool fNoteIsExpanded)
		{
			if (vc == null || vc.NotesSequenceHandler == null)
				return;

			SelectionHelper selHelper;
			if (fScrollNearTop)
			{
				// Make an un-installed selection at the top of the annotation in order to scroll the
				// annotation to the top of the view.
				selHelper = new SelectionHelper();
				selHelper.NumberOfLevels = 2;
				selHelper.LevelInfo[0].cpropPrevious = 0;
				selHelper.LevelInfo[0].ich = -1;
				selHelper.LevelInfo[0].ihvo = iAnnotation;
				selHelper.LevelInfo[0].tag = vc.NotesSequenceHandler.Tag;
				selHelper.LevelInfo[0].ws = 0;
				selHelper.LevelInfo[1].cpropPrevious = 0;
				selHelper.LevelInfo[1].ich = -1;
				selHelper.LevelInfo[1].ihvo = bookIndex;
				selHelper.LevelInfo[1].tag = (int)Scripture.ScriptureTags.kflidBookAnnotations;
				selHelper.LevelInfo[1].ws = 0;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, -2);
				selHelper.IchAnchor = 0;
				selHelper.AssocPrev = false;
				selHelper.NumberOfPreviousProps = 2;
				if (fNoteIsExpanded)
				{
					selHelper.SetSelection(rootSite, true, true, VwScrollSelOpts.kssoNearTop);
				}
				else
				{
					// Annotation is collapsed. Only attempt a selection at the start of it.
					selHelper.SetSelection(rootSite, true, true);
					return;
				}
			}
			else
				EnsureNoteIsVisible(vc, bookIndex, iAnnotation, rootSite);

			// Now make the real (installed) selection in the desired field of the annotation.
			bool fIsResponse = (noteTag == ScrScriptureNote.ScrScriptureNoteTags.kflidResponses);
			selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = (int)noteTag;
			selHelper.LevelInfo[1].ihvo = iResponse;
			selHelper.LevelInfo[1].cpropPrevious = (fIsResponse ? 0 : 1);
			selHelper.LevelInfo[2].tag = vc.NotesSequenceHandler.Tag;
			selHelper.LevelInfo[2].ihvo = iAnnotation;
			selHelper.LevelInfo[3].tag = (int)Scripture.ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[3].ihvo = bookIndex;
			selHelper.IchAnchor = 0;
			selHelper.AssocPrev = false;
			selHelper.SetSelection(rootSite, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the selection in in the Scripture reference of the specified annotation.
		/// </summary>
		/// <param name="vc">The vc.</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="notesDataEntryView">The notes data entry view.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNoteRef(TeNotesVc vc, int bookIndex, int iAnnotation,
			NotesDataEntryView notesDataEntryView)
		{
			EnsureNoteIsVisible(vc, bookIndex, iAnnotation, notesDataEntryView);

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 2;
			selHelper.LevelInfo[0].cpropPrevious = 0;
			selHelper.LevelInfo[0].ich = -1;
			selHelper.LevelInfo[0].ihvo = iAnnotation;
			selHelper.LevelInfo[0].tag = vc.NotesSequenceHandler.Tag;
			selHelper.LevelInfo[0].ws = 0;
			selHelper.LevelInfo[1].cpropPrevious = 0;
			selHelper.LevelInfo[1].ich = -1;
			selHelper.LevelInfo[1].ihvo = bookIndex;
			selHelper.LevelInfo[1].tag = (int)Scripture.ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[1].ws = 0;
			selHelper.IchAnchor = 0;
			selHelper.AssocPrev = false;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginRef);

			selHelper.SetSelection(notesDataEntryView, true, true, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the annotation is mostly visible by making an uninstalled selection
		/// toward the end of the modified date.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void EnsureNoteIsVisible(TeNotesVc vc, int bookIndex, int iAnnotation,
			IVwRootSite notesDataEntryView)
		{
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 2;
			selHelper.LevelInfo[0].cpropPrevious = 0;
			selHelper.LevelInfo[0].ich = -1;
			selHelper.LevelInfo[0].ihvo = iAnnotation;
			selHelper.LevelInfo[0].tag = vc.NotesSequenceHandler.Tag;
			selHelper.LevelInfo[0].ws = 0;
			selHelper.LevelInfo[1].cpropPrevious = 0;
			selHelper.LevelInfo[1].ich = -1;
			selHelper.LevelInfo[1].ihvo = bookIndex;
			selHelper.LevelInfo[1].tag = (int)Scripture.ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[1].ws = 0;
			selHelper.AssocPrev = false;

			// Put the selection at the end of the shortest possible date value. It doesn't
			// have to be right at the end, but the closer it is, the more reliable it will
			// be that it is fully scrolled into view.
			selHelper.IchAnchor = 8;

			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
				(int)CmAnnotation.CmAnnotationTags.kflidDateModified);

			selHelper.SetSelection(notesDataEntryView, false, true, VwScrollSelOpts.kssoDefault);
		}

		#endregion

		#region Overrides of EditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the base method to disable the creation of pictures and footnotes in an
		/// annotation.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sTextRep"></param>
		/// <param name="selDst"></param>
		/// <param name="kodt"></param>
		/// ------------------------------------------------------------------------------------
		public override Guid MakeObjFromText(FdoCache cache, string sTextRep, IVwSelection selDst, out int kodt)
		{
			kodt = 0;
			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if the new writing systems should be created as a side-effect
		/// of a paste operation.
		/// </summary>
		/// <param name="wsf">writing system factory containing the new writing systems</param>
		/// <param name="destWs">The destination writing system (writing system used at the
		/// selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			// Determine writing system at selection (destination for paste).
			destWs = 0;
			if (CurrentSelection != null)
				destWs = CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
			if (destWs <= 0)
				destWs = Cache.DefaultAnalWs; // set to default analysis, if 0.

			// Get list of writing system names.
			List<string> wsMissingNames = new List<string>();
			int cws = wsf.NumberOfWs;

			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));

				IWritingSystem ws;
				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					ws = wsf.get_EngineOrNull(vws[iws]);
					if (ws == null)
					{
						// found corrupt writing system--don't want to use any ws in this pasted string
						return PasteStatus.UseDestWs;
					}
					if (Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(ws.IcuLocale) == 0)
						wsMissingNames.Add(ws.LanguageName); // writing system not found in ws factory
				}
			}

			PasteStatus pasteStatus;
			if (wsMissingNames.Count > 0)
			{
				if (!Options.ShowPasteWsChoice)
					return PasteStatus.UseDestWs;

				// Ask user whether to use destination writing system or copy original writing systems.
				LgWritingSystem lgws = new LgWritingSystem(Cache, destWs);
				Debug.Assert(lgws != null);
				using (AddWsFromPastedTextDlg newWsDlg = new AddWsFromPastedTextDlg(
					m_cache.LangProject.Name.BestAnalysisAlternative.Text,
					lgws.Name.BestAnalysisAlternative.Text, wsMissingNames))
				{
					newWsDlg.ShowDialog(Control.FindForm());
					pasteStatus = newWsDlg.PasteStatus;
				}
			}
			else // no missing writing systems in TsString to paste
				pasteStatus = PasteStatus.PreserveWs;

			return pasteStatus;
		}
		#endregion
	}
}
