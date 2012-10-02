// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Utils.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Management;
using System.Globalization;
using System.Threading;
using Microsoft.Win32;
using System.Drawing;
using System.Reflection;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for Utils.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class MiscUtils
	{

		/// <summary>
		/// Universal points-per-inch factor
		/// </summary>
		public const int kdzmpInch = 72000;

		private static readonly Regex kXmlCharEntity = new Regex(@"&#x([0-9a-f]{1,4});", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the string to pass as ObjData property
		/// </summary>
		/// <param name="guid">GUID of the data</param>
		/// <param name="objectDataType">Type of object (e.g. kodtNameGuidHot).</param>
		/// <returns>byte array</returns>
		/// ------------------------------------------------------------------------------------
		public static byte[] GetObjData(Guid guid, byte objectDataType)
		{
			byte[] rgGuid = (byte[])guid.ToByteArray();
			byte[] rgRet = new byte[rgGuid.Length + 2];
			rgRet[0] = objectDataType;
			rgRet[1] = 0;
			rgGuid.CopyTo(rgRet, 2);

			return rgRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a guid from a string.
		/// </summary>
		/// <param name="sGuid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromObjData(string sGuid)
		{
			char[] guidChars = sGuid.ToCharArray();

			int a = ((int)guidChars[1] << 16) | (int)guidChars[0];
			short b = (short)guidChars[2];
			short c = (short)guidChars[3];
			byte[] d = new byte[8];

			d[0] = (byte)((short)guidChars[4] & 0x00FF);
			d[1] = (byte)((short)guidChars[4] >> 8);

			d[2] = (byte)((short)guidChars[5] & 0x00FF);
			d[3] = (byte)((short)guidChars[5] >> 8);

			d[4] = (byte)((short)guidChars[6] & 0x00FF);
			d[5] = (byte)((short)guidChars[6] >> 8);

			d[7] = (byte)((short)guidChars[7] >> 8);
			d[6] = (byte)((short)guidChars[7] & 0x00FF);

			return new Guid(a, b, c, d);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string from a guid that is the binary representation of the guid
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetObjDataFromGuid(Guid guid)
		{
			byte[] rgGuid = guid.ToByteArray();
			char[] guidChars = new char[8];
			for (int i = 0; i < 8; i++)
				guidChars[i] = (char)(rgGuid[i * 2 + 1] << 8 | rgGuid[i * 2]);

			return new string(guidChars);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the local server name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string LocalServerName
		{
			get {return Environment.MachineName + "\\SILFW";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given server is the local server.
		/// </summary>
		/// <param name="server">The server name.</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsServerLocal(string server)
		{
			return (server.IndexOf(LocalServerName, StringComparison.InvariantCultureIgnoreCase) == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if the current user has admin priviledges.  This will return 'true' if
		/// they are an administator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsUserAdmin
		{
			get
			{
				using (WindowsIdentity idWindow = WindowsIdentity.GetCurrent())
				{
					WindowsPrincipal winPrince = new WindowsPrincipal(idWindow);
					return winPrince.IsInRole(WindowsBuiltInRole.Administrator);
				}
			}
		}

		/// During tests ITextDll will call this on dev machines before TE is built.
		static int m_fIsTEInstalled = -1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether TE is installed or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsTEInstalled
		{
			get
			{
				// If we are in a test environment, we don't want to test for TE.
				if (m_fIsTEInstalled != -1)
					return Convert.ToBoolean(m_fIsTEInstalled);

				bool fTEIsInstalled = false;
				// 1) First test to see if we can find the program installed.
				// HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks\CoreInstallation
				// TE=Translation Editor
				RegistryKey key = DirectoryFinder.FieldWorksLocalMachineRegistryKey;
				if (key != null)
				{
					RegistryKey installationKey = key.OpenSubKey("CoreInstallation");
					if (installationKey != null)
					{
						// we found the installation key, so enable the display iff we find TE is installed.
						string value = (string)installationKey.GetValue("TE");
						fTEIsInstalled = !String.IsNullOrEmpty(value);
						return fTEIsInstalled;
					}
				}

				// 2) We didn't find an installation key (e.g. on dev machine?),
				// so see if we can find the program in the installation directory.
				// Path of the current executing assembly without the "file://"
				string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
				string file = Path.Combine(workingDirectory, "TE.exe");
				fTEIsInstalled = File.Exists(file);
				return fTEIsInstalled;
			}

			set { m_fIsTEInstalled = Convert.ToInt32(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>When passed as a parameter to <see cref="FilterForFileName"/>, this
		/// determines how rigorous filtering is to be.</summary>
		/// ------------------------------------------------------------------------------------
		public enum FilenameFilterStrength
		{
			/// <summary> changes only chars that Windows prohibits in file names </summary>
			kFilterBackup,
			/// <summary> changes a few more chars, as needed for MSDE and/or SQL </summary>
			kFilterMSDE,
			/// <summary> changes even more chars for creating project names </summary>
			kFilterProjName,
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Produce a version of the given name that can be used as a file name. This is done
		/// by replacing invalid characters with underscores '_'. In some cases MSDE and/or
		/// SQL have more stringent requirements for database names.
		/// </summary>
		/// <param name="sName">Name to be filtered</param>
		/// <param name="strength">How rigorous filtering is to be</param>
		/// <returns>the filtered name</returns>
		/// ------------------------------------------------------------------------------------
		public static string FilterForFileName(string sName, FilenameFilterStrength strength)
		{
			StringBuilder cleanName = new StringBuilder(sName);
			string invalidChars = GetInvalidProjectNameChars(strength);

			// replace all invalid characters with an '_'
			for (int i = 0; i < sName.Length; i++)
			{
				if (invalidChars.IndexOf(sName[i]) >= 0 || sName[i] < ' ') // eliminate all control characters too
					cleanName[i] = '_';
			}
			return cleanName.ToString();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of characters that are not allowed for project names.
		/// Note that currently the kFilterProjName set is duplicated in AfDbApp::FilterForFileName.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetInvalidProjectNameChars(FilenameFilterStrength strength)
		{
			int strengthInt = (int)strength;
			string invalidChars = string.Empty;

			switch (strengthInt)
			{
				case (int)FilenameFilterStrength.kFilterProjName:
					// This is to avoid problems with restoring from backup.
					invalidChars += "()";
					goto case (int)(FilenameFilterStrength.kFilterMSDE);
				case (int)FilenameFilterStrength.kFilterMSDE:
					// In some MSDE SQL commands, we have used single quotes or square brackets to
					//   delimit a multi-word, Unicode database name.
					//   REVIEW: Perhaps a creative SQL guru can reduce these restrictions.
					invalidChars += "[];";
					goto case (int)(FilenameFilterStrength.kFilterBackup);
				case (int)FilenameFilterStrength.kFilterBackup:
				default:
					invalidChars += new string(Path.GetInvalidFileNameChars());
					break;
			}

			return invalidChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the folder, removing the file name.
		/// </summary>
		/// <param name="fileSpec">The full file specification</param>
		/// <returns>string containing the name of the folder or string.Empty if it does not
		/// exist</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetFolderName(string fileSpec)
		{
			try
			{
				if (Directory.Exists(fileSpec))
					return fileSpec; // fileSpec is a valid folder that does not contain a file name.

				string directoryName = Path.GetDirectoryName(fileSpec);
				if (Directory.Exists(directoryName))
					return directoryName;
			}
			catch
			{
				// Ignore any errors we get and just return string.Empty below
			}
			return string.Empty; // unable to determine valid folder name.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the high-order word
		/// </summary>
		/// <param name="wParam"></param>
		/// <returns>High-order word</returns>
		/// ------------------------------------------------------------------------------------
		public static int HiWord(IntPtr wParam)
		{
			return (int)(wParam.ToInt32() & 0xFFFF0000) / 0x10000;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the low-order word
		/// </summary>
		/// <param name="wParam"></param>
		/// <returns>Low-order word</returns>
		/// ------------------------------------------------------------------------------------
		public static int LoWord(IntPtr wParam)
		{
			return (int)(wParam.ToInt32() & 0x0000FFFF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Translate mouse buttons received from a Win API mouse message to .NET mouse buttons
		/// </summary>
		/// <param name="winMouseButtons">Windows mouse buttons</param>
		/// <returns>.NET mouse buttons</returns>
		/// ------------------------------------------------------------------------------------
		public static MouseButtons TranslateMouseButtons(Win32.MouseButtons winMouseButtons)
		{
			MouseButtons mouseButton = MouseButtons.None;
			if ((winMouseButtons & Win32.MouseButtons.MK_LBUTTON) == Win32.MouseButtons.MK_LBUTTON)
				mouseButton |= MouseButtons.Left;
			if ((winMouseButtons & Win32.MouseButtons.MK_RBUTTON) == Win32.MouseButtons.MK_RBUTTON)
				mouseButton |= MouseButtons.Right;
			if ((winMouseButtons & Win32.MouseButtons.MK_MBUTTON) == Win32.MouseButtons.MK_MBUTTON)
				mouseButton |= MouseButtons.Middle;
			if ((winMouseButtons & Win32.MouseButtons.MK_XBUTTON1) == Win32.MouseButtons.MK_XBUTTON1)
				mouseButton |= MouseButtons.XButton1;
			if ((winMouseButtons & Win32.MouseButtons.MK_XBUTTON2) == Win32.MouseButtons.MK_XBUTTON2)
				mouseButton |= MouseButtons.XButton2;

			return mouseButton;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Play a sound or beep to indicate an error condition.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ErrorBeep()
		{
			System.Media.SystemSounds.Beep.Play();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the physical memory in bytes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ulong GetPhysicalMemoryBytes()
		{
			EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
			ManagementObjectHelper helper = new ManagementObjectHelper(waitHandle);
			ThreadPool.QueueUserWorkItem(new WaitCallback(helper.GetPhysicalMemoryBytes));
			waitHandle.WaitOne();
			return helper.Memory;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the disk drive statistics.
		/// </summary>
		/// <param name="size"></param>
		/// <param name="free"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int GetDiskDriveStats(out ulong size, out ulong free)
		{
			EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
			ManagementObjectHelper helper = new ManagementObjectHelper(waitHandle);
			ThreadPool.QueueUserWorkItem(new WaitCallback(helper.GetAvailableDiskMemory));
			waitHandle.WaitOne();
			size = helper.DiskSize;
			free = helper.DiskFree;
			return helper.DriveCount;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a string in the form "{X=l,Y=t,Width=w,Height=h}" (where l, t, w, and h are
		/// X, Y, width and height values) and returns a corresponding rectangle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Rectangle GetRcFromString(string str)
		{
			str = str.Replace("{", string.Empty);
			str = str.Replace("}", string.Empty);
			str = str.Replace("X=", string.Empty);
			str = str.Replace("Y=", string.Empty);
			str = str.Replace("Width=", string.Empty);
			str = str.Replace("Height=", string.Empty);

			string[] strVals = str.Split(",".ToCharArray(), 4);
			Rectangle rc = Rectangle.Empty;

			if (strVals != null)
			{
				int val;
				if (strVals.Length > 0 && int.TryParse(strVals[0], out val))
					rc.X = val;
				if (strVals.Length > 1 && int.TryParse(strVals[1], out val))
					rc.Y = val;
				if (strVals.Length > 2 && int.TryParse(strVals[2], out val))
					rc.Width = val;
				if (strVals.Length > 3 && int.TryParse(strVals[3], out val))
					rc.Height = val;
			}

			return rc;
		}

		#region ManagementObjectHelper class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For some reason the class ManagementObjectSearcher doesn't work to well on a STA
		/// thread: it doesn't release all its resources in the Dispose() method. This causes
		/// some tests to fail. Therefore we use ManagementObjectSearcher on a separate (MTA)
		/// thread.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ManagementObjectHelper
		{
			private ulong m_Memory;
			private ulong m_DiskFree = 0;
			private ulong m_DiskSize = 0;
			private int m_DriveCount = 0;
			private EventWaitHandle m_waitHandle;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:Temp"/> class.
			/// </summary>
			/// <param name="waitHandle">The wait handle.</param>
			/// --------------------------------------------------------------------------------
			public ManagementObjectHelper(EventWaitHandle waitHandle)
			{
				m_waitHandle = waitHandle;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the memory.
			/// </summary>
			/// <value>The memory.</value>
			/// --------------------------------------------------------------------------------
			public ulong Memory
			{
				get { return m_Memory; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the physical memory in bytes.
			/// </summary>
			/// <param name="stateInfo">The state info.</param>
			/// --------------------------------------------------------------------------------
			public void GetPhysicalMemoryBytes(object stateInfo)
			{
				m_Memory = 0;
				using (ManagementObjectSearcher searcher =
					new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
				{
					foreach (ManagementObject mem in searcher.Get())
					{
						m_Memory += (ulong)mem.GetPropertyValue("Capacity");
						mem.Dispose();
					}
				}

				m_waitHandle.Set();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the number of local disk drives.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int DriveCount
			{
				get { return m_DriveCount; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the total size of all local disk drives.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ulong DiskSize
			{
				get { return m_DiskSize; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the total available free space of all local disk drives.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ulong DiskFree
			{
				get { return m_DiskFree; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Get the number of disks, the total size, and the free space available.
			/// </summary>
			/// <param name="stateInfo"></param>
			/// --------------------------------------------------------------------------------
			public void GetAvailableDiskMemory(object stateInfo)
			{
				using (ManagementObjectSearcher searcher =
					new ManagementObjectSearcher("select * from Win32_LogicalDisk"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						uint type = (uint)mo.GetPropertyValue("DriveType");
						if (type == 3)
						{
							// for an offline drive WMI returns null
							object obj = mo.GetPropertyValue("FreeSpace");
							if (obj != null)
							{
								m_DiskFree += (ulong)obj;
								m_DiskSize += (ulong)mo.GetPropertyValue("Size");
								++m_DriveCount;
							}
						}
						mo.Dispose();
					}
				}
				m_waitHandle.Set();
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since String.IndexOfAny does not have an override which takes a string array as its
		/// first input parameter, we provide this functionality here.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="rgs"></param>
		/// <param name="iMatched"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int IndexOfAnyString(string s, string[] rgs, out int iMatched)
		{
			return IndexOfAnyString(s, rgs, out iMatched, StringComparison.CurrentCulture);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since String.IndexOfAny does not have an override which takes a string array as its
		/// first input parameter, we provide this functionality here.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="rgs"></param>
		/// <param name="iMatched"></param>
		/// <param name="sc">culture rule to use</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int IndexOfAnyString(string s, string[] rgs, out int iMatched, StringComparison sc)
		{
			iMatched = -1;
			if (s == null || rgs == null || rgs.Length == 0)
				return -1;
			int ichRet = -1;
			for (int i = 0; i < rgs.Length; ++i)
			{
				int ich = s.IndexOf(rgs[i], sc);
				if (ich != -1)
				{
					if (ichRet == -1 || ich < ichRet)
					{
						ichRet = ich;
						iMatched = i;
					}
					else if (ich == ichRet && rgs[i].Length > rgs[iMatched].Length)
					{
						// save the longest match at this location
						iMatched = i;
					}
				}
			}
			return ichRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since String.IndexOfAny does not have an override which takes a string array as its
		/// first input parameter, we provide this functionality here.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="rgs"></param>
		/// <param name="ichStart"></param>
		/// <param name="iMatched"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int IndexOfAnyString(string s, string[] rgs, int ichStart, out int iMatched)
		{
			iMatched = -1;
			if (s == null || rgs == null || rgs.Length == 0)
				return -1;
			int ichRet = -1;
			for (int i = 0; i < rgs.Length; ++i)
			{
				int ich = s.IndexOf(rgs[i], ichStart);
				if (ich != -1)
				{
					if (ichRet == -1 || ich < ichRet)
					{
						ichRet = ich;
						iMatched = i;
					}
					else if (ich == ichRet && rgs[i].Length > rgs[iMatched].Length)
					{
						// save the longest match at this location
						iMatched = i;
					}
				}
			}
			return ichRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extract the language code portion of an ICU Locale string.
		/// </summary>
		/// <param name="sIcuLocale"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ExtractLanguageCode(string sIcuLocale)
		{
			int ich = sIcuLocale.IndexOfAny(new char[] {'_', '-'});
			if (ich >= 0)
				return sIcuLocale.Substring(0, ich);
			else
				return sIcuLocale;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current UI cluture in ICU format (i.e. ID contains underscores). If
		/// the culture is "en_US" then just "en" is returned because, in the context of FW,
		/// they're considered one and the same. (I hope that assumption doesn't come back to
		/// haunt me.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string CurrentUIClutureICU
		{
			get
			{
				CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
				return (ci.Name == "en-US" ? "en" : ci.Name.Replace("-", "_"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current UI cluture in the windows format (i.e. ID contains dashes). If
		/// the culture is "en_US" then "es" is just returned because we don't care about
		/// that distinction. (I hope that assumption doesn't come back to haunt me.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string CurrentUIClutureWindows
		{
			get
			{
				CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
				return (ci.Name == "en-US" ? "en" : ci.Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the closest CultureInfo object for the given writing system.  If none exist,
		/// return null.
		/// </summary>
		/// <param name="sWs"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static CultureInfo GetCultureForWs(string sWs)
		{
			CultureInfo ci = null;
			string sCulture = sWs.Replace('_', '-');
			int idx = sCulture.Length;
			while (ci == null && idx > 0)
			{
				if (idx < sCulture.Length)
					sCulture.Remove(idx);
				try
				{
					ci = new CultureInfo(sCulture);
				}
				catch
				{
					ci = null;
					idx = sCulture.LastIndexOf('-');
				}
			}
			return ci;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given writing system has a valid .Net CultureInfo associated with
		/// it.
		/// </summary>
		/// <param name="sWs"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool WsHasValidCulture(string sWs)
		{
			string sCulture = sWs.Replace('_', '-');
			foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
			{
				if (sCulture == ci.Name)
					return true;
			}
			return false;
		}

		////////////////////////////////////////////////////////////////////////////////////////
		//private static string CultureTypeNames(CultureTypes ct)
		//{
		//    StringBuilder bldr = new StringBuilder();
		//    if (0 != (ct & CultureTypes.NeutralCultures))
		//        bldr.Append("+Neutral");
		//    if (0 != (ct & CultureTypes.SpecificCultures))
		//        bldr.Append("+Specific");
		//    if (0 != (ct & CultureTypes.InstalledWin32Cultures))
		//        bldr.Append("+InstalledWin32");
		//    if (0 != (ct & CultureTypes.UserCustomCulture))
		//        bldr.Append("+UserCustom");
		//    if (0 != (ct & CultureTypes.ReplacementCultures))
		//        bldr.Append("+Replacement");
		//    if (0 != (ct & CultureTypes.FrameworkCultures))
		//        bldr.Append("+Framework");
		//    if (0 != (ct & CultureTypes.WindowsOnlyCultures))
		//        bldr.Append("+WindowsOnly");
		//    return bldr.ToString();
		//}
		////////////////////////////////////////////////////////////////////////////////////////

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the window handle is a child window of the specified parent form.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="hWnd">The window handle.</param>
		/// <returns><c>true</c> if the window is a child window of the parent form; otherwise,
		/// <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsChildWindowOfForm(Form parent, IntPtr hWnd)
		{
			if (parent == null)
				return false;

			// Try to get the managed window for the handle. We will get one only if hWnd is
			// a child window of this application, so we can return true.
			if (Control.FromHandle(hWnd) != null)
				return true;

			// Otherwise hWnd might be the handle of an unmanaged window. We look at all
			// parents and grandparents... to see if we eventually belong to the application
			// window.
			IntPtr hWndParent = hWnd;
			while (hWndParent != IntPtr.Zero)
			{
				hWnd = hWndParent;
				hWndParent = Win32.GetParent(hWnd);
			}
			return (hWnd == parent.Handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes invalid characters as per the XML spec from the specified string,
		/// plus LF and CR.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string CleanupForXmlSpec(string text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				int hexValue = text[i];
				if ((hexValue >= 0x20 || hexValue == 0x9) && hexValue != 0xFFFE && hexValue != 0xFFFF
					&& hexValue != '&')
					continue;

				if (hexValue == '&' && text.Contains("&#x")) // Optimization to check for any XML entities
				{
					Match match = kXmlCharEntity.Match(text);
					Debug.Assert(match.Success);
					if (match.Success)
					{
						Debug.Assert(match.Groups.Count == 2);
						hexValue = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
						// We want to filter out invalid XML characters. LF (0xA) and CR (0xD) are
						// valid but we want to remove those from our text
						if ((hexValue < 0x20 && hexValue != 0x9) || hexValue == 0xFFFE || hexValue == 0xFFFF)
							text = text.Replace(match.Groups[0].Value, string.Empty);
						i -= match.Length;
					}
				}
				else
				{
					text = text.Remove(i, 1);
					i--;
				}
			}
			return text;
		}

	}
}
