using System;

namespace NUnit.Extensions.Forms
{
	public class FormsTestAssertionException : Exception
	{
		public FormsTestAssertionException(string message)
			: base(message)
		{
		}

		public FormsTestAssertionException(string message, Exception ex)
			: base(message, ex)
		{
		}
	}
}