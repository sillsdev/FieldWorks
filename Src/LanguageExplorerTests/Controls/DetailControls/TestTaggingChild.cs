// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;

namespace LanguageExplorerTests.Controls.DetailControls
{
	/// <summary>
	/// So that tagging tests can see protected methods in IText.InterlinTaggingChild
	/// </summary>
	internal sealed class TestTaggingChild : InterlinTaggingChild
	{
		internal TestTaggingChild(LcmCache cache)
		{
			Cache = cache;
			m_tagFact = cache.ServiceLocator.GetInstance<ITextTagFactory>();
			m_segRepo = cache.ServiceLocator.GetInstance<ISegmentRepository>();
		}

		#region Protected methods to test

		internal void CallMakeContextMenuForTags(ContextMenuStrip menu, ICmPossibilityList list)
		{
			MakeContextMenu_AvailableTags(menu, list);
		}

		internal ITextTag CallMakeTextTagInstance(ICmPossibility tagPoss)
		{
			return MakeTextTagInstance(tagPoss);
		}

		internal void CallDeleteTextTags(ISet<ITextTag> tagsToDelete)
		{
			DeleteTextTags(tagsToDelete);
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		protected override void CacheTagString(ITextTag ttag)
		{
			// dummy method
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		protected override void CacheNullTagString(ITextTag ttag)
		{
			// dummy method
		}

		#endregion

		/// <summary>
		/// The 'real' TaggingChild sets this in MakeVc()
		/// </summary>
		internal void SetText(IStText txt)
		{
			RootStText = txt;
		}
	}
}