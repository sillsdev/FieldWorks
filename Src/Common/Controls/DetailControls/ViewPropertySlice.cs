// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	public class ViewPropertySlice : ViewSlice
	{
		protected int m_flid; // The field identifier for the attribute we are displaying.

		/// <summary></summary>
		public ViewPropertySlice()
		{
		}

		/// <summary></summary>
		public ViewPropertySlice(RootSite ctrlT, ICmObject obj, int flid): base(ctrlT)
		{
			Reuse(obj, flid);
		}

		/// <summary>
		/// Put the slice in the same state as if just created with these arguments.
		/// </summary>
		public void Reuse(ICmObject obj, int flid)
		{
			Object = obj;
			m_flid = flid;

		}

		/// <summary>
		/// Gets the ID of the field we are editing.
		/// </summary>
		public int FieldId
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
			set
			{
				CheckDisposed();
				m_flid = value;
			}
		}
	}
}
