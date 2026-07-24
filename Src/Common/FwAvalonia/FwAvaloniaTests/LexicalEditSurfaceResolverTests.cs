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

		// --- Deferred edit surfaces (interlinear, rule-formula). Their tools are deliberately NOT
		// registered, so even UIMode=New falls back to the legacy WinForms surface. A future PR
		// activates each surface by moving its tool name(s) into the active registry list.

		[Test]
		public void InertFollowUpSurfacesFallBackToLegacy_EditSurface()
		{
			foreach (var tool in LexicalEditSurfaceRegistry.Phase1FollowUpSurfaceTools)
			{
				Assert.That(LexicalEditSurfaceResolver.SupportsAvaloniaForTool(tool), Is.False,
					$"deferred edit-surface tool '{tool}' must be inert (unregistered) in this PR");
				Assert.That(
					LexicalEditSurfaceResolver.Resolve(uiMode: LexicalEditSurfaceResolver.NewUIMode, currentToolName: tool),
					Is.EqualTo(LexicalEditSurface.WinForms),
					$"deferred tool '{tool}' must fall back to WinForms even under UIMode=New");
			}
		}

		[TestCase("lexiconEdit")]
		[TestCase("notebookEdit")]
		[TestCase("posEdit")]
		public void BaseDetailEditorTools_StayActive(string tool)
		{
			Assert.That(LexicalEditSurfaceResolver.SupportsAvaloniaForTool(tool), Is.True,
				$"base detail-editor tool '{tool}' must remain registered/active");
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

		[TestCase("New", "New")]
		[TestCase("new", "New")]
		[TestCase("NEW", "New")]
		[TestCase("Legacy", "Legacy")]
		[TestCase("", "Legacy")]
		[TestCase("   ", "Legacy")]
		[TestCase(null, "Legacy")]
		[TestCase("garbage", "Legacy")]
		public void NormalizeUIMode_FailsClosedToLegacy(string input, string expected)
		{
			Assert.That(LexicalEditSurfaceResolver.NormalizeUIMode(input), Is.EqualTo(expected));
		}

		// --- Disabled-tools CSV round-trip (the "Manage Individual Features" persistence format). No prior
		// test exercised ParseDisabledTools/SerializeDisabledTools/IsToolDisabledByUser at all. ---

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void ParseDisabledTools_NullOrBlank_ReturnsEmptySet(string csv)
		{
			Assert.That(LexicalEditSurfaceResolver.ParseDisabledTools(csv), Is.Empty);
		}

		[Test]
		public void ParseDisabledTools_TrimsWhitespaceAroundEachEntry()
		{
			var result = LexicalEditSurfaceResolver.ParseDisabledTools(" lexiconEdit ,  notebookEdit  ");
			Assert.That(result, Is.EquivalentTo(new[] { "lexiconEdit", "notebookEdit" }));
		}

		[Test]
		public void ParseDisabledTools_IgnoresEmptyEntriesFromDoubledOrTrailingCommas()
		{
			// Split(',') on "a,,b," yields ["a", "", "b", ""] -- the blank entries must not become spurious
			// "disabled" tool names (there is no tool named "").
			var result = LexicalEditSurfaceResolver.ParseDisabledTools("lexiconEdit,,notebookEdit,");
			Assert.That(result, Is.EquivalentTo(new[] { "lexiconEdit", "notebookEdit" }));
		}

		[Test]
		public void ParseDisabledTools_IsCaseInsensitive_AndDedupes()
		{
			// disabled sets are looked up case-insensitively (IsToolDisabledByUser), so parsing must dedupe
			// case-variant duplicates rather than keeping both as distinct entries.
			var result = LexicalEditSurfaceResolver.ParseDisabledTools("lexiconEdit,LEXICONEDIT,LexiconEdit");
			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result.Contains("lexiconedit"), Is.True, "lookups must be case-insensitive");
		}

		[Test]
		public void SerializeDisabledTools_NullOrEmpty_ReturnsEmptyString()
		{
			Assert.That(LexicalEditSurfaceResolver.SerializeDisabledTools(null), Is.EqualTo(string.Empty));
			Assert.That(LexicalEditSurfaceResolver.SerializeDisabledTools(Array.Empty<string>()), Is.EqualTo(string.Empty));
		}

		[Test]
		public void SerializeDisabledTools_JoinsWithCommas_PreservingGivenOrder()
		{
			// SerializeDisabledTools does not sort -- callers (the Feature Manager dialog) are responsible for
			// supplying a deterministic order. This pins that it is a plain join, not an implicit sort.
			var csv = LexicalEditSurfaceResolver.SerializeDisabledTools(new[] { "posEdit", "lexiconEdit" });
			Assert.That(csv, Is.EqualTo("posEdit,lexiconEdit"));
		}

		[Test]
		public void ParseThenSerialize_RoundTripsACanonicalCsv_Unchanged()
		{
			const string canonical = "lexiconEdit,notebookEdit";
			var roundTripped = LexicalEditSurfaceResolver.SerializeDisabledTools(
				LexicalEditSurfaceResolver.ParseDisabledTools(canonical));

			// ParseDisabledTools returns a HashSet, whose enumeration order is an implementation detail, not a
			// contract -- so a direct Parse->Serialize round trip is NOT guaranteed to preserve order or exact
			// text for arbitrary input (the Feature Manager dialog avoids this by re-deriving the CSV from its
			// own ordered rows, not from the parsed set -- see LexicalEditFeatureManagerDialogTests). What IS
			// guaranteed, and what this pins, is that the round trip preserves the SET of names.
			Assert.That(LexicalEditSurfaceResolver.ParseDisabledTools(roundTripped),
				Is.EquivalentTo(LexicalEditSurfaceResolver.ParseDisabledTools(canonical)));
		}

		[TestCase("lexiconEdit,notebookEdit", "lexiconEdit", true)]
		[TestCase("lexiconEdit,notebookEdit", "LEXICONEDIT", true, TestName = "IsToolDisabledByUser_CaseInsensitive")]
		[TestCase("lexiconEdit,notebookEdit", "posEdit", false)]
		[TestCase("", "lexiconEdit", false)]
		[TestCase(null, "lexiconEdit", false)]
		public void IsToolDisabledByUser_LooksUpAgainstTheParsedSet(string csv, string toolName, bool expected)
		{
			Assert.That(LexicalEditSurfaceResolver.IsToolDisabledByUser(csv, toolName), Is.EqualTo(expected));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void IsToolDisabledByUser_BlankToolName_AlwaysFalse(string toolName)
		{
			// A blank tool name must never match, even against a CSV that (invalidly) contains a blank entry.
			Assert.That(LexicalEditSurfaceResolver.IsToolDisabledByUser("lexiconEdit,,notebookEdit", toolName), Is.False);
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
