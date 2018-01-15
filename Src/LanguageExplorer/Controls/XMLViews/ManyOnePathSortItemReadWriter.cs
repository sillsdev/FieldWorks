// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Allows a generic way to access a string in a browse view column cell.
	/// (Currently used for Source combo items)
	/// </summary>
	internal class ManyOnePathSortItemReadWriter : FieldReadWriter, IDisposable
	{
		private LcmCache m_cache;
		private XElement m_colSpec;
		private BrowseViewer m_bv;
		private IStringFinder m_finder;
		private IApp m_app;

		internal ManyOnePathSortItemReadWriter(LcmCache cache, XElement colSpec, BrowseViewer bv, IApp app)
			: base(cache.DomainDataByFlid)
		{
			m_cache = cache;
			m_colSpec = colSpec;
			m_bv = bv;
			m_app = app;
			EnsureFinder();
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~ManyOnePathSortItemReadWriter()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_finder as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_finder = null;
			m_cache = null;
			m_colSpec = null;
			m_bv = null;
			m_app = null;
			IsDisposed = true;
		}
		#endregion

		private IManyOnePathSortItem GetManyOnePathSortItem(int hvo)
		{
			return new ManyOnePathSortItem(hvo, null, null);	// just return an item based on the the RootObject
		}

		private void EnsureFinder()
		{
			if (m_finder == null)
			{
				m_finder = LayoutFinder.CreateFinder(m_cache, m_colSpec, m_bv.BrowseView.Vc, m_app);
			}
		}

		/// <summary>
		/// Get's the string associated with the given hvo for a particular column.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public override ITsString CurrentValue(int hvo)
		{
			return m_finder.Key(GetManyOnePathSortItem(hvo));
		}

		/// <summary>
		/// NOTE: ManyOnePathSortItemReadWriter is currently read-only.
		/// </summary>
		public override void SetNewValue(int hvo, ITsString tss)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///
		/// </summary>
		public override int WritingSystem
		{
			get { throw new NotSupportedException(); }
		}
	}
}