// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer
{
	/// <summary />
	public static class DynamicLoader
	{
		// Return the class of object that will be created if CreateObjectUsingLoaderNode is called with this argument.
		// Return null if dynamic loader node not found or if it doesn't specify a valid class.
		public static Type TypeForLoaderNode(XElement parentConfigNode)
		{
			var configuration = parentConfigNode.Element("dynamicloaderinfo");
			if (configuration == null)
			{
				return null;
			}
			var assemblyPath = XmlUtils.GetMandatoryAttributeValue(configuration, "assemblyPath");
			if (assemblyPath == "null")
			{
				return null;
			}
			var className = XmlUtils.GetMandatoryAttributeValue(configuration, "class");
			GetAssembly(assemblyPath, out var assembly);
			return assembly.GetType(className.Trim());
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// configuration has assemblyPath and class (fully qualified) as in other overloads.
		/// The constructor arguments are supplied explicitly.
		/// </summary>
		/// <returns></returns>
		public static object CreateObject(XElement configuration, object[] args = null)
		{
			var assemblyPath = XmlUtils.GetMandatoryAttributeValue(configuration, "assemblyPath");
			// JohnT: see AddAssemblyPathInfo. We use this when the object we're trying to persist
			// as a child of another object is null.
			if (assemblyPath == "null")
			{
				return null;
			}
			assemblyPath = GetAssembly(assemblyPath, out var assembly);
			var className = XmlUtils.GetMandatoryAttributeValue(configuration, "class").Trim();
			var errorMessage = $"Found the DLL '{assemblyPath}' but could not create the class: '{className}'. If there are no 'InnerExceptions' below, then make sure capitalization is correct and that you include the namespace.";
			object thing;
			try
			{
				//make the object
				thing = assembly.CreateInstance(className, false, BindingFlags.Instance | BindingFlags.Public, null, args, null, null);
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
			return thing;
		}

		private static string GetAssembly(string assemblyPath1, out Assembly assembly)
		{
			// Whitespace will cause failures.
			var assemblyPath = assemblyPath1.Trim();
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			try
			{
				assembly = Assembly.LoadFrom(Path.Combine(baseDir, assemblyPath));
			}
			catch (Exception)
			{
				try
				{
					//Try to find without specifying the directory,
					//so that we find things that are in the Path environment variable
					//This is useful in extension situations where the extension's bin directory
					//is not the same as the FieldWorks binary directory (e.g. WeSay)
					assembly = Assembly.LoadFrom(assemblyPath);
				}
				catch (Exception error)
				{
					throw new RuntimeConfigurationException("Could not find the DLL at :" + assemblyPath, error);
				}
			}
			return assemblyPath;
		}

		/// <summary>
		/// Create the object specified by the assemblyPath and class attributes of node,
		/// and if the resulting object implements IPersistAsXml, call InitXml.
		/// </summary>
		public static object RestoreObject(XElement node)
		{
			var obj = CreateObject(node);
			(obj as IPersistAsXml)?.InitXml(node.Clone());
			return obj;
		}

		/// <summary>
		/// Creates a string representation of the supplied object, an XML string
		/// containing the required assemblyPath and class attributes needed to create an
		/// instance using CreateObject, plus whatever gets added to the node by passing
		/// it to the PersistAsXml method of the object. The root element name is supplied
		/// as the elementName argument.
		/// </summary>
		public static string PersistObject(object src, string elementName)
		{
			if (src == null)
			{
				return null;
			}
			var element = new XElement(elementName);
			PersistObject(src as IPersistAsXml, element);
			return element.ToString();
		}

		public static void PersistObject(object src, XElement parent, string elementName)
		{
			var element = new XElement(elementName);
			parent.Add(element);
			PersistObject(src as IPersistAsXml, element);
		}

		private static void PersistObject(IPersistAsXml persistAsXml, XElement element)
		{
			if (persistAsXml == null)
			{
				element.Add(new XAttribute("assemblyPath", "null"));
				return;
			}
			element.Add(new XAttribute("assemblyPath", persistAsXml.GetType().Assembly.GetName().Name + ".dll"));
			element.Add(new XAttribute("class", persistAsXml.GetType().FullName));
			persistAsXml.PersistAsXml(element);
		}
	}
}