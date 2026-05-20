// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Scripture;

namespace ParatextImport
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
		private IEncodingConvertersProvider m_encodingConverterProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SCScriptureText"/> class.
		/// </summary>
		/// <param name="settings">The import settings</param>
		/// <param name="domain">The source domain</param>
		/// ------------------------------------------------------------------------------------
		public SCScriptureText(IScrImportSet settings, ImportDomain domain)
			: this(settings, domain, null)
		{
		}

		internal SCScriptureText(IScrImportSet settings, ImportDomain domain,
			IEncodingConvertersProvider encodingConverterProvider)
		{
			Debug.Assert(settings != null);
			m_settings = settings;
			m_domain = domain;
			m_encodingConverterProvider = encodingConverterProvider;
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
			return new SCTextEnum(m_settings, m_domain, firstRef, lastRef, m_encodingConverterProvider);
		}
		#endregion
	}
}
