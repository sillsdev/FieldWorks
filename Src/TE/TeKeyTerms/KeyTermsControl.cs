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
// File: KeyTermsControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.TE.TeEditorialChecks;
using Microsoft.Win32;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control containing the tool strip and the tree of key terms.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class KeyTermsControl : CheckControl
	{
		#region Data members
		private KeyTermsViewWrapper m_wrapper;
		private ToolStripButton m_tbbUpdateKeyTermEquivalents;
		private ToolStripButton m_tbbUseAsVern;
		private ToolStripButton m_tbbVernNotAssigned;
		private ToolStripButton m_tbbNotRendered;
		private ToolStripButton m_tbbApplyFilter;
		#endregion

		#region Construction/initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermsControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal KeyTermsControl() : this (null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermsControl"/> class.
		/// </summary>
		/// <param name="sCaption">The caption to use when this control is displayed as a
		/// floating window</param>
		/// <param name="sProject">The name of the current project</param>
		/// ------------------------------------------------------------------------------------
		internal KeyTermsControl(string sCaption, string sProject) : base(sCaption, sProject)
		{
			InitializeComponent();

			m_sepShowOnlyAtTop = new ToolStripSeparator();
			m_ToolStrip.Items.Insert(0, m_sepShowOnlyAtTop);

			AddToolStripButton(0, TeResourceHelper.KeyTermFilterImage,
				TeResourceHelper.GetTmResourceString("kstidApplyFilterToKeyTermsToolTip"));

			m_tbbApplyFilter = m_ToolStrip.Items[0] as ToolStripButton;
			m_ToolStrip.Items.Insert(0, new ToolStripSeparator());

			AddToolStripButton(0, TeResourceHelper.KeyTermNotRenderedImage,
				TeResourceHelper.GetTmResourceString("kstidVernEqNotAssignedToolTip"));

			m_tbbNotRendered = m_ToolStrip.Items[0] as ToolStripButton;

			AddToolStripButton(0, TeResourceHelper.KeyTermIgnoreRenderingImage,
				TeResourceHelper.GetTmResourceString("kstidNotRenderedToolTip"));

			m_tbbVernNotAssigned = m_ToolStrip.Items[0] as ToolStripButton;

			AddToolStripButton(0, TeResourceHelper.KeyTermRenderedImage,
				TeResourceHelper.GetTmResourceString("kstidUseAsVernEqToolTip"));

			m_tbbUseAsVern = m_ToolStrip.Items[0] as ToolStripButton;

			m_ToolStrip.Items.Insert(0, new ToolStripSeparator());
			AddToolStripButton(0, TeResourceHelper.UpdateKeyTermEquivalentsImage,
				TeResourceHelper.GetTmResourceString("kstidUpdateKeyTermEquivalentsToolTip"));
			m_tbbUpdateKeyTermEquivalents = m_ToolStrip.Items[0] as ToolStripButton;
			m_ToolStrip.ItemClicked += new ToolStripItemClickedEventHandler(OnItemClicked);
		}
		#endregion

		#region internal Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the key terms view wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal KeyTermsViewWrapper Wrapper
		{
			get { return m_wrapper; }
			set { m_wrapper = value; }
		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled states of the buttons on the key terms tool strip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void UpdateToolStripButtons()
		{
			UpdateToolStripButtons(m_wrapper.SelectedReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled states of the buttons on the key terms tool strip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void UpdateToolStripButtons(KeyTermRef keyTermRef)
		{
			Debug.Assert(m_wrapper != null,
				"The key terms wrapper must be specified in KeyTermsToolStrip");

			m_tbbUseAsVern.Enabled = m_wrapper.EnableUseAsRendering(keyTermRef);
			m_tbbVernNotAssigned.Enabled = m_wrapper.EnableIgnoreUnrendered(keyTermRef);
			m_tbbNotRendered.Enabled = m_wrapper.EnableRenderingNotAssigned(keyTermRef);
			m_tbbApplyFilter.Checked = m_wrapper.ApplyBookFilter;
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when selected Scripture reference changed in the key terms view.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="refArgs">The <see cref="T:SIL.FieldWorks.TE.ScrRefEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		internal void OnScrReferenceChanged(object sender, ScrRefEventArgs refArgs)
		{
			UpdateToolStripButtons(refArgs == null ?
				m_wrapper.SelectedReference : refArgs.KeyTermRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void OnReferenceListEmptied(object sender, EventArgs e)
		{
			UpdateToolStripButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a VwSelectionChanged event in the key terms draft view.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:SIL.FieldWorks.Common.RootSites.VwSelectionArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		internal void OnSelChangedInDraftView(object sender, VwSelectionArgs e)
		{
			UpdateToolStripButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_ktToolStrip control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnItemClicked(Object sender, ToolStripItemClickedEventArgs e)
		{
			Debug.Assert(m_wrapper != null,
				"The key terms wrapper must be specified in KeyTermsToolStrip");

			// Evaluate the clicked toolstrip item to handle the click event.
			if (e.ClickedItem == m_tbbUseAsVern)
				m_wrapper.AssignVernacularEquivalent();
			else if (e.ClickedItem == m_tbbNotRendered)
				m_wrapper.UnassignVernacularEquivalent();
			else if (e.ClickedItem == m_tbbVernNotAssigned)
				m_wrapper.IgnoreSpecifyingVernacularEquivalent();
			else if (e.ClickedItem == m_tbbApplyFilter)
				m_wrapper.ApplyBookFilter = !m_wrapper.ApplyBookFilter;
			else if (e.ClickedItem == m_tbbUpdateKeyTermEquivalents)
				m_wrapper.UpdateKeyTermEquivalents();
		}

		#endregion

		#region Methods for loading and saving settings.
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Load settings
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected override void OnLoadSettings(RegistryKey key)
		{
			if (key != null)
			{
				KeyTermsTree tree = Content as KeyTermsTree;
				if (tree == null)
					return;

				int hvo = (int)key.GetValue("SelectedKeyTermHvo", -1);
				TreeNode node = tree.FindNodeWithHvo(hvo);
				if (node != null)
					tree.SelectedNode = node;
			}

			base.OnLoadSettings(key);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Save settings
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected override void OnSaveSettings(RegistryKey key)
		{
			KeyTermsTree tree = Content as KeyTermsTree;
			if (tree != null && tree.SelectedNode != null && tree.SelectedNode.Tag != null &&
				tree.SelectedNode.Tag.GetType() == typeof(int))
			{
				key.SetValue("SelectedKeyTermHvo", (int)tree.SelectedNode.Tag);
			}
			base.OnSaveSettings(key);
		}

		#endregion
	}
}
