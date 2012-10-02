// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
