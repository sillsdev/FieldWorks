using System;
using System.IO;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

internal static class TestRepoRoot
{
	public static string Find()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null)
		{
			var marker = Path.Combine(dir.FullName, "FieldWorks.proj");
			if (File.Exists(marker))
				return dir.FullName;

			dir = dir.Parent;
		}

		throw new InvalidOperationException("Could not find repo root (FieldWorks.proj) from test base directory.");
	}
}
