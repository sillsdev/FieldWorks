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
// File: KeyTermsViewWrapper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.TE
{
	#region KeyTermRenderingsCreateInfo
	/// <summary>Holds information necessary to create a keyterms rendering view</summary>
	public struct KeyTermRenderingsCreateInfo
	{
		// no explicit information necessary
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds a collection of view windows that make up the Key Terms View
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermsViewWrapper : ChecksViewWrapper
	{
		#region Data members
		/// <summary> </summary>
		protected KeyTermsTree m_ktTree;
		private int m_bookFilterInstance;
		private List<int> m_FilteredBookIds;
		private bool m_fBookFilterApplied;
		private string m_sProjectName;
		private readonly FwStyleSheet m_stylesheet;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private Dictionary<int, Font> m_wsFonts = new Dictionary<int, Font>();
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a KeyTermsViewWrapper
		/// </summary>
		/// <param name="parent">The parent of the split wrapper (can be null). Will be replaced
		/// with real parent later.</param>
		/// <param name="cache">The Cache to give to the views</param>
		/// <param name="draftViewProxy">View proxy for creating the draft view to display in
		/// the lower right corner</param>
		/// <param name="settingsRegKey">The parent control's ISettings registry key</param>
		/// <param name="bookFilterInstance">The book filter instance.</param>
		/// <param name="sProjectName">The name of the current project</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermsViewWrapper(Control parent, FdoCache cache, ViewProxy draftViewProxy,
			RegistryKey settingsRegKey, int bookFilterInstance, string sProjectName,
			FwStyleSheet stylesheet, IHelpTopicProvider helpTopicProvider)
			: base(parent, cache, draftViewProxy, settingsRegKey)
		{
			m_bookFilterInstance = bookFilterInstance;
			Name = "KeyTermsViewWrapper";

			// Set up the right view
			m_rightView.Name = "KeyTermsRightView";
			m_sProjectName = sProjectName;
			m_stylesheet = stylesheet;
			m_helpTopicProvider = helpTopicProvider;
		}
		#endregion

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose, it needs to be handled by fixing the bug.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (Font font in m_wsFonts.Values.Where(font => font != m_ktTree.Font))
					font.Dispose();
			}

			m_wsFonts = null;

			base.Dispose(disposing);
		}

		#endregion // IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where key terms view settings are stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override RegistryKey SettingsKey
		{
			get { return base.SettingsKey; }
			protected set
			{
				if (value != null)
					base.SettingsKey = value.CreateSubKey("KeyTerms");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key term rendering view.
		/// </summary>
		/// <remarks>Public for tests</remarks>
		/// ------------------------------------------------------------------------------------
		public KeyTermRenderingsControl RenderingsControl
		{
			get
			{
				CheckDisposed();
				return m_gridControl as KeyTermRenderingsControl;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a count of the number of references in the renderings display
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int KeyTermReferenceCount
		{
			get
			{
				CheckDisposed();

				if (RenderingsControl == null)
					return 0;

				return RenderingsControl.ReferenceCount;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets information about the current selected reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermRef SelectedReference
		{
			get
			{
				CheckDisposed();

				if (RenderingsControl == null)
					return KeyTermRef.Empty;

				return RenderingsControl.SelectedReference;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets a value indicating whether or not to filter the key term renderings based
		/// on the current scripture book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ApplyBookFilter
		{
			get
			{
				CheckDisposed();
				return m_fBookFilterApplied;
			}
			set
			{
				CheckDisposed();

				if (m_fBookFilterApplied != value)
				{
					m_fBookFilterApplied = value;
					UpdateRenderingsControl();
					((KeyTermsControl)m_treeContainer).UpdateToolStripButtons();
				}
			}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Use as Vernacular Equivalent command by copying the selected text from
		/// the draft view into the rendering for the selected keyterm reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AssignVernacularEquivalent()
		{
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoAssignVern", out undo, out redo);
			using (UndoTaskHelper undoHelper =
				new UndoTaskHelper(m_cache.ActionHandlerAccessor, null, undo, redo))
			{
				AssignVernacularEquivalentInternal();
				undoHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Use as Vernacular Equivalent command by copying the selected text from
		/// the draft view into the rendering for the selected keyterm reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AssignVernacularEquivalentInternal()
		{
			// We may be temporarily changing the text selection:
			IVwSelection selOriginal = null;

			// If the user merely clicked on a word without selecting it, this may be OK.
			// See if the current insertion point can be grown into a selected word:
			IVwSelection sel = m_draftView.EditingHelper.CurrentSelection.Selection.GrowToWord();

			if (sel != null)
			{
				// The user did only click on a word, so temporarily change the selection to the
				// whole word, while storing the pre-existing selection:
				selOriginal = m_draftView.EditingHelper.CurrentSelection.Selection;
				sel.Install();
			}

			KeyTermRef keyTermRef = SelectedReference;

			if (!keyTermRef.IsValid || !ValidKtTextIsSelected(keyTermRef))
				return;

			// Get the selected text from the draft view
			ITsString tss;
			m_draftView.EditingHelper.CurrentSelection.Selection.GetSelectionString(
				out tss, string.Empty);

			// Remove ORCs and chapter & verse numbers.
			string keyTermText = TsStringUtils.GetCleanTextFromTsString(tss, ScrStyleNames.ChapterAndVerse, true,
				m_cache.LanguageWritingSystemFactoryAccessor);

			// See if the word form already exists
			IWfiWordform wordForm = WfiWordformServices.FindOrCreateWordform(m_cache,
				keyTermText, m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem);

			// Change the reference's status and attach the word form to the ChkRef
			keyTermRef.AssignRendering(wordForm);

			// Look through all of the sibling ChkRef items to see if the same word can be
			// automatically assigned because it is found in the text of those verses.
			AutoAssignVernacularEquivalents(keyTermRef.ChkRef, wordForm);

			// If we temporarily changed the selection, restore the original:
			if (selOriginal != null)
				selOriginal.Install();

			((KeyTermsControl)m_treeContainer).UpdateToolStripButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check all of the sibling references for a key term to see if any of the verses
		/// have the same word form that was assigned. If they do, then the status will
		/// be set to auto-assigned.
		/// </summary>
		/// <param name="assignedRef">ChkRef that was explicitly assigned</param>
		/// <param name="wordForm">word for that was assigned to the ChkRef item</param>
		/// ------------------------------------------------------------------------------------
		private void AutoAssignVernacularEquivalents(IChkRef assignedRef, IWfiWordform wordForm)
		{
			int iDummy;
			IChkTerm parent = (IChkTerm)assignedRef.Owner;
			foreach (IChkRef autoAssignRef in parent.OccurrencesOS)
			{
				KeyTermRef ktRef = new KeyTermRef(autoAssignRef);
				if (ktRef.RenderingStatus != KeyTermRenderingStatus.Unassigned)
					continue;

				if (TeEditingHelper.FindTextInVerse(m_scr,
					wordForm.Form.get_String(wordForm.Cache.DefaultVernWs),
					ktRef.RefInCurrVersification, true,
					out iDummy, out iDummy, out iDummy, out iDummy))
				{
					// TODO: update the local view constructor cache
					ktRef.RenderingStatus = KeyTermRenderingStatus.AutoAssigned;
					ktRef.ChkRef.RenderingRA = wordForm;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TODO: currently just calls UpdateKeyTermEquivalents()
		/// </summary>
		/// <param name="tsiBeforeEdit">the text selection information before the user edited
		/// the text.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateKeyTermEquivalents(TextSelInfo tsiBeforeEdit)
		{
			UpdateKeyTermEquivalents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// walk through all the verses in the current book filter, and scan the draft updating
		/// key term renderings and their statuses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateKeyTermEquivalents()
		{
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(ParentForm, m_cache.ThreadHelper))
			{
				progressDlg.Title = TeResourceHelper.GetResourceString("kstidUpdateKeyTermEquivalentsProgressCaption");
				progressDlg.AllowCancel = false;

				progressDlg.RunTask(UpdateKeyTermEquivalents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key term equivalents.
		/// </summary>
		/// <param name="progressDlg">The progress dialog box.</param>
		/// <param name="parameters">Not used</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object UpdateKeyTermEquivalents(IThreadedProgress progressDlg, params object[] parameters)
		{
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoAssignVern", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				UpdateKeyTermEquivalentsInternal(progressDlg);
				undoHelper.RollBack = false;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key term equivalents.
		/// </summary>
		/// <param name="progressDlg">The progress dialog box.</param>
		/// ------------------------------------------------------------------------------------
		protected void UpdateKeyTermEquivalentsInternal(IProgress progressDlg)
		{
			List<InvalidRendering> invalidRenderings = new List<InvalidRendering>();
			try
			{
				// first build a map from verses to the keyterms that should have renderings.
				Set<IChkTerm> chkTerms = m_ktTree.ChkTermsWithRefs;
				if (progressDlg != null)
				{
					progressDlg.Message = TeResourceHelper.GetResourceString("kstidUpdateKeyTermEquivalentsProgressLoading");
					progressDlg.Minimum = 0;
					progressDlg.Maximum = chkTerms.Count;
				}
				Dictionary<int, List<KeyTermRef>> bcvToChkRefs = new Dictionary<int, List<KeyTermRef>>();
				foreach (IChkTerm keyTerm in chkTerms)
				{
					AddChkRefsToBCVmap(keyTerm, ref bcvToChkRefs);
					if (progressDlg != null)
						progressDlg.Step(0);
				}
				// set progress bar to the number of verses to step through.
				if (progressDlg != null)
				{
					progressDlg.Minimum = 0;
					progressDlg.Maximum = bcvToChkRefs.Count;
				}
				// for each keyterm occurrences in each verse, make sure renderings are up to date.
				List<int> sortedKeys = new List<int>(bcvToChkRefs.Keys);
				sortedKeys.Sort();
				foreach (int bcv in sortedKeys)
				{
					// REVIEW (TE-6532): For now, all Key Term Refs in the DB use the Original
					// versisifcation. Should we support other versifications?
					ScrReference currentVerse = new ScrReference(bcv, ScrVers.Original,
						m_scr.Versification);
					if (progressDlg != null)
					{
						progressDlg.Message = String.Format(TeResourceHelper.GetResourceString("kstidUpdateKeyTermEquivalentsProgressMessage"),
							currentVerse.AsString);
					}
					List<KeyTermRef> chkRefsForVerse = bcvToChkRefs[bcv];
					foreach (KeyTermRef keyRef in chkRefsForVerse)
					{
						// skip doing anything about references that have been marked as "Ignore"
						if (keyRef.RenderingStatus == KeyTermRenderingStatus.Ignored)
							continue;
						if (keyRef.ChkRef.RenderingRA != null)
						{
							if (CanFindTextInVerse(keyRef.ChkRef.RenderingRA, currentVerse))
							{
								if (keyRef.RenderingStatus == KeyTermRenderingStatus.Missing)
									keyRef.RenderingStatus = KeyTermRenderingStatus.Assigned;
								continue;
							}
						}
						// if an expected rendering is not found (or there was no previous assignment)
						// see if we can find an alternative rendering to AutoAssign.
						IChkTerm parentKeyTerm = (IChkTerm)keyRef.ChkRef.Owner;
						bool fFound = false;
						foreach (IChkRendering rendering in parentKeyTerm.RenderingsOC)
						{
							if (rendering.SurfaceFormRA == null)
							{
								// We found a surface form that is not defined. Later we'll need to
								// remove this rendering, but for now we'll continue to the next one.
								invalidRenderings.Add(new InvalidRendering(parentKeyTerm, rendering));
								continue;
							}
							if (CanFindTextInVerse(rendering.SurfaceFormRA, currentVerse))
							{
								try
								{
									keyRef.ChkRef.RenderingRA = rendering.SurfaceFormRA;
									if (keyRef.RenderingStatus != KeyTermRenderingStatus.AutoAssigned)
										keyRef.RenderingStatus = KeyTermRenderingStatus.AutoAssigned;
									fFound = true;
									break;
								}
								catch
								{
									// Unable to set rendering because it is invalid.
									invalidRenderings.Add(new InvalidRendering(parentKeyTerm, rendering));
									continue;
								}
							}
						}
						if (!fFound)
						{
							if (keyRef.RenderingStatus == KeyTermRenderingStatus.Assigned &&
								keyRef.ChkRef.RenderingRA != null)
							{
								// keep RenderingsRA info, so we know what is missing.
								keyRef.RenderingStatus = KeyTermRenderingStatus.Missing;
							}
							else
							{
								if (keyRef.ChkRef.RenderingRA != null)
									keyRef.ChkRef.RenderingRA = null;
								if (keyRef.RenderingStatus != KeyTermRenderingStatus.Unassigned)
									keyRef.RenderingStatus = KeyTermRenderingStatus.Unassigned;
							}
						}
					}
					if (progressDlg != null)
						progressDlg.Step(0);
				}
			}
			finally
			{
				if (invalidRenderings.Count > 0)
				{
					// We found at least one invalid surface form, so we need to search through our
					// renderings and remove any that are invalid.
					foreach (InvalidRendering rendering in invalidRenderings)
					{
						// Rendering may be in list twice, so only delete the valid objects
						if (rendering.m_rendering.IsValidObject)
							rendering.m_parentKeyTerm.RenderingsOC.Remove(rendering.m_rendering);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given verse still contains the text of the given wordform.
		/// </summary>
		/// <param name="wfiWordform">The wordform.</param>
		/// <param name="verse">The verse reference.</param>
		/// ------------------------------------------------------------------------------------
		private bool CanFindTextInVerse(IWfiWordform wfiWordform, ScrReference verse)
		{
			int iDummy;
			return TeEditingHelper.FindTextInVerse(m_scr,
				wfiWordform.Form.get_String(wfiWordform.Cache.DefaultVernWs),
				verse, true, out iDummy, out iDummy, out iDummy, out iDummy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not there is a range selection in the draft view that is in
		/// the current check reference's reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsRangeSelectionInKtRef(KeyTermRef keyTermRef)
		{
			CheckDisposed();

			if (EditingHelper == null || EditingHelper.CurrentSelection == null)
				return false;

			// If the selection in the draft view is not a range selection, then there is
			// nothing to use.
			if (!EditingHelper.CurrentSelection.Selection.IsRange)
				return false;

			if (KeyTermRef.IsNullOrEmpty(keyTermRef))
				return false;

			return IsSelectionInRef(keyTermRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the first node in the Key terms tree that corresponds to one of the terms
		/// in the given list, preferring (if possible) one which has an occurrence in the
		/// given reference range.
		/// </summary>
		/// <param name="terms">The terms.</param>
		/// <param name="startRef">The starting Scripture reference.</param>
		/// <param name="endRef">The ending Scripture reference.</param>
		/// <returns><c>true</c> if a matching node is found (even it was already the selected
		/// node); <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool SelectTerm(IEnumerable<IChkTerm> terms, int startRef, int endRef)
		{
			IChkTerm selectedTerm = m_ktTree.SelectedNode.Tag as IChkTerm;
			TreeNode bestNode = null;
			if (selectedTerm != null && terms.Contains(selectedTerm))
			{
				// Already have one of the requested terms selected.
				if (selectedTerm.OccurrencesOS.Any(o => o.Ref >= startRef && o.Ref <= endRef))
					return true;
				// The currently selected node doesn't have an occurrence in range. Remember
				// the current one as the best so far, but see if we can find one that does
				// have an occurrence in range.
				bestNode = m_ktTree.SelectedNode;
			}
			foreach (IChkTerm chkTerm in terms)
			{
				if (chkTerm == selectedTerm)
					continue;
				TreeNode node = m_ktTree.FindNode(chkTerm.Guid);
				if (node != null)
				{
					if (bestNode == null)
						bestNode = node;
					if (chkTerm.OccurrencesOS.Any(o => o.Ref >= startRef && o.Ref <= endRef))
					{
						bestNode = node;
						break;
					}
				}
			}
			if (bestNode != null)
			{
				m_ktTree.SelectedNode = bestNode;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the CkkRef records associated with the given term to the hashtable.
		/// </summary>
		/// <param name="chkTerm">The key term.</param>
		/// <param name="bcvToChkRefs">The BCV to CHK refs.</param>
		/// ------------------------------------------------------------------------------------
		private void AddChkRefsToBCVmap(IChkTerm chkTerm, ref Dictionary<int, List<KeyTermRef>> bcvToChkRefs)
		{
			List<int> filteredBookNum = m_FilteredBookIds;
			// if we don't have a book filter, we only care about
			// updating keyterms for renderings in books we have in our
			// project.
			// NOTE: What do we do about updating renderings for books
			// that have been removed from the project?
			if (filteredBookNum == null)
				filteredBookNum = new List<int>();
			if (filteredBookNum.Count == 0)
			{
				foreach (IScrBook book in m_scr.ScriptureBooksOS)
					filteredBookNum.Add(book.CanonicalNum);
			}

			foreach (IChkRef chkRef in chkTerm.OccurrencesOS)
			{
				if (filteredBookNum != null && filteredBookNum.Count > 0)
				{
					int bookNumOfOccurrence = ScrReference.GetBookFromBcv(chkRef.Ref);
					// if we have a book filter, we only care about mapping for
					// renderings that are showing.
					if (!filteredBookNum.Contains(bookNumOfOccurrence))
						continue;
				}
				if (!bcvToChkRefs.ContainsKey(chkRef.Ref))
				{
					bcvToChkRefs.Add(chkRef.Ref, new List<KeyTermRef>());
				}
				bcvToChkRefs[chkRef.Ref].Add(new KeyTermRef(chkRef));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the command for unassigning vernacular equivalents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UnassignVernacularEquivalent()
		{
			CheckDisposed();

			if (!SelectedReference.IsValid)
				return;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoVernNotAssigned", out undo, out redo);
			using (UndoTaskHelper undoHelper =
				new UndoTaskHelper(m_cache.ActionHandlerAccessor, null, undo, redo))
			{
				// Change the reference's status and clear the word form to the ChkRef
				KeyTermRef keyTerm = SelectedReference;
				keyTerm.RenderingStatus = KeyTermRenderingStatus.Unassigned;
				keyTerm.ChkRef.RenderingRA = null;
				undoHelper.RollBack = false;
			}

			((KeyTermsControl)m_treeContainer).UpdateToolStripButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Ignore Unrendered command by setting the status to "ignore"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void IgnoreSpecifyingVernacularEquivalentInternal()
		{
			CheckDisposed();

			if (!SelectedReference.IsValid)
				return;

			// Change the reference's status and clear the word form (if there is one)
			// to the ChkRef
			KeyTermRef keyTerm = SelectedReference;
			keyTerm.RenderingStatus = KeyTermRenderingStatus.Ignored;
			keyTerm.ChkRef.RenderingRA = null;

			((KeyTermsControl)m_treeContainer).UpdateToolStripButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Ignore Unrendered command by setting the status to "ignore"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void IgnoreSpecifyingVernacularEquivalent()
		{
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoIgnoreVern", out undo, out redo);
			using (UndoTaskHelper undoHelper =
				new UndoTaskHelper(m_cache.ActionHandlerAccessor, null, undo, redo))
			{
				IgnoreSpecifyingVernacularEquivalentInternal();
				undoHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the command should be enabled that allows the user to unassign
		/// a vernacular equivalent (i.e. a rendering).
		/// </summary>
		/// <returns>true to enable, false to disable</returns>
		/// ------------------------------------------------------------------------------------
		public bool EnableRenderingNotAssigned(KeyTermRef keyTermRef)
		{
			CheckDisposed();
			if (keyTermRef == null)
				keyTermRef = SelectedReference;

			// If there are no references displayed, or there is not a current reference, then
			// do not enable the command.
			if (!Visible || KeyTermReferenceCount == 0 || keyTermRef == KeyTermRef.Empty)
				return false;

			// If the current key term reference is unrendered, then enable.
			return (keyTermRef.RenderingStatus != KeyTermRenderingStatus.Unassigned);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the "Use As Rendering" command should be enabled
		/// </summary>
		/// <returns>true if it is valid to enable the command, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool EnableUseAsRendering(KeyTermRef keyTermRef)
		{
			CheckDisposed();
			if (keyTermRef == null)
				keyTermRef = SelectedReference;

			// If there are no references displayed, or there is not a current reference, then
			// do not enable the command.
			if (!Visible || KeyTermReferenceCount == 0 || keyTermRef == KeyTermRef.Empty)
				return false;

			// If the focused window is not the renderings, or the draft view, then don't
			// enable the command.
			if (ActiveCheckingView != CheckView.Grid && ActiveCheckingView != CheckView.Draft)
				return false;

			return ValidKtTextIsSelected(keyTermRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the "Ignore Unrendered" command should be enabled
		/// </summary>
		/// <returns>true to enable, false to disable</returns>
		/// ------------------------------------------------------------------------------------
		public bool EnableIgnoreUnrendered(KeyTermRef keyTermRef)
		{
			CheckDisposed();
			if (keyTermRef == null)
				keyTermRef = SelectedReference;

			// If there are no references displayed, or there is not a current reference, then
			// do not enable the command.
			if (!Visible || KeyTermReferenceCount == 0 || keyTermRef == KeyTermRef.Empty)
				return false;

			// If the current key term reference is unrendered, then enable.
			return (keyTermRef.RenderingStatus != KeyTermRenderingStatus.Ignored);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to selected key terms reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateKeyTermsView()
		{
			CheckDisposed();

			if (SelectedReference != KeyTermRef.Empty)
				EditingHelper.GotoVerse(SelectedReference.RefInCurrVersification);
		}
		#endregion

		#region Event stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the contents of the rendering view (by resetting it's root object) whenever
		/// a different node is selected in the tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ktTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			KeyTermsControl ktCtrl = (KeyTermsControl)m_treeContainer;

			IChkTerm term = e.Node.Tag as IChkTerm;
			if (term != null)
			{
				using (new WaitCursor(TopLevelControl))
				{
					RenderingsControl.LoadRenderingsForKeyTerm(term,
						m_fBookFilterApplied ? m_FilteredBookIds : null);

					ktCtrl.TermDescription = term.Description.UiString;
					ktCtrl.SeeAlso = term.SeeAlso.UiString;
				}
			}
			else
			{
				RenderingsControl.DataGridView.RowCount = 0;
				ktCtrl.TermDescription = null;
				ktCtrl.SeeAlso = null;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms tree view after a book filter change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ApplyBookFilterToKeyTermsTree()
		{
			LoadFilteredBookIds();
			m_ktTree.FilteredBookIds = m_FilteredBookIds;
			m_ktTree.Load();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms tree and rendering view after a book filter change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateBookFilter()
		{
			ApplyBookFilterToKeyTermsTree();
			UpdateRenderingsControl();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms rendering view after a book filter change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateRenderingsControl()
		{
			CheckDisposed();

			Guid guidStoreRef = Guid.Empty;

			try
			{
				KeyTermRef ktref = RenderingsControl.SelectedReference;
				if (ktref != null && ktref != KeyTermRef.Empty)
					guidStoreRef = ktref.ChkRef.Guid;

				if (m_fBookFilterApplied)
					LoadFilteredBookIds();

				ktTree_AfterSelect(this, new TreeViewEventArgs(m_ktTree.SelectedNode));
			}
			finally
			{
				RenderingsControl.SelectKeyTermRef(guidStoreRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the filtered book ids.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadFilteredBookIds()
		{
			FilteredScrBooks filteredScrBooks = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(
				m_bookFilterInstance);

			m_FilteredBookIds = (filteredScrBooks != null ? filteredScrBooks.BookIds : null);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets called whenever the focused reference in the rendering pane changes.
		/// We respond by telling the draft view to scroll to and select the text of the new
		/// verse.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		private void RenderingScrRefChanged(object sender, ScrRefEventArgs e)
		{
			if (!IsRangeSelectionInKtRef(e.KeyTermRef) && EditingHelper != null &&
				EditingHelper.CurrentSelection != null)
			{
				// Check if there's anything displayed in the data grid
				if (e.RefBCV <= 0)
					EditingHelper.GoToFirstBook();
				else
				{
					IWfiWordform wordform = e.KeyTermRef.ChkRef.RenderingRA;
					EditingHelper.SelectVerseText(e.KeyTermRef.RefInCurrVersification,
						wordform == null ? null : wordform.Form.get_String(wordform.Cache.DefaultVernWs));
				}

				((Control)DraftView).Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the grid control.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override UserControl CreateGridControl(FwMainWnd mainWnd)
		{
			KeyTermRenderingsControl control = new KeyTermRenderingsControl(m_cache, mainWnd);
			control.ReferenceChanged += RenderingScrRefChanged;
			return control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the check control.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override CheckControl CreateCheckControl()
		{
			KeyTermsControl ktControl =
				new KeyTermsControl(((ISelectableView)this).BaseInfoBarCaption,	m_sProjectName);

			ktControl.Wrapper = this;
			ktControl.MainPanelContent = m_ktTree;
			ktControl.UpdateToolStripButtons();

			((KeyTermRenderingsControl)m_gridControl).ReferenceChanged += ktControl.OnScrReferenceChanged;
			((KeyTermRenderingsControl)m_gridControl).ReferenceListEmptied += ktControl.OnReferenceListEmptied;
			if (m_draftView is ISelectionChangeNotifier)
				((ISelectionChangeNotifier) m_draftView).VwSelectionChanged += ktControl.OnSelChangedInDraftView;

			return ktControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Initialize()
		{
			if (m_fShownBefore)
				return;

			// Create a key terms tree view
			m_ktTree = new KeyTermsTree(m_cache.LangProject.KeyTermsList);
			m_ktTree.AfterSelect += ktTree_AfterSelect;
			m_ktTree.GetFontForWs = (ws =>
				{
					if (m_stylesheet == null) // Probably in tests
						return m_ktTree.Font;
					Font f;
					if (!m_wsFonts.TryGetValue(ws, out f))
					{
						f = m_stylesheet.GetUiFontForWritingSystem(ws, 0);
						if (f.FontFamily.Name == m_ktTree.Font.FontFamily.Name)
						{
							// This writing system uses the same font as the control was going to use anyway,
							// so just use that font (this avoids overriding the font size so our UI is more
							// consistent).
							f.Dispose();
							f = m_ktTree.Font;
						}
						m_wsFonts[ws] = f;
					}
					return f;
				});

			base.Initialize();

			if (m_ktTree.Nodes.Count == 0)
				ApplyBookFilterToKeyTermsTree();
		}
		#endregion

		#region Message and Update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Use as Vernacular Equivalent" command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnNotYetRendered(object args)
		{
			UnassignVernacularEquivalent();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Use as Rendering" menu item command.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUseAsRendering(object args)
		{
			AssignVernacularEquivalent();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Ignore Unrendered Term" menu item command.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnIgnoreUnrendered(object args)
		{
			IgnoreSpecifyingVernacularEquivalent();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Apply filter to key terms list" menu item command.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnApplyFilterToKeyTerms(object args)
		{
			ApplyBookFilter = !ApplyBookFilter;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Find Key Term" menu item command.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFindKeyTerm(object args)
		{
			((KeyTermsControl)m_treeContainer).ShowFindKeyTermControl();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Key Terms menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportKeyTerms(object args)
		{
			return UpdateImportExport(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Import/Key Terms menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileImportKeyTerms(object args)
		{
			return UpdateImportExport(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the Import or Export Key Terms menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool UpdateImportExport(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;
			itemProps.Visible = true;
			itemProps.Enabled = true;
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export Key Terms menu handler
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportKeyTerms(object args)
		{
			FilteredScrBooks filteredScrBooks = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(
				m_bookFilterInstance);

			int defaultBook = (SelectedReference.ChkRef != null) ?
				BCVRef.GetBookFromBcv(SelectedReference.ChkRef.Ref) : 0;
			using (ExportXmlDialog dlg = new ExportXmlDialog(m_cache, filteredScrBooks, defaultBook,
				m_stylesheet, FileType.OXEKT, m_helpTopicProvider))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					XmlTermRenderingsList terms = new XmlTermRenderingsList(m_cache.LangProject);
					terms.SerializeToFile(dlg.FileName);
					MessageBox.Show(Properties.Resources.kstidOxektExportCompleteMsg,
						Properties.Resources.kstidOxektExportTitle, MessageBoxButtons.OK);
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import Key Terms menu handler
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileImportKeyTerms(object args)
		{
			CheckDisposed();
			using (TeImportExportFileDialog dlg = new TeImportExportFileDialog(m_cache.ProjectId.Name, FileType.OXEKT))
			{
				if (dlg.ShowOpenDialog(null, this) == DialogResult.OK)
				{
					string sUndo, sRedo;
					TeResourceHelper.MakeUndoRedoLabels("kstidImportKeyTerms", out sUndo, out sRedo);

					using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
					   null, sUndo, sRedo))
					{
						using (WaitCursor waitCursor = new WaitCursor(this))
						{

							Exception e;
							XmlTermRenderingsList.LoadFromFile(dlg.FileName, m_cache,
								(occurrence, existingRendering, importedRendering) =>
								{
									using (ResolveKeyTermRenderingImportConflictDlg resolver =
										new ResolveKeyTermRenderingImportConflictDlg(this, occurrence, existingRendering, importedRendering, m_stylesheet))
									{
										return resolver.UseImportedRendering;
									}
								},
								out e);
							if (e != null)
							{
								// Something went wrong while importing so let the user know.
								MessageBox.Show(string.Format(Properties.Resources.kstidOxektImportFailedMsg, e.Message),
												Properties.Resources.kstidOxektImportTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
								return true;
							}
							undoHelper.RollBack = false;
							MessageBox.Show(Properties.Resources.kstidOxektImportCompleteMsg,
								Properties.Resources.kstidOxektImportTitle, MessageBoxButtons.OK);
						}
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the "Update key term equivalents" menu item command.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateKeyTermEquivalents(object args)
		{
			UpdateKeyTermEquivalents();
			return true;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the "Use as Vernacular Equivalent" command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateNotYetRendered(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled =
						(itemProps.Name.StartsWith("cmnu") && !IsMouseOverGridRows ?
						false : EnableRenderingNotAssigned(null));
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the "Use as Vernacular Equivalent" command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateUseAsRendering(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = EnableUseAsRendering(null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the "Ignore Unrendered" command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateIgnoreUnrendered(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled =
						(itemProps.Name.StartsWith("cmnu") && !IsMouseOverGridRows ?
						false : EnableIgnoreUnrendered(null));

					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the "Apply filter to key terms list" menu/toolbar item
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateApplyFilterToKeyTerms(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Update = true;
					itemProps.Enabled = Visible;
					itemProps.Checked = ApplyBookFilter;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw;
#else
				return false; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the mouse is currently over rows in the
		/// key term's data grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsMouseOverGridRows
		{
			get
			{
				KeyTermRenderingsControl ctrl = RenderingsControl;
				if (ctrl == null)
					return false;

				Point pt = ctrl.DataGridView.PointToClient(Control.MousePosition);
				DataGridView.HitTestInfo hti = ctrl.DataGridView.HitTest(pt.X, pt.Y);
				return (hti.RowIndex >= 0);
			}
		}
		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not there is a range selection in the draft view that is in
		/// the current check reference's reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsKeyTermInRangeSelection(KeyTermRef ktRef)
		{
			if (EditingHelper == null || EditingHelper.CurrentSelection == null)
				return false;

			// If the selection in the draft view is not a range selection, or cannot be grown
			// into one, then there is nothing to use.
			if (!EditingHelper.CurrentSelection.Selection.IsRange)
			{
				IVwSelection sel = EditingHelper.CurrentSelection.Selection.GrowToWord();
				if (sel == null)
					return false;
			}

			// If the selection is not in the same Scripture reference as the selected key term
			// then don't enable the command.
			return IsSelectionInRef(ktRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not there is a range selection consisting of at least one
		/// "word" (non-whitespace) in the draft view that is in the current check reference's
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidKtTextIsSelected(KeyTermRef ktRef)
		{
			if (!IsKeyTermInRangeSelection(ktRef))
				return false;

			// The user is allowed to merely click on a word; they don't have to select it.
			// Therefore, we have to see if there is a range selection, and if not, see if
			// we can grow the selection into a word.
			ITsString tss = null;
			if (EditingHelper.CurrentSelection.Selection.IsRange)
			{
				EditingHelper.CurrentSelection.Selection.GetSelectionString(out tss,
					string.Empty);
			}
			else
			{
				IVwSelection sel = EditingHelper.CurrentSelection.Selection.GrowToWord();
				if (sel != null)
					sel.GetSelectionString(out tss, string.Empty);
			}

			return IsValidTSS(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given TSS is valid for use as a vernacular equivalent.
		/// </summary>
		/// <param name="tss">TSS to be checked</param>
		/// <returns>
		/// 	<c>true</c> if TSS is valid; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsValidTSS(ITsString tss)
		{
			if (tss == null)
				return false;
			String keyterm = TsStringUtils.GetCleanTextFromTsString(tss,
				ScrStyleNames.ChapterAndVerse, true, m_cache.LanguageWritingSystemFactoryAccessor);
			return (keyterm != string.Empty && keyterm.Length <= WfiWordformTags.kMaxWordformLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the current selection in the draft view is entirely contained
		/// in the verse which is the given reference.
		/// </summary>
		/// <param name="ktRef">The key terms reference.</param>
		/// ------------------------------------------------------------------------------------
		private bool IsSelectionInRef(KeyTermRef ktRef)
		{
			CheckDisposed();

			if (KeyTermRef.IsNullOrEmpty(ktRef))
				throw new ArgumentException("Parameter must not be null or empty", "ktRef");

			return IsRangeInKtRef(ktRef, EditingHelper.CurrentAnchorRefRange,
				EditingHelper.CurrentEndRefRange);
		}

		#endregion

		#region Static Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified key term reference falls within the given range of
		/// references. Normally this will only return <c>true</c> if the anchor and end are the
		/// same. That is to say, the selection represented by the given ranges must fall wholly
		/// within a single verse. (There is an anomalous case where it could succeed If the
		/// range crosses a verse number (or bridge) such that the end reference...?
		/// </summary>
		/// <param name="keyTermRef">The key term ref.</param>
		/// <param name="anchorRefRange">The anchor ref range (which is made up of two
		/// references to account for possible verse bridges).</param>
		/// <param name="endRefRange">The end ref range (which is made up of two
		/// references to account for possible verse bridges).</param>
		/// <returns>
		/// 	<c>true</c> if the specified key term ref falls within the given range;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsRangeInKtRef(KeyTermRef keyTermRef, ScrReference[] anchorRefRange,
			ScrReference[] endRefRange)
		{
			ScrReference selectedKtRef = keyTermRef.RefInCurrVersification;
			return (anchorRefRange[0] <= selectedKtRef && anchorRefRange[1] >= selectedKtRef &&
				endRefRange[0] == anchorRefRange[0] && endRefRange[1] == anchorRefRange[1]);
		}
		#endregion

		#region Overrides of ChecksViewWrapper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ActivateView()
		{
			base.ActivateView();
			Application.AddMessageFilter(m_ktTree);
			UpdateKeyTermsView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The view is being deactivated, so stop pre-filtering messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DeactivateView()
		{
			base.DeactivateView();
			Application.RemoveMessageFilter(m_ktTree);
		}
		#endregion

		#region Save and Load Settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSaveSettings(RegistryKey key)
		{
			base.OnSaveSettings(key);

			if (key != null)
				key.SetValue("BookFilterApplied", ApplyBookFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void OnLoadSettings(RegistryKey key)
		{
			base.OnLoadSettings(key);

			if (key != null)
			{
				string value = key.GetValue("BookFilterApplied", "false") as string;
				bool apply;
				if (bool.TryParse(value, out apply))
					ApplyBookFilter = apply;
			}
		}
		#endregion
	}

	#region class InvalidRendering
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Class to contain invalid renderings and the key term where the invalid rendering occurred.
	/// There may be multiple invalid renderings per key term, so we cannot use a dictionary.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class InvalidRendering
	{
		public IChkTerm m_parentKeyTerm;
		public IChkRendering m_rendering;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidRendering"/> class.
		/// </summary>
		/// <param name="parentKeyTerm">The owner of the key term.</param>
		/// <param name="rendering">The invalid rendering.</param>
		/// ------------------------------------------------------------------------------------
		public InvalidRendering(IChkTerm parentKeyTerm, IChkRendering rendering)
		{
			m_parentKeyTerm = parentKeyTerm;
			m_rendering = rendering;
		}
	}
	#endregion
}
