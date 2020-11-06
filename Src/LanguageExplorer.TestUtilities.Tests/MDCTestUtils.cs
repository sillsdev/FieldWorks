// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;

namespace LanguageExplorer.TestUtilities.Tests
{
	/// <summary>
	/// Meta Data Cache Test Utility methods
	/// </summary>
	class MDCTestUtils
	{
		public static string GetPathToTestFile(string testFile)
		{
			return Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", testFile);
		}
	}
}
