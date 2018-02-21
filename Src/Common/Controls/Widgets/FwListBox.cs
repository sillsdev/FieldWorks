// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// FwListBox is a simulation of a regular Windows.Forms.ListBox. It has much the same
	/// interface, though not all events and properties are yet supported. There are two main
	/// differences:
	/// (1) It is implemented using FieldWorks Views, and hence can render Graphite fonts properly.
	/// (2) Item labels can be TsStrings, in which case, formatting of items can vary based on the
	///		properties of string runs.
	///
	/// To get this behavior, you can
	///		(a) Let the items actually be ITsStrings.
	///		(b) Let the items implement the SIL.FieldWorks.LCM.ITssValue interface, which has just
	///		one property, public ITsString AsTss {get;}
	///
	///	You must also pass your writing system factory to the FwListBox (set the
	/// WritingSystemFactory property). Otherwise, the combo box will not be able to interpret
	/// the writing systems of any TsStrings it is asked to display. It will improve performance
	/// to do this even if you are not using TsString data.
	/// </summary>
	public class FwListBox : Panel, IVwNotifyChange, IFwListBox
	{
		/// <summary />
		public event EventHandler SelectedIndexChanged;
		/// <summary>Sent when the user makes a choice, but it's the same index so
		/// SelectedIndexChanged is not sent.</summary>
		public event EventHandler SameItemSelected;

		/// <summary>The index actually selected.</summary>
		protected int m_selectedIndex;
		/// <summary>The index highlighted, may be different from selected during tracking in
		/// combo. This is set true in a combo box, when we want to track mouse movement by
		/// highlighting the item hovered over. When it is true, changing the selected index does
		/// not trigger events, but a MouseDown does.</summary>
		protected int m_highlightedIndex;
		private bool m_fTracking;
		// Add if we need them.
		//public event EventHandler SelectedValueChanged;
		//public event EventHandler ValueMemberChanged;

		/// <summary>
		/// This is set true in a combo box, when we want to track mouse movement by highlighting
		/// the item hovered over. When it is true, changing the selected index does not trigger
		/// events, but a MouseDown does.
		/// </summary>
		public bool Tracking
		{
			get
			{
				return m_fTracking;
			}
			set
			{
				m_fTracking = value;
			}
		}

		/// <summary>
		/// Gets the inner list box.
		/// </summary>
		internal InnerFwListBox InnerListBox { get; set; }

		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		public IVwStylesheet StyleSheet
		{
			get
			{
				return InnerListBox.StyleSheet;
			}
			set
			{
				InnerListBox.StyleSheet = value;
			}
		}

		/// <summary>
		/// This will be the Control used for tabbing to the next control, since in the context
		/// of an FwComboBox the ComboListBox get created on a separate form than its sibling
		/// ComboTextBox which is on the same form as the other tabstops.
		/// </summary>
		internal Control TabStopControl { get; set; }

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public FwListBox()
		{
			Items = new ObjectCollection(this);
			InnerListBox = new InnerFwListBox(this) {Dock = DockStyle.Fill, ReadOnlyView = true};
			// ComboBoxStyle is always DropDownList.
			BorderStyle = BorderStyle.Fixed3D;
			Controls.Add(InnerListBox);
			// This causes us to get a notification when the string gets changed.
			DataAccess = InnerListBox.DataAccess;
			DataAccess.AddNotification(this);

			// This makes it, by default if the container's initialization doesn't change it,
			// the same default size as a standard list box.
			Size = new Size(120,84);
			// And, if not changed, it's background color is white.
			BackColor = SystemColors.Window;
			m_selectedIndex = -1; // initially nothing selected.
			m_highlightedIndex = -1; // nor highlighted.
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				Items?.Dispose(); // This has to be done, before the next dispose.
				DataAccess?.RemoveNotification(this);
			}
			DataAccess = null;
			Items = null;
			InnerListBox = null;
			TabStopControl = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets the list of items displayed in the listbox
		/// </summary>
		public ObjectCollection Items { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="FwListBox"/> is updating.
		/// </summary>
		public bool Updating { get; private set; }

		/// <summary>
		/// Begins the update.
		/// </summary>
		public void BeginUpdate()
		{
			Updating = true;
		}

		/// <summary>
		/// Ends the update.
		/// </summary>
		public void EndUpdate()
		{
			Updating = false;
			if (Visible)
			{
				InnerListBox.RefreshDisplay();
			}
		}

		/// <summary>
		/// Move the focus to the real view. (Used to capture the mouse also, but that
		/// interferes with the scroll bar, so I'm using a filter instead.)
		/// </summary>
		public void FocusAndCapture()
		{
			InnerListBox.Focus();
		}

		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		/// <remarks>
		/// Doesn't work because value is not a constant.
		/// [ DefaultValueAttribute(SystemColors.Window) ]
		/// </remarks>
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				InnerListBox.BackColor = value;
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
				InnerListBox.ForeColor = value;
				base.ForeColor = value;
			}
		}

		/// <summary>
		/// Gets or sets the index of the selected item in the listbox
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				return m_selectedIndex;
			}
			set
			{
				if (value < -1 || value >= Items.Count)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "index out of range");
				}
				if (m_selectedIndex != value)
				{
					m_selectedIndex = value;
					HighlightedIndex = value;
					if (!IgnoreSelectedIndexChange)
					{
						RaiseSelectedIndexChanged();
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to fire a selected index changed event
		/// </summary>
		internal bool IgnoreSelectedIndexChange { get; set; }

		/// <summary>
		/// Gets/sets the index that is highlighted. May be different from selected when
		/// tracking mouse movement.
		/// </summary>
		public int HighlightedIndex
		{
			get
			{
				return m_highlightedIndex;
			}
			set
			{
				if (value < -1 || value >= Items.Count)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "index out of range");
				}
				if (m_highlightedIndex != value)
				{
					var oldSelIndex = m_highlightedIndex;
					m_highlightedIndex = value;
					// Simulate replacing the old and new item with themselves, to produce
					// the different visual effect.
					if (oldSelIndex != -1 && oldSelIndex < Items.Count)
					{
						InnerListBox.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, oldSelIndex, 1, 1);
					}
					if (m_highlightedIndex != -1)
					{
						InnerListBox.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, m_highlightedIndex, 1, 1);
					}
				}
			}
		}

		/// <summary>
		/// Ensure the root box has been created.
		/// </summary>
		internal void EnsureRoot()
		{
			if (InnerListBox.RootBox == null)
			{
				InnerListBox.MakeRoot();
			}
		}

		/// <summary>
		/// Get or set the selected item. If nothing is selected, get returns null.
		/// If the value passed is not in the list, an ArgumentOutOfRangeException is thrown.
		/// </summary>
		public object SelectedItem
		{
			get
			{
				return GetItem(m_selectedIndex);
			}
			set
			{
				int tmpIndex;
				SetItem(value, out tmpIndex);
				// reset the initial highlighted item to this.
				SelectedIndex = tmpIndex;
			}
		}

		/// <summary>
		/// Get or set the Highlighted item. If nothing is highlighted, get returns null.
		/// If the value passed is not in the list, an ArgumentOutOfRangeException is thrown.
		/// </summary>
		public object HighlightedItem
		{
			get
			{
				return GetItem(HighlightedIndex);
			}
			set
			{
				int tmpIndex;
				SetItem(value, out tmpIndex);
				HighlightedIndex = tmpIndex;
			}
		}

		/// <summary>
		/// Gets the item.
		/// </summary>
		private object GetItem(int itemIndex)
		{
			return itemIndex < 0 ? null : Items[itemIndex];
		}

		/// <summary>
		/// Sets the item.
		/// </summary>
		private void SetItem(object item, out int itemIndex)
		{
			if (item == null)
			{
				itemIndex = -1;
				return;
			}
			var index = Items.IndexOf(item);
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(item), item, "object not found in list");
			}
			itemIndex = index;
		}

		/// <summary>
		/// Answer whether the indicated index is selected. This is trivial now, but will be less
		/// so if we implement multiple selections.
		/// </summary>
		protected internal bool IsSelected(int index)
		{
			return index == m_selectedIndex;
		}

		/// <summary />
		protected internal bool IsHighlighted(int index)
		{
			return index == m_highlightedIndex;
		}

		/// <summary>
		/// Scroll so that the selection can be seen.
		/// </summary>
		public void ScrollHighlightIntoView()
		{
			if (!Visible || HighlightedIndex < 0)
			{
				return;
			}
			Debug.Assert(Visible, "Dropdown list must be visible to scroll into it.");
			var rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = HighlightedIndex;
			rgvsli[0].tag = InnerFwListBox.ktagItems;
			EnsureRoot();
			var sel = InnerListBox.RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null, true, false, false, false, false);
			InnerListBox.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}

		/// <summary>
		/// Return the selected ITsString, or null if no string is selected.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString SelectedTss
		{
			get
			{
				return m_selectedIndex < 0 ? null : TextOfItem(Items[m_selectedIndex]);
			}
			set
			{
				if (value == null)
				{
					SelectedIndex = -1;
				}
				var newsel = FindIndexOfTss(value);
				if (newsel == -1)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "string not found in list");
				}

				SelectedIndex = newsel;
			}
		}

		/// <summary>
		/// Find the index where the specified TsString occurs. If it does not, or the argument
		/// is null, return -1.
		/// </summary>
		protected internal int FindIndexOfTss(ITsString tss)
		{
			if (tss == null)
			{
				return -1;
			}
			for (var i = 0; i < Items.Count; ++i)
			{
				// To avoid some odd comparison problems in tss strings we will just use the text to compare here
				// since the user can't see anything but that in the list box in any case. (LT-16283)
				var listItemString = TextOfItem(Items[i]).Text;
				var searchString = tss.Text;
				if (listItemString != null && searchString != null && listItemString.Equals(searchString, StringComparison.InvariantCulture))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Find the index where the specified string occurs. If it does not, or the argument
		/// is null, return -1.
		/// </summary>
		public int FindStringExact(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return -1;
			}
			for (var i = 0; i < Items.Count; ++i)
			{
				// Enhance JohnT: this is somewhat inefficient, it may convert a string to a Tss
				// and right back again. But it avoids redundant knowledge of how to get a
				// string value from an item.
				if (TextOfItem(Items[i]).Text == str)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// This WritingSystemCode identifies the WS used to convert ordinary strings
		/// to TsStrings for views. If all items in the collection implement ITssValue, or are
		/// ITsStrings, or if the UI writing system of the Writing System Factory is correct,
		/// this need not be set.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int WritingSystemCode
		{
			get
			{
				return InnerListBox.WritingSystemCode;
			}
			set
			{
				InnerListBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return InnerListBox.WritingSystemFactory;
			}
			set
			{
				InnerListBox.WritingSystemFactory = value;
				if (InnerListBox != null)
				{
					InnerListBox.WritingSystemFactory = value;
				}
			}
		}
		#region IVwNotifyChange Members

		/// <summary>
		/// Receives notifications when something in the data cache changes.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// Nothing to do with this yet...maybe we will find something.
		}

		/// <summary>
		/// Fire the SelectedIndexChanged event.
		/// </summary>
		internal void RaiseSelectedIndexChanged()
		{
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Fire the SameItemSelected event.
		/// </summary>
		internal void RaiseSameItemSelected()
		{
			if (SameItemSelected != null )
			{
				SameItemSelected(this, EventArgs.Empty);
			}
			else
			{
				// By default just close the ComboListBox.
				(this as ComboListBox)?.HideForm();
			}
		}

		#endregion

		/// <summary>
		/// Obtain the text corresponding to the specified item in your contents list.
		/// </summary>
		public virtual ITsString TextOfItem(object item)
		{
			// Enhance JohnT: use ValueItem and reflection to retrieve specified property.
			var result = item as ITsString;
			if (result != null)
			{
				return result;
			}
			var tv = item as ITssValue;
			return tv != null ? tv.AsTss : TsStringUtils.MakeString(item?.ToString() ?? string.Empty, WritingSystemCode);
		}

		#region IFwListBox Members

		/// <summary>
		/// Gets the data access.
		/// </summary>
		public ISilDataAccess DataAccess { get; private set; }
		#endregion
	}
}