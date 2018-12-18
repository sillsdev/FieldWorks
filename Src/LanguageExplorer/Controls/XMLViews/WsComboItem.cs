// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class WsComboItem
	{
		private readonly string m_name;

		/// <summary />
		public WsComboItem(string name, string writingSystemId)
		{
			m_name = name;
			Id = writingSystemId;
		}
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		public override string ToString()
		{
			return m_name;
		}

		/// <summary>
		/// Returns true if the given object is a WsComboItem and the name and id match this WsComboItem.
		/// </summary>
		public override bool Equals(object obj)
		{
			var item = obj as WsComboItem;
			return item != null && m_name == item.m_name && Id == item.Id;
		}

		/// <summary>
		/// Implemented for the Equals method.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			var hashCode = 641297398;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_name);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
			return hashCode;
		}

		/// <summary>
		/// Gets the writing system identifier.
		/// </summary>
		public string Id { get; }

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