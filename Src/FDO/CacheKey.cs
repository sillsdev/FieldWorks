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
// File: CacheKey.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	///
	/// </summary>
	[DebuggerDisplay("Guid={m_guid},Flid={m_flid}")]
	public struct GuidFlidKey
	{
		private Guid m_guid;
		private int m_flid;

		/// <summary>
		///
		/// </summary>
		public Guid Guid
		{
			get { return m_guid; }
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="flid"></param>
		public GuidFlidKey(Guid guid, int flid)
		{
			m_guid = guid;
			m_flid = flid;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is GuidFlidKey))
				return false;

			GuidFlidKey gfk = (GuidFlidKey)obj;
			return (gfk.m_guid == m_guid)
				&& (gfk.m_flid == m_flid);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return m_guid.GetHashCode() ^ (int)m_flid;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0}^{1}", m_guid, m_flid);
		}
	}

	/// <summary>
	///
	/// </summary>
	[DebuggerDisplay("Hvo={m_hvo},Flid={m_flid}")]
	public struct HvoFlidKey
	{
		private int m_hvo;
		private int m_flid;

		/// <summary>
		///
		/// </summary>
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		public HvoFlidKey(int hvo, int flid)
		{
			m_hvo = hvo;
			m_flid = flid;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is HvoFlidKey))
				return false;

			HvoFlidKey hfk = (HvoFlidKey)obj;
			return (hfk.m_hvo == m_hvo)
				&& (hfk.m_flid == m_flid);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (m_hvo ^ (int)m_flid);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0}^{1}", m_hvo, m_flid);
		}
	}

	/// <summary>
	///
	/// </summary>
	[DebuggerDisplay("Hvo={m_hvo},Flid={m_flid},WS={m_ws}")]
	public struct HvoFlidWSKey
	{
		private int m_hvo;
		private int m_flid;
		private int m_ws;

		/// <summary>
		///
		/// </summary>
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		///
		/// </summary>
		public int Ws
		{
			get { return m_ws; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		public HvoFlidWSKey(int hvo, int flid, int ws)
		{
			m_hvo = hvo;
			m_flid = flid;
			m_ws = ws;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is HvoFlidWSKey))
				return false;

			HvoFlidWSKey key = (HvoFlidWSKey)obj;
			return (key.m_hvo == m_hvo)
				&& (key.m_flid == m_flid)
				&& (key.m_ws == m_ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (m_hvo ^ (int)m_flid ^ m_ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0}^{1}^{2}", m_hvo, m_flid, m_ws);
		}
	}
}
