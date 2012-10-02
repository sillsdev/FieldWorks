//-------------------------------------------------------------------------------------------------
// <copyright file="WixProjectConfiguration.cs" company="Microsoft">
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
// Provides configuration information to the Visual Studio shell about a WiX project.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Globalization;
	using System.IO;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	internal sealed class WixProjectConfiguration : ProjectConfiguration
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixProjectConfiguration);

		private CandleSettings candleSettings = new CandleSettings();
		private LightSettings lightSettings = new LightSettings();
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public WixProjectConfiguration(WixProject project, string name) : base(project, name)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public new WixBuildableProjectConfiguration BuildableProjectConfiguration
		{
			get { return (WixBuildableProjectConfiguration)base.BuildableProjectConfiguration; }
		}

		public CandleSettings CandleSettings
		{
			get { return this.candleSettings; }
		}

		public LightSettings LightSettings
		{
			get { return this.lightSettings; }
		}

		public new WixProject Project
		{
			get { return (WixProject)base.Project; }
			set { base.Project = value; }
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get
			{
				return (base.AreContainedObjectsDirty || this.CandleSettings.IsDirty || this.LightSettings.IsDirty);
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public new WixProjectConfiguration Clone(string newName)
		{
			WixProjectConfiguration clonedCopy = new WixProjectConfiguration(this.Project, newName);
			this.CloneInto(clonedCopy);
			return clonedCopy;
		}

		protected override void CloneInto(ProjectConfiguration clonedCopy)
		{
			Tracer.Assert(clonedCopy is WixProjectConfiguration, "We shouldn't be cloning something we're not.");
			base.CloneInto(clonedCopy);
			WixProjectConfiguration wixClonedCopy = clonedCopy as WixProjectConfiguration;
			if (wixClonedCopy != null)
			{
				wixClonedCopy.candleSettings = (CandleSettings)this.candleSettings.Clone();
				wixClonedCopy.lightSettings = (LightSettings)this.lightSettings.Clone();
			}
		}

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			base.ClearDirtyOnContainedObjects();
			this.CandleSettings.ClearDirty();
			this.LightSettings.ClearDirty();
		}

		protected override BuildableProjectConfiguration CreateBuildableProjectConfiguration()
		{
			return new WixBuildableProjectConfiguration(this);
		}

		/// <summary>
		/// Updates the output file paths for candle and light when the relative file path has changed.
		/// </summary>
		protected override void UpdateOutputFiles()
		{
			WixBuildSettings buildSettings = this.Project.BuildSettings;

			// Change the output paths for candle and light
			string absoluteOutputDirectory = PackageUtility.CanonicalizeDirectoryPath(Path.Combine(this.Project.RootDirectory, this.RelativeOutputDirectory));
			string absoluteIntermediateDirectory = PackageUtility.CanonicalizeDirectoryPath(Path.Combine(this.Project.RootDirectory, this.RelativeIntermediateDirectory));
			string lightFileName = buildSettings.OutputName + buildSettings.OutputExtension;

			this.CandleSettings.AbsoluteOutputDirectory = absoluteIntermediateDirectory;
			this.LightSettings.AbsoluteOutputFilePath = Path.Combine(absoluteOutputDirectory, lightFileName);
		}
		#endregion
	}
}
