// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermRenderingInfo.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class KeyTermRenderingInfo
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to hold information about how to select the best rendering for a key
	/// term (supports XML serialization)
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[XmlType("KeyTermRenderingInfo")]
	public class KeyTermRenderingInfo
	{
		#region XML attributes
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("id")]
		public string TermId { get; set; }

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the original phrase.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlAttribute("default")]
		public string PreferredRendering { get; set; }
		#endregion

		#region XML elements
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of additional renderings (i.e., not supplied from external source)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("AdditionalRenderings")]
		public List<string> AddlRenderings = new List<string>();
		#endregion

		#region Constructors
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRenderingInfo"/> class, needed
		/// for XML serialization.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public KeyTermRenderingInfo()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRenderingInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public KeyTermRenderingInfo(string termId, string bestRendering)
		{
			TermId = termId;
			PreferredRendering = bestRendering;
		}
		#endregion
	}
	#endregion
}