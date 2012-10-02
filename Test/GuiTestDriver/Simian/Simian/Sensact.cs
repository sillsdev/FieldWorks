using System;
using System.Collections;
using System.Xml;

namespace Simian
{
	/**
	 * Rules guided by sensor data fire actions to acheive a goal state
	 * of "done". The sensor data and actions are provided by the application.
	 * Rules can be recursive, causing nested instances of senacts to opperate.
	 * This mechanism allows an added dimension of modality and opportunism.
	 * A rule's actions continue until the rule is no longer the first satisfied rule.
	 *
	 * Create a Sensact using a file of rule-sets, sensor and action interfaces.
	 * Call setGoal to activate the rule-set that can achieve the goal.
	 * Call act to use the sensors to issue actions that achieve the goal.
	 * A log can be written to review the history of goals, sensations and actions.
	 *
	 * @author  Michael Lastufka
	 * @version Oct 2, 2008
	 */
	public class Sensact
	{

		private ArrayList m_ruleSets = new ArrayList(10);
		private RuleSet m_ruleSet;
		private ArrayList m_rules;
		private ISensorExec m_sensors;
		private IActionExec m_actions;
		private static EmptyElement m_goal; // may be supplied in a file
		private ArrayList m_substitutes;

		private Log m_log;     // the logger

		/**
		 * Constructor for objects of class Sensact reading rule-sets from a file.
		 * @param rulesFile The rule sets for these sensor guided actions.
		 * @param sensorExec The interface to the application sensors.
		 * @param actionExec The interface to the application actions.
		 */
		public Sensact(string rulesFile,
					   ISensorExec sensorExec, IActionExec actionExec)
		{
			ArrayList ruleSets = readXml(rulesFile);
			Init(ruleSets, sensorExec, actionExec);
		}

		/**
		 * Base constructor for objects of class Sensact.
		 * One of the rule sets must be active, but only one.
		 * It must contain goal action parameters to be acted on.
		 * @param ruleSets The rule sets for these sensor guided actions.
		 * @param sensorExec The interface to the application sensors.
		 * @param actionExec The interface to the application actions.
		 */
		public Sensact(ArrayList ruleSets,
					   ISensorExec sensorExec, IActionExec actionExec)
		{
			Init(ruleSets, sensorExec, actionExec);
		}

		/**
		 * Base initializer for objects of class Sensact.
		 * One of the rule sets must be active, but only one.
		 * It must contain goal action parameters to be acted on.
		 * @param ruleSets The rule sets for these sensor guided actions.
		 * @param sensorExec The interface to the application sensors.
		 * @param actionExec The interface to the application actions.
		 */
		public void Init(ArrayList ruleSets,
					   ISensorExec sensorExec, IActionExec actionExec)
		{
			m_ruleSets = ruleSets;
			m_sensors = sensorExec;
			m_actions = actionExec;
			m_log = Log.getOnly();
		}

		/**
		 * Sets the goal for the rule-sets supplied.
		 * @param A goal with the name of a rule-set and parameters.
		 * @return true if the goal can be met by a rule-set.
		 */
		public bool setGoal(EmptyElement goal)
		{
			if (m_ruleSets == null || m_ruleSets.Count == 0)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("rule-set","none");
				m_log.endElt();
				return false;
			}
			if (m_goal == null && goal == null)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("goal","none");
				m_log.endElt();
				return false;
			}
			if (goal != null) m_goal = goal; // overwrite m_goal
			else              goal = m_goal;
			m_log.writeEltTime("new-goal");
			goal.log();
			m_log.endElt();
			string name = goal.getName();
			foreach (RuleSet rs in m_ruleSets)
			{   if (name.Equals(rs.getName()))
				{ m_ruleSet = rs; break; } // skip the rest of the rule-sets
			}
			if (m_ruleSet == null)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("goal","no rule-set can solve");
				m_log.endElt();
				return false;
			}
			m_substitutes = new ArrayList(2);
			for (int p = 0; p < m_ruleSet.formals(); p++)
			{ // goal attributes are named the same as rule-set parameters
				string paramName = m_ruleSet.getParameterName(p);
				string value = goal.getValue(paramName);
				string formal = m_ruleSet.getParameterValue(p);
				m_substitutes.Add(new Substitute(formal,value));
			}
			m_rules = m_ruleSet.getRules(); // use the active rule set
			return true;
		}

			/**
			 * Perform goal-driven actions based on sensor readings.
			 * Performance continues until a "done" action is encountered.
			 * Performance may be interrupted if a "fail" action,
			 * runtime error or timeout occurs.
			 * Rule order is important. Rules are evaluated in order.
			 * The most difficult to evaluate should be the first rule,
			 * the easiest to satisfy, the last.
			 * @return true if the "done" action is reached, otherwise false.
			 */
			public bool act()
		{
			bool alive     = true;
			bool result    = false;
			Rule lastFired = null;
			while (alive)
			{   // evaluate the rules in order.
				// the same rule can not fire twice in a row
				foreach (Rule rule in m_rules)
				{
					if (rule.evaluate(m_sensors, m_substitutes))
					{   // fire: get the rule's actions
						if (rule == lastFired && !rule.isFireAlways())
						{   break; }// skip the rest of the rules
						lastFired = rule;
						rule.logCondition(true, m_substitutes);
						ArrayList actions = rule.getActions();
						// foreach action:
						foreach (EmptyElement act in actions)
						{   // log the action.
							EmptyElement action = act.SubstituteCopy(m_substitutes);
							action.log(true);
							// if it is a sensact action, consume it
							string name = action.getName();
							if (name.Equals("done")) return true;
							if (name.Equals("fail")) return false;
							// if (name.Equals("try")) {}
							// if (name.Equals("lookup")) {}
							// if (name.Equals("log")) {}
							// does the action activate another rule-set?
							RuleSet ActivateRs = null;
							foreach (RuleSet rs in m_ruleSets)
							{   if (name.Equals(rs.getName()))
								{ ActivateRs = rs; break; } // skip the rest of the rule-sets
							}
							if (ActivateRs != null)
							{   // instantiate another Sensact to use the rule-set
								// suspend this rule-set, activate rs.
								result = doSubGoal(ActivateRs, action);
								alive = result;
							}
							else
							{ // otherwise, send it to the appliction to exectue
								result = m_actions.doAction(action);
								alive = result;
							}
							if (!alive) break;
						} // end of for each action
						break; // skip the rest of the rules
					} // end of if rule fired
				} // end for each rule
			}
			return result;
		}

		private bool doSubGoal(RuleSet ruleSet, EmptyElement goal)
		{
			Sensact subGoal = new Sensact(m_ruleSets, m_sensors, m_actions);
			subGoal.setGoal(goal);
			return subGoal.act();
		}

		/**
		 * Reads an Xml file of rule-sets and a goal.
		 * @return A list of the rule-sets.
		 */
		public static ArrayList readXml(string rulesFile)
		{
			ArrayList ruleSets = null;
			XmlElement rulesDoc = XmlFiler.readXmlFile(rulesFile, "rules", ".");
			XmlNodeList ruleSetsN = rulesDoc.ChildNodes;
			for (int i = 0; i < ruleSetsN.Count; i++)
			{
				XmlNode rsN = ruleSetsN.Item(i);
				if (rsN.NodeType == XmlNodeType.Element)
				{
					if (rsN.Name.Equals("rule-set"))
					{
						RuleSet rs = RuleSet.readXml((XmlElement)rsN);
						if (rs != null)
						{
							if (ruleSets == null) ruleSets = new ArrayList(3);
							ruleSets.Add(rs);
						}
						else
						{
							Log log = Log.getOnly();
							log.writeEltTime("fail");
							log.writeAttr("rule-set", "not read");
							log.endElt();
						}
					}
					else // it's the goal action
					{
						m_goal = EmptyElement.readXml((XmlElement)rsN);
					}
				}
			}
			return ruleSets;
		}

	}
}
