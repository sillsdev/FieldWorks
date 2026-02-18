using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;

public sealed class PartsLayoutLoader
{
	public ResolvedContract Load(LayoutId root, IReadOnlyList<string> searchRoots)
	{
		if (searchRoots.Count == 0)
			throw new ArgumentException("At least one search root is required.", nameof(searchRoots));

		var normalizedRoots = searchRoots
			.Where(r => !string.IsNullOrWhiteSpace(r))
			.Select(r => Path.GetFullPath(r))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var layoutsByKey = new Dictionary<LayoutId, XElement>();
		var partsById = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);

		foreach (var rootDir in normalizedRoots)
		{
			LoadLayoutsFromRoot(rootDir, layoutsByKey);
			LoadPartsFromRoot(rootDir, partsById);
		}

		if (!layoutsByKey.TryGetValue(root, out var layoutElement))
			throw new InvalidOperationException($"Layout not found: {root}");

		var fingerprint = ComputeConfigurationFingerprint(normalizedRoots);
		return new ResolvedContract(root, layoutElement, layoutsByKey, partsById, normalizedRoots, fingerprint);
	}

	private static string ComputeConfigurationFingerprint(IReadOnlyList<string> roots)
	{
		// The cache key needs to change when the Parts/Layout inputs change.
		// For now we use a deterministic hash over the relevant file paths and timestamps.
		// This is fast, stable, and good enough for explicit invalidation + unit tests.
		var sb = new StringBuilder();
		foreach (var root in roots)
		{
			sb.Append("root|").Append(root).Append('\n');
			if (!Directory.Exists(root))
				continue;

			foreach (var file in Directory
				.EnumerateFiles(root, "*.fwlayout", SearchOption.TopDirectoryOnly)
				.Concat(Directory.EnumerateFiles(root, "*Parts.xml", SearchOption.TopDirectoryOnly))
				.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					var info = new FileInfo(file);
					sb.Append("file|")
						.Append(info.FullName)
						.Append('|')
						.Append(info.Length)
						.Append('|')
						.Append(info.LastWriteTimeUtc.Ticks)
						.Append('\n');
				}
				catch
				{
					// Ignore unreadable files; the fingerprint is best-effort.
				}
			}
		}

		using var sha = SHA256.Create();
		var bytes = Encoding.UTF8.GetBytes(sb.ToString());
		var hash = sha.ComputeHash(bytes);
		return Convert.ToHexString(hash);
	}

	private static void LoadLayoutsFromRoot(string rootDir, Dictionary<LayoutId, XElement> layoutsByKey)
	{
		if (!Directory.Exists(rootDir))
			return;

		foreach (var file in Directory.EnumerateFiles(rootDir, "*.fwlayout", SearchOption.TopDirectoryOnly))
		{
			XDocument doc;
			try
			{
				doc = XDocument.Load(file, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			}
			catch
			{
				continue;
			}

			var root = doc.Root;
			if (root is null || root.Name.LocalName != "LayoutInventory")
				continue;

			foreach (var layout in root.Elements().Where(e => e.Name.LocalName == "layout"))
			{
				var classAttr = (string?)layout.Attribute("class");
				var typeAttr = (string?)layout.Attribute("type");
				var nameAttr = (string?)layout.Attribute("name");
				if (string.IsNullOrWhiteSpace(classAttr) || string.IsNullOrWhiteSpace(typeAttr) || string.IsNullOrWhiteSpace(nameAttr))
					continue;

				var key = new LayoutId(classAttr, typeAttr, nameAttr);
				// Earlier roots have higher priority (override -> default). First-wins.
				if (!layoutsByKey.ContainsKey(key))
					layoutsByKey[key] = layout;
			}
		}
	}

	private static void LoadPartsFromRoot(string rootDir, Dictionary<string, XElement> partsById)
	{
		if (!Directory.Exists(rootDir))
			return;

		foreach (var file in Directory.EnumerateFiles(rootDir, "*Parts.xml", SearchOption.TopDirectoryOnly))
		{
			XDocument doc;
			try
			{
				doc = XDocument.Load(file, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			}
			catch
			{
				continue;
			}

			var root = doc.Root;
			if (root is null || root.Name.LocalName != "PartInventory")
				continue;

			foreach (var part in root.Descendants().Where(e => e.Name.LocalName == "part"))
			{
				var id = (string?)part.Attribute("id");
				if (string.IsNullOrWhiteSpace(id))
					continue;

				// Earlier roots have higher priority (override -> default). First-wins.
				if (!partsById.ContainsKey(id))
					partsById[id] = part;
			}
		}
	}
}