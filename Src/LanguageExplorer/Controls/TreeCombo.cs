// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// TreeCombo is like a combo box except that it uses a PopupTree to display a hierarchy
	/// of options.
	/// </summary>
	/// <remarks>
	/// <para>Only a minimum of Combo box features currently needed is implemented.</para>
	/// <para>The key event is 'AfterSelect' which is triggered when an item in the popup tree
	/// is selected.</para>
	/// </remarks>
	public class TreeCombo : FwComboBoxBase
	{
		#region Events

		/// <summary />
		public event TreeViewEventHandler AfterSelect;
		/// <summary />
		public event TreeViewCancelEventHandler BeforeSelect;
		/// <summary />
		public event EventHandler TreeLoad;

		#endregion Events

		#region Construction and disposal

		/// <summary />
		public TreeCombo()
		{
			base.DropDownStyle = ComboBoxStyle.DropDownList;
			TextBox.KeyPress += m_comboTextBox_KeyPress;
			m_button.KeyPress += m_button_KeyPress;
		}

		/// <summary>
		/// Creates the drop down box.
		/// </summary>
		protected override IDropDownBox CreateDropDownBox()
		{
			// Create the list.
			var popupTree = new PopupTree { TabStopControl = TextBox };
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (TextBox != null)
				{
					TextBox.KeyPress -= m_comboTextBox_KeyPress;
				}
				if (m_button != null)
				{
					m_button.KeyPress -= m_button_KeyPress;
				}
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
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ComboBoxStyle DropDownStyle
		{
			get
			{
				return ComboBoxStyle.DropDownList;
			}
			set
			{
				// do nothing
			}
		}

		/// <summary>
		/// Get the main collection of Nodes. Manipulating this is the main way of
		/// adding and removing items from the popup tree.
		/// </summary>
		public TreeNodeCollection Nodes => Tree.Nodes;

		/// <summary>
		/// Get the tree...this is mainly for methods that load the nodes.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PopupTree Tree => m_dropDownBox as PopupTree;

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
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IVwStylesheet StyleSheet
		{
			get
			{
				return base.StyleSheet;
			}
			set
			{
				base.StyleSheet = value;
				if (value != null && TextBox.WritingSystemCode != 0 && Tree != null)
				{
					// Set the font size for the popup tree.
					FontOverrideInfo overrideInfo = null;
					var ttp = value.NormalFontStyle;
					var x = ttp?.GetStrPropValue((int)FwTextPropType.ktptWsStyle);
					if (x != null)
					{
						overrideInfo = GetFontOverrideInfo(x, TextBox.WritingSystemCode);
					}
					var nFontSize = -1;
					if (overrideInfo != null)
					{
						foreach (var propInfo in overrideInfo.m_intProps)
						{
							if (propInfo.m_textPropType == (int)FwTextPropType.ktptFontSize)
							{
								nFontSize = propInfo.m_value;
								break;
							}
						}
					}
					if (nFontSize == -1 && value is LcmStyleSheet)
					{
						nFontSize = ((LcmStyleSheet)value).NormalFontSize;
					}
					var nFontSizeTree = (int)(Tree.Font.SizeInPoints * 1000);
					if (nFontSize > nFontSizeTree)
					{
						var fntOld = Tree.Font;
						float fntSize = nFontSize;
						fntSize /= 1000.0F;
						Tree.Font = new Font(fntOld.FontFamily, fntSize, fntOld.Style, GraphicsUnit.Point, fntOld.GdiCharSet, fntOld.GdiVerticalFont);
					}
				}
			}
		}

		/// <summary>
		/// Get/Set the selected node.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TreeNode SelectedNode
		{
			get
			{
				return Tree.SelectedNode;
			}
			set
			{
				Tree.SelectedNode = value;
				SetComboText(value);
			}
		}

		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override object SelectedItem
		{
			get
			{
				return SelectedNode;
			}
			set
			{
				SelectedNode = value as TreeNode;
			}
		}

		#endregion Properties

		#region Other methods
		private static FontOverrideInfo GetFontOverrideInfo(string source, int wsTreeCombo)
		{
			using (var reader = new BinaryReader(StringUtils.MakeStreamFromString(source)))
			{
				try
				{
					// read until the end of stream
					while (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						var overrideInfo = BaseStyleInfo.ReadOneFontOverride(reader);
						if (overrideInfo.m_ws == wsTreeCombo)
						{
							return overrideInfo;
						}
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
		public void SetComboText(TreeNode node)
		{
			if (node == null)
			{
				TextBox.Text = string.Empty;
			}
			else
			{
				var hvoTreeNode = node as HvoTreeNode;
				if (hvoTreeNode != null)
				{
					TextBox.Tss = hvoTreeNode.Tss;
				}
				else
				{
					TextBox.Text = node.Text;
				}
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
			TextBox.Select();
			TextBox.SelectAll();
		}

		#endregion Other methods

		#region Event handlers

		/// <summary>
		/// Handle a key press in the combo box.
		/// Enable type-ahead to select list items (LT-2190).
		/// </summary>
		private void m_comboTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar))
			{
				RaiseDropDown(); // Typically loads the list, which better be done before we try to select one.
				Tree.SelectNodeStartingWith(e.KeyChar.ToString());
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_button_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar))
			{
				RaiseDropDown(); // Typically loads the list, which better be done before we try to select one.
				Tree.SelectNodeStartingWith(e.KeyChar.ToString());
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_popupTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			BeforeSelect?.Invoke(this, e);
		}

		private void m_tree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// only publish the actions that are like a mouse clicking.
			if (e.Action == TreeViewAction.ByMouse)
			{
				SetComboTextAndFocus(e.Node);
			}
			AfterSelect?.Invoke(this, e);
		}

		private void m_tree_Load(object sender, EventArgs e)
		{
			TreeLoad?.Invoke(this, e);
		}

		private void m_popupTree_PopupTreeClosed(object sender, TreeViewEventArgs e)
		{
			TextBox.Select();
			TextBox.SelectAll();
		}

		#endregion Event handlers
	}
}