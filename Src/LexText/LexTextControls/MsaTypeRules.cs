// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Presentation-agnostic rules for editing a morpho-syntactic analysis (MSA). These mappings
	/// are extracted from <see cref="MSAGroupBox"/> so they can be unit tested without a WinForms
	/// control, a running cache, or an STA thread, and so a future non-WinForms view can reuse
	/// exactly the same logic instead of reimplementing it.
	/// </summary>
	internal static class MsaTypeRules
	{
		/// <summary>
		/// The three affix MSA types. These mirror the three affix MSA classes
		/// (kInfl=MoInflAffMsa, kDeriv=MoDerivAffMsa, kUnclassified=MoUnclassifiedAffixMsa) and the
		/// three entries of the affix-type combo (Inflectional/Derivational/Not Sure). For all of
		/// them the editor shows the affix layout ("Attaches to category"); kStem/kRoot show the
		/// stem layout, and kNotSet/kMixed are not used by the editor.
		/// </summary>
		internal static bool IsAffixMsaType(MsaType type)
		{
			return type == MsaType.kUnclassified || type == MsaType.kInfl || type == MsaType.kDeriv;
		}

		/// <summary>
		/// Whether setting the main category should fall back to kStem. Setting the category must
		/// not turn an affix entry into a stem, so only default to kStem from a stem-like or unset
		/// type; an affix type that the morpheme type already established is preserved.
		/// </summary>
		internal static bool ShouldForceStemForMainPos(MsaType current)
		{
			return current != MsaType.kStem && !IsAffixMsaType(current);
		}

		/// <summary>
		/// The MSA type implied by a morpheme type, given the type currently selected. Stem-like
		/// morpheme types map to kStem/kRoot; affix morpheme types map to kUnclassified unless an
		/// affix type is already chosen, in which case the (more specific) current type is kept.
		/// </summary>
		/// <param name="morphTypeGuid">The morpheme type's Guid as a string (IMoMorphType.Guid.ToString()).</param>
		/// <param name="current">The currently selected MSA type.</param>
		internal static MsaType MsaTypeForMorphType(string morphTypeGuid, MsaType current)
		{
			switch (morphTypeGuid)
			{
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphBoundStem:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					return MsaType.kStem;
				case MoMorphTypeTags.kMorphProclitic:
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphEnclitic:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphBoundRoot:
					return MsaType.kRoot;
				default:
					// Affix morpheme types (prefix/suffix/infix/circumfix/simulfix/suprafix/interfixes).
					// kInfl/kDeriv are more specific than kUnclassified, so leave them alone; only
					// upgrade to kUnclassified from a stem-like type.
					return (current == MsaType.kRoot || current == MsaType.kStem)
						? MsaType.kUnclassified
						: current;
			}
		}

		/// <summary>
		/// Build the <see cref="SandboxGenericMSA"/> described by the current editor state. The slot
		/// is included only for an inflectional affix whose slot is valid for the selected category,
		/// and the secondary category only for a derivational affix.
		/// </summary>
		internal static SandboxGenericMSA BuildSandboxMsa(MsaType type, IPartOfSpeech mainPos,
			IPartOfSpeech secondaryPos, IMoInflAffixSlot slot, bool slotValid)
		{
			var sandboxMsa = new SandboxGenericMSA { MsaType = type };
			switch (type)
			{
				case MsaType.kRoot: // Fall through
				case MsaType.kStem:
					sandboxMsa.MainPOS = mainPos;
					break;
				case MsaType.kInfl:
					sandboxMsa.MainPOS = mainPos;
					if (slot != null && slotValid)
						sandboxMsa.Slot = slot;
					break;
				case MsaType.kDeriv:
					sandboxMsa.MainPOS = mainPos;
					sandboxMsa.SecondaryPOS = secondaryPos;
					break;
				case MsaType.kUnclassified:
					sandboxMsa.MainPOS = mainPos;
					break;
			}
			return sandboxMsa;
		}
	}
}
