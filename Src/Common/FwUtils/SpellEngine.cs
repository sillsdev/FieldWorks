using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.FieldWorks.Common.FwUtils
{
	internal class SpellEngine : ISpellEngine, IDisposable
	{
		/// <summary>
		/// File of exceptions (for a vernacular dictionary, may be the whole dictionary).
		/// Words are added by appearing, each on a line
		/// Words beginning * are known bad words.
		/// </summary>
		internal string ExceptionPath { get; set; }

		internal SpellEngine(string affixPath, string dictPath, string exceptionPath)
		{
			try
			{
				m_hunspellHandle = hunspell_initialize(affixPath, dictPath);
				ExceptionPath = exceptionPath;
				if (File.Exists(exceptionPath))
				{
					using (var reader = new StreamReader(exceptionPath, Encoding.UTF8))
					{
						string line;
						while ((line = reader.ReadLine()) != null)
						{
							var item = line;
							bool correct = true;
							if (item.Length > 0 && item[0] == '*')
							{
								correct = false;
								item = item.Substring(1);
							}
							SetInternalStatus(item, correct);
						}
					}
				}
			}
			catch (Exception)
			{
				if (m_hunspellHandle != IntPtr.Zero)
				{
					hunspell_uninitialize(m_hunspellHandle);
					m_hunspellHandle = IntPtr.Zero;
					throw;
				}
			}

		}

		private IntPtr m_hunspellHandle;
		//private Hunspell m_dict;
		public bool Check(string word)
		{
			return hunspell_spell(m_hunspellHandle, word) != 0;
		}

		private bool m_isVernacular;
		private bool m_fGotIsVernacular;
		/// <summary>
		/// A dictionary is considered vernacular if it contains our special word. That is, we presume it is one
		/// we created and can rewrite if we choose; and it should not be used for any dictionary ID that is
		/// not an exact match.
		/// </summary>
		public bool IsVernacular
		{
			get
			{
				if (!m_fGotIsVernacular)
				{
					m_isVernacular = Check(SpellingHelper.PrototypeWord);
					m_fGotIsVernacular = true;
				}
				return m_isVernacular;
			}
		}

		/// <summary>
		/// Get a list of suggestions for alternate words to use in place of the mis-spelled one.
		/// </summary>
		/// <param name="badWord"></param>
		/// <returns></returns>
		public ICollection<string> Suggest(string badWord)
		{
			IntPtr pointerToAddressStringArray;
			int resultCount = hunspell_suggest(m_hunspellHandle, badWord, out pointerToAddressStringArray);
			var results = MarshalUnmananagedStrArray2ManagedStrArray(pointerToAddressStringArray, resultCount);
			hunspell_free_list(m_hunspellHandle, ref pointerToAddressStringArray, resultCount);
			return results;
		}

		public void SetStatus(string word, bool isCorrect)
		{
			if (Check(word) == isCorrect)
				return; // nothing to do.
			// Review: any IO exceptions we should handle? How??
			SetInternalStatus(word, isCorrect);
			var builder = new StringBuilder();
			bool insertedLineForWord = false;
			if (File.Exists(ExceptionPath))
			{
				using (var reader = new StreamReader(ExceptionPath, Encoding.UTF8))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						var item = line;
						bool correct = true;
						if (item.Length > 0 && item[0] == '*')
						{
							correct = false;
							item = item.Substring(1);
						}
						// If we already got it, or the current line is before the word, just copy the line to the output.
						if (insertedLineForWord || String.Compare(item, word, System.StringComparison.Ordinal) < 0)
						{
							builder.AppendLine(line);
							continue;
						}
						// We've come to the right place to insert our word.
						if (!isCorrect)
							builder.Append("*");
						builder.AppendLine(word);
						insertedLineForWord = true;
						if (word != item) // then current line must be a pre-existing word that comes after ours.
							builder.AppendLine(line); // so add it in after item
					}
				}
			}
			if (!insertedLineForWord) // no input file, or the word comes after any existing one
			{
				// The very first exception!
				if (!isCorrect)
					builder.Append("*");
				builder.AppendLine(word);
			}
			// Write the new file over the old one.
			File.WriteAllText(ExceptionPath, builder.ToString(), Encoding.UTF8);
		}

		private void SetInternalStatus(string word, bool isCorrect)
		{
			if (isCorrect)
			{
				if (IsVernacular)
				{
					// Custom vernacular-only dictionary.
					// want it 'affixed' like the prototype, which has been marked to suppress other-case matches
					hunspell_add_with_affix(m_hunspellHandle, word, SpellingHelper.PrototypeWord);
				}
				else
				{
					// not our custom dictionary, some majority language, we can't (and probably don't want)
					// to be restrictive about case.
					hunspell_add(m_hunspellHandle, word);
				}
			}
			else
			{
				hunspell_remove(m_hunspellHandle, word);
			}
		}

		~SpellEngine()
		{
			Dispose(false);
		}

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

		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (m_hunspellHandle != IntPtr.Zero)
			{
				hunspell_uninitialize(m_hunspellHandle);
				m_hunspellHandle = IntPtr.Zero;
			}
		}

		// This method transforms an array of unmanaged character pointers (pointed to by pUnmanagedStringArray)
		// into an array of managed strings.
		// Adapted with thanks from http://limbioliong.wordpress.com/2011/08/14/returning-an-array-of-strings-from-c-to-c-part-1/
		static string[] MarshalUnmananagedStrArray2ManagedStrArray(IntPtr pUnmanagedStringArray, int StringCount)
		{
			IntPtr[] pIntPtrArray = new IntPtr[StringCount];
			var ManagedStringArray = new string[StringCount];

			Marshal.Copy(pUnmanagedStringArray, pIntPtrArray, 0, StringCount);

			for (int i = 0; i < StringCount; i++)
			{
				var data = new List<byte>();
				var ptr = pIntPtrArray[i];
				int offset = 0;
				while (true)
				{
					var ch = Marshal.ReadByte(ptr, offset++);
					if (ch == 0)
					{
						break;
					}
					data.Add(ch);
				}
				ManagedStringArray[i] = Encoding.UTF8.GetString(data.ToArray());
			}
			return ManagedStringArray;
		}

		private const string klibHunspell = "libhunspell";

		[DllImport(klibHunspell, EntryPoint = "hunspell_initialize",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern IntPtr hunspell_initialize([MarshalAs(UnmanagedType.LPStr)] string aff_file,
			[MarshalAs(UnmanagedType.LPStr)] string dict_file);

		[DllImport(klibHunspell, EntryPoint = "hunspell_uninitialize",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern void hunspell_uninitialize(IntPtr handle);

		[DllImport(klibHunspell, EntryPoint = "hunspell_spell",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int hunspell_spell(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string word);

		[DllImport(klibHunspell, EntryPoint = "hunspell_add",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int hunspell_add(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string word);

		[DllImport(klibHunspell, EntryPoint = "hunspell_add_with_affix",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int hunspell_add_with_affix(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string word, [MarshalAs(UnmanagedType.LPStr)] string example);

		[DllImport(klibHunspell, EntryPoint = "hunspell_remove",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int hunspell_remove(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string word);

		[DllImport(klibHunspell, EntryPoint = "hunspell_suggest",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int hunspell_suggest(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string word, out IntPtr suggestions);

		[DllImport(klibHunspell, EntryPoint = "hunspell_free_list",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern void hunspell_free_list(IntPtr handle, ref IntPtr list, int count);

	}
}