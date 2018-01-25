// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Helper for keeping track of our location in the text when switching from and back to the
	/// Texts area (cf. LT-1543).  It also serves to keep our place when switching between
	/// RawTextPane (Baseline), GlossPane, AnalyzePane(Interlinearizer), TaggingPane, PrintPane and ConstChartPane.
	/// </summary>
	public class InterAreaBookmark : IStTextBookmark
	{
		private IPropertyTable m_propertyTable;
		private string m_bookmarkId;

		internal InterAreaBookmark()
		{
		}

		internal InterAreaBookmark(InterlinMaster interlinMaster, LcmCache cache, IPropertyTable propertyTable)	// For restoring
		{
			// Note: resist any temptation to save mediator in a memer variable. Bookmarks are kept in a static dictionary
			// and may well have a longer life than the mediator. There is danger of using if after it is disposed. See LT-12435.
			Init(interlinMaster, cache, propertyTable);
			Restore(interlinMaster.IndexOfTextRecord);
		}

		internal void Init(InterlinMaster interlinMaster, LcmCache cache, IPropertyTable propertyTable)
		{
			Debug.Assert(interlinMaster != null);
			Debug.Assert(cache != null);
			Debug.Assert(propertyTable != null);
			m_propertyTable = propertyTable;
		}

		/// <summary>
		/// Saves the given AnalysisOccurrence in the InterlinMaster.
		/// </summary>
		public void Save(AnalysisOccurrence point, bool fPersistNow, int index)
		{
			if (point == null || !point.IsValid)
			{
				Reset(index); // let's just reset for an empty location.
				return;
			}
			var iParaInText = point.Segment.Paragraph.IndexInOwner;
			var begOffset = point.Segment.GetAnalysisBeginOffset(point.Index);
			var endOffset = point.HasWordform ? begOffset + point.BaselineText.Length : begOffset;

			Save(index, iParaInText, begOffset, endOffset, fPersistNow);
		}

		/// <summary>
		/// Saves the current selected annotation in the InterlinMaster.
		/// </summary>
		public void Save(bool fPersistNow, int index)
		{
			if (fPersistNow)
			{
				SavePersisted(index);
			}
		}

		internal void Save(int textIndex, int paragraphIndex, int beginCharOffset, int endCharOffset, bool fPersistNow)
		{
			IndexOfParagraph = paragraphIndex;
			BeginCharOffset = beginCharOffset;
			EndCharOffset = endCharOffset;
			TextIndex = textIndex;
			Save(fPersistNow, textIndex);
		}

		private string BookmarkNamePrefix
		{
			get
			{
				var result = "ITexts-Bookmark-" + m_bookmarkId + "-";
				return result;
			}
		}

		internal string RecordIndexBookmarkName => BookmarkPropertyName("IndexOfRecord");

		private string BookmarkPropertyName(string attribute)
		{
			return BookmarkNamePrefix + attribute;
		}

		private void SavePersisted(int recordIndex)
		{
			m_propertyTable.SetProperty(RecordIndexBookmarkName, recordIndex, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("IndexOfParagraph"), IndexOfParagraph, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("CharBeginOffset"), BeginCharOffset, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("CharEndOffset"), EndCharOffset, SettingsGroup.LocalSettings, true, false);
		}

		/// <summary>
		/// Restore the InterlinMaster bookmark to its previously saved state.
		/// </summary>
		public void Restore(int index)
		{
			// verify we're restoring to the right text. Is there a better way to verify this?
			var restoredRecordIndex = m_propertyTable.GetValue(RecordIndexBookmarkName, SettingsGroup.LocalSettings, -1);
			if (index != restoredRecordIndex)
			{
				return;
			}
			IndexOfParagraph = m_propertyTable.GetValue(BookmarkPropertyName("IndexOfParagraph"), SettingsGroup.LocalSettings, 0);
			BeginCharOffset = m_propertyTable.GetValue(BookmarkPropertyName("CharBeginOffset"), SettingsGroup.LocalSettings, 0);
			EndCharOffset = m_propertyTable.GetValue(BookmarkPropertyName("CharEndOffset"), SettingsGroup.LocalSettings, 0);
		}

		/// <summary>
		/// Reset the bookmark to its default values.
		/// </summary>
		public void Reset(int index)
		{
			IndexOfParagraph = 0;
			BeginCharOffset = 0;
			EndCharOffset = 0;

			SavePersisted(index);
		}

		#region IStTextBookmark
		public int IndexOfParagraph { get; private set; }

		public int BeginCharOffset { get; private set; }
		public int EndCharOffset { get; private set; }

		public int TextIndex { get; private set; }

		#endregion
	}
}