// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Tasks 5.5/5.8 (and the runtime half of 8.4): the region-manifest engine-isolation audit. The
	/// migrated Avalonia path must carry no dependency on native Views rendering, native render
	/// engines (Graphite/Uniscribe), Gecko/browser engines, or legacy view stacks — at the assembly
	/// level (what the production assembly can even load) and at the source level (what production
	/// code names). A failure of either test blocks Avalonia default readiness by construction.
	/// </summary>
	[TestFixture]
	public class EngineIsolationAuditTests
	{
		// Assembly names the production FwAvalonia assembly must never reference.
		// System.Windows.Forms is the single approved adapter exception: WinFormsAvaloniaControlHost
		// (in-process hosting) requires it; it hosts the surface and owns no rendering of user text.
		private static readonly string[] ForbiddenAssemblyFragments =
		{
			"Graphite", "ViewsInterfaces", "RootSite", "SimpleRootSite", "Gecko", "Geckofx",
			"SIL.LCModel", "xCore", "XMLViews", "DetailControls"
		};

		// Identifiers from the region-manifest forbidden-symbol list (parity-evidence.md §4) that
		// production source must not name (native Views render/editor pipeline, legacy DataTree/Slice
		// editor surface, native render engines, browser/PDF engines).
		private static readonly string[] ForbiddenSourceSymbols =
		{
			"IVwRootBox", "IVwEnv", "IVwGraphics", "VwRootBox", "ManagedVwWindow",
			"IRenderEngine", "IRenderEngineFactory", "GraphiteEngineClass", "UniscribeEngineClass",
			"FwGrEngine", "GraphiteSegment", "RootSiteControl",
			"GeckoWebBrowser", "XWebBrowser", "GeckofxHtmlToPdf", "FieldWorksPdfMaker",
			"DataTree", "Slice", "SliceFactory", "XmlView", "BrowseViewer"
		};

		[Test]
		public void ProductionAssembly_ReferencesNoNativeRenderLegacyOrDomainAssemblies()
		{
			var referenced = typeof(LexicalEditRegionView).Assembly.GetReferencedAssemblies()
				.Select(r => r.Name)
				.ToList();

			var violations = referenced
				.Where(name => ForbiddenAssemblyFragments.Any(bad => name.IndexOf(bad, StringComparison.OrdinalIgnoreCase) >= 0))
				.ToList();

			Assert.That(violations, Is.Empty,
				"FwAvalonia must stay free of native-render/legacy/domain assembly references; found: "
				+ string.Join(", ", violations));
		}

		[Test]
		public void ProductionSource_NamesNoForbiddenNativeRenderSymbols()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null, "could not locate the repo root");

			var productionRoot = Path.Combine(dir.FullName, "Src", "Common", "FwAvalonia");
			var testRoot = Path.Combine(productionRoot, "FwAvaloniaTests");
			var sources = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
				.Where(path => !path.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase))
				.OrderBy(path => path, StringComparer.Ordinal)
				.ToList();
			Assert.That(sources, Is.Not.Empty);

			var violations = new List<string>();
			foreach (var path in sources)
			{
				// Comments and string literals are stripped before matching. The DataTree/Slice/
				// SliceFactory/BrowseViewer parity symbols are legitimately named throughout this
				// codebase's XML-doc comments and diagnostic message text to document exactly which
				// legacy behavior a migrated code path mirrors (see parity-evidence.md); that is
				// documentation, not a dependency. What this audit must catch is the symbol being
				// named as actual code — a using directive, type reference, base type, cast, member
				// access, or the like — which would mean production code really depends on it.
				var code = StripCommentsAndStrings(File.ReadAllText(path));
				foreach (var symbol in ForbiddenSourceSymbols)
				{
					// Whole-identifier match so e.g. code mentioning "Views" broadly is fine but
					// naming the actual forbidden type is not.
					if (Regex.IsMatch(code, $@"\b{Regex.Escape(symbol)}\b"))
						violations.Add($"{Path.GetFileName(path)}: {symbol}");
				}
			}

			Assert.That(violations, Is.Empty,
				"FwAvalonia production source must not name native Views/render-engine/browser symbols; found: "
				+ string.Join("; ", violations));
		}

		/// <summary>
		/// Removes C# comments (//, /* */) and string/character literal contents (regular,
		/// verbatim @"", and interpolated $"" / $@"") from source text, replacing each with spaces
		/// of the same length (preserving line/column positions and never merging adjacent tokens).
		/// This is a lightweight lexical pass, not a full C# tokenizer, but it is sufficient to keep
		/// the forbidden-symbol scan focused on actual code rather than prose that discusses legacy
		/// symbols by name (doc comments, TODOs, exception/assert message text, etc.).
		/// </summary>
		private static string StripCommentsAndStrings(string source)
		{
			var sb = new System.Text.StringBuilder(source.Length);
			var i = 0;
			var n = source.Length;
			while (i < n)
			{
				var c = source[i];
				var c2 = i + 1 < n ? source[i + 1] : '\0';

				// Line comment.
				if (c == '/' && c2 == '/')
				{
					while (i < n && source[i] != '\n')
					{
						sb.Append(' ');
						i++;
					}
					continue;
				}

				// Block comment.
				if (c == '/' && c2 == '*')
				{
					sb.Append(' ', 2);
					i += 2;
					while (i < n && !(source[i] == '*' && i + 1 < n && source[i + 1] == '/'))
					{
						sb.Append(source[i] == '\n' ? '\n' : ' ');
						i++;
					}
					if (i < n)
					{
						sb.Append(' ', 2);
						i += 2;
					}
					continue;
				}

				// Verbatim string (optionally interpolated: @"..." or $@"..."/@$"...").
				var verbatimStart = -1;
				if (c == '@' && c2 == '"') verbatimStart = i + 2;
				else if (c == '@' && c2 == '$' && i + 2 < n && source[i + 2] == '"') verbatimStart = i + 3;
				else if (c == '$' && c2 == '@' && i + 2 < n && source[i + 2] == '"') verbatimStart = i + 3;
				if (verbatimStart >= 0)
				{
					sb.Append(' ', verbatimStart - i);
					i = verbatimStart;
					while (i < n)
					{
						if (source[i] == '"' && i + 1 < n && source[i + 1] == '"')
						{
							sb.Append(' ', 2);
							i += 2;
							continue;
						}
						if (source[i] == '"')
						{
							sb.Append(' ');
							i++;
							break;
						}
						sb.Append(source[i] == '\n' ? '\n' : ' ');
						i++;
					}
					continue;
				}

				// Regular or interpolated non-verbatim string ("..." or $"...").
				var regularStart = -1;
				if (c == '"') regularStart = i + 1;
				else if (c == '$' && c2 == '"') regularStart = i + 2;
				if (regularStart >= 0)
				{
					sb.Append(' ', regularStart - i);
					i = regularStart;
					while (i < n && source[i] != '"' && source[i] != '\n')
					{
						if (source[i] == '\\' && i + 1 < n)
						{
							sb.Append(' ', 2);
							i += 2;
							continue;
						}
						sb.Append(' ');
						i++;
					}
					if (i < n && source[i] == '"')
					{
						sb.Append(' ');
						i++;
					}
					continue;
				}

				// Character literal.
				if (c == '\'')
				{
					sb.Append(' ');
					i++;
					while (i < n && source[i] != '\'' && source[i] != '\n')
					{
						if (source[i] == '\\' && i + 1 < n)
						{
							sb.Append(' ', 2);
							i += 2;
							continue;
						}
						sb.Append(' ');
						i++;
					}
					if (i < n && source[i] == '\'')
					{
						sb.Append(' ');
						i++;
					}
					continue;
				}

				sb.Append(c);
				i++;
			}
			return sb.ToString();
		}
	}
}
