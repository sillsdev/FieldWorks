// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Summary description for StringTableTests.
	/// </summary>
	[TestFixture]
	public class StringTableTests
	{
		private string m_tempFolder;
		private StringTable m_table;

		/// <summary />
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			m_tempFolder = CreateTestResourceFiles(typeof(Properties.Resources), "food");
			m_table = new StringTable(Path.Combine(m_tempFolder, "fruit", "citrus"));
		}

		/// <summary />
		[TestFixtureTearDown]
		public void FixtureCleanup()
		{
			try
			{
				Directory.Delete(m_tempFolder, true);
			}
			catch (IOException)
			{
			}
		}

		/// <summary />
		[Test]
		public void InBaseFile()
		{
			Assert.AreEqual("orng", m_table.GetString("orange"));
		}

		/// <summary />
		[Test]
		public void InParentFile()
		{
			Assert.AreEqual("pssnfrt", m_table.GetString("passion fruit"));
			Assert.AreEqual("ppy", m_table.GetString("papaya"));
		}

		/// <summary />
		[Test]
		public void OmitTxtAttribute()
		{
			/*
			 this one demonstrates that omitting the txt attribute just means that we should return the id value
			 <string id="Banana"/>
			*/
			Assert.AreEqual("Banana", m_table.GetString("Banana"));
		}

		/// <summary />
		[Test]
		public void WithPath()
		{
			Assert.AreEqual(m_table.GetString("MyPineapple", "InPng/InMyYard"), "pnppl");
		}

		/// <summary />
		[Test]
		public void WithXPathFragment()
		{
			// find any group that contains a match
			// the leading '/' here will lead to a double slash,
			// something like strings//group,
			// meaning that this can be found in any group.
			Assert.AreEqual(m_table.GetStringWithXPath("MyPineapple", "/group/"), "pnppl");
		}

		/// <summary />
		[Test]
		public void WithRootXPathFragment()
		{
			// Give the path of groups explicitly in a compact form.
			Assert.AreEqual(m_table.GetString("MyPineapple", "InPng/InMyYard"), "pnppl");
		}

		/// <summary />
		[Test]
		public void StringListXmlNode()
		{
			var doc = XDocument.Parse(@"<stringList group='InPng/InMyYard' ids='   MyPapaya, MyPineapple  '/>");
			var node = doc.Root;
			var strings = m_table.GetStringsFromStringListNode(node);
			Assert.AreEqual(2, strings.Length);
			Assert.AreEqual(strings[1], "pnppl");
		}

		/// <summary />
		public static string CreateTestResourceFiles(Type resourcesType, string folderName)
		{
			var folderPath = Path.Combine(Path.GetTempPath(), folderName);
			var resourceMgrPropInfo = resourcesType.GetProperty("ResourceManager", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			Debug.Assert(resourceMgrPropInfo != null);
			var resourceMgr = (ResourceManager)resourceMgrPropInfo.GetValue(null);
			var props = resourcesType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);

			foreach (var pi in props)
			{
				// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (pi.PropertyType == typeof(string) && pi.Name.StartsWith(folderName + "__"))
				{
					CreateSingleTempTestFile(pi.Name, resourceMgr);
				}
			}
			return folderPath;
		}

		private static void CreateSingleTempTestFile(string resName, ResourceManager resourceMgr)
		{
			var path = resName.Replace("__", Path.DirectorySeparatorChar.ToString());
			path = path.Replace("_DASH_", "-");
			path = path.Replace("_", ".");
			path = Path.Combine(Path.GetTempPath(), path);
			var folder = Path.GetDirectoryName(path);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			File.WriteAllText(path, resourceMgr.GetString(resName), Encoding.UTF8);
		}
	}
}