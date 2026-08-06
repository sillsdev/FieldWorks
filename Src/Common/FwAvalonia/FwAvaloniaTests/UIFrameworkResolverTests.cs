// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Pure-logic tests for the two-adapter feature flag and control factory. No Avalonia runtime
	/// is required, which is itself part of the evidence: the default (flag off) path constructs
	/// nothing Avalonia.
	/// </summary>
	[TestFixture]
	public class UIFrameworkResolverTests
	{
		[Test]
		public void Resolve_DefaultsToWinForms_WhenFlagUnset()
		{
			var framework = UIFrameworkResolver.Resolve();
			Assert.That(framework, Is.EqualTo(UIFramework.Legacy));
		}

		[Test]
		public void Resolve_OverrideWinsOverPersistedUIMode()
		{
			var winForms = UIFrameworkResolver.Resolve(
				overrideEnabled: false,
				uiMode: UIFrameworkResolver.NewUIMode);
			Assert.That(winForms, Is.EqualTo(UIFramework.Legacy));

			var avalonia = UIFrameworkResolver.Resolve(
				overrideEnabled: true,
				uiMode: UIFrameworkResolver.LegacyUIMode);
			Assert.That(avalonia, Is.EqualTo(UIFramework.Avalonia));
		}

		[TestCase(UIFrameworkResolver.LegacyUIMode, UIFramework.Legacy)]
		[TestCase(UIFrameworkResolver.NewUIMode, UIFramework.Avalonia)]
		[TestCase(null, UIFramework.Legacy)]
		[TestCase("", UIFramework.Legacy)]
		[TestCase("SomethingElse", UIFramework.Legacy)]
		public void Resolve_UsesPersistedUIMode(string uiMode, UIFramework expected)
		{
			var framework = UIFrameworkResolver.Resolve(uiMode: uiMode);
			Assert.That(framework, Is.EqualTo(expected));
		}

		// --- Tool-gating contract (characterization). ---
		// These lock the safety property the
		// migration cares about: an unrecognized tool must NEVER silently resolve to Avalonia, even
		// when UIMode=New or an explicit override is on. A null/whitespace tool means "no tool
		// context supplied" and intentionally delegates to the UIMode/override preference (it is NOT
		// a tool gate); product callers that know their tool must pass it.

		[TestCase("lexiconEdit", true)]
		[TestCase("lexiconEditPopup", true)]
		[TestCase("LEXICONEDIT", true)]      // case-insensitive
		public void SupportsAvaloniaForTool_TrueForSupportedTools(string toolName, bool expected)
		{
			Assert.That(UIFrameworkResolver.SupportsAvaloniaForTool(toolName), Is.EqualTo(expected));
		}

		[TestCase("interlinearEdit")]
		[TestCase("grammarSketch")]
		[TestCase("someUnregisteredTool")]
		public void SupportsAvaloniaForTool_FalseForUnregisteredTool(string toolName)
		{
			Assert.That(UIFrameworkResolver.SupportsAvaloniaForTool(toolName), Is.False,
				"an unregistered tool must not advertise Avalonia support");
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void SupportsAvaloniaForTool_TrueForNoToolContext_DelegatesToPreference(string toolName)
		{
			// Documented contract: no tool context => not a tool gate, defer to UIMode/override.
			Assert.That(UIFrameworkResolver.SupportsAvaloniaForTool(toolName), Is.True);
		}

		[Test]
		public void Resolve_UnregisteredTool_NeverYieldsAvalonia_EvenWithNewUIMode()
		{
			var framework = UIFrameworkResolver.Resolve(
				uiMode: UIFrameworkResolver.NewUIMode,
				currentToolName: "someUnregisteredTool");
			Assert.That(framework, Is.EqualTo(UIFramework.Legacy),
				"the tool gate must defeat a New preference for an unregistered tool (no silent Avalonia)");
		}

		[Test]
		public void Resolve_UnregisteredTool_NeverYieldsAvalonia_EvenWithExplicitOverride()
		{
			var framework = UIFrameworkResolver.Resolve(
				overrideEnabled: true,
				currentToolName: "someUnregisteredTool");
			Assert.That(framework, Is.EqualTo(UIFramework.Legacy),
				"the tool gate is checked first and must defeat an explicit override for an unregistered tool");
		}

		// --- Deferred editors (interlinear, rule-formula): their tools are deliberately NOT
		// registered, so even UIMode=New falls back to legacy WinForms. Activating one
		// means moving its tool name(s) into the active registry list.

		[Test]
		public void InertFollowUpToolsFallBackToLegacy_UIFramework()
		{
			foreach (var tool in UIFrameworkRegistry.Phase1FollowUpTools)
			{
				Assert.That(UIFrameworkResolver.SupportsAvaloniaForTool(tool), Is.False,
					$"deferred tool '{tool}' must be inert (unregistered)");
				Assert.That(
					UIFrameworkResolver.Resolve(uiMode: UIFrameworkResolver.NewUIMode, currentToolName: tool),
					Is.EqualTo(UIFramework.Legacy),
					$"deferred tool '{tool}' must fall back to WinForms even under UIMode=New");
			}
		}

		[TestCase("lexiconEdit")]
		[TestCase("notebookEdit")]
		[TestCase("posEdit")]
		public void BaseDetailEditorTools_StayActive(string tool)
		{
			Assert.That(UIFrameworkResolver.SupportsAvaloniaForTool(tool), Is.True,
				$"base detail-editor tool '{tool}' must remain registered/active");
		}

		[Test]
		public void Resolve_SupportedTool_WithNewUIMode_YieldsAvalonia()
		{
			var framework = UIFrameworkResolver.Resolve(
				uiMode: UIFrameworkResolver.NewUIMode,
				currentToolName: "lexiconEdit");
			Assert.That(framework, Is.EqualTo(UIFramework.Avalonia));
		}

		[Test]
		public void Resolve_SupportedTool_DefaultsToWinForms_WhenPreferenceUnset()
		{
			var framework = UIFrameworkResolver.Resolve(currentToolName: "lexiconEdit");
			Assert.That(framework, Is.EqualTo(UIFramework.Legacy),
				"a supported tool still defaults to Legacy until New is chosen");
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
			Assert.That(UIFrameworkResolver.NormalizeUIMode(input), Is.EqualTo(expected));
		}

		// --- Disabled-tools CSV round-trip (the UIModeDisabledTools persistence format). ---

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void ParseDisabledTools_NullOrBlank_ReturnsEmptySet(string csv)
		{
			Assert.That(UIFrameworkResolver.ParseDisabledTools(csv), Is.Empty);
		}

		[Test]
		public void ParseDisabledTools_TrimsWhitespaceAroundEachEntry()
		{
			var result = UIFrameworkResolver.ParseDisabledTools(" lexiconEdit ,  notebookEdit  ");
			Assert.That(result, Is.EquivalentTo(new[] { "lexiconEdit", "notebookEdit" }));
		}

		[Test]
		public void ParseDisabledTools_IgnoresEmptyEntriesFromDoubledOrTrailingCommas()
		{
			// Split(',') on "a,,b," yields ["a", "", "b", ""] -- the blank entries must not become spurious
			// "disabled" tool names (there is no tool named "").
			var result = UIFrameworkResolver.ParseDisabledTools("lexiconEdit,,notebookEdit,");
			Assert.That(result, Is.EquivalentTo(new[] { "lexiconEdit", "notebookEdit" }));
		}

		[Test]
		public void ParseDisabledTools_IsCaseInsensitive_AndDedupes()
		{
			// disabled sets are looked up case-insensitively (IsToolDisabledByUser), so parsing must dedupe
			// case-variant duplicates rather than keeping both as distinct entries.
			var result = UIFrameworkResolver.ParseDisabledTools("lexiconEdit,LEXICONEDIT,LexiconEdit");
			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result.Contains("lexiconedit"), Is.True, "lookups must be case-insensitive");
		}

		[Test]
		public void SerializeDisabledTools_NullOrEmpty_ReturnsEmptyString()
		{
			Assert.That(UIFrameworkResolver.SerializeDisabledTools(null), Is.EqualTo(string.Empty));
			Assert.That(UIFrameworkResolver.SerializeDisabledTools(Array.Empty<string>()), Is.EqualTo(string.Empty));
		}

		[Test]
		public void SerializeDisabledTools_JoinsWithCommas_PreservingGivenOrder()
		{
			// SerializeDisabledTools does not sort -- the caller is responsible for supplying a deterministic
			// order. This pins that it is a plain join, not an implicit sort.
			var csv = UIFrameworkResolver.SerializeDisabledTools(new[] { "posEdit", "lexiconEdit" });
			Assert.That(csv, Is.EqualTo("posEdit,lexiconEdit"));
		}

		[Test]
		public void ParseThenSerialize_RoundTripsACanonicalCsv_Unchanged()
		{
			const string canonical = "lexiconEdit,notebookEdit";
			var roundTripped = UIFrameworkResolver.SerializeDisabledTools(
				UIFrameworkResolver.ParseDisabledTools(canonical));

			// ParseDisabledTools returns a HashSet, whose enumeration order is an implementation detail, not a
			// contract -- so a direct Parse->Serialize round trip is NOT guaranteed to preserve order or exact
			// text for arbitrary input. A caller that needs a stable CSV must re-derive it from its own ordered
			// source, not from the parsed set. What IS guaranteed, and what this pins, is that the round trip
			// preserves the SET of names.
			Assert.That(UIFrameworkResolver.ParseDisabledTools(roundTripped),
				Is.EquivalentTo(UIFrameworkResolver.ParseDisabledTools(canonical)));
		}

		[TestCase("lexiconEdit,notebookEdit", "lexiconEdit", true)]
		[TestCase("lexiconEdit,notebookEdit", "LEXICONEDIT", true, TestName = "IsToolDisabledByUser_CaseInsensitive")]
		[TestCase("lexiconEdit,notebookEdit", "posEdit", false)]
		[TestCase("", "lexiconEdit", false)]
		[TestCase(null, "lexiconEdit", false)]
		public void IsToolDisabledByUser_LooksUpAgainstTheParsedSet(string csv, string toolName, bool expected)
		{
			Assert.That(UIFrameworkResolver.IsToolDisabledByUser(csv, toolName), Is.EqualTo(expected));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void IsToolDisabledByUser_BlankToolName_AlwaysFalse(string toolName)
		{
			// A blank tool name must never match, even against a CSV that (invalidly) contains a blank entry.
			Assert.That(UIFrameworkResolver.IsToolDisabledByUser("lexiconEdit,,notebookEdit", toolName), Is.False);
		}
	}

	/// <summary>
	/// The app-wide registry is the single supported-tool list.
	/// A tool opts in by registration; unregistered tools never resolve to Avalonia.
	/// </summary>
	[TestFixture]
	public class UIFrameworkRegistryTests
	{
		[Test]
		public void Default_SupportsShippedTools_NotUnregistered()
		{
			var registry = UIFrameworkRegistry.CreateDefault();
			Assert.That(registry.SupportsAvalonia("lexiconEdit"), Is.True);
			Assert.That(registry.SupportsAvalonia("lexiconEditPopup"), Is.True);
			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.False);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void Default_NoToolContext_DefersToPreference(string toolName)
		{
			Assert.That(UIFrameworkRegistry.CreateDefault().SupportsAvalonia(toolName), Is.True);
		}

		[Test]
		public void RegisterSupportedTool_OptsInANewTool()
		{
			var registry = UIFrameworkRegistry.CreateDefault();
			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.False);

			registry.RegisterSupportedTool("interlinearEdit");

			Assert.That(registry.SupportsAvalonia("interlinearEdit"), Is.True);
		}

		[Test]
		public void RegisterSupportedTool_BlankName_Throws()
		{
			Assert.That(() => UIFrameworkRegistry.CreateDefault().RegisterSupportedTool("  "),
				Throws.ArgumentException);
		}

		[Test]
		public void Resolve_WithRegistry_NewlyRegisteredTool_NewUIMode_YieldsAvalonia()
		{
			var registry = UIFrameworkRegistry.CreateDefault();
			registry.RegisterSupportedTool("interlinearEdit");

			var withRegistration = UIFrameworkResolver.Resolve(
				registry, uiMode: UIFrameworkResolver.NewUIMode, currentToolName: "interlinearEdit");
			Assert.That(withRegistration, Is.EqualTo(UIFramework.Avalonia));

			// Without registering it, the same tool stays on WinForms (registration is required).
			var withoutRegistration = UIFrameworkResolver.Resolve(
				uiMode: UIFrameworkResolver.NewUIMode, currentToolName: "interlinearEdit");
			Assert.That(withoutRegistration, Is.EqualTo(UIFramework.Legacy));
		}

		[Test]
		public void Resolve_NullRegistry_UsesShippedDefault()
		{
			var framework = UIFrameworkResolver.Resolve(
				(UIFrameworkRegistry)null,
				uiMode: UIFrameworkResolver.NewUIMode, currentToolName: "lexiconEdit");
			Assert.That(framework, Is.EqualTo(UIFramework.Avalonia));
		}
	}

	/// <summary>Tests that the factory never constructs the Avalonia control when the flag is off.</summary>
	[TestFixture]
	public class EditControlFactoryTests
	{
		[Test]
		public void Create_FlagOff_DoesNotConstructAvaloniaRuntime()
		{
			var avaloniaBuilds = 0;
			var factory = new EditControlFactory(
				winFormsControlBuilder: () => "winforms",
				avaloniaControlBuilder: () => { avaloniaBuilds++; return "avalonia"; });

			var result = factory.Create(UIFramework.Legacy);

			Assert.That(result, Is.EqualTo("winforms"));
			Assert.That(avaloniaBuilds, Is.EqualTo(0), "Avalonia builder must not run when the flag is off.");
			Assert.That(factory.AvaloniaConstructionCount, Is.EqualTo(0));
		}

		[Test]
		public void Create_FlagOn_ConstructsAvaloniaOnce()
		{
			var avaloniaBuilds = 0;
			var factory = new EditControlFactory(
				winFormsControlBuilder: () => "winforms",
				avaloniaControlBuilder: () => { avaloniaBuilds++; return "avalonia"; });

			var result = factory.Create(UIFramework.Avalonia);

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
			var referenced = typeof(DataTree).Assembly.GetReferencedAssemblies();
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
