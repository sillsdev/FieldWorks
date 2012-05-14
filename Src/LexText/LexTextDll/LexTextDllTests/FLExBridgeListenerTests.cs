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
// File: FLExBridgeListenerTests.cs
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
	/// Contains tests of FLExBridgeListener.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_listener and m_mediator get disposed in TearDown()")]
	public class FLExBridgeListenerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member Data

		/// <summary>
		/// For testing.
		/// </summary>
		private TestFLExBridgeListenerDb4o m_listener;

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

			// Setup test FLExBridgeListener
			m_listener = new TestFLExBridgeListenerDb4o(); // Always returns true to IsDb4oProject
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="CommandSet gets added to Mediator and disposed there")]
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
		/// Tests the dialog that pops up when OnFLExBridge is called on a Db4o project.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Ignore("Only test manually")]
		public void TestSendReceiveDb4oDialog()
		{
			// Setup

			// SUT
			var returnValue = m_listener.OnFLExBridge(null);

			// Verify
			Assert.IsTrue(returnValue, "Should return true in any case.");
		}
	}
}
