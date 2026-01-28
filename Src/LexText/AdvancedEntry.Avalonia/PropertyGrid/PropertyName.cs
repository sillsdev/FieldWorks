using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;

internal static class PropertyName
{
	public static string From(string seed, ISet<string> usedNames)
	{
		var baseName = Sanitize(seed);
		if (string.IsNullOrWhiteSpace(baseName))
			baseName = "P";

		var name = baseName;
		var i = 2;
		while (usedNames.Contains(name))
		{
			name = $"{baseName}_{i}";
			i++;
		}

		usedNames.Add(name);
		return name;
	}

	private static string Sanitize(string s)
	{
		var sb = new StringBuilder(s.Length);
		foreach (var ch in s)
		{
			if (char.IsLetterOrDigit(ch))
				sb.Append(ch);
			else
				sb.Append('_');
		}
		return sb.ToString();
	}
}