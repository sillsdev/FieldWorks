// ---------------------------------------------------------------------------------------------
// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleMarkupInfo.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.Utils;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StyleMarkupInfo contains information from a stylesheet needed for Scripture checking.
	/// It deserializes from an XML file and provides serialized lists of styles for particular
	/// checks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("Styles")]
	public class StyleMarkupInfo
	{
		#region Public member variables
		[XmlArray("markup")]
		public List<StyleMarkup> StyleMarkupList;

		private StylePropsInfo m_stylePropInfo;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified XML file.
		/// </summary>
		/// <param name="filename">The name of the XML file.</param>
		/// <returns>information from the stylesheet needed for checking</returns>
		/// ------------------------------------------------------------------------------------
		public static StyleMarkupInfo Load(string filename)
		{
			StyleMarkupInfo smi = XmlSerializationHelper.DeserializeFromFile<StyleMarkupInfo>(filename);
			smi.m_stylePropInfo = new StylePropsInfo(smi);
			return (smi ?? new StyleMarkupInfo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether styles information have been loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool StylesAreLoaded
		{
			get { return StylePropsInfo.s_sentenceInitial != null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information about the styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StylePropsInfo StyleInfo
		{
			get { return m_stylePropInfo; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StyleMarkup contains information from a single style in a stylesheet which will be used
	/// for Scripture checks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("tag")]
	public class StyleMarkup
	{
		/// <summary>The name of a style in the stylesheet.</summary>
		[XmlAttribute("id")]
		public string Id;

		/// <summary>The type attribute value (i.e. character or paragraph) for a style in the stylesheet.</summary>
		[XmlAttribute("type")]
		public string Type;

		/// <summary>The use attribute value for a style in the stylesheet.</summary>
		[XmlAttribute("use")]
		public string Use;

		/// <summary>The context attribute value for a style in the stylesheet.</summary>
		[XmlAttribute("context")]
		public string Context;

		/// <summary>The structure attribute value for a style in the stylesheet.</summary>
		[XmlAttribute("structure")]
		public string Structure;
	}

	#region StyleInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleInfo
	{
		/// <summary></summary>
		public enum StyleTypes
		{
			paragraph,
			character
		}

		public enum UseTypes
		{
			line,
			prose,
			stanzabreak,
			other
		}

		string m_styleName;
		StyleTypes m_styleType;
		UseTypes m_useType = UseTypes.other;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfo"/> class.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleType">Type of the style.</param>
		/// <param name="useType">Style usage.</param>
		/// ------------------------------------------------------------------------------------
		public StyleInfo(string styleName, StyleTypes styleType, UseTypes useType)
		{
			m_styleName = styleName;
			m_styleType = styleType;
			m_useType = useType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the style.
		/// </summary>
		/// <value>The name of the style.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string StyleName
		{
			get { return m_styleName; }
			set { m_styleName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of the style.
		/// </summary>
		/// <value>The type of the style.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public StyleTypes StyleType
		{
			get { return m_styleType; }
			set { m_styleType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the usage of the style.
		/// </summary>
		/// <value>The usage of the style.</value>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public UseTypes UseType
		{
			get { return m_useType; }
			set { m_useType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0}; {1}; {2}", m_styleName, m_styleType, m_useType);
		}
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StylePropsInfo contains lists of styles with certain properties. Styles that:
	/// * begin sentences,
	/// * are used for proper nouns,
	/// * are used in tables,
	/// * are used for lists,
	/// * have special use (e.g. Interlude, Opening, Closing),
	/// * are used for titles,
	/// * are used for headings
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StylePropsInfo
	{
		#region Member variables
		/// <summary>styles that begin with a capital because they start a sentence.</summary>
		internal static List<StyleInfo> s_sentenceInitial = null;
		/// <summary>styles that begin with a capital because they are proper names.</summary>
		internal static List<StyleInfo> s_properNoun = null;
		/// <summary>styles that begin with a capital because they occur in a table.</summary>
		internal static List<StyleInfo> s_table = null;
		/// <summary>styles that begin with a capital because they occur in a list.</summary>
		internal static List<StyleInfo> s_list = null;
		/// <summary>styles that begin with a capital for miscellaneous reasons (e.g. interlude)</summary>
		internal static List<StyleInfo> s_special = null;
		/// <summary>styles that begin with a capital because they are titles.</summary>
		internal static List<StyleInfo> s_title = null;
		/// <summary>styles that begin with a capital because they are headings</summary>
		internal static List<StyleInfo> s_heading = null;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="StylePropsInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StylePropsInfo()
		{
			s_sentenceInitial = new List<StyleInfo>();
			s_properNoun = new List<StyleInfo>();
			s_table = new List<StyleInfo>();
			s_list = new List<StyleInfo>();
			s_special = new List<StyleInfo>();
			s_heading = new List<StyleInfo>();
			s_title = new List<StyleInfo>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="StylePropsInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StylePropsInfo(StyleMarkupInfo smi) : this()
		{
			CreateStyleLists(smi);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the style lists needed for Scripture checking.
		/// </summary>
		/// <param name="smi">information needed for Scripture checking from all styles</param>
		/// ------------------------------------------------------------------------------------
		private void CreateStyleLists(StyleMarkupInfo smi)
		{
			foreach (StyleMarkup style in smi.StyleMarkupList)
			{
				StyleInfo.StyleTypes styleType = (style.Type == "paragraph") ?
					StyleInfo.StyleTypes.paragraph : StyleInfo.StyleTypes.character;

				string styleName = style.Id.Replace("_", " ");

				// The following uses should begin with a capital letter:
				//  * sentence initial
				//  * proper name
				//  * table
				//  * list
				//  * special (e.g. interlude, closing)
				// Save them in their own list so that we can report an appropriate error to the user.
				if (style.Use == "proseSentenceInitial")
					StylePropsInfo.s_sentenceInitial.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.prose));
				else if (style.Use == "lineSentenceInitial")
					StylePropsInfo.s_sentenceInitial.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.line));
				else if (style.Use == "properNoun")
					StylePropsInfo.s_properNoun.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));
				else if (style.Use == "table")
					StylePropsInfo.s_table.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));
				else if (style.Use == "list")
					StylePropsInfo.s_list.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));
				else if (style.Use == "special")
					StylePropsInfo.s_special.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));
				else if (style.Use == "stanzabreak")
					StylePropsInfo.s_special.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.stanzabreak));

				// Titles should begin with a capital letter. Styles used for titles have a context of "title".
				if (!string.IsNullOrEmpty(style.Context) && style.Context == "title")
					StylePropsInfo.s_title.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));

				// Headings should begin with a capital letter. Styles used for headings have a structure of "heading".
				if (!string.IsNullOrEmpty(style.Structure) && style.Structure == "heading")
					StylePropsInfo.s_heading.Add(new StyleInfo(styleName, styleType, StyleInfo.UseTypes.other));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified XML source.
		/// </summary>
		/// <param name="xmlSource">The XML source.</param>
		/// <returns>information about the styles deserialized from the XML source</returns>
		/// ------------------------------------------------------------------------------------
		public static StylePropsInfo Load(string xmlSource)
		{
			return XmlSerializationHelper.DeserializeFromString<StylePropsInfo>(xmlSource);
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of styles in this class as a serialized string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string XmlString
		{
			get { return XmlSerializationHelper.SerializeToString<StylePropsInfo>(this); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles that begin sentences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> SentenceInitial
		{
			get { return s_sentenceInitial; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles used for proper nouns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> ProperNouns
		{
			get { return s_properNoun; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles used in tables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> Table
		{
			get { return s_table; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles used for lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> List
		{
			get { return s_list; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles with special uses (e.g. Interlude, Opening, Closing).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> Special
		{
			get { return s_special; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles used for headings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> Heading
		{
			get { return s_heading; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles used for titles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<StyleInfo> Title
		{
			get { return s_title; }
			set { }
		}
		#endregion

	}
}
