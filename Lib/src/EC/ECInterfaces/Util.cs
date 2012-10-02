// Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
using System;

namespace ECInterfaces
{
	public static class Util
	{
		public static bool IsUnix
		{
			get { return Environment.OSVersion.Platform == PlatformID.Unix; }
		}

		private static string s_CommonAppDataFolder;

		/// <summary>
		/// Gets the path for storing common application data that might be shared between
		/// multiple applications and multiple users on the same machine.
		///
		/// On Windows this returns Environment.SpecialFolder.CommonApplicationData
		/// (C:\ProgramData),on Linux /var/lib/fieldworks.
		/// </summary>
		private static string CommonApplicationData
		{
			get
			{
				if (s_CommonAppDataFolder == null)
				{
					if (IsUnix)
					{
						s_CommonAppDataFolder = "/var/lib/fieldworks";
					}
					else
					{
						s_CommonAppDataFolder =
							Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
					}
				}
				return s_CommonAppDataFolder;
			}
		}

		/// <summary>
		/// Gets a special folder, very similar to Environment.GetFolderPath. The main
		/// difference is that this method works cross-platform and does some translations.
		/// For example CommonApplicationData (/usr/share) is not writeable on Linux, so we
		/// translate that to /var/lib/fieldworks instead.
		/// </summary>
		public static string GetSpecialFolderPath(Environment.SpecialFolder folder)
		{
			if (folder == Environment.SpecialFolder.CommonApplicationData)
				return CommonApplicationData;
			return Environment.GetFolderPath(folder);
		}
	}
}
