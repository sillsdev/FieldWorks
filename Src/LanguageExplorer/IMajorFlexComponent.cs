// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for major FLEx components
	/// </summary>
	internal interface IMajorFlexComponent : IFlexComponent
	{
		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to another component.
		/// </remarks>
		void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters);

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		void Activate(MajorFlexComponentParameters majorFlexComponentParameters);

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		void PrepareToRefresh();

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		void FinishRefresh();

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		void EnsurePropertiesAreCurrent();
	}
}