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
#if DEBUG
using System.Diagnostics;
#endif
using System.Windows.Forms;
using System.Linq;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
#if !DEBUG
using SIL.FieldWorks.Resources;
#endif

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
		private bool m_fVersionUpdated;
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
		/// Gets the FdoCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract FdoCache Cache { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract IFdoOwningCollection<ICmResource> ResourceList { get; }

		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name (no path) of the settings file. This is the resource name with the
		/// correct file extension appended.
		/// For example, "TeStyles.xml"
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
			IProgress progressDlg, T doc);
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
			Logger.WriteEvent(message); // This is so we get the actual error in release builds
#if !DEBUG
			message = ResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
			throw new InstallationException(message, e);
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
		public void EnsureCurrentResource(IProgress progressDlg)
		{
			var doc = LoadDoc();
			var newVersion = new Guid();
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
			ICmResource resource = (from res in ResourceList.ToArray()
					where res.Name.Equals(ResourceName)
					select res).FirstOrDefault();

			// Re-load the factory settings if they are not at current version.
			if (resource == null || newVersion != resource.Version)
			{
				using (var dlg = new ProgressDialogWithTask(
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
			ICmResource resource = (from res in ResourceList.ToArray()
									where res.Name.Equals(ResourceName)
									select res).FirstOrDefault();
			if (resource == null)
			{
				// Resource does not exist yet. Add it to the collection.
				ICmResource newResource = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
				ResourceList.Add(newResource);
				newResource.Name = ResourceName;
				newResource.Version = newVersion;
#if DEBUG
				m_fVersionUpdated = true;
#endif
				return;
			}

			resource.Version = newVersion;
#if DEBUG
			m_fVersionUpdated = true;
#endif
		}
		#endregion
	}
}
