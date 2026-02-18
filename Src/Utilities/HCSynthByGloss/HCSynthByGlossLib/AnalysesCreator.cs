// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSynthByGloss
{
	public class AnalysesCreator
	{
		private static readonly AnalysesCreator instance = new AnalysesCreator();
		public string category { get; set; } = "";
		const char closingWedge = '>';
		const char openingWedge = '<';
		public List<string> Forms { get; } = new List<string>();
		public int RootIndex { get; set; } = -1;

		public static AnalysesCreator Instance
		{
			get { return instance; }
		}

		public List<Morpheme> ExtractMorphemes(string analysis, Morpher srcMorpher)
		{
			List<Morpheme> morphemes = new List<Morpheme>();
			instance.Forms.Clear();
			var state = State.BEGIN;
			int index = 0;
			int morphIndex = 0;
			RootIndex = -1;
			while (index < analysis.Length)
			{
				switch (state)
				{
					case State.BEGIN:
						index++;
						if (analysis[index] == openingWedge)
							state = State.PREFIX;
						else
							state = State.ROOT;
						break;
					case State.PREFIX:
						morphIndex++;
						index = AddMorph(analysis, srcMorpher, morphemes, ++index, closingWedge);
						if (analysis[index] == openingWedge)
							state = State.PREFIX;
						else
							state = State.ROOT;
						break;
					case State.ROOT:
						if (RootIndex == -1)
						{
							RootIndex = morphIndex;
							// we need the prefixes to be synthesized in the reverse order
							morphemes.Reverse();
						}
						morphIndex++;
						index = AddMorph(analysis, srcMorpher, morphemes, index, openingWedge);
						state = State.CATEGORY;
						break;
					case State.CATEGORY:
						int indexEnd = analysis.Substring(index).IndexOf(closingWedge) + index;
						category = analysis.Substring(index, indexEnd - index);
						index = indexEnd + 1;
						if (analysis[index] == openingWedge)
							state = State.SUFFIX;
						else
							state = State.END;
						break;
					case State.SUFFIX:
						morphIndex++;
						index = AddMorph(analysis, srcMorpher, morphemes, ++index, closingWedge);
						if (analysis[index] == openingWedge)
							state = State.SUFFIX;
						else
							state = State.END;
						break;
					case State.END:
						index = analysis.Length;
						// we need the suffixes to be synthesized in the reverse order
						if (morphemes.Count > RootIndex + 1)
						{
							morphemes.Reverse(RootIndex + 1, morphemes.Count - (RootIndex + 1));
						}
						break;
				}
			}
			return morphemes;
		}

		private static int AddMorph(
			string analysis,
			Morpher srcMorpher,
			List<Morpheme> morphemes,
			int index,
			char endMarker
		)
		{
			int indexEnd = analysis.Substring(index).IndexOf(endMarker) + index;
			string shape = analysis.Substring(index, indexEnd - index);
			// Note: for testing we ignore the morpher.
			if (srcMorpher != null)
			{
				IMorpheme morph = srcMorpher.Morphemes.FirstOrDefault(
					m =>
						m.Gloss != null
						&& m.Gloss.Normalize(NormalizationForm.FormD)
							== shape.Normalize(NormalizationForm.FormD)
				);
				morphemes.Add(morph as Morpheme);
			}
			else
			{
				morphemes.Add(null);
			}
			instance.Forms.Add(shape);
			index = indexEnd + 1;
			return index;
		}
	}

	enum State
	{
		BEGIN,
		PREFIX,
		ROOT,
		CATEGORY,
		SUFFIX,
		END
	}
}
