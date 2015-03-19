// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwUtils.cs
// Responsibility: TE Team

using System.IO;
#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#endif
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
		/// The name of the Language Explorer folder (Even though this is the same as
		/// FwDirectoryFinder.ksFlexFolderName and FwSubKey.LexText, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksFlexAppName = "Language Explorer";
		/// <summary>
		/// The fully-qualified (with namespace) C# object name for LexTextApp
		/// </summary>
		public const string ksFullFlexAppObjectName = "SIL.FieldWorks.XWorks.LexText.LexTextApp";
		/// <summary>
		/// The current version of FieldWorks. This is also known in COMInterfaces/IcuWrappers.cs, InitIcuDataDir.
		/// </summary>
		public const int SuiteVersion = 9;

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

#if __MonoCS__
		// On Linux, the default string output does not choose a font based on the characters in
		// the string, but on the current user interface locale.  At times, we want to display,
		// for example, Korean when the user interface locale is English.  By default, this
		// nicely displays boxes instead of characters.  Thus, we need some way to obtain the
		// correct font in such situations.  See FWNX-1069 for the obvious place this is needed.

		// These constants and enums are borrowed from fontconfig.h
		const string FC_FAMILY = "family";			/* String */
		const string FC_LANG = "lang";				/* String - RFC 3066 langs */
		enum FcMatchKind
		{
			FcMatchPattern,
			FcMatchFont,
			FcMatchScan
		};
		enum FcResult
		{
			FcResultMatch,
			FcResultNoMatch,
			FcResultTypeMismatch,
			FcResultNoId,
			FcResultOutOfMemory
		}
		[DllImport ("libfontconfig.so.1")]
		static extern IntPtr FcPatternCreate();
		[DllImport ("libfontconfig.so.1")]
		static extern int FcPatternAddString(IntPtr pattern, string fcObject, string stringValue);
		[DllImport ("libfontconfig.so.1")]
		static extern void FcDefaultSubstitute(IntPtr pattern);
		[DllImport ("libfontconfig.so.1")]
		static extern void FcPatternDestroy(IntPtr pattern);
		[DllImport ("libfontconfig.so.1")]
		static extern int FcConfigSubstitute(IntPtr config, IntPtr pattern, FcMatchKind kind);
		[DllImport ("libfontconfig.so.1")]
		static extern IntPtr FcFontMatch(IntPtr config, IntPtr pattern, out FcResult result);
		// Note that the output string from this method must NOT be freed, so we have to play games
		// with deferred marshalling.
		[DllImport ("libfontconfig.so.1")]
		static extern FcResult FcPatternGetString(IntPtr pattern, string fcObject, int index, out IntPtr stringValue);

		/// <summary>
		/// Store the mapping from language to font to save future computation.
		/// </summary>
		static Dictionary<string, string> m_mapLangToFont = new Dictionary<string, string>();

		/// <summary>
		/// Get the name of the default font for the given language tag.
		/// </summary>
		/// <param name="lang">ISO 3066 tag for the language (e.g., "en", "es", "zh-CN", etc.)</param>
		/// <returns>Name of the font, or <c>null</c> if not found.</returns>
		public static string GetFontNameForLanguage(string lang)
		{
			string fontName = null;
			if (m_mapLangToFont.TryGetValue(lang, out fontName))
				return fontName;
			IntPtr pattern = FcPatternCreate();
			int isOk = FcPatternAddString(pattern, FC_LANG, lang);
			if (isOk == 0)
			{
				FcPatternDestroy(pattern);
				return null;
			}
			isOk = FcConfigSubstitute(IntPtr.Zero, pattern, FcMatchKind.FcMatchPattern);
			if (isOk == 0)
			{
				FcPatternDestroy(pattern);
				return null;
			}
			FcDefaultSubstitute(pattern);
			FcResult result;
			IntPtr fullPattern = FcFontMatch(IntPtr.Zero, pattern, out result);
			if (result != FcResult.FcResultMatch)
			{
				FcPatternDestroy(pattern);
				FcPatternDestroy(fullPattern);
				return null;
			}
			FcPatternDestroy(pattern);
			IntPtr res;
			FcResult fcRes = FcPatternGetString(fullPattern, FC_FAMILY, 0, out res);
			if (fcRes == FcResult.FcResultMatch)
			{
				fontName = Marshal.PtrToStringAuto(res);
				m_mapLangToFont.Add(lang, fontName);
			}
			FcPatternDestroy(fullPattern);
			return fontName;
		}
#endif

		/// <summary>
		/// Whenever possible use this in place of new PalasoWritingSystemManager.
		/// It sets the TemplateFolder, which unfortunately the constructor cannot do because
		/// the direction of our dependencies does not allow it to reference FwUtils and access FwDirectoryFinder.
		/// </summary>
		/// <returns></returns>
		public static WritingSystemManager CreateWritingSystemManager()
		{
			return new WritingSystemManager {TemplateFolder = FwDirectoryFinder.TemplateDirectory};
		}

	}
}
