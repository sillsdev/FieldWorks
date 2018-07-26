// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class specifies the fields to display in an interlinear text, and which writing systems
	/// to display, where relevant. It has an indexer, and item(n) is the spec of what to show as
	/// the nth line. This class also has the knowledge about allowed orders.
	/// </summary>
	/// <remarks>
	/// NOTE: whenever this class is updated to include the tagging line(s), InterlinClipboardHelper
	/// (in InterlinDocView.cs) has to be fixed.  It currently hacks up a solution for a single
	/// tagging line when it thinks it needs to do so.
	/// Of course, it might be better implemented overall by a CollectorEnv approach...
	/// </remarks>
	public class InterlinLineChoices : ICloneable
	{
		readonly Color kMorphLevelColor = Color.Purple;
		readonly Color kWordLevelColor = Color.Blue;
		internal List<LineOption> m_allLineOptions = new List<LineOption>();
		internal List<InterlinLineSpec> m_specs = new List<InterlinLineSpec>();
		internal int m_wsDefVern; // The default vernacular writing system.
		internal int m_wsDefAnal; // The default analysis writing system.
		internal ILangProject m_proj;   // provides more ws info.
		internal LcmCache m_cache;
		private readonly Dictionary<int, string> m_fieldNames = new Dictionary<int, string>();
		InterlinMode m_mode = InterlinMode.Analyze;

		public InterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs)
			: this(proj, defaultVernacularWs, defaultAnalysisWs, InterlinMode.Analyze)
		{
		}

		public InterlinLineChoices(LcmCache cache, int defaultVernacularWs, int defaultAnalysisWs)
			: this(cache, defaultVernacularWs, defaultAnalysisWs, InterlinMode.Analyze)
		{
		}

		public InterlinLineChoices(LcmCache cache, int defaultVernacularWs, int defaultAnalysisWs, InterlinMode mode)
		{
			m_cache = cache;
			Mode = mode;
			UpdateFieldNamesFromLines(mode);
			m_wsDefVern = defaultVernacularWs;
			m_wsDefAnal = defaultAnalysisWs == WritingSystemServices.kwsAnal ? m_cache.DefaultAnalWs : defaultAnalysisWs;
			AllLineOptions = LineOptions(mode).ToList();
		}

		public InterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs, InterlinMode mode)
			: this(proj.Cache, defaultVernacularWs, defaultAnalysisWs, mode)
		{
			m_proj = proj; // Not used any more. TODO: remove, and modify callers.
		}

		/// <summary>
		/// The mode that the configured lines are in.
		/// If the mode changes, we'll reinitialze the fieldname (label) info.
		/// </summary>
		internal InterlinMode Mode
		{
			get { return m_mode; }
			set
			{
				if (m_mode == value)
				{
					return;
				}
				m_mode = value;
				// recompute labels for flids
				UpdateFieldNamesFromLines(m_mode);
			}
		}

		internal List<LineOption> AllLineOptions
		{
			get { return m_allLineOptions; }
			set
			{
				m_allLineOptions = value;

				// AllLineOptions and AllLineSpecs will be identical
				// On a set of AllLineOptions, AllLineSpecs will also be updated.
				var newLineSpecs = new List<InterlinLineSpec>();
				foreach (var option in value)
				{
					newLineSpecs.Add(CreateSpec(option.Flid, 0));
				}
				AllLineSpecs = newLineSpecs;
			}
		}

		internal List<InterlinLineSpec> AllLineSpecs { get; private set; } = new List<InterlinLineSpec>();

		/// <summary>
		/// Count previous occurrences of the flid at the specified index.
		/// </summary>
		public int PreviousOccurrences(int index)
		{
			var prev = 0;
			for (var i = 0; i < index; i++)
			{
				if (this[i].Flid == this[index].Flid)
				{
					prev++;
				}
			}
			return prev;
		}

		public static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis)
		{
			return DefaultChoices(proj, vern, analysis, InterlinMode.Analyze);
		}

		public static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis, InterlinMode mode)
		{
			var result = new InterlinLineChoices(proj, vern, analysis, mode);
			switch (mode)
			{
				case InterlinMode.Analyze:
					result.SetStandardState();
					break;
				case InterlinMode.Chart:
					result.SetStandardChartState();
					break;
				case InterlinMode.Gloss:
				case InterlinMode.GlossAddWordsToLexicon:
					result.SetStandardGlossState();
					break;
			}
			return result;
		}

		internal void SetStandardChartState()
		{
			m_specs.Clear();
			Add(kflidWord);
			Add(kflidWordGloss);
			Add(kflidMorphemes);
			Add(kflidLexGloss);
			Add(kflidLexEntries);
			Add(kflidLexPos);
		}

		internal void SetStandardState()
		{
			m_specs.Clear();
			Add(kflidWord); // 0
			Add(kflidMorphemes); // 1
			Add(kflidLexEntries); // 2
			Add(kflidLexGloss); // 3
			Add(kflidLexPos); // 4
			Add(kflidWordGloss); // 5
			Add(kflidWordPos); // 6
			Add(kflidFreeTrans); // 7
		}

		internal void SetStandardGlossState()
		{
			m_specs.Clear();
			Add(kflidWord); // 0
			Add(kflidWordGloss); // 5
			Add(kflidWordPos); // 6
			Add(kflidFreeTrans); // 7
		}

		public string Persist(ILgWritingSystemFactory wsf)
		{
			var builder = new StringBuilder();
			builder.Append(GetType().Name);
			foreach (var spec in m_specs)
			{
				builder.Append(",");
				builder.Append(spec.Flid);
				builder.Append("%");
				builder.Append(wsf.GetStrFromWs(spec.WritingSystem));
			}
			return builder.ToString();
		}

		/// <summary />
		public static InterlinLineChoices Restore(string data, ILgWritingSystemFactory wsf, ILangProject proj, int defVern, int defAnalysis, InterlinMode mode = InterlinMode.Analyze)
		{
			Debug.Assert(defVern != 0);
			Debug.Assert(defAnalysis != 0);

			InterlinLineChoices result;
			var parts = data.Split(',');

			switch (parts[0])
			{
				case "InterlinLineChoices":
					result = new InterlinLineChoices(proj, defVern, defAnalysis, mode);
					break;
				case "EditableInterlinLineChoices":
					result = new EditableInterlinLineChoices(proj, defVern, defAnalysis);
					break;
				default:
					throw new Exception("Unrecognised type of InterlinLineChoices: " + parts[0]);
			}
			for (var i = 1; i < parts.Length; i++)
			{
				var flidAndWs = parts[i].Split('%');
				if (flidAndWs.Length != 2)
				{
					throw new Exception($"Unrecognized InterlinLineSpec: {parts[i]}");
				}
				var flid = int.Parse(flidAndWs[0]);
				var ws = wsf.GetWsFromStr(flidAndWs[1]);
				result.Add(flid, ws);
			}
			return result;
		}

		public int Count => m_specs.Count;

		public bool HaveMorphemeLevel
		{
			get
			{
				for (var i = 0; i < m_specs.Count; i++)
				{
					if (this[i].MorphemeLevel)
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Answer true if it is valid for the field indicated in spec2 to immediately
		/// follow the one indicated in spec1. By default any order is OK. This routine is
		/// not responsible for the restrictions related to keeping word level stuff before
		/// freeforms and morpheme-level stuff together.
		/// </summary>
		internal virtual bool CanFollow(InterlinLineSpec spec1, InterlinLineSpec spec2)
		{
			return true;
		}

		public virtual int Add(InterlinLineSpec spec)
		{
			var fGotMorpheme = HaveMorphemeLevel;
			for (var i = m_specs.Count - 1; i >= 0; i--)
			{
				if (this[i].Flid != spec.Flid)
				{
					continue;
				}
				// It's always OK (and optimal) to insert a new occurrence of the same
				// flid right after the last existing one.
				m_specs.Insert(i + 1, spec);
				return i + 1;
			}
			for (var i = m_specs.Count - 1; i >= 0; i--)
			{
				if (!CanFollow(this[i], spec))
				{
					continue;
				}
				var firstMorphemeIndex = FirstMorphemeIndex;
				// Even if otherwise OK, if we're inserting something morpheme level
				// and there's already morpheme-level stuff present it must follow
				// the existing morpheme-level stuff.
				if (fGotMorpheme && spec.MorphemeLevel && i >= firstMorphemeIndex &&
					(!this[i].MorphemeLevel ||
					spec.Flid == kflidMorphemes ||
					spec.Flid == kflidLexEntries && this[i].Flid != kflidMorphemes))
				{
					continue;
				}
				// And word-level annotations can't follow freeform ones.
				if (spec.WordLevel && !this[i].WordLevel)
				{
					continue;
				}
				m_specs.Insert(i + 1, spec);
				return i + 1;
			}
			m_specs.Insert(0, spec); // can't follow anything, put first.
			return 0;
		}

		/// <summary>
		/// Answer true if it is OK to change the writing system of the specified field.
		/// By default this is always OK.
		/// </summary>
		public virtual bool OkToChangeWritingSystem(int index)
		{
			return true;
		}

		/// <summary>
		/// Call before calling Remove. True and null message indicates no problem to
		/// remove this field.
		/// If it returns a true, display a message box indicating the Remove is not
		/// possible, using the returned message.
		/// If it returns false and a message, there is a warning, display it and
		/// allow the user to possibly cancel.
		/// </summary>
		public virtual bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (m_specs.Count == 1)
			{
				message = ITextStrings.ksNeedOneField;
				return false;
			}
			message = null;
			return true;
		}

		internal bool OkToRemove(int choiceIndex)
		{
			return OkToRemove(this[choiceIndex]);
		}

		internal bool OkToRemove(InterlinLineSpec spec)
		{
			string message;
			return OkToRemove(spec, out message);
		}

		/// <summary>
		/// Remove the specified field (and any dependents, with warning).
		/// If there are dependents, this will interact with the user to ask whether to
		/// go ahead.
		/// </summary>
		public virtual void Remove(InterlinLineSpec spec)
		{
			m_specs.Remove(spec);
			Debug.Assert(m_specs.Count > 0);
		}


		/// <summary>
		/// Removes the Line Choice specified by a flid and writing system, then returns if the Remove was successful
		/// </summary>
		public bool Remove(int flid, int ws)
		{
			var spec = m_specs.Find(x => x.Flid == flid && x.WritingSystem == ws);
			if (spec == null)
			{
				return false;
			}
			if (OkToRemove(spec))
			{
				m_specs.Remove(spec);
				return true;
			}
			return false;
		}

		/// These constants are defined for brevity here and convenience in testing. They use real field
		/// IDs where that is possible. The names correspond to what we see by default in the dialog.
		public const int kflidWord = WfiWordformTags.kflidForm;
		/// <summary></summary>
		public const int kflidMorphemes = WfiMorphBundleTags.kflidMorph;
		/// <summary>
		/// We get the lex entry by following the owner of the morpheme. So rather arbitrarily we
		/// use that constant to identify that field.
		/// </summary>
		public const int kflidLexEntries = (int)CmObjectFields.kflidCmObject_Owner;
		public const int kflidLexGloss = WfiMorphBundleTags.kflidSense;
		public const int kflidLexPos = WfiMorphBundleTags.kflidMsa;
		public const int kflidWordGloss = WfiGlossTags.kflidForm;
		public const int kflidWordPos = WfiAnalysisTags.kflidCategory;
		public const int kflidFreeTrans = InterlinVc.ktagSegmentFree;
		public const int kflidLitTrans = InterlinVc.ktagSegmentLit;
		public const int kflidNote = InterlinVc.ktagSegmentNote;

		private LineOption[] UpdateFieldNamesFromLines(InterlinMode mode)
		{
			var options = LineOptions(mode);
			m_fieldNames.Clear();
			foreach (var opt in options)
			{
				m_fieldNames[opt.Flid] = opt.ToString();
			}
			return options;
		}

		/// <summary>
		/// Get the standard list of lines. Also updates the member variable storing the line names.
		/// </summary>
		internal LineOption[] LineOptions()
		{
			return UpdateFieldNamesFromLines(m_mode);
		}

		private LineOption[] LineOptions(InterlinMode mode)
		{
			var customLineOptions = GetCustomLineOptions(mode);

			if (mode == InterlinMode.Chart)
			{
				return new[]
				{
					new LineOption(kflidWord, ITextStrings.ksWord),
					new LineOption(kflidWordGloss, ITextStrings.ksWordGloss),
					new LineOption(kflidMorphemes, ITextStrings.ksMorphemes),
					new LineOption(kflidLexGloss, ITextStrings.ksGloss),
					new LineOption(kflidLexEntries, ITextStrings.ksLexEntries),
					new LineOption(kflidLexPos, ITextStrings.ksGramInfo)
				}.Union(customLineOptions).ToArray();
			}

			return new[] {
				 new LineOption(kflidWord, ITextStrings.ksWord),
				 new LineOption(kflidMorphemes, ITextStrings.ksMorphemes),
				 new LineOption(kflidLexEntries, ITextStrings.ksLexEntries),
				 new LineOption(kflidLexGloss, ITextStrings.ksLexGloss),
				 new LineOption(kflidLexPos, ITextStrings.ksGramInfo),
				 new LineOption(kflidWordGloss, mode == InterlinMode.GlossAddWordsToLexicon ? ITextStrings.ksLexWordGloss : ITextStrings.ksWordGloss),
				 new LineOption(kflidWordPos, mode == InterlinMode.GlossAddWordsToLexicon ? ITextStrings.ksLexWordCat : ITextStrings.ksWordCat),
				 new LineOption(kflidFreeTrans, ITextStrings.ksFreeTranslation),
				 new LineOption(kflidLitTrans, ITextStrings.ksLiteralTranslation),
				 new LineOption(kflidNote, ITextStrings.ksNote)
			}.Union(customLineOptions).ToArray();
		}

		private List<LineOption> GetCustomLineOptions(InterlinMode mode)
		{
			var customLineOptions = new List<LineOption>();
			switch (mode)
			{
				case InterlinMode.Analyze:
				case InterlinMode.Chart:
				case InterlinMode.Gloss:
					if (m_cache != null)
					{
						var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCacheAccessor;
						customLineOptions.AddRange(mdc.GetFields(m_cache.MetaDataCacheAccessor.GetClassId("Segment"), false, (int)CellarPropertyTypeFilter.All).Where(flid => mdc.IsCustom(flid)).Select(flid => new LineOption(flid, mdc.GetFieldLabel(flid))));
					}
					break;
			}

			return customLineOptions;
		}

		/// <summary />
		/// <exception cref="KeyNotFoundException">Thrown if the key is not found in the Dictionary.</exception>
		public string LabelFor(int flid)
		{
			return m_fieldNames[flid];
		}

		internal int LabelRGBFor(int choiceIndex)
		{
			return LabelRGBFor(this[choiceIndex]);
		}

		internal int LabelRGBFor(InterlinLineSpec spec)
		{
			return (int)CmObjectUi.RGB(LabelColorFor(spec));
		}

		internal Color LabelColorFor(InterlinLineSpec spec)
		{
			if (spec.MorphemeLevel)
			{
				return kMorphLevelColor;
			}

			if (spec.WordLevel && spec.Flid != kflidWord)
			{
				return kWordLevelColor;
			}
			return SystemColors.ControlText;
		}

		// Find where the spec is in your collection.
		public int IndexOf(InterlinLineSpec spec)
		{
			return m_specs.IndexOf(spec);
		}

		/// <summary>
		/// Add the specified flid (in the appropriate default writing system).
		/// </summary>
		/// <returns>the index where the new field was inserted</returns>
		public int Add(int flid)
		{
			return Add(flid, 0);
		}

		/// <summary>
		/// Answer the index of the first item that is at the morpheme level (or -1 if none)
		/// </summary>
		public int FirstMorphemeIndex
		{
			get
			{
				for (var i = 0; i < Count; i++)
				{
					if (this[i].MorphemeLevel)
					{
						return i;
					}
				}
				return -1;
			}
		}

		/// <summary>
		/// Answer the index of the first item that is at the lex entry level (or -1 if none)
		/// </summary>
		public int FirstLexEntryIndex
		{
			get
			{
				for (var i = 0; i < Count; i++)
				{
					if (this[i].LexEntryLevel)
					{
						return i;
					}
				}
				return -1;
			}
		}

		/// <summary>
		/// Answer the index of the first item that is at the morpheme level (or Count if none).
		/// (Returning Count if none makes it easy to loop from FirstFreeformIndex to Count
		/// to get them all.)
		/// </summary>
		public int FirstFreeformIndex
		{
			get
			{
				for (var i = 0; i < Count; i++)
				{
					if (!this[i].WordLevel)
					{
						return i;
					}
				}
				return Count;
			}
		}

		/// <summary>
		/// Answer the index of the last item that is at the morpheme level (or -1 if none)
		/// </summary>
		public int LastMorphemeIndex
		{
			get
			{
				for (var i = Count - 1; i >= 0; i--)
				{
					if (this[i].MorphemeLevel)
					{
						return i;
					}
				}
				return -1;
			}
		}

		/// <summary>
		/// Answer true if the spec at index is the first one that has its flid.
		/// (This is used to decide whether to display a pull-down icon in the sandbox.)
		/// </summary>
		public bool IsFirstOccurrenceOfFlid(int index)
		{
			var flid = this[index].Flid;
			for (var i = 0; i < index; i++)
			{
				if (this[i].Flid == flid)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Answer an array list of integers, the writing systems we care about for the specified flid.
		/// Note that some of these may be magic; one is returned for each occurrence of flid.
		/// </summary>
		public List<int> WritingSystemsForFlid(int flid)
		{
			return WritingSystemsForFlid(flid, false);
		}

		/// <summary>
		/// Get the writing system for the given flid.
		/// </summary>
		public List<int> WritingSystemsForFlid(int flid, bool fGetDefaultForMissing)
		{
			var result = new List<int>();
			foreach (var spec in m_specs)
			{
				if (spec.Flid == flid && result.IndexOf(spec.WritingSystem) < 0)
				{
					result.Add(spec.WritingSystem);
				}
			}
			if (fGetDefaultForMissing && result.Count == 0)
			{
				var newSpec = CreateSpec(flid, 0);
				result.Add(newSpec.WritingSystem);
			}
			return result;
		}

		/// <summary>
		/// Answer the number of times the specified flid is displayed (typically using different writing systems).
		/// </summary>
		public int RepetitionsOfFlid(int flid)
		{
			var result = 0;
			foreach (var spec in m_specs)
			{
				if (spec.Flid == flid)
				{
					result++;
				}
			}
			return result;
		}

		/// <summary>
		/// A list of integers representing the writing systems we care about for the view.
		/// </summary>
		public List<int> WritingSystems
		{
			get
			{
				var result = new List<int>();
				foreach (var spec in m_specs)
				{
					if (result.IndexOf(spec.WritingSystem) < 0)
					{
						result.Add(spec.WritingSystem);
					}
				}
				return result;
			}
		}

		/// <summary>
		/// Answer an array of the writing systems to display for the field at index,
		/// and any subsequent fields with the same flid.
		/// </summary>
		public int[] AdjacentWssAtIndex(int index, int hvo)
		{
			var first = index;
			var lim = index + 1;
			while (lim < Count && this[lim].Flid == this[first].Flid)
			{
				lim++;
			}
			var result = new int[lim - first];
			for (var i = first; i < lim; i++)
			{
				var wsId = this[i].WritingSystem;
				if (wsId < 0) // if this is a magic writing system
				{
					wsId = WritingSystemServices.ActualWs(m_cache, wsId, hvo, this[i].Flid);
				}
				result[i - first] = wsId;
			}
			return result;
		}
		/// <summary>
		/// Answer an array list of integers, the writing systems we care about for the specified flid,
		/// except the one specified.
		/// </summary>
		public List<int> OtherWritingSystemsForFlid(int flid, int wsToOmit)
		{
			if (wsToOmit == 0)
			{
				// eliminate the default writing system for this flid.
				var specDefault = CreateSpec(flid, 0);
				wsToOmit = specDefault.WritingSystem;
			}
			var result = new List<int>();
			foreach (var spec in m_specs)
			{
				if (spec.Flid == flid && result.IndexOf(spec.WritingSystem) < 0 && spec.WritingSystem != wsToOmit)
				{
					result.Add(spec.WritingSystem);
				}
			}
			return result;
		}

		public bool IsDefaultSpec(InterlinLineSpec spec)
		{
			return spec.SameSpec(CreateSpec(spec.Flid, 0));
		}

		/// <summary>
		/// Add the specified flid (in the appropriate default writing system).
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="wsRequested">If zero, supply the default ws for the field; otherwise
		/// use the one supplied.</param>
		/// <returns>the integer where inserted</returns>
		public int Add(int flid, int wsRequested)
		{
			return Add(CreateSpec(flid, wsRequested));
		}

		/// <summary>
		/// returns the main spec for this flid, searching for the first one that cant be removed.
		/// then searches for the first default spec.
		/// then simply the first spec.
		/// </summary>
		public InterlinLineSpec GetPrimarySpec(int specFlid)
		{
			var matchingSpecs = ItemsWithFlids(new[] { specFlid });
			// should we consider creating a default spec instead?
			if (matchingSpecs.Count == 0)
			{
				return null;
			}
			// search for the first matching spec that we can't remove.
			foreach (var spec in matchingSpecs)
			{
				if (!OkToRemove(spec))
				{
					return spec;
				}
			}

			// search for the first spec that is a default spec.
			foreach (var spec in matchingSpecs)
			{
				if (IsDefaultSpec(spec))
				{
					return spec;
				}
			}

			// lastly return the first matchingSpec
			return matchingSpecs[0];
		}

		/// <summary />
		internal InterlinLineSpec CreateSpec(int flid, int wsRequested)
		{
			int ws;
			var fMorphemeLevel = false;
			var fWordLevel = true;
			var flidString = 0;
			var comboContent = WsComboContent.kwccAnalysis; // The usual choice
			switch (flid)
			{
				case kflidWord:
					comboContent = ColumnConfigureDialog.ChooseComboContent(m_cache, m_wsDefVern, "vernacular");
					ws = m_wsDefVern;
					break; // vern, not interlin, word
				case kflidLexEntries:
				case kflidMorphemes:
					fMorphemeLevel = true;
					comboContent = ColumnConfigureDialog.ChooseComboContent(m_cache, m_wsDefVern, "vernacular");
					flidString = MoFormTags.kflidForm;
					ws = m_wsDefVern;
					break; // vern, morpheme
				case kflidLexGloss:
					fMorphemeLevel = true;
					ws = WritingSystemServices.kwsFirstAnal;
					flidString = LexSenseTags.kflidGloss;
					comboContent = WsComboContent.kwccBestAnalysis;
					break; // analysis, morpheme
				case kflidLexPos:
					fMorphemeLevel = true;
					// getting to the string takes a couple of levels
					// so just do it when we have the actual hvos.
					flidString = -1;
					ws = WritingSystemServices.kwsFirstAnal;
					comboContent = WsComboContent.kwccBestAnalysis;
					break; // analysis, morpheme
				case kflidWordGloss:
					ws = m_wsDefAnal;
					break; // not morpheme-level
				case kflidWordPos:
					ws = WritingSystemServices.kwsFirstAnal;
					flidString = CmPossibilityTags.kflidAbbreviation;
					comboContent = WsComboContent.kwccBestAnalysis;
					break; // not morpheme-level
				case kflidFreeTrans:
				case kflidLitTrans:
					ws = m_wsDefAnal;
					fWordLevel = false;
					break;
				case kflidNote:
					comboContent = WsComboContent.kwccVernAndAnal;
					ws = m_wsDefAnal;
					fWordLevel = false;
					break;
				default:
					var mdc = m_cache.GetManagedMetaDataCache();
					if (!mdc.IsCustom(flid))
					{
						throw new Exception(@"Adding unknown field to interlinear");
					}
					ws = mdc.GetFieldWs(flid);
					fWordLevel = false;
					comboContent = WsComboContent.kwccAnalAndVern;
					break;
			}

			return new InterlinLineSpec
			{
				ComboContent = comboContent,
				Flid = flid,
				WritingSystem = wsRequested == 0 ? ws : wsRequested,
				MorphemeLevel = fMorphemeLevel,
				WordLevel = fWordLevel,
				StringFlid = flidString
			};
		}

		public InterlinLineSpec this[int index] => m_specs[index];

		public IEnumerator GetEnumerator()
		{
			return m_specs.GetEnumerator();
		}

		/// <summary>
		/// Return the index of the (first) spec with the specified flid and ws, if any.
		/// First tries to match the ws exactly, and then will see if we can find it in
		/// collections referred to by a magic value.
		/// </summary>
		/// <returns>-1 if not found.</returns>
		internal int IndexOf(int flid, int ws)
		{
			var index = -1;
			if (ws > 0)
			{
				// first try to find an exact match.
				index = IndexOf(flid, ws, true);
			}

			if (index == -1)
			{
				index = IndexOf(flid, ws, false);
			}
			return index;
		}

		/// <summary />
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		/// <param name="fExact">if true, see if we can find a line choice that matches the exact writing system given.
		/// if false, we'll try to see if a line choice refers to a collection (via magic value) that contains the ws.</param>
		/// <returns></returns>
		internal int IndexOf(int flid, int ws, bool fExact)
		{
			for (var i = 0; i < m_specs.Count; i++)
			{
				if (this[i].Flid == flid && MatchingWritingSystem(this[i].WritingSystem, ws, fExact))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary />
		private bool MatchingWritingSystem(int wsConfig, int ws, bool fExact)
		{
			if (wsConfig == ws)
			{
				return true;
			}

			if (fExact)
			{
				return false;
			}

			if (m_proj == null)
			{
				return false;
			}
			var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			switch (wsConfig)
			{
				case WritingSystemServices.kwsAnal:
					return ws == m_cache.DefaultAnalWs;

				case WritingSystemServices.kwsPronunciation:
					return ws == m_cache.DefaultPronunciationWs;

				case WritingSystemServices.kwsVern:
					return ws == m_cache.DefaultVernWs;

				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsFirstAnal:
					return m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsAnalVerns:
				case WritingSystemServices.kwsFirstAnalOrVern:
				case WritingSystemServices.kwsVernAnals:
				case WritingSystemServices.kwsFirstVernOrAnal:
					return m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj) || m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsFirstVern:
				case WritingSystemServices.kwsVernInParagraph:
				case WritingSystemServices.kwsVerns:
					return m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsFirstPronunciation:
				case WritingSystemServices.kwsPronunciations:
				case WritingSystemServices.kwsReversalIndex:
				default:
					return false;
			}
		}

		/// <summary>
		/// Return the index of the (first) spec with the specified flid, or -1 if not found
		/// </summary>
		public int IndexOf(int flid)
		{
			for (var i = 0; i < m_specs.Count; i++)
			{
				if (this[i].Flid == flid)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Return a collection containing any line which has any of the specified flids.
		/// </summary>
		internal List<InterlinLineSpec> ItemsWithFlids(int[] flids)
		{
			return ItemsWithFlids(flids, null);
		}

		internal List<InterlinLineSpec> ItemsWithFlids(int[] flids, int[] wsList)
		{
			Debug.Assert(wsList == null || wsList.Length == flids.Length, "wsList should be empty or match the same item count in flids.");
			var result = new List<InterlinLineSpec>();
			for (var i = 0; i < m_specs.Count; i++)
			{
				result.AddRange(flids.Where((flid, j) => this[i].Flid == flid && (wsList == null || this[i].WritingSystem == wsList[j])).Select(t => this[i]));
			}
			return result;
		}

		/// <summary>
		/// Answer where line n should move up to (if it can move up).
		/// If it can't answer -1.
		/// </summary>
		internal int WhereToMoveUpTo(int n)
		{
			if (n == 0)
			{
				return -1; // first line can't move up!
			}
			var spec = this[n];
			// Can't move up at all if it's a freeform and the previous line is not.
			if (!spec.WordLevel && this[n - 1].WordLevel)
			{
				return -1;
			}
			var newPos = n - 1; // default place to put it.
			if (!spec.MorphemeLevel && this[newPos].MorphemeLevel)
			{
				for (; newPos > 0 && this[newPos - 1].MorphemeLevel; newPos--)
				{/* Move past them to update 'newPos'. */}
			}

			if (spec.Flid != kflidNote && this[newPos].Flid == kflidNote)
			{
				for (; newPos > 0 && this[newPos - 1].Flid == kflidNote; newPos--)
				{/* Move past them to update 'newPos'. */}
			}
			// If it can't go here it just can't move.
			if (newPos > 0 && !CanFollow(this[newPos - 1], spec))
			{
				return -1;
			}

			if (!CanFollow(spec, this[newPos]))
			{
				return -1;
			}
			return newPos;
		}

		/// <summary>
		/// Answer true if the item at line n can be moved up a line.
		/// </summary>
		public virtual bool OkToMoveUp(int n)
		{
			return WhereToMoveUpTo(n) >= 0;
		}

		/// <summary>
		/// Moves a choice line up to the position n.
		/// Morpheme lines, word lines and
		/// </summary>
		public void MoveUp(int n)
		{
			var dest = WhereToMoveUpTo(n);
			if (dest < 0)
			{
				return;
			}
			var spec = this[n];
			// If this was the first morpheme field, move the others too.
			var fMoveMorphemeGroup = spec.MorphemeLevel && !this[n - 1].MorphemeLevel;
			var fMoveNoteGroup = spec.Flid == kflidNote && this[n - 1].Flid != kflidNote;
			m_specs.RemoveAt(n);
			m_specs.Insert(dest, spec);
			if (!fMoveMorphemeGroup && !fMoveNoteGroup)
			{
				return;
			}
			for (var i = n + 1; i < Count && ((fMoveMorphemeGroup && this[i].MorphemeLevel) || (fMoveNoteGroup && this[i].Flid == kflidNote)); i++)
			{
				var specT = this[i];
				m_specs.RemoveAt(i);
				m_specs.Insert(dest + i - n, specT);
			}
		}

		/// <summary>
		/// Answer true if the item at line n can be moved down a line.
		/// Currently this is true if the following line can be moved up.
		/// </summary>
		public virtual bool OkToMoveDown(int n)
		{
			return n <= Count - 2 && OkToMoveUp(n + 1);
		}

		/// <summary>
		/// Currently, moving line n down is the same as moving line n+1 up.
		/// </summary>
		public void MoveDown(int n)
		{
			if (n > Count - 2)
			{
				return;
			}
			MoveUp(n + 1);
		}

		#region ICloneable Members

		public object Clone()
		{
			var result = MemberwiseClone() as InterlinLineChoices;
			// We need a deep clone of the specs, because not only may we reorder the
			// list and add items, but we may alter items, e.g., by setting the WS.
			result.m_specs = new List<InterlinLineSpec>(m_specs.Count);
			foreach (var spec in m_specs)
			{
				result.m_specs.Add(spec.Clone() as InterlinLineSpec);
			}
			return result;
		}
		#endregion
	}
}
