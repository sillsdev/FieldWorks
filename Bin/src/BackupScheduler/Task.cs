/*
 *
 * This file was taken from http://www.codeproject.com/KB/cs/tsnewlib.aspx
 * ("A New Task Scheduler Class Library for .NET" by Dennis Austin)
 * This file is licensed under The Code Project Open License (CPOL):
 * http://www.codeproject.com/info/cpol10.aspx
 *
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using TaskSchedulerInterop;

namespace TaskScheduler {
	#region Enums
	/// <summary>
	/// Options for a task, used for the Flags property of a Task. Uses the
	/// "Flags" attribute, so these values are combined with |.
	/// Some flags are documented as Windows 95 only, but they have a
	/// user interface in Windows XP so that may not be true.
	/// </summary>
	[Flags]
	public enum TaskFlags {
		/// <summary>
		/// The precise meaning of this flag is elusive.  The MSDN documentation describes it
		/// only for use in converting jobs from the Windows NT "AT" service to the newer
		/// Task Scheduler.  No other use for the flag is documented.
		/// </summary>
		Interactive = 0x1,
		/// <summary>
		/// The task will be deleted when there are no more scheduled run times.
		/// </summary>
		DeleteWhenDone = 0x2,
		/// <summary>
		/// The task is disabled.  Used to temporarily prevent a task from being triggered normally.
		/// </summary>
		Disabled = 0x4,
		/// <summary>
		/// The task begins only if the computer is idle at the scheduled start time.
		/// The computer is not considered idle until the task's <see cref="Task.IdleWaitMinutes"/> time
		/// elapses with no user input.
		/// </summary>
		StartOnlyIfIdle = 0x10,
		/// <summary>
		/// The task terminates if the computer makes an idle to non-idle transition while the task is running.
		/// For information regarding idle triggers, see <see cref="OnIdleTrigger"/>.
		/// </summary>
		KillOnIdleEnd = 0x20,
		/// <summary>
		/// The task does not start if the computer is running on battery power.
		/// </summary>
		DontStartIfOnBatteries = 0x40,
		/// <summary>
		/// The task ends, and the associated application quits if the computer switches
		/// to battery power.
		/// </summary>
		KillIfGoingOnBatteries = 0x80,
		/// <summary>
		/// The task runs only if the system is docked.
		/// (Not mentioned in current MSDN documentation; probably obsolete.)
		/// </summary>
		RunOnlyIfDocked = 0x100,
		/// <summary>
		/// The task item is hidden.
		///
		/// This is implemented by setting the job file's hidden attribute.  Testing revealed that clearing
		/// this flag doesn'task clear the file attribute, so the library sets the file attribute directly.  This
		/// flag is kept in sync with the task's Hidden property, so they function equivalently.
		/// </summary>
		Hidden = 0x200,
		/// <summary>
		/// The task runs only if there is currently a valid Internet connection.
		/// Not currently implemented. (Check current MSDN documentation for updates.)
		/// </summary>
		RunIfConnectedToInternet = 0x400,
		/// <summary>
		/// The task starts again if the computer makes a non-idle to idle transition before all the
		/// task's task_triggers elapse. (Use this flag in conjunction with KillOnIdleEnd.)
		/// </summary>
		RestartOnIdleResume = 0x800,
		/// <summary>
		/// Wake the computer to run this task.  Seems to be misnamed, but the name is taken from
		/// the low-level interface.
		///
		/// </summary>
		SystemRequired = 0x1000,
		/// <summary>
		/// The task runs only if the user specified in SetAccountInformation() is
		/// logged on interactively.  This flag has no effect on TaskNames set to run in
		/// the local SYSTEM account.
		/// </summary>
		RunOnlyIfLoggedOn = 0x2000
	}

	/// <summary>
	/// Status values returned for a task.  Some values have been determined to occur although
	/// they do no appear in the Task Scheduler system documentation.
	/// </summary>
	public enum TaskStatus {
		/// <summary>
		/// The task is ready to run at its next scheduled time.
		/// </summary>
		Ready = HResult.SCHED_S_TASK_READY,
		/// <summary>
		/// The task is currently running.
		/// </summary>
		Running = HResult.SCHED_S_TASK_RUNNING,
		/// <summary>
		/// One or more of the properties that are needed to run this task on a schedule have not been set.
		/// </summary>
		NotScheduled = HResult.SCHED_S_TASK_NOT_SCHEDULED,
		/// <summary>
		/// The task has not yet run.
		/// </summary>
		NeverRun = HResult.SCHED_S_TASK_HAS_NOT_RUN,
		/// <summary>
		/// The task will not run at the scheduled times because it has been disabled.
		/// </summary>
		Disabled = HResult.SCHED_S_TASK_DISABLED,
		/// <summary>
		/// There are no more runs scheduled for this task.
		/// </summary>
		NoMoreRuns = HResult.SCHED_S_TASK_NO_MORE_RUNS,
		/// <summary>
		/// The last run of the task was terminated by the user.
		/// </summary>
		Terminated = HResult.SCHED_S_TASK_TERMINATED,
		/// <summary>
		/// Either the task has no triggers or the existing triggers are disabled or not set.
		/// </summary>
		NoTriggers = HResult.SCHED_S_TASK_NO_VALID_TRIGGERS,
		/// <summary>
		/// Event triggers don'task have set run times.
		/// </summary>
		NoTriggerTime = HResult.SCHED_S_EVENT_TRIGGER
	}
	#endregion

	/// <summary>
	/// Represents an item in the Scheduled Tasks folder.  There are no public constructors for Task.
	/// New instances are generated by a <see cref="ScheduledTasks"/> object using Open or Create methods.
	/// A task object holds COM interfaces;  call its <see cref="Close"/> method to release them.
	/// </summary>
	public class Task : IDisposable {
		#region Fields
		/// <summary>
		/// Internal COM interface
		/// </summary>
		private ITask iTask;
		/// <summary>
		/// Name of this task (with no .job extension)
		/// </summary>
		private string name;
		/// <summary>
		/// List of triggers for this task
		/// </summary>
		private TriggerList triggers;
		#endregion

		#region Constructors
		/// <summary>
		/// Internal constructor for a task, used by <see cref="ScheduledTasks"/>.
		/// </summary>
		/// <param name="iTask">Instance of an ITask.</param>
		/// <param name="taskName">Name of the task.</param>
		internal Task(ITask iTask, string taskName) {
			this.iTask = iTask;
			if (taskName.EndsWith(".job"))
				name = taskName.Substring(0, taskName.Length-4);
			else
				name = taskName;
			triggers = null;
			this.Hidden = GetHiddenFileAttr();
		}
		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the task.  The name is also the filename (plus a .job extension)
		/// the Task Scheduler uses to store the task information.  To change the name of a
		/// task, use <see cref="Save()"/> to save it as a new name and then delete
		/// the old task.
		/// </summary>
		public string Name {
			get {
				return name;
			}
		}

		/// <summary>
		/// Gets the list of triggers associated with the task.
		/// </summary>
		public TriggerList Triggers {
			get {
				if (triggers == null) {
					// Trigger list has not been requested before; create it
					triggers = new TriggerList(iTask);
				}
				return triggers;
			}
		}

		/// <summary>
		/// Gets/sets the application filename that task is to run.  Get returns
		/// an absolute pathname.  A name searched with the PATH environment variable can
		/// be assigned, and the path search is done when the task is saved.
		/// </summary>
		public string ApplicationName {
			get {
				IntPtr lpwstr;
				iTask.GetApplicationName(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
			set {
				iTask.SetApplicationName(value);
			}
		}

		/// <summary>
		/// Gets the name of the account under which the task process will run.
		/// </summary>
		public string AccountName {
			get {
				IntPtr lpwstr = IntPtr.Zero;
				iTask.GetAccountInformation(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
		}

		/// <summary>
		/// Gets/sets the comment associated with the task.  The comment appears in the
		/// Scheduled Tasks user interface.
		/// </summary>
		public string Comment {
			get {
				IntPtr lpwstr;
				iTask.GetComment(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
			set {
				iTask.SetComment(value);
			}
		}

		/// <summary>
		/// Gets/sets the creator of the task.  If no value is supplied, the system
		/// fills in the account name of the caller when the task is saved.
		/// </summary>
		public string Creator {
			get {
				IntPtr lpwstr;
				iTask.GetCreator(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
			set {
				iTask.SetCreator(value);
			}
		}

		/// <summary>
		/// Gets/sets the number of times to retry task execution after failure. (Not implemented.)
		/// </summary>
		private short ErrorRetryCount {
			get {
				ushort ret;
				iTask.GetErrorRetryCount(out ret);
				return (short)ret;
			}
			set {
				iTask.SetErrorRetryCount((ushort)value);
			}
		}

		/// <summary>
		/// Gets/sets the time interval, in minutes, to delay between error retries. (Not implemented.)
		/// </summary>
		private short ErrorRetryInterval {
			get {
				ushort ret;
				iTask.GetErrorRetryInterval(out ret);
				return (short)ret;
			}
			set {
				iTask.SetErrorRetryInterval((ushort)value);
			}
		}

		/// <summary>
		/// Gets the Win32 exit code from the last execution of the task.  If the task failed
		/// to start on its last run, the reason is returned as an exception.  Not updated while
		/// in an open task;  the property does not change unless the task is closed and re-opened.
		/// <exception>Various exceptions for a task that couldn'task be run.</exception>
		/// </summary>
		public int ExitCode {
			get {
				uint ret = 0;
				iTask.GetExitCode(out ret);
				return (int)ret;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TaskFlags"/> associated with the current task.
		/// </summary>
		public TaskFlags Flags {
			get {
				uint ret;
				iTask.GetFlags(out ret);
				return (TaskFlags)ret;
			}
			set {
				iTask.SetFlags((uint)value);
			}
		}

		/// <summary>
		/// Gets/sets how long the system must remain idle, even after the trigger
		/// would normally fire, before the task will run.
		/// </summary>
		public short IdleWaitMinutes {
			get {
				ushort ret, nothing;
				iTask.GetIdleWait(out ret, out nothing);
				return (short)ret;
			}
			set {
				ushort m = (ushort)IdleWaitDeadlineMinutes;
				iTask.SetIdleWait((ushort)value, m);
			}
		}

		/// <summary>
		/// Gets/sets the maximum number of minutes that Task Scheduler will wait for a
		/// required idle period to occur.
		/// </summary>
		public short IdleWaitDeadlineMinutes {
			get {
				ushort ret, nothing;
				iTask.GetIdleWait(out nothing, out ret);
				return (short)ret;
			}
			set {
				ushort m = (ushort)IdleWaitMinutes;
				iTask.SetIdleWait(m, (ushort)value);
			}
		}

		/// <summary>
		/// <p>Gets/sets the maximum length of time the task is permitted to run.
		/// Setting MaxRunTime also affects the value of <see cref="Task.MaxRunTimeLimited"/>.
		/// </p>
		/// <p>The longest MaxRunTime implemented is 0xFFFFFFFE milliseconds, or
		/// about 50 days.  If you set a TimeSpan longer than that, the
		/// MaxRunTime will be unlimited.</p>
		/// </summary>
		/// <Remarks>
		/// </Remarks>
		public TimeSpan MaxRunTime {
			get {
				uint ret;
				iTask.GetMaxRunTime(out ret);
				return new TimeSpan((long)ret * TimeSpan.TicksPerMillisecond);
			}
			set {
				double proposed = ((TimeSpan)value).TotalMilliseconds;
				if (proposed >= uint.MaxValue) {
					iTask.SetMaxRunTime(uint.MaxValue);
				} else {
					iTask.SetMaxRunTime((uint)proposed);
				}

				//iTask.SetMaxRunTime((uint)((TimeSpan)value).TotalMilliseconds);
			}
		}

		/// <summary>
		/// <p>If the maximum run time is limited, the task will be terminated after
		/// <see cref="Task.MaxRunTime"/> expires.  Setting the value to FALSE, i.e. unlimited,
		/// invalidates MaxRunTime.</p>
		/// <p>The Task Scheduler service will try to send a WM_CLOSE message when it needs to terminate
		/// a task.  If the message can'task be sent, or the task does not respond with three minutes,
		/// the task will be terminated using TerminateProcess.</p>
		/// </summary>
		public bool MaxRunTimeLimited {
			get {
				uint ret;
				iTask.GetMaxRunTime(out ret);
				return (ret == uint.MaxValue);
			}
			set {
				if (value) {
					uint ret;
					iTask.GetMaxRunTime(out ret);
					if (ret == uint.MaxValue) {
						iTask.SetMaxRunTime(72*360*1000); //72 hours.  Thats what Explorer sets.
					}
				} else {
					iTask.SetMaxRunTime(uint.MaxValue);
				}
			}
		}

		/// <summary>
		/// Gets the most recent time the task began running.  <see cref="DateTime.MinValue"/>
		/// returned if the task has not run.
		/// </summary>
		public DateTime MostRecentRunTime {
			get {
				SystemTime st = new SystemTime();
				iTask.GetMostRecentRunTime(ref st);
				if (st.Year == 0)
					return DateTime.MinValue;
				return new DateTime((int)st.Year, (int)st.Month, (int)st.Day, (int)st.Hour, (int)st.Minute, (int)st.Second, (int)st.Milliseconds);
			}
		}

		/// <summary>
		/// Gets the next time the task will run. Returns <see cref="DateTime.MinValue"/>
		/// if the task is not scheduled to run.
		/// </summary>
		public DateTime NextRunTime {
			get {
				SystemTime st = new SystemTime();
				iTask.GetNextRunTime(ref st);
				if (st.Year == 0)
					return DateTime.MinValue;
				return new DateTime((int)st.Year, (int)st.Month, (int)st.Day, (int)st.Hour, (int)st.Minute, (int)st.Second, (int)st.Milliseconds);
			}
		}

		/// <summary>
		/// Gets/sets the command-line parameters for the task.
		/// </summary>
		public string Parameters {
			get {
				IntPtr lpwstr;
				iTask.GetParameters(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
			set {
				iTask.SetParameters(value);
			}
		}

		/// <summary>
		/// Gets/sets the priority for the task process.
		/// Note:  ProcessPriorityClass defines two levels (AboveNormal and BelowNormal) that are
		/// not documented in the task scheduler interface and can'task be use on Win 98 platforms.
		/// </summary>
		public System.Diagnostics.ProcessPriorityClass Priority {
			get {
				uint ret;
				iTask.GetPriority(out ret);
				return (System.Diagnostics.ProcessPriorityClass)ret;
			}
			set {
				if (value==System.Diagnostics.ProcessPriorityClass.AboveNormal ||
					value==System.Diagnostics.ProcessPriorityClass.BelowNormal ) {
					throw new ArgumentException("Unsupported Priority Level");
				}
				iTask.SetPriority((uint)value);
			}
		}

		/// <summary>
		/// Gets the status of the task.  Returns <see cref="TaskStatus"/>.
		/// Not updated while a task is open.
		/// </summary>
		public TaskStatus Status {
			get {
				int ret;
				iTask.GetStatus(out ret);
				return (TaskStatus)ret;
			}
		}

		/// <summary>
		/// Extended Flags associated with a task. These are associated with the ITask com interface
		/// and none are currently defined.
		/// </summary>
		private int FlagsEx {
			get {
				uint ret;
				iTask.GetTaskFlags(out ret);
				return (int)ret;
			}
			set {
				iTask.SetTaskFlags((uint)value);
			}
		}

		/// <summary>
		/// Gets/sets the initial working directory for the task.
		/// </summary>
		public string WorkingDirectory {
			get {
				IntPtr lpwstr;
				iTask.GetWorkingDirectory(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			}
			set {
				iTask.SetWorkingDirectory(value);
			}
		}

		/// <summary>
		/// Hidden TaskNames are stored in files with
		/// the hidden file attribute so they don'task appear in the Explorer user interface.
		/// Because there is a special interface for Scheduled Tasks, they don'task appear
		/// even if Explorer is set to show hidden files.
		/// Functionally equivalent to TaskFlags.Hidden.
		/// </summary>
		public bool Hidden {
			get {
				return (this.Flags & TaskFlags.Hidden) != 0;
			}
			set {
				if (value) {
					this.Flags |= TaskFlags.Hidden;
				} else {
					this.Flags &= ~TaskFlags.Hidden;
				}
			}
		}
		/// <summary>
		/// Gets/sets arbitrary data associated with the task.  The tag can be used for any purpose
		/// by the client, and is not used by the Task Scheduler.  Known as WorkItemData in the
		/// IWorkItem com interface.
		/// </summary>
		public object Tag {
			get {
				ushort DataLen;
				IntPtr Data;
				iTask.GetWorkItemData(out DataLen, out Data);
				byte[] bytes = new byte[DataLen];
				Marshal.Copy(Data, bytes, 0, DataLen);
				MemoryStream stream = new MemoryStream(bytes, false);
				BinaryFormatter b = new BinaryFormatter();
				return b.Deserialize(stream);
			}
			set {
				if (!value.GetType().IsSerializable)
					throw new ArgumentException("Objects set as Data for Tasks must be serializable", "value");
				BinaryFormatter b = new BinaryFormatter();
				MemoryStream stream = new MemoryStream();
				b.Serialize(stream, value);
				iTask.SetWorkItemData((ushort)stream.Length, stream.GetBuffer());
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Set the hidden attribute on the file corresponding to this task.
		/// </summary>
		/// <param name="set">Set the attribute accordingly.</param>
		private void SetHiddenFileAttr(bool set) {
			IPersistFile iFile = (IPersistFile)iTask;
			string fileName;
			iFile.GetCurFile(out fileName);
			System.IO.FileAttributes attr;
			attr = System.IO.File.GetAttributes(fileName);
			if (set)
				attr |= System.IO.FileAttributes.Hidden;
			else
				attr &= ~System.IO.FileAttributes.Hidden;
			System.IO.File.SetAttributes(fileName, attr);
		}
		/// <summary>
		/// Get the hidden attribute from the file corresponding to this task.
		/// </summary>
		/// <returns>The value of the attribute.</returns>
		private bool GetHiddenFileAttr() {
			IPersistFile iFile = (IPersistFile)iTask;
			string fileName;
			iFile.GetCurFile(out fileName);
			System.IO.FileAttributes attr;
			try {
				attr = System.IO.File.GetAttributes(fileName);
				return (attr & System.IO.FileAttributes.Hidden) != 0;
			} catch {
				return false;
			}
		}

		/// <summary>
		/// Calculate the next time the task would be scheduled
		/// to run after a given arbitrary time.  If the task will not run
		/// (perhaps disabled) then returns <see cref="DateTime.MinValue"/>.
		/// </summary>
		/// <param name="after">The time to calculate from.</param>
		/// <returns>The next time the task would run.</returns>
		public DateTime NextRunTimeAfter(DateTime after) {
			//Add one second to get a run time strictly greater than the specified time.
			after = after.AddSeconds(1);
			//Convert to a valid SystemTime
			SystemTime stAfter = new SystemTime();
			stAfter.Year = (ushort)after.Year;
			stAfter.Month = (ushort)after.Month;
			stAfter.Day = (ushort)after.Day;
			stAfter.DayOfWeek = (ushort)after.DayOfWeek;
			stAfter.Hour = (ushort)after.Hour;
			stAfter.Minute = (ushort)after.Minute;
			stAfter.Second = (ushort)after.Second;
			SystemTime stLimit = new SystemTime();
			// Would like to pass null as the second parameter to GetRunTimes, indicating that
			// the interval is unlimited.  Can'task figure out how to do that, so use a big time value.
			stLimit = stAfter;
			stLimit.Year = (ushort)DateTime.MaxValue.Year;
			stLimit.Month = 1;  //Just in case stAfter date was Feb 29, but MaxValue.Year is not a leap year!
			IntPtr pTimes;
			ushort nFetch = 1;
			iTask.GetRunTimes(ref stAfter, ref stLimit, ref nFetch, out pTimes);
			if (nFetch == 1) {
				SystemTime stNext = new SystemTime();
				stNext = (SystemTime)Marshal.PtrToStructure(pTimes, typeof(SystemTime));
				Marshal.FreeCoTaskMem(pTimes);
				return new DateTime(stNext.Year, stNext.Month, stNext.Day, stNext.Hour, stNext.Minute, stNext.Second);
			} else {
				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Schedules the task for immediate execution.
		/// The system works from the saved version of the task, so call <see cref="Save()"/> before running.
		/// If the task has never been saved, it throws an argument exception.  Problems starting
		/// the task are reported by the <see cref="ExitCode"/> property, not by exceptions on Run.
		/// </summary>
		/// <remarks>The system never updates an open task, so you don'task get current results for
		/// the <see cref="Status"/> or the <see cref="ExitCode"/> properties until you close
		/// and reopen the task.
		/// </remarks>
		/// <exception cref="ArgumentException"></exception>
		public void Run() {
			iTask.Run();
		}

		/// <summary>
		/// Saves changes to the established task name.
		/// </summary>
		/// <overloads>Saves changes that have been made to this Task.</overloads>
		/// <remarks>The account name is checked for validity
		/// when a Task is saved.  The password is not checked, but the account name
		/// must be valid (or empty).
		/// </remarks>
		/// <exception cref="COMException">Unable to establish existence of the account specified.</exception>
		public void Save() {
			IPersistFile iFile = (IPersistFile)iTask;
			iFile.Save(null, false);
			SetHiddenFileAttr(Hidden);  //Do the Task Scheduler's work for it because it doesn'task reset properly
		}

		/// <summary>
		/// Saves the Task with a new name.  The task with the old name continues to
		/// exist in whatever state it was last saved.  It is no longer open, because.
		/// the Task object is associated with the new name from now on.
		/// If there is already a task using the new name, it is overwritten.
		/// </summary>
		/// <remarks>See the <see cref="Save()"/>() overload.</remarks>
		/// <param name="name">The new name to be used for this task.</param>
		/// <exception cref="COMException">Unable to establish existence of the account specified.</exception>
		public void Save(string name) {
			IPersistFile iFile = (IPersistFile)iTask;
			string path;
			iFile.GetCurFile(out path);
			string newPath;
			newPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + name + Path.GetExtension(path);
			iFile.Save(newPath, true);
			iFile.SaveCompleted(newPath); /* probably unnecessary */
			this.name = name;
			SetHiddenFileAttr(Hidden);  //Do the Task Scheduler's work for it because it doesn'task reset properly
		}

		/// <summary>
		/// Release COM interfaces for this Task.  After a Task is closed, accessing its
		/// members throws a null reference exception.
		/// </summary>
		public void Close() {
			if (triggers != null) {
				triggers.Dispose();
			}
			Marshal.ReleaseComObject(iTask);
			iTask = null;
		}

		/// <summary>
		/// For compatibility with earlier versions.  New clients should use <see cref="DisplayPropertySheet()"/>.
		/// </summary>
		/// <remarks>
		/// Display the property pages of this task for user editing.  If the user clicks OK, the
		/// task's properties are updated and the task is also automatically saved.
		/// </remarks>
		public void DisplayForEdit() {
			iTask.EditWorkItem(0, 0);
		}

		/// <summary>
		/// Argument for DisplayForEdit to determine which property pages to display.
		/// </summary>
		[Flags]
		public enum PropPages {
			/// <summary>
			/// The task property page
			/// </summary>
			Task = 0x01,
			/// <summary>
			/// The schedule property page
			/// </summary>
			Schedule = 0x02,
			/// <summary>
			/// The setting property page
			/// </summary>
			Settings = 0x04
		}
		///
		/// <summary>
		/// Display all property pages.
		/// </summary>
		/// <remarks>
		/// The method does not return until the user has dismissed the dialog box.
		/// If the dialog box is dismissed with the OK button, returns true and
		/// updates properties in the task.
		/// The changes are not made permanent, however, until the task is saved.  (Save() method.)
		/// </remarks>
		/// <returns><c>true</c> if dialog box was dismissed with OK, otherwise <c>false</c>.</returns>
		/// <overloads>Display the property pages of this task for user editing.</overloads>
		public bool DisplayPropertySheet() {
			//iTask.EditWorkItem(0, 0);  //This implementation saves automatically, so we don'task use it.
			return DisplayPropertySheet(PropPages.Task | PropPages.Schedule | PropPages.Settings);
		}

		/// <summary>
		/// Display only the specified property pages.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DisplayPropertySheet()"/>() overload.
		/// </remarks>
		/// <param name="pages">Controls which pages are presented</param>
		/// <returns><c>true</c> if dialog box was dismissed with OK, otherwise <c>false</c>.</returns>
		public bool DisplayPropertySheet(PropPages pages) {
			PropSheetHeader hdr = new PropSheetHeader();
			IProvideTaskPage iProvideTaskPage = (IProvideTaskPage)iTask;
			IntPtr[] hPages = new IntPtr[3];
			IntPtr hPage;
			int nPages = 0;
			if ((pages & PropPages.Task) != 0) {
				//get task page
				iProvideTaskPage.GetPage(0, false, out hPage);
				hPages[nPages++] = hPage;
			}
			if ((pages & PropPages.Schedule) != 0) {
				//get task page
				iProvideTaskPage.GetPage(1, false, out hPage);
				hPages[nPages++] = hPage;
			}
			if ((pages & PropPages.Settings) != 0) {
				//get task page
				iProvideTaskPage.GetPage(2, false, out hPage);
				hPages[nPages++] = hPage;
			}
			if (nPages == 0) throw (new ArgumentException("No Property Pages to display"));
			hdr.dwSize = (uint)Marshal.SizeOf(hdr);
			hdr.dwFlags = (uint) (PropSheetFlags.PSH_DEFAULT | PropSheetFlags.PSH_NOAPPLYNOW);
			hdr.pszCaption = this.Name;
			hdr.nPages = (uint)nPages;
			GCHandle gch = GCHandle.Alloc(hPages, GCHandleType.Pinned);
			hdr.phpage = gch.AddrOfPinnedObject();
			int res = PropertySheetDisplay.PropertySheet(ref hdr);
			gch.Free();
			if (res < 0) throw (new Exception("Property Sheet failed to display"));
			return res>0;
		}


		/// <summary>
		/// Sets the account under which the task will run.  Supply the account name and
		/// password as parameters.  For the localsystem account, pass an empty string for
		/// the account name and null for the password.  See Remarks.
		/// </summary>
		/// <param name="accountName">Full account name.</param>
		/// <param name="password">Password for the account.</param>
		/// <remarks>
		/// <p>To have the task to run under the local system account, pass the empty string ("")
		/// as accountName and null as the password.  The caller must be running in
		/// an administrator account or in the local system account.
		/// </p>
		/// <p>
		/// You can also specify a null password if the task has the flag RunOnlyIfLoggedOn set.
		/// This allows you to schedule a task for an account for which you don'task know the password,
		/// but the account must be logged on interactively at the time the task runs.</p>
		/// </remarks>
		public void SetAccountInformation(string accountName, string password) {
			IntPtr pwd = Marshal.StringToCoTaskMemUni(password);
			iTask.SetAccountInformation(accountName, pwd);
			Marshal.FreeCoTaskMem(pwd);
		}
		/// <summary>
		/// Overload for SetAccountInformation which permits use of a SecureString for the
		/// password parameter.  The decoded password will remain in memory only as long as
		/// needed to be passed to the TaskScheduler service.
		/// </summary>
		/// <param name="accountName">Full account name.</param>
		/// <param name="password">Password for the account.</param>
		public void SetAccountInformation(string accountName, SecureString password) {
			IntPtr pwd = Marshal.SecureStringToCoTaskMemUnicode(password);
			iTask.SetAccountInformation(accountName, pwd);
			Marshal.ZeroFreeCoTaskMemUnicode(pwd);
		}

		/// <summary>
		/// Request that the task be terminated if it is currently running.  The call returns
		/// immediately, although the task may continue briefly.  For Windows programs, a WM_CLOSE
		/// message is sent first and the task is given three minutes to shut down voluntarily.
		/// Should it not, or if the task is not a Windows program, TerminateProcess is used.
		/// </summary>
		/// <exception cref="COMException">The task is not running.</exception>
		public void Terminate() {
				iTask.Terminate();
		}

		/// <summary>
		/// Overridden. Outputs the name of the task, the application and parameters.
		/// </summary>
		/// <returns>String representing task.</returns>
		public override string ToString() {
			return string.Format("{0} (\"{1}\" {2})", name, ApplicationName, Parameters);
		}
		#endregion

		#region Implementation of IDisposable
		/// <summary>
		/// A synonym for Close.
		/// </summary>
		public void Dispose() {
			this.Close();
		}
		#endregion
	}
}
