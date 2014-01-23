// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: INormalizationCharacterFactory.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows for INormalizationCharacter to be mocked out for tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface INormalizationCharacterFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(IPuaCharacter puaChar, string property);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(string line, string property);
	}
}