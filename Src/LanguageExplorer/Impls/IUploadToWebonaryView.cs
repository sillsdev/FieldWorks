// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Interface for controller to interact with the dialog
	/// </summary>
	public interface IUploadToWebonaryView
	{
		void UpdateStatus(string statusString);
		void SetStatusCondition(WebonaryStatusCondition condition);
		UploadToWebonaryModel Model { get; set; }
	}
}