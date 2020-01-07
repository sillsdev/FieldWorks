// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Interface that a collector environment may implement if it only wants picture paths,
	/// not the pictures themselves.
	/// </summary>
	public interface ICollectPicturePathsOnly
	{
		/// <summary>
		/// This should be called in place of AddPicture.
		/// </summary>
		void APictureIsBeingAdded();
	}
}