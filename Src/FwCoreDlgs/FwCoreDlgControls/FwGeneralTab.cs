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
// File: FwGeneralTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwGeneralTab : UserControl, IFWDisposable, IStylesTab
	{
		#region Member variables
		// reference from FwStylesDlg
		private StyleListBoxHelper m_styleListHelper;
		// reference from FwStylesDlg
		private StyleInfoTable m_styleTable;
		// reference from FwStylesDlg
		private Dictionary<string, string> m_renamedStyles;

		private bool m_fShowBiDiLabels;
		private MsrSysType m_userMeasurementType;
		private bool m_owningDialogCanceled = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwGeneralTab"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwGeneralTab()
		{
			InitializeComponent();
		}
		#endregion

		#region IFWDisposable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		#endregion

		#region IStylesTab Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the information on the tab to the specified style info.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToInfo(StyleInfo styleInfo)
		{
			// save the changes from the general tab
			// NOTE: The name has to be set last as ChangeStyleName can update the basedOn
			// and Following styles for styleInfo to its correct values.
			styleInfo.SaveBasedOn(m_cboBasedOn.Text);
			styleInfo.SaveFollowing(m_cboFollowingStyle.Text);
			if (m_txtStyleName.Text != styleInfo.Name)
				ChangeStyleName(styleInfo);
			styleInfo.SaveDescription(m_txtStyleUsage.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information on the tab with the information in the specified style info.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(StyleInfo styleInfo)
		{
			if (styleInfo == null)
			{
				FillForDefaultParagraphCharacters();
				return;
			}

			m_txtStyleName.Enabled = !styleInfo.IsBuiltIn;
			m_txtStyleUsage.ReadOnly = styleInfo.IsBuiltIn;
			m_cboBasedOn.Enabled = !styleInfo.IsBuiltIn;
			m_cboFollowingStyle.Enabled = !styleInfo.IsBuiltIn;
			m_txtShortcut.Enabled = false;
			m_txtStyleName.Text = styleInfo.Name;
			m_lblStyleType.Text = (styleInfo.IsCharacterStyle) ?
				FwCoreDlgControls.kstidCharacterStyleText : FwCoreDlgControls.kstidParagraphStyleText;
			m_txtStyleUsage.Text = styleInfo.Usage;
			m_lblStyleDescription.Text = styleInfo.ToString(m_fShowBiDiLabels, m_userMeasurementType);

			// Handle the Based On style combo
			FillBasedOnStyles(styleInfo);
			if (styleInfo.BasedOnStyle != null)
				m_cboBasedOn.SelectedItem = styleInfo.BasedOnStyle.Name;
			else
			{
				if (styleInfo.IsCharacterStyle)
					m_cboBasedOn.SelectedIndex = 0;	// "default paragraph characters"
				else
					m_cboBasedOn.SelectedIndex = -1;
			}

			UpdateFollowingStylesCbo(styleInfo);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the style as typed into the style name text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public String StyleName
		{
			get { return m_txtStyleName.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style list helper.
		/// </summary>
		/// <value>The style list helper.</value>
		/// ------------------------------------------------------------------------------------
		public StyleListBoxHelper StyleListHelper
		{
			get { return m_styleListHelper; }
			set { m_styleListHelper = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style table.
		/// </summary>
		/// <value>The style table.</value>
		/// ------------------------------------------------------------------------------------
		public StyleInfoTable StyleTable
		{
			get { return m_styleTable; }
			set { m_styleTable = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether to show labels that are meaningful for both left-to-right and
		/// right-to-left. If this value is false, then simple "Left" and "Right" labels will be
		/// used in the display, rather than "Leading" and "Trailing".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowBiDiLabels
		{
			set
			{
				CheckDisposed();
				m_fShowBiDiLabels = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of the user measurement.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MsrSysType UserMeasurementType
		{
			get { return m_userMeasurementType; }
			set { m_userMeasurementType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the renamed styles collection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, string> RenamedStyles
		{
			set { m_renamedStyles = value; }
		}

		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.GotFocus"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			m_txtStyleName.Focus();
			m_txtStyleName.SelectAll();
		}
		#endregion

		#region General tab handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the dialog to have default paragraph characters selected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillForDefaultParagraphCharacters()
		{
			m_txtStyleName.Text = FDO.FdoResources.DefaultParaCharsStyleName;
			m_txtStyleName.Enabled = false;
			m_txtStyleUsage.Text = FDO.FdoResources.DefaultParaCharsStyleUsage;
			m_txtStyleUsage.ReadOnly = true;
			m_lblStyleType.Text = FwCoreDlgControls.kstidCharacterStyleText;
			m_cboBasedOn.SelectedIndex = -1;
			m_cboBasedOn.Enabled = false;
			m_cboFollowingStyle.SelectedIndex = -1;
			m_cboFollowingStyle.Enabled = false;
			m_lblStyleDescription.Text = string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the based on styles combo for a specific style
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		private void FillBasedOnStyles(BaseStyleInfo styleInfo)
		{
			m_cboBasedOn.Items.Clear();

			// If this is a character style then put in "Default Paragraph Characters"
			if (styleInfo.IsCharacterStyle)
				m_cboBasedOn.Items.Add(FdoResources.DefaultParaCharsStyleName);

			// Add all of the styles that are not myself or any style that derives from me and
			// have the same context as me
			List<string> styleList = new List<string>();
			foreach (BaseStyleInfo baseStyle in m_styleTable.Values)
			{
				// If the style types are not the same, then do not allow them.
				if (baseStyle.IsCharacterStyle != styleInfo.IsCharacterStyle)
					continue;
				// TE-6344: If styleInfo is already based on baseStyle, then we must include baseStyle
				// in the list, even if it is not normally a style that can be a based-on
				// style. This allows a style with a context of internal (such as "Normal" in
				// TE) to appear in the list when it is the basis for a built-in or copied style.
				if (styleInfo.BasedOnStyle == baseStyle)
				{
					Debug.Assert(!DerivesFromOrSame(baseStyle, styleInfo)); // Sanity check for circular reference
					styleList.Add(baseStyle.Name);
				}
				else if (!DerivesFromOrSame(baseStyle, styleInfo) && baseStyle.CanInheritFrom &&
					StylesCanBeABaseFor(baseStyle, styleInfo))
				{
					styleList.Add(baseStyle.Name);
				}
			}
			styleList.Sort();
			m_cboBasedOn.Items.AddRange(styleList.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the following styles combo for a specific style
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		private void FillFollowingStyles(BaseStyleInfo styleInfo)
		{
			m_cboFollowingStyle.Items.Clear();

			// Add all of the styles of the same type
			List<string> styleList = new List<string>();
			foreach (BaseStyleInfo style in m_styleTable.Values)
			{
				// If the style types are not the same, then do not allow them.
				if (style.IsCharacterStyle != styleInfo.IsCharacterStyle)
					continue;
				// TE-6346: Add this style to the list if it's already the following style for the
				// given styleInfo, even if it's an internal style because internal styles can have
				// themselves as their own following style.
				if (styleInfo.NextStyle == style || !style.IsInternalStyle)
					styleList.Add(style.Name);
			}
			styleList.Sort();
			m_cboFollowingStyle.Items.AddRange(styleList.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the following styles combo box.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateFollowingStylesCbo(StyleInfo styleInfo)
		{
			// Handle the Following Paragraph Style combo box
			if (styleInfo.IsCharacterStyle)
			{
				m_cboFollowingStyle.Items.Clear();
				m_cboFollowingStyle.Enabled = false;
			}
			else
			{
				FillFollowingStyles(styleInfo);
				if (styleInfo.NextStyle == null)
					m_cboFollowingStyle.SelectedIndex = -1;
				else
					m_cboFollowingStyle.SelectedItem = styleInfo.NextStyle.Name;
			}
		}
		#endregion

		#region Renaming style
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Validating event of the m_txtStyleName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_txtStyleName_Validating(object sender, CancelEventArgs e)
		{
			// If the user pressed cancel, this method gets called before the owning
			// form will process the cancel which is not good if this validation
			// fails because it means the user cannot cancel the changes that caused
			// the validation failure. Therefore, do our best to determine whether
			// or not the user is here as a result of losing focus to the cancel button.
			// If so, don't bother validating but set a flag indicating the user is
			// cancelling out of the dialog so the Validated event doesn't try to save
			// the invalid change, otherwise the program will crash.
			Form owningForm = FindForm();
			if (owningForm != null && owningForm.ActiveControl == owningForm.CancelButton)
			{
				m_owningDialogCanceled = true;
				return;
			}

			m_txtStyleName.Text = m_txtStyleName.Text.Trim();

			if (m_styleListHelper.SelectedStyleName == m_txtStyleName.Text)
				return;

			if (m_styleTable.ContainsKey(m_txtStyleName.Text))
			{
				e.Cancel = true;
				MessageBox.Show(this,
					string.Format(ResourceHelper.GetResourceString("kstidDuplicateStyleError"),
					m_txtStyleName.Text), Application.ProductName);
			}
			else if (m_txtStyleName.Text.Equals(string.Empty))
			{
				e.Cancel = true;
				MessageBox.Show(this,
					string.Format(ResourceHelper.GetResourceString("kstidBlankStyleNameError"),
					m_txtStyleName.Text), Application.ProductName);

				// set style name from duplicate back to name in style list box (default)
				m_txtStyleName.Text = m_styleListHelper.SelectedStyleName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Validated event of the m_txtStyleName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_txtStyleName_Validated(object sender, EventArgs e)
		{
			if (m_owningDialogCanceled)
				return;

			if (m_styleListHelper.SelectedStyle != null)
			{
				StyleInfo styleInfo = (StyleInfo)m_styleListHelper.SelectedStyle.StyleInfo;
				if (styleInfo == null || m_txtStyleName.Text == styleInfo.Name)
				{
					// We DON'T want to go on to try to re-select this style in the list
					// because if there's another style that differs only by case, we might find
					// the wrong one and accidentally think we're renaming this style.
					return;
				}

				SaveToInfo(styleInfo);
				UpdateFollowingStylesCbo(styleInfo);
			}
			m_styleListHelper.SelectedStyleName = m_txtStyleName.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the name of the style
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		private void ChangeStyleName(StyleInfo styleInfo)
		{
			string newName = m_txtStyleName.Text;
			string oldName = styleInfo.Name;
			// fix any styles that refer to this one
			foreach (StyleInfo updateStyle in m_styleTable.Values)
			{
				if (updateStyle.BasedOnStyle != null && updateStyle.BasedOnStyle.Name == oldName)
					updateStyle.SaveBasedOn(newName);
				if (updateStyle.NextStyle != null && updateStyle.NextStyle.Name == oldName)
					updateStyle.SaveFollowing(newName);
			}

			// save the new name and update the entry in the style table
			styleInfo.SaveName(newName);
			m_styleTable.Remove(oldName);
			m_styleTable.Add(newName, styleInfo);

			// Change the displayed entry
			m_styleListHelper.Rename(oldName, newName);

			// Save an entry to rename the style if it is a real style
			if (styleInfo.RealStyle != null)
				SaveRenamedStyle(oldName, newName);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified base style can be used as a base for the
		/// specified style
		/// </summary>
		/// <param name="baseStyle">The base style</param>
		/// <param name="styleInfo">The style</param>
		/// <returns>True if the base style can be used as a base for the specified style,
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool StylesCanBeABaseFor(BaseStyleInfo baseStyle, BaseStyleInfo styleInfo)
		{
			// If the style is not in the DB yet, then we want to allow any style to be a base
			// so the user can select something
			if (styleInfo.RealStyle == null)
				return true;

			// Styles can always be based on general styles
			if (baseStyle.Context == ContextValues.General)
				return true;

			// If the base style is actually the base style of the style, then show it in the
			// list
			if (styleInfo.BasedOnStyle == baseStyle)
				return true;

			// Otherwise, the context, structure and function of the style must match for a
			// style to be based on it.
			return (baseStyle.Context == styleInfo.Context &&
				baseStyle.Structure == styleInfo.Structure &&
				baseStyle.Function == baseStyle.Function);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if style1 derives from style2 or if the styles are the same
		/// </summary>
		/// <param name="style1">style 1</param>
		/// <param name="style2">style 2</param>
		/// <returns>true if style1 derives from style2</returns>
		/// ------------------------------------------------------------------------------------
		private bool DerivesFromOrSame(BaseStyleInfo style1, BaseStyleInfo style2)
		{
			while (style1 != null)
			{
				if (style2.Name == style1.Name)
					return true;
				style1 = style1.BasedOnStyle;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the renamed style information
		/// </summary>
		/// <param name="oldName">The old name.</param>
		/// <param name="newName">The new name.</param>
		/// ------------------------------------------------------------------------------------
		protected void SaveRenamedStyle(string oldName, string newName)
		{
			// Save the style name change in a list so the change can be applied to the
			// database later. If the style has already been renamed, then just replace the
			// existing entry. This list is keyed on the new name with the old name as the value.
			string originalName = null;
			if (m_renamedStyles.TryGetValue(oldName, out originalName))
			{
				m_renamedStyles.Remove(oldName);
				if (originalName != newName)
					m_renamedStyles[newName] = originalName;
			}
			else
				m_renamedStyles.Add(newName, oldName);
		}
	}
}
