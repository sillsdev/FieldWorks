// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OnDesktop.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GuiTestDriver
{
	/// <summary>
	/// OnDesktop is the top-level context, containing the contents of the accil node.
	/// </summary>
	public class OnDesktop : Context
	{
		public OnDesktop() : base()
		{
			m_tag = "on-desktop";
		}

		public override void Execute()
		{   // first, see if the last test left an error window open
			//m_memory = System.  //Application.Process
			CheckForErrorDialogs(false);
			// find any window then switch to its parent until it doesn't have one
			//IntPtr foundHwndPtr = FindWindow(null, null); // Random Window
			IntPtr foundHwndPtr = FindWindow(null, "Program Manager"); // 9:Program Manager/9:$NL;/33:OnDesktop?
			if ((int)foundHwndPtr != 0)
			{
				m_ah = new AccessibilityHelper(foundHwndPtr);
				m_log.isNotNull(m_ah, makeNameTag() + "window " + (int)foundHwndPtr + "isn't accessible");
				m_log.areEqual(m_ah.Name, "Program Manager", makeNameTag() + "window " + m_ah.Name + " found insteadof Program Manager");
				//	while (m_ah.Name != "OnDesktop" && m_ah.Role != AccessibleRole.Window && m_ah.Parent != null)
			//		m_ah = m_ah.Parent;
			}
			m_log.isNotNull(m_ah, @"Program Manager object not found");
			m_log.isTrue(m_ah.Name == "Program Manager" && m_ah.Role == AccessibleRole.Window, @"Program Manager not found! Got """ + m_ah.Role + ":" + m_ah.Name + @""" instead");
			if (1 == m_logLevel)
				m_log.paragraph(makeNameTag() + "OnDesktop is &quot;" + m_ah.Role + ":" + m_ah.Name + "&quot;");

			base.Execute();
			Finished = true; // tell do-once it's done
		}
	}
}
