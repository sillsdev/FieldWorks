// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwListBoxTests.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FwListBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwListBoxTests : BaseTest
	{
		#region Data Members
		TestFwStylesheet m_stylesheet;
		WritingSystemManager m_wsManager;
		int m_hvoEnglishWs;
		#endregion

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_wsManager = new WritingSystemManager();

			// setup English ws.
			WritingSystem enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
		}

		#region ObjectCollection Collection tests.

		[Test]
		public void Add_EmptyObjectCollection_CollectionContainsSingleElement()
		{

			using (var listBox = new FwListBox())
			{

				using (var collection = new FwListBox.ObjectCollection(listBox))
				{
					ITsString testString = TsStringHelper.MakeTSS("test", m_hvoEnglishWs);

					// The Test
					collection.Add(testString);

					Assert.AreEqual(1, collection.Count);
					Assert.IsTrue(collection.Contains(testString));
				}
			}
	}

		[Test]
		public void Remove_CollectionWithSingleElement_CollectionShouldBeEmpty()
		{
			using (var listBox = new FwListBox())
			{
				using (var collection = new FwListBox.ObjectCollection(listBox))
				{
					ITsString testString = TsStringHelper.MakeTSS("test", m_hvoEnglishWs);
					collection.Add(testString);

					// The Test
					collection.Remove(testString);

					Assert.AreEqual(0, collection.Count);
					Assert.IsFalse(collection.Contains(testString));
				}
			}
		}
		[Test]
		public void Clear_CollectionWithSingleElement_CollectionShouldBeEmpty()
		{
			using (var listBox = new FwListBox())
			{
				using (var collection = new FwListBox.ObjectCollection(listBox))
				{
					ITsString testString = TsStringHelper.MakeTSS("test", m_hvoEnglishWs);
					collection.Add(testString);

					// The Test
					collection.Clear();

					Assert.AreEqual(0, collection.Count);
					Assert.IsFalse(collection.Contains(testString));
				}
			}
		}

		[Test]
		public void SetIndex_CollectionWithSingleElement_ValueShouldHaveChanged()
		{
			using (var listBox = new FwListBox())
			{
				using (var collection = new FwListBox.ObjectCollection(listBox))
				{
					ITsString testString1 = TsStringHelper.MakeTSS("test1", m_hvoEnglishWs);
					ITsString testString2 = TsStringHelper.MakeTSS("test2", m_hvoEnglishWs);
					collection.Add(testString1);

					// The Test
					collection[0] = testString2;

					Assert.AreEqual(1, collection.Count);
					Assert.IsFalse(collection.Contains(testString1));
					Assert.IsTrue(collection.Contains(testString2));
				}
			}
		}
		#endregion

		#region InnerFwListBox tests

		[Test]
		public void WritingSystemCode_EmptyFwListBox_DoesNotThrowException()
		{
			using (var listBox = new FwListBox())
			{
				using (var innerFwListBox = new InnerFwListBox(listBox))
				{
					// The Test
					Assert.GreaterOrEqual(innerFwListBox.WritingSystemCode, 0);
				}
			}
		}

		[Test]
		public void ShowHighlight_EmptyFwListBox_ReturnsTrue()
		{
			using (var listBox = new FwListBox())
			{
				using (var innerFwListBox = new InnerFwListBox(listBox))
				{
					// The Test
					Assert.AreEqual(true, innerFwListBox.ShowHighlight);
				}
			}
		}

		[Test]
		public void SetShowHighlight_EmptyFwListBox_ShouldBeSetToFalse()
		{
			using (var listBox = new FwListBox())
			{
				using (var innerFwListBox = new InnerFwListBox(listBox))
				{
					// The Test
					innerFwListBox.ShowHighlight = false;
					Assert.AreEqual(false, innerFwListBox.ShowHighlight);
				}
			}
		}

		[Test]
		public void IsHighlighted_EmptyFwListBox_ReturnsFalse()
		{
			using (var listBox = new FwListBox())
			{
				using (var innerFwListBox = new InnerFwListBox(listBox))
				{
					// The Test
					Assert.AreEqual(false, innerFwListBox.IsHighlighted(0));
				}
			}
		}

		[Test]
		public void IsHighlighted_CollectionWithSingleElement_ReturnsTrue()
		{
			using (var listBox = new FwListBox())
			{
					using (var innerFwListBox = new InnerFwListBox(listBox))
					{
						ITsString testString1 = TsStringHelper.MakeTSS("test1", m_hvoEnglishWs);
						listBox.Items.Add(testString1);
						innerFwListBox.MakeRoot();
						listBox.HighlightedIndex = 0;

						// The Test
						Assert.AreEqual(true, innerFwListBox.IsHighlighted(0));
					}
			}
		}
		#endregion
	}

}
