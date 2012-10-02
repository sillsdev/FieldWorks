// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: GAFAWSData.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of the main GAFAWSData calss, and some of its supporting classes.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Main class in the GAFAWS data layer.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[XmlRootAttribute(Namespace="", IsNullable=false)]
	public sealed class GAFAWSData
	{
		#region Data members

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of WordRecord objects.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private List<WordRecord> m_wordRecords;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of Morpheme objects.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private List<Morpheme> m_morphemes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Holder for the classes objects.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Classes m_classes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of problems.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private List<Challenge> m_challenges;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Doomain specific XML holder.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Other m_other;

		#endregion // Data members

		#region Serialized Data
		// [NOTE: Don't reorder these, or they won't be dumped in the right order,
		// and the resulting XML file won't pass the schema.]

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of word records.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlArrayItemAttribute("WordRecord", IsNullable=false)]
		public List<WordRecord> WordRecords
		{
			get { return m_wordRecords; }
			set { m_wordRecords = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of morphemes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlArrayItemAttribute("Morpheme", IsNullable=false)]
		public List<Morpheme> Morphemes
		{
			get { return m_morphemes; }
			set { m_morphemes = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of position classes. (Reserved for use by the Paradigm DLL.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Classes Classes
		{
			get { return m_classes; }
			set { m_classes = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of problems. (Reserved for use by the Paradigm DLL.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlArrayItemAttribute("Challenge", IsNullable=false)]
		public List<Challenge> Challenges
		{
			get { return m_challenges; }
			set { m_challenges = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Model-specific data.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Other Other
		{
			get { return m_other; }
			set { m_other = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// An attribute for a date. (Reserved for use by the Paradigm DLL.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute()]
		public string date;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// An attribute for a time. (Reserved for use by the Paradigm DLL.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute()]
		public string time;

		#endregion	// Serialized Data

		#region Construction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor. DO NOT USE THIS CONSTRUCTOR.
		/// Use static 'Create' method to make an initialized instance.
		/// This public is only for use by XML serialization routines.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public GAFAWSData()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Static method used to create a well-formed instance of a GAFAWSData obejct
		/// from code.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public static GAFAWSData Create()
		{
			GAFAWSData gd = new GAFAWSData();

			gd.m_wordRecords = new List<WordRecord>();
			gd.m_morphemes = new List<Morpheme>();
			gd.m_classes = new Classes();
			gd.m_challenges = new List<Challenge>();
			return gd;
		}
		#endregion // Construction

		#region Serialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save the data to a file.
		/// </summary>
		/// <param name="pathname">Pathname of file to save to.</param>
		/// -----------------------------------------------------------------------------------
		public void SaveData(string pathname)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(GAFAWSData));
			TextWriter writer = new StreamWriter(pathname);
			serializer.Serialize(writer, this);
			writer.Close();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Load data from file.
		/// </summary>
		/// <param name="pathname">Pathname of file to load.</param>
		/// <returns>An instance of GAFAWSData, if successful.</returns>
		/// <remarks>
		/// [NOTE: This may throw some exceptions, if pathname is bad,
		/// or it is not a suitable file.]
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public static GAFAWSData LoadData(string pathname)
		{
			GAFAWSData gd = null;
			XmlTextReader reader = new XmlTextReader(pathname);
			XmlSerializer serializer = new XmlSerializer(typeof(GAFAWSData));
			try
			{
				gd = (GAFAWSData)serializer.Deserialize(reader);
				// Remove empty collections from Wordrecord objects,
				// since the loader makes them, but the XML schema has them as optional.
				foreach (WordRecord wr in gd.m_wordRecords)
				{
					if (wr.Prefixes.Count == 0)
						wr.Prefixes = null;
					if (wr.Suffixes.Count == 0)
						wr.Suffixes = null;
				}
			}
			finally
			{
				reader.Close();
			}

			return gd;
		}
		#endregion	// Serialization
	}


	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A problem report. (Reserved for use by the Paradigm DLL.)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public sealed class Challenge
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Report message.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute()]
		public string message;
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Affix position class. (Reserved for use by the Paradigm DLL.)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public sealed class Class
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Class ID.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute(DataType="ID")]
		public string CLID;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Class name.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute()]
		public string name;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// 0 for an unknown position, otherwise 1.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValueAttribute("0")]
		public string isFogBank = "0";
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Holder for classes. (Reserved for use by the Paradigm DLL.)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public sealed class Classes
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Prefix position classes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[XmlArrayItemAttribute(IsNullable=false)]
		public List<Class> PrefixClasses;

		/// <summary>
		/// Suffix position classes.
		/// </summary>
		[XmlArrayItemAttribute(IsNullable=false)]
		public List<Class> SuffixClasses;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Classes()
		{
			PrefixClasses = new List<Class>();
			SuffixClasses = new List<Class>();
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Model-specific XML data.
	/// Model-specific data must be added as XmlElement objects,
	/// and placed in the XmlElements collection.
	/// Currently, the XML Schema allows for 'Other' on:
	///		GAFAWSData
	///		WordRecord
	///		Morpheme
	///		Stem
	///		Affix
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public sealed class Other
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Generic XML elements.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public List<XmlElement> XmlElements;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Other()
		{
			XmlElements = new List<XmlElement>();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Other(string xml)
		{
			XmlElements = new List<XmlElement>();
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			XmlElements.Add((XmlElement)doc.LastChild);
		}
	}
}
