// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FindExtra.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// Find-extra selects all child model elements of the one specified and examines their GUI
// counterparts. The first element found that is not expected, becomes "extra" and fails.
// This instruction finds GUI elements that are not modeled.
// To find properties of GUI elements that are not modeled, but whose elements are, use glimpse.
// </remarks>

using System;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for FindExtra.
	/// </summary>
	public class FindExtra: GlimpseExtra
	{
		string m_select;

		public FindExtra()
		{
			m_select = null;
		}
		public string Select
		{
			get {return m_select;}
			set {m_select = value;}
		}

		public override void Execute(TestState ts)
		{
			Assert.IsNotNull(m_select,"Find-extra has no select attribute");
			Assert.IsTrue(m_select != "","Find-extra found an empty select attribute");
			XmlElement root = Application.GuiModelRoot;
			XmlNodeList parentList = root.SelectNodes(m_select);
			Assert.IsNotNull(parentList,"Find-extra select = '"+m_select+"' is not valid XPATH");
			Assert.IsTrue(parentList.Count > 0,"Find-extra select = '"+m_select+"' found no matching node");
			Assert.AreEqual(1,parentList.Count,"Find-extra select = '"+m_select+"' found more than one matching node");
			XmlNode parent = parentList[0];
			m_path = Utilities.InterpretSelectedPath(parent);
			if (m_path != null) m_path = Utilities.ValidatePath(m_path);
			m_names = InterpretSelectedNames(parent);
			base.Execute(ts);
		}

		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
				case "select":
					return m_select;
				default:
					return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Uses the select string to query the application GUI model for a list of child gui elements.
		/// </summary>
		/// <param name="select">XPATH indicating a GUI element</param>
		/// <returns>GUI path to the element on the screen</returns>
		string InterpretSelectedNames(XmlNode parent)
		{ // this is the parent of the list to be formed from names of the gui elements
			string names = null;
			if (parent.HasChildNodes == true)
			{ // get the child nodes with name attributes
				foreach ( XmlNode child in parent)
				{
					if (child.NodeType != XmlNodeType.Comment)
					{
						Assert.IsNotNull(child.Attributes,"Find-extra "+m_select+" includes a model node with no attributes.");
						XmlAttribute xa = child.Attributes["name"];
						// a null xa is replaced by #NONE to show it is nameless, like separators can be
						//Assert.IsNotNull("xa, Find-extra "+m_select+" includes a model node with no name attribute.");
						if (names != null) names += "/";
						if (xa == null) names += "#NONE";
						else names += xa.Value;
					}
				}
			}
			return names;
		}
	}
}
