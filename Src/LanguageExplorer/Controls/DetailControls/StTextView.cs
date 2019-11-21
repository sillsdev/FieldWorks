// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class StTextView : RootSiteControl
	{
		private StVc m_vc;
		private IStText m_text;
		private ISharedEventHandlers _sharedEventHandlers;
		private ContextMenuStrip _contextMenuStrip;

		internal StTextView(ISharedEventHandlers sharedEventHandlers)
		{
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));

			_sharedEventHandlers = sharedEventHandlers;
		}

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
			Cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
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
				_contextMenuStrip?.Dispose();
			}
			m_vc = null;
			_sharedEventHandlers = null;
			_contextMenuStrip = null;

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

		protected override bool DoContextMenu(IVwSelection invSel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			var mainWind = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			var sel = RootBox?.Selection;
			if (mainWind == null || sel == null)
			{
				return false;
			}
			if (_contextMenuStrip == null)
			{
				// Start: <menu id="mnuStTextChoices">
				const string mnuStTextChoices = "mnuStTextChoices";
				_contextMenuStrip = new ContextMenuStrip
				{
					Name = mnuStTextChoices
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);
				/*
				_sharedEventHandlers should have the cut, copy & paste handlers.
				<menu id="mnuStTextChoices">
					<item command="CmdCut" />
					<item command="CmdCopy" />
					<item command="CmdPaste" />
					<item label="-" translate="do not translate" />
					<item command="CmdLexiconLookup" />
					<item command="CmdAddToLexicon" />
				</menu>
				*/
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, _contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdCut), LanguageExplorerResources.Cut);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, _contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdCopy), LanguageExplorerResources.Copy);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, _contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdPaste), LanguageExplorerResources.Paste);
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(_contextMenuStrip);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, _contextMenuStrip, LexiconLookup_Clicked, LanguageExplorerResources.Find_in_Dictionary);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, _contextMenuStrip, AddToLexicon_Clicked, LanguageExplorerResources.Entry);
			}
			_contextMenuStrip.Show(this, pt);

			return true;
		}

		private void LexiconLookup_Clicked(object sender, EventArgs e)
		{
			((StTextSlice)Parent).LexiconLookup();
		}

		private void AddToLexicon_Clicked(object sender, EventArgs e)
		{
			((StTextSlice)Parent).AddToLexicon();
		}
	}
}