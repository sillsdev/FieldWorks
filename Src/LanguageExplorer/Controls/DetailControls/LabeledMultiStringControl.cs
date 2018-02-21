// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Widgets;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// LabeledMultiStringControl (used in InsertEntryDlg)
	/// has an LcmCache, but it is used only to figure out the writing systems to use; the control
	/// works with a dummy cache, object, and flid, and the resulting text must be read back.
	/// </summary>
	public class LabeledMultiStringControl : UserControl, IVwNotifyChange
	{
		InnerLabeledMultiStringControl m_innerControl;
		bool m_isHot;
		bool m_hasBorder;
		Padding m_textPadding;

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// </summary>
		public LabeledMultiStringControl(LcmCache cache, int wsMagic, IVwStylesheet vss)
		{
			m_innerControl = new InnerLabeledMultiStringControl(cache, wsMagic);
			InternalInit(cache, vss);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// For use with a non-standard list of wss (like available UI languages).
		/// (See CustomListDlg)
		/// </summary>
		public LabeledMultiStringControl(LcmCache cache, List<CoreWritingSystemDefinition> nonStandardWritingSystemList, IVwStylesheet vss)
		{

			m_innerControl = new InnerLabeledMultiStringControl(cache, nonStandardWritingSystemList);
			InternalInit(cache, vss);
		}

		private void InternalInit(LcmCache cache, IVwStylesheet vss)
		{
			if (Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
			}

			if (vss != null)
			{
				m_innerControl.StyleSheet = vss;
			}
			m_innerControl.Dock = DockStyle.Fill;
			Controls.Add(m_innerControl);
			m_innerControl.MakeRoot();

			m_innerControl.RootBox.DataAccess.AddNotification(this);
			m_innerControl.MouseEnter += m_innerControl_MouseEnter;
			m_innerControl.MouseLeave += m_innerControl_MouseLeave;
			m_innerControl.GotFocus += m_innerControl_GotFocus;
			m_innerControl.LostFocus += m_innerControl_LostFocus;

			HasBorder = true;
			Height = PreferredHeight;
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
			{
				return;
			}

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_innerControl != null)
				{
					m_innerControl.RootBox?.DataAccess?.RemoveNotification(this);
					m_innerControl.MouseEnter -= m_innerControl_MouseEnter;
					m_innerControl.MouseLeave -= m_innerControl_MouseLeave;
					m_innerControl.GotFocus -= m_innerControl_GotFocus;
					m_innerControl.LostFocus -= m_innerControl_LostFocus;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerControl = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Gets the preferred height.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

				var height = m_innerControl.RootBox != null && m_innerControl.RootBox.Height > 0 ? Math.Min(m_innerControl.RootBox.Height + 8, 66) : 46;
				return height + base.Padding.Vertical + borderHeight;
			}
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
		/// Gets or sets the border style of the tree view control.
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
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		/// <returns>true if the control has focus; otherwise, false.
		/// </returns>
		public override bool Focused => m_innerControl.Focused;

		/// <summary>
		/// Gets the root box.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwRootBox RootBox => m_innerControl.RootBox;

		private TextBoxState State => Enabled ? (m_isHot ? TextBoxState.Hot : TextBoxState.Normal) : TextBoxState.Disabled;

		private void SetPadding()
		{
			var rect = ContentRectangle;
			base.Padding = new Padding((rect.Left - ClientRectangle.Left) + m_textPadding.Left,
				(rect.Top - ClientRectangle.Top) + m_textPadding.Top, (ClientRectangle.Right - rect.Right) + m_textPadding.Right,
				(ClientRectangle.Bottom - rect.Bottom) + m_textPadding.Bottom);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			(FwTextBox.CreateRenderer(State, ContainsFocus, true))?.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		private void m_innerControl_MouseLeave(object sender, EventArgs e)
		{
			m_isHot = false;
			Invalidate();
		}

		private void m_innerControl_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		private void m_innerControl_LostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		private void m_innerControl_GotFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

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
		/// Get one of the resulting strings.
		/// </summary>
		public ITsString Value(int ws)
		{
			return m_innerControl.Value(ws);
		}

		/// <summary>
		/// Set one of the strings.
		/// </summary>
		public void SetValue(int ws, ITsString tss)
		{
			m_innerControl.SetValue(ws, tss);
		}

		/// <summary>
		/// Set one of the strings.
		/// </summary>
		public void SetValue(int ws, string txt)
		{
			SetValue(ws, TsStringUtils.MakeString(txt, ws));
		}

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public int NumberOfWritingSystems => m_innerControl.WritingSystems.Count;

		/// <summary>
		/// Get the nth string and writing system.
		/// </summary>
		public ITsString ValueAndWs(int index, out int ws)
		{
			ws = m_innerControl.WritingSystems[index].Handle;
			return m_innerControl.RootBox.DataAccess.get_MultiStringAlt(InnerLabeledMultiStringControl.khvoRoot, InnerLabeledMultiStringControl.kflid, ws);
		}

		/// <summary>
		/// Get the nth writing system.
		/// </summary>
		/// <param name="index">The index.</param>
		public int Ws(int index)
		{
			return m_innerControl.WritingSystems[index].Handle;
		}

		/// <summary>
		/// Selects a range of text based on the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="start">The position of the first character in the current text selection within the text box.</param>
		/// <param name="length">The number of characters to select.</param>
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
		public void Select(int ws, int start, int length)
		{
			if (start < 0)
			{
				throw new ArgumentException("Starting position is less than zero.", nameof(start));
			}
			if (length < 0)
			{
				throw new ArgumentException("Length is less than zero.", nameof(length));
			}

			var sel = m_innerControl.RootBox.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, wsDummy;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out wsDummy);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out wsDummy);
				if (Math.Min(ichAnchor, ichEnd) == start && Math.Max(ichAnchor, ichEnd) == start + length)
				{
					return;
				}
			}
			try
			{
				m_innerControl.RootBox.MakeTextSelection(0, 0, null, InnerLabeledMultiStringControl.kflid, 0, start, start + length, ws, false, -1, null, true);
			}
			catch
			{
			}
		}

		#region IVwNotifyChange Members

		/// <summary />
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			OnTextChanged(new EventArgs());
		}

		#endregion
	}
}
