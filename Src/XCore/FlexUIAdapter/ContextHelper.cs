// Copyright (c) 2003-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using System.Xml;

using SIL.LCModel.Utils;

namespace XCore
{
	/// summary>
	/// adapts DotNetBar to provide context help
	/// /summary>
	public class ContextHelper : BaseContextHelper
	{
		public override int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}
	}
}
