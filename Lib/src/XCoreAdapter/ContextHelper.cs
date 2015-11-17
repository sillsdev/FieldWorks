// --------------------------------------------------------------------------------------------
// Copyright (c) 2004-2015 SIL International
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

	}
}
