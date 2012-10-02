using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception raised when Paratext is not installed correctly or Paratext Project cannot be
	/// loaded.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextLoadException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ParatextLoadException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception</param>
		/// ------------------------------------------------------------------------------------
		public ParatextLoadException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
