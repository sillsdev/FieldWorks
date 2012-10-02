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
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using NUnit.Framework;

using XCore;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests merging two main menus
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MainMenuMergeTest: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItems()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "First", null,
				null, null, null));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
							null, null, null));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("First", result.MenuItems[0].Text);
							Assert.AreEqual("Second", result.MenuItems[1].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items with MergeOrder property set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItemsMergeOrderSet()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.Add, 1, Shortcut.None, "First", null,
						null, null, null));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
							null, null, null));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("Second", result.MenuItems[0].Text);
							Assert.AreEqual("First", result.MenuItems[1].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menu items with MergeOrder property set but merge on the other menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2MenuItemsMergeOrderSetOn2ndMenu()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.Add, 1, Shortcut.None, "First", null,
						null, null, null));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
							null, null, null));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("Second", result.MenuItems[0].Text);
							Assert.AreEqual("First", result.MenuItems[1].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Add2Menus()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "First", null,
						null, null, new MenuItem[] { new MenuItem("FirstOne") }));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.Add, 0, Shortcut.None, "Second", null,
							null, null, new MenuItem[] { new MenuItem("SecondOne") }));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("First", result.MenuItems[0].Text);
							Assert.AreEqual("Second", result.MenuItems[1].Text);
							Assert.AreEqual(1, result.MenuItems[0].MenuItems.Count);
							Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
							Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
							Assert.AreEqual("SecondOne", result.MenuItems[1].MenuItems[0].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2Menus()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
						null, null, new MenuItem[] { new MenuItem("FirstOne") }));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
							null, null, new MenuItem[] { new MenuItem("SecondOne") }));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(1, result.MenuItems.Count);
							Assert.AreEqual("First", result.MenuItems[0].Text);
							Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
							Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
							Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusOn2ndMenu()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
						null, null, new MenuItem[] { new MenuItem("FirstOne") }));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
							null, null, new MenuItem[] { new MenuItem("SecondOne") }));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(second, first))
						{
							Assert.AreEqual(1, result.MenuItems.Count);
							Assert.AreEqual("Second", result.MenuItems[0].Text);
							Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
							Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[0].Text);
							Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[1].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus where first menu has more main menu items than second
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusLargerFirstMenu()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
						null, null, new MenuItem[] { new MenuItem("FirstOne") }));
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 1, Shortcut.None, "First2", null,
						null, null, new MenuItem[] { new MenuItem("First2One") }));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
							null, null, new MenuItem[] { new MenuItem("SecondOne") }));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("First", result.MenuItems[0].Text);
							Assert.AreEqual("First2", result.MenuItems[1].Text);
							Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
							Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
							Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
							Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
							Assert.AreEqual("First2One", result.MenuItems[1].MenuItems[0].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus where second menu has more main menu items than first
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Merge2MenusLargerSecondMenu()
		{
			using (MainMenu first = new MainMenu())
			{
				first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
						null, null, new MenuItem[] { new MenuItem("FirstOne") }));

				using (MainMenu second = new MainMenu())
				{
					second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
							null, null, new MenuItem[] { new MenuItem("SecondOne") }));
					second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 1, Shortcut.None, "Second2", null,
							null, null, new MenuItem[] { new MenuItem("Second2One") }));

					using (MenuExtender menuExtender = new MenuExtender())
					{
						using (MainMenu result = menuExtender.MergeMenus(first, second))
						{
							Assert.AreEqual(2, result.MenuItems.Count);
							Assert.AreEqual("First", result.MenuItems[0].Text);
							Assert.AreEqual("Second2", result.MenuItems[1].Text);
							Assert.AreEqual(2, result.MenuItems[0].MenuItems.Count);
							Assert.AreEqual("FirstOne", result.MenuItems[0].MenuItems[0].Text);
							Assert.AreEqual("SecondOne", result.MenuItems[0].MenuItems[1].Text);
							Assert.AreEqual(1, result.MenuItems[1].MenuItems.Count);
							Assert.AreEqual("Second2One", result.MenuItems[1].MenuItems[0].Text);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge two menus and make sure that subscribted to only one click event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOnlyOneClickEventSubscribed()
		{
			MessageTest messageTest = new MessageTest();
				messageTest.m_nFirstOneClicked = 0;
				messageTest.m_nSecondOneClicked = 0;

				using (var mediator = new Mediator())
				using (MenuExtender menuExtender = new MenuExtender())
				{
					menuExtender.MessageMediator = mediator;
					menuExtender.MessageMediator.AddColleague(messageTest);

					using (MainMenu first = new MainMenu())
					{
						using (MenuItem firstOne = new MenuItem("FirstOne"))
						{
							first.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "First", null,
									null, null, new MenuItem[] { firstOne }));
							menuExtender.SetCommandId(firstOne, "FirstOne");

							using (MainMenu second = new MainMenu())
							{
								using (MenuItem secondOne = new MenuItem("SecondOne"))
								{
									second.MenuItems.Add(new MenuItem(MenuMerge.MergeItems, 0, Shortcut.None, "Second", null,
											null, null, new MenuItem[] { secondOne }));
									menuExtender.SetCommandId(secondOne, "SecondOne");

									using (MainMenu result = menuExtender.MergeMenus(first, second))
									{
										result.MenuItems[0].MenuItems[0].PerformClick();
										result.MenuItems[0].MenuItems[1].PerformClick();

										Assert.AreEqual(1, messageTest.m_nFirstOneClicked);
										Assert.AreEqual(1, messageTest.m_nSecondOneClicked);
										// Clean up the mess now.
										menuExtender.MessageMediator.RemoveColleague(messageTest);
									}
								}
							}
						}
					}
				}
			}
		#region MessageTest class
		internal class MessageTest : IxCoreColleague
			{
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
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a pointer to ourself
			/// </summary>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public IxCoreColleague[] GetMessageTargets()
			{
				return new IxCoreColleague[] { this };
			}

			/// <summary>
			/// Should not be called if disposed (or in the process of disposing).
			/// </summary>
			public bool ShouldNotCall
			{
				get { return false; }
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
