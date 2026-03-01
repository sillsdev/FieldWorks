// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2010-08-03 SliceTests.cs
using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
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

		#region Characterization Tests — Core Properties

		/// <summary>
		/// Abbreviation auto-generates from Label (first 4 chars) when not explicitly set.
		/// </summary>
		[Test]
		public void Abbreviation_AutoGeneratedFromLabel()
		{
			using (var slice = new Slice())
			{
				slice.Label = "Citation Form";
				// Setting Label also auto-sets Abbreviation via the setter logic.
				// But Abbreviation is set via its own property — verify when set to null/empty.
				slice.Abbreviation = null;
				Assert.That(slice.Abbreviation, Is.EqualTo("Cita"),
					"Abbreviation should auto-generate to first 4 chars of label");
			}
		}

		/// <summary>
		/// Label shorter than 4 chars → abbreviation is the full label.
		/// </summary>
		[Test]
		public void Abbreviation_ShortLabel_UsesFullLabel()
		{
			using (var slice = new Slice())
			{
				slice.Label = "Go";
				slice.Abbreviation = null;
				Assert.That(slice.Abbreviation, Is.EqualTo("Go"),
					"Abbreviation for short label should be the full label");
			}
		}

		/// <summary>
		/// Explicit abbreviation overrides auto-generation.
		/// </summary>
		[Test]
		public void Abbreviation_ExplicitOverridesAutoGeneration()
		{
			using (var slice = new Slice())
			{
				slice.Label = "Citation Form";
				slice.Abbreviation = "CF";
				Assert.That(slice.Abbreviation, Is.EqualTo("CF"),
					"Explicit abbreviation should override auto-generation");
			}
		}

		/// <summary>
		/// IsHeaderNode reads the header XML attribute.
		/// </summary>
		[Test]
		public void IsHeaderNode_ReadsXmlAttribute()
		{
			using (var slice = new Slice())
			{
				slice.ConfigurationNode = CreateXmlElementFromOuterXmlOf("<slice header=\"true\" />");
				Assert.That(slice.IsHeaderNode, Is.True);
			}
		}

		/// <summary>
		/// IsHeaderNode is false when header attribute is absent.
		/// </summary>
		[Test]
		public void IsHeaderNode_FalseWhenAbsent()
		{
			using (var slice = new Slice())
			{
				slice.ConfigurationNode = CreateXmlElementFromOuterXmlOf("<slice label=\"SomeField\" />");
				Assert.That(slice.IsHeaderNode, Is.False);
			}
		}

		/// <summary>
		/// ContainingDataTree returns null when the slice is not parented.
		/// </summary>
		[Test]
		public void ContainingDataTree_NullWhenOrphaned()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.ContainingDataTree, Is.Null,
					"Orphaned slice should have null ContainingDataTree");
			}
		}

		/// <summary>
		/// ContainingDataTree returns the parent DataTree after Install.
		/// </summary>
		[Test]
		public void ContainingDataTree_ReturnsParent()
		{
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			Assert.That(m_Slice.ContainingDataTree, Is.SameAs(m_DataTree),
				"Installed slice should return its parent DataTree");
		}

		/// <summary>
		/// WrapsAtomic reads the wrapsAtomic XML attribute.
		/// </summary>
		[Test]
		public void WrapsAtomic_ReadsConfigAttribute()
		{
			using (var slice = new Slice())
			{
				slice.ConfigurationNode = CreateXmlElementFromOuterXmlOf("<slice wrapsAtomic=\"true\" />");
				Assert.That(slice.WrapsAtomic, Is.True);
			}
		}

		/// <summary>
		/// WrapsAtomic defaults to false.
		/// </summary>
		[Test]
		public void WrapsAtomic_DefaultsFalse()
		{
			using (var slice = new Slice())
			{
				slice.ConfigurationNode = CreateXmlElementFromOuterXmlOf("<slice label=\"Foo\" />");
				Assert.That(slice.WrapsAtomic, Is.False);
			}
		}

		/// <summary>
		/// IsRealSlice returns true for regular slices.
		/// </summary>
		[Test]
		public void IsRealSlice_TrueForRegularSlice()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.IsRealSlice, Is.True,
					"Regular Slice should be real");
			}
		}

		/// <summary>
		/// BecomeReal on a non-dummy slice returns itself.
		/// </summary>
		[Test]
		public void BecomeReal_BaseReturnsSelf()
		{
			using (var slice = new Slice())
			{
				var result = slice.BecomeReal(0);
				Assert.That(result, Is.SameAs(slice),
					"BecomeReal on a real slice should return itself");
			}
		}

		/// <summary>
		/// CallerNodeEqual compares OuterXml of two nodes.
		/// </summary>
		[Test]
		public void CallerNodeEqual_StructuralComparison()
		{
			using (var slice = new Slice())
			{
				// Set CallerNode.
				var node1 = CreateXmlElementFromOuterXmlOf("<part ref=\"CitationForm\" label=\"CF\" />");
				slice.CallerNode = node1;

				// Create structurally identical but different .NET reference.
				var node2 = CreateXmlElementFromOuterXmlOf("<part ref=\"CitationForm\" label=\"CF\" />");

				Assert.That(slice.CallerNodeEqual(node2), Is.True,
					"CallerNodeEqual should compare by OuterXml");

				var node3 = CreateXmlElementFromOuterXmlOf("<part ref=\"Bibliography\" label=\"Bib\" />");
				Assert.That(slice.CallerNodeEqual(node3), Is.False,
					"Different XML content should not be equal");
			}
		}

		#endregion

		#region Characterization Tests — Lifecycle

		/// <summary>
		/// Constructor sets Visible to false.
		/// </summary>
		[Test]
		public void Constructor_SetsVisibleFalse()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.Visible, Is.False,
					"New slices should start invisible");
			}
		}

		/// <summary>
		/// After Dispose, CheckDisposed throws ObjectDisposedException.
		/// </summary>
		[Test]
		public void CheckDisposed_AfterDispose_Throws()
		{
			var slice = new Slice();
			slice.Dispose();

			Assert.Throws<ObjectDisposedException>(() => slice.CheckDisposed());
		}

		#endregion

		#region Characterization Tests — Expansion

		/// <summary>
		/// Default expansion state is Fixed.
		/// </summary>
		[Test]
		public void Expansion_DefaultIsFixed()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisFixed),
					"Default expansion should be Fixed");
			}
		}

		/// <summary>
		/// ExpansionStateKey is null for Fixed slices.
		/// </summary>
		[Test]
		public void ExpansionStateKey_NullForFixedSlices()
		{
			using (var slice = new Slice())
			{
				// Default expansion is Fixed.
				Assert.That(slice.ExpansionStateKey, Is.Null,
					"Fixed slices should have null ExpansionStateKey");
			}
		}

		/// <summary>
		/// ExpansionStateKey is non-null when expansion is Expanded and object is set.
		/// </summary>
		[Test]
		public void ExpansionStateKey_NonNullForExpandedWithObject()
		{
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_Slice.Object = obj;
			m_Slice.Expansion = DataTree.TreeItemState.ktisExpanded;

			Assert.That(m_Slice.ExpansionStateKey, Is.Not.Null,
				"Expanded slice with an object should have a non-null ExpansionStateKey");
			Assert.That(m_Slice.ExpansionStateKey, Does.StartWith("expand"),
				"ExpansionStateKey should start with 'expand'");
		}

		#endregion

		#region Characterization Tests — Static Utilities

		/// <summary>
		/// StartsWith correctly handles boxed int equality.
		/// </summary>
		[Test]
		public void StartsWith_BoxedIntEquality()
		{
			// Two arrays with boxed ints that have the same value.
			var target = new object[] { 1, 2, 3, "extra" };
			var match = new object[] { 1, 2, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.True,
				"StartsWith should handle boxed int equality");
		}

		/// <summary>
		/// StartsWith returns false when match is longer than target.
		/// </summary>
		[Test]
		public void StartsWith_MatchLongerThanTarget_ReturnsFalse()
		{
			var target = new object[] { 1, 2 };
			var match = new object[] { 1, 2, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.False);
		}

		/// <summary>
		/// StartsWith returns false for mismatched elements.
		/// </summary>
		[Test]
		public void StartsWith_MismatchedElements_ReturnsFalse()
		{
			var target = new object[] { 1, 2, 3 };
			var match = new object[] { 1, 99, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.False);
		}

		/// <summary>
		/// ExtraIndent returns 1 when indent="true".
		/// </summary>
		[Test]
		public void ExtraIndent_TrueAttribute_ReturnsOne()
		{
			var node = CreateXmlElementFromOuterXmlOf("<indent indent=\"true\" />");
			Assert.That(Slice.ExtraIndent(node), Is.EqualTo(1));
		}

		/// <summary>
		/// ExtraIndent returns 0 when indent attribute is absent.
		/// </summary>
		[Test]
		public void ExtraIndent_NoAttribute_ReturnsZero()
		{
			var node = CreateXmlElementFromOuterXmlOf("<indent />");
			Assert.That(Slice.ExtraIndent(node), Is.EqualTo(0));
		}

		#endregion

		#region Characterization Tests — Weight

		/// <summary>
		/// Weight property can be set and retrieved.
		/// </summary>
		[Test]
		public void Weight_SetAndGet()
		{
			using (var slice = new Slice())
			{
				slice.Weight = ObjectWeight.heavy;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.heavy));

				slice.Weight = ObjectWeight.light;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.light));

				slice.Weight = ObjectWeight.field;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.field));
			}
		}

		#endregion
	}
}
