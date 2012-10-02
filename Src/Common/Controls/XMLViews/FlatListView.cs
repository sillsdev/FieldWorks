using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FlatListView : UserControl, IFWDisposable
	{
		#region Data members

		private FdoCache m_cache;
		private IVwStylesheet m_stylesheet; // used to figure font heights.
		private Mediator m_mediator;
		private XmlNode m_configNode;
		private int m_flidFake;
		private BrowseViewer m_bvList;

		#endregion Data members

		#region Construction and Initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FlatListView"/> class.
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
		/// <param name="cache"></param>
		/// <param name="stylesheet"></param>
		/// <param name="mediator"></param>
		/// <param name="xnConfig"></param>
		/// <param name="rghvo"></param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, Mediator mediator,
			XmlNode xnConfig, List<int> rghvo)
		{
			CheckDisposed();
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_mediator = mediator;
			m_configNode = xnConfig;
			this.SuspendLayout();
			m_flidFake = FdoCache.DummyFlid;
			StoreData(rghvo);
			m_bvList = new SIL.FieldWorks.Common.Controls.BrowseViewer(m_configNode,
				m_cache.LangProject.Hvo, m_flidFake, m_cache, m_mediator, null);
			m_bvList.Location = new Point(0, 0);
			m_bvList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom |
				AnchorStyles.Right;
			m_bvList.Name = "m_bv";
			m_bvList.Sorter = null;
			m_bvList.TabStop = true;
			m_bvList.StyleSheet = m_stylesheet;
			m_bvList.Dock = DockStyle.Fill;
			this.Controls.Add(m_bvList);
			this.ResumeLayout(false);
		}

		/// <summary>
		/// Store the given hvos in the cache as a fake vector property belonging to the
		/// language project.
		/// </summary>
		/// <param name="rghvo"></param>
		private void StoreData(List<int> rghvo)
		{
			IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
			cda.CacheVecProp(m_cache.LangProject.Hvo, m_flidFake, rghvo.ToArray(),
				rghvo.Count);
		}
		#endregion

		#region IFWDisposable Members

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

		/// <summary>
		/// Set the initial list of checked items.
		/// </summary>
		/// <param name="rghvo"></param>
		public void SetCheckedItems(List<int> rghvo)
		{
			CheckDisposed();
			m_bvList.SetCheckedItems(rghvo);
		}

		/// <summary>
		/// Retrieve the final list of checked items.
		/// </summary>
		/// <returns></returns>
		public List<int> GetCheckedItems()
		{
			CheckDisposed();
			return m_bvList.CheckedItems;
		}

		/// <summary>
		/// Retrieve the index of the selected row in the browse view.
		/// </summary>
		public int SelectedIndex
		{
			get { return m_bvList.SelectedIndex; }
		}
		#endregion
	}
}
