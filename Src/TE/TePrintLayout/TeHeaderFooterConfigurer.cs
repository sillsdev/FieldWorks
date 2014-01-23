// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeHeaderFooterConfigurer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.PrintLayout;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TeHeaderFooterConfigurer.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeHeaderFooterConfigurer: HeaderFooterConfigurer
	{
		private int m_filterInstance;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeHeaderFooterConfigurer"/> class.
		/// </summary>
		/// <param name="cache">FDO Cache to use</param>
		/// <param name="hvoHFSet">something that is an int</param>
		/// <param name="wsDefault">Default writing system</param>
		/// <param name="filterInstance">book filter instance</param>
		/// <param name="printDateTime">printing date/time</param>
		/// ------------------------------------------------------------------------------------
		public TeHeaderFooterConfigurer(FdoCache cache, int hvoHFSet, int wsDefault,
			int filterInstance, DateTime printDateTime):
			base(cache, hvoHFSet, wsDefault, printDateTime)
		{
			m_filterInstance = filterInstance;
		}

		#region IHeaderFooterConfigurer overrides
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the header view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// <returns>the header view constructor</returns>
		/// -------------------------------------------------------------------------------------
		public override IVwViewConstructor MakeHeaderVc(IPageInfo page)
		{
			TeHeaderFooterVc vc = new TeHeaderFooterVc(m_fdoCache, page, m_wsDefault,
				m_printDateTime, m_filterInstance);
			vc.SetDa(m_fdoCache.MainCacheAccessor);
			return vc;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the footer view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// <returns>the footer view constructor</returns>
		/// -------------------------------------------------------------------------------------
		public override IVwViewConstructor MakeFooterVc(IPageInfo page)
		{
			TeHeaderFooterVc vc = new TeHeaderFooterVc(m_fdoCache, page, m_wsDefault,
				m_printDateTime, m_filterInstance);
			vc.SetDa(m_fdoCache.MainCacheAccessor);
			return vc;
		}
		#endregion
	}
}
