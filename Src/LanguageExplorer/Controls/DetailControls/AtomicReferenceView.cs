// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Main class for displaying the AtomicReferenceSlice.
	/// </summary>
	internal class AtomicReferenceView : ReferenceViewBase
	{
		#region Constants and data members

		// View frags.
		public const int kFragAtomicRef = 1;
		public const int kFragObjName = 2;

		protected AtomicReferenceVc m_atomicReferenceVc;
		// this is used to guarantee correct initial size.
		private int m_hOld;
		protected string m_displayWs;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		internal event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public AtomicReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
			}
			m_atomicReferenceVc = null;
			m_rootObj = null;
			m_displayNameProperty = null;
		}

		public void Initialize(ICmObject rootObj, int rootFlid, string rootFieldName, LcmCache cache, string displayNameProperty, string displayWs)
		{
			m_displayWs = displayWs;
			Initialize(rootObj, rootFlid, rootFieldName, cache, displayNameProperty);
		}

		/// <summary>
		/// Set the item from the chooser.
		/// </summary>
		public void SetObject(ICmObject obj)
		{
			m_rootObj = obj;
			if (RootBox == null)
			{
				return;
			}
			SetRootBoxObj();
			var h2 = RootBox.Height;
			if (m_hOld == h2)
			{
				return;
			}
			m_hOld = h2;
			ViewSizeChanged?.Invoke(this, new FwViewSizeEventArgs(h2, RootBox.Width));
		}

		public ICmObject Object => m_rootObj;

		protected virtual void SetRootBoxObj()
		{
			if (m_rootObj == null || !m_rootObj.IsValidObject)
			{
				return;
			}
			// The ViewSizeChanged logic should be triggered automatically by a notification
			// from the rootbox.
			var h1 = RootBox.Height;
			RootBox.SetRootObject(m_rootObj.Hvo, m_atomicReferenceVc, kFragAtomicRef, RootBox.Stylesheet);
			if (h1 == RootBox.Height)
			{
				return;
			}

			ViewSizeChanged?.Invoke(this, new FwViewSizeEventArgs(RootBox.Height, RootBox.Width));
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		public void FinishInit(XElement configurationNode)
		{
			var textStyle = configurationNode.Attribute("textStyle");
			if (textStyle == null)
			{
				return;
			}
			TextStyle = textStyle.Value;
			if (m_atomicReferenceVc != null)
			{
				m_atomicReferenceVc.TextStyle = textStyle.Value;
			}
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			base.MakeRoot();

			if (m_cache == null || DesignMode)
			{
				return;
			}

			SetReferenceVc();
			RootBox.DataAccess = GetDataAccess();
			SetRootBoxObj();
		}

		protected virtual ISilDataAccess GetDataAccess()
		{
			return m_cache.DomainDataByFlid;
		}

		public virtual void SetReferenceVc()
		{
			m_atomicReferenceVc = new AtomicReferenceVc(m_cache, m_rootFlid, m_displayNameProperty);
		}

		#endregion // RootSite required methods

		#region other overrides

		/// <summary>
		/// Called when the focus leaves the control. We want to hide the selection.
		/// </summary>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			RootBox?.DestroySelection();
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			if (vwselNew == null)
			{
				return;
			}
			var cvsli = vwselNew.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			if (cvsli == 0)
			{
				// No objects in selection: don't allow a selection.
				RootBox.DestroySelection();
				// Enhance: invoke launcher's selection dialog.
				return;
			}
			ITsString tss;
			int ichAnchor;
			int ichEnd;
			bool fAssocPrev;
			int hvoObj;
			int hvoObjEnd;
			int tag;
			int ws;
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd, out tag, out ws);
			if (hvoObj != hvoObjEnd)
			{
				return;
			}
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ihvoEnd;
			ITsTextProps ttp;
			var rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli, out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd, out ttp);
			Debug.Assert(RootBox != null);
			// Create a selection that covers the entire target object.  If it differs from
			// the new selection, we'll install it (which will recurse back to this method).
			var vwselWhole = RootBox.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null, false, false, false, true, false);
			if (vwselWhole == null)
			{
				return;
			}
			ITsString tssWhole;
			int ichAnchorWhole;
			int ichEndWhole;
			int hvoObjWhole;
			int hvoObjEndWhole;
			bool fAssocPrevWhole;
			int tagWhole;
			int wsWhole;
			vwselWhole.TextSelInfo(false, out tssWhole, out ichAnchorWhole, out fAssocPrevWhole, out hvoObjWhole, out tagWhole, out wsWhole);
			vwselWhole.TextSelInfo(true, out tssWhole, out ichEndWhole, out fAssocPrevWhole, out hvoObjEndWhole, out tagWhole, out wsWhole);
			if (hvoObj == hvoObjWhole && hvoObjEnd == hvoObjEndWhole && (ichAnchor != ichAnchorWhole || ichEnd != ichEndWhole))
			{
				// Install it this time!
				RootBox.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null, false, false, false, true, true);
			}
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// AtomicReferenceView
			//
			this.Name = "AtomicReferenceView";
			this.Size = new System.Drawing.Size(232, 18);
		}
		#endregion
	}
}