// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal class ConcOccurrenceBrowseView : RecordBrowseView
	{
		int m_hvoSelectedOccurrence; // dummy HVO for occurrence, only understood by ConcSda
		XmlView m_previewPane;
		private ISilDataAccess m_decoratedSda; // typically a ConcSda, understands the segment property of the fake HVO.

		/// <summary />
		internal void Init(XmlView pubView, ISilDataAccess sda)
		{
			m_previewPane = pubView;
			m_decoratedSda = sda;
		}

		/// <summary />
		public override void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			PreviewCurrentSelection(e.Hvo);
			base.OnSelectionChanged(sender, e);
		}

		/// <summary />
		protected override void ShowRecord()
		{
			if (!m_fullyInitialized || m_suppressShowRecord)
				return;
			if (Clerk == null || Clerk.CurrentObjectHvo == 0)
				return;
			PreviewCurrentSelection(Clerk.CurrentObjectHvo);
			base.ShowRecord();
		}

		private void PreviewCurrentSelection(int hvoOccurrence)
		{
			if (m_hvoSelectedOccurrence == hvoOccurrence)
				return;
			m_hvoSelectedOccurrence = hvoOccurrence;
			m_previewPane.RootObjectHvo = m_decoratedSda.get_ObjectProp(m_hvoSelectedOccurrence, ConcDecorator.kflidSegment);
		}
	}
}