using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.IText;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;

namespace ITextDllTests
{
	/// <summary>
	/// So that tagging tests can see protected methods in IText.InterlinTaggingChild
	/// </summary>
	class TestTaggingChild: InterlinTaggingChild
	{
		public TestTaggingChild(FdoCache cache)
		{
			Cache = cache;
			m_textTagAnnDefn = CmAnnotationDefn.TextMarkupTag(Cache).Hvo;
			m_twficAnnDefn = CmAnnotationDefn.Twfic(Cache).Hvo;
		}

		/// <summary>
		/// This test version hides the InterlinTaggingChild version, but only allows
		/// tests to set the SelectedWfics property without messing with actual selections.
		/// </summary>
		public new List<int> SelectedWfics
		{
			get { return base.SelectedWfics; }
			set { m_selectedWfics = value; }
		}

		#region Protected methods to test

		internal void CallMakeContextMenuForTags(ContextMenuStrip menu, ICmPossibilityList list)
		{
			MakeContextMenu_AvailableTags(menu, list);
		}

		internal ICmIndirectAnnotation CallMakeTextTagAnnotation(int hvoTagPoss)
		{
			return MakeTextTagAnnotation(hvoTagPoss);
		}

		internal void CallDeleteTextTagAnnotations(Set<int> hvosToDelete)
		{
			DeleteTextTagAnnotations(hvosToDelete);
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		/// <param name="tagAnn"></param>
		protected override Set<int> CacheTagString(ICmIndirectAnnotation tagAnn)
		{
			return null;
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		/// <param name="tagAnn"></param>
		protected override void CacheNullTagString(int hvoTag)
		{
		}

		/// <summary>
		/// The test version doesn't want anything to do with views (which the superclass version does).
		/// </summary>
		/// <param name="wficCollection"></param>
		protected override void UpdateAffectedBundles(Set<int> wficCollection)
		{
		}

		#endregion

	}
}
