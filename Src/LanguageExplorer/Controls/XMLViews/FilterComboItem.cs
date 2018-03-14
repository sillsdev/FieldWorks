// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A FilterComboItem stores a pointer to a particular FilterSortItem and a particular Matcher.
	/// It knows how to install its matcher into the filter and update things.
	/// Subclasses may launch a dialog and create the matcher appropriately first.
	/// </summary>
	public class FilterComboItem : ITssValue, IDisposable
	{
		/// <summary></summary>
		protected IMatcher m_matcher;
		internal FilterSortItem m_fsi;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilterComboItem"/> class.
		/// </summary>
		public FilterComboItem(ITsString tssName, IMatcher matcher, FilterSortItem fsi)
		{
			AsTss = tssName;
			m_matcher = matcher;
			m_fsi = fsi;
		}

		#region IDisposable & Co. implementation

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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_matcher = null;
			m_fsi = null; // Disposed elesewhere.
			AsTss = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Invokes this instance.
		/// </summary>
		public virtual bool Invoke()
		{
			InvokeWithInstalledMatcher();
			return true;
		}

		/// <summary>
		///
		/// </summary>
		protected internal virtual void InvokeWithInstalledMatcher()
		{
			if (m_matcher != null)
			{
				m_matcher.Label = GetLabelForMatcher();
			}
			m_fsi.Matcher = m_matcher;
			// Raises event which implements the change of filter. This MUST be the last thing done
			// (be especially careful about subclasses) because a side effect is to persist the
			// filter. Changes (e.g., to matcher label) after this won't take effect.
			// Note that for this reason some overrides don't call the base class method.
			m_fsi.Filter = (m_matcher == null ? null : new FilterBarCellFilter(m_fsi.Finder, m_matcher));
		}

		/// <summary />
		protected virtual ITsString GetLabelForMatcher()
		{
			return AsTss;
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified filter.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this filter is active. Otherwise return null.
		/// By default, if the filter is exactly the same, just return your label.
		/// </summary>
		public virtual ITsString SetFromFilter(RecordFilter recordFilter, FilterSortItem item)
		{
			var filter = recordFilter as FilterBarCellFilter;
			if (filter == null)
			{
				return null; // combo items that don't produce FilterBarCellFilters should override.
			}
			if (!filter.Finder.SameFinder(item.Finder))
			{
				return null;
			}
			var matcher = filter.Matcher;
			var result = SetFromMatcher(matcher);
			if (result != null)
			{
				m_matcher = matcher;
			}
			return result;
		}

		/// <summary>
		/// Guts of SetFromFilter for FilterBarCellFilters.
		/// </summary>
		internal virtual ITsString SetFromMatcher(IMatcher matcher)
		{
			if (m_matcher != null && m_matcher.SameMatcher(matcher))
			{
				return AsTss;
			}
			return null;
		}

		#region ITssValue implementation

		/// <summary>
		/// Get a TsString representation of the object.
		/// </summary>
		public ITsString AsTss { get; private set; }

		#endregion ITssValue implementation
	}
}