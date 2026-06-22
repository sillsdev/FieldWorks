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
	/// advanced-entry-view (Configure-Columns P1): the per-tool browse-column store round-trips the shown
	/// columns (key + optional width) through a ConfigurationSettings-folder JSON file (one per tool), loads
	/// lazily, caches per tool, and treats an empty configuration as "delete the file" (revert to the shipped
	/// default). Corrupt / version-mismatched / mislabeled files degrade to "no config" rather than crashing.
	/// Mirrors ViewDefinitionOverrideStoreTests.
	/// </summary>
	[TestFixture]
	public class BrowseColumnConfigStoreTests
	{
		private string _dir;

		[SetUp]
		public void SetUp()
			=> _dir = Path.Combine(Path.GetTempPath(), "browsecols-store-" + Guid.NewGuid().ToString("N"));

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_dir))
				Directory.Delete(_dir, recursive: true);
		}

		private static BrowseColumnConfigEntry[] Cols(params (string key, double? width)[] cols)
		{
			var result = new BrowseColumnConfigEntry[cols.Length];
			for (var i = 0; i < cols.Length; i++)
				result[i] = new BrowseColumnConfigEntry(cols[i].key, cols[i].width);
			return result;
		}

		[Test]
		public void Save_ThenTryGet_RoundTripsThroughDisk()
		{
			var store = new BrowseColumnConfigStore(_dir);
			store.Save("lexiconBrowse", Cols(("form|v|Form", 150), ("gloss|a|Gloss", null)));

			// A fresh store (no in-memory cache) must read the same config back from the file.
			var reloaded = new BrowseColumnConfigStore(_dir).TryGet("lexiconBrowse");

			Assert.That(reloaded, Is.Not.Null);
			Assert.That(reloaded.Count, Is.EqualTo(2));
			Assert.That(reloaded[0].Key, Is.EqualTo("form|v|Form"));
			Assert.That(reloaded[0].Width, Is.EqualTo(150));
			Assert.That(reloaded[1].Key, Is.EqualTo("gloss|a|Gloss"));
			Assert.That(reloaded[1].Width, Is.Null);
		}

		[Test]
		public void TryGet_NoFile_ReturnsNull()
			=> Assert.That(new BrowseColumnConfigStore(_dir).TryGet("lexiconBrowse"), Is.Null);

		[Test]
		public void Save_WritesToPredictablePerToolFile()
		{
			var store = new BrowseColumnConfigStore(_dir);
			store.Save("lexiconBrowse", Cols(("form|v|Form", null)));

			var expected = Path.Combine(_dir, "lexiconBrowse.browsecolumns.json");
			Assert.That(File.Exists(expected), Is.True);
			Assert.That(store.PathFor("lexiconBrowse"), Is.EqualTo(expected));
		}

		[Test]
		public void Save_EmptyConfig_DeletesTheFile()
		{
			var store = new BrowseColumnConfigStore(_dir);
			store.Save("lexiconBrowse", Cols(("form|v|Form", null)));
			Assert.That(File.Exists(store.PathFor("lexiconBrowse")), Is.True);

			store.Save("lexiconBrowse", new BrowseColumnConfigEntry[0]); // reverted to default

			Assert.That(File.Exists(store.PathFor("lexiconBrowse")), Is.False,
				"an empty config deletes the file so the loader sees the shipped default columns");
			Assert.That(store.TryGet("lexiconBrowse"), Is.Null);
		}

		[Test]
		public void TryGet_DistinctTools_AreIsolated()
		{
			var store = new BrowseColumnConfigStore(_dir);
			store.Save("lexiconBrowse", Cols(("a|x|A", null)));
			store.Save("reversalBrowse", Cols(("b|y|B", null)));

			Assert.That(store.TryGet("lexiconBrowse")[0].Key, Is.EqualTo("a|x|A"));
			Assert.That(store.TryGet("reversalBrowse")[0].Key, Is.EqualTo("b|y|B"));
			Assert.That(store.TryGet("otherTool"), Is.Null);
		}

		[Test]
		public void TryGet_CorruptFile_ReportsErrorAndReturnsNull()
		{
			Directory.CreateDirectory(_dir);
			File.WriteAllText(Path.Combine(_dir, "lexiconBrowse.browsecolumns.json"), "{ not valid json");
			var store = new BrowseColumnConfigStore(_dir);

			Exception captured = null;
			var result = store.TryGet("lexiconBrowse", (path, e) => captured = e);

			Assert.That(result, Is.Null, "a corrupt file degrades to the shipped default, never a crash");
			Assert.That(captured, Is.Not.Null, "the load failure is surfaced for logging");
		}

		[Test]
		public void TryGet_ToolHeaderMismatch_IsIgnored()
		{
			Directory.CreateDirectory(_dir);
			// A file whose JSON tool header disagrees with the requested tool (renamed/hand-edited) is not used.
			File.WriteAllText(Path.Combine(_dir, "lexiconBrowse.browsecolumns.json"),
				BrowseColumnConfigStore.Serialize("reversalBrowse", Cols(("a|x|A", null))));

			Assert.That(new BrowseColumnConfigStore(_dir).TryGet("lexiconBrowse"), Is.Null);
		}

		[Test]
		public void TryGet_CachesAcrossCalls_AndSaveRefreshesCache()
		{
			var store = new BrowseColumnConfigStore(_dir);
			Assert.That(store.TryGet("lexiconBrowse"), Is.Null);

			store.Save("lexiconBrowse", Cols(("a|x|A", null)));
			Assert.That(store.TryGet("lexiconBrowse"), Is.Not.Null);
		}
	}
}
