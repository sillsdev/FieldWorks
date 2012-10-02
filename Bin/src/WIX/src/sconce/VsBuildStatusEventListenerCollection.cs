//-------------------------------------------------------------------------------------------------
// <copyright file="VsBuildStatusEventListenerCollection.cs" company="Microsoft">
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
// Collection class for IVsBuildStatusCallback event listeners.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using Microsoft.VisualStudio.Shell.Interop;

	public sealed class VsBuildStatusEventListenerCollection : EventListenerCollection
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(VsBuildStatusEventListenerCollection);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public VsBuildStatusEventListenerCollection()
		{
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		public IVsBuildStatusCallback this[int index]
		{
			get { return (IVsBuildStatusCallback)this.GetAt(index); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public uint Add(IVsBuildStatusCallback listener)
		{
			return base.Add(listener);
		}

		/// <summary>
		/// Clones this object by performing a shallow copy of the collection items.
		/// </summary>
		/// <returns>A shallow copy of this object.</returns>
		public override object Clone()
		{
			VsBuildStatusEventListenerCollection clone = new VsBuildStatusEventListenerCollection();
			this.CloneInto(clone);
			return clone;
		}

		/// <summary>
		/// Notifies all of the listeners that a project has begun building.
		/// </summary>
		/// <returns>true if the build should continue; false if one or more of the listeners requested
		/// that the build should be canceled.</returns>
		public bool OnBuildBegin()
		{
			bool continueBuilding = true;

			// Let all of our listeners know that the build has started.
			Tracer.WriteLineInformation(classType, "OnBuildBegin", "Notifying all of our listeners that the build has started.");

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsBuildStatusCallback eventItem in clone)
			{
				try
				{
					int continueFlag = Convert.ToInt32(continueBuilding);
					eventItem.BuildBegin(ref continueFlag);
					if (continueFlag == 0)
					{
						continueBuilding = false;
					}
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnBuildBegin", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}

			return continueBuilding;
		}

		/// <summary>
		/// Notifies all of the listeners that a project has finished building.
		/// </summary>
		/// <param name="success">true if the build operation completed successfully. On an up-to-date check,
		/// <paramref name="success"/> must be set to true when the project configuration is up to date and
		/// false when the project configuration is not up to date.</param>
		public void OnBuildEnd(bool success)
		{
			// Let all of our listeners know that the build has ended.
			if (success)
			{
				Tracer.WriteLineInformation(classType, "OnBuildEnd", "Notifying all of our listeners that the build has ended successfully.");
			}
			else
			{
				Tracer.WriteLineInformation(classType, "OnBuildEnd", "Notifying all of our listeners that the build has ended with errors.");
			}

			int successFlag = Convert.ToInt32(success);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsBuildStatusCallback eventItem in clone)
			{
				try
				{
					eventItem.BuildEnd(successFlag);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnBuildEnd", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the listeners that a build operation is in progress.
		/// </summary>
		/// <returns>true if the build should continue; false if one or more of the listeners requested
		/// that the build should be canceled.</returns>
		public bool OnTick()
		{
			bool continueBuilding = true;

			// Let all of our listeners know that the build has started.
			Tracer.WriteLineVerbose(classType, "OnTick", "Notifying all of our listeners that the build is in progress.");

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsBuildStatusCallback eventItem in clone)
			{
				try
				{
					int continueFlag = Convert.ToInt32(continueBuilding);
					eventItem.Tick(ref continueFlag);
					if (continueFlag == 0)
					{
						continueBuilding = false;
					}
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnTick", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}

			return continueBuilding;
		}
		#endregion
	}
}