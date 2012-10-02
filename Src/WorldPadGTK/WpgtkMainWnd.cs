/*
 * MarkS - 2008-09-11
 */

using System;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.GtkCustomWidget;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// A mainwnd that implements ISimpleMainWnd so it can be passed to
	/// the C++ WPX load+save code. Holds page layout information, headers, footers,
	/// and points to the stylesheet.
	/// </summary>
	[ComVisible(true)]
	[Guid("0924cfad-88b1-4728-8497-7dc84ee3a34c")]
	[ClassInterface(ClassInterfaceType.None)]
	[ProgId("SIL.FieldWorks.WorldPad.WpgtkMainWnd")]
	public class WpgtkMainWnd : ISimpleMainWnd
	{
		/// Implements ISimpleMainWnd method.
		public ISimpleMainWnd LauncherWindow()
		{
			throw new NotImplementedException();
		}

		/// <summary>Gets sylesheet associated with this mainwnd.</summary>
		/// Implements ISimpleMainWnd method.
		/// <returns>stylesheet associated with this mainwnd</returns>
		public ISimpleStylesheet GetStylesheet()
		{
			return (ISimpleStylesheet)Stylesheet;
		}

		/// Implements ISimpleMainWnd method.
		public int TopMargin()
		{
			return m_topMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int BottomMargin()
		{
			return m_bottomMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int LeftMargin()
		{
			return m_leftMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int RightMargin()
		{
			return m_rightMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int HeaderMargin()
		{
			return m_headerMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int FooterMargin()
		{
			return m_footerMargin;
		}

		/// Implements ISimpleMainWnd method.
		public int PageSize()
		{
			return m_pageSize;
		}

		/// Implements ISimpleMainWnd method.
		public int PageHeight()
		{
			return m_pageHeight;
		}

		/// Implements ISimpleMainWnd method.
		public int PageWidth()
		{
			return m_pageWidth;
		}

		/// Implements ISimpleMainWnd method.
		public int PageOrientation()
		{
			return m_pageOrientation;
		}

		/// Implements ISimpleMainWnd method.
		public ITsString PageHeader()
		{
			return m_pageHeader;
		}

		/// Implements ISimpleMainWnd method.
		public ITsString PageFooter()
		{
			return m_pageFooter;
		}

		/// Implements ISimpleMainWnd method.
		public void SetTopMargin(int dymp)
		{
			m_topMargin = dymp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetBottomMargin(int dymp)
		{
			m_bottomMargin = dymp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetLeftMargin(int dxmp)
		{
			m_leftMargin = dxmp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetRightMargin(int dxmp)
		{
			m_rightMargin = dxmp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetHeaderMargin(int dymp)
		{
			m_headerMargin = dymp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetFooterMargin(int dymp)
		{
			m_footerMargin = dymp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageSize(int s)
		{
			m_pageSize = s;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageHeight(int dymp)
		{
			m_pageHeight = dymp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageWidth(int dxmp)
		{
			m_pageWidth = dxmp;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageOrientation(int n)
		{
			m_pageOrientation = n;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageHeader(ITsString _tss)
		{
			m_pageHeader = _tss;
		}

		/// Implements ISimpleMainWnd method.
		public void SetPageFooter(ITsString _tss)
		{
			m_pageFooter = _tss;
		}

		/// <value>Stylesheet associated with this mainwnd</value>
		public WpStylesheet Stylesheet
		{
			get
			{
				return stylesheet;
			}
			set
			{
				stylesheet = value;
			}
		}

		/// <summary>Stylesheet associated with this mainwnd</summary>
		private WpStylesheet stylesheet;

		// Page size, margin, and footer/header information.
		private int m_topMargin;
		private int m_bottomMargin;
		private int m_leftMargin;
		private int m_rightMargin;
		private int m_headerMargin;
		private int m_footerMargin;
		private int m_pageSize;
		private int m_pageHeight;
		private int m_pageWidth;
		private int m_pageOrientation;
		private ITsString m_pageHeader;
		private ITsString m_pageFooter;
	}
}
