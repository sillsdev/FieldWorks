// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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