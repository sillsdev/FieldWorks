// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WordImporter.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// Implements WordImporter
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// a class for parsing text files and populating WfiWordSets with them.
	/// </summary>
	public class WordImporter : IFWDisposable
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
			m_lgCharPropEngineVern = (ILgCharacterPropertyEngine)
				m_cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				m_cache.LangProject.DefaultVernacularWritingSystem);
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~WordImporter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			if (m_lgCharPropEngineVern != null)
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_lgCharPropEngineVern);
				m_lgCharPropEngineVern = null;
			}

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public void PopulateWordset(string path, IWfiWordSet wordSet)
		{
			CheckDisposed();

			// Note: The ref collection class ensures duplicates are not added to CasesRC.
			wordSet.CasesRC.Add(GetWordformsInFile(path));
		}

		// NB: This will currently return hvos
		private int[] GetWordformsInFile(string path)
		{
			// This is technically a Set, but we don't need to speed of the Set,
			// so the List is good enough.
			List<int> hvos = new List<int>();
			// Note: The GetUniqueWords uses the key for faster processing,
			// even though we don;t use it here.
			Dictionary<string, IWfiWordform> wordforms = new Dictionary<string, IWfiWordform>();
			System.IO.TextReader reader = null;
			try
			{
				reader = File.OpenText(path);
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
			finally
			{
				if (reader != null)
				{
					reader.Close();
					reader = null;
				}
			}
			foreach (IWfiWordform wf in wordforms.Values)
				hvos.Add(wf.Hvo);
			return (hvos.ToArray());
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
						wordforms.Add(word, WfiWordform.FindOrCreateWordform(m_cache, word, m_ws));
					length = 0;
					start = -1;
				}
			}
		}
	}
}
