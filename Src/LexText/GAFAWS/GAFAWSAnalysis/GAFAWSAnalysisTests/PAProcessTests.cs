// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: PAProcessTests.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Unit tests for the PositionAnalyzer class.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Test class for PositionAnalyzer class.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class PAProcessTests
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PAProcessTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check the Process method.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ProcessTest()
		{
			string asmPathname = Assembly.GetExecutingAssembly().CodeBase;
			asmPathname = asmPathname.Substring(8);
			string asmPath = asmPathname.Substring(0, asmPathname.LastIndexOf("/"));
			string testFile =  System.IO.Path.Combine(asmPath, "TestA1.xml");
			PositionAnalyzer pa = new PositionAnalyzer();
			string outPath = pa.Process(testFile);
		}
	}
}
