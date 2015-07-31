// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Text;
using System.Xml;

namespace Simian
{
	/// <summary>
	/// A EmptyElement has an name and a set of attributes.
	/// Attribute values are referenced via dot notation name.attrName
	///
	/// @author Michael Lastufka
	/// @version 10-12-2007
	/// </summary>
	public class EmptyElement
	{
		protected readonly string m_name;

		protected ArrayList m_attributes;

		/** Create a EmptyElement with a descriptive name
		 * @param priority Value 0 to 9, 0 not important, 9 most important
		 * @param id designation of the EmptyElement
		 */
		public EmptyElement(string name)
		{
			m_name = name;
			m_attributes = new ArrayList(5);
		}

		/** Gets the EmptyElement's name
		 * @return the EmptyElement's identifier
		 */
		public string getName() { return m_name; }

		/** Creates an attribute or changes its value if it exists
		 * @param name designation of the attribute
		 * @param value value of the attribute to set
		 */
		public void addAttribute(string name, string value)
		{
			Attribute a = findAttribute(name);
			if (a == null)
			{ // add the attribute
				if (m_attributes == null)
					m_attributes = new ArrayList(5);
				a = new Attribute(name, value);
				m_attributes.Add(a);
			}
			else
			{ // it already exists, so change it
				if (value == null) m_attributes.Remove(a);
				else
				{
					a.value = value;
				}
			}
		}

		/**
		 * Removes the attribute from this element.
		 * @param name designation of the attribute
		 */
		public void removeAttribute(string name)
		{
			addAttribute(name, null);
		}

		/** Counts the number of attributes.
		 * @return the number of attributes in the element
		 */
		public int countAttributes()
		{
			if (m_attributes == null) return 0;
			return m_attributes.Count;
		}

		/**
		 * Gets the name of an attribute by index number, starting at zero.
		 * @param index a zero-based indicator of the attribute to examine.
		 * @return the name of the indexed attribute or null if not found.
		 */
		public string getAttributeName(int index)
		{
			if (index < m_attributes.Count)
				return ((Attribute)m_attributes[index]).name;
			else return null;
		}

		/**
		 * Gets the value of an attribute by index number, starting at zero.
		 * @param index a zero-based indicator of the attribute to examine.
		 * @return the value of the indexed attribute or null if not found.
		 */
		public string getAttributeValue(int index)
		{
			if (index < m_attributes.Count)
				return ((Attribute)m_attributes[index]).value;
			else return null;
		}

		/**
		 * Verifies the presence of an attribute by name.
		 * @param name the name of the attribute to verify.
		 * @return true if the named attribute is found.
		 */
		public bool hasAttribute(string name)
		{
			if (name == null || name.Equals("")) return false;
			else return findAttribute(name) != null;
		}

		/** Gets the value of an attribute
		 * @param name designation of the attribute
		 * @retrun the value of the attribute or null
		 */
		public String getValue(string name)
		{
			Attribute p = findAttribute(name);
			if (p != null) return p.value;
			return null;
		}

		/** Determines if this EmptyElement is the same as another.
		 * Priority is ignored, negation is not ignored.
		 * @param ee another EmptyElement that may be equal to this one
		 * @return true if it is the same
		 */
		public bool equals(EmptyElement ee)
		{
			if (ee == null) return false;
			bool same = m_name.Equals(ee.getName());
			if (same) return hasSameAttributes(ee);
			return false;
		}

		/** Determines if this EmptyElement has the same attributes as another.
		 * @param ee another EmptyElement
		 * @return true if it has the same attributes
		 */
		public bool hasSameAttributes(EmptyElement ee)
	{
	   if (ee == null) return false;
	   bool same = countAttributes() == ee.countAttributes();
	   if (same && m_attributes != null)
	   { // count is the same, so if extra, one will not be equal
		   foreach (Attribute a in m_attributes)
		   {
			   Attribute a2 = ee.findAttribute(a.name);
			   if (!a.equals(a2)) return false;
		   }
	   }
	   return same;
	}

		/**
		 * Finds the attribute named
		 * @param name the designation of the attribute desired
		 * @return the attribute or null if not found
		 */
		protected Attribute findAttribute(string name)
	{
	   if (name != null && name != "" && m_attributes != null)
	   {
		   foreach (Attribute a in m_attributes)
		   {
			   if (a.name.Equals(name)) return a;
		   }
	   }
	   return null;
	}

		/**
		 * Makes a deep copy of an empty element.
		 * Contents are not copied.
		 * @return A copy.
		 */
		public EmptyElement copy()
	{
		EmptyElement ee = new EmptyElement(m_name);
		foreach (Attribute a in m_attributes)
		{
		   ee.addAttribute(a.name, a.value);
		}
		return ee;
	}

	/// <summary>
	/// Substitutes the matching formal attribute values of the element
	/// with the values in the substitute attributes.
	/// </summary>
	/// <param name="substitutes">The list of substitute attribute/value pairs</param>
	/// <returns>A deep copy of the empty element with values substituted</returns>
	public EmptyElement SubstituteCopy(ArrayList substitutes)
	{
		EmptyElement xerox = copy();
		// null valued subs remove attrs from the list, so itterate in reverse
		for (int a = xerox.countAttributes()-1; a > -1 ; a--)
		{ // if formal values are found, substitute
			String value = xerox.getAttributeValue(a);
			foreach (Substitute sub in substitutes)
			{ if (sub.formal.Equals(value))
				xerox.addAttribute(xerox.getAttributeName(a),sub.value);
			}
		}
		return xerox;
	}

		/**
		 * Write this EmptyElement to the Log.
		 * @param markTime if true, log with time stamp
		 */
		public void log(bool markTime)
	{
		Log log = Log.getOnly();
		if (markTime) log.writeEltTime(m_name);
		else          log.writeElt(m_name);
		foreach (Attribute a in m_attributes)
		{
			log.writeAttr(a.name, a.value);
		}
		log.endElt();
	}

		/**
		 * Write this EmptyElement to the Log.
		 */
		public void log()
		{
			log(false);
		}

		/**
		 * Make a string image of this EmptyElement.
		 * Derived non-empty elements set content to true then
		 * use imageEnd() to close the element.
		 * @param content true if the derived image is not empty.
		 * @return The image of this element as empty or a start tag.
		 */
		protected string image(bool content)
	{
	   string image = "<" + m_name;
	   foreach (Attribute a in m_attributes)
	   {
		   image += " " + a.name;
		   image += "=\"" + a.value + "\"";
	   }
	   if (!content) image += "/";
	   image += ">";
	   return image;
	}

		/**
		 * Make a string image of this EmptyElement.
		 * @return The image of this element as empty.
		 */
		public string image()
		{
			return image(false);
		}

		/**
		 * Make a string image of the end of this Element.
		 * Used by derived non-empty elements to close the element.
		 * @return The image of this element as an end tag.
		 */
		protected string imageEnd()
		{
			return "</" + m_name + ">";
		}


		/// <summary>
		/// Attributes have a name and value.
		/// </summary>
		protected class Attribute
		{
			public String name;
			public String value;
			public Attribute(String name, String value)
			{
				this.name = name;
				this.value = value;
			}

			/// <summary>
			/// Determines if this attribute is the same in name and value as another.
			///
			/// </summary>
			/// <param name="attr">another attribute that may be equal to this one</param>
			/// <returns>true if it is the same in name and value</returns>
			public bool equals(Attribute attr)
			{
				if (attr == null) return false;
				bool same = false;
				if (name != null && name != "")
					same = name.Equals(attr.name);
				if (same && value != null)
					same = value.Equals(attr.value);
				return same;
			}
		}

		public static EmptyElement readXml(XmlNode emptyElt)
		{
			EmptyElement ee = null;
			if (emptyElt == null) return ee;
			if (emptyElt.HasChildNodes)
			{ // this is not empty!
				Log log = Log.getOnly();
				log.writeElt("fail");
				log.writeAttr("node", emptyElt.Name);
				log.writeAttr("expected", "empty");
				log.endElt();
			}
			else // this is a truly empty node
			{
				ee = new EmptyElement(emptyElt.Name);
				XmlAttributeCollection attrs = emptyElt.Attributes;
				for (int a = 0; a < attrs.Count; a++)
				{ // read all attributes
					XmlNode attr = attrs.Item(a);
					ee.addAttribute(attr.Name, attr.Value);
				}
			}
			return ee;
		}

		/**
		 * Parses the XML representation of a parent with empty children.
		 * @param parent the XML node with subnodes.
		 * @return the empty children that were read or null.
		 */
		public static ArrayList readEmptyChildren(XmlNode parent)
		{
			ArrayList emptyList = null;
			XmlNodeList children = parent.ChildNodes;
			for (int c = 0; c < children.Count; c++)
			{ // read children
				XmlNode child = children.Item(c);
				if (child.NodeType == XmlNodeType.Element)
				{
					EmptyElement emptyChild = EmptyElement.readXml((XmlElement)child);
					if (emptyList == null) emptyList = new ArrayList();
					emptyList.Add(emptyChild);
				}
			}
			return emptyList;
		}
	}
}