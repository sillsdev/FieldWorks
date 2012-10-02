using System;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for TimeContext.
	/// </summary>
	public class TimeContext: Context
	{
		private Int32  m_ellapsedTime = 0;
		private Int32  m_expectedTime = 0;
		private bool   m_result = false;
		private string m_description = null;

		/// <summary>
		/// Constructor for TimeContext.
		/// </summary>
		/// <param name="expect">Time expected to execute in milliseconds</param>
		public TimeContext(Int32 expect)
		{
			m_tag = "monitor-time";
			m_expectedTime = expect;
		}

		public string Decsription
		{
			set { m_description = value; }
			get { return m_description; }
		}

		public Int32 EllapsedTime
		{
			get {return m_ellapsedTime;}
		}

		/// <summary>
		/// If execution takes longer than m_expectedTime, fail.
		/// </summary>
		public override void Execute()
		{
			int prevLogLevel = m_logLevel;
			if (m_logLevel != 1) m_logLevel = 2;
			base.Execute(); // includes any waiting time via ins/@wait
			// Use Instruction.m_ExecuteTickCount since it is set before any children are executed
			m_ellapsedTime = Utilities.NumTicks(m_ExecuteTickCount, System.Environment.TickCount);
			m_logLevel = prevLogLevel;
			m_result = m_ellapsedTime <= m_expectedTime;
			Logger.getOnly().result(this);
			base.Finished = true;
			if (m_result) base.Success = true; // teminates do-once
			// base.PassFailInContext(p, f, out p, out f);
			if (!m_result) fail("Ellapsed time: "+m_ellapsedTime+" exceeded expected time "+m_expectedTime);
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "ellapsed-time";
			switch (name)
			{
				case "expect": return m_expectedTime.ToString();
				case "ellapsed-time": return m_ellapsedTime.ToString();
				default:     return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_expectedTime != 0)   image += @" expect="""+m_expectedTime+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_description != null) image += @" desc=""" + m_description + @"""";
			image += @" ellapsed-time="""+m_ellapsedTime+@"""";
			if (m_expectedTime != 0) image += @" expect=""" + m_expectedTime + @"""";
			image += @" result=""" + m_result + @"""";
			return image;
		}
	}
}
