// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
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
		public IStText StText
		{
			get
			{
				return m_text;
			}
			set
			{
				var oldText = m_text;
				m_text = value;
				if (RootBox != null && m_text != null && oldText != m_text)
				{
					RootBox.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
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
			Cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			m_vc = new StVc("Normal", ws)
			{
				Cache = m_cache,
				Editable = true
			};
			DoSpellCheck = true;
			if (RootBox == null)
			{
				MakeRoot();
			}
			else if (m_text != null)
			{
				RootBox.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
				RootBox.Reconstruct();
			}
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
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
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			RootBox.DataAccess = m_cache.DomainDataByFlid;
			if (m_text != null)
			{
				RootBox.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
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