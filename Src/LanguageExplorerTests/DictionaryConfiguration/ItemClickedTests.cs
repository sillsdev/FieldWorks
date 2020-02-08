// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using LanguageExplorer;
using LanguageExplorer.Areas;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	/// <summary>
	/// Test the itemClicked method in XmlDocView.
	/// There's really nothing XmlDocView-specific to this so we test with a simple root site for proof of concept.
	/// There's still a good deal of integration test to this (it uses chunks of SimpleRootSite and LCM and the Views code)
	/// but at least it's not using all of XmlDocView and the XML configuration.
	/// </summary>
	[TestFixture]
	public class ItemClickedTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void ItemClicked()
		{
			// test data
			var kick = MakeEntry("kick", "strike with foot");
			var boot = MakeEntry("boot", "strike with boot");
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
			{
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var lexRefType = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lexRefType);
			lexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtSenseSequence;
			lexRefType.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeString("TestRelation", Cache.DefaultAnalWs);
			var lexRef = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			lexRefType.MembersOC.Add(lexRef);
			lexRef.TargetsRS.Add(kick.SensesOS[0]);
			lexRef.TargetsRS.Add(boot.SensesOS[0]);
			// Make the view to test.
			using (var view = new TestRootSite(Cache, kick.Hvo))
			{
				view.Width = 1000; // keep everything on one line
				var dummy = view.Handle; // triggers MakeRoot as handle is created
				var dummy2 = view.RootBox.Width; // triggers constructing boxes.
				view.PerformLayout();
				var widthKick = view.Vc.ItemWidths[kick.LexemeFormOA.Hvo];
				var widthKickSense = view.Vc.ItemWidths[kick.SensesOS[0].Hvo];
				var mockItems = new MockSortItemProvider();
				mockItems.Items.AddRange(new[] { kick.Hvo, boot.Hvo });
				var nullAdjuster = new NullTargetAdjuster();
				// click on the lexeme form: should find nothing, it's a root object.
				var where = new Point(1, 1);
				var result = XmlDocView.SubitemClicked(where, LexEntryTags.kClassId, view, Cache, mockItems, nullAdjuster);
				Assert.That(result, Is.Null);
				// click on the gloss of kick: should find nothing, it's still part of the root.
				where = new Point(widthKick + 5, 1);
				result = XmlDocView.SubitemClicked(where, LexEntryTags.kClassId, view, Cache, mockItems, nullAdjuster);
				Assert.That(result, Is.Null);
				// click on the gloss, asking for a sense: should find nothing, no containing sense
				result = XmlDocView.SubitemClicked(where, LexSenseTags.kClassId, view, Cache, mockItems, nullAdjuster);
				Assert.That(result, Is.Null);
				// click on the synonym: should find the other entry.
				where = new Point(widthKick + widthKickSense * 2 + 5, 1);
				result = XmlDocView.SubitemClicked(where, LexEntryTags.kClassId, view, Cache, mockItems, nullAdjuster);
				Assert.That(result, Is.EqualTo(boot));
				// Should not return the item it otherwise would if it is not a possible target.
				mockItems.Items.Remove(boot.Hvo);
				result = XmlDocView.SubitemClicked(where, LexEntryTags.kClassId, view, Cache, mockItems, nullAdjuster);
				Assert.That(result, Is.Null);
				// MainEntryFromSubEntryTargetAdjuster should convert subentry to main entry
				// Make boot a subentry of bootRoot
				var bootRoot = MakeEntry("boo", "fragment of boot");
				var ler = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				boot.EntryRefsOS.Add(ler);
				ler.RefType = LexEntryRefTags.krtComplexForm;
				ler.PrimaryLexemesRS.Add(bootRoot);
				mockItems.Items.Add(boot.Hvo); // not rejected as item
				mockItems.Items.Add(bootRoot.Hvo); // has to be valid itself also
				var subentryAdjuster = new MainEntryFromSubEntryTargetAdjuster();
				result = XmlDocView.SubitemClicked(where, LexEntryTags.kClassId, view, Cache, mockItems, subentryAdjuster);
				Assert.That(result, Is.EqualTo(bootRoot));
			}
		}

		/// <summary>
		/// Copied from StringServicesTests (plus UOW); possibly best for each test set to have own utility functions?
		/// </summary>
		private ILexEntry MakeEntry(string lf, string gloss)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs);
			return entry;
		}

		private sealed class MockSortItemProvider : ISortItemProvider
		{
			internal readonly List<int> Items = new List<int>();

			#region ISortItemProvider Members

			int ISortItemProvider.AppendItemsFor(int hvo)
			{
				throw new NotSupportedException();
			}

			int ISortItemProvider.IndexOf(int hvo)
			{
				return Items.IndexOf(hvo);
			}

			int ISortItemProvider.ListItemsClass
			{
				get { throw new NotSupportedException(); }
			}

			void ISortItemProvider.RemoveItemsFor(int hvo)
			{
				throw new NotSupportedException();
			}

			IManyOnePathSortItem ISortItemProvider.SortItemAt(int index)
			{
				throw new NotSupportedException();
			}

			#endregion
		}

		private sealed class TestRootSite : SimpleRootSite
		{
			private LcmCache m_cache;
			private int _hvoRoot;

			internal TestRootSite(LcmCache cache, int root)
			{
				m_cache = cache;
				_hvoRoot = root;
			}

			public TestVc Vc { get; set; }

			public override void MakeRoot()
			{
				Vc = new TestVc(m_cache);
				WritingSystemFactory = m_cache.WritingSystemFactory;
				base.MakeRoot();
				RootBox.DataAccess = m_cache.DomainDataByFlid;
				RootBox.SetRootObject(_hvoRoot, Vc, 1, null);
			}
		}

		private sealed class TestVc : VwBaseVc
		{
			private readonly LcmCache _cache;

			internal TestVc(LcmCache cache)
			{
				_cache = cache;
			}

			internal readonly Dictionary<int, int> ItemWidths = new Dictionary<int, int>();

			/// <summary>
			/// This is the main interesting method of displaying objects and fragments of them. Most
			/// subclasses should override.
			/// </summary>
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case 1: // LexEntry
						vwenv.OpenParagraph();
						vwenv.AddObjProp(LexEntryTags.kflidLexemeForm, this, 2);
						vwenv.AddObjVecItems(LexEntryTags.kflidSenses, this, 3);
						vwenv.CloseParagraph();
						break;
					case 2: // MoForm
						vwenv.AddStringAltMember(MoFormTags.kflidForm, _cache.DefaultVernWs, this);
						NoteItemWidth(vwenv, hvo, MoFormTags.kflidForm, _cache.DefaultVernWs);
						break;
					case 3: // LexSense
						vwenv.AddStringAltMember(LexSenseTags.kflidGloss, _cache.DefaultAnalWs, this);
						NoteItemWidth(vwenv, hvo, LexSenseTags.kflidGloss, _cache.DefaultAnalWs);
						var flid = _cache.MetaDataCacheAccessor.GetFieldId("LexSense", "LexSenseReferences", false);
						vwenv.AddObjVecItems(flid, this, 4);
						break;
					case 4: // LexReference
						vwenv.AddObjVecItems(LexReferenceTags.kflidTargets, this, 5);
						break;
					case 5: // target of lex reference, which in our test data is made to be a sense
						vwenv.AddStringAltMember(LexSenseTags.kflidGloss, _cache.DefaultAnalWs, this);
						NoteItemWidth(vwenv, hvo, LexSenseTags.kflidGloss, _cache.DefaultAnalWs);
						break;
				}
			}

			private void NoteItemWidth(IVwEnv vwenv, int hvo, int flid, int ws)
			{
				var tss = vwenv.DataAccess.get_MultiStringAlt(hvo, flid, ws);
				int mpx, mpy;
				vwenv.get_StringWidth(tss, null, out mpx, out mpy);
				var dx = mpx * 96 / 72000; // to pixels
				ItemWidths[hvo] = dx;
			}
		}
	}
}
