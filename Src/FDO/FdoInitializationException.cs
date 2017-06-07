using System;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This exception indicates an error that occurred while opening or creating a FDO project.
	/// </summary>
	public class FdoInitializationException : Exception
	{
		/// <summary />
		public FdoInitializationException(string message)
			: base(message)
		{
		}

		/// <summary />
		public FdoInitializationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
