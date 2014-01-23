// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MessageTest.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for MessageTest.
	/// </summary>
	[TestFixture]
	public class MessageTest
	{
		public MessageTest()
		{
		}
		[Test]
		public void CreateAndReadOne()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "MessageTest.xml";
			NoOp nop1 = new NoOp();
			nop1.Data = 123;
			ts.AddNamedInstruction("niceId",nop1);
			NoOp nop2 = new NoOp();
			nop2.Data = 456;
			ts.AddNamedInstruction("greatId",nop2);
			Message message = new Message();
			message.AddText("Let's count, '");
			message.AddDataRef("$niceId.data",nop1);
			message.AddText("' and '");
			message.AddDataRef("$greatId.data",nop2);
			message.AddText("' to see it work!");
			string result = message.Read();
			Assert.AreEqual("Let's count, '123' and '456' to see it work!",result,"Message not relayed intact!");
		}
		[Test]
		public void DataRefWithNoId()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "MessageTest.xml";
			NoOp nop1 = new NoOp();
			nop1.Data = 123;
			ts.AddNamedInstruction("niceId",nop1);
			NoOp nop2 = new NoOp();
			nop2.Data = 456;
			ts.AddNamedInstruction("greatId",nop2);
			Message message = new Message();
			message.AddText("Let's count, '");
			message.AddDataRef("$.data",nop1);
			message.AddText("' and '");
			message.AddDataRef("$greatId.data",nop2);
			message.AddText("' to see it work!");
			string result = message.Read();
			Assert.AreEqual("Let's count, '123' and '456' to see it work!",result,"Message not relayed intact!");
		}
		[Test]
		public void DataRefDefaulted()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "MessageTest.xml";
			NoOp nop1 = new NoOp();
			nop1.Data = 123;
			ts.AddNamedInstruction("niceId",nop1);
			NoOp nop2 = new NoOp();
			nop2.Data = 456;
			ts.AddNamedInstruction("greatId",nop2);
			Message message = new Message();
			message.AddText("Let's count, '");
			message.AddDataRef("$niceId",nop1);
			message.AddText("' and '");
			message.AddDataRef("$greatId.data",nop2);
			message.AddText("' to see it work!");
			string result = message.Read();
			Assert.AreEqual("Let's count, '123' and '456' to see it work!",result,"Message not relayed intact!");
		}
		[Test]
		public void DataRefAllDefaulted()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "MessageTest.xml";
			NoOp nop1 = new NoOp();
			nop1.Data = 123;
			ts.AddNamedInstruction("niceId",nop1);
			NoOp nop2 = new NoOp();
			nop2.Data = 456;
			ts.AddNamedInstruction("greatId",nop2);
			Message message = new Message();
			message.AddText("Let's count, '");
			message.AddDataRef("$",nop1);
			message.AddText("' and '");
			message.AddDataRef("$greatId.data",nop2);
			message.AddText("' to see it work!");
			string result = message.Read();
			Assert.AreEqual("Let's count, '123' and '456' to see it work!",result,"Message not relayed intact!");
		}
	}
}
