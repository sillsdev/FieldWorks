// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	internal class IntComboItem
	{
		private readonly string m_name;

		public IntComboItem(string name, int val)
		{
			m_name = name;
			Value = val;
		}

		public int Value { get; }

		public override string ToString()
		{
			return m_name;
		}
	}
}