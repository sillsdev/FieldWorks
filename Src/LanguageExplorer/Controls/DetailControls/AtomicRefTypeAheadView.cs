// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class AtomicRefTypeAheadView : RootSiteControl
	{
		private readonly int m_hvoObj;
		private readonly int m_flid;
		AtomicRefTypeAheadVc m_vc;

		public AtomicRefTypeAheadView(int hvo, int flid)
		{
			m_hvoObj = hvo;
			m_flid = flid;
		}

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
		}

		#endregion IDisposable override

		/// <summary>
		/// Give type-ahead helper a chance to handle keypress.
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			using (new HoldGraphics(this))
			{
				if (m_vc.TasVc.OnKeyPress(EditingHelper, e, ModifierKeys, m_graphicsManager.VwGraphics))
				{
					return;
				}
			}
			base.OnKeyPress(e);
		}

		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			m_vc.TasVc.SelectionChanged(prootb, vwselNew);
			base.HandleSelectionChange(prootb, vwselNew);
		}


		protected override void OnLostFocus(EventArgs e)
		{
			m_vc.TasVc.OnLostFocus(m_rootb);
			base.OnLostFocus(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			m_vc.TasVc.OnGotFocus(RootBox);
			base.OnGotFocus(e);
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_vc = new AtomicRefTypeAheadVc(m_flid, m_cache);
			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
		}
	}
}