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
// File: ApplicationContextTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Tests the ApplicationContext class.
	/// </summary>
	[TestFixture]
	public class ApplicationContextTest
	{
		public ApplicationContextTest()
		{
		}
		[Test]
		public void CreateAppContext()
		{
			ApplicationContext ac = MakeAppContext();
			Assert.IsNotNull(ac, "Application context not created.");
		}
		[Test]
		public void CreateAppContextFromSource()
		{
			TestState ts = TestState.getOnly("LT");
			ApplicationContext ac = MakeAppContext();
			ts.AddNamedInstruction("Find me",ac);
			ApplicationContext ac2 = new ApplicationContext();
			ac2.SetSource("Find me");
			Assert.AreEqual(ac,(ApplicationContext)ac2.TestGetOfSource(),"Context source not set right");
		}
		[Test]
		public void PassFailBothNull()
		{
			string onPassIn = null;
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void FailIsSkip()
		{
			string onPassIn = null;
			string onFailIn = "skip";
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}
		[Test]
		public void FailIsAssert()
		{
			string onPassIn = null;
			string onFailIn = "assert";
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void PassIsSkip()
		{
			string onPassIn = "skip";
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass, "on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void PassIsAssert()
		{
			string onPassIn = "assert";
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("assert",onPass,"on-pass is '"+onPass+"' not 'assert'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}
		[Test]
		public void PassFailBothAssert()
		{
			string onPassIn = "assert";
			string onFailIn = "assert";
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("assert",onPass,"on-pass is '"+onPass+"' not 'assert'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void PassFailBothSkip()
		{
			string onPassIn = "skip";
			string onFailIn = "skip";
			string onPass = null;
			string onFail = null;
			PassFailContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}

		[Test]
		public void ContextPassFailBothNull()
		{
			string onPassIn = null;
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void ContextFailIsSkip()
		{
			string onPassIn = null;
			string onFailIn = "skip";
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}
		[Test]
		public void ContextFailIsAssert()
		{
			string onPassIn = null;
			string onFailIn = "assert";
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void ContextPassIsSkip()
		{
			string onPassIn = "skip";
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void ContextPassIsAssert()
		{
			string onPassIn = "assert";
			string onFailIn = null;
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("assert",onPass,"on-pass is '"+onPass+"' not 'assert'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}
		[Test]
		public void ContextPassFailBothAssert()
		{
			string onPassIn = "assert";
			string onFailIn = "assert";
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("assert",onPass,"on-pass is '"+onPass+"' not 'assert'");
			Assert.AreEqual("assert",onFail,"on-fail is '"+onFail+"' not 'assert'");
		}
		[Test]
		public void ContextPassFailBothSkip()
		{
			string onPassIn = "skip";
			string onFailIn = "skip";
			string onPass = null;
			string onFail = null;
			PassFailAncestorContext(onPassIn,onFailIn,out onPass,out onFail);
			Assert.AreEqual("skip",onPass,"on-pass is '"+onPass+"' not 'skip'");
			Assert.AreEqual("skip",onFail,"on-fail is '"+onFail+"' not 'skip'");
		}

		public void PassFailContext(string PassIn, string FailIn, out string onPass, out string onFail)
		{
			// Set up a typical instruction tree.
			TestState ts = TestState.getOnly("LT");
			NoOp op1 = new NoOp();
			NoOp op2 = new NoOp();
			NoOp op3 = new NoOp();
			Desktop Desk = new Desktop();
			Desk.Add(op1);
			op1.Parent = Desk;
			Desk.Add(op2);
			op2.Parent = Desk;
			Desk.Add(op3);
			op3.Parent = Desk;
			ApplicationContext ac = MakeAppContext();
			ts.AddNamedInstruction("Find me",ac);
			ApplicationContext AppCon = new ApplicationContext();
			AppCon.SetSource("Find me");
			Assert.AreEqual(ac,(ApplicationContext)AppCon.TestGetOfSource(),"Context source not set right");
			Desk.Add(AppCon);
			NoOp ins1 = new NoOp();
			NoOp ins2 = new NoOp();
			NoOp ins3 = new NoOp();
			AppCon.Add(ins1);
			ins1.Parent = AppCon;
			AppCon.Add(ins2);
			ins2.Parent = AppCon;
			AppCon.Add(ins3);
			ins3.Parent = AppCon;
			ins2.PassFailInContext(PassIn,FailIn,out onPass,out onFail);
		}
		public void PassFailAncestorContext(string PassIn, string FailIn, out string onPass, out string onFail)
		{
			// Set up a typical instruction tree.
			TestState ts = TestState.getOnly("LT");
			ts.Script = "ApplicationContextTest.xml";
			NoOp op1 = new NoOp();
			NoOp op2 = new NoOp();
			NoOp op3 = new NoOp();
			Desktop Desk = new Desktop();
			Desk.OnPass = PassIn;
			Desk.OnFail = FailIn;
			Desk.Add(op1);
			op1.Parent = Desk;
			Desk.Add(op2);
			op2.Parent = Desk;
			Desk.Add(op3);
			op3.Parent = Desk;
			ApplicationContext ac = MakeAppContext();
			ts.AddNamedInstruction("Find me",ac);
			ApplicationContext AppCon = new ApplicationContext();
			AppCon.SetSource("Find me");
			Assert.AreEqual(ac,(ApplicationContext)AppCon.TestGetOfSource(),"Context source not set right");
			Desk.Add(AppCon);
			AppCon.Parent = Desk;
			NoOp ins1 = new NoOp();
			NoOp ins2 = new NoOp();
			NoOp ins3 = new NoOp();
			AppCon.Add(ins1);
			ins1.Parent = AppCon;
			AppCon.Add(ins2);
			ins2.Parent = AppCon;
			AppCon.Add(ins3);
			ins3.Parent = AppCon;
			ins2.PassFailInContext(null,null,out onPass,out onFail);
		}
		private ApplicationContext MakeAppContext()
		{
			ApplicationContext ac = new ApplicationContext();
			ac.Run = "ok";
			//ac.GuiModel = "../../../GuiModels/TE.xml";
			ac.Path  = @"C:\fw\output\debug";
			ac.Exe   = "Harvest.exe";
			ac.Title = "Harvest";
			//string src = GetAttribute(xn, "source");
			//if (src != null) ac.SetSource(src, ts);
			ac.OnPass = "skip";
			ac.OnFail = "assert";
			return ac;
		}
	}
}
