// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI for LexPronunciation.
	/// </summary>
	public class LexPronunciationUi : CmObjectUi
	{

		/// <summary>
		/// Create one. Argument must be a LexPronunciation.
		/// </summary>
		/// <param name="obj"></param>
		public LexPronunciationUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is ILexPronunciation);
		}

		// For MakeUi method.
		internal LexPronunciationUi() {}

		/// <summary>
		/// Handle the context menu for inserting a LexPronunciation.
		/// </summary>
		public static LexPronunciationUi CreateNewUiObject(LcmCache cache, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));

			LexPronunciationUi result = null;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, cache.ActionHandlerAccessor,
				() =>
			{
				int newHvo = cache.DomainDataByFlid.MakeNewObject(classId, hvoOwner, flid, insertionPosition);
				result = (LexPronunciationUi)MakeUi(cache, newHvo);
				// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
				var dummy = cache.LangProject.DefaultPronunciationWritingSystem;
			});
			return result;
		}
	}
}
