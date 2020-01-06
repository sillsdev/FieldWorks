// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks
{
	/// <summary>Things this dialog can wait for</summary>
	public enum WaitFor
	{
		/// <summary>Application currently has no windows open because it's doing something.
		/// This dialog will wait until one is created.</summary>
		WindowToActivate,
		/// <summary>Other application has a data update in progress.
		/// This dialog will wait until it is finished.</summary>
		OtherBusyApp,
		/// <summary>Other application has a modal dialog or message box open.
		/// This dialog will wait until all open modal dialogs are closed.</summary>
		ModalDialogsToClose,
	}
}