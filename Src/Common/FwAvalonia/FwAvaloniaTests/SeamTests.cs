// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class RefreshCoordinatorTests
	{
		[Test]
		public void RequestRefresh_RunsImmediately_WhenNotSuspended()
		{
			var c = new RefreshCoordinator();
			Assert.That(c.RequestRefresh(), Is.True);
			Assert.That(c.RefreshPending, Is.False);
		}

		[Test]
		public void RequestRefresh_IsSuppressedAndPending_WhenSuspended()
		{
			var c = new RefreshCoordinator();
			c.BeginSuspend();
			Assert.That(c.RequestRefresh(), Is.False, "refresh should be suppressed while suspended");
			Assert.That(c.RefreshPending, Is.True);
		}

		[Test]
		public void EndSuspend_ReportsRefreshDue_WhenRequestedWhileSuspended()
		{
			var c = new RefreshCoordinator();
			c.BeginSuspend();
			c.RequestRefresh();
			Assert.That(c.EndSuspend(), Is.True, "a refresh requested while suspended is due on release");
			Assert.That(c.RefreshPending, Is.False);
		}

		[Test]
		public void EndSuspend_ReportsNothingDue_WhenNoRefreshRequested()
		{
			var c = new RefreshCoordinator();
			c.BeginSuspend();
			Assert.That(c.EndSuspend(), Is.False);
		}
	}

	[TestFixture]
	public class LexicalEditorRegistryTests
	{
		[Test]
		public void Resolve_ReturnsFallback_ForUnregisteredKey()
		{
			var registry = new LexicalEditorRegistry(fallbackHandler: "legacy");
			Assert.That(registry.IsRegistered("multistring"), Is.False);
			Assert.That(registry.Resolve("multistring"), Is.EqualTo("legacy"));
		}

		[Test]
		public void Resolve_ReturnsRegisteredHandler_OverFallback()
		{
			var registry = new LexicalEditorRegistry(fallbackHandler: "legacy");
			registry.Register("multistring", "avalonia-text");
			Assert.That(registry.IsRegistered("multistring"), Is.True);
			Assert.That(registry.Resolve("multistring"), Is.EqualTo("avalonia-text"));
		}

		[Test]
		public void Register_RejectsNullHandlerAndEmptyKey()
		{
			var registry = new LexicalEditorRegistry();
			Assert.That(() => registry.Register("k", null), Throws.ArgumentNullException);
			Assert.That(() => registry.Register("", "h"), Throws.ArgumentException);
		}
	}

	[TestFixture]
	public class RegionLifetimeAndSchedulerTests
	{
		private sealed class Spy : IDisposable
		{
			private readonly Action _onDispose;
			public Spy(Action onDispose) { _onDispose = onDispose; }
			public void Dispose() => _onDispose();
		}

		[Test]
		public void RegionLifetime_DisposesRegistered_InReverseOrder_Once()
		{
			var order = new System.Collections.Generic.List<int>();
			var region = new RegionLifetime();
			region.Register(new Spy(() => order.Add(1)));
			region.Register(new Spy(() => order.Add(2)));

			region.Dispose();
			region.Dispose(); // idempotent

			Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
			Assert.That(region.IsDisposed, Is.True);
		}

		[Test]
		public void RegionLifetime_LateRegistration_DisposesImmediately()
		{
			var disposed = false;
			var region = new RegionLifetime();
			region.Dispose();
			region.Register(new Spy(() => disposed = true));
			Assert.That(disposed, Is.True);
		}

		[Test]
		public void ImmediateUiScheduler_RunsSynchronously()
		{
			var scheduler = new ImmediateUiScheduler();
			var ran = false;
			scheduler.Post(() => ran = true);
			Assert.That(ran, Is.True);
			Assert.That(scheduler.IsOnUiThread, Is.True);
		}

		[Test]
		public void InMemoryPropertyStateStore_RoundTripsTypedValues()
		{
			var store = new InMemoryPropertyStateStore();
			store.Set("count", 7);
			Assert.That(store.TryGet<int>("count", out var v), Is.True);
			Assert.That(v, Is.EqualTo(7));
			Assert.That(store.TryGet<string>("count", out _), Is.False, "wrong type should not match");
			Assert.That(store.Remove("count"), Is.True);
			Assert.That(store.TryGet<int>("count", out _), Is.False);
		}
	}

	[TestFixture]
	public class MorphTypeSwapLogicTests
	{
		[TestCase(MorphTypeKind.Root)]
		[TestCase(MorphTypeKind.Stem)]
		[TestCase(MorphTypeKind.BoundRoot)]
		[TestCase(MorphTypeKind.BoundStem)]
		[TestCase(MorphTypeKind.Enclitic)]
		[TestCase(MorphTypeKind.Proclitic)]
		[TestCase(MorphTypeKind.Clitic)]
		[TestCase(MorphTypeKind.Particle)]
		[TestCase(MorphTypeKind.Phrase)]
		[TestCase(MorphTypeKind.DiscontiguousPhrase)]
		public void IsStemType_True_ForStemLikeTypes(MorphTypeKind kind)
		{
			Assert.That(MorphTypeSwapLogic.IsStemType(kind), Is.True);
		}

		[TestCase(MorphTypeKind.Prefix)]
		[TestCase(MorphTypeKind.Suffix)]
		[TestCase(MorphTypeKind.Infix)]
		[TestCase(MorphTypeKind.Simulfix)]
		[TestCase(MorphTypeKind.Suprafix)]
		[TestCase(MorphTypeKind.Circumfix)]
		[TestCase(MorphTypeKind.PrefixingInterfix)]
		[TestCase(MorphTypeKind.InfixingInterfix)]
		[TestCase(MorphTypeKind.SuffixingInterfix)]
		public void IsStemType_False_ForAffixLikeTypes(MorphTypeKind kind)
		{
			Assert.That(MorphTypeSwapLogic.IsStemType(kind), Is.False);
		}

		[Test]
		public void Analyze_StemToAffix_RequiresDataLossPrompt()
		{
			var d = MorphTypeSwapLogic.Analyze(MorphTypeKind.Stem, MorphTypeKind.Suffix);
			Assert.That(d.RequiresDataLossPrompt, Is.True);
			Assert.That(d.Direction, Is.EqualTo(MorphSwapDirection.StemToAffix));
		}

		[Test]
		public void Analyze_AffixToStem_RequiresDataLossPrompt()
		{
			var d = MorphTypeSwapLogic.Analyze(MorphTypeKind.Prefix, MorphTypeKind.Root);
			Assert.That(d.RequiresDataLossPrompt, Is.True);
			Assert.That(d.Direction, Is.EqualTo(MorphSwapDirection.AffixToStem));
		}

		[Test]
		public void Analyze_SameSide_DoesNotPrompt()
		{
			Assert.That(MorphTypeSwapLogic.Analyze(MorphTypeKind.Stem, MorphTypeKind.Root).RequiresDataLossPrompt, Is.False);
			Assert.That(MorphTypeSwapLogic.Analyze(MorphTypeKind.Prefix, MorphTypeKind.Suffix).RequiresDataLossPrompt, Is.False);
			Assert.That(MorphTypeSwapLogic.Analyze(MorphTypeKind.Stem, MorphTypeKind.Stem).RequiresDataLossPrompt, Is.False);
		}
	}
}
