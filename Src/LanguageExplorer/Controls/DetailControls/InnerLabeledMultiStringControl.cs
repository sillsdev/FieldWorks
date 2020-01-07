// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class InnerLabeledMultiStringControl : SimpleRootSite
	{
		private LabeledMultiStringVc m_vc;
		private LcmCache m_realCache; // real one we get writing system info from
		private ISilDataAccess m_sda; // one actually used in the view.
		internal const int khvoRoot = -3045; // arbitrary but recognizeable numbers for debugging.
		internal const int kflid = 4554;

		public InnerLabeledMultiStringControl(LcmCache cache, int wsMagic)
		{
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			WritingSystems = WritingSystemServices.GetWritingSystemList(cache, wsMagic, 0, false);
			AutoScroll = true;
			IsTextBox = true;   // range selection not shown when not in focus
		}

		public InnerLabeledMultiStringControl(LcmCache cache, List<CoreWritingSystemDefinition> wsList)
		{
			// Ctor for use with a non-standard list of wss (like available UI languages)
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			WritingSystems = wsList;
			AutoScroll = true;
			IsTextBox = true;   // range selection not shown when not in focus
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_realCache = null;
			WritingSystems = null;
			m_vc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystems { get; private set; }

		/// <summary></summary>
		public override void MakeRoot()
		{
			if (DesignMode)
			{
				return;
			}
			// The simple root site won't lay out properly until this is done.
			// It needs to be done before base.MakeRoot or it won't lay out at all ever!
			WritingSystemFactory = m_realCache.WritingSystemFactory;
			base.MakeRoot();
			RootBox.DataAccess = m_sda;
			var wsUser = m_realCache.ServiceLocator.WritingSystemManager.UserWs;
			var wsEn = m_realCache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			m_vc = new LabeledMultiStringVc(kflid, WritingSystems, wsUser, true, wsEn);
			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			RootBox.SetRootObject(khvoRoot, m_vc, 1, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
		}

		/// <summary>
		/// User pressed a key.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!m_editingHelper.HandleOnKeyDown(e))
			{
				base.OnKeyDown(e);
			}
			if (!e.Handled && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
			{
				MultiStringSelectionUtils.HandleUpDownArrows(e, RootBox, EditingHelper.CurrentSelection, WritingSystems, kflid);
			}
		}

		internal ITsString Value(int ws)
		{
			return m_sda.get_MultiStringAlt(khvoRoot, kflid, ws);
		}

		internal void SetValue(int ws, ITsString tss)
		{
			m_sda.SetMultiStringAlt(khvoRoot, kflid, ws, tss);
		}
	}
}