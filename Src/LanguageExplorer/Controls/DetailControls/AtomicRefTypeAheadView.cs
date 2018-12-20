// Copyright (c) 2005-2019 SIL International
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
		private AtomicRefTypeAheadVc m_vc;

		public AtomicRefTypeAheadView(int hvo, int flid)
		{
			m_hvoObj = hvo;
			m_flid = flid;
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

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
			m_vc.TasVc.SelectionChanged(prootb, vwselNew);
			base.HandleSelectionChange(prootb, vwselNew);
		}


		protected override void OnLostFocus(EventArgs e)
		{
			m_vc.TasVc.OnLostFocus(RootBox);
			base.OnLostFocus(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			m_vc.TasVc.OnGotFocus(RootBox);
			base.OnGotFocus(e);
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			m_vc = new AtomicRefTypeAheadVc(m_flid, m_cache);
			RootBox.DataAccess = m_cache.DomainDataByFlid;
			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			RootBox.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
		}
	}
}