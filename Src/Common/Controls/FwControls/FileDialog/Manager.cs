// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Manager class for the OpenFileDialog. Allows to use a different OpenFileDialog during
	/// unit tests.
	/// </summary>
	public static class Manager
	{
		private static Type s_OpenFileDialogType;
		private static Type s_SaveFileDialogType;
		private static Type s_FolderBrowserDialogType;

		static Manager()
		{
			Reset();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the OpenFileDialog type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetOpenFileDialog<T>() where T : IOpenFileDialog
		{
			s_OpenFileDialogType = typeof(T);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the SaveFileDialog type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetSaveFileDialog<T>() where T : ISaveFileDialog
		{
			s_SaveFileDialogType = typeof(T);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the FolderBrowserDialog type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetFolderBrowserDialog<T>() where T : IFolderBrowserDialog
		{
			s_FolderBrowserDialogType = typeof(T);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Resets the dialog types to the default.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void Reset()
		{
			if (Platform.IsWindows)
				ResetWindows();
			else
				ResetLinux();
		}

		// NOTE: leave this as a separate method. Otherwise we need the gtk-sharp assemblies
		// on Windows.
		private static void ResetLinux()
		{
			SetOpenFileDialog<Linux.OpenFileDialogLinux>();
			SetSaveFileDialog<Linux.SaveFileDialogLinux>();
			SetFolderBrowserDialog<Linux.FolderBrowserDialogLinux>();
		}

		private static void ResetWindows()
		{
			SetOpenFileDialog<Windows.OpenFileDialogWindows>();
			SetSaveFileDialog<Windows.SaveFileDialogWindows>();
			SetFolderBrowserDialog<Windows.FolderBrowserDialogWindows>();
		}

		/// <summary>
		/// Creates the open file dialog.
		/// </summary>
		/// <returns>The open file dialog.</returns>
		public static IOpenFileDialog CreateOpenFileDialog()
		{
			return (IOpenFileDialog)Activator.CreateInstance(s_OpenFileDialogType);
		}

		/// <summary>
		/// Creates the save file dialog.
		/// </summary>
		/// <returns>The save file dialog.</returns>
		public static ISaveFileDialog CreateSaveFileDialog()
		{
			return (ISaveFileDialog)Activator.CreateInstance(s_SaveFileDialogType);
		}

		/// <summary>
		/// Creates the folder browser dialog.
		/// </summary>
		/// <returns>The folder browser dialog.</returns>
		public static IFolderBrowserDialog CreateFolderBrowserDialog()
		{
			return (IFolderBrowserDialog)Activator.CreateInstance(s_FolderBrowserDialogType);
		}
	}
}
