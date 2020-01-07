// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon.Tools.Edit;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Areas.Lexicon
{
	/// <summary>
	/// Unit tests for <see cref="DummyReversalIndexEntrySlice"/> which uses <see cref="DummyReversalIndexEntrySliceView"/>
	/// </summary>
	[TestFixture]
	public sealed class ReversalEntryViewTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_lexEntry;
		private DummyReversalIndexEntrySlice m_reversalSlice;
		private IReversalIndexRepository m_revIndexRepo;
		private IReversalIndexEntryRepository m_revIndexEntryRepo;

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			m_revIndexEntryRepo = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>();
		}

		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();
			m_reversalSlice.DummyControl.Dispose();
			m_reversalSlice.Dispose();
			m_reversalSlice = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => m_lexEntry?.Delete());
			m_lexEntry = null;
		}

		/// <summary>
		/// Tests that a reversal index entry in the dummy cache gets converted into a real one when the focus on the view control is lost.
		/// </summary>
		[Test]
		public void DummyReversalCreatedOnFocusLost()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_lexEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_lexEntry.SensesOS.Add(sense);
			});
			var engWsId = Cache.DefaultAnalWs;
			m_reversalSlice = new DummyReversalIndexEntrySlice(m_lexEntry.SensesOS[0]);
			m_reversalSlice.Cache = Cache;
			// Creating the reversal index before the slice is fully initialized ensures the reversal index gets loaded into the dummy cache
			var ri = m_revIndexRepo.FindOrCreateIndexForWs(engWsId);
			m_reversalSlice.FinishInit();
			var reversalView = m_reversalSlice.DummyControl;
			var indexFirstEntry = 0;
			reversalView.CacheNewDummyEntry(ri.Hvo, engWsId); // Create an additional dummy reversal entry before we edit one
			reversalView.EditRevIndexEntryInCache(ri.Hvo, indexFirstEntry, engWsId, TsStringUtils.MakeString("first", engWsId));
			// The dummy cache will have two dummy reversal index entries, but non exists in the real data yet.
			// The reversal index entry control must maintain a dummy entry at the end to allow a place to click to add new entries.
			Assert.AreEqual(0, m_revIndexEntryRepo.Count);
			Assert.AreEqual(2, reversalView.GetIndexSize(ri.Hvo)); // The second dummy entry will remain a dummy
			reversalView.KillFocus(new Control());
			Assert.AreEqual(1, m_revIndexEntryRepo.Count);
			Assert.AreEqual(2, reversalView.GetIndexSize(ri.Hvo));
			var rie = m_revIndexEntryRepo.AllInstances().First();
			Assert.AreEqual("first", rie.ShortName);
			Assert.AreEqual(1, m_lexEntry.SensesOS[0].ReferringReversalIndexEntries.Count());
			Assert.True(m_lexEntry.SensesOS[0].ReferringReversalIndexEntries.Contains(rie));
		}

		/// <summary>
		/// A dummy class for <see cref="ReversalIndexEntrySlice"/> for testing purposes.
		/// </summary>
		private sealed class DummyReversalIndexEntrySlice : ReversalIndexEntrySlice
		{
			public DummyReversalIndexEntrySlice(ICmObject obj) : base(obj)
			{
			}

			/// <summary>
			/// Allows access to the view.
			/// </summary>
			public DummyReversalIndexEntrySliceView DummyControl { get; private set; }

			public override void FinishInit()
			{
				var ctrl = new DummyReversalIndexEntrySliceView(MyCmObject.Hvo)
				{
					Cache = Cache
				};
				DummyControl = ctrl;
				if (ctrl.RootBox == null)
				{
					ctrl.MakeRoot();
				}
			}
		}

		/// <summary>
		/// A dummy class for <see cref="ReversalIndexEntrySliceView"/> for testing purposes.
		/// </summary>
		private sealed class DummyReversalIndexEntrySliceView : ReversalIndexEntrySliceView
		{
			public DummyReversalIndexEntrySliceView(int hvo) : base(hvo)
			{
			}

			/// <summary>
			/// Gets the number of reversal index entries for a given reversal index.
			/// </summary>
			public int GetIndexSize(int hvoIndex)
			{
				return m_sdaRev.get_VecSize(hvoIndex, kFlidEntries);
			}

			/// <summary>
			/// Exposes the OnKillFocus and OnLostFocus methods for testing purposes.
			/// </summary>
			public void KillFocus(Control newWindow)
			{
				OnKillFocus(newWindow, false);
				OnLostFocus(new EventArgs());
			}

			/// <summary>
			/// Helper method to create a new empty dummy reversal index entry in the dummy cache.
			/// </summary>
			public void CacheNewDummyEntry(int hvoIndex, int ws)
			{
				var count = m_sdaRev.get_VecSize(hvoIndex, kFlidEntries);
				m_sdaRev.CacheReplace(hvoIndex, kFlidEntries, count, count, new int[] { m_dummyId }, 1);
				m_sdaRev.CacheStringAlt(m_dummyId--, ReversalIndexEntryTags.kflidReversalForm, ws, TsStringUtils.EmptyString(ws));
			}

			/// <summary>
			/// Helper method to edit a TsString in the dummy cache at a given position.
			/// </summary>
			public void EditRevIndexEntryInCache(int hvoIndex, int index, int ws, ITsString tss)
			{
				var hvoEntry = m_sdaRev.get_VecItem(hvoIndex, kFlidEntries, index);
				m_sdaRev.SetMultiStringAlt(hvoEntry, ReversalIndexEntryTags.kflidReversalForm, ws, tss);
			}
		}
	}
}