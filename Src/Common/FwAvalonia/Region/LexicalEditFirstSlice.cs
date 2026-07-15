// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Builds the LexEntry-identity first-slice view definition by compiling the live shipped layout
	/// inventory through <see cref="ViewDefinitionCompiler"/> (task 4.10), replacing the hand-authored
	/// definition. The three first-slice fields are selected from the real compiled layouts:
	/// - lexeme form: the <c>AsLexemeForm</c> slice (<c>Form</c>, multistring) compiled from
	///   <c>MoStemAllomorph/AsLexemeFormBasic</c> via the base-class part fallback to <c>MoForm</c>;
	/// - morph type: its compiled <c>MorphTypeBasic</c> caller child (<c>MorphType</c>,
	///   <c>MorphTypeAtomicReference</c>);
	/// - gloss: the <c>LexSense-Detail-GlossAllA</c> part slice (<c>Gloss</c>, multistring) compiled
	///   from the real parts inventory through a one-line caller layout. The shipped
	///   <c>LexSense/Normal</c> layout reaches Gloss only through its <c>HeavySummary</c> part ref,
	///   which has no part definition in the shipped inventory — legacy <c>DataTree</c> walks the class
	///   hierarchy and then silently omits it (DataTree.ProcessPartRefNode), so the part inventory, not
	///   that layout, is the live source for the gloss slice's semantics.
	/// Stable ids therefore derive from the real layout/part paths. Product metadata (automation ids,
	/// <see cref="SurfaceRouting.Product"/>) is stamped on the selected nodes. The authored definition
	/// remains only as an explicit fallback (with a diagnostic) for when the layout directory is
	/// missing or a shipped layout changes shape.
	/// </summary>
	public static class LexicalEditFirstSlice
	{
		// One-line caller layout for the gloss slice; all slice semantics (label, editor, ws, menus)
		// come from the real LexSense-Detail-GlossAllA part it references.
		private const string GlossCallerLayout =
			"<layout class='LexSense' type='detail' name='FirstSliceGloss'><part ref='GlossAllA'/></layout>";

		/// <summary>Subclass → base class chain for part-ref resolution, mirroring the LCModel hierarchy.</summary>
		private static readonly Dictionary<string, string> MoFormBaseClassMap = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			{ "MoStemAllomorph", "MoForm" },
			{ "MoAffixAllomorph", "MoAffixForm" },
			{ "MoAffixForm", "MoForm" },
			{ "MoAffixProcess", "MoForm" }
		};

		/// <summary>
		/// Compiles the first-slice definition from the shipped layout/parts directory. Returns null when
		/// the directory or any required layout/node cannot be found, so the caller can fall back to
		/// <see cref="AuthoredFallback"/> explicitly.
		/// </summary>
		public static ViewDefinitionModel CompileFromLayoutDirectory(string partsDirectory, ViewDefinitionCompiler compiler = null)
		{
			if (string.IsNullOrEmpty(partsDirectory) || !Directory.Exists(partsDirectory))
			{
				return null;
			}

			try
			{
				// Finding D: the parts merge rides the ONE shared loader FullEntryRegionComposer
				// (xWorks) also uses, so the two compile lanes cannot drift apart.
				var partsXml = LayoutSourceLoader.LoadMergedPartsXml(partsDirectory);
				if (partsXml == null)
				{
					return null;
				}

				compiler = compiler ?? SharedCompiler;

				var lexemeFormModel = CompileLayout(compiler, partsDirectory, "Morphology.fwlayout",
					"MoStemAllomorph", "AsLexemeFormBasic", partsXml, MoFormBaseClassMap);
				var senseModel = compiler.Compile(
					new ViewDefinitionSourceSnapshot("LexSense", "detail", GlossCallerLayout, partsXml));
				if (lexemeFormModel == null || senseModel == null)
				{
					return null;
				}

				var formNode = FindField(lexemeFormModel.Roots, "Form");
				var morphTypeNode = FindField(lexemeFormModel.Roots, "MorphType");
				var glossNode = FindField(senseModel.Roots, "Gloss");
				if (formNode == null || morphTypeNode == null || glossNode == null)
				{
					return null;
				}

				var roots = new List<ViewNode>
				{
					StampProductLeaf(formNode, "LexemeFormEditor", FwAvaloniaStrings.LexemeFormLabel),
					StampProductLeaf(morphTypeNode, "MorphTypeChooser", null),
					StampProductLeaf(glossNode, "SenseGlossEditor", null)
				};

				return new ViewDefinitionModel("LexEntry", "identity", "detail", roots, Array.Empty<ViewDiagnostic>());
			}
			catch (IOException)
			{
				return null;
			}
			catch (System.Xml.XmlException)
			{
				return null;
			}
		}

		/// <summary>
		/// The previous hand-authored definition, kept only as an explicit fallback. Field names match the
		/// compiled definition (<c>Form</c>/<c>MorphType</c>/<c>Gloss</c>) so one value provider serves both.
		/// </summary>
		public static ViewDefinitionModel AuthoredFallback()
		{
			var roots = new List<ViewNode>
			{
				Leaf("LexEntry/identity/#0", FwAvaloniaStrings.LexemeFormLabel, "Form", "multistring", "all vernacular", "LexemeFormEditor"),
				Leaf("LexEntry/identity/#1", FwAvaloniaStrings.MorphTypeLabel, "MorphType", "morphtypeatomicreference", null, "MorphTypeChooser"),
				Leaf("LexEntry/identity/#2", FwAvaloniaStrings.GlossLabel, "Gloss", "multistring", "all analysis", "SenseGlossEditor")
			};

			var diagnostics = new[]
			{
				new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "authored-fallback",
					"The first-slice definition could not be compiled from the live layout inventory; using the authored fallback.",
					"LexEntry/identity")
			};

			return new ViewDefinitionModel("LexEntry", "identity", "detail", roots, diagnostics);
		}

		private static readonly ViewDefinitionCompiler SharedCompiler = new ViewDefinitionCompiler();

		private static ViewDefinitionModel CompileLayout(
			ViewDefinitionCompiler compiler, string partsDirectory, string layoutFileName,
			string className, string layoutName, string partsXml,
			IReadOnlyDictionary<string, string> baseClassMap)
		{
			var layoutPath = Path.Combine(partsDirectory, layoutFileName);
			if (!File.Exists(layoutPath))
			{
				return null;
			}

			// Finding D: layout lookup rides the shared loader's first-wins matcher.
			var layout = LayoutSourceLoader.FindLayout(
				new[] { XElement.Load(layoutPath) }, className, layoutName);
			if (layout == null)
			{
				return null;
			}

			var snapshot = new ViewDefinitionSourceSnapshot(className, "detail", layout.ToString(), partsXml, baseClassMap);
			return compiler.Compile(snapshot);
		}

		private static ViewNode FindField(IReadOnlyList<ViewNode> nodes, string field)
		{
			foreach (var node in nodes)
			{
				if (node.Kind == ViewNodeKind.Field && node.Field == field)
				{
					return node;
				}

				var match = FindField(node.Children, field);
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		// The first slice renders the selected nodes flat, so children (e.g. AllomorphStatus under the
		// lexeme form) are stripped; they return when the region grows past the identity slice.
		private static ViewNode StampProductLeaf(ViewNode source, string automationId, string labelOverride)
			=> new ViewNode(source.StableId, ViewNodeKind.Field, labelOverride ?? source.Label, source.Abbreviation,
				source.Field, source.RawEditor, source.EditorClassification, source.WritingSystem, source.Visibility,
				source.Expansion, source.Indented, source.TargetLayout, null,
				source.LocalizationKey, automationId, SurfaceRouting.Product);

		private static ViewNode Leaf(string stableId, string label, string field, string editor, string ws, string automationId)
			=> new ViewNode(stableId, ViewNodeKind.Field, label, null, field, editor,
				EditorClassification.Known, ws, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
				localizationKey: null, automationId: automationId, routing: SurfaceRouting.Product);
	}
}
