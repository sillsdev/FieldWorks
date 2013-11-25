// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Condition.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace GuiTestDriver
{
	/// <summary>
	/// Spceifies a condition clause and evaluates it.
	/// </summary>
	public class Condition
	{
		string m_arg1 = null;
		string m_is   = null;
		string m_arg2 = null;
		ArrayList m_children = null;

		public Condition()
		{
		}

		public string Of
		{
			get {return m_arg1;}
			set {m_arg1 = value;}
		}

		public string To
		{
			get {return m_arg2;}
			set {m_arg2 = value;}
		}

		public string Is
		{
			set {m_is = value;}
			get {return m_is;}
		}

		public void AddCondition(Condition cond)
		{
			Assert.IsNotNull(cond,"A null child condition can't be added to a condition");
			m_children.Add(cond);
		}

		public bool Evaluate()
		{
			Logger log = Logger.getOnly();
			// base.Execute(ts); // so far, nothing accesses the process, so don't wait
			bool result = false;
			// evaluate this condition
			if (m_is != null)
			{
				string arg1 = Utilities.evalExpr(m_arg1);
				if (arg1 == null) arg1 = "#not-exists#";
				string arg2 = Utilities.evalExpr(m_arg2);
				if (arg2 == null) arg2 = "#not-exists#";
				string img = image(arg1, arg2);
				log.paragraph(img);
				switch (m_is.ToLower()) // allow mixed case
				{
					case "equal": // diatic
						log.paragraph("equal: Is "+arg1+" = "+arg2+"?");
						if (arg1 != "#not-exists#" && arg2 != "#not-exists#")
							result = arg1.Equals(arg2);
						else Assert.Fail("Condition is='equal' requires of[get] and to[with].");
						break;
					case "true": // monatic
						log.paragraph("true: Is "+arg1.ToLower()+" "+true.ToString().ToLower()+"?");
						if (arg1 != "#not-exists#")
							result = arg1.ToLower().Equals(true.ToString().ToLower());
						break;
					case "false": // monatic
						log.paragraph("false: Is "+arg1.ToLower()+" "+false.ToString().ToLower()+"?");
						if (arg1 != "#not-exists#")
							result = arg1.ToLower().Equals(false.ToString().ToLower());
						break;
					default: // not an operator, perhaps a literal or number to equate to arg1
						if (Utilities.IsLiteral(m_is) == true)
						{
							string arg = Utilities.GetLiteral(m_is);
							log.paragraph("Lit: Is "+arg1+" = "+arg+"?");
							result = arg1.Equals(arg);
						}
						else if (Utilities.IsNumber(m_is) == true)
						{ // may need to convert to number and back to normalize
							log.paragraph("Num: Is " + arg1 + " = " + m_is + "?");
							result = arg1.Equals(m_is);
						}
						else // Assume this is an unquoted literal
						{
							string arg = Utilities.evalExpr(m_is);
							if (arg.Equals("#not-exists#"))
								Assert.Fail("Condition 'is' value '" + m_is + "' not known");
							if (arg != null && arg.StartsWith("rexp#"))
							{
								Regex rx = new Regex(arg.Substring(5));
								result = rx.IsMatch(arg1);
								log.paragraph("Reg. Exp.: Is " + arg1 + " matches " + arg.Substring(5) + " was " + result.ToString());
							}
							else
							{
								log.paragraph("Assumed Lit: Is " + arg1 + " = " + arg + "?");
								result = arg1.Equals(arg);
							}
						}
						break;
				}
			}
			log.paragraph(@"condition result="""+result.ToString()+@"""");
			// evaluate the child conditions if this one was false
			if (result == false && m_children != null)
			{ // effectively "or" to the children
				result = Condition.EvaluateList(m_children);
			}
			return result;
		}

		public static bool EvaluateList(ArrayList condList)
		{ // Evaluate the conditions: siblings are and'ed, children are short-circuit or'ed.
			bool result = false;
			if (condList != null)
			{
				result = true; // if not the "and" below will fail
				IEnumerator icond = condList.GetEnumerator();
				while (icond.MoveNext() == true)
				{
					Condition cond = (Condition)icond.Current;
					result &= cond.Evaluate();
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
		public string image()
		{
			string image = @"&lt;condition";
			if (m_arg1 != null) image += @" of='" + m_arg1 + @"'";
			if (m_is != null) image += @" is='" + m_is + @"'";
			if (m_arg2 != null) image += @" to='" + m_arg2 + @"'";
			image += "/&gt;";
			return image;
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <param name="a1">Evaluation of 1st arguement</param>
		/// <param name="a2">Evaluation of 2nd arguement</param>
		/// <returns>An image of this instruction.</returns>
		public string image(string a1, string a2)
		{
			string image = @"&lt;condition";
			if (m_arg1 != null) image += @" of=""" + m_arg1 + @" (" + Utilities.attrText(a1) + @")""";
			if (m_is != null) image += @" is=""" + m_is + @"""";
			if (m_arg2 != null) image += @" to=""" + m_arg2 + @" (" + Utilities.attrText(a2) + @")""";
			image += "/&gt;";
			return image;
		}
	}
}
