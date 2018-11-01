// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// The new Styles Dialog
	/// </summary>
	internal partial class FwApplyStyleDlg : Form
	{
		#region Data Members
		private StyleListBoxHelper m_styleListHelper;
		private StyleInfoTable m_styleTable;
		private LcmStyleSheet m_styleSheet;
		private string m_paraStyleName;
		private string m_charStyleName;
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructor and initialization

		internal FwApplyStyleDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwApplyStyleDlg"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="paraStyleName">Name of the currently selected paragraph style.</param>
		/// <param name="charStyleName">Name of the currently selected character style.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public FwApplyStyleDlg(LcmCache cache, LcmStyleSheet styleSheet, string paraStyleName, string charStyleName, IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			m_paraStyleName = paraStyleName;
			m_charStyleName = charStyleName;

			// Load the style information
			m_styleTable = new StyleInfoTable(StyleServices.NormalStyleName, cache.ServiceLocator.WritingSystemManager);
			m_styleSheet = styleSheet;
			m_styleListHelper = new StyleListBoxHelper(m_lstStyles)
			{
				ShowInternalStyles = false
			};
		}

		/// <summary>
		/// Raises the handle created event.
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			FillStyleList();

			// Mark the current styles
			m_styleListHelper.MarkCurrentStyle(m_paraStyleName);
			m_styleListHelper.MarkCurrentStyle(m_charStyleName);

			// Select the current paragraph style in the list
			if (CanApplyParagraphStyle && m_paraStyleName != null)
			{
				m_styleListHelper.SelectedStyleName = m_paraStyleName;
			}
			else if (CanApplyCharacterStyle)
			{
				m_styleListHelper.SelectedStyleName = m_charStyleName ?? StyleUtils.DefaultParaCharsStyleName;
			}
		}

		/// <summary>
		/// Fills the style table and populates the list based on it.
		/// </summary>
		private void FillStyleList()
		{
			m_styleTable.Clear();
			for (var i = 0; i < m_styleSheet.CStyles; i++)
			{
				var style = m_styleSheet.get_NthStyleObject(i);
				if (ApplicableStyleContexts == null || ApplicableStyleContexts.Contains(style.Context))
				{
					m_styleTable.Add(style.Name, new StyleInfo(style));
				}
			}
			if (CanApplyCharacterStyle && !CanApplyParagraphStyle)
			{
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			}
			else if (CanApplyParagraphStyle && !CanApplyCharacterStyle)
			{
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;
			}
			else if (!CanApplyCharacterStyle && !CanApplyParagraphStyle)
			{
				throw new InvalidOperationException("Can't show the Apply Style dialog box if neither character nor paragraph styles can be applied.");
			}
			else
			{
				m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstLim;
			}

			m_styleListHelper.AddStyles(m_styleTable, null);
			m_styleListHelper.Refresh();
		}
		#endregion

		#region Event handlers

		/// <summary>
		/// Handles the Click event of the m_btnOk control.
		/// </summary>
		private void m_btnOk_Click(object sender, EventArgs e)
		{
			StyleChosen = m_lstStyles.Text;
		}

		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, sender == helpToolStripMenuItem ? $"style:{m_lstStyles.SelectedItem}" : "kstidApplyStyleDialog");
		}

		/// <summary>
		/// Handles the MouseDown event of the styles list. If the user clicks with the right
		/// mouse button we have to select the style.
		/// </summary>
		private void m_lstStyles_MouseDown(object sender, MouseEventArgs e)
		{
			m_lstStyles.Focus(); // This can fail if validation fails in control that had focus.
			if (m_lstStyles.Focused && e.Button == MouseButtons.Right)
			{
				m_lstStyles.SelectedIndex = m_lstStyles.IndexFromPoint(e.Location);
			}
		}

		/// <summary>
		/// Handles the MouseUp event of the styles list. If the user clicks with the right
		/// mouse button we have to bring up the context menu if the mouse up event occurs over
		/// the selected style.
		/// </summary>
		private void m_lstStyles_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && m_lstStyles.IndexFromPoint(e.Location) == m_lstStyles.SelectedIndex)
			{
				contextMenuStyles.Show(m_lstStyles, e.Location);
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// Sets a value indicating whether character styles should be shown in the list of
		/// styles that can be applied.
		/// </summary>
		public bool CanApplyCharacterStyle { get; set; } = true;

		/// <summary>
		/// Sets a value indicating whether paragraph styles should be shown in the list of
		/// styles that can be applied.
		/// </summary>
		public bool CanApplyParagraphStyle { get; set; } = true;

		/// <summary>
		/// Gets the name of the style chosen.
		/// </summary>
		public string StyleChosen { get; private set; }

		/// <summary>
		/// Specifies a set of style contexts that should be used to determine which styles can be
		/// applied.
		/// </summary>
		public List<ContextValues> ApplicableStyleContexts { get; set; }
		#endregion
	}
}