// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2010-08-03 SliceTests.cs
using System;
using System.Collections;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	[TestFixture]
	public partial class SliceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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
			Assert.That(m_Slice, Is.Not.Null);
		}

		/// <summary></summary>
		[Test]
		public void Basic2()
		{
			using (var control = new Control())
			{
				using (var slice = new Slice(control))
				{
			Assert.That(slice.Control, Is.EqualTo(control));
			Assert.That(slice, Is.Not.Null);
		}
			}
		}

		/// <summary>Helper</summary>
		public static XmlElement CreateXmlElementFromOuterXmlOf(string outerXml)
		{
			var document = new XmlDocument();
			document.LoadXml(outerXml);
			var element = document.DocumentElement;
			return element;
		}

		/// <summary>Helper</summary>
		private static Slice GenerateSlice(LcmCache cache, DataTree datatree)
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
			m_propertyTable = new PropertyTable(m_Mediator);
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
			m_propertyTable = new PropertyTable(m_Mediator);
			m_Slice.PropTable = m_propertyTable;
			m_propertyTable.SetProperty("cache", Cache, false);

			m_Slice.Collapse();
		}
		/// <summary>
		/// Create a DataTree with a GhostStringSlice object. Test to ensure that the PropTable is not null.
		/// </summary>
		[Test]
		public void CreateGhostStringSlice_ParentSliceNotNull()
		{
			var path = GeneratePath();
			var reuseMap = new ObjSeqHashMap();
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			m_Mediator = new Mediator();
			m_Slice.Mediator = m_Mediator;
			m_propertyTable = new PropertyTable(m_Mediator);
			m_Slice.PropTable = m_propertyTable;
			var node = CreateXmlElementFromOuterXmlOf("<seq field=\"Pronunciations\" layout=\"Normal\" ghost=\"Form\" ghostWs=\"pronunciation\" ghostLabel=\"Pronunciation\" menu=\"mnuDataTree-Pronunciation\" />");
			int indent = 0;
			int insertPosition = 0;
			int flidEmptyProp = 5002031;    // runtime flid of ghost field
			m_DataTree.MakeGhostSlice(path, node, reuseMap, obj, m_Slice, flidEmptyProp, null, indent, ref insertPosition);
			var ghostSlice = m_DataTree.Slices[0];
			Assert.That(ghostSlice, Is.Not.Null);
			Assert.That(m_Slice.PropTable, Is.EqualTo(ghostSlice.PropTable));
		}



		private sealed class ExposedSlice : Slice
		{
			public string ExposedHelpId => HelpId;
			public bool ExposedShouldHide => ShouldHide;
		}

		private void CreateMoveSlices(out Slice first, out Slice middle, out Slice last)
		{
			m_DataTree = new DataTree();
			var doc = new XmlDocument();
			doc.LoadXml("<layout><part ref='A'/><part ref='B'/><part ref='C'/></layout>");
			var layout = doc.DocumentElement;
			var partA = layout.SelectSingleNode("part[@ref='A']");
			var partB = layout.SelectSingleNode("part[@ref='B']");
			var partC = layout.SelectSingleNode("part[@ref='C']");

			first = new Slice { Key = new object[] { layout, partA } };
			middle = new Slice { Key = new object[] { layout, partB } };
			last = new Slice { Key = new object[] { layout, partC } };

			m_DataTree.Controls.Add(first);
			m_DataTree.Controls.Add(middle);
			m_DataTree.Controls.Add(last);
			m_DataTree.Slices.Add(first);
			m_DataTree.Slices.Add(middle);
			m_DataTree.Slices.Add(last);
		}

		private void CreateEmbeddedSliceHarness(out Slice root, out Slice child, out Slice outsider)
		{
			m_DataTree = new DataTree();
			m_DataTree.Initialize(Cache, false, DataTreeTests.GenerateLayouts(), DataTreeTests.GenerateParts());

			var doc = new XmlDocument();
			doc.LoadXml("<layout><part ref='A'/><part ref='B'/></layout>");
			var layout = doc.DocumentElement;
			var partA = layout.SelectSingleNode("part[@ref='A']");
			var partB = layout.SelectSingleNode("part[@ref='B']");

			root = new Slice
			{
				Object = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(),
				Key = new object[] { layout, partA },
				Expansion = DataTree.TreeItemState.ktisFixed
			};

			child = new Slice
			{
				Object = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(),
				Key = new object[] { layout, partA, partB },
				Expansion = DataTree.TreeItemState.ktisFixed
			};

			outsider = new Slice
			{
				Object = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(),
				Key = new object[] { layout, partB },
				Expansion = DataTree.TreeItemState.ktisFixed
			};

			root.Parent = m_DataTree;
			child.Parent = m_DataTree;
			outsider.Parent = m_DataTree;
			m_DataTree.Controls.Add(root);
			m_DataTree.Controls.Add(child);
			m_DataTree.Controls.Add(outsider);
			m_DataTree.Slices.Add(root);
			m_DataTree.Slices.Add(child);
			m_DataTree.Slices.Add(outsider);
		}

		private void CreateSequentialSlices(int count, out Slice[] slices)
		{
			m_DataTree = new DataTree();
			slices = new Slice[count];
			for (int i = 0; i < count; i++)
			{
				var slice = new Slice
				{
					Key = new object[] { "k", i }
				};
				slice.Parent = m_DataTree;
				m_DataTree.Controls.Add(slice);
				m_DataTree.Slices.Add(slice);
				slices[i] = slice;
			}
		}
	}
}
