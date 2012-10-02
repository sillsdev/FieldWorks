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
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.FDO;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrapper for the Editorial Checks view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EditorialChecksViewWrapper: ChecksViewWrapper
	{
		#region Data members
		private string m_sProjectName;
		private FilteredScrBooks m_bookFilter;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		#endregion

		#region Construction/initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="EditorialChecksViewWrapper"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="bookFilter">The book filter.</param>
		/// <param name="draftViewProxy">View proxy used to create the view.</param>
		/// <param name="settingsRegKey">The settings reg key.</param>
		/// <param name="sProjectName">The name of the current project</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The application (needed to get the product name)</param>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksViewWrapper(Control parent, FdoCache cache,
			FilteredScrBooks bookFilter, ViewProxy draftViewProxy, RegistryKey settingsRegKey,
			string sProjectName, IHelpTopicProvider helpTopicProvider, IApp app) :
			base(parent, cache, draftViewProxy, settingsRegKey)
		{
			Name = "EditorialChecks";
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
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
				m_treeContainer.RefreshControlsAfterDocking();
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

			return new EditorialChecksControl(m_cache, m_app, m_bookFilter,
				((ISelectableView)this).BaseInfoBarCaption, m_sProjectName,
				(mainWnd != null ? mainWnd.TMAdapter : null), m_helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the grid control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override UserControl CreateGridControl(FwMainWnd mainWnd)
		{
			return new EditorialChecksRenderingsControl(m_cache, m_bookFilter, mainWnd, m_helpTopicProvider);
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
			if (EditingHelper == null || EditingHelper.EditedRootBox == null)
				return;
			if (e != null && e != CheckingError.Empty)
			{
				EditingHelper.EditedRootBox.DestroySelection();
				IStTxtPara para = e.MyNote.BeginObjectRA as IStTxtPara;
				if (para != null && para.Owner is IStFootnote)
				{
					// Checking error is for text in a footnote. Make sure the footnote pane is open.
					IStFootnote footnote = para.Owner as IStFootnote;
					((IViewFootnotes)DraftView).ShowFootnoteView(footnote);
				}
				else
				{
					((IViewFootnotes)DraftView).FootnoteViewFocused = false;
				}

				// Note: EditingHelper is actually the active editing helper, so the above
				// code can change its value.
				EditingHelper.GoToScrScriptureNoteRef(e.MyNote);
			}

		}
		#endregion
	}
}
