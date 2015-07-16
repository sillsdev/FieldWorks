// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2010-08-03 SliceTests.cs
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	[TestFixture]
	public class SliceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DataTree m_DataTree;
		private Slice m_Slice;
		private Mediator m_Mediator;
		private PropertyTable m_propertyTable;

		/// <summary/>
		public override void TestTearDown()
		{
			if (m_Slice != null)
			{
				m_Slice.Dispose();
				m_Slice = null;
			}
			if (m_DataTree != null)
			{
				m_DataTree.Dispose();
				m_DataTree = null;
			}
			if (m_Mediator != null)
			{
				m_Mediator.Dispose();
				m_Mediator = null;
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}

			base.TestTearDown();
		}

		/// <summary></summary>
		[Test]
		public void Basic1()
		{
			m_Slice = new Slice();
			Assert.NotNull(m_Slice);
		}

		/// <summary></summary>
		[Test]
		public void Basic2()
		{
			using (var control = new Control())
			{
				using (var slice = new Slice(control))
				{
			Assert.AreEqual(control, slice.Control);
			Assert.NotNull(slice);
		}
			}
		}

		/// <summary>Helper</summary>
		private static XmlElement CreateXmlElementFromOuterXmlOf(string outerXml)
		{
			var document = new XmlDocument();
			document.LoadXml(outerXml);
			var element = document.DocumentElement;
			return element;
		}

		/// <summary>Helper</summary>
		private static Slice GenerateSlice(FdoCache cache, DataTree datatree)
		{
			var slice = new Slice();
			var parts = DataTreeTests.GenerateParts();
			var layouts = DataTreeTests.GenerateLayouts();
			datatree.Initialize(cache, false, layouts, parts);
			slice.Parent = datatree;
			return slice;
		}

		/// <summary>Helper</summary>
		private static ArrayList GeneratePath()
		{
			var path = new ArrayList(7);
			// Data taken from a running Sena 3
			path.Add(CreateXmlElementFromOuterXmlOf("<layout class=\"LexEntry\" type=\"detail\" name=\"Normal\"><part label=\"Lexeme Form\" ref=\"LexemeForm\" /><part label=\"Citation Form\" ref=\"CitationFormAllV\" /><part ref=\"ComplexFormEntries\" visibility=\"ifdata\" /><part ref=\"EntryRefs\" param=\"Normal\" visibility=\"ifdata\" /><part ref=\"EntryRefsGhostComponents\" visibility=\"always\" /><part ref=\"EntryRefsGhostVariantOf\" visibility=\"never\" /><part ref=\"Pronunciations\" param=\"Normal\" visibility=\"ifdata\" /><part ref=\"Etymology\" menu=\"mnuDataTree-InsertEtymology\" visibility=\"ifdata\" /><part ref=\"CommentAllA\" /><part ref=\"LiteralMeaningAllA\" visibility=\"ifdata\" /><!-- Only for Subentries. --><part ref=\"BibliographyAllA\" visibility=\"ifdata\" /><part ref=\"RestrictionsAllA\" visibility=\"ifdata\" /><part ref=\"SummaryDefinitionAllA\" visibility=\"ifdata\" /><part ref=\"ExcludeAsHeadword\" label=\"Exclude As Headword\" visibility=\"never\" /><!-- todo 'ifTrue' --><part ref=\"CurrentLexReferences\" visibility=\"ifdata\" /><!-- Special part to indicate where custom fields should be inserted at.  Handled in Common.Framework.DetailControls.DataTree --><part customFields=\"here\" /><part ref=\"ImportResidue\" label=\"Import Residue\" visibility=\"ifdata\" /><part ref=\"DateCreatedAllA\" visibility=\"never\" /><part ref=\"DateModifiedAllA\" visibility=\"never\" /><part ref=\"Senses\" param=\"Normal\" expansion=\"expanded\" /><part ref=\"VariantFormsSection\" expansion=\"expanded\" label=\"Variants\" menu=\"mnuDataTree-VariantForms\" hotlinks=\"mnuDataTree-VariantForms-Hotlinks\"><indent><part ref=\"VariantForms\" /></indent></part><part ref=\"AlternateFormsSection\" expansion=\"expanded\" label=\"Allomorphs\" menu=\"mnuDataTree-AlternateForms\" hotlinks=\"mnuDataTree-AlternateForms-Hotlinks\"><indent><part ref=\"AlternateForms\" param=\"Normal\" /></indent></part><part ref=\"GrammaticalFunctionsSection\" label=\"Grammatical Info. Details\" menu=\"mnuDataTree-Help\" hotlinks=\"mnuDataTree-Help\"><indent><part ref=\"MorphoSyntaxAnalyses\" param=\"Normal\" /></indent></part></layout>"));
			path.Add(CreateXmlElementFromOuterXmlOf("<part label=\"Lexeme Form\" ref=\"LexemeForm\" />"));
			path.Add(CreateXmlElementFromOuterXmlOf("<obj field=\"LexemeForm\" layout=\"AsLexemeFormBasic\" menu=\"mnuDataTree-Help\" ghost=\"Form\" ghostWs=\"vernacular\" ghostLabel=\"Lexeme Form\" ghostClass=\"MoStemAllomorph\" ghostInitMethod=\"SetMorphTypeToRoot\" />"));
			path.Add(21631);
			path.Add(CreateXmlElementFromOuterXmlOf("<layout class=\"MoStemAllomorph\" type=\"detail\" name=\"AsLexemeFormBasic\"><part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part></layout>"));
			path.Add(CreateXmlElementFromOuterXmlOf("<part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part>"));
			path.Add(CreateXmlElementFromOuterXmlOf("<slice field=\"Form\" label=\"Form\" editor=\"multistring\" ws=\"all vernacular\" weight=\"light\" menu=\"mnuDataTree-LexemeForm\" contextMenu=\"mnuDataTree-LexemeFormContext\" spell=\"no\"><properties><bold value=\"on\" /><fontsize value=\"120%\" /></properties></slice>"));
			return path;
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// </remarks>
		[Test]
		public void CreateIndentedNodes_basic()
		{
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);

			// Data taken from a running Sena 3
			var caller = CreateXmlElementFromOuterXmlOf("<part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part>");

			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			const int indent = 0;
			int insPos = 1;

			var path = GeneratePath();

			var reuseMap = new ObjSeqHashMap();
			// Data taken from a running Sena 3
			var node = CreateXmlElementFromOuterXmlOf("<slice field=\"Form\" label=\"Form\" editor=\"multistring\" ws=\"all vernacular\" weight=\"light\" menu=\"mnuDataTree-LexemeForm\" contextMenu=\"mnuDataTree-LexemeFormContext\" spell=\"no\"><properties><bold value=\"on\" /><fontsize value=\"120%\" /></properties></slice>");

			m_Slice.CreateIndentedNodes(caller, obj, indent, ref insPos, path, reuseMap, node);
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// </remarks>
		[Test]
		public void Expand()
		{
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			m_Slice.Key = GeneratePath().ToArray();
			m_Slice.Object = obj;
			m_Mediator = new Mediator();
			m_Slice.Mediator = m_Mediator;
			m_propertyTable = new PropertyTable(new MockPublisher());
			m_Slice.PropTable = m_propertyTable;
			m_propertyTable.SetProperty("cache", Cache, false);

			m_Slice.Expand();
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// Isn't actually collapsing anything.
		/// </remarks>
		[Test]
		public void Collapse()
		{
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();

			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			m_Slice.Key = GeneratePath().ToArray();
			m_Slice.Object = obj;
			m_Mediator = new Mediator();
			m_Slice.Mediator = m_Mediator;
			m_propertyTable = new PropertyTable(new MockPublisher());
			m_Slice.PropTable = m_propertyTable;
			m_propertyTable.SetProperty("cache", Cache, false);

			m_Slice.Collapse();
		}
	}
}
