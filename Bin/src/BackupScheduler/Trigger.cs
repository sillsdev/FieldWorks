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
using TaskSchedulerInterop;

namespace TaskScheduler {
	#region Enums
	/// <summary>
	/// Valid types of triggers
	/// </summary>
	internal enum TriggerType {
		/// <summary>
		/// Trigger is set to run the task a single time.
		/// </summary>
		RunOnce = 0,
		/// <summary>
		/// Trigger is set to run the task on a daily interval.
		/// </summary>
		RunDaily = 1,
		/// <summary>
		/// Trigger is set to run the work item on specific days of a specific week of a specific month.
		/// </summary>
		RunWeekly = 2,
		/// <summary>
		/// Trigger is set to run the task on a specific day(s) of the month.
		/// </summary>
		RunMonthly = 3,
		/// <summary>
		/// Trigger is set to run the task on specific days, weeks, and months.
		/// </summary>
		RunMonthlyDOW = 4,
		/// <summary>
		/// Trigger is set to run the task if the system remains idle for the amount of time specified by the idle wait time of the task.
		/// </summary>
		OnIdle = 5,
		/// <summary>
		/// Trigger is set to run the task at system startup.
		/// </summary>
		OnSystemStart = 6,
		/// <summary>
		/// Trigger is set to run the task when a user logs on.
		/// </summary>
		OnLogon = 7
	}

	/// <summary>
	/// Values for days of the week (Monday, Tuesday, etc.)  These carry the Flags
	/// attribute so DaysOfTheWeek and be combined with | (or).
	/// </summary>
	[Flags]
	public enum DaysOfTheWeek : short {
		/// <summary>
		/// Sunday
		/// </summary>
		Sunday = 0x1,
		/// <summary>
		/// Monday
		/// </summary>
		Monday = 0x2,
		/// <summary>
		/// Tuesday
		/// </summary>
		Tuesday = 0x4,
		/// <summary>
		/// Wednesday
		/// </summary>
		Wednesday = 0x8,
		/// <summary>
		/// Thursday
		/// </summary>
		Thursday = 0x10,
		/// <summary>
		/// Friday
		/// </summary>
		Friday = 0x20,
		/// <summary>
		/// Saturday
		/// </summary>
		Saturday = 0x40
	}

	/// <summary>
	/// Values for week of month (first, second, ..., last)
	/// </summary>
	public enum WhichWeek : short {
		/// <summary>
		/// First week of the month
		/// </summary>
		FirstWeek = 1,
		/// <summary>
		/// Second week of the month
		/// </summary>
		SecondWeek = 2,
		/// <summary>
		/// Third week of the month
		/// </summary>
		ThirdWeek = 3,
		/// <summary>
		/// Fourth week of the month
		/// </summary>
		FourthWeek = 4,
		/// <summary>
		/// Last week of the month
		/// </summary>
		LastWeek = 5
	}

	/// <summary>
	/// Values for months of the year (January, February, etc.)  These carry the Flags
	/// attribute so DaysOfTheWeek and be combined with | (or).
	/// </summary>
	[Flags]
	public enum MonthsOfTheYear : short {
		/// <summary>
		/// January
		/// </summary>
		January = 0x1,
		/// <summary>
		/// February
		/// </summary>
		February = 0x2,
		/// <summary>
		/// March
		/// </summary>
		March = 0x4,
		/// <summary>
		/// April
		/// </summary>
		April = 0x8,
		/// <summary>
		///May
		/// </summary>
		May = 0x10,
		/// <summary>
		/// June
		/// </summary>
		June = 0x20,
		/// <summary>
		/// July
		/// </summary>
		July = 0x40,
		/// <summary>
		/// August
		/// </summary>
		August = 0x80,
		/// <summary>
		/// September
		/// </summary>
		September = 0x100,
		/// <summary>
		/// October
		/// </summary>
		October = 0x200,
		/// <summary>
		/// November
		/// </summary>
		November = 0x400,
		/// <summary>
		/// December
		/// </summary>
		December = 0x800
	}
	#endregion

	/// <summary>
	/// Trigger is a generalization of all the concrete trigger classes, and any actual
	/// Trigger object is one of those types.  When included in the TriggerList of a
	/// Task, a Trigger determines when a scheduled task will be run.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Create a concrete trigger for a specific start condition and then call TriggerList.Add
	/// to include it in a task's TriggerList.</para>
	/// <para>
	/// A Trigger that is not yet in a Task's TriggerList is said to be unbound and it holds
	/// no resources (i.e. COM interfaces).  Once it is added to a TriggerList, it is bound and
	/// holds a COM interface that is only released when the Trigger is removed from the list or
	/// the corresponding Task is closed.</para>
	/// <para>
	/// A Trigger that is already bound cannot be added to a TriggerList.  To copy a Trigger from
	/// one list to another, use <see cref="Clone()"/> to create an unbound copy and then add the
	/// copy to the new list.  To move a Trigger from one list to another, use <see cref="TriggerList.Remove"/>
	/// to extract the Trigger from the first list before adding it to the second.</para>
	/// </remarks>
	public abstract class Trigger : ICloneable {
		#region Enums
		/// <summary>
		/// Flags for triggers
		/// </summary>
		[Flags]
			private enum TaskTriggerFlags {
			HasEndDate = 0x1,
			KillAtDurationEnd = 0x2,
			Disabled = 0x4
		}
		#endregion

		#region Fields
		private ITaskTrigger iTaskTrigger; //null for an unbound Trigger
		internal TaskTrigger taskTrigger;
		#endregion

		#region Constructors and Initializers
		/// <summary>
		/// Internal base constructor for an unbound Trigger.
		/// </summary>
		internal Trigger() {
			iTaskTrigger = null;
			taskTrigger = new TaskTrigger();
			taskTrigger.TriggerSize = (ushort)Marshal.SizeOf(taskTrigger);
			taskTrigger.BeginYear = (ushort)DateTime.Today.Year;
			taskTrigger.BeginMonth = (ushort)DateTime.Today.Month;
			taskTrigger.BeginDay = (ushort)DateTime.Today.Day;
		}

		/// <summary>
		/// Internal constructor which initializes itself from
		/// from an ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">Instance of ITaskTrigger from system task scheduler.</param>
		internal Trigger(ITaskTrigger iTrigger) {
			if (iTrigger == null)
				throw new ArgumentNullException("iTrigger", "ITaskTrigger instance cannot be null");
			taskTrigger = new TaskTrigger();
			taskTrigger.TriggerSize = (ushort)Marshal.SizeOf(taskTrigger);
			iTrigger.GetTrigger(ref taskTrigger);
			iTaskTrigger = iTrigger;
		}

		#endregion

		#region Implement ICloneable
		/// <summary>
		/// Clone returns an unbound copy of the Trigger object.  It can be use
		/// on either bound or unbound original.
		/// </summary>
		/// <returns></returns>
		public object Clone() {
			Trigger newTrigger = (Trigger)this.MemberwiseClone();
			newTrigger.iTaskTrigger = null; // The clone is not bound
			return newTrigger;
		}
		#endregion

		#region Properties

		/// <summary>
		/// Get whether the Trigger is currently bound
		/// </summary>
		internal bool Bound {
			get {
				return iTaskTrigger != null;
			}
		}

		/// <summary>
		/// Gets/sets the beginning year, month, and day for the trigger.
		/// </summary>
		public DateTime BeginDate {
			get {
				return new DateTime(taskTrigger.BeginYear, taskTrigger.BeginMonth, taskTrigger.BeginDay);
			}
			set {
				taskTrigger.BeginYear = (ushort)value.Year;
				taskTrigger.BeginMonth = (ushort)value.Month;
				taskTrigger.BeginDay = (ushort)value.Day;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets indication that the task uses an EndDate.  Returns true if a value has been
		/// set for the EndDate property.  Set can only be used to turn indication off.
		/// </summary>
		/// <exception cref="ArgumentException">Has EndDate becomes true only by setting the EndDate
		/// property.</exception>
		public bool HasEndDate {
			get {
				return ((taskTrigger.Flags & (uint)TaskTriggerFlags.HasEndDate) == (uint)TaskTriggerFlags.HasEndDate);
			}
			set {
				if (value)
					throw new ArgumentException("HasEndDate can only be set false");
				taskTrigger.Flags &= ~(uint)TaskTriggerFlags.HasEndDate;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets the ending year, month, and day for the trigger.  After a value has been set
		/// with EndDate, HasEndDate becomes true.
		/// </summary>
		public DateTime EndDate {
			get {
				if (taskTrigger.EndYear == 0)
					return DateTime.MinValue;
				return new DateTime(taskTrigger.EndYear, taskTrigger.EndMonth, taskTrigger.EndDay);
			}
			set {
				taskTrigger.Flags |= (uint)TaskTriggerFlags.HasEndDate;
				taskTrigger.EndYear = (ushort)value.Year;
				taskTrigger.EndMonth = (ushort)value.Month;
				taskTrigger.EndDay = (ushort)value.Day;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets the number of minutes after the trigger fires that it remains active.  Used
		/// in conjunction with <see cref="IntervalMinutes"/> to run a task repeatedly for a period of time.
		/// For example, if you want to start a task at 8:00 A.M. repeatedly restart it until 5:00 P.M.,
		/// there would be 540 minutes (9 hours) in the duration.
		/// Can also be used to terminate a task that is running when the DurationMinutes expire.  Use
		/// <see cref="KillAtDurationEnd"/> to specify that task should be terminated at that time.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Setting must be greater than or equal
		/// to the IntervalMinutes setting.</exception>
		public int DurationMinutes {
			get {
				return (int)taskTrigger.MinutesDuration;
			}
			set {
				if (value < taskTrigger.MinutesInterval)
					throw new ArgumentOutOfRangeException("DurationMinutes", value, "DurationMinutes must be greater than or equal the IntervalMinutes value");
				taskTrigger.MinutesDuration = (uint)value;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets the number of minutes between executions for a task that is to be run repeatedly.
		/// Repetition continues until the interval specified in <see cref="DurationMinutes"/> expires.
		/// IntervalMinutes are counted from the start of the previous execution.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Setting must be less than
		/// to the DurationMinutes setting.</exception>
		public int IntervalMinutes {
			get {
				return (int)taskTrigger.MinutesInterval;
			}
			set {
				if (value > taskTrigger.MinutesDuration)
					throw new ArgumentOutOfRangeException("IntervalMinutes", value, "IntervalMinutes must be less than or equal the DurationMinutes value");
				taskTrigger.MinutesInterval = (uint)value;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets whether task will be killed (terminated) when DurationMinutes expires.
		/// See <see cref="Trigger.DurationMinutes"/>.
		/// </summary>
		public bool KillAtDurationEnd {
			get {
				return ((taskTrigger.Flags & (uint)TaskTriggerFlags.KillAtDurationEnd) == (uint)TaskTriggerFlags.KillAtDurationEnd);
			}
			set {
				if (value)
					taskTrigger.Flags |= (uint)TaskTriggerFlags.KillAtDurationEnd;
				else
					taskTrigger.Flags &= ~(uint)TaskTriggerFlags.KillAtDurationEnd;
				SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets whether trigger is disabled.
		/// </summary>
		public bool Disabled {
			get {
				return ((taskTrigger.Flags & (uint)TaskTriggerFlags.Disabled) == (uint)TaskTriggerFlags.Disabled);
			}
			set {
				if (value)
					taskTrigger.Flags |= (uint)TaskTriggerFlags.Disabled;
				else
					taskTrigger.Flags &= ~(uint)TaskTriggerFlags.Disabled;
				SyncTrigger();
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Creates a new, bound Trigger object from an ITaskTrigger interface.  The type of the
		/// concrete object created is determined by the type of ITaskTrigger.
		/// </summary>
		/// <param name="iTaskTrigger">Instance of ITaskTrigger.</param>
		/// <returns>One of the concrete classes derived from Trigger.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException">Unable to recognize trigger type.</exception>
		internal static Trigger CreateTrigger(ITaskTrigger iTaskTrigger) {
			if (iTaskTrigger == null)
				throw new ArgumentNullException("iTaskTrigger", "Instance of ITaskTrigger cannot be null");
			TaskTrigger sTaskTrigger = new TaskTrigger();
			sTaskTrigger.TriggerSize = (ushort)Marshal.SizeOf(sTaskTrigger);
			iTaskTrigger.GetTrigger(ref sTaskTrigger);
			switch ((TriggerType)sTaskTrigger.Type) {
			case TriggerType.RunOnce:
				return new RunOnceTrigger(iTaskTrigger);
			case TriggerType.RunDaily:
				return new DailyTrigger(iTaskTrigger);
			case TriggerType.RunWeekly:
				return new WeeklyTrigger(iTaskTrigger);
			case TriggerType.RunMonthlyDOW:
				return new MonthlyDOWTrigger(iTaskTrigger);
			case TriggerType.RunMonthly:
				return new MonthlyTrigger(iTaskTrigger);
			case TriggerType.OnIdle:
				return new OnIdleTrigger(iTaskTrigger);
			case TriggerType.OnSystemStart:
				return new OnSystemStartTrigger(iTaskTrigger);
			case TriggerType.OnLogon:
				return new OnLogonTrigger(iTaskTrigger);
			default:
				throw new ArgumentException("Unable to recognize type of trigger referenced in iTaskTrigger",
											"iTaskTrigger");
			}
		}

		/// <summary>
		/// When a bound Trigger is changed, the corresponding trigger in the system
		/// Task Scheduler is updated to stay in sync with the local structure.
		/// </summary>
		protected void SyncTrigger() {
			if (iTaskTrigger!=null) iTaskTrigger.SetTrigger(ref taskTrigger);
		}

		/// <summary>
		/// Bind a Trigger object to an ITaskTrigger interface.  This causes the Trigger to
		/// sync itself with the interface and remain in sync whenever it is modified in the future.
		/// If the Trigger is already bound, an ArgumentException is thrown.
		/// </summary>
		/// <param name="iTaskTrigger">An interface representing a trigger in Task Scheduler.</param>
		/// <exception cref="ArgumentException">Attempt to bind and already bound trigger.</exception>
		internal void Bind(ITaskTrigger iTaskTrigger) {
			if (this.iTaskTrigger != null)
				throw new ArgumentException("Attempt to bind an already bound trigger");
			this.iTaskTrigger = iTaskTrigger;
			iTaskTrigger.SetTrigger(ref taskTrigger);
		}
		/// <summary>
		/// Bind a Trigger to the same interface the argument trigger is bound to.
		/// </summary>
		/// <param name="trigger">A bound Trigger. </param>
		internal void Bind(Trigger trigger) {
			Bind(trigger.iTaskTrigger);
		}

		/// <summary>
		/// Break the connection between this Trigger and the system Task Scheduler.  This
		/// releases COM resources used in bound Triggers.
		/// </summary>
		internal void Unbind() {
			if (iTaskTrigger != null) {
				Marshal.ReleaseComObject(iTaskTrigger);
				iTaskTrigger = null;
			}
		}

		/// <summary>
		/// Gets a string, supplied by the WindowsTask Scheduler, of a bound Trigger.
		/// For an unbound trigger, returns "Unbound Trigger".
		/// </summary>
		/// <returns>String representation of the trigger.</returns>
		public override string ToString() {
			if (iTaskTrigger != null) {
				IntPtr lpwstr;
				iTaskTrigger.GetTriggerString(out lpwstr);
				return CoTaskMem.LPWStrToString(lpwstr);
			} else {
				return "Unbound " + this.GetType().ToString();
			}
		}

		/// <summary>
		/// Determines if two triggers are internally equal.  Does not consider whether
		/// the Triggers are bound or not.
		/// </summary>
		/// <param name="obj">Value of trigger to compare.</param>
		/// <returns>true if triggers are equivalent.</returns>
		public override bool Equals(object obj) {
			return taskTrigger.Equals(((Trigger)obj).taskTrigger);
		}

		/// <summary>
		/// Gets a hash code for the current trigger.  A Trigger has the same hash
		/// code whether it is bound or not.
		/// </summary>
		/// <returns>Hash code value.</returns>
		public override int GetHashCode() {
			return taskTrigger.GetHashCode();
		}
		#endregion

	}

	/// <summary>
	/// Generalization of all triggers that have a start time.
	/// </summary>
	/// <remarks>StartableTrigger serves as a base class for triggers with a
	/// start time, but it has little use to clients.</remarks>
	public abstract class StartableTrigger : Trigger {
		/// <summary>
		/// Internal constructor, same as base.
		/// </summary>
		internal StartableTrigger() : base() {
		}

		/// <summary>
		/// Internal constructor from ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from system Task Scheduler.</param>
		internal StartableTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}

		/// <summary>
		/// Sets the start time of the trigger.
		/// </summary>
		/// <param name="hour">Hour of the day that the trigger will fire.</param>
		/// <param name="minute">Minute of the hour.</param>
		/// <exception cref="ArgumentOutOfRangeException">The hour is not between 0 and 23 or the minute is not between 0 and 59.</exception>
		protected void SetStartTime(ushort hour, ushort minute) {
//			if (hour < 0 || hour > 23)
//				throw new ArgumentOutOfRangeException("hour", hour, "hour must be between 0 and 23");
//			if (minute < 0 || minute > 59)
//				throw new ArgumentOutOfRangeException("minute", minute, "minute must be between 0 and 59");
//			taskTrigger.StartHour = hour;
//			taskTrigger.StartMinute = minute;
//			base.SyncTrigger();
			StartHour = (short)hour;
			StartMinute = (short)minute;
		}

		/// <summary>
		/// Gets/sets hour of the day that trigger will fire (24 hour clock).
		/// </summary>
		public short StartHour {
			get {
				return (short)taskTrigger.StartHour;
			}
			set {
				if (value < 0 || value > 23)
					throw new ArgumentOutOfRangeException("hour", value, "hour must be between 0 and 23");
				taskTrigger.StartHour = (ushort)value;
				base.SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets minute of the hour (specified in <see cref="StartHour"/>) that trigger will fire.
		/// </summary>
		public short StartMinute {
			get {
				return (short)taskTrigger.StartMinute;
			}
			set {
				if (value < 0 || value > 59)
					throw new ArgumentOutOfRangeException("minute", value, "minute must be between 0 and 59");
				taskTrigger.StartMinute = (ushort)value;
				base.SyncTrigger();
			}
		}
	}

	/// <summary>
	/// Trigger that fires once only.
	/// </summary>
	public class RunOnceTrigger : StartableTrigger {
		/// <summary>
		/// Create a RunOnceTrigger that fires when specified.
		/// </summary>
		/// <param name="runDateTime">Date and time to fire.</param>
		public RunOnceTrigger(DateTime runDateTime) : base() {
			taskTrigger.BeginYear = (ushort)runDateTime.Year;
			taskTrigger.BeginMonth = (ushort)runDateTime.Month;
			taskTrigger.BeginDay = (ushort)runDateTime.Day;
			SetStartTime((ushort)runDateTime.Hour, (ushort)runDateTime.Minute);
			taskTrigger.Type = TaskTriggerType.TIME_TRIGGER_ONCE;
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from system Task Scheduler.</param>
		internal RunOnceTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}
	}

	/// <summary>
	/// Trigger that fires at a specified time, every so many days.
	/// </summary>
	public class DailyTrigger : StartableTrigger {
		/// <summary>
		/// Creates a DailyTrigger that fires only at an interval of so many days.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of the hour trigger will fire.</param>
		/// <param name="daysInterval">Number of days between task runs.</param>
		public DailyTrigger(short hour, short minutes, short daysInterval) : base() {
			SetStartTime((ushort)hour, (ushort)minutes);
			taskTrigger.Type = TaskTriggerType.TIME_TRIGGER_DAILY;
			taskTrigger.Data.daily.DaysInterval = (ushort)daysInterval;
		}

		/// <summary>
		/// Creates DailyTrigger that fires every day.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of hour (specified in "hour") trigger will fire.</param>
		public DailyTrigger(short hour, short minutes) : this(hour, minutes, 1) {
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from system Task Scheduler.</param>
		internal DailyTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}

		/// <summary>
		/// Gets/sets the number of days between successive firings.
		/// </summary>
		public short DaysInterval {
			get {
				return (short)taskTrigger.Data.daily.DaysInterval;
			}
			set {
				taskTrigger.Data.daily.DaysInterval = (ushort)value;
				base.SyncTrigger();
			}
		}
	}

	/// <summary>
	/// Trigger that fires at a specified time, on specified days of the week,
	/// every so many weeks.
	/// </summary>
	public class WeeklyTrigger : StartableTrigger {
		/// <summary>
		/// Creates a WeeklyTrigger that is eligible to fire only during certain weeks.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of hour (specified in "hour") trigger will fire.</param>
		/// <param name="daysOfTheWeek">Days of the week task will run.</param>
		/// <param name="weeksInterval">Number of weeks between task runs.</param>
		public WeeklyTrigger(short hour, short minutes, DaysOfTheWeek daysOfTheWeek, short weeksInterval) : base() {
			SetStartTime((ushort)hour, (ushort)minutes);
			taskTrigger.Type = TaskTriggerType.TIME_TRIGGER_WEEKLY;
			taskTrigger.Data.weekly.WeeksInterval = (ushort)weeksInterval;
			taskTrigger.Data.weekly.DaysOfTheWeek = (ushort)daysOfTheWeek;
		}

		/// <summary>
		/// Creates a WeeklyTrigger that is eligible to fire during any week.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of hour (specified in "hour") trigger will fire.</param>
		/// <param name="daysOfTheWeek">Days of the week task will run.</param>
		public WeeklyTrigger(short hour, short minutes, DaysOfTheWeek daysOfTheWeek) : this(hour, minutes, daysOfTheWeek, 1) {
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger interface from system Task Scheduler.</param>
		internal WeeklyTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}

		/// <summary>
		/// Gets/sets number of weeks from one eligible week to the next.
		/// </summary>
		public short WeeksInterval {
			get {
				return (short)taskTrigger.Data.weekly.WeeksInterval;
			}
			set {
				taskTrigger.Data.weekly.WeeksInterval = (ushort)value;
				base.SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets the days of the week on which the trigger fires.
		/// </summary>
		public DaysOfTheWeek WeekDays {
			get {
				return (DaysOfTheWeek)taskTrigger.Data.weekly.DaysOfTheWeek;
			}
			set {
				taskTrigger.Data.weekly.DaysOfTheWeek = (ushort)value;
				base.SyncTrigger();
			}
		}
	}

	/// <summary>
	/// Trigger that fires at a specified time, on specified days of the week,
	/// in specified weeks of the month, during specified months of the year.
	/// </summary>
	public class MonthlyDOWTrigger : StartableTrigger {
		/// <summary>
		/// Creates a MonthlyDOWTrigger that fires during specified months only.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minute of the hour trigger will fire.</param>
		/// <param name="daysOfTheWeek">Days of the week trigger will fire.</param>
		/// <param name="whichWeeks">Weeks of the month trigger will fire.</param>
		/// <param name="months">Months of the year trigger will fire.</param>
		public MonthlyDOWTrigger(short hour, short minutes, DaysOfTheWeek daysOfTheWeek, WhichWeek whichWeeks, MonthsOfTheYear months) : base() {
			SetStartTime((ushort)hour, (ushort)minutes);
			taskTrigger.Type = TaskTriggerType.TIME_TRIGGER_MONTHLYDOW;
			taskTrigger.Data.monthlyDOW.WhichWeek = (ushort)whichWeeks;
			taskTrigger.Data.monthlyDOW.DaysOfTheWeek = (ushort)daysOfTheWeek;
			taskTrigger.Data.monthlyDOW.Months = (ushort)months;
		}

		/// <summary>
		/// Creates a MonthlyDOWTrigger that fires every month.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minute of the hour trigger will fire.</param>
		/// <param name="daysOfTheWeek">Days of the week trigger will fire.</param>
		/// <param name="whichWeeks">Weeks of the month trigger will fire.</param>
		public MonthlyDOWTrigger(short hour, short minutes, DaysOfTheWeek daysOfTheWeek, WhichWeek whichWeeks) :
			this(hour, minutes, daysOfTheWeek, whichWeeks,
			MonthsOfTheYear.January|MonthsOfTheYear.February|MonthsOfTheYear.March|MonthsOfTheYear.April|MonthsOfTheYear.May|MonthsOfTheYear.June|MonthsOfTheYear.July|MonthsOfTheYear.August|MonthsOfTheYear.September|MonthsOfTheYear.October|MonthsOfTheYear.November|MonthsOfTheYear.December) {
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from the system Task Scheduler.</param>
		internal MonthlyDOWTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}

		/// <summary>
		/// Gets/sets weeks of the month in which trigger will fire.
		/// </summary>
		public short WhichWeeks {
			get {
				return (short)taskTrigger.Data.monthlyDOW.WhichWeek;
			}
			set {
				taskTrigger.Data.monthlyDOW.WhichWeek = (ushort)value;
				base.SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets days of the week on which trigger will fire.
		/// </summary>
		public DaysOfTheWeek WeekDays {
			get {
				return (DaysOfTheWeek)taskTrigger.Data.monthlyDOW.DaysOfTheWeek;
			}
			set {
					taskTrigger.Data.monthlyDOW.DaysOfTheWeek = (ushort)value;
					base.SyncTrigger();
			}
		}

		/// <summary>
		/// Gets/sets months of the year in which trigger will fire.
		/// </summary>
		public MonthsOfTheYear Months {
			get {
				return (MonthsOfTheYear)taskTrigger.Data.monthlyDOW.Months;
			}
			set {
				taskTrigger.Data.monthlyDOW.Months = (ushort)value;
				base.SyncTrigger();
			}
		}
	}

	/// <summary>
	/// Trigger that fires at a specified time, on specified days of themonth,
	/// on specified months of the year.
	/// </summary>
	public class MonthlyTrigger : StartableTrigger {
		/// <summary>
		/// Creates a MonthlyTrigger that fires only during specified months of the year.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of hour (specified in "hour") trigger will fire.</param>
		/// <param name="daysOfMonth">Days of the month trigger will fire.  (See <see cref="Days"/> property.</param>
		/// <param name="months">Months of the year trigger will fire.</param>
		public MonthlyTrigger(short hour, short minutes, int[] daysOfMonth, MonthsOfTheYear months): base() {
			SetStartTime((ushort)hour, (ushort)minutes);
			taskTrigger.Type = TaskTriggerType.TIME_TRIGGER_MONTHLYDATE;
			taskTrigger.Data.monthlyDate.Months = (ushort)months;
			taskTrigger.Data.monthlyDate.Days = (uint)IndicesToMask(daysOfMonth);
		}

		/// <summary>
		/// Creates a MonthlyTrigger that fires during any month.
		/// </summary>
		/// <param name="hour">Hour of day trigger will fire.</param>
		/// <param name="minutes">Minutes of hour (specified in "hour") trigger will fire.</param>
		/// <param name="daysOfMonth">Days of the month trigger will fire.  (See <see cref="Days"/> property.</param>
		public MonthlyTrigger(short hour, short minutes, int[] daysOfMonth) :
			this(hour, minutes, daysOfMonth,
			MonthsOfTheYear.January|MonthsOfTheYear.February|MonthsOfTheYear.March|MonthsOfTheYear.April|MonthsOfTheYear.May|MonthsOfTheYear.June|MonthsOfTheYear.July|MonthsOfTheYear.August|MonthsOfTheYear.September|MonthsOfTheYear.October|MonthsOfTheYear.November|MonthsOfTheYear.December) {
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from system Task Scheduler.</param>
		internal MonthlyTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}


		/// <summary>
		/// Gets/sets months of the year trigger will fire.
		/// </summary>
		public MonthsOfTheYear Months {
			get {
				return (MonthsOfTheYear)taskTrigger.Data.monthlyDate.Months;
			}
			set {
				taskTrigger.Data.monthlyDOW.Months = (ushort)value;
				base.SyncTrigger();
			}
		}

		/// <summary>
		/// Convert an integer representing a mask to an array where each element contains the index
		/// of a bit that is ON in the mask.  Bits are considered to number from 1 to 32.
		/// </summary>
		/// <param name="mask">An interger to be interpreted as a mask.</param>
		/// <returns>An array with an element for each bit of the mask which is ON.</returns>
		private static int[] MaskToIndices(int mask) {
			//count bits in mask
			int cnt = 0;
			for (int i=0; (mask>>i)>0; i++)
				cnt = cnt + (1 & (mask>>i));
			//allocate return array with one entry for each bit
			int[] indices = new int[cnt];
			//fill array with bit indices
			cnt = 0;
			for (int i=0; (mask>>i)>0; i++)
				if ((1 & (mask>>i)) == 1)
					indices[cnt++] = i+1;
			return indices;
		}
		/// <summary>
		/// Converts an array of bit indices into a mask with bits  turned ON at every index
		/// contained in the array.  Indices must be from 1 to 32 and bits are numbered the same.
		/// </summary>
		/// <param name="indices">An array with an element for each bit of the mask which is ON.</param>
		/// <returns>An interger to be interpreted as a mask.</returns>
		private static int IndicesToMask(int[] indices) {
			int mask = 0;
			foreach (int index in indices) {
				if (index<1 || index>31) throw new ArgumentException("Days must be in the range 1..31");
				mask = mask | 1<<(index-1);
			}
			return mask;
		}

		/// <summary>
		/// Gets/sets days of the month trigger will fire.
		/// </summary>
		/// <value>An array with one element for each day that the trigger will fire.
		/// The value of the element is the number of the day, in the range 1..31.</value>
		public int[] Days {
			get {
				return MaskToIndices((int)taskTrigger.Data.monthlyDate.Days);
			}
			set {
				taskTrigger.Data.monthlyDate.Days = (uint)IndicesToMask(value);
				base.SyncTrigger();
			}
		}
	}

	/// <summary>
	/// Trigger that fires when the system is idle for a period of time.
	/// Length of period set by <see cref="Task.IdleWaitMinutes"/>.
	/// </summary>
	public class OnIdleTrigger : Trigger {
		/// <summary>
		/// Creates an OnIdleTrigger.  Idle period set separately.
		/// See <see cref="Task.IdleWaitMinutes"/> inherited property.
		/// </summary>
		public OnIdleTrigger() : base() {
			taskTrigger.Type = TaskTriggerType.EVENT_TRIGGER_ON_IDLE;
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">Current base Trigger.</param>
		internal OnIdleTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}
	}

	/// <summary>
	/// Trigger that fires when the system starts.
	/// </summary>
	public class OnSystemStartTrigger : Trigger {
		/// <summary>
		/// Creates an OnSystemStartTrigger.
		/// </summary>
		public OnSystemStartTrigger() : base() {
			taskTrigger.Type = TaskTriggerType.EVENT_TRIGGER_AT_SYSTEMSTART;
		}

		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger interface from system Task Scheduler.</param>
		internal OnSystemStartTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}
	}

	/// <summary>
	/// Trigger that fires when a user logs on.
	/// </summary>
	/// <remarks>Triggers of this type fire when any user logs on, not just the
	/// user identified in the account information.</remarks>
	public class OnLogonTrigger : Trigger {
		/// <summary>
		/// Creates an OnLogonTrigger.
		/// </summary>
		public OnLogonTrigger() : base() {
			taskTrigger.Type = TaskTriggerType.EVENT_TRIGGER_AT_LOGON;
		}
		/// <summary>
		/// Internal constructor to create from existing ITaskTrigger interface.
		/// </summary>
		/// <param name="iTrigger">ITaskTrigger from system Task Scheduler.</param>
		internal OnLogonTrigger(ITaskTrigger iTrigger) : base(iTrigger) {
		}
	}


}