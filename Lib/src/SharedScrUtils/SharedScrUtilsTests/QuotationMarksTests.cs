// --------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: QuotationMarkTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using NUnit.Framework;
using Microsoft.Win32;
using System.Reflection;

namespace SILUBS.SharedScrUtils
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
			Assert.AreEqual(1, m_qmList.DistinctLevels);
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
			Assert.AreEqual(2, m_qmList.DistinctLevels);
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
			Assert.AreEqual(2, m_qmList.DistinctLevels);
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
			Assert.AreEqual(3, m_qmList.DistinctLevels);
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
			Assert.AreEqual(3, m_qmList.DistinctLevels);
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
			Assert.AreEqual(3, m_qmList.DistinctLevels);
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
			Assert.AreEqual(2, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(3, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(2, m_qmList.DistinctLevels);
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
			Assert.AreEqual(3, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(5, m_qmList.DistinctLevels);
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
			Assert.AreEqual(5, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(4, m_qmList.DistinctLevels);
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
			Assert.AreEqual(5, m_qmList.DistinctLevels);
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
			Assert.IsNull(m_qmList.InvalidOpenerCloserCombinations);
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
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.LowerLevel);
			Assert.IsFalse(result.LowerLevelIsOpener);
			Assert.AreEqual(2, result.UpperLevel);
			Assert.IsTrue(result.UpperLevelIsOpener);
			Assert.AreEqual(">>", result.QMark);
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
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.LowerLevel);
			Assert.IsTrue(result.LowerLevelIsOpener);
			Assert.AreEqual(2, result.UpperLevel);
			Assert.IsFalse(result.UpperLevelIsOpener);
			Assert.AreEqual("<<", result.QMark);
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
			Assert.IsNull(m_qmList.InvalidOpenerCloserCombinations);
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
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.LowerLevel);
			Assert.IsTrue(result.LowerLevelIsOpener);
			Assert.AreEqual(1, result.UpperLevel);
			Assert.IsFalse(result.UpperLevelIsOpener);
			Assert.AreEqual("!", result.QMark);
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
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.LowerLevel);
			Assert.IsTrue(result.LowerLevelIsOpener);
			Assert.AreEqual(1, result.UpperLevel);
			Assert.IsFalse(result.UpperLevelIsOpener);
			Assert.AreEqual("!", result.QMark);
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
			Assert.IsNull(m_qmList.InvalidOpenerCloserCombinations);
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
			Assert.AreEqual(1, m_qmList.Levels);
			Assert.IsTrue(string.IsNullOrEmpty(m_qmList[0].Opening));
			Assert.IsTrue(string.IsNullOrEmpty(m_qmList[0].Closing));
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
			Assert.AreEqual(2, m_qmList.Levels);
			Assert.IsTrue(string.IsNullOrEmpty(m_qmList[1].Opening));
			Assert.IsTrue(string.IsNullOrEmpty(m_qmList[1].Closing));
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
			Assert.AreEqual(3, m_qmList.Levels);
			Assert.AreEqual("<<", m_qmList[2].Opening);
			Assert.AreEqual(">>", m_qmList[2].Closing);

			m_qmList.AddLevel();
			Assert.AreEqual(4, m_qmList.Levels);
			Assert.AreEqual("<", m_qmList[3].Opening);
			Assert.AreEqual(">", m_qmList[3].Closing);

			m_qmList.AddLevel();
			Assert.AreEqual(5, m_qmList.Levels);
			Assert.AreEqual("<<", m_qmList[4].Opening);
			Assert.AreEqual(">>", m_qmList[4].Closing);
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
				Assert.AreEqual(i + 1, m_qmList.Levels);
				Assert.IsTrue(string.IsNullOrEmpty(m_qmList[i].Opening));
				Assert.IsTrue(string.IsNullOrEmpty(m_qmList[i].Closing));
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
			Assert.AreEqual(4, m_qmList.Levels);
			Assert.AreEqual("<<", m_qmList[3].Opening);
			Assert.AreEqual(">>", m_qmList[3].Closing);

			m_qmList.AddLevel();
			Assert.AreEqual(5, m_qmList.Levels);
			Assert.AreEqual("<", m_qmList[4].Opening);
			Assert.AreEqual(">", m_qmList[4].Closing);
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
			Assert.AreEqual(5, m_qmList.Levels);
			Assert.AreEqual("<<", m_qmList[4].Opening);
			Assert.AreEqual(">>", m_qmList[4].Closing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestIsEmpty()
		{
			Assert.IsTrue(m_qmList.IsEmpty);

			m_qmList[0].Opening = "[";
			Assert.IsFalse(m_qmList.IsEmpty);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = "[";
			Assert.IsFalse(m_qmList.IsEmpty);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = string.Empty;
			m_qmList[1].Opening = "[";
			Assert.IsFalse(m_qmList.IsEmpty);

			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = "[";
			Assert.IsFalse(m_qmList.IsEmpty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestIsComplete()
		{
			Assert.IsTrue(m_qmList.IsEmpty);
			Assert.IsFalse(m_qmList[0].IsComplete);
			Assert.IsFalse(m_qmList[1].IsComplete);

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = string.Empty;
			Assert.IsFalse(m_qmList[0].IsComplete);

			m_qmList[0].Opening = string.Empty;
			m_qmList[0].Closing = "]";
			Assert.IsFalse(m_qmList[0].IsComplete);

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "[";
			Assert.IsTrue(m_qmList[0].IsComplete);
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
			Assert.AreEqual(3, m_qmList.Levels);

			Assert.AreEqual(0, m_qmList.FindGap());

			m_qmList[1].Opening = "[";
			m_qmList[1].Closing = string.Empty;
			Assert.AreEqual(1, m_qmList.FindGap());

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "]";
			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = string.Empty;
			m_qmList[2].Opening = "{";
			m_qmList[2].Closing = string.Empty;
			Assert.AreEqual(2, m_qmList.FindGap());
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
			Assert.AreEqual(3, m_qmList.Levels);

			m_qmList[0].Opening = "[";
			m_qmList[0].Closing = "]";
			Assert.AreEqual(1, m_qmList.TrimmedList.Levels);

			m_qmList[1].Opening = string.Empty;
			m_qmList[1].Closing = "}";
			Assert.AreEqual(2, m_qmList.TrimmedList.Levels);

			QuotationMarksList qmTrimmed = m_qmList.TrimmedList;
			Assert.AreEqual(m_qmList[0].Opening, qmTrimmed[0].Opening);
			Assert.AreEqual(m_qmList[0].Closing, qmTrimmed[0].Closing);
			Assert.AreEqual(m_qmList[1].Opening, qmTrimmed[1].Opening);
			Assert.AreEqual(m_qmList[1].Closing, qmTrimmed[1].Closing);
		}
	}
}
