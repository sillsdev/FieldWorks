// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// This is an abstract class that provides the basic functionality for a Views-enabled
	/// ComboBox. Classes that extend this class provide an implementation of the drop down
	/// box.
	/// </summary>
	public abstract class FwComboBoxBase : UserControl, IVwNotifyChange
	{
		#region Events
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

		/// <summary>
		/// The button that pulls down the list.
		/// </summary>
		protected DropDownButton m_button;
		/// <summary>
		/// The drop down box
		/// </summary>
		protected IDropDownBox m_dropDownBox;
		/// <summary>
		/// this flag is set if we receive the DropDownWidth message.
		/// Until we receive one of those, the list width tracks the main control width.
		/// </summary>
		protected bool m_fListWidthSet;
		// The type of combo box.
		ComboBoxStyle m_dropDownStyle;
		ComboBoxState m_state = ComboBoxState.Normal;
		bool m_hasBorder = true;
		bool m_useVisualStyleBackColor = true;
		Panel m_textBoxPanel;
		Panel m_buttonPanel;
		Padding m_textPadding;

		#endregion Data members

		#region Construction and disposal

		/// <summary>
		/// Construct one.
		/// </summary>
		protected FwComboBoxBase()
		{
			if (Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
			}

			SuspendLayout();
			// Set this box's own properties (first, as we use some of them in figuring the
			// size of other things).

			// Make and install the ComboTextBox
			TextBox = new ComboTextBox(this)
			{
				AccessibleName = "TextBox",
				Dock = DockStyle.Fill,
				Visible = true
			};

			// This causes us to get a notification when the string gets changed, so we can fire our
			// TextChanged event.
			m_sda = TextBox.DataAccess;
			m_sda.AddNotification(this);

			TextBox.KeyDown += m_comboTextBox_KeyDown;
			TextBox.MouseDown += m_comboTextBox_MouseDown;
			TextBox.GotFocus += m_comboTextBox_GotFocus;
			TextBox.LostFocus += m_comboTextBox_LostFocus;
			TextBox.TabIndex = 1;
			TextBox.TabStop = true;

			m_textBoxPanel = new Panel
			{
				AccessibleName = "TextBoxPanel",
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent
			};
			m_textBoxPanel.Controls.Add(TextBox);
			Controls.Add(m_textBoxPanel);

			// Make and install the button that pops up the list.
			m_button = new DropDownButton(this)
			{
				AccessibleName = "DropDownButton",
				Dock = DockStyle.Right,
				TabStop = false
			};

			m_button.MouseDown += m_button_MouseDown;
			m_button.KeyDown += m_button_KeyDown;
			m_button.GotFocus += m_button_GotFocus;
			m_button.LostFocus += m_button_LostFocus;

			m_buttonPanel = new Panel
			{
				AccessibleName = "DropDownButtonPanel",
				Dock = DockStyle.Right,
				BackColor = Color.Transparent
			};
			m_buttonPanel.Controls.Add(m_button);
			Controls.Add(m_buttonPanel);

			HasBorder = true;
			Padding = new Padding(Application.RenderWithVisualStyles ? 2 : 1);
			base.BackColor = SystemColors.Window;

			m_buttonPanel.Width = m_button.PreferredWidth + m_buttonPanel.Padding.Horizontal;

			m_dropDownBox = CreateDropDownBox();
			m_dropDownBox.Form.VisibleChanged += Form_VisibleChanged;

			ResumeLayout();
		}

		/// <summary>
		/// Creates the drop down box.
		/// </summary>
		protected abstract IDropDownBox CreateDropDownBox();

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				if (m_button != null)
				{
					m_button.MouseDown -= m_button_MouseDown;
					m_button.KeyDown -= m_button_KeyDown;
					m_button.GotFocus -= m_button_GotFocus;
					m_button.LostFocus -= m_button_LostFocus;
				}

				m_sda?.RemoveNotification(this);

				if (TextBox != null)
				{
					TextBox.KeyDown -= m_comboTextBox_KeyDown;
					TextBox.MouseDown -= m_comboTextBox_MouseDown;
					TextBox.GotFocus -= m_comboTextBox_GotFocus;
					TextBox.LostFocus -= m_comboTextBox_LostFocus;
				}

				if (m_dropDownBox != null && !m_dropDownBox.IsDisposed)
				{
					m_dropDownBox.Form.VisibleChanged -= Form_VisibleChanged;
					m_dropDownBox.Dispose();
				}
			}
			m_sda = null;
			m_button = null;
			TextBox = null;
			m_dropDownBox = null;

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

		#region Properties

		/// <summary>
		/// Gets the state.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBoxState State
		{
			get
			{
				return m_state;
			}

			set
			{
				m_state = value;
				Invalidate(true);
			}
		}

		internal const string COMBOBOX_CLASS = "COMBOBOX";
		const int CP_READONLY = 5;
		const int CP_BORDER = 4;
		const int CBB_NORMAL = 1;
		const int CBB_HOT = 2;
		const int CBB_FOCUSED = 3;
		const int CBB_DISABLED = 4;

		/// <summary>
		/// Gets a value indicating whether a combo box can be rendered as a button.
		/// </summary>
		internal static bool SupportsButtonStyle
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
				{
					return false;
				}
				return VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_READONLY, (int)ComboBoxState.Normal));
			}
		}

		private Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
				{
					return ClientRectangle;
				}
				if (!m_hasBorder && (!SupportsButtonStyle || !m_useVisualStyleBackColor || m_dropDownStyle != ComboBoxStyle.DropDownList))
				{
					return ClientRectangle;
				}

				using (var g = CreateGraphics())
				{
					var renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box has a border.
		/// </summary>
		public bool HasBorder
		{
			get
			{
				return m_hasBorder;
			}

			set
			{
				m_hasBorder = value;
				if (Application.RenderWithVisualStyles)
				{
					SetPadding();
				}
				else
				{
					BorderStyle = m_hasBorder ? BorderStyle.Fixed3D : BorderStyle.None;
				}
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
		[Browsable(false),
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
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Padding
		{
			get
			{
				return m_textPadding;
			}

			set
			{
				m_textPadding = value;
				SetPadding();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box will use the visual style background.
		/// </summary>
		public bool UseVisualStyleBackColor
		{
			get
			{
				return m_useVisualStyleBackColor;
			}

			set
			{
				m_useVisualStyleBackColor = value;
#if !__MonoCS__
				if (value)
				{
					TextBox.BackColor = Color.Transparent;
				}
#endif
			}
		}


		VisualStyleRenderer Renderer
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
				{
					return null;
				}

				VisualStyleElement element = null;
				if (m_dropDownStyle == ComboBoxStyle.DropDownList && m_useVisualStyleBackColor)
				{
					element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_READONLY, (int)m_state);
				}
				if (element != null && VisualStyleRenderer.IsElementDefined(element))
				{
					return new VisualStyleRenderer(element);
				}
				if (!m_hasBorder)
				{
					return null;
				}

				if (ContainsFocus)
				{
					element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_FOCUSED);
					if (!VisualStyleRenderer.IsElementDefined(element))
					{
						element = VisualStyleElement.TextBox.TextEdit.Focused;
					}
				}
				else
				{
					switch (m_state)
					{
						case ComboBoxState.Normal:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_NORMAL);
							if (!VisualStyleRenderer.IsElementDefined(element))
							{
								element = VisualStyleElement.TextBox.TextEdit.Normal;
							}
							break;
						case ComboBoxState.Hot:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_HOT);
							if (!VisualStyleRenderer.IsElementDefined(element))
							{
								element = VisualStyleElement.TextBox.TextEdit.Hot;
							}
							break;
						case ComboBoxState.Pressed:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_HOT);
							if (!VisualStyleRenderer.IsElementDefined(element))
							{
								element = VisualStyleElement.TextBox.TextEdit.Hot;
							}
							break;
						case ComboBoxState.Disabled:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_DISABLED);
							if (!VisualStyleRenderer.IsElementDefined(element))
							{
								element = VisualStyleElement.TextBox.TextEdit.Disabled;
							}
							break;
					}
				}

				return new VisualStyleRenderer(element);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var renderer = Renderer;
			if (renderer != null)
			{
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
				if (!m_useVisualStyleBackColor)
				{
					e.Graphics.FillRectangle(new SolidBrush(BackColor), ContentRectangle);
				}
				if (ContainsFocus && m_dropDownStyle == ComboBoxStyle.DropDownList)
				{
					var contentRect = ContentRectangle;
					var focusRect = new Rectangle(contentRect.Left + 1, contentRect.Top + 1, m_textBoxPanel.Width - 2, contentRect.Height - 2);
					ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
				}
			}
		}

		/// <summary>
		/// Also make this accessible for fine adjustments.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboTextBox TextBox { get; protected set; }

		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IVwStylesheet StyleSheet
		{
			get
			{
				return TextBox.StyleSheet;
			}
			set
			{
				TextBox.StyleSheet = value;
			}
		}

		/// <summary />
		public static int ComboHeight => 21;

		/// <summary />
		protected override Size DefaultSize => new Size(100, ComboHeight);

		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public abstract object SelectedItem { get; set; }

		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		// Doesn't work because value is not a constant.
		//[ DefaultValueAttribute(SystemColors.Window) ]
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				m_useVisualStyleBackColor = false;
				if (TextBox != null && TextBox.BackColor != SystemColors.Highlight)
				{
					TextBox.BackColor = value;
				}
				base.BackColor = value;
			}
		}

		/// <summary>
		/// Copy this to the embedded window.
		/// </summary>
		public override Color ForeColor
		{
			get
			{
				return base.ForeColor;
			}
			set
			{
				if (TextBox != null)
				{
					if (TextBox.ForeColor != SystemColors.HighlightText)
					{
						TextBox.ForeColor = value;
					}
				}
				base.ForeColor = value;
			}
		}

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[Browsable(true),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				return TextBox == null ? string.Empty : TextBox.Text;
			}
			set
			{
				if (!DesignMode)
				{
					TextBox.Text = value;
				}
			}
		}

		/// <summary>
		/// Set the drop down style. Must currently be one of DropDown (allows editing) or
		/// DropDownList (does not). Behavior is not exactly as for standard combos...
		/// the program (though not the user) is allowed to set a value in the text box
		/// which is not a member of the list, even when style is DropDownList.
		/// </summary>
		public virtual ComboBoxStyle DropDownStyle
		{
			get
			{
				return m_dropDownStyle;
			}
			set
			{
				if (value == m_dropDownStyle)
				{
					return;
				}
				Debug.Assert(value != ComboBoxStyle.Simple); // not (yet) supported.
				m_dropDownStyle = value;
				// if it's a DropDownList, then don't allow 'editable'
				TextBox.EditingHelper.Editable = m_dropDownStyle != ComboBoxStyle.DropDownList;
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ITsString Tss
		{
			get
			{
				return TextBox.Tss;
			}
			set
			{
				TextBox.Tss = value;
			}
		}

		/// <summary />
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int WritingSystemCode
		{
			get
			{
				return TextBox.WritingSystemCode;
			}
			set
			{
				TextBox.WritingSystemCode = value;
			}
		}

		/// <summary />
		public int DropDownWidth
		{
			get
			{
				return m_dropDownBox.Form.Width;
			}
			set
			{
				m_fListWidthSet = true;
				m_dropDownBox.Form.Width = value;
			}
		}

		/// <summary />
		public bool DroppedDown
		{
			get
			{
				return m_dropDownBox.Form.Visible;
			}
			set
			{
				if (value)
				{
					RaiseDropDown();
					ShowDropDownBox();
				}
				else
				{
					HideDropDownBox();
				}
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return TextBox.WritingSystemFactory;
			}
			set
			{
				TextBox.WritingSystemFactory = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		public override bool Focused => ContainsFocus;

		/// <summary>
		/// When setting the font for this control we need it also set for the
		/// TextBox and ListBox
		/// </summary>
		public override Font Font
		{
			get
			{
				return base.Font;
			}
			set
			{
				TextBox.Font = value;
				base.Font = value;
			}
		}

		/// <summary>
		/// Gets the height of the preferred.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				if (TextBox != null && m_button != null)
				{
					if (Application.RenderWithVisualStyles)
					{
						return Math.Max(TextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical, m_button.PreferredHeight + m_buttonPanel.Padding.Vertical);
					}
					return TextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical;
				}
				return Height;
			}
		}

		#endregion Properties

		#region Other methods

		/// <summary>
		/// Activates a child control.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select; otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward in the tab order.</param>
		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			if (!directed)
			{
				SelectNextControl(null, forward, true, true, false);
			}
		}

		/// <summary>
		/// Selects all of the text in the text box.
		/// </summary>
		public void SelectAll()
		{
			TextBox.SelectAll();
		}

		private void SetPadding()
		{
			var rect = ContentRectangle;
			var padding = new Padding(rect.Left - ClientRectangle.Left, rect.Top - ClientRectangle.Top, ClientRectangle.Right - rect.Right, ClientRectangle.Bottom - rect.Bottom);

			m_textBoxPanel.Padding = new Padding(padding.Left + m_textPadding.Left, padding.Top + m_textPadding.Top, m_textPadding.Right, padding.Bottom + m_textPadding.Bottom);
			if (Application.RenderWithVisualStyles && !SupportsButtonStyle)
			{
				m_buttonPanel.Padding = new Padding(0, padding.Top, padding.Right, padding.Bottom);
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
				return TextBox.AdjustStringHeight;
			}
			set
			{
				TextBox.AdjustStringHeight = value;
			}
		}

		/// <summary>
		/// Adjust, figuring the stylesheet based on the main window of the mediator.
		/// </summary>
		public void AdjustForStyleSheet(Form parent, Control grower, IPropertyTable propertyTable)
		{
			AdjustForStyleSheet(parent, grower, propertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"));
		}

		/// <summary>
		/// Assumes the control is part of a form and should use a stylesheet.
		/// This becomes the stylesheet of the text box.
		/// If, as a result, the preferred height of this becomes greater than its actual
		/// height, the height of this is adjusted to suit.
		/// In addition, if grower is not null (grower is typically a containing panel),
		/// the height of grower is increased by the same amount.
		/// Also, the height of the indicated form is increased, and any top level controls
		/// which need it are adjusted appropriately.
		/// </summary>
		public void AdjustForStyleSheet(Form parent, Control grower, IVwStylesheet stylesheet)
		{
			if (StyleSheet == null)
			{
				StyleSheet = stylesheet;
			}
			var oldHeight = Height;
			var newHeight = Math.Max(oldHeight, PreferredHeight);
			var delta = newHeight - oldHeight;
			if (delta == 0)
			{
				var oldHeight2 = TextBox.Height;
				var newHeight2 = TextBox.PreferredHeight;
				if (newHeight2 > oldHeight2)
				{
					delta = newHeight2 - oldHeight2;
					newHeight += delta;
				}
			}
			if (delta != 0)
			{
				var oldTop = Top;
				Height = newHeight;
				// Need to get the inner box's height adjusted BEFORE we fix the string.
				PerformLayout();
				Tss = FontHeightAdjuster.GetUnadjustedTsString(Tss);
				if (grower != null)
				{
					var anchorTop = ((int)grower.Anchor & (int)AnchorStyles.Top) != 0;
					var anchorBottom = ((int)grower.Anchor & (int)AnchorStyles.Bottom) != 0;
					if (anchorTop)
					{
						if (anchorBottom)
						{
							// anchored top and bottom, will grow with parent window, do nothing
						}
						else
						{
							// anchored top only, increase height manually
							grower.Height += delta;
						}
					}
					else
					{
						if (anchorBottom)
						{
							// anchored bottom only, will move down as parent window grows, adjust top and height
							grower.Height += delta;
							grower.Top -= delta;
						}
						else
						{
							// not anchored at all, just increase height manually
							grower.Height += delta;
						}
					}
				}
				FontHeightAdjuster.GrowDialogAndAdjustControls(parent, delta, grower ?? this);
				// In at least one bizarre case 'this' is positioned within a group box (the grower) but is not
				// a child of it. This is therefore below the top of the grower, so it gets moved down.
				// But we don't ever want this.
				if (Top != oldTop)
				{
					Top = oldTop;
				}
			}
		}

		/// <summary>
		/// Shows the drop down box.
		/// </summary>
		protected void ShowDropDownBox()
		{
			var workingArea = Screen.GetWorkingArea(this);
			var sz = m_dropDownBox.Form.Size;
			// Unless the programmer set an explicit width for the list box, make it a good size.
			if (!m_fListWidthSet)
			{
				var naturalHeight = m_dropDownBox.NaturalHeight;
				if (naturalHeight < 20)
				{
					naturalHeight = 20; // don't let it be invisible if no items.
				}
				sz.Height = Math.Min(naturalHeight, workingArea.Height * 4 / 10);
				var naturalWidth = m_dropDownBox.NaturalWidth;
				if (sz.Height < naturalHeight)
				{
					naturalWidth += 20; // allow generous room for scroll bar.
				}
				var width = Math.Max(Width, naturalWidth);
				sz.Width = Math.Min(width, workingArea.Width * 4 / 10);

			}
			else
			{
				//m_comboListBox.FormWidth = this.Size.Width;
				sz.Width = Width;
			}

			if (sz != m_dropDownBox.Form.Size)
			{
				m_dropDownBox.Form.Size = sz;
			}
#if __MonoCS__	// FWNX-748: ensure a launching form that is not m_dropDownBox itself.
// In Mono, Form.ActiveForm occasionally returns m_dropDownBox at this point.  So we
// try another approach to finding the launching form for displaying m_dropDownBox.
// Note that the launching form never changes, so it needs to be set only once.
			if (m_dropDownBox.LaunchingForm == null)
			{
				Control parent = this;
				Form launcher = parent as Form;
				while (parent != null && launcher == null)
				{
					parent = parent.Parent;
					launcher = parent as Form;
				}
				if (launcher != null)
					m_dropDownBox.LaunchingForm = launcher;
			}
#endif
			m_dropDownBox.Launch(Parent.RectangleToScreen(Bounds), workingArea);

			// for some reason, sometimes the size of the form changes after it has become visible, so
			// we change it back if we need to
			if (sz != m_dropDownBox.Form.Size)
			{
				m_dropDownBox.Form.Size = sz;
			}
		}

		/// <summary>
		/// Hides the drop down box.
		/// </summary>
		protected void HideDropDownBox()
		{
			m_dropDownBox.HideForm();
		}

		/// <summary>
		/// Raises the drop down event.
		/// </summary>
		protected void RaiseDropDown()
		{
			DropDown?.Invoke(this, new EventArgs());
		}

		void SetTextBoxHighlight()
		{
			// change control to highlight state.
			if (m_dropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!SupportsButtonStyle || !m_useVisualStyleBackColor)
				{
					if (TextBox.BackColor != SystemColors.Highlight)
					{
						TextBox.BackColor = SystemColors.Highlight;
					}

					if (TextBox.ForeColor != SystemColors.HighlightText)
					{
						TextBox.ForeColor = SystemColors.HighlightText;
					}
				}
			}
			else
			{
				TextBox.SelectAll();
			}
			Invalidate(true);
		}

		void RemoveTextBoxHighlight()
		{
			if (m_dropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!SupportsButtonStyle || !m_useVisualStyleBackColor)
				{
					// revert to original color state.
					TextBox.BackColor = BackColor;
					TextBox.ForeColor = ForeColor;
				}
			}
			Invalidate(true);
		}

		#endregion Other methods

		#region IVwNotifyChange implementation

		/// <summary />
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// Currently the only property that can change is the string,
			// but verify it in case we later make a mechanism to work with a shared
			// cache. If it is the right property, report TextChanged.
			if (tag != InnerFwTextBox.ktagText)
			{
				return;
			}
			OnTextChanged(new EventArgs());
		}

		#endregion IVwNotifyChange implementation

		#region Event handlers

		private void m_comboTextBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (DropDownStyle == ComboBoxStyle.DropDownList)
			{
				Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
				Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_LBUTTONDOWN, 0, 0);
				Win32.PostMessage(m_button.Handle, Win32.WinMsgs.WM_LBUTTONUP, 0, 0);
			}
		}

		private void m_button_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				RaiseDropDown();
				ShowDropDownBox();
			}
		}

		private void m_button_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == (Keys.Down | Keys.Alt))
			{
				RaiseDropDown();
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_comboTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == (Keys.Down | Keys.Alt))
			{
				RaiseDropDown();
				ShowDropDownBox();
				e.Handled = true;
			}
		}

		private void m_comboTextBox_GotFocus(object sender, EventArgs e)
		{
			SetTextBoxHighlight();
		}

		private void m_comboTextBox_LostFocus(object sender, EventArgs e)
		{
			RemoveTextBoxHighlight();
		}

		private void m_button_GotFocus(object sender, EventArgs e)
		{
			SetTextBoxHighlight();
		}

		private void m_button_LostFocus(object sender, EventArgs e)
		{
			RemoveTextBoxHighlight();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.EnabledChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			if (Application.RenderWithVisualStyles)
			{
				State = Enabled ? ComboBoxState.Normal : ComboBoxState.Disabled;
			}
		}

		private void Form_VisibleChanged(object sender, EventArgs e)
		{
			if (Application.RenderWithVisualStyles)
			{
				State = m_dropDownBox.Form.Visible ? ComboBoxState.Pressed : ComboBoxState.Normal;
			}
		}

		#endregion Event handlers
	}
}