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
		internal List<ParagraphElement> GetUsedParagraphElements()
		{
			// Get an enumerator to the flattened list of all StyleElements.
			var enumerator = paragraphStyles.Values.SelectMany(x => x);
			// Create a single list of all the StyleElements.
			return enumerator.Where(x => x.Used).ToList();
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
			string parentUniqueDisplayName, int wsId)
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

				// We don't want to process a picture style or the normal style, as these are handled with global styles.
				bool processParagraphStyle = ((nodePath != WordStylesGenerator.NormalCharNodePath) &&
											(nodePath != WordStylesGenerator.PictureAndCaptionNodePath) &&
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
					if (parentRunProps != null)
					{
						style.Append(parentRunProps.CloneNode(true));
					}
					else
					{
						style.Append(new StyleRunProperties());
					}
				}

				// Update the style name.
				WordStylesGenerator.SetStyleName(style, uniqueDisplayName);

				// Update the BasedOn value.
				CharacterElement basedOnElem = null;
				if (nodePath != WordStylesGenerator.NormalCharNodePath)
				{
					TryGetCharacterStyle(WordStylesGenerator.NormalCharNodePath, wsId, out CharacterElement normalElem);
					basedOnElem = normalElem.Redirect ?? normalElem;
					WordStylesGenerator.SetBasedOn(style, basedOnElem.UniqueDisplayName());
				}

				// Add the character style element to the collection.
				bool wsIsRtl = LcmWordGenerator.IsWritingSystemRightToLeft(cache, wsId);
				var charElement = new CharacterElement(displayNameBase, style, uniqueNumber,
					nodePath, wsId, wsIsRtl, basedOnElem);
				stylesWithSameDisplayNameBase.Add(charElement);
				uniquePathToCharElement.Add(CharacterElement.GetUniquePath(nodePath, wsId), charElement);
				uniqueNameToCharElement.Add(uniqueDisplayName, charElement);

				// Additional handling for paragraph style.
				if (processParagraphStyle)
				{
					Style paraStyle = null;
					BulletInfo? bulletInfo = null;
					paraStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(styleName, _propertyTable, out bulletInfo);

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
		/// nodes in the nodeList.
		/// If a style for the last node in the nodeList is already in the collection then just return
		/// its unique name.
		/// </summary>
		/// <param name="nodeList">List of nodes starting from the root.</param>
		/// <param name="wsId">The writing system id associated with this style.</param>
		/// <returns>The unique display name for the last node in the nodeList. (It should be referenced in a Run.)</returns>
		public string AddStyles(List<ConfigurableDictionaryNode> nodeList, int wsId)
		{
			// Generate the unique name and style for each node (starting from the root node).
			string uniqueDisplayName = "";
			string parentUniqueDisplayName = null;
			string nodePath = null;
			foreach (var node in nodeList)
			{
				nodePath += $".{CssGenerator.GetClassAttributeForConfig(node)} ";

				uniqueDisplayName = AddStyle(node.Style, node.DisplayLabel,
					nodePath, parentUniqueDisplayName, wsId);
				parentUniqueDisplayName = uniqueDisplayName;
			}
			return uniqueDisplayName;
		}

		/// <summary>
		/// Add a character style that has additional data added to the nodePath.
		/// </summary>
		/// <param name="specialNodePath">The node path for the node with all additions added to the end.</param>
		/// <returns>The unique display name that should be referenced in a Run.</returns>
		public string AddSpecialCharacterStyle(string styleName, string displayName, string parentUniqueDisplayName,
			string specialNodePath, int wsId)
		{
			return AddStyle(styleName, displayName, specialNodePath, parentUniqueDisplayName, wsId);
		}

		internal void AddGlobalCharacterStyles()
		{
			var cache = _propertyTable.GetValue<LcmCache>("cache");

			// Add the Normal default character style.
			AddStyle(WordStylesGenerator.NormalParagraphStyleName, WordStylesGenerator.NormalCharDisplayName,
				WordStylesGenerator.NormalCharNodePath, null, WordStylesGenerator.DefaultStyle);

			// Add the Normal writing system styles.
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				AddStyle(WordStylesGenerator.NormalParagraphStyleName, WordStylesGenerator.NormalCharDisplayName,
					WordStylesGenerator.NormalCharNodePath, null, aws.Handle);
			}

			// Set the redirects for the Normal styles so BasedOn values can initially be set to the correct values
			// and not need to be changed.
			RedirectCharacterElements();

			// Mark the normal styles as 'used' if they are not going to be redirected.
			characterStyles.TryGetValue(WordStylesGenerator.NormalCharDisplayName, out var elements);
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
				WordStylesGenerator.LetterHeadingNodePath, null, wsId);
			GetCharacterElement(letterHeadUniqueName).Used = true;
		}

		/// <summary>
		/// Redirect character elements to other character elements that have identical properties.
		/// Limit the redirection to elements with the same wsId.
		/// The results of the redirection will be used to reduce the number of styles created in Word.
		/// </summary>
		internal void RedirectCharacterElements()
		{
			foreach (var kvPair in characterStyles)
			{
				List<CharacterElement> stylesWithSameDisplayNameBase = kvPair.Value;

				// First redirect all the default styles.
				List<CharacterElement> defaultElements = stylesWithSameDisplayNameBase.Where(elem =>
						elem.WritingSystemId == WordStylesGenerator.DefaultStyle).ToList();

				// Iterate through the elements, starting with the second, to see if it is the same
				// as any of the elements preceding it.
				for (int currentElem = 1; currentElem < defaultElements.Count; currentElem++)
				{
					// Don't redirect a characterElement that is linked to a paragraphElement.
					if (defaultElements[currentElem].LinkedParagraphElement != null)
					{
						continue;
					}

					// Iterate through the preceding elements to check if they have the same properties.
					for (int precedingElem = 0; precedingElem < currentElem; precedingElem++)
					{
						// If the preceding element is already re-directed then there is no need to compare the current
						// element to this preceding element, since it is the same as a preceding element that we would
						// have already checked.
						if (defaultElements[precedingElem].Redirect == null)
						{
							// Don't redirect to a characterElement that is linked to a paragraphElement.
							if (defaultElements[precedingElem].LinkedParagraphElement != null)
							{
								continue;
							}

							if (defaultElements[currentElem].Style.Descendants<StyleRunProperties>().First().OuterXml
								.Equals(defaultElements[precedingElem].Style.Descendants<StyleRunProperties>().First().OuterXml))
							{
								// Properties are the same, redirect the later element to the earlier one.
								defaultElements[currentElem].Redirect = defaultElements[precedingElem];
								break;
							}
						}
					}
				}

				// Second redirect all the non-default styles to a non-default style with the same ws.
				List<CharacterElement> nonDefaultElements = stylesWithSameDisplayNameBase.Where(elem =>
					elem.WritingSystemId != WordStylesGenerator.DefaultStyle).ToList();

				// Iterate through the non-default elements, to see if it is the same as any of the
				// non-default elements with the same WS that proceed it.
				for (int currentElem = 0; currentElem < nonDefaultElements.Count; currentElem++)
				{
					// Iterate through the preceding elements to check if they have the same properties.
					for (int precedingElem = 0; precedingElem < currentElem; precedingElem++)
					{
						// If the preceding element is already re-directed then there is no need to compare the current
						// element to this preceding element, since it is the same as a preceding element that we would
						// have already checked.
						if (nonDefaultElements[precedingElem].Redirect == null)
						{
							if (nonDefaultElements[currentElem].WritingSystemId == nonDefaultElements[precedingElem].WritingSystemId &&
								nonDefaultElements[currentElem].Style.Descendants<StyleRunProperties>().First().OuterXml
								.Equals(nonDefaultElements[precedingElem].Style.Descendants<StyleRunProperties>().First().OuterXml))
							{
								// Properties are the same, redirect the later element to the earlier one.
								nonDefaultElements[currentElem].Redirect = nonDefaultElements[precedingElem];
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Redirect paragraph elements to other paragraph elements that have identical properties.
		/// The results of the redirection will be used to reduce the number of styles created in Word.
		/// </summary>
		internal void RedirectParagraphElements()
		{
			// Iterate through the list of elements with different Display Name Bases.
			foreach (var kvPair in paragraphStyles)
			{
				List<ParagraphElement> elements = kvPair.Value;

				// Iterate through the elements, starting with the second, to see if it is the same
				// as any of the elements preceding it.
				for (int currentElem = 1; currentElem < elements.Count; currentElem++)
				{
					// Don't redirect an element that has BulletInfo.
					// Continuation elements will not have their LinkedCharacterElement property set.  No need to check them
					// for redirection. They will get redirected along with the associated normal paragraph element.
					if ((elements[currentElem].BulletInfo != null) || (elements[currentElem].LinkedCharacterElement == null))
					{
						continue;
					}

					// Iterate through the preceding elements to check if they have the same properties.
					for (int precedingElem = 0; precedingElem < currentElem; precedingElem++)
					{
						// Don't redirect to an element that has BulletInfo.
						// Continuation elements will not have their LinkedCharacterElement property set.  No need to check them
						// for redirection. They will get redirected along with the associated normal paragraph element.
						if ((elements[precedingElem].BulletInfo != null) || (elements[precedingElem].LinkedCharacterElement == null))
						{
							continue;
						}

						// If the preceding element is already re-directed then there is no need to compare the current
						// element to this preceding element, since it is the same as a preceding element that we would
						// have already checked.
						if (elements[precedingElem].Redirect == null)
						{
							// Paragraph properties must be the same to redirect.
							var currentParaProps = elements[currentElem].Style.Descendants<StyleParagraphProperties>().FirstOrDefault();
							var precedingParaProps = elements[precedingElem].Style.Descendants<StyleParagraphProperties>().FirstOrDefault();
							if ((currentParaProps == null && precedingParaProps == null) ||
								currentParaProps != null &&
								precedingParaProps != null &&
								currentParaProps.OuterXml.Equals(precedingParaProps.OuterXml))
							{
								// The linked character properties must be the same to redirect.
								if (elements[currentElem].LinkedCharacterElement.Style.Descendants<StyleRunProperties>().First().OuterXml
									.Equals(elements[precedingElem].LinkedCharacterElement.Style.Descendants<StyleRunProperties>().First().OuterXml))
								{
									// Properties are the same, redirect the later element to the earlier one.
									elements[currentElem].Redirect = elements[precedingElem];
									elements[currentElem].LinkedCharacterElement.Redirect = elements[precedingElem].LinkedCharacterElement;
									if (elements[currentElem].ContinuationElement != null)
									{
										if (elements[precedingElem].ContinuationElement == null)
										{
											AddParagraphContinuationStyle(elements[precedingElem]);
										}
										elements[currentElem].ContinuationElement.Redirect = elements[precedingElem].ContinuationElement;
									}
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
		internal ParagraphElement AddParagraphStyle(List<ConfigurableDictionaryNode> nodeList)
		{
			// Adding the styles for the DefaultStyle, will result in the paragraph style being added.
			string charUniqueDispName = AddStyles(nodeList, WordStylesGenerator.DefaultStyle);
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
				if (charElem.NodePath != WordStylesGenerator.NormalCharNodePath)
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
			}
		}

		internal void AddParagraphElement(ParagraphElement paraElem)
		{
			lock (_collectionLock)
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
		}

		/// <summary>
		/// Creates a paragraph continuation element.
		/// </summary>
		/// <param name="paraElem">The element used to build the continuation element.</param>
		/// <returns>The continuation element.</returns>
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
			normElem.Used = true;

			// Page Header Style
			TryGetCharacterStyle(WordStylesGenerator.NormalCharNodePath, WordStylesGenerator.DefaultStyle, out CharacterElement normalCharElem);
			var pageHeaderStyle = WordStylesGenerator.GeneratePageHeaderStyle(normStyle, normalCharElem.Style);
			// Intentionally re-using the bulletInfo from Normal.
			var pageHeaderElem = new ParagraphElement(WordStylesGenerator.PageHeaderDisplayName,
				pageHeaderStyle, 1, WordStylesGenerator.PageHeaderNodePath, bulletInfo);
			AddParagraphElement(pageHeaderElem);
			pageHeaderElem.Used = true;

			// Letter Header Style
			var headStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(WordStylesGenerator.LetterHeadingStyleName,
				_propertyTable, out bulletInfo);
			WordStylesGenerator.SetStyleName(headStyle, WordStylesGenerator.LetterHeadingDisplayName);
			WordStylesGenerator.SetBasedOn(headStyle, WordStylesGenerator.NormalParagraphDisplayName);
			var headElem = new ParagraphElement(WordStylesGenerator.LetterHeadingDisplayName,
				headStyle, 1, WordStylesGenerator.LetterHeadingNodePath, bulletInfo);
			AddParagraphElement(headElem);
			headElem.Used = true;

			// Picture & Caption Style
			// Creating a style for the paragraph that will contain the image and caption
			var pictureCaptionStyle = new Style()
			{
				Type = StyleValues.Paragraph,
				StyleId = WordStylesGenerator.PictureAndCaptionTextboxDisplayName,
				StyleName = new StyleName() { Val = WordStylesGenerator.PictureAndCaptionTextboxDisplayName }
			};

			var parProps = new StyleParagraphProperties();
			// The image and caption should always be centered within the textbox.
			parProps.Justification = new Justification() { Val = JustificationValues.Center };
			// In FLEx, pictures have no added before/after paragraph spacing.
			parProps.Append(new SpacingBetweenLines() { Before = "0", After = "0" });
			pictureCaptionStyle.Append(parProps);

			var pictureCaptionElem = new ParagraphElement(
				WordStylesGenerator.PictureAndCaptionTextboxDisplayName,
				pictureCaptionStyle, 1, WordStylesGenerator.PictureAndCaptionNodePath, null);
			AddParagraphElement(pictureCaptionElem);
			pictureCaptionElem.Used = true;
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
			string nodePath, int wsId, bool wsIsRtl, CharacterElement basedOnElem) :
			base(displayNameBase, style, uniqueNumber, nodePath)
		{
			this.WritingSystemId = wsId;
			this.WritingSystemIsRtl = wsIsRtl;
			this.BasedOnElement = basedOnElem;
		}
		internal int WritingSystemId { get; }
		internal bool WritingSystemIsRtl { get; set; }
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
		internal ParagraphElement Redirect { get; set; }

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