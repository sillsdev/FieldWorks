using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for DynamicLoader.
	/// </summary>
	public class DynamicLoader
	{
		protected DynamicLoader()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// </summary>
		/// <param name="parentConfigNode">A parent node that must have one child nade named 'dynamicloaderinfo', which contains the two required attributes.</param>
		/// <returns></returns>
		static public Object CreateObjectUsingLoaderNode(XmlNode parentConfigNode)
		{
			XmlNode dynLoaderNode = parentConfigNode.SelectSingleNode("dynamicloaderinfo");
			if (dynLoaderNode == null)
				throw new ArgumentException("Required 'dynamicloaderinfo' XML node not found.", "parentConfigNode");

			return CreateObject(dynLoaderNode);
		}

		// Return the class of object that will be created if CreateObjectUsingLoaderNode is called with this argument.
		// Return null if dynamic loader node not found or if it doesn't specify a valid class.
		static public Type TypeForLoaderNode(XmlNode parentConfigNode)
		{
			XmlNode configuration = parentConfigNode.SelectSingleNode("dynamicloaderinfo");
			if (configuration == null)
				return null;
			string assemblyPath = XmlUtils.GetManditoryAttributeValue(configuration, "assemblyPath");
			if (assemblyPath == "null")
				return null;
			string className = XmlUtils.GetManditoryAttributeValue(configuration, "class");
			Assembly assembly;
			GetAssembly(assemblyPath, out assembly);
			return assembly.GetType(className.Trim());
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// The XmlNode must have attributes assemblyPath (typically just a dll name without a path,
		/// if it is in the same location as the XmlUtils.dll), and class, the fully qualified class name.
		/// It may also have a child node "args" with a sequence of "arg" children, each of which
		/// specifies a name and value; these seem to be basically ignored except that if one of
		/// them has the name xpathToConfigurationNode, the value is used as an xpath (relative to the
		/// input configuration) to find a node that is passed as an argument to the constructor.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="rootAssemblyPath"></param>
		/// <returns></returns>
		static public Object CreateObject(XmlNode configuration)
		{
			return CreateObject(configuration, CreateArgs(configuration));
		}

		private static object[] CreateArgs(XmlNode configuration)
		{
			List<object> argList = new List<object>();
			// see if we can find "args" children that specify arguments to pass in.
			if (configuration != null && configuration.HasChildNodes)
			{
				XmlNodeList argNodes = configuration.SelectNodes("args/arg");
				if (argNodes.Count > 0)
				{
					Dictionary<string, string> argDict = new Dictionary<string, string>();
					foreach (XmlNode argNode in argNodes)
					{
						string argName = XmlUtils.GetManditoryAttributeValue(argNode, "name");
						string argVal = XmlUtils.GetManditoryAttributeValue(argNode, "value");
						argDict.Add(argName, argVal);
					}
					string argValue;
					if (argDict.TryGetValue("xpathToConfigurationNode", out argValue))
					{
						// "xpathToConfigurationNode" is a special argument for passing the nodes
						// that the object we're creating knows how to process.
						// NOTE: assume the xpath is with respect to the dynamicloaderinfo "configuration" node
						XmlNode configNodeForObject = configuration.SelectSingleNode(argValue);
						if (configNodeForObject != null)
							argList.Add(configNodeForObject);
					}
				}
			}
			return argList.Count > 0 ? argList.ToArray() : null;
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// configuration has assemblyPath and class (fully qualified) as in other overloads.
		/// The constructor arguments are supplied explicitly.
		/// </summary>
		/// <returns></returns>
		static public Object CreateObject(XmlNode configuration, params object[] args)
		{
			string  assemblyPath = XmlUtils.GetManditoryAttributeValue(configuration, "assemblyPath");
			// JohnT: see AddAssemblyPathInfo. We use this when the object we're trying to persist
			// as a child of another object is null.
			if (assemblyPath == "null")
				return null;
			string className = XmlUtils.GetManditoryAttributeValue(configuration, "class");
			return CreateObject(assemblyPath, className, args);
		}
		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// </summary>
		static public Object CreateObject(string assemblyPath, string className)
		{
			return CreateObject(assemblyPath, className, null);
		}

		static string CouldNotCreateObjectMsg(string assemblyPath, string className)
		{
			return "XCore found the DLL "
				+ assemblyPath
				+ " but could not create the class: "
				+ className
				+ ". If there are no 'InnerExceptions' below, then make sure capitalization is correct and that you include the name space (e.g. XCore.Ticker).";
		}

		/// <summary>
		/// Dynamically find an assembly and create an object of the name to class.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="rootAssemblyPath"></param>
		/// <param name="args">args to the constructor</param>
		/// <returns></returns>
		static public Object CreateObject(string assemblyPath1, string className1, params object[] args)
		{
			Assembly assembly;
			string assemblyPath = GetAssembly(assemblyPath1, out assembly);

			string className = className1.Trim();
			Object thing = null;
			try
			{
				//make the object
				//Object thing = assembly.CreateInstance(className);
				thing = assembly.CreateInstance(className, false, BindingFlags.Instance|BindingFlags.Public,
					null, args, null, null );
			}
			catch (Exception err)
			{
				Debug.WriteLine(err.Message);
				var bldr = new StringBuilder(CouldNotCreateObjectMsg(assemblyPath, className));

				Exception inner = err;

				while(inner != null)
				{
					bldr.AppendLine();
					bldr.Append("Inner exception message = " + inner.Message);
					inner = inner.InnerException;
				}
				throw new ConfigurationException(bldr.ToString(), err);
			}
			if (thing == null)
			{
				// Bizarrely, CreateInstance is not specified to throw an exception if it can't
				// find the specified class. But we want one.
				throw new ConfigurationException(CouldNotCreateObjectMsg(assemblyPath, className));
			}
			return thing;
		}

		private static string GetAssembly(string assemblyPath1, out Assembly assembly)
		{
			// Whitespace will cause failures.
			string assemblyPath = assemblyPath1.Trim();
			//allow us to say "assemblyPath="%fwroot%\Src\XCo....  , at least during testing
			// RR: It may allow it, but it crashes, when it can't find the dll.
			//assemblyPath = System.Environment.ExpandEnvironmentVariables(assemblyPath);
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
					throw new RuntimeConfigurationException("XCore Could not find the DLL at :" + assemblyPath, error);
				}
			}
			return assemblyPath;
		}

		/// <summary>
		/// Create the object specified by the assemblyPath and class attributes of node,
		/// and if the resulting object implements IPersistAsXml, call InitXml.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		static public object RestoreObject(XmlNode node)
		{
			object obj = CreateObject(node);
			IPersistAsXml persistObj = obj as IPersistAsXml;
			if (persistObj != null)
				persistObj.InitXml(node);
			return obj;
		}

		/// <summary>
		/// Create an XmlNode out of the source, and use it to recreate an object.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		static public object RestoreObject(string source)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(source);
			return RestoreObject(doc.DocumentElement);
		}


		/// <summary>
		/// Return the object obtained by calling RestoreObject on the element
		/// selected from node by xpath.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="xpath"></param>
		/// <returns></returns>
		static public object RestoreFromChild(XmlNode node, string xpath)
		{
			XmlNode child = node.SelectSingleNode(xpath);
			if (child == null)
				throw new Exception("expected child " + xpath);
			return RestoreObject(child);
		}

		/// <summary>
		/// Creates a string representation of the supplied object, an XML string
		/// containing the required assemblyPath and class attributes needed to create an
		/// instance using CreateObject, plus whatever gets added to the node by passsing
		/// it to the PersistAsXml method of the object. The root element name is supplied
		/// as the elementName argument.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="elementName"></param>
		/// <returns></returns>
		static public string PersistObject(object src, string elementName)
		{
			if (src == null)
				return null;
			IPersistAsXml obj = src as IPersistAsXml;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<" + elementName + "/>");
			XmlNode root = doc.DocumentElement;
			AddAssemblyClassInfoTo(root, src);
			if (obj != null)
				obj.PersistAsXml(root);
			return root.OuterXml;
		}

		static public XmlNode PersistObject(object src, XmlNode parent, string elementName)
		{
			IPersistAsXml obj = src as IPersistAsXml;
			XmlNode node = parent.OwnerDocument.CreateElement(elementName);
			parent.AppendChild(node);
			AddAssemblyClassInfoTo(node, obj);
			if (obj != null)
				obj.PersistAsXml(node);
			return node;
		}

		/// <summary>
		/// Add to the specified node assembly and class information for the specified object.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="root"></param>
		/// <param name="sorter"></param>
		static internal void AddAssemblyClassInfoTo(XmlNode node, object obj)
		{
			XmlDocument doc = node.OwnerDocument;
			XmlAttribute xaAssembly = doc.CreateAttribute("assemblyPath");
			node.Attributes.Append(xaAssembly);
			if (obj == null)
			{
				xaAssembly.Value = "null";
				return;
			}
			xaAssembly.Value = obj.GetType().Assembly.GetName().Name + ".dll";
			node.Attributes.Append(xaAssembly);
			XmlAttribute xaClass = doc.CreateAttribute("class");
			node.Attributes.Append(xaClass);
			xaClass.Value = obj.GetType().FullName;
			node.Attributes.Append(xaClass);
		}
	}

	public interface IPersistAsXml
	{
		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// object equivalent to yourself. The node already contains information
		/// sufficient to create an instance of the proper class.
		/// </summary>
		/// <param name="node"></param>
		void PersistAsXml(XmlNode node);

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		void InitXml(XmlNode node);
	}

}
