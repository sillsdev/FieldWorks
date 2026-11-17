// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// What a clerk tells its listeners about a JumpToRecord that reaches it through Pub/Sub.
	/// </summary>
	[TestFixture]
	public class RecordClerkPublishedJumpTests : AllReversalEntriesRecordListTestBase
	{
		private PublishedJumpClerkForTests m_clerk;

		/// <summary>The targets the clerk announced, in call order.</summary>
		private readonly List<int> m_announced = new List<int>();

		/// <summary>The clerk's current record at the last announcement.</summary>
		private int m_currentWhenAnnounced;

		[OneTimeSetUp]
		public override void FixtureInit()
		{
			base.FixtureInit();
			const string configuration = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='AllReversalEntries'>
						<dynamicloaderinfo assemblyPath='xWorksTests.dll' class='SIL.FieldWorks.XWorks.PublishedJumpClerkForTests'/>
						<recordList owner='ReversalIndex' property='AllEntries'>
							<dynamicloaderinfo assemblyPath='xWorksTests.dll' class='SIL.FieldWorks.XWorks.AllReversalEntriesRecordListForTests'/>
						</recordList>
					</clerk>
				</clerks>
				<tools>
					<tool label='Reversal Indexes' value='reversalToolEditComplete' icon='SideBySideView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='AllReversalEntries' altTitleId='ReversalIndexEntry-Plural' persistContext='Reversal'
								backColor='White' layout='' layoutProperty='ReversalIndexPublicationLayout' layoutSuffix='Preview' editable='false'
								configureObjectName='ReversalIndex'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(configuration);
			var clerkNode = doc.SelectSingleNode("//tools/tool[@label='Reversal Indexes']//parameters[@area='lexicon']");
			m_clerk = (PublishedJumpClerkForTests)RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false, false);
			m_clerk.Init(m_mediator, m_propertyTable, clerkNode);
			m_clerk.PublishedJumpStarting += hvo =>
			{
				m_announced.Add(hvo);
				m_currentWhenAnnounced = m_clerk.CurrentObjectHvo;
			};
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			AddSomeReversalEntries(2);
			m_propertyTable.SetProperty("ActiveClerk", m_clerk, false);
			m_clerk.ActivateUI(false);
			m_clerk.List.SetSuppressingLoadList(false);
			m_clerk.List.ReloadList();
			m_clerk.List.CurrentIndex = 0;
			m_announced.Clear();
			m_currentWhenAnnounced = 0;
		}

		protected override void TearDown()
		{
			base.TearDown();
			m_clerk?.Dispose();
			m_clerk = null;
		}

		[Test]
		public void PublishedJump_ToTheActiveClerk_IsAnnouncedBeforeTheClerkMoves()
		{
			var start = m_clerk.CurrentObjectHvo;
			var target = RecordOtherThanCurrent();

			// SUT
			m_clerk.PublishedJump(target);

			Assert.That(m_announced, Is.EqualTo(new[] { target }));
			Assert.That(m_currentWhenAnnounced, Is.EqualTo(start),
				"listeners must hear of the jump before the current record changes");
			Assert.That(m_clerk.CurrentObjectHvo, Is.EqualTo(target));
		}

		[Test]
		public void PublishedJump_ToAClerkThatIsNotActive_IsIgnored()
		{
			var start = m_clerk.CurrentObjectHvo;
			m_propertyTable.RemoveProperty("ActiveClerk");

			// SUT
			m_clerk.PublishedJump(RecordOtherThanCurrent());

			Assert.That(m_announced, Is.Empty);
			Assert.That(m_clerk.CurrentObjectHvo, Is.EqualTo(start));
		}

		[Test]
		public void OnJumpToRecord_IsNotAnnounced()
		{
			var target = RecordOtherThanCurrent();

			// SUT
			m_clerk.OnJumpToRecord(target);

			Assert.That(m_announced, Is.Empty, "a Mediator jump reaches each listener on its own");
			Assert.That(m_clerk.CurrentObjectHvo, Is.EqualTo(target));
		}

		private int RecordOtherThanCurrent()
		{
			return m_revIndex.AllEntries.Select(entry => entry.Hvo).First(hvo => hvo != m_clerk.CurrentObjectHvo);
		}
	}

	// ReSharper disable once UnusedMember.Global (created by reflection)
	public class PublishedJumpClerkForTests : ReversalEntryClerkForListTests
	{
		internal RecordList List => m_list;

		/// <summary>Hands the clerk a jump the way its JumpToRecord subscription does.</summary>
		internal void PublishedJump(int hvo) => JumpToRecordViaPublisher(hvo);
	}
}
