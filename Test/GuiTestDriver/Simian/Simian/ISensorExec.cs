using System;
using System.Collections.Generic;
using System.Text;

namespace Simian
{
	/// <summary>
	/// Sensors are implemented in the application that uses Sensact.
	/// This interface regulates how Sensact retrieves sense data.
	/// @author  Michael Lastufka
	/// @version Oct 2, 2008
	/// </summary>
	public interface ISensorExec
	{
		/// <summary>
		/// Determines the result of a sensation via the application.
		///
		/// </summary>
		/// <param name="sensorRef">A sensor expression in a rule.</param>
		/// <returns>true if the sensor detected its target.</returns>
		bool sensation(EmptyElement sensorRef);
	}
}
