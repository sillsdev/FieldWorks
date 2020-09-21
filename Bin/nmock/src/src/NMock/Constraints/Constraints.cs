// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;

namespace NMock.Constraints
{

	public abstract class BaseConstraint : IConstraint
	{
		public virtual object ExtractActualValue(object actual)
		{
			return actual;
		}

		public abstract bool Eval(object val);
		public abstract string Message { get; }
	}

	public class IsNull : BaseConstraint
	{
		public override bool Eval(object val)
		{
			return ExtractActualValue(val) == null;
		}

		public override string Message
		{
			get { return "null"; }
		}
	}

	public class IsAnything : BaseConstraint
	{
		public override bool Eval(object val)
		{
			return true;
		}

		public override string Message
		{
			get { return ""; }
		}
	}

	public class IsIn : BaseConstraint
	{
		private object[] inList;

		public IsIn(params object[] inList)
		{
			if (inList.Length == 1 && inList[0].GetType().IsArray)
			{
				Array arr = (Array)inList[0];
				this.inList = new object[arr.Length];
				arr.CopyTo(this.inList, 0);
			}
			else
			{
				this.inList = inList;
			}
		}

		public override bool Eval(object val)
		{
			foreach (object o in inList)
			{
				if (o.Equals(ExtractActualValue(val)))
				{
					return true;
				}
			}
			return false;
		}

		public override string Message
		{
			get
			{
				StringBuilder builder = new StringBuilder("IN ");
				foreach (object o in inList)
				{
					builder.Append('<');
					builder.Append(o);
					builder.Append(">, ");
				}
				string result = builder.ToString();
				if (result.EndsWith(", "))
				{
					result = result.Substring(0, result.Length - 2);
				}
				return result;
			}
		}
	}

	public class IsEqual : BaseConstraint {
		private object compare;

		public IsEqual(object compare)
		{
			this.compare = compare;
		}

		public override bool Eval(object val)
		{
			if ((val != null) && (val.GetType().IsArray) && (compare.GetType().IsArray))
			{
				return ArrayCompare((System.Array)val, (System.Array)compare);
			}
			else
			{
				return compare.Equals(ExtractActualValue(val));
			}
		}

		private bool ArrayCompare(System.Array a1, System.Array a2)
		{
			if(a1.Length != a2.Length) return false;
			for(int i=0; i < a1.Length; i++)
			{
				if(!a1.GetValue(i).Equals(a2.GetValue(i))) return false;
			}
			return true;
		}

		public override string Message
		{
			get { return "<" + compare + ">"; }
		}
	}

	public class IsTypeOf : BaseConstraint
	{
		private Type type;

		public IsTypeOf(Type type)
		{
			this.type = type;
		}

		public override bool Eval(object val)
		{
			object actualValue = ExtractActualValue(val);
			return actualValue == null ? false : type.IsAssignableFrom(actualValue.GetType());
		}

		public override string Message
		{
			get { return "typeof <" + type.FullName + ">"; }
		}
	}

	public class Not : BaseConstraint
	{
		private IConstraint p;

		public Not(IConstraint p)
		{
			this.p = p;
		}

		public override bool Eval(object val)
		{
			return !p.Eval(ExtractActualValue(val));
		}

		public override string Message
		{
			get { return "NOT " + p.Message; }
		}
	}

	public class And : BaseConstraint
	{
		private IConstraint p1, p2;

		public And(IConstraint p1, IConstraint p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}

		public override bool Eval(object val)
		{
			object actualValue = ExtractActualValue(val);
			return p1.Eval(actualValue) && p2.Eval(actualValue);
		}

		public override string Message
		{
			get { return p1.Message + " AND " + p2.Message; }
		}
	}

	public class Or : BaseConstraint
	{
		private IConstraint p1, p2;

		public Or(IConstraint p1, IConstraint p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}

		public override bool Eval(object val)
		{
			object actualValue = ExtractActualValue(val);
			return p1.Eval(actualValue) || p2.Eval(actualValue);
		}

		public override string Message
		{
			get { return p1.Message + " OR " + p2.Message; }
		}
	}

	public class NotNull : BaseConstraint
	{
		public override bool Eval(object val)
		{
			return ExtractActualValue(val) != null;
		}

		public override string Message
		{
			get { return "NOT null"; }
		}
	}

	public class NotEqual : BaseConstraint
	{
		private IConstraint p;

		public NotEqual(object compare)
		{
			p = new Not(new IsEqual(compare));
		}

		public override bool Eval(object val)
		{
			return p.Eval(ExtractActualValue(val));
		}

		public override string Message
		{
			get { return p.Message; }
		}
	}

	public class NotIn : BaseConstraint
	{
		private IConstraint p;

		public NotIn(params object[] inList)
		{
			p = new Not(new IsIn(inList));
		}

		public override bool Eval(object val)
		{
			return p.Eval(ExtractActualValue(val));
		}

		public override string Message
		{
			get { return p.Message; }
		}
	}

	public class IsEqualIgnoreCase : BaseConstraint
	{
		private IConstraint p;

		public IsEqualIgnoreCase(object compare)
		{
			p = new IsEqual(compare.ToString().ToLower());
		}

		public override bool Eval(object val)
		{
			return p.Eval(ExtractActualValue(val).ToString().ToLower());
		}

		public override string Message
		{
			get { return p.Message; }
		}
	}

	public class IsEqualIgnoreWhiteSpace : BaseConstraint
	{
		private IConstraint p;

		public IsEqualIgnoreWhiteSpace(object compare)
		{
			p = new IsEqual(StripSpace(compare.ToString()));
		}

		public override bool Eval(object val)
		{
			return p.Eval(StripSpace(ExtractActualValue(val).ToString()));
		}

		public static string StripSpace(string s)
		{
			StringBuilder result = new StringBuilder();
			bool lastWasSpace = true;
			foreach(char c in s)
			{
				if (Char.IsWhiteSpace(c))
				{
					if (!lastWasSpace)
					{
						result.Append(' ');
					}
					lastWasSpace = true;
				}
				else
				{
					result.Append(c);
					lastWasSpace = false;
				}
			}
			return result.ToString().Trim();
		}

		public override string Message
		{
			get { return p.Message; }
		}
	}

	public class IsMatch : BaseConstraint
	{
		private Regex regex;

		public IsMatch(Regex regex)
		{
			this.regex = regex;
		}

		public IsMatch(String regex) : this(new Regex(regex))
		{
		}

		public IsMatch(String regex, bool ignoreCase) :
			this(new Regex(regex, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None))
		{
		}

		public override bool Eval(object val)
		{
			object actualValue = ExtractActualValue(val);
			return actualValue == null ? false : regex.IsMatch(actualValue.ToString());
		}

		public override string Message
		{
			get { return "<" + regex.ToString() + ">"; }
		}
	}

	public class IsCloseTo : BaseConstraint
	{

		private double expected;
		private double error;

		public IsCloseTo(double expected, double error)
		{
			this.expected = expected;
			this.error = error;
		}

		public override bool Eval(object val)
		{
			try
			{
				double actual = Convert.ToDouble(ExtractActualValue(val));
				return Math.Abs(actual - expected) <= error;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		public override string Message
		{
			get { return "<" + expected + ">"; }
		}
	}

	public class StartsWith : BaseConstraint
	{

		private string expected;

		public StartsWith(string startsWithPattern)
		{
			this.expected = startsWithPattern;
		}

		public override bool Eval(object val)
		{
			return ((string)val).StartsWith(expected);
		}

		public override string Message
		{
			get { return "StartsWith<" + expected + ">"; }
		}
	}

	/// <summary>
	/// This constraint decorates another constraint, allowing it to test
	/// a property of the object, rather than the property itself.
	///
	/// Properties of properties can be specified by using the
	/// "Property.SubProperty" notation.
	/// </summary>
	public class PropertyIs : BaseConstraint
	{
		private string property;
		private IConstraint constraint;

		public PropertyIs(string property, object expected)
		{
			this.property = property;
			constraint = expected as IConstraint;
			if (constraint == null)
			{
				constraint = new IsEqual(expected);
			}
		}

		public override bool Eval(object val)
		{
			object actualValue = ExtractActualValue(val);
			if (actualValue == null)
			{
				return false;
			}
			// split "a.b.c" into "a", "b", "c"
			object propertyValue = actualValue;
			foreach(string propertyBit in property.Split(new char[] {'.'}))
			{
				propertyValue = getProperty(propertyValue, propertyBit);
			}
			return constraint.Eval(propertyValue);
		}

		private object getProperty(object val, string property)
		{
			Type type = val.GetType();
			PropertyInfo propertyInfo = type.GetProperty(property);
			return propertyInfo.GetValue(val, null);
		}

		public override string Message
		{
			get { return String.Format("Property {0}: {1}", property, constraint.Message); }
		}
	}

	public class Constraint : BaseConstraint
	{
		public delegate bool Method(object val);
		private Method m;

		public Constraint(Method m)
		{
			this.m = m;
		}

		public override bool Eval(object val)
		{
			return m(ExtractActualValue(val));
		}

		public override string Message
		{
			get { return "Custom Constraint"; }
		}
	}


	public class CollectingConstraint : BaseConstraint
	{
		private object parameter;

		public object Parameter
		{
			get { return parameter; }
		}

		public override bool Eval(object val)
		{
			parameter = ExtractActualValue(val);
			return true;
		}

		public override string Message
		{
			get { return "Collecting Constraint"; }
		}
	}
}
