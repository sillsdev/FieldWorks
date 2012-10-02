// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FieldSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.  Collections;

using SIL.FieldWorks.FDO;


namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public abstract class FieldSlice : Slice
	{
		/// <summary>
		/// The field identifier for the attribute we are displaying.
		/// </summary>
		protected int m_flid;

		protected string m_fieldName;

		/// <summary>
		/// Get the flid.
		/// </summary>
		public override int Flid
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
		}

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public FieldSlice() : base()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		public FieldSlice(System.Windows.Forms.Control ctrlT, FdoCache cache, ICmObject obj, int flid)
			: base(ctrlT)
		{
			Debug.Assert(cache != null);
			Debug.Assert(obj != null);
			m_cache = cache;
			Object = obj;
			m_flid = flid;
			m_fieldName = m_cache.MetaDataCacheAccessor.GetFieldName((uint)m_flid);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fieldName = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected abstract void UpdateDisplayFromDatabase();

		internal protected override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();
			if (tag == Flid)
			{
				UpdateDisplayFromDatabase();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called when the slice is first created, but also when it is
		/// "reused" (e.g. refresh or new target object)
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			UpdateDisplayFromDatabase();
			//tc.AccessibilityObject.Name = this.Label;
			this.Control.AccessibilityObject.Name = this.Label;
		}
	}
}