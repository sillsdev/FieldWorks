using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using System.Diagnostics;
using SIL.Utils; // for Win32 message defns.

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// IComboList describes the shared methods of FwComboBox and ComboListBox, allowing the two classes
	/// to be more easily used as alternates for similar purposes.
	/// </summary>
	public interface IComboList
	{
		/// <summary></summary>
		event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Gets or sets the drop down style.
		/// </summary>
		/// <value>The drop down style.</value>
		ComboBoxStyle DropDownStyle { get; set; }
		/// <summary></summary>
		int SelectedIndex { get; set; }
		/// <summary></summary>
		string Text { get; set; }
		/// <summary></summary>
		FwListBox.ObjectCollection Items { get; }
		/// <summary></summary>
		int FindStringExact(string str);
		/// <summary>
		/// Get or set the writing system factory used to interpret strings. It's important to set this
		/// if using TsStrings for combo items.
		/// </summary>
		ILgWritingSystemFactory WritingSystemFactory { get; set ; }
		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		object SelectedItem { get; set; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		/// <value>The style sheet.</value>
		/// ------------------------------------------------------------------------------------
		IVwStylesheet StyleSheet { get; set; }
	}

	/// <summary>
	/// This interface is implemented by all drop down boxes.
	/// </summary>
	public interface IDropDownBox : IDisposable
	{
		/// <summary>
		/// Gets the drop down form.
		/// </summary>
		/// <value>The form.</value>
		Form Form { get; }
		/// <summary>
		/// Launches the drop down box.
		/// </summary>
		/// <param name="launcherBounds">The launcher bounds.</param>
		/// <param name="screenBounds">The screen bounds.</param>
		void Launch(Rectangle launcherBounds, Rectangle screenBounds);
		/// <summary>
		/// Hides the drop down box.
		/// </summary>
		void HideForm();
		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		int NaturalWidth { get; }
		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		int NaturalHeight { get; }
		/// <summary>
		/// Returns Control's IsDisposed bool.
		/// </summary>
		bool IsDisposed { get; }
	}

	/// <summary>
	/// This is an abstract class that provides the basic functionality for a Views-enabled
	/// ComboBox. Classes that extend this class provide an implementation of the drop down
	/// box.
	/// </summary>
	public abstract class FwComboBoxBase : UserControl, IFWDisposable, IVwNotifyChange, IWritingSystemAndStylesheet
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
		/// The combo text box
		/// </summary>
		protected ComboTextBox m_comboTextBox;
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
		protected bool m_fListWidthSet = false;
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
		public FwComboBoxBase()
		{
			if (Application.RenderWithVisualStyles)
				DoubleBuffered = true;

			SuspendLayout();
			// Set this box's own properties (first, as we use some of them in figuring the
			// size of other things).

			// Make and install the ComboTextBox
			m_comboTextBox = new ComboTextBox(this);
			m_comboTextBox.AccessibleName = "TextBox";
			m_comboTextBox.Dock = DockStyle.Fill;
			m_comboTextBox.Visible = true;

			// This causes us to get a notification when the string gets changed, so we can fire our
			// TextChanged event.
			m_sda = m_comboTextBox.DataAccess;
			m_sda.AddNotification(this);

			m_comboTextBox.KeyDown += m_comboTextBox_KeyDown;
			m_comboTextBox.MouseDown += m_comboTextBox_MouseDown;
			m_comboTextBox.GotFocus += m_comboTextBox_GotFocus;
			m_comboTextBox.LostFocus += m_comboTextBox_LostFocus;
			m_comboTextBox.TabIndex = 1;
			m_comboTextBox.TabStop = true;

			m_textBoxPanel = new Panel();
			m_textBoxPanel.AccessibleName = "TextBoxPanel";
			m_textBoxPanel.Dock = DockStyle.Fill;
			m_textBoxPanel.BackColor = Color.Transparent;
			m_textBoxPanel.Controls.Add(m_comboTextBox);
			Controls.Add(m_textBoxPanel);

			// Make and install the button that pops up the list.
			m_button = new DropDownButton(this);
			m_button.AccessibleName = "DropDownButton";
			m_button.Dock = DockStyle.Right; // Enhance JohnT: Left if RTL language?
			m_button.TabStop = false;

			//m_button.FlatStyle = FlatStyle.Flat; // no raised edges etc for this button.
			////			m_button.Click += new EventHandler(m_button_Click);

			m_button.MouseDown += m_button_MouseDown;
			m_button.KeyDown += m_button_KeyDown;
			m_button.GotFocus += m_button_GotFocus;
			m_button.LostFocus += m_button_LostFocus;

			m_buttonPanel = new Panel();
			m_buttonPanel.AccessibleName = "DropDownButtonPanel";
			m_buttonPanel.Dock = DockStyle.Right;
			m_buttonPanel.BackColor = Color.Transparent;
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
		/// <returns></returns>
		protected abstract IDropDownBox CreateDropDownBox();

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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

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

				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_comboTextBox != null)
				{
					m_comboTextBox.KeyDown -= m_comboTextBox_KeyDown;
					m_comboTextBox.MouseDown -= m_comboTextBox_MouseDown;
					m_comboTextBox.GotFocus -= m_comboTextBox_GotFocus;
					m_comboTextBox.LostFocus -= m_comboTextBox_LostFocus;
				}

				if (m_dropDownBox != null && !m_dropDownBox.IsDisposed)
				{
					m_dropDownBox.Form.VisibleChanged -= Form_VisibleChanged;
					m_dropDownBox.Dispose();
				}
			}
			m_sda = null;
			m_button = null;
			m_comboTextBox = null;
			m_dropDownBox = null;

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

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
		/// <value>
		/// 	<c>true</c> if a combo box can be rendered as a button, otherwise <c>false</c>.
		/// </value>
		internal static bool SupportsButtonStyle
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return false;
				VisualStyleElement element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_READONLY, (int)ComboBoxState.Normal);
				return VisualStyleRenderer.IsElementDefined(element);
			}
		}

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return ClientRectangle;

				if (!m_hasBorder && (!SupportsButtonStyle || !m_useVisualStyleBackColor || m_dropDownStyle != ComboBoxStyle.DropDownList))
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
				{
					var renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
				}
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


		VisualStyleRenderer Renderer
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return null;

				VisualStyleElement element = null;
				if (m_dropDownStyle == ComboBoxStyle.DropDownList && m_useVisualStyleBackColor)
					element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_READONLY, (int)m_state);
				if (element != null && VisualStyleRenderer.IsElementDefined(element))
					return new VisualStyleRenderer(element);

				if (!m_hasBorder)
					return null;

				if (ContainsFocus)
				{
					element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_FOCUSED);
					if (!VisualStyleRenderer.IsElementDefined(element))
						element = VisualStyleElement.TextBox.TextEdit.Focused;
				}
				else
				{
					switch (m_state)
					{
						case ComboBoxState.Normal:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_NORMAL);
							if (!VisualStyleRenderer.IsElementDefined(element))
								element = VisualStyleElement.TextBox.TextEdit.Normal;
							break;
						case ComboBoxState.Hot:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_HOT);
							if (!VisualStyleRenderer.IsElementDefined(element))
								element = VisualStyleElement.TextBox.TextEdit.Hot;
							break;
						case ComboBoxState.Pressed:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_HOT);
							if (!VisualStyleRenderer.IsElementDefined(element))
								element = VisualStyleElement.TextBox.TextEdit.Hot;
							break;
						case ComboBoxState.Disabled:
							element = VisualStyleElement.CreateElement(COMBOBOX_CLASS, CP_BORDER, CBB_DISABLED);
							if (!VisualStyleRenderer.IsElementDefined(element))
								element = VisualStyleElement.TextBox.TextEdit.Disabled;
							break;
					}
				}

				return new VisualStyleRenderer(element);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var renderer = Renderer;
			if (renderer != null)
			{
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
				if (!m_useVisualStyleBackColor)
					e.Graphics.FillRectangle(new SolidBrush(BackColor), ContentRectangle);
				if (ContainsFocus && m_dropDownStyle == ComboBoxStyle.DropDownList)
				{
					Rectangle contentRect = ContentRectangle;
					Rectangle focusRect = new Rectangle(contentRect.Left + 1, contentRect.Top + 1,
						m_textBoxPanel.Width - 2, contentRect.Height - 2);
					ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
				}
			}
		}

		/// <summary>
		/// Also make this accessible for fine adjustments.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboTextBox TextBox
		{
			get
			{
				CheckDisposed();
				return m_comboTextBox;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		/// <value>The style sheet.</value>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_comboTextBox.StyleSheet;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.StyleSheet = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int ComboHeight
		{
			get
			{
				return 21;
			} // Seems to be the right figure to make a standard-looking combo.
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
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public abstract object SelectedItem
		{
			get;
			set;
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
		[BrowsableAttribute(true),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				CheckDisposed();

				if (m_comboTextBox == null)
					return String.Empty;

				return m_comboTextBox.Text;
			}
			set
			{
				CheckDisposed();

				m_comboTextBox.Text = value;
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
				CheckDisposed();
				return m_dropDownStyle;
			}
			set
			{
				CheckDisposed();

				if (value == m_dropDownStyle)
					return;
				Debug.Assert(value != ComboBoxStyle.Simple); // not (yet) supported.
				m_dropDownStyle = value;
				// if it's a DropDownList, then don't allow 'editable'
				m_comboTextBox.EditingHelper.Editable = m_dropDownStyle != ComboBoxStyle.DropDownList;
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ITsString Tss
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
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int WritingSystemCode
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
				return m_dropDownBox.Form.Width;
			}
			set
			{
				CheckDisposed();
				m_fListWidthSet = true;
				m_dropDownBox.Form.Width = value;
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
				return m_dropDownBox.Form.Visible;
			}
			set
			{
				CheckDisposed();

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
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ILgWritingSystemFactory WritingSystemFactory
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
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		/// <value></value>
		/// <returns>true if the control has focus; otherwise, false.
		/// </returns>
		public override bool Focused
		{
			get
			{
				return ContainsFocus;
			}
		}

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
				m_comboTextBox.Font = value;
				base.Font = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the preferred.
		/// </summary>
		/// <value>The height of the preferred.</value>
		/// ------------------------------------------------------------------------------------
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
					{
						return Math.Max(m_comboTextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical,
										m_button.PreferredHeight + m_buttonPanel.Padding.Vertical);
					}
					return m_comboTextBox.PreferredHeight + m_textBoxPanel.Padding.Vertical;
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
				SelectNextControl(null, forward, true, true, false);
		}

		/// <summary>
		/// Selects all of the text in the text box.
		/// </summary>
		public void SelectAll()
		{
			CheckDisposed();
			m_comboTextBox.SelectAll();
		}

		void SetPadding()
		{
			Rectangle rect = ContentRectangle;
			var padding = new Padding(rect.Left - ClientRectangle.Left, rect.Top - ClientRectangle.Top,
				ClientRectangle.Right - rect.Right, ClientRectangle.Bottom - rect.Bottom);

			m_textBoxPanel.Padding = new Padding(padding.Left + m_textPadding.Left, padding.Top + m_textPadding.Top, m_textPadding.Right,
				padding.Bottom + m_textPadding.Bottom);
			if (Application.RenderWithVisualStyles && !SupportsButtonStyle)
				m_buttonPanel.Padding = new Padding(0, padding.Top, padding.Right, padding.Bottom);
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
		/// Adjust, figuring the stylesheet based on the main window of the mediator.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="grower"></param>
		/// <param name="mediator"></param>
		public void AdjustForStyleSheet(Form parent, Control grower, XCore.Mediator mediator)
		{
			CheckDisposed();

			AdjustForStyleSheet(parent, grower, FontHeightAdjuster.StyleSheetFromMediator(mediator));
		}
		/// ------------------------------------------------------------------------------------
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
		/// <param name="parent">The parent.</param>
		/// <param name="grower">The grower.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public void AdjustForStyleSheet(Form parent, Control grower, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			if (StyleSheet == null)
				StyleSheet = stylesheet;
			int oldHeight = Height;
			int newHeight = Math.Max(oldHeight, PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta == 0)
			{
				int oldHeight2 = m_comboTextBox.Height;
				int newHeight2 = m_comboTextBox.PreferredHeight;
				if (newHeight2 > oldHeight2)
				{
					delta = newHeight2 - oldHeight2;
					newHeight += delta;
				}
			}
			if (delta != 0)
			{
				int oldTop = Top;
				Height = newHeight;
				// Need to get the inner box's height adjusted BEFORE we fix the string.
				PerformLayout();
				Tss = FontHeightAdjuster.GetUnadjustedTsString(Tss);
				if (grower != null)
				{
					bool anchorTop = ((((int)grower.Anchor) & ((int)AnchorStyles.Top)) != 0);
					bool anchorBottom = ((((int)grower.Anchor) & ((int)AnchorStyles.Bottom)) != 0);
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
					Top = oldTop;
			}
		}

		/// <summary>
		/// Shows the drop down box.
		/// </summary>
		protected void ShowDropDownBox()
		{
			CheckDisposed();

			Rectangle workingArea = Screen.GetWorkingArea(this);

			Size sz = m_dropDownBox.Form.Size;
			// Unless the programmer set an explicit width for the list box, make it a good size.
			if (!m_fListWidthSet)
			{
				int naturalHeight = m_dropDownBox.NaturalHeight;
				if (naturalHeight < 20)
					naturalHeight = 20; // don't let it be invisible if no items.
				sz.Height = Math.Min(naturalHeight, workingArea.Height * 4 / 10);

				int naturalWidth = m_dropDownBox.NaturalWidth;
				if (sz.Height < naturalHeight)
					naturalWidth += 20; // allow generous room for scroll bar.
				int width = Math.Max(Width, naturalWidth);
				sz.Width = Math.Min(width, workingArea.Width * 4 / 10);

			}
			else
			{
				//m_comboListBox.FormWidth = this.Size.Width;
				sz.Width = Width;
			}

			if (sz != m_dropDownBox.Form.Size)
				m_dropDownBox.Form.Size = sz;
			m_dropDownBox.Launch(Parent.RectangleToScreen(Bounds), workingArea);

			// for some reason, sometimes the size of the form changes after it has become visible, so
			// we change it back if we need to
			if (sz != m_dropDownBox.Form.Size)
				m_dropDownBox.Form.Size = sz;
		}

		/// <summary>
		/// Hides the drop down box.
		/// </summary>
		protected void HideDropDownBox()
		{
			CheckDisposed();

			m_dropDownBox.HideForm();
		}

		/// <summary>
		/// Raises the drop down event.
		/// </summary>
		protected void RaiseDropDown()
		{
			if (DropDown != null)
				DropDown(this, new EventArgs());
		}

		void SetTextBoxHighlight()
		{
			// change control to highlight state.
			if (m_dropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!SupportsButtonStyle || !m_useVisualStyleBackColor)
				{
					if (m_comboTextBox.BackColor != SystemColors.Highlight)
						m_comboTextBox.BackColor = SystemColors.Highlight;
					if (m_comboTextBox.ForeColor != SystemColors.HighlightText)
						m_comboTextBox.ForeColor = SystemColors.HighlightText;
				}
			}
			else
			{
				m_comboTextBox.SelectAll();
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
					m_comboTextBox.BackColor = BackColor;
					m_comboTextBox.ForeColor = ForeColor;
				}
			}
			Invalidate(true);
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
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// Currently the only property that can change is the string,
			// but verify it in case we later make a mechanism to work with a shared
			// cache. If it is the right property, report TextChanged.
			if (tag != InnerFwTextBox.ktagText)
				return;
			OnTextChanged(new EventArgs());
		}

		#endregion IVwNotifyChange implementation

		#region Event handlers

		void m_comboTextBox_MouseDown(object sender, MouseEventArgs e)
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

		void Form_VisibleChanged(object sender, EventArgs e)
		{
			if (Application.RenderWithVisualStyles)
				State = m_dropDownBox.Form.Visible ? ComboBoxState.Pressed : ComboBoxState.Normal;
		}

		#endregion Event handlers
	}

	/// <summary>
	/// FwComboBox is a simulation of a regular Windows.Forms.ComboBox. It has much the same interface, though not all
	/// events and properties are yet supported. There are two main differences:
	/// (1) It is implemented using FieldWorks Views, and hence can render Graphite fonts properly.
	/// (2) Item labels can be TsStrings, in which case, formatting of items can vary based on the properties of string runs.
	///
	/// To get this behavior, you can
	///		(a) Let the items actually be ITsStrings.
	///		(b) Let the items implement the SIL.FieldWorks.FDO.ITssValue interface, which has just one property, public ITsString AsTss {get;}
	///
	///	You must also pass your writing system factory to the FwComboBox (set the WritingSystemFactory property).
	///	Otherwise, the combo box will not be able to interpret the writing systems of any TsStrings it is asked to display.
	///	It will improve performance to do this even if you are not using TsString data.
	/// </summary>
	public class FwComboBox : FwComboBoxBase, IComboList
	{
		#region Events

		/// <summary></summary>
		public event EventHandler SelectedIndexChanged;

		#endregion Events

		#region Data members

		// The previous combo box text string
		private ITsString m_tssPrevious;

		#endregion Data members

		#region Construction and disposal

		/// <summary>
		/// Construct one.
		/// </summary>
		public FwComboBox()
		{
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
			var comboListBox = new ComboListBox();
			comboListBox.LaunchButton = m_button;	// set the button for processing
			comboListBox.SelectedIndexChanged += m_listBox_SelectedIndexChanged;
			comboListBox.SameItemSelected += m_listBox_SameItemSelected;
			comboListBox.TabStopControl = m_comboTextBox;
			return comboListBox;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_comboTextBox != null)
					m_comboTextBox.KeyPress -= m_comboTextBox_KeyPress;

				if (m_button != null)
					m_button.KeyPress -= m_button_KeyPress;

				if (ListBox != null)
				{
					ListBox.SelectedIndexChanged -= m_listBox_SelectedIndexChanged;
					ListBox.SameItemSelected -= m_listBox_SameItemSelected;
				}
			}
			if (m_tssPrevious != null)
			{
				Marshal.ReleaseComObject(m_tssPrevious);
				m_tssPrevious = null;
			}

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

		#region Properties

		/// <summary>
		/// Make the list box accessible so its height can be adjusted.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboListBox ListBox
		{
			get
			{
				CheckDisposed();
				return m_dropDownBox as ComboListBox;
			}
		}

		/// <summary>
		/// The index of the item currently selected.
		/// Review JohnT: what value should be returned if the user has edited the text box
		/// to a value not in the list? Probably -1...do we need to do more to ensure this?
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				CheckDisposed();
				return ListBox.SelectedIndex;
			}
			set
			{
				CheckDisposed();
				ListBox.SelectedIndex = value;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		/// <value>The style sheet.</value>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return base.StyleSheet;
			}
			set
			{
				CheckDisposed();

				base.StyleSheet = value;
				ListBox.StyleSheet = value;
			}
		}

		/// <summary>
		/// Retrieve the list of items in the menu. Changes may be made to this to affect
		/// the visible menu contents.
		/// </summary>
		public FwListBox.ObjectCollection Items
		{
			get
			{
				CheckDisposed();
				return ListBox.Items;
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
				return ListBox.SelectedItem;
			}
			set
			{
				CheckDisposed();

				ListBox.SelectedItem = value;
				if (value != null)
					m_comboTextBox.Tss = ListBox.TextOfItem(value);
			}
		}

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[BrowsableAttribute(true),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				CheckDisposed();

				return base.Text;
			}
			set
			{
				CheckDisposed();

				base.Text = value;
				ListBox.SelectedIndex = ListBox.FindStringExact(value);
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ITsString Tss
		{
			get
			{
				CheckDisposed();

				return base.Tss;
			}
			set
			{
				CheckDisposed();

				base.Tss = value;
				// Don't just set the SelectedTss, as that throws an exception if not found.
				ListBox.SelectedIndex = ListBox.FindIndexOfTss(value);
			}
		}

		/// <summary>
		/// This is used (e.g., in filter bar) when the text we want to show in the combo
		/// is something different from the text of the selected item.
		/// </summary>
		/// <param name="tss"></param>
		public void SetTssWithoutChangingSelectedIndex(ITsString tss)
		{
			base.Tss = tss;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override int WritingSystemCode
		{
			get
			{
				CheckDisposed();
				return base.WritingSystemCode;
			}
			set
			{
				CheckDisposed();

				base.WritingSystemCode = value;
				ListBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return base.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				base.WritingSystemFactory = value;
				ListBox.WritingSystemFactory = value;
			}
		}

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
				base.Font = value;
				ListBox.Font = value;
			}
		}

		/// <summary>
		/// This property contains the previous text box text string
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString PreviousTextBoxText
		{
			get
			{
				CheckDisposed();
				return m_tssPrevious;
			}
			set
			{
				CheckDisposed();
				m_tssPrevious = value;
			}
		}

		#endregion Properties

		#region Other methods

		/// <summary>
		/// Add items to the FWComboBox but adjust the string so it
		/// matches the Font size.
		/// </summary>
		/// <param name="tss"></param>
		public void AddItem(ITsString tss)
		{
			CheckDisposed();

			//first calculate things to we adjust the font to the correct size.
			int mpEditHeight = FwTextBox.GetDympMaxHeight(m_comboTextBox);
			ITsString tssAdjusted;
			tssAdjusted = FontHeightAdjuster.GetAdjustedTsString(tss, mpEditHeight, StyleSheet,
						WritingSystemFactory);
			Items.Add(tssAdjusted);
		}


		/// <summary>
		/// Find the index where exactly this string occurs in the list, or -1 if it does not.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public int FindStringExact(string str)
		{
			CheckDisposed();

			return ListBox.FindStringExact(str);
		}

		/// <summary>
		/// Find the index where exactly this string occurs in the list, or -1 if it does not.
		/// </summary>
		/// <param name="tss"></param>
		/// <returns></returns>
		public int FindStringExact(ITsString tss)
		{
			CheckDisposed();

			return ListBox.FindIndexOfTss(tss);
		}

		/// <summary>
		/// Fire the SelectedIndexChanged event.
		/// </summary>
		protected void RaiseSelectedIndexChanged()
		{
			if (SelectedIndexChanged != null)
				SelectedIndexChanged(this, EventArgs.Empty);
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
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// Currently the only property that can change is the string,
			// but verify it in case we later make a mechanism to work with a shared
			// cache. If it is the right property, report TextChanged.
			if (tag != InnerFwTextBox.ktagText)
				return;
			ListBox.IgnoreSelectedIndexChange = true;
			try
			{
				ListBox.SelectedIndex = ListBox.FindIndexOfTss(m_comboTextBox.Tss);
			}
			finally
			{
				ListBox.IgnoreSelectedIndexChange = false;
			}

			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		#endregion IVwNotifyChange implementation

		#region Event handlers

		private void m_listBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// If the selection came from choosing in the list box, we need to hide the list
			// box and focus on the text box.
			bool fNeedFocus = false;
			if (ListBox.Form != null && ListBox.Form.Visible && !ListBox.KeepDropDownListDuringSelection)
			{
				HideDropDownBox();
				fNeedFocus = true;
			}
			if (ListBox.SelectedIndex == -1)
			{
				if (DropDownStyle != ComboBoxStyle.DropDown)
				{
					// Clear out the text box, since there isn't a selection now.
					// (Don't do this if typing is allowed, because we get a selection change to -1
					// when the user types something not in the list, and we don't want to erase what
					// the user typed.)
					PreviousTextBoxText = m_comboTextBox.Tss;	// save for future use (if needed)
					ITsStrBldr bldr = m_comboTextBox.Tss.GetBldr();
					ITsTextProps props = bldr.get_Properties(0);
					string str = bldr.GetString().Text;
					int cStr = str != null ? str.Length : 0;
					bldr.Replace(0, cStr, String.Empty, props);
					// Can't do this since string might be null.
					// bldr.Replace(0, bldr.GetString().Text.Length, String.Empty, props);
					m_comboTextBox.Tss = bldr.GetString();
				}
			}
			else
			{
				// It's a real selection, so copy to the text box.
				PreviousTextBoxText = m_comboTextBox.Tss;	// save for future use (if needed)
				m_comboTextBox.Tss = ListBox.SelectedTss;
			}
			if (fNeedFocus)
				m_comboTextBox.Focus();
			// Finally notify our own delegates.
			RaiseSelectedIndexChanged();
		}
		private void m_listBox_SameItemSelected(object sender, EventArgs e)
		{
			if (!ListBox.KeepDropDownListDuringSelection)
			{
				HideDropDownBox();
				m_comboTextBox.Focus();
			}
		}

		/// <summary>
		/// Handle a key press in the combo box. If it is a dropdown list
		/// use typing to try to make a selection (cf. LT-2190).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_comboTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// These have important meanings we don't want to suppress by saying we handled it.
			if (e.KeyChar == '\t' || e.KeyChar == '\r' || e.KeyChar == (char)Win32.VirtualKeycodes.VK_ESCAPE)
				return;
			if (DropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!Char.IsControl(e.KeyChar))
				{
					// We should drop the list down first, so that
					// ScrollHighlightIntoView() will execute.
					DroppedDown = true;
					ListBox.HighlightItemStartingWith(e.KeyChar.ToString());
				}
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handle a key press in the combo box. If it is a dropdown list
		/// use typing to try to make a selection (cf. LT-2190).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_button_KeyPress(object sender, KeyPressEventArgs e)
		{
			// These have important meanings we don't want to suppress by saying we handled it.
			if (e.KeyChar == '\t' || e.KeyChar == '\r' || e.KeyChar == (char)Win32.VirtualKeycodes.VK_ESCAPE)
				return;
			if (DropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!Char.IsControl(e.KeyChar))
				{
					// We should drop the list down first, so that
					// ScrollHighlightIntoView() will execute.
					DroppedDown = true;
					ListBox.HighlightItemStartingWith(e.KeyChar.ToString());
				}
				e.Handled = true;
			}
		}

		#endregion Event handlers
	}

	/// <summary>
	/// A "Combo" list box is one that can launch itself as a stand-alone, yet modal window, like the drop-down
	/// list in a Combo. It can also be used for other drop-down lists, for example, from an icon button.
	/// It is displayed using the Launch method. It is automatically hidden if the user clicks outside it
	/// (and the click is absorbed in the process). The item the user is hovering over is highlighted.
	/// </summary>
	public class ComboListBox : FwListBox, IComboList, IDropDownBox
	{
		#region Data members

		// This is a Form to contain the ListBox. I tried just making the list box visible,
		// but it seems only a Form can show up as a top-level window.
		Form m_listForm;
		// We track the form that was active when we launched, in hopes of working around
		// a peculiar bug that brings another window to the front when we close on some
		// systems (LT-2962).
		Form m_previousForm;
		// This filter captures clicks outside the list box while it is displayed.
		FwComboMessageFilter m_comboMessageFilter;
		// This flag determines whether we close the Dropdown List during a selection.
		private bool m_fKeepDropDownListOpen = false;

		// Button control that can contain the button that is used to bring up the list
		// This could be null / empty if not used.
		Button m_button;

		ComboBoxState m_state = ComboBoxState.Normal;

		private bool m_activateOnShow;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <value>The state.</value>
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
				m_button.Invalidate();
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
				return BorderStyle != BorderStyle.None;
			}

			set
			{
				CheckDisposed();
				BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
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
				return false;
			}

			set
			{
				CheckDisposed();
				// do nothing;
			}
		}

		/// <summary>
		/// This property exposes the button that is used to launch the list.
		/// </summary>
		public Button LaunchButton
		{
			get
			{
				CheckDisposed();
				return m_button;
			}
			set
			{
				CheckDisposed();
				m_button = value;
			}
		}
		/// <summary>
		/// Giving ComboListBox this property is a convenience for clisnts that wish to use
		/// it somewhat interchangeably with FwComboBox. The style is always DropDownList.
		/// </summary>
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

				// required interface method does nothing at all.
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool ActivateOnShow
		{
			get
			{
				CheckDisposed();
				return m_activateOnShow;
			}

			set
			{
				CheckDisposed();
				m_activateOnShow = value;
				m_listForm.TopMost = value;
			}
		}

		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		public int NaturalWidth
		{
			get
			{
				CheckDisposed();

				m_innerFwListBox.ShowHighlight = false;
				EnsureRoot();
				int result = m_innerFwListBox.RootBox.Width;
				m_innerFwListBox.ShowHighlight = true;
				return result + 5; // allows something for borders, etc.
			}
		}

		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		public int NaturalHeight
		{
			get
			{
				CheckDisposed();

				EnsureRoot();
				// The extra pixels are designed to be enough so that a scroll bar is not shown.
				// It allows for borders and so forth around the actual root box.
				return m_innerFwListBox.RootBox.Height + 10;
			}
		}

		/// <summary>
		/// Allows the control to be more like an FwComboBox for classes wishing to initialize both the
		/// same way. Value is always derived from the selected index. Setting has no effect unless
		/// the text matches a selected item.
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

				if (SelectedIndex < 0)
					return String.Empty;

				if (Items == null)
					return String.Empty;

				if (SelectedIndex >= Items.Count)
					return String.Empty;

				if (Items[SelectedIndex] == null)
					return String.Empty;

				return Items[SelectedIndex].ToString();
			}
			set
			{
				CheckDisposed();

				SelectedIndex = FindStringExact(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form Form
		{
			get
			{
				CheckDisposed();
				return m_listForm;
			}
		}

		#endregion Properties

		#region Construction and disposal


		/// <summary>
		/// Make one.
		/// </summary>
		public ComboListBox()
		{
			m_activateOnShow = true;
			HasBorder = true;
			Dock = DockStyle.Fill; // It fills the list form.
			// Create a form to hold the list.
			m_listForm = new Form {Size = Size, FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true};
			m_listForm.Controls.Add(this);
			m_listForm.Deactivate += m_ListForm_Deactivate;
			Tracking = true;

			// Make sure this isn't null, allow launch to update its value
			m_previousForm = Form.ActiveForm;

		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Don't call Controls.Clear() - we need to dispose the controls it contains.
				// This will be done in base class.
				if (m_listForm != null)
				{
					m_listForm.Deactivate -= m_ListForm_Deactivate;
					m_listForm.Controls.Remove(this);
					m_listForm.Close();
					m_listForm.Dispose();
				}
				if (m_comboMessageFilter != null)
				{
					Application.RemoveMessageFilter(m_comboMessageFilter);
					m_comboMessageFilter.Dispose();
				}
			}
			m_listForm = null;
			m_button = null;
			m_comboMessageFilter = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable & Co. implementation

		#endregion Construction and disposal

		#region Other methods

		/// <summary>
		/// Adjust the size of the list box, so it is just large enough to show all items,
		/// or at most the size specified by the arguments (or at least 100x10).
		/// </summary>
		/// <param name="maxHeight"></param>
		/// <param name="maxWidth"></param>
		public void AdjustSize(int maxWidth, int maxHeight)
		{
			CheckDisposed();

			int height = NaturalHeight;
			// Give ourselves a small margin of width, plus extra if we need a scroll bar.
			int width = NaturalWidth + (height > maxHeight ? 25 : 10);

			Form.Width = Math.Max(100, Math.Min(width, maxWidth));
			Form.Height = Math.Max(10, Math.Min(height, maxHeight));
		}

		/// <summary>
		/// Launch the ComboListBox.
		/// Typical usage, where 'this' is a control that the list should appear below:
		/// 		m_listBox.Launch(Parent.RectangleToScreen(Bounds), Screen.GetWorkingArea(this));
		/// Or, where rect is a rectangle in the client area of control 'this':
		///			m_listBox.Launch(RectangleToScreen(rect), Screen.GetWorkingArea(this);
		///	(Be sure to set the height and width of the ComboListBox first.)
		/// </summary>
		/// <param name="launcherBounds">A rectangle in 'screen' coordinates indicating where to display the list. Typically, as shown
		/// above, the location of something the user clicked to make the list display. It's significance is that
		/// the list will usually be shown with its top left just to the right of the bottom left of the rectangle, and
		/// (if the list width has not already been set explicitly) its width will match the rectangle. If there is not
		/// room to displya the list below this rectangle, it will be displayed above instead.</param>
		/// <param name="screenBounds">A rectangle in 'screen' coordinates indicating the location of the actual screen
		/// that the list is to appear on.</param>
		public void Launch(Rectangle launcherBounds, Rectangle screenBounds)
		{
			CheckDisposed();

			m_previousForm = Form.ActiveForm;
			m_listForm.ShowInTaskbar = false; // this is mainly to prevent it showing in the task bar.
			//Figure where to put it. First try right below the main combo box.
			// Pathologically the list box may be bigger than the available height. If so shrink it.
			int maxListHeight = Math.Max(launcherBounds.Top - screenBounds.Top,
				screenBounds.Bottom - launcherBounds.Bottom);
			if (m_listForm.Height > maxListHeight)
				m_listForm.Height = maxListHeight;
			// This is the default position right below the combo.
			var popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Bottom, m_listForm.Width, m_listForm.Height);
			if (screenBounds.Bottom < popupBounds.Bottom)
			{
				// extends below the bottom of the screen. Use a rectangle above instead.
				// We already made sure it will fit in one place or the other.
				popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Top - m_listForm.Height,
					m_listForm.Width, m_listForm.Height);
			}
			if (screenBounds.Right < popupBounds.Right)
			{
				// Extends too far to the right; adjust (amount is negative to move left).
				popupBounds.Offset(screenBounds.Right - popupBounds.Right, 0);
			}
			if (screenBounds.Left > popupBounds.Left)
			{
				// Extends too far to the left; adjust (amount is positive to move right).
				popupBounds.Offset(screenBounds.Left - popupBounds.Left, 0);
			}
			m_listForm.Location = new Point(popupBounds.Left, popupBounds.Top);

			if (m_activateOnShow)
				m_listForm.Show(m_previousForm);
			else
				ShowInactiveTopmost(m_previousForm, m_listForm);

			if (m_comboMessageFilter != null)
			{
				// losing our ref to m_comboMessageFilter, while it's still added to the Application Message Filter,
				// would mean it would never get removed. (Which would be very bad.)
				Application.RemoveMessageFilter(m_comboMessageFilter);
				m_comboMessageFilter.Dispose();
			}

			m_comboMessageFilter = new FwComboMessageFilter(this);
			Application.AddMessageFilter(m_comboMessageFilter);
			if (m_activateOnShow)
				FocusAndCapture();
		}

		private const int SW_SHOWNOACTIVATE = 4;
		private const int HWND_TOPMOST = -1;
		private const uint SWP_NOACTIVATE = 0x0010;

		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		private static extern bool SetWindowPos(
			 int hWnd,           // window handle
			 int hWndInsertAfter,    // placement-order handle
			 int X,          // horizontal position
			 int Y,          // vertical position
			 int cx,         // width
			 int cy,         // height
			 uint uFlags);       // window positioning flags

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private const int GWL_HWNDPARENT = -8;

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private static void ShowInactiveTopmost(Form owner, Form frm)
		{
			if (owner != null)
				SetWindowLong(frm.Handle, GWL_HWNDPARENT, owner.Handle.ToInt32());
			ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
			SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, SWP_NOACTIVATE);
		}

		/// <summary>
		/// Hide the containing form (and thus the list box as a whole).
		/// </summary>
		public void HideForm()
		{
			CheckDisposed();

			// The order of the following two lines is very important!  On some
			// machines the LT-2962 issue will show itself if this is changed.
			// The summary statement is that making the form not visible causes
			// the system to activate another application, and with out telling
			// it before hand what to activate it would get confused and cycle
			// through the applications that were currently running.  By
			// activating the main form first and then hiding the little form
			// the bug is not seen (and maybe not present...
			// but that's much like the forest and a falling tree debate.)
			//
			// ** Dont change the order of the following two lines **
			if (m_previousForm != null) // Somehow may not be, if no form is active when launched!
				m_previousForm.Activate();
			if (m_listForm != null)
				m_listForm.Visible = false;
			// reset HighlightedItem to current selected.
			HighlightedIndex = SelectedIndex;
			if (m_comboMessageFilter != null)
			{
				Application.RemoveMessageFilter(m_comboMessageFilter);
				m_comboMessageFilter.Dispose();
				m_comboMessageFilter = null;
			}
		}

		/// <summary>
		/// Cycle the selection to the next item that begins with the specified text.
		/// (Case insensitive. Trims whitespace in list items.)
		/// </summary>
		/// <param name="start">the search key</param>
		public void HighlightItemStartingWith(string start)
		{
			CheckDisposed();

			int iPreviousHighlighted = -1;
			int iStarting;
			string itemStr = string.Empty;

			if (HighlightedItem != null)
				itemStr = TextOfItem(HighlightedItem).Text.Trim();

			// If the new start key matches the start key for the current selection,
			// we'll start our search from there.
			if (HighlightedItem != null &&
				itemStr.ToLower().StartsWith(start.ToLower()))
			{
				// save the current position as our previous index
				iPreviousHighlighted = HighlightedIndex;
				// set our search index to one after the currently selected item
				iStarting = iPreviousHighlighted + 1;
			}
			else
			{
				// start our search from the beginning
				iStarting = 0;
			}

			int iEnding = Items.Count - 1;
			bool fFound = FindAndHighlightItemStartingWith(iStarting, iEnding, start);

			if (!fFound && iStarting != 0)
			{
				// Cycle from the beginning and see if we find a match before our
				// previous (i.e. current) selection.
				iStarting = 0;
				iEnding = iPreviousHighlighted - 1;
				FindAndHighlightItemStartingWith(iStarting, iEnding, start);
				// if we don't find a match, return quietly. Nothing else to select.
			}
			return;
		}

		/// <summary>
		/// Highlight the item whose text starts with the given startKey string (if item exists).
		/// </summary>
		/// <param name="iStarting">starting index</param>
		/// <param name="iEnding">ending index</param>
		/// <param name="startKey">search key</param>
		/// <returns></returns>
		private bool FindAndHighlightItemStartingWith(int iStarting, int iEnding, string startKey)
		{
			bool fFound = false;
			for (int i = iStarting; i <= iEnding; ++i)
			{
				string itemStr = TextOfItem(Items[i]).Text.Trim();
				if (itemStr.ToLower().StartsWith(startKey.ToLower()))
				{
					// Highlight this item
					HighlightedItem = Items[i];
					ScrollHighlightIntoView();
					fFound = true;
					break;
				}
			}
			return fFound;
		}

		/// <summary>
		/// Use this property to avoid closing the dropdown list during a selection.
		/// Client is required to reset the flag after selection is completed.
		/// (Default is false.)
		/// </summary>
		public bool KeepDropDownListDuringSelection
		{
			get
			{
				CheckDisposed();
				return m_fKeepDropDownListOpen;
			}
			set
			{
				CheckDisposed();
				m_fKeepDropDownListOpen = value;
			}
		}

		#endregion Other methods

		#region Event handlers

		private void m_ListForm_Deactivate(object sender, EventArgs e)
		{
			HideForm();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a combo list box loses focus, hide it.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus (e);
			HideForm();
		}
		/// <summary>
		/// This is called when the embedded InnerFwListBox detects a click outside its bounds,
		/// which happens only when it has been told to capture the mouse. ComboListBox overrides
		/// this to close the list box.
		/// </summary>
		public void OnCapturedClick()
		{
			HideForm();
		}

		#endregion Event handlers
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>Message filter for detecting events that may hide the compbo </summary>
	/// ------------------------------------------------------------------------------------
	internal class FwComboMessageFilter : IMessageFilter, IFWDisposable
	{
		private ComboListBox m_comboListbox;
		private bool m_fGotMouseDown; // true after a mouse down occurs anywhere at all.

		/// <summary>Constructor for filter object</summary>
		public FwComboMessageFilter(ComboListBox comboListbox)
		{
			m_comboListbox = comboListbox;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwComboMessageFilter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_comboListbox = null; // It is disposed of elsewhere.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		// DEBUGGING (used for tracking down lost left mouse button messages in the sandbox)
		//public static bool bShowRegMsgs = true;	// each time app run, first show fw regmsgs
		//private static long lTimerMsgCount = 0;

		/// --------------------------------------------------------------------------------
		/// <summary></summary>
		/// <param name="m">message to be filtered</param>
		/// <returns>true if the message is consumed, false to pass it on.</returns>
		/// --------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			// DEBUGGING (used for tracking down lost left mouse button messages in the sandbox)
			//Control cx = Control.FromHandle(m.HWnd);
			//string name = "<>";
			//if (cx != null)
			//	name = "<"+cx.GetType().ToString()+">";	// .AccessibleName;
			//
			//// registered 'fw' messages
			//if (bShowRegMsgs)
			//{
			//	string[] registeredFWMsgs = {
			//									"AfMenuBar ShowMenu",
			//									"AfMainWnd ShowHelp",
			//									"AfMainWnd Activate",
			//									"PendingFixKeyboard",
			//									"FwFilterSimpleDlg Condition Changed",
			//									"FwFilterBuilderShellDlg Activate",
			//									"WM_KMSELECTLANG",
			//									"WM_KMKBCHANGE" };
			//
			//	System.Diagnostics.Debug.WriteLine("*Registered FW Messages:");
			//	foreach (string s in registeredFWMsgs)
			//	{
			//		uint msgID = Win32.RegisterWindowMessage(s);
			//		System.Diagnostics.Debug.WriteLine("  0x" + msgID.ToString("X4") + " = \"" + s + "\"" );
			//	}
			//	bShowRegMsgs = false;
			//}
			//
			//
			//string wmHexString = "0x" + m.Msg.ToString("X4");
			//string wmString;
			//if (m.Msg >=0x8000 && m.Msg <= 0xbfff)
			//	wmString = "PrivateAppMsg(" + wmHexString + ")";
			//else if (m.Msg >= 0xc000)
			//	wmString = "RegisteredMsg(" + wmHexString + ")";
			//else if (m.Msg > 0xffff)
			//	wmString = "RESERVED SysMsg(" + wmHexString + ")";
			//else
			//{
			//	wmString = ((Win32.WinMsgs)m.Msg).ToString();
			//	if (wmString.StartsWith("WM_") == false)
			//		wmString = wmHexString;
			//}
			//
			//switch (m.Msg)
			//{
			//	case 275:
			//		lTimerMsgCount++;
			//		long rem;
			//		Math.DivRem(lTimerMsgCount, 100, out rem);
			//		if (rem == 0)
			//		{
			//			System.Diagnostics.Debug.WriteLine("WM_TIMER count = " + lTimerMsgCount);			 //
			//		}
			//		break;
			//	case 49332:	// an unknown registered msg 0xC0B4 that is seen A LOT!
			//	default:
			//		System.Diagnostics.Debug.WriteLine("WM=" + wmString +  " " + name);
			//
			//		break;
			//}
			switch ((Win32.WinMsgs)m.Msg)
			{
				case Win32.WinMsgs.WM_CHAR:
				{
					// Handle the Escape key by removing list if present with out selecting
					int wparam = m.WParam.ToInt32();
					Win32.VirtualKeycodes vkey = (Win32.VirtualKeycodes)(wparam & 0xffff);
					if (vkey == Win32.VirtualKeycodes.VK_ESCAPE)
					{
						m_comboListbox.OnCapturedClick();
						return true;
					}
					// Doesn't work, apparently Alt-up doesn't go through WM_CHAR.
					//				if ( vkey == Win32.VirtualKeycodes.VK_UP && ((wparam & (1 << 28)) != 0))
					//				{
					//					m_listbox.OnCapturedClick();
					//					return true;
					//				}
					return false;
				}
					////			case Win32.WinMsgs.WM_NCLBUTTONDOWN:
					////			case Win32.WinMsgs.WM_NCLBUTTONUP:
				case Win32.WinMsgs.WM_LBUTTONDOWN:
				case Win32.WinMsgs.WM_LBUTTONUP:
				{
					// Handle any mouse left button activity.
					// This is tricky. A mouse DOWN anywhere outside our control closes the window without
					// making a choice. This is done by calling OnCapturedClick, and happens if we get
					// to the end of this case WITHOUT returning false.
					// A mouse UP usually does the same thing (this would correspond to clicking an item,
					// then changing your mind and moving the mouse away from the control).
					// But a mouse UP must only do it AFTER there has been a mouse DOWN.
					// Otherwise, the control disappears if the user drags slightly outside the launch button
					// (or didn't hit it exactly) in the process of the original click that launched it.
					// Non-client areas include the title bar, menu bar, window borders,
					// and scroll bars. But the only one in our combo is the scroll bar.
					if ((Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONDOWN)
					{
						m_fGotMouseDown = true; // and go on to check whether it's inside
					}
					else
					{
						if (!m_fGotMouseDown)
							return false; // ignore mouse up until we get mouse down
					}
					Control c = Control.FromHandle(m.HWnd);
					// Clicking anywhere in an FwListBox, including it's scroll bar,
					// behaves normally.

					if ((c == m_comboListbox.InnerListBox || c == m_comboListbox.LaunchButton) && c.Visible)
					{
						int xPos = MiscUtils.LoWord(m.LParam);	// LOWORD(m.LParam);
						int yPos = MiscUtils.HiWord(m.LParam);	// HIWORD(m.LParam);

						if (xPos > 0x8fff || yPos < 0 ||	// x or y is negitive
							xPos > c.ClientSize.Width ||	// x is to big
							yPos > c.ClientSize.Height)		// y is to big
						{
							// this is our exit case - event outside of client space...
						}
						else
							return false;
					}
					// On Mono clicking on the Scrollbar causes return from Control.FromHandle
					// to be a ImplicitScrollBar which is a child of the ComboTextBox InnerListBox.
					if (c is ScrollBar && c.Parent == m_comboListbox.InnerListBox)
					{
						return false;
					}
					else if (c.GetType().ToString() == "SIL.FieldWorks.IText.Sandbox")
					{
						//Size lbSize = m_listbox.Bounds;
						int x1Pos = MiscUtils.LoWord(m.LParam);	// LOWORD(m.LParam);
						int y1Pos = MiscUtils.HiWord(m.LParam);	// HIWORD(m.LParam);

						// convert from one client to another client
						Win32.POINT screenPos;
						screenPos.x = x1Pos;
						screenPos.y = y1Pos;
						Win32.ClientToScreen(c.Handle, ref screenPos);
						Win32.ScreenToClient(m_comboListbox.Handle, ref screenPos);

						// Test the regular window, if fails then add 21 to the pos and
						// try it again (allow for the button up on the icon that
						// started this combolist).
						if (m_comboListbox.Bounds.Contains(screenPos.x, screenPos.y) ||
							m_comboListbox.Bounds.Contains(screenPos.x, screenPos.y+21))
							return false;
					}

					// Any other click is captured and turned into a message that causes the list box to go away.
					m_comboListbox.OnCapturedClick();
					// We want to consume mouse down, but not mouse up, because consuming mouse up
					// somehow suppresses the next click.
					return (Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONDOWN;
				}
				default:
					return false;
			}
		}
	}

	/// <summary>
	/// For some reason the embedded text box never gets a layout unless we do this.
	/// </summary>
	public class ComboTextBox : InnerFwTextBox
	{
		FwComboBoxBase m_comboBox;

		internal ComboTextBox(FwComboBoxBase comboBox)
		{
			m_comboBox = comboBox;
			// Allows it to be big unless client shrinks it.
			Font = new Font(Font.Name, (float)100.0);
			if (Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
				BackColor = Color.Transparent;
			}
			else
			{
				// And, if not changed, it's background color is white.
				BackColor = SystemColors.Window;
			}
		}

		/// <summary>
		/// Override to prevent scrolling in DropDownList mode.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="scrollOption"></param>
		/// <returns></returns>
		public override bool ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			if (m_comboBox != null && m_comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
			{
				// no meaningful selections are possible, no reason ever to scroll it.

				// That's true as long as we always left-align.
				// If we use right-alignment with a huge width to prevent wrapping, no scrolling means the text is invisible,
				// somewhere way off to the right. We'd then need something like this, but better, because this doesn't
				// work when the combo is resized, as when you change the size of a column and the filter bar combo resizes.
				// See also what I did in OnSizeChanged.
				if (Rtl)
				{
					// But, if it is RTL, we need to scroll, typically only once, to make it as visible as possible.
					// Right alignment otherwise puts the string way off to the right.
					var initialSel = m_rootb.MakeSimpleSel(true, false, false, false);
					base.ScrollSelectionIntoView(initialSel, VwScrollSelOpts.kssoDefault);
				}
				return false;
			}
			return base.ScrollSelectionIntoView(sel, scrollOption);
		}

		/// <summary>
		/// We need to kludge to make sure the content stays visible in RTL scripts.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_rootb == null || !Rtl)
				return;
			// To get the text aligned as well as we readily can, first scroll to show all of it,
			// then if need be again to see the start.
			BaseMakeSelectionVisible(m_rootb.MakeSimpleSel(true, false, true, false));
			BaseMakeSelectionVisible(m_rootb.MakeSimpleSel(true, false, false, false));
		}

		internal override void RemoveNonRootNotifications()
		{
			base.RemoveNonRootNotifications();
			DataAccess.RemoveNotification(this);
			DataAccess.RemoveNotification(m_comboBox);
		}

		internal override void RestoreNonRootNotifications()
		{
			base.RestoreNonRootNotifications();
			DataAccess.AddNotification(this);
			DataAccess.AddNotification(m_comboBox);
		}

		/// <summary>
		/// Raises the <see cref="E:Paint"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Application.RenderWithVisualStyles)
			{
				var renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
				renderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
			}
			base.OnPaint(e);
		}

		/// <summary>
		/// Stupid required comment!
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			PerformLayout();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseEnter"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Hot;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Normal;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Hot;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class DropDownButton : Button
	{
		FwComboBoxBase m_comboBox;
		bool m_isHot = false;
		bool m_isPressed = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="DropDownButton"/> class.
		/// </summary>
		/// <param name="comboBox">The combo box.</param>
		public DropDownButton(FwComboBoxBase comboBox)
		{
			m_comboBox = comboBox;
			if (Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
			}
			else
			{
				Image = ResourceHelper.ComboMenuArrowIcon; // no text, just the image
				BackColor = SystemColors.Control;
			}
		}

		/// <summary>
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The default <see cref="T:System.Drawing.Size"/> of the control.
		/// </returns>
		protected override Size DefaultSize
		{
			get
			{
				return new Size(PreferredWidth, PreferredHeight);
			}
		}

		const int CP_DROPDOWNBUTTON = 1;
		const int CP_DROPDOWNBUTTONRIGHT = 6;

		VisualStyleRenderer Renderer
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return null;

				VisualStyleElement element;
				if (FwComboBoxBase.SupportsButtonStyle)
				{
					ComboBoxState curState;
					if (m_comboBox == null)
					{
						curState = ComboBoxState.Normal;
					}
					else if (m_comboBox.UseVisualStyleBackColor && m_comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
					{
						curState = m_comboBox.State == ComboBoxState.Disabled ? ComboBoxState.Disabled : ComboBoxState.Normal;
					}
					else
					{
						switch (m_comboBox.State)
						{
							case ComboBoxState.Pressed:
							case ComboBoxState.Disabled:
								curState = m_comboBox.State;
								break;

							default:
								curState = m_isHot ? ComboBoxState.Hot : ComboBoxState.Normal;
								break;
						}
					}
					element = VisualStyleElement.CreateElement(FwComboBoxBase.COMBOBOX_CLASS, CP_DROPDOWNBUTTONRIGHT, (int)curState);
				}
				else
				{
					ComboBoxState curState;
					if (m_comboBox == null)
						curState = ComboBoxState.Normal;
					else if (m_comboBox.State == ComboBoxState.Pressed)
						curState = m_isPressed ? ComboBoxState.Pressed : ComboBoxState.Normal;
					else
						curState = m_comboBox.State;
					element = VisualStyleElement.CreateElement(FwComboBoxBase.COMBOBOX_CLASS, CP_DROPDOWNBUTTON, (int)curState);
				}
				return new VisualStyleRenderer(element);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			VisualStyleRenderer renderer = Renderer;
			if (renderer != null)
			{
				if (renderer.IsBackgroundPartiallyTransparent())
					renderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
			}
		}

		/// <summary>
		/// Gets the height of the preferred.
		/// </summary>
		/// <value>The height of the preferred.</value>
		public int PreferredHeight
		{
			get
			{
				VisualStyleRenderer renderer = Renderer;
				if (renderer != null)
				{
					using (Graphics g = CreateGraphics())
						return renderer.GetPartSize(g, ThemeSizeType.True).Height;
				}
				return FwComboBoxBase.ComboHeight;
			}
		}

		/// <summary>
		/// Gets the width of the preferred.
		/// </summary>
		/// <value>The width of the preferred.</value>
		public int PreferredWidth
		{
			get
			{
				// this seems more correct than what VisualStyleRenderer.GetPartSize returns,
				// this also seems decent for classic L&F
				return 17;
			}
		}

		/// <summary>
		/// Notifies the <see cref="T:System.Windows.Forms.Button"/> whether it is the default button so that it can adjust its appearance accordingly.
		/// </summary>
		/// <param name="value">true if the button is to have the appearance of the default button; otherwise, false.</param>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		public override void NotifyDefault(bool value)
		{
			base.NotifyDefault(false);
		}

		/// <summary>
		/// Gets a value indicating whether the control should display focus rectangles.
		/// </summary>
		/// <value></value>
		/// <returns>true if the control should display focus rectangles; otherwise, false.
		/// </returns>
		protected override bool ShowFocusCues
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Normal;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Normal;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseEnter"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Hot;
			m_isHot = true;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Normal;
			m_isHot = false;
			if (m_isPressed)
			{
				m_isPressed = false;
				Invalidate();
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
				m_comboBox.State = ComboBoxState.Hot;
			if (m_isPressed)
			{
				m_isPressed = false;
				Invalidate();
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseDown"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Disabled)
			{
				m_comboBox.State = ComboBoxState.Pressed;
				m_isPressed = true;
			}
		}
	}
}
