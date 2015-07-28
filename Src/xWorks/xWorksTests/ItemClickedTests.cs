// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Test the itemClicked method in XmlDocView.
	/// There's really nothing XmlDocView-specific to this so we test with a simple root site for proof of concept.
	/// There's still a good deal of integration test to this (it uses chunks of SimpleRootSite and FDO and the Views code)
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
			var lexRefType = MakeLexRefType("TestRelation");
			var lexRef = MakeLexReference(lexRefType, kick.SensesOS[0]);
			lexRef.TargetsRS.Add(boot.SensesOS[0]);

			// Make the view to test.
			using (var view = new TestRootSite(Cache, kick.Hvo))
			{
				view.Width = 1000; // keep everything on one line
				var dummy = view.Handle; // triggers MakeRoot as handle is created
				var dummy2 = view.RootBox.Width; // triggers constructing boxes.
				view.PerformLayout();

				int widthKick = view.Vc.ItemWidths[kick.LexemeFormOA.Hvo];
				int widthKickSense = view.Vc.ItemWidths[kick.SensesOS[0].Hvo];

				var mockItems = new MockSortItemProvider();
				mockItems.Items.AddRange(new[] { kick.Hvo, boot.Hvo });
				var nullAdjuster = new NullTargetAdjuster();

				// click on the lexeme form: should find nothing, it's a root object.
				Point where = new Point(1, 1);
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
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			AddSense(entry, gloss);
			return entry;
		}

		private ILexSense AddSense(ILexEntry entry, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss,
				Cache.DefaultAnalWs);
			return sense;
		}

		private ILexReference MakeLexReference(ILexRefType owner, ILexSense firstTarget)
		{
			ILexReference result = null;
			result = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			owner.MembersOC.Add(result);
			result.TargetsRS.Add(firstTarget);
			return result;
		}
		private ILexRefType MakeLexRefType(string name)
		{
			ILexRefType result = null;
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			result = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(result);
			result.MappingType = (int) LexRefTypeTags.MappingTypes.kmtSenseSequence;
			result.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(name, Cache.DefaultAnalWs);
			return result;
		}
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache is a reference")]
	class TestRootSite : SimpleRootSite
	{
		private FdoCache m_cache;
		private int m_hvoRoot;

		public TestRootSite(FdoCache cache, int root)
		{
			m_cache = cache;
			m_hvoRoot = root;
		}

		public TestVc Vc { get; set; }

		public override void MakeRoot()
		{
			base.MakeRoot();

			Vc = new TestVc(m_cache);
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			m_rootb.SetRootObject(m_hvoRoot, Vc, 1, null);
		}
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache is a reference")]
	class TestVc: VwBaseVc
	{
		private FdoCache m_cache;

		public TestVc(FdoCache cache)
		{
			m_cache = cache;
		}

		public Dictionary<int, int> ItemWidths = new Dictionary<int, int>();
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them. Most
		/// subclasses should override.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// -----------------------------------------------------------------------------------
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
					vwenv.AddStringAltMember(MoFormTags.kflidForm, m_cache.DefaultVernWs, this);
					NoteItemWidth(vwenv, hvo, MoFormTags.kflidForm, m_cache.DefaultVernWs);
					break;
				case 3: // LexSense
					vwenv.AddStringAltMember(LexSenseTags.kflidGloss, m_cache.DefaultAnalWs, this);
					NoteItemWidth(vwenv, hvo, LexSenseTags.kflidGloss, m_cache.DefaultAnalWs);
					int flid = m_cache.MetaDataCacheAccessor.GetFieldId("LexSense", "LexSenseReferences", false);
					vwenv.AddObjVecItems(flid, this, 4);
					break;
				case 4: // LexReference
					vwenv.AddObjVecItems(LexReferenceTags.kflidTargets, this, 5);
					break;
				case 5: // target of lex reference, which in our test data is made to be a sense
					vwenv.AddStringAltMember(LexSenseTags.kflidGloss, m_cache.DefaultAnalWs, this);
					NoteItemWidth(vwenv, hvo, LexSenseTags.kflidGloss, m_cache.DefaultAnalWs);
					break;
			}
		}

		private void NoteItemWidth(IVwEnv vwenv, int hvo, int flid, int ws)
		{
			var tss = vwenv.DataAccess.get_MultiStringAlt(hvo, flid, ws);
			int mpx, mpy;
			vwenv.get_StringWidth(tss, null, out mpx, out mpy);
			int dx = mpx*96/72000; // to pixels
			ItemWidths[hvo] = dx;
		}
	}
	class MockSortItemProvider : ISortItemProvider
	{
		public List<int> Items = new List<int>();
		#region ISortItemProvider Members

		public int AppendItemsFor(int hvo)
		{
			throw new NotImplementedException();
		}

		public int IndexOf(int hvo)
		{
			return Items.IndexOf(hvo);
		}

		public int ListItemsClass
		{
			get { throw new NotImplementedException(); }
		}

		public void RemoveItemsFor(int hvo)
		{
			throw new NotImplementedException();
		}

		public SIL.FieldWorks.Filters.IManyOnePathSortItem SortItemAt(int index)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
