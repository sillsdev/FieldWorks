// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Pure-logic tests for the two-adapter feature flag and surface factory. No Avalonia runtime
	/// is required, which is itself part of the evidence: the default (flag off) path constructs
	/// nothing Avalonia.
	/// </summary>
	[TestFixture]
	public class LexicalEditSurfaceResolverTests
	{
		[Test]
		public void Resolve_DefaultsToWinForms_WhenFlagUnset()
		{
			var surface = LexicalEditSurfaceResolver.Resolve();
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms));
		}

		[Test]
		public void Resolve_OverrideWinsOverPersistedUIMode()
		{
			var winForms = LexicalEditSurfaceResolver.Resolve(
				overrideEnabled: false,
				uiMode: LexicalEditSurfaceResolver.NewUIMode);
			Assert.That(winForms, Is.EqualTo(LexicalEditSurface.WinForms));

			var avalonia = LexicalEditSurfaceResolver.Resolve(
				overrideEnabled: true,
				uiMode: LexicalEditSurfaceResolver.LegacyUIMode);
			Assert.That(avalonia, Is.EqualTo(LexicalEditSurface.Avalonia));
		}

		[TestCase(LexicalEditSurfaceResolver.LegacyUIMode, LexicalEditSurface.WinForms)]
		[TestCase(LexicalEditSurfaceResolver.NewUIMode, LexicalEditSurface.Avalonia)]
		[TestCase(null, LexicalEditSurface.WinForms)]
		[TestCase("", LexicalEditSurface.WinForms)]
		[TestCase("SomethingElse", LexicalEditSurface.WinForms)]
		public void Resolve_UsesPersistedUIMode(string uiMode, LexicalEditSurface expected)
		{
			var surface = LexicalEditSurfaceResolver.Resolve(uiMode: uiMode);
			Assert.That(surface, Is.EqualTo(expected));
		}

		// --- Tool-gating contract (characterization; Stage 2.2 / migration-program review). ---
		// The currentToolName gate was previously untested. These lock the safety property the
		// migration cares about: an unrecognized tool must NEVER silently resolve to Avalonia, even
		// when UIMode=New or an explicit override is on. A null/whitespace tool means "no tool
		// context supplied" and intentionally delegates to the UIMode/override preference (it is NOT
		// a tool gate); product callers that know their tool must pass it (a Stage 2/11 wiring rule).

		[TestCase("lexiconEdit", true)]
		[TestCase("lexiconEditPopup", true)]
		[TestCase("LEXICONEDIT", true)]      // case-insensitive
		public void SupportsAvaloniaForTool_TrueForSupportedTools(string toolName, bool expected)
		{
			Assert.That(LexicalEditSurfaceResolver.SupportsAvaloniaForTool(toolName), Is.EqualTo(expected));
		}

		[TestCase("interlinearEdit")]
		[TestCase("grammarSketch")]
		[TestCase("someUnregisteredTool")]
		public void SupportsAvaloniaForTool_FalseForUnregisteredTool(string toolName)
		{
			Assert.That(LexicalEditSurfaceResolver.SupportsAvaloniaForTool(toolName), Is.False,
				"an unregistered tool must not advertise Avalonia support");
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void SupportsAvaloniaForTool_TrueForNoToolContext_DelegatesToPreference(string toolName)
		{
			// Documented contract: no tool context => not a tool gate, defer to UIMode/override.
			Assert.That(LexicalEditSurfaceResolver.SupportsAvaloniaForTool(toolName), Is.True);
		}

		[Test]
		public void Resolve_UnregisteredTool_NeverYieldsAvalonia_EvenWithNewUIMode()
		{
			var surface = LexicalEditSurfaceResolver.Resolve(
				uiMode: LexicalEditSurfaceResolver.NewUIMode,
				currentToolName: "someUnregisteredTool");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms),
				"the tool gate must defeat a New preference for an unregistered tool (no silent Avalonia)");
		}

		[Test]
		public void Resolve_UnregisteredTool_NeverYieldsAvalonia_EvenWithExplicitOverride()
		{
			var surface = LexicalEditSurfaceResolver.Resolve(
				overrideEnabled: true,
				currentToolName: "someUnregisteredTool");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms),
				"the tool gate is checked first and must defeat an explicit override for an unregistered tool");
		}

		[Test]
		public void Resolve_SupportedTool_WithNewUIMode_YieldsAvalonia()
		{
			var surface = LexicalEditSurfaceResolver.Resolve(
				uiMode: LexicalEditSurfaceResolver.NewUIMode,
				currentToolName: "lexiconEdit");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.Avalonia));
		}

		[Test]
		public void Resolve_SupportedTool_DefaultsToWinForms_WhenPreferenceUnset()
		{
			var surface = LexicalEditSurfaceResolver.Resolve(currentToolName: "lexiconEdit");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms),
				"a supported tool still defaults to the safe WinForms surface until New is chosen");
		}

		// ----- Browse surface (Stage 3 product wiring, from editable-table) -----

		[Test]
		public void ResolveBrowse_LexiconBrowse_NewMode_SelectsAvalonia()
		{
			var surface = LexicalEditSurfaceResolver.ResolveBrowse(
				uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: "lexiconBrowse");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.Avalonia));
		}

		[Test]
		public void ResolveBrowse_LexiconBrowse_DefaultsToWinForms_WhenPreferenceUnset()
		{
			var surface = LexicalEditSurfaceResolver.ResolveBrowse(currentToolName: "lexiconBrowse");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms),
				"the browse table still defaults to the safe legacy BrowseViewer until New is chosen");
		}

		[Test]
		public void ResolveBrowse_LexiconEdit_NewMode_SelectsAvalonia()
		{
			// The Lexicon Edit tool's left Entries pane reports currentContentControl = "lexiconEdit",
			// so the browse gate must cover it (the primary requested target), not only "lexiconBrowse".
			var surface = LexicalEditSurfaceResolver.ResolveBrowse(
				uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: "lexiconEdit");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.Avalonia));
		}

		[TestCase("concordance")]
		[TestCase("")]               // blank tool does NOT opt a browse surface in
		[TestCase(null)]
		public void ResolveBrowse_NonBrowseOrBlankTool_StaysWinForms_EvenInNewMode(string toolName)
		{
			var surface = LexicalEditSurfaceResolver.ResolveBrowse(
				uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: toolName);
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.WinForms));
		}

		[Test]
		public void ResolveBrowse_ExplicitOverride_WinsOverPreference()
		{
			Assert.That(LexicalEditSurfaceResolver.ResolveBrowse(
				overrideEnabled: true, uiMode: LexicalEditSurfaceResolver.LegacyUIMode,
				currentToolName: "lexiconBrowse"), Is.EqualTo(LexicalEditSurface.Avalonia));
			Assert.That(LexicalEditSurfaceResolver.ResolveBrowse(
				overrideEnabled: false, uiMode: LexicalEditSurfaceResolver.NewUIMode,
				currentToolName: "lexiconBrowse"), Is.EqualTo(LexicalEditSurface.WinForms));
		}
	}

	/// <summary>
	/// Stage 2.2: the app-wide surface registry generalizes the formerly-hardcoded supported-tool list.
	/// A tool opts into the Avalonia surface by registration; unregistered tools never resolve to Avalonia.
	/// </summary>
	[TestFixture]
	public class LexicalEditSurfaceRegistryTests
	{
		[Test]
		public void Default_SupportsShippedTools_NotUnregistered()
		{
			var registry = LexicalEditSurfaceRegistry.CreateDefault();
			Assert.That(registry.SupportsAvalonia("lexiconEdit"), Is.True);
			Assert.That(registry.SupportsAvalonia("lexiconEditPopup"), Is.True);
			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.False);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void Default_NoToolContext_DefersToPreference(string toolName)
		{
			Assert.That(LexicalEditSurfaceRegistry.CreateDefault().SupportsAvalonia(toolName), Is.True);
		}

		[Test]
		public void RegisterSupportedTool_OptsInANewTool()
		{
			var registry = LexicalEditSurfaceRegistry.CreateDefault();
			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.False);

			registry.RegisterSupportedTool("interlinearEdit");

			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.True);
		}

		[Test]
		public void RegisterSupportedTool_BlankName_Throws()
		{
			Assert.That(() => LexicalEditSurfaceRegistry.CreateDefault().RegisterSupportedTool("  "),
				Throws.ArgumentException);
		}

		[Test]
		public void Resolve_WithRegistry_NewlyRegisteredTool_NewUIMode_YieldsAvalonia()
		{
			var registry = LexicalEditSurfaceRegistry.CreateDefault();
			registry.RegisterSupportedTool("interlinearEdit");

			var withRegistration = LexicalEditSurfaceResolver.Resolve(
				registry, uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: "interlinearEdit");
			Assert.That(withRegistration, Is.EqualTo(LexicalEditSurface.Avalonia));

			// Without registering it, the same tool stays on WinForms (registration is required).
			var withoutRegistration = LexicalEditSurfaceResolver.Resolve(
				uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: "interlinearEdit");
			Assert.That(withoutRegistration, Is.EqualTo(LexicalEditSurface.WinForms));
		}

		[Test]
		public void Resolve_NullRegistry_UsesShippedDefault()
		{
			var surface = LexicalEditSurfaceResolver.Resolve(
				(LexicalEditSurfaceRegistry)null,
				uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: "lexiconEdit");
			Assert.That(surface, Is.EqualTo(LexicalEditSurface.Avalonia));
		}
	}

	/// <summary>Tests that the factory never constructs the Avalonia surface when the flag is off.</summary>
	[TestFixture]
	public class LexicalEditSurfaceFactoryTests
	{
		[Test]
		public void Create_FlagOff_DoesNotConstructAvaloniaRuntime()
		{
			var avaloniaBuilds = 0;
			var factory = new LexicalEditSurfaceFactory(
				winFormsSurfaceBuilder: () => "winforms",
				avaloniaSurfaceBuilder: () => { avaloniaBuilds++; return "avalonia"; });

			var result = factory.Create(LexicalEditSurface.WinForms);

			Assert.That(result, Is.EqualTo("winforms"));
			Assert.That(avaloniaBuilds, Is.EqualTo(0), "Avalonia builder must not run when the flag is off.");
			Assert.That(factory.AvaloniaConstructionCount, Is.EqualTo(0));
		}

		[Test]
		public void Create_FlagOn_ConstructsAvaloniaOnce()
		{
			var avaloniaBuilds = 0;
			var factory = new LexicalEditSurfaceFactory(
				winFormsSurfaceBuilder: () => "winforms",
				avaloniaSurfaceBuilder: () => { avaloniaBuilds++; return "avalonia"; });

			var result = factory.Create(LexicalEditSurface.Avalonia);

			Assert.That(result, Is.EqualTo("avalonia"));
			Assert.That(avaloniaBuilds, Is.EqualTo(1));
			Assert.That(factory.AvaloniaConstructionCount, Is.EqualTo(1));
		}
	}

	/// <summary>
	/// Audits the FwAvalonia assembly's references to prove it carries no native Views or Graphite
	/// dependency, satisfying the migration's "no native viewing or Graphite" requirement at the
	/// assembly-reference level (the headless render test proves it at runtime).
	/// </summary>
	[TestFixture]
	public class FwAvaloniaAssemblyReferenceAuditTests
	{
		[Test]
		public void FwAvaloniaAssembly_HasNoNativeViewsOrGraphiteReferences()
		{
			var referenced = typeof(LexicalEditRegionView).Assembly.GetReferencedAssemblies();
			var forbidden = new[] { "Graphite", "ViewsInterfaces", "Views.dll", "RootSite", "Gecko", "Geckofx" };

			foreach (var name in referenced.Select(r => r.Name))
			{
				foreach (var bad in forbidden)
				{
					Assert.That(
						name.IndexOf(bad, StringComparison.OrdinalIgnoreCase),
						Is.LessThan(0),
						$"FwAvalonia assembly must not reference '{bad}', but references '{name}'.");
				}
			}
		}
	}
}
