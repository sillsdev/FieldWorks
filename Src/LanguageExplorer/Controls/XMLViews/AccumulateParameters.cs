// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Accumulate all parameters. Inherits from TestForParameter so it can inherit
	/// the function that defines one.
	/// </summary>
	internal class AccumulateParameters : TestForParameter
	{
		public override bool Visit(XAttribute xa)
		{
			if (IsParameter(xa.Value))
			{
				Parameters.Add(xa.Value);
			}
			return false; // this one wants to accumulate them all
		}

		public List<string> Parameters { get; } = new List<string>();
	}
}