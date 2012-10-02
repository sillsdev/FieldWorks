using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.FdoUi
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
		/// <param name="mediator"></param>
		/// <param name="classId"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="insertionPosition"></param>
		/// <returns></returns>
		public new static LexPronunciationUi CreateNewUiObject(Mediator mediator, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			LexPronunciationUi result = null;
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoInsert, FdoUiStrings.ksRedoInsert, cache.ActionHandlerAccessor,
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
