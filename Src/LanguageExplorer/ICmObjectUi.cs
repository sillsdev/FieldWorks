// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer
{
	internal interface ICmObjectUi
	{
		/// <summary>
		/// Retrieve the CmObject we are providing UI functions for.
		/// </summary>
		ICmObject MyCmObject { get; }

		/// <summary>
		/// Delete the object, after showing a confirmation dialog.
		/// Return true if deleted, false, if cancelled.
		/// </summary>
		bool DeleteUnderlyingObject();

		/// <summary>
		/// Merge the underling objects. This method handles the confirm dialog, then delegates
		/// the actual merge to ReallyMergeUnderlyingObject. If the flag is true, we merge
		/// strings and owned atomic objects; otherwise, we don't change any that aren't null
		/// to begin with.
		/// </summary>
		void MergeUnderlyingObject(bool fLoseNoTextData);

		/// <summary />
		void MoveUnderlyingObjectToCopyOfOwner();
	}
}