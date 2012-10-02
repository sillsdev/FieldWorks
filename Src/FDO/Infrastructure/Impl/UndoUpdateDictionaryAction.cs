using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This is a template for the problem of updating a dictionary which is a cache for finding objects
	/// of class T from some string property of a T.
	/// Typically the method that updates the cache when property T changes inserts one of these into
	/// the undo stack so the cache gets cleared up if the action is undone.
	/// Currently it is assumed that there is no alternate value stored for either key, that is,
	/// the dictionary should contain nothing for the key that doesn't correspond to the current state.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class UndoUpdateDictionaryAction<T> : IUndoAction where T: class
	{
		protected string m_oldKey;
		protected string m_newKey;
		protected T m_target;
		Dictionary<string, T> m_lookupTable;

		public UndoUpdateDictionaryAction(string oldKey, string newKey, T target, Dictionary<string, T> lookupTable)
		{
			m_oldKey = oldKey;
			m_newKey = newKey;
			m_target = target;
			m_lookupTable = lookupTable;
		}
		public virtual bool Undo()
		{
			if (!String.IsNullOrEmpty(m_newKey))
				m_lookupTable.Remove(m_newKey);
			if (!String.IsNullOrEmpty(m_oldKey) && m_target != null)
				m_lookupTable[m_oldKey] = m_target;
			return true;
		}

		public virtual bool Redo()
		{
			if (!String.IsNullOrEmpty(m_oldKey))
				m_lookupTable.Remove(m_oldKey);
			if (!String.IsNullOrEmpty(m_newKey) && m_target != null)
				m_lookupTable[m_newKey] = m_target;
			return true;
		}

		public void Commit()
		{
		}

		public bool IsDataChange
		{
			get { return false; }
		}

		public bool IsRedoable
		{
			get { return true; }
		}

		public bool SuppressNotification
		{
			set { }
		}
	}
}
