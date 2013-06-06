// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwUtils.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using SIL.CoreImpl;


namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Collection of miscellaneous utility methods needed for FieldWorks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FwUtils
	{
		/// <summary>
		/// The name of the overarching umbrella application that will one day conquer the world:
		/// "FieldWorks"
		/// </summary>
		public const string ksSuiteName = "FieldWorks";
		/// <summary>
		/// The name of the Translation Editor folder (Even though this is the same as
		/// DirectoryFinder.ksTeFolderName and FwSubKey.TE, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksTeAppName = "Translation Editor";
		/// <summary>The command-line abbreviation for Translation Editor</summary>
		public const string ksTeAbbrev = "TE";
		/// <summary>
		/// The fully-qualified (with namespace) C# object name for TeApp
		/// </summary>
		public const string ksFullTeAppObjectName = "SIL.FieldWorks.TE.TeApp";
		/// <summary>
		/// The name of the Language Explorer folder (Even though this is the same as
		/// DirectoryFinder.ksFlexFolderName and FwSubKey.LexText, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksFlexAppName = "Language Explorer";
		/// <summary>The command-line abbreviation for the Language Explorer</summary>
		public const string ksFlexAbbrev = "FLEx";
		/// <summary>
		/// The fully-qualified (with namespace) C# object name for LexTextApp
		/// </summary>
		public const string ksFullFlexAppObjectName = "SIL.FieldWorks.XWorks.LexText.LexTextApp";
		/// <summary>
		/// The current version of FieldWorks. This is also known in COMInterfaces/IcuWrappers.cs, InitIcuDataDir.
		/// </summary>
		public const int SuiteVersion = 8;

		/// <summary>Used in tests to fake TE being installed (Set by using reflection)</summary>
		private static bool? s_fIsTEInstalled;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether TE is installed or not (formerly part of MiscUtils in Utils.cs).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsTEInstalled
		{
			get
			{
				// If we have already determined that TE is installed (or are forcing it to
				// pretend it is for the sake of tests, we don't need to re-test for TE.
				if (s_fIsTEInstalled != null)
					return (bool)s_fIsTEInstalled;

				// see if we can find the program in the installation directory.
				s_fIsTEInstalled = File.Exists(DirectoryFinder.TeExe);
				return (bool)s_fIsTEInstalled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether FLEx is installed.
		/// We consider FLEx to be installed if we can find it in the same directory as our
		/// own assembly. That's a rather strong requirement, but it's how we install it.
		/// </summary>
		/// <remarks>We could do the really complicated thing they do above to see if TE is
		/// installed, but why bother?</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool IsFlexInstalled
		{
			get { return File.Exists(DirectoryFinder.FlexExe); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a name suitable for use as a pipe name from the specified project handle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GeneratePipeHandle(string handle)
		{
			const string ksSuiteIdPrefix = ksSuiteName + ":";
			return (handle.StartsWith(ksSuiteIdPrefix) ? string.Empty : ksSuiteIdPrefix) +
				handle.Replace('/', ':').Replace('\\', ':');
		}

		/// <summary>
		/// Whenever possible use this in place of new PalasoWritingSystemManager.
		/// It sets the TemplateFolder, which unfortunately the constructor cannot do because
		/// the direction of our dependencies does not allow it to reference FwUtils and access DirectoryFinder.
		/// </summary>
		/// <returns></returns>
		public static PalasoWritingSystemManager CreateWritingSystemManager()
		{
			var result = new PalasoWritingSystemManager();
			result.TemplateFolder = DirectoryFinder.TemplateDirectory;
			return result;
		}

	}
}
