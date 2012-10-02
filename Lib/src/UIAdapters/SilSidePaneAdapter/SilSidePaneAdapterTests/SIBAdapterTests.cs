// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
