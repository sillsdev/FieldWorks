// Copyright (c) 2014-$year$ SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.XWorks
{
	public class WordStyleCollection
	{
		// The dictionary Key is the displayNameBase without the added int that uniquely identifies the different Styles.
		// Examples of Key:
		//     Definition (or Gloss)[lang='en']
		//     Homograph-Number:Referenced Sense Headword[lang='fr']
		// The dictionary value is the list of StyleElements (ie. styles) that share the same displayNameBase. The
		// style.StyleId values will be the unique display names that are based on the displayNameBase.
		// Example:
		//	Key:					Definition (or Gloss)[lang='en']
		//	style.StyleId values:	Definition (or Gloss)[lang='en']	(the first style does not have a '1' added to the unique name)
		//							Definition (or Gloss)2[lang='en']
		//							Definition (or Gloss)3[lang='en']
		//
		private Dictionary<string,List<StyleElement>> styleDictionary = new Dictionary<string, List<StyleElement>>();

		/// <summary>
		/// Returns a single list containing all of the Styles.
		/// </summary>
		public List<Style> GetStyles()
		{
			lock(styleDictionary)
			{
				// Get an enumerator to the flattened list of all StyleElements.
				var enumerator = styleDictionary.Values.SelectMany(x => x);
				// Create a single list of all the Styles.
				return enumerator.Select(x => x.Style).ToList();
			}
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear()
		{
			lock(styleDictionary)
			{
				styleDictionary.Clear();
			}
		}

		/// <summary>
		/// Check if a style is already in the collection.
		/// NOTE: To support multiple threads this method must be called in the same lock that also
		/// acts on the result (ie. calling AddStyle()).
		/// </summary>
		/// <param name="nodeStyleName">The unique FLEX style name, typically comes from node.Style.</param>
		/// <param name="displayNameBase">The key value in the styleDictionary.</param>
		/// <param name="style">Returns the found Style, or returns null if not found.</param>
		/// <returns>True if found, else false.</returns>
		public bool TryGetStyle(string nodeStyleName, string displayNameBase, out Style style)
		{
			lock (styleDictionary)
			{
				if (styleDictionary.TryGetValue(displayNameBase, out List<StyleElement> stylesWithSameDisplayNameBase))
				{
					foreach (var elem in stylesWithSameDisplayNameBase)
					{
						// If the style already exists, return it's name.
						if (elem.NodeStyleName == nodeStyleName)
						{
							style = elem.Style;
							return true;
						}
					}
				}
			}

			style = null;
			return false;
		}

		/// <summary>
		/// Check if a style already exists in any of the Lists in the entire collection.
		/// NOTE: To support multiple threads this method must be called in the same lock that also
		/// acts on the result (ie. calling AddStyle()).
		/// </summary>
		/// <param name="nodeStyleName">The unique FLEX style name, typically comes from node.Style.</param>
		/// <param name="style">Returns the found Style, or returns null if not found.</param>
		/// <returns>True if found, else false.</returns>
		public bool TryGetStyle(string nodeStyleName, out Style style)
		{
			lock (styleDictionary)
			{
				foreach (var keyValuePair in styleDictionary)
				{
					foreach (var elem in keyValuePair.Value)
					{
						// If the style already exists, return it's name.
						if (elem.NodeStyleName == nodeStyleName)
						{
							style = elem.Style;
							return true;
						}
					}
				}
			}

			style = null;
			return false;
		}

		/// <summary>
		/// Adds a style to the collection.
		/// If a style with the identical style information is already in the collection then just return
		/// the unique name.
		/// If the identical style is not already in the collection then generate a unique name,
		/// update the style name values (with the unique name), and return the unique name.
		/// </summary>
		/// <param name="style">The style to add to the collection. (It's name might get modified.)</param>
		/// <param name="nodeStyleName">The unique FLEX style name, typically comes from node.Style.</param>
		/// <param name="displayNameBase">The base name that will be used to create the unique display name
		/// for the style.  The root of this name typically comes from the node.DisplayLabel but it can have
		/// additional information if it is based on other styles and/or has a writing system.</param>
		/// <returns>The unique display name. The name that should be referenced in a Run.</returns>
		public string AddStyle(Style style, string nodeStyleName, string displayNameBase)
		{
			lock (styleDictionary)
			{
				if (styleDictionary.TryGetValue(displayNameBase, out List<StyleElement> stylesWithSameDisplayNameBase))
				{
					if (TryGetStyle(nodeStyleName, displayNameBase, out Style existingStyle))
					{
						return existingStyle.StyleId;
					}
				}
				// Else this is the first style with this root. Add it to the Dictionary.
				else
				{
					stylesWithSameDisplayNameBase = new List<StyleElement>();
					styleDictionary.Add(displayNameBase, stylesWithSameDisplayNameBase);
				}

				// Get a unique display name.
				string uniqueDisplayName = displayNameBase;
				// Append a number to all except the first.
				int styleCount = stylesWithSameDisplayNameBase.Count;
				if (styleCount > 0)
				{
					int separatorIndex = uniqueDisplayName.IndexOf(WordStylesGenerator.StyleSeparator);
					separatorIndex = separatorIndex != -1 ? separatorIndex : uniqueDisplayName.IndexOf("[");
					// Append the number before the basedOn information.
					// Note: We do not want to append the number to the end of the uniqueDisplayName if
					// there is basedOn information because that could result in the name not being
					// unique. (ex. The '2' in the unique name, "name : basedOn2" could then apply to the
					// complete name, "name : basedOn" or just the basedOn name, "basedOn2".
					if (separatorIndex != -1)
					{
						uniqueDisplayName = uniqueDisplayName.Substring(0, separatorIndex) +
											(styleCount + 1).ToString() +
											uniqueDisplayName.Substring(separatorIndex);
					}
					// No basedOn information, append the number to the end.
					else
					{
						uniqueDisplayName += (styleCount + 1).ToString();
					}
				}

				// Update the style name.
				style.StyleId = uniqueDisplayName;
				if (style.StyleName == null)
				{
					style.StyleName = new StyleName() { Val = style.StyleId };
				}
				else
				{
					style.StyleName.Val = style.StyleId;
				}

				// Add the style element to the collection.
				var styleElement = new StyleElement(nodeStyleName, style);
				stylesWithSameDisplayNameBase.Add(styleElement);

				return uniqueDisplayName;
			}
		}
	}

	// WordStyleCollection dictionary values.
	internal class StyleElement
	{
		/// <param name="nodeStyleName">The unmodified FLEX style name. Typically comes from node.Style. Can be null.</param>
		/// <param name="style">The style with it's styleId set to the uniqueDisplayName.
		///     Examples of uniqueDisplayName:
		///         Definition (or Gloss)[lang='en']
		///			Definition (or Gloss)2[lang='en']
		///         Grammatical Info.2 : Category Info.[lang='en']
		///         Subentries : Grammatical Info.2 : Category Info.[lang='en']
		/// </param>
		internal StyleElement(string nodeStyleName, Style style)
		{
			this.NodeStyleName = nodeStyleName;
			this.Style = style;
		}
		internal string NodeStyleName { get; }
		internal Style Style { get; }
	}
}
