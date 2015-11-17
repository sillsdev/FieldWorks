// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;		// controls and etc...
using System.Windows.Forms.VisualStyles;
using System.Xml;
using Palaso.Media;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Text;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// LabeledMultiStringControl (used in InsertEntryDlg)
	/// has an FdoCache, but it is used only to figure out the writing systems to use; the control
	/// works with a dummy cache, object, and flid, and the resulting text must be read back.
	/// </summary>
	public class LabeledMultiStringControl : UserControl, IVwNotifyChange, IFWDisposable
	{
		InnerLabeledMultiStringControl m_innerControl;
		bool m_isHot = false;
		bool m_hasBorder;
		Padding m_textPadding;

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// </summary>
		public LabeledMultiStringControl(FdoCache cache, int wsMagic, IVwStylesheet vss)
		{
			m_innerControl = new InnerLabeledMultiStringControl(cache, wsMagic);
			InternalInit(cache, vss);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// For use with a non-standard list of wss (like available UI languages).
		/// (See CustomListDlg)
		/// </summary>
		/// <param name="cache">The FdoCache.</param>
		/// <param name="wsList">The non-standard list of IWritingSystems.</param>
		/// <param name="vss">The stylesheet.</param>
		public LabeledMultiStringControl(FdoCache cache, List<IWritingSystem> wsList, IVwStylesheet vss)
		{

			m_innerControl = new InnerLabeledMultiStringControl(cache, wsList);
			InternalInit(cache, vss);
		}

		private void InternalInit(FdoCache cache, IVwStylesheet vss)
		{
			if (Application.RenderWithVisualStyles)
				DoubleBuffered = true;

			if (vss != null)
				m_innerControl.StyleSheet = vss;
			m_innerControl.Dock = DockStyle.Fill;
			this.Controls.Add(m_innerControl);
			m_innerControl.MakeRoot();

			m_innerControl.RootBox.DataAccess.AddNotification(this);
			m_innerControl.MouseEnter += new EventHandler(m_innerControl_MouseEnter);
			m_innerControl.MouseLeave += new EventHandler(m_innerControl_MouseLeave);
			m_innerControl.GotFocus += new EventHandler(m_innerControl_GotFocus);
			m_innerControl.LostFocus += new EventHandler(m_innerControl_LostFocus);

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
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_innerControl != null)
				{
					if (m_innerControl.RootBox != null && m_innerControl.RootBox.DataAccess != null)
						m_innerControl.RootBox.DataAccess.RemoveNotification(this);

					m_innerControl.MouseEnter -= new EventHandler(m_innerControl_MouseEnter);
					m_innerControl.MouseLeave -= new EventHandler(m_innerControl_MouseLeave);
					m_innerControl.GotFocus -= new EventHandler(m_innerControl_GotFocus);
					m_innerControl.LostFocus -= new EventHandler(m_innerControl_LostFocus);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerControl = null;

			base.Dispose(disposing);
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

		#endregion IDisposable override

		/// <summary>
		/// Gets the preferred height.
		/// </summary>
		/// <value>The preferred height.</value>
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
				int height = 0;
				if (m_innerControl.RootBox != null && m_innerControl.RootBox.Height > 0)
					height = Math.Min(m_innerControl.RootBox.Height + 8, 66);
				else
					height = 46;	// barely enough to make a scroll bar workable
				return height + base.Padding.Vertical + borderHeight;
			}
		}

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles || !m_hasBorder)
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
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
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		/// <returns>true if the control has focus; otherwise, false.
		/// </returns>
		public override bool Focused
		{
			get
			{
				CheckDisposed();
				return m_innerControl.Focused;
			}
		}

		/// <summary>
		/// Gets the root box.
		/// </summary>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return m_innerControl.RootBox;
			}
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

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			VisualStyleRenderer renderer = FwTextBox.CreateRenderer(State, ContainsFocus, true);
			if (renderer != null)
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		void m_innerControl_MouseLeave(object sender, EventArgs e)
		{
			m_isHot = false;
			Invalidate();
		}

		void m_innerControl_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		void m_innerControl_LostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		void m_innerControl_GotFocus(object sender, EventArgs e)
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
				SelectNextControl(null, forward, true, true, false);
		}

		/// <summary>
		/// Get one of the resulting strings.
		/// </summary>
		public ITsString Value(int ws)
		{
			CheckDisposed();

			return m_innerControl.Value(ws);
		}

		/// <summary>
		/// Set one of the strings.
		/// </summary>
		public void SetValue(int ws, ITsString tss)
		{
			CheckDisposed();

			m_innerControl.SetValue(ws, tss);
		}

		/// <summary>
		/// Set one of the strings.
		/// </summary>
		public void SetValue(int ws, string txt)
		{
			CheckDisposed();

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			SetValue(ws, tsf.MakeString(txt, ws));
		}

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public int NumberOfWritingSystems
		{
			get
			{
				CheckDisposed();
				return m_innerControl.WritingSystems.Count;
			}
		}

		/// <summary>
		/// Get the nth string and writing system.
		/// </summary>
		public ITsString ValueAndWs(int index, out int ws)
		{
			CheckDisposed();

			ws = m_innerControl.WritingSystems[index].Handle;
			return m_innerControl.RootBox.DataAccess.get_MultiStringAlt(InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws);
		}

		/// <summary>
		/// Get the nth writing system.
		/// </summary>
		/// <param name="index">The index.</param>
		public int Ws(int index)
		{
			CheckDisposed();
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
			CheckDisposed();

			if (start < 0)
				throw new ArgumentException("Starting position is less than zero.", "start");
			if (length < 0)
				throw new ArgumentException("Length is less than zero.", "length");

			IVwSelection sel = m_innerControl.RootBox.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, wsDummy;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out wsDummy);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out wsDummy);
				if (Math.Min(ichAnchor, ichEnd) == start && Math.Max(ichAnchor, ichEnd) == start + length)
					return;
			}
			try
			{
				m_innerControl.RootBox.MakeTextSelection(0, 0, null, InnerLabeledMultiStringControl.kflid, 0, start, start + length,
					ws, false, -1, null, true);
			}
			catch
			{
			}
		}

		#region IVwNotifyChange Members

		/// <summary></summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			OnTextChanged(new EventArgs());
		}

		#endregion
	}
}
