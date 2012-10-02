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
