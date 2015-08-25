// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IRootSiteGroup.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface wraps one or more slave root sites. It allows the distribution of
	/// messages to the correct root site and synchronizes its slave root boxes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IRootSiteGroup
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add another slave to the synchronization group.
		/// Note that it is usually also necessary to add it to the Controls collection.
		/// That isn't done here to give the client more control over when it is done.
		/// </summary>
		/// <param name="rootsite"></param>
		/// ------------------------------------------------------------------------------------
		void AddToSyncGroup(IRootSiteSlave rootsite);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets which slave rootsite is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active rootsite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSite FocusedRootSite { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See RootSite.InvalidateForLazyFix for explanation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InvalidateForLazyFix();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the member of the rootsite group that controls scrolling (i.e. the one
		/// with the vertical scroll bar).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSiteSlave ScrollingController { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Controls whether size change suppression is in effect.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool SizeChangedSuppression { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the slaves in this group
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<IRootSiteSlave> Slaves { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		/// <param name="rootb">The root box.</param>
		/// ------------------------------------------------------------------------------------
		void Synchronize(IVwRootBox rootb);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object that synchronizes all the root boxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IVwSynchronizer Synchronizer { get; }
	}
}
