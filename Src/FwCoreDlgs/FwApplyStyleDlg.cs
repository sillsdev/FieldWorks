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
// File: FwApplyStyleDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The new Styles Dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwApplyStyleDlg : Form, IFWDisposable
	{
		#region Data Members
		private StyleListBoxHelper m_styleListHelper;
		private StyleInfoTable m_styleTable;
		private FwStyleSheet m_styleSheet;
		private string m_paraStyleName;
		private string m_charStyleName;

		private string m_chosenStyleName;
		private int m_customUserLevel = 0;
		private bool m_fCanApplyCharacterStyle = true;
		private bool m_fCanApplyParagraphStyle = true;
		private List<ContextValues> m_applicableStyleContexts;
		private IVwRootSite m_rootSite;
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructor and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwApplyStyleDlg"/> class.
		/// </summary>
		/// <param name="rootSite">The root site.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoStylesOwner">The hvo of the object which owns the style.</param>
		/// <param name="stylesTag">The "flid" in which the styles are owned.</param>
		/// <param name="normalStyleName">Name of the normal style.</param>
		/// <param name="customUserLevel">The custom user level.</param>
		/// <param name="paraStyleName">Name of the currently selected paragraph style.</param>
		/// <param name="charStyleName">Name of the currently selected character style.</param>
		/// <param name="hvoRootObject">The hvo of the root object in the current view.</param>
		/// <param name="app">The application.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FwApplyStyleDlg(IVwRootSite rootSite, FdoCache cache, int hvoStylesOwner,
			int stylesTag, string normalStyleName, int customUserLevel, string paraStyleName,
			string charStyleName, int hvoRootObject, IApp app,
			IHelpTopicProvider helpTopicProvider)
		{
			m_rootSite = rootSite;
			InitializeComponent();
			m_customUserLevel = customUserLevel;
			m_helpTopicProvider = helpTopicProvider;
			m_paraStyleName = paraStyleName;
			m_charStyleName = charStyleName;

			// Cache is null in tests
			if (cache == null)
				return;

			m_cboTypes.SelectedIndex = 1; // All Styles

			// Load the style information
			m_styleTable = new StyleInfoTable(normalStyleName,
				cache.ServiceLocator.WritingSystemManager);
			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(cache, hvoStylesOwner, stylesTag);
			m_styleListHelper = new StyleListBoxHelper(m_lstStyles);
			m_styleListHelper.ShowInternalStyles = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the handle created event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (m_styleTable.Count == 0)
				FillStyleList();

			// Mark the current styles
			m_styleListHelper.MarkCurrentStyle(m_paraStyleName);
			m_styleListHelper.MarkCurrentStyle(m_charStyleName);

			// Select the current paragraph style in the list
			if (m_fCanApplyParagraphStyle && m_paraStyleName != null)
				m_styleListHelper.SelectedStyleName = m_paraStyleName;
			else if (m_fCanApplyCharacterStyle)
			{
				if (m_charStyleName != null)
					m_styleListHelper.SelectedStyleName = m_charStyleName;
				else
					m_styleListHelper.SelectedStyleName = ResourceHelper.DefaultParaCharsStyleName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the style table and populates the list based on it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillStyleList()
		{
			m_styleTable.Clear();
			for (int i = 0; i < m_styleSheet.CStyles; i++)
			{
				var style = m_styleSheet.get_NthStyleObject(i);
				if (m_applicableStyleContexts == null ||
					m_applicableStyleContexts.Contains(style.Context))
				{
					m_styleTable.Add(style.Name, new StyleInfo(style));
				}
			}
			if (m_fCanApplyCharacterStyle && !m_fCanApplyParagraphStyle)
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			else if (m_fCanApplyParagraphStyle && !m_fCanApplyCharacterStyle)
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;
			else if (!m_fCanApplyCharacterStyle && !m_fCanApplyParagraphStyle)
				throw new InvalidOperationException("Can't show the Apply Style dialog box if neither character nor paragraph styles can be applied.");
			else
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstLim;

			m_styleListHelper.AddStyles(m_styleTable, null);
			m_styleListHelper.Refresh();
		}
		#endregion

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboTypes control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_styleListHelper == null)
				return;
			switch (m_cboTypes.SelectedIndex)
			{
				case 0:	// basic
					m_styleListHelper.MaxStyleLevel = 0;
					break;

				case 1:	// all
					m_styleListHelper.MaxStyleLevel = Int32.MaxValue;
					break;

				case 2:	// custom
					m_styleListHelper.MaxStyleLevel = m_customUserLevel;
					break;
			}
			m_styleListHelper.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnOk control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnOk_Click(object sender, EventArgs e)
		{
			m_chosenStyleName = m_lstStyles.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			string helpTopic;
			if (sender == helpToolStripMenuItem)
				helpTopic = string.Format("style:{0}", m_lstStyles.SelectedItem);
			else
				helpTopic = "kstidApplyStyleDialog";

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_mnuResetStyle_Click(object sender, EventArgs e)
		{
			MessageBox.Show("About to reset " + m_lstStyles.SelectedItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseDown event of the styles list. If the user clicks with the right
		/// mouse button we have to select the style.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstStyles_MouseDown(object sender, MouseEventArgs e)
		{
			m_lstStyles.Focus(); // This can fail if validation fails in control that had focus.
			if (m_lstStyles.Focused && e.Button == MouseButtons.Right)
				m_lstStyles.SelectedIndex = m_lstStyles.IndexFromPoint(e.Location);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseUp event of the styles list. If the user clicks with the right
		/// mouse button we have to bring up the context menu if the mouse up event occurs over
		/// the selected style.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstStyles_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (m_lstStyles.IndexFromPoint(e.Location) == m_lstStyles.SelectedIndex)
					contextMenuStyles.Show(m_lstStyles, e.Location);
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the combo box where the user
		/// can select the type of styles to show (all, basic, or custom styles). This combo
		/// box is shown in TE but not in the other apps.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowSelectStyleTypes
		{
			get { return m_pnlTypesCombo.Visible; }
			set { m_pnlTypesCombo.Visible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether character styles should be shown in the list of
		/// styles that can be applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanApplyCharacterStyle
		{
			set
			{
				CheckDisposed();
				m_fCanApplyCharacterStyle = value;
				if (m_styleTable.Count > 0)
					FillStyleList();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether paragraph styles should be shown in the list of
		/// styles that can be applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanApplyParagraphStyle
		{
			set
			{
				CheckDisposed();
				m_fCanApplyParagraphStyle = value;
				if (m_styleTable.Count > 0)
					FillStyleList();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the style chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleChosen
		{
			get
			{
				CheckDisposed();
				return m_chosenStyleName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Specifies a set of style contexts that should be used to determine which styles can be
		/// applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ContextValues> ApplicableStyleContexts
		{
			set
			{
				CheckDisposed();
				m_applicableStyleContexts = value;
				if (m_styleTable.Count > 0)
					FillStyleList();
			}
		}
		#endregion
	}
}
