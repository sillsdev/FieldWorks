using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// So that tagging tests can see protected methods in IText.InterlinTaggingChild
	/// </summary>
	class TestTaggingChild: InterlinTaggingChild
	{
		public TestTaggingChild(FdoCache cache)
		{
			Cache = cache;
			m_tagFact = cache.ServiceLocator.GetInstance<ITextTagFactory>();
			m_segRepo = cache.ServiceLocator.GetInstance<ISegmentRepository>();
		}

		/// <summary>
		/// This test version hides the InterlinTaggingChild version, but only allows
		/// tests to set the SelectedWordforms property without messing with actual selections.
		/// </summary>
		public new List<AnalysisOccurrence> SelectedWordforms
		{
			get { return m_selectedWordforms; }
			set { m_selectedWordforms = value; }
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

		internal void CallDeleteTextTags(Set<ITextTag> tagsToDelete)
		{
			DeleteTextTags(tagsToDelete);
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		/// <param name="ttag"></param>
		protected override void CacheTagString(ITextTag ttag)
		{
			// dummy method
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		/// <param name="ttag"></param>
		protected override void CacheNullTagString(ITextTag ttag)
		{
			// dummy method
		}

		#endregion

		/// <summary>
		/// The 'real' TaggingChild sets this in MakeVc()
		/// </summary>
		/// <param name="txt"></param>
		internal void SetText(IStText txt)
		{
			RootStText = txt;
		}
	}
}
