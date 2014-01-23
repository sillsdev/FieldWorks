// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UtilitiesTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using SIL.FieldWorks.Common.Utils;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for UtilitiesTest.
	/// </summary>
	[TestFixture]
	public class UtilitiesTest
	{
		public UtilitiesTest()
		{
		}
		[Test]
		public void ParsePathTest()
		{
			ArrayList al = Utilities.ParsePath("View/Filters/Configure...");
			Assert.AreEqual("View",(string)al[0],"ParsePath name 0 is '"+al[0]+"' not 'View'");
			Assert.AreEqual("Filters",(string)al[1],"ParsePath name 1 is '"+al[1]+"' not 'Filters'");
			Assert.AreEqual("Configure...",(string)al[2],"ParsePath name 2 is '"+al[2]+"' not 'Configure...'");
			al = Utilities.ParsePath("//Hi/View/Filters/Configure...");
			Assert.AreEqual("/Hi",(string)al[0],"ParsePath name with '/' is '"+al[0]+"' not '/Hi'");
			Assert.AreEqual("View",(string)al[1],"ParsePath name 1 is '"+al[1]+"' not 'View'");
			Assert.AreEqual("Filters",(string)al[2],"ParsePath name 2 is '"+al[2]+"' not 'Filters'");
			Assert.AreEqual("Configure...",(string)al[3],"ParsePath name 3 is '"+al[3]+"' not 'Configure...'");
			al = Utilities.ParsePath("View/Filters//Hi/Configure...");
			Assert.AreEqual("View",(string)al[0],"ParsePath name 0 is '"+al[0]+"' not 'View'");
			Assert.AreEqual("Filters/Hi",(string)al[1],"ParsePath name 1 is '"+al[1]+"' not 'Filters/Hi'");
			Assert.AreEqual("Configure...",(string)al[2],"ParsePath name 2 is '"+al[2]+"' not 'Configure...'");
			al = Utilities.ParsePath("View/Filters/Configure...//Hi");
			Assert.AreEqual("View",(string)al[0],"ParsePath name 0 is '"+al[0]+"' not 'View'");
			Assert.AreEqual("Filters",(string)al[1],"ParsePath name 1 is '"+al[1]+"' not 'Filters'");
			Assert.AreEqual("Configure.../Hi",(string)al[2],"ParsePath name 2 is '"+al[2]+"' not 'Configure.../Hi'");
			al = Utilities.ParsePath("/View/Filters/Configure...");
			Assert.AreEqual("NONE:NAMELESS", (string)al[0], "ParsePath name 0 is '" + al[0] + "' not 'NONE:NAMELESS'");
			Assert.AreEqual("View",(string)al[1],"ParsePath name 1 is '"+al[1]+"' not 'View'");
			Assert.AreEqual("Filters",(string)al[2],"ParsePath name 2 is '"+al[2]+"' not 'Filters'");
			Assert.AreEqual("Configure...",(string)al[3],"ParsePath name 3 is '"+al[3]+"' not 'Configure...'");
			Assert.AreEqual(4,al.Count,"ParsePath count is '"+al.Count+"' not '4'");
		}
		[Test]
		public void SplitTokenTest()
		{
			string name = null;
			string type = null;
			Utilities.SplitTypedToken("View",out name,out type);
			Assert.AreEqual("View",name,"name is '"+name+"' not 'View'");
			Assert.AreEqual("none",type,"type is '"+type+"' not 'none'");
		}
		public void SplitTypedTokenTest()
		{
			string name = null;
			string type = null;
			Utilities.SplitTypedToken("menu:View",out name,out type);
			Assert.AreEqual("View",name,"name is '"+name+"' not 'View'");
			Assert.AreEqual("Menu",type,"type is '"+type+"' not 'Menu'");
		}
		[Test]
		public void IsLiteralOk()
		{
			string tok = "'this is a literal'";
			bool result = Utilities.IsLiteral(tok);
			Assert.AreEqual(true, result, "It can't tell it's a literal");
		}
		[Test]
		public void IsLiteralNotOk()
		{
			string tok = "'this is not a literal";
			bool result = Utilities.IsLiteral(tok);
			Assert.AreEqual(false, result, "It can't tell it's not a literal");
		}
		[Test]
		public void IsLiteralNull()
		{
			string tok = null;
			bool result = Utilities.IsLiteral(tok);
			Assert.AreEqual(false, result, "It can't tell it's null");
		}
		[Test]
		public void IsLiteralEmpty()
		{
			string tok = "";
			bool result = Utilities.IsLiteral(tok);
			Assert.AreEqual(false, result, "It can't tell it's empty");
		}
		[Test]
		public void getLiteralOk()
		{
			string tok = "'this is a literal'";
			string result = Utilities.GetLiteral(tok);
			Assert.AreEqual("this is a literal", result, "It can't tell it's not a literal");
		}
		[Test]
		public void isNumberOk()
		{
			string tok = "-453.90";
			bool result = Utilities.IsNumber(tok);
			Assert.AreEqual(true, result, "It can't tell it's a number");
		}
		[Test]
		public void isNumberNot()
		{
			string tok = "-45+3.90";
			bool result = Utilities.IsNumber(tok);
			Assert.AreEqual(false, result, "It can't tell it's not a number");
		}
		[Test]
		public void isNumberNull()
		{
			string tok = null;
			bool result = Utilities.IsNumber(tok);
			Assert.AreEqual(false, result, "It can't tell it's null");
		}
		[Test]
		public void isNumberEmpty()
		{
			string tok = "";
			bool result = Utilities.IsNumber(tok);
			Assert.AreEqual(false, result, "It can't tell it's empty");
		}
		[Test]
		public void getNumberOK()
		{
			string tok = "-453.90";
			double result = Utilities.GetNumber(tok);
			Assert.AreEqual(-453.9, result, "It can't tell it's empty");
		}
		[Test]
		public void typeToRoleOK()
		{
			Assert.AreEqual(AccessibleRole.PushButton,Utilities.TypeToRole("button"),"'button' is not 'pushbutton'");
			Assert.AreEqual(AccessibleRole.MenuItem,Utilities.TypeToRole("menu"),"'menu' is not 'menuitem'");
			Assert.AreEqual(AccessibleRole.None,Utilities.TypeToRole("sidebar"),"'sidebar' is not 'pushbutton'");
			Assert.AreEqual(AccessibleRole.Grouping,Utilities.TypeToRole("group"),"'group' is not 'grouping'");
			Assert.AreEqual(AccessibleRole.Text,Utilities.TypeToRole("para"),"'para' is not 'text'");
			Assert.AreEqual(AccessibleRole.Text,Utilities.TypeToRole("line"),"'line' is not 'text'");
			Assert.AreEqual(AccessibleRole.Grouping,Utilities.TypeToRole("view"),"'view' is not 'grouping'");
			Assert.AreEqual(AccessibleRole.None,Utilities.TypeToRole("other"),"'other' is not 'none'");
		}

		[Test]
		public void findInGui()
		{
			Process m_proc = Process.Start(@"C:\WINDOWS\NOTEPAD.EXE");
			AccessibilityHelper m_ah;
			m_proc.WaitForInputIdle();
			while (Process.GetProcessById(m_proc.Id).MainWindowHandle == IntPtr.Zero)
				Thread.Sleep(100);
			m_proc.WaitForInputIdle();

			 SIL.FieldWorks.Common.Utils.

			Win32.SetForegroundWindow(m_proc.MainWindowHandle);
			m_ah = new AccessibilityHelper(m_proc.MainWindowHandle);

			AccessibilityHelper ah = null;
			GuiPath gpath = new GuiPath("menu:Help/menu:Help Topics");
			ah = gpath.FindInGui(m_ah, null);
			Assert.IsNotNull(ah,"'menu:Help/menu:Help Topics' Accessibility Helper not found");
			Assert.AreEqual("Help Topics",ah.Name,"'menu:Help/menu:Help Topics' menu item not found");
			Assert.AreEqual(AccessibleRole.MenuItem,ah.Role,"'menu:Help/menu:Help Topics' menu item role not found");
			try
			{
				m_proc.WaitForInputIdle();
				Win32.SetForegroundWindow(m_proc.MainWindowHandle);
				SendKeys.SendWait("%{F4}");
				m_proc.WaitForInputIdle();
				m_proc.WaitForExit();
			}
			catch
			{
			}
		}

		[Test]
		public void evalExpr()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "UtilitiesTest.xml";
			SelectText ins1 = new SelectText();
			ts.AddNamedInstruction("one",ins1);
			ins1.Path = "window:cleaner/item:dust";
			// note: ins1 has no default data set (ie. $one.text = null)
			Var ins2 = new Var();
			ts.AddNamedInstruction("_two",ins2);
			ins2.Set = " how's this??";
			Var ins3 = new Var();
			ts.AddNamedInstruction("thr-ee",ins3);
			ins3.Set = "And $_two; this should be ignored?";

			ins2.Execute();
			ins3.Execute();

			string result = Utilities.evalExpr("$one");
			Assert.AreEqual(null,result);
			result = Utilities.evalExpr("$one;");
			Assert.AreEqual(null,result);
			result = Utilities.evalExpr("$one ");
			Assert.AreEqual(" ",result);
			result = Utilities.evalExpr(" $one");
			Assert.AreEqual(" ",result);
			result = Utilities.evalExpr("$one; ");
			Assert.AreEqual(" ",result);
			result = Utilities.evalExpr(";$one; ");
			Assert.AreEqual("; ",result);
			result = Utilities.evalExpr(";$one;;");
			Assert.AreEqual(";;",result);
			result = Utilities.evalExpr("$one1");
			Assert.AreEqual("$one1",result);
			result = Utilities.evalExpr("$on;e");
			Assert.AreEqual("$on;e",result);

			result = Utilities.evalExpr("$_two");
			Assert.AreEqual(" how's this??",result);
			result = Utilities.evalExpr("$_two;");
			Assert.AreEqual(" how's this??",result);
			result = Utilities.evalExpr("$_two ");
			Assert.AreEqual(" how's this?? ",result);
			result = Utilities.evalExpr(" $_two");
			Assert.AreEqual("  how's this??",result);
			result = Utilities.evalExpr("$_two; ");
			Assert.AreEqual(" how's this?? ",result);
			result = Utilities.evalExpr(";$_two; ");
			Assert.AreEqual("; how's this?? ",result);
			result = Utilities.evalExpr(";$_two;;");
			Assert.AreEqual("; how's this??;",result);
			result = Utilities.evalExpr("$_two1");
			Assert.AreEqual("$_two1",result);
			result = Utilities.evalExpr("$_tw;o");
			Assert.AreEqual("$_tw;o",result);

			result = Utilities.evalExpr("$one.;");
			Assert.AreEqual(null,result);
			result = Utilities.evalExpr("$one..;");
			Assert.AreEqual("[select-text-1 does not have data for '.']",result);
			result = Utilities.evalExpr("$one.path");
			Assert.AreEqual("window:cleaner/item:dust",result);
			result = Utilities.evalExpr("$one.path;");
			Assert.AreEqual("window:cleaner/item:dust",result);
			result = Utilities.evalExpr("$one.path.;");
			Assert.AreEqual("[select-text-1 does not have data for 'path.']",result);

			result = Utilities.evalExpr("text$one;$_two;$thr-ee");
			Assert.AreEqual("text how's this??And  how's this?? this should be ignored?",result);
			result = Utilities.evalExpr("text$one;$_two;$thr-ee");
			Assert.AreEqual("text how's this??And  how's this?? this should be ignored?",result);
			result = Utilities.evalExpr("text$one.path;$_two;$thr-ee OK");
			Assert.AreEqual("textwindow:cleaner/item:dust how's this??And  how's this?? this should be ignored? OK",result);
			result = Utilities.evalExpr("text $_two $one.path OK");
			Assert.AreEqual("text  how's this?? window:cleaner/item:dust OK",result);
		}
		[Test]
		public void evalFunction()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "UtilitiesTest.xml";
			SelectText ins1 = new SelectText();
			ts.AddNamedInstruction("one", ins1);
			ins1.Path = "window:cleaner/item:dust";
			// note: ins1 has no default data set (ie. $one.text = null)
			Var ins2 = new Var();
			ts.AddNamedInstruction("_two", ins2);
			ins2.Set = " how's this??";
			Var ins3 = new Var();
			ts.AddNamedInstruction("thr-ee", ins3);
			ins3.Set = "And $_two; this should be ignored?";

			ins2.Execute();
			ins3.Execute();

			string result = Utilities.evalExpr("$random()");
			Assert.IsTrue(result == "0" || result == "1", "default random not 0 or 1");
			result = Utilities.evalExpr("$random(,)");
			Assert.IsTrue(result == "$random(,)", "random can't just have a comma for an argument");
			result = Utilities.evalExpr("$random(,4)");
			Assert.IsTrue(result == "$random(,4)", "random has to have a max argument");
			result = Utilities.evalExpr("$random(4,)");
			Assert.IsTrue(result == "$random(4,)", "random has to have a min argument when there's a comma");
			result = Utilities.evalExpr("$random(45,50)");
			Assert.IsTrue(result == "$random(45,50)", "min has to be less than max");
			result = Utilities.evalExpr("$random(50,45)");
			Assert.IsTrue(result == "45" || result == "46" || result == "47" ||
						  result == "48" || result == "49" || result == "50",
				"Random must be between 45 and 50");
			result = Utilities.evalExpr("$random(-6,-10)");
			Assert.IsTrue(result == "-6" || result == "-7" || result == "-8" ||
						  result == "-9" || result == "-10",
				"Random must be between -10 and -6");
			result = Utilities.evalExpr("$random(3,-2)");
			Assert.IsTrue(result == "-2" || result == "-1" || result == "0" ||
						  result == "1" || result == "2" || result == "3",
				"Random must be between -2 and 3");
		}
	}
}
