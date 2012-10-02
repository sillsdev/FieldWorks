using System;

namespace SIL.Utils
{
	/// <summary>
	/// A home for functions related to Unicode surrogate pairs.
	/// (Some of these unfortunately are duplicated in FwUtils.)
	/// </summary>
	public class Surrogates
	{
		/// <summary>
		/// Returns a string representation of the codepoint, handling surrogate pairs if necessary.
		/// </summary>
		/// <param name="codepoint">The codepoint</param>
		/// <returns>The string representation of the codepoint</returns>
		public static string StringFromCodePoint(int codepoint)
		{
			if(codepoint <= 0xFFFF)
				return new string((char)codepoint,1);

			char codepointH = (char)(((codepoint-0x10000)/0x400) + 0xD800);
			char codepointL = (char)(((codepoint-0x10000)%0x400) + 0xDC00);
			return "" + codepointH + codepointL;
		}
		/// <summary>
		/// Return a full 32-bit character value from the surrogate pair.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="ch1"></param>
		/// <param name="ch2"></param>
		/// <returns></returns>
		public static int Int32FromSurrogates(char ch1, char ch2)
		{
			System.Diagnostics.Debug.Assert(IsLeadSurrogate(ch1));
			return ((ch1 - 0xD800) << 10) + ch2 + 0x2400;
		}
		/// <summary>
		/// Whether the character is the first of a surrogate pair.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="ch">The character</param>
		/// <returns></returns>
		public static bool IsLeadSurrogate(char ch)
		{
			const char minLeadSurrogate = '\xD800';
			const char maxLeadSurrogate = '\xDBFF';
			return ch >= minLeadSurrogate && ch <= maxLeadSurrogate;
		}
		/// <summary>
		/// Whether the character is the second of a surrogate pair.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="ch">The character</param>
		/// <returns></returns>
		public static bool IsTrailSurrogate(char ch)
		{
			const char minTrailSurrogate = '\xDC00';
			const char maxTrailSurrogate = '\xDFFF';
			return ch >= minTrailSurrogate && ch <= maxTrailSurrogate;
		}
		/// <summary>
		/// Increment an index into a string, allowing for surrogates.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int NextChar (string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]) && ich < st.Length - 1 && IsTrailSurrogate(st[ich+1]))
				return ich + 2;
			return ich + 1;
		}

		/// <summary>
		/// Decrement an index into a string, allowing for surrogates.
		/// Assumes ich is pointing at the START of a character; will move back two if the two characters
		/// at ich-1 and ich-2 are a pair.
		/// </summary>
		public static int PrevChar (string st, int ich)
		{
			if (ich >= 2 && IsLeadSurrogate(st[ich - 2]) && IsTrailSurrogate(st[ich - 1]))
				return ich - 2;
			return ich - 1;
		}
	}
}
