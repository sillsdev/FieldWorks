using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class LayoutContractTests
{
	[Test]
	public void CanLoadShippedLexEntryRootLayout()
	{
		var repoRoot = TestRepoRoot.Find();
		var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");

		var loader = new PartsLayoutLoader();
		var contract = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { partsDir });

		Assert.That(contract.LayoutElement.Name.LocalName, Is.EqualTo("layout"));
		Assert.That(contract.LayoutElement.Elements().Count(), Is.GreaterThan(0));
		Assert.That(contract.LayoutsById.Count, Is.GreaterThan(0));
		Assert.That(contract.PartsById.Count, Is.GreaterThan(0));
	}

	[Test]
	public void OverridePartsWinOverShippedDefaults()
	{
		var repoRoot = TestRepoRoot.Find();
		var shippedPartsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");

		var tempRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "parts-override");
		Directory.CreateDirectory(tempRoot);

		File.WriteAllText(
			Path.Combine(tempRoot, "LexEntryParts.xml"),
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
			"<PartInventory>\n" +
			"  <bin class=\"LexEntry\">\n" +
			"    <part id=\"LexEntry-Detail-CitationFormAllV\" type=\"detail\">\n" +
			"      <slice field=\"CitationForm\" label=\"OVERRIDE LABEL\" editor=\"multistring\" ws=\"all vernacular\" />\n" +
			"    </part>\n" +
			"  </bin>\n" +
			"</PartInventory>\n");

		File.WriteAllText(
			Path.Combine(tempRoot, "LexEntry.fwlayout"),
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
			"<LayoutInventory>\n" +
			"  <layout class=\"LexEntry\" type=\"detail\" name=\"Normal\">\n" +
			"    <part ref=\"CitationFormAllV\" label=\"Citation Form\"/>\n" +
			"  </layout>\n" +
			"</LayoutInventory>\n");

		var loader = new PartsLayoutLoader();
		var contract = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { tempRoot, shippedPartsDir });

		Assert.That(contract.PartsById.ContainsKey("LexEntry-Detail-CitationFormAllV"), Is.True);
		Assert.That(contract.GetPartOrThrow("LexEntry-Detail-CitationFormAllV").ToString(), Does.Contain("OVERRIDE LABEL"));
	}
}
