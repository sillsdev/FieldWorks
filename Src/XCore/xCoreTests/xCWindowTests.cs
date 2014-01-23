// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XWindowTests.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using NUnit.Framework;

namespace XCore
{

	#region XWindow tests

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test XWindow methods.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class XWindowTests : XWindowTestsBase
	{
		protected override string TestFile
		{
			get { return "basicTest.xml"; }
		}

		[Test]
		public void ItemCountOfListMenu()
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			Assert.AreEqual( adapter.GetItemCountOfGroup("Vowels"), 5);
			//CheckMenuCount("Vowels", 5);
		}
		[Test]
		public void ItemCountOfListSubMenu()
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			Assert.AreEqual( adapter.GetItemCountOfGroup("SubVowels"), 5);
			//CheckSubMenuCount("Type", "Vowels",5);
		}

		[Test]
		public void ToggleBoolMenuItem ()
		{
			ClearTesterFields ();
			bool original =(bool) m_window.PropertyTable.GetBoolProperty("toggleTest",false);
			ClickMenuItem("Misc", "toggle");
			Assert.AreEqual( (bool) m_window.PropertyTable.GetValue("toggleTest"), !original);
			ClickMenuItem("Misc", "toggle");
			Assert.AreEqual((bool) m_window.PropertyTable.GetValue("toggleTest"),original);
		}

		[Test][Ignore("Temporary due to Broadcast now being a defered action. (TEST needs to be revised.)")]
		public void PropertyChangeBroadcast ()
		{
			ClearTesterFields ();
			Assert.AreEqual(CurrentOutputValue,"","The output pane should have cleared");
			ClickMenuItem("Misc", "toggle");
			// At this point the broadcast msgs are queued up on the mediator waiting for this
			// to finish so they can be processed.  The next line was written when all
			// broadcasts were sent as soon as they were called, now they are defered until after
			// the current processing finishes.
			// Assert.IsFalse(CurrentOutputValue== "");
		}

		[Test]
		public void SimpleMenuCommand ()
		{
			CurrentOutputValue = "hello there";
			ClearTesterFields ();
			Assert.AreEqual(CurrentOutputValue , "","The output pane should have cleared");
		}

		[Test]
		public void CommandWithParameter()
		{
			ClearTesterFields();
			ClickMenuItem("Type", "A");
			Assert.AreEqual("A", TesterControl.letters.Text, "The letters field should now show 'A'");
		}

		[Test]
		public void EnableDisableCommandMenuItem()
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;

			Assert.IsFalse(adapter.IsItemEnabled("Misc", "enableTest")	,"The 'enable test' item should have been disabled");

			//the tester control will disable this item,when it is polled,
			//	if this check box is not checked
			//			TesterControl.cbEnableTest.Checked = false;
			//			MenuItem menu =FindMenu("Misc");
			//			menu.PerformClick();//cause the polling of the display properties of each of the items
			//			MenuItem item =FindMenuItem("enableTest");
			//			Assert.IsFalse(item.Enabled,"The 'enable test' item should have been disabled");

			//now enable it
			TesterControl.cbEnableTest.Checked = true;
			//			menu.PerformClick();//cause the polling of the display properties of each of the items
			//			//this will actually be a new MenuItem as it is re-created each time
			//			item =FindMenuItem("enableTest");
			//			Assert.IsTrue (item.Enabled,"The 'enable test' item should have been enabled");

			Assert.IsTrue(adapter.IsItemEnabled("Misc", "enableTest")	,"The 'enable test' item should have been emabled");
		}

		/// <summary>
		/// This tests the persistence of the windowSize property in the PropertyTable.
		/// </summary>
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Depends on native control in comctl32.dll")]
		public void WindowSizePersistence()
		{
			// This is one of only two tests that really has to show a window.
			m_window.WindowState = FormWindowState.Normal;
			var windSize = m_window.Size;
			m_window.Show();
			var newWindSize = new Size(windSize.Width + 20, windSize.Height + 30);
			m_window.Size = newWindSize;
			var persistedWinSize = m_window.PropertyTable.GetValue("windowSize");
			ReopenWindow();
			var newWinSize = m_window.PropertyTable.GetValue("windowSize");
			Assert.AreEqual(persistedWinSize, newWinSize);
		}

		/// <remarks>This test has a letter to make it run at the end, since it changes the interface but other tests rely on.
		/// With the introduction of nunit 2.2, the order changed to alphabetical rather than code file order, and we suddenly found
		/// that tests that are relying on a given set up our been screwed up by tests like this which change the state of things..</remarks>
		[Test]
		public void ZDynamicallyModifyListGroup ()
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			TesterControl.cbModifyVowelList .Checked = false;
			//			MenuItem menu =FindMenu("Vowels");
			//			menu.PerformClick();//cause the polling of the display properties
			//			MenuItem item =FindMenuItem("OO");		//this item should not be there yet
			//			Assert.IsTrue(item == null,"The extra Vowels are not supposed to be there yet. the list should revert to what is in the XML configuration if no colleagues modifies it.");
			Assert.IsFalse(adapter.HasItem("Vowels","OO"), "The extra Vowels are not supposed to be there yet. the list should revert to what is in the XML configuration if no colleagues modifies it.");

			TesterControl.cbModifyVowelList .Checked = true;
			//			menu.PerformClick();//cause the polling of the display properties
			//			item =FindMenuItem("OO");		//make sure one of the new items was added
			//			Assert.IsTrue(item != null,"The extra Vowels do not seem to have been added");
			Assert.IsTrue(adapter.HasItem("Vowels","OO"), "The extra Vowels do not seem to have been added");
		}

		/// <remarks>This test has a letter to make it run at the end, since it changes the interface but other tests rely on.
		/// With the introduction of nunit 2.2, the order changed to alphabetical rather than code file order, and we suddenly found
		/// that tests that are relying on a given set up our been screwed up by tests like this which change the state of things..</remarks>
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Depends on native control in comctl32.dll")]
		public void ZPropertyPersistence ()
		{
			// This is one of only two tests that really has to show a window.
			m_window.Show();
			Random r = new Random();
			int x =r.Next();
			m_window.Mediator.PropertyTable.SetProperty("testRandom",x);
			m_window.Mediator.PropertyTable.SetPropertyPersistence("testRandom", true);
			ReopenWindow();
			Assert.AreEqual(x,m_window.Mediator.PropertyTable.GetIntProperty ("testRandom",x-1));

			//now say that the property should not be persisted
			m_window.Mediator.PropertyTable.SetPropertyPersistence("testRandom", false);
			ReopenWindow();
			Assert.AreEqual(x-1,m_window.Mediator.PropertyTable.GetIntProperty ("testRandom",x-1));
		}

		protected string CurrentOutputValue
		{
			get
			{
				return TesterControl.output.Text;
			}
			set
			{
				TesterControl.output.Text= value;
			}
		}
		protected Tester TesterControl
		{
			get
			{
				return (Tester)m_window.CurrentContentControl;
			}
		}
		protected void ClearTesterFields ()
		{
			ClickMenuItem("DebugMenu", "ClearFields");
		}
		#endregion

		#region menu checking utilities
		//		protected void VisitMenu(MenuItem menu)
		//		{
		//			menu.PerformClick();
		//		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating the UI by passing a stream
		/// </summary>
		/// <remarks>This test has a letter to make it run at the end, since it changes the interface but other tests rely on.
		/// With the introduction of nunit 2.2, the order changed to alphabetical rather than code file order, and we suddenly found
		/// that tests that are relying on a given set up our been screwed up by tests like this which change the state of things..</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ZCreateUIfromStream()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			using (System.IO.Stream stream =
				assembly.GetManifestResourceStream("XCore.basicTest.xml"))
			{
				Assert.IsNotNull(stream, "Couldn't get the XML file.");

				using (XCore.XWindow window = new XWindow())
				{
					window.PropertyTable.UserSettingDirectory = m_settingsPath;
					window.LoadUI(stream);

					ITestableUIAdapter adapter = (ITestableUIAdapter)window.MenuAdapter;
					Assert.AreEqual(adapter.GetItemCountOfGroup("Vowels"), 5);
				}
			}
		}
	}

	#endregion // XWindowTests tests
}
