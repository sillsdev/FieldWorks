// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SectionFilterTest.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		Unit tests for SectionFilter
// </remarks>
// --------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.PublishingSolution;
using System;

namespace FlexDePluginTests
{
	/// <summary>
	///This is a test class for SectionFilterTest and is intended
	///to contain all SectionFilterTest Unit Tests
	///</summary>
	[TestFixture]
	public class SectionFilterTest : SectionFilter
	{
		/// <summary>
		///A test for SectionFilterReversal
		///</summary>
		[Test]
		public void SectionFilterReversalTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			target.SectionFilterReversal = expected;
			actual = target.SectionFilterReversal;
			Assert.AreEqual(expected, actual);
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for SectionFilterMain
		///</summary>
		[Test]
		public void SectionFilterMainTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			target.SectionFilterMain = expected;
			actual = target.SectionFilterMain;
			Assert.AreEqual(expected, actual);
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for OutputLocationPath
		///</summary>
		[Test]
		public void OutputLocationPathTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			string actual;
			actual = target.OutputLocationPath;
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ExistingDirectoryLocationPath
		///</summary>
		[Test]
		public void ExistingDirectoryLocationPathTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			string actual;
			actual = target.ExistingDirectoryLocationPath;
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ExistingDirectoryInput
		///</summary>
		[Test]
		public void ExistingDirectoryInputTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			bool actual;
			actual = target.ExistingDirectoryInput;
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for DictionaryName
		///</summary>
		[Test]
		public void DictionaryNameTest()
		{
			SectionFilter target = new SectionFilter(); // TODO: Initialize to an appropriate value
			string actual;
			actual = target.DictionaryName;
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for validateInput
		///</summary>
		[Test]
		// [DeploymentItem("FlexDePlugin.dll")]
		public void validateInputTest()
		{
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = validateInput();
			Assert.AreEqual(expected, actual);
			// TODO: Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ValidateDirectoryLocation
		///</summary>
		[Test]
		// [DeploymentItem("FlexDePlugin.dll")]
		public void ValidateDirectoryLocationTest()
		{
			ValidateDirectoryLocation();
			// TODO: A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for SectionFilter Constructor
		///</summary>
		[Test]
		public void SectionFilterConstructorTest()
		{
			SectionFilter target = new SectionFilter();
			// TODO: TODO: Implement code to verify target");
		}
	}
}
