// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NMockConstraintsTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
	public class IgnoreOrderConstraintsTests
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
