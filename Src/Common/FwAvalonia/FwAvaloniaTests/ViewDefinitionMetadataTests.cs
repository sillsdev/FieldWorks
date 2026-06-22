// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 4.7: localization/accessibility/routing metadata on typed view-definition nodes. Legacy XML
	/// carries none of these, so imported legacy layouts keep their exact semantic snapshot; authored or
	/// region-spec sources may set them and they then appear in the snapshot.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionMetadataTests
	{
		private sealed class StubResolver : IPartResolver
		{
			private readonly XElement _content;
			public StubResolver(XElement content) { _content = content; }
			public XElement ResolvePart(string className, string layoutType, string refName) => _content;
			public XElement ResolvePartByRef(string refName) => _content;
		}

		private static ViewDefinitionModel Import(string contentXml)
		{
			var layout = new XElement("layout",
				new XAttribute("class", "LexEntry"),
				new XAttribute("name", "test"),
				new XAttribute("type", "detail"),
				new XElement("part", new XAttribute("ref", "Field")));
			var content = XElement.Parse(contentXml);
			return new XmlLayoutImporter().Import(layout, new StubResolver(content));
		}

		[Test]
		public void Defaults_AreNullAndInherit_WhenNotAuthored()
		{
			var model = Import("<slice editor='multistring' field='CitationForm' />");
			var node = model.Roots.Single();

			Assert.That(node.LocalizationKey, Is.Null);
			Assert.That(node.AutomationId, Is.Null);
			Assert.That(node.Routing, Is.EqualTo(SurfaceRouting.Inherit));
		}

		[Test]
		public void Snapshot_IsUnchanged_WhenNoMetadataIsPresent()
		{
			var model = Import("<slice editor='multistring' field='CitationForm' />");
			var snapshot = model.ToSnapshot();

			Assert.That(snapshot, Does.Not.Contain("loc="));
			Assert.That(snapshot, Does.Not.Contain("autoId="));
			Assert.That(snapshot, Does.Not.Contain("routing="));
		}

		[Test]
		public void Importer_ReadsMetadataAttributes_WhenPresent()
		{
			var model = Import(
				"<slice editor='multistring' field='CitationForm' localizationKey='ksCitationForm' automationId='CitationFormEditor' surface='product' />");
			var node = model.Roots.Single();

			Assert.That(node.LocalizationKey, Is.EqualTo("ksCitationForm"));
			Assert.That(node.AutomationId, Is.EqualTo("CitationFormEditor"));
			Assert.That(node.Routing, Is.EqualTo(SurfaceRouting.Product));
		}

		[Test]
		public void Snapshot_IncludesMetadata_WhenPresent()
		{
			var model = Import(
				"<slice editor='multistring' field='CitationForm' automationId='CitationFormEditor' surface='preview' />");
			var snapshot = model.ToSnapshot();

			Assert.That(snapshot, Does.Contain("autoId=CitationFormEditor"));
			Assert.That(snapshot, Does.Contain("routing=Preview"));
		}

		[Test]
		public void LabelId_IsAcceptedAsLocalizationKeyFallback()
		{
			var model = Import("<slice editor='multistring' field='CitationForm' labelId='ksCf' />");
			Assert.That(model.Roots.Single().LocalizationKey, Is.EqualTo("ksCf"));
		}
	}
}
