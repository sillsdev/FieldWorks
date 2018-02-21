// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Collections;
using System.Drawing;


namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary />
	public class StyleComboListHelper : BaseStyleListHelper
	{
		/// <summary>
		/// Construct a new StyleComboListHelper for the given ComboBox.
		/// </summary>
		public StyleComboListHelper(ComboBox comboBox) : base(comboBox)
		{
			comboBox.DrawMode = DrawMode.OwnerDrawFixed;
			comboBox.DrawItem += CtrlDrawItem;
			comboBox.SelectionChangeCommitted += CtrlSelectedIndexChanged;
			comboBox.DropDown += ComboDropDown;
			comboBox.Sorted = false;
			comboBox.MaxDropDownItems = 30;
			comboBox.KeyPress +=StyleListHelper_KeyPress;
		}

		#region Properties
		/// <summary>
		/// Gets the control cast as a ComboBox
		/// </summary>
		private ComboBox ComboBoxControl => (ComboBox)m_ctrl;

		/// <summary>
		/// Gets the SelectedItem property from the style combo or list box.
		/// </summary>
		public override StyleListItem SelectedStyle => (StyleListItem)ComboBoxControl.SelectedItem;

		/// <summary>
		/// Gets the Items property from the style combo or list box.
		/// </summary>
		protected override ICollection Items => ComboBoxControl.Items;

		/// <summary>
		/// Gets or sets the combo's selected style by name.
		/// </summary>
		public override string SelectedStyleName
		{
			get
			{
				return (ComboBoxControl.SelectedIndex != -1 ? SelectedStyle.ToString() : string.Empty);
			}
			set
			{
				if (value != null)
				{
					var index = -1;
					if (value != string.Empty)
					{
						index = ComboBoxControl.FindString(value);

						// If the style was not found, then it must be currently excluded.
						// If it is a valid style, set it to in use, add it to the ComboBox,
						// and then find its index.
						if (index == -1 && m_styleItemList != null)
						{
							StyleListItem item;
							if (m_styleItemList.TryGetValue(value, out item))
							{
								// We never want to display Normal style if it is not already in
								// the style list.
								if (item.Name != "Normal")
								{
									ComboBoxControl.Items.Add(item);
									index = ComboBoxControl.FindString(value);
								}
							}
						}
					}

					m_ignoreChosenDelegate = true;
					ComboBoxControl.SelectedIndex = index;
					m_ignoreChosenDelegate = false;
				}
			}
		}
		#endregion

		#region List Control Delegate Methods
		/// <summary>
		/// Draw the items in the list
		/// </summary>
		private void CtrlDrawItem(object sender, DrawItemEventArgs e)
		{
			var selected = ((e.State & DrawItemState.Selected) != 0);
			// Draw the item's background fill.
			e.Graphics.FillRectangle(new SolidBrush((selected ? SystemColors.Highlight : SystemColors.Window)), e.Bounds);

			// Don't bother doing any more painting if there isn't anything to paint.
			if (e.Index < 0)
			{
				return;
			}
			var rc = e.Bounds;
			rc.Inflate(-1, 0);
			rc.X += 2;

			// Get the item being drawn.
			var item = (StyleListItem)ComboBoxControl.Items[e.Index];

			// Determine what image to draw, considering the selection state of the item and
			// whether the item is a character style or a paragraph style.
			var icon = GetCorrectIcon(item, selected);

			// Draw the icon only if we're not drawing a combo box's edit portion.
			if ((e.State & DrawItemState.ComboBoxEdit) == 0)
			{
				e.Graphics.DrawImage(icon, rc.Left, rc.Top + (rc.Height - icon.Height) / 2);
			}

			// Draw the item's text, considering the item's selection state. Item text in the
			// edit portion will be draw further left than those in the drop-down because text
			// in the edit portion doesn't have the icon to the left.
			e.Graphics.DrawString(item.Name, m_ctrl.Font, new SolidBrush(selected ? SystemColors.HighlightText : SystemColors.WindowText), rc.Left + ((e.State & DrawItemState.ComboBoxEdit) != 0 ? 0 : icon.Width), rc.Top);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Determines whether the combo box contains the specified style name.
		/// </summary>
		public bool Contains(string styleName)
		{
			return ComboBoxControl.Items.Contains(styleName);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Add item during refresh.
		/// </summary>
		protected override void UpdateStyleList(StyleListItem[] items)
		{
			if (m_ignoreListRefresh)
			{
				return;
			}
			var selectedStyle = ComboBoxControl.SelectedText;
			ComboBoxControl.Items.Clear();
			ComboBoxControl.BeginUpdate();
			ComboBoxControl.Items.AddRange(items);
			ComboBoxControl.EndUpdate();
			SelectedStyleName = selectedStyle;
		}
		#endregion
	}
}