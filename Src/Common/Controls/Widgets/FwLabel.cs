// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwLabel.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simulation of a regular .NET label control which uses a view to display the text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwLabel : Control, IFWDisposable, IVwNotifyChange, ISupportInitialize
	{
		#region Data members
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged
		/// section of Dispose. (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;

		/// <summary>The rootsite that occupies 100% of the rectangle of this control</summary>
		private InnerFwTextBox m_innerFwTextBox;
		private ITsString m_tssOrig; // needed to support resize because the inner text box actually changes the string
		private System.Drawing.ContentAlignment m_contentAlignment;
		#endregion

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwLabel()
		{
			m_innerFwTextBox = new InnerFwTextBox();
			m_innerFwTextBox.ReadOnlyView = true;

			Padding = new Padding(1, 2, 1, 1);
			Controls.Add(m_innerFwTextBox);

			// This causes us to get a notification when the string gets changed,
			// so we can fire our TextChanged event.
			m_sda = m_innerFwTextBox.DataAccess;
			m_sda.AddNotification(this);
			m_innerFwTextBox.AdjustStringHeight = false;
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!Application.RenderWithVisualStyles)
				return;

			VisualStyleRenderer renderer;
			if (Enabled)
				renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
			else
				renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Disabled);
			renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.TextChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			AlignText();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Resize"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Tss = m_tssOrig;
			AlignText();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the parent is shown for the first time.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnContainingFormShown(object sender, EventArgs e)
		{
			AlignText();
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
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.",
					GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false
		/// to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				// m_innerFwTextBox is part of Controls collection and will be disposed there.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerFwTextBox = null;
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion

		#region IVwNotifyChange Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Informs the recipient that a property of an object has changed. In some cases, may
		/// provide useful information about how much of it changed. Note that some objects
		/// reporting changes may not have full information about the extent of the change, in
		/// which case, they should err on the side of exaggerating it, for example by
		/// pretending that all objects were deleted and a new group inserted.
		/// </summary>
		/// <param name="hvo">The object that changed</param>
		/// <param name="tag">The property that changed</param>
		/// <param name="ivMin">For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.</param>
		/// <param name="cvIns">For vectors, the number of items inserted. For atomic objects,
		/// 1 if an item was added. Otherwise (including basic properties), 0.</param>
		/// <param name="cvDel">For vectors, the number of items deleted. For atomic objects,
		/// 1 if an item was deleted. Otherwise (including basic properties), 0.</param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// The only property that can change is the string, so report TextChanged.
			OnTextChanged(new EventArgs());
		}

		#endregion

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
		}

		#endregion //Methods for applying styles and writing systems

		#region Selection methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates a child control. Optionally specifies the direction in the tab order to
		/// select the control from.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select;
		/// otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward
		/// in the tab order.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Select(bool directed, bool forward)
		{
			// a label (and FwLabel) isn't selectable
		}
		#endregion // Selection methods that are for a text box.

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
		[BrowsableAttribute(true)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				CheckDisposed();

				if (m_innerFwTextBox == null)
					return ""; // happens somewhere during OnHandleDestroyed sometimes.
				return m_innerFwTextBox.Text;
			}
			set
			{
				CheckDisposed();

				m_innerFwTextBox.Text = value;
				OnTextChanged(EventArgs.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public virtual ITsString Tss
		{
			get
			{
				CheckDisposed();

				return m_innerFwTextBox.Tss;
			}
			set
			{
				CheckDisposed();

				m_tssOrig = value;
				m_innerFwTextBox.Tss = value;
				OnTextChanged(EventArgs.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text alignment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		public virtual System.Drawing.ContentAlignment TextAlign
		{
			get { return m_contentAlignment; }
			set
			{
				if (m_contentAlignment != value)
				{
					m_contentAlignment = value;
					AlignText();
				}
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
		#endregion // Properties

		#region Other public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a bitmap and renders the text in this FwTextBox in the bitmap.
		/// </summary>
		/// <param name="rect">The rectangle that specifies the width and height of the bitmap.
		/// </param>
		/// <returns>A bitmap representation of the text in this FwTextBox.</returns>
		/// ------------------------------------------------------------------------------------
		public Bitmap CreateBitmapOfText(Rectangle rect)
		{
			Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
			VwSelectionState selState = m_innerFwTextBox.RootBox.SelectionState;
			m_innerFwTextBox.RootBox.Activate(VwSelectionState.vssDisabled);

			DrawToBitmap(bitmap, rect);

			m_innerFwTextBox.RootBox.Activate(selState);

			return bitmap;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Aligns the inner textbox according to the set TextAlign property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AlignText()
		{
			if (m_innerFwTextBox.RootBox == null)
				return;

			Rectangle textRect = m_innerFwTextBox.TextRect;
			int textHeight = textRect.Height;
			int textWidth = textRect.Width;

			if (textHeight > ClientRectangle.Height)
				textHeight = ClientRectangle.Height;
			if (textWidth > ClientRectangle.Width)
				textWidth = ClientRectangle.Width;

			int left = 0;
			int top = 0;
			if (m_contentAlignment == System.Drawing.ContentAlignment.TopCenter ||
				m_contentAlignment == System.Drawing.ContentAlignment.MiddleCenter ||
				m_contentAlignment == System.Drawing.ContentAlignment.BottomCenter)
			{
				left = (ClientRectangle.Width - textWidth) / 2;
			}
			else if (m_contentAlignment == System.Drawing.ContentAlignment.TopRight ||
				m_contentAlignment == System.Drawing.ContentAlignment.MiddleRight ||
				m_contentAlignment == System.Drawing.ContentAlignment.BottomRight)
			{
				left = ClientRectangle.Right - textWidth;
			}

			if (m_contentAlignment == System.Drawing.ContentAlignment.MiddleLeft ||
				m_contentAlignment == System.Drawing.ContentAlignment.MiddleCenter ||
				m_contentAlignment == System.Drawing.ContentAlignment.MiddleRight)
			{
				top = (ClientRectangle.Height - textHeight) / 2;
			}
			else if (m_contentAlignment == System.Drawing.ContentAlignment.BottomLeft ||
				m_contentAlignment == System.Drawing.ContentAlignment.BottomCenter ||
				m_contentAlignment == System.Drawing.ContentAlignment.BottomRight)
			{
				top = ClientRectangle.Bottom - textHeight;
			}

			m_innerFwTextBox.Location = new Point(left, top);
			m_innerFwTextBox.Width = textWidth;
			m_innerFwTextBox.Height = m_innerFwTextBox.RootBox.Height;

			IVwSelection sel = m_innerFwTextBox.RootBox.MakeSimpleSel(true, false, false, false);
			m_innerFwTextBox.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}
		#endregion

		#region ISupportInitialize Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Signals the object that initialization is starting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BeginInit()
		{
			CheckDisposed();
			// nothing to do
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Signals the object that initialization is complete.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndInit()
		{
			CheckDisposed();
			if (FindForm() != null)
				FindForm().Shown += new EventHandler(OnContainingFormShown);
		}

		#endregion
	}
}
