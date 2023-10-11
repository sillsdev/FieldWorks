// Copyright (c) 2014-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public class ConfiguredLcmGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private PropertyTable m_propertyTable;
		private FwXApp m_application;
		private FwXWindow m_window;
		private int m_wsFr;

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			ConfiguredLcmGenerator.Init();
			base.FixtureTeardown();
			FwRegistrySettings.Release();
			Dispose();
		}

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~ConfiguredLcmGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion

		[Test]
		public void GeneratorSettings_NullArgsThrowArgumentNull()
		{
			// ReSharper disable AccessToDisposedClosure // Justification: Assert calls lambdas immediately, so XHTMLWriter is not used after being disposed
			// ReSharper disable ObjectCreationAsStatement // Justification: We expect the constructor to throw, so there's no created object to assign anywhere :)
			Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredLcmGenerator.GeneratorSettings(Cache, (PropertyTable)null, false, false, null));
			Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredLcmGenerator.GeneratorSettings(null, new ReadOnlyPropertyTable(m_propertyTable), false, false, null));
			// ReSharper restore ObjectCreationAsStatement
			// ReSharper restore AccessToDisposedClosure
		}

		[Test]
		public void GenerateXHTMLForEntry_NullArgsThrowArgumentNull()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateContentForEntry(null, mainEntryNode, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateContentForEntry(entry, (ConfigurableDictionaryNode)null, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, null));
		}

		[Test]
		public void GenerateXHTMLForEntry_BadConfigurationThrows()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			//Test a blank main node description
			Assert.That(() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("Invalid configuration"));
			//Test a configuration with a valid but incorrect type
			mainEntryNode.FieldDescription = "LexSense";
			Assert.That(() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("doesn't configure this type"));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_NullConfigurationNodeThrowsNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(null));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_RootMemberWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var stringNode = new ConfigurableDictionaryNode { FieldDescription = "RootMember" };
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { stringNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(stringNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InterfacePropertyWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_FirstParentInterfacePropertyIsUsable()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestMoForm" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.MoFormType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SecondParentInterfacePropertyIsUsable()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestIcmObject" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.CmObjectType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_GrandparentInterfacePropertyIsUsable()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.CollectionType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_NonInterfaceMemberIsUsable()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var stringNodeInClass = new ConfigurableDictionaryNode { FieldDescription = "TestNonInterfaceString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				Children = new List<ConfigurableDictionaryNode> { stringNodeInClass }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(stringNodeInClass));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidChildDoesNotThrow()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SubFieldWorks()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidSubFieldReturnsInvalidProperty()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "NonExistantSubField"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidRootThrowsWithMessage()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.NonExistantClass",
			};
			// SUT
			Assert.That(() => ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(rootNode),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains(rootNode.FieldDescription));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_PictureFileReturnsCmPictureType()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var pictureFileNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA" };
			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { pictureFileNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(pictureFileNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.CmPictureType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_StTextReturnsPrimitive()
		{
			ConfiguredLcmGenerator.Init();
			var fieldName = "CustomMultiPara";
			using (var customField = new CustomFieldForTest(Cache, fieldName, fieldName, Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), StTextTags.kClassId, -1,
			 CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = fieldName,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var locator = Cache.ServiceLocator;
				// Set custom field data
				var multiParaHvo = Cache.MainCacheAccessor.MakeNewObject(StTextTags.kClassId, testEntry.Hvo, customField.Flid, -2);
				var textObject = locator.GetInstance<IStTextRepository>().GetObject(multiParaHvo);
				var paragraph = locator.GetInstance<IStTxtParaFactory>().Create();
				textObject.ParagraphsOS.Add(paragraph);
				paragraph.Contents = TsStringUtils.MakeString(customData, m_wsFr);
				//SUT
				var type = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, Cache);
				Assert.AreEqual(ConfiguredLcmGenerator.PropertyType.PrimitiveType, type);
			}
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_ExtensionMethodToString()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "@extension:SIL.FieldWorks.XWorks.TestExtensionMethod.Creator",
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode },
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_ExtensionMethodMissingMethodDoesNotThrow()
		{
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";
			var missingClassMember = new ConfigurableDictionaryNode
			{
				FieldDescription = "@extension:SIL.FieldWorks.XWorks.MissingClass.Creator",
			};
			var missingMethodMember = new ConfigurableDictionaryNode
			{
				FieldDescription = "@extension:SIL.FieldWorks.XWorks.TestPictureClass.MissingMethod",
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				Children = new List<ConfigurableDictionaryNode> { missingClassMember, missingMethodMember },
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = ConfiguredLcmGenerator.PropertyType.PrimitiveType;
		 // SUT
		 Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(missingClassMember));
		 Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.InvalidProperty));
		 Assert.DoesNotThrow(() => result = ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(missingMethodMember));
		 Assert.That(result, Is.EqualTo(ConfiguredLcmGenerator.PropertyType.InvalidProperty));
		}

		[Test]
		public void IsMainEntry_ReturnsFalseForMinorEntry()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			var rootConfig = new DictionaryConfigurationModel(true);
			var lexemeConfig = new DictionaryConfigurationModel(false);
			// SUT
			Assert.False(ConfiguredLcmGenerator.IsMainEntry(variantEntry, lexemeConfig), "Variant, Lexeme");
			Assert.False(ConfiguredLcmGenerator.IsMainEntry(variantEntry, rootConfig), "Variant, Root");
			Assert.False(ConfiguredLcmGenerator.IsMainEntry(complexEntry, rootConfig), "Complex, Root");
			// (complex entries are considered main entries in lexeme-based configs)
		}

		[Test]
		public void IsMainEntry_ReturnsTrueForMainEntry()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			var rootConfig = new DictionaryConfigurationModel(true);
			var lexemeConfig = new DictionaryConfigurationModel(false);
			// SUT
			Assert.That(ConfiguredLcmGenerator.IsMainEntry(mainEntry, rootConfig), "Main, Root");
			Assert.That(ConfiguredLcmGenerator.IsMainEntry(mainEntry, lexemeConfig), "Main, Lexeme");
			Assert.That(ConfiguredLcmGenerator.IsMainEntry(complexEntry, lexemeConfig), "Complex, Lexeme");
			// (complex entries are considered minor entries in root-based configs)
			Assert.That(ConfiguredLcmGenerator.IsMainEntry(Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create(),
				new DictionaryConfigurationModel()), "Reversal Index Entries are always considered Main Entries");
		}
	}
}
