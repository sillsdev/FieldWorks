// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class WsComboItem
	{
		private readonly string m_name;
		private readonly string m_id;
		private readonly string m_abbreviation;

		/// <summary>
		/// Initializes a new instance of the <see cref="WsComboItem"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="id">The writing system ID.</param>
		/// <param name="abbreviation">The abbreviation (defaults to null).</param>
		public WsComboItem(string name, string id, string abbreviation = null)
		{
			m_name = name;
			m_id = id;
			m_abbreviation = abbreviation;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return m_name;
		}

		/// <summary>
		/// Gets the abbreviation.
		/// If no abbreviation exists then the name is returned.
		/// </summary>
		public string Abbreviation
		{
			get
			{
				if (String.IsNullOrEmpty(m_abbreviation))
					return m_name;
				return m_abbreviation;
			}
		}

		/// <summary>
		/// Returns true if the given object is a WsComboItem and the name, id, and abbreviation match this WsComboItem.
		/// </summary>
		/// <param name="obj">The object to compare</param>
		/// <returns>True if equal, false if not equal</returns>
		public override bool Equals(object obj)
		{
			var item = obj as WsComboItem;
			return item != null &&
				   m_name == item.m_name &&
				   m_id == item.m_id &&
				   Abbreviation == item.Abbreviation;
		}

		/// <summary>
		/// Implemented for the Equals method.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			var hashCode = 641297398;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_name);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_id);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Abbreviation);
			return hashCode;
		}

		/// <summary>
		/// Gets the writing system identifier.
		/// </summary>
		/// <value>The writing system identifier.</value>
		public string Id
		{
			get { return m_id; }
		}

		/// <summary>
		/// Stores the handle of the writing system.
		/// </summary>
		public int WritingSystem { get; set; }

		/// <summary>
		/// Stores the type of the writing system (vernacular, analysis, or both)
		/// </summary>
		public string WritingSystemType { get; set; }
	}
}