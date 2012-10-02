// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TsStringComparerTests.cs
// Responsibility: TE Team
//
// <remarks>
// NOTE: previously we checked the result of TsStringComparer.Compare() for -1/1.
// The comment for Compare however says that the result will be <0/>0. On my machine I
// got -48 for whatever reason, so I decided to change the test code to match the comment.
// If we realize that it has to be -1/1 then we should revert the test code and also
// change the COMMENT of the Compare method.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the TsStringComparer class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsStringComparerTests : FwCOMTestBase // we can't derive from BaseTest because of circular dependencies
	{
		private IWritingSystemManager m_wsManager;
		private TsStringComparer m_comparer;
		private ITsStrFactory m_tssFact;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixtures the setup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_wsManager = new PalasoWritingSystemManager();
			m_comparer = new TsStringComparer(m_wsManager.Create("en"));
			m_tssFact = TsStrFactoryClass.Create();
		}

		/// <summary/>
		[TestFixtureTearDown]
		public void TearDown()
		{
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If both arguments are null we expect Compare to return 0.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void BothArgumentsNull()
		{
			Assert.That(m_comparer.Compare(null, null), Is.EqualTo(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If one argument is null we expect Compare to return 1/-1.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OneArgumentNull()
		{
			ITsString tss = m_tssFact.MakeString("bla", 1);
			Assert.That(m_comparer.Compare(null, tss), Is.LessThan(0));
			Assert.That(m_comparer.Compare(tss, null), Is.GreaterThan(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If one argument is the empty string we expect Compare to return 1/-1.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OneArgumentEmptyString()
		{
			ITsString tss = m_tssFact.MakeString("bla", 1);
			ITsString tssEmpty = m_tssFact.MakeString(string.Empty, 1);
			Assert.That(m_comparer.Compare(tssEmpty, tss), Is.LessThan(0));
			Assert.That(m_comparer.Compare(tss, tssEmpty), Is.GreaterThan(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If one argument is the empty string and the other argument is null we expect
		/// Compare to return 0.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OneArgumentEmptyStringOtherNull()
		{
			ITsString tss = m_tssFact.MakeString(string.Empty, 1);
			Assert.That(m_comparer.Compare(null, tss), Is.EqualTo(0));
			Assert.That(m_comparer.Compare(tss, null), Is.EqualTo(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test comparing two strings
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareTwoStrings()
		{
			ITsString tss1 = m_tssFact.MakeString("bla", 1);
			ITsString tss2 = m_tssFact.MakeString("zzz", 1);
			Assert.That(m_comparer.Compare(tss1, tss2), Is.LessThan(0));
			Assert.That(m_comparer.Compare(tss2, tss1), Is.GreaterThan(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Comparing two instances of the same strings should return 0
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareIdenticalStrings()
		{
			ITsString tss1 = m_tssFact.MakeString("bla", 1);
			ITsString tss2 = m_tssFact.MakeString("bla", 1);
			Assert.That(m_comparer.Compare(tss1, tss2), Is.EqualTo(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Comparing two strings with different writing systems
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareTwoStringsWithDifferentWs()
		{
			ITsString tss1 = m_tssFact.MakeString("bla", 1);
			ITsString tss2 = m_tssFact.MakeString("zzz", 2);
			Assert.That(m_comparer.Compare(tss1, tss2), Is.LessThan(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If first argument is not a ITsString we expect that an ArgumentException is thrown
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void FirstArgumentNotTsString()
		{
			ITsString tss = m_tssFact.MakeString("bla", 1);
			m_comparer.Compare(123, tss);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If second argument is not a ITsString we expect that an ArgumentException is thrown
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void SecondArgumentNotTsString()
		{
			ITsString tss = m_tssFact.MakeString("bla", 1);
			m_comparer.Compare(tss, 123);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test comparing two strings when no ICU locale has been set.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareTwoStringsWithoutIcu()
		{
			var comparer = new TsStringComparer();
			ITsString tss1 = m_tssFact.MakeString("bla", 1);
			ITsString tss2 = m_tssFact.MakeString("zzz", 1);
			Assert.That(comparer.Compare(tss1, tss2), Is.LessThan(0));
			Assert.That(comparer.Compare(tss2, tss1), Is.GreaterThan(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test comparing two strings when one argument is a ITsString and the other one is a
		/// regular string
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareTsStringWithRegularString()
		{
			ITsString tss = m_tssFact.MakeString("bla", 1);
			Assert.That(m_comparer.Compare(tss, "zzz"), Is.LessThan(0));
			Assert.That(m_comparer.Compare("zzz", tss), Is.GreaterThan(0));
		}
	}
}
