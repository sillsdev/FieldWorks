// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Desktop.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GuiTestDriver
{
	/// <summary>
	/// Desktop is the top-level context, containing the contents of the accil node.
	/// </summary>
	public class Desktop : Context
	{
		public Desktop() : base()
		{
			m_tag = "on-desktop";
		}

		public override void Execute()
		{   // first, see if the last test left an error window open
			//m_memory = System.  //Application.Process
			CheckForErrorDialogs(false);
			// find any window then switch to its parent until it doesn't have one
			//IntPtr foundHwndPtr = FindWindow(null, null); // Random Window
			IntPtr foundHwndPtr = FindWindow(null, "Program Manager"); // 9:Program Manager/9:$NL;/33:Desktop?
			if ((int)foundHwndPtr != 0)
			{
				m_ah = new AccessibilityHelper(foundHwndPtr);
				m_log.isNotNull(m_ah, makeNameTag() + "window " + (int)foundHwndPtr + "isn't accessible");
				m_log.areEqual(m_ah.Name, "Program Manager", makeNameTag() + "window " + m_ah.Name + " found insteadof Program Manager");
				//	while (m_ah.Name != "Desktop" && m_ah.Role != AccessibleRole.Window && m_ah.Parent != null)
			//		m_ah = m_ah.Parent;
			}
			// isTrue(m_ah.Name == "Desktop" && m_ah.Role == AccessibleRole.Window, @"Desktop not found! Got """+m_ah.Role+":"+m_ah.Name+@""" instead");
			if (1 == m_logLevel)
				m_log.paragraph(makeNameTag() + "Desktop is &quot;" + m_ah.Role + ":" + m_ah.Name + "&quot;");

			base.Execute();
			Finished = true; // tell do-once it's done
		}
	}
}
