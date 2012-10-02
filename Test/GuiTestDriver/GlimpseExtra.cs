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
// File: GlimpseExtra.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// Checks to see if there are extra GUI elements of the selected type.
// Glimpse-extra selects all child model elements of the one specified and examines their GUI
// counterparts. The first element found that is not expected, becomes "extra" and fails.
// This instruction finds GUI elements that are not modeled.
// To find properties of GUI elements that are not modeled, but whose elements are, use glimpse.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for GlimpseExtra.
	/// </summary>
	public class GlimpseExtra : CheckBase
	{
		protected string m_names;
		string m_extra;
		ArrayList m_list = null;

		public GlimpseExtra(): base()
		{
			m_names  = null;
			m_extra  = null;
			m_list   = null;
			m_tag    = "glimpse-extra";
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
			InterpretMessage(xn.ChildNodes);
			return true;
		}

		public string Names
		{
			get {return m_names;}
			set {m_names = value;}
		}

		// When used in a do-once instruction, this call is repeated.
		// Note the code that keeps m_select from being prepended to m_path more than once.
		public override void Execute()
		{
			base.Execute();
			if (m_path   != null && m_path   == "") m_path   = null;
			if (m_select != null && m_select == "") m_select = null;
			if (m_selectPath != null && m_selectPath == "") m_selectPath = null;
			if (m_names != null && m_names == "") m_names = null;
			// must have:
			// one of select or names to provide a list to check against
			// with names, one of path or selectPath to get the place to check in the GUI
			m_log.isTrue(m_select != null || m_names != null, makeNameTag() + " must have a 'names' or 'select' attribute.");
			if (m_names != null)
				m_log.isTrue(m_path != null || m_selectPath != null, makeNameTag() + " must have a 'path' or 'selectPath' attribute with 'names'.");
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + " must occur in some context");
			m_path = Utilities.evalExpr(m_path);
			m_selectPath = Utilities.evalExpr(m_selectPath);
			m_select = Utilities.evalExpr(m_select);
			// set the gui path from path or select
			if (m_select != null && !m_doneOnce)
			{  // set m_names and possibly m_path

				m_log.paragraph(makeNameTag() + " creating selection targets via " + m_select);
				XmlNodeList pathNodes = Instructionator.selectNodes(con, m_select, makeNameTag());
				m_log.isNotNull(pathNodes, makeNameTag() + " select='" + m_select + "' returned no model node");
				// The @select text may have selected a string that is itself xPath!
				// If so, select on that xPath
				if (pathNodes.Count == 1 && pathNodes.Item(0).NodeType == XmlNodeType.Text)
				{ // this text node should be an xpath statement
					string xPathImage = pathNodes.Item(0).Value;
					m_log.paragraph(makeNameTag() + " selected a text node with more XPATH: " + xPathImage);
					pathNodes = Instructionator.selectNodes(con, xPathImage, makeNameTag() + " selecting " + xPathImage);
					m_log.isNotNull(pathNodes, makeNameTag() + " selecting " + xPathImage + " from select='" + m_select + "' returned no model node");
				}
				if (pathNodes.Count >= 1)
				{ // there are some nodes - make a list
					m_names = null;
					foreach (XmlNode xname in pathNodes)
					{
						if (m_names == null)
						{
							if (m_path == null)
							{
								XmlPath xPath = new XmlPath(xname.ParentNode);
								m_path = xPath.Path;
							}
						}
						else m_names += "/";
						string name = XmlFiler.getAttribute(xname, "name");
						if (name == null || name == "") m_names += "#NONE";
						else m_names += name;
					}
				}
				m_doneOnce = true; // avoid adding to m_path again on subsequent do-once iterations
			}
			if (m_selectPath != null && m_selectPath != "")
			{
				XmlPath node = SelectToPath(con, m_selectPath); // process m_select
				m_path = node.Path + m_path;
			}

			GuiPath gpath = new GuiPath(m_path);
			m_log.isNotNull(gpath, makeNameTag() + " attribute path='" + m_path + "' not parsed");
			if (m_names != null) m_list = Utilities.ParsePath(m_names);
			PassFailInContext(m_onPass,m_onFail,out m_onPass,out m_onFail);
			AccessibilityHelper ah = con.Accessibility;
			m_log.isNotNull(ah, makeNameTag() + " context not accessible");
			//check to see if it is visible
			m_Result = false;

			try { Application.Process.WaitForInputIdle(); }
			catch (Win32Exception e)
			{ m_log.paragraph(makeNameTag() + " WaitForInputIdle: " + e.Message);
			  m_Result = false;}

			if (m_Result) ah = gpath.FindInGui(ah, null);
			if (ah != null) m_Result = GlimpseGUI(ah);
			Finished = true; // tell do-once it's done

			if ((m_onPass == "assert" && m_Result == true)
				||(m_onFail == "assert" && m_Result == false) )
			{
				if (m_message != null)
					m_log.fail(m_message.Read());
				else
					m_log.fail(makeNameTag() + " Result = '" + m_Result + "', on-pass='" + m_onPass + "', on-fail='" + m_onFail + "'");
			}
			Logger.getOnly().result(this);
		}

		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
				case "extra":	return m_extra;
				case "names":	return m_names;
				default:		return base.GetDataImage(name);
			}
		}

		bool GlimpseGUI(AccessibilityHelper ah)
		{
			AccessibilityHelper ah2 = ah;
			if (ah.Role == AccessibleRole.MenuItem)
			{ // for menu items, drop to popup menu role, then its children
				foreach (AccessibilityHelper child in ah)
				{
					ah2 = child;
					break;
				}
			}
			if (ah2.ChildCount > 0)
			{
				// check each child against the list.
				foreach (AccessibilityHelper child in ah2)
				{
					if (child.States != AccessibleStates.Invisible)
					{
						string Name = child.Name;
						if (Name == null || Name == "") Name = "#NONE";
						if (!m_list.Contains(Name))
						{
							m_extra = Name;
							break;
						}
					}
				}
			}
			else m_extra = "#noChild";
			return m_extra == null;
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
			if (m_names != null)  image += @" names="""+Utilities.attrText(m_names)+@"""";
			if (m_list != null)   image += @" list="""+Utilities.attrText(Utilities.ArrayListToString(m_list))+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_extra != null)  image += @" extra="""+Utilities.attrText(m_extra)+@"""";
			return image;
		}
	}
}
