// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EditorialChecksViewWrapper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using Microsoft.Win32;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrapper fot the Editorial Checks view
	/// </summary>
	/// <returns></returns>
	/// ----------------------------------------------------------------------------------------
	public class EditorialChecksViewWrapper: ChecksViewWrapper
	{
		#region Data members
		private string m_sProjectName;
		private FilteredScrBooks m_bookFilter;
		#endregion

		#region Construction/initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="EditorialChecksViewWrapper"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="bookFilter">The book filter.</param>
		/// <param name="viewCreateInfo">Information used to create the view.</param>
		/// <param name="settingsRegKey">The settings reg key.</param>
		/// <param name="sProjectName">The name of the current project</param>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksViewWrapper(Control parent, FdoCache cache,
			FilteredScrBooks bookFilter, object viewCreateInfo, RegistryKey settingsRegKey,
			string sProjectName) : base(parent, cache, viewCreateInfo, settingsRegKey)
		{
			Name = "EditorialChecks";
			m_sProjectName = sProjectName;
			m_bookFilter = bookFilter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Initialize()
		{
			base.Initialize();

			EditorialChecksControl edChkCtrl = m_treeContainer as EditorialChecksControl;
			if (edChkCtrl != null)
			{
				edChkCtrl.RunChecksClick += HandleRunShowErrorsClicked;
				edChkCtrl.ShowChecksClick += HandleRunShowErrorsClicked;
				edChkCtrl.CheckErrorsList = RenderingsControl;
			}

			RenderingsControl.ReferenceChanged += OnRefInGridChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ActivateView()
		{
			bool initialActivation = InitialActivation;
			base.ActivateView();

			if (initialActivation)
				((EditorialChecksControl)m_treeContainer).RefreshHistoryPaneSize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event when the user clicked on the "Run Checks" or
		/// "Show Existing Results" errors buttons on the EditorialChecksControl (i.e. the one
		/// containing the tree control filled with available checks).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleRunShowErrorsClicked(object sender, EventArgs e)
		{
			((IViewFootnotes)DraftView).HideFootnoteView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the check control.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override CheckControl CreateCheckControl()
		{
			FwMainWnd mainWnd = TopLevelControl as FwMainWnd;

			return new EditorialChecksControl(m_cache, m_bookFilter,
				((ISelectableView)this).BaseInfoBarCaption, m_sProjectName,
				(mainWnd != null ? mainWnd.TMAdapter : null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the grid control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override UserControl CreateGridControl(FwMainWnd mainWnd)
		{
			return new EditorialChecksRenderingsControl(m_cache, m_bookFilter, mainWnd);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where key terms view settings are stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override RegistryKey SettingsKey
		{
			get	{ return base.SettingsKey; }
			protected set
			{
				if (value != null)
					base.SettingsKey = value.CreateSubKey("EditorialChecks");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key term rendering view.
		/// </summary>
		/// <remarks>Public for tests</remarks>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksRenderingsControl RenderingsControl
		{
			get
			{
				CheckDisposed();
				return m_gridControl as EditorialChecksRenderingsControl;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets information about the current selected reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckingError SelectedReference
		{
			get
			{
				CheckDisposed();

				if (RenderingsControl == null)
					return CheckingError.Empty;

				return RenderingsControl.SelectedCheckingError;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not there is a range selection in the draft view that is in
		/// the current check reference's reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool RangeSelectionIsInCurrentRef
		{
			get
			{
				if (EditingHelper == null || EditingHelper.CurrentSelection == null)
					return false;

				// If the selection in the draft view is not a range selection, then there is
				// nothing to use.
				if (!EditingHelper.CurrentSelection.Selection.IsRange)
					return false;

				// If the selection is not in the same Scripture reference as the selected key term
				// then don't enable the command.
				return SelectionIsInCurrentRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the current selection in the draft view could be the
		/// vernacular equivalent for the currently selected key term in the rendering view.
		/// That is, it determines whether the selection is in the verse which is the current
		/// reference in the Key Terms References pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool SelectionIsInCurrentRef
		{
			get
			{
				CheckDisposed();

				if (SelectedReference == CheckingError.Empty)
					return false;

				ScrReference[] anchorRefRange = EditingHelper.GetCurrentAnchorRefRange();
				ScrReference[] endRefRange = EditingHelper.GetCurrentEndRefRange();
				return (anchorRefRange[0] <= SelectedReference.BeginRef &&
					anchorRefRange[1] >= SelectedReference.BeginRef &&
					endRefRange[0] <= SelectedReference.EndRef &&
					endRefRange[1] >= SelectedReference.EndRef);
			}
		}
		#endregion

		#region Event handlers
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets called whenever the focused reference in the error pane changes.
		/// We respond by telling the draft view to scroll to and select the text of the new
		/// verse.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -------------------------------------------------------------------------------------
		private void OnRefInGridChanged(object sender, CheckingError e)
		{
			// TE-6691 -- Not fully initialized yet so return
			if (EditingHelper == null)
				return;
			if (e != CheckingError.Empty)
			{
				EditingHelper.EditedRootBox.DestroySelection();

				StTxtPara para = e.BeginObjectRA as StTxtPara;
				if (para != null && para.Owner is StFootnote)
				{
					// Checking error is for text in a footnote. Make sure the footnote pane is open.
					StFootnote footnote = para.Owner as StFootnote;
					((IViewFootnotes)DraftView).ShowFootnoteView(footnote);
				}
				else
				{
					((IViewFootnotes)DraftView).FootnoteViewFocused = false;
				}

				// Note: EditingHelper is actually the active editing helper, so the above
				// code can change its value.
				EditingHelper.GoToScrScriptureNoteRef(e);
			}

		}
		#endregion
	}
}
