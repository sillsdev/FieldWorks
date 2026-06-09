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
			const string clerkXPath = "//item[@value='lists']/parameters/clerks/clerk";
			const string commandXPath = "//commands/command";
			const string contextXPath = "//contextMenus/menu";
			const string toolXPath = "//item[@value='lists']/parameters/tools/tool";
			//var fakeUIDisplay = new UIListDisplayProperties(new XCore.List(node.SelectSingleNode(toolXPath), null));
			//var cdispNodesBefore = fakeUIDisplay.List.Count;
			var contextNodes = node.SelectNodes(contextXPath);
			var ccontextNodesBefore = contextNodes == null ? 0 : contextNodes.Count;
			var commandNodes = node.SelectNodes(commandXPath);
			var ccommandNodesBefore = commandNodes == null ? 0 : commandNodes.Count;
			var clerkNodes = node.SelectNodes(clerkXPath);
			var cclerkNodesBefore = clerkNodes == null ? 0 : clerkNodes.Count;
			var toolNodes = node.SelectNodes(toolXPath);
			var ctoolNodesBefore = toolNodes == null ? 0 : toolNodes.Count;

			const string listName = "testList1";
			var ws = WritingSystemServices.kwsAnals;
			var testList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listName, ws);

			// SUT
			m_listener.AddListsToWindowConfig(new List<ICmPossibilityList> { testList }, node);

			// Verify
			// The above routine no longer handles display nodes
			//Assert.That(fakeUIDisplay.List.Count, Is.EqualTo(cdispNodesBefore + 1), "Didn't add a display node.");
			var ctoolNodesAfter = node.SelectNodes(toolXPath).Count;
			Assert.That(ctoolNodesAfter, Is.EqualTo(ctoolNodesBefore + 1), "Didn't add a tool node.");
			var cclerkNodesAfter = node.SelectNodes(clerkXPath).Count;
			Assert.That(cclerkNodesAfter, Is.EqualTo(cclerkNodesBefore + 1), "Didn't add a clerk node.");
			var ccommandNodesAfter = node.SelectNodes(commandXPath).Count;
			Assert.That(ccommandNodesAfter, Is.EqualTo(ccommandNodesBefore + 1), "Didn't add a command node.");
			var ccontextNodesAfter = node.SelectNodes(contextXPath).Count;
			Assert.That(ccontextNodesAfter, Is.EqualTo(ccontextNodesBefore + 1), "Didn't add a context menu node.");
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
			Publisher.Publish(new PublisherParameterObject(EventConstants.GetToolForList, parameters));

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
			Publisher.Publish(new PublisherParameterObject(EventConstants.GetToolForList, parameters));

			// Verify: whitespace stripped from the name, with "Edit" appended.
			Assert.That(parameters[1], Is.EqualTo("MyCustomListEdit"));
		}
	}
}
