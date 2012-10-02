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
// File: FwTextBoxControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a FwTextBox that can be hosted in a FwTextBoxCell
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextBoxControl : FwTextBox, IDataGridViewEditingControl
	{
		#region Member variables
		private DataGridView m_dataGridView;
		private bool m_valueChanged;
		private int m_rowIndex;
		private string m_oldText;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxControl()
		{
			// FwTextBox initializes Padding with 1,2,1,1. If we use this in FwTextBoxControl,
			// then the lines are initially clipped (TE-6172).
			//Padding = new Padding(0);

			// Changed this back to its original value in spite of TE-6172. TE-6172 is no
			// longer an issue because of the changes made to draw TsStrings in DataGridView
			// cells using multiscribe rather than as a bitmap. This control is now only
			// used when TsStrings are edited in DataGridView cells. Padding is necessary
			// to keep the text in edit mode and the text in display-only mode, from having
			// such different locations. See TE-6767, TE-6834, TE-6394 and TE-6630.
			Padding = new Padding(1, 2, 1, 1);
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override ITsString Tss
		{
			get { return base.Tss; }
			set
			{
				base.Tss = value;
				m_oldText = value.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.TextChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);

			if (m_dataGridView != null)
			{
				m_valueChanged = m_oldText != Tss.Text;
				m_dataGridView.NotifyCurrentCellDirty(m_valueChanged);
			}
		}
		#endregion

		#region IDataGridViewEditingControl Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the control's user interface (UI) to be consistent with the specified cell
		/// style.
		/// </summary>
		/// <param name="dataGridViewCellStyle">The
		/// <see cref="T:System.Windows.Forms.DataGridViewCellStyle"></see> to use as the model
		/// for the UI.</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
		{
			// Nothing needs to be done for now.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <see cref="T:System.Windows.Forms.DataGridView"></see> that
		/// contains the cell.
		/// </summary>
		/// <value></value>
		/// <returns>The <see cref="T:System.Windows.Forms.DataGridView"></see> that contains
		/// the <see cref="T:System.Windows.Forms.DataGridViewCell"></see> that is being edited;
		/// null if there is no associated <see cref="T:System.Windows.Forms.DataGridView"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public DataGridView EditingControlDataGridView
		{
			get { return m_dataGridView; }
			set { m_dataGridView = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the formatted value of the cell being modified by the editor.
		/// </summary>
		/// <returns>An <see cref="T:System.Object"></see> that represents the formatted value
		/// of the cell.</returns>
		/// ------------------------------------------------------------------------------------
		public object EditingControlFormattedValue
		{
			get
			{
				return GetEditingControlFormattedValue(DataGridViewDataErrorContexts.Formatting
					| DataGridViewDataErrorContexts.Display);
			}
			set
			{
				if (value is string)
				{
					int ws = ((FwTextBoxRow)m_dataGridView.Rows[m_rowIndex]).WritingSystemHandle;
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					Tss = tsf.MakeString((string)value, ws);
				}
				else if (value is ITsString)
					Tss = (ITsString)value;
				else if (value is FwTextBox)
					Tss = ((FwTextBox)value).Tss;
				else if (value is Bitmap)
				{
					Debug.Fail("Don't know what to do if we get a bitmap");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the hosting cell's parent row.
		/// </summary>
		/// <returns>The index of the row that contains the cell, or –1 if there is no parent
		/// row.</returns>
		/// ------------------------------------------------------------------------------------
		public int EditingControlRowIndex
		{
			get { return m_rowIndex; }
			set { m_rowIndex = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the value of the editing control differs
		/// from the value of the hosting cell.
		/// </summary>
		/// <returns>true if the value of the control differs from the cell value; otherwise,
		/// false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool EditingControlValueChanged
		{
			get { return m_valueChanged; }
			set { m_valueChanged = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified key is a regular input key that the editing control
		/// should process or a special key that the <see cref="T:System.Windows.Forms.DataGridView"/>
		/// should process.
		/// </summary>
		/// <param name="keyData">A <see cref="T:System.Windows.Forms.Keys"></see> that
		/// represents the key that was pressed.</param>
		/// <param name="dataGridViewWantsInputKey">true when the
		/// <see cref="T:System.Windows.Forms.DataGridView"></see> wants to process the
		/// <see cref="T:System.Windows.Forms.Keys"></see> in keyData; otherwise, false.</param>
		/// <returns>
		/// true if the specified key is a regular input key that should be handled by the
		/// editing control; otherwise, false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
		{
			switch (keyData & Keys.KeyCode)
			{
				case Keys.PageDown:
				case Keys.PageUp:
				case Keys.Up:
				case Keys.Down:
				case Keys.Enter:
				case Keys.Tab:
					return false;
				default:
					return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cursor used when the mouse pointer is over the
		/// <see cref="P:System.Windows.Forms.DataGridView.EditingPanel"></see> but not over the
		/// editing control.
		/// </summary>
		/// <returns>A <see cref="T:System.Windows.Forms.Cursor"></see> that represents the
		/// mouse pointer used for the editing panel. </returns>
		/// ------------------------------------------------------------------------------------
		public Cursor EditingPanelCursor
		{
			get { return base.Cursor; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the formatted value of the cell.
		/// </summary>
		/// <param name="context">A bitwise combination of
		/// <see cref="T:System.Windows.Forms.DataGridViewDataErrorContexts"></see> values that
		/// specifies the context in which the data is needed.</param>
		/// <returns>
		/// An <see cref="T:System.Object"></see> that represents the formatted version of the
		/// cell contents.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
		{
			// First step is to scroll the selection into view. This is especially important
			// when we switch between RTL and LTR text (otherwise it might show up as white
			// since it is scrolled out of view).
			ScrollSelectionIntoView();

			return (Tss != null ? Tss.Text : string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares the currently selected cell for editing.
		/// </summary>
		/// <param name="selectAll">true to select all of the cell's content; otherwise,
		/// false.</param>
		/// ------------------------------------------------------------------------------------
		public void PrepareEditingControlForEdit(bool selectAll)
		{
			ScrollSelectionIntoView();

			if (selectAll)
				SelectAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the cell contents need to be repositioned
		/// whenever the value changes.
		/// </summary>
		/// <returns>true if the contents need to be repositioned; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool RepositionEditingControlOnValueChange
		{
			get { return false; }
		}
		#endregion
	}
}
