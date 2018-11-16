// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;

namespace SILUBS.ScriptureChecks
{
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