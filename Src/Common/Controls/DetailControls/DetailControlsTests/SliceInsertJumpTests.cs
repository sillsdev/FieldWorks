// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// An insert that leaves no slice for the new object publishes JumpToRecord for it, to the
	/// slice's own window only.
	/// </summary>
	[TestFixture]
	public class SliceInsertJumpTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;
		private StubWindow m_otherWindow;
		private List<object> m_jumps;
		private List<object> m_otherWindowJumps;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		public override void TestSetup()
		{
			base.TestSetup();

			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("form", Cache.DefaultVernWs);

			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, false);
			m_propertyTable.SetPropertyDispose("cache", false);
			var window = new StubWindow();
			m_propertyTable.SetProperty("window", window, false);
			m_otherWindow = new StubWindow();
			m_jumps = new List<object>();
			m_otherWindowJumps = new List<object>();
			Subscriber.Subscribe(EventConstants.JumpToRecord, RecordJump, window);
			Subscriber.Subscribe(EventConstants.JumpToRecord, RecordOtherWindowJump, m_otherWindow);

			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			// This layout shows the citation form only, so an etymology gets no slice.
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// The insert runs its own unit of work.
			m_actionHandler.EndUndoTask();
		}

		public override void TestTearDown()
		{
			Subscriber.Unsubscribe(EventConstants.JumpToRecord, RecordJump);
			Subscriber.Unsubscribe(EventConstants.JumpToRecord, RecordOtherWindowJump);
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
				m_parent = null;
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

			base.TestTearDown();
		}

		private void RecordJump(object target) => m_jumps.Add(target);

		private void RecordOtherWindowJump(object target) => m_otherWindowJumps.Add(target);

		[Test]
		public void InsertWithNoSliceForTheNewObject_JumpsToIt()
		{
			var slice = (Slice)m_dtree.Controls[0];

			slice.HandleInsertCommand("Etymology", "LexEtymology", "LexEntry", null,
				out var newObject);

			Assert.That(newObject, Is.InstanceOf<ILexEtymology>(), "the insert happened");
			Assert.That(m_jumps, Is.EqualTo(new object[] { newObject.Hvo }),
				"one jump, to the new object, by the time the insert returns");
			Assert.That(m_otherWindowJumps, Is.Empty, "another window hears nothing");
			Assert.That(m_mediator.JobItems, Is.Zero, "nothing is queued on the Mediator");
		}

		private sealed class StubWindow : IxWindow
		{
			public Mediator Mediator => null;
			public PropertyTable PropTable => null;
			public void SuspendIdleProcessing() { }
			public void ResumeIdleProcessing() { }
			public void SuspendWindowSizePersistence() { }
			public void ResumeWindowSizePersistence() { }
		}
	}
}
