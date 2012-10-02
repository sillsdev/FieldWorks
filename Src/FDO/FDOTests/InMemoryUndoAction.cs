// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InMemoryUndoAction.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An undo action for use with the InMemoryCache that stores the old and new object as
	/// undo and redo values.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InMemoryUndoAction: UndoActionBase
	{
		#region UndoRedoObject class
		[DebuggerDisplay("Hvo={Key.Hvo},Tag={Key.Tag},Object={Object}")]
		private class UndoRedoObject
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:UndoRedoObject"/> class.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="obj">The obj.</param>
			/// --------------------------------------------------------------------------------
			public UndoRedoObject(CacheKey key, object obj)
			{
				Key = key;
				Object = obj;
			}

			/// <summary>The Key</summary>
			public CacheKey Key;
			/// <summary>The object</summary>
			public object Object;
		}
		#endregion

		#region Member variables
		private CacheBase m_CacheBase;
		private UndoRedoObject m_UndoObject;
		private UndoRedoObject m_RedoObject;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InMemoryUndoAction"/> class.
		/// </summary>
		/// <param name="cacheBase">The cache base.</param>
		/// ------------------------------------------------------------------------------------
		public InMemoryUndoAction(CacheBase cacheBase)
		{
			m_CacheBase = cacheBase;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the old object for use with undo.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="oldObject">The old object.</param>
		/// ------------------------------------------------------------------------------------
		public void AddUndo(CacheKey key, object oldObject)
		{
			m_UndoObject = new UndoRedoObject(key, oldObject);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the new object for use with redo.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="newObject">The new object.</param>
		/// ------------------------------------------------------------------------------------
		public void AddRedo(CacheKey key, object newObject)
		{
			m_RedoObject = new UndoRedoObject(key, newObject);
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member IsDataChange
		/// </summary>
		/// <returns>A System.Boolean</returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return (m_RedoObject != null && m_UndoObject != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Redo
		/// </summary>
		/// <param name="fRefreshPending">fRefreshPending</param>
		/// <returns>A System.Boolean</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			if (m_RedoObject == null)
				return false;
			m_CacheBase[m_RedoObject.Key] = m_RedoObject.Object;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Undo
		/// </summary>
		/// <param name="fRefreshPending">fRefreshPending</param>
		/// <returns>A System.Boolean</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (m_UndoObject == null)
				return false;
			m_CacheBase[m_UndoObject.Key] = m_UndoObject.Object;
			return true;
		}
		#endregion
	}
}
