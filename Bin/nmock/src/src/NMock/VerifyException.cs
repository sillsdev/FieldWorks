using System;

namespace NMock
{
	public class VerifyException : Exception
	{

		private string reason;
		private object actual;
		private object expected;

		private const string format = "{0}\nexpected:{1}\n but was:<{2}>";

		public VerifyException(string reason, object expected, object actual) : base(reason)
		{
			this.reason = reason;
			this.expected = expected;
			this.actual = actual;
		}

		public string Reason
		{
			get { return reason; }
		}

		public object Expected
		{
			get { return expected; }
		}

		public object Actual
		{
			get { return actual; }
		}

		public override string Message
		{
			get
			{
				return String.Format(format, reason, expected, actual);
			}
		}
	}
}
