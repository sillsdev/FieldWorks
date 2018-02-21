// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary />
	public class StyleListBoxHelper : BaseStyleListHelper
	{
		/// <summary>
		/// Construct a new StyleListBoxHelper for the given CaseSensistiveListBox.
		/// </summary>
		public StyleListBoxHelper(CaseSensitiveListBox listBox) : base(listBox)
		{
			listBox.DrawMode = DrawMode.OwnerDrawFixed;
			listBox.DrawItem += CtrlDrawItem;
			listBox.SelectedIndexChanged += CtrlSelectedIndexChanged;
			listBox.Sorted = false;
		}

		#region Properties
		/// <summary>
		/// Gets the control cast as a CaseSensistiveListBox
		/// </summary>
		private CaseSensitiveListBox ListBoxControl => (CaseSensitiveListBox)m_ctrl;

		/// <summary>
		/// Gets/sets the SelectedItem property for the style list box.
		/// </summary>
		public override StyleListItem SelectedStyle => (StyleListItem)ListBoxControl.SelectedItem;

		/// <summary>
		/// Gets the Items property from the style list box.
		/// </summary>
		protected override ICollection Items => ListBoxControl.Items;

		/// <summary>
		/// Gets or sets the list box's selected style by name.
		/// </summary>
		public override string SelectedStyleName
		{
			get
			{
				return (ListBoxControl.SelectedIndex != -1 ? SelectedStyle.ToString() : string.Empty);
			}
			set
			{
				if (value != null)
				{
					var i = -1;
					if (value != string.Empty)
					{
						i = ListBoxControl.FindStringExact(value);
					}
					m_ignoreChosenDelegate = true;
					ListBoxControl.SelectedIndex = i;
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
			// Draw the item's background fill
			e.Graphics.FillRectangle(new SolidBrush((selected ? SystemColors.Highlight : SystemColors.Window)), e.Bounds);

			// Don't bother doing any more painting if there isn't anything to paint.
			if (e.Index < 0)
			{
				return;
			};

			var rc = e.Bounds;
			rc.Inflate(-1, 0);
			rc.X += 2;
			rc.Width -= 2;

			// Get the item being drawn.
			var item = (StyleListItem)ListBoxControl.Items[e.Index];

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
			e.Graphics.DrawString(item.Name, m_ctrl.Font,
				selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText,
				rc.Left + ((e.State & DrawItemState.ComboBoxEdit) != 0 ? 0 : icon.Width),
				rc.Top);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Adds the specified new style.
		/// </summary>
		public override void Add(BaseStyleInfo newStyle)
		{
			base.Add(newStyle);
			ListBoxControl.SelectedIndex = ListBoxControl.FindStringExact(newStyle.Name);
		}

		/// <summary>
		/// Removes the specified style from the list
		/// </summary>
		public override void Remove(BaseStyleInfo style)
		{
			// Save the index of the selected item so it can be restored later.
			var oldSelectedIndex = ListBoxControl.SelectedIndex;

			base.Remove(style);

			if (oldSelectedIndex >= ListBoxControl.Items.Count)
			{
				--oldSelectedIndex;
			}
			ListBoxControl.SelectedIndex = oldSelectedIndex;
		}

		/// <summary>
		/// Add item during refresh.
		/// </summary>
		protected override void UpdateStyleList(StyleListItem[] items)
		{
			if (m_ignoreListRefresh)
			{
				return;
			}

			var selectedStyle = ListBoxControl.SelectedItem?.ToString() ?? string.Empty;
			ListBoxControl.Items.Clear();
			ListBoxControl.BeginUpdate();
			ListBoxControl.Items.AddRange(items);
			ListBoxControl.EndUpdate();

			SelectedStyleName = selectedStyle;

			// Ensure an item is selected, even if the previous selection is no longer
			// shown.
			if (!string.IsNullOrEmpty(selectedStyle) && ListBoxControl.SelectedItem == null)
			{
				if (ListBoxControl.Items.Count > 0)
				{
					ListBoxControl.SelectedIndex = 0;
				}
			}
		}
		#endregion
	}
}