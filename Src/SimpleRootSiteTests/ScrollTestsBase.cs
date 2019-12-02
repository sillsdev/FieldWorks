// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Base class for tests testing scroll changes
	/// </summary>
	public class ScrollTestsBase
	{
		internal IVwRootBox m_rootb;
		internal SimpleRootSite m_site;
		internal IVwSelection m_selection;
		/// <summary />
		private FlexComponentParameters _flexComponentParameters;

		/// <summary />
		[SetUp]
		public void Setup()
		{
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();

			m_site = new DummyRootSite();
			m_site.InitializeFlexComponent(_flexComponentParameters);

			var rootb = MockRepository.GenerateMock<IVwRootBox>();
			rootb.Expect(rb => rb.Height).Return(10000);
			rootb.Expect(rb => rb.Width).Return(m_site.ClientRectangle.X);
			rootb.Expect(rb => rb.IsPropChangedInProgress).Return(false);

			m_site.RootBox = rootb;

			m_selection = MockRepository.GenerateMock<IVwSelection>();
			m_selection.Expect(s => s.IsValid).Return(true);
			m_site.CreateControl();
			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
		}

		/// <summary />
		[TearDown]
		public void TearDown()
		{
			m_site.Dispose();
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
		}

		/// <summary>
		/// Set the location of the m_selection
		/// </summary>
		/// <param name="rcPrimary">m_selection rectangle (from top of view)</param>
		/// <param name="fEndBeforeAnchor"><c>true</c> if end is before anchor</param>
		/// <param name="scrollPos">The scroll position</param>
		/// <param name="fIsRange"><c>true</c> if it is a range m_selection</param>
		protected void SetLocation(Rect rcPrimary, bool fEndBeforeAnchor, Point scrollPos, bool fIsRange)
		{
			m_selection.Expect(s =>
			{
				Rect outRect;
				bool outJunk;
				s.Location(null, new Rect(), new Rect(), out rcPrimary, out outRect, out outJunk, out fEndBeforeAnchor);
			}).IgnoreArguments().OutRef(new Rect(rcPrimary.left - scrollPos.X, rcPrimary.top - scrollPos.Y, rcPrimary.right - scrollPos.X, rcPrimary.bottom - scrollPos.Y), new Rect(0, 0, 0, 0), false, fEndBeforeAnchor);
			m_selection.Expect(s => s.IsRange).Return(fIsRange);
			m_selection.Expect(s => s.SelType).Return(VwSelType.kstText);
			m_selection.Expect(s => s.EndBeforeAnchor).Return(fEndBeforeAnchor);
			m_site.ScrollPosition = scrollPos;
		}

		/// <summary>
		/// Returns <c>true</c> if the end point of the m_selection is inside of the client window
		/// </summary>
		protected bool IsInClientWindow(int endPos)
		{
			var fRange = m_selection.IsRange;
			var fEndBeforeAnchor = m_selection.EndBeforeAnchor;
			var yScrollPos = -m_site.ScrollPosition.Y;
			return endPos >= yScrollPos + (fRange && !fEndBeforeAnchor ? 2 : 1) * m_site.LineHeight && endPos <= yScrollPos + m_site.ClientHeight - (fRange && fEndBeforeAnchor ? 2 : 1) * m_site.LineHeight;
		}

		/// <summary />
		private sealed class DummyRootSite : SimpleRootSite
		{
			private Point m_position = new Point(0, 0);

			/// <summary />
			public new IVwRootBox RootBox
			{
				set
				{
					base.RootBox = value;
				}
			}

			/// <summary />
			public override Point ScrollPosition
			{
				set
				{
					m_position = new Point(-value.X, -value.Y);
				}
				get
				{
					return m_position;
				}
			}

			/// <summary>
			/// We don't do horizontal scrolling
			/// </summary>
			protected override bool DoAutoHScroll => false;

			/// <summary>
			/// Creates a DummyEditingHelper
			/// </summary>
			protected override EditingHelper CreateEditingHelper()
			{
				return new DummyEditingHelper();
			}
		}
	}
}