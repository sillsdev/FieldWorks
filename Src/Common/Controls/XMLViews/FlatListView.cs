// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FlatListView : UserControl
	{
		/// <summary>
		/// This event notifies you that the selected object changed, passing an argument from which you can
		/// directly obtain the new object. This SelectionChangedEvent will not fire if the selection moves
		/// from one occurrene of an object to another occurrence of the same object.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChanged;

		#region Data members

		private const int ObjectListFlid = 89999943;

		private LcmCache m_cache;
		private IVwStylesheet m_stylesheet; // used to figure font heights.
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private XmlNode m_configNode;
		private BrowseViewer m_bvList;
		private ObjectListPublisher m_listPublisher;
		IEnumerable<ICmObject> m_objs;
		RecordFilter m_filter;

		#endregion Data members

		#region Construction and Initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FlatListView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FlatListView()
		{
			InitializeComponent();
			// Everything interesting happens in Initialize().
		}

		/// <summary>
		/// Create and initialize the browse view, storing the data it will display.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// <param name="xnConfig">The config node.</param>
		/// <param name="objs">The objs.</param>
		public void Initialize(LcmCache cache, IVwStylesheet stylesheet, Mediator mediator, PropertyTable propertyTable,
			XmlNode xnConfig, IEnumerable<ICmObject> objs)
		{
			CheckDisposed();
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configNode = xnConfig;
			SuspendLayout();
			m_listPublisher = new ObjectListPublisher(cache.DomainDataByFlid as ISilDataAccessManaged, ObjectListFlid);

			StoreData(objs);
			m_objs = objs;
			m_bvList = new BrowseViewer(m_configNode, m_cache.LanguageProject.Hvo, ObjectListFlid, m_cache, m_mediator, m_propertyTable,
				null, m_listPublisher);
			m_bvList.Location = new Point(0, 0);
			m_bvList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom |
				AnchorStyles.Right;
			m_bvList.Name = "m_bvList";
			m_bvList.Sorter = null;
			m_bvList.TabStop = true;
			m_bvList.StyleSheet = m_stylesheet;
			m_bvList.Dock = DockStyle.Fill;
			m_bvList.SelectionChanged += m_bvList_SelectionChanged;
			m_bvList.SortersCompatible += m_bvList_AreSortersCompatible;
			m_bvList.SorterChanged += m_bvList_SorterChanged;
			m_bvList.FilterChanged += m_bvList_FilterChanged;
			Controls.Add(m_bvList);
			ResumeLayout(false);
		}

		private bool m_bvList_AreSortersCompatible(RecordSorter first, RecordSorter second)
		{
			return first.CompatibleSorter(second);
		}

		private void m_bvList_SorterChanged(object sender, EventArgs args)
		{
			using (new WaitCursor(this))
			{
				// Sort m_objs based on the sorter.
				ArrayList itemList = new ArrayList((from obj in m_objs select new ManyOnePathSortItem(obj.Hvo, null, null)).ToArray());
				m_bvList.Sorter.Sort(itemList);
				m_objs = (from ManyOnePathSortItem item in itemList select GetObject(item.RootObjectHvo)).ToList();
				// Store the filtered data.
				StoreData(GetFilteredObjects());
			}
		}

		/// <summary>
		/// Get the object for the given hvo.
		/// </summary>
		private ICmObject GetObject(int hvo)
		{
			foreach (var obj in m_objs)
			{
				if (obj.Hvo == hvo)
				{
					return obj;
				}
			}
			return null;
		}

		private void m_bvList_FilterChanged(object sender, Filters.FilterChangeEventArgs args)
		{
			// Update the filter.
			if (m_filter == null)
			{
				// Had no filter to begin with
				Debug.Assert(args.Removed == null);
				m_filter = args.Added is NullFilter ? null : args.Added;
			}
			else if (m_filter.SameFilter(args.Removed))
			{
				// Simplest case: we had just one filter, the one being removed.
				// Change filter to whatever (if anything) replaces it.
				m_filter = args.Added is NullFilter ? null : args.Added;
			}
			else if (m_filter is AndFilter)
			{
				AndFilter af = m_filter as AndFilter;
				if (args.Removed != null)
				{
					af.Remove(args.Removed);
				}
				if (args.Added != null)
				{
					//When the user chooses "all records/no filter", the RecordClerk will remove
					//its previous filter and add a NullFilter. In that case, we don't really need to add
					//	that filter. Instead, we can just add nothing.
					if (!(args.Added is NullFilter))
						af.Add(args.Added);
				}
				// Remove AndFilter if we get down to one.
				// This is not just an optimization, it allows the last filter to be removed
				// leaving empty, so the status bar can show that there is then no filter.
				if (af.Filters.Count == 1)
					m_filter = af.Filters[0] as RecordFilter;
			}
			else
			{
				// m_filter is not an AndFilter, so can't contain the one we're removing, nor IS it the one
				// we're removing...so we have no way to remove, and it's an error if we're trying to.
				Debug.Assert(args.Removed == null || args.Removed is NullFilter);
				if (args.Added != null && !(args.Added is NullFilter)) // presumably true or nothing changed, but for paranoia..
				{
					// We already checked for m_filter being null, so we now have two filters,
					// and need to make an AndFilter.
					AndFilter addFilter = new AndFilter();
					addFilter.Add(m_filter);
					addFilter.Add(args.Added);
					m_filter = addFilter;
				}
			}
			// Store the filtered data.
			StoreData(GetFilteredObjects());
		}

		/// <summary>
		/// Get the filtered objects from m_objs.
		/// </summary>
		private IEnumerable<ICmObject> GetFilteredObjects()
		{
			if (m_filter == null)
			{
				return m_objs;
			}
			ArrayList itemList = new ArrayList((from obj in m_objs select new ManyOnePathSortItem(obj.Hvo, null, null)).ToArray());
			IList<ICmObject> objList = new List<ICmObject>();
			foreach (ManyOnePathSortItem item in itemList)
			{
				if (m_filter.Accept(item))
				{
					objList.Add(GetObject(item.RootObjectHvo));
				}
			}
			return objList;
		}

		/// <summary>
		/// Store the given hvos in the cache as a fake vector property belonging to the
		/// language project.
		/// </summary>
		/// <param name="objs">The objs.</param>
		private void StoreData(IEnumerable<ICmObject> objs)
		{
			var rghvo = (from obj in objs
						 select obj.Hvo).ToArray();
			m_listPublisher.CacheVecProp(m_cache.LanguageProject.Hvo, rghvo, true);
		}
		#endregion

		#region IDisposable Members

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
				throw new ObjectDisposedException(
					String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion

		#region Other methods

		private void m_bvList_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged(this, e);
		}

		/// <summary>
		/// Set the initial list of checked items.
		/// </summary>
		/// <param name="objs">The objs.</param>
		public void SetCheckedItems(IEnumerable<ICmObject> objs)
		{
			CheckDisposed();

			var rghvo = (from obj in objs
						 select obj.Hvo).ToArray();
			m_bvList.SetCheckedItems(rghvo);
		}

		/// <summary>
		/// Retrieve the final list of checked items.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ICmObject> GetCheckedItems()
		{
			CheckDisposed();
			return from hvo in m_bvList.CheckedItems
				   select m_cache.ServiceLocator.GetObject(hvo);
		}

		/// <summary>
		/// Retrieve the index of the selected row in the browse view.
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				return m_bvList.SelectedIndex;
			}

			set
			{
				m_bvList.SelectedIndex = value;
			}
		}
		#endregion
	}
}
