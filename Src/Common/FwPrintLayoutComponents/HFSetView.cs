// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is the small view that the Header/Footer setup dialog uses to display the header
	/// or footer previews
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class HFSetView : SimpleRootSite
	{
		#region Member variables
		private ISilDataAccess m_sda;
		private HeaderFooterVc m_vc;
		private int m_hvoHeader;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new HFSetView
		/// </summary>
		/// <param name="sda">The ISilDataAccess for the view</param>
		/// <param name="vc">The view constructor used to create the view</param>
		/// <param name="hvoHeader">The id of the PubHeader used to get the Header/Footer
		/// information to display in the view</param>
		/// ------------------------------------------------------------------------------------
		public HFSetView(ISilDataAccess sda, HeaderFooterVc vc, int hvoHeader) : base()
		{
			m_sda = sda;
			WritingSystemFactory = m_sda.WritingSystemFactory;
			m_vc = vc;
			m_hvoHeader = hvoHeader;
		}

		#endregion

		#region IDisposable override

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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
			m_sda = null; // This comes from an FdoCache, so just turn loose of it.
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the PubHeader hvo used to get the Header/Footer information to display in the
		/// view.
		/// NOTE: after this is set, MakeRoot() and PerformLayout() need to be called to
		/// refresh the view display
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Header
		{
			set
			{
				CheckDisposed();

				m_hvoHeader = value;
				if (m_rootb != null)
				{
					m_rootb.SetRootObject(m_hvoHeader, m_vc, HeaderFooterVc.kfragPageHeaderFooter,
						null);
					m_rootb.Reconstruct();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the page number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageNumber
		{
			get
			{
				CheckDisposed();
				return m_vc.CurrentPage.PageNumber;
			}
			set
			{
				CheckDisposed();
				m_vc.CurrentPage.PageNumber = value;
			}
		}

		#endregion

		#region Overrides of SimpleRootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this so that we can tell the view that we are ready to show
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool GotCacheOrWs
		{
			get
			{
				CheckDisposed();
				return m_sda != null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the rootbox for the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;

			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_sda;
			m_rootb.SetRootObject(m_hvoHeader, m_vc, HeaderFooterVc.kfragPageHeaderFooter, null);
			HorizMargin = 10;
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles OnKeyPress to remove any formatting before passing the keystroke along. This
		/// prevents the gray background used for ORC strings from bleeding over into literal
		/// text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (EditingHelper == null)
				return;

			IVwSelection vwsel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			if (!EditingHelper.GetCharacterProps(out vwsel, out vttp, out vvps))
				return;

			EditingHelper.RemoveCharFormatting(vwsel, ref vttp, null, true);
			base.OnKeyPress(e);
		}

		#endregion
	}
}
