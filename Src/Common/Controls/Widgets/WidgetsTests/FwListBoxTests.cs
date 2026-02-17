// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FwListBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwListBoxTests
	{
		#region Data Members
		TestFwStylesheet m_stylesheet;
		WritingSystemManager m_wsManager;
		int m_hvoEnglishWs;
		#endregion

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			m_wsManager = new WritingSystemManager();

			// setup English ws.
			CoreWritingSystemDefinition enWs;
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
					ITsString testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);

					// The Test
					collection.Add(testString);

					Assert.That(collection.Count, Is.EqualTo(1));
					Assert.That(collection.Contains(testString), Is.True);
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
					ITsString testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);
					collection.Add(testString);

					// The Test
					collection.Remove(testString);

					Assert.That(collection.Count, Is.EqualTo(0));
					Assert.That(collection.Contains(testString), Is.False);
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
					ITsString testString = TsStringUtils.MakeString("test", m_hvoEnglishWs);
					collection.Add(testString);

					// The Test
					collection.Clear();

					Assert.That(collection.Count, Is.EqualTo(0));
					Assert.That(collection.Contains(testString), Is.False);
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
					ITsString testString1 = TsStringUtils.MakeString("test1", m_hvoEnglishWs);
					ITsString testString2 = TsStringUtils.MakeString("test2", m_hvoEnglishWs);
					collection.Add(testString1);

					// The Test
					collection[0] = testString2;

					Assert.That(collection.Count, Is.EqualTo(1));
					Assert.That(collection.Contains(testString1), Is.False);
					Assert.That(collection.Contains(testString2), Is.True);
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
					Assert.That(innerFwListBox.WritingSystemCode, Is.GreaterThanOrEqualTo(0));
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
					Assert.That(innerFwListBox.ShowHighlight, Is.EqualTo(true));
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
					Assert.That(innerFwListBox.ShowHighlight, Is.EqualTo(false));
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
					Assert.That(innerFwListBox.IsHighlighted(0), Is.EqualTo(false));
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
						ITsString testString1 = TsStringUtils.MakeString("test1", m_hvoEnglishWs);
						listBox.Items.Add(testString1);
						innerFwListBox.MakeRoot();
						listBox.HighlightedIndex = 0;

						// The Test
						Assert.That(innerFwListBox.IsHighlighted(0), Is.EqualTo(true));
					}
			}
		}
		#endregion
	}

}
