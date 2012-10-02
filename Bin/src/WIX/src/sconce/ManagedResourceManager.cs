//--------------------------------------------------------------------------------------------------
// <copyright file="ManagedResourceManager.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains the ManagedResourceManager class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.Resources;

	/// <summary>
	/// Contains utility functions for retrieving resources from the managed satellite assemblies.
	/// </summary>
	public class ManagedResourceManager
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ManagedResourceManager);
		private static readonly ResourceManager thisAssemblyManager = new ResourceManager(typeof(ManagedResourceManager).Namespace + ".Strings", typeof(ManagedResourceManager).Assembly);

		private string missingManifestString;
		private ResourceManager manager;
		private bool isMissingManifest;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagedResourceManager"/> class.
		/// </summary>
		public ManagedResourceManager()
			: this(thisAssemblyManager)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManagedResourceManager"/> class.
		/// </summary>
		/// <param name="resourceManager">
		/// The manager to use for retrieving resources. Allows subclasses in other assemblies to
		/// use the same infrastructure, but provide their own <see cref="ResourceManager"/>.
		/// </param>
		protected ManagedResourceManager(ResourceManager resourceManager)
		{
			this.manager = resourceManager;
			this.missingManifestString = "Cannot find the resources '" + resourceManager.BaseName + "' in assembly.";
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating whether the specified name is a defined string resource name.
		/// </summary>
		/// <param name="name">The resource identifier to check.</param>
		/// <returns>true if the string identifier is defined in our assembly; otherwise, false.</returns>
		public virtual bool IsStringDefined(string name)
		{
			return SconceStrings.IsValidStringName(name);
		}

		/// <summary>
		/// Gets an unformatted string from the resource file.
		/// </summary>
		/// <param name="name">The resource identifier of the string to retrieve.</param>
		/// <returns>An unformatted string from the resource file.</returns>
		public string GetString(string name)
		{
			return this.GetString(name, null);
		}

		/// <summary>
		/// Gets a string from the resource file and formats it using the specified arguments.
		/// </summary>
		/// <param name="name">The resource identifier of the string to retrieve.</param>
		/// <param name="args">An array of objects to use in the formatting. Can be null or empty.</param>
		/// <returns>A formatted string from the resource file.</returns>
		public string GetString(string name, params object[] args)
		{
			string returnString = this.missingManifestString;

			// If we previously tried to get a string from the resource file and we had a
			// MissingManifestResourceException, then we don't want to try again. Exceptions
			// are expensive especially when we could be getting lots of strings.
			if (!this.isMissingManifest)
			{
				try
				{
					// First give the subclass a chance to retrieve the string
					if (this.IsStringDefined(name))
					{
						returnString = this.manager.GetString(name, CultureInfo.CurrentUICulture);
					}
					//\ Try getting the string from our assembly if the subclass can't handle it
					else if (SconceStrings.IsValidStringName(name))
					{
						returnString = thisAssemblyManager.GetString(name, CultureInfo.CurrentUICulture);
					}
					else
					{
						Tracer.WriteLineWarning(classType, "GetString", "The string id '{0}' is not defined.", name);
						returnString = name;
					}

					// Format the message if there are arguments specified. Note that we format
					// using the CurrentCulture and not the CurrentUICulture (although in almost all
					// cases it will be the same).
					if (returnString != null && args != null && args.Length > 0)
					{
						returnString = String.Format(CultureInfo.CurrentCulture, returnString, args);
					}
				}
				catch (MissingManifestResourceException e)
				{
					this.isMissingManifest = true;
					Tracer.Fail("The resource cannot be found in the assembly: {0}", e);
				}
			}

			return returnString;
		}
		#endregion
	}
}