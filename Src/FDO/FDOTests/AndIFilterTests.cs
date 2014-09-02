// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AndIFilterTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region Class NamelessFilter
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a filter for testing that has an empty name
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class NamelessFilter : IFilter
	{
		#region Data members
		private bool m_fInitCriteriaCalled = false;
		#endregion

		#region IFilter Members
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Inits the criteria.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void InitCriteria()
		{
			if (m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria already called for the NamelessFilter class.");
			m_fInitCriteriaCalled = true;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Matches the criteria.
		/// </summary>
		/// <param name="hvoObj">The hvo obj.</param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			if (!m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria not called for the NamelessFilter class.");

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FilterName
		{
			get { return string.Empty; }
		}

		#endregion
	}
	#endregion

	#region Class OddFilter
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a filter for testing that ignores any DummyText object with an odd Id
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class OddFilter : IFilter
	{
		#region Data members
		private bool m_fInitCriteriaCalled = false;
		#endregion

		#region IFilter Members
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Inits the criteria.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void InitCriteria()
		{
			if (m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria already called for the OddFilter class.");
			m_fInitCriteriaCalled = true;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Matches the criteria.
		/// </summary>
		/// <param name="hvoObj">The hvo obj.</param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			if (!m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria not called for the OddFilter class.");

			return (hvoObj % 2) != 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public string FilterName
		{
			get { return "Odd"; }
		}

		#endregion
	}
	#endregion

	#region Class RangeFilter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple implementation of IFilter that just checks for an HVO in a certain range.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class RangeFilter : IFilter
	{
		#region Data members
		private bool m_fInitCriteriaCalled = false;
		internal int m_minHvoToMatch;
		internal int m_maxHvoToMatch;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeFilter"/> class.
		/// </summary>
		/// <param name="min">The minimum HVO to match.</param>
		/// <param name="max">The maximum HVO to match.</param>
		/// ------------------------------------------------------------------------------------
		internal RangeFilter(int min, int max)
		{
			if (min > max)
				throw new ArgumentException("min > max");
			m_minHvoToMatch = min;
			m_maxHvoToMatch = max;
		}
		#endregion

		#region IFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="M:SIL.FieldWorks.FDO.IFilter.MatchesCriteria(System.Int32)"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitCriteria()
		{
			if (m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria already called for the RangeFilter class.");
			m_fInitCriteriaCalled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns>
		/// 	<c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c>
		/// </returns>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			if (!m_fInitCriteriaCalled)
				throw new InvalidOperationException("InitCriteria not called for the RangeFilter class.");

			return (hvoObj >= m_minHvoToMatch && hvoObj <= m_maxHvoToMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string FilterName
		{
			get { return "Range " + m_minHvoToMatch + "-" + m_maxHvoToMatch; }
		}

		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the AndIFilter class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AndIFilterTests : BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AndIFilter class with two filters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoFilters()
		{
			AndIFilter filter = new AndIFilter(new OddFilter(), new RangeFilter(23, 76));
			filter.InitCriteria();
			Assert.AreEqual("Odd & Range 23-76", filter.Name);
			Assert.IsTrue(filter.MatchesCriteria(23));
			Assert.IsTrue(filter.MatchesCriteria(75));
			Assert.IsFalse(filter.MatchesCriteria(22));
			Assert.IsFalse(filter.MatchesCriteria(42));
			Assert.IsFalse(filter.MatchesCriteria(76));
			Assert.IsFalse(filter.MatchesCriteria(21));
			Assert.IsFalse(filter.MatchesCriteria(77));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AndIFilter class with one filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OneFilter()
		{
			AndIFilter filter = new AndIFilter();
			filter.Add(new OddFilter());
			filter.InitCriteria();
			Assert.AreEqual("Odd", filter.Name);
			Assert.IsTrue(filter.MatchesCriteria(23));
			Assert.IsTrue(filter.MatchesCriteria(75));
			Assert.IsFalse(filter.MatchesCriteria(22));
			Assert.IsFalse(filter.MatchesCriteria(42));
			Assert.IsFalse(filter.MatchesCriteria(76));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AndIFilter class with three filters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ThreeFilters()
		{
			AndIFilter filter = new AndIFilter(3);
			filter.Add(new OddFilter());
			filter.Add(new RangeFilter(3, 25));
			filter.Add(new RangeFilter(10, 36));
			filter.InitCriteria();
			Assert.AreEqual("Odd & Range 3-25 & Range 10-36", filter.Name);
			Assert.IsTrue(filter.MatchesCriteria(11));
			Assert.IsTrue(filter.MatchesCriteria(25));
			Assert.IsFalse(filter.MatchesCriteria(2));
			Assert.IsFalse(filter.MatchesCriteria(9));
			Assert.IsFalse(filter.MatchesCriteria(27));
			Assert.IsFalse(filter.MatchesCriteria(12));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AndIFilter class when one of the filters is nameless
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NamelessFilter()
		{
			AndIFilter filter = new AndIFilter(new OddFilter(), new NamelessFilter());
			filter.InitCriteria();
			Assert.AreEqual("Odd", filter.Name);
		}
	}
}
