using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using System.Diagnostics;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	///
	/// </summary>
	public interface IFwListBox
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the text corresponding to the specified item in your contents list.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ITsString TextOfItem(object item);

		/// <summary>
		/// Gets the data access.
		/// </summary>
		/// <value></value>
		ISilDataAccess DataAccess { get; }

		/// <summary>
		///
		/// </summary>
		int SelectedIndex { get; set; }
	}

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
	///		(b) Let the items implement the SIL.FieldWorks.FDO.ITssValue interface, which has just
	///		one property, public ITsString AsTss {get;}
	///
	///	You must also pass your writing system factory to the FwListBox (set the
	/// WritingSystemFactory property). Otherwise, the combo box will not be able to interpret
	/// the writing systems of any TsStrings it is asked to display. It will improve performance
	/// to do this even if you are not using TsString data.
	/// </summary>
	public class FwListBox : Panel, IFWDisposable, IVwNotifyChange, IFwListBox
	{
		/// <summary></summary>
		public event EventHandler SelectedIndexChanged;
		/// <summary>Sent when the user makes a choice, but it's the same index so
		/// SelectedIndexChanged is not sent.</summary>
		public event EventHandler SameItemSelected;

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		internal InnerFwListBox m_innerFwListBox;
		private ObjectCollection m_items;
		/// <summary>The index actually selected.</summary>
		protected int m_SelectedIndex;
		/// <summary>The index highlighted, may be different from selected during tracking in
		/// combo. This is set true in a combo box, when we want to track mouse movement by
		/// highlighting the item hovered over. When it is true, changing the selected index does
		/// not trigger events, but a MouseDown does.</summary>
		protected int m_HighlightedIndex;
		private bool m_fTracking = false;
		// Add if we need them.
		//public event EventHandler SelectedValueChanged;
		//public event EventHandler ValueMemberChanged;
		private Control m_tabStopControl = null;	// see comments on TabStopControl property.

		/// <summary>
		/// This is set true in a combo box, when we want to track mouse movement by highlighting
		/// the item hovered over. When it is true, changing the selected index does not trigger
		/// events, but a MouseDown does.
		/// </summary>
		public bool Tracking
		{
			get
			{
				CheckDisposed();
				return m_fTracking;
			}
			set
			{
				CheckDisposed();
				m_fTracking = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inner list box.
		/// </summary>
		/// <value>The inner list box.</value>
		/// ------------------------------------------------------------------------------------
		internal InnerFwListBox InnerListBox
		{
			get
			{
				CheckDisposed();
				return m_innerFwListBox;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		/// <value>The style sheet.</value>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_innerFwListBox.StyleSheet;
			}
			set
			{
				CheckDisposed();
				m_innerFwListBox.StyleSheet = value;
			}
		}

		/// <summary>
		/// This will be the Control used for tabbing to the next control, since in the context
		/// of an FwComboBox the ComboListBox get created on a separate form than its sibling
		/// ComboTextBox which is on the same form as the other tabstops.
		/// </summary>
		internal Control TabStopControl
		{
			get
			{
				CheckDisposed();
				return m_tabStopControl;
			}
			set
			{
				CheckDisposed();
				m_tabStopControl = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwListBox()
		{
			m_items = new ObjectCollection(this);
			m_innerFwListBox = new InnerFwListBox(this);
			m_innerFwListBox.Dock = DockStyle.Fill;
			m_innerFwListBox.ReadOnlyView = true;		// ComboBoxStyle is always DropDownList.
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Controls.Add(m_innerFwListBox);
			// This causes us to get a notification when the string gets changed.
			m_sda = m_innerFwListBox.DataAccess;
			m_sda.AddNotification(this);

			// This makes it, by default if the container's initialization doesn't change it,
			// the same default size as a standard list box.
			this.Size = new Size(120,84);
			// And, if not changed, it's background color is white.
			this.BackColor = SystemColors.Window;
			m_SelectedIndex = -1; // initially nothing selected.
			m_HighlightedIndex = -1; // nor highlighted.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Don't call Controls.Clear(). The Controls collection will be taken care of in the base class

				if (m_items != null)
					m_items.Dispose(); // This has to be done, before the next dispose.

				if (m_sda != null)
					m_sda.RemoveNotification(this);

				// Don't explicitly dispose inner list box - it gets disposed as part of Controls!
			}
			m_sda = null;
			m_items = null;
			m_innerFwListBox = null;
			m_tabStopControl = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of items displayed in the listbox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ObjectCollection Items
		{
			get
			{
				CheckDisposed();
				return m_items;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the focus to the real view. (Used to capture the mouse also, but that
		/// interferes with the scroll bar, so I'm using a filter instead.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FocusAndCapture()
		{
			CheckDisposed();

			m_innerFwListBox.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the default on BackColor, and copies it to the embedded window.
		/// </summary>
		/// <remarks>
		/// Doesn't work because value is not a constant.
		/// [ DefaultValueAttribute(SystemColors.Window) ]
		/// </remarks>
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

				m_innerFwListBox.BackColor = value;
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

				m_innerFwListBox.ForeColor = value;
				base.ForeColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the selected item in the listbox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectedIndex
		{
			get
			{
				CheckDisposed();

				return m_SelectedIndex;
			}
			set
			{
				CheckDisposed();

				if (value < -1 || value >= m_items.Count)
					throw new ArgumentOutOfRangeException("value", value, "index out of range");
				if (m_SelectedIndex != value)
				{
					m_SelectedIndex = value;
					HighlightedIndex = value;
					if (!IgnoreSelectedIndexChange)
						RaiseSelectedIndexChanged();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to fire a selected index changed event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IgnoreSelectedIndexChange { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the index that is highlighted. May be different from selected when
		/// tracking mouse movement.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HighlightedIndex
		{
			get
			{
				CheckDisposed();

				return m_HighlightedIndex;
			}
			set
			{
				CheckDisposed();

				if (value < -1 || value >= m_items.Count)
					throw new ArgumentOutOfRangeException("value", value, "index out of range");
				if (m_HighlightedIndex != value)
				{
					int oldSelIndex = m_HighlightedIndex;
					m_HighlightedIndex = value;
					// Simulate replacing the old and new item with themselves, to produce
					// the different visual effect.
					if (oldSelIndex != -1 && oldSelIndex < m_items.Count)
					{
						m_innerFwListBox.DataAccess.PropChanged(null,
							(int)PropChangeType.kpctNotifyAll,
							InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
							oldSelIndex, 1, 1);
					}
					if (m_HighlightedIndex != -1)
					{
						m_innerFwListBox.DataAccess.PropChanged(null,
							(int)PropChangeType.kpctNotifyAll,
							InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
							m_HighlightedIndex, 1, 1);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure the root box has been created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void EnsureRoot()
		{
			CheckDisposed();

			if (m_innerFwListBox.RootBox == null)
				m_innerFwListBox.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the selected item. If nothing is selected, get returns null.
		/// If the value passed is not in the list, an ArgumentOutOfRangeException is thrown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object SelectedItem
		{
			get
			{
				CheckDisposed();

				return GetItem(m_SelectedIndex);
			}
			set
			{
				CheckDisposed();

				int tmpIndex = SelectedIndex;
				SetItem(value, out tmpIndex);
				// reset the initial highlighted item to this.
				this.SelectedIndex = tmpIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the Highlighted item. If nothing is highlighted, get returns null.
		/// If the value passed is not in the list, an ArgumentOutOfRangeException is thrown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object HighlightedItem
		{
			get
			{
				CheckDisposed();

				return GetItem(HighlightedIndex);
			}
			set
			{
				CheckDisposed();

				int tmpIndex = HighlightedIndex;
				SetItem(value, out tmpIndex);
				HighlightedIndex = tmpIndex;
				if (this.Visible)
					ScrollHighlightIntoView();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the item.
		/// </summary>
		/// <param name="itemIndex">Index of the item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object GetItem(int itemIndex)
		{
			if (itemIndex < 0)
				return null;
			return m_items[itemIndex];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="itemIndex">Index of the item.</param>
		/// ------------------------------------------------------------------------------------
		private void SetItem(object item, out int itemIndex)
		{
			if (item == null)
			{
				itemIndex = -1;
				return;
			}
			int index = m_items.IndexOf(item);
			if (index < 0)
				throw new ArgumentOutOfRangeException("value", item, "object not found in list");
			itemIndex = index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer whether the indicated index is selected. This is trivial now, but will be less
		/// so if we implement multiple selections.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected internal bool IsSelected(int index)
		{
			return index == m_SelectedIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected internal bool IsHighlighted(int index)
		{
			return index == m_HighlightedIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll so that the selection can be seen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ScrollHighlightIntoView()
		{
			if (HighlightedIndex < 0)
				return;
			Debug.Assert(this.Visible == true, "Dropdown list must be visible to scroll into it.");
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = HighlightedIndex;
			rgvsli[0].tag = InnerFwListBox.ktagItems;
			EnsureRoot();
			IVwSelection sel = m_innerFwListBox.RootBox.MakeTextSelInObj(0, rgvsli.Length,
				rgvsli, 0, null, true, false, false, false, false);
			m_innerFwListBox.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the selected ITsString, or null if no string is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString SelectedTss
		{
			get
			{
				CheckDisposed();

				if (m_SelectedIndex < 0)
					return null;
				return TextOfItem(m_items[m_SelectedIndex]);
			}
			set
			{
				CheckDisposed();

				if (value == null)
					SelectedIndex = -1;
				int newsel = FindIndexOfTss(value);
				if (newsel == -1)
					throw new ArgumentOutOfRangeException("value", value, "string not found in list");
				else
					SelectedIndex = newsel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index where the specified TsString occurs. If it does not, or the argument
		/// is null, return -1.
		/// </summary>
		/// <param name="tss"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected internal int FindIndexOfTss(ITsString tss)
		{
			if (tss == null)
				return -1;
			for (int i = 0; i < m_items.Count; ++i)
			{
				if (TextOfItem(m_items[i]).Equals(tss))
					return i;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index where the specified string occurs. If it does not, or the argument
		/// is null, return -1.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int FindStringExact(string str)
		{
			CheckDisposed();

			if (str == null || str == "")
				return -1;
			for (int i = 0; i < m_items.Count; ++i)
			{
				// Enhance JohnT: this is somewhat inefficient, it may convert a string to a Tss
				// and right back again. But it avoids redundant knowledge of how to get a
				// string value from an item.
				if (TextOfItem(m_items[i]).Text == str)
					return i;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This WritingSystemCode identifies the WS used to convert ordinary strings
		/// to TsStrings for views. If all items in the collection implement ITssValue, or are
		/// ITsStrings, or if the UI writing system of the Writing System Factory is correct,
		/// this need not be set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int WritingSystemCode
		{
			get
			{
				CheckDisposed();

				return m_innerFwListBox.WritingSystemCode;
			}
			set
			{
				CheckDisposed();

				m_innerFwListBox.WritingSystemCode = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				return m_innerFwListBox.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				m_innerFwListBox.WritingSystemFactory = value;
				if (m_innerFwListBox != null)
					m_innerFwListBox.WritingSystemFactory = value;
			}
		}
		#region IVwNotifyChange Members

		/// <summary>
		/// Receives notifications when something in the data cache changes.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// Nothing to do with this yet...maybe we will find something.
		}

		/// <summary>
		/// Fire the SelectedIndexChanged event.
		/// </summary>
		internal void RaiseSelectedIndexChanged()
		{
			CheckDisposed();

			if (SelectedIndexChanged != null ) SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary>
		/// Fire the SameItemSelected event.
		/// </summary>
		internal void RaiseSameItemSelected()
		{
			CheckDisposed();

			if (SameItemSelected != null )
			{
				SameItemSelected(this, EventArgs.Empty);
			}
			else
			{
				// By default just close the ComboListBox.
				if (this is ComboListBox)
					(this as ComboListBox).HideForm();
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain the text corresponding to the specified item in your contents list.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString TextOfItem(object item)
		{
			CheckDisposed();

			// Enhance JohnT: use ValueItem and reflection to retrieve specified property.
			ITsString result = item as ITsString;
			if (result != null)
				return result;
			ITssValue tv = item as ITssValue;
			if (tv != null)
				return tv.AsTss;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(item != null ? item.ToString() : string.Empty, WritingSystemCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Like ListBox, FwListBox defines an ObjectCollection class for the items
		/// in the list. This class provides a subset of the functionality of the
		/// ArrayList used to implement it. The reason for not using an ArrayList is that
		/// operations that change the contents of the list have to have side effects
		/// on the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class ObjectCollection : IList, IEnumerable, IFWDisposable
		{
			private ArrayList m_list;
			private IFwListBox m_owner;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Construct empty.
			/// </summary>
			/// <param name="owner"></param>
			/// ------------------------------------------------------------------------------------
			public ObjectCollection(IFwListBox owner)
			{
				m_list = new ArrayList();
				m_owner = owner;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Construct with supplied initial items.
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="values"></param>
			/// ------------------------------------------------------------------------------------
			public ObjectCollection(IFwListBox owner, object[] values)
			{
				m_list = new ArrayList(values);
				m_owner = owner;
			}

			#region IDisposable & Co. implementation
			// Region last reviewed: never

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

			/// <summary>
			/// True, if the object has been disposed.
			/// </summary>
			private bool m_isDisposed = false;

			/// <summary>
			/// See if the object has been disposed.
			/// </summary>
			public bool IsDisposed
			{
				get { return m_isDisposed; }
			}

			/// <summary>
			/// Finalizer, in case client doesn't dispose it.
			/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
			/// </summary>
			/// <remarks>
			/// In case some clients forget to dispose it directly.
			/// </remarks>
			~ObjectCollection()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
			}

			/// <summary>
			///
			/// </summary>
			/// <remarks>Must not be virtual.</remarks>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SupressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

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
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
				// Must not be run more than once.
				if (m_isDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
					Clear();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_list = null;
				m_owner = null;

				m_isDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			bool IList.IsFixedSize
			{
				get
				{
					CheckDisposed();
					return false;
				}
			}
			/// <summary>Numbef of items. </summary>
			public virtual int Count
			{
				get
				{
					CheckDisposed();
					return m_list.Count;
				}
			}
			/// <summary>Always false; we don't support the DataSource approach. </summary>
			public virtual bool IsReadOnly
			{
				get
				{
					CheckDisposed();
					return false;
				}
			}
			/// <summary>
			/// Indexer. Set must modify the display.
			/// </summary>
			public virtual object this[int index]
			{
				get
				{
					CheckDisposed();
					return m_list[index];
				}
				set
				{
					CheckDisposed();

					ITsString oldText = m_owner.TextOfItem(m_list[index]);
					m_list[index] = value;
					ITsString newText = m_owner.TextOfItem(m_list[index]);
					if (!oldText.Equals(newText))
					{
						int hvo = m_owner.DataAccess.get_VecItem(
							InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index);
						m_owner.DataAccess.SetString(hvo,
							InnerFwListBox.ktagText, newText);
						m_owner.DataAccess.PropChanged(null,
							(int)PropChangeType.kpctNotifyAll,
							hvo, InnerFwListBox.ktagText,
							0, newText.Length, oldText.Length);
					}
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Add an item to the collection and the display.
			/// </summary>
			/// <param name="item"></param>
			/// <returns>zero-based index of position of new item.</returns>
			/// ------------------------------------------------------------------------------------
			public int Add(object item)
			{
				CheckDisposed();

				int index = m_list.Count; // nb index is count BEFORE Add.
				m_list.Add(item);
				InsertItemAtIndex(index, item);
				return index;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Add a whole collection of objects.
			/// </summary>
			/// <param name="items"></param>
			/// ------------------------------------------------------------------------------------
			public void AddRange(Object[] items)
			{
				CheckDisposed();

				int index = m_list.Count; // nb index is count BEFORE Add.
				m_list.AddRange(items);
				for (int i = 0; i < items.Length; ++i)
				{
					int hvoNew = m_owner.DataAccess.MakeNewObject(InnerFwListBox.kclsItem,
						InnerFwListBox.khvoRoot,
						InnerFwListBox.ktagItems, index + i);
					m_owner.DataAccess.SetString(hvoNew,
						InnerFwListBox.ktagText,  m_owner.TextOfItem(items[i]));
				}
				m_owner.DataAccess.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll,
					InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
					index, items.Length, 0);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Clear all items.
			/// </summary>
			/// <remarks>
			/// Enhance JohnT: add the version that takes an ObjectCollection.
			/// </remarks>
			/// ------------------------------------------------------------------------------------
			public virtual void Clear()
			{
				CheckDisposed();

				int citems = m_list.Count;
				for (int i = 0; i < citems; i++)
				{
					// Dispose items
					var disposable = m_list[i] as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				m_list.Clear();
				var cda = m_owner.DataAccess as IVwCacheDa;
				if (cda == null)
					return; // This can happen, when this is called when 'disposing' is false.
				cda.CacheVecProp(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, new int[0], 0);
				m_owner.DataAccess.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll,
					InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
					0, 0, citems);

				m_owner.SelectedIndex = -1;

				Debug.Assert(m_owner.DataAccess.get_VecSize(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems) == 0);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// See if the item is present.
			/// </summary>
			/// <param name="item"></param>
			/// <returns></returns>
			/// ------------------------------------------------------------------------------------
			public virtual bool Contains(object item)
			{
				CheckDisposed();

				return m_list.Contains(item);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Copy to a destination array.
			/// </summary>
			/// <param name="dest"></param>
			/// <param name="arrayIndex"></param>
			/// ------------------------------------------------------------------------------------
			void ICollection.CopyTo(Array dest, int arrayIndex)
			{
				CheckDisposed();

				m_list.CopyTo(dest, arrayIndex);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Syncrhonization is not supported.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			object ICollection.SyncRoot
			{
				get
				{
					CheckDisposed();
					return null;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Synchronization is not supported.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			bool ICollection.IsSynchronized
			{
				get
				{
					CheckDisposed();
					return false;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Get an enumerator for the list.
			/// </summary>
			/// <returns></returns>
			/// ------------------------------------------------------------------------------------
			public virtual IEnumerator GetEnumerator()
			{
				CheckDisposed();
				return m_list.GetEnumerator();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Find the zero-based position of the item, or -1 if not found.
			/// </summary>
			/// <param name="item"></param>
			/// <returns></returns>
			/// ------------------------------------------------------------------------------------
			public virtual int IndexOf(object item)
			{
				CheckDisposed();
				return m_list.IndexOf(item);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Insert the specified item at the specified position.
			/// </summary>
			/// <param name="index"></param>
			/// <param name="item"></param>
			/// ------------------------------------------------------------------------------------
			public virtual void Insert(int index, object item)
			{
				CheckDisposed();

				m_list.Insert(index, item);
				InsertItemAtIndex(index, item);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Shared function for Insert and Add.
			/// </summary>
			/// <param name="index"></param>
			/// <param name="item"></param>
			/// ------------------------------------------------------------------------------------
			protected void InsertItemAtIndex(int index, object item)
			{
				int hvoNew = m_owner.DataAccess.MakeNewObject(InnerFwListBox.kclsItem,
					InnerFwListBox.khvoRoot,
					InnerFwListBox.ktagItems, index);
				m_owner.DataAccess.SetString(hvoNew,
					InnerFwListBox.ktagText, m_owner.TextOfItem(item));
				m_owner.DataAccess.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll,
					InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
					index, 1, 0);

				if (m_owner.SelectedIndex >= index)
					m_owner.SelectedIndex = m_owner.SelectedIndex + 1;

				Debug.Assert(m_owner.DataAccess.get_VecSize(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems) == m_list.Count);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="item"></param>
			/// --------------------------------------------------------------------------------
			public virtual void Remove(object item)
			{
				CheckDisposed();

				int index = m_list.IndexOf(item);
				if (index >= 0)
					RemoveAt(index);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="index"></param>
			/// --------------------------------------------------------------------------------
			public virtual void RemoveAt(int index)
			{
				CheckDisposed();

				m_list.RemoveAt(index);
				int hvoObj = m_owner.DataAccess.get_VecItem(
					InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index);
				m_owner.DataAccess.DeleteObjOwner(
					InnerFwListBox.khvoRoot, hvoObj, InnerFwListBox.ktagItems, index);
				m_owner.DataAccess.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll,
					InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems,
					index, 0, 1);

				if (m_owner.SelectedIndex >= index)
					m_owner.SelectedIndex = m_owner.SelectedIndex - 1;
			}
		}

		#region IFwListBox Members


		/// <summary>
		/// Gets the data access.
		/// </summary>
		/// <value></value>
		public ISilDataAccess DataAccess
		{
			get { return m_sda; }
		}

		#endregion
	}

	internal interface IHighlightInfo
	{
		bool ShowHighlight { get; set; }
		bool IsHighlighted(int index);
	}

	internal interface IFwListBoxSite : IHighlightInfo, IWritingSystemAndStylesheet
	{
		Color ForeColor { set; get; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// InnerFwListBox implements the main body of an FwListBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class InnerFwListBox : SimpleRootSite, IFwListBoxSite
	{
		// This 'view' displays the strings representing the list items by representing
		// each string as property ktagText of one of the objects of ktagItems of
		// object khvoRoot. In addition, for each item we display a long string of blanks,
		// so we can make the selection highlight go the full width of the window.
		protected internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		protected internal const int ktagItems = 9002;
		protected internal const int kfragRoot = 8002; // likewise.
		protected internal const int kfragItems = 8003;
		protected internal const int khvoRoot = 7003; // likewise.
		protected internal const int kclsItem = 5007;
		// Our own cache, so we need to get rid of it.
		protected IVwCacheDa m_CacheDa; // Main cache object
		protected ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
		protected FwListBox m_owner;
		private ListBoxVc m_vc;

		protected int m_WritingSystem; // Writing system to use when Text is set.

		// Set this false to (usually temporarily) disable changing the background color
		// for the selected item. This allows us to get an accurate figure for the overall
		// width of the view.
		bool m_fShowHighlight = true;

		/// <summary>
		/// Constructor
		/// </summary>
		internal InnerFwListBox(FwListBox owner)
		{
			m_owner = owner;
			m_CacheDa = VwCacheDaClass.Create();
			m_DataAccess = (ISilDataAccess)m_CacheDa;
			// So many things blow up so badly if we don't have one of these that I finally decided to just
			// make one, even though it won't always, perhaps not often, be the one we want.
			m_wsf = new PalasoWritingSystemManager();
			m_DataAccess.WritingSystemFactory = WritingSystemFactory;
			this.VScroll = true;
			this.AutoScroll = true;
		}

		internal new ISilDataAccess DataAccess
		{
			get
			{
				CheckDisposed();

				return m_DataAccess;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();

				if (m_WritingSystem == 0)
					m_WritingSystem = WritingSystemFactory.UserWs;
				return m_WritingSystem;
			}
			set
			{
				CheckDisposed();

				m_WritingSystem = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ListBoxVc ViewConstructor
		{
			get { return m_vc; }
			set { m_vc = value; }
		}

		public bool ShowHighlight
		{
			get
			{
				CheckDisposed();

				return m_fShowHighlight;
			}
			set
			{
				CheckDisposed();

				if (value == m_fShowHighlight)
					return;
				m_fShowHighlight = value;
				if (RootBox != null)
					RootBox.Reconstruct();
			}
		}


		public override int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();

			// Simulate infinite width. I (JohnT) think the / 2 is a good idea to prevent overflow
			// if the view code at some point adds a little bit to it.
			// return Int32.MaxValue / 2;
			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			return 1000000;
		}

		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				if (m_wsf == null)
					m_wsf = new PalasoWritingSystemManager();
				return base.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				if (m_wsf != value)
				{
					base.WritingSystemFactory = value;
					// Enhance JohnT: this should probably be done by the base class.
					m_DataAccess.WritingSystemFactory = value;
					m_WritingSystem = 0; // gets reloaded if used.
					if (m_vc != null)
					{
						m_vc.UpdateBlankString(this);
					}
					if (m_rootb != null)
						m_rootb.Reconstruct();
				}
			}
		}

		internal FwListBox Owner
		{
			get
			{
				CheckDisposed();

				return m_owner;
			}
		}

		/// <summary>
		/// Create the root box and initialize it.
		/// </summary>
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_DataAccess;
			if (m_vc == null)
				m_vc = new ListBoxVc(this);
			m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			EditingHelper.DefaultCursor = Cursors.Arrow;
			base.MakeRoot();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Cleanup managed stuff here.
				if (m_CacheDa != null)
					m_CacheDa.ClearAllData();
			}
			// Cleanup unmanaged stuff here.
			m_DataAccess = null;
			if (m_CacheDa != null)
			{
				if (Marshal.IsComObject(m_CacheDa))
					Marshal.ReleaseComObject(m_CacheDa);
				m_CacheDa = null;
			}
			m_owner = null; // It will get disposed on its own, if it hasn't been already.
			m_vc = null;

			base.Dispose(disposing);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (this.Visible && e.Button == MouseButtons.Left)
			{
				base.OnMouseUp (e);
				if (m_owner.SelectedIndex == m_owner.HighlightedIndex)
					m_owner.RaiseSameItemSelected();
				else
					m_owner.SelectedIndex = m_owner.HighlightedIndex;
			}
		}

		protected void HighlightFromMouse(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// If we don't have any items, we certainly can't highlight them!
			if (m_owner.Items.Count == 0)
				return;
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y,
				new SIL.Utils.Rect(rcSrcRoot.Left, rcSrcRoot.Top,
				rcSrcRoot.Right, rcSrcRoot.Bottom),
				new SIL.Utils.Rect(rcDstRoot.Left, rcDstRoot.Top,
				rcDstRoot.Right, rcDstRoot.Bottom),
				false);
			if (sel == null)
				return; // or set selected index to -1?
			int index;
			int hvo, tag, prev; // dummies.
			IVwPropertyStore vps; // dummy
			// Level 0 would give info about ktagText and the hvo of the dummy line object.
			// Level 1 gives info about which line object it is in the root.
			sel.PropInfo(false, 1, out hvo, out tag, out index, out prev, out vps);
			Debug.Assert(index < m_owner.Items.Count && index >= 0);
			// We are getting an out-of-bounds crash in setting HighlightedIndex at times,
			// for no apparent reason (after fixing the display bug of FWNX-803).
			if (index >= 0 && index < m_owner.Items.Count)
				m_owner.HighlightedIndex = index;
		}

		/// <summary>
		/// While tracking, we move the highlight as the mouse moves.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove (e);
			if (!m_owner.Tracking)
				return;
			using(new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Point pt = new Point(e.X, e.Y);
				HighlightFromMouse(PixelToView(pt), rcSrcRoot, rcDstRoot);
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!Char.IsControl(e.KeyChar))
			{
				if (m_owner is ComboListBox)
				{
					// Highlight list item based upon first character
					(m_owner as ComboListBox).HighlightItemStartingWith(e.KeyChar.ToString()); // closes the box
					e.Handled = true;
				}
			}
			else if ((int)e.KeyChar == '\r' || (int)e.KeyChar == '\t')
			{
				// If we're in a ComboBox, we must handle the ENTER key here, otherwise
				// SimpleRootSite may handle it inadvertently forcing the parent dialog to close (cf. LT-2280).
				HandleListItemSelect();

				if(e.KeyChar == '\r')
					e.Handled = true;
			}

			base.OnKeyPress(e);
		}

		private void HandleListItemSelect()
		{
			if (m_owner.HighlightedIndex >= 0)
			{
				if (m_owner.SelectedIndex == m_owner.HighlightedIndex)
					m_owner.RaiseSameItemSelected();
				else
					m_owner.SelectedIndex = m_owner.HighlightedIndex;
			}
			// if the user didn't highlight an item, treat this as we would selecting
			// the same item we did before.
			m_owner.RaiseSameItemSelected();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown (e);
			switch (e.KeyCode)
			{
				case Keys.Right:
				case Keys.Down:
				{
					// Handle Alt-Down
					if (e.Alt && e.KeyCode == Keys.Down && m_owner is ComboListBox)
					{
						HandleListItemSelect();
						e.Handled = true;
					}
					else
					{
						// If we don't have any items, we certainly can't highlight them!
						if (m_owner.Items.Count == 0)
							return;
						// don't increment if already at the end
						if (m_owner.HighlightedIndex < m_owner.Items.Count-1)
							m_owner.HighlightedIndex += 1;
					}
					break;
				}
				case Keys.Left:
				case Keys.Up:
				{
					// Handle Alt-Up
					if (e.Alt && e.KeyCode == Keys.Up && m_owner is ComboListBox)
					{
						HandleListItemSelect();
						e.Handled = true;
					}
					else
					{
						// If we don't have any items, we certainly can't highlight them!
						if (m_owner.Items.Count == 0)
							return;

						// don't scroll up past first item
						if (m_owner.HighlightedIndex > 0 )
							m_owner.HighlightedIndex -= 1;
						else if (m_owner.HighlightedIndex < 0)
							m_owner.HighlightedIndex = 0;	// reset to first item.
					}
					break;
				}
				default:
					break;
			}
		}

		public bool IsHighlighted(int index)
		{
			return Owner.IsHighlighted(index);
		}
	}

	internal class ListBoxVc : FwBaseVc
	{
		protected IFwListBoxSite m_listbox;
		protected ITsString m_tssBlanks;

		/// <summary>
		/// Construct one. Must be part of an InnerFwListBox.
		/// </summary>
		/// <param name="listbox"></param>
		internal ListBoxVc(IFwListBoxSite listbox)
		{
			m_listbox = listbox;
			UpdateBlankString(listbox);
		}

		public void UpdateBlankString(IFwListBoxSite listbox)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tssBlanks = tsf.MakeString (new string(' ', 200), m_listbox.WritingSystemCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main method just displays the text with the appropriate properties.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// <param name="hvo">The HVo of the object to display</param>
		/// <param name="frag">The fragment to lay out</param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case InnerFwListBox.kfragRoot:
					Font f = m_listbox.Font;
					if (m_listbox.StyleSheet == null)
					{
						// Try to get items a reasonable size based on whatever font has been set for the
						// combo as a whole. We don't want to do this if a stylesheet has been set, because
						// it will override the sizes specified in the stylesheet.
						// Enhance JohnT: there are several more properties we could readily copy over
						// from the font, but this is a good start.
						vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
							(int)FwTextPropVar.ktpvMilliPoint, (int)(f.SizeInPoints * 1000));
					}
					// Setting the font family here appears to override the fonts associated with the
					// TsString data.  This causes trouble for non-Microsoft Sans Serif writing systems.
					// See LT-551 for the bug report that revealed this problem.
					//				vwenv.set_StringProperty((int) FwTextPropType.ktptFontFamily,
					//					f.FontFamily.Name);
					vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
						(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(m_listbox.ForeColor));
					DisplayList(vwenv);
					break;
				case InnerFwListBox.kfragItems:
					int index, hvoDummy, tagDummy;
					int clev = vwenv.EmbeddingLevel;
					vwenv.GetOuterObject(clev - 1, out hvoDummy, out tagDummy, out index);
					bool fHighlighted = m_listbox.IsHighlighted(index);
					if (fHighlighted && m_listbox.ShowHighlight)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
							(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.HighlightText)));
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
							(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Highlight)));
					}
					vwenv.OpenParagraph();
					var tss = vwenv.DataAccess.get_StringProp(hvo, InnerFwListBox.ktagText);
					if (fHighlighted && m_listbox.ShowHighlight)
					{
						// Insert a string that has the foreground color not set, so the foreground color set above can take effect.
						ITsStrBldr bldr = tss.GetBldr();
						bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptForeColor, -1, -1);
						vwenv.AddString(bldr.GetString());
					}
					else
					{
						// Use the same Add method on both branches of the if.  Otherwise, wierd
						// results can happen on the display.  (See FWNX-803, which also affects
						// the Windows build, not just the Linux build!)
						vwenv.AddString(tss);
					}
					vwenv.AddString(m_tssBlanks);
					vwenv.CloseParagraph();
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the list of items in the list box.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayList(IVwEnv vwenv)
		{
			vwenv.OpenDiv();
			vwenv.AddObjVecItems((int)InnerFwListBox.ktagItems, this,
				(int)InnerFwListBox.kfragItems);
			vwenv.CloseDiv();
		}
	}
}


// Enhance:
// 1. Support multiple selections.
// 2. Support property for retrieving TsString from object.
// 3. Support stuff for adjusting size to whole number of items.
// 4. Support forcing fixed-height items.
// 5. Handle long lists using laziness.
// 6. Support subclass for checked items.
// 7. Support icons associated with items.
// 8. Visual feedback as mouse hovers.
// 9. Support selection by typing (do it right!)
