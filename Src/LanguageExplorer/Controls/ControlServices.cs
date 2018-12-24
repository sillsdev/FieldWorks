// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Random static methods for the LanguageExplorer.Controls namespace.
	/// If at some later time a better organizing principle can be found,
	/// then this class can be removed and the methods moved elsewhere.
	/// </summary>
	internal static class ControlServices
	{
		/// <summary>
		/// Traverse a tree of PartOfSpeech objects.
		///	Put the appropriate descendant identifiers into collector.
		/// </summary>
		/// <param name="cache">data access to retrieve info</param>
		/// <param name="itemFlid">want children where this is non-empty in the collector</param>
		/// <param name="flidName">multi unicode prop to get name of item from</param>
		/// <param name="wsName">multi unicode writing system to get name of item from</param>
		/// <param name="collector">Add for each item an HvoTreeNode with the name and id of the item.</param>
		internal static void GatherPartsOfSpeech(LcmCache cache, int itemFlid, int flidName, int wsName, List<HvoTreeNode> collector)
		{
			var mainPartsOfSpeechList = cache.LanguageProject.PartsOfSpeechOA;
			var allPartsOfSpeech = mainPartsOfSpeechList.AllPossibilities();
			foreach (var possibility in allPartsOfSpeech)
			{
				var pos = possibility as IPartOfSpeech;
				if (pos == null)
				{
					// Can't ever happen.
					continue;
				}
				var canMakeNode = false;
				switch (itemFlid)
				{
					case PartOfSpeechTags.kflidInflectionClasses:
						canMakeNode = pos.InflectionClassesOC.Any();
						break;
					case PartOfSpeechTags.kflidInflectableFeats:
						canMakeNode = pos.InflectableFeatsRC.Any();
						break;
				}
				if (canMakeNode)
				{
					IMultiUnicode multiUnicode = null;
					switch (flidName)
					{
						case CmPossibilityTags.kflidName:
							multiUnicode = pos.Name;
							break;
						case CmPossibilityTags.kflidAbbreviation:
							multiUnicode = pos.Abbreviation;
							break;
					}
					if (multiUnicode != null)
					{
						int wsDummy;
						collector.Add(new HvoTreeNode(multiUnicode.GetAlternativeOrBestTss(wsName, out wsDummy), pos.Hvo));
					}
				}
			}
		}
	}
}
