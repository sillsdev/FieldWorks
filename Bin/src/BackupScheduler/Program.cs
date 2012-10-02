using System;
using System.Security;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Security.Principal;
using TaskScheduler;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace BackupScheduler
{
	static class Program
	{
		private const string FwHive = @"SOFTWARE\SIL\FieldWorks";
		private const string FwCodeDir = @"RootCodeDir";
		private const string FwBackupScript = @"Backup.vbs";

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			UserAccount Account = new UserAccount();

			// We can'task set up a scheduled task if the user has no password, so test this first:
			if (Account.IsUserPasswordBlank())
			{
				MessageBox.Show(Properties.Resources.ErrorBlankPassword,
					Properties.Resources.MsgTitle, MessageBoxButtons.OK,
					MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1,
					MessageBoxOptions.ServiceNotification);
				return;
			}

			// Ensure the Task Scheduler service is running:
			ServiceController sc = new ServiceController("Schedule");
			try
			{
				if (sc.Status == ServiceControllerStatus.Stopped)
				{
					const int SixtySecondsAsMilliSeconds = 60 * 1000;
					TimeSpan timeout = TimeSpan.FromMilliseconds(SixtySecondsAsMilliSeconds);
					sc.Start();
					sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
				}
			}
			catch
			{
				MessageBox.Show(Properties.Resources.ErrorSchedulerService,
					Properties.Resources.MsgTitle, MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation);
				return;
			}

			// Initiate link to Windows Task Scheduler:
			const string TaskNameRoot = "SIL FieldWorks Backup";
			const string TaskNameExtRoot = TaskNameRoot + " by ";
			string TaskName = TaskNameExtRoot + Account.AccountNameAlt;

			// Investigate if there are any backup schedules created by others:
			ScheduledTasks TaskList = new ScheduledTasks();
			string[] TaskNames = TaskList.GetTaskNames();
			string ScheduleOwners = "";
			bool fMeIncluded = false;
			int ctOtherUsersSchedules = 0;
			foreach (string CurrentTaskName in TaskNames)
			{
				if (CurrentTaskName.StartsWith(TaskNameExtRoot))
				{
					string Schedule = "";
					Task CurrentTask = TaskList.OpenTask(CurrentTaskName);
					if (CurrentTask != null)
					{
						foreach (Trigger tr in CurrentTask.Triggers)
						{
							if (Schedule.Length > 0)
								Schedule += "; ";
							Schedule += tr.ToString();
						}
						CurrentTask.Close();
					}
					string Owner = CurrentTaskName.Substring(TaskNameExtRoot.Length);
					if (Owner.EndsWith(".job", StringComparison.CurrentCultureIgnoreCase))
						Owner = Owner.Remove(Owner.Length - 4);
					if (Owner == Account.AccountNameAlt)
					{
						fMeIncluded = true;
						ScheduleOwners += Properties.Resources.MsgOtherSchedulersMe;
					}
					else
					{
						ScheduleOwners += Owner;
						ctOtherUsersSchedules++;
					}
					ScheduleOwners += ": " +
						((Schedule.Length > 0) ? Schedule :
						Properties.Resources.MsgScheduleNotAccessible);

					ScheduleOwners += Environment.NewLine;
				}
			}
			if (ctOtherUsersSchedules > 0)
			{
				string Msg = Properties.Resources.MsgOtherSchedulers + Environment.NewLine
					+ ScheduleOwners + Environment.NewLine
					+ (fMeIncluded? Properties.Resources.MsgOtherSchedulersAndMe :
					Properties.Resources.MsgOtherSchedulersNotMe)
					+ Environment.NewLine + Properties.Resources.MsgOtherSchedulersAddendum;
				if (MessageBox.Show(Msg, Properties.Resources.MsgTitle,
					MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation)
					== DialogResult.Cancel)
				{
					return;
				}
			}

			// Retrieve the current Backup task, if there is one:
			bool fPreExistingTask = true;
			Task task = TaskList.OpenTask(TaskName);

			// If there isn'task one already, make a new one:
			if (task == null)
			{
				fPreExistingTask = false;
				task = TaskList.CreateTask(TaskName);

				if (task == null)
				{
					MessageBox.Show(Properties.Resources.ErrorNoTask,
						Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
						MessageBoxIcon.Stop);
					return;
				}

				// Set the program to run on the schedule by looking up the FW location:
				string ScriptPath = null;
				RegistryKey rKey = Registry.LocalMachine.OpenSubKey(FwHive);
				if (rKey != null)
				{
					System.Object regObj = rKey.GetValue(FwCodeDir);
					if (regObj != null)
					{
						string FwFolder = regObj.ToString();
						if (!FwFolder.EndsWith(@"\"))
							FwFolder += @"\";
						ScriptPath = FwFolder + FwBackupScript;
					}
				}
				if (ScriptPath == null)
				{
					MessageBox.Show(Properties.Resources.ErrorNoScript,
						Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					return;
				}
				// On 64-bit machines, we have to force the scheduled script to run with the 32-bit // version of the script engine, otherwise it fails to load the 32-bit COM class // in the script:
				if (Is64Bit())
				{
					// Create the path to the 32-bit script engine:
					task.ApplicationName = Environment.GetEnvironmentVariable("WINDIR") + @"\SysWOW64\wscript.exe";
					// put the path to the script in as the parameter to the script engine:
					task.Parameters = "\"" + ScriptPath + "\"";
				}
				else
				{
					task.ApplicationName = ScriptPath;
					task.Parameters = "";
				}
				task.Comment = "Created automatically by FieldWorks Backup system.";

				// Give it an arbitrary weekday run at 5 in the afternoon:
				task.Triggers.Add(new WeeklyTrigger(17, 0, DaysOfTheWeek.Monday |
					DaysOfTheWeek.Tuesday | DaysOfTheWeek.Wednesday | DaysOfTheWeek.Thursday |
					DaysOfTheWeek.Friday));
			}

			// Display the Windows Task Scheduler schedule page for our task:
			bool fDisplaySchedule = true;
			while (fDisplaySchedule)
			{
				if (task.DisplayPropertySheet(Task.PropPages.Schedule))
				{
					// User pressed OK on Schedule page.
					DialogResult DlgResult;
					bool fPasswordCorrect = false;
					do
					{
						// User must give their Windows logon password in order to use schedule:
						BackupSchedulePasswordDlg PwdDlg = new BackupSchedulePasswordDlg();
						DlgResult = PwdDlg.ShowDialog();
						SecureString Password = PwdDlg.Password;
						PwdDlg.Dispose();

						if (DlgResult == DialogResult.OK)
						{
							try
							{
								// Test if the password the user gave is correct:
								fPasswordCorrect = Account.IsPasswordCorrect(Password);
								if (fPasswordCorrect)
								{
									// Configure the scheduled task with the user account details:
									task.SetAccountInformation(Account.AccountName, Password);
									task.Save();
									fDisplaySchedule = false;
								}
							}
							catch (System.Exception e)
							{
								if (e.Message == "Password error")
									DlgResult = DialogResult.Cancel;
							}
						}
					} while (DlgResult == DialogResult.OK && !fPasswordCorrect);
				}
				else // User pressed cancel on Task Scheduler dialog
				{
					if (fPreExistingTask)
					{
						// Give user the option to delete the pre-existing shceduled backup.

						// Make a string listing the scheduled backup time(s):
						string Schedule = Properties.Resources.MsgCurrentSchedule;
						Schedule += Environment.NewLine;
						foreach (Trigger tr in task.Triggers)
							Schedule += tr.ToString() + Environment.NewLine;
						Schedule += Environment.NewLine;
						if (MessageBox.Show(
							Schedule + Properties.Resources.MsgDeleteExistingTask,
							Properties.Resources.MsgTitle, MessageBoxButtons.YesNo,
							MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
							== DialogResult.Yes)
						{
							task.Close();
							task = null;
							TaskList.DeleteTask(TaskName);
						}
					}
					fDisplaySchedule = false;
				}
			} // End while fDisplaySchedule
			if (task != null)
				task.Close();
		}

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

		public static bool Is64Bit()
		{
			bool retVal;
			IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
			return retVal;
		}
	}
}
