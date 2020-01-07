// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FieldWorks.TestUtilities;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	[TestFixture]
	public class MakeBestTests : SelectionHelperTestsBase
	{
		/// <summary>
		/// Show the form
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			ShowForm(Lng.English | Lng.Empty, DisplayType.kAll);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void ExistingIPPos()
		{
			// Set selection somewhere (A == E); should set to same position
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 2, 2, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(2, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);

		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void AfterEndOfLine()
		{
			// Set selection after end of line (A == E); should set to last character
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			var nExpected = SimpleBasicView.kSecondParaEng.Length;
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(nExpected, newSel.IchAnchor);
			Assert.AreEqual(nExpected, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void EmptyLine()
		{
			// Set selection somewhere on an empty line (A == E); should set to position 0
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 2, 0, 0, 0, 5, 5, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(0, newSel.IchAnchor);
			Assert.AreEqual(0, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void ExistingRange()
		{
			// Set selection somewhere (A < E); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 5, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(2, newSel.IchAnchor);
			Assert.AreEqual(5, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void EndpointAfterEndOfLine()
		{
			// Set endpoint after end of line (A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, 99, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(3, newSel.IchAnchor);
			Assert.AreEqual(SimpleBasicView.kSecondParaEng.Length, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void ExistingEndBeforeAnchor()
		{
			// Set selection somewhere (E < A); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 0, 1, 0, 0, 5, 4, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(5, newSel.IchAnchor);
			Assert.AreEqual(4, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MakeBest_AnchorAfterEndOfLine()
		{
			// Set anchor after end of line (E < A); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 88, 2, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(SimpleBasicView.kSecondParaEng.Length, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_ExistingAnchorBeforeEnd()
		{
			// Both A and E exist (A < E); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 2, 2, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 2, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_NonExistingEnd()
		{
			// A exists, E past end of line ( A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 99, 99, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_NonExistingAnchor()
		{
			// A doesn't exist, E does ( A < E); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 77, 77, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_EndBeforeAnchor()
		{
			// Both A and E exist (E < A); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 2, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 2, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_EndBeforeNonExistingAnchor()
		{
			// A doesn't exist, E does ( E < A); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0,
				true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_AnchorAfterNonExistingEnd()
		{
			// A does exist, E doesn't ( E < A ); should set E to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, 77, 77, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void EndpointAtEndOfLine()
		{
			// Set endpoint at end of line (A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, SimpleBasicView.kSecondParaEng.Length, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(3, newSel.IchAnchor);
			Assert.AreEqual(SimpleBasicView.kSecondParaEng.Length, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MakeBest_AnchorAtEndOfLine()
		{
			// Set anchor at end of line (E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, 2, true);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(SimpleBasicView.kSecondParaEng.Length, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_EndAtEOT()
		{
			// A exists, E at absolute end ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length,
				0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_AnchorAtEOT()
		{
			// A at absolute end of text, E exists ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length,
				0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_EndBeforeAnchorAtEOT()
		{
			// A at absolute end of text, E exists ( E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0,
				true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		[Test]
		public void MultiSection_AnchorAfterEndAtEOT()
		{
			// A exists, E at absolute end of text ( E < A )
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, false);
			var vwsel = m_SelectionHelper.MakeBest(true);

			var newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelLimitType.Anchor, newSel, 0, 0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, newSel, 0, 0, SimpleBasicView.kSecondParaEng.Length, 0, false,
				2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}
	}
}