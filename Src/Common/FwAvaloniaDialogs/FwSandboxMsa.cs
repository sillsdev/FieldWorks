// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-FREE mirror of the WinForms <c>MsaType</c> enum (LexText's <c>MsaType</c>), the morpheme
	/// syntax-analysis class that drives <see cref="FwMsaGroupBox"/>'s adaptive layout exactly as it drives
	/// the WinForms <c>MSAGroupBox</c>. The host maps the entry's morph type to one of these (the same
	/// mapping <c>MSAGroupBox.MorphTypePreference</c> performs) and feeds it in; the box shows the matching
	/// widgets. <see cref="NotSet"/> is the unconfigured sentinel (legacy <c>kNotSet</c>).
	/// </summary>
	public enum FwMsaType
	{
		/// <summary>Unconfigured (legacy kNotSet); the host should set a real type before the box is useful.</summary>
		NotSet,
		/// <summary>Stem (legacy kStem): Main POS only, label "Category".</summary>
		Stem,
		/// <summary>Root (legacy kRoot): identical layout to <see cref="Stem"/> — Main POS only.</summary>
		Root,
		/// <summary>Inflectional affix (legacy kInfl): Affix-Type + Main POS + Slot.</summary>
		Inflectional,
		/// <summary>Derivational affix (legacy kDeriv): Affix-Type + Main POS + Secondary POS.</summary>
		Derivational,
		/// <summary>Unclassified affix (legacy kUnclassified): Affix-Type (= Not sure) + Main POS.</summary>
		Unclassified
	}

	/// <summary>
	/// The lightweight, LCModel-FREE payload <see cref="FwMsaGroupBox"/> emits — the mirror of the WinForms
	/// <c>SandboxGenericMSA</c> (MsaType + MainPOS + SecondaryPOS + Slot), but carrying opaque id STRINGS
	/// instead of LCModel <c>IPartOfSpeech</c>/<c>IMoInflAffixSlot</c> objects. Stage 3 maps this back to a
	/// real MSA (find-or-create) by resolving the ids through the project cache. Like the WinForms property,
	/// only the fields RELEVANT to the current <see cref="MsaType"/> are populated:
	///   * Stem / Root / Unclassified → <see cref="MainPosId"/> only.
	///   * Inflectional → <see cref="MainPosId"/> + <see cref="SlotId"/> (slot only when valid for the POS).
	///   * Derivational → <see cref="MainPosId"/> + <see cref="SecondaryPosId"/>.
	/// </summary>
	public sealed class FwSandboxMsa
	{
		public FwSandboxMsa(FwMsaType msaType, string mainPosId = null, string secondaryPosId = null,
			string slotId = null, string inflectionClassId = null,
			IReadOnlyList<FwFeatureValueAssignment> inflectionFeatures = null)
		{
			MsaType = msaType;
			MainPosId = mainPosId;
			SecondaryPosId = secondaryPosId;
			SlotId = slotId;
			InflectionClassId = inflectionClassId;
			// Defensive copy so the payload is an immutable snapshot (the box keeps mutating its live set).
			InflectionFeatures = inflectionFeatures == null
				? (IReadOnlyList<FwFeatureValueAssignment>)Array.Empty<FwFeatureValueAssignment>()
				: inflectionFeatures.Where(a => a != null).ToList();
		}

		/// <summary>The morpheme syntax-analysis class (drives which other fields are meaningful).</summary>
		public FwMsaType MsaType { get; }

		/// <summary>Opaque id of the main part of speech, or null when "not specified" (the &lt;Any&gt; row).</summary>
		public string MainPosId { get; }

		/// <summary>Opaque id of the secondary ("changes to") POS — only for <see cref="FwMsaType.Derivational"/>.</summary>
		public string SecondaryPosId { get; }

		/// <summary>Opaque id of the inflectional-affix slot — only for <see cref="FwMsaType.Inflectional"/>.</summary>
		public string SlotId { get; }

		/// <summary>
		/// Opaque id of the inflection class (the WinForms <c>InsertEntryDlg.InflectionClass</c> →
		/// <c>IMoStemMsa.InflectionClassRA</c>), or null for "&lt;None&gt;". Only meaningful for the STEM/ROOT MSA
		/// (the common case). PARITY: derivational from/to inflection classes (<c>IMoDerivAffMsa.FromInflectionClassRA</c>
		/// / <c>ToInflectionClassRA</c>) are NOT carried here — Stage 6 scopes the inflection-class picker to the stem
		/// MSA, matching how the legacy <c>InsertEntryDlg</c> exposes a single inflection class on the stem/deriv-step MSA.
		/// </summary>
		public string InflectionClassId { get; }

		/// <summary>
		/// The chosen inflection-feature assignments (Phase-1 §19b Stage 2) — the LCModel-free flat
		/// <c>(closedFeatureId, valueId)</c> set the hosted <see cref="FwFeatureStructureEditor"/> emitted, carried only
		/// for the INFLECTIONAL / DERIVATIONAL MSA (where the WinForms box opens <c>MsaInflectionFeatureListDlg</c> over
		/// <c>IMoInflAffMsa.InflFeatsOA</c> / <c>IMoDerivAffMsa.FromMsFeaturesOA</c>). Empty when no feature was chosen
		/// (the legacy "delete the FS" / unspecified case). The launcher rebuilds the nested <c>IFsFeatStruc</c> from
		/// this flat set via recursive-ascent <c>GetOrCreateValue</c> on commit, in the SAME UOW as the MSA
		/// find-or-create. PARITY (§19b): stem/root MSAs (<c>IMoStemMsa.MsFeaturesOA</c>) and the derivational TO
		/// features are not carried — the box scopes the inflection-feature editor to the infl/deriv-FROM surface, the
		/// common case the legacy create/insert flow exposes. Never null.
		/// </summary>
		public IReadOnlyList<FwFeatureValueAssignment> InflectionFeatures { get; }
	}

	/// <summary>
	/// Which POS chooser inside <see cref="FwMsaGroupBox"/> raised a "Create a new Part of Speech..." request — the
	/// MAIN POS chooser (the "Category"/"Attaches to Category" field, present for every MsaType) or the SECONDARY
	/// POS chooser (the derivational "Changes to Category" field). The host uses it to route the created node back to
	/// the right chooser via <c>AcceptCreatedMainPos</c> / <c>AcceptCreatedSecondaryPos</c>. Stage 4 added this so the
	/// merged create event (which does not say which chooser fired) can be disambiguated by the VM.
	/// </summary>
	public enum FwPosTarget
	{
		/// <summary>The main POS chooser (the "Category" / "Attaches to Category" field).</summary>
		Main,

		/// <summary>The secondary ("changes to") POS chooser (derivational affixes only).</summary>
		Secondary
	}

	/// <summary>
	/// A lightweight, LCModel-FREE inflection-affix slot option fed to <see cref="FwMsaGroupBox"/>'s Slot
	/// picker (the mirror of an <c>IMoInflAffixSlot</c> the WinForms box loads into <c>m_fwcbSlots</c>). The
	/// host (Stage 3) builds these from the main POS's affix slots; <see cref="Id"/> is round-tripped verbatim.
	/// </summary>
	public sealed class FwInflectionSlot
	{
		public FwInflectionSlot(string id, string name)
		{
			Id = id;
			Name = name;
		}

		/// <summary>Opaque stable identifier (a guid string in the product); round-tripped verbatim.</summary>
		public string Id { get; }

		/// <summary>The display name shown in the Slot combo.</summary>
		public string Name { get; }
	}

	/// <summary>
	/// A lightweight, LCModel-FREE inflection-class option fed to <see cref="FwMsaGroupBox"/>'s inflection-class
	/// picker (the mirror of an <c>IMoInflClass</c> in the selected main POS's <c>InflectionClassesOC</c>, including
	/// nested <c>SubclassesOC</c>). The host (Stage 6) builds these from the currently-selected main POS and re-feeds
	/// them when the main POS changes — exactly how the slot list follows the POS. <see cref="Id"/> is round-tripped
	/// verbatim; <see cref="Depth"/> carries the nesting level so the picker can indent subclasses like the WinForms
	/// <c>InflectionClassPopupTreeManager</c> tree.
	/// </summary>
	public sealed class FwInflectionClass
	{
		public FwInflectionClass(string id, string name, int depth = 0)
		{
			Id = id;
			Name = name;
			Depth = depth;
		}

		/// <summary>Opaque stable identifier (a guid string in the product); round-tripped verbatim.</summary>
		public string Id { get; }

		/// <summary>The display name shown in the inflection-class picker.</summary>
		public string Name { get; }

		/// <summary>The nesting depth (0 for a top-level class, deeper for nested subclasses) for indentation.</summary>
		public int Depth { get; }
	}
}
