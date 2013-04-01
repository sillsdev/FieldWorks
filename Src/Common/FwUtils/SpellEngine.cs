using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

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
				m_hunspellHandle = Hunspell_initialize(MarshallAsUtf8Bytes(affixPath), MarshallAsUtf8Bytes(dictPath));
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
			catch (Exception e)
			{
				Debug.WriteLine("Initializing Hunspell: {0} exception: {1} ", e.GetType(), e.Message);
				if (m_hunspellHandle != IntPtr.Zero)
				{
					Hunspell_uninitialize(m_hunspellHandle);
					m_hunspellHandle = IntPtr.Zero;
					throw;
				}
			}

		}

		private IntPtr m_hunspellHandle;

		public bool Check(string word)
		{
			return Hunspell_spell(m_hunspellHandle, MarshallAsUtf8Bytes(word)) != 0;
		}

		/// <summary>
		/// We can't declare these arguments (char * in C++) as [MarshalAs(UnmanagedType.LPStr)] string, because that
		/// unconditionally coverts the string to bytes using the current system code page, which is never what we want.
		/// So we declare them as byte[] and marshal like this. The C++ code requires null termination so add a null
		/// before converting. (This doesn't seem to be necessary, but better safe than sorry.)
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		private static byte[] MarshallAsUtf8Bytes(string word)
		{
			return Encoding.UTF8.GetBytes(Icu.Normalize(word, Icu.UNormalizationMode.UNORM_NFC) + "\0");
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
			int resultCount = Hunspell_suggest(m_hunspellHandle, MarshallAsUtf8Bytes(badWord), out pointerToAddressStringArray);
			if (pointerToAddressStringArray == IntPtr.Zero)
				return new string[0];
			var results = MarshalUnmananagedStrArray2ManagedStrArray(pointerToAddressStringArray, resultCount);
			Hunspell_free_list(m_hunspellHandle, ref pointerToAddressStringArray, resultCount);
			return results;
		}

		public void SetStatus(string word1, bool isCorrect)
		{
			var word = Icu.Normalize(word1, Icu.UNormalizationMode.UNORM_NFC);
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
					Hunspell_add_with_affix(m_hunspellHandle, MarshallAsUtf8Bytes(word), MarshallAsUtf8Bytes(SpellingHelper.PrototypeWord));
				}
				else
				{
					// not our custom dictionary, some majority language, we can't (and probably don't want)
					// to be restrictive about case.
					Hunspell_add(m_hunspellHandle, MarshallAsUtf8Bytes(word));
				}
			}
			else
			{
				Hunspell_remove(m_hunspellHandle, MarshallAsUtf8Bytes(word));
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
				Hunspell_uninitialize(m_hunspellHandle);
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

#if __MonoCS__
		private const string klibHunspell = "libhunspell-1.3.so.0";		// this is the standard installed version on Precise and Quantal.
		// Hunspell on Linux uses methods that start with uppercase Hunspell and has
		// different methods for (un-)initializing.
		private const string klibHunspellCtor = "Hunspell_create";
		private const string klibHunspellDtor = "Hunspell_destroy";
		private const string klibHunspellPrefix = "Hunspell_";
#else
		private const string klibHunspell = "libhunspell";
		// Hunspell on Windows uses different methods that start with lowercase hunspell!
		private const string klibHunspellCtor = "hunspell_initialize";
		private const string klibHunspellDtor = "hunspell_uninitialize";
		private const string klibHunspellPrefix = "hunspell_";
#endif

		[DllImport(klibHunspell, EntryPoint = klibHunspellCtor,
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern IntPtr Hunspell_initialize(byte[] aff_file,
			byte[] dict_file);

		[DllImport(klibHunspell, EntryPoint = klibHunspellDtor,
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern void Hunspell_uninitialize(IntPtr handle);

		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "spell",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_spell(IntPtr handle, byte[] word);

		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "add",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_add(IntPtr handle, byte[] word);

		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "add_with_affix",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_add_with_affix(IntPtr handle, byte[] word, byte[] example);

		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "remove",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_remove(IntPtr handle, byte[] word);

#if __MonoCS__
		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "suggest",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_suggest_Impl(IntPtr handle, out IntPtr suggestions, byte[] word);
#else
		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "suggest",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int Hunspell_suggest_Impl(IntPtr handle, byte[] word, out IntPtr suggestions);
#endif
		private static int Hunspell_suggest(IntPtr handle, byte[] word, out IntPtr suggestions)
		{
#if __MonoCS__
			return Hunspell_suggest_Impl(handle, out suggestions, word);
#else
			return Hunspell_suggest_Impl(handle, word, out suggestions);
#endif
		}

		[DllImport(klibHunspell, EntryPoint = klibHunspellPrefix + "free_list",
			CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern void Hunspell_free_list(IntPtr handle, ref IntPtr list, int count);

	}
}
