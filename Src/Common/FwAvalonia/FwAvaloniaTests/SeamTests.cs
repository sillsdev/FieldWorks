// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The crash guard for WinForms-hosted Avalonia: MicroCom proxy finalizers post their
	/// native Release through the captured SynchronizationContext; when the WinForms marshaling
	/// window is gone that post throws on the FINALIZER thread and terminates the process. The
	/// wrapper swallows exactly those marshal failures on POST (the finalizer path) and passes
	/// everything else through -- synchronous Send failures still surface to the waiting caller.
	/// </summary>
	[TestFixture]
	public class FinalizerSafeSynchronizationContextTests
	{
		private sealed class DeadTargetContext : System.Threading.SynchronizationContext
		{
			public override void Post(System.Threading.SendOrPostCallback d, object state)
				=> throw new InvalidOperationException("Invoke or BeginInvoke cannot be called on a control until the window handle has been created.");

			public override void Send(System.Threading.SendOrPostCallback d, object state)
				=> throw new ObjectDisposedException("marshaling control");
		}

		private sealed class RecordingContext : System.Threading.SynchronizationContext
		{
			public int Posts;
			public override void Post(System.Threading.SendOrPostCallback d, object state)
			{
				Posts++;
				d(state);
			}
		}

		[Test]
		public void Post_SwallowsDeadMarshalingTargetFailures_InsteadOfKillingTheProcess()
		{
			var dropped = new System.Collections.Generic.List<string>();
			var originalHandler = FinalizerSafeSynchronizationContext.NonMicroComDropHandler;
			FinalizerSafeSynchronizationContext.NonMicroComDropHandler = dropped.Add;
			try
			{
				var guarded = new FinalizerSafeSynchronizationContext(new DeadTargetContext());
				Assert.DoesNotThrow(() => guarded.Post(_ => { }, null),
					"the exact failure mode of MicroComProxyBase.Finalize must not propagate");
				Assert.That(dropped, Has.Count.EqualTo(1),
					"a dropped NON-MicroCom post (this test's lambda) must route through the " +
					"loud-in-Debug drop handler — a silently vanished async continuation is a hang");
				Assert.Throws<ObjectDisposedException>(() => guarded.Send(_ => { }, null),
					"Send is synchronous caller work, not a finalizer Release — a silently skipped " +
					"callback would corrupt the waiting caller, so the failure must surface");
			}
			finally
			{
				FinalizerSafeSynchronizationContext.NonMicroComDropHandler = originalHandler;
			}
		}

		[Test]
		public void IsMicroComCallback_NamespaceAssumption_StillMatchesTheReferencedAvalonia()
		{
			// The classifier assumes MicroCom finalizer posts come from callback methods declared
			// under the "MicroCom." namespace. Pin that against the actual referenced assemblies:
			// if an Avalonia bump relocates MicroComProxyBase, this fails loudly instead of the
			// classifier silently treating every finalizer Release as a "dropped" post.
			var proxyBase = Type.GetType("MicroCom.Runtime.MicroComProxyBase, MicroCom.Runtime", false)
				?? AppDomain.CurrentDomain.GetAssemblies()
					.Select(a => a.GetType("MicroCom.Runtime.MicroComProxyBase", false))
					.FirstOrDefault(t => t != null);
			Assert.That(proxyBase, Is.Not.Null,
				"MicroCom.Runtime.MicroComProxyBase not found in the referenced assemblies — " +
				"update FinalizerSafeSynchronizationContext.IsMicroComCallback's namespace check");
			Assert.That(proxyBase.FullName, Does.StartWith("MicroCom."));

			Assert.That(FinalizerSafeSynchronizationContext.IsMicroComCallback(_ => { }), Is.False,
				"a test-declared callback is not MicroCom");
		}

		[Test]
		public void Post_DelegatesToTheRealContext_WhenAlive()
		{
			var inner = new RecordingContext();
			var guarded = new FinalizerSafeSynchronizationContext(inner);
			var ran = false;
			guarded.Post(_ => ran = true, null);
			Assert.That(inner.Posts, Is.EqualTo(1));
			Assert.That(ran, Is.True, "normal posts flow through unchanged");
		}

		[Test]
		public void Install_WrapsTheAmbientContext_AndIsIdempotent()
		{
			var original = System.Threading.SynchronizationContext.Current;
			try
			{
				System.Threading.SynchronizationContext.SetSynchronizationContext(
					new System.Threading.SynchronizationContext());
				FinalizerSafeSynchronizationContext.InstallOnCurrentThread();
				var installed = System.Threading.SynchronizationContext.Current;
				Assert.That(installed, Is.InstanceOf<FinalizerSafeSynchronizationContext>());

				FinalizerSafeSynchronizationContext.InstallOnCurrentThread();
				Assert.That(System.Threading.SynchronizationContext.Current, Is.SameAs(installed),
					"re-install must not double-wrap");
			}
			finally
			{
				System.Threading.SynchronizationContext.SetSynchronizationContext(original);
			}
		}
	}

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
	public class DetailLifetimeAndSchedulerTests
	{
		private sealed class Spy : IDisposable
		{
			private readonly Action _onDispose;
			public Spy(Action onDispose) { _onDispose = onDispose; }
			public void Dispose() => _onDispose();
		}

		[Test]
		public void DetailLifetime_DisposesRegistered_InReverseOrder_Once()
		{
			var order = new System.Collections.Generic.List<int>();
			var detail = new DetailLifetime();
			detail.Register(new Spy(() => order.Add(1)));
			detail.Register(new Spy(() => order.Add(2)));

			detail.Dispose();
			detail.Dispose(); // idempotent

			Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
			Assert.That(detail.IsDisposed, Is.True);
		}

		[Test]
		public void DetailLifetime_LateRegistration_DisposesImmediately()
		{
			var disposed = false;
			var detail = new DetailLifetime();
			detail.Dispose();
			detail.Register(new Spy(() => disposed = true));
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

	/// <summary>
	/// FwAvaloniaPlatform.IsHeadless resolves Avalonia internals BY STRING NAME (AvaloniaLocator in
	/// Avalonia.Base; IWindowingPlatform in Avalonia.Controls; AvaloniaLocator.Current + its GetService).
	/// Unlike a public API, a version bump can relocate these without a compile break, which would leave
	/// the reflection returning null forever -- silently reporting "not headless" and disabling
	/// the
	/// headless-embed no-op for the WHOLE suite. This pins each target against the referenced Avalonia,
	/// failing loudly (mirroring the MicroCom pin above) so a bump forces an FwAvaloniaPlatform update.
	/// </summary>
	[TestFixture]
	public class FwAvaloniaPlatformReflectionPinTests
	{
		[Test]
		public void ReflectionTargets_StillResolve_InTheReferencedAvalonia()
		{
			var baseAsm = Assembly.Load("Avalonia.Base");
			var controlsAsm = Assembly.Load("Avalonia.Controls");

			var locatorType = baseAsm.GetType("Avalonia.AvaloniaLocator");
			Assert.That(locatorType, Is.Not.Null,
				"Avalonia.AvaloniaLocator no longer resolves in Avalonia.Base — update " +
				"FwAvaloniaPlatform.ResolveWindowingPlatform's type lookup.");

			var windowingPlatformType = controlsAsm.GetType("Avalonia.Platform.IWindowingPlatform");
			Assert.That(windowingPlatformType, Is.Not.Null,
				"Avalonia.Platform.IWindowingPlatform no longer resolves in Avalonia.Controls — update " +
				"FwAvaloniaPlatform.ResolveWindowingPlatform's type lookup.");

			var currentProperty = locatorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
			Assert.That(currentProperty, Is.Not.Null,
				"AvaloniaLocator.Current (public static) is gone — update " +
				"FwAvaloniaPlatform.ResolveWindowingPlatform's Current lookup.");

			var getService = currentProperty.PropertyType
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(m => m.Name == "GetService" && m.GetParameters().Length == 1
					&& m.GetParameters()[0].ParameterType == typeof(Type));
			Assert.That(getService, Is.Not.Null,
				"AvaloniaLocator.Current no longer exposes GetService(Type) — update " +
				"FwAvaloniaPlatform.ResolveWindowingPlatform's GetService invocation.");
		}
	}
}
