using System;
using System.ComponentModel;
using SIL.LCModel.Utils;

namespace GenerateHCConfig
{
	internal class NullThreadedProgress : IThreadedProgress
	{
		private readonly ISynchronizeInvoke m_synchronizeInvoke;

		public NullThreadedProgress(ISynchronizeInvoke synchronizeInvoke)
		{
			m_synchronizeInvoke = synchronizeInvoke;
		}

		public void Step(int amount)
		{
			Position += amount * StepSize;
		}

		public string Title { get; set; }

		public string Message { get; set; }

		public int Position { get; set; }

		public int StepSize { get; set; }

		public int Minimum { get; set; }

		public int Maximum { get; set; }

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		public bool IsIndeterminate { get; set; }

		public bool AllowCancel { get; set; }

		public bool IsCanceling
		{
			get { return false; }
		}

#pragma warning disable CS0067 // Event is never used
		public event CancelEventHandler Canceling;
#pragma warning restore CS0067

		public object RunTask(
			Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters
		)
		{
			return RunTask(true, backgroundTask, parameters);
		}

		public object RunTask(
			bool fDisplayUi,
			Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters
		)
		{
			return backgroundTask(this, parameters);
		}

		public bool Canceled { get; set; }
	}
}
