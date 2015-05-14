// --------------------------------------------------------------------------------------------
// Copyright (c) 2010-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File:
// Responsibility:
// --------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;

using SIL.FieldWorks.Common.UIAdapters;
using XCore;

namespace SilSidePaneAdapterTests
{
	[TestFixture]
	public class SIBAdapterTests
	{
		private ISIBInterface m_sibAdapter;
		private Control m_parent = null;
		private Panel m_infoBarContainer = null;
		private Mediator m_mediator;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_parent = new Panel();

			m_sibAdapter = new SIBAdapter();
			Debug.Assert(m_sibAdapter != null);
			m_sibAdapter.Initialize(m_parent, m_infoBarContainer, m_mediator);
		}

		[Test]
		public void AddTab_basic()
		{
			// Add a tab
			SBTabProperties tabProps = new SBTabProperties();
			tabProps.Name = "tabname";
			tabProps.Text = "tabtext";
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = "SideBarConfigure";
			tabProps.ConfigureMenuText = "cfgText";
			tabProps.InfoBarButtonToolTipFormat = "fmttooltip";
			m_sibAdapter.AddTab(tabProps);
		}
	}
}
