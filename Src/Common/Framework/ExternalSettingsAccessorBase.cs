// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExternalSettingsAccessorBase.cs
// Responsibility: FieldWorks Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls; // for ProgressDialogWithTask
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FieldWorks classes that process external "resource" files to load
	/// factory settings into databases.
	/// </summary>
	/// <typeparam name="T">The type of document returned by LoadDoc and accessed by GetVersion
	/// (e.g. XmlNode or class representing the contents of an XML file</typeparam>
	/// ----------------------------------------------------------------------------------------
	public abstract class ExternalSettingsAccessorBase<T>
	{
#if DEBUG
		private bool m_fVersionUpdated = false;
#endif

		#region Abstract properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The path where the settings file is found.
		/// For example, @"\Translation Editor\Testyles.xml"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string ResourceFilePathFromFwInstall { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name (no path, no extension) of the settings file.
		/// For example, "Testyles"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string ResourceName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DB object which owns the CmResource corresponding to the settings file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract ICmObject ResourceOwner { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid of the property in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract int ResourcesFlid { get; }

		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name (no path) of the settings file. This is the resource name with the
		/// correct file extension appended.
		/// For example, "Testyles.xml"
		/// If the external resource is not an XML file, override this to append an extension
		/// other than ".xml".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		virtual protected string ResourceFileName
		{
			get { return ResourceName + ".xml"; }
		}
		#endregion

		#region Abstract and Virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected abstract Guid GetVersion(T document);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (e.g., create styles or add publication info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="progressDlg">The progress dialog box itself.</param>
		/// <param name="doc">The loaded document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected abstract void ProcessResources(ProgressDialogWithTask dlg,
			IAdvInd4 progressDlg, T doc);
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message)
		{
			ReportInvalidInstallation(message, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// <param name="e">Optional inner exception</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message, Exception e)
		{
#if !DEBUG
			message = ResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
			throw new Exception(message, e);
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the settings file.
		/// </summary>
		/// <returns>The loaded document</returns>
		/// ------------------------------------------------------------------------------------
		protected abstract T LoadDoc();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the stylesheet for the specified object is current.
		/// </summary>
		/// <param name="progressDlg">The progress dialog if one is already up.</param>
		/// ------------------------------------------------------------------------------------
		public void EnsureCurrentResource(IAdvInd4 progressDlg)
		{
			T doc = LoadDoc();
			Guid newVersion = new Guid();
			try
			{
				newVersion = GetVersion(doc);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation(string.Format(
					FrameworkStrings.ksInvalidResourceFileVersion, ResourceFileName), e);
			}

			// Get the current version of the settings used in this project.
			CmResource resource = CmResource.GetResource(ResourceOwner.Cache, ResourceOwner.Hvo,
				ResourcesFlid, ResourceName);

			// Re-load the factory settings if they are not at current version.
			if (resource == null || newVersion != resource.Version)
			{
				using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(
					Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null))
				{
					ProcessResources(dlg, progressDlg, doc);
#if DEBUG
					Debug.Assert(m_fVersionUpdated);
#endif
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the new resource version in the DB (if the owner has such a property).
		/// </summary>
		/// <param name="newVersion">The new version.</param>
		/// ------------------------------------------------------------------------------------
		protected void SetNewResourceVersion(Guid newVersion)
		{
			if (ResourcesFlid != 0)
			{
				CmResource.SetResource(ResourceOwner.Cache, ResourceOwner.Hvo,
					ResourcesFlid, ResourceName, newVersion);
			}
#if DEBUG
			m_fVersionUpdated = true;
#endif
		}
		#endregion
	}
}
