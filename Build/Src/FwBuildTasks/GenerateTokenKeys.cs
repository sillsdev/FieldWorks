// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Avalonia;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Reads the FieldWorks-owned design-token <c>x:Key</c> declarations out of the
	/// FwAvaloniaTheme/FwAvaloniaDialogs .axaml token files and emits a generated C# file of
	/// <c>public const string</c> key-name constants, so a call site
	/// (<c>FwThemeResources.Require*</c>)
	/// references a compile-time-checked constant instead of a literal string that only fails at
	/// runtime when it typos or outlives a renamed/deleted key.
	///
	/// Also bakes <see cref="Thickness"/> VALUES (not just key names) for every literal (non
	/// <c>StaticResource</c>-aliased) Thickness token, parsed with Avalonia's own
	/// <see cref="Thickness.Parse"/> rather than hand-rolled comma-splitting. This exists for the
	/// narrow case where Avalonia's compiled XAML rejects <c>x:Static</c> as a resource
	/// declaration, so a C# style builder (e.g. CompactDialogStyles) cannot read a token via
	/// <c>{StaticResource}</c> and previously hand-duplicated the literal instead.
	///
	/// Identifier mapping: an x:Key's '.' characters become '_' (e.g. "DataTree.RowSpacing"
	/// becomes DataTree_RowSpacing); a key with no '.' keeps its exact text (e.g.
	/// "FwLabelBrush").
	/// </summary>
	public class GenerateTokenKeys : Task
	{
		private static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";

		/// <summary>Every top-level ThemeDictionary entry becomes a key constant.</summary>
		private static readonly HashSet<string> TokenElementNames = new HashSet<string>(
			new[] { "SolidColorBrush", "Color", "Double", "Thickness", "CornerRadius", "StaticResource" });

		/// <summary>.axaml files whose EVERY x:Key becomes a generated constant.</summary>
		[Required]
		public string[] FullKeyTokenFiles { get; set; }

		/// <summary>.axaml file whose x:Keys are filtered to those starting with <see
		/// cref="KeyPrefix"/>.</summary>
		[Required]
		public string PrefixedKeyTokenFile { get; set; }

		/// <summary>Only x:Keys in <see cref="PrefixedKeyTokenFile"/> starting with this text are
		/// emitted.</summary>
		[Required]
		public string KeyPrefix { get; set; }

		[Required]
		public string OutputFile { get; set; }

		[Required]
		public string Namespace { get; set; }

		public string ClassName { get; set; } = "GeneratedTokenKeys";

		public override bool Execute()
		{
			try
			{
				var entries = new List<TokenEntry>();
				var seenIdentifiers = new HashSet<string>(StringComparer.Ordinal);

				foreach (var file in FullKeyTokenFiles ?? Array.Empty<string>())
					CollectEntries(file, prefix: null, entries, seenIdentifiers);

				CollectEntries(PrefixedKeyTokenFile, KeyPrefix, entries, seenIdentifiers);

				File.WriteAllText(OutputFile, Render(entries), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				return true;
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex, showStackTrace: true);
				return false;
			}
		}

		/// <summary>
		/// Loads one token file and appends a <see cref="TokenEntry"/> for every distinct x:Key
		/// found on a recognized token-value element (skipping duplicate keys across a
		/// ThemeDictionary's Light/Dark variants, and skipping any key not starting with
		/// <paramref name="prefix"/> when one is given).
		/// </summary>
		private void CollectEntries(string file, string prefix, List<TokenEntry> entries, HashSet<string> seenIdentifiers)
		{
			var doc = XDocument.Load(file);
			var seenKeysInFile = new HashSet<string>(StringComparer.Ordinal);

			foreach (var element in doc.Descendants())
			{
				if (!TokenElementNames.Contains(element.Name.LocalName))
					continue;
				var keyAttribute = element.Attribute(XamlNs + "Key");
				if (keyAttribute == null)
					continue;
				var key = keyAttribute.Value;
				if (prefix != null && !key.StartsWith(prefix, StringComparison.Ordinal))
					continue;
				if (!seenKeysInFile.Add(key))
					continue; // Light/Dark ThemeDictionary variants repeat the same key name.

				var identifier = ToCSharpIdentifier(key);
				if (!seenIdentifiers.Add(identifier))
					throw new InvalidDataException(
						$"Token key '{key}' in '{file}' maps to the identifier '{identifier}', already produced by an earlier key.");

				var thicknessValue = TryParseLiteralThickness(element);
				entries.Add(new TokenEntry(identifier, key, file, thicknessValue));
			}
		}

		/// <summary>
		/// A literal (inline-text) <c>&lt;Thickness&gt;</c> element parses to a baked value;
		/// a <c>&lt;StaticResource&gt;</c> alias or any other element has none.
		/// </summary>
		private static Thickness? TryParseLiteralThickness(XElement element)
		{
			if (element.Name.LocalName != "Thickness")
				return null;
			var text = element.Value.Trim();
			return string.IsNullOrEmpty(text) ? (Thickness?)null : Thickness.Parse(text);
		}

		/// <summary>Replaces every '.' with '_'; the rest of a token key is already a valid C#
		/// identifier.</summary>
		private static string ToCSharpIdentifier(string key) => key.Replace('.', '_');

		private string Render(List<TokenEntry> entries)
		{
			var sourceFiles = string.Join("\n", entries.Select(e => e.SourceFile).Distinct().Select(f => "//   " + f));
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated>");
			sb.AppendLine("// Generated by the GenerateTokenKeys MSBuild task (Build/Src/FwBuildTasks/GenerateTokenKeys.cs)");
			sb.AppendLine("// from the x:Key declarations in:");
			sb.AppendLine(sourceFiles);
			sb.AppendLine("// Do not edit by hand -- re-run the build to regenerate after changing a token file.");
			sb.AppendLine("// </auto-generated>");
			sb.AppendLine();
			sb.AppendLine("using Avalonia;");
			sb.AppendLine();
			sb.AppendLine("namespace " + Namespace);
			sb.AppendLine("{");
			sb.AppendLine("\t/// <summary>");
			sb.AppendLine("\t/// Compile-time-safe key-name constants for every FieldWorks-owned Avalonia design token,");
			sb.AppendLine("\t/// plus baked literal Thickness VALUES for the tokens a C# style builder cannot read via");
			sb.AppendLine("\t/// {StaticResource} at runtime. See GenerateTokenKeys's own doc comment for the mapping rule.");
			sb.AppendLine("\t/// </summary>");
			sb.AppendLine("\tpublic static class " + ClassName);
			sb.AppendLine("\t{");
			foreach (var entry in entries)
			{
				sb.AppendLine($"\t\tpublic const string {entry.Identifier} = \"{EscapeStringLiteral(entry.OriginalKey)}\";");
				if (entry.ThicknessValue.HasValue)
					sb.AppendLine($"\t\tpublic static readonly Thickness {entry.Identifier}Value = {ThicknessLiteral(entry.ThicknessValue.Value)};");
			}
			sb.AppendLine("\t}");
			sb.AppendLine("}");
			return sb.ToString();
		}

		private static string EscapeStringLiteral(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

		/// <summary>
		/// The shortest Avalonia Thickness constructor call that reproduces the parsed value: a
		/// single value when all four sides match, horizontal/vertical when only those two
		/// differ,
		/// otherwise all four components explicit.
		/// </summary>
		private static string ThicknessLiteral(Thickness t)
		{
			string N(double d) => d.ToString(CultureInfo.InvariantCulture);
			if (t.Left == t.Top && t.Top == t.Right && t.Right == t.Bottom)
				return $"new Thickness({N(t.Left)})";
			if (t.Left == t.Right && t.Top == t.Bottom)
				return $"new Thickness({N(t.Left)}, {N(t.Top)})";
			return $"new Thickness({N(t.Left)}, {N(t.Top)}, {N(t.Right)}, {N(t.Bottom)})";
		}

		private class TokenEntry
		{
			public TokenEntry(string identifier, string originalKey, string sourceFile, Thickness? thicknessValue)
			{
				Identifier = identifier;
				OriginalKey = originalKey;
				SourceFile = sourceFile;
				ThicknessValue = thicknessValue;
			}

			public string Identifier { get; }
			public string OriginalKey { get; }
			public string SourceFile { get; }
			public Thickness? ThicknessValue { get; }
		}
	}
}
