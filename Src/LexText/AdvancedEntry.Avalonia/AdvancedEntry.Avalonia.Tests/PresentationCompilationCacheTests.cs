using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class PresentationCompilationCacheTests
{
	[SetUp]
	public void SetUp()
	{
		PresentationCompilationCache.Clear();
	}

	[Test]
	public void GetOrAdd_SameKey_DoesNotRecompile()
	{
		var key = new PresentationCompilationCacheKey(
			"test-project",
			new LayoutId("LexEntry", "detail", "Normal"),
			"fingerprint-1");

		var compileCount = 0;
		_ = PresentationCompilationCache.GetOrAdd(key, () =>
		{
			compileCount++;
			return new PresentationLayout(new("test"))
			{
				RootClass = "LexEntry",
				RootType = "detail",
				RootName = "Normal",
				Children = Array.Empty<PresentationNode>(),
			};
		});
		_ = PresentationCompilationCache.GetOrAdd(key, () =>
		{
			compileCount++;
			return new PresentationLayout(new("test"))
			{
				RootClass = "LexEntry",
				RootType = "detail",
				RootName = "Normal",
				Children = Array.Empty<PresentationNode>(),
			};
		});

		Assert.That(compileCount, Is.EqualTo(1));
	}

	[Test]
	public void Invalidate_RemovesCachedValue()
	{
		var key = new PresentationCompilationCacheKey(
			"test-project",
			new LayoutId("LexEntry", "detail", "Normal"),
			"fingerprint-1");

		var compileCount = 0;
		_ = PresentationCompilationCache.GetOrAdd(key, () =>
		{
			compileCount++;
			return new PresentationLayout(new("test"))
			{
				RootClass = "LexEntry",
				RootType = "detail",
				RootName = "Normal",
				Children = Array.Empty<PresentationNode>(),
			};
		});
		Assert.That(PresentationCompilationCache.Invalidate(key), Is.True);
		_ = PresentationCompilationCache.GetOrAdd(key, () =>
		{
			compileCount++;
			return new PresentationLayout(new("test"))
			{
				RootClass = "LexEntry",
				RootType = "detail",
				RootName = "Normal",
				Children = Array.Empty<PresentationNode>(),
			};
		});

		Assert.That(compileCount, Is.EqualTo(2));
	}

	[Test]
	public void PartsLayoutLoader_FingerprintChanges_WhenInputFilesChange()
	{
		var tempRoot = Path.Combine(Path.GetTempPath(), "FwAdvancedEntry.CacheFingerprint." + Guid.NewGuid());
		Directory.CreateDirectory(tempRoot);
		try
		{
			var layoutPath = Path.Combine(tempRoot, "LexEntry.fwlayout");
			var partsPath = Path.Combine(tempRoot, "LexEntryParts.xml");

			File.WriteAllText(layoutPath,
				"<LayoutInventory>" +
				"<layout class='LexEntry' type='detail' name='Normal'><part ref='Foo' /></layout>" +
				"</LayoutInventory>");
			File.WriteAllText(partsPath,
				"<PartInventory><parts><part id='LexEntry-Detail-Foo'><slice field='CitationForm' label='Citation Form'/></part></parts></PartInventory>");

			var loader = new PartsLayoutLoader();
			var contract1 = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { tempRoot });

			// Ensure timestamp/length changes.
			System.Threading.Thread.Sleep(20);
			File.AppendAllText(partsPath, "\n<!-- changed -->");

			var contract2 = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { tempRoot });

			Assert.That(contract2.ConfigurationFingerprint, Is.Not.EqualTo(contract1.ConfigurationFingerprint));
		}
		finally
		{
			try
			{
				Directory.Delete(tempRoot, recursive: true);
			}
			catch
			{
				// ignored
			}
		}
	}
}
