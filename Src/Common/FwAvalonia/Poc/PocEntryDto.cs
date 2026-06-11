// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// A single writing-system alternative for a multi-string field. Carries the per-writing-system
	/// font settings the Avalonia editor must honor (the fidelity question this spike answers).
	/// Detached from LCModel so the POC can run in the Preview Host and headless tests.
	/// </summary>
	public sealed class WsAlternative
	{
		public WsAlternative(string wsAbbrev, string value, string fontFamily = "Charis SIL", double fontSize = 12d)
		{
			WsAbbrev = wsAbbrev;
			Value = value;
			FontFamily = fontFamily;
			FontSize = fontSize;
		}

		/// <summary>Short writing-system label shown in the dense per-alternative row (e.g. "en", "seh").</summary>
		public string WsAbbrev { get; }

		/// <summary>Configured font family for this writing system.</summary>
		public string FontFamily { get; }

		/// <summary>Configured font size for this writing system.</summary>
		public double FontSize { get; }

		/// <summary>The editable text value.</summary>
		public string Value { get; set; }
	}

	/// <summary>A selectable morph type option for the chooser popup.</summary>
	public sealed class MorphTypeOption
	{
		public MorphTypeOption(string key, string name)
		{
			Key = key;
			Name = name;
		}

		public string Key { get; }

		public string Name { get; }

		public override string ToString() => Name;
	}

	/// <summary>
	/// Minimal representative slice of a lexical entry: a multi-writing-system lexeme form,
	/// a morph type selection, and one multi-writing-system sense gloss. These three cover the
	/// dominant Lexical Edit interaction classes (dense WS text + chooser flyout) for the spike.
	/// </summary>
	public sealed class PocEntryDto
	{
		public PocEntryDto(
			IList<WsAlternative> lexemeForm,
			IList<MorphTypeOption> morphTypeOptions,
			string morphTypeKey,
			IList<WsAlternative> senseGloss)
		{
			LexemeForm = lexemeForm;
			MorphTypeOptions = morphTypeOptions;
			MorphTypeKey = morphTypeKey;
			SenseGloss = senseGloss;
		}

		public IList<WsAlternative> LexemeForm { get; }

		public IList<MorphTypeOption> MorphTypeOptions { get; }

		public string MorphTypeKey { get; set; }

		public IList<WsAlternative> SenseGloss { get; }

		/// <summary>The currently selected morph type option, or null.</summary>
		public MorphTypeOption SelectedMorphType
		{
			get
			{
				foreach (var option in MorphTypeOptions)
				{
					if (option.Key == MorphTypeKey)
					{
						return option;
					}
				}

				return null;
			}
		}

		/// <summary>Builds a representative sample entry for preview/headless scenarios.</summary>
		public static PocEntryDto CreateSample()
		{
			var lexemeForm = new List<WsAlternative>
			{
				new WsAlternative("seh", "kazi"),
				new WsAlternative("en", "work", "Times New Roman")
			};

			var morphTypes = new List<MorphTypeOption>
			{
				new MorphTypeOption("stem", "stem"),
				new MorphTypeOption("root", "root"),
				new MorphTypeOption("prefix", "prefix"),
				new MorphTypeOption("suffix", "suffix")
			};

			var gloss = new List<WsAlternative>
			{
				new WsAlternative("en", "to work", "Times New Roman"),
				new WsAlternative("pt", "trabalhar", "Times New Roman")
			};

			return new PocEntryDto(lexemeForm, morphTypes, "stem", gloss);
		}
	}
}
