// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	/// <summary>Content that counts how often it is disposed.</summary>
	internal sealed class DisposalSpy : Avalonia.Controls.Control, IDisposable
	{
		public int Disposals;

		public void Dispose() => Disposals++;
	}

	[TestFixture]
	public class DetailHostControlTests
	{
		private sealed class TestHost : AvaloniaHostControlBase
		{
			public void Set(Avalonia.Controls.Control content) => SetHostContent(content);

			public bool OfferUndoRedoKey(Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers)
				=> RaiseUndoRedoKeyUnhandled(key, modifiers);
		}

		// LT-22820: the owner matches the key against FieldWorks' shortcuts, which are WinForms
		// key data.
		[AvaloniaTest]
		public void UnhandledUndoRedoKey_ReachesTheOwner_AsWinFormsKeyData()
		{
			using (var host = new TestHost())
			{
				var seen = Keys.None;
				host.UndoRedoKeyUnhandled += (s, e) =>
				{
					seen = e.KeyData;
					e.Handled = true;
				};

				var taken = host.OfferUndoRedoKey(Avalonia.Input.Key.Z,
					Avalonia.Input.KeyModifiers.Control);

				Assert.That(seen, Is.EqualTo(Keys.Control | Keys.Z));
				Assert.That(taken, Is.True, "the owner's Handled comes back");
			}
		}

		[AvaloniaTest]
		public void UnhandledUndoRedoKey_WithNoOwner_IsNotTaken()
		{
			using (var host = new TestHost())
			{
				Assert.That(host.OfferUndoRedoKey(Avalonia.Input.Key.Y,
					Avalonia.Input.KeyModifiers.Control), Is.False);
			}
		}

		[Test]
		public void ToWinFormsKeys_MapsLettersAndModifiers_AndNothingElse()
		{
			Assert.That(AvaloniaHostControlBase.ToWinFormsKeys(Avalonia.Input.Key.Y,
				Avalonia.Input.KeyModifiers.Control), Is.EqualTo(Keys.Control | Keys.Y));
			Assert.That(AvaloniaHostControlBase.ToWinFormsKeys(Avalonia.Input.Key.Z,
				Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift),
				Is.EqualTo(Keys.Control | Keys.Shift | Keys.Z));
			Assert.That(AvaloniaHostControlBase.ToWinFormsKeys(Avalonia.Input.Key.A,
				Avalonia.Input.KeyModifiers.Alt), Is.EqualTo(Keys.Alt | Keys.A));
			Assert.That(AvaloniaHostControlBase.ToWinFormsKeys(Avalonia.Input.Key.F5,
				Avalonia.Input.KeyModifiers.Control), Is.EqualTo(Keys.None));
		}

		[AvaloniaTest]
		public void SwappingContent_DisposesWhatItReplaces_AndNothingElse()
		{
			using (var host = new TestHost())
			{
				var first = new DisposalSpy();
				var second = new DisposalSpy();
				host.Set(first);

				host.Set(second);
				Assert.That(first.Disposals, Is.EqualTo(1), "the replaced content is disposed");
				Assert.That(second.Disposals, Is.Zero, "the shown content is not");

				host.Set(second);
				Assert.That(second.Disposals, Is.Zero, "setting the shown content again keeps it");

				host.ShowMessage("nothing selected");
				Assert.That(second.Disposals, Is.EqualTo(1), "a message replaces it too");
			}
		}

		[AvaloniaTest]
		public void DisposingTheHost_DisposesWhatItShows()
		{
			var spy = new DisposalSpy();
			var host = new TestHost();
			host.Set(spy);

			host.Dispose();

			Assert.That(spy.Disposals, Is.EqualTo(1));
		}

		// A detail pane host claims the arrow keys (never Enter) for the hosted Avalonia control. The
		// claiming decision is shared by every detail host through InputKeyClaimPolicy; the pane case
		// passes claimEnterKey:false so Enter keeps its normal meaning in the pane.
		private static bool ShouldBypass(bool hostContainsFocus, int keyCode)
			=> InputKeyClaimPolicy.ShouldClaimKey((Keys)keyCode, hostContainsFocus, claimEnterKey: false);

		[Test]
		public void DirectionalKeys_AreBypassed_WhenAvaloniaHostContainsFocus()
		{
			Assert.That(ShouldBypass(true, 0x26), Is.True);
			Assert.That(ShouldBypass(true, 0x28), Is.True);
			Assert.That(ShouldBypass(true, 0x25), Is.True);
			Assert.That(ShouldBypass(true, 0x27), Is.True);
		}

		[Test]
		public void TabKey_Bypassed_WhenAvaloniaHostContainsFocus()
		{
			Assert.That(ShouldBypass(true, 0x09), Is.True);
		}

		[Test]
		public void NonDirectionalKeys_AndUnfocusedHost_AreNotBypassed()
		{
			Assert.That(ShouldBypass(false, 0x26), Is.False);
			Assert.That(ShouldBypass(false, 0x09), Is.False);
			Assert.That(ShouldBypass(true, 0x0D), Is.False);
		}
	}
}