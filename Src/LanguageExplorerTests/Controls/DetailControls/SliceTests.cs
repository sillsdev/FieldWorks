// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using LanguageExplorerTests.Impls;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.Controls.DetailControls
{
	/// <summary />
	[TestFixture]
	public class SliceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DataTree _dataTree;
		private Slice _slice;
		private FlexComponentParameters _flexComponentParameters;
		private DummyFwMainWnd _dummyWindow;

		#region Overrides of MemoryOnlyBackendProviderRestoredForEachTestTestBase

		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			_dummyWindow = new DummyFwMainWnd();
			_flexComponentParameters.PropertyTable.SetProperty(FwUtils.window, _dummyWindow);
		}
		#endregion

		/// <summary />
		public override void TestTearDown()
		{
			try
			{
				_dummyWindow?.Dispose();
				_slice?.Dispose();
				_dataTree?.Dispose();
				_flexComponentParameters?.PropertyTable?.Dispose();

				_dummyWindow = null;
				_slice = null;
				_dataTree = null;
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

		/// <summary />
		[Test]
		public void Basic1()
		{
			_slice = new Slice();
			Assert.NotNull(_slice);
		}

		/// <summary />
		[Test]
		public void Basic2()
		{
			using (var control = new Control())
			using (var slice = new Slice(control))
			{
				Assert.AreEqual(control, slice.Control);
				Assert.NotNull(slice);
			}
		}

		/// <summary />
		internal static XElement CreateXmlElementFromOuterXmlOf(string outerXml)
		{
			return XElement.Parse(outerXml);
		}

		/// <summary />
		private static Slice GenerateSlice(LcmCache cache, DataTree datatree)
		{
			var slice = new Slice();
			var parts = DataTreeTests.GenerateParts();
			var layouts = DataTreeTests.GenerateLayouts();
			datatree.Initialize(cache, false, layouts, parts);
			slice.Parent = datatree;
			return slice;
		}

		/// <summary />
		private static ArrayList GeneratePath()
		{
			// Data taken from a running Sena 3
			var path = new ArrayList(7)
			{
				CreateXmlElementFromOuterXmlOf("<layout class=\"LexEntry\" type=\"detail\" name=\"Normal\"><part label=\"Lexeme Form\" ref=\"LexemeForm\" /><part label=\"Citation Form\" ref=\"CitationFormAllV\" /><part ref=\"ComplexFormEntries\" visibility=\"ifdata\" /><part ref=\"EntryRefs\" param=\"Normal\" visibility=\"ifdata\" /><part ref=\"EntryRefsGhostComponents\" visibility=\"always\" /><part ref=\"EntryRefsGhostVariantOf\" visibility=\"never\" /><part ref=\"Pronunciations\" param=\"Normal\" visibility=\"ifdata\" /><part ref=\"Etymology\" menu=\"mnuDataTree-InsertEtymology\" visibility=\"ifdata\" /><part ref=\"CommentAllA\" /><part ref=\"LiteralMeaningAllA\" visibility=\"ifdata\" /><!-- Only for Subentries. --><part ref=\"BibliographyAllA\" visibility=\"ifdata\" /><part ref=\"RestrictionsAllA\" visibility=\"ifdata\" /><part ref=\"SummaryDefinitionAllA\" visibility=\"ifdata\" /><part ref=\"ExcludeAsHeadword\" label=\"Exclude As Headword\" visibility=\"never\" /><!-- todo 'ifTrue' --><part ref=\"CurrentLexReferences\" visibility=\"ifdata\" /><!-- Special part to indicate where custom fields should be inserted at.  Handled in Common.Framework.DetailControls.DataTree --><part customFields=\"here\" /><part ref=\"ImportResidue\" label=\"Import Residue\" visibility=\"ifdata\" /><part ref=\"DateCreatedAllA\" visibility=\"never\" /><part ref=\"DateModifiedAllA\" visibility=\"never\" /><part ref=\"Senses\" param=\"Normal\" expansion=\"expanded\" /><part ref=\"VariantFormsSection\" expansion=\"expanded\" label=\"Variants\" menu=\"mnuDataTree-VariantForms\" hotlinks=\"mnuDataTree-VariantForms-Hotlinks\"><indent><part ref=\"VariantForms\" /></indent></part><part ref=\"AlternateFormsSection\" expansion=\"expanded\" label=\"Allomorphs\" menu=\"mnuDataTree-AlternateForms\" hotlinks=\"mnuDataTree-AlternateForms-Hotlinks\"><indent><part ref=\"AlternateForms\" param=\"Normal\" /></indent></part><part ref=\"GrammaticalFunctionsSection\" label=\"Grammatical Info. Details\" menu=\"mnuDataTree-Help\" hotlinks=\"mnuDataTree-Help\"><indent><part ref=\"MorphoSyntaxAnalyses\" param=\"Normal\" /></indent></part></layout>"),
				CreateXmlElementFromOuterXmlOf("<part label=\"Lexeme Form\" ref=\"LexemeForm\" />"),
				CreateXmlElementFromOuterXmlOf("<obj field=\"LexemeForm\" layout=\"AsLexemeFormBasic\" menu=\"mnuDataTree-Help\" ghost=\"Form\" ghostWs=\"vernacular\" ghostLabel=\"Lexeme Form\" ghostClass=\"MoStemAllomorph\" ghostInitMethod=\"SetMorphTypeToRoot\" />"), 21631,
				CreateXmlElementFromOuterXmlOf("<layout class=\"MoStemAllomorph\" type=\"detail\" name=\"AsLexemeFormBasic\"><part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part></layout>"),
				CreateXmlElementFromOuterXmlOf("<part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part>"),
				CreateXmlElementFromOuterXmlOf("<slice field=\"Form\" label=\"Form\" editor=\"multistring\" ws=\"all vernacular\" weight=\"light\" menu=\"mnuDataTree-LexemeForm\" contextMenu=\"mnuDataTree-LexemeFormContext\" spell=\"no\"><properties><bold value=\"on\" /><fontsize value=\"120%\" /></properties></slice>")
			};
			return path;
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// </remarks>
		[Test]
		public void CreateIndentedNodes_basic()
		{
			_dataTree = new DataTree(new SharedEventHandlers());
			_dataTree.InitializeFlexComponent(_flexComponentParameters);
			_slice = GenerateSlice(Cache, _dataTree);

			// Data taken from a running Sena 3
			var caller = CreateXmlElementFromOuterXmlOf("<part ref=\"AsLexemeForm\" label=\"Lexeme Form\" expansion=\"expanded\"><indent><part ref=\"IsAbstractBasic\" label=\"Is Abstract Form\" visibility=\"never\" /><!-- could use 'ifTrue' if we had it --><part ref=\"MorphTypeBasic\" visibility=\"ifdata\" /><part ref=\"PhoneEnvBasic\" visibility=\"ifdata\" /><part ref=\"StemNameForLexemeForm\" visibility=\"ifdata\" /></indent></part>");

			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			const int indent = 0;
			var insPos = 1;

			var path = GeneratePath();

			var reuseMap = new ObjSeqHashMap();
			// Data taken from a running Sena 3
			var node = CreateXmlElementFromOuterXmlOf("<slice field=\"Form\" label=\"Form\" editor=\"multistring\" ws=\"all vernacular\" weight=\"light\" menu=\"mnuDataTree-LexemeForm\" contextMenu=\"mnuDataTree-LexemeFormContext\" spell=\"no\"><properties><bold value=\"on\" /><fontsize value=\"120%\" /></properties></slice>");

			_slice.CreateIndentedNodes(caller, obj, indent, ref insPos, path, reuseMap, node);
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// </remarks>
		[Test]
		public void Expand()
		{
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			_dataTree = new DataTree(new SharedEventHandlers());
			_dataTree.InitializeFlexComponent(_flexComponentParameters);
			_slice = GenerateSlice(Cache, _dataTree);
			_slice.Key = GeneratePath().ToArray();
			_slice.MyCmObject = obj;
			_slice.InitializeFlexComponent(_flexComponentParameters);
			_flexComponentParameters.PropertyTable.SetProperty("cache", Cache);

			_slice.Expand();
		}

		/// <remarks>
		/// Currently just enough to compile and run.
		/// Isn't actually collapsing anything.
		/// </remarks>
		[Test]
		public void Collapse()
		{
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();

			_dataTree = new DataTree(new SharedEventHandlers());
			_dataTree.InitializeFlexComponent(_flexComponentParameters);
			_slice = GenerateSlice(Cache, _dataTree);
			_slice.Key = GeneratePath().ToArray();
			_slice.MyCmObject = obj;
			_slice.InitializeFlexComponent(_flexComponentParameters);
			_flexComponentParameters.PropertyTable.SetProperty("cache", Cache);

			_slice.Collapse();
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
			_dataTree = new DataTree(new SharedEventHandlers());
			_dataTree.InitializeFlexComponent(_flexComponentParameters);
			_slice = GenerateSlice(Cache, _dataTree);
			_slice.InitializeFlexComponent(_flexComponentParameters);
			var node = CreateXmlElementFromOuterXmlOf("<seq field=\"Pronunciations\" layout=\"Normal\" ghost=\"Form\" ghostWs=\"pronunciation\" ghostLabel=\"Pronunciation\" menu=\"mnuDataTree-Pronunciation\" />");
			const int indent = 0;
			var insertPosition = 0;
			const int flidEmptyProp = 5002031; // runtime flid of ghost field
			_dataTree.MakeGhostSlice(path, node, reuseMap, obj, _slice, flidEmptyProp, null, indent, ref insertPosition);
			var ghostSlice = _dataTree.Slices[0];
			Assert.NotNull(ghostSlice);
		}
	}
}