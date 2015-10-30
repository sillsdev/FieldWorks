// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Utils
{
	public class Surrogates
	{
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
		/// Decrement an index into a string, allowing for surrogates.
		/// Assumes ich is pointing at the START of a character; will move back two if the two characters
		/// at ich-1 and ich-2 are a pair.
		/// </summary>
		public static int PrevChar(string st, int ich)
		{
			if (ich >= 2 && IsLeadSurrogate(st[ich - 2]) && IsTrailSurrogate(st[ich - 1]))
				return ich - 2;
			return ich - 1;
		}
	}
}
