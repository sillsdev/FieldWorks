// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This one modifies the attribute, replacing the default value of the named parameter.
	/// </summary>
	internal class ReplaceParamDefault : IAttributeVisitor
	{
		readonly string m_paramPrefix;
		readonly string m_defVal;

		public ReplaceParamDefault(string paramName, string defVal)
		{
			m_paramPrefix = "$" + paramName + "=";
			m_defVal = defVal;
		}


		public bool Visit(XAttribute xa)
		{
			if (!xa.Value.StartsWith(m_paramPrefix))
			{
				return false;
			}
			xa.Value = m_paramPrefix + m_defVal;
			return true;
		}
	}
}