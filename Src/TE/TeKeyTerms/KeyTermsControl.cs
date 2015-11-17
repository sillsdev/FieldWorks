// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermsControl.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
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
		private readonly ToolStripButton m_tbbUpdateKeyTermEquivalents;
		private readonly ToolStripButton m_tbbUseAsVern;
		private readonly ToolStripButton m_tbbVernNotAssigned;
		private readonly ToolStripButton m_tbbNotRendered;
		private readonly ToolStripButton m_tbbApplyFilter;
		private readonly ToolStripButton m_tbbFindKeyTerm;
		private readonly string m_fmtSeeAlso;

		private ToolStripDropDown m_findDropDown;
		private ToolStripComboBox m_cboFind;
		#endregion

		#region Construction/initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermsControl"/> class.
		/// </summary>
		/// <param name="sCaption">The caption to use when this control is displayed as a
		/// floating window</param>
		/// <param name="sProject">The name of the current project</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ToolStripSeparator gets added to toolstrip and disposed there")]
		internal KeyTermsControl(string sCaption, string sProject) : base(sCaption, sProject)
		{
			InitializeComponent();

			m_fmtSeeAlso = lblSeeAlso.Text;

			m_sepShowOnlyAtTop = new ToolStripSeparator();
			m_ToolStrip.Items.Insert(0, m_sepShowOnlyAtTop);

			AddToolStripButton(0, TeResourceHelper.FindKeyTermImage,
				TeResourceHelper.GetTmResourceString("kstidFindKeyTermToolTip"));

			m_tbbFindKeyTerm = m_ToolStrip.Items[0] as ToolStripButton;

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
			m_ToolStrip.ItemClicked += OnItemClicked;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the term description information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string TermDescription
		{
			set
			{
				lblDescription.Text = value;
				lblDescription.Visible = !(string.IsNullOrEmpty(value));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the see also information for the term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string SeeAlso
		{
			set
			{
				lblSeeAlso.Text = string.Format(m_fmtSeeAlso, value);
				lblSeeAlso.Visible = !(string.IsNullOrEmpty(value));
			}
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
			else if (e.ClickedItem == m_tbbFindKeyTerm)
				ShowFindKeyTermControl();
			else if (e.ClickedItem == m_tbbUpdateKeyTermEquivalents)
				m_wrapper.UpdateKeyTermEquivalents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the find key term control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ShowFindKeyTermControl()
		{
			if (m_findDropDown == null)
			{
				m_findDropDown = new ToolStripDropDown();
				m_cboFind = new ToolStripComboBox();
				m_cboFind.ComboBox.MinimumSize = new Size(300, m_cboFind.ComboBox.Height);
				m_cboFind.DropDownStyle = ComboBoxStyle.DropDown;
				m_findDropDown.Items.Add(m_cboFind);
				m_cboFind.KeyPress += FindComboKeyPress;
			}
			else if (m_findDropDown.Items.Count == 2)
				m_findDropDown.Items.RemoveAt(1);
			m_findDropDown.Show(m_ToolStrip, m_tbbFindKeyTerm.Bounds.Left, m_tbbFindKeyTerm.Bounds.Bottom);
			m_cboFind.ComboBox.SelectAll();
			//m_cboFind.ComboBox.Select();
			m_cboFind.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the combo key press.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.KeyPressEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripLabel is added to m_findDropDown.Items collection and disposed there")]
		private void FindComboKeyPress(object sender, KeyPressEventArgs e)
		{
			if (m_findDropDown.Items.Count == 2)
				m_findDropDown.Items.RemoveAt(1);

			if (e.KeyChar != '\r' || string.IsNullOrEmpty(m_cboFind.Text))
				return;

			e.Handled = true;

			if (m_cboFind.SelectedIndex < 0)
				m_cboFind.Items.Add(m_cboFind.Text);
			KeyTermsTree tree = (KeyTermsTree)MainPanelContent;
			KeyTermsTree.FindResult result = tree.FindNextMatch(m_cboFind.Text);
			if (result == KeyTermsTree.FindResult.MatchFound)
				m_findDropDown.Hide();
			else
			{
				ToolStripLabel msg = new ToolStripLabel(result == KeyTermsTree.FindResult.NoMatchFound ?
					Properties.Resources.kstidNoMatchFound : Properties.Resources.kstidNoMoreMatches);
				msg.ForeColor = Color.Red;
				m_findDropDown.Items.Add(msg);
			}
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
				KeyTermsTree tree = MainPanelContent as KeyTermsTree;
				if (tree == null)
					return;

				Guid guid = Guid.Empty;
				try
				{
					guid = new Guid((string)key.GetValue("SelectedKeyTerm", string.Empty));

				}
				catch
				{
				}
				TreeNode node = tree.FindNode(guid);
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
			KeyTermsTree tree = MainPanelContent as KeyTermsTree;
			if (tree != null && tree.SelectedNode != null && tree.SelectedNode.Tag != null &&
				tree.SelectedNode.Tag is IChkTerm)
			{
				key.SetValue("SelectedKeyTerm", ((IChkTerm)tree.SelectedNode.Tag).Guid);
			}
			base.OnSaveSettings(key);
		}

		#endregion
	}
}
