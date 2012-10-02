/*
 *
 * This file was taken from http://www.codeproject.com/KB/cs/tsnewlib.aspx
 * ("A New Task Scheduler Class Library for .NET" by Dennis Austin)
 * This file is licensed under The Code Project Open License (CPOL):
 * http://www.codeproject.com/info/cpol10.aspx
 *
 */

using System;
using System.Runtime.InteropServices;

namespace TaskSchedulerInterop {
	#region class HRESULT -- Values peculiar to the task scheduler.
	internal class HResult {
		// The task is ready to run at its next scheduled time.
		public const int SCHED_S_TASK_READY               = 0x00041300;
		// The task is currently running.
		public const int SCHED_S_TASK_RUNNING             = 0x00041301;
		// The task will not run at the scheduled times because it has been disabled.
		public const int SCHED_S_TASK_DISABLED            = 0x00041302;
		// The task has not yet run.
		public const int SCHED_S_TASK_HAS_NOT_RUN         = 0x00041303;
		// There are no more runs scheduled for this task.
		public const int SCHED_S_TASK_NO_MORE_RUNS        = 0x00041304;
		// One or more of the properties that are needed to run this task on a schedule have not been set.
		public const int SCHED_S_TASK_NOT_SCHEDULED       = 0x00041305;
		// The last run of the task was terminated by the user.
		public const int SCHED_S_TASK_TERMINATED          = 0x00041306;
		// Either the task has no triggers or the existing triggers are disabled or not set.
		public const int SCHED_S_TASK_NO_VALID_TRIGGERS   = 0x00041307;
		// Event triggers don'task have set run times.
		public const int SCHED_S_EVENT_TRIGGER            = 0x00041308;
		// Trigger not found.
		public const int SCHED_E_TRIGGER_NOT_FOUND        = unchecked((int)0x80041309);
		// One or more of the properties that are needed to run this task have not been set.
		public const int SCHED_E_TASK_NOT_READY           = unchecked((int)0x8004130A);
		// There is no running instance of the task to terminate.
		public const int SCHED_E_TASK_NOT_RUNNING         = unchecked((int)0x8004130B);
		// The Task Scheduler Service is not installed on this computer.
		public const int SCHED_E_SERVICE_NOT_INSTALLED    = unchecked((int)0x8004130C);
		// The task object could not be opened.
		public const int SCHED_E_CANNOT_OPEN_TASK         = unchecked((int)0x8004130D);
		// The object is either an invalid task object or is not a task object.
		public const int SCHED_E_INVALID_TASK             = unchecked((int)0x8004130E);
		// No account information could be found in the Task Scheduler security database for the task indicated.
		public const int SCHED_E_ACCOUNT_INFORMATION_NOT_SET = unchecked((int)0x8004130F);
		// Unable to establish existence of the account specified.
		public const int SCHED_E_ACCOUNT_NAME_NOT_FOUND   = unchecked((int)0x80041310);
		// Corruption was detected in the Task Scheduler security database; the database has been reset.
		public const int SCHED_E_ACCOUNT_DBASE_CORRUPT    = unchecked((int)0x80041311);
		// Task Scheduler security services are available only on Windows NT.
		public const int SCHED_E_NO_SECURITY_SERVICES     = unchecked((int)0x80041312);
		// The task object version is either unsupported or invalid.
		public const int SCHED_E_UNKNOWN_OBJECT_VERSION   = unchecked((int)0x80041313);
		// The task has been configured with an unsupported combination of account settings and run time options.
		public const int SCHED_E_UNSUPPORTED_ACCOUNT_OPTION = unchecked((int)0x80041314);
		// The Task Scheduler Service is not running.
		public const int SCHED_E_SERVICE_NOT_RUNNING      = unchecked((int)0x80041315);
		// The Task Scheduler service must be configured to run in the System account to function properly.  Individual TaskNames may be configured to run in other accounts.
		public const int SCHED_E_SERVICE_NOT_LOCALSYSTEM  = unchecked((int)0x80041316);
	}
	#endregion

// ------ Types used in in the Task Scheduler Interfaces ------
	internal enum TaskTriggerType {
		TIME_TRIGGER_ONCE            = 0,  // Ignore the Type field.
		TIME_TRIGGER_DAILY           = 1,  // Use DAILY
		TIME_TRIGGER_WEEKLY          = 2,  // Use WEEKLY
		TIME_TRIGGER_MONTHLYDATE     = 3,  // Use MONTHLYDATE
		TIME_TRIGGER_MONTHLYDOW      = 4,  // Use MONTHLYDOW
		EVENT_TRIGGER_ON_IDLE        = 5,  // Ignore the Type field.
		EVENT_TRIGGER_AT_SYSTEMSTART = 6,  // Ignore the Type field.
		EVENT_TRIGGER_AT_LOGON       = 7   // Ignore the Type field.
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Daily {
		public ushort DaysInterval;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Weekly {
		public ushort WeeksInterval;
		public ushort DaysOfTheWeek;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MonthlyDate {
		public uint Days;
		public ushort Months;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MonthlyDOW {
		public ushort WhichWeek;
		public ushort DaysOfTheWeek;
		public ushort Months;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct TriggerTypeData {
		[FieldOffset(0)]
		public Daily daily;
		[FieldOffset(0)]
		public Weekly weekly;
		[FieldOffset(0)]
		public MonthlyDate monthlyDate;
		[FieldOffset(0)]
		public MonthlyDOW monthlyDOW;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TaskTrigger {
		public ushort TriggerSize;             // Structure size.
		public ushort Reserved1;               // Reserved. Must be zero.
		public ushort BeginYear;               // Trigger beginning date year.
		public ushort BeginMonth;              // Trigger beginning date month.
		public ushort BeginDay;                // Trigger beginning date day.
		public ushort EndYear;                 // Optional trigger ending date year.
		public ushort EndMonth;                // Optional trigger ending date month.
		public ushort EndDay;                  // Optional trigger ending date day.
		public ushort StartHour;               // Run bracket start time hour.
		public ushort StartMinute;             // Run bracket start time minute.
		public uint MinutesDuration;           // Duration of run bracket.
		public uint MinutesInterval;           // Run bracket repetition interval.
		public uint Flags;                     // Trigger flags.
		public TaskTriggerType Type;           // Trigger type.
		public TriggerTypeData Data;           // Trigger data peculiar to this type (union).
		public ushort Reserved2;               // Reserved. Must be zero.
		public ushort RandomMinutesInterval;   // Maximum number of random minutes after start time.
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct SystemTime {
		public ushort Year;
		public ushort Month;
		public ushort DayOfWeek;
		public ushort Day;
		public ushort Hour;
		public ushort Minute;
		public ushort Second;
		public ushort Milliseconds;
	}


// ------ Types for calling PropertySheet (comctl32) through PInvoke ------
	[StructLayout(LayoutKind.Sequential)]
	internal struct PropSheetHeader {
		public UInt32 dwSize;
		public UInt32 dwFlags;
		public IntPtr hwndParent;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public String pszCaption;
		public UInt32 nPages;
		public UInt32 nStartPage;
		public IntPtr phpage;
		public IntPtr pfnCallback;
		public IntPtr hbmWatermark;
		public IntPtr hplWatermark;
		public IntPtr hbmHeader;
	}

	[Flags]
	internal enum PropSheetFlags : uint {
		PSH_DEFAULT =            0x00000000,
		PSH_PROPTITLE =          0x00000001,
		PSH_USEHICON =           0x00000002,
		PSH_USEICONID =          0x00000004,
		PSH_PROPSHEETPAGE =      0x00000008,
		PSH_WIZARDHASFINISH =    0x00000010,
		PSH_WIZARD =             0x00000020,
		PSH_USEPSTARTPAGE =      0x00000040,
		PSH_NOAPPLYNOW =         0x00000080,
		PSH_USECALLBACK =        0x00000100,
		PSH_HASHELP =            0x00000200,
		PSH_MODELESS =           0x00000400,
		PSH_RTLREADING =         0x00000800,
		PSH_WIZARDCONTEXTHELP =  0x00001000,
		PSH_WIZARD97 =           0x01000000,
		PSH_WATERMARK =          0x00008000,
		PSH_USEHBMWATERMARK =    0x00010000,  // user pass in a hbmWatermark instead of pszbmWatermark
		PSH_USEHPLWATERMARK =    0x00020000,  //
		PSH_STRETCHWATERMARK =   0x00040000,  // stretchwatermark also applies for the header
		PSH_HEADER =             0x00080000,
		PSH_USEHBMHEADER =       0x00100000,
		PSH_USEPAGELANG =        0x00200000  // use frame dialog template matched to page
	}

	internal class PropertySheetDisplay {
		//Display a property sheet
		[DllImport("comctl32.dll")]
		public static extern int PropertySheet([In, MarshalAs(UnmanagedType.Struct)] ref PropSheetHeader psh);
	}


// ----- Interfaces -----
	[Guid("148BD527-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ITaskScheduler {
		void SetTargetComputer([In, MarshalAs(UnmanagedType.LPWStr)] string Computer);
		void GetTargetComputer(out System.IntPtr Computer);
		void Enum([Out, MarshalAs(UnmanagedType.Interface)] out IEnumWorkItems EnumWorkItems);
		void Activate([In, MarshalAs(UnmanagedType.LPWStr)] string Name, [In] ref System.Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object obj);
		void Delete([In, MarshalAs(UnmanagedType.LPWStr)] string Name);
		void NewWorkItem([In, MarshalAs(UnmanagedType.LPWStr)] string TaskName, [In] ref System.Guid rclsid, [In] ref System.Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object obj);
		void AddWorkItem([In, MarshalAs(UnmanagedType.LPWStr)] string TaskName, [In, MarshalAs(UnmanagedType.Interface)] ITask WorkItem);
		void IsOfType([In, MarshalAs(UnmanagedType.LPWStr)] string TaskName, [In] ref System.Guid riid);
	}

	[Guid("148BD528-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IEnumWorkItems {
		[PreserveSig()]
		int Next([In] uint RequestCount, [Out] out System.IntPtr Names, [Out] out uint Fetched);
		void Skip([In] uint Count);
		void Reset();
		void Clone([Out, MarshalAs(UnmanagedType.Interface)] out IEnumWorkItems EnumWorkItems);
	}

#if WorkItem
		// The IScheduledWorkItem interface is actually never used because ITask inherits all of its
		// methods.  As ITask is the only kind of WorkItem (in 2002) it is the only interface we need.
		[Guid("a6b952f0-a4b1-11d0-997d-00aa006887ec"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IScheduledWorkItem
		{
			void CreateTrigger([Out] out ushort NewTriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
			void DeleteTrigger([In] ushort TriggerIndex);
			void GetTriggerCount([Out] out ushort Count);
			void GetTrigger([In] ushort TriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
			void GetTriggerString([In] ushort TriggerIndex, out System.IntPtr TriggerString);
			void GetRunTimes([In, MarshalAs(UnmanagedType.Struct)] ref SystemTime Begin, [In, MarshalAs(UnmanagedType.Struct)] ref SystemTime End, ref ushort Count, [Out] out System.IntPtr TaskTimes);
			void GetNextRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime NextRun);
			void SetIdleWait([In] ushort IdleMinutes, [In] ushort DeadlineMinutes);
			void GetIdleWait([Out] out ushort IdleMinutes, [Out] out ushort DeadlineMinutes);
			void Run();
			void Terminate();
			void EditWorkItem([In] uint hParent, [In] uint dwReserved);
			void GetMostRecentRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime LastRun);
			void GetStatus([Out, MarshalAs(UnmanagedType.Error)] out int Status);
			void GetExitCode([Out] out uint ExitCode);
			void SetComment([In, MarshalAs(UnmanagedType.LPWStr)] string Comment);
			void GetComment(out System.IntPtr Comment);
			void SetCreator([In, MarshalAs(UnmanagedType.LPWStr)] string Creator);
			void GetCreator(out System.IntPtr Creator);
			void SetWorkItemData([In] ushort DataLen, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, ArraySubType=UnmanagedType.U1)] byte[] Data);
			void GetWorkItemData([Out] out ushort DataLen, [Out] out System.IntPtr Data);
			void SetErrorRetryCount([In] ushort RetryCount);
			void GetErrorRetryCount([Out] out ushort RetryCount);
			void SetErrorRetryInterval([In] ushort RetryInterval);
			void GetErrorRetryInterval([Out] out ushort RetryInterval);
			void SetFlags([In] uint Flags);
			void GetFlags([Out] out uint Flags);
			void SetAccountInformation([In, MarshalAs(UnmanagedType.LPWStr)] string AccountName, [In, MarshalAs(UnmanagedType.LPWStr)] string Password);
			void GetAccountInformation(out System.IntPtr AccountName);
		}
#endif

	[Guid("148BD524-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ITask {
		void CreateTrigger([Out] out ushort NewTriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
		void DeleteTrigger([In] ushort TriggerIndex);
		void GetTriggerCount([Out] out ushort Count);
		void GetTrigger([In] ushort TriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
		void GetTriggerString([In] ushort TriggerIndex, out System.IntPtr TriggerString);
		void GetRunTimes([In, MarshalAs(UnmanagedType.Struct)] ref SystemTime Begin, [In, MarshalAs(UnmanagedType.Struct)] ref SystemTime End, ref ushort Count, [Out] out System.IntPtr TaskTimes);
		void GetNextRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime NextRun);
		void SetIdleWait([In] ushort IdleMinutes, [In] ushort DeadlineMinutes);
		void GetIdleWait([Out] out ushort IdleMinutes, [Out] out ushort DeadlineMinutes);
		void Run();
		void Terminate();
		void EditWorkItem([In] uint hParent, [In] uint dwReserved);
		void GetMostRecentRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime LastRun);
		void GetStatus([Out, MarshalAs(UnmanagedType.Error)] out int Status);
		void GetExitCode([Out] out uint ExitCode);
		void SetComment([In, MarshalAs(UnmanagedType.LPWStr)] string Comment);
		void GetComment(out System.IntPtr Comment);
		void SetCreator([In, MarshalAs(UnmanagedType.LPWStr)] string Creator);
		void GetCreator(out System.IntPtr Creator);
		void SetWorkItemData([In] ushort DataLen, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, ArraySubType=UnmanagedType.U1)] byte[] Data);
		void GetWorkItemData([Out] out ushort DataLen, [Out] out System.IntPtr Data);
		void SetErrorRetryCount([In] ushort RetryCount);
		void GetErrorRetryCount([Out] out ushort RetryCount);
		void SetErrorRetryInterval([In] ushort RetryInterval);
		void GetErrorRetryInterval([Out] out ushort RetryInterval);
		void SetFlags([In] uint Flags);
		void GetFlags([Out] out uint Flags);
		void SetAccountInformation([In, MarshalAs(UnmanagedType.LPWStr)] string AccountName, [In] IntPtr Password);
		void GetAccountInformation(out System.IntPtr  AccountName);
		void SetApplicationName([In, MarshalAs(UnmanagedType.LPWStr)] string ApplicationName);
		void GetApplicationName(out System.IntPtr ApplicationName);
		void SetParameters([In, MarshalAs(UnmanagedType.LPWStr)] string Parameters);
		void GetParameters(out System.IntPtr Parameters);
		void SetWorkingDirectory([In, MarshalAs(UnmanagedType.LPWStr)] string WorkingDirectory);
		void GetWorkingDirectory( out System.IntPtr WorkingDirectory);
		void SetPriority([In] uint Priority);
		void GetPriority([Out] out uint Priority);
		void SetTaskFlags([In] uint Flags);
		void GetTaskFlags([Out] out uint Flags);
		void SetMaxRunTime([In] uint MaxRunTimeMS);
		void GetMaxRunTime([Out] out uint MaxRunTimeMS);
	}

	[Guid("148BD52B-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ITaskTrigger {
		void SetTrigger([In, Out, MarshalAs(UnmanagedType.Struct)] ref TaskTrigger Trigger);
		void GetTrigger([In, Out, MarshalAs(UnmanagedType.Struct)] ref TaskTrigger Trigger);
		void GetTriggerString(out System.IntPtr TriggerString);
	}
	[Guid("4086658a-cbbb-11cf-b604-00c04fd8d565"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IProvideTaskPage {
		void GetPage([In] int tpType, [In] bool fPersistChanges, [Out] out IntPtr phPage);
	}

// ------ Classes ------
	[ComImport, Guid("148BD52A-A2AB-11CE-B11F-00AA00530503")]
	internal class CTaskScheduler {
	}

	[ComImport, Guid("148BD520-A2AB-11CE-B11F-00AA00530503")]
	internal class CTask {
	}

	internal class CoTaskMem {
		/// <summary>
		/// Many COM methods in ITask, ITaskTrigger, and ITaskScheduler return an LPWStr which should
		/// should be freed after the string is accessed.  The "out" pointer could be converted
		/// to a string during marshalling, but then the memory wouldn'task be freed.  Instead
		/// these entries return an IntPtr--call this method to convert it to a string.
		/// </summary>
		/// <param name="lpwstr">A pointer to a unicode string in COM Task Memory, invalid at exit.</param>
		/// <returns>String value.</returns>
		public static string LPWStrToString(System.IntPtr lpwstr) {
			string ret = Marshal.PtrToStringUni(lpwstr);
			Marshal.FreeCoTaskMem(lpwstr);
			return ret;
		}
	}
}
