// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VersificationTests.cs
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
	/// Tests for the VersificationTable class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class VersificationTableTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up to initialize VersificationTable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			BCVRefTests.InitializeVersificationTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests VersificationTable.LastChapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastChapterForBook()
		{
			Assert.AreEqual(2, VersificationTable.Get(ScrVers.English).LastChapter(
				BCVRef.BookToNumber("HAG")));
			Assert.AreEqual(150, VersificationTable.Get(ScrVers.English).LastChapter(
				BCVRef.BookToNumber("PSA")));
			Assert.AreEqual(0, VersificationTable.Get(ScrVers.English).LastChapter(-1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the last verse for chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastVerseForChapter()
		{
			Assert.AreEqual(20, VersificationTable.Get(ScrVers.English).LastVerse(
				BCVRef.BookToNumber("PSA"), 9));
			Assert.AreEqual(39, VersificationTable.Get(ScrVers.Septuagint).LastVerse(
				BCVRef.BookToNumber("PSA"), 9));
			Assert.AreEqual(1, VersificationTable.Get(ScrVers.English).LastVerse(
				BCVRef.BookToNumber("PSA"), 0), "Intro chapter (0) should be treated as having 1 verse.");
			Assert.AreEqual(0, VersificationTable.Get(ScrVers.Septuagint).LastVerse(0, 0));
		}
	}
}
