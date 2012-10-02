// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StyleListHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleListBoxHelper : BaseStyleListHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new StyleListBoxHelper for the given CaseSensistiveListBox.
		/// </summary>
		/// <param name="listBox">the given CaseSensistiveListBox</param>
		/// ------------------------------------------------------------------------------------
		public StyleListBoxHelper(CaseSensitiveListBox listBox) : base(listBox)
		{
			listBox.DrawMode = DrawMode.OwnerDrawFixed;
			listBox.DrawItem += new DrawItemEventHandler(CtrlDrawItem);
			listBox.SelectedIndexChanged += new EventHandler(CtrlSelectedIndexChanged);
			listBox.Sorted = false;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control cast as a CaseSensistiveListBox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private CaseSensitiveListBox ListBoxControl
		{
			get {return (CaseSensitiveListBox)m_ctrl;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the SelectedItem property for the style list box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override StyleListItem SelectedStyle
		{
			get { return (StyleListItem)ListBoxControl.SelectedItem; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Items property from the style list box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override ICollection Items
		{
			get {return ListBoxControl.Items;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list box's selected style by name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SelectedStyleName
		{
			get
			{
				return (ListBoxControl.SelectedIndex != -1 ?
					SelectedStyle.ToString() : string.Empty);
			}
			set
			{
				if (value != null)
				{
					int i = -1;

					if (value != string.Empty)
					{
						i = ListBoxControl.FindStringExact(value);

						// If the style was not found, then it must be currently excluded.
						// Mark the style level as "in use" and add it to the CaseSensistiveListBox.
						// Then look for it again in the CaseSensistiveListBox.
						if (i == -1 && m_styleItemList != null)
						{
							StyleListItem item;
							if (m_styleItemList.TryGetValue(value, out item))
							{
								item.SetInUse(true);
								Refresh();
								i = ListBoxControl.FindStringExact(value);
							}
						}
					}

					m_ignoreChosenDelegate = true;
					ListBoxControl.SelectedIndex = i;
					m_ignoreChosenDelegate = false;
				}
			}
		}
		#endregion

		#region List Control Delegate Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the items in the list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void CtrlDrawItem(object sender, DrawItemEventArgs e)
		{
			bool selected = ((e.State & DrawItemState.Selected) != 0);

			// Draw the item's background fill.
			e.Graphics.FillRectangle(new SolidBrush((selected ?
				SystemColors.Highlight : SystemColors.Window)), e.Bounds);

			// Don't bother doing any more painting if there isn't anything to paint.
			if (e.Index < 0)
				return;

			Rectangle rc = e.Bounds;
			rc.Inflate(-1, 0);
			rc.X += 2;
			rc.Width -= 2;

			// Get the item being drawn.
			StyleListItem item = (StyleListItem)ListBoxControl.Items[e.Index];

			//// If this is a current style, then draw a triangle mark
			//const int triangleHeight = 8;
			//if (item.IsCurrentStyle)
			//{
			//    Point[] triangle = new Point[] {
			//        new Point(rc.Left, rc.Top + (rc.Height - triangleHeight) / 2),
			//        new Point(rc.Left, rc.Top + (rc.Height + triangleHeight) / 2),
			//        new Point(rc.Left + triangleHeight * 3 / 4, rc.Top + (rc.Bottom - rc.Top) / 2)};
			//    e.Graphics.FillPolygon(
			//        selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText,
			//        triangle);
			//}
			//rc.X += triangleHeight;
			//rc.Width -= triangleHeight;

			// Determine what image to draw, considering the selection state of the item and
			// whether the item is a character style or a paragraph style.
			Image icon = GetCorrectIcon(item, selected);

			// Draw the icon only if we're not drawing a combo box's edit portion.
			if ((e.State & DrawItemState.ComboBoxEdit) == 0)
				e.Graphics.DrawImage(icon, rc.Left, rc.Top + (rc.Height - icon.Height) / 2);

			// Draw the item's text, considering the item's selection state. Item text in the
			// edit portion will be draw further left than those in the drop-down because text
			// in the edit portion doesn't have the icon to the left.
			e.Graphics.DrawString(item.Name, m_ctrl.Font,
				selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText,
				rc.Left + ((e.State & DrawItemState.ComboBoxEdit) != 0 ? 0 : icon.Width),
				rc.Top);
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified new style.
		/// </summary>
		/// <param name="newStyle">The new style.</param>
		/// ------------------------------------------------------------------------------------
		public override void Add(BaseStyleInfo newStyle)
		{
			base.Add(newStyle);
			int index = ListBoxControl.FindStringExact(newStyle.Name);
			ListBoxControl.SelectedIndex = index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified style from the list
		/// </summary>
		/// <param name="style">The style.</param>
		/// ------------------------------------------------------------------------------------
		public override void Remove(BaseStyleInfo style)
		{
			// Save the index of the selected item so it can be restored later.
			int oldSelectedIndex = ListBoxControl.SelectedIndex;

			base.Remove(style);

			if (oldSelectedIndex >= ListBoxControl.Items.Count)
				--oldSelectedIndex;
			ListBoxControl.SelectedIndex = oldSelectedIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the styles in the m_ctrl list based on the ExcludeStylesWithContext and
		/// ShowOnlyStylesOfType properties. This should be called when the caller wants to
		/// update the m_ctrl list after changing one of those two properties, but doesn't want
		/// to rebuild the entire m_styleItemList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Refresh()
		{
			if (m_ignoreListRefresh)
				return;

			string selectedStyle = ListBoxControl.SelectedItem != null ?
				ListBoxControl.SelectedItem.ToString() : string.Empty;
			ListBoxControl.Items.Clear();
			ListBoxControl.BeginUpdate();
			RefreshStyleItemList(selectedStyle);
			ListBoxControl.EndUpdate();
		}

		/// <summary>
		/// Add item during refresh.
		/// </summary>
		/// <param name="item"></param>
		protected override void AddStyleListItem(StyleListItem item)
		{
			ListBoxControl.Items.Add(item);
		}
		#endregion
	}
}
