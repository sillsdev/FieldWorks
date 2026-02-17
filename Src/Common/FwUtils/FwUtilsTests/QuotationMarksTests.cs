// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the QuotationMark class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class QuotationMarkTests
	{
		private QuotationMarksList m_qmList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_qmList = QuotationMarksList.NewList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only one level (Simple case)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_1Level()
		{
			m_qmList.RemoveLastLevel();
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only two levels (Simple case)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_2Levels()
		{
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only three levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_3Levels_repeated1()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only three levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_3Levels_diffOpen()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only three levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_3Levels_diffClose()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = "]";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is only three levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_3Levels()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels_repeated1And2()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = ">>";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels_repeated2()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels_repeated1To3()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<<";
			m_qmList[3].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels_diffOpen()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = ">>";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels_diffClose()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there is four levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_4Levels()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "{";
			m_qmList[3].Closing = "}";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_repeated1And2()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = ">>";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			m_qmList[4].Opening = "<<";
			m_qmList[4].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_repeated1To3()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<<";
			m_qmList[3].Closing = ">>";
			m_qmList[4].Opening = "<";
			m_qmList[4].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_repeated1To4()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "{";
			m_qmList[3].Closing = "}";
			m_qmList[4].Opening = "<<";
			m_qmList[4].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_repeated2()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "{";
			m_qmList[3].Closing = "}";
			m_qmList[4].Opening = "<";
			m_qmList[4].Closing = ">";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(5));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_repeated3()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "{";
			m_qmList[3].Closing = "}";
			m_qmList[4].Opening = "[";
			m_qmList[4].Closing = "]";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(5));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_diffOpen()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = ">>";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			m_qmList[4].Opening = "<<";
			m_qmList[4].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels_diffClose()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";
			m_qmList[4].Opening = "<<";
			m_qmList[4].Closing = ">>";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DistinctLevels property when there are five levels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestDistinctLevels_5Levels()
		{
			m_qmList.EnsureLevelExists(5);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "{";
			m_qmList[3].Closing = "}";
			m_qmList[4].Opening = "*";
			m_qmList[4].Closing = "*";
			Assert.That(m_qmList.DistinctLevels, Is.EqualTo(5));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_1()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = ">>";
			Assert.That(m_qmList.InvalidOpenerCloserCombinations, Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_2()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = ">>";
			m_qmList[2].Closing = "]";
			QuotationMarksList.InvalidComboInfo result = m_qmList.InvalidOpenerCloserCombinations;
			Assert.That(result, Is.Not.Null);
			Assert.That(result.LowerLevel, Is.EqualTo(0));
			Assert.That(result.LowerLevelIsOpener, Is.False);
			Assert.That(result.UpperLevel, Is.EqualTo(2));
			Assert.That(result.UpperLevelIsOpener, Is.True);
			Assert.That(result.QMark, Is.EqualTo(">>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_3()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "<<";
			QuotationMarksList.InvalidComboInfo result = m_qmList.InvalidOpenerCloserCombinations;
			Assert.That(result, Is.Not.Null);
			Assert.That(result.LowerLevel, Is.EqualTo(0));
			Assert.That(result.LowerLevelIsOpener, Is.True);
			Assert.That(result.UpperLevel, Is.EqualTo(2));
			Assert.That(result.UpperLevelIsOpener, Is.False);
			Assert.That(result.QMark, Is.EqualTo("<<"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_4()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "!";
			m_qmList[0].Closing = "!";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "!";
			m_qmList[2].Closing = "!";
			Assert.That(m_qmList.InvalidOpenerCloserCombinations, Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_5()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "!";
			m_qmList[0].Closing = "!";
			m_qmList[1].Opening = "!";
			m_qmList[1].Closing = "!";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			QuotationMarksList.InvalidComboInfo result = m_qmList.InvalidOpenerCloserCombinations;
			Assert.That(result, Is.Not.Null);
			Assert.That(result.LowerLevel, Is.EqualTo(0));
			Assert.That(result.LowerLevelIsOpener, Is.True);
			Assert.That(result.UpperLevel, Is.EqualTo(1));
			Assert.That(result.UpperLevelIsOpener, Is.False);
			Assert.That(result.QMark, Is.EqualTo("!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_6()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "!";
			m_qmList[0].Closing = "!";
			m_qmList[1].Opening = "!";
			m_qmList[1].Closing = "!";
			m_qmList[2].Opening = "!";
			m_qmList[2].Closing = "!";
			QuotationMarksList.InvalidComboInfo result = m_qmList.InvalidOpenerCloserCombinations;
			Assert.That(result, Is.Not.Null);
			Assert.That(result.LowerLevel, Is.EqualTo(0));
			Assert.That(result.LowerLevelIsOpener, Is.True);
			Assert.That(result.UpperLevel, Is.EqualTo(1));
			Assert.That(result.UpperLevelIsOpener, Is.False);
			Assert.That(result.QMark, Is.EqualTo("!"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InvalidOpenerCloserCombinations property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInvalidOpenerCloserCombinations_7()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<<";
			m_qmList[1].Closing = ">>";
			m_qmList[2].Opening = "<<";
			m_qmList[2].Closing = ">>";
			Assert.That(m_qmList.InvalidOpenerCloserCombinations, Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToEmptyList()
		{
			m_qmList.Clear();
			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(1));
			Assert.That(string.IsNullOrEmpty(m_qmList[0].Opening), Is.True);
			Assert.That(string.IsNullOrEmpty(m_qmList[0].Closing), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToListWith1Level()
		{
			m_qmList.RemoveLastLevel();
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(2));
			Assert.That(string.IsNullOrEmpty(m_qmList[1].Opening), Is.True);
			Assert.That(string.IsNullOrEmpty(m_qmList[1].Closing), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToListWith2Levels()
		{
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(3));
			Assert.That(m_qmList[2].Opening, Is.EqualTo("<<"));
			Assert.That(m_qmList[2].Closing, Is.EqualTo(">>"));

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(4));
			Assert.That(m_qmList[3].Opening, Is.EqualTo("<"));
			Assert.That(m_qmList[3].Closing, Is.EqualTo(">"));

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(5));
			Assert.That(m_qmList[4].Opening, Is.EqualTo("<<"));
			Assert.That(m_qmList[4].Closing, Is.EqualTo(">>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToListWith2Levels_1empty()
		{
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";

			for (int i = 2; i < 5; i++)
			{
				m_qmList.AddLevel();
				Assert.That(m_qmList.Levels, Is.EqualTo(i + 1));
				Assert.That(string.IsNullOrEmpty(m_qmList[i].Opening), Is.True);
				Assert.That(string.IsNullOrEmpty(m_qmList[i].Closing), Is.True);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToListWith3Levels()
		{
			m_qmList.EnsureLevelExists(3);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(4));
			Assert.That(m_qmList[3].Opening, Is.EqualTo("<<"));
			Assert.That(m_qmList[3].Closing, Is.EqualTo(">>"));

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(5));
			Assert.That(m_qmList[4].Opening, Is.EqualTo("<"));
			Assert.That(m_qmList[4].Closing, Is.EqualTo(">"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddLevel method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddLevelToListWith4Levels()
		{
			m_qmList.EnsureLevelExists(4);
			m_qmList[0].Opening = "<<";
			m_qmList[0].Closing = ">>";
			m_qmList[1].Opening = "<";
			m_qmList[1].Closing = ">";
			m_qmList[2].Opening = "[";
			m_qmList[2].Closing = "]";
			m_qmList[3].Opening = "<";
			m_qmList[3].Closing = ">";

			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(5));
			Assert.That(m_qmList[4].Opening, Is.EqualTo("<<"));
			Assert.That(m_qmList[4].Closing, Is.EqualTo(">>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestIsEmpty()
		{
			Assert.That(m_qmList.IsEmpty, Is.True);

			m_qmList[0].Opening = "[";
			Assert.That(m_qmList.IsEmpty, Is.False);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = "[";
			Assert.That(m_qmList.IsEmpty, Is.False);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = string.Empty;
			m_qmList[1].Opening = "[";
			Assert.That(m_qmList.IsEmpty, Is.False);

			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = "[";
			Assert.That(m_qmList.IsEmpty, Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestIsComplete()
		{
			Assert.That(m_qmList.IsEmpty, Is.True);
			Assert.That(m_qmList[0].IsComplete, Is.False);
			Assert.That(m_qmList[1].IsComplete, Is.False);

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = string.Empty;
			Assert.That(m_qmList[0].IsComplete, Is.False);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = "]";
			Assert.That(m_qmList[0].IsComplete, Is.False);

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "[";
			Assert.That(m_qmList[0].IsComplete, Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestFindGap()
		{
			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(3));

			Assert.That(m_qmList.FindGap(), Is.EqualTo(0));

			m_qmList[1].Opening = "[";
			m_qmList[1].Closing = string.Empty;
			Assert.That(m_qmList.FindGap(), Is.EqualTo(1));

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "]";
			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = string.Empty;
			m_qmList[2].Opening = "{";
			m_qmList[2].Closing = string.Empty;
			Assert.That(m_qmList.FindGap(), Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTrimmedList()
		{
			m_qmList.AddLevel();
			Assert.That(m_qmList.Levels, Is.EqualTo(3));

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "]";
			Assert.That(m_qmList.TrimmedList.Levels, Is.EqualTo(1));

			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = "}";
			Assert.That(m_qmList.TrimmedList.Levels, Is.EqualTo(2));

			QuotationMarksList qmTrimmed = m_qmList.TrimmedList;
			Assert.That(qmTrimmed[0].Opening, Is.EqualTo(m_qmList[0].Opening));
			Assert.That(qmTrimmed[0].Closing, Is.EqualTo(m_qmList[0].Closing));
			Assert.That(qmTrimmed[1].Opening, Is.EqualTo(m_qmList[1].Opening));
			Assert.That(qmTrimmed[1].Closing, Is.EqualTo(m_qmList[1].Closing));
		}
	}
}
