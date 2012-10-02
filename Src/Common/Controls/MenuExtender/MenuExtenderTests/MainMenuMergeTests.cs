// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MainMenuMerge.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests merging two main menus
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MainMenuMergeTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItems()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "First", null,
				null, null, null));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
				null, null, null));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("First", result.MenuItems[0].Text);
			Assert.AreEqual("Second", result.MenuItems[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items with MergeOrder property set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItemsMergeOrderSet()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.Add, 1, Shortcut.None, "First", null,
				null, null, null));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
				null, null, null));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("Second", result.MenuItems[0].Text);
			Assert.AreEqual("First", result.MenuItems[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items with MergeOrder property set but merge on the other menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItemsMergeOrderSetOn2ndMenu()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.Add, 1, Shortcut.None, "First", null,
				null, null, null));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
				null, null, null));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("Second", result.MenuItems[0].Text);
			Assert.AreEqual("First", result.MenuItems[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2Menus()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "First", null,
				null, null, new MenuItem[] { new MenuItem("FirstOne") }));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
				null, null, new MenuItem[] { new MenuItem("SecondOne") }));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("First", result.MenuItems[0].Text);
			Assert.AreEqual("Second", result.MenuItems[1].Text);
			Assert.AreEqual(1, result.MenuItems[0].MenuItems.Count);
			Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
			Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
			Assert.AreEqual("SecondOne", result.MenuItems[1].MenuItems[0].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2Menus()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
				null, null, new MenuItem[] { new MenuItem("FirstOne") }));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
				null, null, new MenuItem[] { new MenuItem("SecondOne") }));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(1, result.MenuItems.Count);
			Assert.AreEqual("First", result.MenuItems[0].Text);
			Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
			Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
			Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusOn2ndMenu()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
				null, null, new MenuItem[] { new MenuItem("FirstOne") }));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
				null, null, new MenuItem[] { new MenuItem("SecondOne") }));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(second, first);
			Assert.AreEqual(1, result.MenuItems.Count);
			Assert.AreEqual("Second", result.MenuItems[0].Text);
			Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
			Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[0].Text);
			Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus where first menu has more main menu items than second
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusLargerFirstMenu()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
				null, null, new MenuItem[] { new MenuItem("FirstOne") }));
			first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 1, Shortcut.None, "First2", null,
				null, null, new MenuItem[] { new MenuItem("First2One") }));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
				null, null, new MenuItem[] { new MenuItem("SecondOne") }));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("First", result.MenuItems[0].Text);
			Assert.AreEqual("First2", result.MenuItems[1].Text);
			Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
			Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
			Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
			Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
			Assert.AreEqual("First2One", result.MenuItems[1].MenuItems[0].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus where second menu has more main menu items than first
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusLargerSecondMenu()
		{
			MainMenu first = new MainMenu();
			first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
				null, null, new MenuItem[] { new MenuItem("FirstOne") }));

			MainMenu second = new MainMenu();
			second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
				null, null, new MenuItem[] { new MenuItem("SecondOne") }));
			second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 1, Shortcut.None, "Second2", null,
				null, null, new MenuItem[] { new MenuItem("Second2One") }));

			MenuExtender menuExtender = new MenuExtender();
			MainMenu result = menuExtender.MergeMenus(first, second);
			Assert.AreEqual(2, result.MenuItems.Count);
			Assert.AreEqual("First", result.MenuItems[0].Text);
			Assert.AreEqual("Second2", result.MenuItems[1].Text);
			Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
			Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
			Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
			Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
			Assert.AreEqual("Second2One", result.MenuItems[1].MenuItems[0].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus and make sure that subscribted to only one click event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOnlyOneClickEventSubscribed()
		{
			using (MessageTest messageTest = new MessageTest())
			{
				messageTest.m_nFirstOneClicked = 0;
				messageTest.m_nSecondOneClicked = 0;

				MenuExtender menuExtender = new MenuExtender();
				menuExtender.MessageMediator = new Mediator();
				menuExtender.MessageMediator.AddColleague(messageTest);

				MainMenu first = new MainMenu();
				MenuItem firstOne = new MenuItem("FirstOne");
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
					null, null, new MenuItem[] { firstOne }));
				menuExtender.SetCommandId(firstOne, "FirstOne");

				MainMenu second = new MainMenu();
				MenuItem secondOne = new MenuItem("SecondOne");
				second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
					null, null, new MenuItem[] { secondOne }));
				menuExtender.SetCommandId(secondOne, "SecondOne");

				MainMenu result = menuExtender.MergeMenus(first, second);
				result.MenuItems[0].MenuItems[0].PerformClick();
				result.MenuItems[0].MenuItems[1].PerformClick();

				Assert.AreEqual(1, messageTest.m_nFirstOneClicked);
				Assert.AreEqual(1, messageTest.m_nSecondOneClicked);
				// Clean up the mess now.
				menuExtender.MessageMediator.RemoveColleague(messageTest);
				menuExtender.MessageMediator.Dispose();
				menuExtender.MessageMediator = null;
			}
		}

		#region MessageTest class
		internal class MessageTest : IxCoreColleague, IFWDisposable
		{
			private Mediator m_mediator;

			#region IDisposable & Co. implementation
			// Region last reviewed: never

			/// <summary>
			/// True, if the object has been disposed.
			/// </summary>
			private bool m_isDisposed = false;

			/// <summary>
			/// See if the object has been disposed.
			/// </summary>
			public bool IsDisposed
			{
				get { return m_isDisposed; }
			}

			/// <summary>
			/// Check to see if the object has been disposed.
			/// All public Properties and Methods should call this
			/// before doing anything else.
			/// </summary>
			public void CheckDisposed()
			{
				if (IsDisposed)
					throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
			}

			/// <summary>
			/// Finalizer, in case client doesn't dispose it.
			/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
			/// </summary>
			/// <remarks>
			/// In case some clients forget to dispose it directly.
			/// </remarks>
			~MessageTest()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
			}

			/// <summary>
			///
			/// </summary>
			/// <remarks>Must not be virtual.</remarks>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SupressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected virtual void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (m_isDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_mediator = null;

				m_isDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			#region IxCoreColleague Members

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Do nothing
			/// </summary>
			/// <param name="mediator"></param>
			/// <param name="configurationParameters"></param>
			/// --------------------------------------------------------------------------------
			public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
			{
				CheckDisposed();

				m_mediator = mediator;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a pointer to ourself
			/// </summary>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public IxCoreColleague[] GetMessageTargets()
			{
				CheckDisposed();

				return new IxCoreColleague[] { this };
			}

			#endregion

			internal int m_nFirstOneClicked = 0;
			internal int m_nSecondOneClicked = 0;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// FirstOne menu item clicked
			/// </summary>
			/// <param name="menuItem"></param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected bool OnFirstOne(object menuItem)
			{
				m_nFirstOneClicked++;
				return true;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// SecondOne menu item clicked
			/// </summary>
			/// <param name="menuItem"></param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected bool OnSecondOne(object menuItem)
			{
				m_nSecondOneClicked++;
				return true;
			}
		}
		#endregion

	}
}
