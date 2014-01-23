// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OnDialog.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices; // DllImport

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for OnDialog.
	/// </summary>
	public class OnDialog : Context
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		public enum Msg { WM_SETFOCUS = 0x0007 };

		string m_name;
		string m_title;
		bool   m_found = false;
		int    m_until;

		static System.Collections.Stack m_DlgHwndStack = new System.Collections.Stack();

		public OnDialog()
		{
			m_name  = null;
			m_title = null;
			m_tag   = "on-dialog";
			m_until = 30000; // set the milliseconds to wait for the dialog to appear
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			m_log.isTrue(Title != null || Select != null, makeNameTag() + Name + "' has no Title or selected model.");
			ModelNode = con.ModelNode;
			return true;
		}

		// look for the expected dialog to appear. If it does, make an accessibilty
		// helper for it.
		public override void Execute()
		{
			// base.Execute(ts); // can't call this yet as it executes the children
			WaitMsec(); // do call this but make sure Wait is reset to zero!
			Wait = 0; // reset to zero so there is no delay after the dialog is found.
			// number is needed in diagnostics for the log
			if (Number == -1) Number = TestState.getOnly().IncInstructionCount;

			/// If present, use the selected dialog model title
			Context con = (Context)Ancestor(typeof(Context));
			if (m_select != null && m_select != "")
			{  // make a new model context node and move dialog's children to it
				m_select = Utilities.evalExpr(m_select);
				XmlDocument doc = m_elt.OwnerDocument;
				XmlElement modElt = doc.CreateElement("model");
				modElt.SetAttribute("select", m_select);
				XmlNodeList children = m_elt.ChildNodes;
				int count = children.Count;
				while (count > 0)
				{  // move dialog children to model
					XmlNode child = children.Item(0); //get the first child
					modElt.AppendChild(child); // automatically removed from m_elt!!
					count = children.Count;
				}
				m_elt.AppendChild(modElt);
				// set the title to look for
				// can only have one text node
				XmlNodeList pathNodes = Instructionator.selectNodes(this, m_select, makeName());
				m_log.isNotNull(pathNodes, makeNameTag() + " select='" + m_select + "' returned no model");
				m_log.isTrue(pathNodes.Count > 0, makeNameTag() + " select='" + m_select + "' returned no model nodes");
				// This is the model node
				XmlNode modNode = pathNodes.Item(0);
				if (m_title == null || m_title == "")
				{  // no title override, so set the title from the model
					string titleCheck = XmlFiler.getAttribute(modNode, "title");
					if (titleCheck != null)
					{
						m_title = titleCheck;
						m_log.paragraph("on-dialog title set from selected model " + titleCheck);
					}
				}
				else { m_log.paragraph("on-dialog title set from @title " + m_title); }
				string nameCheck = XmlFiler.getAttribute(modNode, "name");
				if (nameCheck != null)
				{
					m_name = nameCheck;
					m_log.paragraph("on-dialog name set from selected model " + nameCheck);
				}
				m_select = null; // can only do this one time in do-once or model
			}
			// if no name, try title
			if (m_title != null && m_title != "" && (m_name == null || m_name == "")) m_name = m_title;

			m_log.isNotNull(m_title, makeNameTag() + " No @title in script or model for this dialog.");
			m_log.isFalse(m_title == "", makeNameTag() + " @title in script or model is blank.");
			m_log.isFalse(m_name == null && !m_title.StartsWith("rexp#"), makeNameTag() + " No @name step in script or model for this dialog.");
			m_log.isFalse(m_name == "" && !m_title.StartsWith("rexp#"), makeNameTag() + " @name step in script or model is blank.");
			//if (m_title != null && m_title != "") m_title = Utilities.evalExpr(m_title);
			//if (m_name != null && m_name != "") m_name = Utilities.evalExpr(m_name);
			m_title = Utilities.evalExpr(m_title);
			m_name = Utilities.evalExpr(m_name);

			m_log.paragraph(image());

			if (Application != null)
			{
				try { Application.Process.WaitForInputIdle(); }
				catch (Win32Exception e)
				{ m_log.paragraph(makeNameTag() + " WaitForInputIdle: " + e.Message); }
			}
			// Give the window m_Rest seconds to show up
			int startTick = System.Environment.TickCount;
			IntPtr foundHwndPtr;
			string name = null;
			Regex rx = null;

			if (m_title != null && m_title.StartsWith("rexp#"))
			{   // Create a regular expression object
				try { rx = new Regex(m_title.Substring(5)); }
				catch (ArgumentException e)
				{
					m_log.fail(makeNameTag() + " title from rexp# [" + m_title.Substring(5)
								  + "] error: " + e.Message);
				}
			}

			while (!m_found)
			{   // If there is a regular expression, try it.
				if (rx != null)
				{   // try the main window name then other windows it may own via the regular expression
					m_log.paragraph("Searching all processes");
					Process[] allProcs = Process.GetProcesses();
					for (int p = 0; p < allProcs.Length; p++)
					{
						Process pro = allProcs[p];
						try {
							if (rx.IsMatch(pro.MainWindowTitle)) {
								m_found = true;
								m_ah = new AccessibilityHelper(pro.Handle);
								break;
							}
						}
						catch (Exception e) {
							m_log.paragraph(makeNameTag() + " main title from rexp# [" + m_title.Substring(5)
										  + "] process error: " + e.Message);
						}
						#region Attempt to explore process threads - useful?
						// try the windows that belong to this process
						/*try {
							foreach (ProcessThread pt in pro.Threads)
							{

								string para = "on-dialog matching proc [" + pro.ProcessName + ":";
								if (pt.Site != null) para += pt.Site.Name + "]";
								else                 para += "]";
								m_log.paragraph(para);
								if (pt.Site != null && rx.IsMatch(pt.Site.Name)) {
									m_found = true;
									m_ah = new AccessibilityHelper(pro.Handle);
									break;
								}
							}
						}
						catch (Exception e)
						{
							m_log.paragraph("on-dialog title from rexp# [" + m_title.Substring(5)
										  + "] process error: " + e.Message);
						} */
						#endregion
					}
				}
				if (!m_found)
				{   // get the window handle for windows with the right name
					// unfortuneately, other windows, or partially formed windows
					// seem to be obtained too.
					m_log.paragraph("Searching the desktop for a window via FindWindow");
					if (rx != null) foundHwndPtr = FindWindow(null, null);
					else foundHwndPtr = FindWindow(null, m_title);
					if ((int)foundHwndPtr != 0)
					{   // is this the window? Is it completely formed?
						m_ah = new AccessibilityHelper(foundHwndPtr);
						if (m_ah == null) m_log.paragraph(makeNameTag() + " Obtained window with no Accessibiilty!");
						else // this window has accessibility - hope it's fully built
						{   // is this or one of its children the window?
							name = m_ah.Name; //when name1 = "", m_ah is probably bad - i.e. not an object
							if (name == "") { } // do nothing, keep looking
							else if (name.Equals(m_title) || name.Equals(this.m_name))
							{   // this is likely it
								m_found = true;
							}
							else // m_ah might be the ah for the main app or dialog window
							{   // Maybe one of its children is the window we want
								m_log.paragraph("Searching for a child window");
								m_ah = m_ah.FindChild(m_title, AccessibleRole.Dialog);
								if (m_ah != null)
								{ // is this the window?
									name = m_ah.Name; // name1 can't be null
									if (name == "") { } // do nothing, keep looking
									else if (name.Equals(m_title) || name.Equals(this.m_name))
									{   // this might be it
										m_found = true;
									}
								}
							}
						}
					}
				}

				if (Utilities.NumTicks(startTick, System.Environment.TickCount) > m_until)
					break;	// time is up
				System.Threading.Thread.Sleep(100);
			}

			m_Rest = 0; // don't wait later when base.Execute is invoked

			if (m_found) m_DlgHwndStack.Push(m_ah.HWnd);
			else
			{   // Didn't find the window
				m_ah = null;
			}

			string contextPass, contextFail;
			PassFailInContext(OnPass, OnFail, out contextPass, out contextFail);	//  out m_onPass, out m_onFail);
			m_log.paragraph(makeNameTag() + " passIn=" + OnPass + " failIn=" + OnFail + " pass=" + contextPass + " fail=" + contextFail);
			if (!m_found && contextFail == "skip")
			{
				return; // quietly exit
			}
			m_log.isTrue(m_found, makeNameTag() + m_title + @"' was not created or not accessible");
			if (name == null)
			{
				m_log.paragraph(makeNameTag() + " Wierd: ah exists but name was null - should NEVER happen!!");
				name = "";
			}
			if (contextPass == "assert")
				m_log.fail(makeNameTag() + m_title + " was not supposed to display.");

			base.Execute();
			m_log.result(this);
			base.Finished = true;	// finished processing this dialog context
			m_DlgHwndStack.Pop();

			if (m_DlgHwndStack.Count > 0)
			{
				int hwnd = (int)m_DlgHwndStack.Peek();
				SendMessage((IntPtr)hwnd,
					(int)Msg.WM_SETFOCUS,0,0);
				m_log.paragraph(makeNameTag() + " Sent Focus message to containing context object");
				//	m_ah.Parent.SendWindowMessage((int)SIL.FieldWorks.Common.Utils.Win32.WinMsgs.WM_SETFOCUS,0,0);
			}
		}

		public string Name
		{
			get {return m_name;}
			set {m_name = value;}
		}

		public string Title
		{
			get {return m_title;}
			set {m_title = value; if (m_title == "NAMELESS")m_title = "";}
		}

		public int Until
		{
			get { return m_until; }
			set { m_until = value; }
		}

		/*public AccessibilityHelper getTopWindow()
		{
			IntPtr handle = FindWindow(null, null);
			return new AccessibilityHelper(handle);
		}*/

		/// <summary>
		/// Gets the image of the specified data. If name is null,
		/// the title is returned.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "title";
			switch (name)
			{
			case "result":  return m_found.ToString();
			case "title":   return m_title;
			case "name":    return m_name;
			case "until":   return m_until.ToString();
			case "found":   return m_found.ToString();
			default:		return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_name != null)       image += @" name="""+Utilities.attrText(m_name)+@"""";
			if (m_title != null) image += @" title=""" + Utilities.attrText(m_title) + @"""";
			if (m_until != 30000) image += @" until=""" + m_until.ToString() + @"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			image += @" found="""+m_found+@"""";
			image += @" title=""" + Utilities.attrText(m_title) + @"""";
			image += @" until=""" + m_until + @"""";
			string found = "";
			if (m_ah == null) found = "-not found-";
			else              found = m_ah.Name;
			image += @" name="""+found+@"""";
			return image;
		}
	}
}
