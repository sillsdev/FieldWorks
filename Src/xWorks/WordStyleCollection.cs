// Copyright (c) 2014-$year$ SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using DocumentFormat.OpenXml.Wordprocessing;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using System.Collections.Generic;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class WordStyleCollection
	{
		private ReadOnlyPropertyTable _propertyTable;
		private object _collectionLock;

		// The Key is the displayNameBase without the added int or wsId that uniquely identifies the different Styles.
		// The dictionary Value is the list of elements that share the same displayNameBase.
		private Dictionary<string, List<CharacterElement>> characterStyles = new Dictionary<string, List<CharacterElement>>();
		private Dictionary<string, List<ParagraphElement>> paragraphStyles = new Dictionary<string, List<ParagraphElement>>();

		// The Key is a path containing Non-Unique names for all the nodes from the root up to and
		// including the 'node' of interest.
		private Dictionary<string, CharacterElement> uniquePathToCharElement = new Dictionary<string, CharacterElement>();
		private Dictionary<string, ParagraphElement> uniquePathToParaElement = new Dictionary<string, ParagraphElement>();

		// The Key is the unique display name for the element (ie. element.Style.StyleId)
		private Dictionary<string, CharacterElement> uniqueNameToCharElement = new Dictionary<string, CharacterElement>();

		private int bulletAndNumberingUniqueIdCounter = 1;
		private int pictureUniqueIdCounter = 1;

		public WordStyleCollection(ReadOnlyPropertyTable propertyTable, object collectionLock)
		{
			_propertyTable = propertyTable;
			_collectionLock = collectionLock;
		}

		/// <summary>
		/// Returns a single list containing all the used CharacterElements.
		/// </summary>
		internal List<CharacterElement> GetUsedCharacterElements()
		{
			// Get an enumerator to the flattened list of all StyleElements.
			var enumerator = characterStyles.Values.SelectMany(x => x);
			// Create a single list of all the StyleElements.
			return enumerator.Where(x => x.Used).ToList();
		}

		/// <summary>
		/// Returns a single list containing all the ParagraphElements.
		/// </summary>
		internal List<ParagraphElement> GetParagraphElements()
		{
			// Get an enumerator to the flattened list of all StyleElements.
			var enumerator = paragraphStyles.Values.SelectMany(x => x);
			// Create a single list of all the StyleElements.
			return enumerator.ToList();
		}

		/// <summary>
		/// Finds a CharacterElement from the uniqueDisplayName.
		/// </summary>
		/// <param name="uniqueDisplayName">The style name that uniquely identifies a style.</param>
		internal CharacterElement GetCharacterElement(string uniqueDisplayName)
		{
			return uniqueNameToCharElement[uniqueDisplayName];
		}

		/// <summary>
		/// Finds a ParagraphElement from the uniqueDisplayName.
		/// </summary>
		/// <param name="uniqueDisplayName">The style name that uniquely identifies a style.</param>
		internal ParagraphElement GetParagraphElement(string uniqueDisplayName)
		{
			return paragraphStyles.Values.SelectMany(x => x)
				.FirstOrDefault(element => element.UniqueDisplayName() == uniqueDisplayName);
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear()
		{
			lock(_collectionLock)
			{
				characterStyles.Clear();
				paragraphStyles.Clear();
				uniquePathToCharElement.Clear();
				uniquePathToParaElement.Clear();
				uniqueNameToCharElement.Clear();
				bulletAndNumberingUniqueIdCounter = 1;
				pictureUniqueIdCounter = 1;
			}
		}

		/// <summary>
		/// Check if a style is already in the collection.
		/// NOTE: To support multiple threads this method must be called in the same lock that also
		/// acts on the result (ie. calling AddStyles()).
		/// </summary>
		/// <param name="elem">Returns the found character style element, or returns null if not found.</param>
		/// <returns>True if found, else false.</returns>
		internal bool TryGetCharacterStyle(string nodePath, int wsId, out CharacterElement elem)
		{
			return uniquePathToCharElement.TryGetValue(CharacterElement.GetUniquePath(nodePath, wsId), out elem);
		}

		/// <summary>
		/// Check if a style is already in the collection.
		/// NOTE: To support multiple threads this method must be called in the same lock that also
		/// acts on the result (ie. calling AddParagraphStyle()).
		/// </summary>
		/// <param name="nodePath">Node path to the paragraph style.</param>
		/// <param name="paraElem">Returns the found paragraph element, or returns null if not found.</param>
		/// <returns>True if found, else false.</returns>
		///
		internal bool TryGetParagraphStyle(string nodePath, out ParagraphElement paraElem)
		{
			return uniquePathToParaElement.TryGetValue(nodePath, out paraElem);
		}

		/// <summary>
		/// Worker method that can result in many styles getting created (both
		/// character and paragraph styles).
		/// </summary>
		/// <returns>The uniqueDisplayName for the character style that is created.</returns>
		private string AddStyle(string styleName, string displayNameBase, string nodePath,
			string parentUniqueDisplayName, int wsId, bool pictureStyle, string sensesSubentriesUniqueDisplayName = null)
		{
			// Use a special name for a 'Headword' that is a '.subentries .headword' so that the
			// '.subentries .headword' is not used as a guideword.
			if (displayNameBase == WordStylesGenerator.HeadwordDisplayName &&
				nodePath.Contains(WordStylesGenerator.SubentriesClassName) &&
				nodePath.Contains(WordStylesGenerator.HeadwordClassName))
			{
				displayNameBase = WordStylesGenerator.SubentriesHeadword;
			}

			lock(_collectionLock)
			{
				if (TryGetCharacterStyle(nodePath, wsId, out CharacterElement existingStyle))
				{
					return existingStyle.UniqueDisplayName();
				}

				// If this is the first style with this root then add it to the Dictionary.
				if (!characterStyles.TryGetValue(displayNameBase, out List<CharacterElement> stylesWithSameDisplayNameBase))
				{
					stylesWithSameDisplayNameBase = new List<CharacterElement>();
					characterStyles.Add(displayNameBase, stylesWithSameDisplayNameBase);
				}

				bool processParagraphStyle = ((nodePath != WordStylesGenerator.RootCharacterNodePath) &&
											  (wsId == WordStylesGenerator.DefaultStyle) &&
											  WordStylesGenerator.IsParagraphStyle(styleName, _propertyTable));

				// Get a unique display name.
				var cache = _propertyTable.GetValue<LcmCache>("cache");
				var wsString = cache.WritingSystemFactory.GetStrFromWs(wsId);
				bool retry;
				string uniqueDisplayName;
				// Append a number to all except the first.
				int uniqueNumber = stylesWithSameDisplayNameBase.Where(x => x.WritingSystemId == wsId).Count() +1;
				do
				{
					retry = false;
					uniqueDisplayName = displayNameBase;
					if (processParagraphStyle)
					{
						uniqueDisplayName += WordStylesGenerator.LinkedCharacterStyle;
					}
					if (uniqueNumber != 1)
					{
						uniqueDisplayName += uniqueNumber.ToString();
					}
					if (wsId != WordStylesGenerator.DefaultStyle)
					{
						uniqueDisplayName += WordStylesGenerator.GetWsString(wsString);
					}

					// There could be multiple custom fields with names that only differ by an appended number.  Make sure our
					// final unique display name is truly unique.
					if (uniqueNameToCharElement.ContainsKey(uniqueDisplayName))
					{
						uniqueNumber++;
						retry = true;
					}
				} while (retry);

				// Get the parent properties so we can include them in the style.
				StyleRunProperties parentRunProps = null;
				if(!string.IsNullOrEmpty(parentUniqueDisplayName))
				{
					CharacterElement parentElem = GetCharacterElement(parentUniqueDisplayName);
					var parentStyle = parentElem.Style;
					parentRunProps = parentStyle.GetFirstChild<StyleRunProperties>();
				}

				Style style = null;
				if (!string.IsNullOrEmpty(styleName))
				{
					// Get the style
					style = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(styleName, wsId, _propertyTable, parentRunProps);
				}
				if (style == null)
				{
					style = new Style();
					style.Type = StyleValues.Character;
					style.Append(parentRunProps.CloneNode(true));
				}

				// Update the style name.
				WordStylesGenerator.SetStyleName(style, uniqueDisplayName);

				// Update the BasedOn value.
				CharacterElement basedOnElem = null;
				if (nodePath != WordStylesGenerator.RootCharacterNodePath)
				{
					TryGetCharacterStyle(WordStylesGenerator.RootCharacterNodePath, wsId, out CharacterElement normalElem);
					basedOnElem = normalElem.Redirect ?? normalElem;
					WordStylesGenerator.SetBasedOn(style, basedOnElem.UniqueDisplayName());
				}

				// Add the character style element to the collection.
				bool wsIsRtl = LcmWordGenerator.IsWritingSystemRightToLeft(cache, wsId);
				var charElement = new CharacterElement(displayNameBase, style, uniqueNumber,
					nodePath, wsId, wsIsRtl, basedOnElem, sensesSubentriesUniqueDisplayName);
				stylesWithSameDisplayNameBase.Add(charElement);
				uniquePathToCharElement.Add(CharacterElement.GetUniquePath(nodePath, wsId), charElement);
				uniqueNameToCharElement.Add(uniqueDisplayName, charElement);

				// If the wsId is not the default and this is not the Letter Heading style, then add the default.
				// When we are done creating styles and want to re-use styles, the defaults are what we want to
				// try and use first.
				if (wsId != WordStylesGenerator.DefaultStyle &&
					nodePath != WordStylesGenerator.LetterHeadingNodePath)
				{
					AddStyle(styleName, displayNameBase, nodePath, parentUniqueDisplayName,
						WordStylesGenerator.DefaultStyle, pictureStyle, sensesSubentriesUniqueDisplayName);
				}

				// Additional handling for paragraph style.
				if (processParagraphStyle)
				{
					Style paraStyle = null;
					BulletInfo? bulletInfo = null;
					if (pictureStyle)
					{
						paraStyle = WordStylesGenerator.GenerateParagraphStyleFromPictureOptions();
					}
					else
					{
						paraStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(styleName, _propertyTable, out bulletInfo);
					}

					if (paraStyle == null)
					{
						paraStyle = new Style();
						paraStyle.Type = StyleValues.Paragraph;
					}
					AddParagraphStyle(paraStyle, bulletInfo, charElement);
				}

				return uniqueDisplayName;
			}
		}

		/// <summary>
		/// Adds all the necessary character and paragraph styles. This includes styles for all
		/// nodes in the nodePath.
		/// If a style for the nodePath is already in the collection then just return
		/// the unique name.
		/// </summary>
		/// <param name="node">The node to build styles from.</param>
		/// <param name="wsId">The writing system id associated with this style.</param>
		/// <returns>The unique display name that should be referenced in a Run.</returns>
		public string AddStyles(ConfigurableDictionaryNode node, int wsId)
		{
			// Create a list of nodes for the path, from root to 'node'.
			List<ConfigurableDictionaryNode> nodes = new List<ConfigurableDictionaryNode>();
			var pathNode = node;
			while (pathNode != null)
			{
				nodes.Add(pathNode);
				pathNode = pathNode.Parent;
			}
			nodes.Reverse();

			// Generate the unique name and style for each node (starting from the root node).
			string uniqueDisplayName = "";
			string sensesSubentriesUniqueDisplayName = "";
			string parentUniqueDisplayName = null;
			string parentSensesSubentryUniqueDisplayName = null;
			string nodePath = null;
			string sensesSubentriesNodePath = null;
			bool buildSenseSubentryRules = false;
			for (int ii = 0; ii < nodes.Count; ii++)
			{
				var workingNode = nodes[ii];
				var workingDisplayName = workingNode.DisplayLabel;
				var workingStyleName = workingNode.Style;
				nodePath += $".{CssGenerator.GetClassAttributeForConfig(workingNode)} ";
				bool pictureStyle = workingNode.DictionaryNodeOptions is DictionaryNodePictureOptions;

				// If this node is '.subentries' and the next node is '.mainentrysubentries',
				// then we need to build the same set of rules twice for the child nodes, once
				// for ".entry .subentries" and once for ".entry .senses .subentries". We do
				// this so that later, if AddSenseData() is called, we can switch to the style
				// built on the '.entry .senses .subentries' path.
				// We only need to build the two sets for the children. The node
				// '.entry .senses .subentries' does NOT need a second set of rules. It's rules
				// will get created through the normal execution of this method.
				if (nodes[ii].DisplayLabel == "Subentries" && (ii + 1 < nodes.Count) &&
					nodes[ii + 1].DisplayLabel == "MainEntrySubentries")
				{
					// Build the styles for the '.entry .senses .subentries' path.
					var mainEntryNode = nodes[ii - 1];
					var sensesNode = mainEntryNode.Children.First(child => child.DisplayLabel == "Senses");
					var sensesSubentriesNode = sensesNode.Children.First(child => child.DisplayLabel == "Subentries");
					parentSensesSubentryUniqueDisplayName = AddStyles(sensesSubentriesNode, wsId);
					sensesSubentriesNodePath = CssGenerator.GetNodePath(sensesSubentriesNode);
				}

				if (workingDisplayName == "MainEntrySubentries")
				{
					buildSenseSubentryRules = true;
					continue;
				}

				if (buildSenseSubentryRules)
				{
					sensesSubentriesNodePath += $".{CssGenerator.GetClassAttributeForConfig(workingNode)} ";
					sensesSubentriesUniqueDisplayName = AddStyle(workingStyleName, workingDisplayName,
						sensesSubentriesNodePath, parentSensesSubentryUniqueDisplayName, wsId, pictureStyle);
					parentSensesSubentryUniqueDisplayName = sensesSubentriesUniqueDisplayName;
				}
				uniqueDisplayName = AddStyle(workingStyleName, workingDisplayName,
					nodePath, parentUniqueDisplayName, wsId, pictureStyle, sensesSubentriesUniqueDisplayName);
				parentUniqueDisplayName = uniqueDisplayName;
			}
			return uniqueDisplayName;
		}

		/// <summary>
		/// Adds character styles that have additional data added to the nodePath.
		/// </summary>
		/// <returns>The unique display name that should be referenced in a Run.</returns>
		public string AddSpecialCharacterStyle(string styleName, string displayName, string parentUniqueDisplayName,
			ConfigurableDictionaryNode node, string nodePathAdditions, int wsId)
		{
			string nodePath = CssGenerator.GetNodePath(node) + nodePathAdditions;
			return AddStyle(styleName, displayName, nodePath, parentUniqueDisplayName, wsId, false);
		}

		internal void AddGlobalCharacterStyles()
		{
			var cache = _propertyTable.GetValue<LcmCache>("cache");

			// Add the Normal default character style.
			AddStyle(WordStylesGenerator.NormalParagraphStyleName, WordStylesGenerator.RootCharacterDisplayName,
				WordStylesGenerator.RootCharacterNodePath, null, WordStylesGenerator.DefaultStyle, false);

			// Add the Normal writing system styles.
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				AddStyle(WordStylesGenerator.NormalParagraphStyleName, WordStylesGenerator.RootCharacterDisplayName,
					WordStylesGenerator.RootCharacterNodePath, null, aws.Handle, false);
			}

			// Set the redirects for the Normal styles so BasedOn values can initially be set to the correct values
			// and not need to be changed.
			RedirectCharacterElements();

			// Mark the normal styles as 'used' if they are not going to be redirected.
			characterStyles.TryGetValue(WordStylesGenerator.RootCharacterDisplayName, out var elements);
			foreach (var elem in elements)
			{
				if (elem.Redirect == null)
				{
					elem.Used = true;
				}
			}

			// Add the Letter Heading style.
			var wsId = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var letterHeadUniqueName = AddStyle(WordStylesGenerator.LetterHeadingStyleName, WordStylesGenerator.LetterHeadingDisplayName,
				WordStylesGenerator.LetterHeadingNodePath, null, wsId, false);
			GetCharacterElement(letterHeadUniqueName).Used = true;
		}

		/// <summary>
		/// Redirect character elements to other character elements that have identical properties.
		/// Limit the redirection to elements with the same wsId or the defaultId.
		/// The results of the redirection will be used to reduce the number of styles created in Word.
		/// </summary>
		internal void RedirectCharacterElements()
		{
			foreach (var kvPair in characterStyles)
			{
				string DisplayNameBase = kvPair.Key;
				List<CharacterElement> stylesWithSameDisplayNameBase = kvPair.Value;

				// First redirect all the default styles.
				List<CharacterElement> defaultElements = stylesWithSameDisplayNameBase.Where(elem =>
						elem.WritingSystemId == WordStylesGenerator.DefaultStyle).ToList();

				// Iterate through the elements, starting with the second, to see if it is the same
				// as any of the elements proceeding it.
				for (int currentElem = 1; currentElem < defaultElements.Count; currentElem++)
				{
					// Iterate through the proceeding elements to check if they have the same properties.
					for (int proceedingElem = 0; proceedingElem < currentElem; proceedingElem++)
					{
						// If the proceeding element is already re-directed then there is no need to compare the current
						// element to this proceeding element, since it is the same as a proceeding element that we would
						// have already checked.
						if (defaultElements[proceedingElem].Redirect == null)
						{
							if (defaultElements[currentElem].Style.Descendants<StyleRunProperties>().First().OuterXml
								.Equals(defaultElements[proceedingElem].Style.Descendants<StyleRunProperties>().First().OuterXml))
							{
								// Properties are the same, redirect the later element to the earlier one.
								defaultElements[currentElem].Redirect = defaultElements[proceedingElem];
								break;
							}
						}
					}
				}

				// Second redirect all the non-default styles. First try to redirect to a default style, second
				// try to redirect to a non-default style with the same ws.
				List<CharacterElement> nonDefaultElements = stylesWithSameDisplayNameBase.Where(elem =>
					elem.WritingSystemId != WordStylesGenerator.DefaultStyle).ToList();

				// Iterate through the non-default elements, to see if it is the same as any of the default elements
				// or the same as any of the non-default elements with the same WS that proceed it.
				for (int currentElem = 0; currentElem < nonDefaultElements.Count; currentElem++)
				{
					// Check if the current element is the same as any of the default elements.
					//
					// There is no need to check the default styles if the style the current element is based on is not also
					// re-directed to the default style.
					if (nonDefaultElements[currentElem].NodePath == WordStylesGenerator.RootCharacterNodePath ||
						nonDefaultElements[currentElem].BasedOnElement.WritingSystemId == WordStylesGenerator.DefaultStyle)
					{
						foreach (var defaultElem in defaultElements)
						{
							// If the default element is already re-directed then there is no need to compare the current
							// element to this default element, since it is the same as a proceeding element that we would
							// have already checked.
							if (defaultElem.Redirect == null)
							{
								if (nonDefaultElements[currentElem].Style.Descendants<StyleRunProperties>().First().OuterXml
									.Equals(defaultElem.Style.Descendants<StyleRunProperties>().First().OuterXml))
								{
									// Properties are the same, redirect to the default element.
									nonDefaultElements[currentElem].Redirect = defaultElem;
									break;
								}
							}
						}
					}

					// Check if the current element is the same as any of the proceeding elements with the same ws.
					if (nonDefaultElements[currentElem].Redirect == null)
					{
						// Iterate through the proceeding elements to check if they have the same properties.
						for (int proceedingElem = 0; proceedingElem < currentElem; proceedingElem++)
						{
							// If the proceeding element is already re-directed then there is no need to compare the current
							// element to this proceeding element, since it is the same as a proceeding element that we would
							// have already checked.
							if (nonDefaultElements[proceedingElem].Redirect == null)
							{
								if (nonDefaultElements[currentElem].WritingSystemId == nonDefaultElements[proceedingElem].WritingSystemId &&
									nonDefaultElements[currentElem].Style.Descendants<StyleRunProperties>().First().OuterXml
									.Equals(nonDefaultElements[proceedingElem].Style.Descendants<StyleRunProperties>().First().OuterXml))
								{
									// Properties are the same, redirect the later element to the earlier one.
									nonDefaultElements[currentElem].Redirect = nonDefaultElements[proceedingElem];
									break;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Adds the paragraph style and the linked character style.
		/// </summary>
		/// <returns>>The unique display name that should be referenced in a Paragraph.</returns>
		internal ParagraphElement AddParagraphStyle(ConfigurableDictionaryNode node)
		{
			// Adding the styles for the DefaultStyle, will result in the paragraph style being added.
			string charUniqueDispName = AddStyles(node, WordStylesGenerator.DefaultStyle);
			var charElem = uniqueNameToCharElement[charUniqueDispName];
			return charElem.LinkedParagraphElement;
		}

		/// <summary>
		/// Worker method to add the paragraph style and link it to the associated character style.
		/// </summary>
		/// <param name="charElem">The associated character element.</param>
		private void AddParagraphStyle(Style paraStyle, BulletInfo? bulletInfo, CharacterElement charElem)
		{
			lock (_collectionLock)
			{
				// Update the paragraph style name.
				string uniqueDisplayName = charElem.UniqueNumber == 1 ? charElem.DisplayNameBase : charElem.DisplayNameBase + charElem.UniqueNumber.ToString();
				WordStylesGenerator.SetStyleName(paraStyle, uniqueDisplayName);

				// Update the BasedOn value.
				if (charElem.NodePath != WordStylesGenerator.RootCharacterNodePath)
				{
					TryGetParagraphStyle(WordStylesGenerator.NormalParagraphNodePath, out ParagraphElement normalParaElem);
					WordStylesGenerator.SetBasedOn(paraStyle, normalParaElem.UniqueDisplayName());
				}

				// Add the paragraph element to the collection.
				var paraElem = new ParagraphElement(charElem.DisplayNameBase, paraStyle, charElem.UniqueNumber, charElem.NodePath, bulletInfo);
				AddParagraphElement(paraElem);

				// Link the paragraph and character styles.
				paraElem.LinkedCharacterElement = charElem;
				charElem.LinkedParagraphElement = paraElem;
				paraElem.Style.Append(new LinkedStyle() { Val = charElem.UniqueDisplayName() });
				charElem.Style.Append(new LinkedStyle() { Val = paraElem.UniqueDisplayName() });

				// Set the paragraph and character styles to used.
				charElem.Used = true;
				paraElem.Used = true;
			}
		}

		private void AddParagraphElement(ParagraphElement paraElem)
		{
			// If this is the first style with this base, then add it to the Dictionary.
			if (!paragraphStyles.TryGetValue(paraElem.DisplayNameBase, out List<ParagraphElement> paraStylesWithSameDisplayNameBase))
			{
				paraStylesWithSameDisplayNameBase = new List<ParagraphElement>();
				paragraphStyles.Add(paraElem.DisplayNameBase, paraStylesWithSameDisplayNameBase);
			}

			// Add the paragraph element to the collection.
			paraStylesWithSameDisplayNameBase.Add(paraElem);
			uniquePathToParaElement[paraElem.NodePath] = paraElem;
		}

		/// <summary>
		/// Creates a paragraph continuation element.
		/// </summary>
		/// <param name="paraElem">The element used to build the continuation element.</param>
		/// <returns></returns>
		internal ParagraphElement AddParagraphContinuationStyle(ParagraphElement paraElem)
		{
			lock (_collectionLock)
			{
				if (paraElem.ContinuationElement != null)
				{
					return paraElem.ContinuationElement;
				}

				var contStyle = WordStylesGenerator.GenerateContinuationStyle(paraElem.Style);

				// Add the continuation element to the collection.
				var contElem = new ParagraphElement(paraElem.DisplayNameBase + WordStylesGenerator.EntryStyleContinue, contStyle, paraElem.UniqueNumber,
					paraElem.NodePath + WordStylesGenerator.EntryStyleContinue, null);
				string uniqueDisplayName = contElem.UniqueNumber == 1 ? contElem.DisplayNameBase : contElem.DisplayNameBase + contElem.UniqueNumber.ToString();
				WordStylesGenerator.SetStyleName(contElem.Style, uniqueDisplayName);
				AddParagraphElement(contElem);
				paraElem.ContinuationElement = contElem;
				contElem.Used = true;
				return contElem;
			}
		}

		internal void AddGlobalParagraphStyles()
		{
			// Normal style
			var normStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(WordStylesGenerator.NormalParagraphStyleName,
				_propertyTable, out BulletInfo? bulletInfo);
			var normElem = new ParagraphElement(WordStylesGenerator.NormalParagraphDisplayName,
				normStyle, 1, WordStylesGenerator.NormalParagraphNodePath, bulletInfo);
			AddParagraphElement(normElem);

			// Page Header Style
			TryGetCharacterStyle(WordStylesGenerator.RootCharacterNodePath, WordStylesGenerator.DefaultStyle, out CharacterElement rootElem);
			var pageHeaderStyle = WordStylesGenerator.GeneratePageHeaderStyle(normStyle, rootElem.Style);
			// Intentionally re-using the bulletInfo from Normal.
			var pageHeaderElem = new ParagraphElement(WordStylesGenerator.PageHeaderDisplayName,
				pageHeaderStyle, 1, WordStylesGenerator.PageHeaderNodePath, bulletInfo);
			AddParagraphElement(pageHeaderElem);

			// Letter Header Style
			var headStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(WordStylesGenerator.LetterHeadingStyleName,
				_propertyTable, out bulletInfo);
			WordStylesGenerator.SetStyleName(headStyle, WordStylesGenerator.LetterHeadingDisplayName);
			WordStylesGenerator.SetBasedOn(headStyle, WordStylesGenerator.NormalParagraphDisplayName);
			var headElem = new ParagraphElement(WordStylesGenerator.LetterHeadingDisplayName,
				headStyle, 1, WordStylesGenerator.LetterHeadingNodePath, bulletInfo);
			AddParagraphElement(headElem);
		}

		/// <summary>
		/// Returns a unique id that is used for bullet and numbering in paragraph styles.
		/// </summary>
		public int GetNewBulletAndNumberingUniqueId
		{
			get
			{
				lock (_collectionLock)
				{
					return bulletAndNumberingUniqueIdCounter++;
				}
			}
		}

		/// <summary>
		/// Returns a unique id that is used for picture IDs.
		/// </summary>
		public int GetAndIncrementPictureUniqueIdCount
		{
			get
			{
				lock (_collectionLock)
				{
					return pictureUniqueIdCounter++;
				}
			}
		}
	}

	internal class CharacterElement : BaseElement
	{
		/// <param name="wsId">The writing system id associated with this style.</param>
		/// <param name="wsIsRtl">True if the writing system is right to left.</param>
		/// <param name="basedOnElem">The element this elements style is based on.</param>
		/// <param name="sensesSubentriesUniqueDisplayName">The unique display name for the associated senses subentries element.</param>
		internal CharacterElement(string displayNameBase, Style style, int uniqueNumber,
			string nodePath, int wsId, bool wsIsRtl, CharacterElement basedOnElem, string sensesSubentriesUniqueDisplayName = null) :
			base(displayNameBase, style, uniqueNumber, nodePath)
		{
			this.WritingSystemId = wsId;
			this.WritingSystemIsRtl = wsIsRtl;
			this.BasedOnElement = basedOnElem;
			this.SensesSubentriesUniqueDisplayName = sensesSubentriesUniqueDisplayName;
		}
		internal int WritingSystemId { get; }
		internal bool WritingSystemIsRtl { get; }
		internal string SensesSubentriesUniqueDisplayName { get; set; }
		internal CharacterElement Redirect { get; set; }
		internal CharacterElement BasedOnElement { get; }
		internal ParagraphElement LinkedParagraphElement { get; set; }
		internal static string GetUniquePath(string nodePath, int wsId)
		{
			return nodePath + wsId;
		}
	}

	internal class ParagraphElement : BaseElement
	{
		/// <param name="bulletInfo">Bullet and Numbering info used by some paragraph styles.</param>
		internal ParagraphElement(string displayNameBase, Style paraStyle, int uniqueNumber, string nodePath, BulletInfo? bulletInfo) :
			base(displayNameBase, paraStyle, uniqueNumber, nodePath)
		{
			BulletInfo = bulletInfo;
			NumberingFirstNumUniqueIds = new List<int>();
		}

		/// <summary>
		/// Bullet and Numbering info used by some (not all) paragraph styles.
		/// </summary>
		internal BulletInfo? BulletInfo { get; }
		internal CharacterElement LinkedCharacterElement { get; set; }
		internal ParagraphElement ContinuationElement { get; set; }

		/// <summary>
		/// Unique id for this style that can be used for all bullet list items, and
		/// for all numbered list items except for the first list item in each list.
		/// </summary>
		internal int? BulletAndNumberingUniqueId { get; set; }

		/// <summary>
		/// For numbered lists the first list item in each list must have it's own unique id. This
		/// allows us to re-start the numbering for each list.
		/// </summary>
		internal List<int> NumberingFirstNumUniqueIds { get; set; }
	}

	internal abstract class BaseElement
	{
		/// <param name="displayNameBase">The unmodified FLEX node display name from node.DisplayLabel.
		///     Does not include language tag or appended number.</param>
		/// <param name="style">The style with it's styleId set to the uniqueDisplayName.
		///     Examples of uniqueDisplayName:
		///         Definition (or Gloss)[lang=en]
		///			Definition (or Gloss)2[lang=en] </param>
		/// <param name="uniqueNumber">The number to distinguish between styles with the same displayNameBase but different nodePaths.</param>
		/// <param name="nodePath">Path from the root node to the leaf node.</param>
		internal BaseElement(string displayNameBase, Style style, int uniqueNumber, string nodePath)
		{
			this.DisplayNameBase = displayNameBase;
			this.Style = style;
			this.UniqueNumber = uniqueNumber;
			this.NodePath  = nodePath;
		}

		internal string DisplayNameBase { get; }
		internal Style Style { get; }
		internal int UniqueNumber { get; }
		internal string NodePath { get; }
		internal bool Used { get; set; }
		internal string UniqueDisplayName()
		{
			return Style.StyleId;
		}
	}
}