// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

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
	/// Audits the POC assembly's references to prove it carries no native Views or Graphite
	/// dependency, satisfying the spike's "no native viewing or Graphite" requirement at the
	/// assembly-reference level (the headless render test proves it at runtime).
	/// </summary>
	[TestFixture]
	public class PocAssemblyReferenceAuditTests
	{
		[Test]
		public void PocAssembly_HasNoNativeViewsOrGraphiteReferences()
		{
			var referenced = typeof(PocLexEntrySlice).Assembly.GetReferencedAssemblies();
			var forbidden = new[] { "Graphite", "ViewsInterfaces", "Views.dll", "RootSite", "Gecko", "Geckofx" };

			foreach (var name in referenced.Select(r => r.Name))
			{
				foreach (var bad in forbidden)
				{
					Assert.That(
						name.IndexOf(bad, StringComparison.OrdinalIgnoreCase),
						Is.LessThan(0),
						$"POC assembly must not reference '{bad}', but references '{name}'.");
				}
			}
		}
	}
}
