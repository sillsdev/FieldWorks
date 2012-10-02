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
// File: Click.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Xml;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Click.
	/// </summary>
	public class Click : ActionBase, IPathVisitor
	{
		//string m_type;
		string m_side;
		string m_for;
		string m_when;
		int    m_repeat = 1;
		int    m_dx, m_dy;
		const int k_offset = 10;
		Int32  m_until = 5000;  // 5 seconds

		string m_message = null;

		public Click()
		{
			m_tag    = "click";
			m_side   = null;
			m_for    = null;
			m_when   = null;
			m_dx     = k_offset; // click x offset from path or select
			m_dy     = k_offset; // click y offset from path or select
		}
		public string When
		{
			get { return m_when; }
			set { m_when = value; }
		}

		public int Until
		{
			get { return m_until; }
			set { m_until = value; }
		}

		public string Side
		{
			get {return m_side;}
			set {m_side = value;}
		}
		public string For
		{
			get {return m_for;}
			set {m_for = value;}
		}
		public int Repeat
		{
			get {return m_repeat;}
			set {
					if (value < 1) value = 1;
					m_repeat = value;
				}
		}

		public int Dx
		{
			get {return m_dx;}
			set {m_dx = value;}
		}

		public int Dy
		{
			get {return m_dy;}
			set {m_dy = value;}
		}

		/// <summary>
		/// Execute a click, creating and executing more for @select and
		/// creating and executing child instructions.
		/// When used in a do-once instruction, this call is repeated.
		/// </summary>
		public override void Execute()
		{
			// Increase the wait time if the rest time is greater
			// This gives the script writer some control over how
			// long to try the click.
			base.Execute();
			Context con = (Context)Ancestor(typeof(Context));
			isNotNull(con, makeNameTag() +" must occur in some context");
			AccessibilityHelper ah = con.Accessibility;
			isNotNull(ah, makeNameTag() + " context is not accessible");
			m_path = Utilities.evalExpr(m_path);

			/// Use the path or GUI Model or both
			if (m_select != null && m_select != "" && !m_doneOnce) {
				m_path = SelectToPath(con, m_select) + m_path; // process m_select
				m_doneOnce = true;
			}

			if (m_path != null && m_path != "") {
				GuiPath gpath = new GuiPath(m_path);
				isNotNull(gpath, makeNameTag() + " path='" + m_path + "' not parsed");
				ClickPathUntilFound(ah, gpath);
			}
			else fail(makeNameTag() + "attribute 'path' or 'select' must be set or evaluate properly.");
		}

		/// <summary>
		/// Tries to find the target ah repeatedly for the "Rest" period
		/// </summary>
		/// <param name="ah"></param>
		/// <param name="gpath"></param>
		private void ClickPathUntilFound(AccessibilityHelper ah, GuiPath gpath)
		{
			string badPath = "";
			if (DoingOnce()) badPath = ClickPath(ah, gpath);
			else
			{ // act as if it's being done once
				//IntPtr handle = (IntPtr)ah.HWnd; // get an updated ah based on its window handle
				bool done = false;
				while(!done && !m_finished)
				{
					//ah = new AccessibilityHelper((handle)); // refresh the context ah
					ah = new AccessibilityHelper(ah); // refresh the context ah
					if (ah == null) m_log.paragraph("ClickPathUntilFound on "+gpath+" handled a null context");
					if (ah != null) badPath = ClickPath(ah, gpath);
					done = Utilities.NumTicks(m_ExecuteTickCount, System.Environment.TickCount) > m_until;
					System.Threading.Thread.Sleep(500); // try every half second
				}
				// if not clicked, it waited a long time on a bad path
				if (!m_finished) fail(badPath);
			}
		}

		private string ClickPath(AccessibilityHelper ah, GuiPath gpath)
		{
			//bool forAll = false;
			//bool clickParent = false;
			//AccessibilityHelper child;
			//if (m_for != null && "all" == (string)m_for) forAll = true;
			if (1 == m_logLevel)
				m_log.paragraph("Click starting path from &quot;" + ah.Role + ":" + ah.Name + "&quot;");
			ah = gpath.FindInGui(ah, this);
			if (ah != null)
			{
				if (1 == m_logLevel) m_log.paragraph("Clicking last pair in path");
				int j;
				for (j = 0; j < m_repeat; j++)
				{  // click 10 pixels from the left edge - see below
					if (m_side == "right")
						ah.SimulateRightClickRelative(m_dx, m_dy);
					else
						ah.SimulateClickRelative(m_dx, m_dy);
					// when @wait="no" don't wait at all between repeated clicks
					if (m_wait) Thread.Sleep(400); // wait a while eg. let menus open, etc.
				}
				m_finished = true; // tell do-once it's done
			}
			else return m_message;
			return "";
		}

		/// <summary>
		/// method to apply when a non-terminal node has been found
		/// </summary>
		/// <param name="ah"></param>
		public void visitNode(AccessibilityHelper ah)
		{ // does this ah need to be clicked or something to get to its children?
			if (1 == m_logLevel)
				m_log.paragraph("Click found &quot;" + ah.Role + ":" + ah.Name + "&quot;");

			if ((ah.Role == AccessibleRole.MenuItem) || (m_for != null && "all" == (string)m_for))
			{
				if (1 == m_logLevel) m_log.paragraph("Click determining what to do with this intermediate step");
				bool isFocused = (ah.States & AccessibleStates.Focused) == AccessibleStates.Focused;
				if (!isFocused)
				{
					if (1 == m_logLevel) m_log.paragraph("Clicking relative to &quot;" + ah.Role + ":" + ah.Name + "&quot; by (" + m_dx + ", " + m_dy + ") since it does not have focus");
					ah.SimulateClickRelative(m_dx, m_dy);
				}
				else
				{
					if (1 == m_logLevel) m_log.paragraph("Click hovering on &quot;" + ah.Role + ":" + ah.Name + "&quot; since it has focus");
					ah.MoveMouseOverMe(); // hover
				}
			}
		}

		/// <summary>
		/// Method to apply when a path node is not found.
		/// No path above it and none below will call this method;
		/// only the one not found.
		/// </summary>
		/// <param name="path">The path step last tried.</param>
		public void notFound(GuiPath path)
		{
			m_message = "Item '" + path.Role + ":" + path.Name + "' not found to click";
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "path";
			switch (name)
			{
				case "side":	return m_side;
				case "for":		return m_for;
				case "when":    return m_when;
				case "until":   return m_until.ToString();
				case "repeat":  return m_repeat.ToString();
				case "dx":		return m_dx.ToString();
				case "dy":		return m_dy.ToString();
				case "offset":	return k_offset.ToString();
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
			if (m_side != null)   image += @" side="""+m_side+@"""";
			if (m_for != null)    image += @" for="""+m_for+@"""";
			if (m_when != null)   image += @" when=""" + m_when + @"""";
			if (m_until != 5000)  image += @" until=""" + m_until + @"""";
			if (m_repeat != 1)    image += @" repeat=""" + m_repeat + @"""";
			if (m_dx != k_offset) image += @" dx=""" + m_dx + @"""";
			if (m_dy != k_offset) image += @" dy="""+m_dy+@"""";
			image += @" offset="""+k_offset+@"""";
			return image;
		}

		/*
		 * Dot Net Bar Menu behavior:
		 *
		 * Clicking once on a menu button or submenu opens its submenu.
		 * Hovering over a menu button does not open its submenu,
		 * but hovering over a submenu opens its submenu after a short delay
		 * less than 0.4 seconds.
		 * A submenu doesn't even exist until it is opened.
		 *
		 * Double clicking a menu button opens then closes it.
		 * A second click to a submenu may either open it or close it
		 * depending on the amount of time it has been hovering.
		 *
		 * If clicks are too fast, the second gets lost since the submenu
		 * needs some time to build itself.
		 *
		 * So, to get a submenu item, the clicks can't be too close together
		 * and can't be too far apart either. There is a window of opportunity.
		 * Also, if an operation leaves a menu open, the next menu click will close
		 * all the menus. Hovering opens submenus once any menu is open.
		 *
		 * Also, if a submenu gets closed by a click so its items are not visible,
		 * then clicking one of those items causes the cursor to jump to the
		 * upper-left corner of the screen (because it's invisible its location is not defined?).
		 */

		/*
		 * Clicking 10 pixels from the left edge insures that the active region
		 * of a text entry will be hit. The default center misses sometimes when
		 * the text container is long but not active except directly over text.
		 */
	}
}
