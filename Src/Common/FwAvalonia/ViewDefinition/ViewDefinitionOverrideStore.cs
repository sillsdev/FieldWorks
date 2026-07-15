// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// The per-project home of the sparse <see cref="ViewDefinitionOverride"/> patches that drive the
	/// Avalonia lexical-edit surface's per-field "Field Visibility"/"Move Field" commands
	/// (canonical-view-definition-design.md Layer 2: "sparse JSON patch documents keyed by StableId,
	/// stored as files in the project ConfigurationSettings folder"). One file per (class, layout); the
	/// override layer — not the legacy Inventory store — is what Compose actually reads.
	///
	/// Pure FwAvalonia: the caller (the xWorks host) resolves the project ConfigurationSettings folder
	/// from <c>LcmFileHelper.GetConfigSettingsDir</c> and hands the path here, so this stays LCModel-free
	/// and unit-testable with a temp directory. Patches load lazily and cache per (class, layout); each
	/// mutation re-serializes the one file it touched (via <see cref="ViewDefinitionOverrideJsonSerializer"/>).
	/// </summary>
	public sealed class ViewDefinitionOverrideStore
	{
		// Distinct extension so these never collide with the legacy whole-copy "{Class}.fwlayout" files
		// in the same folder, and so the file name reads as "what + which layout" for support staff.
		internal const string FileExtension = ".viewoverride.json";

		private readonly string _directory;
		private readonly Dictionary<(string Class, string Layout), ViewDefinitionOverride> _cache
			= new Dictionary<(string, string), ViewDefinitionOverride>();
		private readonly object _sync = new object();

		public ViewDefinitionOverrideStore(string configurationSettingsDirectory)
		{
			_directory = configurationSettingsDirectory
				?? throw new ArgumentNullException(nameof(configurationSettingsDirectory));
		}

		/// <summary>
		/// The patch for (<paramref name="className"/>, <paramref name="layoutName"/>), or null when the
		/// project never customized that layout. Loads from disk on first access and caches; a corrupt or
		/// version-mismatched file is treated as "no override" (load failure is reported to
		/// <paramref name="onLoadError"/> rather than crashing compose — the legacy Inventory drops stale
		/// overrides too).
		/// </summary>
		public ViewDefinitionOverride TryGet(string className, string layoutName,
			Action<string, Exception> onLoadError = null)
		{
			if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(layoutName))
				return null;

			var key = (className, layoutName);
			lock (_sync)
			{
				if (_cache.TryGetValue(key, out var cached))
					return cached;

				ViewDefinitionOverride loaded = null;
				var path = PathFor(className, layoutName);
				try
				{
					if (File.Exists(path))
					{
						var patch = ViewDefinitionOverrideJsonSerializer.Deserialize(File.ReadAllText(path));
						// Guard against a hand-edited/renamed file whose header disagrees with its name.
						if (string.Equals(patch.ClassName, className, StringComparison.Ordinal)
							&& string.Equals(patch.LayoutName, layoutName, StringComparison.Ordinal))
						{
							loaded = patch.IsEmpty ? null : patch;
						}
					}
				}
				catch (Exception e)
				{
					onLoadError?.Invoke(path, e);
					loaded = null;
				}

				_cache[key] = loaded;
				return loaded;
			}
		}

		/// <summary>
		/// Persists <paramref name="patch"/> for its (ClassName, LayoutName) and refreshes the cache. An
		/// empty patch deletes the file (the project no longer customizes that layout), so an undo-to-base
		/// leaves no stale override behind — the same "no file = shipped definition" contract the loader
		/// relies on.
		/// </summary>
		public void Save(ViewDefinitionOverride patch)
		{
			if (patch == null) throw new ArgumentNullException(nameof(patch));
			if (string.IsNullOrEmpty(patch.ClassName) || string.IsNullOrEmpty(patch.LayoutName))
				throw new ArgumentException("Override must carry a class and layout name to be stored.");

			var key = (patch.ClassName, patch.LayoutName);
			var path = PathFor(patch.ClassName, patch.LayoutName);
			lock (_sync)
			{
				if (patch.IsEmpty)
				{
					if (File.Exists(path))
						File.Delete(path);
					_cache[key] = null;
					return;
				}

				Directory.CreateDirectory(_directory);
				File.WriteAllText(path, ViewDefinitionOverrideJsonSerializer.Serialize(patch));
				_cache[key] = patch;
			}
		}

		/// <summary>The on-disk path for a (class, layout) patch (also the file the loader reads).</summary>
		public string PathFor(string className, string layoutName)
			=> Path.Combine(_directory, MakeFileName(className, layoutName));

		// "{Class}.{Layout}.viewoverride.json" — sanitized so an exotic layout name can never escape the
		// folder or collide with a path separator (layout names are inventory tokens, but be defensive).
		internal static string MakeFileName(string className, string layoutName)
		{
			var safeClass = Sanitize(className);
			var safeLayout = Sanitize(layoutName);
			return safeClass + "." + safeLayout + FileExtension;
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
