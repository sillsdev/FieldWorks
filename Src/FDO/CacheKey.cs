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
	#region Class CacheKey
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Regular key used for the cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[DebuggerDisplay("Hvo={m_hvo},Tag={m_tag}")]
	public class CacheKey
	{
		private int m_hvo;
		private int m_tag;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CacheKey"/> class.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// -----------------------------------------------------------------------------------
		public CacheKey(int hvo, int tag)
		{
			m_hvo = hvo;
			m_tag = tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Hvo
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Tag
		{
			get { return m_tag; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a (hopefully) suitable hash code
		/// </summary>
		/// <returns>Hash code</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return (m_hvo ^ m_tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether two <see cref="CacheKey"/> instances are equal
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare with the current <c>Object</c>
		/// </param>
		/// <returns><c>true</c> if the specified <see cref="Object"/> is equal to the current
		/// <c>Object</c>; otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (!(obj is CacheKey))
				return false;

			CacheKey key = (CacheKey)obj;
			return (key.m_hvo == m_hvo && key.m_tag == m_tag);
		}
	}
	#endregion

	#region Class CacheKeyEx
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Key for storing multi string properties in the cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[DebuggerDisplay("Hvo={Hvo},Tag={Tag},Other={Other}")]
	public class CacheKeyEx : CacheKey
	{
		private int m_Other;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CacheKeyEx"/> struct.
		/// </summary>
		/// <param name="hvo">Hvo</param>
		/// <param name="tag">Tag</param>
		/// <param name="other">Encoding or other additional key property</param>
		/// ------------------------------------------------------------------------------------
		public CacheKeyEx(int hvo, int tag, int other)
			: base(hvo, tag)
		{
			m_Other = other;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the encoding or other additional key property e.g. for a multi string
		/// property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Other
		{
			get { return m_Other; }
			set { m_Other = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a (hopefully) suitable hash code
		/// </summary>
		/// <returns>Hash code</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return (base.GetHashCode() ^ m_Other);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether two <see cref="CacheKeyEx"/> instances are equal
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare with the current <c>Object</c>
		/// </param>
		/// <returns><c>true</c> if the specified <see cref="Object"/> is equal to the current
		/// <c>Object</c>; otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (!(obj is CacheKeyEx))
				return false;

			CacheKeyEx key = (CacheKeyEx)obj;
			return (key.m_Other == m_Other && base.Equals(obj));
		}
	}
	#endregion
}
