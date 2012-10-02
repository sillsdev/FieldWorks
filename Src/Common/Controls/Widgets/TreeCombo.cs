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
using System.Diagnostics;
using System.Drawing;
using System.IO; // for Win32 message defns.
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TreeCombo is like a combo box except that it uses a PopupTree to display a hierarchy
	/// of options. ENHANCE: this should be refactored to inherit from FwComboBox
	/// </summary>
	/// <remarks>
	/// <para>Only a minimum of Combo box features currently needed is implemented.</para>
	/// <para>The key event is 'AfterSelect' which is triggered when an item in the popup tree
	/// is selected.</para>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class TreeCombo : UserControl, IFWDisposable, IVwNotifyChange, IWritingSystemAndStylesheet, IVisualStyleComboBox
	{
		#region Events

		/// <summary></summary>
		public event TreeViewEventHandler AfterSelect;
		/// <summary></summary>
		public event TreeViewCancelEventHandler BeforeSelect;
		/// <summary></summary>
		public event EventHandler TreeLoad;
		/// <summary>
		/// Occurs when about to drop down the list. May be used to set up the items, if that doesn't take too long.
		/// </summary>
		public event EventHandler DropDown;

		#endregion Events

		#region Data members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		ComboTextBox m_comboTextBox;
		// The tree box, usually only shown if the down arrow is clicked.
		PopupTree m_popupTree;
		// The button that pulls down the list.
		DropDownButton m_button;
		ComboBoxState m_state = ComboBoxState.Normal;
		bool m_hasBorder = true;
		bool m_useVisualStyleBackColor = true;
		Panel m_textBoxPanel;
		Panel m_buttonPanel;
		Padding m_textPadding;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <value>The state.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBoxState State
		{
			get
			{
				CheckDisposed();
				return m_state;
			}

			set
			{
				CheckDisposed();
				m_state = value;
				Invalidate(true);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box has a border.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has a border, otherwise <c>false</c>.
		/// </value>
		public bool HasBorder
		{
			get
			{
				CheckDisposed();
				return m_hasBorder;
			}

			set
			{
				CheckDisposed();
				m_hasBorder = value;
				if (Application.RenderWithVisualStyles)
					SetPadding();
				else
					BorderStyle = m_hasBorder ? BorderStyle.Fixed3D : BorderStyle.None;
			}
		}

		/// <summary>
		/// Gets or sets the border style of the tree view control.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.BorderStyle"/> values. The default is <see cref="F:System.Windows.Forms.BorderStyle.Fixed3D"/>.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The assigned value is not one of the <see cref="T:System.Windows.Forms.BorderStyle"/> values.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new BorderStyle BorderStyle
		{
			get
			{
				return base.BorderStyle;
			}

			set
			{
				if (!Application.RenderWithVisualStyles)
				{
					base.BorderStyle = value;
					m_hasBorder = value != BorderStyle.None;
				}
			}
		}

		/// <summary>
		/// Gets or sets padding within the control. This adjusts the padding around the text.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.Padding"/> representing the control's internal spacing characteristics.
		/// </returns>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Padding
		{
			get
			{
				CheckDisposed();
				return m_textPadding;
			}

			set
			{
				CheckDisposed();
				m_textPadding = value;
				SetPadding();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box will use the visual style background.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the combo box will use the visual style background, otherwise <c>false</c>.
		/// </value>
		public bool UseVisualStyleBackColor
		{
			get
			{
				CheckDisposed();
				return m_useVisualStyleBackColor;
			}

			set
			{
				CheckDisposed();
				m_useVisualStyleBackColor = value;
			}
		}

		/// <summary>
		/// Gets or sets the drop down style.
		/// </summary>
		/// <value>The drop down style.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBoxStyle DropDownStyle
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

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return ClientRectangle;

				if (!m_hasBorder && (!FwComboBox.SupportsButtonStyle || !m_useVisualStyleBackColor))
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
				}
			}
		}

		/// <summary>
		/// Paints the background of the control.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			VisualStyleRenderer renderer = FwComboBox.CreateRenderer(m_state, m_useVisualStyleBackColor, true, ContainsFocus,
				m_hasBorder);
			if (renderer != null)
			{
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
				if (!m_useVisualStyleBackColor)
					e.Graphics.FillRectangle(new SolidBrush(BackColor), ContentRectangle);
				if (ContainsFocus)
				{
					Rectangle contentRect = ContentRectangle;
					Rectangle focusRect = new Rectangle(contentRect.Left + 1, contentRect.Top + 1,
						m_textBoxPanel.Width - 2, contentRect.Height - 2);
					ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
				}
			}
			else
			{
				base.OnPaintBackground(e);
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
				return m_popupTree.Nodes;
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
				return m_popupTree;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int ComboHeight
		{
			get { return 21; } // Seems to be the right figure to make a standard-looking combo.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Size DefaultSize
		{
			get
			{
				return new Size(100, ComboHeight);
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
				m_popupTree.Font = value;
				base.Font = value;
			}
		}

		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		// Doesn't work because value is not a constant.
		//[ DefaultValueAttribute(SystemColors.Window) ]
		public override Color BackColor
		{
			get
			{
				CheckDisposed();

				return base.BackColor;
			}
			set
			{
				CheckDisposed();

				m_useVisualStyleBackColor = false;
				if (m_comboTextBox != null)
				{
					if (m_comboTextBox.BackColor != SystemColors.Highlight)
						m_comboTextBox.BackColor = value;
				}
				base.BackColor = value;
			}
		}

		/// <summary>
		/// Get the preferred height of this, based on the preferred height of the internal
		/// text box.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				CheckDisposed();
				if (m_comboTextBox != null && m_button != null)
				{
					if (Application.RenderWithVisualStyles)
						return Math.Max(m_comboTextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical,
							m_button.PreferredHeight + m_buttonPanel.Padding.Vertical);
					else
						return m_comboTextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical;
				}
				else
					return this.Height;
			}
		}

		/// <summary>
		/// true to adjust font height to fix text box. When set false, client will normally
		/// call PreferredHeight and adjust control size to suit.
		/// </summary>
		public bool AdjustStringHeight
		{
			get
			{
				CheckDisposed();
				return m_comboTextBox.AdjustStringHeight;
			}
			set
			{
				CheckDisposed();
				m_comboTextBox.AdjustStringHeight = value;
			}
		}

		/// <summary>
		/// Get/set the stylesheet for the combo box.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwStylesheet StyleSheet
		{
			get
			{
				if (m_comboTextBox != null)
					return m_comboTextBox.StyleSheet;
				else
					return null;
			}
			set
			{
				if (m_comboTextBox != null)
				{
					m_comboTextBox.StyleSheet = value;
					if (value != null && m_comboTextBox.WritingSystemCode != 0 && m_popupTree != null)
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
						if (nFontSize == -1 && value is SIL.FieldWorks.FDO.FwStyleSheet)
						{
							nFontSize = (value as SIL.FieldWorks.FDO.FwStyleSheet).NormalFontSize;
						}
						int nFontSizeTree = (int)(m_popupTree.Font.SizeInPoints * 1000);
						if (nFontSize > nFontSizeTree)
						{
							Font fntOld = m_popupTree.Font;
							float fntSize = (float)nFontSize;
							fntSize /= 1000.0F;
							m_popupTree.Font = new Font(fntOld.FontFamily, fntSize, fntOld.Style,
								GraphicsUnit.Point, fntOld.GdiCharSet, fntOld.GdiVerticalFont);
						}
					}
				}
			}
		}

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
							SIL.FieldWorks.FDO.Cellar.BaseStyleInfo.ReadOneFontOverride(reader);
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
		/// Copy this to the embedded window.
		/// </summary>
		public override Color ForeColor
		{
			get
			{
				CheckDisposed();

				return base.ForeColor;
			}
			set
			{
				CheckDisposed();

				if (m_comboTextBox != null)
				{
					if (m_comboTextBox.ForeColor != SystemColors.HighlightText)
						m_comboTextBox.ForeColor = value;
				}
				base.ForeColor = value;
			}
		}

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[ BrowsableAttribute(true),
		DesignerSerializationVisibilityAttribute
			(DesignerSerializationVisibility.Visible)
		]
		public override string Text
		{
			get
			{
				CheckDisposed();

				return m_comboTextBox.Text;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.Text = value;
				// Enhance JohnT: try to select it in the tree.
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString Tss
		{
			get
			{
				CheckDisposed();

				return m_comboTextBox.Tss;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.Tss = value;
				// Don't just set the SelectedTss, as that throws an exception if not found.
				// Enhance JohnT: try to select it in the tree.
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();
				return m_comboTextBox.WritingSystemCode;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.WritingSystemCode = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DropDownWidth
		{
			get
			{
				CheckDisposed();
				return m_popupTree.Width;
			}
			set
			{
				CheckDisposed();

				m_popupTree.Width = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DroppedDown
		{
			get
			{
				CheckDisposed();
				return m_popupTree.Visible;
			}
			set
			{
				CheckDisposed();

				if (value)
					ShowTree();
				else
					HideTree();
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return m_comboTextBox.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.WritingSystemFactory = value;
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

		#endregion Properties

		#region Construction and disposal

		/// <summary>
		/// Construct one.
		/// </summary>
		public TreeCombo()
		{
			this.SuspendLayout();
			// Set this box's own properties (first, as we use some of them in figuring the
			// size of other things).

			// Make and install the ComboTextBox
			m_comboTextBox = new ComboTextBox(this);
			m_comboTextBox.AccessibleName = "TextBox";
			m_comboTextBox.Dock = DockStyle.Fill;
			m_comboTextBox.Visible = true;
			// Don't allow typing or pasting into the text box: see LT-6595.
			m_comboTextBox.ReadOnlyView = true;

			// These two lines causes us to get a notification when the string gets changed,
			// so we can fire our TextChanged event.
			m_sda = m_comboTextBox.DataAccess;
			m_sda.AddNotification(this);

			m_comboTextBox.MouseDown += new MouseEventHandler(m_comboTextBox_MouseDown);
			m_comboTextBox.KeyPress += new KeyPressEventHandler(m_comboTextBox_KeyPress);
			m_comboTextBox.KeyDown += new KeyEventHandler(m_comboTextBox_KeyDown);
			m_comboTextBox.GotFocus += new EventHandler(m_comboTextBox_GotFocus);
			m_comboTextBox.LostFocus += new EventHandler(m_comboTextBox_LostFocus);

			m_textBoxPanel = new Panel();
			m_textBoxPanel.AccessibleName = "TextBoxPanel";
			m_textBoxPanel.Dock = DockStyle.Fill;
			m_textBoxPanel.BackColor = Color.Transparent;
			m_textBoxPanel.Controls.Add(m_comboTextBox);
			this.Controls.Add(m_textBoxPanel);

			// Make and install the button that pops up the list.
			m_button = new DropDownButton(this);
			m_button.AccessibleName = "DropDownButton";
			m_button.Dock = DockStyle.Right; // Enhance JohnT: Left if RTL language?

			//m_button.FlatStyle = FlatStyle.Flat; // no raised edges etc for this button.
			m_button.MouseDown += new MouseEventHandler(m_button_MouseDown);
			m_button.GotFocus += new EventHandler(m_button_GotFocus);
			m_button.LostFocus += new EventHandler(m_button_LostFocus);
			m_button.TabStop = false;

			m_buttonPanel = new Panel();
			m_buttonPanel.AccessibleName = "DropDownButtonPanel";
			m_buttonPanel.Dock = DockStyle.Right;
			m_buttonPanel.BackColor = Color.Transparent;
			m_buttonPanel.Controls.Add(m_button);
			this.Controls.Add(m_buttonPanel);

			HasBorder = true;
			Padding = new Padding(Application.RenderWithVisualStyles ? 2 : 1);
			base.BackColor = SystemColors.Window;

			m_buttonPanel.Width = m_button.PreferredWidth + m_buttonPanel.Padding.Horizontal;

			// Create the list.
			m_popupTree = new PopupTree();
			m_popupTree.TabStopControl = m_comboTextBox;
			//m_tree.SelectedIndexChanged += new EventHandler(m_listBox_SelectedIndexChanged);
			//m_listBox.SameItemSelected += new EventHandler(m_listBox_SameItemSelected);
			m_popupTree.AfterSelect += new TreeViewEventHandler(m_tree_AfterSelect);
			m_popupTree.BeforeSelect += new TreeViewCancelEventHandler(m_popupTree_BeforeSelect);
			m_popupTree.Load += new EventHandler(m_tree_Load);
			m_popupTree.PopupTreeClosed += new TreeViewEventHandler(m_popupTree_PopupTreeClosed);
			m_popupTree.VisibleChanged += new EventHandler(m_popupTree_VisibleChanged);

			this.ResumeLayout();
		}

		void SetPadding()
		{
			Rectangle rect = ContentRectangle;
			Padding padding = new Padding(rect.Left - ClientRectangle.Left, rect.Top - ClientRectangle.Top,
				ClientRectangle.Right - rect.Right, ClientRectangle.Bottom - rect.Bottom);

			m_textBoxPanel.Padding = new Padding(padding.Left + m_textPadding.Left, padding.Top + m_textPadding.Top, m_textPadding.Right,
				padding.Bottom + m_textPadding.Bottom);
			if (Application.RenderWithVisualStyles && !FwComboBox.SupportsButtonStyle)
				m_buttonPanel.Padding = new Padding(0, padding.Top, padding.Right, padding.Bottom);
		}

		void m_popupTree_PopupTreeClosed(object sender, TreeViewEventArgs e)
		{
			m_comboTextBox.FocusAndSelectAll();
		}

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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				if (m_button != null)
				{
					m_button.MouseDown -= new MouseEventHandler(m_button_MouseDown);
					m_button.GotFocus -= new EventHandler(m_button_GotFocus);
					m_button.LostFocus -= new EventHandler(m_button_LostFocus);
				}

				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_comboTextBox != null)
				{
					m_comboTextBox.KeyPress -= new KeyPressEventHandler(m_comboTextBox_KeyPress);
					m_comboTextBox.KeyDown -= new KeyEventHandler(m_comboTextBox_KeyDown);
					m_comboTextBox.MouseDown -= new MouseEventHandler(m_comboTextBox_MouseDown);
					m_comboTextBox.GotFocus -= new EventHandler(m_comboTextBox_GotFocus);
					m_comboTextBox.LostFocus -= new EventHandler(m_comboTextBox_LostFocus);
				}
				if (m_popupTree != null)
				{
					m_popupTree.AfterSelect -= new TreeViewEventHandler(m_tree_AfterSelect);
					m_popupTree.BeforeSelect -= new TreeViewCancelEventHandler(m_popupTree_BeforeSelect);
					m_popupTree.Load -= new EventHandler(m_tree_Load);
					m_popupTree.VisibleChanged -= new EventHandler(m_popupTree_VisibleChanged);
				}

				if (m_popupTree != null && !m_popupTree.IsDisposed)
					m_popupTree.Dispose();
			}
			m_button = null; // So OnLayout knows to do nothing.
			m_comboTextBox = null;
			m_popupTree = null;
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

		#region Other methods

		internal void HideTree()
		{
			CheckDisposed();

			m_popupTree.Hide();
		}

		private void ShowTree()
		{
			TriggerDropDown();
			ShowTreeAfterDropDownEvent();
		}

		private void TriggerDropDown()
		{
			if (DropDown != null)
				DropDown(this, new EventArgs());
		}
		private void ShowTreeAfterDropDownEvent()
		{
			// Unless the programmer set an explicit width for the list box, make it match this.
			if (m_popupTree.Width != this.Width)
				m_popupTree.Width = this.Width;

			m_popupTree.Launch(Parent.RectangleToScreen(Bounds), Screen.GetWorkingArea(this));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FocusAndSelectText()
		{
			CheckDisposed();

			m_comboTextBox.FocusAndSelectAll();
		}

		#endregion Other methods

		#region IVwNotifyChange implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// Currently the only property that can change is the string,
			// but verify it in case we later make a mechanism to work with a shared
			// cache. If it is the right property, report TextChanged.
			if (tag != InnerFwTextBox.ktagText)
				return;
//			m_listBox.SelectedIndex = m_listBox.FindIndexOfTss(m_fwTextBox.Tss);
//			OnTextChanged(new EventArgs());
		}
		#endregion IVwNotifyChange implementation

		#region Event handlers

		void m_button_MouseDown(object sender, MouseEventArgs e)
		{
			ShowTree();
		}

		void m_comboTextBox_MouseDown(object sender, MouseEventArgs e)
		{
			Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
			Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_LBUTTONDOWN, 0, 0);
			Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_LBUTTONUP, 0, 0);
		}

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
				TriggerDropDown(); // Typically loads the list, which better be done before we try to select one.
				m_popupTree.SelectNodeStartingWith(e.KeyChar.ToString());
				ShowTreeAfterDropDownEvent();
				e.Handled = true;
			}
		}

		private void m_comboTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			Debug.WriteLine("Key is " + e.KeyData + " value " + e.KeyValue + " modifiers " + e.Modifiers);
			if (e.KeyData == (Keys.Down | Keys.Alt) ||
				e.KeyData == (Keys.Up | Keys.Alt))
			{
				ShowTree();
				e.Handled = true;
				return;
			}
			this.OnKeyDown(e);
		}

		private void m_popupTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (BeforeSelect != null)
				BeforeSelect(this, e);
		}

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
			FocusAndSelectText();
		}

		private void m_tree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// only publish the actions that are like a mouse clicking.
			if (e.Action == TreeViewAction.ByMouse)
			{
				SetComboText(e.Node);
			}
			if (AfterSelect != null)
				AfterSelect(this, e);
		}

		private void m_tree_Load(object sender, EventArgs e)
		{
			if (TreeLoad != null)
				TreeLoad(this, e);
		}

		void m_popupTree_VisibleChanged(object sender, EventArgs e)
		{
			if (Application.RenderWithVisualStyles)
				State = m_popupTree.Visible ? ComboBoxState.Pressed : ComboBoxState.Normal;
		}

		void m_comboTextBox_GotFocus(object sender, EventArgs e)
		{
			SetTextBoxHighlight();
		}

		void m_comboTextBox_LostFocus(object sender, EventArgs e)
		{
			RemoveTextBoxHighlight();
		}

		void m_button_GotFocus(object sender, EventArgs e)
		{
			SetTextBoxHighlight();
		}

		void m_button_LostFocus(object sender, EventArgs e)
		{
			RemoveTextBoxHighlight();
		}

		void SetTextBoxHighlight()
		{
			// change control to highlight state.
			if (!FwComboBox.SupportsButtonStyle || !m_useVisualStyleBackColor)
			{
				if (m_comboTextBox.BackColor != SystemColors.Highlight)
					m_comboTextBox.BackColor = SystemColors.Highlight;
				if (m_comboTextBox.ForeColor != SystemColors.HighlightText)
					m_comboTextBox.ForeColor = SystemColors.HighlightText;
			}
			Invalidate(true);
		}

		void RemoveTextBoxHighlight()
		{
			if (!FwComboBox.SupportsButtonStyle || !m_useVisualStyleBackColor)
			{
				// revert to original color state.
				m_comboTextBox.BackColor = BackColor;
				m_comboTextBox.ForeColor = ForeColor;
			}
			Invalidate(true);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.EnabledChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			if (Application.RenderWithVisualStyles)
				State = Enabled ? ComboBoxState.Normal : ComboBoxState.Disabled;
		}

		#endregion Event handlers
	}
}
