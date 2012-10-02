using System;
using System.Collections.Generic;
using System.Text;

namespace GuiTestDriver
{
	class Functions
	{
		Functions() { }

		/// <summary>
		/// Identify and evaluate the function in the token.
		/// </summary>
		/// <param name="token">The function call: name(arg1,arg2,...,argn)</param>
		/// <returns>The function's result as a string</returns>
		static public string identifyAndEvaluate(string token)
		{
			// parse the token: name(arg1,arg2,...,argn)
			int oParen = token.IndexOf('(');
			int cParen = token.IndexOf(')');
			if (oParen >= 0 && cParen > 0 && oParen > cParen)
			{ // there is a set of matching parentheses
				string name = null;
				string args = null;
				if (cParen - oParen > 1) args = token.Substring(oParen+1,cParen-oParen-1);
				if (oParen > 0) name = token.Substring(0, oParen);
				if (name != null)
				{ // do we know about this function?
					switch (name)
					{
					case "random": return randomFunc(args);
					default: return null; // don't know about this one - log it??
					}
				}
				else
				{
					// TBD case of function = (X,Y,Z)
					// should it be a screen point, general index?
				}
			}
			// else just a name, not a function
			return null;
		}

		/// <summary>
		/// Implements an integer result random number generator.
		/// The first is the maximum integer to generated, 1 if abscent.
		/// The second argument is the minimum integer to generate, 0 if abscent.
		/// </summary>
		/// <param name="args">The function argument list: max,min</param>
		/// <returns>A random integer from min to min + range - 1</returns>
		static private string randomFunc(string args)
		{
			int min = 0;
			int max = 1;
			// tweak min and max according to args
			int comma = -1;
			if (args != null) comma = args.IndexOf(',');
			if (comma > 0)
			{  // there's a comma and two args
				max = int.Parse(args.Substring(0, comma));
				if (args.Length > comma + 1) min = int.Parse(args.Substring(comma + 1));
			}
			// use min and max to find a random number
			Random genRand = new Random();
			int ranNum = genRand.Next(min, max+1);
			return ranNum.ToString();
		}
	}

}
