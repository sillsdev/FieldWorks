using System;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Helper for keeping track of our location in the text when switching from and back to the
	/// Texts area (cf. LT-1543).  It also serves to keep our place when switching between
	/// RawTextPane (Baseline), GlossPane, AnalyzePane(Interlinearizer), TaggingPane, PrintPane and ConstChartPane.
	/// </summary>
	public class InterAreaBookmark : IStTextBookmark
	{
		XCore.Mediator m_mediator;
		FdoCache m_cache = null;
		int m_iParagraph;
		int m_BeginOffset;
		int m_EndOffset;
		private string m_bookmarkId;
		private int m_textIndex;

		internal InterAreaBookmark()
		{
		}

		internal InterAreaBookmark(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)	// For restoring
		{
			Init(interlinMaster, mediator, cache);
			Restore(interlinMaster.IndexOfTextRecord);
		}

		internal void Init(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)
		{
			Debug.Assert(interlinMaster != null);
			Debug.Assert(mediator != null);
			Debug.Assert(cache != null);
			m_mediator = mediator;
			m_cache = cache;
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
		public void Save(bool fPersistNow, int index)
		{
			if (fPersistNow)
				this.SavePersisted(index);
		}

		internal void Save(int textIndex, int paragraphIndex, int beginCharOffset, int endCharOffset, bool fPersistNow)
		{
			m_iParagraph = paragraphIndex;
			m_BeginOffset = beginCharOffset;
			m_EndOffset = endCharOffset;
			m_textIndex = textIndex;
			this.Save(fPersistNow, textIndex);
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
			Debug.Assert(m_mediator != null);
			m_mediator.PropertyTable.SetProperty(RecordIndexBookmarkName, recordIndex, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("IndexOfParagraph"), m_iParagraph, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharBeginOffset"), m_BeginOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharEndOffset"), m_EndOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(RecordIndexBookmarkName, true, PropertyTable.SettingsGroup.LocalSettings);
			// m_mediator.PropertyTable.SetPropertyPersistence(pfx + "Title", true);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("IndexOfParagraph"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharBeginOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharEndOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Restore the InterlinMaster bookmark to its previously saved state.
		/// </summary>
		public void Restore(int index)
		{
			Debug.Assert(m_mediator != null);
			// verify we're restoring to the right text. Is there a better way to verify this?
			int restoredRecordIndex = m_mediator.PropertyTable.GetIntProperty(RecordIndexBookmarkName, -1, PropertyTable.SettingsGroup.LocalSettings);
			if (index != restoredRecordIndex)
				return;
			m_iParagraph = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("IndexOfParagraph"), 0, PropertyTable.SettingsGroup.LocalSettings);
			m_BeginOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharBeginOffset"), 0, PropertyTable.SettingsGroup.LocalSettings);
			m_EndOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharEndOffset"), 0, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Reset the bookmark to its default values.
		/// </summary>
		public void Reset(int index)
		{
			m_iParagraph = 0;
			m_BeginOffset = 0;
			m_EndOffset = 0;

			this.SavePersisted(index);
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