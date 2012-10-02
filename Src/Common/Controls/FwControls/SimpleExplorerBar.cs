using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a simple explorer bar-like control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SimpleExplorerBar : FwPanel
	{
		/// <summary></summary>
		public delegate void ExplorerBarItemStateChangedHandler(
			SimpleExplorerBar expBar, ExplorerBarItem item);

		/// <summary></summary>
		public event ExplorerBarItemStateChangedHandler ItemCollapsed;
		/// <summary></summary>
		public event ExplorerBarItemStateChangedHandler ItemExpanded;

		private readonly List<ExplorerBarItem> m_items = new List<ExplorerBarItem>();

		private bool m_alwaysShowVScroll = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a SimpleExplorerBar object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleExplorerBar()
		{
			base.AutoScroll = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collection of ExplorerBarItems contained in the explorer bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExplorerBarItem[] Items
		{
			get {return m_items.ToArray();}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control's background color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Color BackColor
		{
			get	{return base.BackColor;}
			set
			{
				base.BackColor = value;
				foreach (ExplorerBarItem item in m_items)
					item.BackColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to always show the vertical scroll
		/// bar. If this is true, it will be disabled when it is not needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AlwaysShowVScroll
		{
			get { return m_alwaysShowVScroll; }
			set
			{
				m_alwaysShowVScroll = value;
				ManageVScrollBar();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an ExplorerBarItem with the specified text and hosting the specified control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExplorerBarItem Add(string text, Control control)
		{
			ExplorerBarItem item = new ExplorerBarItem(text, control);
			Add(item);
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified item to the item collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Add(ExplorerBarItem item)
		{
			item.Dock = DockStyle.Top;
			item.BackColor = BackColor;
			m_items.Add(item);
			Controls.Add(item);
			item.BringToFront();

			item.Collapsed += HandleItemExpandingOrCollapsing;
			item.Expanded += HandleItemExpandingOrCollapsing;
			item.SizeChanged += item_SizeChanged;
			ManageVScrollBar();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles an item expanding or collapsing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleItemExpandingOrCollapsing(object sender, EventArgs e)
		{
			ExplorerBarItem item = sender as ExplorerBarItem;
			if (item == null)
				return;

			ScrollControlIntoView(item);
			ManageVScrollBar();

			if (item.IsExpanded && ItemExpanded != null)
				ItemExpanded(this, item);
			else if (!item.IsExpanded && ItemCollapsed != null)
				ItemCollapsed(this, item);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SizeChanged event for the ExplorerBarItems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void item_SizeChanged(object sender, EventArgs e)
		{
			ManageVScrollBar();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Manages the visibility and enabled state of the vertical scroll bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ManageVScrollBar()
		{
			if (!m_alwaysShowVScroll)
				return;

			int totalItemHeights = 0;
			foreach (ExplorerBarItem item in m_items)
				totalItemHeights += item.Height;

			if (totalItemHeights > ClientSize.Height)
			{
				VerticalScroll.Enabled = true;
				AutoScroll = true;
			}
			else
			{
				AutoScroll = false;
				VerticalScroll.Visible = true;
				VerticalScroll.Enabled = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified item from the collection of items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Remove(ExplorerBarItem item)
		{
			if (item != null && m_items.Contains(item))
			{
				Controls.Remove(item);
				m_items.Remove(item);
				item.Collapsed -= HandleItemExpandingOrCollapsing;
				item.Expanded -= HandleItemExpandingOrCollapsing;
				item.SizeChanged -= item_SizeChanged;
				ManageVScrollBar();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes from the item collection the item at the specified index.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Remove(int index)
		{
			if (index < m_items.Count)
				Remove(m_items[index]);
		}
	}
}
