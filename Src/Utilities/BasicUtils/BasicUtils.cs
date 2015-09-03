// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BasicUtils.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class BasicUtils
	{
		#region Advapi32.dll
		// Requires Windows NT 3.1 or later
		// From http://www.codeproject.com/useritems/processownersid.asp

		private const int TOKEN_QUERY = 0X00000008;

		private const int ERROR_NO_MORE_ITEMS = 259;

		private enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId
		}

		/// <summary>The TOKEN_USER structure identifies the user associated with an access token.</summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct TOKEN_USER
		{
			/// <summary>Specifies a SID_AND_ATTRIBUTES structure representing the user
			/// associated with the access token. There are currently no attributes defined for
			/// user security identifiers (SIDs).</summary>
			public SID_AND_ATTRIBUTES User;
		}

		/// <summary>The SID_AND_ATTRIBUTES structure represents a security identifier (SID) and
		/// its attributes. SIDs are used to uniquely identify users or groups.</summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct SID_AND_ATTRIBUTES
		{
			/// <summary>Pointer to a SID structure. </summary>
			public IntPtr Sid;
			/// <summary>Specifies attributes of the SID. This value contains up to 32 one-bit
			/// flags. Its meaning depends on the definition and use of the SID.</summary>
			public int Attributes;
		}

#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The OpenProcessToken function opens the access token associated with a process.
		/// </summary>
		/// <param name="processHandle">[in] Handle to the process whose access token is opened.
		/// The process must have the PROCESS_QUERY_INFORMATION access permission. </param>
		/// <param name="desiredAccess">[in] Specifies an access mask that specifies the
		/// requested types of access to the access token. These requested access types are
		/// compared with the discretionary access control list (DACL) of the token to determine
		/// which accesses are granted or denied. </param>
		/// <param name="tokenHandle">[out] Pointer to a handle that identifies the newly opened
		/// access token when the function returns. </param>
		/// <returns>If the function succeeds, the return value is <c>true</c>.
		/// If the function fails, the return value is <c>false</c>. To get extended error
		/// information, call GetLastError.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32")]
		private static extern bool OpenProcessToken(
			IntPtr processHandle, // handle to process
			int desiredAccess, // desired access to process
			out IntPtr tokenHandle); // handle to open access token

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetTokenInformation function retrieves a specified type of information about an
		/// access token. The calling process must have appropriate access rights to obtain the
		/// information.
		/// </summary>
		/// <param name="hToken">[in] Handle to an access token from which information is
		/// retrieved. If TokenInformationClass specifies TokenSource, the handle must have
		/// TOKEN_QUERY_SOURCE access. For all other TokenInformationClass values, the handle
		/// must have TOKEN_QUERY access.</param>
		/// <param name="tokenInfoClass">[in] Specifies a value from the TOKEN_INFORMATION_CLASS
		/// enumerated type to identify the type of information the function retrieves.</param>
		/// <param name="tokenInformation">[out] Pointer to a buffer the function fills with the
		/// requested information. The structure put into this buffer depends upon the type of
		/// information specified by the TokenInformationClass parameter (see MSDN).</param>
		/// <param name="tokeInfoLength">[in] Specifies the size, in bytes, of the buffer
		/// pointed to by the TokenInformation parameter. If TokenInformation is <c>null</c>,
		/// this parameter must be zero.</param>
		/// <param name="returnLength"><para>[out] Pointer to a variable that receives the number of
		/// bytes needed for the buffer pointed to by the TokenInformation parameter. If this
		/// value is larger than the value specified in the TokenInformationLength parameter,
		/// the function fails and stores no data in the buffer.</para>
		/// <para>If the value of the TokenInformationClass parameter is TokenDefaultDacl and
		/// the token has no default DACL, the function sets the variable pointed to by
		/// ReturnLength to sizeof(TOKEN_DEFAULT_DACL) and sets the DefaultDacl member of the
		/// TOKEN_DEFAULT_DACL structure to NULL.</para></param>
		/// <returns>If the function succeeds, the return value is <c>true</c>.
		/// If the function fails, the return value is <c>false</c>. To get extended error
		/// information, call GetLastError.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32", CharSet = CharSet.Auto)]
		private static extern bool GetTokenInformation(
			IntPtr hToken,
			TOKEN_INFORMATION_CLASS tokenInfoClass,
			IntPtr tokenInformation,
			int tokeInfoLength,
			out int returnLength);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The CloseHandle function closes an open object handle.
		/// </summary>
		/// <param name="handle">[in] Handle to an open object. This parameter can be a pseudo
		/// handle or INVALID_HANDLE_VALUE. </param>
		/// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("kernel32")]
		private static extern bool CloseHandle(IntPtr handle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The ConvertSidToStringSid function converts a security identifier (SID) to a string
		/// format suitable for display, storage, or transmission.
		/// </summary>
		/// <param name="sid">[in] Pointer to the SID structure to convert.</param>
		/// <param name="stringSid">[out] The SID string.</param>
		/// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32", CharSet = CharSet.Auto)]
		private static extern bool ConvertSidToStringSid(
			IntPtr sid,
			[MarshalAs(UnmanagedType.LPTStr)] out string stringSid);
#endif
		#endregion

		#region Helper methods
#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user SID for the given process token.
		/// </summary>
		/// <param name="hToken">The process token.</param>
		/// <returns>The SID of the user that owns the process, or <c>IntPtr.Zero</c> if we
		/// can't get the user information (e.g. insufficient permissions to retrieve this
		/// information)</returns>
		/// ------------------------------------------------------------------------------------
		private static IntPtr GetSidForProcessToken(IntPtr hToken)
		{
			int bufferLen = 256;
			IntPtr buffer = Marshal.AllocHGlobal(bufferLen);

			try
			{
				int returnLen;
				if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, buffer,
					bufferLen, out returnLen))
				{
					TOKEN_USER tokUser;
					tokUser = (TOKEN_USER)Marshal.PtrToStructure(buffer, typeof(TOKEN_USER));
					return tokUser.User.Sid;
				}
				return IntPtr.Zero;
			}
			catch (Exception ex)
			{
				// Just ignore exceptions and return false
				Debug.WriteLine("ProcessTokenToSid failed: " + ex.Message);
				return IntPtr.Zero;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}
#endif
		#endregion

		/// <summary>
		/// Return true if the application is in the process of shutting down after a crash.
		/// Some settings should not be saved in this case.
		/// </summary>
		static public bool InCrashedState { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user for the given process.
		/// </summary>
		/// <param name="process">The process.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetUserForProcess(Process process)
		{
			try
			{
#if !__MonoCS__
				IntPtr procToken;
				string sidString = null;
				if (OpenProcessToken(process.Handle, TOKEN_QUERY, out procToken))
				{
					IntPtr sid = GetSidForProcessToken(procToken);
					if (sid != IntPtr.Zero)
						ConvertSidToStringSid(sid, out sidString);

					CloseHandle(procToken);
				}
				return sidString;
#else
				return process.StartInfo.UserName;
#endif
			}
			catch (Exception ex)
			{
				Debug.WriteLine("GetUserForProcess failed: " + ex.Message);
				return "";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the name of the TestLangProj database.
		/// This is to allow multiple users to test with same database server and different
		/// named TestLangProj files.
		/// </summary>
		/// <returns>TestLangProj name</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetTestLangProjDataBaseName()
		{
			string dbName = Environment.GetEnvironmentVariable("TE_DATABASE");
			if (String.IsNullOrEmpty(dbName))
				return "TestLangProj";
			return dbName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the exception <paramref name="e"/> is caused by an unsupported
		/// culture, i.e. a culture that neither Windows nor .NET support and there is no
		/// custom culture (see LT-8248).
		/// </summary>
		/// <param name="e">The exception.</param>
		/// <returns><c>true</c> if the exception is caused by an unsupported culture;
		/// otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsUnsupportedCultureException(Exception e)
		{
			return e is CultureNotFoundException;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a datetime value with the seconds and milliseconds stripped off (does not
		/// actually round to the nearest minute).
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public static DateTime ToTheMinute(this DateTime value)
		{
			return (value.Second != 0 || value.Millisecond != 0) ?
				new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0) :
				value;
		}
	}
}
