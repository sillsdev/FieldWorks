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
// File: FwTextBox.cs
// Responsibility:
//
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Collections.Generic;

using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Widgets
{
	/// -----------------------------------------------------------------------------------------
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
	/// -----------------------------------------------------------------------------------------
	public class FwTextBox : UserControl, IFWDisposable, IVwNotifyChange, ISupportInitialize
	{
		#region Data Members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBox()
		{
			m_innerFwTextBox = new InnerFwTextBox();

			if (Application.RenderWithVisualStyles)
				DoubleBuffered = true;

			HasBorder = true;
			Padding = Application.RenderWithVisualStyles ? new Padding(2) : new Padding(1, 2, 1, 1);
			m_innerFwTextBox.Dock = DockStyle.Fill;
			Controls.Add(m_innerFwTextBox);
			// This causes us to get a notification when the string gets changed,
			// so we can fire our TextChanged event.
			m_sda = m_innerFwTextBox.DataAccess;
			m_sda.AddNotification(this);

			m_innerFwTextBox.LostFocus += OnInnerTextBoxLostFocus;
			m_innerFwTextBox.GotFocus += m_innerFwTextBox_GotFocus;
			m_innerFwTextBox.MouseEnter += m_innerFwTextBox_MouseEnter;
			m_innerFwTextBox.MouseLeave += m_innerFwTextBox_MouseLeave;

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

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles || !m_hasBorder)
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
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
		/// <param name="ss"></param>
		public void AdjustForStyleSheet(IVwStylesheet ss)
		{
			int oldHeight = Height;
			StyleSheet = ss;
			int newHeight = Math.Max(oldHeight, PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta == 0)
				return;
			Height = newHeight;
			// Need to get the inner box's height adjusted BEFORE we fix the string.
			PerformLayout();
			Tss = FontHeightAdjuster.GetUnadjustedTsString(Tss);
			foreach (Control c in this.Parent.Controls)
			{
				if (c == this)
					continue;
				bool anchorTop = ((((int)c.Anchor) & ((int)AnchorStyles.Top)) != 0);
				if (c.Top > this.Top && anchorTop)
				{
					// Anchored at the top and below our control: move it down
					c.Top += delta;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the text box has a border.
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
		/// Gets or sets the border style of the tree view control. If the application is using visual styles, this has no effect.
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
		/// True if this text box allows the Enter key to insert a newline.
		/// </summary>
		public bool SuppressEnter
		{
			get { return m_innerFwTextBox.SuppressEnter; }
			set { m_innerFwTextBox.SuppressEnter = value; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font of the text displayed by the control.
		/// </summary>
		/// <value></value>
		/// <returns>The <see cref="T:System.Drawing.Font"></see> to apply to the text displayed by the control. The default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultFont"></see> property.</returns>
		/// <PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override Font Font
		{
			get
			{
				CheckDisposed();

				return base.Font;
			}
			set
			{
				CheckDisposed();

				base.Font = value;
			}
		}

		/// <summary>
		/// calculate the height of the edit boxes in millipoints.
		/// </summary>
		/// <param name="control"></param>
		/// <returns></returns>
		internal static int GetDympMaxHeight(Control control)
		{
			using (Graphics gr = control.CreateGraphics())
			{
				// use height of client area, which does not include borders, scrollbars, etc.
				int mpEditHeight = (control.ClientSize.Height - control.Padding.Vertical) * 72000 / (int)gr.DpiY;

				// Make sure we don't go bigger than the size that was specified in the designer.
				mpEditHeight = Math.Min(mpEditHeight, FontHeightAdjuster.GetFontHeight(control.Font));
				return mpEditHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the GotFocus event of the m_innerFwTextBox control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_innerFwTextBox_GotFocus(object sender, EventArgs e)
		{
			OnGotFocus(e);
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height the box would like to be to neatly display its current data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				CheckDisposed();
				int borderHeight = 0;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TextHeight
		{
			get
			{
				CheckDisposed();
				if (DesignMode)
					return -1;
				return m_innerFwTextBox.TextHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the preferred width given the current stylesheet and string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredWidth
		{
			get
			{
				CheckDisposed();
				int borderWidth = 0;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// true to adjust font height to fix box. When set false, client will normally
		/// call PreferredHeight and adjust control size to suit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AdjustStringHeight
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.AdjustStringHeight;
			}
			set
			{
				CheckDisposed();
				m_innerFwTextBox.AdjustStringHeight = value;
			}
		}

		void m_innerFwTextBox_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		void m_innerFwTextBox_MouseLeave(object sender, EventArgs e)
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
				return null;

			VisualStyleElement element = null;
			if (focused)
			{
				element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_FOCUSED);
				if (!VisualStyleRenderer.IsElementDefined(element))
					element = VisualStyleElement.TextBox.TextEdit.Focused;
			}
			else
			{
				switch (state)
				{
					case TextBoxState.Hot:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_HOT);
						if (!VisualStyleRenderer.IsElementDefined(element))
							element = VisualStyleElement.TextBox.TextEdit.Hot;
						break;

					case TextBoxState.Normal:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_NORMAL);
						if (!VisualStyleRenderer.IsElementDefined(element))
							element = VisualStyleElement.TextBox.TextEdit.Normal;
						break;

					case TextBoxState.Disabled:
						element = VisualStyleElement.CreateElement(EDIT_CLASS, EP_EDITBORDER_NOSCROLL, EPSN_DISABLED);
						if (!VisualStyleRenderer.IsElementDefined(element))
							element = VisualStyleElement.TextBox.TextEdit.Disabled;
						break;
				}
			}

			return new VisualStyleRenderer(element);
		}

		TextBoxState State
		{
			get
			{
				if (Enabled)
					return m_isHot ? TextBoxState.Hot : TextBoxState.Normal;
				else
					return TextBoxState.Disabled;
			}
		}

		void SetPadding()
		{
			Rectangle rect = ContentRectangle;
			base.Padding = new Padding((rect.Left - ClientRectangle.Left) + m_textPadding.Left,
				(rect.Top - ClientRectangle.Top) + m_textPadding.Top, (ClientRectangle.Right - rect.Right) + m_textPadding.Right,
				(ClientRectangle.Bottom - rect.Bottom) + m_textPadding.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			VisualStyleRenderer renderer = CreateRenderer(State, Focused, m_hasBorder);
			if (renderer != null)
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		#region IDisposable override

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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified style to the current selection of the Tss string
		/// </summary>
		/// <param name="sStyle">The name of the style to apply</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyStyle(string sStyle)
		{
			CheckDisposed();

			m_innerFwTextBox.EditingHelper.ApplyStyle(sStyle);
			if (m_innerFwTextBox.Tss.Length == 0)
			{
				// Need to also set the properties on the string. This allows searching for
				// styles only.
				ITsStrBldr bldr = m_innerFwTextBox.Tss.GetBldr();
				bldr.SetStrPropValue(0, 0, (int)FwTextPropType.ktptNamedStyle, sStyle);
				m_innerFwTextBox.Tss = bldr.GetString();
			}
			m_innerFwTextBox.RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified writing system to the current selection
		/// </summary>
		/// <param name="hvoWs">The ID of the writing system to apply</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyWS(int hvoWs)
		{
			CheckDisposed();

			m_innerFwTextBox.ApplyWS(hvoWs);
			if (m_innerFwTextBox.Tss.Length == 0)
			{
				// Need to also set the properties on the string. This allows searching for
				// writing system only.
				ITsStrBldr bldr = m_innerFwTextBox.Tss.GetBldr();
				bldr.SetIntPropValues(0, 0, (int)FwTextPropType.ktptWs, 0, hvoWs);
				m_innerFwTextBox.Tss = bldr.GetString();
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
							continue;
						bool anchorTop = ((((int)c.Anchor) & ((int)AnchorStyles.Top)) != 0);
						bool anchorBottom = ((((int)c.Anchor) & ((int)AnchorStyles.Bottom)) != 0);
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
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public void Select(int start, int length)
		{
			CheckDisposed();

			m_innerFwTextBox.Select(start, length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects all text in the text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SelectAll()
		{
			CheckDisposed();

			m_innerFwTextBox.SelectAll();
		}

		#endregion Selection methods that are for a text box.

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the root box.
		/// </summary>
		/// <value>The root box.</value>
		/// ------------------------------------------------------------------------------------
		protected IVwRootBox RootBox
		{
			get { return m_innerFwTextBox.RootBox; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Based on the current selection, returns the (character) style name or null if there
		/// are multiple styles or an empty string if there is no character style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SelectedStyle
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.EditingHelper.GetCharStyleNameFromSelection();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the container enables the user to scroll to any controls placed
		/// outside of its visible boundaries.
		/// </summary>
		/// <value><c>true</c> if the container enables auto-scrolling; otherwise, <c>false</c>. The default value is <c>false</c>.</value>
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
		/// <value><c>true</c> if the text box control wraps words; <c>false</c> if the text box control automatically scrolls horizontally
		/// when the user types past the right edge of the control. The default is <c>false</c>.</value>
		public bool WordWrap
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.WordWrap;
			}

			set
			{
				CheckDisposed();
				m_innerFwTextBox.WordWrap = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether pressing ENTER in a text box control creates a new line of text
		/// in the control or activates the default button for the form.
		/// </summary>
		/// <value><c>true</c> if the ENTER key creates a new line of text in a multiline version of the control;
		/// <c>false</c> if the ENTER key activates the default button for the form. The default is <c>false</c>.</value>
		public bool AcceptsReturn
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.AcceptsReturn;
			}

			set
			{
				CheckDisposed();
				m_innerFwTextBox.AcceptsReturn = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the selection from the text box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public IVwSelection Selection
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.RootBox.Selection;
			}
		}

		/// <summary>
		/// Gets or sets the starting point of text selected in the text box.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.SelectionStart;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.SelectionStart = value;
			}
		}

		/// <summary>
		/// Gets or sets the number of characters selected in the text box.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public int SelectionLength
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.SelectionLength;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.SelectionLength = Math.Min(value, m_innerFwTextBox.Text.Length);
			}
		}

		/// <summary>
		/// Gets or sets the selected text.
		/// </summary>
		/// <value>The selected text.</value>
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public string SelectedText
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.SelectedText;
			}

			set
			{
				CheckDisposed();
				m_innerFwTextBox.SelectedText = value;
			}
		}

		/// <summary>
		/// Gets or sets the selected TSS.
		/// </summary>
		/// <value>The selected TSS.</value>
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public ITsString SelectedTss
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.SelectedTss;
			}

			set
			{
				CheckDisposed();
				m_innerFwTextBox.SelectedTss = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an ID string that can be used for debugging purposes to identify the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string controlID
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.m_controlID;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.m_controlID = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

				m_innerFwTextBox.BackColor = value;
				base.BackColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy this to the embedded window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

				m_innerFwTextBox.ForeColor = value;
				base.ForeColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ BrowsableAttribute(true)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				CheckDisposed();

				if (m_innerFwTextBox == null)
					return String.Empty; // happens somewhere during OnHandleDestroyed sometimes.

				return m_innerFwTextBox.Text;
			}
			set
			{
				CheckDisposed();

				// set the text, if it changed (or it hasn't already been set).
				// Note: in order to get an initial cursor in an empty textbox,
				// we must compare m_innerFwTextBox.Tss.Text (which will be null) to the value
				// (which should be an empty string). m_innerFwTextBox.Text will return an
				// empty string if it hasn't already been set, and comparing the value to that
				// would skip this block, and hence our code in Tss that makes a selection in the string.
				if (m_innerFwTextBox.Tss.Text != value)
				{
					m_innerFwTextBox.Text = value;
					OnTextChanged(new EventArgs());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ITsString Tss
		{
			get
			{
				CheckDisposed();

				if (m_innerFwTextBox == null)
					return null;

				return m_innerFwTextBox.Tss;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.Tss = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the text box (embedded control) has input focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Focused
		{
			get
			{
				CheckDisposed();
				return m_innerFwTextBox.Focused;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.WritingSystemCode;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(value != 1, "This is most likely an inappropriate value!");
				m_innerFwTextBox.WritingSystemCode = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The stylesheet used for the data being displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.StyleSheet;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.StyleSheet = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

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
				SelectNextControl(null, forward, true, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any selection from the text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveSelection()
		{
			CheckDisposed();

			m_innerFwTextBox.RootBox.Activate(VwSelectionState.vssDisabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the current selection into view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ScrollSelectionIntoView()
		{
			CheckDisposed();
			if (m_innerFwTextBox.RootBox != null)
			{
				m_innerFwTextBox.ScrollSelectionIntoView(m_innerFwTextBox.RootBox.Selection,
					VwScrollSelOpts.kssoDefault);
			}
		}

		#endregion Helper methods to expose base-class and/or inner class methods

		#region Other public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a bitmap and renders the text in this FwTextBox in the bitmap.
		/// </summary>
		/// <param name="margins">Rectangle with the margins.</param>
		/// <param name="content">The rectangle that specifies the width and height of the content.
		/// </param>
		/// <returns>A bitmap representation of the text in this FwTextBox.</returns>
		/// ------------------------------------------------------------------------------------
		public Bitmap CreateBitmapOfText(Padding margins, Rectangle content)
		{
			if (m_innerFwTextBox.RootBox == null)
				return null;

			Bitmap bitmap = new Bitmap(margins.Left + content.Width + margins.Right,
				margins.Top + content.Height + margins.Bottom);
			VwSelectionState selState = m_innerFwTextBox.RootBox.SelectionState;
			m_innerFwTextBox.RootBox.Activate(VwSelectionState.vssDisabled);

			content.Offset(margins.Left, margins.Top);

			DrawToBitmap(bitmap, content);

			m_innerFwTextBox.RootBox.Activate(selState);

			return bitmap;
		}
		#endregion

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
			if (m_innerFwTextBox.NotificationsDisabled)
				return; // inner box is adjusting size, not a real change to the text.

			// The only property that can change is the string, so report TextChanged.
			OnTextChanged(new EventArgs());
		}

		#endregion IVwNotifyChange implementation

		#region ISupportInitialize implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required to implement ISupportInitialize.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void BeginInit()
		{
			CheckDisposed();

			// This is a no-op, baby!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When all properties have been initialized, this will be called. This is where we
		/// can register to handle the InputLanguageChanged event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void EndInit()
		{
			CheckDisposed();

			m_innerFwTextBox.RegisterForInputLanguageChanges();
		}
		#endregion ISupportInitialize implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the inner fw text box loses focus, fire InnerTextBoxLostFocus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnInnerTextBoxLostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the key down.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.KeyEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		internal void HandleKeyDown(KeyEventArgs e)
		{
			OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scales a control's location, size, padding and margin.
		/// </summary>
		/// <param name="factor">The factor by which the height and width of the control will be
		/// scaled.</param>
		/// <param name="specified">A <see cref="T:System.Windows.Forms.BoundsSpecified"/> value
		/// that specifies the bounds of the control to use when defining its size and position.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			base.ScaleControl(factor, specified);

			if ((specified & BoundsSpecified.Height) != 0)
				m_innerFwTextBox.Zoom *= factor.Height;
		}
	}

	#region TextBoxEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TextBoxEditingHelper : EditingHelper
	{
		private InnerFwTextBox m_innerFwTextBox;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextBoxEditingHelper"/> class.
		/// </summary>
		/// <param name="innerFwTextBox">The inner fw text box.</param>
		/// ------------------------------------------------------------------------------------
		public TextBoxEditingHelper(InnerFwTextBox innerFwTextBox) :
			base(innerFwTextBox)
		{
			m_innerFwTextBox = innerFwTextBox;
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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerFwTextBox = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified style to the current selection
		/// </summary>
		/// <param name="sStyle">The name of the style to apply</param>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyle)
		{
			CheckDisposed();

			base.ApplyStyle(sStyle);

			// Give text box a chance to adjust height of text to fit box.
			m_innerFwTextBox.AdjustHeight();
		}

		protected override void OnCharAux(string stuInput, VwShiftStatus ss, Keys modifiers)
		{
			if ((modifiers & Keys.Alt) != Keys.Alt && stuInput == "\r")
				stuInput = InnerFwTextBox.LineBreak;
			base.OnCharAux(stuInput, ss, modifiers);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if all writing systems in the pasted string are in this
		/// project. If so, we will keep the writing system formatting. Otherwise, we will
		/// use the destination writing system (at the selection). We don't want to add new
		/// writing systems from a paste into an FwTextBox.
		/// </summary>
		/// <param name="wsf">writing system factory containing the writing systems in the
		/// pasted ITsString</param>
		/// <param name="destWs">[out] The destination writing system (writing system used at
		/// the selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			// Determine writing system at selection (destination for paste).
			destWs = 0;
			if (CurrentSelection != null)
				destWs = CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
			if (destWs <= 0)
				destWs = m_innerFwTextBox.WritingSystemCode;

			return AllWritingSystemsDefined(wsf) ? PasteStatus.PreserveWs : PasteStatus.UseDestWs;
		}

	}
	#endregion

	/// <summary>
	/// This is a custom implementation of the ISilDataAccess interface for use by custom
	/// Views-enabled text boxes.
	/// </summary>
	internal class TextBoxDataAccess : SilDataAccessManagedBase
	{
		private readonly Dictionary<HvoFlidWSKey, ITsString> m_strings = new Dictionary<HvoFlidWSKey, ITsString>();
		private ILgWritingSystemFactory m_wsf;

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object. If the writing system factory is set, all strings will be removed.
		/// All rootboxes that use this data access should be reconstructed to remove any references
		/// to the old factory.
		///
		/// </summary>
		/// <value></value>
		/// <returns>A ILgWritingSystemFactory</returns>
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_wsf;
			}
			set
			{
				m_strings.Clear();
				m_wsf = value;
			}
		}

		public override void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
		}

		public override void BeginNonUndoableTask()
		{
		}

		public override void EndUndoTask()
		{
		}

		public override void EndNonUndoableTask()
		{
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			return get_MultiStringAlt(hvo, tag, 0);
		}

		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			SetMultiStringAlt(hvo, tag, 0, _tss);
		}

		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			Debug.Assert(ws > WritingSystemServices.kwsFirstAnal); // FWNX-260: kwsFirstAnal not handled in C++ yet (March/2010).

			var key = new HvoFlidWSKey(hvo, tag, ws);
			ITsString tss;
			if (!m_strings.TryGetValue(key, out tss))
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeString("", ws == 0 ? m_wsf.UserWs : ws);
				m_strings[key] = tss;
			}
			return tss;
		}

		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			var key = new HvoFlidWSKey(hvo, tag, ws);
			if (_tss == null)
				m_strings.Remove(key);
			else
				m_strings[key] = _tss;
			SendPropChanged(hvo, tag, ws, 0, 0);
		}

		public override bool get_IsValidObject(int hvo)
		{
			return true;
		}

		private TextBoxMetaDataCache m_mdc = new TextBoxMetaDataCache();

		public override IFwMetaDataCache MetaDataCache
		{
			get
			{
				return m_mdc;
			}
			set
			{
				base.MetaDataCache = value;
			}
		}
	}

	/// <summary>
	/// A very trivial MDC, just enough to allow styles to be applied to text in the text box.
	/// </summary>
	class TextBoxMetaDataCache : IFwMetaDataCache
	{
		public void InitXml(string bstrPathname, bool fClearPrevCache)
		{
			throw new NotImplementedException();
		}

		public void GetFieldIds(int cflid, ArrayPtr rgflid)
		{
			throw new NotImplementedException();
		}

		public string GetOwnClsName(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetDstClsName(int luFlid)
		{
			throw new NotImplementedException();
		}

		public int GetOwnClsId(int luFlid)
		{
			throw new NotImplementedException();
		}

		public int GetDstClsId(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetFieldName(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetFieldLabel(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetFieldHelp(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetFieldXml(int luFlid)
		{
			throw new NotImplementedException();
		}

		public int GetFieldWs(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// The only field that gets asked about in a text box is the main string, and we need a type that DOES
		/// allow styles to be applied.
		/// </summary>
		public int GetFieldType(int luFlid)
		{
			return (int)CellarPropertyType.String;
		}

		public bool get_IsValidClass(int luFlid, int luClid)
		{
			throw new NotImplementedException();
		}

		public void GetClassIds(int cclid, ArrayPtr rgclid)
		{
			throw new NotImplementedException();
		}

		public string GetClassName(int luClid)
		{
			throw new NotImplementedException();
		}

		public bool GetAbstract(int luClid)
		{
			throw new NotImplementedException();
		}

		public int GetBaseClsId(int luClid)
		{
			throw new NotImplementedException();
		}

		public string GetBaseClsName(int luClid)
		{
			throw new NotImplementedException();
		}

		public int GetFields(int luClid, bool fIncludeSuperclasses, int grfcpt, int cflidMax, ArrayPtr _rgflid)
		{
			throw new NotImplementedException();
		}

		public int GetClassId(string bstrClassName)
		{
			throw new NotImplementedException();
		}

		public int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			throw new NotImplementedException();
		}

		public int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			throw new NotImplementedException();
		}

		public void GetDirectSubclasses(int luClid, int cluMax, out int _cluOut, ArrayPtr _rgluSubclasses)
		{
			throw new NotImplementedException();
		}

		public void GetAllSubclasses(int luClid, int cluMax, out int _cluOut, ArrayPtr _rgluSubclasses)
		{
			throw new NotImplementedException();
		}

		public void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		public bool get_IsVirtual(int luFlid)
		{
			throw new NotImplementedException();
		}

		public string GetFieldNameOrNull(int luFlid)
		{
			throw new NotImplementedException();
		}

		public int FieldCount
		{
			get { throw new NotImplementedException(); }
		}

		public int ClassCount
		{
			get { throw new NotImplementedException(); }
		}
	}


	#region InnerFwTextBox class
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// InnerFwTextBox implements the main body of an FwTextBox. Has to be public so combo box
	/// can return its text box.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class InnerFwTextBox : SimpleRootSite, IVwNotifyChange, IWritingSystemAndStylesheet
	{
		#region Data members

		// This 'view' displays the string m_tssData by pretending it is property ktagText of
		// object khvoRoot.
		internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		const int kfragRoot = 8002; // likewise.
		const int khvoRoot = 7003; // likewise.
		internal const string LineBreak = "\u2028";

		// Neither of these caches are used by FdoCache.
		// They are only used here.
		internal ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
		TextBoxVc m_vc;

		internal int m_WritingSystem; // Writing system to use when Text is set.
		internal bool m_fUsingTempWsFactory;
		// This stores a value analogous to AutoScrollPosition,
		// but unlike that, it isn't disabled by AutoScroll false.
		Point m_ScrollPosition = new Point(0,0); // our own scroll position
		internal string m_controlID;
		// true to adjust font height to fix box. When set false, client will normally
		// call PreferredHeight and adjust control size to suit.
		internal bool m_fAdjustStringHeight = true;
		internal bool NotificationsDisabled { get; private set;}

		private bool m_wordWrap;

		// Maximum characters allowed.
		private int m_maxLength = Int32.MaxValue;
		// true while we are in Dispose(bool) method
		private bool m_fIsDisposing;
		internal int m_mpEditHeight;

		/// <summary>
		/// This variable serves the purpose of DesignMode (which unfortunately always returns
		/// false).  If it is false, then using ITsString objects (and related objects) is not
		/// possible, presumably because the COM DLL isn't registered (which it isn't explicitly
		/// for FW 7 since FW 7 uses registration free COM via manifest files), and that's
		/// presumably because we're being invoked from Visual Studio in DesignMode.
		/// </summary>
		bool m_fTssRegistered;

		#endregion Data members

		#region Constructor/destructor
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public InnerFwTextBox()
		{
			m_DataAccess = new TextBoxDataAccess();
			// Check for the availability of the FwKernel COM DLL.  Too bad we have to catch an
			// exception to make this check...
			try
			{
				ITsStrBldr bldr = TsStrBldrClass.Create();
				m_vc = new TextBoxVc(this);
				m_fTssRegistered = true;
			}
			catch
			{
				m_fTssRegistered = false;
			}
			// So many things blow up so badly if we don't have one of these that I finally decided to just
			// make one, even though it won't always, perhaps not often, be the one we want.
			CreateTempWritingSystemFactory();
			m_DataAccess.WritingSystemFactory = WritingSystemFactory;
			IsTextBox = true;	// range selection not shown when not in focus
		}

		internal bool Rtl
		{
			get { return m_vc.m_rtl; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Make a writing system factory that is based on the Languages folder (ICU-based).
		/// This is only used in Designer, tests, and momentarily (during construction) in
		/// production, until the client sets supplies a real one.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		private void CreateTempWritingSystemFactory()
		{
			m_wsf = new PalasoWritingSystemManager();
			m_fUsingTempWsFactory = true;
		}

		/// <summary>
		/// The maximum length of text allowed in this context.
		/// </summary>
		public int MaxLength
		{
			get { return m_maxLength; }
			set { m_maxLength = value; }
		}

		/// <summary>
		/// True if this text box prevents the Enter key from inserting a newline.
		/// (This is not normally needed in dialogs since Enter is handled before individual controls see it.)
		/// </summary>
		public bool SuppressEnter { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			//Visible = false;

			if (disposing)
			{
				m_fIsDisposing = true;

				// This disposes m_wsf
				ShutDownTempWsFactory();
			}
			// Must happen before call to base.

			base.Dispose(disposing);

			if (disposing)
			{
				m_DataAccess.RemoveNotification(this);
				if (m_editingHelper != null)
					m_editingHelper.Dispose();
				if (m_rootb != null)
					m_rootb.Close();
				m_fIsDisposing = false;
			}
			m_rootb = null;
			m_editingHelper = null;
			m_vc = null;
			m_controlID = null;
			m_DataAccess = null;
			m_wsf = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// true to adjust font height to fix box. When set false, client will normally
		/// call PreferredHeight and adjust control size to suit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool AdjustStringHeight
		{
			get
			{
				CheckDisposed();

				return m_fAdjustStringHeight;
			}
			set
			{
				CheckDisposed();

				m_fAdjustStringHeight = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the writing system factory and release it explicitly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShutDownTempWsFactory()
		{
			if (m_fUsingTempWsFactory)
			{
				var disposable = m_wsf as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				m_wsf = null;
				m_fUsingTempWsFactory = false;
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// The desired text alignment. The default depends on the Vc's basic text direction, but the method is
		/// here rather than on the VC to allow it to be overriden in ComboTextBox.
		/// </summary>
		internal virtual FwTextAlign Alignment
		{
			get { return Rtl ? FwTextAlign.ktalRight : FwTextAlign.ktalLeft; }
		}

		/// <summary>
		/// Indicates whether a text box control automatically wraps words to the beginning of the next line when necessary.
		/// </summary>
		/// <value><c>true</c> if the text box control wraps words; <c>false</c> if the text box control automatically scrolls horizontally
		/// when the user types past the right edge of the control. The default is <c>false</c>.</value>
		public bool WordWrap
		{
			get
			{
				CheckDisposed();
				return m_wordWrap;
			}

			set
			{
				CheckDisposed();
				m_wordWrap = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				if (base.WritingSystemFactory == null)
				{
					CreateTempWritingSystemFactory();
				}
				return base.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				if (base.WritingSystemFactory != value)
				{
					ShutDownTempWsFactory();
					// when the writing system factory changes, delete any string that was there
					// and reconstruct the root box.
					base.WritingSystemFactory = value;
					m_vc.setWsfAndWs(value, WritingSystemCode);
					// Enhance JohnT: Base class should probably do this.
					if (m_DataAccess != null)
					{
						m_DataAccess.WritingSystemFactory = value;
						if (m_rootb != null)
							m_rootb.Reconstruct();
					}

				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();

				if (m_WritingSystem == 0)
					m_WritingSystem = WritingSystemFactory.UserWs;
				return m_WritingSystem;
			}
			set
			{
				CheckDisposed();

				m_WritingSystem = value;
				m_vc.setWsfAndWs(WritingSystemFactory, value);
				// If the contents is currently empty, make sure inital typing will occur in this WS.
				// (Unless it is zero, which is not a valid WS...hope it gets changed again if so.)
				if (Tss.Length == 0 && value != 0)
					Tss = TsStringUtils.MakeTss("", value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The stylesheet used for the data being displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
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

				if (base.StyleSheet != value)
				{
					base.StyleSheet = value;
					if (m_rootb != null)
						m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font of the text displayed by the control.
		/// </summary>
		/// <value></value>
		/// <returns>The <see cref="T:System.Drawing.Font"></see> to apply to the text displayed by the control. The default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultFont"></see> property.</returns>
		/// <PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override Font Font
		{
			get
			{
				CheckDisposed();

				return base.Font;
			}
			set
			{
				CheckDisposed();

				base.Font = value;
			}
		}

		/// <summary>
		/// The real string we are displaying.
		/// </summary>
		/// <value>The TSS.</value>
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public ITsString Tss
		{
			get
			{
				CheckDisposed();

				if (m_DataAccess == null || !m_fTssRegistered)
					return null;

				if (m_wsf == null)
					m_DataAccess.WritingSystemFactory = WritingSystemFactory;
				ITsString tss = m_DataAccess.get_StringProp(khvoRoot, ktagText);
				// We need to return the TsString without font size because it won't set properly.
				tss = TsStringUtils.RemoveIntProp(tss, (int)FwTextPropType.ktptFontSize);
				// replace line breaks with new lines
				return ReplaceAll(tss, LineBreak, Environment.NewLine);
			}
			set
			{
				CheckDisposed();
				// replace new lines with line breaks
				ITsString tss = ReplaceAll(value, Environment.NewLine, LineBreak);

				// Reduce the font size of any run in the new string as necessary to keep the text
				// from being clipped by the height of the box.
				if (tss != null && DoAdjustHeight)
				{
					m_DataAccess.SetString(khvoRoot, ktagText,
						FontHeightAdjuster.GetAdjustedTsString(tss, m_mpEditHeight, StyleSheet,
						WritingSystemFactory));
				}
				else
				{
					m_DataAccess.SetString(khvoRoot, ktagText, tss);
				}

				if (m_rootb != null && tss != null)
				{
					IVwSelection sel = m_rootb.MakeSimpleSel(true, true, true, true);
					ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				}
			}
		}

		private static ITsString ReplaceAll(ITsString tss, string oldSubStr, string newSubStr)
		{
			if (tss == null || tss.Length == 0)
				return tss;

			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			List<ITsString> lines = TsStringUtils.Split(tss, new[] { oldSubStr }, StringSplitOptions.None);
			for (int i = 0; i < lines.Count; i++)
			{
				tisb.AppendTsString(lines[i]);
				if (i < lines.Count - 1 || tss.Text.EndsWith(oldSubStr))
					tisb.Append(newSubStr);
			}
			return tisb.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Because we turn AutoScroll off to suppress the scroll bars, we need our own
		/// private representation of the actual scroll position.
		/// The setter has to behave in the same bizarre way as AutoScrollPosition,
		/// that setting it to (x,y) results in the new value being (-x, -y).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();
				if (AutoScroll)
					return base.ScrollPosition;
				return m_ScrollPosition;
			}
			set
			{
				CheckDisposed();
				if (AutoScroll)
					base.ScrollPosition = value;

				Point newPos = new Point(-value.X, -value.Y);
				if (newPos != m_ScrollPosition)
				{
					// Achieve the scroll by just invalidating. For a small window this is fine.
					m_ScrollPosition = newPos;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// The text box in a combo should never autoscroll. Doing so produces LT-11073 among other problems.
		/// </summary>
		protected override bool ScrollToSelectionOnSizeChanged
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ BrowsableAttribute(true),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				CheckDisposed();

				if (m_fIsDisposing)
					return string.Empty;

				var tss = Tss;

				if (tss == null)
					return String.Empty;

				return tss.Text ?? string.Empty;
			}
			set
			{
				CheckDisposed();
				Tss = TsStringUtils.MakeTss(value, WritingSystemCode);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accessor for data access object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		new internal ISilDataAccess DataAccess
		{
			get
			{
				CheckDisposed();
				return m_DataAccess;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the starting point of text selected in the text box.
		/// JohnT, 8 Aug 2006: contrary to previous behavior, this is now the logically first
		/// character selected, which I think is consistent with TextBox, not necessarily the
		/// anchor.
		/// </summary>
		/// <value>The selection start.</value>
		/// <exception cref="ArgumentException">
		/// Thrown when setting the value to less than zero.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get
			{
				CheckDisposed();

				if (RootBox == null)
					return 0;
				IVwSelection sel = RootBox.Selection;
				if (sel == null)
					return 0;

				ITsString tss;
				int ichAnchor;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int ws;
				int ichEnd;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObj, out tag, out ws);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				return IntToExtIndex(Math.Min(ichAnchor, ichEnd));
			}
			set
			{
				CheckDisposed();

				if (value < 0)
					throw new ArgumentException("The value cannot be less than zero.");
				Select(value, SelectionLength);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of characters selected in the text box.
		/// (JohnT, 8 Aug 2006: contrary to the previous implementation, this is now always a
		/// positive number...or zero...never negative, irrespective of the relative positions
		/// of the anchor and endpoint.)
		/// </summary>
		/// <value>The length of the selection.</value>
		/// <exception cref="ArgumentException">
		/// Thrown when setting the value to less than zero.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
										(DesignerSerializationVisibility.Hidden)]
		public virtual int SelectionLength
		{
			get
			{
				CheckDisposed();

				if (RootBox == null)
					return 0;
				IVwSelection sel = RootBox.Selection;
				if (sel == null)
					return 0;

				ITsString tss;
				int ichEnd;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int ws;
				int ichAnchor;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObj, out tag, out ws);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				return Math.Abs(IntToExtIndex(ichAnchor) - IntToExtIndex(ichEnd));
			}
			set
			{
				CheckDisposed();

				if (value < 0)
					throw new ArgumentException("The value cannot be less than zero.");
				Select(SelectionStart, value);
			}
		}

		/// <summary>
		/// Gets or sets the selected text.
		/// </summary>
		/// <value>The selected text.</value>
		public string SelectedText
		{
			get
			{
				CheckDisposed();
				ITsString tss = SelectedTss;
				if (tss == null)
					return string.Empty;
				string result = tss.Text;
				if (result == null)
					return string.Empty;
				return result;
			}

			set
			{
				CheckDisposed();
				SelectedTss = TsStringUtils.MakeTss(value, WritingSystemCode);
			}
		}

		/// <summary>
		/// Gets or sets the selected TSS.
		/// </summary>
		/// <value>The selected TSS.</value>
		public ITsString SelectedTss
		{
			get
			{
				CheckDisposed();
				ITsString tss = Tss;
				if (tss == null)
					return null;
				return tss.Substring(SelectionStart, SelectionLength);
			}

			set
			{
				CheckDisposed();
				int selStart = SelectionStart;
				Tss = Tss.Replace(selStart, SelectionLength, value);
				Select(selStart + value.Length, 0);
			}
		}

		#endregion

		#region Overridden rootsite methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a special extended editing helper for this text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new TextBoxEditingHelper(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate infinite width if needed.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();

			if (m_wordWrap)
				return base.GetAvailWidth(prootb);

			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			return 1000000;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the root box and initialize it. We want this one to work even in design mode, and since
		/// we supply the cache and data ourselves, that's possible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_DataAccess;
			m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			base.MakeRoot();
			m_DataAccess.AddNotification(this);
			//Text = "This is a view"; // Todo: remove after preliminary testing.
			//m_rootb.MakeSimpleSel(true, true, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden property to indicate that this control will handle horizontal scrolling
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool DoAutoHScroll
		{
			get
			{
				CheckDisposed();
				return true;
			}
		}

		/// <summary>
		/// Returns the selection that should be made visible if null is passed to
		/// MakeSelectionVisible. FwTextBox overrides to pass a selection that is the whole
		/// range, if nothing is selected.
		/// </summary>
		protected override IVwSelection SelectionToMakeVisible
		{
			get
			{
				if (m_rootb == null)
					return null;
				if (m_rootb.Selection != null)
					return m_rootb.Selection;
				return m_rootb.MakeSimpleSel(true, false, true, false);
			}
		}

		internal void BaseMakeSelectionVisible(IVwSelection sel)
		{
			base.MakeSelectionVisible(sel, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// First try to make everything visible, if possible. This is especially helpful with
		/// RTL text.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// <param name="fWantOneLineSpace">if set to <c>true</c> [f want one line space].</param>
		/// <returns>Flag indicating whether the selection was made visible</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool MakeSelectionVisible(IVwSelection sel, bool fWantOneLineSpace)
		{
			// Select everything (but don't install it).
			IVwSelection wholeSel = m_rootb.MakeSimpleSel(true, false, true, false);
			if (wholeSel != null)
			{
				Rectangle rcPrimary;
				using (new HoldGraphics(this))
				{
					bool fEndBeforeAnchor;
					SelectionRectangle(wholeSel, out rcPrimary, out fEndBeforeAnchor);
				}
				if (rcPrimary.Width < ClientRectangle.Width - HorizMargin * 2)
					return base.MakeSelectionVisible(wholeSel, false);
			}
			else
			{
				if (sel == null || !sel.IsValid)
				{
					Logger.WriteError(new InvalidOperationException("MakeSelectionVisible called when no valid selection could be made."));
					Logger.WriteEvent("m_rootb.Height = " + m_rootb.Height);
					Logger.WriteEvent("m_rootb.Width = " + m_rootb.Width);
					int hvoRoot, frag;
					IVwViewConstructor vc;
					IVwStylesheet stylesheet;
					m_rootb.GetRootObject(out hvoRoot, out vc, out frag, out stylesheet);
					Logger.WriteEvent("Root object = " + hvoRoot);
					Logger.WriteEvent("Root fragment = " + frag);
				}
				Debug.Fail("Unable to make a simple selection in rootbox.");
			}
			// And, in case it really is longer than the box (or was null!), make sure we really can see the actual selection;
			// but ONLY if we had one, otherwise, it tries again to make everything visible
			if (m_rootb.Selection != null && m_rootb.Selection.IsValid)
				return base.MakeSelectionVisible(sel, fWantOneLineSpace);
			return false;
		}

		#endregion

		#region Other methods

		/// <summary>
		/// Converts the internal string index to the external string index. They might be different because a line break character
		/// is used internally to represent a new line and CRLF is used externally on Windows to represent a new line.
		/// </summary>
		/// <param name="intIndex">The internal string index.</param>
		/// <returns>The external string index.</returns>
		protected int IntToExtIndex(int intIndex)
		{
			if (Environment.NewLine.Length == LineBreak.Length)
				return intIndex;

			ITsString tss = m_DataAccess.get_StringProp(khvoRoot, ktagText);
			int hardBreakCount = SubstringCount(tss == null || tss.Length == 0 ? string.Empty : tss.Text.Substring(0, intIndex), LineBreak);
			return intIndex + (hardBreakCount * (Environment.NewLine.Length - LineBreak.Length));
		}

		/// <summary>
		/// Converts the external string index to the internal string index. They might be different because a line break character
		/// is used internally to represent a new line and CRLF is used externally on Windows to represent a new line.
		/// </summary>
		/// <param name="extIndex">The external string index.</param>
		/// <returns>The internal string index.</returns>
		protected int ExtToIntIndex(int extIndex)
		{
			if (Environment.NewLine.Length == LineBreak.Length)
				return extIndex;

			ITsString tss = Tss;
			if (tss == null || tss.Length == 0)
				return extIndex; // empty string, no newlines to adjust for
			// Count the newlines before extIndex.
			string content = tss.Text;
			if (extIndex < content.Length) // allow for the possibility, which should be unlikely, that index is past the end.
				content = content.Substring(extIndex);
			int newLineCount = SubstringCount(content, Environment.NewLine);

			return extIndex + (newLineCount * (LineBreak.Length - Environment.NewLine.Length));
		}

		private static int SubstringCount(string str, string substr)
		{
			int num = 0;
			int pos = -1;
			while ((pos = str.IndexOf(substr, pos + 1)) > 0)
				num++;
			return num;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the background color for the control.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Drawing.Color"/> that represents the background color
		/// of the control. The default is the value of the
		/// <see cref="P:System.Windows.Forms.Control.DefaultBackColor"/> property.</returns>
		/// ------------------------------------------------------------------------------------
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

				base.BackColor = value;
				if (m_rootb != null)
					m_rootb.Reconstruct();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the foreground color of the control.
		/// </summary>
		/// <value></value>
		/// <returns>The foreground <see cref="T:System.Drawing.Color"/> of the control. The
		/// default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultForeColor"/>
		/// property.</returns>
		/// ------------------------------------------------------------------------------------
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
				base.ForeColor = value;
				if (m_rootb != null)
					m_rootb.Reconstruct();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a rectangle that encloses the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle TextRect
		{
			get
			{
				var rect = new Rectangle();
				IVwSelection sel = RootBox.MakeSimpleSel(true, false, false, false);
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot, rcDstRoot;
					Rect rcSec, rcPrimary;
					bool fSplit, fEndBeforeAnchor;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
						out rcSec, out fSplit, out fEndBeforeAnchor);

					rect.X = rcPrimary.left;
					rect.Y = rcPrimary.top;

					sel = RootBox.MakeSimpleSel(false, false, false, false);
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
						out rcSec, out fSplit, out fEndBeforeAnchor);

					rect.Width = rcPrimary.right - rect.X;
					rect.Height = RootBox.Height;
				}
				return rect;
			}
		}

		/// <summary>
		/// Selects a range of text in the text box.
		/// </summary>
		/// <param name="start">The position of the first character in the current text selection within the text box.</param>
		/// <param name="length">The number of characters to select. </param>
		/// <remarks>
		/// If you want to set the start position to the first character in the control's text, set the <i>start</i> parameter to 0.
		/// You can use this method to select a substring of text, such as when searching through the text of the control and replacing information.
		/// <b>Note:</b> You can programmatically move the caret within the text box by setting the <i>start</i> parameter to the position within
		/// the text box where you want the caret to move to and set the <i>length</i> parameter to a value of zero (0).
		/// The text box must have focus in order for the caret to be moved.
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// The value assigned to either the <i>start</i> parameter or the <i>length</i> parameter is less than zero.
		/// </exception>
		public void Select(int start, int length)
		{
			CheckDisposed();

			if (start < 0)
				throw new ArgumentException("Starting position is less than zero.", "start");
			if (length < 0)
				throw new ArgumentException("Length is less than zero.", "length");

			if (m_rootb == null)
				return;

			int intStart = ExtToIntIndex(start);
			int intEnd = ExtToIntIndex(start + length);

			IVwSelection sel = m_rootb.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, ws;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out ws);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out ws);
				if (Math.Min(ichAnchor, ichEnd) == intStart && Math.Max(ichAnchor, ichEnd) == intEnd)
					return;
			}
			try
			{
				m_rootb.MakeTextSelection(0, 0, null, ktagText, 0, intStart, intEnd, -1, false, -1, null, true);
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The height the box would like to be to neatly display its current data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PreferredHeight
		{
			get
			{
				// TextHeight doesn't always return the exact height (font height is specified in points, which
				// when converted to pixels can often get rounded down), so we add one extra pixel
				// to be sure there is enough room to fit the text properly so that even if AdjustHeight is
				// set to true, it will not have to adjust the font size to fit.
				return TextHeight + 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal height of the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TextHeight
		{
			get
			{
				CheckDisposed();
				if (m_wsf == null)
					throw new Exception("A text box is being asked for its height, but its writing system factory has not been set.");

				try
				{
					IVwCacheDa cda = VwCacheDaClass.Create();
					ISilDataAccess sda = (ISilDataAccess) cda;
					sda.WritingSystemFactory = WritingSystemFactory;
					sda.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetUnadjustedTsString(Tss));
					IVwRootBox rootb = VwRootBoxClass.Create();
					rootb.SetSite(this);
					rootb.DataAccess = sda;
					rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
					using (new HoldGraphics(this))
					{
						rootb.Layout(m_graphicsManager.VwGraphics, GetAvailWidth(rootb));
					}
					int dy = rootb.Height;
					rootb.Close(); // MUST close root boxes (see LT-5345)!
					return dy;
				}
				catch (Exception e)
				{
					throw new Exception("Failed to compute the height of an FwTextBox, though it has a writing system factory", e);
				}

				// This isn't reliable, because before we get to call it, we have typically
				// adjusted the string!
				//if (m_rootb == null)
				//{
				//    MakeRoot();
				//}
				//if (m_dxdLayoutWidth < 0)
				//    PerformLayout();
				//return m_rootb.Height + 4; // for borders etc.
			}
		}

		/// <summary>
		/// Return the simple width (plus a small fudge factor) of the current string in screen units.
		/// </summary>
		public int PreferredWidth
		{
			get
			{
				CheckDisposed();
				bool fOldSaveSize = m_vc.SaveSize;
				try
				{
					m_vc.SaveSize = true;
					IVwCacheDa cda = VwCacheDaClass.Create();
					ISilDataAccess sda = (ISilDataAccess)cda;
					sda.WritingSystemFactory = WritingSystemFactory;
					sda.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetUnadjustedTsString(Tss));
					IVwRootBox rootb = VwRootBoxClass.Create();
					rootb.SetSite(this);
					rootb.DataAccess = sda;
					rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
					int dx = 0;
					using (new HoldGraphics(this))
					{
						rootb.Layout(m_graphicsManager.VwGraphics, GetAvailWidth(rootb));
						int dpx = m_graphicsManager.VwGraphics.XUnitsPerInch;
						dx = (m_vc.PreferredWidth * dpx) / 72000;
					}
					rootb.Close();
					return dx + 8;
				}
				finally
				{
					m_vc.SaveSize = fOldSaveSize;
				}
			}
		}

		/// <summary>
		/// show default cursor for read-only text boxes.
		/// </summary>
		/// <param name="mousePos"></param>
		protected override void OnMouseMoveSetCursor(Point mousePos)
		{
			if (ReadOnlyView)
				Cursor = Cursors.Default;
			else
				base.OnMouseMoveSetCursor(mousePos);
		}

		/// <summary>
		/// Gets a value indicating whether to attempt to adjust the height of the string
		/// in the textbox to fit the height of the textbox.
		/// </summary>
		protected bool DoAdjustHeight
		{
			get
			{
				CheckDisposed();
				return m_fAdjustStringHeight && WritingSystemFactory != null && !AutoScroll;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts text height after a style change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool AdjustHeight()
		{
			CheckDisposed();

			if (!DoAdjustHeight || !m_fTssRegistered)
				return false;

			// Reduce the font size of any run in the new string as necessary to keep the text
			// from being clipped by the height of the box.
			// ENHANCE: Consider having GetAdjustedTsString return a value to tell whether any
			// adjustments were made. If not, we don't need to call SetString.
			RemoveNonRootNotifications(); // want to suppress any side effects and just update the view.
			try
			{
				m_DataAccess.SetString(khvoRoot, ktagText,
					FontHeightAdjuster.GetAdjustedTsString(FontHeightAdjuster.GetUnadjustedTsString(Tss), m_mpEditHeight, StyleSheet,
					WritingSystemFactory));

			}
			finally
			{
				RestoreNonRootNotifications();
			}
			return true;
		}

		internal virtual void RestoreNonRootNotifications()
		{
			if (m_rootb != null)
				DataAccess.AddNotification(this);
			NotificationsDisabled = false;
		}

		internal virtual void RemoveNonRootNotifications()
		{
			DataAccess.RemoveNotification(this);
			NotificationsDisabled = true;
		}
		#endregion

		#region Event-handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the edit box gets resized, recalc the maximum line height (when setting a Tss
		/// string, applying styles, or setting the WS, we need to reduce the font size if
		/// necessary to keep the text from being clipped by the height of the box).
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (!m_fTssRegistered)
				return;

			m_mpEditHeight = FwTextBox.GetDympMaxHeight(this);
			if (AdjustHeight())
				m_rootb.Reconstruct();
			// Don't bother making selection visible until our writing system is set, or the
			// string has something in it.  See LT-9472.
			// Also don't try if we have no selection; this can produce undesirable scrolling when the
			// window is just too narrow. LT-11073
			if (m_rootb.Selection == null)
				return;
			ITsString tss = Tss;
			if (m_WritingSystem != 0 || (tss != null && tss.Text != null))
				MakeSelectionVisible(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// watch for keys to do the cut/copy/paste operations
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Parent is FwTextBox)
			{
				(Parent as FwTextBox).HandleKeyDown(e);
				if (e.Handled)
					return;
			}
			if (!EditingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
		}

		/// <summary>
		/// Override to suppress Enter key if it is not allowed.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (SuppressEnter && e.KeyChar == '\r')
			{
				return;
			}
			base.OnKeyPress(e);
		}
		#endregion

		#region Methods for applying styles and writing systems
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified writing system to the current selection
		/// </summary>
		/// <param name="hvoWs">The ID of the writing system to apply</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyWS(int hvoWs)
		{
			CheckDisposed();

			EditingHelper.ApplyWritingSystem(hvoWs);

			AdjustHeight();
		}
		#endregion

		#region IVwNotifyChange Members

		/// <summary>
		/// Any change to this private data access must be a change to our string, so check its length.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			ITsString tssValue = Tss;
			if (tssValue != null && tssValue.Length > m_maxLength)
			{
				MessageBox.Show(this, string.Format(Strings.ksStringTooLong, m_maxLength), Strings.ksWarning,
								MessageBoxButtons.OK, MessageBoxIcon.Warning);
				ITsStrBldr bldr = tssValue.GetBldr();
				bldr.ReplaceTsString(m_maxLength, bldr.Length, null);
				Tss = bldr.GetString();
			}
		}

		#endregion
	}
	#endregion

	#region TextBoxVc class
	internal class TextBoxVc : FwBaseVc
	{
		#region Data members
		internal bool m_rtl;
		private bool m_fSaveSize;
		private int m_dxWidth;
#pragma warning disable 414
		private int m_dyHeight;
#pragma warning restore 414
		private readonly InnerFwTextBox m_innerTextBox;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct one. Must be part of an InnerFwTextBox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal TextBoxVc(InnerFwTextBox innerTextBox)
		{
			m_innerTextBox = innerTextBox;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to save the size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool SaveSize
		{
			get { return m_fSaveSize; }
			set { m_fSaveSize = value; }
		}

		/// <summary>
		/// Return the simple width of the current string in millipoints.
		/// </summary>
		internal int PreferredWidth
		{
			get { return m_dxWidth; }
		}

		public override ITsTextProps UpdateRootBoxTextProps(ITsTextProps ttp)
		{
			ITsPropsBldr propsBldr = ttp.GetBldr();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
				(int)RGB(m_innerTextBox.BackColor));

			using (Graphics graphics = m_innerTextBox.CreateGraphics())
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint,
					m_innerTextBox.Padding.Top * 72000 / (int)graphics.DpiY);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint,
					m_innerTextBox.Padding.Bottom * 72000 / (int)graphics.DpiY);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint,
					(m_rtl ? m_innerTextBox.Padding.Right : m_innerTextBox.Padding.Left) * 72000 / (int)graphics.DpiX);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint,
					(m_rtl ? m_innerTextBox.Padding.Left : m_innerTextBox.Padding.Right) * 72000 / (int)graphics.DpiX);
			}

			return propsBldr.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main method just displays the text with the appropriate properties.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)RGB(m_innerTextBox.ForeColor));

			if (m_rtl)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
					(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, (int)m_innerTextBox.Alignment);

			vwenv.OpenParagraph();
			vwenv.AddStringProp(InnerFwTextBox.ktagText, this);
			if (m_fSaveSize)
			{
				ITsString tss = vwenv.DataAccess.get_StringProp(hvo, InnerFwTextBox.ktagText);
				vwenv.get_StringWidth(tss, null, out m_dxWidth, out m_dyHeight);
			}
			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the writing system factory and the writing system hvo.
		/// </summary>
		/// <param name="wsf">The WSF.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public void setWsfAndWs(ILgWritingSystemFactory wsf, int ws)
		{
			Debug.Assert(wsf != null, "Set the WritingSystemFactory first!");
			ILgWritingSystem wsObj = wsf.get_EngineOrNull(ws);
			m_rtl = (wsObj != null && wsObj.RightToLeftScript);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public uint RGB(Color c)
		{
			if (c == Color.Transparent)
				return 0xC0000000;
			return RGB(c.R, c.G, c.B);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r)|((short)((byte)(g))<<8))|(((short)(byte)(b))<<16)));

		}
	}
	#endregion
}
