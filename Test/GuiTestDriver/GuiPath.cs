// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
//using System.Collections;
using System.Windows.Forms;
using NUnit.Framework;

namespace GuiTestDriver
{
	public class GuiPath
	{
		GuiPath m_parent = null;
		GuiPath m_child = null;
		private string m_name = null;
		private AccessibleRole m_role = AccessibleRole.None;
		private string m_type = null;
		private string m_varId;
		private int m_Nth = 1;

		/// <summary>
		/// A linked list node that represents one step in a GUI path.
		/// A step in the typedPath is represented by this node.
		/// Following steps generate subsequent GuiPath nodes recursively.
		/// </summary>
		/// <param name="typedPath">a typed path</param>
		public GuiPath(string typedPath)
		{
			// remove the first step from the typedPath and process it
			int last = 0;
			string step = Utilities.GetFirstStep(typedPath, out last);
			Utilities.SplitTypedToken(step, out m_name, out m_type);
			if (m_type == "value" && (m_name == null || m_name == ""))
				m_name = ""; // want it to be empty
			else if (m_name == null || m_name == "") m_name = "NAMELESS";
			try
			{
				int roleValue = Convert.ToInt32(m_type);
				m_role = (AccessibleRole)roleValue;
			}
			catch (Exception)
			{
				m_role = Utilities.TypeToRole(m_type);
			}

			// Parse the name
			string name;
			string varId;
			m_Nth = parseIndex(m_name, out name, out varId);
			if (m_name == name) m_Nth = 1;
			m_name = name;
			m_varId = varId;

			if (m_type == "para" || m_type == "line")
			{
				try { m_Nth = Convert.ToInt32(m_name); }
				catch (System.FormatException)
				{ Assert.Fail(m_type + ": needs to be followed by an integer, not " + m_name); }
				if (m_type == "para") m_name = "Paragraph";
				if (m_type == "line") m_name = "Srting";
			}

			// if not consumed, split off the end of the path as a child path
			if (1 + last < typedPath.Length)
			{
				m_child = new GuiPath(typedPath.Substring(last + 1));
				m_child.m_parent = this;
			}
		}

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}
		public AccessibleRole Role
		{
			get { return m_role; }
			set { m_role = value; }
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public int Nth
		{
			get { return m_Nth; }
			set { m_Nth = value; }
		}
		public string VarId
		{
			get { return m_varId; }
			set { m_varId = value; }
		}
		public GuiPath Prev
		{
			get { return m_parent; }
		}
		public GuiPath Next
		{
			get { return m_child; }
		}
		public int NumBelow
		{
			get { if (m_child != null) return m_child.NumBelow + 1; else return 0; }
		}

		/// <summary>
		/// Find the GUI element represented by this path step in the application GUI
		/// beginning from the context specified.
		///
		/// </summary>
		/// <param name="ahContext">The context to start the search from</param>
		/// <param name="visitor">The class with the visitNode() method to
		/// apply to each node except the last found, may be null</param>
		/// <returns>The AccessibilityHelper of the last node found or null if the path does not exist</returns>
		public AccessibilityHelper FindInGui(AccessibilityHelper ahContext, IPathVisitor visitor)
		{
			AccessibilityHelper ah = ahContext;

			if (m_name == "#focus")
			{ // get the ah from the focused Gui element
				ah = ah.GetFocused;
			}
			else
				ah = ah.SearchPath(this, visitor);
			return ah;
		}

		/// <summary>
		/// Parses the expression "[n]" or "[*]" or [*id] or [$varId] in a pair name.
		/// If a variable is found ($), it's value is treated as if it were read instead.
		/// returns the part of the input without the expression and n, 0 for * and id.
		/// </summary>
		/// <param name="pName">The GuiPath pair name to be parsed</param>
		/// <param name="name">pName without the index expression</param>
		/// <param name="varId">The name of a variable that is to hold the '*' index value</param>
		/// <returns>the number in square brackets or zero.</returns>
		static private int parseIndex(string pName, out string name, out string varId)
		{
			name = "NONE";
			varId = null;
			int Nth = 0;
			int posEnd = 0;
			// find the last ] and work forward
			if (pName != null && pName != "") posEnd = pName.LastIndexOf(']');
			if (posEnd > 1) // this might be an indexed token
			{
				bool found = false;
				int posN = posEnd - 1;
				while (!found && posN > -1) found = pName[posN--] == '[';
				if (found)
				{
					posN += 2; // start of the index
					// if the content is not a number, leave it be
					string image = pName.Substring(posN, posEnd - posN);
					if (image[0] == '*') // Nth = 0
					{
						if (image.Length > 1) varId = image.Substring(1, image.Length - 1);
					}
					else
					{
						if (image[0] == '$')
						{ // evaluate the variable
							image = Utilities.evalExpr(image);
						}
						// image is a number at this point
						try { Nth = Convert.ToInt32((image)); }
						catch (FormatException)
						{ Nth = 0; posN = pName.Length + 1; }
					}
					name = pName.Substring(0, posN - 1);
				}
				else name = pName;
			}
			else name = pName;
			return Nth;
		}

		/// <summary>
		/// Used to test parseIndex. nameExpect and numExpect are the truth values.
		/// </summary>
		/// <param name="pName">The GuiPath pair name to be parsed</param>
		/// <param name="nameExpect">pName without the index expression</param>
		/// <param name="numExpect">The index in the []'s</param>
		/// <param name="varIdExpect">The variable id in the []'s</param>
		/// <returns>true when name is nameExpect and index is numExpect.</returns>
		static public bool parseIndexTesting(string pName, string nameExpect, int numExpect, string varIdExpect)
		{
			string name;
			string varId;
			int num = parseIndex(pName, out name, out varId);
			return (name == nameExpect) && (num == numExpect) && (varId == varIdExpect);
		}

		/// <summary>
		/// Counts the number of nodes connected to this one
		/// </summary>
		/// <returns>number of GuiPath nodes below this one plus this one</returns>
		public int count()
		{
			if (m_child == null) return 1;
			return m_child.count() + 1;
		}

		/// <summary>
		/// Converts the gPath to a path of typed tokens
		/// </summary>
		/// <returns>A path of typed tokens</returns>
		public string toString()
		{
			string path = m_type + ":" + m_name;
			if (m_varId != null && m_varId != "") path += "[" + m_varId + "=" + m_Nth + "]";
			else path += "[" + m_Nth + "]";
			if (m_child != null) path += @"/" + m_child.toString();
			return path;
		}

	}

}
