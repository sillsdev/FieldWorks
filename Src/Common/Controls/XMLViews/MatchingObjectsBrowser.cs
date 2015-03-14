// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using SIL.Xml;
using XCore;
using SIL.FieldWorks.Filters;
using SIL.CoreImpl;
using System.Collections;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A browse view that displays the results of a search.
	/// </summary>
	public class MatchingObjectsBrowser : UserControl, IFWDisposable
	{
		#region Events

		/// <summary>
		/// Declare an event to deal with selection changed.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChanged;

		/// <summary>
		/// Occurs when a specific object is selected with a double-click or enter key.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionMade;

		/// <summary>
		/// Occurs when the search has completed.
		/// </summary>
		public event EventHandler SearchCompleted;

		/// <summary>
		/// Occurs when the underlying BrowseViewer's columns have changed.
		/// </summary>
		public event EventHandler ColumnsChanged;

		#endregion Events

		#region Data members

		private const int ListFlid = ObjectListPublisher.MinFakeFlid + 1111;

		private FdoCache m_cache;
		private IVwStylesheet m_stylesheet; // used to figure font heights.
		private Mediator m_mediator;

		private BrowseViewer m_bvMatches;
		private ObjectListPublisher m_listPublisher;

		private SearchEngine m_searchEngine;

		private ICmObject m_selObject;

		private ICmObject m_startingObject;

		private string[] m_visibleColumns;

		#endregion Data members

		#region Disposal methods

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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				m_searchEngine.SearchCompleted -= m_searchEngine_SearchCompleted;
			}
			m_cache = null;

			base.Dispose(disposing);
		}

		#endregion Disposal methods

		#region Properties

		/// <summary>
		/// Gets the selected object.
		/// </summary>
		/// <value>The selected object.</value>
		public ICmObject SelectedObject
		{
			get
			{
				return m_selObject;
			}
		}

		/// <summary>
		/// Gets or sets the starting object.
		/// </summary>
		public ICmObject StartingObject
		{
			get
			{
				CheckDisposed();
				return m_startingObject;
			}

			set
			{
				CheckDisposed();
				m_startingObject = value;
			}
		}

		/// <summary>
		/// Used by a Find dialog's SearchEngine to determine whether to search on a particular field or not
		/// </summary>
		public bool IsVisibleColumn(string keyString)
		{
			CheckDisposed();

			return m_visibleColumns.Any(columnLayoutName => columnLayoutName.Contains(keyString));
		}

		#endregion Properties

		#region Public methods
		/// <summary>
		/// Initialize the control, creating the BrowseViewer among other things.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configNode">The config node.</param>
		/// <param name="searchEngine">The search engine.</param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, Mediator mediator, XmlNode configNode,
			SearchEngine searchEngine)
		{
			Initialize(cache, stylesheet, mediator, configNode, searchEngine, null);
		}

		/// <summary>
		/// Initialize the control, creating the BrowseViewer among other things.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configNode">The config node.</param>
		/// <param name="searchEngine">The search engine.</param>
		/// <param name="reversalWs">The reversal writing system.</param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, Mediator mediator, XmlNode configNode,
			SearchEngine searchEngine, CoreWritingSystemDefinition reversalWs)
		{
			CheckDisposed();

			m_cache = cache;
			m_stylesheet = stylesheet;
			m_mediator = mediator;
			m_searchEngine = searchEngine;
			m_searchEngine.SearchCompleted += m_searchEngine_SearchCompleted;

			SuspendLayout();
			CreateBrowseViewer(configNode, reversalWs);
			ResumeLayout(false);
		}

		private void m_searchEngine_SearchCompleted(object sender, SearchCompletedEventArgs e)
		{
			UpdateResults(e.Fields.FirstOrDefault(), e.Results);
			// On the completion of a new search set the selection to the first result without stealing the focus
			// from any other controls.
			if(m_bvMatches.BrowseView.IsHandleCreated) // hotfix paranoia test
			{
				var oldEnabledState = m_bvMatches.Enabled;
				m_bvMatches.Enabled = false;
				// disable the control before changing the selection so that the focus won't change
				m_bvMatches.SelectedIndex = m_bvMatches.AllItems.Count > 0 ? 0 : -1;
				m_bvMatches.Enabled = oldEnabledState; // restore the control to it's previous enabled state
			}
		}

		/// <summary>
		/// Searches the specified fields asynchronously.
		/// </summary>
		public void SearchAsync(IEnumerable<SearchField> fields)
		{
			// Start the search
			m_searchEngine.SearchAsync(fields);
		}

		/// <summary>
		/// respond to an up arrow key in the find select box
		/// </summary>
		public void SelectNext()
		{
			int i = m_bvMatches.SelectedIndex;
			if (i != -1 && i + 1 < m_bvMatches.AllItems.Count)
				m_bvMatches.SelectedIndex = i + 1;
		}

		/// <summary>
		/// respond to a down arrow key in the find select box
		/// </summary>
		public void SelectPrevious()
		{
			int i = m_bvMatches.SelectedIndex;
			if (i > 0)
				m_bvMatches.SelectedIndex = i - 1;
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// This comes from a single click on a row in the browse view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			m_selObject = m_cache.ServiceLocator.GetObject(e.Hvo);
			FireSelectionChanged();
		}

		/// <summary>
		/// This comes from a double click on a row in the browse view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_SelectionMade(object sender, FwObjectSelectionEventArgs e)
		{
			m_selObject = m_cache.ServiceLocator.GetObject(e.Hvo);
			FireSelectionMade();
		}

		/// <summary>
		/// This comes from modifying the browse view columns
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_ColumnsChanged(object sender, EventArgs e)
		{
			if (e is ColumnWidthChangedEventArgs)
				return; // don't want to know about this kind

			UpdateVisibleColumns();
			if (ColumnsChanged != null)
				// Find dialogs can subscribe to this to know when to check for new search fields
				ColumnsChanged(this, new EventArgs());
		}

#if __MonoCS__
		private bool m_recursionProtection = false; // FWNX-262
#endif

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Enter"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnEnter(EventArgs e)
		{
#if __MonoCS__
			if (m_recursionProtection) // FWNX-262
				return;
			m_recursionProtection = true;
#endif

			m_bvMatches.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.border;
			base.OnEnter(e);
			m_bvMatches.Select();

#if __MonoCS__
			m_recursionProtection = false;
#endif
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Leave"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			m_bvMatches.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.all;
		}

		#endregion Event Handlers

		#region Other methods

		private void CreateBrowseViewer(XmlNode configNode, CoreWritingSystemDefinition reversalWs)
		{
			m_listPublisher = new ObjectListPublisher(m_cache.DomainDataByFlid as ISilDataAccessManaged, ListFlid);
			m_bvMatches = new BrowseViewer(configNode, m_cache.LanguageProject.LexDbOA.Hvo, ListFlid, m_cache, m_mediator,
				null, m_listPublisher);
			m_bvMatches.SuspendLayout();
			m_bvMatches.Location = new Point(0, 0);
			m_bvMatches.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
			m_bvMatches.Name = "m_bvMatches";
			m_bvMatches.Sorter = null;
			m_bvMatches.TabStop = false;
			m_bvMatches.StyleSheet = m_stylesheet;
			m_bvMatches.Dock = DockStyle.Fill;
			if (reversalWs != null)
				m_bvMatches.BrowseView.Vc.ReversalWs = reversalWs.Handle;
			m_bvMatches.SelectionChanged += m_bvMatches_SelectionChanged;
			m_bvMatches.SelectionMade += m_bvMatches_SelectionMade;
			UpdateVisibleColumns();
			Controls.Add(m_bvMatches);
			m_bvMatches.ResumeLayout();
			m_bvMatches.ColumnsChanged += m_bvMatches_ColumnsChanged;
		}

		private void UpdateVisibleColumns()
		{
			var results = new List<string>();
			foreach (var columnSpec in m_bvMatches.ColumnSpecs)
			{
				var colLabel = columnSpec.GetOptionalStringAttribute("layout", null);
				if (colLabel == null)
				{
					// In this case we are likely dealing with a dialog that does NOT use IsVisibleColumn()
					// and there will be one pre-determined SearchField
					continue;
				}
				results.Add(colLabel);
			}
			m_visibleColumns = results.ToArray();
		}

		private void UpdateResults(SearchField firstField, IEnumerable<int> results)
		{
			ITsString firstSearchStr = firstField.String;
			// if the firstSearchStr is null we can't get its writing system
			RecordSorter sorter = null;
			if (firstSearchStr != null)
			{
				int ws = firstSearchStr.get_WritingSystemAt(0);
				sorter = CreateFindResultSorter(firstSearchStr, ws);
			}
			int[] hvos;
			if (sorter != null)
			{
				// Convert each ICmObject in results to a IManyOnePathSortItem, and sort
				// using the sorter.
				var records = new ArrayList();
				foreach (int hvo in results.Where(hvo => StartingObject == null || StartingObject.Hvo != hvo))
					records.Add(new ManyOnePathSortItem(hvo, null, null));
				sorter.Sort(records);
				hvos = records.Cast<IManyOnePathSortItem>().Select(i => i.KeyObject).ToArray();
			}
			else
			{
				hvos = results.Where(hvo => StartingObject == null || StartingObject.Hvo != hvo).ToArray();
			}

			int count = hvos.Length;
			int prevIndex = m_bvMatches.SelectedIndex;
			int prevHvo = prevIndex == -1 ? 0 : m_bvMatches.AllItems[prevIndex];
			m_listPublisher.CacheVecProp(m_cache.LanguageProject.LexDbOA.Hvo, hvos);
			TabStop = count > 0;
			// Disable the list so that it doesn't steal the focus (LT-9481)
			m_bvMatches.Enabled = false;
			try
			{
				// LT-6366
				if (count == 0)
				{
					if (m_bvMatches.BrowseView.IsHandleCreated)
						m_bvMatches.SelectedIndex = -1;
					m_selObject = null;
				}
				else
				{
					int newIndex = 0;
					var allItems = m_bvMatches.AllItems; // This is an important optimization; each call marshals the whole list!
					for (int i = 0; i < allItems.Count; i++)
					{
						if (allItems[i] == prevHvo)
						{
							newIndex = i;
							break;
						}
					}
					if (m_bvMatches.BrowseView.IsHandleCreated)
						m_bvMatches.SelectedIndex = newIndex;
					m_selObject = m_cache.ServiceLocator.GetObject(allItems[newIndex]);
					FireSelectionChanged();
				}
			}
			finally
			{
				m_bvMatches.Enabled = true;
			}

			if (!m_searchEngine.IsBusy && SearchCompleted != null)
				SearchCompleted(this, new EventArgs());
		}

		private FindResultSorter CreateFindResultSorter(ITsString firstSearchStr, int ws)
		{
			var browseViewSorter = m_bvMatches.CreateSorterForFirstColumn(ws);

			return browseViewSorter == null ? null: new FindResultSorter(firstSearchStr, browseViewSorter);
		}

		private void FireSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, new FwObjectSelectionEventArgs(m_selObject.Hvo));
		}

		private void FireSelectionMade()
		{
			if (SelectionMade != null)
				SelectionMade(this, new FwObjectSelectionEventArgs(m_selObject.Hvo));
		}

		#endregion Other methods
	}
}
