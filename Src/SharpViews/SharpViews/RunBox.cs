// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// A RunBox is a sort of NullObject for a GroupBox: it is used as a grouping construct
	/// when several things that would normally be grouped into a parent box are instead
	/// directly added to the higher level parent.
	/// </summary>
	internal class RunBox : GroupBox
	{
		public RunBox(AssembledStyles styles) : base(styles)
		{
		}

		public override void Layout(LayoutInfo transform)
		{
			throw new NotImplementedException("We should never try to lay one of these out");
		}
	}
}
