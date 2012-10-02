using System;
using System.Collections.Generic;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// The actual item itself with the key pieces of information
	/// </summary>
	public class ChangedDataItem
	{
		private int m_hvo;
		private int m_flid;
		private int m_classid;
		private string m_sClassName;

		public ChangedDataItem(int hvo, int flid, int classid, string sClassName)
		{
			m_hvo = hvo;
			m_flid = flid;
			m_classid = classid;
			m_sClassName = sClassName;
		}

		/// <summary>
		/// Get the class ID of the changed item
		/// </summary>
		public int ClassId
		{
			get
			{
				return m_classid;
			}
		}
		/// <summary>
		/// Get the class name of the changed item
		/// </summary>
		public string ClassName
		{
			get
			{
				return m_sClassName;
			}
		}
		/// <summary>
		/// Get the flid of the changed item
		/// </summary>
		public int Flid
		{
			get
			{
				return m_flid;
			}
		}
		/// <summary>
		/// Get the hvo of the changed item
		/// </summary>
		public int Hvo
		{
			get
			{
				return m_hvo;
			}
		}
	}
}
