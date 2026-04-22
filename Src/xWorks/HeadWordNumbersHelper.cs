using System.Collections.Generic;

namespace SIL.FieldWorks.XWorks
{
	public static class HeadWordNumbersHelper
	{
		public static List<string> GetUnicodeCharacters(string digits)
		{
			if (!string.IsNullOrEmpty(digits))
			{
				// Note that some digits use surrogate pairs in UTF-16, so it's not as simple as splitting into a char array.
				// We need to check for surrogate pairs to split everything correctly into Unicode characters.
				var characters = new List<string>();
				for (int i = 0; i < digits.Length; i++)
				{
					// The first part of a surrogate pair is the high surrogate and the second part is the low surrogate.
					if (char.IsHighSurrogate(digits[i]) && i + 1 < digits.Length &&
						char.IsLowSurrogate(digits[i + 1]))
					{
						characters.Add(digits.Substring(i, 2));
						i++; // Skip the next char since it's part of the surrogate pair we just added.
					}
					else
					{
						characters.Add(digits[i].ToString());
					}
				}
				if (characters.Count == 10)
					return characters;
			}
			return null;
		}
	}
}
