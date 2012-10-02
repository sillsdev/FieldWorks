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
// File: FwInheritablePropComboBox.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a combo box whose whole purpose in life is to represent a property which can be
	/// either explicit or inherited.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwInheritablePropComboBox : FwOverrideComboBox
	{
		#region Data members
		private bool m_ShowingInheritedProperties = true;
		private string m_sUnspecifiedOption;
		#endregion

		#region Event thingies
		/// <summary></summary>
		public event DrawItemEventHandler DrawItemForeground;
		/// <summary></summary>
		public event DrawItemEventHandler DrawItemBackground;
		#endregion

		#region Contructor & Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwInheritablePropComboBox"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwInheritablePropComboBox()
		{
			DrawMode = DrawMode.OwnerDrawFixed;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected index of a combobox adjusted based on whether or not the current
		/// style inherits.
		/// </summary>
		/// <returns>
		/// The index of the value selected from the list WITH the "(unspecified)"
		/// option (i.e., if the displayed list does not include the unspecified option, the
		/// value returned will be one greater than the actual selected index in the control.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int AdjustedSelectedIndex
		{
			get
			{
				CheckDisposed();
				return (ShowingInheritedProperties || SelectedIndex < 0) ? SelectedIndex :
					SelectedIndex + 1;
			}
			set
			{
				CheckDisposed();
				int newIndex = (ShowingInheritedProperties ? value : value - 1);
				Debug.Assert(ShowingInheritedProperties || newIndex >= 0,
					"We shouldn't try select an unspecified index for non inheritable values");
				SelectedIndex = newIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance represents a property which is
		/// inherited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsInherited
		{
			get
			{
				CheckDisposed();
				return SelectedIndex == 0 || ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets a value indicating whether this control's container is currently
		/// displaying properties for a style which inherits from another style or for a
		/// WS-specific override for a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowingInheritedProperties
		{
			get { CheckDisposed(); return m_ShowingInheritedProperties; }
			set
			{
				CheckDisposed();
				if (m_ShowingInheritedProperties == value)
					return;
				m_ShowingInheritedProperties = value;
				if (value)
					Items.Insert(0, m_sUnspecifiedOption);
				else
				{
					if (m_sUnspecifiedOption == null)
					{
						Debug.Assert(Items.Count > 0, "The 0th item in the Items collection should be the 'unspecified' item. But there is no 0th item. Ouch!");
						m_sUnspecifiedOption = Items[0].ToString();
					}
					Items.RemoveAt(0);
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the inheritable prop.
		/// </summary>
		/// <param name="prop">The prop.</param>
		/// <returns><c>true</c> if this control's state has been set to reflect an inherited
		/// property. If this returns <c>false</c>, caller should take whatever action necessary
		/// to select the correct item based on the value of the property (we couldn't come up
		/// with any clean way to encapsulate this behavior).</returns>
		/// ------------------------------------------------------------------------------------
		public bool SetInheritableProp<T>(InheritableStyleProp<T> prop)
		{
			ForeColor = (prop.IsInherited && ShowingInheritedProperties) ?
				SystemColors.GrayText : SystemColors.WindowText;
			if (!prop.ValueIsSet)
			{
				SelectedIndex = 0;
				return true;
			}
			return false;
		}
		#endregion

		#region Drawing code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ComboBox.DrawItem"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DrawItemEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= Items.Count || DesignMode)
			{
				base.OnDrawItem(e); // This can happen if the selected item is removed.
				return;
			}

			Color drawColor = ((e.State & DrawItemState.Selected) != 0) ?
				SystemColors.HighlightText : e.ForeColor;
			Color backColor = ((e.State & DrawItemState.Selected) != 0) ?
				SystemColors.Highlight : e.BackColor;

			// Make sure we always draw the dropdown list in window text color.
			if ((e.State & DrawItemState.ComboBoxEdit) == 0)
				drawColor = SystemColors.WindowText;

			if (DrawItemBackground != null)
			{
				DrawItemBackground(this, new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index,
					e.State, drawColor, backColor));
			}
			else
			{
				// Draw it ourselves, filling in the background and drawing the focus rectangle if needed.
				e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
				if ((e.State & DrawItemState.Focus) != 0)
					e.DrawFocusRectangle();
			}

			if (DrawItemForeground != null)
			{
				DrawItemForeground(this, new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index,
					e.State, drawColor, backColor));
			}
			else
			{
				// Draw it ourselves
				e.Graphics.DrawString(Items[e.Index].ToString(), e.Font, new SolidBrush(drawColor),
					new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height));
			}
		}
		#endregion
	}
}
