// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AreaListenerTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.LexText;
using XCore;


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
			m_listener.Init(m_mediator, null);
		}

		[TearDown]
		public void TearDown()
		{
			if (m_listener != null)
				m_listener.Dispose();
			m_listener = null;
			if (m_mediator != null)
				m_mediator.Dispose();
			m_mediator = null;
		}

		#region Helper Methods

		private void SetupTestMediator()
		{
			m_mediator = new Mediator();
			m_mediator.PropertyTable.SetProperty("cache", Cache);
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

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method AddListToXmlConfig.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
			//Assert.AreEqual(cdispNodesBefore + 1, fakeUIDisplay.List.Count, "Didn't add a display node.");
			var ctoolNodesAfter = node.SelectNodes(toolXPath).Count;
			Assert.AreEqual(ctoolNodesBefore + 1, ctoolNodesAfter, "Didn't add a tool node.");
			var cclerkNodesAfter = node.SelectNodes(clerkXPath).Count;
			Assert.AreEqual(cclerkNodesBefore + 1, cclerkNodesAfter, "Didn't add a clerk node.");
			var ccommandNodesAfter = node.SelectNodes(commandXPath).Count;
			Assert.AreEqual(ccommandNodesBefore + 1, ccommandNodesAfter, "Didn't add a command node.");
			var ccontextNodesAfter = node.SelectNodes(contextXPath).Count;
			Assert.AreEqual(ccontextNodesBefore + 1, ccontextNodesAfter, "Didn't add a context menu node.");
		}
	}
}
