// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// This is a generic UndoAction, for changes that do not affect actual model data, but which do need
	/// to be done, undone, and redone. By inheritance it claims not to be a data change.
	/// </summary>
	public class GenericUndoAction : UndoActionBase
	{
		Action DoItAction { get; set; }
		Action UndoItAction{ get; set;}

		/// <summary>
		/// Make one (also does the whatToDo action)
		/// </summary>
		public GenericUndoAction(Action whatToDo, Action whatToUndo)
		{
			DoItAction = whatToDo;
			UndoItAction = whatToUndo;
			if (whatToDo != null)
				whatToDo();
		}

		/// <summary>
		/// Redo the original action
		/// </summary>
		public override bool Redo()
		{
			if (DoItAction != null)
				DoItAction();
			return true;
		}

		/// <summary>
		/// Undo using the specified action.
		/// </summary>
		public override bool Undo()
		{
			if (UndoItAction != null)
				UndoItAction();
			return true;
		}
	}

	/// <summary>
	/// Like generic undo action, but in addition, causes a PropChanged to be generated.
	/// </summary>
	class GenericPropChangeUndoAction : GenericUndoAction, IFdoPropertyChanged
	{
		private ICmObject m_object;
		private int m_flid;
		public GenericPropChangeUndoAction(Action whatToDo, Action whatToUndo, ICmObject obj, int flid) : base(whatToDo, whatToUndo)
		{
			m_object = obj;
			m_flid = flid;
		}

		/// <summary>
		/// This may be overridden by subclasses that need to know whether the current state is undone
		/// (fForUndo true) or done/Redone (fForUndo false)
		/// </summary>
		public ChangeInformation GetChangeInfo(bool fForUndo)
		{
			return new ChangeInformation(m_object, m_flid, 0, 0, 0);
		}

		/// <summary>
		/// Gets a value indicating whether the changed object is (now) deleted or uninitialized.
		/// </summary>
		public bool ObjectIsInvalid
		{
			get { return !m_object.IsValidObject; }
		}
	}
}
