// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HeaderFooterConfigurer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a standard implementation of IHeaderFooterConfigurer, which supplies
	/// instances of HeaderFooterVc for laying out headers and footers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class HeaderFooterConfigurer : IHeaderFooterConfigurer
	{
		#region Data members
		/// <summary></summary>
		protected FdoCache m_fdoCache;
		/// <summary></summary>
		protected IPubHFSet m_hfSet;
		/// <summary></summary>
		protected int m_wsDefault;
		/// <summary></summary>
		protected DateTime m_printDateTime;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="HeaderFooterConfigurer"/> class.
		/// </summary>
		/// <param name="cache">The cache representing our DB connection.</param>
		/// <param name="hvoHeaderFooterSet">Id of the header/footer set to be used</param>
		/// <param name="wsDefault">Default writing system</param>
		/// <param name="printDateTime">printing date/time</param>
		/// ------------------------------------------------------------------------------------
		public HeaderFooterConfigurer(FdoCache cache, int hvoHeaderFooterSet, int wsDefault,
			DateTime printDateTime)
		{
			m_fdoCache = cache;
			if (hvoHeaderFooterSet > 0)
				m_hfSet = m_fdoCache.ServiceLocator.GetInstance<IPubHFSetRepository>().GetObject(hvoHeaderFooterSet);
			m_wsDefault = wsDefault;
			m_printDateTime = printDateTime;
		}

		#region IHeaderFooterConfigurer Members
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the header view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// <returns>the header view construtor</returns>
		/// -------------------------------------------------------------------------------------
		public virtual IVwViewConstructor MakeHeaderVc(IPageInfo page)
		{
			// TODO: Supply params to configure header based on the header/footer set being used.
			HeaderFooterVc vc = new HeaderFooterVc(page, m_wsDefault, m_printDateTime, m_fdoCache);
			vc.SetDa(m_fdoCache.MainCacheAccessor);
			return vc;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the footer view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// <returns>the footer view construtor</returns>
		/// -------------------------------------------------------------------------------------
		public virtual IVwViewConstructor MakeFooterVc(IPageInfo page)
		{
			// TODO: Supply params to configure footer based on the header/footer set being used.
			HeaderFooterVc vc = new HeaderFooterVc(page, m_wsDefault, m_printDateTime, m_fdoCache);
			vc.SetDa(m_fdoCache.MainCacheAccessor);
			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hvo of the PubHeader that should be used as the root Id when the header or
		/// footer stream is constructed for the given page.
		/// </summary>
		/// <param name="pageNumber">Used for determining whether this page is first, even, or
		/// odd</param>
		/// <param name="fHeader"><c>true</c> if the id for header is desired; <c>false</c> if
		/// the id for the footer is desired</param>
		/// <param name="fDifferentFirstHF">Indicates whether caller wishes to get a different
		/// header or footer if this is the first page (and if the HF set has a different value
		/// set for the first page)</param>
		/// <param name="fDifferentEvenHF">Indicates whether caller wishes to get a different
		/// header or footer if this is an even page (and if the HF set has a different value
		/// set for even pages)</param>
		/// <returns>the hvo of the requested PubHeader</returns>
		/// ------------------------------------------------------------------------------------
		public int GetHvoRoot(int pageNumber, bool fHeader, bool fDifferentFirstHF,
			bool fDifferentEvenHF)
		{
			if (m_hfSet == null)
				return 0;
			IPubHeader hf;

			if (pageNumber == 1 && fDifferentFirstHF)
			{
				if (fHeader)
				{
					hf = m_hfSet.FirstHeaderOA;
					return (hf != null) ? hf.Hvo : m_hfSet.DefaultHeaderOA.Hvo;
				}
				else
				{
					hf = m_hfSet.FirstFooterOA;
					return (hf != null) ? hf.Hvo : m_hfSet.DefaultFooterOA.Hvo;
				}
			}

			if (pageNumber % 2 == 0 && fDifferentEvenHF)
			{
				if (fHeader)
				{
					hf = m_hfSet.EvenHeaderOA;
					return (hf != null) ? hf.Hvo : m_hfSet.DefaultHeaderOA.Hvo;
				}
				else
				{
					hf = m_hfSet.EvenFooterOA;
					return (hf != null) ? hf.Hvo : m_hfSet.DefaultFooterOA.Hvo;
				}
			}

			if (fHeader)
			{
				if (m_hfSet.DefaultHeaderOA != null)
					return m_hfSet.DefaultHeaderOA.Hvo;
				else
					return 0;
			}
			else
			{
				if (m_hfSet.DefaultFooterOA != null)
					return m_hfSet.DefaultFooterOA.Hvo;
				else
					return 0;
			}
		}
		#endregion
	}
}
