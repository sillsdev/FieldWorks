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
// File: TreeCombo.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO; // for Win32 message defns.
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TreeCombo is like a combo box except that it uses a PopupTree to display a hierarchy
	/// of options.
	/// </summary>
	/// <remarks>
	/// <para>Only a minimum of Combo box features currently needed is implemented.</para>
	/// <para>The key event is 'AfterSelect' which is triggered when an item in the popup tree
	/// is selected.</para>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class TreeCombo : FwComboBoxBase
	{
		#region Events

		/// <summary></summary>
		public event TreeViewEventHandler AfterSelect;
		/// <summary></summary>
		public event TreeViewCancelEventHandler BeforeSelect;
		/// <summary></summary>
		public event EventHandler TreeLoad;

		#endregion Events

		#region Construction and disposal

		/// <summary>
		/// Construct one.
		/// </summary>
		public TreeCombo()
		{
			base.DropDownStyle = ComboBoxStyle.DropDownList;
			m_comboTextBox.KeyPress += m_comboTextBox_KeyPress;
			m_button.KeyPress += m_button_KeyPress;
		}

		/// <summary>
		/// Creates the drop down box.
		/// </summary>
		/// <returns></returns>
		protected override IDropDownBox CreateDropDownBox()
		{
			// Create the list.
			var popupTree = new PopupTree();
			popupTree.TabStopControl = m_comboTextBox;
			//m_tree.SelectedIndexChanged += new EventHandler(m_listBox_SelectedIndexChanged);
			//m_listBox.SameItemSelected += new EventHandler(m_listBox_SameItemSelected);
			popupTree.AfterSelect += m_tree_AfterSelect;
			popupTree.BeforeSelect += m_popupTree_BeforeSelect;
			popupTree.Load += m_tree_Load;
			popupTree.PopupTreeClosed += m_popupTree_PopupTreeClosed;
			return popupTree;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				if (m_comboTextBox != null)
					m_comboTextBox.KeyPress -= m_comboTextBox_KeyPress;

				if (m_button != null)
					m_button.KeyPress -= m_button_KeyPress;

				if (Tree != null)
				{
					Tree.AfterSelect -= m_tree_AfterSelect;
					Tree.BeforeSelect -= m_popupTree_BeforeSelect;
					Tree.Load -= m_tree_Load;
					Tree.PopupTreeClosed -= m_popupTree_PopupTreeClosed;
				}
			}

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

		#region Properties

		/// <summary>
		/// Gets or sets the drop down style.
		/// </summary>
		/// <value>The drop down style.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ComboBoxStyle DropDownStyle
		{
			get
			{
				CheckDisposed();
				return ComboBoxStyle.DropDownList;
			}

			set
			{
				CheckDisposed();
				// do nothing
			}
		}

		/// <summary>
		/// Get the main collection of Nodes. Manipulating this is the main way of
		/// adding and removing items from the popup tree.
		/// </summary>
		public TreeNodeCollection Nodes
		{
			get
			{
				CheckDisposed();
				return Tree.Nodes;
			}
		}

		/// <summary>
		/// Get the tree...this is mainly for methods that load the nodes.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PopupTree Tree
		{
			get
			{
				CheckDisposed();
				return m_dropDownBox as PopupTree;
			}
		}


		/// <summary>
		/// We need to ensure the PopupTree is set to use the same font
		/// </summary>
		public override Font Font
		{
			get
			{
				return base.Font;
			}
			set
			{
				base.Font = value;
				Tree.Font = value;
			}
		}

		/// <summary>
		/// Get/set the stylesheet for the combo box.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IVwStylesheet StyleSheet
		{
			get
			{
				return base.StyleSheet;
			}
			set
			{
				base.StyleSheet = value;
				if (value != null && m_comboTextBox.WritingSystemCode != 0 && Tree != null)
				{
					// Set the font size for the popup tree.
					FontOverrideInfo overrideInfo = null;
					ITsTextProps ttp = value.NormalFontStyle;
					if (ttp != null)
					{
						string x = ttp.GetStrPropValue((int)FwTextPropType.ktptWsStyle);
						if (x != null)
							overrideInfo = GetFontOverrideInfo(x, m_comboTextBox.WritingSystemCode);
					}
					int nFontSize = -1;
					if (overrideInfo != null)
					{
						for (int i = 0; i < overrideInfo.m_intProps.Count; ++i)
						{
							if (overrideInfo.m_intProps[i].m_textPropType == (int)FwTextPropType.ktptFontSize)
							{
								nFontSize = overrideInfo.m_intProps[i].m_value;
								break;
							}
						}
					}
					if (nFontSize == -1 && value is FwStyleSheet)
						nFontSize = ((FwStyleSheet)value).NormalFontSize;

					int nFontSizeTree = (int)(Tree.Font.SizeInPoints * 1000);
					if (nFontSize > nFontSizeTree)
					{
						Font fntOld = Tree.Font;
						float fntSize = nFontSize;
						fntSize /= 1000.0F;
						Tree.Font = new Font(fntOld.FontFamily, fntSize, fntOld.Style,
							GraphicsUnit.Point, fntOld.GdiCharSet, fntOld.GdiVerticalFont);
					}
				}
			}
		}

		/// <summary>
		/// Get/Set the selected node.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TreeNode SelectedNode
		{
			get
			{
				CheckDisposed();
				return Tree.SelectedNode;
			}
			set
			{
				CheckDisposed();

				Tree.SelectedNode = value;
				SetComboText(value);
			}
		}

		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override object SelectedItem
		{
			get
			{
				CheckDisposed();
				return SelectedNode;
			}
			set
			{
				CheckDisposed();

				if (value is TreeNode || value == null)
					SelectedNode = value as TreeNode;
			}
		}

		#endregion Properties

		#region Other methods

		FontOverrideInfo GetFontOverrideInfo(string source, int wsTreeCombo)
		{
			using (BinaryReader reader = new BinaryReader(StringUtils.MakeStreamFromString(source)))
			{
				try
				{
					// read until the end of stream
					while (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						FontOverrideInfo overrideInfo =
							BaseStyleInfo.ReadOneFontOverride(reader);
						if (overrideInfo.m_ws == wsTreeCombo)
							return overrideInfo;
					}
				}
				catch (EndOfStreamException)
				{
				}
				finally
				{
					reader.Close();
				}
			}
			return null;
		}

		/// <summary>
		/// Sets the text but doesn't cause a focus.
		/// </summary>
		private void SetComboText(TreeNode node)
		{
			if (node == null)
			{
				m_comboTextBox.Text = "";
			}
			else
			{
				HvoTreeNode hvoTreeNode = node as HvoTreeNode;
				if (hvoTreeNode != null)
					m_comboTextBox.Tss = hvoTreeNode.Tss;
				else
					m_comboTextBox.Text = node.Text;
			}
		}

		/// <summary>
		/// Sets the text and focuses the control
		/// FWNX-270: renamed from SetComboText to make it clear that this method
		/// causes a focus
		/// </summary>
		private void SetComboTextAndFocus(TreeNode node)
		{
			SetComboText(node);
			m_comboTextBox.Select();
			m_comboTextBox.SelectAll();
		}

		#endregion Other methods

		#region Event handlers

		/// <summary>
		/// Handle a key press in the combo box.
		/// Enable type-ahead to select list items (LT-2190).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_comboTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!Char.IsControl(e.KeyChar))
			{
				RaiseDropDown(); // Typically loads the list, which better be done before we try to select one.
				Tree.SelectNodeStartingWith(e.KeyChar.ToString());
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_button_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!Char.IsControl(e.KeyChar))
			{
				RaiseDropDown(); // Typically loads the list, which better be done before we try to select one.
				Tree.SelectNodeStartingWith(e.KeyChar.ToString());
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_popupTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (BeforeSelect != null)
				BeforeSelect(this, e);
		}

		private void m_tree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// only publish the actions that are like a mouse clicking.
			if (e.Action == TreeViewAction.ByMouse)
			{
				SetComboTextAndFocus(e.Node);
			}
			if (AfterSelect != null)
				AfterSelect(this, e);
		}

		private void m_tree_Load(object sender, EventArgs e)
		{
			if (TreeLoad != null)
				TreeLoad(this, e);
		}

		private void m_popupTree_PopupTreeClosed(object sender, TreeViewEventArgs e)
		{
			m_comboTextBox.Select();
			m_comboTextBox.SelectAll();
		}

		#endregion Event handlers
	}
}
