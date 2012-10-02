// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SCScriptureText.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Cellar;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for SCScriptureText.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SCScriptureText: ISCScriptureText
	{
		/// <summary></summary>
		protected IScrImportSet m_settings;
		/// <summary></summary>
		protected ImportDomain m_domain;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SCScriptureText"/> class.
		/// </summary>
		/// <param name="settings">The import settings</param>
		/// <param name="domain">The source domain</param>
		/// ------------------------------------------------------------------------------------
		public SCScriptureText(IScrImportSet settings, ImportDomain domain)
		{
			Debug.Assert(settings != null);
			m_settings = settings;
			m_domain = domain;
		}

		#region ISCScriptureText Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a text segment enumerator for importing a project
		/// </summary>
		/// <param name="firstRef">start reference for importing</param>
		/// <param name="lastRef">end reference for importing</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ISCTextEnum TextEnum(BCVRef firstRef, BCVRef lastRef)
		{
			// get the enumerator that will return text segments
			return new SCTextEnum(m_settings, m_domain, firstRef, lastRef);
		}
		#endregion
	}
}
