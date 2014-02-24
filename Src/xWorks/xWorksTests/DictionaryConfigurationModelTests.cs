// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationModelTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Save_BasicValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root"
				};
			//SUT
			model.Save();
			ValidateAgainstSchema(modelFile);
			AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 0);
		}

		[Test]
		public void Save_ConfigWithOneNodeValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var oneConfigNode = new ConfigurableDictionaryNode();
			oneConfigNode.Label = "Main Entry";
			oneConfigNode.IsEnabled = true;
			oneConfigNode.Before = "[";
			oneConfigNode.FieldDescription = "LexEntry";

			var model = new DictionaryConfigurationModel
			{
				FilePath = modelFile,
				Version = 0,
				Label = "root",
				Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
			};
			//SUT
			model.Save();
			ValidateAgainstSchema(modelFile);
			AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
		}

		[Test]
		public void Save_ConfigWithTwoNodesValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var firstNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry"
				};

			var secondNode = new ConfigurableDictionaryNode()
				{
					Label = "Minor Entry",
					Before = "{",
					After = "}",
					FieldDescription = "LexEntry",
					IsEnabled = false
				};

			var model = new DictionaryConfigurationModel
			{
				FilePath = modelFile,
				Version = 0,
				Label = "root",
				Parts = new List<ConfigurableDictionaryNode> { firstNode, secondNode }
			};
			//SUT
			model.Save();
			ValidateAgainstSchema(modelFile);
			AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 2);
		}

		[Test]
		public void Save_ConfigNodeWithChildrenValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var headword = new ConfigurableDictionaryNode
				{
					Label = "Headword",
					FieldDescription = "LexEntry, headword",
					IsEnabled = true
				};
			var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { headword }
				};

			var model = new DictionaryConfigurationModel
			{
				FilePath = modelFile,
				Version = 0,
				Label = "root",
				Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
			};
			//SUT
			model.Save();

			ValidateAgainstSchema(modelFile);
			AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ConfigurationItem", 1);
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
							  Justification = "Certain types can't be validated. e.g. xs:byte, otherwise implemented enough for us")]
		private static void ValidateAgainstSchema(string xmlFile)
		{
			var schemaLocation = Path.Combine(Path.Combine(DirectoryFinder.FlexFolder, "Configuration"), "DictionaryConfiguration.xsd");
			var schemas = new XmlSchemaSet();
			using(var reader = XmlReader.Create(schemaLocation))
			{
				schemas.Add("", reader);
				var document = XDocument.Load(xmlFile);
				document.Validate(schemas, (sender, args) => Assert.Fail("Model saved as xml did not validate against schema: {0}", args.Message));
			}
		}

		[Test]
		public void SpecifyParents_ThrowsOnNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationModel.SpecifyParents(null));
		}

		[Test]
		public void SpecifyParents_DoesNotChangeRootNode()
		{
			var child = new ConfigurableDictionaryNode();
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>() {child},
				Parent = null
			};
			var parts = new List<ConfigurableDictionaryNode> {rootNode};
			// SUT
			DictionaryConfigurationModel.SpecifyParents(parts);
			Assert.That(parts[0].Parent, Is.Null, "Shouldn't have changed parent of a root node");
		}

		[Test]
		public void SpecifyParents_UpdatesParentPropertyOfChild()
		{
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>()
			};
			var childA = new ConfigurableDictionaryNode()
			{
				Children = new List<ConfigurableDictionaryNode>()
			};
			var childB = new ConfigurableDictionaryNode();
			var grandchild = new ConfigurableDictionaryNode();
			rootNode.Children.Add(childA);
			rootNode.Children.Add(childB);
			childA.Children.Add(grandchild);

			var parts = new List<ConfigurableDictionaryNode> { rootNode };
			// SUT
			DictionaryConfigurationModel.SpecifyParents(parts);
			Assert.That(grandchild.Parent, Is.EqualTo(childA), "Parent should have been set");
			Assert.That(childA.Parent, Is.EqualTo(rootNode), "Parent should have been set");
			Assert.That(childB.Parent, Is.EqualTo(rootNode), "Parent should have been set");
		}
	}
}
