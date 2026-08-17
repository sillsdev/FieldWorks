// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AreaListenerTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexText;
using XCore;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;

namespace LexTextDllTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains tests of AreaListener.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AreaListenerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member Data

		private const string ClerkXPath = "//item[@value='lists']/parameters/clerks/clerk";
		private const string CommandXPath = "//commands/command";
		private const string ContextMenuXPath = "//contextMenus/menu";
		private const string ToolXPath = "//item[@value='lists']/parameters/tools/tool";

		/// <summary>
		/// For testing.
		/// </summary>
		private AreaListener m_listener;

		/// <summary>
		/// For testing.
		/// </summary>
		private Mediator m_mediator;

		private PropertyTable m_propertyTable;

		/// <summary>
		/// For testing.
		/// </summary>
		private XmlNode m_testWindowConfig;

		#endregion

		// Fixture Setup
		protected override void CreateTestData()
		{
			base.CreateTestData();

			SetupTestMediator();

			// Setup test AreaListener
			m_listener = new AreaListener();
			m_listener.Init(m_mediator, m_propertyTable, null);
		}

		[TearDown]
		public void TearDown()
		{
			if (m_listener != null)
			{
				m_listener.Dispose();
				m_listener = null;

			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
		}

		#region Helper Methods

		private void SetupTestMediator()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, true);
			m_testWindowConfig = SetupMinimalWindowConfig();
			var cmdSet = new CommandSet(m_mediator);
			cmdSet.Init(m_testWindowConfig);
			m_mediator.Initialize(cmdSet);
		}

		private static XmlNode SetupMinimalWindowConfig()
		{
			var fakeWindowConfig = new XmlDocument();
			fakeWindowConfig.LoadXml(
				"<root>"
				+ "<commands>"
				  + "<command id=\"CmdJumpToBogusList\" label=\"Show in bogus list\" message=\"JumpToTool\">"
					+ "<parameters tool=\"BogusEdit\" className=\"CmPossibility\"/>"
				  + "</command>"
				+ "</commands>"
				+ "<contextMenus/>"
				+ "<item label=\"Lists\" value=\"lists\" icon=\"folder-lists\">"
				  + "<parameters id=\"lists\">"
					+ "<clerks>"
					+ "</clerks>"
					+ "<tools>"
					+ "</tools>"
				  + "</parameters>"
				+ "</item>"
			  + "</root>");
			return fakeWindowConfig.DocumentElement;
		}

		/// <summary>
		/// Builds a window configuration whose 'lists' area has a single tool wired, via its clerk's
		/// recordList, to the possibility list identified by <paramref name="listGuid"/>.
		/// </summary>
		private static XmlNode SetupWindowConfigWithListTool(string clerkId, string toolValue, string listGuid)
		{
			var fakeWindowConfig = new XmlDocument();
			fakeWindowConfig.LoadXml(
				"<root>"
				+ "<commands/>"
				+ "<contextMenus/>"
				+ "<item label=\"Lists\" value=\"lists\" icon=\"folder-lists\">"
				  + "<parameters id=\"lists\">"
					+ "<clerks>"
					  + "<clerk id=\"" + clerkId + "\">"
						+ "<recordList owner=\"unowned\" property=\"" + listGuid + "\"/>"
					  + "</clerk>"
					+ "</clerks>"
					+ "<tools>"
					  + "<tool value=\"" + toolValue + "\">"
						+ "<control><parameters clerk=\"" + clerkId + "\"/></control>"
					  + "</tool>"
					+ "</tools>"
				  + "</parameters>"
				+ "</item>"
			  + "</root>");
			return fakeWindowConfig.DocumentElement;
		}

		private void UseTestWindowConfiguration()
		{
			m_propertyTable.SetProperty("WindowConfiguration", m_testWindowConfig, true);
			m_propertyTable.SetPropertyPersistence("WindowConfiguration", false);
		}

		private ICmPossibilityList CreateUnownedList(string name)
		{
			return Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>()
				.CreateUnowned(name, WritingSystemServices.kwsAnals);
		}

		/// <summary>
		/// Refreshes the Lists area sidebar the way xCore does and returns the tool name of every
		/// entry the refresh left in the display, in display order.
		/// </summary>
		private List<string> RefreshListsToolsDisplay()
		{
			var display = new UIListDisplayProperties(new XCore.List(null));
			m_listener.OnDisplayListsToolsList(null, ref display);
			return display.List.Cast<ListItem>().Select(item => item.value).ToList();
		}

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method AddListToXmlConfig.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddListToXmlConfig()
		{
			// Setup
			var node = m_testWindowConfig;
			//var fakeUIDisplay = new UIListDisplayProperties(new XCore.List(node.SelectSingleNode(ToolXPath), null));
			//var cdispNodesBefore = fakeUIDisplay.List.Count;
			var contextNodes = node.SelectNodes(ContextMenuXPath);
			var ccontextNodesBefore = contextNodes == null ? 0 : contextNodes.Count;
			var commandNodes = node.SelectNodes(CommandXPath);
			var ccommandNodesBefore = commandNodes == null ? 0 : commandNodes.Count;
			var clerkNodes = node.SelectNodes(ClerkXPath);
			var cclerkNodesBefore = clerkNodes == null ? 0 : clerkNodes.Count;
			var toolNodes = node.SelectNodes(ToolXPath);
			var ctoolNodesBefore = toolNodes == null ? 0 : toolNodes.Count;

			var testList = CreateUnownedList("testList1");

			// SUT
			m_listener.AddListsToWindowConfig(new List<ICmPossibilityList> { testList }, node);

			// Verify
			// The above routine no longer handles display nodes
			//Assert.That(fakeUIDisplay.List.Count, Is.EqualTo(cdispNodesBefore + 1), "Didn't add a display node.");
			var ctoolNodesAfter = node.SelectNodes(ToolXPath).Count;
			Assert.That(ctoolNodesAfter, Is.EqualTo(ctoolNodesBefore + 1), "Didn't add a tool node.");
			var cclerkNodesAfter = node.SelectNodes(ClerkXPath).Count;
			Assert.That(cclerkNodesAfter, Is.EqualTo(cclerkNodesBefore + 1), "Didn't add a clerk node.");
			var ccommandNodesAfter = node.SelectNodes(CommandXPath).Count;
			Assert.That(ccommandNodesAfter, Is.EqualTo(ccommandNodesBefore + 1), "Didn't add a command node.");
			var ccontextNodesAfter = node.SelectNodes(ContextMenuXPath).Count;
			Assert.That(ccontextNodesAfter, Is.EqualTo(ccontextNodesBefore + 1), "Didn't add a context menu node.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Every ownerless list created since the previous refresh must reach the Lists area, not
		/// just one of them; a single session can create several.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FillListAreaList_AddsEveryListCreatedSinceLastRefresh()
		{
			UseTestWindowConfiguration();
			CreateUnownedList("Original List");
			var afterFirstRefresh = RefreshListsToolsDisplay();
			Assert.That(afterFirstRefresh, Does.Contain("OriginalListEdit"));

			CreateUnownedList("Second List");
			CreateUnownedList("Third List");

			// SUT
			var afterSecondRefresh = RefreshListsToolsDisplay();

			Assert.That(afterSecondRefresh.Except(afterFirstRefresh),
				Is.EquivalentTo(new[] { "SecondListEdit", "ThirdListEdit" }), "Missed a new list.");
			Assert.That(afterSecondRefresh, Is.Unique, "Showed a list more than once.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// A refresh that finds no new ownerless list must leave the window configuration and the
		/// mediator command set alone. Adding a list a second time appends duplicate tool, clerk
		/// and context menu nodes, and throws on its already registered command id.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FillListAreaList_RepeatedRefresh_DoesNotDuplicateConfigNodes()
		{
			UseTestWindowConfiguration();
			CreateUnownedList("Original List");
			RefreshListsToolsDisplay();
			CreateUnownedList("Second List");
			CreateUnownedList("Third List");
			var afterListsAdded = RefreshListsToolsDisplay();
			var ctoolNodes = m_testWindowConfig.SelectNodes(ToolXPath).Count;
			var cclerkNodes = m_testWindowConfig.SelectNodes(ClerkXPath).Count;
			var ccommandNodes = m_testWindowConfig.SelectNodes(CommandXPath).Count;
			var ccontextNodes = m_testWindowConfig.SelectNodes(ContextMenuXPath).Count;

			// SUT: a refresh with nothing new to pick up
			var afterIdleRefresh = RefreshListsToolsDisplay();

			Assert.That(afterIdleRefresh, Is.EquivalentTo(afterListsAdded));
			Assert.That(m_testWindowConfig.SelectNodes(ToolXPath).Count,
				Is.EqualTo(ctoolNodes), "Duplicated a tool node.");
			Assert.That(m_testWindowConfig.SelectNodes(ClerkXPath).Count,
				Is.EqualTo(cclerkNodes), "Duplicated a clerk node.");
			Assert.That(m_testWindowConfig.SelectNodes(CommandXPath).Count,
				Is.EqualTo(ccommandNodes), "Duplicated a command node.");
			Assert.That(m_testWindowConfig.SelectNodes(ContextMenuXPath).Count,
				Is.EqualTo(ccontextNodes), "Duplicated a context menu node.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting a Custom list rebuilds only the window it was deleted in, so any other main
		/// window keeps a tool whose list is gone. Refreshing there must drop that tool rather
		/// than fail to resolve its guid.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FillListAreaList_ListDeletedInAnotherWindow_DropsItsTool()
		{
			UseTestWindowConfiguration();
			var doomedList = CreateUnownedList("Doomed List");
			CreateUnownedList("Surviving List");
			var afterFirstRefresh = RefreshListsToolsDisplay();
			Assert.That(afterFirstRefresh, Does.Contain("DoomedListEdit"));

			doomedList.Delete();

			// SUT
			var afterDeletion = RefreshListsToolsDisplay();

			Assert.That(afterDeletion, Does.Not.Contain("DoomedListEdit"));
			Assert.That(afterDeletion, Does.Contain("SurvivingListEdit"), "Dropped the wrong tool.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Publishing EventConstants.GetToolForList for a list that is wired into the window
		/// configuration must return that tool's name via the second element of the payload array.
		/// This exercises the full Pub/Sub path that LinkListener.FollowActiveLink relies on:
		/// publish through the Publisher, the AreaListener subscriber handles it synchronously,
		/// and the result comes back in parameters[1]. (LT-21515)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetToolForList_KnownList_ReturnsConfiguredToolName()
		{
			// Setup: a list wired to a configured tool via its clerk's recordList.
			var ws = WritingSystemServices.kwsAnals;
			var list = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("Some List", ws);
			var windowConfig = SetupWindowConfigWithListTool("someListClerk", "myConfiguredListEdit", list.Guid.ToString());
			m_propertyTable.SetProperty("WindowConfiguration", windowConfig, true);
			m_propertyTable.SetPropertyPersistence("WindowConfiguration", false);

			var parameters = new object[2];
			parameters[0] = list;

			// SUT: publish exactly as LinkListener.FollowActiveLink does.
			Publisher.Publish(new PublisherParameterObject(EventConstants.GetToolForList, parameters, null));

			// Verify: the configured tool name was returned via the payload.
			Assert.That(parameters[1], Is.EqualTo("myConfiguredListEdit"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Publishing EventConstants.GetToolForList for a list that is NOT in the configuration
		/// must fall back to the generated custom-list tool name (the list name with whitespace
		/// removed, plus "Edit"), returned via parameters[1]. (LT-21515)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetToolForList_UnknownList_ReturnsCustomToolName()
		{
			// Setup: a window configuration whose 'tools' section has no matching tool.
			m_propertyTable.SetProperty("WindowConfiguration", SetupMinimalWindowConfig(), true);
			m_propertyTable.SetPropertyPersistence("WindowConfiguration", false);

			var ws = WritingSystemServices.kwsAnals;
			var customList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("My Custom List", ws);

			var parameters = new object[2];
			parameters[0] = customList;

			// SUT
			Publisher.Publish(new PublisherParameterObject(EventConstants.GetToolForList, parameters, null));

			// Verify: whitespace stripped from the name, with "Edit" appended.
			Assert.That(parameters[1], Is.EqualTo("MyCustomListEdit"));
		}
	}
}
