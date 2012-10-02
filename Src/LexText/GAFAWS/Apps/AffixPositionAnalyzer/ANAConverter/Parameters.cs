// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: Parameters.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Parameters Class for serialization.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// <summary>
	/// Parameters for the converter.
	/// </summary>
	[XmlRootAttribute("ANAConverterOptions", Namespace="", IsNullable=false)]
	public class Parameters
	{
		/// <summary>
		/// Root Delimiters object.
		/// </summary>
		private RootDelimiters m_rootDelimiters;

		/// <summary>
		/// Markers object.
		/// </summary>
		private Markers m_markers;

		/// <summary>
		/// Categories List.
		/// </summary>
		private List<Category> m_categories;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Parameters()
		{
			m_rootDelimiters = new RootDelimiters();
			m_markers = new Markers();
			m_categories = new List<Category>();
		}

		/// <summary>
		/// RootDelimiters
		/// </summary>
		[XmlElementAttribute("RootDelimiter", typeof(RootDelimiters))]
		public RootDelimiters RootDelimiter
		{
			set { m_rootDelimiters = value; }
			get { return m_rootDelimiters; }
		}

		/// <summary>
		/// Markers
		/// </summary>
		[XmlElementAttribute("Marker", typeof(Markers))]
		public Markers Marker
		{
			set { m_markers = value; }
			get { return m_markers; }
		}

		/// <summary>
		/// Categories
		/// </summary>
		[XmlElementAttribute("Category", typeof(Category))]
		public List<Category> Categories
		{
			set { m_categories = value; }
			get { return m_categories ; }
		}

		/// <summary>
		/// Serialize.
		/// </summary>
		public void Serialize(string filename)
		{
			try
			{
				XmlSerializer serializer =
					new XmlSerializer(typeof(Parameters));
				TextWriter writer = new StreamWriter(filename);
				serializer.Serialize(writer, this);
				writer.Close();
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// DeSerialize.
		/// </summary>
		/// <param name="parms"></param>
		/// <returns></returns>
		public static Parameters DeSerialize(string parms)
		{
			FileStream parameterReader = null;
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Parameters));

				parameterReader = new FileStream(parms, FileMode.Open);
				if (parameterReader.Length == 0)
					throw new FileLoadException();
				Parameters p = new Parameters();
				return p = (Parameters)serializer.Deserialize(parameterReader);
			}
			finally
			{
				if (parameterReader != null)
					parameterReader.Close();
			}
		}
}


	/// <summary>
	/// RootDelimeter class, open and close.
	/// </summary>
	public class RootDelimiters
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public RootDelimiters()
		{
			OpenDelimiter = '<';
			CloseDelimiter = '>';
		}

		/// <summary>
		/// m_openDelimiter attribute.
		/// </summary>
		private char m_openDelimiter;

		/// <summary>
		/// m_closeDelimiter attribute.
		/// </summary>
		private char m_closeDelimiter;

		/// <summary>
		/// OpenDelimiter attribute
		/// </summary>
		[XmlAttributeAttribute("openDelimiter")]
		public char OpenDelimiter
		{
			set { m_openDelimiter = value; }
			get { return m_openDelimiter; }
		}

		/// <summary>
		/// CloseDelimiter attribute.
		/// </summary>
		[XmlAttributeAttribute("closeDelimiter")]
		public char CloseDelimiter
		{
			set { m_closeDelimiter = value; }
			get { return m_closeDelimiter; }
		}
	}

	/// <summary>
	/// Marker class, ambiguity and decomposition.
	/// </summary>
	public class Markers
	{
		/// <summary>
		/// m_ambiguity member variable.
		/// </summary>
		private char m_ambiguity;

		/// <summary>
		/// m_decomposition member variable.
		/// </summary>
		private char m_decomposition;

		/// <summary>
		/// Constructor
		/// </summary>
		public Markers()
		{
			Ambiguity = '%';
			Decomposition = '-';
		}

		/// <summary>
		/// Ambiguity attribute.
		/// </summary>
		[XmlAttributeAttribute("ambiguity")]
		public char Ambiguity
		{
			set { m_ambiguity = value; }
			get { return m_ambiguity; }
		}

		/// <summary>
		/// Decomposition attribute.
		/// </summary>
		[XmlAttributeAttribute("decomposition")]
		public char Decomposition
		{
			set { m_decomposition = value; }
			get { return m_decomposition; }
		}
	}

	/// <summary>
	/// Category
	/// </summary>
	public class Category
	{
		/// <summary>
		/// Empty Constructor.
		/// </summary>
		public Category()
		{
		}

		/// <summary>
		/// Empty Constructor.
		/// </summary>
		public Category(string c)
		{
			Cat = c;
		}

		/// <summary>
		/// Cat attribute.
		/// </summary>
		[XmlAttributeAttribute("name")]
		public string Cat
		{
			set { m_name = value; }
			get { return m_name; }
		}

		/// <summary>
		/// m_name member variable.
		/// </summary>
		private string m_name;

	}
}
