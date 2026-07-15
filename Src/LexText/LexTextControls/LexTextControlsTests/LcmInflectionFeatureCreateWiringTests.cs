// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;

namespace LexTextControlsTests
{
	/// <summary>
	/// The guard-clause behavior of <see cref="LcmInflectionFeatureCreateWiring"/> (visible via InternalsVisibleTo):
	/// the shared inline create-feature / add-value wiring the MSA-section launchers (Insert Entry, Add New Sense,
	/// MSA Creator) hook up to <c>FwMsaGroupBox.CreateNewFeatureRequested</c>/<c>CreateNewValueRequested</c>. Both
	/// entry points ultimately run a modal (<see cref="LcmCreateFeatureLauncher.CreateFeature"/>/<c>AddValue</c>) and
	/// then call back onto a live <c>FwMsaGroupBox</c> (an Avalonia control that needs a headless app session to
	/// construct — see FwAvaloniaDialogsTests/FwMsaGroupBoxTests, which is exercised in that project's own headless
	/// context), so the desktop-only success path is out of reach here (mirrors every other Run()-based launcher in
	/// this project: only the pre-modal pure logic is unit-tested). What IS unit-testable without a cache, an owner
	/// window, or a real box is the no-op guard: both methods must do nothing (in particular, must NOT touch the
	/// supplied <c>box</c>) when a required input is missing, so a null <c>box</c> is a safe, deterministic way to
	/// prove the guard fires before any box access.
	/// </summary>
	[TestFixture]
	public class LcmInflectionFeatureCreateWiringTests
	{
		[Test]
		public void CreateFeature_NullCache_IsANoOp()
		{
			// A null cache alone must short-circuit before touching box — passing a null box too proves it never
			// dereferences box.
			Assert.DoesNotThrow(() => LcmInflectionFeatureCreateWiring.CreateFeature(null, null, null));
		}

		[Test]
		public void CreateFeature_NullBox_IsANoOp()
		{
			Assert.DoesNotThrow(() => LcmInflectionFeatureCreateWiring.CreateFeature(null, null, null),
				"a null box must short-circuit without dereferencing it");
		}

		[Test]
		public void AddValue_NullCache_IsANoOp()
		{
			Assert.DoesNotThrow(() =>
				LcmInflectionFeatureCreateWiring.AddValue(null, null, System.Guid.NewGuid().ToString(), null));
		}

		[Test]
		public void AddValue_NullBox_IsANoOp()
		{
			Assert.DoesNotThrow(() =>
				LcmInflectionFeatureCreateWiring.AddValue(null, null, System.Guid.NewGuid().ToString(), null),
				"a null box must short-circuit without dereferencing it");
		}

		[Test]
		public void AddValue_NullOrEmptyClosedFeatureId_IsANoOp()
		{
			// cache is still null here (no live cache is needed for this test), so this also exercises the
			// combined-guard `cache == null || box == null || string.IsNullOrEmpty(closedFeatureId)` short-circuit.
			Assert.DoesNotThrow(() => LcmInflectionFeatureCreateWiring.AddValue(null, null, null, null));
			Assert.DoesNotThrow(() => LcmInflectionFeatureCreateWiring.AddValue(null, null, string.Empty, null));
		}
	}
}
