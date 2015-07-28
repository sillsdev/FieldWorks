// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests;
using SimpleRootSiteDataProviderTestsBase=SIL.FieldWorks.Common.RootSites.SimpleRootSiteDataProviderTestsBase;

namespace SIL.FieldWorks.Common.Widgets
{

	[TestFixture]
	public class UiaWidgetTests : SimpleRootSiteDataProviderTestsBase
	{

		[Test]
		public void FwListBox_Empty()
		{
			using (var site = new TestListBox(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
				site.WritingSystemCode = m_wsEng;
				site.ShowHighlight = false;

				using (new SimpleRootSiteDataProviderTestsHelper(site))
				{
					site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new ListBoxVc(site));
					site.ShowForm();
					var selections = CollectorEnvServices.CollectStringPropertySelectionPoints(site.RootBox);
					Assert.AreEqual(0, selections.Count());
				}
			}
		}

		[Test]
		[Ignore("fix asserts in views code")]
		public void FwListBox_OneItem()
		{
			using (var fwList = new TestFwList())
			{
				using (var site = new TestListBox(fwList.DataAccess))
				{
					site.StyleSheet = FixtureStyleSheet;
					site.WritingSystemFactory = m_wsManager;
					site.WritingSystemCode = m_wsEng;
					using (new SimpleRootSiteDataProviderTestsHelper(site))
					{
						site.MakeRoot(InnerFwListBox.kfragRoot, () => new ListBoxVc(site));
						using (var items = new FwListBox.ObjectCollection(fwList))
						{
							TsStringUtils.MakeTss("Item0", m_wsEng);
							site.ShowForm();
							var selections = CollectorEnvServices.CollectStringPropertySelectionPoints(site.RootBox);
							Assert.AreEqual(1, selections.Count());
						}
					}
				}
			}
		}
	}

	internal class TestFwList : IFwListBox, IDisposable
	{
		private VwCacheDa m_cacheDa = VwCacheDaClass.Create();

		public TestFwList()
		{
			DataAccess = m_cacheDa;
			DataAccess.WritingSystemFactory = new PalasoWritingSystemManager();
		}

		public ITsString TextOfItem(object item)
		{
			return item as ITsString;
		}

		public ISilDataAccess DataAccess { get; private set; }

		public int SelectedIndex { get; set; }

		public bool Updating
		{
			get { return false; }
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~TestFwList()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_cacheDa != null)
				{
					m_cacheDa.ClearAllData();
					if (Marshal.IsComObject(m_cacheDa))
						Marshal.ReleaseComObject(m_cacheDa);
				}
			}
			m_cacheDa = null;

			IsDisposed = true;
		}
		#endregion
	}

	internal class TestListBox : RootSiteDataProviderViewBase, IFwListBoxSite
	{
		public TestListBox(ISilDataAccess cache)
			: base(cache, InnerFwListBox.khvoRoot)
		{

		}

		#region IHighlightInfo Members

		public bool IsHighlighted(int index)
		{
			return false;
		}

		public bool ShowHighlight { get; set; }

		#endregion

		#region IWritingSystemAndStylesheet Members


		public int WritingSystemCode { get; set; }

		#endregion
	}
}
