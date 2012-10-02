//-------------------------------------------------------------------------------------------------
// <copyright file="PackageUtility.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Helper methods for the project.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.OLE.Interop;

	/// <summary>
	/// Provides miscellaneous helper methods to the project.
	/// </summary>
	public sealed class PackageUtility
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(PackageUtility);
		private static readonly char[] InvalidFileCharacters = { '/', '?', ':', '&', '\\', '*', '"', '<', '>', '|', '#', '%' };
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private PackageUtility()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Appends an extra &quot;consult the trace log at...&quot; message to the specified message.
		/// </summary>
		/// <param name="message">The message to append the &quot;consult the trace log at ...&quot; message to.</param>
		/// <remarks>Note the conditional compilation attribute which causes this method to not be called in non-traced builds.</remarks>
		[Conditional("TRACE")]
		public static void AppendConsultTraceMessage(ref string message)
		{
			message += SconceStrings.ConsultTraceLog(Tracer.LogPath);
		}

		/// <summary>
		/// Appends the specified debug information to the specified message.
		/// </summary>
		/// <param name="message">The message to append the debugging information to.</param>
		/// <param name="debugInformation">The extra information to show in a debug build.</param>
		/// <remarks>Note the conditional compilation attribute which causes this method to not be called in release builds.</remarks>
		[Conditional("DEBUG")]
		public static void AppendDebugInformation(ref string message, string debugInformation)
		{
			message += " [DEBUG INFO]: " + debugInformation;
		}

		/// <summary>
		/// Canonicalizes the directory path to a normalized form.
		/// </summary>
		/// <param name="path">The directory path to canonicalize.</param>
		/// <returns>The canonicalized directory path.</returns>
		public static string CanonicalizeDirectoryPath(string path)
		{
			path = EnsureTrailingChar(path, Path.DirectorySeparatorChar);
			return CanonicalizeFilePath(path);
		}

		/// <summary>
		/// Canonicalizes the path to a normalized form.
		/// </summary>
		/// <param name="path">The path to canonicalize.</param>
		/// <returns>The canonicalized path.</returns>
		public static string CanonicalizeFilePath(string path)
		{
			Uri uri = new Uri(path);
			return uri.LocalPath;
		}

		/// <summary>
		/// Creates a native <see cref="CAUUID"/> structure by allocating the necessary COM memory for the array.
		/// </summary>
		/// <param name="guids">An array of GUIDs to return in the <b>CAUUID</b> structure.</param>
		/// <returns>A <see cref="CAUUID"/> structure filled in with the contents of <paramref name="guids"/>.</returns>
		public static CAUUID CreateCAUUIDFromGuidArray(Guid[] guids)
		{
			CAUUID cauuid = new CAUUID();

			if (guids != null)
			{
				cauuid.cElems = (uint)guids.Length;

				// Allocate the memory for the array of GUIDs
				int cbGuid = Marshal.SizeOf(typeof(Guid));
				cauuid.pElems = Marshal.AllocCoTaskMem(guids.Length * cbGuid);

				// Iterate over the GUID array and copy them into the COM memory
				IntPtr pCurrent = cauuid.pElems;
				for (int i = 0; i < guids.Length; i++)
				{
					// Copy the managed GUID structure to the COM memory block
					Marshal.StructureToPtr(guids[i], pCurrent, false);

					// Move the pointer to the next element
					pCurrent = new IntPtr(pCurrent.ToInt64() + cbGuid);
				}
			}

			return cauuid;
		}

		/// <summary>
		/// Performs an invariant-culture comparison of the two strings to see if the first string
		/// ends with the second string.
		/// </summary>
		/// <param name="s">The string to check.</param>
		/// <param name="value">The text to verify is at the end of <paramref name="s"/>.</param>
		/// <param name="comparisonType">One of the <see cref="StringComparison"/> values that determines how <paramref name="s"/> and <paramref name="value"/> are compared.</param>
		/// <returns>true if <paramref name="s"/> ends with <paramref name="value"/>; otherwise, false.</returns>
		public static bool EndsWith(string s, string value, StringComparison comparisonType)
		{
			if (s == null)
			{
				return false;
			}
			else if (value == null)
			{
				return true;
			}

			// In order to end with the value, the string has to be at least as long as it.
			if (s.Length < value.Length)
			{
				return false;
			}

			// Now do the comparison.
			return (String.Compare(s, s.Length - value.Length, value, 0, value.Length, comparisonType) == 0);
		}

		/// <summary>
		/// Adds the specified character to the start of the string if it doesn't already exist at the beginning.
		/// </summary>
		/// <param name="value">The string to add the character to.</param>
		/// <param name="charToEnsure">The character that will be at the start of the string upon return.</param>
		/// <returns>The original string with the specified character at the start.</returns>
		public static string EnsureLeadingChar(string value, char charToEnsure)
		{
			if (value[0] != charToEnsure)
			{
				value = charToEnsure + value;
			}
			return value;
		}

		/// <summary>
		/// Adds the specified character to the end of the string if it doesn't already exist at the end.
		/// </summary>
		/// <param name="value">The string to add the trailing character to.</param>
		/// <param name="charToEnsure">The character that will be at the end of the string upon return.</param>
		/// <returns>The original string with the specified character at the end.</returns>
		public static string EnsureTrailingChar(string value, char charToEnsure)
		{
			Tracer.VerifyStringArgument(value, "value");
			if (value[value.Length - 1] != charToEnsure)
			{
				value += charToEnsure;
			}
			return value;
		}

		/// <summary>
		/// Performs an invariant-culture, case-insensitive comparison on the two strings.
		/// </summary>
		/// <param name="string1">The first string to compare.</param>
		/// <param name="string2">The second string to compare.</param>
		/// <returns>true if the two strings are equal (case-insensitive).</returns>
		public static bool FileStringEquals(string string1, string string2)
		{
			bool differsInCase;
			return FileStringEquals(string1, string2, out differsInCase);
		}

		/// <summary>
		/// Performs an invariant-culture, case-insensitive comparison on the two strings.
		/// </summary>
		/// <param name="string1">The first string to compare.</param>
		/// <param name="string2">The second string to compare.</param>
		/// <param name="differsInCase">Returns a value indicating whether the two strings differ only in their case.</param>
		/// <returns>true if the two strings are equal (case-insensitive).</returns>
		public static bool FileStringEquals(string string1, string string2, out bool differsInCase)
		{
			if (String.Equals(string1, string2, StringComparison.InvariantCultureIgnoreCase))
			{
				differsInCase = !String.Equals(string1, string2, StringComparison.InvariantCulture);
				return true;
			}

			differsInCase = false;
			return false;
		}

		/// <summary>
		/// Attempts to format the specified string by calling <see cref="System.String.Format(IFormatProvider, string, object[])"/>.
		/// If a <see cref="FormatException"/> is raised, then <paramref name="format"/> is returned.
		/// </summary>
		/// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.</param>
		/// <param name="format">A string containing zero or more format items.</param>
		/// <param name="args">An object array containing zero or more objects to format.</param>
		/// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string equivalent of the corresponding instances of object in args.</returns>
		public static string SafeStringFormat(IFormatProvider provider, string format, params object[] args)
		{
			string formattedString = format;

			try
			{
				if (args != null && args.Length > 0)
				{
					formattedString = String.Format(provider, format, args);
				}
			}
			catch (FormatException)
			{
			}

			return formattedString;
		}

		/// <summary>
		/// Attempts to format the specified string by calling <c>System.PackageUtility.SafeStringFormatInvariant(format, args)</c>.
		/// If a <see cref="FormatException"/> is raised, then <paramref name="format"/> is returned.
		/// </summary>
		/// <param name="format">A string containing zero or more format items.</param>
		/// <param name="args">An object array containing zero or more objects to format.</param>
		/// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string equivalent of the corresponding instances of object in args.</returns>
		public static string SafeStringFormatInvariant(string format, params object[] args)
		{
			return SafeStringFormat(CultureInfo.InvariantCulture, format, args);
		}

		/// <summary>
		/// Performs an invariant-culture comparison of the two strings to see if the first string
		/// starts with the second string.
		/// </summary>
		/// <param name="s">The string to check.</param>
		/// <param name="value">The text to verify is at the beginning of <paramref name="s"/>.</param>
		/// <param name="comparisonType">One of the <see cref="StringComparison"/> values that determines how <paramref name="s"/> and <paramref name="value"/> are compared.</param>
		/// <returns>true if <paramref name="s"/> starts with <paramref name="value"/>; otherwise, false.</returns>
		public static bool StartsWith(string s, string value, StringComparison comparisonType)
		{
			if (s == null)
			{
				return false;
			}
			else if (value == null)
			{
				return true;
			}

			// In order to start with the value, the string has to be at least as long as it.
			if (s.Length < value.Length)
			{
				return false;
			}

			// Now do the comparison.
			return (String.Compare(s, 0, value, 0, value.Length, comparisonType) == 0);
		}

		/// <summary>
		/// Verifies that the two objects represent the same instance of an object.
		/// </summary>
		/// <param name="object1">Can be an object, interface or IntPtr</param>
		/// <param name="object2">Can be an object, interface or IntPtr</param>
		/// <returns>True if the 2 items represent the same thing</returns>
		/// <remarks>
		/// This essentially compares the IUnkown pointers of the two objects.
		/// It is needed in scenarios where aggregation is involved.
		/// This method is taken almost verbatim from the VS SDK.
		/// </remarks>
		public static bool IsSameComObject(object object1, object object2)
		{
			bool isSame = false;
			IntPtr unknown1 = IntPtr.Zero;
			IntPtr unknown2 = IntPtr.Zero;

			if (object1 == null || object2 == null)
			{
				return (Object.ReferenceEquals(object1, object2));
			}

			try
			{
				unknown1 = QueryInterfaceIUnknown(object1);
				unknown2 = QueryInterfaceIUnknown(object2);

				isSame = IntPtr.Equals(unknown1, unknown2);
			}
			finally
			{
				if (unknown1 != IntPtr.Zero)
				{
					Marshal.Release(unknown1);
				}

				if (unknown2 != IntPtr.Zero)
				{
					Marshal.Release(unknown2);
				}
			}

			return isSame;
		}

		/// <summary>
		/// Returns a value indicating whether the specified path is relative to the specified base path.
		/// </summary>
		/// <param name="basePath">The path from which to test for relativity.</param>
		/// <param name="pathToTest">The path to test if it is relative to the base path.</param>
		/// <returns>true if <paramref name="pathToTest"/> is relative to <paramref name="basePath"/>.</returns>
		public static bool IsRelative(string basePath, string pathToTest)
		{
			basePath = CanonicalizeFilePath(basePath);
			pathToTest = CanonicalizeFilePath(pathToTest);
			return StartsWith(pathToTest, basePath, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Returns a value indicating whether the specified name is a valid file or folder name
		/// (i.e. it does not contain any illegal characters and is not blank).
		/// </summary>
		/// <param name="name">The name to check.</param>
		/// <returns>true if <paramref name="name"/> is a valid file or folder name; otherwise, false.</returns>
		public static bool IsValidFileOrFolderName(string name)
		{
			// Check for null or empty values.
			if (name == null)
			{
				return false;
			}

			if (name.Length == 0)
			{
				return false;
			}

			// Check for any invalid characters and make sure the whole string isn't just '.' or ' ' characters.
			bool allDots = true;
			bool allSpaces = true;
			foreach (char c in name)
			{
				foreach (char invalidChar in InvalidFileCharacters)
				{
					if (c == invalidChar)
					{
						return false;
					}
				}

				if (c != '.')
				{
					allDots = false;
				}

				if (c != ' ')
				{
					allSpaces = false;
				}
			}

			return !allDots && !allSpaces;
		 }

		/// <summary>
		/// Makes a path relative to a base path.
		/// </summary>
		/// <param name="basePath">The path from which to make a relative path.</param>
		/// <param name="pathToMakeRelative">The path that will be made relative to <paramref name="basePath"/></param>
		/// <returns>The relative path.</returns>
		public static string MakeRelative(string basePath, string pathToMakeRelative)
		{
			Uri baseUri = new Uri(basePath);
			Uri relativeUri = new Uri(pathToMakeRelative);
#if USE_NET20_FRAMEWORK
			relativeUri = baseUri.MakeRelativeUri(relativeUri);
			string relativePath = relativeUri.ToString();
#else
			string relativePath = baseUri.MakeRelative(relativeUri);
#endif
			return relativePath.Replace("/", @"\");
		}

		/// <summary>
		/// Adds quote characters around the string only if the string has embedded spaces.
		/// </summary>
		/// <param name="stringToQuote">The path to quote.</param>
		/// <returns>The quoted path.</returns>
		public static string QuoteString(string stringToQuote)
		{
			if (stringToQuote.IndexOf(' ') >= 0)
			{
				return "\"" + stringToQuote + "\"";
			}
			return stringToQuote;
		}

		/// <summary>
		/// Strips the specified character from the front of the specified string if it exists.
		/// If the character is not the first character, the original string is returned.
		/// </summary>
		/// <param name="value">The string to strip.</param>
		/// <param name="charToStrip">The character to strip if it exists.</param>
		/// <returns>The stripped string or the original string if the character doesn't
		/// exist at the front of the string.</returns>
		public static string StripLeadingChar(string value, char charToStrip)
		{
			if (value != null && value.Length > 0 && value[0] == charToStrip)
			{
				return value.Substring(1);
			}
			return value;
		}

		/// <summary>
		/// Strips the specified character from the end of the specified string if it exists.
		/// If the character is not the last character, then original string is returned.
		/// </summary>
		/// <param name="value">The string to strip.</param>
		/// <param name="charToStrip">The character to strip if it exists.</param>
		/// <returns>The stripped string or the original string if the character doesn't
		/// exist at the end of the string.</returns>
		public static string StripTrailingChar(string value, char charToStrip)
		{
			if (value != null && value.Length > 0 && value[value.Length - 1] == charToStrip)
			{
				return value.Substring(0, value.Length - 1);
			}
			return value;
		}

		/// <summary>
		/// Recursively copies all of the files in the source directory to the target directory.
		/// </summary>
		/// <param name="sourceDirectory">The source directory.</param>
		/// <param name="targetDirectory">The target directory.</param>
		public static void XCopy(string sourceDirectory, string targetDirectory)
		{
			XCopyOrMove(sourceDirectory, targetDirectory, false);
		}

		/// <summary>
		/// Recursively moves all of the files in the source directory to the target directory.
		/// </summary>
		/// <param name="sourceDirectory">The source directory.</param>
		/// <param name="targetDirectory">The target directory.</param>
		public static void XMove(string sourceDirectory, string targetDirectory)
		{
			XCopyOrMove(sourceDirectory, targetDirectory, true);
		}

		/// <summary>
		/// Retrieve the IUnknown interface pointer for the managed or COM object passed in.
		/// </summary>
		/// <param name="objectToQuery">Managed or COM object to get an IUnknown interface pointer for.</param>
		/// <returns>Pointer to the IUnknown interface of the object.</returns>
		/// <remarks>This method is taken almost verbatim from the VS SDK.</remarks>
		private static IntPtr QueryInterfaceIUnknown(object objectToQuery)
		{
			bool releaseIt = false;
			IntPtr unknown = IntPtr.Zero;
			IntPtr result;

			try
			{
				if (objectToQuery is IntPtr)
				{
					unknown = (IntPtr)objectToQuery;
				}
				else
				{
					// This is a managed object (or RCW)
					unknown = Marshal.GetIUnknownForObject(objectToQuery);
					releaseIt = true;
				}

				// We might already have an IUnknown, but if this is an aggregated
				// object, it may not be THE IUnknown until we QI for it.
				Guid IID_IUnknown = NativeMethods.IID_IUnknown;
				NativeMethods.ThrowOnFailure(Marshal.QueryInterface(unknown, ref IID_IUnknown, out result));
			}
			finally
			{
				if (releaseIt && unknown != IntPtr.Zero)
				{
					Marshal.Release(unknown);
				}
			}

			return result;
		}

		/// <summary>
		/// Recursively copies or moves all of the files in the source directory to the target directory.
		/// </summary>
		/// <param name="sourceDirectory">The source directory.</param>
		/// <param name="targetDirectory">The target directory.</param>
		/// <param name="move">Specifies whether to move the files or just copy them.</param>
		private static void XCopyOrMove(string sourceDirectory, string targetDirectory, bool move)
		{
			if (!Directory.Exists(sourceDirectory))
			{
				return;
			}

			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			// Copy all of the files
			foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
			{
				string destFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
				if (!File.Exists(destFile))
				{
					File.Copy(sourceFile, destFile, false);
					if (move)
					{
						// Remove any read-only flags.
						File.SetAttributes(sourceFile, FileAttributes.Normal);
						File.Delete(sourceFile);
					}
				}
			}

			// Recursively move all of the files in the sub-directories
			foreach (string subDirectory in Directory.GetDirectories(sourceDirectory))
			{
				string targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(subDirectory));
				XCopyOrMove(subDirectory, targetSubDirectory, move);
			}
		}
		#endregion
	}
}
