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
				ArrayList itemList = new ArrayList((from obj in m_objs select new ManyOnePathSortItem(obj.Hvo, null, null)).ToArray());
				m_bvList.Sorter.Sort(itemList);
				IList<ICmObject> objList = (from ManyOnePathSortItem item in itemList select GetObject(item.RootObjectHvo)).ToList();
				StoreData(objList);
			}
		}

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
