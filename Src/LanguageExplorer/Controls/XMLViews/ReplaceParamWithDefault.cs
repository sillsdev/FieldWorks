// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This one modifies the attribute, replacing the parameter with its default.
	/// </summary>
	internal class ReplaceParamWithDefault : TestForParameter
	{
		public override bool Visit(XAttribute xa)
		{
			if (!IsParameter(xa.Value))
			{
				return false;
			}
			xa.Value = xa.Value.Substring(xa.Value.IndexOf('=') + 1);
			return false;
		}
	}
}