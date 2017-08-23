// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
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
		/// <param name="propertyTable"></param>
		/// <param name="classId"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="insertionPosition"></param>
		/// <returns></returns>
		public static LexPronunciationUi CreateNewUiObject(IPropertyTable propertyTable, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			LexPronunciationUi result = null;
			LcmCache cache = propertyTable.GetValue<LcmCache>("cache");
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
