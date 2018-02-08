// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class XmlSeqSelectionRestorer: SelectionRestorer
	{
		private LcmCache Cache { get; }

		public XmlSeqSelectionRestorer(SimpleRootSite rootSite, LcmCache cache) : base(rootSite)
		{
			Cache = cache;
		}

		/// <summary>
		/// We override the usual selection restoration to
		/// (1) try to restore the full original selection even if read-only (e.g., document view)
		/// (2) try to select a containing object if we can't select the exact same thing
		/// (3) try to select a related object (for lexicon, at least) if we can't select the exact same one.
		/// </summary>
		protected override void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			base.Dispose(fDisposing);

			if (!fDisposing || IsDisposed || m_savedSelection == null || m_rootSite.RootBox.Height <= 0)
			{
				return;
			}

			var wasRange = m_savedSelection.IsRange; // may change during RestoreSelection (ugh!)
			var oldLevels = m_savedSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			var originalRestore = RestoreSelection();
			if (originalRestore != null && IsGoodRestore(originalRestore, wasRange))
			{
				return;
			}
			// Try related objects.
			if (oldLevels.Length == 0)
			{
				return; // paranoia
			}
			var rootLevelInfo = oldLevels[oldLevels.Length - 1];
			var hvoTarget = rootLevelInfo.hvo;
			ICmObject target;
			var sda = m_savedSelection.RootSite.RootBox.DataAccess;
			int rootHvo, frag;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			m_savedSelection.RootSite.RootBox.GetRootObject(out rootHvo, out vc, out frag, out ss);
			var vsliTarget = new[] { rootLevelInfo };
			var chvo = sda.get_VecSize(rootHvo, rootLevelInfo.tag);
			if (Cache.ServiceLocator.ObjectRepository.TryGetObject(hvoTarget, out target) && target is ILexEntry)
			{
				// maybe we can't see it because it has become a subentry.
				var subentry = (ILexEntry) target;
				var componentsEntryRef = subentry.EntryRefsOS.FirstOrDefault(se => se.RefType == LexEntryRefTags.krtComplexForm);
				var root = componentsEntryRef?.PrimaryEntryRoots.FirstOrDefault();
				if (root != null)
				{
					for (var i = 0; i < chvo; i++)
					{
						if (sda.get_VecItem(rootHvo, rootLevelInfo.tag, i) == root.Hvo)
						{
							vsliTarget[0].ihvo = i; // do NOT modify rootLevelInfo instead; it is a struct, so that would not change vsliTarget
							var newSel = m_savedSelection.RootSite.RootBox.MakeTextSelInObj(m_savedSelection.IhvoRoot, 1, vsliTarget, 1, vsliTarget, true, false, true, true, true);
							if (newSel != null)
							{
								return;
							}
							break; // if we found it but for some reason can't select it give up.
						}
					}
				}
			}
			// If all else fails try to make a nearby selection. First see if we can make a later one, or at the same index...
			for (var i = rootLevelInfo.ihvo; i < chvo; i++)
			{
				vsliTarget[0].ihvo = i; // do NOT modify rootLevelInfo instead; it is a struct, so that would not change vsliTarget
				var newSel = m_savedSelection.RootSite.RootBox.MakeTextSelInObj(m_savedSelection.IhvoRoot, 1, vsliTarget, 1, vsliTarget, true, false, true, true, true);
				if (newSel != null)
				{
					return;
				}
			}
			for (var i = rootLevelInfo.ihvo - 1; i > 0; i--)
			{
				vsliTarget[0].ihvo = i; // do NOT modify rootLevelInfo instead; it is a struct, so that would not change vsliTarget
				var newSel = m_savedSelection.RootSite.RootBox.MakeTextSelInObj(m_savedSelection.IhvoRoot, 1, vsliTarget, 1, vsliTarget, true, false, true, true, true);
				if (newSel != null)
				{
					return;
				}
			}
		}

		/// <summary>
		/// Answer true if restored is a good restored selection for m_savedSelection.
		/// Typically any selection derived from it is good, but if we started with a range and ended with an IP,
		/// and the view is read-only, that's no so good. It will disappear in a read-only view.
		/// </summary>
		private bool IsGoodRestore(IVwSelection restored, bool wasRange)
		{
			if (restored.IsRange)
			{
				return true; // can't be a problem
			}
			if (!wasRange)
			{
				return true; // original was an IP, we expect the restored one to be.
			}
			if (m_rootSite.ReadOnlyView)
			{
				return false; // for a read-only view we want a range if at all possible.
			}
			return true; // for an editable view an IP is a reasonable result.
		}
	}
}