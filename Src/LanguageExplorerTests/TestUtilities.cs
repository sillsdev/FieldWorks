// SilSidePane, Copyright 2010-2019 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.Xml;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Utilities to help with unit testing
	/// </summary>
	internal static class TestUtilities
	{
		/// <summary>
		/// Use by converting code that doesn't have proper access such as
		///     alpha._bravo.charlie(delta); // _bravo is an inaccessible private field
		/// to
		///     GetPrivateField(alpha,"_bravo").charlie(delta);
		/// </summary>
		/// <param name="obj">
		/// object which has a private field
		/// </param>
		/// <param name="fieldName">
		/// name of private field in object that you want to access
		/// </param>
		/// <returns>
		/// Value of private field. You will need to cast it.
		/// </returns>
		internal static object GetPrivateField(object obj, string fieldName)
		{
			return obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
		}

		/// <summary />
		internal static object GetPrivateProperty(object obj, string fieldName)
		{
			return obj.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj, null);
		}

		internal static T GetPrivatePropertyOfType<T>(object obj, string fieldName)
		{
			return (T)obj.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance, null, typeof(T), new Type[0], null).GetValue(obj, null);
		}

		/// <summary>
		/// Return true if the two nodes match. Corresponding children should match, and
		/// corresponding attributes (though not necessarily in the same order).
		/// The nodes are expected to be actually XmlElements
		/// </summary>
		internal static bool NodesMatch(XElement node1, XElement node2)
		{
			if (node1.Name != node2.Name)
			{
				return false;
			}
			if (node1.Name.LocalName != node2.Name.LocalName)
			{
				return false;
			}
			if (node1.Elements().Count() != node2.Elements().Count())
			{
				return false;
			}
			if (node1.Attributes().Count() != node2.Attributes().Count())
			{
				return false;
			}
			if (node1.GetInnerText() != node2.GetInnerText())
			{
				return false;
			}
			foreach (var attr in node1.Attributes())
			{
				var xa2 = node2.Attribute(attr.Name);
				if (xa2 == null || attr.Value != xa2.Value)
				{
					return false;
				}
			}
			var node1ElementsAsList = node1.Elements().ToList();
			var node2ElementsAsList = node2.Elements().ToList();
			return !node1ElementsAsList.Where((t, i) => !NodesMatch(t, node2ElementsAsList[i])).Any();
		}

		internal static XElement GetRootNode(XDocument doc, string name)
		{
			return doc.Root.XPathSelectElement("//" + name);
		}
	}
}