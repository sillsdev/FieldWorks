// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
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
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;
		private XElement m_configNode;
		private BrowseViewer m_bvList;
		private ObjectListPublisher m_listPublisher;

		#endregion Data members

		#region Construction and Initialization

		/// <summary>
		/// Initializes a new instance of the <see cref="FlatListView"/> class.
		/// </summary>
		public FlatListView()
		{
			InitializeComponent();
			// Everything interesting happens in Initialize().
		}

		/// <summary>
		/// Create and initialize the browse view, storing the data it will display.
		/// </summary>
		public void Initialize(LcmCache cache, IVwStylesheet stylesheet, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, XElement xnConfig, IEnumerable<ICmObject> objs)
		{
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_subscriber = subscriber;
			m_configNode = xnConfig;
			SuspendLayout();
			m_listPublisher = new ObjectListPublisher(cache.DomainDataByFlid as ISilDataAccessManaged, ObjectListFlid);

			StoreData(objs);
			m_bvList = new BrowseViewer(m_configNode, m_cache.LanguageProject.Hvo, m_cache, null, m_listPublisher)
			{
				Location = new Point(0, 0),
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
				Name = "m_bvList",
				Sorter = null,
				TabStop = true,
				Dock = DockStyle.Fill
			};
			m_bvList.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			m_bvList.StyleSheet = m_stylesheet;
			m_bvList.FinishInitialization(m_cache.LanguageProject.Hvo, ObjectListFlid);
			m_bvList.SelectionChanged += m_bvList_SelectionChanged;
			Controls.Add(m_bvList);
			ResumeLayout(false);
		}

		/// <summary>
		/// Store the given hvos in the cache as a fake vector property belonging to the
		/// language project.
		/// </summary>
		private void StoreData(IEnumerable<ICmObject> objs)
		{
			m_listPublisher.CacheVecProp(m_cache.LanguageProject.Hvo, objs.Select(obj => obj.Hvo).ToArray());
		}
		#endregion

		#region Other methods

		private void m_bvList_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			SelectionChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Set the initial list of checked items.
		/// </summary>
		public void SetCheckedItems(IEnumerable<ICmObject> objs)
		{
			m_bvList.SetCheckedItems(objs.Select(obj => obj.Hvo).ToArray());
		}

		/// <summary>
		/// Retrieve the final list of checked items.
		/// </summary>
		public IEnumerable<ICmObject> GetCheckedItems()
		{
			return m_bvList.CheckedItems.Select(hvo => m_cache.ServiceLocator.GetObject(hvo));
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