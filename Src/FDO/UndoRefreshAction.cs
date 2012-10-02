using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This is a dummy class designed to force an undo sequence to do a full refresh instead
	/// of relying on PropChanged notifications.
	/// </summary>
	public class UndoRefreshAction : UndoActionBase
	{
		/// <summary>
		/// simple constructor for simple class.
		/// </summary>
		public UndoRefreshAction()
		{
		}

		#region Overrides of UndoActionBase
		/// <summary>
		/// redo the nonaction we did.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Redo(bool fRefreshPending)
		{
			return true;
		}

		/// <summary>
		/// WE NEED A REFRESH OPERATION WHEN UNDOING OR REDOING!!!
		/// </summary>
		/// <returns></returns>
		public override bool RequiresRefresh()
		{
			return true;
		}

		/// <summary>
		/// undo the nonaction we did.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public override bool Undo(bool fRefreshPending)
		{
			return true;
		}

		#endregion
	}
}
