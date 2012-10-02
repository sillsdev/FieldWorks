// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeHeaderFooterConfigurer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
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
