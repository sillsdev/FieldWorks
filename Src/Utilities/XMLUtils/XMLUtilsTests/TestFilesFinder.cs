using System;
using System.Diagnostics;

namespace XMLUtilsTests
{
	/// <summary>
	/// A little class to simply find the root source directory of these tests
	/// </summary>
	public class TestFilesFinder
	{
		public TestFilesFinder()
		{

		}

		//TODO: the following code relies on FieldWorks, so needs to be
		//revisited in order to get a non-FieldWorks version of this to test correctly.
		//Perhaps we need a layer which detects whether FieldWorks is in existence and if not
		//fall back to assuming that we are running from the codebase/bin/debug directory?
		public static string TestFilesRootDirectory
		{
			get
			{
				string source =SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory;
				string path =System.IO.Path.Combine(source, @"Utilities\XMLUtils\XMLUtilsTests");
				if(!System.IO.Directory.Exists(path))
				{
					Debug.Fail( path + " not found.  Have the XmlUtilsTests been moved?");
				}
				return path;
			}
		}
	}
}
