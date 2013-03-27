using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This class displays an atomic reference property. Currently it must be for a property for which
	/// where ReferenceTargetCandidates returns a useful list of results.
	/// </summary>
	public class AtomicRefTypeAheadSlice : ViewPropertySlice
	{
		public AtomicRefTypeAheadSlice(ICmObject obj, int flid) : base(new AtomicRefTypeAheadView(obj.Hvo, flid), obj, flid)
		{
		}

		#region View Constructors

		public class AtomicRefTypeAheadVc : FwBaseVc
		{
			TypeAheadSupportVc m_tasvc;

			public AtomicRefTypeAheadVc(int flid, FdoCache cache)
			{
				m_tasvc = new TypeAheadSupportVc(flid, cache);
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				m_tasvc.Insert(vwenv, hvo);
			}

			public TypeAheadSupportVc TasVc
			{
				get { return m_tasvc; }
			}
		}

		#endregion // View Constructors

		#region RootSite implementation

		class AtomicRefTypeAheadView : RootSiteControl
		{
			int m_hvoObj;
			int m_flid;
			AtomicRefTypeAheadVc m_vc = null;

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

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Give type-ahead helper a chance to handle keypress.
			/// </summary>
			/// <param name="e"></param>
			/// -----------------------------------------------------------------------------------
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				using (new HoldGraphics(this))
				{
					if (m_vc.TasVc.OnKeyPress(EditingHelper, e, ModifierKeys, m_graphicsManager.VwGraphics))
						return;
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
				base.OnGotFocus (e);
			}

			public override void MakeRoot()
			{
				CheckDisposed();
				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;
				m_vc = new AtomicRefTypeAheadVc(m_flid, m_fdoCache);

				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;

				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
			}
		}

		#endregion // RootSite implementation
	}
}
