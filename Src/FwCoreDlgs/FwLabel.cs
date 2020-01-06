// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Simulation of a regular .NET label control which uses a view to display the text.
	/// </summary>
	public class FwLabel : Control, IVwNotifyChange, ISupportInitialize
	{
		#region Data members
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged
		/// section of Dispose. (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;

		/// <summary>The rootsite that occupies 100% of the rectangle of this control</summary>
		private InnerFwTextBox m_innerFwTextBox;
		private ITsString m_tssOrig; // needed to support resize because the inner text box actually changes the string
		private System.Drawing.ContentAlignment m_contentAlignment;
		#endregion

		#region Construction

		/// <summary />
		public FwLabel()
		{
			m_innerFwTextBox = new InnerFwTextBox { ReadOnlyView = true };

			Padding = new Padding(1, 2, 1, 1);
			Controls.Add(m_innerFwTextBox);

			// This causes us to get a notification when the string gets changed,
			// so we can fire our TextChanged event.
			m_sda = m_innerFwTextBox.DataAccess;
			m_sda.AddNotification(this);
			m_innerFwTextBox.AdjustStringHeight = false;
			m_innerFwTextBox.Dock = DockStyle.Fill;
			m_innerFwTextBox.WordWrap = true;
		}
		#endregion

		#region Overrides

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!Application.RenderWithVisualStyles)
			{
				return;
			}

			var renderer = Enabled ? new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal) : new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Disabled);
			renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		/// <inheritdoc />
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			AlignText();
		}

		/// <inheritdoc />
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Tss = m_tssOrig;
			AlignText();
		}

		/// <summary>
		/// Called when the parent is shown for the first time.
		/// </summary>
		private void OnContainingFormShown(object sender, EventArgs e)
		{
			AlignText();
		}
		#endregion

		#region IDisposable Members

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_sda?.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerFwTextBox = null;
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion

		#region IVwNotifyChange Members

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
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// The only property that can change is the string, so report TextChanged.
			OnTextChanged(new EventArgs());
		}

		#endregion

		#region Methods for applying styles and writing systems
		/// <summary>
		/// Applies the specified style to the current selection of the Tss string
		/// </summary>
		public void ApplyStyle(string sStyle)
		{
			m_innerFwTextBox.EditingHelper.ApplyStyle(sStyle);
			m_innerFwTextBox.RefreshDisplay();
		}

		/// <summary>
		/// Applies the specified writing system to the current selection
		/// </summary>
		public void ApplyWS(int hvoWs)
		{
			m_innerFwTextBox.ApplyWS(hvoWs);
		}

		#endregion //Methods for applying styles and writing systems

		#region Selection methods
		/// <summary>
		/// Activates a child control. Optionally specifies the direction in the tab order to
		/// select the control from.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select;
		/// otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward
		/// in the tab order.</param>
		protected override void Select(bool directed, bool forward)
		{
			// a label (and FwLabel) isn't selectable
		}
		#endregion // Selection methods that are for a text box.

		#region Properties
		/// <summary>
		/// Gets the root box.
		/// </summary>
		protected IVwRootBox RootBox => m_innerFwTextBox.RootBox;

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
		[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				return m_innerFwTextBox == null ? "" : m_innerFwTextBox.Text;
			}
			set
			{
				m_innerFwTextBox.Text = value;
				OnTextChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[Browsable(false)]
		public virtual ITsString Tss
		{
			get
			{
				return m_innerFwTextBox.Tss;
			}
			set
			{
				m_tssOrig = value;
				m_innerFwTextBox.Tss = value;
				OnTextChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Gets or sets the text alignment.
		/// </summary>
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
				m_innerFwTextBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// The stylesheet used for the data being displayed.
		/// </summary>
		[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		#endregion // Properties

		#region Helper methods
		/// <summary>
		/// Aligns the inner textbox according to the set TextAlign property.
		/// </summary>
		private void AlignText()
		{
			if (m_innerFwTextBox.RootBox == null)
			{
				return;
			}

			var textRect = m_innerFwTextBox.TextRect;
			var textHeight = textRect.Height;
			var textWidth = textRect.Width;

			if (textHeight > ClientRectangle.Height)
			{
				textHeight = ClientRectangle.Height;
			}
			if (textWidth > ClientRectangle.Width)
			{
				textWidth = ClientRectangle.Width;
			}

			var left = 0;
			var top = 0;
			switch (m_contentAlignment)
			{
				case System.Drawing.ContentAlignment.TopCenter:
				case System.Drawing.ContentAlignment.MiddleCenter:
				case System.Drawing.ContentAlignment.BottomCenter:
					left = (ClientRectangle.Width - textWidth) / 2;
					break;
				case System.Drawing.ContentAlignment.TopRight:
				case System.Drawing.ContentAlignment.MiddleRight:
				case System.Drawing.ContentAlignment.BottomRight:
					left = ClientRectangle.Right - textWidth;
					break;
			}

			switch (m_contentAlignment)
			{
				case System.Drawing.ContentAlignment.MiddleLeft:
				case System.Drawing.ContentAlignment.MiddleCenter:
				case System.Drawing.ContentAlignment.MiddleRight:
					top = (ClientRectangle.Height - textHeight) / 2;
					break;
				case System.Drawing.ContentAlignment.BottomLeft:
				case System.Drawing.ContentAlignment.BottomCenter:
				case System.Drawing.ContentAlignment.BottomRight:
					top = ClientRectangle.Bottom - textHeight;
					break;
			}

			m_innerFwTextBox.Location = new Point(left, top);
			m_innerFwTextBox.Width = textWidth;
			m_innerFwTextBox.Height = m_innerFwTextBox.RootBox.Height;

			var sel = m_innerFwTextBox.RootBox.MakeSimpleSel(true, false, false, false);
			m_innerFwTextBox.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}
		#endregion

		#region ISupportInitialize Members

		/// <summary>
		/// Signals the object that initialization is starting.
		/// </summary>
		public void BeginInit()
		{
			// nothing to do
		}

		/// <summary>
		/// Signals the object that initialization is complete.
		/// </summary>
		public void EndInit()
		{
			var form = FindForm();
			if (form != null)
			{
				form.Shown += OnContainingFormShown;
			}
		}

		#endregion
	}
}