// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal abstract class CreateAndInsertStText : ICreateAndInsert<IStText>
	{
		protected InterlinearTextsRecordList List { get; }
		protected LcmCache Cache { get; }
		protected IStText NewStText { get; private set; }

		internal CreateAndInsertStText(LcmCache cache, InterlinearTextsRecordList list)
		{
			Cache = cache;
			List = list;
		}

		#region ICreateAndInsert<IStText> Members

		public abstract IStText Create();

		#endregion

		/// <summary>
		/// updates NewStText
		/// </summary>
		protected void CreateNewTextWithEmptyParagraph(int wsText)
		{
			var newText = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			NewStText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			newText.ContentsOA = NewStText;
			List.CreateFirstParagraph(NewStText, wsText);
			InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(NewStText, false);
			if (Cache.LangProject.DiscourseDataOA == null)
			{
				Cache.LangProject.DiscourseDataOA = Cache.ServiceLocator.GetInstance<IDsDiscourseDataFactory>().Create();
			}
			Cache.ServiceLocator.GetInstance<IDsConstChartFactory>().Create(Cache.LangProject.DiscourseDataOA, newText.ContentsOA, Cache.LangProject.GetDefaultChartTemplate());
		}
	}
}