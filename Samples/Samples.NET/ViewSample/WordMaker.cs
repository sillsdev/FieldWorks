using System;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Samples.ViewSample
{
	/// <summary>
	/// Summary description for WordMaker.
	/// </summary>
	/// <summary>
	/// This class is initialized with an ITsString, then NextWord may be called until it returns
	/// null to obtain a breakdown into words.
	/// </summary>
	public class WordMaker
	{
		ITsString m_tss;
		int m_ich; // current position in string, initially 0, advances to char after word or end.
		int m_cch; // length of string
		string m_st; // text of m_tss
		ILgCharacterPropertyEngine m_cpe;

		/// <summary>
		/// Start it off analyzing a string.
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="cpe">engine to use.</param>
		public WordMaker(ITsString tss, ILgCharacterPropertyEngine cpe)
		{
			m_tss = tss;
			m_ich = 0;
			m_st = tss.get_Text();
			m_cch = m_st.Length;
			m_cpe = cpe;
		}

		public WordMaker(ITsString tss, ILgWritingSystemFactory encf)
		{
			m_tss = tss;
			m_ich = 0;
			m_st = tss.get_Text();
			if (m_st == null)
				m_st = "";
			m_cch = m_st.Length;
			// Get a character property engine from the wsf.
			m_cpe = encf.get_UnicodeCharProps();
			Debug.Assert(m_cpe != null, "encf.get_UnicodeCharProps() returned null");
		}

		public static bool IsLeadSurrogate(char ch)
		{
			const char minLeadSurrogate = '\xD800';
			const char maxLeadSurrogate = '\xDBFF';
			return ch >= minLeadSurrogate && ch <= maxLeadSurrogate;
		}
		/// <summary>
		/// Increment an index into a string, allowing for surrogates.
		/// Refactor JohnT: there should be some more shareable place to put this...
		/// a member function of string would be ideal...
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int NextChar(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return ich + 2;
			return ich + 1;
		}
		/// <summary>
		/// Return a full 32-bit character value from the surrogate pair.
		/// </summary>
		/// <param name="ch1"></param>
		/// <param name="ch2"></param>
		/// <returns></returns>
		public static int Int32FromSurrogates(char ch1, char ch2)
		{
			Debug.Assert(IsLeadSurrogate(ch1));
			return ((ch1 - 0xD800) << 10) + ch2 + 0x2400;
		}
		/// <summary>
		/// Return the full 32-bit character starting at position ich in st
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int FullCharAt(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return Int32FromSurrogates(st[ich], st[ich + 1]);
			else return Convert.ToInt32(st[ich]);
		}

		ITsString GetSubString(ITsString tss, int ichMin, int ichLim)
		{
			ITsStrBldr tsb = tss.GetBldr();
			int len = tss.get_Length();
			if (ichLim < len)
				tsb.Replace(ichLim, tss.get_Length(), null, null);
			if (ichMin > 0)
				tsb.Replace(0, ichMin, null, null);
			return tsb.GetString();
		}

		public ITsString NextWord(out int ichMin, out int ichLim)
		{
			// m_ich is always left one character position after a non-wordforming character.
			// This is considered implicitly true at the start of the string.
			bool fPrevWordForming = false;
			int ichStartWord = -1;
			ichMin = ichLim = 0;
			for (; m_ich < m_cch; m_ich = NextChar(m_st, m_ich))
			{
				bool fThisWordForming = m_cpe.get_IsWordForming(FullCharAt(m_st, m_ich));
				if (fThisWordForming && !fPrevWordForming)
				{
					// Start of word.
					ichStartWord = m_ich;
				}
				else if (fPrevWordForming && !fThisWordForming)
				{
					// End of word
					Debug.Assert(ichStartWord >= 0);
					ichMin = ichStartWord;
					ichLim = m_ich;
					return GetSubString(m_tss, ichStartWord, m_ich);
				}
				fPrevWordForming = fThisWordForming;
			}
			if (fPrevWordForming)
			{
				ichMin = ichStartWord;
				ichLim = m_ich;
				return GetSubString(m_tss, ichStartWord, m_ich);
			}
			else
				return null; // didn't find any more words.
		}
	}
}
