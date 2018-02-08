// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;

namespace LanguageExplorer.Controls.XMLViews
{
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
	public class FilterSortItem : IDisposable
	{
		private XElement m_viewSpec;
		private IStringFinder m_finder;
		private FwComboBox m_combo;
		private IMatcher m_matcher;
		private RecordFilter m_filter;
		private RecordSorter m_sorter;
		private readonly DisposableObjectsSet<RecordFilter> m_FiltersToDispose = new DisposableObjectsSet<RecordFilter>();

		/// <summary />
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
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

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

		/// <summary />
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				// We didn't make these either, but we need to deal with them.
				// No. These belong to the RecordList.
				//if (m_matcher != null && m_matcher is IDisposable)
				//	(m_matcher as IDisposable).Dispose();

				// At least in the tests these get created for us; we're the only one using them,
				// so we should dispose them.
				(Finder as IDisposable)?.Dispose();
				(Sorter as IDisposable)?.Dispose();
				m_FiltersToDispose.Dispose();
				m_combo?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_viewSpec = null;
			m_combo = null;
			m_finder = null;
			m_sorter = null;
			m_filter = null;
			m_matcher = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Gets or sets the spec.
		/// </summary>
		public XElement Spec
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

		/// <summary>
		/// Gets or sets the finder.
		/// </summary>
		/// <remarks>A Finder assigned here will be disposed by FilterSortItem.Dispose.</remarks>
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
				(m_finder as IDisposable)?.Dispose();

				m_finder = value;
			}
		}

		/// <summary>
		/// Gets or sets the combo.
		/// </summary>
		/// <remarks>The Combo that gets set here will be disposed by FilterSortItem.</remarks>
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
				(m_combo as IDisposable)?.Dispose();
				m_combo = value;
			}
		}

		/// <summary>
		/// Gets or sets the matcher.
		/// </summary>
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
				{
					m_matcher.WritingSystemFactory = m_combo.WritingSystemFactory;
				}
			}
		}

		/// <summary>
		/// Gets or sets the sorter.
		/// </summary>
		/// <remarks>A Sorter assigned here will be disposed by FilterSortItem.Dispose.</remarks>
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
				(m_sorter as IDisposable)?.Dispose();
				m_sorter = value;
			}
		}

		/// <summary>
		/// Gets or sets the filter.
		/// </summary>
		/// <value>The filter.</value>
		/// <remarks>A Filter assigned here will be disposed by FilterSortItem.Dispose.</remarks>
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

				var old = m_filter;
				m_filter = value;
				m_FiltersToDispose.Add(value);
				// Change the filter if they are not the same );
				if (FilterChanged != null && (m_filter != null && !m_filter.SameFilter(old)) || (old != null && !old.SameFilter(m_filter)))
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
		public bool SetFromFilter(RecordFilter filter)
		{
			CheckDisposed();

			// Need to set even if set previously. Otherwise it doesn't refresh properly.
			//if (filter == m_filter)
			//	return true;  // we're already set.
			if (m_combo == null)
			{
				return false; // probably can't happen, but play safe
			}
			foreach (FilterComboItem fci in m_combo.Items)
			{
				var tssLabel = fci?.SetFromFilter(filter, this);
				if (tssLabel == null)
				{
					continue;
				}
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
		// Add to RecordList ability to add/remove filters and refresh list.
		// Configure RecordBrowseView to handle filter changes by updating record list.
	}
}