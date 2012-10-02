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
using XCore;
using SIL.FieldWorks.Filters;
using SIL.CoreImpl;
using System.Collections;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	///
	/// </summary>
	public struct SearchField
	{
		private readonly int m_flid;
		private readonly ITsString m_tss;

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchField"/> struct.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="tss">The string.</param>
		public SearchField(int flid, ITsString tss)
		{
			m_flid = flid;
			m_tss = tss;
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The flid.</value>
		public int Flid
		{
			get
			{
				return m_flid;
			}
		}

		/// <summary>
		/// Gets the string.
		/// </summary>
		/// <value>The string.</value>
		public ITsString String
		{
			get
			{
				return m_tss;
			}
		}
	}

	/// <summary>
	///
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

		#endregion Events

		#region Data members

		private const int ListFlid = ObjectListPublisher.MinFakeFlid + 1111;

		private FdoCache m_cache;
		private IVwStylesheet m_stylesheet; // used to figure font heights.
		private Mediator m_mediator;

		private BrowseViewer m_bvMatches;
		private ObjectListPublisher m_listPublisher;

		private StringSearcher<ICmObject> m_searcher;

		private List<ICmObject> m_searchableObjs;
		private int m_curObjIndex;
		private ICmObject m_selObject;
		private Func<ICmObject, IEnumerable<SearchField>> m_stringSelector;

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
				m_searcher.Clear();
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

		#endregion Properties

		#region Public methods

		/// <summary>
		/// Initialize the control, creating the BrowseViewer among other things.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configNode">The config node.</param>
		/// <param name="objs">The searchable objects.</param>
		/// <param name="type">The match type.</param>
		/// <param name="stringSelector">The string selector.</param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, Mediator mediator, XmlNode configNode,
			IEnumerable<ICmObject> objs, SearchType type, Func<ICmObject, IEnumerable<SearchField>> stringSelector)
		{
			Initialize(cache, stylesheet, mediator, configNode, objs, type, stringSelector, null);
		}

		/// <summary>
		/// Initialize the control, creating the BrowseViewer among other things.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configNode">The config node.</param>
		/// <param name="objs">The searchable objects.</param>
		/// <param name="type">The match type.</param>
		/// <param name="stringSelector">A function which generates, for an object we could match, a collection of SearchFields
		/// which indicate the string value and field that could be matched. It should NOT generate SearchFields with null
		/// or empty strings. (An earlier version of this class discarded them, but it is much more efficient never to make them.)</param>
		/// <param name="reversalWs">The reversal writing system.</param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, Mediator mediator, XmlNode configNode,
			IEnumerable<ICmObject> objs, SearchType type, Func<ICmObject, IEnumerable<SearchField>> stringSelector,
			IWritingSystem reversalWs)
		{
			CheckDisposed();

			m_cache = cache;
			m_stylesheet = stylesheet;
			m_mediator = mediator;
			m_searchableObjs = objs.ToList();
			m_stringSelector = stringSelector;
			m_searcher = new StringSearcher<ICmObject>(type, m_cache.ServiceLocator.WritingSystemManager);

			SuspendLayout();
			CreateBrowseViewer(configNode, reversalWs);
			ResumeLayout(false);
		}

		/// <summary>
		/// Searches the specified fields.
		/// </summary>
		/// <param name="fields">The fields.</param>
		/// <param name="filters">The filters.</param>
		public void Search(IEnumerable<SearchField> fields, IEnumerable<ICmObject> filters)
		{
			CreateSearchers();

			var results = new HashSet<ICmObject>();
			ITsString firstSearchStr = null;
			foreach (SearchField field in fields)
			{
				if (ShouldAbort())
					return;
				if (firstSearchStr == null)
					firstSearchStr = field.String;
				results.UnionWith(m_searcher.Search(field.Flid, field.String));
			}

			if (filters != null)
				results.ExceptWith(filters);

			if (ShouldAbort())
				return;
			// The following fixes LT-10293.
			RecordSorter sorter = null;
			if (firstSearchStr != null)
			{
				int ws = firstSearchStr.get_WritingSystemAt(0);
				bool isVern = m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(ws);
				sorter = m_bvMatches.CreateSorterForFirstColumn(isVern, ws);
			}
			if (sorter != null)
			{
				// Convert each ICmObject in results to a IManyOnePathSortItem, and sort
				// using the sorter.
				var records = new ArrayList(results.Count);
				foreach (ICmObject obj in results)
					records.Add(new ManyOnePathSortItem(obj));
				sorter.Sort(records);
				var hvos = new int[records.Count];
				for (int i = 0; i < records.Count; ++i)
					hvos[i] = (((IManyOnePathSortItem) records[i]).KeyObject);
				UpdateResults(hvos);
			}
			else
			{
				UpdateResults(results.Select(obj => obj.Hvo).ToArray());
			}
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

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset()
		{
			m_curObjIndex = 0;
			m_searcher.Clear();
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

		private void CreateSearchers()
		{
			int control = 0;
			// This is tricky and non-obvious. This loop does NOT initialize m_curObjIndex. Therefore unless something
			// adds to m_searchableObjs (not sure this can happen) or calls Reset() to set it back to zero, this routine
			// will do nothing except the first time it is called (after m_searchableObjs is set). I don't know exactly
			// why it was done that way, but that is why it is fast after the first time.
			for (; m_curObjIndex < m_searchableObjs.Count; m_curObjIndex++)
			{
				// Every so often see whether the user has typed something that makes our search irrelevant.
				if (control++ % 50 == 0 && ShouldAbort())
					return;

				foreach (SearchField field in m_stringSelector(m_searchableObjs[m_curObjIndex]))
					m_searcher.Add(m_searchableObjs[m_curObjIndex], field.Flid, field.String);
			}
		}

		private void CreateBrowseViewer(XmlNode configNode, IWritingSystem reversalWs)
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
			Controls.Add(m_bvMatches);
			m_bvMatches.ResumeLayout();
		}

		private void UpdateResults(int[] hvos)
		{
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

		/// <summary>
		/// Abort resetting if the user types anything, anywhere.
		/// Also sets the flag (if it returns true) to indicate the search WAS aborted.
		/// </summary>
		/// <returns></returns>
		private static bool ShouldAbort()
		{
#if !__MonoCS__
			var msg = new Win32.MSG();
			return Win32.PeekMessage(ref msg, IntPtr.Zero, (uint)Win32.WinMsgs.WM_KEYDOWN, (uint)Win32.WinMsgs.WM_KEYDOWN,
				(uint)Win32.PeekFlags.PM_NOREMOVE);
#else
			// ShouldAbort seems to be used for optimization purposes so returing false
			// just loses the optimization.
			return false;
#endif
		}

		#endregion Other methods
	}
}
