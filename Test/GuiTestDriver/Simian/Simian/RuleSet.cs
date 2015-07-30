// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml;

namespace Simian
{
	/**
	 * Contains a number of rules that work together to accomplish a testable goal.
	 *
	 * @author  Michael Lastufka
	 * @version Oct 17, 2008
	 */
	public class RuleSet
	{
		private string m_name;
		private string m_desc;
		private EmptyElement m_params;
		private ArrayList m_rules;

		public const string NoValue = "#no-value#";

		/**
		 * Constructor for objects of class RuleSet
		 * @param name designation of this rule set
		 * @param desc concise description os this rule set
		 */
		public RuleSet(string name, string desc)
		{
			m_name = name;
			m_desc = desc;
			m_params = new EmptyElement("params");
			m_rules = new ArrayList(3);
		}

		/** Gets the RuleSet's name.
		 * @return the RuleSet's identifier.
		 */
		public string getName() {return m_name;}

		/**
		 * Adds a rule to the rule set.
		 * @param rule the rule to add.
		 */
		public void addRule (Rule rule)
		{
			if (rule != null) m_rules.Add(rule);
		}

		/** Creates a parameter known by the application.
		 * @param name designation of the parameter.
		 */
		public void addParameter(string name)
		{
			m_params.addAttribute(name, NoValue);
		}

		/** Sets a parameter value.
		 * @param name designation of the parameter known by the application.
		 * @param value a valid value - no checking is done here.
		 */
		public void setParameter(string name, string value)
		{
			m_params.addAttribute(name, value);
		}

		/** Gets a parameter value.
		 * @param name designation of the parameter known by the application.
		 * @return the value - no checking is done here.
		 */
		public string getParameter(string name)
		{
			return m_params.getValue(name);
		}

		/// <summary>
		/// Counts the number of RuleSet formal parameters.
		/// </summary>
		/// <returns>The number of formal parameters.</returns>
		public int formals() { return m_params.countAttributes(); }

		/**
		 * Gets the name of a parameter by index number, starting at zero.
		 * @param index a zero-based indicator of the parameter to examine.
		 * @return the name of the indexed parameter or null if not found.
		 */
		public string getParameterName(int index)
		{ return m_params.getAttributeName(index); }

		/**
		 * Gets the value of an parameter by index number, starting at zero.
		 * @param index a zero-based indicator of the parameter to examine.
		 * @return the value of the indexed parameter or null if not found.
		 */
		public string getParameterValue(int index)
		{ return m_params.getAttributeValue(index); }

		/** Gets the rules.
		 * @return All the rules.
		 */
		public ArrayList getRules() { return m_rules; }

		/**
		 * Reads an Xml node representation of a rule-set element.
		 * @param emptyElt the empty xml node.
		 */
		public static RuleSet readXml(XmlNode ruleSetElt)
		{
			RuleSet rs = null;
			if (ruleSetElt == null) return rs;
			Log log = Log.getOnly();
			if (ruleSetElt.HasChildNodes)
			{ // it contains rules
				string id = XmlFiler.getStringAttr((XmlElement)ruleSetElt,"id",NoValue);
				if (id.Equals(NoValue))
				{
					log.writeElt("fail");
					log.writeAttr("node", ruleSetElt.Name);
					log.writeAttr("expected", "id");
					log.writeAttr("was", "not found");
					log.endElt();
					return rs;
				}
				string desc = XmlFiler.getStringAttr((XmlElement)ruleSetElt,"desc",NoValue);
				rs = new RuleSet(id, desc);
				XmlAttributeCollection attrs = ruleSetElt.Attributes;
				for (int a = 0; a < attrs.Count; a++)
				{ // read all attributes
					XmlNode attr = attrs.Item(a);
					string nn = attr.Name;
					if (!nn.Equals("id") && !nn.Equals("desc"))
					{
						rs.setParameter(attr.Name, attr.Value);
					}
				}
				XmlNodeList rules = ruleSetElt.ChildNodes;
				for (int r = 0; r < rules.Count; r++)
				{
					XmlNode ruleN = rules.Item(r);
					if (ruleN.NodeType == XmlNodeType.Element)
					{
						Rule rule = Rule.readXml((XmlElement)ruleN);
						rs.addRule(rule);
					}
				}
			}
			else // this is a truly empty node
			{
				log.writeElt("fail");
				log.writeAttr("node", ruleSetElt.Name);
				log.writeAttr("expected", "rules");
				log.writeAttr("was", "empty");
				log.endElt();
			}
			return rs;
		}

	}
}
