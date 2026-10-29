// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using NUnit.Framework;
using XCore;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// How <see cref="RootSite"/> stores the style it shows in the Styles combo.
	/// </summary>
	[TestFixture]
	public class BestSelectionStyleTests : RootsiteBasicViewTestsBase
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		protected override DummyBasicView CreateDummyBasicView()
		{
			return new StyleView(m_hvoRoot, m_flidContainingTexts);
		}

		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_mediator.AddColleague(new ListeningColleague());
			m_propertyTable = new PropertyTable(m_mediator);
			m_basicView.Init(m_mediator, m_propertyTable, null);
		}

		public override void TestTearDown()
		{
			base.TestTearDown();
			m_propertyTable.Dispose();
			m_mediator.Dispose();
		}

		[Test]
		public void BestSelectionStyle_StyleDiffers_StoresItWithoutQueueingAChange()
		{
			m_propertyTable.SetProperty("BestStyleName", "NoSuchStyle", false);
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);
			var queued = m_mediator.JobItems;

			var style = ((StyleView)m_basicView).SelectionStyle;

			// The Styles combo shows the getter's value in the toolbar pass that called it.
			Assert.That(m_mediator.JobItems, Is.EqualTo(queued));
			Assert.That(style, Is.Not.EqualTo("NoSuchStyle"));
			Assert.That(m_propertyTable.GetStringProperty("BestStyleName", null), Is.EqualTo(style));
		}

		private sealed class StyleView : DummyBasicView
		{
			internal StyleView(int hvoRoot, int flid) : base(hvoRoot, flid)
			{
			}

			internal string SelectionStyle => BestSelectionStyle;
		}

		private sealed class ListeningColleague : IxCoreColleague
		{
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
		}
	}
}
