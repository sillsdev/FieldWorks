// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: GuiPathTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for GuiPathTest.
	/// </summary>
	[TestFixture]
	public class GuiPathTest
	{
		public GuiPathTest()
		{
		}
		[Test]
		public void ParseNameOnlyGuiPathTest()
		{
			GuiPath gpath = new GuiPath("View/Filters/Configure...");
			Assert.AreEqual("View", gpath.Name, "ParseGuiPath name 0 is '" + gpath.Name + "' not 'View'");
			Assert.AreEqual("None", gpath.Role.ToString(), "ParseGuiPath role 0 is '" + gpath.Role.ToString() + "' not 'None'");
			Assert.AreEqual("Filters", gpath.Next.Name, "ParseGuiPath name 1 is '" + gpath.Next.Name + "' not 'Filters'");
			Assert.AreEqual("None", gpath.Next.Role.ToString(), "ParseGuiPath role 1 is '" + gpath.Next.Role.ToString() + "' not 'None'");
			Assert.AreEqual("Configure...", gpath.Next.Next.Name, "ParseGuiPath name 2 is '" + gpath.Next.Next.Name + "' not 'Configure...'");
			Assert.AreEqual("None", gpath.Next.Next.Role.ToString(), "ParseGuiPath role 2 is '" + gpath.Next.Next.Role.ToString() + "' not 'None'");
		}
		[Test]
		public void ParseGuiPathTest()
		{
			GuiPath gpath = new GuiPath("menu:View/menu:Filters/button:Configure...");
			Assert.AreEqual("View", gpath.Name, "ParseGuiPath name 0 is '" + gpath.Name + "' not 'View'");
			Assert.AreEqual("MenuItem", gpath.Role.ToString(), "ParseGuiPath role 0 is '" + gpath.Role.ToString() + "' not 'MenuItem'");
			Assert.AreEqual("Filters", gpath.Next.Name, "ParseGuiPath name 1 is '" + gpath.Next.Name + "' not 'Filters'");
			Assert.AreEqual("MenuItem", gpath.Next.Role.ToString(), "ParseGuiPath role 1 is '" + gpath.Next.Role.ToString() + "' not 'MenuItem'");
			Assert.AreEqual("Configure...", gpath.Next.Next.Name, "ParseGuiPath name 2 is '" + gpath.Next.Next.Name + "' not 'Configure...'");
			Assert.AreEqual("PushButton", gpath.Next.Next.Role.ToString(), "ParseGuiPath role 2 is '" + gpath.Next.Next.Role.ToString() + "' not 'PushButton'");
		}
		[Test]
		public void ParseIndexTest()
		{
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff", "stuff", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[3]", "stuff", 3, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff3[43]", "stuff3", 43, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[-4]", "stuff", -4, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[-654]", "stuff", -654, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[3", "stuff[3", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff3]", "stuff3]", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[*3]", "stuff", 0, "3"));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[3*]", "stuff[3*]", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("s[tu]ff[3]", "s[tu]ff", 3, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("[-98]", "", -98, null));
		}
		[Test]
		public void ParseStarTest()
		{
			Assert.IsTrue(GuiPath.parseIndexTesting("stuf[*]f", "stuf", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[*]", "stuff", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff3[*i]", "stuff3", 0, "i"));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[*id]", "stuff", 0, "id"));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[**id*]", "stuff", 0, "*id*"));
		}
		[Test]
		public void ParseVarTest()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "UtilitiesTest.xml";
			Var ins2 = new Var();
			ts.AddNamedInstruction("one",ins2);
			ins2.Set = "4";
			ins2.Execute();
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[$hi]", "stuff[$hi]", 0, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[$one]", "stuff", 4, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff3[$one;3]", "stuff3", 43, null));
			Assert.IsTrue(GuiPath.parseIndexTesting("stuff[3$one]", "stuff[3$one]", 0, null));
		}
	}
}
