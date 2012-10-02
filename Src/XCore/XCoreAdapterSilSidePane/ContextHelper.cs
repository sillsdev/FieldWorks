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
using System.Xml;

using SIL.Utils;

namespace XCore
{


	/// summary>
	/// adapts DotNetBar to provide context help
	/// /summary>
	public class ContextHelper : BaseContextHelper
	{
		//DevComponents.DotNetBar.BalloonTip m_balloon;


		public ContextHelper() : base()
		{
//			m_balloon = new DevComponents.DotNetBar.BalloonTip();
		}


		#region XCORE Message Handlers



		protected override void SetHelps(Control target,string caption, string text )
		{
//			m_balloon.SetBalloonCaption(target, caption);
//			m_balloon.SetBalloonText(target, text);
		}
		#endregion


		public override Control ParentControl
		{
			set
			{
				CheckDisposed();
//				m_balloon.SetBalloonText(value, "containing control");
//				m_balloon.ShowAlways=true;
//				m_balloon.AutoClose =false;
//				m_balloon.ShowCloseButton = false;
			}
		}



		protected override bool ShowAlways
		{
			set
			{
//				m_balloon.ShowAlways= value;
//				m_balloon.Enabled= value;
			}
		}

		public override int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}
	}
}
