// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Interface to add common methods between PhoneEnvReferenceSlice and PhEnvStrRepresentationSlice
	/// </summary>
	internal interface IPhEnvSliceCommon
	{
		/// <summary>
		/// See if an environment error message can be shown.
		/// </summary>
		bool CanShowEnvironmentError { get; }

		/// <summary>
		/// Show an environment error message.
		/// </summary>
		void ShowEnvironmentError();

		/// <summary>
		/// See if a hashmark can be inserted in an environment slice.
		/// </summary>
		bool CanInsertHashMark { get; }

		/// <summary>
		/// Insert a hashmark in an environment slice.
		/// </summary>
		void InsertHashMark();

		/// <summary>
		/// See if an optional item can be inserted in an environment slice.
		/// </summary>
		bool CanInsertOptionalItem { get; }

		/// <summary>
		/// Insert an optional item in an environment slice.
		/// </summary>
		void InsertOptionalItem();

		/// <summary>
		/// See if a natural class can be inserted in an environment slice.
		/// </summary>
		bool CanInsertNaturalClass { get; }

		/// <summary>
		/// Insert a natural class in an environment slice.
		/// </summary>
		void InsertNaturalClass();

		/// <summary>
		/// See if an environment bar can be inserted in an environment slice.
		/// </summary>
		bool CanInsertEnvironmentBar { get; }

		/// <summary>
		/// Insert an environment bar in an environment slice.
		/// </summary>
		void InsertEnvironmentBar();

		/// <summary>
		/// See if a slash can be inserted in an environment.
		/// </summary>
		bool CanInsertSlash { get; }

		/// <summary>
		/// Insert a slash in an environment slice.
		/// </summary>
		void InsertSlash();
	}
}