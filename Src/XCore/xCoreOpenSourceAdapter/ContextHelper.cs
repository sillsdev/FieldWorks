// Copyright (c) 2003-2013 SIL International
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

  namespace XCore
  {
	/// summary>
	/// /summary>
	public class ContextHelper : BaseContextHelper
	{
		public ContextHelper() : base()
		{
		}

		public override Control ParentControl
		{
			set
			{
				CheckDisposed();
			}
		}

		protected override void SetHelps(Control target,string caption, string text )
		{
		}

		protected override bool ShowAlways
		{
			set
			{
			}
		}

		public override int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}
	}
  }
