// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.Utils.FileDialog
{
	/// <summary>
	/// Manager class for the OpenFileDialog. Allows to use a different OpenFileDialog during
	/// unit tests.
	/// </summary>
	public static class Manager
	{
		private static Type s_OpenFileDialogType;
		private static Type s_SaveFileDialogType;

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

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Resets the dialog types to the default.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void Reset()
		{
#if __MonoCS__
			SetOpenFileDialog<Linux.OpenFileDialogLinux>();
			SetSaveFileDialog<Linux.SaveFileDialogLinux>();
#else
			SetOpenFileDialog<Windows.OpenFileDialogWindows>();
			SetSaveFileDialog<Windows.SaveFileDialogWindows>();
#endif
		}

		public static IOpenFileDialog CreateOpenFileDialog()
		{
			return (IOpenFileDialog)Activator.CreateInstance(s_OpenFileDialogType);
		}

		public static ISaveFileDialog CreateSaveFileDialog()
		{
			return (ISaveFileDialog)Activator.CreateInstance(s_SaveFileDialogType);
		}
	}
}
