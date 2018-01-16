// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class StTextView : RootSiteControl
	{
		private StVc m_vc;
		private IStText m_text;

		/// <summary>
		/// Gets or sets the StText object.
		/// </summary>
		/// <value>The StText object.</value>
		public IStText StText
		{
			get
			{
				CheckDisposed();
				return m_text;
			}

			set
			{
				CheckDisposed();
				var oldText = m_text;
				m_text = value;
				if (m_rootb != null && m_text != null && oldText != m_text)
				{
					m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
				}
			}
		}

		/// <summary>
		/// Select at the specified position in the first paragraph.
		/// </summary>
		internal void SelectAt(int ich)
		{
			try
			{
				var vsli = new SelLevInfo[1];
				vsli[0].tag = StTextTags.kflidParagraphs;
				vsli[0].ihvo = 0;
				RootBox.MakeTextSelection(0, 1, vsli, StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null, true);
			}
			catch (Exception)
			{
				Debug.Assert(false, "Unexpected failure to make selection in StTextView");
			}

		}

		public void Init(int ws)
		{
			CheckDisposed();
			Cache = PropertyTable.GetValue<LcmCache>("cache");
			StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
			m_vc = new StVc("Normal", ws)
			{
				Cache = m_cache,
				Editable = true
			};
			DoSpellCheck = true;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_text != null)
			{
				m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
				m_rootb.Reconstruct();
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
			m_vc = null;

			// Dispose unmanaged resources here, whether disposing is true or false.
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
				return;

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;
			if (m_text != null)
			{
				m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
			}

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot))
			{
				return true;
			}
			var mainWind = ParentForm as IFwMainWnd;
			var sel = RootBox?.Selection;
			if (mainWind == null || sel == null)
			{
				return false;
			}
#if RANDYTODO
			mainWind.ShowContextMenu("mnuStTextChoices", new Point(Cursor.Position.X, Cursor.Position.Y), null, null);
#endif
			return true;
		}
	}
}