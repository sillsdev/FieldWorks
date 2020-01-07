// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Used to display interlinear text from a ConstChartWordGroup in a dialog.
	/// </summary>
	internal class DialogInterlinRibbon : InterlinRibbon
	{
		/// <summary>
		/// In this subclass, we set the root later.
		/// </summary>
		public DialogInterlinRibbon(LcmCache cache) : base(cache, 0)
		{
			m_occurenceListId = -2012; // use a different flid for this subclass
		}

		public override int OccurenceListId => m_occurenceListId;

		public override void MakeInitialSelection()
		{
			SelectUpToEnd();
		}

		private void SelectUpToEnd()
		{
			SelectUpTo(Decorator.get_VecSize(HvoRoot, OccurenceListId) - 1);
		}

		/// <summary>
		/// This override ensures that we always have whole objects selected.
		/// Enhance: it may cause flicker during drag, in which case, we may change to only do it on mouse up,
		/// or only IF the mouse is up.
		/// </summary>
		protected override void HandleSelectionChange(object sender, VwSelectionArgs args)
		{
			if (m_InSelectionChanged || RootBox.Selection == null)
			{
				return;
			}
			var info = new TextSelInfo(RootBox);
			var end = Math.Max(info.ContainingObjectIndex(info.Levels(true) - 1, true), info.ContainingObjectIndex(info.Levels(false) - 1, false));
			var begin = Math.Min(info.ContainingObjectIndex(info.Levels(true) - 1, true), info.ContainingObjectIndex(info.Levels(false) - 1, false));
			SelectRange(begin, end);
		}

		private void SelectRange(int begin1, int end1)
		{
			if (HvoRoot == 0)
			{
				return;
			}
			var end = Math.Min(end1, Decorator.get_VecSize(HvoRoot, OccurenceListId) - 1);
			var begin = Math.Min(begin1, end);
			if (end < 0 || begin < 0)
			{
				return;
			}
			try
			{
				m_InSelectionChanged = true;
				var levelsA = new SelLevInfo[1];
				levelsA[0].ihvo = begin;
				levelsA[0].tag = OccurenceListId;
				var levelsE = new SelLevInfo[1];
				levelsE[0].ihvo = end;
				levelsE[0].tag = OccurenceListId;
				RootBox.MakeTextSelInObj(0, levelsA.Length, levelsA, levelsE.Length, levelsE, false, false, false, true, true);
			}
			finally
			{
				m_InSelectionChanged = false;
			}
		}
	}
}