// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace SILUBS.ScriptureChecks
{
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
}
