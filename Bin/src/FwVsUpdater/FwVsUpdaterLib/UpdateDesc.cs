// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UpdateDesc.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SIL.FieldWorks.DevTools.FwVsUpdater
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class UpdateDesc
	{
		/// <summary></summary>
		[XmlArrayItem(ElementName="file")]
		public UpdateFile[] files;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public struct UpdateFile
	{
		/// <summary></summary>
		[XmlAttribute(AttributeName="name")]
		public string Name;
		/// <summary></summary>
		[XmlAttribute(AttributeName="once")]
		public bool Once;
		/// <summary></summary>
		[XmlAttribute(AttributeName="exitVS")]
		public bool ExitVs;
		/// <summary></summary>
		[XmlAttribute(AttributeName="ignoreFirstTime")]
		public bool IgnoreFirstTime;
	}
}
