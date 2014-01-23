// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: NMockConstraintsTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using NUnit.Framework;
using NMock;
using NMock.Constraints;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// <summary>
	/// Tests for IgnoreOrderConstraint
	/// </summary>
	[TestFixture]
	public class IgnoreOrderConstraintsTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		public interface IIgnoreOrderTest
		{
			void Method(int[] param);
		}

		private DynamicMock m_mock;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_mock = new DynamicMock(typeof(IIgnoreOrderTest));
			m_mock.Expect("Method",
				new object[] { new IgnoreOrderConstraint(0, 1, 2 )},
				new string[] { typeof(int[]).FullName });
			m_mock.AdditionalReferences = new string[] { "COMInterfacesTests.dll" };
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that order is ignored if all expected values are passed in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OrderIsIgnored()
		{
			IIgnoreOrderTest test = (IIgnoreOrderTest)m_mock.MockInstance;
			test.Method(new int[] {2, 0, 1});

			m_mock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that unexpected values fail.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(VerifyException), ExpectedMessage="Method() called with " +
			"incorrect parameter (1)\nexpected:{ 0, 1, 2}\n but was:<System.Int32[]>")]
		public void ValuesAreChecked()
		{
			IIgnoreOrderTest test = (IIgnoreOrderTest)m_mock.MockInstance;
			test.Method(new int[] {3, 0, 1});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that differing length fails
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(VerifyException))]
		public void LengthChecked()
		{
			IIgnoreOrderTest test = (IIgnoreOrderTest)m_mock.MockInstance;
			test.Method(new int[] {0, 1});
		}
	}
}
