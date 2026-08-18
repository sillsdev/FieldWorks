// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Review consolidation (morph-type GUID knowledge): <see cref="MorphTypeSwapLogic"/> is the
	/// single GUID -> kind table, but it lives in the deliberately LCModel-free FwAvalonia seam
	/// project, so it mirrors the fixed <c>MoMorphTypeTags</c> model GUIDs as literals. This
	/// fixture -- in xWorksTests, which references BOTH assemblies -- pins every literal to its
	/// MoMorphTypeTags constant and pins the stem set to the legacy
	/// <c>MorphTypeAtomicLauncher.IsStemType</c> guid list, so neither mirror can drift.
	/// </summary>
	[TestFixture]
	public class MorphTypeGuidConsolidationTests
	{
		private static readonly (Guid Guid, MorphTypeKind Kind)[] ExpectedKinds =
		{
			(MoMorphTypeTags.kguidMorphRoot, MorphTypeKind.Root),
			(MoMorphTypeTags.kguidMorphStem, MorphTypeKind.Stem),
			(MoMorphTypeTags.kguidMorphBoundRoot, MorphTypeKind.BoundRoot),
			(MoMorphTypeTags.kguidMorphBoundStem, MorphTypeKind.BoundStem),
			(MoMorphTypeTags.kguidMorphParticle, MorphTypeKind.Particle),
			(MoMorphTypeTags.kguidMorphClitic, MorphTypeKind.Clitic),
			(MoMorphTypeTags.kguidMorphProclitic, MorphTypeKind.Proclitic),
			(MoMorphTypeTags.kguidMorphEnclitic, MorphTypeKind.Enclitic),
			(MoMorphTypeTags.kguidMorphPhrase, MorphTypeKind.Phrase),
			(MoMorphTypeTags.kguidMorphDiscontiguousPhrase, MorphTypeKind.DiscontiguousPhrase),
			(MoMorphTypeTags.kguidMorphPrefix, MorphTypeKind.Prefix),
			(MoMorphTypeTags.kguidMorphSuffix, MorphTypeKind.Suffix),
			(MoMorphTypeTags.kguidMorphInfix, MorphTypeKind.Infix),
			(MoMorphTypeTags.kguidMorphSimulfix, MorphTypeKind.Simulfix),
			(MoMorphTypeTags.kguidMorphSuprafix, MorphTypeKind.Suprafix),
			(MoMorphTypeTags.kguidMorphCircumfix, MorphTypeKind.Circumfix),
			(MoMorphTypeTags.kguidMorphPrefixingInterfix, MorphTypeKind.PrefixingInterfix),
			(MoMorphTypeTags.kguidMorphInfixingInterfix, MorphTypeKind.InfixingInterfix),
			(MoMorphTypeTags.kguidMorphSuffixingInterfix, MorphTypeKind.SuffixingInterfix)
		};

		[Test]
		public void TryClassify_PinsEveryGuidLiteral_ToItsMoMorphTypeTagsConstant()
		{
			foreach (var (guid, expectedKind) in ExpectedKinds)
			{
				Assert.That(MorphTypeSwapLogic.TryClassify(guid, out var kind), Is.True,
					$"the seam's table must contain MoMorphTypeTags {expectedKind} ({guid})");
				Assert.That(kind, Is.EqualTo(expectedKind),
					$"the seam's literal for {expectedKind} drifted from MoMorphTypeTags");
			}
		}

		// The legacy MorphTypeAtomicLauncher.IsStemType guid list (bound root/stem, enclitic,
		// particle, proclitic, root, stem, clitic, phrase, discontiguous phrase) -- the launcher
		// cannot delegate to the seam yet (DetailControls has no FwAvalonia reference), so this
		// pins the two sets to each other until the launcher retires.
		[Test]
		public void IsStemType_ByGuid_MatchesTheLegacyLauncherSet()
		{
			var legacyStemGuids = new[]
			{
				MoMorphTypeTags.kguidMorphBoundRoot,
				MoMorphTypeTags.kguidMorphBoundStem,
				MoMorphTypeTags.kguidMorphEnclitic,
				MoMorphTypeTags.kguidMorphParticle,
				MoMorphTypeTags.kguidMorphProclitic,
				MoMorphTypeTags.kguidMorphRoot,
				MoMorphTypeTags.kguidMorphStem,
				MoMorphTypeTags.kguidMorphClitic,
				MoMorphTypeTags.kguidMorphPhrase,
				MoMorphTypeTags.kguidMorphDiscontiguousPhrase
			};

			foreach (var (guid, kind) in ExpectedKinds)
			{
				var expected = Array.IndexOf(legacyStemGuids, guid) >= 0;
				Assert.That(MorphTypeSwapLogic.IsStemType(guid), Is.EqualTo(expected),
					$"{kind} stem/affix classification drifted from MorphTypeAtomicLauncher.IsStemType");
			}
		}

		[Test]
		public void UnknownGuid_DoesNotClassify_AndIsNotAStemType()
		{
			var unknown = Guid.NewGuid(); // a user-created morph type has no fixed model guid
			Assert.That(MorphTypeSwapLogic.TryClassify(unknown, out _), Is.False);
			Assert.That(MorphTypeSwapLogic.IsStemType(unknown), Is.False,
				"unknown guids classify as not-a-stem, like the legacy null guard");
		}
	}

	/// <summary>
	/// Review consolidation (editor-kind knowledge): <see cref="EditorKindMap.ClassifyDetailFieldKind"/>
	/// is the ONE editor-string -> category table the composer's dispatch switch and
	/// <c>DetailModelProjector.ClassifyKind</c> both consume. These cases pin the categories the
	/// two consumers' behavior depends on.
	/// </summary>
	[TestFixture]
	public class EditorKindMapDetailCategoryTests
	{
		[TestCase("multistring", DetailEditorCategory.Text)]
		[TestCase("string", DetailEditorCategory.Text)]
		[TestCase("MorphTypeAtomicReference", DetailEditorCategory.MorphTypeChooser)]
		[TestCase("summary", DetailEditorCategory.Summary)]
		[TestCase("lit", DetailEditorCategory.Literal)]
		[TestCase("picture", DetailEditorCategory.Picture)]
		[TestCase("image", DetailEditorCategory.Picture)]
		[TestCase("jtview", DetailEditorCategory.EmbeddedView)]
		[TestCase("command", DetailEditorCategory.Command)]
		[TestCase("enumComboBox", DetailEditorCategory.EnumCombo)]
		[TestCase("possatomicreference", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("atomicreferencepos", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("atomicreferenceposdisabled", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("defaultatomicreference", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase("defaultatomicreferencedisabled", DetailEditorCategory.AtomicReferenceChooser)]
		[TestCase(null, DetailEditorCategory.Grouping)]
		[TestCase("", DetailEditorCategory.Grouping)]
		// Other: consumers refine by CellarPropertyType (composer) or render as text (mapper).
		[TestCase("checkbox", DetailEditorCategory.Other)]
		[TestCase("gendate", DetailEditorCategory.Other)]
		[TestCase("integer", DetailEditorCategory.Other)]
		[TestCase("autocustom", DetailEditorCategory.Other)]
		[TestCase("no-such-editor", DetailEditorCategory.Other)]
		public void ClassifyDetailFieldKind_RoutesLikeTheLegacyDispatch(string rawEditor,
			DetailEditorCategory expected)
		{
			// Case-insensitive, like DataTree's editor.ToLower() dispatch.
			Assert.That(EditorKindMap.ClassifyDetailFieldKind(rawEditor), Is.EqualTo(expected));
		}
	}
}
