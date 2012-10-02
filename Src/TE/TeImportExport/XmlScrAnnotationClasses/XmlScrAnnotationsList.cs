// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlScrAnnotationsList.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Security.Principal;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.Scripture;
using System.IO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	#region Enumerations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum XmlNoteType
	{
		/// <summary></summary>
		Consultant,
		/// <summary></summary>
		Translator,
		/// <summary></summary>
		PreTypesettingCheck,
		/// <summary></summary>
		Unspecified
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a list of PuncPattern objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	//[XmlRoot("oxesa", Namespace = "http://www.wycliffe.net/scrnotes/namespace/version_1.0")]
	[XmlRoot("annotations")]
	public class XmlScrAnnotationsList
	{
		#region Data members
		private FdoCache m_cache;
		private ILgWritingSystemFactory m_lgwsf;
		#endregion

		#region XML attributes
		/// <summary>The ICU locale of the default vernacular writing system</summary>
		[XmlAttribute("vernacular")]
		public string VernacularIcuLocale;

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets or sets the type and version.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//[XmlAttribute("type")]
		//public string TypeAndVersion
		//{
		//    get { return "Wycliffe-1.0"; }
		//    set
		//    {
		//        if (value != "Wycliffe-1.0")
		//            throw new UnrecognizedOxesaVersionException();
		//    }
		//}

		/// <summary>The default language for annotation data (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string DefaultIcuLocale;

		/// <summary>The date and time of the export</summary>
		[XmlAttribute("exported")]
		public DateTime DateTimeExported;

		/// <summary>The name of the FW project</summary>
		[XmlAttribute("project")]
		public string ProjectName;

		/// <summary>Machine name (or in the future, person's name) that produced the OXESA file</summary>
		[XmlAttribute("contributor")]
		public string ExportedBy;
		#endregion

		#region XML elements
		/// <summary>List of Scripture annotations</summary>
		[XmlElement("annotation")]
		public List<XmlScrNote> Annotations = new List<XmlScrNote>();

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlScrAnnotationsList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlScrAnnotationsList()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlScrAnnotationsList"/> class based on
		/// the given collection of Scripture notes.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public XmlScrAnnotationsList(FdoCache cache)
		{
			m_cache = cache;
			m_lgwsf = m_cache.LanguageWritingSystemFactoryAccessor;
			VernacularIcuLocale = m_lgwsf.GetStrFromWs(m_cache.DefaultVernWs);
			DefaultIcuLocale = m_lgwsf.GetStrFromWs(m_cache.DefaultAnalWs);
			DateTimeExported = DateTime.Now;
			ProjectName = m_cache.LangProject.Name.GetAlternative(SpecialWritingSystemCodes.BestAnalysis);
			WindowsIdentity whoami = WindowsIdentity.GetCurrent();
			ExportedBy = whoami.Name.Normalize();
		}

		#endregion

		#region Serialization and Deserialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXESA file.</param>
		/// <returns>A loaded ScrAnnotationsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlScrAnnotationsList LoadFromFile(string filename)
		{
			return LoadFromFile(filename, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXESA file.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <returns>A loaded ScrAnnotationsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlScrAnnotationsList LoadFromFile(string filename, FdoCache cache,
			FwStyleSheet styleSheet)
		{
			Exception e;
			return LoadFromFile(filename, cache, styleSheet, out e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified file.
		/// </summary>
		/// <param name="filename">The name of the OXESA file.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="e">Exception that was encountered or null</param>
		/// <returns>A loaded ScrAnnotationsList</returns>
		/// ------------------------------------------------------------------------------------
		public static XmlScrAnnotationsList LoadFromFile(string filename, FdoCache cache,
			FwStyleSheet styleSheet, out Exception e)
		{
			XmlScrAnnotationsList list =
				XmlSerializationHelper.DeserializeFromFile<XmlScrAnnotationsList>(filename, true, out e);

			if (cache != null && styleSheet != null && list != null)
			{
				try
				{
					StyleProxyListManager.Initialize(styleSheet);
					list.WriteToCache(cache, styleSheet, Path.GetDirectoryName(filename));
				}
				finally
				{
					StyleProxyListManager.Cleanup();
				}
			}

			return (list ?? new XmlScrAnnotationsList());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes to file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns><c>true</c> if successfully serialized; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool SerializeToFile(string filename)
		{
			return XmlSerializationHelper.SerializeToFile(filename, this);
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified annotations.
		/// </summary>
		/// <param name="annotations">The annotations.</param>
		/// ------------------------------------------------------------------------------------
		public void Add(FdoObjectSet<IScrScriptureNote> annotations)
		{
			foreach (IScrScriptureNote ann in annotations)
				Annotations.Add(new XmlScrNote(ann, m_cache.DefaultAnalWs, m_lgwsf));
		}

		#endregion

		#region Methods to write annotations to cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the list of annotations to the specified cache.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="OXESADir">The OXESA directory.</param>
		/// ------------------------------------------------------------------------------------
		protected void WriteToCache(FdoCache cache, FwStyleSheet styleSheet,
			string OXESADir)
		{
			Scripture scr = cache.LangProject.TranslatedScriptureOA as Scripture;

			try
			{
				foreach (XmlScrNote ann in Annotations)
				{
					ScrNoteImportManager.Initialize(scr, ann.BeginScrBCVRef.Book, OXESADir);
					ann.WriteToCache(scr, styleSheet);
				}
			}
			finally
			{
				ScrNoteImportManager.Cleanup();
			}
		}

		#endregion
	}
}
