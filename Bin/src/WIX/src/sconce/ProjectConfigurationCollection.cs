//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectConfigurationCollection.cs" company="Microsoft">
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
// Collection class for ProjectConfiguration objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Globalization;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Collection class for <see cref="ProjectConfiguration"/> objects.
	/// </summary>
	public sealed class ProjectConfigurationCollection : SortedCollection
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ProjectConfigurationCollection);

		private Project project;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectConfigurationCollection"/> class.
		/// </summary>
		/// <param name="project">The project that owns this collection (indirectly through the <see cref="ConfigurationProvider"/>.</param>
		public ProjectConfigurationCollection(Project project)
			: base(new ProjectConfigurationComparer())
		{
			this.project = project;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the current project configuration from the environment.
		/// </summary>
		public ProjectConfiguration Current
		{
			get
			{
				// Get the build manager from VS
				IVsSolutionBuildManager solutionBuildMgr = this.Project.ServiceProvider.GetServiceOrThrow(typeof(SVsSolutionBuildManager), typeof(IVsSolutionBuildManager), classType, "Current") as IVsSolutionBuildManager;

				// Get the active project configuration
				IVsProjectCfg[] current = new IVsProjectCfg[1];
				NativeMethods.ThrowOnFailure(solutionBuildMgr.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, this.Project, current));

				return current[0] as ProjectConfiguration;
			}
		}

		/// <summary>
		/// Gets the project that owns this collection.
		/// </summary>
		public Project Project
		{
			get { return this.project; }
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		public ProjectConfiguration this[int index]
		{
			get { return (ProjectConfiguration)this.InnerList[index]; }
		}

		public ProjectConfiguration this[string name]
		{
			get
			{
				Tracer.VerifyStringArgument(name, "name");
				int index = this.IndexOf(name);
				if (index >= 0)
				{
					return this[index];
				}
				return null;
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Adds a <see cref="ProjectConfiguration"/> to the collection.
		/// </summary>
		/// <param name="configuration">The <see cref="ProjectConfiguration"/> to add to the collection.</param>
		public void Add(ProjectConfiguration configuration)
		{
			Tracer.VerifyNonNullArgument(configuration, "configuration");

			// TODO: Finish
			this.InnerList.Add(configuration);
		}

		/// <summary>
		/// Clones the collection by performing a deep copy if the elements implement <see cref="ICloneable"/>,
		/// otherwise a shallow copy is performed.
		/// </summary>
		/// <returns>A clone of this object.</returns>
		public override object Clone()
		{
			ProjectConfigurationCollection clone = new ProjectConfigurationCollection(this.Project);
			this.CloneInto(clone);
			return clone;
		}

		/// <summary>
		/// Returns a value indicating whether there exists at least one <see cref="ProjectConfiguration"/>
		/// object with the given name in the collection.
		/// </summary>
		/// <param name="name">The name of the configuration (Debug or Release, for example).</param>
		/// <returns>true if there is at least one <see cref="ProjectConfiguration"/> in the collection
		/// with the specified name.</returns>
		public bool Contains(string name)
		{
			return (this.IndexOf(name) >= 0);
		}

		public int IndexOf(string name)
		{
			// Do a liner search. We could create a new ProjectConfiguration object and then call
			// InnerList.IndexOf(config), but that means we'd have to have the parent Project in
			// this collection. There will never be more than a few (Debug and Release are the default)
			// configurations anyway, so performance is not an issue.
			for (int i = 0; i < this.Count; i++)
			{
				ProjectConfiguration config = this[i];
				if (String.Equals(name, config.Name, StringComparison.CurrentCultureIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}

		public void Remove(string name)
		{
			Tracer.VerifyStringArgument(name, "name");
			int index = this.IndexOf(name);
			if (index >= 0)
			{
				this.RemoveAt(index);
			}
		}

		protected override void ValidateType(object value)
		{
			base.ValidateType(value);
			if (!(value is ProjectConfiguration))
			{
				throw new ArgumentException("Value must be of type ProjectConfiguration.", "value");
			}
		}
		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Compares <see cref="ProjectConfiguration"/> objects by their Name in a case-insensitive manner.
		/// </summary>
		private sealed class ProjectConfigurationComparer : IComparer
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ProjectConfigurationComparer"/> class.
			/// </summary>
			public ProjectConfigurationComparer()
			{
			}

			/// <summary>
			/// Compares two <see cref="ProjectConfiguration"/> objects.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>A negative number if <paramref name="x"/> &lt; <paramref name="y"/>; a positive
			/// number if <paramref name="x"/> &gt; <paramref name="y"/>; zero if <paramref name="x"/>
			/// is equal to <paramref name="y"/>.</returns>
			public int Compare(object x, object y)
			{
				// Null checks
				if (x == null && y == null)
				{
					return 0;
				}

				if (x != null && y == null)
				{
					return -1;
				}

				if (x == null && y != null)
				{
					return 1;
				}

				// Cast and compare.
				ProjectConfiguration config1 = (ProjectConfiguration)x;
				ProjectConfiguration config2 = (ProjectConfiguration)y;
				return String.Compare(config1.Name, config2.Name, StringComparison.CurrentCultureIgnoreCase);
			}
		}
		#endregion
	}
}