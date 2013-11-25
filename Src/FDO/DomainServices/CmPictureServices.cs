// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CmPictureServices.cs
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Static methods for dealing with CmPictures
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class CmPictureServices
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified text is a (clipboard-style) text representation of
		/// a picture.
		/// </summary>
		/// <param name="textRepOfPicture">Potential text representation of a picture.</param>
		/// <returns>
		/// 	<c>true</c> if the specified text represents a picture; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsTextRepOfPicture(string textRepOfPicture)
		{
			string[] tokens = textRepOfPicture.Split('|');
			return ValidTextRepOfPicture(tokens);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified tokens specify the parameters needed to create a
		/// CmPicture.
		/// </summary>
		/// <param name="tokens">Ordered list of tokens as expected by the
		/// CmPictureFactory.Create method.</param>
		/// ------------------------------------------------------------------------------------
		internal static bool ValidTextRepOfPicture(string[] tokens)
		{
			return tokens.Length >= 9 && tokens[0] == CmPictureTags.kClassName;
		}
	}
}
