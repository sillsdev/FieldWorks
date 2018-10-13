// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// The SbWord object has no Pos set.
	/// </summary>
	internal class IhMissingWordPos : InterlinComboHandler
	{
		POSPopupTreeManager m_pOSPopupTreeManager;

		internal PopupTree Tree { get; private set; }

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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_pOSPopupTreeManager != null)
				{
					m_pOSPopupTreeManager.AfterSelect -= m_pOSPopupTreeManager_AfterSelect;
					m_pOSPopupTreeManager.Dispose();
				}
				if (Tree != null)
				{
					Tree.Load -= m_tree_Load;
					Tree.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_pOSPopupTreeManager = null;
			Tree = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void SetupCombo()
		{
			Tree = new PopupTree
			{
				// Try a bigger size here only for Sandbox POS editing (GordonM) [LT-7529]
				// Enhance: It would be better to know what size we need for the data,
				// but it gets displayed before we know what data goes in it!
				// PopupTree.DefaultSize was (120, 200)
				Size = new Size(180, 220)
			};
			// Handle AfterSelect events through POSPopupTreeManager in m_tree_Load().
			Tree.Load += m_tree_Load;
		}
		public override void Activate(Rect loc)
		{
			if (Tree == null)
			{
				base.Activate(loc);
			}
			else
			{
				Tree.Launch(m_sandbox.RectangleToScreen(loc), Screen.GetWorkingArea(m_sandbox));
			}
		}

		// This indicates there was not a previous real word POS recorded. The 'real' subclass
		// overrides to answer 1. The value signifies the number of objects stored in the
		// ktagSbWordPos property before the user made a selection in the menu.
		internal virtual int WasReal()
		{
			return 0;
		}

		public override List<int> Items
		{
			get
			{
				LoadItemsIfNeeded();
				return base.Items;
			}
		}

		private void LoadItemsIfNeeded()
		{
			var items = new List<int>();
			if (m_pOSPopupTreeManager != null && m_pOSPopupTreeManager.IsTreeLoaded)
			{
				return;
			}
			m_tree_Load(null, null);
			m_items = null;
			// not sure if this is guarranteed to be in the same order each time, but worth a try.
			items.AddRange(m_caches.MainCache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.Select(possibility => possibility.Hvo));
			m_items = items;
		}

		public override int IndexOfCurrentItem
		{
			get
			{
				LoadItemsIfNeeded();
				// get currently selected item.
				var hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, SandboxBase.ktagSbWordPos));
				// look it up in the items.
				return Items.IndexOf(hvoLastCategory);
			}
		}

		public override void HandleSelect(int index)
		{
			var hvoPos = Items[index];
			var possibility = m_caches.MainCache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoPos);

			// Called only if it's a combo box.
			SelectItem(Items[index], possibility.Name.BestVernacularAnalysisAlternative.Text);
		}

		// We can't add the items until the form loads, or we get a spurious horizontal scroll bar.
		private void m_tree_Load(object sender, EventArgs e)
		{
			if (m_pOSPopupTreeManager == null)
			{
				var cache = m_caches.MainCache;
				m_pOSPopupTreeManager = new POSPopupTreeManager(Tree, cache, cache.LangProject.PartsOfSpeechOA, cache.DefaultAnalWs, false, m_sandbox.PropertyTable, m_sandbox.Publisher, m_sandbox.FindForm());
				m_pOSPopupTreeManager.AfterSelect += m_pOSPopupTreeManager_AfterSelect;
			}
			m_pOSPopupTreeManager.LoadPopupTree(m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, SandboxBase.ktagSbWordPos)));
		}

		private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// we only want to actually select the item if we have clicked on it
			// or if we are simulating a click (e.g. by pressing Enter).
			if (!m_fUnderConstruction && e.Action == TreeViewAction.ByMouse)
			{
				SelectItem((e.Node as HvoTreeNode).Hvo, e.Node.Text);
			}
		}

		internal void SelectItem(int hvo, string label)
		{
			// if we haven't changed the selection, we don't need to change anything in the cache.
			var hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, SandboxBase.ktagSbWordPos));
			if (hvoLastCategory == hvo)
			{
				return;
			}
			var hvoPos = 0;
			if (hvo > 0)
			{
				var tssAbbr = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvo, CmPossibilityTags.kflidName, m_caches.MainCache.DefaultAnalWs);
				m_caches.FindOrCreateSec(hvo, SandboxBase.kclsidSbNamedObj, m_hvoSbWord, SandboxBase.ktagSbWordDummy, SandboxBase.ktagSbNamedObjName, tssAbbr);
				hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvo, CmPossibilityTags.kflidAbbreviation);
				m_caches.DataAccess.SetInt(hvoPos, SandboxBase.ktagSbNamedObjGuess, 0);
			}
			m_caches.DataAccess.SetObjProp(m_hvoSbWord, SandboxBase.ktagSbWordPos, hvoPos);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordPos, 0, 1, WasReal());
			m_sandbox.SelectIcon(SandboxBase.ktagWordPosIcon);
		}
	}
}