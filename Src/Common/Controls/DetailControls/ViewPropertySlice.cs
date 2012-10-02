using System;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	public class ViewPropertySlice : ViewSlice
	{
		protected int m_hvoContext; // The object, one of whose attributes we are displaying.
		protected int m_flid; // The field identifier for the attribute we are displaying.

		public ViewPropertySlice()
		{
		}
		public ViewPropertySlice(RootSite ctrlT, int hvoObj, int flid): base(ctrlT)
		{
			m_hvoContext = hvoObj;
			m_flid = flid;
		}

		/// <summary>
		/// The object being manipulated.
		/// </summary>
		public int ContextObject
		{
			get
			{
				CheckDisposed();
				return m_hvoContext;
			}
			set
			{
				CheckDisposed();
				m_hvoContext = value;
			}
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
