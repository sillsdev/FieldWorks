// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer
{
	/// <summary />
	internal static class DynamicLoader
	{
		// Return the class of object that will be created if CreateObject is called with this argument.
		internal static Type TypeForLoaderNode(XElement configurationElement)
		{
			return Assembly.GetExecutingAssembly().GetType(XmlUtils.GetMandatoryAttributeValue(configurationElement, "class").Trim());
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// configuration has class (fully qualified) as in other overloads.
		/// The constructor arguments are supplied explicitly.
		/// </summary>
		internal static T CreateObject<T>(XElement configuration, object[] args = null) where T : class
		{
			var assembly = Assembly.GetExecutingAssembly();
			var className = XmlUtils.GetMandatoryAttributeValue(configuration, "class").Trim();
			var errorMessage = $"Found LanguageExplorer.dll, but could not create the class: '{className}'. If there are no 'InnerExceptions' below, then make sure capitalization is correct and that you include the namespace.";
			object thing;
			try
			{
				//make the object
				thing = assembly.CreateInstance(className, false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null, null);
			}
			catch (Exception err)
			{
				Debug.WriteLine(err.Message);
				var bldr = new StringBuilder(errorMessage);
				var inner = err;
				while (inner != null)
				{
					bldr.AppendLine();
					bldr.Append("Inner exception message = " + inner.Message);
					inner = inner.InnerException;
				}
				throw new FwConfigurationException(bldr.ToString(), err);
			}
			if (thing == null)
			{
				// Bizarrely, CreateInstance is not specified to throw an exception if it can't
				// find the specified class. But we want one.
				throw new FwConfigurationException(errorMessage);
			}
			return (T)thing;
		}
	}
}