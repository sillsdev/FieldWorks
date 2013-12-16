// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: WordImporter.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// Implements WordImporter
// </remarks>

using System;
using System.IO;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// a class for parsing text files and populating WfiWordSets with them.
	/// </summary>
	public class WordImporter
	{
		private ILgCharacterPropertyEngine m_lgCharPropEngineVern;
		private FdoCache m_cache;
		private int m_ws;

		public WordImporter(FdoCache cache)
		{
			m_cache = cache;
			m_ws = cache.DefaultVernWs;

			//the following comment is from the FDO Scripture class, so the answer may appear there.

			// Get a default character property engine.
			// REVIEW SteveMc(TomB): We need the cpe for the primary vernacular writing system. What
			// should we be passing as the second param (i.e., the old writing system)? For now,
			// 0 seems to work.
			m_lgCharPropEngineVern = m_cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(m_cache.DefaultVernWs);
		}

		public void PopulateWordset(string path, IWfiWordSet wordSet)
		{
			// Note: The ref collection class ensures duplicates are not added to CasesRC.
			foreach (var wordform in GetWordformsInFile(path))
				wordSet.CasesRC.Add(wordform);
		}

		// NB: This will currently return hvos
		private IEnumerable<IWfiWordform> GetWordformsInFile(string path)
		{
			// Note: The GetUniqueWords uses the key for faster processing,
			// even though we don;t use it here.
			var wordforms = new Dictionary<string, IWfiWordform>();
			using (TextReader reader = File.OpenText(path))
			{
// do timing tests; try doing readtoend() to one string and then parsing that; see if more efficient
				// While there is a line to be read in the file
				// REVIEW: can this be broken by a very long line?
				// RR answer: Yes it can.
				// According to the ReadLine docs an ArgumentOutOfRangeException
				// exception will be thrown if the line length exceeds the MaxValue constant property of an Int32,
				// which is 2,147,483,647. I doubt you will run into this exception any time soon. :-)
				string line;
				while((line = reader.ReadLine()) != null)
					GetUniqueWords(wordforms, line);
			}
			return wordforms.Values;
		}

		/// <summary>
		/// Collect up a set of unique WfiWordforms.
		/// </summary>
		/// <param name="wfi"></param>
		/// <param name="ws"></param>
		/// <param name="wordforms">Table of unique wordforms.</param>
		/// <param name="buffer"></param>
		private void GetUniqueWords(Dictionary<string, IWfiWordform> wordforms, string buffer)
		{
			int start = -1; // -1 means we're still looking for a word to start.
			int length = 0;
			int totalLengh = buffer.Length;
			for(int i = 0; i < totalLengh; i++)
			{
				bool isWordforming = m_lgCharPropEngineVern.get_IsWordForming(buffer[i]);
				if (isWordforming)
				{
					length++;
					if (start < 0) //first character in this word?
						start = i;
				}

				if ((start > -1) // had a word and found yet?
					 && (!isWordforming || i == totalLengh - 1 /*last char of the input*/))
				{
					string word = buffer.Substring(start, length);
					if (!wordforms.ContainsKey(word))
					{
						var tss = m_cache.TsStrFactory.MakeString(word, m_ws);
						wordforms.Add(word, WfiWordformServices.FindOrCreateWordform(m_cache, tss));
					}
					length = 0;
					start = -1;
				}
			}
		}
	}
}
