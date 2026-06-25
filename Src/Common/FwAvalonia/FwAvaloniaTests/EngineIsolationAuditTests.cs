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

		// Identifiers from the region-manifest forbidden-symbol list that production source must not
		// name (native Views render/editor pipeline, native render engines, browser/PDF engines).
		private static readonly string[] ForbiddenSourceSymbols =
		{
			"IVwRootBox", "IVwEnv", "IVwGraphics", "VwRootBox", "ManagedVwWindow",
			"IRenderEngine", "IRenderEngineFactory", "GraphiteEngineClass", "UniscribeEngineClass",
			"FwGrEngine", "GraphiteSegment", "RootSiteControl",
			"GeckoWebBrowser", "XWebBrowser", "GeckofxHtmlToPdf", "FieldWorksPdfMaker"
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
				var text = File.ReadAllText(path);
				foreach (var symbol in ForbiddenSourceSymbols)
				{
					// Whole-identifier match so e.g. a comment mentioning "Views" broadly is fine but
					// naming the actual forbidden type is not (even in a comment, naming the native
					// pipeline in production source is a smell the audit surfaces for review).
					if (Regex.IsMatch(text, $@"\b{Regex.Escape(symbol)}\b"))
						violations.Add($"{Path.GetFileName(path)}: {symbol}");
				}
			}

			Assert.That(violations, Is.Empty,
				"FwAvalonia production source must not name native Views/render-engine/browser symbols; found: "
				+ string.Join("; ", violations));
		}
	}
}
