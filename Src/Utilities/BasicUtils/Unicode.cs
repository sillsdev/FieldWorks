using System;

namespace SIL.Utils
{
	/// <summary>
	/// A collection of utility functions related to Unicode.
	/// See also CaseFunctions, Surrogates, IcuWrappers.
	/// </summary>
	public class Unicode
	{
		/// <summary>
		/// Returns a list of the characters that are considered white space.
		/// This is all the characters having the Zs character type, as of Unicode 4.1,
		/// except for the two non-breaking spaces (since we mainly use this list to
		/// decide where to break things).
		/// </summary>
		public static char[] SpaceChars
		{
			get
			{
				return new char[]
				{
					' ',
					//'\x00a0', // NO-BREAK SPACE
					'\x1680', // OGHAM SPACE MARK
					'\x180e', // MONGOLIAN VOWEL SEPARATOR
					'\x2000', // EN QUAD
					'\x2001', // EM QUAD
					'\x2002', // EN SPACE
					'\x2003', // EM SPACE
					'\x2004', // THREE-PER-EM SPACE
					'\x2005', // FOUR-PER-EM SPACE
					'\x2006', // SIX-PER-EM SPACE
					'\x2007', // FIGURE SPACE
					'\x2008', // PUNCTUATION SPACE
					'\x2009', // THIN SPACE
					'\x200A', // HAIR SPACE
					//'\x202F', // NARROW NO-BREAK SPACE
					'\x205F', // MEDIUM MATHEMATICAL SPACE
					'\x3000' // IDEOGRAPHIC SPACE
				};
			}
		}
	}
}
