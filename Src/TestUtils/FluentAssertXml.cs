using System;
using System.Xml;
using NUnit.Framework;
using Palaso.Xml;

namespace Palaso.TestUtilities
{
	//NB: if c# ever allows us to add static exension methods,
	//then all this could be an extension on nunit's Assert class.

	public class AssertThatXmlIn
	{
		public static AssertDom Dom(XmlDocument dom)
		{
			return new AssertDom(dom);
		}
		public static AssertFile File(string path)
		{
			return new AssertFile(path);
		}
		public static AssertXmlString String(string xmlString)
		{
			return new AssertXmlString(xmlString);
		}
	}

	public class AssertXmlString : AssertXmlCommands
	{
		private readonly string _xmlString;

		public AssertXmlString(string xmlString)
		{
			_xmlString = xmlString;
		}

		protected override XmlNode NodeOrDom
		{
			get
			{
				var dom = new XmlDocument();
				dom.LoadXml(_xmlString);
				return dom;
			}
		}
	}

	public class AssertFile : AssertXmlCommands
	{
		private readonly string _path;

		public AssertFile(string path)
		{
			_path = path;
		}

		protected override XmlNode NodeOrDom
		{
			get
			{
				var dom = new XmlDocument();
				dom.Load(_path);
				return dom;
			}
		}
	}

	public class AssertDom : AssertXmlCommands
	{
		private readonly XmlDocument _dom;

		public AssertDom(XmlDocument dom)
		{
			_dom = dom;
		}

		protected override XmlNode NodeOrDom
		{
			get
			{
				return _dom;
			}
		}
	}

	public abstract class AssertXmlCommands
	{
		protected abstract XmlNode NodeOrDom { get; }


		public void HasAtLeastOneMatchForXpath(string xpath, XmlNamespaceManager nameSpaceManager)
		{
			XmlNode node = GetNode(xpath, nameSpaceManager);
			if (node == null)
			{
				Console.WriteLine("Could not match " + xpath);
				PrintNodeToConsole(NodeOrDom);
			}
			Assert.IsNotNull(node, "Not matched: " + xpath);
		}

		/// <summary>
		/// Will honor default namespace
		/// </summary>
		public  void HasAtLeastOneMatchForXpath(string xpath)
		{
			XmlNode node = GetNode(xpath);
			if (node == null)
			{
				Console.WriteLine("Could not match " + xpath);
				PrintNodeToConsole(NodeOrDom);
			}
			Assert.IsNotNull(node, "Not matched: " + xpath);
		}

		/// <summary>
		/// Will honor default namespace
		/// </summary>
		public void HasSpecifiedNumberOfMatchesForXpath(string xpath, int count)
		{
			var nodes = NodeOrDom.SafeSelectNodes(xpath);
			if (nodes==null)
			{
				Console.WriteLine("Expected {0} but got 0 matches for {1}",count,  xpath);
				PrintNodeToConsole(NodeOrDom);
				Assert.AreEqual(count,0);
			}
			else if (nodes.Count != count)
			{
				Console.WriteLine("Expected {0} but got {1} matches for {2}",count, nodes.Count, xpath);
				PrintNodeToConsole(NodeOrDom);
				Assert.AreEqual(count, nodes.Count, "matches for "+xpath);
			}
		}

		public static void PrintNodeToConsole(XmlNode node)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			XmlWriter writer = XmlWriter.Create(Console.Out, settings);
			node.WriteContentTo(writer);
			writer.Flush();
			Console.WriteLine();
		}


		public  void HasNoMatchForXpath(string xpath, XmlNamespaceManager nameSpaceManager)
		{
			XmlNode node = GetNode( xpath, nameSpaceManager);
			if (node != null)
			{
				Console.WriteLine("Was not supposed to match " + xpath);
				PrintNodeToConsole(NodeOrDom);
			}
			Assert.IsNull(node, "Should not have matched: " + xpath);
		}

		public  void HasNoMatchForXpath(string xpath)
		{
			XmlNode node = GetNode( xpath, new XmlNamespaceManager(new NameTable()));
			if (node != null)
			{
				Console.WriteLine("Was not supposed to match " + xpath);
				PrintNodeToConsole(NodeOrDom);
			}
			Assert.IsNull(node, "Should not have matched: " + xpath);
		}




		private XmlNode GetNode(string xpath)
		{
			return NodeOrDom.SelectSingleNodeHonoringDefaultNS(xpath);
		}

		private XmlNode GetNode(string xpath, XmlNamespaceManager nameSpaceManager)
		{
			return NodeOrDom.SelectSingleNode(xpath, nameSpaceManager);
		}
	}
}