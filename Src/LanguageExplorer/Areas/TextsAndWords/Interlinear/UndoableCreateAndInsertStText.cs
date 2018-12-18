// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class UndoableCreateAndInsertStText : CreateAndInsertStText
	{
		private string UndoText { get; }
		private string RedoText { get; }

		internal UndoableCreateAndInsertStText(LcmCache cache, InterlinearTextsRecordList list, string undoText, string redoText)
			: base(cache, list)
		{
			UndoText = undoText;
			RedoText = redoText;
		}

		#region ICreateAndInsert<IStText> Members

		public override IStText Create()
		{
			// NB: Don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
			var wsText = List.GetWsForNewText();
			UndoableUnitOfWorkHelper.Do(UndoText, RedoText, Cache.ActionHandlerAccessor, () => CreateNewTextWithEmptyParagraph(wsText));
			return NewStText;
		}

		#endregion
	}
}