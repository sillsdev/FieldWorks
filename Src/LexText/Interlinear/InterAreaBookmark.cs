using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Helper for keeping track of our location in the text when switching from and back to the
	/// Texts area (cf. LT-1543).  It also serves to keep our place when switching between
	/// RawTextPane (Baseline), GlossPane, AnalyzePane(Interlinearizer), TaggingPane, PrintPane and ConstChartPane.
	/// </summary>
	public class InterAreaBookmark : IStTextBookmark
	{
		private IPropertyTable m_propertyTable;
		int m_iParagraph;
		int m_BeginOffset;
		int m_EndOffset;
		private string m_bookmarkId;
		private int m_textIndex;

		internal InterAreaBookmark()
		{
		}

		internal InterAreaBookmark(InterlinMaster interlinMaster, FdoCache cache, IPropertyTable propertyTable)	// For restoring
		{
			// Note: resist any temptation to save mediator in a memer variable. Bookmarks are kept in a static dictionary
			// and may well have a longer life than the mediator. There is danger of using if after it is disposed. See LT-12435.
			Init(interlinMaster, cache, propertyTable);
			Restore(interlinMaster.IndexOfTextRecord);
		}

		internal void Init(InterlinMaster interlinMaster, FdoCache cache, IPropertyTable propertyTable)
		{
			Debug.Assert(interlinMaster != null);
			Debug.Assert(cache != null);
			Debug.Assert(propertyTable != null);
			m_propertyTable = propertyTable;
		}

		/// <summary>
		/// Saves the given AnalysisOccurrence in the InterlinMaster.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		/// <param name="index">The index of the selected text in the list</param>
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
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		/// <param name="index"></param>
		public void Save(bool fPersistNow, int index)
		{
			if (fPersistNow)
				SavePersisted(index);
		}

		internal void Save(int textIndex, int paragraphIndex, int beginCharOffset, int endCharOffset, bool fPersistNow)
		{
			m_iParagraph = paragraphIndex;
			m_BeginOffset = beginCharOffset;
			m_EndOffset = endCharOffset;
			m_textIndex = textIndex;
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

		internal string RecordIndexBookmarkName
		{
			get
			{
				return BookmarkPropertyName("IndexOfRecord");
			}
		}

		private string BookmarkPropertyName(string attribute)
		{
			return BookmarkNamePrefix + attribute;
		}

		private void SavePersisted(int recordIndex)
		{
			m_propertyTable.SetProperty(RecordIndexBookmarkName, recordIndex, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("IndexOfParagraph"), m_iParagraph, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("CharBeginOffset"), m_BeginOffset, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty(BookmarkPropertyName("CharEndOffset"), m_EndOffset, SettingsGroup.LocalSettings, true, false);
		}

		/// <summary>
		/// Restore the InterlinMaster bookmark to its previously saved state.
		/// </summary>
		public void Restore(int index)
		{
			// verify we're restoring to the right text. Is there a better way to verify this?
			int restoredRecordIndex = m_propertyTable.GetValue(RecordIndexBookmarkName, SettingsGroup.LocalSettings, -1);
			if (index != restoredRecordIndex)
				return;
			m_iParagraph = m_propertyTable.GetValue(BookmarkPropertyName("IndexOfParagraph"), SettingsGroup.LocalSettings, 0);
			m_BeginOffset = m_propertyTable.GetValue(BookmarkPropertyName("CharBeginOffset"), SettingsGroup.LocalSettings, 0);
			m_EndOffset = m_propertyTable.GetValue(BookmarkPropertyName("CharEndOffset"), SettingsGroup.LocalSettings, 0);
		}

		/// <summary>
		/// Reset the bookmark to its default values.
		/// </summary>
		public void Reset(int index)
		{
			m_iParagraph = 0;
			m_BeginOffset = 0;
			m_EndOffset = 0;

			SavePersisted(index);
		}

		#region IStTextBookmark
		public int IndexOfParagraph { get { return m_iParagraph; } }
		public int BeginCharOffset { get { return m_BeginOffset; } }
		public int EndCharOffset { get { return m_EndOffset; } }

		public int TextIndex
		{
			get {
				return m_textIndex;
			}
		}

		#endregion
	}
}