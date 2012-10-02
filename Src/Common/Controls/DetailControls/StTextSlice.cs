using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// An StTextSlice implements the sttext editor type for atomic attributes whose value is an StText.
	/// The resulting view allows the editing of the text, including creating and destroying (and splitting
	/// and merging) of the paragraphs using the usual keyboard actions.
	/// </summary>
	public class StTextSlice : ViewSlice
	{
		public StTextSlice(int hvoOwner, int flid, int ws) : base(new StTextView(hvoOwner, flid, ws))
		{
			((StTextView)Control).Slice = this;
		}

		protected internal override void SetWidthForDataTreeLayout(int width)
		{
			CheckDisposed();

			if (Width == width)
				return; // Nothing to do.

			base.SetWidthForDataTreeLayout(width);

			if (RootSite.RootBox == null)
				RootSite.MakeRoot();
		}
	}

	#region RootSite class

	public class StTextView : RootSite
	{
		StVc m_vc;
		int m_hvoStText;
		int m_hvoOwner;
		int m_flid; // of atomic attr that holds StText.
		int m_ws; // default ws for new StTexts, or 0 for default, which is analysis.
		StTextSlice m_slice;

		public StTextView(int hvoOwner, int flid, int ws) : base(null)
		{
			m_hvoOwner = hvoOwner;
			m_flid = flid;
			m_ws = ws;
		}

		public StTextSlice Slice
		{
			get
			{
				CheckDisposed();
				return m_slice;
			}
			set
			{
				CheckDisposed();
				m_slice = value;
			}
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();
			}
			m_vc = null;
			m_slice = null;

			// Dispose unmanaged resources here, whether disposing is true or false.
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode || m_slice.ContainingDataTree == null)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			if (m_ws == 0)
				m_ws = m_fdoCache.DefaultAnalWs;

			m_hvoStText = m_fdoCache.GetObjProperty(m_hvoOwner, m_flid);
			// If we don't already have an StText in this field, make one now.
			if (m_hvoStText == 0)
			{
				ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
				// Create one and initialize it. Don't use the FdoCache method, because it's important NOT to notify
				// our own data tree of the change...if we do, it could start a new regenerate in the middle of an
				// existing one with bad consequences.
				m_hvoStText = sda.MakeNewObject(StText.kclsidStText, m_hvoOwner, m_flid, -2);
				int hvoStTxtPara = sda.MakeNewObject(StTxtPara.kclsidStTxtPara, m_hvoStText, (int)StText.StTextTags.kflidParagraphs, 0);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				sda.SetString(hvoStTxtPara, (int) StTxtPara.StTxtParaTags.kflidContents, tsf.MakeString("", m_ws));
				// Notify change on the main property. The other properties we changed are part of the new object, and nothing
				// can know their previous state and need to see the change.
				sda.PropChanged(m_slice.ContainingDataTree, (int)PropChangeType.kpctNotifyAllButMe, m_hvoOwner, m_flid, 0, 1, 0);
			}

			m_vc = new StVc("Normal", m_ws);
			m_vc.Cache = m_fdoCache;
			m_vc.Editable = true;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_hvoStText, m_vc, (int)StTextFrags.kfrText,
				m_styleSheet);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}
	}

	#endregion RootSite class
}
