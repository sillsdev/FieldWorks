// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// advanced-entry-view: the per-project override store round-trips a patch through the
	/// ConfigurationSettings-folder JSON file (one per class+layout), loads lazily, caches per key, and
	/// treats an empty patch as "delete the file" (undo-to-base leaves no stale override). Corrupt or
	/// mislabeled files degrade to "no override" rather than crashing compose.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideStoreTests
	{
		private string _dir;

		[SetUp]
		public void SetUp()
		{
			_dir = Path.Combine(Path.GetTempPath(), "viewoverride-store-" + Guid.NewGuid().ToString("N"));
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_dir))
				Directory.Delete(_dir, recursive: true);
		}

		private static ViewDefinitionOverride Patch(params ViewOverrideOperation[] ops)
			=> new ViewDefinitionOverride("LexEntry", "Normal", "detail", ops, null);

		private static ViewOverrideOperation Vis(string id, ViewVisibility vis)
			=> new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, id, visibility: vis);

		[Test]
		public void Save_ThenTryGet_RoundTripsThroughDisk()
		{
			var store = new ViewDefinitionOverrideStore(_dir);
			store.Save(Patch(Vis("/#0", ViewVisibility.Never)));

			// A fresh store (no in-memory cache) must read the same patch back from the file.
			var reloaded = new ViewDefinitionOverrideStore(_dir).TryGet("LexEntry", "Normal");

			Assert.That(reloaded, Is.Not.Null);
			Assert.That(reloaded.Operations.Count, Is.EqualTo(1));
			Assert.That(reloaded.Operations[0].StableId, Is.EqualTo("/#0"));
			Assert.That(reloaded.Operations[0].Visibility, Is.EqualTo(ViewVisibility.Never));
		}

		[Test]
		public void TryGet_NoFile_ReturnsNull()
		{
			Assert.That(new ViewDefinitionOverrideStore(_dir).TryGet("LexEntry", "Normal"), Is.Null);
		}

		[Test]
		public void Save_WritesToPredictablePerClassLayoutFile()
		{
			var store = new ViewDefinitionOverrideStore(_dir);
			store.Save(Patch(Vis("/#0", ViewVisibility.Always)));

			var expected = Path.Combine(_dir, "LexEntry.Normal.viewoverride.json");
			Assert.That(File.Exists(expected), Is.True);
			Assert.That(store.PathFor("LexEntry", "Normal"), Is.EqualTo(expected));
		}

		[Test]
		public void Save_EmptyPatch_DeletesTheFile()
		{
			var store = new ViewDefinitionOverrideStore(_dir);
			store.Save(Patch(Vis("/#0", ViewVisibility.Never)));
			Assert.That(File.Exists(store.PathFor("LexEntry", "Normal")), Is.True);

			store.Save(Patch()); // emptied — the project no longer customizes this layout

			Assert.That(File.Exists(store.PathFor("LexEntry", "Normal")), Is.False,
				"an empty override deletes the file so the loader sees the shipped definition");
			Assert.That(store.TryGet("LexEntry", "Normal"), Is.Null);
		}

		[Test]
		public void TryGet_DistinctKeys_AreIsolated()
		{
			var store = new ViewDefinitionOverrideStore(_dir);
			store.Save(Patch(Vis("/#0", ViewVisibility.Never)));
			store.Save(new ViewDefinitionOverride("LexSense", "Normal", "detail",
				new[] { Vis("/#1", ViewVisibility.IfData) }, null));

			Assert.That(store.TryGet("LexEntry", "Normal").Operations[0].StableId, Is.EqualTo("/#0"));
			Assert.That(store.TryGet("LexSense", "Normal").Operations[0].StableId, Is.EqualTo("/#1"));
			Assert.That(store.TryGet("LexSense", "Other"), Is.Null);
		}

		[Test]
		public void TryGet_CorruptFile_ReportsErrorAndReturnsNull()
		{
			Directory.CreateDirectory(_dir);
			File.WriteAllText(Path.Combine(_dir, "LexEntry.Normal.viewoverride.json"), "{ not valid json");
			var store = new ViewDefinitionOverrideStore(_dir);

			Exception captured = null;
			var result = store.TryGet("LexEntry", "Normal", (path, e) => captured = e);

			Assert.That(result, Is.Null, "a corrupt file degrades to no-override, never a crash");
			Assert.That(captured, Is.Not.Null, "the load failure is surfaced to the caller for logging");
		}

		[Test]
		public void TryGet_HeaderMismatch_IsIgnored()
		{
			// A file whose JSON header disagrees with the requested key (renamed/hand-edited) is not used.
			Directory.CreateDirectory(_dir);
			var foreignPatch = new ViewDefinitionOverride("LexSense", "Normal", "detail",
				new[] { Vis("/#0", ViewVisibility.Never) }, null);
			File.WriteAllText(Path.Combine(_dir, "LexEntry.Normal.viewoverride.json"),
				ViewDefinitionOverrideJsonSerializer.Serialize(foreignPatch));

			Assert.That(new ViewDefinitionOverrideStore(_dir).TryGet("LexEntry", "Normal"), Is.Null);
		}

		[Test]
		public void TryGet_CachesAcrossCalls_AndSaveRefreshesCache()
		{
			var store = new ViewDefinitionOverrideStore(_dir);
			Assert.That(store.TryGet("LexEntry", "Normal"), Is.Null);

			// Save updates the in-memory cache, so the next TryGet returns the new patch without re-reading.
			store.Save(Patch(Vis("/#0", ViewVisibility.Never)));
			Assert.That(store.TryGet("LexEntry", "Normal"), Is.Not.Null);
		}
	}
}
