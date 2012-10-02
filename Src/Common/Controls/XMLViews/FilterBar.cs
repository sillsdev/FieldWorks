using System;
using System.Windows.Forms;
using System.Xml;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using XCore;
using System.Reflection;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class will be used in both sorting and filtering. From a viewSpec node is derived
	/// a IStringFinder that can find one or several strings that display in the column for a
	/// particular object. Using that and the list of records we build a combo box displaying
	/// options for filtering this column. From what is typed and selected in the combo,
	/// we may construct a Matcher, which combines with the string finder to make a
	/// FilterBarCellFilter. Eventually we will use the string finder also if sorting by this
	/// column.</summary>
	///
	/// <remarks>
	/// Todo: for reasonable efficiency, need a way to preload the information needed to
	/// evaluate filter for all items. This might be a method on RecordFilter.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class FilterSortItem : IFWDisposable
	{
		XmlNode m_viewSpec;
		IStringFinder m_finder;
		FwComboBox m_combo;
		IMatcher m_matcher;
		RecordFilter m_filter;
		RecordSorter m_sorter;

		/// <summary></summary>
		public event FilterChangeHandler FilterChanged;

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
		~FilterSortItem()
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				// We didn't make these either, but we need to deal with them.
				// No. These belong to the RecordList.
				//if (m_finder != null && (m_finder is IDisposable))
				//	(m_finder as IDisposable).Dispose();
				//if (m_sorter != null && (m_sorter is IDisposable))
				//	(m_sorter as IDisposable).Dispose();
				//if (m_filter != null)
				//	(m_filter as IDisposable).Dispose();
				//if (m_matcher != null && m_matcher is IDisposable)
				//	(m_matcher as IDisposable).Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_viewSpec = null;
			m_combo = null; // We didn't create it, so let whoever did dispose it.
			m_finder = null;
			m_sorter = null;
			m_filter = null;
			m_matcher = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the spec.
		/// </summary>
		/// <value>The spec.</value>
		/// ------------------------------------------------------------------------------------
		public XmlNode Spec
		{
			get
			{
				CheckDisposed();
				return m_viewSpec;
			}
			set
			{
				CheckDisposed();
				m_viewSpec = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the finder.
		/// </summary>
		/// <value>The finder.</value>
		/// ------------------------------------------------------------------------------------
		public IStringFinder Finder
		{
			get
			{
				CheckDisposed();
				return m_finder;
			}
			set
			{
				CheckDisposed();
				m_finder = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the combo.
		/// </summary>
		/// <value>The combo.</value>
		/// ------------------------------------------------------------------------------------
		public FwComboBox Combo
		{
			get
			{
				CheckDisposed();
				return m_combo;
			}
			set
			{
				CheckDisposed();
				m_combo = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the matcher.
		/// </summary>
		/// <value>The matcher.</value>
		/// ------------------------------------------------------------------------------------
		public IMatcher Matcher
		{
			get
			{
				CheckDisposed();
				return m_matcher;
			}
			set
			{
				CheckDisposed();

				m_matcher = value;
				if (m_matcher != null && m_matcher.WritingSystemFactory == null && m_combo != null)
					m_matcher.WritingSystemFactory = m_combo.WritingSystemFactory;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the sorter.
		/// </summary>
		/// <value>The sorter.</value>
		/// ------------------------------------------------------------------------------------
		public RecordSorter Sorter
		{
			get
			{
				CheckDisposed();
				return m_sorter;
			}
			set
			{
				CheckDisposed();
				m_sorter = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the filter.
		/// </summary>
		/// <value>The filter.</value>
		/// ------------------------------------------------------------------------------------
		public RecordFilter Filter
		{
			get
			{
				CheckDisposed();
				return m_filter;
			}
			set
			{
				CheckDisposed();

				RecordFilter old = m_filter;
				m_filter = value;
				// Change the filter if they are not the same
				if (FilterChanged != null &&
					(m_filter != null && !m_filter.SameFilter(old)) || (old != null && !old.SameFilter(m_filter)))
				{
					FilterChanged(this, new FilterChangeEventArgs(m_filter, old));
				}
			}
		}

		/// <summary>
		/// If this filter could have been created from this FSI, set it as your active
		/// filter and update your display accordingly, and answer true. Otherwise
		/// answer false.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public bool SetFromFilter(RecordFilter filter)
		{
			CheckDisposed();

			if (filter == m_filter)
				return true;  // we're already set.
			if (m_combo == null)
				return false; // probably can't happen, but play safe
			foreach (FilterComboItem fci in m_combo.Items)
			{
				if (fci == null)
					continue; // for safety, probably can't happen
				ITsString tssLabel = fci.SetFromFilter(filter, this);
				if (tssLabel == null)
					continue;
				m_combo.SelectedIndex = -1; // prevents failure of setting Tss if not in list.
				m_combo.Tss = tssLabel;
				m_filter = filter; // remember this filter is active!
				return true;
			}
			return false;
		}

		// Todo:
		// Add to FilterBar event for changing (add and/or remove) filter.
		// Add same to BrowseViewer (connect so forwards to from FilterBar if any)
		// Add to RecordClerk ability to add/remove filters and refresh list.
		// Configure RecordBrowseView to handle filter changes by updating record clerk.
	}

	/// <summary>
	/// A FilterBar contains a sequence of combos or grey areas, one for each column of a browse view.
	/// </summary>
	public class FilterBar : UserControl, IFWDisposable
	{
		BrowseViewer m_bv;
		List<XmlNode> m_columns;
		FilterSortItem[] m_items;
		IFwMetaDataCache m_mdc; // m_cache.MetaDataCacheAccessor
		FdoCache m_cache; // Use minimally, may want to factor out for non-db use.
		ISilDataAccess m_sda; // m_cache.MainCacheAccessor
		ILgWritingSystemFactory m_wsf;
		int m_userWs;

		int m_stdFontHeight; // Keep track of this font height for calculating FilterBar heights
		IVwStylesheet m_stylesheet;
		int m_colOffset; // 0 if no select column, 1 if there is.
		// True during UpdateActiveItems to suppress side-effects of setting text of combo.
		bool m_fInUpdateActive = false;
		/// <summary>
		/// This is invoked when the user sets up, removes, or changes the filter for a column.
		/// </summary>
		public event FilterChangeHandler FilterChanged;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilterBar"/> class.
		/// </summary>
		/// <param name="bv">The bv.</param>
		/// <param name="spec">The spec.</param>
		/// ------------------------------------------------------------------------------------
		public FilterBar(BrowseViewer bv, XmlNode spec)
		{
			m_bv = bv;
			m_columns = m_bv.ColumnSpecs;

			m_cache = bv.Cache;
			m_mdc = m_cache.MetaDataCacheAccessor;
			m_sda = m_cache.MainCacheAccessor;
			m_wsf = m_sda.WritingSystemFactory;
			m_userWs = m_cache.DefaultUserWs;

			// Store the standard font height for use in SetStyleSheet
			Font tempFont = new Font("Times New Roman", (float)10.0);
			m_stdFontHeight = tempFont.Height;

			// This light grey background shows through for any columns where we don't have a combo
			// because we can't figure a IStringFinder from the XmlParameters.
			this.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
			MakeItems();
			this.AccessibilityObject.Name = "FilterBar";
		}

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (Control ctl in Controls)
				{
					if (ctl is FwComboBox)
					{
						FwComboBox combo = ctl as FwComboBox;
						combo.SelectedIndexChanged -= new EventHandler(Combo_SelectedIndexChanged);
						foreach (Object obj in combo.Items)
						{
							if (obj is IDisposable)
								(obj as IDisposable).Dispose();
						}
						combo.Items.Clear();
					}
				}
				foreach (FilterSortItem fsi in m_items)
				{
					fsi.FilterChanged -= new FilterChangeHandler(this.FilterChangedHandler);
					fsi.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_bv = null; // Parent window.
			m_cache = null;
			m_columns = null; // Client needs to deal with the columns.
			m_items = null;
			m_mdc = null;
			m_sda = null;
			m_wsf = null;

			// This will handle any controls that are in the Controls property.
			base.Dispose(disposing);
		}

		/// <summary>
		/// An array of info about all columns (except the extra check box column, if present).
		/// </summary>
		internal FilterSortItem[] ColumnInfo
		{
			get
			{
				CheckDisposed();
				return m_items;
			}
		}

		// Offset to add to real column index to get corresponding index into ColumnInfo.
		// Current 1 if check boxes present, otherwise zero.
		internal int ColumnOffset
		{
			get
			{
				CheckDisposed();
				return m_colOffset;
			}
		}

		// User has changed list of columns. Rework everything.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the column list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateColumnList()
		{
			CheckDisposed();

			m_columns = m_bv.ColumnSpecs;
			this.SuspendLayout();
			foreach (FilterSortItem fsi in m_items)
			{
				if (fsi != null && fsi.Combo != null)
				{
					this.Controls.Remove(fsi.Combo);
					fsi.Combo.Dispose();	// Close internal rootbox!  (See LT-5345.)
					fsi.Combo = null;
				}
			}
			m_items = null;
			MakeItems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeItems()
		{
			CheckDisposed();

			if (m_items != null)
				return; // already made.
			if (m_bv.BrowseView == null || m_bv.BrowseView.Vc == null)
				return; // too soon.
			m_colOffset = m_bv.BrowseView.Vc.HasSelectColumn ? 1 : 0;
			m_items = new FilterSortItem[m_columns.Count];
			// Here we figure which columns we can filter on.
			int icol = -1; // will increment at start of loop.
			foreach (XmlNode colSpec in m_columns)
			{
				icol++;
				m_items[icol] = MakeItem(colSpec);
			}
		}

		/// <summary>
		/// Given the current record filter of the clerk, determine whether any of the active
		/// filters could have been created by any of your filter sort items, and if so,
		/// update the filter bar to show they are active.
		/// </summary>
		/// <param name="currentFilter"></param>
		public void UpdateActiveItems(RecordFilter currentFilter)
		{
			CheckDisposed();

			try
			{
				m_fInUpdateActive = true;
				if (currentFilter is AndFilter)
				{
					// We have to copy the list, because the internal operation of this loop
					// may change the original list, invalidating the (implicit) enumerator.
					// See LT-1133 for an example of what happens without making this copy.
					ArrayList filters = (ArrayList)(currentFilter as AndFilter).Filters.Clone();
					foreach (RecordFilter filter in filters)
					{
						ActivateCompatibleFilter(filter);
					}
				}
				else
				{
					// Try to activate the single filter itself.
					ActivateCompatibleFilter(currentFilter);
				}
			}
			finally
			{
				//Adjust the FilterBar height and combo box heights to accomodate the
				//strings in the filter comboBoxes
				AdjustBarHeights();
				m_fInUpdateActive = false;
			}
		}

		/// <summary>
		/// Adjust the FilterBar and Combo boxes to reflect writing system font sizes for the active filters.
		/// </summary>
		private void AdjustBarHeights()
		{
			CheckDisposed();

			int maxComboHeight = GetMaxComboHeight();
			SetBarHeight(maxComboHeight); // Set height of FilterBar and its ComboBoxes
		}

		/// <summary>
		/// Given a filter bar cell filter which is (part of) the active filter for your
		/// clerk, if one of your cells understands it install it as the active filter
		/// for that cell. Otherwise, remove it from the clerk filter.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns>true if compatible filter is found and activated, false if
		/// a match was not found and signal clerk to deactivate.</returns>
		private bool ActivateCompatibleFilter(RecordFilter filter)
		{
			foreach (FilterSortItem fsi in m_items)
			{
				if (fsi != null && fsi.SetFromFilter(filter))
					return true;
			}
			// we couldn't find a match in the active columns.
			// if we've already fully initialized the filters, then remove it from the clerk filter.
			if (FilterChanged != null)
				FilterChanged(this, new FilterChangeEventArgs(null, filter));
			return false;
		}

		/// <summary>
		/// Determine if the given filter (or subfilter) can be activated for the given
		/// column specification.
		/// </summary>
		/// <param name="filter">target filter</param>
		/// <param name="colSpec">column node spec </param>
		/// <returns>true, if the column node spec can use the filter.</returns>
		public bool CanActivateFilter(RecordFilter filter, XmlNode colSpec)
		{
			CheckDisposed();

			if (filter is AndFilter)
			{
				ArrayList filters = (ArrayList)(filter as AndFilter).Filters;
				foreach (RecordFilter rf in filters)
				{
					if ((rf is FilterBarCellFilter || rf is ListChoiceFilter) &&
						CanActivateFilter(rf, colSpec))
					{
						return true;
					}
				}
			}
			else if (filter is FilterBarCellFilter)
			{
				FilterBarCellFilter fbcf = filter as FilterBarCellFilter;
				IStringFinder colFinder = LayoutFinder.CreateFinder(m_cache, colSpec, m_bv.BrowseView.Vc);
				if (fbcf.Finder.SameFinder(colFinder))
					return true;
			}
			else if (filter is ListChoiceFilter)
			{
				return (filter as ListChoiceFilter).CompatibleFilter(colSpec);
			}
			return false;
		}

		/// <summary>
		/// Set the widths of the columns.
		/// </summary>
		/// <param name="widths"></param>
		public void SetColWidths(int[] widths)
		{
			CheckDisposed();

			// We can only do this meaningfully if given the right number of lengths.
			// If this is wrong (which for example can happen if this routine gets
			// called during UpdateColumnList of the browse view before UpdateColumnList
			// has been called on the filter bar), ignore it, and hope we get adjusted
			// again after everything has the right number of items.
			if (widths.Length - m_colOffset != m_items.Length)
				return;
			int x = 0;
			if (m_colOffset > 0)
			{
				x = widths[0];
			}
			// not sure how to get the correct value for this, but it looks like column headers
			// are offset by a small value, so we shift the filter bar to line up properly
			x += 2;
			for (int i = 0; i < widths.Length - m_colOffset; ++i)
			{
				if (m_items[i] != null)
				{
					m_items[i].Combo.Left = x;
					m_items[i].Combo.Width = widths[i + m_colOffset];
				}
				x += widths[i + m_colOffset];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a FilterSortItem for the column specified by the given viewSpec,
		/// if it follows a pattern we recognize. Otherwise, return null.
		/// If successful, the FSI is initialized with the viewSpec, a IStringFinder, and
		/// a combo.
		/// The child of the column node is like the contents of a fragment for displaying
		/// the item.
		/// Often the thing we really want is embedded inside something else.
		/// <para>
		/// 		<properties>
		/// 			<bold value="on"/>
		/// 		</properties>
		/// 		<stringalt class="LexEntry" field="CitationForm" ws="vernacular"/>
		/// 	</para>
		/// </summary>
		/// <param name="colSpec">The col spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected internal FilterSortItem MakeItem(XmlNode colSpec)
		{
			return MakeLayoutItem(colSpec);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a spec that might be some sort of element, or might be something wrapping a flow object
		/// around that element, return the element. Or, it might be a "frag" element wrapping all of that.
		/// </summary>
		/// <param name="viewSpec">The view spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		XmlNode ExtractFromFlow(XmlNode viewSpec)
		{
			if (viewSpec == null)
				return null;
			if (viewSpec.Name == "frag")
				viewSpec = viewSpec.FirstChild;
			if (viewSpec.Name == "para" || viewSpec.Name == "div")
			{
				if (viewSpec.ChildNodes.Count == 2 && viewSpec.FirstChild.Name == "properties")
					return viewSpec.ChildNodes[1];
				else if (viewSpec.ChildNodes.Count == 1)
					return viewSpec.FirstChild;
			}
			return viewSpec; // None of the special flow object cases, use the node itself.
		}

		string GetStringAtt(XmlNode node, string name)
		{
			XmlAttribute xa = node.Attributes[name];
			if (xa == null)
				return null;
			return xa.Value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a FilterSortItem with a finder that is a LayoutFinder with the specified layout name.
		/// </summary>
		/// <param name="colSpec">The col spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected FilterSortItem MakeLayoutItem(XmlNode colSpec)
		{
			FilterSortItem result = new FilterSortItem();
			result.Spec = colSpec; // SetupFsi uses this to get the writing system to use for the combo.
			result.Finder = LayoutFinder.CreateFinder(m_cache, colSpec, m_bv.BrowseView.Vc);
			SetupFsi(result);
			int ws = LangProject.GetWritingSystem(colSpec, m_cache, null, 0);
			if (ws == 0)
				ws = m_cache.DefaultVernWs;
			string sWs = m_wsf.GetStrFromWs(ws);
			result.Sorter = new GenRecordSorter(new StringFinderCompare(result.Finder,
				new IcuComparer(sWs)));
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a FilterSortItem with a Finder that is MultiIndirectMlPropFinder, where
		/// the first sequence flid is flidSeq1, and the rest are derived from childSpec,
		/// the contents of the "frag" n&lt;/frag&gt;
		/// </summary>
		/// <param name="viewSpec">The view spec.</param>
		/// <param name="flidSeq1">The flid seq1.</param>
		/// <param name="childSpec">The child spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected FilterSortItem MakeMultiIndirectItem(XmlNode viewSpec, int flidSeq1,
			XmlNode childSpec)
		{
			// currently we can only handle more levels of objseq.
			// enhance JohnT: could enhance MultiIndirectMlPropFinder so it takes information
			// about which props are sequences, then we could also handle <obj> elements.
			XmlNode subSpec = childSpec;
			List<int> flids = new List<int>();
			flids.Add(flidSeq1);
			while(subSpec != null && subSpec.Name == "objseq")
			{
				string className = GetStringAtt(subSpec, "class");
				string attrName = GetStringAtt(subSpec, "field");
				if (className == null || attrName == null)
					return null; // Can't interpret an incomplete objseq.
				int flidSeq = (int)m_mdc.GetFieldId(className, attrName, true);
				flids.Add(flidSeq);
				subSpec = ExtractFromFlow(m_bv.BrowseView.Vc.GetSubFrag(subSpec));
			}
			if (subSpec == null || subSpec.Name != "stringalt")
				return null; // leaf must be a stringalt.
			string classNameS = GetStringAtt(subSpec, "class");
			string attrNameS = GetStringAtt(subSpec, "field");
			int ws = LangProject.GetWritingSystem(subSpec, m_cache, null, 0);
			if (classNameS == null || attrNameS == null || ws == 0)
				return null; // Can't interpret an incomplete stringalt.

			int flid = (int)m_mdc.GetFieldId(classNameS, attrNameS, true);
			string sWs = m_wsf.GetStrFromWs(ws);
			if (sWs == null || sWs == "")
				return null;		// must have a valid writing system.

			FilterSortItem result = new FilterSortItem();
			result.Spec = viewSpec;
			result.Finder = new MultiIndirectMlPropFinder(m_sda, flids.ToArray(), flid, ws);
			SetupFsi(result);
			result.Sorter = new GenRecordSorter(new StringFinderCompare(result.Finder,
				new IcuComparer(sWs)));
			return result;

		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a FilterSortItem with a Finder that is an OneIndirectMlPropFinder based on
		/// saSpec, which is a stringalt element, and flidSeq, which is the sequence containing
		/// the named items.
		/// </summary>
		/// <param name="viewSpec">The view spec.</param>
		/// <param name="flidSeq">The flid seq.</param>
		/// <param name="saSpec">The sa spec.</param>
		/// <param name="fAtomic">if set to <c>true</c> [f atomic].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected FilterSortItem MakeOneIndirectItem(XmlNode viewSpec, int flidSeq,
			XmlNode saSpec, bool fAtomic)
		{
			string className = GetStringAtt(saSpec, "class");
			string attrName = GetStringAtt(saSpec, "field");
			int ws = LangProject.GetWritingSystem(saSpec, m_cache, null, 0);
			if (className == null || attrName == null || ws == 0)
				return null; // Can't interpret an incomplete stringalt.

			int flid = (int)m_mdc.GetFieldId(className, attrName, true);
			string sWs = m_wsf.GetStrFromWs(ws);
			if (sWs == null || sWs == "")
				return null;		// must have a valid writing system.

			FilterSortItem result = new FilterSortItem();
			result.Spec = viewSpec;
			if (fAtomic)
				result.Finder = new OneIndirectAtomMlPropFinder(m_sda, flidSeq, flid, ws);
			else
				result.Finder = new OneIndirectMlPropFinder(m_sda, flidSeq, flid, ws);
			SetupFsi(result);
			result.Sorter = new GenRecordSorter(new StringFinderCompare(result.Finder,
				new IcuComparer(sWs)));
			return result;
		}

		// Make a FilterSortItem with a Finder that is an OwnMlPropFinder based on saSpec,
		// which is a stringalt element.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the string alt item.
		/// </summary>
		/// <param name="viewSpec">The view spec.</param>
		/// <param name="saSpec">The sa spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected FilterSortItem MakeStringAltItem(XmlNode viewSpec, XmlNode saSpec)
		{
			string className = GetStringAtt(saSpec, "class");
			string attrName = GetStringAtt(saSpec, "field");
			int ws = LangProject.GetWritingSystem(saSpec, m_cache, null, 0);
			if (className == null || attrName == null || ws == 0)
				return null; // Can't interpret an incomplete stringalt.

			string sWs = m_wsf.GetStrFromWs(ws);
			if (sWs == null || sWs == "")
				return null;		// must have a valid writing system.
			int flid = (int)m_mdc.GetFieldId(className, attrName, true);

			FilterSortItem result = new FilterSortItem();
			result.Spec = viewSpec;
			result.Finder = new OwnMlPropFinder(m_sda, flid, ws);
			SetupFsi(result);
			result.Sorter = new GenRecordSorter(new StringFinderCompare(result.Finder,
				new IcuComparer(sWs)));
			return result;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a FilterSortItem with a Finder that is an OwnIntPropFinder based on intSpec,
		/// which is an &lt;int&gt; element..
		/// </summary>
		/// <param name="viewSpec">The view spec.</param>
		/// <param name="intSpec">The int spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected FilterSortItem MakeIntItem(XmlNode viewSpec, XmlNode intSpec)
		{
			string className = GetStringAtt(intSpec, "class");
			string attrName = GetStringAtt(intSpec, "field");
			if (className == null || attrName == null)
				return null; // Can't interpret an incomplete int.
			int flid = (int)m_mdc.GetFieldId(className, attrName, true);

			FilterSortItem result = new FilterSortItem();
			result.Spec = viewSpec;
			result.Finder = new OwnIntPropFinder(m_sda, flid);

			MakeIntCombo(result);
			result.FilterChanged += new FilterChangeHandler(this.FilterChangedHandler);
			result.Sorter = new GenRecordSorter(new StringFinderCompare(result.Finder,
				new IntStringComparer()));
			return result;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a default size for a FilterBar. The width is arbitrary, as it is always docked
		/// top, but the height is important and should match a standard combo.
		/// </summary>
		/// <value></value>
		/// <returns>The default <see cref="T:System.Drawing.Size"/> of the control.</returns>
		/// ------------------------------------------------------------------------------------
		protected override Size DefaultSize
		{
			get
			{
				return new Size(100, FwComboBox.ComboHeight);
			}
		}

		ITsString MakeLabel(string name)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(name, m_userWs);
		}

		/// <summary>
		/// The stuff common to all the ways we mak an FSI.
		/// </summary>
		/// <param name="item"></param>
		protected void SetupFsi(FilterSortItem item)
		{
			MakeCombo(item);
			item.FilterChanged += new FilterChangeHandler(this.FilterChangedHandler);
		}

		private void FilterChangedHandler(object sender, FilterChangeEventArgs args)
		{
			if (FilterChanged != null)
				FilterChanged(this, args);
		}

		/// <summary>
		/// Create the common options for all FSI combos (except Integer).
		/// </summary>
		/// <param name="item"></param>
		protected void MakeCombo(FilterSortItem item)
		{
			FwComboBox combo = new FwComboBox();
			combo.DropDownStyle = ComboBoxStyle.DropDownList;
			combo.BackColor = SystemColors.Window;
			combo.WritingSystemFactory = m_wsf;
			combo.StyleSheet = m_bv.StyleSheet;
			item.Combo = combo;
			combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksShowAll), null, item));

			string blankPossible = XmlUtils.GetOptionalAttributeValue(item.Spec, "blankPossible", "true");
			switch (blankPossible)
			{
				case "true":
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksBlanks), new BlankMatcher(), item));
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksNonBlanks), new NonBlankMatcher(), item));
					break;
			}

			// Enhance JohnT: figure whether the column has vernacular or analysis data...
			int ws = 0;
			if (item.Spec != null)
			{
				string wsParam = XmlViewsUtils.FindWsParam(item.Spec);
				if (wsParam.Length == 0)
					wsParam = XmlUtils.GetOptionalAttributeValue(item.Spec, "ws", "");
				ws = XmlViewsUtils.GetWsFromString(wsParam, m_cache);
			}
			if (ws == 0)
				ws = m_cache.DefaultVernWs; // some sort of fall-back in case we can't determine a WS from the spec.

			string beSpec = XmlUtils.GetOptionalAttributeValue(item.Spec, "bulkEdit", "");
			if (String.IsNullOrEmpty(beSpec))
				beSpec = XmlUtils.GetOptionalAttributeValue(item.Spec, "chooserFilter", "");

			string sortType = XmlUtils.GetOptionalAttributeValue(item.Spec, "sortType", null);
			switch (sortType)
			{
				case "integer":
					// For columns which are interger values we offer the user a couple preset filters
					// one is  "0"  and the other is "Greater than zero"
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksZero),
						new ExactMatcher(MatchExactPattern(XMLViewsStrings.ksZero)), item));
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksGreaterThanZero),
						new RangeIntMatcher(1, Int32.MaxValue), item));
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksGreaterThanOne),
						new RangeIntMatcher(2, Int32.MaxValue), item));
					combo.Items.Add(new RestrictComboItem(MakeLabel(XMLViewsStrings.ksRestrict_), item, m_cache.DefaultUserWs, combo));
					break;
				case "date":
					combo.Items.Add(new RestrictDateComboItem(MakeLabel(XMLViewsStrings.ksRestrict_), item, m_cache.DefaultUserWs, combo));
					break;
				case "YesNo":
					// For columns which have only the values of "yes" or "no" we offer the user these preset
					// filters to choose.
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksYes),
						new ExactMatcher(MatchExactPattern(XMLViewsStrings.ksYes)), item));
					combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksNo),
						new ExactMatcher(MatchExactPattern(XMLViewsStrings.ksNo)), item));
					break;
				case "stringList":
					string[] labels = m_bv.BrowseView.GetStringList(item.Spec);
					if (labels == null)
						break;
					foreach (string aLabel in labels)
					{
						combo.Items.Add(new FilterComboItem(MakeLabel(aLabel),
							new ExactMatcher(MatchExactPattern(aLabel)), item));
					}
					if (labels.Length > 2)
					{
						foreach (string aLabel in labels)
						{
							combo.Items.Add(new FilterComboItem(MakeLabel(string.Format(XMLViewsStrings.ksExcludeX, aLabel)),
								new InvertMatcher(new ExactMatcher(MatchExactPattern(aLabel))), item));
						}
					}
					break;
				default:
					// If it isn't any of those, include the bad spelling item, provided we have a dictionary
					// for the relevant language, and provided it is NOT a list (for which we will make a chooser).
					if (!String.IsNullOrEmpty(beSpec))
						break;
					Enchant.Dictionary dict = m_bv.BrowseView.EditingHelper.GetDictionary(ws);
					if (dict != null)
					{
						combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksSpellingErrors),
							new BadSpellingMatcher(ws, dict), item));
					}
					break;
			}
			combo.Items.Add(new FindComboItem(MakeLabel(XMLViewsStrings.ksFilterFor_), item, ws, combo, m_bv));

			if (!String.IsNullOrEmpty(beSpec))
			{
				MakeListChoiceFilterItem(item, combo, beSpec, m_bv.Mediator);
			}
			// Todo: lots more interesting items.
			// - search the list for existing names
			// - "any of" and "none of" launch a dialog with check boxes for all existing values.
			//		- maybe a control to check all items containing...
			// - "containing" launches dialog asking for string (may contain # at start or end).
			// - "matching pattern" launches dialog to obtain pattern.
			// - "custom" may launch dialog with "OR" options and "is, is not, is less than, is greater than, matches,..."
			// How can we get the current items? May not be available until later...
			// - May need to add 'ShowList' event to FwComboBox so we can populate the list when we show it.

			combo.SelectedIndex = 0;
			// Do this after selecting initial item, so we don't get a spurious notification.
			combo.SelectedIndexChanged += new EventHandler(Combo_SelectedIndexChanged);
			combo.AccessibleName = "FwComboBox";
			this.Controls.Add(combo);
		}

		internal IVwPattern MatchExactPattern(String str)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			int ws = m_cache.DefaultAnalWs;
			IVwPattern m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString(str, ws);
			return m_pattern;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a combo menu item (and install it) for choosing from a list, based on the column
		/// spec at item.Spec.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="combo">The combo.</param>
		/// <param name="beSpec">The be spec.</param>
		/// <param name="mediator">The mediator.</param>
		/// ------------------------------------------------------------------------------------
		private void MakeListChoiceFilterItem(FilterSortItem item, FwComboBox combo, string beSpec, XCore.Mediator mediator)
		{
			switch (beSpec)
			{
				case "complexListMultiple":
					combo.Items.Add(new ListChoiceComboItem(MakeLabel(XMLViewsStrings.ksChoose_), item, m_cache, mediator, combo, false, null));
					break;
				case "external":
					Type beType = DynamicLoader.TypeForLoaderNode(item.Spec);
					Type filterType = null;
					if (typeof(ListChoiceFilter).IsAssignableFrom(beType))
					{
						// typically it is a chooserFilter attribute, and gives the actual filter.
						filterType = beType;
					}
					else
					{
						// typically got a bulkEdit spec, and the editor class may know a compatible filter class.
						MethodInfo mi = beType.GetMethod("FilterType", BindingFlags.Static | BindingFlags.Public);
						if (mi != null)
							filterType = mi.Invoke(null, null) as Type;
					}

					if (filterType != null)
					{
						PropertyInfo pi = filterType.GetProperty("Atomic", BindingFlags.Public | BindingFlags.Static);
						bool fAtomic = false;
						if (pi != null)
							fAtomic = (bool)pi.GetValue(null, null);
						ListChoiceComboItem comboItem = new ListChoiceComboItem(MakeLabel(XMLViewsStrings.ksChoose_), item, m_cache, mediator, combo,
							fAtomic, filterType);
						combo.Items.Add(comboItem);

						PropertyInfo piLeaf = filterType.GetProperty("LeafFlid", BindingFlags.Public | BindingFlags.Static);
						if (piLeaf != null)
							comboItem.LeafFlid = (int)piLeaf.GetValue(null, null);
					}
					break;
				case "atomicFlatListItem": // Fall through
				case "morphTypeListItem":  // Fall through
				case "variantConditionListItem":
					combo.Items.Add(new ListChoiceComboItem(MakeLabel(XMLViewsStrings.ksChoose_), item, m_cache, mediator, combo, true, null));
					break;
				default:
					// if we didn't find it, try "chooserFilter", if we haven't already.
					string chooserFilter = XmlUtils.GetOptionalAttributeValue(item.Spec, "chooserFilter", "");
					if (!String.IsNullOrEmpty(chooserFilter) && chooserFilter != beSpec)
						MakeListChoiceFilterItem(item, combo, chooserFilter, mediator);
					return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the int combo.
		/// </summary>
		/// <param name="item">The item.</param>
		/// ------------------------------------------------------------------------------------
		protected void MakeIntCombo(FilterSortItem item)
		{
			// This is just similar enough to MakeCombo to be annoying.
			FwComboBox combo = new FwComboBox();
			combo.DropDownStyle = ComboBoxStyle.DropDownList;
			combo.WritingSystemFactory = m_wsf;
			item.Combo = combo;
			combo.Items.Add(new FilterComboItem(MakeLabel(XMLViewsStrings.ksShowAll), null, item));
			combo.Items.Add(new RestrictComboItem(MakeLabel(XMLViewsStrings.ksRestrict_),  item, m_cache.DefaultUserWs, combo));
			combo.SelectedIndex = 0;
			// Do this after selecting initial item, so we don't get a spurious notification.
			combo.SelectedIndexChanged +=new EventHandler(Combo_SelectedIndexChanged);
			combo.AccessibleName = "FwComboBox";
			this.Controls.Add(combo);
		}

		private void Combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			FwComboBox combo = sender as FwComboBox;
			if (combo == null)
				return;

			if (m_fInUpdateActive)
			{
				// The following colorization was requested by LT-2183.
				combo.BackColor = combo.SelectedIndex == 0 ? SystemColors.Window : Color.Yellow;
				return;
			}

			FilterComboItem fci = combo.SelectedItem as FilterComboItem;
			if (fci != null) // Happens when we set the text to what the user typed.
			{
				if (fci.Invoke())
					// The following colorization was requested by LT-2183.
					combo.BackColor = combo.SelectedIndex == 0 ? SystemColors.Window : Color.Yellow;
				else
					// Restore previous combo text
					combo.Tss = combo.PreviousTextBoxText;
			}
		}

		/// <summary>
		/// Reset any filters to empty.  This assumes that index 0 of the internal combobox
		///  selects the "no filter".
		/// </summary>
		public void RemoveAllFilters()
		{
			CheckDisposed();

			if (m_items == null)
				return;
			for (int i = 0; i < m_items.Length; ++i)
			{
				if (m_items[i] != null &&
					m_items[i].Combo != null &&
					m_items[i].Combo.SelectedIndex != 0)
				{
					m_items[i].Combo.SelectedIndex = 0;
				}
			}
			//Adjust the FilterBar height and combo box heights to accomodate the
			//strings in the filter comboBoxes
			AdjustBarHeights();
		}

		/// <summary>
		/// Apply the stylesheet to each combobox.
		/// </summary>
		/// <param name="stylesheet">Apply this to each ComboBox</param>
		internal void SetStyleSheet(IVwStylesheet stylesheet)
		{
			CheckDisposed();

			m_stylesheet = stylesheet;

			// Also apply stylesheet to each ComboBox.
			foreach (FilterSortItem item in m_items)
			{
				if (item.Combo is FwComboBox)
				{
					item.Combo.StyleSheet = stylesheet;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Search through the strings for the filters on columns and pick the tallest font height
		/// </summary>
		/// <returns>Height of tallest ComboBox font</returns>
		/// ------------------------------------------------------------------------------------
		private int GetMaxComboHeight()
		{
			int maxComboHeight = 0;

			// For each column in browse views seach through the Combo boxes (Filters)
			// then return the tallest font height from these.
			foreach (FilterSortItem item in m_items)
			{
				int ws = StringUtils.GetWsAtOffset(item.Combo.Tss, 0);
				Font tempFont = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontForNormalStyle(
				ws, m_stylesheet, m_wsf);
				maxComboHeight = Math.Max(maxComboHeight, tempFont.Height );
			}

			return maxComboHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the 'FilterBar' height and that of all its associated 'ComboBox'es
		/// </summary>
		/// <param name="height">Height value to apply to ComboBox and FilterBar</param>
		/// ------------------------------------------------------------------------------------
		private void SetBarHeight(int height)
		{
			// Calculate what to add to height for combobox to look right
			height += FwComboBox.ComboHeight - this.m_stdFontHeight;

			this.Height = height;
			foreach (FilterSortItem item in m_items)
			{
				if (item.Combo is FwComboBox)
				{
					item.Combo.Height = height;
					item.Combo.PerformLayout();
					item.Combo.Tss = FontHeightAdjuster.GetUnadjustedTsString(item.Combo.Tss);
				}
			}
		}


	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// A FilterComboItem stores a pointer to a particular FilterSortItem and a particular Matcher.
	/// It knows how to install its matcher into the filter and update things.
	/// Subclasses may launch a dialog and create the matcher appropriately first.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class FilterComboItem : ITssValue, IFWDisposable
	{
		/// <summary></summary>
		protected IMatcher m_matcher;
		internal FilterSortItem m_fsi;
		ITsString m_tssName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilterComboItem"/> class.
		/// </summary>
		/// <param name="tssName">Name of the TSS.</param>
		/// <param name="matcher">The matcher.</param>
		/// <param name="fsi">The fsi.</param>
		/// ------------------------------------------------------------------------------------
		public FilterComboItem(ITsString tssName, IMatcher matcher, FilterSortItem fsi)
		{
			m_tssName = tssName;
			m_matcher = matcher;
			m_fsi = fsi;
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
		~FilterComboItem()
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				//if (m_matcher != null)
				//{
				//	if (m_matcher is IDisposable)
				//		(m_matcher as IDisposable).Dispose();
				//}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_matcher = null;
			m_fsi = null; // Disposed elesewhere.
			if (m_tssName != null)
			{
				Marshal.ReleaseComObject(m_tssName);
				m_tssName = null;
			}

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool Invoke()
		{
			CheckDisposed();

			InvokeWithInstalledMatcher();
			return true;
		}

		/// <summary>
		///
		/// </summary>
		internal protected virtual void InvokeWithInstalledMatcher()
		{
			if (m_matcher != null)
				m_matcher.Label = GetLabelForMatcher();
			m_fsi.Matcher = m_matcher;
			// Raises event which implements the change of filter. This MUST be the last thing done
			// (be especially careful about subclasses) because a side effect is to persist the
			// filter. Changes (e.g., to matcher label) after this won't take effect.
			// Note that for this reason some overrides don't call the base class method.
			m_fsi.Filter = (m_matcher == null ? null : new FilterBarCellFilter(m_fsi.Finder, m_matcher));
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		protected virtual ITsString GetLabelForMatcher()
		{
			return m_tssName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether this combo item could have produced the specified filter.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this filter is active. Otherwise return null.
		/// By default, if the filter is exactly the same, just return your label.
		/// </summary>
		/// <param name="recordFilter">The record filter.</param>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString SetFromFilter(RecordFilter recordFilter, FilterSortItem item)
		{
			CheckDisposed();

			FilterBarCellFilter filter = recordFilter as FilterBarCellFilter;
			if (filter == null)
				return null; // combo items that don't produce FilterBarCellFilters should override.
			if (!filter.Finder.SameFinder(item.Finder))
				return null;
			IMatcher matcher = filter.Matcher;
			ITsString result = SetFromMatcher(matcher);
			if (result != null)
				m_matcher = matcher;
			return result;
		}

		/// <summary>
		/// Guts of SetFromFilter for FilterBarCellFilters.
		/// </summary>
		/// <param name="matcher"></param>
		/// <returns></returns>
		internal virtual ITsString SetFromMatcher(IMatcher matcher)
		{
			CheckDisposed();

			if (m_matcher != null && m_matcher.SameMatcher(matcher))
				return this.m_tssName;
			else
				return null;
		}

		#region ITssValue implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a TsString representation of the object.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public ITsString AsTss
		{
			get
			{
				CheckDisposed();
				return m_tssName;
			}
		}

		#endregion ITssValue implementation
	}

	/// <summary></summary>
	public enum ListMatchOptions
	{
		/// <summary></summary>
		Any, // True if any value in the item matches any value in the list
		/// <summary></summary>
		None, // True if no value in the item matches any value in the list,
		/// <summary></summary>
		All, // True if every value in the list occurs in the item (but others may occur also)
		/// <summary></summary>
		Exact // True if item has exactly the listed items (no more or less).
	}

	/// <summary>
	/// A list choice filter accepts items based on whether an object field in the root object
	/// contains one or more of a list of values.
	///
	/// Enhance: special case when first flid in sort item matches our flid: this means
	/// we are only showing one item from the property in this row. In this case, for Any
	/// show items in the list, for None show items not in the list, for All probably show items in the list.
	///
	/// </summary>
	public abstract class ListChoiceFilter : RecordFilter
	{
		private bool m_fIsUserVisible = false;
		ListMatchOptions m_mode;
		/// <summary></summary>
		protected FdoCache m_cache;
		Set<int> m_targets;
		int[] m_originalTargets;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ListChoiceFilter"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="targets">The targets.</param>
		/// ------------------------------------------------------------------------------------
		public ListChoiceFilter(FdoCache cache, ListMatchOptions mode, int[] targets)
		{
			m_cache = cache;
			m_mode = mode;
			Targets = targets;
		}
		/// <summary>
		/// Need zero-argument constructor for persistence. Don't use otherwise.
		/// </summary>
		public ListChoiceFilter()
		{
		}

		internal int[] Targets
		{
			get { return m_originalTargets; }
			set
			{
				m_originalTargets = value;
				m_targets = new Set<int>(value);
			}
		}
		internal ListMatchOptions Mode
		{
			get { return m_mode; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Accept(ManyOnePathSortItem item)
		{
			int[] values = GetItems(item);
			if (m_mode == ListMatchOptions.All || m_mode == ListMatchOptions.Exact)
			{
				Set<int> matches = new Set<int>(m_targets.Count);
				foreach (int hvo in values)
				{
					if (m_targets.Contains(hvo))
					{
						matches.Add(hvo);
						if (m_mode == ListMatchOptions.All && matches.Count == m_targets.Count)
							return true;
					}
					else if (m_mode == ListMatchOptions.Exact)
						return false; // found one that isn't present.
				}
				return matches.Count == m_targets.Count; // success if we found them all.
			}
			else
			{
				// any or none: look for first match
				foreach (int hvo in values)
				{
					if (m_targets.Contains(hvo))
					{
						// If we wanted any, finding one is a success; if we wanted none, finding any is a failure.
						return m_mode == ListMatchOptions.Any;
					}
				}
				// If we wanted any, not finding any is failure; if we wanted none, not finding any is success.
				return m_mode != ListMatchOptions.Any;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the items.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected abstract int[] GetItems(ManyOnePathSortItem item);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				m_cache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "mode", ((int)m_mode).ToString());
			XmlUtils.AppendAttribute(node, "targets", XmlUtils.MakeListValue(new List<int>(m_originalTargets)));
			if (m_fIsUserVisible)
				XmlUtils.AppendAttribute(node, "visible", "true");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml(node);
			m_mode = (ListMatchOptions)XmlUtils.GetMandatoryIntegerAttributeValue(node, "mode");
			Targets = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "targets");
			m_fIsUserVisible = XmlUtils.GetBooleanAttributeValue(node, "visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compatibles the filter.
		/// </summary>
		/// <param name="colSpec">The col spec.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool CompatibleFilter(XmlNode colSpec)
		{
			return BeSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "bulkEdit", null)
				|| BeSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "chooserFilter", null);
		}
		// The value of the "bulkEdit" property that causes this kind of filter to be created.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the be spec.
		/// </summary>
		/// <value>The be spec.</value>
		/// ------------------------------------------------------------------------------------
		protected abstract string BeSpec { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is user visible; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public override bool IsUserVisible
		{
			get
			{
				return m_fIsUserVisible;
			}
		}
		/// <summary>
		/// Provides a setter for IsUserVisible.  Needed to fix LT-6250.
		/// </summary>
		/// <param name="fVisible"></param>
		/// <returns></returns>
		public void MakeUserVisible(bool fVisible)
		{
			m_fIsUserVisible = fVisible;
		}

		/// <summary>
		/// This filter is valid only if all of the targets are valid object ids.
		/// </summary>
		public override bool IsValid
		{
			get
			{
				// We don't want to crash if we haven't been properly initialized!  See LT-9731.
				// And this  filter probably isn't valid anyway.
				if (m_cache == null)
					return false;
				foreach (int hvo in m_targets)
				{
					if (!m_cache.IsValidObject(hvo))
						return false;
				}
				return true;
			}
		}
	}

	/// <summary>
	/// this class depends upon column spec for figuring path information
	/// from the list item, since the list item (and hence flid and subflid)
	/// may change.
	/// </summary>
	public class ColumnSpecFilter : ListChoiceFilter
	{
		XmlNode m_colSpec = null;
		XmlBrowseViewBaseVc m_vc = null;

		/// <summary>
		/// for persistence.
		/// </summary>
		public ColumnSpecFilter()
			: base()
		{ }
		/// <summary>
		/// Filter used to compare against the hvos in a cell in rows described by ManyOnePathSortItem items.
		/// This depends upon xml spec to find the hvos, not a preconceived list item class, which is helpful for BulkEditEntries where
		/// our list item class can vary.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mode"></param>
		/// <param name="colSpec"></param>
		/// <param name="targets"></param>
		public ColumnSpecFilter(FdoCache cache, ListMatchOptions mode, int[] targets, XmlNode colSpec)
			: base(cache, mode, targets)
		{
			m_colSpec = colSpec;
		}

		private XmlBrowseViewBaseVc Vc
		{
			get
			{
				if (m_vc == null)
					m_vc = new XmlBrowseViewBaseVc(m_cache, null);
				return m_vc;
			}
		}

		/// <summary>
		/// eg. persist the column specification
		/// </summary>
		/// <param name="node"></param>
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			if (m_colSpec != null)
			{
				XmlNode colSpecToPersist = node.OwnerDocument.ImportNode(m_colSpec, true);
				node.AppendChild(colSpecToPersist);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			// Review: How do we validate this columnSpec is still valid?
			XmlNode colSpec = node.SelectSingleNode("./column");
			if(colSpec != null)
				m_colSpec = colSpec.CloneNode(true);
		}

		/// <summary>
		/// return all the hvos for this column in the row identified by the sort item.
		/// </summary>
		/// <param name="rowItem"></param>
		/// <returns></returns>
		protected override int[] GetItems(ManyOnePathSortItem rowItem)
		{
			int hvoRootObj = rowItem.RootObject.Hvo;
			XmlBrowseViewBaseVc.ItemsCollectorEnv collector =
				new XmlBrowseViewBaseVc.ItemsCollectorEnv(null, m_cache, hvoRootObj);
			if (Vc != null && m_colSpec != null)
				this.Vc.DisplayCell(rowItem, m_colSpec, hvoRootObj, collector);
			return collector.HvosCollectedInCell.ToArray();
		}

		/// <summary>
		///
		/// </summary>
		protected override string BeSpec
		{
			get
			{
				// we can get this from the columnSpec
				string beSpec = XmlUtils.GetOptionalAttributeValue(m_colSpec, "bulkEdit", "");
				if (String.IsNullOrEmpty(beSpec))
					beSpec = XmlUtils.GetOptionalAttributeValue(m_colSpec, "chooserFilter", "");
				if (beSpec == "")
				{
					// Enhance: figure it out from the the column spec parts.
				}
				return beSpec;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="colSpec"></param>
		/// <returns></returns>
		public override bool CompatibleFilter(XmlNode colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
				return false;
			// see if we can compare layout attributes
			Queue<string> possibleAttributes = new Queue<string>(new string[] {"layout", "subfield", "field"});
			return XmlViewsUtils.TryMatchExistingAttributes(colSpec, m_colSpec, ref possibleAttributes);
		}
	}

	/// <summary>
	/// Subclass that specifies a flid of the main object to match on.
	/// </summary>
	internal abstract class FlidChoiceFilter : ListChoiceFilter
	{
		int m_flid;
		public FlidChoiceFilter(FdoCache cache, ListMatchOptions mode, int flid, int[] targets)
			: base(cache, mode, targets)
		{
			m_flid = flid;
		}
		internal FlidChoiceFilter() { } // default for persistence.

		internal int Flid
		{
			get { return m_flid; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if we are sorted by column of the given flid, and if so returns the hvo for that item.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="iPathFlid">The i path flid.</param>
		/// <param name="item">The item.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns>
		/// 	<c>true</c> if [is sorted by field] [the specified flid]; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static internal bool IsSortedByField(int flid, int iPathFlid, ManyOnePathSortItem item, out int hvo)
		{
			hvo = 0;
			if (item.PathLength > iPathFlid && item.PathFlid(iPathFlid) == flid)
			{
				if ((item.PathLength > 1) && (item.PathLength != (iPathFlid + 1)))
					hvo = item.PathObject(1);
				else
					hvo = item.KeyObject;
				return true;
			}
			return false;
		}

		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flid", m_flid.ToString());
		}

		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml(node);
			m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		public override bool CompatibleFilter(XmlNode colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
				return false;
			return m_flid == BulkEditBar.GetFlidFromClassDotName(m_cache, colSpec, "field");
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ListChoiceComboItem : FilterComboItem
	{
		//int m_flid; // field to search for items.
		int m_hvoList; // root object of list.
		//int m_subflid; // optional flid to get from root object to one that has property m_flid
		/// <summary>
		/// If this has a value, it represents a 'leaf' property that should be followed from the
		/// tree items to the ones that can actually be selected.
		/// </summary>
		int m_leafFlid;
		FdoCache m_cache;
		XCore.Mediator m_mediator;
		FwComboBox m_combo;
		bool m_fAtomic;
		XmlNode m_colSpec;
		Type m_filterType; // non-null for external fields.

		/// <summary></summary>
		protected bool m_includeAbbr = false;
		/// <summary></summary>
		protected string m_bestWS = null;

		/// <summary>
		/// This is copied from C:\fw\Src\xWorks\RecordBarTreeHandler.cs
		/// There should be some refactoring done on how we access the xml attributes 'includeAbbr' and 'ws'
		/// </summary>
		protected string GetDisplayPropertyName
		{
			get
			{
				// NOTE: For labels with abbreviations using "LongName" rather than "AbbrAndNameTSS"
				// seems to load quicker for Semantic Domains and AnthroCodes.
				if (m_includeAbbr)
					return "LongName";
				else
					return "ShortNameTSS";
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ListChoiceComboItem"/> class.
		/// </summary>
		/// <param name="tssName">Name of the TSS.</param>
		/// <param name="fsi">The fsi.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="combo">The combo.</param>
		/// <param name="fAtomic">if set to <c>true</c> [f atomic].</param>
		/// <param name="filterType">Type of the filter.</param>
		/// ------------------------------------------------------------------------------------
		public ListChoiceComboItem(ITsString tssName, FilterSortItem fsi, FdoCache cache,
			XCore.Mediator mediator, FwComboBox combo, bool fAtomic, Type filterType)
			: base(tssName, null, fsi)
		{
			m_colSpec = fsi.Spec;

			if (filterType == null)
			{
				m_hvoList = BulkEditBar.GetNamedList(cache, fsi.Spec, "list");
				// This basically duplicates the loading of treeBarHandler properties. Currently, we don't have access
				// to that information in XMLViews, and even if we did, the information may not be loaded until
				// the user actually switches to that RecordList.
				XmlNode windowConfiguration = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
				string owningClass;
				string property;
				BulkEditBar.GetListInfo(fsi.Spec, out owningClass, out property);
				XmlNode recordListNode = windowConfiguration.SelectSingleNode(
					String.Format("//recordList[@owner='{0}' and @property='{1}']", owningClass, property));
				XmlNode treeBarHandlerNode = recordListNode.ParentNode.SelectSingleNode("treeBarHandler");
				m_includeAbbr = XmlUtils.GetBooleanAttributeValue(treeBarHandlerNode, "includeAbbr");
				m_bestWS = XmlUtils.GetOptionalAttributeValue(treeBarHandlerNode, "ws", null);
			}
			else
			{
				MethodInfo mi = filterType.GetMethod("List", BindingFlags.Public | BindingFlags.Static);
				m_hvoList = (int)mi.Invoke(null, new object[] { cache });
			}
			m_cache = cache;
			m_mediator = mediator;
			m_combo = combo;
			m_fAtomic = fAtomic;
			m_filterType = filterType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the leaf flid.
		/// </summary>
		/// <value>The leaf flid.</value>
		/// ------------------------------------------------------------------------------------
		public int LeafFlid
		{
			get
			{
				CheckDisposed();
				return m_leafFlid;
			}
			set
			{
				CheckDisposed();
				m_leafFlid = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Invoke()
		{
			CheckDisposed();

			ObjectLabelCollection labels = GetObjectLabelsForList();
			int[] oldTargets = new int[0];
			ListMatchOptions oldMode = ListMatchOptions.Any;
			if (m_fsi.Filter is ListChoiceFilter)
			{
				oldTargets = (m_fsi.Filter as ListChoiceFilter).Targets;
				oldMode = (m_fsi.Filter as ListChoiceFilter).Mode;
			}
			using (ReallySimpleListChooser chooser = MakeChooser(labels, oldTargets))
			{
				chooser.Cache = m_cache;
				chooser.SetObjectAndFlid(0, 0);
				chooser.ShowAnyAllNoneButtons(oldMode, m_fAtomic);
				chooser.EnableCtrlClick();
				System.Windows.Forms.DialogResult res = chooser.ShowDialog(m_fsi.Combo);
				if (System.Windows.Forms.DialogResult.Cancel == res)
					return false;

				ListChoiceFilter filter = MakeFilter(chooser.ListMatchMode, chooser.ChosenHvos.ToArray());
				InvokeWithFilter(filter);
				// Enhance JohnT: if there is just one item, maybe we could use its short name somehow?
			}
			return true;
		}

		private ListChoiceFilter MakeFilter(ListMatchOptions matchMode, int[] chosenHvos)
		{
			ListChoiceFilter filter = null;
			if (m_filterType != null)
			{
				if (m_filterType.IsSubclassOf(typeof(ColumnSpecFilter)))
				{
					ConstructorInfo ci = m_filterType.GetConstructor(
						new Type[] { typeof(FdoCache), typeof(ListMatchOptions), typeof(int[]), typeof(XmlNode)});
					filter = (ListChoiceFilter)ci.Invoke(new object[] { m_cache, matchMode, chosenHvos, m_fsi.Spec});
				}
				else
				{
					ConstructorInfo ci = m_filterType.GetConstructor(
						new Type[] { typeof(FdoCache), typeof(ListMatchOptions), typeof(int[]) });
					filter = (ListChoiceFilter)ci.Invoke(new object[] { m_cache, matchMode, chosenHvos });
				}
			}
			else
			{
				// make a filter that figures path information from the column specs
				filter = new ColumnSpecFilter(m_cache, matchMode, chosenHvos, m_fsi.Spec);
			}
			return filter;
		}

		private ObjectLabelCollection GetObjectLabelsForList()
		{
			ICmPossibilityList list = CmPossibilityList.CreateFromDBObject(m_cache, m_hvoList);
			List<int> candidates = new List<int>(list.PossibilitiesOS.HvoArray);
			bool fShowEmpty = XmlUtils.GetOptionalBooleanAttributeValue(m_colSpec, "canChooseEmpty", false);
			ObjectLabelCollection labels = new ObjectLabelCollection(m_cache, candidates,
				GetDisplayPropertyName, m_bestWS, fShowEmpty); //analysis vernacular //best analorvern //best analysis
			return labels;
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="matchMode"></param>
		/// <param name="chosenLabels"></param>
		public void InvokeWithColumnSpecFilter(ListMatchOptions matchMode, List<string> chosenLabels)
		{
			if (m_fsi.Spec == null)
				return;	// doesn't have a spec to create ColumnSpecFilter
			ObjectLabelCollection labels = GetObjectLabelsForList();
			List<int> chosenHvos = new List<int>();
			if (chosenLabels.Count > 0)
				FindChosenHvos(labels, chosenLabels, ref chosenHvos);
			ListChoiceFilter filter = MakeFilter(matchMode, chosenHvos.ToArray());
			InvokeWithFilter(filter);
		}

		private static void FindChosenHvos(ObjectLabelCollection labels, List<string> chosenLabels, ref List<int> chosenHvos)
		{
			foreach (ObjectLabel label in labels)
			{
				// go through the labels, and build of list of matching labels.
				if (chosenLabels.Contains(label.DisplayName))
					chosenHvos.Add(label.Hvo);
				// do the same for subitems
				if (label.SubItems.Count != 0)
					FindChosenHvos(label.SubItems, chosenLabels, ref chosenHvos);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="filter"></param>
		private void InvokeWithFilter(ListChoiceFilter filter)
		{
			filter.MakeUserVisible(true);	// This fixes LT-6250.
			// Todo: do something about persisting and restoring the filter.
			// We can't call base.Invoke because it is designed for FilterBarCellFilters.
			m_combo.SelectedIndex = -1; // allows setting Tss to non-member.
			m_combo.Tss = MakeLabel(filter);
			m_fsi.Matcher = null;
			m_fsi.Filter = filter;  //NOTE:  it is necessary to change the filter after a change in the string m_combo.Tss
			//		so that the heights of the comboBoxes in the Filterbar adjusted to accomodate the string.
		}

		private ReallySimpleListChooser MakeChooser(ObjectLabelCollection labels, int[] oldTargets)
		{
			XCore.PersistenceProvider persistProvider =
				new PersistenceProvider(m_mediator.PropertyTable);
			if (m_leafFlid == 0)
				return new ReallySimpleListChooser(persistProvider,
								labels, "Items", m_cache, oldTargets);
			else
				return new LeafChooser(persistProvider,
								labels, "Items", m_cache, oldTargets, m_leafFlid);
		}

		private ITsString MakeLabel(ListChoiceFilter filter)
		{
			string label;
			if (filter.Targets.Length == 1)
			{
				ITsString name;
				if (filter.Targets[0] == 0)
				{
					NullObjectLabel empty = new NullObjectLabel();
					name = m_cache.MakeUserTss(empty.DisplayName);
				}
				else
				{
					ICmObject obj = CmObject.CreateFromDBObject(m_cache, filter.Targets[0]);
					name = obj.ShortNameTSS;
				}
				switch (filter.Mode)
				{
					case ListMatchOptions.None:
						{
							return ComposeLabel(name, XMLViewsStrings.ksNotX);
						}
					case ListMatchOptions.Exact:
						{
							return ComposeLabel(name, XMLViewsStrings.ksOnlyX);
						}
					default: //  appropriate for both Any and All, which mean the same in this case.
						return name;
				}

			}
			else
			{
				switch (filter.Mode)
				{
					case ListMatchOptions.All:
						label = XMLViewsStrings.ksAllOf_;
						break;
					case ListMatchOptions.None:
						label = XMLViewsStrings.ksNoneOf_;
						break;
					case ListMatchOptions.Exact:
						label = XMLViewsStrings.ksOnly_;
						break;
					default: // typically Any
						label = XMLViewsStrings.ksAnyOf_;
						break;
				}
				return m_cache.MakeUserTss(label);
			}
		}

		/// <summary>
		/// Compose a label from the ShortNameTSS value formatted with additional text.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sFmt"></param>
		/// <returns></returns>
		private ITsString ComposeLabel(ITsString name, string sFmt)
		{
			string sLabel = String.Format(sFmt, name.Text);
			ITsString tsLabel = m_cache.MakeUserTss(sLabel);
			int ich = sFmt.IndexOf("{0}");
			if (ich >= 0)
			{
				int cchName = name.Text == null ? 0 : name.Text.Length;
				if (cchName > 0)
				{
					ITsTextProps ttp = name.get_Properties(0);
					ITsStrBldr bldr = tsLabel.GetBldr();
					bldr.SetProperties(ich, ich + cchName, ttp);
					return bldr.GetString();
				}
			}
			return tsLabel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether this combo item could have produced the specified filter.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this filter is active. Otherwise return null.
		/// By default, if the filter is exactly the same, just return your label.
		/// </summary>
		/// <param name="recordFilter">The record filter.</param>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString SetFromFilter(RecordFilter recordFilter, FilterSortItem item)
		{
			CheckDisposed();

			ListChoiceFilter filter = recordFilter as ListChoiceFilter;
			if (filter == null)
				return null;
			if (!filter.CompatibleFilter(m_colSpec))
				return null;
			return MakeLabel(filter);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RestrictDateComboItem : FilterComboItem
	{
		FwComboBox m_combo;
		int m_ws;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RestrictDateComboItem"/> class.
		/// </summary>
		/// <param name="tssName">Name of the TSS.</param>
		/// <param name="fsi">The fsi.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="combo">The combo.</param>
		/// ------------------------------------------------------------------------------------
		public RestrictDateComboItem(ITsString tssName, FilterSortItem fsi, int ws, FwComboBox combo)
			: base(tssName, null, fsi)
		{
			m_combo = combo;
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
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
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_combo = null;

			base.Dispose (disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Invoke()
		{
			CheckDisposed();

			using (SimpleDateMatchDlg dlg = new SimpleDateMatchDlg())
			{
				dlg.SetDlgValues(m_matcher);
				if (dlg.ShowDialog(m_combo) != DialogResult.OK)
					return false;

				m_matcher = dlg.ResultingMatcher;
				m_matcher.WritingSystemFactory = m_combo.WritingSystemFactory;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				m_combo.SelectedIndex = -1; // allows setting text to item not in list, see comment in FindComboItem.Invoke().
				m_combo.Tss = tsf.MakeString(dlg.Pattern, m_ws);
				ITsString label = m_combo.Tss;
				m_matcher.Label = label;
				// We can't call base.Invoke BEFORE we set the label, because it will persist
				// the wrong label. And we can't call it AFTER we set the label, becaseu it
				// will override our label. So we just copy here a simplified version of the
				// base method. If it gets much more complicated, factor out the common parts
				// into new methods.
				//base.Invoke ();
				m_fsi.Matcher = m_matcher;
				m_fsi.Filter = new FilterBarCellFilter(m_fsi.Finder, m_matcher);
			}

			return true;
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified matcher.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this matcher is active. Otherwise return null.
		/// </summary>
		/// <param name="matcher"></param>
		/// <returns></returns>
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			CheckDisposed();

			if (matcher is DateTimeMatcher)
				return matcher.Label;
			else
				return null;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RestrictComboItem : FilterComboItem
	{
		FwComboBox m_combo;
		int m_ws;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RestrictComboItem"/> class.
		/// </summary>
		/// <param name="tssName">Name of the TSS.</param>
		/// <param name="fsi">The fsi.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="combo">The combo.</param>
		/// ------------------------------------------------------------------------------------
		public RestrictComboItem(ITsString tssName, FilterSortItem fsi, int ws, FwComboBox combo)
			: base(tssName, null, fsi)
		{
			m_combo = combo;
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
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
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_combo = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Invoke()
		{
			CheckDisposed();

			using (SimpleIntegerMatchDlg dlg = new SimpleIntegerMatchDlg())
			{
				dlg.SetDlgValues(m_matcher);
				if (dlg.ShowDialog(m_combo) != DialogResult.OK)
					return false;

				m_matcher = dlg.ResultingMatcher;
				m_matcher.WritingSystemFactory = m_combo.WritingSystemFactory;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				m_combo.SelectedIndex = -1; // allows setting text to item not in list, see comment in FindComboItem.Invoke().
				m_combo.Tss = tsf.MakeString(dlg.Pattern, m_ws);
				ITsString label = m_combo.Tss;
				m_matcher.Label = label;
				// We can't call base.Invoke BEFORE we set the label, because it will persist
				// the wrong label. And we can't call it AFTER we set the label, becaseu it
				// will override our label. So we just copy here a simplified version of the
				// base method. If it gets much more complicated, factor out the common parts
				// into new methods.
				//base.Invoke ();
				m_fsi.Matcher = m_matcher;
				m_fsi.Filter = new FilterBarCellFilter(m_fsi.Finder, m_matcher);
			}
			return true;
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified matcher.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this matcher is active. Otherwise return null.
		/// </summary>
		/// <param name="matcher"></param>
		/// <returns></returns>
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			CheckDisposed();

			if (matcher is IntMatcher)
				return matcher.Label;
			else
				return null;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FindComboItem : FilterComboItem
	{
		int m_ws;
		FwComboBox m_combo;
		BrowseViewer m_bv;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FindComboItem"/> class.
		/// </summary>
		/// <param name="tssName">Name of the TSS.</param>
		/// <param name="fsi">The fsi.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="combo">The combo.</param>
		/// <param name="bv">The bv.</param>
		/// ------------------------------------------------------------------------------------
		public FindComboItem(ITsString tssName, FilterSortItem fsi, int ws, FwComboBox combo, BrowseViewer bv)
			: base(tssName, null, fsi)
		{
			m_ws = ws;
			m_combo = combo;
			m_bv = bv;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Invoke()
		{
			CheckDisposed();

			IVwStylesheet stylesheet = Widgets.FontHeightAdjuster.StyleSheetFromMediator(m_bv.Mediator);
			using (SimpleMatchDlg dlg = new SimpleMatchDlg(m_combo.WritingSystemFactory, m_ws, stylesheet))
			{
				dlg.SetDlgValues(m_matcher, stylesheet);
				if (dlg.ShowDialog() != DialogResult.OK || dlg.Pattern.Length == 0)
					return false;

				//if (m_matcher != null && m_matcher is IDisposable)
				//	(m_matcher as IDisposable).Dispose();
				m_matcher = dlg.ResultingMatcher;
				InvokeWithInstalledMatcher();
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		internal protected override void InvokeWithInstalledMatcher()
		{
			// This is a kludge to get around a dubious behavior of combo box: if we set the
			// Tss to something not in the list, and something in the list was previously
			// selected, it fails, making the string empty. If there was already nothing
			// selected, setting the text goes ahead.
			m_combo.SelectedIndex = -1;
			m_combo.Tss = (m_matcher as SimpleStringMatcher).Pattern.Pattern;
			base.InvokeWithInstalledMatcher();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		protected override ITsString GetLabelForMatcher()
		{
			return m_combo.Tss;
		}

		internal int Ws
		{
			get { return m_ws; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
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
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_combo = null; // Disposed elsewhere.

			base.Dispose (disposing);
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified matcher.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this matcher is active. Otherwise return null.
		/// </summary>
		/// <param name="matcher"></param>
		/// <returns></returns>
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			CheckDisposed();

			if (matcher is SimpleStringMatcher)
				return matcher.Label;
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the matcher.
		/// </summary>
		/// <value>The matcher.</value>
		/// ------------------------------------------------------------------------------------
		public IMatcher Matcher
		{
			get
			{
				CheckDisposed();
				return m_matcher;
			}
			set
			{
				CheckDisposed();
				m_matcher = value;
			}
		}
	}
}
