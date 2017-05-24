using System;

namespace SIL.LCModel
{
	/// <summary>
	/// This exception indicates an error that occurred while opening or creating a LCM project.
	/// </summary>
	public class LcmInitializationException : Exception
	{
		/// <summary />
		public LcmInitializationException(string message)
			: base(message)
		{
		}

		/// <summary />
		public LcmInitializationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
