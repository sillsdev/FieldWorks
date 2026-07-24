// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>One persisted shown column: its catalog key and an optional pixel width.</summary>
	public sealed class BrowseColumnConfigEntry
	{
		public BrowseColumnConfigEntry(string key, double? width = null)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			Width = width;
		}

		public string Key { get; }
		public double? Width { get; }
	}

	/// <summary>
	/// The per-tool home of the Avalonia browse Configure-Columns choices (the legacy per-tool
	/// <c>ColListId</c> PropertyTable persistence, re-homed as a JSON file). One file per tool in the
	/// project ConfigurationSettings folder — <c>{tool}.browsecolumns.json</c> with
	/// <c>{formatVersion, tool, columns:[{key, width?}]}</c> — loaded lazily and cached, an empty/default
	/// configuration deleting the file ("no file = shipped default columns"), and a corrupt/version-
	/// mismatched file degrading to the shipped default rather than crashing.
	///
	/// Pure FwAvalonia, mirroring <see cref="ViewDefinitionOverrideStore"/>: the xWorks host resolves the
	/// project ConfigurationSettings folder from <c>LcmFileHelper.GetConfigSettingsDir(ProjectId.ProjectFolder)</c>
	/// and hands the path here, so this stays LCModel-free and unit-testable with a temp directory.
	/// </summary>
	public sealed class BrowseColumnConfigStore
	{
		internal const string FileExtension = ".browsecolumns.json";
		internal const int CurrentFormatVersion = 1;

		private readonly string _directory;
		private readonly Dictionary<string, IReadOnlyList<BrowseColumnConfigEntry>> _cache
			= new Dictionary<string, IReadOnlyList<BrowseColumnConfigEntry>>(StringComparer.Ordinal);
		private readonly object _sync = new object();

		public BrowseColumnConfigStore(string configurationSettingsDirectory)
		{
			_directory = configurationSettingsDirectory
				?? throw new ArgumentNullException(nameof(configurationSettingsDirectory));
		}

		/// <summary>
		/// The persisted shown columns for <paramref name="tool"/>, or null when the tool was never
		/// customized (the caller then seeds the shipped default). Loads from disk on first access and
		/// caches; a corrupt or version-mismatched file is treated as "no config" — the failure is reported
		/// to <paramref name="onLoadError"/> rather than crashing.
		/// </summary>
		public IReadOnlyList<BrowseColumnConfigEntry> TryGet(string tool, Action<string, Exception> onLoadError = null)
		{
			if (string.IsNullOrEmpty(tool))
				return null;

			lock (_sync)
			{
				if (_cache.TryGetValue(tool, out var cached))
					return cached;

				IReadOnlyList<BrowseColumnConfigEntry> loaded = null;
				var path = PathFor(tool);
				try
				{
					if (File.Exists(path))
					{
						var parsed = Deserialize(File.ReadAllText(path), tool);
						if (parsed != null && parsed.Count > 0)
							loaded = parsed;
					}
				}
				catch (Exception e)
				{
					onLoadError?.Invoke(path, e);
					loaded = null;
				}

				_cache[tool] = loaded;
				return loaded;
			}
		}

		/// <summary>
		/// Persists the ordered shown <paramref name="columns"/> for <paramref name="tool"/> and refreshes
		/// the cache. An empty/null list deletes the file (the tool reverts to the shipped default), the same
		/// "no file = shipped default" contract the loader relies on.
		/// </summary>
		public void Save(string tool, IReadOnlyList<BrowseColumnConfigEntry> columns)
		{
			if (string.IsNullOrEmpty(tool))
				throw new ArgumentException("A tool name is required to persist browse columns.", nameof(tool));

			var path = PathFor(tool);
			lock (_sync)
			{
				if (columns == null || columns.Count == 0)
				{
					if (File.Exists(path))
						File.Delete(path);
					_cache[tool] = null;
					return;
				}

				Directory.CreateDirectory(_directory);
				File.WriteAllText(path, Serialize(tool, columns));
				_cache[tool] = columns.Select(c => new BrowseColumnConfigEntry(c.Key, c.Width)).ToList();
			}
		}

		/// <summary>The on-disk path for a tool's column config (also the file the loader reads).</summary>
		public string PathFor(string tool) => Path.Combine(_directory, Sanitize(tool) + FileExtension);

		public static string Serialize(string tool, IReadOnlyList<BrowseColumnConfigEntry> columns)
		{
			var array = new JArray();
			foreach (var c in columns)
			{
				var o = new JObject { ["key"] = c.Key };
				if (c.Width.HasValue)
					o["width"] = c.Width.Value;
				array.Add(o);
			}
			var root = new JObject
			{
				["formatVersion"] = CurrentFormatVersion,
				["tool"] = tool,
				["columns"] = array
			};
			return root.ToString(Formatting.Indented);
		}

		// Returns the parsed columns, or null when the file's version mismatches or its tool header disagrees
		// (a renamed/hand-edited file is not used). Throws on malformed JSON so the load path logs + degrades.
		public static IReadOnlyList<BrowseColumnConfigEntry> Deserialize(string json, string expectedTool)
		{
			if (string.IsNullOrEmpty(json))
				return null;
			var root = JObject.Parse(json);
			var version = (int?)root["formatVersion"] ?? -1;
			if (version != CurrentFormatVersion)
				return null;
			var tool = (string)root["tool"];
			if (expectedTool != null && !string.Equals(tool, expectedTool, StringComparison.Ordinal))
				return null;
			var columns = new List<BrowseColumnConfigEntry>();
			foreach (var token in (JArray)root["columns"] ?? new JArray())
			{
				var o = (JObject)token;
				var key = (string)o["key"];
				if (string.IsNullOrEmpty(key))
					continue;
				var width = (double?)o["width"];
				columns.Add(new BrowseColumnConfigEntry(key, width));
			}
			return columns;
		}

		private static string Sanitize(string token)
		{
			if (string.IsNullOrEmpty(token))
				return "_";
			var invalid = Path.GetInvalidFileNameChars();
			return new string(token.Select(c => Array.IndexOf(invalid, c) >= 0 || c == '.' ? '_' : c).ToArray());
		}
	}
}
