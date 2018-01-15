// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class WsComboItem
	{
		private readonly string m_name;

		/// <summary>
		/// Initializes a new instance of the <see cref="WsComboItem"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="id">The writing system ID.</param>
		public WsComboItem(string name, string id)
		{
			m_name = name;
			Id = id;
		}
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		public override string ToString()
		{
			return m_name;
		}
		/// <summary>
		/// Gets the writing system identifier.
		/// </summary>
		/// <value>The writing system identifier.</value>
		public string Id { get; }
	}
}