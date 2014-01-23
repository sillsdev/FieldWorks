// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UcdCharacterTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit tests, gets disposed in FixtureTearDown()")]
	public class UcdCharacterTests // can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			m_DebugProcs = new DebugProcs();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo() method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareTo()
		{
			UCDCharacter ucd = new BidiCharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.AreEqual(0, ucd.CompareTo("L"));
			Assert.Greater(0, ucd.CompareTo("R"));
			Assert.Less(0, ucd.CompareTo("AL"));
		}
	}
}
