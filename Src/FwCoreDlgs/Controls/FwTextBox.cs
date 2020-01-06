// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// FwTextBox is a simulation of a regular Windows.Forms.TextBox. It has much the same
	/// interface, though not all events and properties are yet supported. There are two main
	/// differences:
	/// (1) It is implemented using FieldWorks Views, and hence can render Graphite fonts
	///     properly.
	/// (2) You can read and write the contents as an ITsString, using the Tss property,
	///		allowing formatting to vary based on the properties of string runs.
	///
	///	Although there is not yet any support for the user to alter run properties while inside
	///	the FwTextBox, it is possible to paste text from elsewhere in an FW application complete
	///	with style information.
	///
	///	You must also pass your writing system factory to the FwTextBox (set the
	///	WritingSystemFactory property).  Otherwise, the combo box will not be able to interpret
	///	the writing systems of any TsStrings it is asked to display.  It will improve performance
	///	to do this even if you are not using TsString data.
	/// </summary>
	public class FwTextBox : UserControl, IVwNotifyChange, ISupportInitialize
	{
		#region Data Members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		/// <remarks>This is strictly a sandbox cache.</remarks>
		private ISilDataAccess m_sda;

		/// <summary>The rootsite that occupies 100% of the rectangle of this control</summary>
		private InnerFwTextBox m_innerFwTextBox;

		private bool m_hasBorder;
		private bool m_isHot;
		private Padding m_textPadding;

		#endregion Data Members

		#region Construction and destruction

		/// <summary />
		public FwTextBox()
		{
			var inDesigner = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
			m_innerFwTextBox = new InnerFwTextBox { InDesigner = inDesigner };

			if (Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
			}

			HasBorder = true;
			Padding = Application.RenderWithVisualStyles ? new Padding(2) : new Padding(1, 2, 1, 1);
			m_innerFwTextBox.Dock = DockStyle.Fill;
			Controls.Add(m_innerFwTextBox);
			if (!inDesigner)
			{
				// This causes us to get a notification when the string gets changed,
				// so we can fire our TextChanged event.
				m_sda = m_innerFwTextBox.DataAccess;
				m_sda.AddNotification(this);
				m_innerFwTextBox.LostFocus += OnInnerTextBoxLostFocus;
				m_innerFwTextBox.GotFocus += m_innerFwTextBox_GotFocus;
				m_innerFwTextBox.MouseEnter += m_innerFwTextBox_MouseEnter;
				m_innerFwTextBox.MouseLeave += m_innerFwTextBox_MouseLeave;
			}

			// This makes it, by default if the container's initialization doesn't change it,
			// the same default size as a standard text box.
			Size = new Size(100, 22);

			// And, if not changed, it's background color is white.
			BackColor = SystemColors.Window;
			// Since the TE team put a limit on the text height based on the control's Font,
			// we want a default font size that is big enough never to limit things.
			Font = new Font(Font.Name, 100.0f);

			// We don't want to auto scale because that messes up selections. You can see this
			// by commenting this line. If FwFindReplaceDlg.AutoScaleMode is set to Font the test
			// SIL.FieldWorks.FwCoreDlgs.FwFindReplaceDlgTests.ApplyWS_ToSelectedString will
			// fail because it didn't make a range selection.
			AutoScaleMode = AutoScaleMode.None;
		}

		private Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles || !m_hasBorder)
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
		/// Adjust the height of the text box appropriately for the given stylesheet.
		/// This version moves any controls below the text box, but does not adjust the size of the parent control.
		/// </summary>
		public void AdjustForStyleSheet(IVwStylesheet ss)
		{
			var oldHeight = Height;
			StyleSheet = ss;
			var newHeight = Math.Max(oldHeight, PreferredHeight);
			var delta = newHeight - oldHeight;
			if (delta == 0)
			{
				return;
			}
			Height = newHeight;
			// Need to get the inner box's height adjusted BEFORE we fix the string.
			PerformLayout();
			Tss = FontHeightAdjuster.GetUnadjustedTsString(Tss);
			foreach (Control c in Parent.Controls)
			{
				if (c == this)
				{
					continue;
				}
				var anchorTop = ((int)c.Anchor & (int)AnchorStyles.Top) != 0;
				if (c.Top > Top && anchorTop)
				{
					// Anchored at the top and below our control: move it down
					c.Top += delta;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the text box has a border.
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
		/// Gets or sets the border style of the tree view control. If the application is using visual styles, this has no effect.
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.BorderStyle"/> values. The default is <see cref="F:System.Windows.Forms.BorderStyle.Fixed3D"/>.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The assigned value is not one of the <see cref="T:System.Windows.Forms.BorderStyle"/> values.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.Padding"/> representing the control's internal spacing characteristics.
		/// </returns>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		/// True if this text box allows the Enter key to insert a newline.
		/// </summary>
		public bool SuppressEnter
		{
			get { return m_innerFwTextBox.SuppressEnter; }
			set { m_innerFwTextBox.SuppressEnter = value; }
		}

		/// <summary>
		/// calculate the height of the edit boxes in millipoints.
		/// </summary>
		internal static int GetDympMaxHeight(Control control)
		{
			using (var gr = control.CreateGraphics())
			{
				// use height of client area, which does not include borders, scrollbars, etc.
				var mpEditHeight = (control.ClientSize.Height - control.Padding.Vertical) * 72000 / (int)gr.DpiY;
				// Make sure we don't go bigger than the size that was specified in the designer.
				mpEditHeight = Math.Min(mpEditHeight, FontHeightAdjuster.GetFontHeight(control.Font));
				return mpEditHeight;
			}
		}

		/// <summary>
		/// Handles the GotFocus event of the m_innerFwTextBox control.
		/// </summary>
		private void m_innerFwTextBox_GotFocus(object sender, EventArgs e)
		{
			OnGotFocus(e);
			Invalidate();
		}

		/// <summary>
		/// Gets the height the box would like to be to neatly display its current data.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				var borderHeight = 0;
				switch (BorderStyle)
				{
					case BorderStyle.Fixed3D:
						borderHeight = SystemInformation.Border3DSize.Height * 2;
						break;

					case BorderStyle.FixedSingle:
						borderHeight = SystemInformation.BorderSize.Height * 2;
						break;
				}
				return m_innerFwTextBox.PreferredHeight + base.Padding.Vertical + borderHeight;
			}
		}

		/// <summary>
		/// Get the preferred width given the current stylesheet and string.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredWidth
		{
			get
			{
				var borderWidth = 0;
				switch (BorderStyle)
				{
					case BorderStyle.Fixed3D:
						borderWidth = SystemInformation.Border3DSize.Width * 2;
						break;

					case BorderStyle.FixedSingle:
						borderWidth = SystemInformation.BorderSize.Width * 2;
						break;
				}
				return m_innerFwTextBox.PreferredWidth + base.Padding.Horizontal + borderWidth;
			}
		}

		/// <summary>
		/// true to adjust font height to fix box. When set false, client will normally
		/// call PreferredHeight and adjust control size to suit.
		/// </summary>
		public bool AdjustStringHeight
		{
			get
			{
				return m_innerFwTextBox.AdjustStringHeight;
			}
			set
			{
				m_innerFwTextBox.AdjustStringHeight = value;
			}
		}

		private void m_innerFwTextBox_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		private void m_innerFwTextBox_MouseLeave(object sender, EventArgs e)
		{
			m_isHot = false;
			Invalidate();
		}

		const string EDIT_CLASS = "EDIT";
		const int EP_EDITBORDER_NOSCROLL = 6;
		const int EPSN_NORMAL = 1;
		const int EPSN_HOT = 2;
		const int EPSN_FOCUSED = 3;
		const int EPSN_DISABLED = 4;

		internal static VisualStyleRenderer CreateRenderer(TextBoxState state, bool focused, bool hasBorder)
		{
			if (!Application.RenderWithVisualStyles || !hasBorder)
			{
				return null;
			}

			VisualStyleElement element = null;
			if (focused)
			{
				element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_FOCUSED);
				if (!VisualStyleRenderer.IsElementDefined(element))
				{
					element = VisualStyleElement.TextBox.TextEdit.Focused;
				}
			}
			else
			{
				switch (state)
				{
					case TextBoxState.Hot:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_HOT);
						if (!VisualStyleRenderer.IsElementDefined(element))
						{
							element = VisualStyleElement.TextBox.TextEdit.Hot;
						}
						break;

					case TextBoxState.Normal:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_NORMAL);
						if (!VisualStyleRenderer.IsElementDefined(element))
						{
							element = VisualStyleElement.TextBox.TextEdit.Normal;
						}
						break;

					case TextBoxState.Disabled:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_DISABLED);
						if (!VisualStyleRenderer.IsElementDefined(element))
						{
							element = VisualStyleElement.TextBox.TextEdit.Disabled;
						}
						break;
				}
			}

			return new VisualStyleRenderer(element);
		}

		private TextBoxState State => Enabled ? (m_isHot ? TextBoxState.Hot : TextBoxState.Normal) : TextBoxState.Disabled;

		private void SetPadding()
		{
			var rect = ContentRectangle;
			base.Padding = new Padding((rect.Left - ClientRectangle.Left) + m_textPadding.Left,
				(rect.Top - ClientRectangle.Top) + m_textPadding.Top, (ClientRectangle.Right - rect.Right) + m_textPadding.Right,
				(ClientRectangle.Bottom - rect.Bottom) + m_textPadding.Bottom);
		}

		/// <summary />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			CreateRenderer(State, Focused, m_hasBorder)?.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		#region IDisposable override

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
				// Dispose managed resources here.
				m_sda?.RemoveNotification(this);

				if (m_innerFwTextBox != null)
				{
					Controls.Remove(m_innerFwTextBox);
					m_innerFwTextBox.LostFocus -= OnInnerTextBoxLostFocus;
					m_innerFwTextBox.GotFocus -= m_innerFwTextBox_GotFocus;
					m_innerFwTextBox.MouseEnter -= m_innerFwTextBox_MouseEnter;
					m_innerFwTextBox.MouseLeave -= m_innerFwTextBox_MouseLeave;
					m_innerFwTextBox.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerFwTextBox = null;
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override
		#endregion Construction and destruction

		#region Methods for applying styles and writing systems
		/// <summary>
		/// Applies the specified style to the current selection of the Tss string
		/// </summary>
		public void ApplyStyle(string sStyle)
		{
			m_innerFwTextBox.EditingHelper.ApplyStyle(sStyle);
			if (m_innerFwTextBox.Tss.Length == 0)
			{
				// Need to also set the properties on the string. This allows searching for
				// styles only.
				var bldr = m_innerFwTextBox.Tss.GetBldr();
				bldr.SetStrPropValue(0, 0, (int)FwTextPropType.ktptNamedStyle, sStyle);
				m_innerFwTextBox.Tss = bldr.GetString();
			}
			m_innerFwTextBox.RefreshDisplay();
		}

		/// <summary>
		/// Applies the specified writing system to the current selection
		/// </summary>
		public void ApplyWS(int hvoWs)
		{
			m_innerFwTextBox.ApplyWS(hvoWs);
			if (m_innerFwTextBox.Tss.Length == 0)
			{
				// Need to also set the properties on the string. This allows searching for
				// writing system only.
				var bldr = m_innerFwTextBox.Tss.GetBldr();
				bldr.SetIntPropValues(0, 0, (int)FwTextPropType.ktptWs, 0, hvoWs);
				m_innerFwTextBox.Tss = bldr.GetString();
			}
		}

		/// <summary>
		/// Adjust, figuring the stylesheet based on the main window of the mediator.
		/// </summary>
		public void AdjustForStyleSheet(Form parent, Control grower, IPropertyTable propertyTable)
		{
			AdjustForStyleSheet(parent, grower, FwUtils.StyleSheetFromPropertyTable(propertyTable));
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
			if (delta != 0)
			{
				Height = newHeight;
				// Need to get the inner box's height adjusted BEFORE we fix the string.
				PerformLayout();
				Tss = FontHeightAdjuster.GetUnadjustedTsString(Tss);
				if (grower != null)
				{
					grower.Height += delta;
					foreach (Control c in grower.Controls)
					{
						if (c == this)
						{
							continue;
						}
						var anchorTop = ((int)c.Anchor & (int)AnchorStyles.Top) != 0;
						var anchorBottom = ((int)c.Anchor & (int)AnchorStyles.Bottom) != 0;
						if (c.Top > Top && anchorTop)
						{
							// Anchored at the top and below our control: move it down
							c.Top += delta;
						}
						if (anchorTop && anchorBottom)
						{
							// Anchored top and bottom, it stretched with the panel,
							// but we don't want that.
							c.Height -= delta;
						}
					}
				}
				FontHeightAdjuster.GrowDialogAndAdjustControls(parent, delta, grower ?? this);
			}
		}

		#endregion Methods for applying styles and writing systems

		#region Selection methods that are for a text box.
		/// <summary>
		/// Selects a range of text in the text box.
		/// </summary>
		/// <param name="start">The position of the first character in the current text selection
		/// within the text box.</param>
		/// <param name="length">The number of characters to select.</param>
		/// <remarks>
		/// If you want to set the start position to the first character in the control's text,
		/// set the <i>start</i> parameter to 0.
		/// You can use this method to select a substring of text, such as when searching through
		/// the text of the control and replacing information.
		/// <b>Note:</b> You can programmatically move the caret within the text box by setting
		/// the <i>start</i> parameter to the position within
		/// the text box where you want the caret to move to and set the <i>length</i> parameter
		/// to a value of zero (0).
		/// The text box must have focus in order for the caret to be moved.
		/// </remarks>
		public void Select(int start, int length)
		{
			m_innerFwTextBox.Select(start, length);
		}

		/// <summary>
		/// Selects all text in the text box.
		/// </summary>
		public void SelectAll()
		{
			m_innerFwTextBox.SelectAll();
		}

		#endregion Selection methods that are for a text box.

		#region Properties
		/// <summary>
		/// Gets the root box.
		/// </summary>
		protected IVwRootBox RootBox => m_innerFwTextBox.RootBox;

		/// <summary>
		/// Based on the current selection, returns the (character) style name or null if there
		/// are multiple styles or an empty string if there is no character style
		/// </summary>
		public string SelectedStyle => m_innerFwTextBox.EditingHelper.GetCharStyleNameFromSelection();

		/// <summary>
		/// Gets or sets a value indicating whether the container enables the user to scroll to any controls placed
		/// outside of its visible boundaries.
		/// </summary>
		public override bool AutoScroll
		{
			get
			{
				return m_innerFwTextBox.AutoScroll;
			}

			set
			{
				m_innerFwTextBox.AutoScroll = value;
			}
		}

		/// <summary>
		/// Indicates whether a text box control automatically wraps words to the beginning of the next line when necessary.
		/// </summary>
		public bool WordWrap
		{
			get
			{
				return m_innerFwTextBox.WordWrap;
			}

			set
			{
				m_innerFwTextBox.WordWrap = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether pressing ENTER in a text box control creates a new line of text
		/// in the control or activates the default button for the form.
		/// </summary>
		public bool AcceptsReturn
		{
			get
			{
				return m_innerFwTextBox.AcceptsReturn;
			}

			set
			{
				m_innerFwTextBox.AcceptsReturn = value;
			}
		}

		/// <summary>
		/// Get the selection from the text box
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwSelection Selection => m_innerFwTextBox.RootBox.Selection;

		/// <summary>
		/// Gets or sets the starting point of text selected in the text box.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get
			{
				return m_innerFwTextBox.SelectionStart;
			}
			set
			{
				m_innerFwTextBox.SelectionStart = value;
			}
		}

		/// <summary>
		/// Gets or sets the number of characters selected in the text box.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionLength
		{
			get
			{
				return m_innerFwTextBox.SelectionLength;
			}
			set
			{
				m_innerFwTextBox.SelectionLength = Math.Min(value, m_innerFwTextBox.Text.Length);
			}
		}

		/// <summary>
		/// Gets or sets the selected text.
		/// </summary>
		/// <value>The selected text.</value>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string SelectedText
		{
			get
			{
				return m_innerFwTextBox.SelectedText;
			}

			set
			{
				m_innerFwTextBox.SelectedText = value;
			}
		}

		/// <summary>
		/// Gets or sets the selected TSS.
		/// </summary>
		/// <value>The selected TSS.</value>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString SelectedTss
		{
			get
			{
				return m_innerFwTextBox.SelectedTss;
			}

			set
			{
				m_innerFwTextBox.SelectedTss = value;
			}
		}

		/// <summary>
		/// Set an ID string that can be used for debugging purposes to identify the control.
		/// </summary>
		public string controlID
		{
			get
			{
				return m_innerFwTextBox.m_controlID;
			}
			set
			{
				m_innerFwTextBox.m_controlID = value;
			}
		}

		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				m_innerFwTextBox.BackColor = value;
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
				m_innerFwTextBox.ForeColor = value;
				base.ForeColor = value;
			}
		}

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				return m_innerFwTextBox == null ? string.Empty : m_innerFwTextBox.Text;
			}
			set
			{
				// set the text, if it changed (or it hasn't already been set).
				// Note: in order to get an initial cursor in an empty textbox,
				// we must compare m_innerFwTextBox.Tss.Text (which will be null) to the value
				// (which should be an empty string). m_innerFwTextBox.Text will return an
				// empty string if it hasn't already been set, and comparing the value to that
				// would skip this block, and hence our code in Tss that makes a selection in the string.
				if (!DesignMode && m_innerFwTextBox.Tss.Text != value)
				{
					m_innerFwTextBox.Text = value;
					OnTextChanged(new EventArgs());
				}
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
				return m_innerFwTextBox?.Tss;
			}
			set
			{
				m_innerFwTextBox.Tss = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the text box (embedded control) has input focus.
		/// </summary>
		public override bool Focused => m_innerFwTextBox.Focused;

		/// <summary />
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				return m_innerFwTextBox.WritingSystemCode;
			}
			set
			{
				Debug.Assert(value != 1, "This is most likely an inappropriate value!");
				m_innerFwTextBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// The stylesheet used for the data being displayed.
		/// </summary>
		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwStylesheet StyleSheet
		{
			get
			{
				return m_innerFwTextBox.StyleSheet;
			}
			set
			{
				m_innerFwTextBox.StyleSheet = value;
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_innerFwTextBox.WritingSystemFactory;
			}
			set
			{
				m_innerFwTextBox.WritingSystemFactory = value;
			}
		}
		#endregion Properties

		#region Helper methods to expose base-class and/or inner class methods

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
		/// Remove any selection from the text box.
		/// </summary>
		public void RemoveSelection()
		{
			m_innerFwTextBox.RootBox.Activate(VwSelectionState.vssDisabled);
		}

		#endregion Helper methods to expose base-class and/or inner class methods

		#region IVwNotifyChange implementation

		/// <summary />
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_innerFwTextBox.NotificationsDisabled)
			{
				return; // inner box is adjusting size, not a real change to the text.
			}

			// The only property that can change is the string, so report TextChanged.
			OnTextChanged(new EventArgs());
		}

		#endregion IVwNotifyChange implementation

		#region ISupportInitialize implementation
		/// <summary>
		/// Required to implement ISupportInitialize.
		/// </summary>
		public virtual void BeginInit()
		{
			// This is a no-op, baby!
		}

		/// <summary>
		/// When all properties have been initialized, this will be called.
		/// </summary>
		public virtual void EndInit()
		{
		}
		#endregion ISupportInitialize implementation

		/// <summary>
		/// When the inner fw text box loses focus, fire InnerTextBoxLostFocus.
		/// </summary>
		private void OnInnerTextBoxLostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		/// <summary>
		/// Handles the key down.
		/// </summary>
		internal void HandleKeyDown(KeyEventArgs e)
		{
			OnKeyDown(e);
		}

		/// <summary>
		/// Scales a control's location, size, padding and margin.
		/// </summary>
		/// <param name="factor">The factor by which the height and width of the control will be
		/// scaled.</param>
		/// <param name="specified">A <see cref="T:System.Windows.Forms.BoundsSpecified"/> value
		/// that specifies the bounds of the control to use when defining its size and position.
		/// </param>
		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			base.ScaleControl(factor, specified);

			if ((specified & BoundsSpecified.Height) != 0)
			{
				m_innerFwTextBox.Zoom *= factor.Height;
			}
		}
	}
}