// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal sealed class StTextDataTree : DataTree
	{
		private InfoPane m_infoPane;

		internal InfoPane InfoPane
		{
			set { m_infoPane = value; }
		}

		internal StTextDataTree(LcmCache cache)
			: base()
		{
			m_cache = cache;
			InitializeBasic(cache, false);
			InitializeComponent();
		}

		protected override void SetDefaultCurrentSlice(bool suppressFocusChange)
		{
			base.SetDefaultCurrentSlice(suppressFocusChange);
			// currently we always want the focus in the first slice by default,
			// since the user cannot control the governing browse view with a cursor.
			if (!suppressFocusChange && CurrentSlice == null)
			{
				FocusFirstPossibleSlice();
			}
		}

		public override void ShowObject(ICmObject root, string layoutName, string layoutChoiceField, ICmObject descendant, bool suppressFocusChange)
		{
			if (m_infoPane != null && m_infoPane.CurrentRootHvo == 0)
			{
				return;
			}
			var showObj = root;
			ICmObject stText;
			if (root.ClassID == CmBaseAnnotationTags.kClassId)  // RecordList is tracking the annotation
			{
				// This pane, as well as knowing how to work with a record list of Texts, knows
				// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
				// a word.
				var cba = (ICmBaseAnnotation)root;
				var cmoPara = cba.BeginObjectRA;
				stText = cmoPara.Owner;
				showObj = stText;
			}
			else
			{
				stText = root;
			}

			if (stText.OwningFlid == TextTags.kflidContents)
			{
				showObj = stText.Owner;
			}
			base.ShowObject(showObj, layoutName, layoutChoiceField, showObj, suppressFocusChange);
		}
	}
}