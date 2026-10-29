// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Tests that the Baseline pane's ShowInvisibleSpaces and ClickInvisibleSpace handlers
	/// apply each other's effect directly instead of broadcasting the other property.
	/// </summary>
	[TestFixture]
	public class RawTextPaneTests : MemoryOnlyBackendProviderTestBase
	{
		private IStText m_stText;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private TestRawTextPane m_pane;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				Cache.ServiceLocator.GetInstance<ITextFactory>().Create().ContentsOA = m_stText;
				m_stText.AddNewTextPara(null).Contents =
					TsStringUtils.MakeString("xxxa xxxb xxxc.", Cache.DefaultVernWs);
			});
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_mediator.AddColleague(new ListeningColleague());
			m_propertyTable = new PropertyTable(m_mediator);
			// Reading a missing property stores and broadcasts its default
			m_propertyTable.SetProperty("ShowInvisibleSpaces", false, false);
			m_propertyTable.SetProperty("ClickInvisibleSpace", false, false);
			m_pane =new TestRawTextPane(Cache, m_mediator, m_propertyTable, m_stText.Hvo);
		}

		[TearDown]
		public override void TestTearDown()
		{
			m_pane.Dispose();
			m_propertyTable.Dispose();
			m_mediator.Dispose();
			base.TestTearDown();
		}

		[Test]
		public void ClickInvisibleSpaceOn_SpacesHidden_ShowsSpacesWithoutBroadcast()
		{
			// SUT
			ChooseMenuItem("ClickInvisibleSpace", true);

			Assert.That(m_propertyTable.GetBoolProperty("ShowInvisibleSpaces", false), Is.True);
			Assert.That(m_pane.SpacesShown, Is.True, "the pane must show the spaces itself");
			Assert.That(m_mediator.JobItems, Is.Zero, "ShowInvisibleSpaces must change without a broadcast");
		}

		[Test]
		public void ShowInvisibleSpacesOff_ClickInsertsOn_EndsClickInsertsWithoutBroadcast()
		{
			ChooseMenuItem("ShowInvisibleSpaces", true);
			ChooseMenuItem("ClickInvisibleSpace", true);

			// SUT
			ChooseMenuItem("ShowInvisibleSpaces", false);

			Assert.That(m_propertyTable.GetBoolProperty("ClickInvisibleSpace", true), Is.False);
			Assert.That(m_pane.SpacesShown, Is.False);
			Assert.That(m_mediator.JobItems, Is.Zero, "ClickInvisibleSpace must change without a broadcast");

			// The pane must also have recorded that click-insert is off, or turning it on
			// again would be taken for no change and leave the spaces hidden.
			ChooseMenuItem("ClickInvisibleSpace", true);
			Assert.That(m_propertyTable.GetBoolProperty("ShowInvisibleSpaces", false), Is.True);
			Assert.That(m_pane.SpacesShown, Is.True);
		}

		/// <summary>
		/// Sets the property as the menu item's click does and delivers the broadcast to the
		/// pane, so a test only counts the broadcasts the pane makes itself.
		/// </summary>
		private void ChooseMenuItem(string property, bool value)
		{
			m_propertyTable.SetProperty(property, value, false);
			m_pane.OnPropertyChanged(property);
		}

		/// <summary>
		/// A colleague registered so the Mediator queues broadcasts: with no colleague it
		/// discards them before queueing, and the job count would prove nothing.
		/// </summary>
		internal sealed class ListeningColleague : IxCoreColleague
		{
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
		}

		/// <summary>
		/// A laid-out pane on the given text with an insertion point, so a redraw can keep
		/// the selection.
		/// </summary>
		private sealed class TestRawTextPane : RawTextPane
		{
			internal TestRawTextPane(LcmCache cache, Mediator mediator, PropertyTable propertyTable, int hvoText)
			{
				Cache = cache;
				m_mediator = mediator;
				m_propertyTable = propertyTable;
				Width = 300;
				Height = 300;
				SetRoot(hvoText);
				OnLayout(new LayoutEventArgs(this, "Bounds"));
				RootBox.MakeSimpleSel(true, true, false, true);
			}

			internal bool SpacesShown => ((ShowSpaceDecorator)RootBox.DataAccess).ShowSpaces;
		}
	}
}
