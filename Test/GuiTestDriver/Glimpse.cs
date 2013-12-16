// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Glimpse.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// Glimpse selects all model elements specified and examines their GUI
// counterpart for the property specified. If the GUI element has the
// same property value or they are both null, the check passes.
// If the two differ in value or existance, the check fails.
// On failure, Value and Expect hold the GUI prop value and the one
// from the model.
// If path is specified, only one GUI element is examined and
// compared to expect.
// </remarks>

using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Glimpse.
	/// </summary>
	public class Glimpse : CheckBase
	{
		string m_prop;
		string m_expect;
		string m_got;

		public Glimpse(): base()
		{
			m_prop   = null;
			m_expect = null;
			m_got    = null;
			m_tag    = "glimpse";
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
			m_log.isTrue(Path != null || Select != null, makeNameTag() + "Glimpse instruction must have a path or select.");
			m_log.isTrue(Path != "" || Select != "", makeNameTag() + "Glimpse instruction must have a non-empty path or select.");
			InterpretMessage(xn.ChildNodes);
			return true;
		}

		public string Prop
		{
			get {return m_prop;}
			set {m_prop = value;}
		}

		public string Expect
		{
			get {return m_expect;}
			set {m_expect = value;}
		}

		// When used in a do-once instruction, this call is repeated.
		// Note the code that keeps m_select from being appended to m_path more than once.
		public override void Execute()
		{
			base.Execute();
			PassFailInContext(m_onPass,m_onFail,out m_onPass,out m_onFail);
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + "Glimpse must occur in some context");
			AccessibilityHelper ah = con.Accessibility;
			m_log.isNotNull(ah, makeNameTag() + "Glimpse context not accessible");
			m_path   = Utilities.evalExpr(m_path);
			m_expect = Utilities.evalExpr(m_expect);

			/// Use the path or GUI Model or both
			if (m_select != null && m_select != "" && !m_doneOnce) {
				XmlPath node = SelectToPath(con, m_select); // process m_select
				m_path = node.Path + m_path;
				m_doneOnce = true;  // avoid adding to m_path again on subsequent do-once iterations
			}

			try { Application.Process.WaitForInputIdle(); }
			catch (Win32Exception e)
			{
				m_log.paragraph(makeNameTag() + " WaitForInputIdle: " + e.Message);
			}

			if (m_path != null && m_path != "")
			{
				GuiPath gpath = new GuiPath(m_path);
				m_log.isNotNull(makeNameTag() + gpath, "attribute path='" + m_path + "' not parsed");
				GlimpsePath(ah, gpath);
			}
				else m_log.fail(makeNameTag() + "attribute 'path' or 'select' must be set.");
			Logger.getOnly().result(this);
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
				case "value":	return m_got;
				case "prop":	return m_prop;
				case "expect":	return m_expect;
				default:		return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Returns the value of the property of a GUI Model node or null.
		/// Not all props are explicitely represented in the GUI Model.
		/// For example, visible can only be known at run time.
		/// For these, the string @"N/A" is returned.
		/// </summary>
		/// <param name="node">The GUI Model node representing a GUI element</param>
		/// <returns></returns>
		string getModelPropVal(XmlNode node)
		{
			string propValue = null;
			const string na = @"N/A";
			if (m_prop == null) m_prop = "visible";
			switch (m_prop)
			{
				case "children":
				{ // number of children
					XmlNodeList children = node.ChildNodes;
					propValue = children.Count.ToString();
					break;
				}
				case "handle":
				{
					propValue = na;
					break;
				}
				case "hotkey":
				{
					XmlAttribute hk = node.Attributes["hk"];
					if (hk == null) propValue = "NONE";
					else            propValue = hk.Value.ToLower();
					break;
				}
				case "name":
				{
					propValue = ""; // Utilities.InterpretNodeName(node);
					break;
				}
				case "role":
				{
					propValue = ""; // Utilities.CalcType(node);
					break;
				}
				case "value":
				{ // GUI Model can't determine the current value
					propValue = na;
					break;
				}
				case "visible":
				{ // GUI Model can't determine when something is visible
					propValue = na;
					break;
				}
				case "checked":
				{ // GUI Model can't determine when something is checked
					propValue = na;
					break;
				}
				case "normal":
				{ // GUI Model can't determine when something is normal
					propValue = na;
					break;
				}
				default:
				{
					m_log.fail(makeNameTag() + "property '" + m_prop + "' is not understood");
					break;
				}
			}
			return propValue;
		}

		/// <summary>
		/// Glimpse one gui-path and return the result. If the expected value is not found,
		/// an assertion is raised.
		/// </summary>
		/// <param name="ah">Context accessibility helper, taken as the starting place for gpath.</param>
		/// <param name="gpath">The path through the GUI to the control.</param>
		/// <returns></returns>
		bool GlimpsePath(AccessibilityHelper ah, GuiPath gpath)
		{
			m_Result = true;
			m_got = null;
			IPathVisitor visitor = null;
			ah = gpath.FindInGui(ah, visitor);
			m_Result = GlimpseGUI(ah);
			if (m_Result) base.Finished = true; // teminates do-once
			if ((m_onPass == "assert" && m_Result == true)
				||(m_onFail == "assert" && m_Result == false) )
			{
				if (m_message != null && m_message.HasContent())
					m_log.fail(makeNameTag() + m_message.Read());
				else
				{
					string image = makeNameTag() + "Property [" + m_prop + "]";
					if (m_prop != "absent" && m_prop != "present")
						image += "was [" + m_got + "] expecting [" + m_expect + "]";
					image += "Result = '" + m_Result + "', on-pass='" + m_onPass + "', on-fail='" + m_onFail + "'";
					if (gpath != null)
						m_log.fail(image + " on gPath = " + gpath.toString());
					else
						m_log.fail(image);
				}
			}
			return m_Result;
		}

		/// <summary>
		/// Examines the GUI for the value of the property specified via @prop.
		/// If @expect is set, the @prop value is compared to it.
		/// The result is true when @expect = the @prop GUI value.
		/// When @prop names a boolean property, and @expect is not set, then
		/// the boolean value is the result.
		/// </summary>
		/// <param name="ah">Context accessibility helper, taken as the starting place for gpath. May be null.</param>
		/// <returns>True if @expect = @prop GUI value, false otherwise, or the boolean value of @prop in the GUI.</returns>
		bool GlimpseGUI(AccessibilityHelper ah)
		{
			bool result = true;
			if (m_prop == null) m_prop = "present";
			switch (m_prop)
			{
			case "absent":
				{
					if (ah == null) result = true;
					else result = false;
					break;
				}
			case "children":
				{
					if (m_expect == null) m_expect = "0";
					int exectedValue = 0;
					if (Utilities.IsNumber(m_expect))
						exectedValue = (int)Utilities.GetNumber(m_expect);
					else
						m_log.fail(makeNameTag() + "children requires an integer not '" + m_expect + "'.");
					if (ah == null)
					{
						if (exectedValue == 0) result = true;
						else result = false;
					}
					else
					{   // ah != null
						int val = ah.ChildCount;
						m_got = val.ToString();
						result = val.Equals(exectedValue);
					}
					break;
				}
				case "handle":
				{
					if (m_expect == null) m_expect = "0";
					int exectedValue = 0;
					if (Utilities.IsNumber(m_expect))
						exectedValue = (int)Utilities.GetNumber(m_expect);
					else
						m_log.fail(makeNameTag() + "handle requires a big integer not '" + m_expect + "'.");
					if (ah == null)
					{
						if (exectedValue == 0) result = true;
						else result = false;
					}
					else
					{   // ah != null
						int val = ah.HWnd;
						m_got = val.ToString();
						result = val.Equals(exectedValue);
					}
					break;
				}
				case "hotkey":
				{
					if (ah == null)
					{
						if (m_expect == null) result = true;
						else result = false;
					}
					else
					{   // ah != null
						m_got = ah.Shortcut;
						if (m_got == null || m_got == "")
							m_got = "NONE";
						result = m_got == m_expect; // neither can be null
					}
					break;
				}
				case "name":
				{
					if (ah == null)
					{
						if (m_expect == null) result = true;
						else result = false;
					}
					else
					{   // ah != null
						m_got = ah.Name;
						if (m_got == null || m_got == "") m_got = "NAMELESS";
						result = m_got == m_expect; // neither can be null
					}
					break;
				}
				case "role":
				{
					if (m_expect == null) m_expect = "none";
					m_got = ah.Role.ToString();
					result = m_got == m_expect;
					break;
				}
				case "value":
				{
					if (ah == null)
					{
						if (m_expect == null) result = true;
						else result = false;
					}
					else
					{   // ah != null
						m_got = ah.Value;
						if (m_expect != null && m_expect.StartsWith("rexp#"))
						{
							Regex rx = new Regex(m_expect.Substring(5));
							result = rx.IsMatch(m_got);
							m_log.paragraph(makeNameTag() + "Expect reg exp " + m_expect.Substring(5)
								+ " on " + m_got + " was " + result.ToString());
						}
						else result = m_got == m_expect || (m_got == "" && m_expect == null); // either can be null
					}
					break;
				}
				case "visible":
				{
					if (m_expect == null) m_expect = "True";
					if (ah == null)
					{
						if (m_expect == "True") result = false;
						else result = true;
					}
					else
					{   // ah != null
						result = !(((AccessibleStates.Invisible & ah.States) == AccessibleStates.Invisible) ||
							((AccessibleStates.Offscreen & ah.States) == AccessibleStates.Offscreen));
						m_got = result.ToString();
						result = m_got.ToLower() == m_expect.ToLower();
					}
					break;
				}
				case "checked":
				{
					if (m_expect == null) m_expect = "True";
					if (ah == null)
					{
						if (m_expect == "True") result = false;
						else result = true;
					}
					else
					{   // ah != null
						result = ((AccessibleStates.Checked & ah.States) == AccessibleStates.Checked);
						m_got = result.ToString();
						result = m_got.ToLower() == m_expect.ToLower();
					}
					break;
				}
				case "selected":
				{
					if (m_expect == null) m_expect = "True";
					if (ah == null)
					{
						if (m_expect == "True") result = false;
						else result = true;
					}
					else
					{   // ah != null
						result = ((AccessibleStates.Selected & ah.States) == AccessibleStates.Selected);
						m_got = result.ToString();
						result = m_got.ToLower() == m_expect.ToLower();
					}
					break;
				}
				case "present":
				{
					if (ah == null) result = false;
					else result = true;
					break;
				}
				case "unavailable":
				{
					if (m_expect == null) m_expect = "True";
					if (ah == null)
					{
						if (m_expect == "True") result = true;
						else result = false;
					}
					else
					{   // ah != null
						result = ((AccessibleStates.Unavailable & ah.States) == AccessibleStates.Unavailable);
						m_got = result.ToString();
						result = m_got.ToLower() == m_expect.ToLower();
					}
					break;
				}
				default:
				{
					m_log.fail( makeNameTag() + "property '" + m_prop + "' is not understood");
					m_got = result.ToString();
					result = false;
					break;
				}
			}
			return result;
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
			if (m_prop != null) image += @" prop=""" + m_prop + @"""";
			if (m_expect != null) image += @" expect="""+Utilities.attrText(m_expect)+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_got != null)    image += @" got="""+Utilities.attrText(m_got)+@"""";
			return image;
		}
	}
}
