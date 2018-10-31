// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Tests for FwListBox.
	/// </summary>
	[TestFixture]
	public class FwListBoxTests
	{
		#region Data Members
		private TestFwStylesheet m_stylesheet;
		private WritingSystemManager m_wsManager;
		private int m_hvoEnglishWs;
		private FwListBox _fwListBox;
		#endregion

		/// <summary />
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_wsManager = new WritingSystemManager();

			// setup English ws.
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
		}

		/// <summary />
		[SetUp]
		public void TestSetup()
		{
			_fwListBox = new FwListBox();
		}

		/// <summary />
		[TearDown]
		public void TearDown()
		{
			_fwListBox?.Dispose();
			_fwListBox = null;
		}

		#region ObjectCollection Collection tests.

		/// <summary />
		[Test]
		public void Add_EmptyObjectCollection_CollectionContainsSingleElement()
		{
			using (var collection = new ObjectCollection(_fwListBox))
			{
				var testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);

				// The Test
				collection.Add(testString);

				Assert.AreEqual(1, collection.Count);
				Assert.IsTrue(collection.Contains(testString));
			}
		}

		/// <summary />
		[Test]
		public void Remove_CollectionWithSingleElement_CollectionShouldBeEmpty()
		{
			using (var collection = new ObjectCollection(_fwListBox))
			{
				var testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);
				collection.Add(testString);

				// The Test
				collection.Remove(testString);

				Assert.AreEqual(0, collection.Count);
				Assert.IsFalse(collection.Contains(testString));
			}
		}
		/// <summary />
		[Test]
		public void Clear_CollectionWithSingleElement_CollectionShouldBeEmpty()
		{
			using (var collection = new ObjectCollection(_fwListBox))
			{
				var testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);
				collection.Add(testString);

				// The Test
				collection.Clear();

				Assert.AreEqual(0, collection.Count);
				Assert.IsFalse(collection.Contains(testString));
			}
		}

		/// <summary />
		[Test]
		public void SetIndex_CollectionWithSingleElement_ValueShouldHaveChanged()
		{
			using (var collection = new ObjectCollection(_fwListBox))
			{
				var testString1 = TsStringUtils.MakeString("test1", m_hvoEnglishWs);
				var testString2 = TsStringUtils.MakeString("test2", m_hvoEnglishWs);
				collection.Add(testString1);

				// The Test
				collection[0] = testString2;

				Assert.AreEqual(1, collection.Count);
				Assert.IsFalse(collection.Contains(testString1));
				Assert.IsTrue(collection.Contains(testString2));
			}
		}
		#endregion

		#region InnerFwListBox tests

		/// <summary />
		[Test]
		public void WritingSystemCode_EmptyFwListBox_DoesNotThrowException()
		{
			using (var innerFwListBox = new InnerFwListBox(_fwListBox))
			{
				// The Test
				Assert.GreaterOrEqual(innerFwListBox.WritingSystemCode, 0);
			}
		}

		/// <summary />
		[Test]
		public void ShowHighlight_EmptyFwListBox_ReturnsTrue()
		{
			using (var listBox = new FwListBox())
			using (var innerFwListBox = new InnerFwListBox(listBox))
			{
				// The Test
				Assert.AreEqual(true, innerFwListBox.ShowHighlight);
			}
		}

		/// <summary />
		[Test]
		public void SetShowHighlight_EmptyFwListBox_ShouldBeSetToFalse()
		{
			using (var innerFwListBox = new InnerFwListBox(_fwListBox))
			{
				// The Test
				innerFwListBox.ShowHighlight = false;
				Assert.AreEqual(false, innerFwListBox.ShowHighlight);
			}
		}

		/// <summary />
		[Test]
		public void IsHighlighted_EmptyFwListBox_ReturnsFalse()
		{
			using (var innerFwListBox = new InnerFwListBox(_fwListBox))
			{
				// The Test
				Assert.AreEqual(false, innerFwListBox.IsHighlighted(0));
			}
		}

		/// <summary />
		[Test]
		public void IsHighlighted_CollectionWithSingleElement_ReturnsTrue()
		{
			using (var innerFwListBox = new InnerFwListBox(_fwListBox))
			{
				var testString1 = TsStringUtils.MakeString("test1", m_hvoEnglishWs);
				_fwListBox.Items.Add(testString1);
				innerFwListBox.MakeRoot();
				_fwListBox.HighlightedIndex = 0;

				// The Test
				Assert.AreEqual(true, innerFwListBox.IsHighlighted(0));
			}
		}
		#endregion
	}
}