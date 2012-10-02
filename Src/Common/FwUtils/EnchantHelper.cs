using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Enchant;
using SIL.FieldWorks.Common.COMInterfaces;

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
		public static Enchant.Dictionary GetDictionary(int ws, SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystemFactory wsf)
		{
			string dictId = DictionaryId(ws, wsf);
			if (dictId == null)
				return null;
			Dictionary dict = Enchant.Broker.Default.RequestDictionary(dictId);
			IWritingSystem engine = wsf.get_EngineOrNull(ws);
			if (engine == null)
				return dict; // should never happen? Can't verify ID so go ahead and return it.
			if (dict.Information.Language == engine.SpellCheckDictionary)
				return dict; // exact match
			if (IsPrivateDictionary(dict.Information.Language))
				return null; // private dictionaries may only be returned when matching exactly.
			return dict;
		}

		/// <summary>
		/// Return the string which should be used to request a dictionary for the specified writing system,
		/// or null if none will work.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		private static string DictionaryId(int ws, SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystemFactory wsf)
		{
			IWritingSystem wsEngine = wsf.get_EngineOrNull(ws);
			if (wsEngine == null)
				return null;
			string wsId = wsEngine.SpellCheckDictionary;
			if (String.IsNullOrEmpty(wsId) || wsId == "<None>")
				return null;
			if (Enchant.Broker.Default.DictionaryExists(wsId))
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
		public static bool DictionaryExists(int ws, SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystemFactory wsf)
		{
			return GetDictionary(ws, wsf) != null;
		}

		/// <summary>
		/// Ensure that the spelling dictionary (if any) for the specified ws will give the specified
		/// answer regarding the specified word.
		/// </summary>
		public static void SetSpellingStatus(string word, int ws,
			SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystemFactory wsf, bool fCorrect)
		{
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
		/// </summary>
		public static Dictionary EnsureDictionary(int ws, string icuLocale, ILgWritingSystemFactory wsf)
		{
			Enchant.Dictionary result = EnchantHelper.GetDictionary(ws, wsf);
			if (result != null)
				return result;
			string dirPath = GetSpellingDirectoryPath();
			if (!System.IO.Directory.Exists(dirPath))
				System.IO.Directory.CreateDirectory(dirPath);
			string dicPath = GetDicPath(dirPath, icuLocale);
			System.IO.TextWriter writer = System.IO.File.CreateText(System.IO.Path.ChangeExtension(dicPath, ".aff"));
			writer.WriteLine("SET UTF-8");
			writer.Close();
			if (!System.IO.File.Exists(dicPath))
			{
				// If it already exists, probably we disabled it by deleting the .aff file--an approach we
				// no longer use; re-creating it should reinstate it.
				writer = System.IO.File.CreateText(dicPath);
				writer.WriteLine("0");
				writer.Close();
			}
			// Apparently, although the broker will find the new dictionary when asked for it explicitly,
			// it doesn't appear in the list of possible dictionaries (Enchant.Broker.Default.Dictionaries)
			// which is used to populate the spelling dictionary combo box in the writing system dialog
			// unless we dispose the old broker (which causes a new one to be created).
			// Note: I (JohnT) have a vague recollection that disposing the broker can cause problems for
			// any existing dictionaries we hang on to. So don't dispose it more than necessary.
			Enchant.Broker.Default.Dispose();
			// Now it should exist!
			return EnchantHelper.GetDictionary(ws, wsf);
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
			if (!File.Exists(path))
				return false;
			return new FileInfo(path).Length <= 3;
		}

		/// <summary>
		/// Get the path for the dictionary for a particular locale, if it is one of our private ones,
		/// given the path to the directory where we make them and the icuLocale.
		/// </summary>
		/// <param name="dirPath"></param>
		/// <param name="icuLocale"></param>
		/// <returns></returns>
		private static string GetDicPath(string dirPath, string icuLocale)
		{
			string filePath = System.IO.Path.Combine(dirPath, icuLocale);
			return System.IO.Path.ChangeExtension(filePath, ".dic");
		}

		/// <summary>
		/// Locates the directory in which we make our private dictionaries built from the WordformInventory.
		/// </summary>
		private static string GetSpellingDirectoryPath()
		{
			string appdataFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
			return System.IO.Path.Combine(appdataFolder, @"enchant\myspell\");
		}
	}
}
