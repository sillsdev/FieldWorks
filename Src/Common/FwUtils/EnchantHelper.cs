// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2004' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EnchantHelper.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Enchant;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class provides a very low-level common place to put code we want shared by
	/// everything using Enchant (except C++ code).
	/// </summary>
	public class EnchantHelper
	{
		/// <summary>
		/// Get a dictionary for the specified writing system, or null if we don't know of one.
		/// We ideally want a dictionary that exactly matches the specified writing system, that is,
		/// dict.Information.Language == the SpellCheckDictionary of the writing system.
		/// If we can't find such a dictionary, for major languages (those that have non-trivial base dictionary files),
		/// we will return a dictionary that shares a prefix, for example, 'en' when looking for 'en_US' or vice versa.
		/// This is not allowed for minority languages (where the dictionary is one we created ourselves that is empty,
		/// and all the spelling information is in the overrides kept by Enchant); we return null if we can't find
		/// an exact match or an approximate match that is a 'major' language dictionary.
		/// Note: a similar algorithm is unfortunately implemented in VwRootBox::GetDictionary
		/// and in WritingSystemPropertiesDialog.PopulateSpellingDictionaryComboBox.
		/// </summary>
		public static Enchant.Dictionary GetDictionary(int ws, ILgWritingSystemFactory wsf)
		{
			string dictId = DictionaryId(ws, wsf);
			if (dictId == null)
				return null;
			Dictionary dict = GetDict(dictId);
			if (dict == null)
				return null;

			ILgWritingSystem engine = wsf.get_EngineOrNull(ws);
			if (engine == null)
				return dict; // should never happen? Can't verify ID so go ahead and return it.
			if (dict.Information.Language == engine.SpellCheckingId)
				return dict; // exact match
			if (IsPrivateDictionary(dict.Information.Language))
				return null; // private dictionaries may only be returned when matching exactly.
			return dict;
		}

		internal static Dictionary GetDict(string dictId)
		{
			Dictionary dict = null;
			// See FWR-1830. It seems sometimes Enchant gets into a state where it can't return a dictionary,
			// though normally it can for this ID, and it just said it could.
			// In case this is race condition or other timing-related problem, which it seems to be, try a few
			// times.
			int retries = 10;
			while (dict == null && retries-- > 0)
			{
				try
				{
					dict = Enchant.Broker.Default.RequestDictionary(dictId);
				}
				catch (ApplicationException ex)
				{
					Debug.WriteLine("Enchant.Broker.RequestDictionary threw " + ex.Message);
					System.Threading.Thread.Sleep(100);
				}
			}
			return dict;
		}

		// The raw id that should be used to create a dictionary for the given WS, if none exists.
		private static string RawDictionaryId(int ws, ILgWritingSystemFactory wsf)
		{
			ILgWritingSystem wsEngine = wsf.get_EngineOrNull(ws);
			if (wsEngine == null)
				return null;
			string wsId = wsEngine.SpellCheckingId;
			if (String.IsNullOrEmpty(wsId))
				return wsEngine.Id.Replace('-', '_'); // Enchant does not allow hyphen; that is OK since lang ID does not allow underscore.
			if (wsId == "<None>")
				return null;
			return wsId;
		}

		static Dictionary<string, bool> s_existingDictionaries = new Dictionary<string, bool>();

		/// <summary>
		/// LT-11603 was traced in part to calls to Dictionary.Exists which never return, possibly because
		/// the OS has become confused and thinks the file is locked. To guard against this we run the test in a background
		/// thread with a timeout. Not sure whether this is enough protection, other calls may be at risk also, but if
		/// we can confirm the dictionary exists at least it isn't spuriously locked when we first consider it.
		/// </summary>
		private static bool DictionaryExists(string wsId)
		{
			bool result;
			if (s_existingDictionaries.TryGetValue(wsId, out result))
				return result;
			var tester = new ExistsTester() {WsId = wsId};
			var newThread = new Thread(tester.Test);
			newThread.Start();
			if (newThread.Join(4000))
				result = tester.Result;
			else
			{
				result = false;
				MessageBox.Show(String.Format(FwUtilsStrings.kstIdCantDoDictExists, wsId), FwUtilsStrings.kstidCantDoDictExistsCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			s_existingDictionaries[wsId] = result;
			return result;
		}

		class ExistsTester
		{
			public bool Result;
			public string WsId;

			public void Test()
			{
				Result = Broker.Default.DictionaryExists(WsId);
			}
		}

		/// <summary>
		/// Return the string which should be used to request a dictionary for the specified writing system,
		/// or null if none will work.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		private static string DictionaryId(int ws, ILgWritingSystemFactory wsf)
		{
			string wsId = RawDictionaryId(ws, wsf);
			if (String.IsNullOrEmpty(wsId))
				return null;
			if (DictionaryExists(wsId))
			{
				return wsId;
			}

			// If no dictionary exists which matches the language name exactly then
			// search for one.
			//
			// Enchant.Broker.Default.Dictionaries is a list of the dictionaries found in
			// C:\Documents and Settings\USERNAME\Application Data\enchant\myspell
			// followed by the dictionaries found in Open Office.
			// C:\Program Files\OpenOffice.org 2.4\share\dict\ooo
			// The Views code is also programmed to find the first match.
			foreach (Enchant.DictionaryInfo info in Enchant.Broker.Default.Dictionaries)
			{
				if (info.Language.StartsWith(wsId))
					return info.Language;
			}
			return null;
		}

		/// <summary>
		/// Returns true exactly if GetDictionary() with the same arguments will retrieve a dictionary (rather than null).
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		public static bool DictionaryExists(int ws, ILgWritingSystemFactory wsf)
		{
			return GetDictionary(ws, wsf) != null;
		}

		/// <summary>
		/// Ensure that the spelling dictionary (if any) for the specified ws will give the specified
		/// answer regarding the specified word.
		/// </summary>
		public static void SetSpellingStatus(string word, int ws,
			ILgWritingSystemFactory wsf, bool fCorrect)
		{
			//I'm not sure why we want to avoid keeping references to Enchant Dictionaries around
			//but since we are doing this here and  in WfiWordFormServices any object which holds onto
			//a dictionary reference must deal with the possibility that it will be disposed out from under them.
			// -naylor 9-2011
			using (Enchant.Dictionary dict = GetDictionary(ws, wsf))
			{
				if (dict == null)
					return; // no spelling dict to update.
				SetSpellingStatus(word, fCorrect, dict);
			}
		}

		/// <summary>
		/// Ensure that the specified spelling dictionary will give the specified answer regarding the specified word.
		/// </summary>
		public static void SetSpellingStatus(string word, bool fCorrect, Dictionary dict)
		{
			if (fCorrect)
			{
				if (!dict.Check(word))
					dict.Add(word);
			}
			else
			{
				if (dict.Check(word))
					dict.Remove(word);
			}
		}

		/// <summary>
		/// Make sure that a dictionary exists for the specified writing system.
		/// Currently this will NOT do so if its spelling ID is set to None (in angle brackets).
		/// Callers may want to include code like this:
		/// 			    var wsObj = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
		///        if (wsObj.SpellCheckingId == "None") // add angle brackets around None
		///           wsObj.SpellCheckingId = wsObj.Id.Replace('-', '_');
		/// Enhance JohnT: maybe code like that should be part of this method? But it is meant to
		/// just make sure the dictionary exists, once the right SpellCheckingId has been established.
		/// </summary>
		public static Dictionary EnsureDictionary(int ws, ILgWritingSystemFactory wsf)
		{
			Dictionary result = GetDictionary(ws, wsf);
			if (result != null)
				return result;
			string dictId = RawDictionaryId(ws, wsf);
			if (dictId == null)
				return null; // No Dictionary ID set. Caller has probably messed up, but we can't fix it here.
			EnsureDictionary(dictId);
			// Now it should exist!
			return GetDictionary(ws, wsf);
		}

		internal static void EnsureDictionary(string dictId)
		{
			string dirPath = GetSpellingDirectoryPath();
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);
			string dicPath = GetDicPath(dirPath, dictId);
			using (var writer = FileUtils.OpenFileForWrite(Path.ChangeExtension(dicPath, ".aff"), Encoding.UTF8))
			writer.WriteLine("SET UTF-8");
			if (!FileUtils.FileExists(dicPath))
			{
				// If it already exists, probably we disabled it by deleting the .aff file--an approach we
				// no longer use; re-creating it should reinstate it.
				using (var writer = FileUtils.OpenFileForWrite(dicPath, Encoding.UTF8))
				writer.WriteLine("0");
			}
			// Apparently, although the broker will find the new dictionary when asked for it explicitly,
			// it doesn't appear in the list of possible dictionaries (Enchant.Broker.Default.Dictionaries)
			// which is used to populate the spelling dictionary combo box in the writing system dialog
			// unless we dispose the old broker (which causes a new one to be created).
			// Note: I (JohnT) have a vague recollection that disposing the broker can cause problems for
			// any existing dictionaries we hang on to. So don't dispose it more than necessary.
			Broker.Default.Dispose();
			s_existingDictionaries.Clear();
		}

		/// <summary>
		/// Return true if we have a dictionary for the specified locale that is private to FieldWorks,
		/// that is, we made it from the wordform inventory. Such dictionaries may not be used for a
		/// writing system unless their name exactly matches the IcuLocale.
		/// If the ".dic" file was created by our code, it contains only "0" (plus crlf) so should be only
		/// 3 bytes long.
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <returns></returns>
		public static bool IsPrivateDictionary(string icuLocale)
		{
			string path = GetDicPath(GetSpellingDirectoryPath(), icuLocale);
			 if (!FileUtils.FileExists(path))
				return false;
			return new FileInfo(path).Length <= 3;
		}

		/// <summary>
		/// When a FieldWorks user adds a valid spelling for a word for a language
		/// then the word is added to the override *.dic file.
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <returns>return true if the file has at least one word in it.</returns>
		public static bool OverrideSpellingsExist(string icuLocale)
		{
			string path = GetDicPath(GetSpellingOverridesDirectory(), icuLocale);
			if (!FileUtils.FileExists(path))
				return false;
			return new FileInfo(path).Length > 0;
		}

		/// <summary>
		/// Get the path for the dictionary for a particular locale, if it is one of our private ones,
		/// given the path to the directory where we make them and the icuLocale.
		/// </summary>
		/// <param name="dirPath"></param>
		/// <param name="icuLocale"></param>
		/// <returns></returns>
		internal static string GetDicPath(string dirPath, string icuLocale)
		{
			string filePath = Path.Combine(dirPath, icuLocale);
			return Path.ChangeExtension(filePath, ".dic");
		}

		/// <summary>
		/// Return a List of the Enchant Dictionary
		/// </summary>
		/// <returns>Return a List of the Enchant Dictionary</returns>
		public static IEnumerable<String> GetEnchantDictionaryList()
		{
			var enchantDictionaries = new List<String>();

			foreach (var info in Enchant.Broker.Default.Dictionaries)
			{
				enchantDictionaries.Add(info.Language);
			}
			return enchantDictionaries;
		}

		/// <summary>
		/// Locates the directory in which we make our private dictionaries built from the WordformInventory.
		/// </summary>
		internal static string GetSpellingDirectoryPath()
		{
			string appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path.Combine(appdataFolder, @"enchant\myspell\");
		}

		/// <summary>
		/// Locates the directory in which Enchant puts spelling override files for dictionaries.
		/// </summary>
		internal static string GetSpellingOverridesDirectory()
		{
			string appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path.Combine(appdataFolder, @"enchant");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="icuLocale">icuLocale for a writing system.</param>
		/// <returns>If a dictionary file exists for the writing system then return the path, otherwise return null.</returns>
		public static string GetOverrideDictionaryPath(string icuLocale)
		{
			string filePath = GetDicPath(GetSpellingOverridesDirectory(), icuLocale);
			if (!FileUtils.FileExists(filePath))
				return null;
			return filePath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the replace spelling override file, replacing any exsting one.
		/// </summary>
		/// <param name="sourceFile">Enchant Spelling Additions dictionary file we want to put
		/// in the enchant folder</param>
		/// <exception cref="ArgumentException">sourceFile contains one or more of the invalid
		/// characters defined in GetInvalidPathChars</exception>
		/// <exception cref="ArgumentNullException">sourceFile is null</exception>
		/// <exception cref="IOException">File IO problem</exception>
		/// ------------------------------------------------------------------------------------
		public static void AddReplaceSpellingOverrideFile(string sourceFile)
		{
			var dictionaryFile = Path.GetFileName(sourceFile);
			var spellingOverridesDirectory = GetSpellingOverridesDirectory();
			Directory.CreateDirectory(spellingOverridesDirectory); // just possibly on a new machine may not exist.
			var enchantDictionaryFilePath = Path.Combine(spellingOverridesDirectory, dictionaryFile);
			File.Copy(sourceFile, enchantDictionaryFilePath, true);
		}
	}
}
