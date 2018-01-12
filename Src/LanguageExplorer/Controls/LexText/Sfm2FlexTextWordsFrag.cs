// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This converts Sfm to a subset of the FlexText xml standard that only deals with Words, their Glosses and their Morphology.
	/// This frag is special case (non-conforming) in that it can have multiple glosses in the same writing system.
	/// </summary>
	public class Sfm2FlexTextWordsFrag : Sfm2FlexTextBase<InterlinearMapping>
	{
		HashSet<Tuple<InterlinDestination, string>> m_txtItemsAddedToWord = new HashSet<Tuple<InterlinDestination, string>>();

		public Sfm2FlexTextWordsFrag()
			: base(new List<string>(new[] { "document", "word" }))
		{}
		protected override void WriteToDocElement(byte[] data, InterlinearMapping mapping)
		{

			switch (mapping.Destination)
			{
				// Todo: many cases need more checks for correct state.
				default: // Ignored
					break;
				case InterlinDestination.Wordform:
					var key = new Tuple<InterlinDestination, string>(mapping.Destination, mapping.WritingSystem);
					// don't add more than one "txt" to word parent element
					if (m_txtItemsAddedToWord.Contains(key) && ParentElementIsOpen("word"))
					{
						WriteEndElement();
						m_txtItemsAddedToWord.Clear();
					}
					MakeItem(mapping, data, "txt", "word");
					m_txtItemsAddedToWord.Add(key);
					break;
				case InterlinDestination.WordGloss:
					// (For AdaptIt Knowledge Base sfm) it is okay to add more than one "gls" with same writing system to word parent element
					// this is a special case and probably doesn't strictly conform to FlexText standard.
					MakeItem(mapping, data, "gls", "word");
					break;
			}
		}
	}
}