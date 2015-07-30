// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml;

namespace Simian
{
/**
 * Each sensact rule set works to a goal action of "done".
 * Rules consist of conditions reported by sensors and actions
 * to be taken when the conditions are satisfied. Rules are
 * written assuming the actions will continue until discontinued
 * by another action, likely in another rule.
 * If a rule continues to be the first in order to be satisfied,
 * it is only fired the first time unless fire="always" is set.
 * The default is fire="once".
 *
 * A rule xml image looks like:
 *
 * <rule id="" desc="" fire="once|always">
 *   <!-- sensations -->
 *   <when>
 *     <{name} param1="expression1" ... paramN="expressionN"/>
 *        :      :          :             :          :
 *     <{name} param1="expression1" ... paramN="expressionN"/>
 *   </when>
 *   <!-- actions -->
 *   <{name} param1="expression1" ... paramN="expressionN"/>
 *      :      :          :             :          :
 *   <{name} param1="expression1" ... paramN="expressionN"/>
 * </rule>
 *
 * Sensations and actions are evaluated in left-to-right order.
 * Complex logic and model references are hidden in sensor predicates.
 * Sensations and actions have "simple" parameters.
 * Any sensations and actions not consumed by sensact are passed on to the
 * calling app.
 *
 * Expressions: can be XPath, a litteral, variable expression or math
 * expression using variables that are interpreted by the application
 * that receives them.
 *
 * Senact sensors: must evaluate to true or false.
 *
 * <starting/> - senses initial execution of containing rule set.
 *
 * <{name} param1="expression1" ... paramN="expressionN"/> - form of a generic sensation
 *
 * Senact actions:
 *
 * <{name} set="expression"/> - set variable name to the expression.
 *
 * <{name} param1="expression1" ... paramN="expressionN"/> - form of a generic action
 *
 * <done/> - return to the parent level as successful
 *
 * <fail/> -  log(predicate and arguments, failed, reason) then return to the parent level as failed
 *
 * <try> {...} </try> - randomly selects an action in the list to execute.
 *                 If the action succeeds, no more actions are executed.
 *                 If it returns failed, it is removed from the list and try { .. } executes again.
 *                 If try {} has nothing on its list, it returns the fail action.
 *
 * <lookup (...) - looks up the item in the gui model and returns info about it.
 *
 * log(...) - writes data to a log file.
 *
 * @author  Michael Lastufka
 * @version Oct 2, 2008
 */
public class Rule
{
	private const String NoId = "**No id**";
	private const String NoDesc = "**No description**";

	private String m_desc = NoDesc;
	private String m_id = NoId;
	private readonly bool m_fireAlways;
	private ArrayList m_when;
	private ArrayList m_actions;

	/**
	 * Constructor for objects of class Rule
	 * Must have at least one when condition and one action.
	 * @param id Short designation (often coded) for this rule
	 * @param desc Longer description of purpose
	 * @param when List of conditions to satisfy
	 * @param actions List of actions to take
	 */
	public Rule(String id, String desc, bool fireAlways,
				ArrayList when,
				ArrayList actions)
	{
		m_id = id;
		m_desc = desc;
		m_fireAlways = fireAlways;
		m_when = when;
		m_actions = actions;
	}

	/**
	 * Constructor for objects of class Rule
	 * Must have at least one when condition and one action.
	 * @param id Short designation (often coded) for this rule
	 * @param desc Longer description of purpose
	 * @param when List of conditions to satisfy
	 * @param actions List of actions to take
	 */
	public Rule(String id, String desc,
				ArrayList when,
				ArrayList actions)
		: this(id, desc, false, when, actions)
	{    }

	/**
	 * Evaluates the rule using sensor data references via the
	 * application interface. The application must supply a value
	 * for the sensor data required. Sensor data may be stale, estimated
	 * or a status word, like NaN.
	 * A copy of rule conditions are sent to the sensor.
	 * @param sensorExec The interface to the application sensors.
	 * @param substitutes List of formal parameters and their new values.
	 * @return true if the rule conditions are satisfied, false otherwise.
	 */
	public bool evaluate (ISensorExec sensorExec, ArrayList substitutes)
	{
		bool result = true; // test this hypothesis
		foreach (EmptyElement cond in m_when)
		{
			if (!cond.getName().Equals("starting"))
			{ // rule conditions are anded together
				EmptyElement newCond = cond.SubstituteCopy(substitutes);
				// Log log = Log.getOnly(null);
				// newCond.log();
				result &= sensorExec.sensation(newCond);
			}
		}
		return result;
	}

	/**
	 * Tells whether this rule should always fire.
	 * By default rules fire only once when they are the
	 * first in a sequence to be satisfied continually.
	 * @return true if the rule is to be fired each time it is satisfied.
	 */
	public bool isFireAlways() { return m_fireAlways;}

	/**
	 * Gets this rule's actions.
	 * @return actions to perform.
	 */
	public ArrayList getActions() { return m_actions; }

	/**
	* Write this Rule to the Log.
	* @param markTime if true, log with time stamp
	* @param markActions if false, don't log actions.
	* @param substitutes List of formal parameters and their new values.
	*/
	private void log(bool markTime, bool markActions, ArrayList substitutes)
	{
		Log log = Log.getOnly();
		if (markTime) log.writeEltTime("rule");
		else          log.writeElt("rule");
		log.writeAttr("id", m_id);
		if (m_desc != null) log.writeAttr("desc", m_desc);

		log.writeElt("when");
		foreach (EmptyElement w in m_when)
		{
			EmptyElement copyEl = w.SubstituteCopy(substitutes);
			copyEl.log();
		}
		log.endElt(); // </when>

		if (markActions) foreach (EmptyElement a in m_actions) a.log();

		log.endElt(); // </rule>
	}

	/**
	* Write this Rule to the Log.
	* @param markTime if true, log with time stamp
	* @param substitutes List of formal parameters and their new values.
	*/
	public void log(bool markTime, ArrayList substitutes)
	{
		log(markTime, true, substitutes);
	}

	/**
	* Write this Rule to the Log.
	* @param substitutes List of formal parameters and their new values.
	*/
	public void log(ArrayList substitutes)
	{
		log(false, true, substitutes);
	}

	/**
	* Write this Rule condition to the Log.
	* @param markTime if true, log with time stamp
	* @param substitutes List of formal parameters and their new values.
	*/
	public void logCondition(bool markTime, ArrayList substitutes)
	{
		log(markTime, false, substitutes);
	}

	/**
	* Write this Rule condition to the Log.
	* @param substitutes List of formal parameters and their new values.
	*/
	public void logCondition(ArrayList substitutes)
	{
		log(false, false, substitutes);
	}

	/**
	 * Make a string image of this Rule.
	 * To see the actions set actions to true.
	 * @param actions true if the derived image is not empty.
	 * @return The image of this Rule possibly with its actions.
	 */
	protected String image(bool actions)
	{
		String image = "<rule" + " id=\""+m_id+"\"";
		if (m_desc != null)
		{
			image += " desc=\""+m_desc+"\">";
		}
		image += "<when>";
		foreach (EmptyElement w in m_when)
		{ image += w.image(); }
		image += "</when>";
		if (actions)
		{
		   foreach (EmptyElement a in m_actions)
		   { image += a.image(); }
		}
		image += "</rule>";
		return image;
	}


	/**
	 * Parses the XML representation of a rule and creates a Rule
	 * object based on it.
	 * @param ruleElt the XML <rule> node and subnodes.
	 * @return the rule that was read or null.
	 */
	public static Rule readXml(XmlElement ruleElt)
	{
		Rule rule = null;
		Log log = Log.getOnly();
		if (ruleElt.Name.Equals("rule"))
		{   // get <when>
			XmlNodeList nodes = ruleElt.ChildNodes;
			if (nodes == null || 0 == nodes.Count) return rule;
			String id = XmlFiler.getStringAttr(ruleElt, "id", NoId);
			String desc = XmlFiler.getStringAttr(ruleElt, "desc", NoDesc);
			String fireA = XmlFiler.getStringAttr(ruleElt, "fire", NoDesc);
			bool fire = fireA.Equals("always");
			ArrayList when = null;
			ArrayList actions = null;
			for (int n = 0; n < nodes.Count; n++)
			{ // one when and many action elements
				XmlNode node = nodes.Item(n);
				if (node.Name.Equals("when") && when == null)
				{ // process the one when element
					when = EmptyElement.readEmptyChildren(node);
				}
				else if (node.Name.Equals("when"))
				{ // too many when elements
					log.writeElt("fail");
					log.writeAttr("rule",id);
					log.writeAttr("when","multiple");
					log.endElt();
					when = null;
				}
				else
				{ // an action element
					if (node.NodeType == XmlNodeType.Element)
					{
						EmptyElement empty = EmptyElement.readXml(node);
						if (actions == null) actions = new ArrayList();
						actions.Add(empty);
					}
				}
			}
			rule = new Rule(id, desc, fire, when, actions);
		}
		return rule;
	}
}
}
