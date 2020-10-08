// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI for LexPronunciation.
	/// </summary>
	internal sealed class LexPronunciationUi : CmObjectUi
	{
		// For MakeLcmModelUiObject method.
		internal LexPronunciationUi() { }

		/// <summary>
		/// Handle the context menu for inserting a LexPronunciation.
		/// </summary>
		internal static LexPronunciationUi MakeLcmModelUiObject(LcmCache cache, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			Guard.AgainstNull(cache, nameof(cache));

			LexPronunciationUi result = null;
			UndoableUnitOfWorkHelper.Do(LcmUiResources.ksUndoInsert, LcmUiResources.ksRedoInsert, cache.ActionHandlerAccessor, () =>
			{
				var newHvo = cache.DomainDataByFlid.MakeNewObject(classId, hvoOwner, flid, insertionPosition);
				result = (LexPronunciationUi)MakeLcmModelUiObject(cache, newHvo);
				// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
				var dummy = cache.LangProject.DefaultPronunciationWritingSystem;
			});
			return result;
		}
	}
}