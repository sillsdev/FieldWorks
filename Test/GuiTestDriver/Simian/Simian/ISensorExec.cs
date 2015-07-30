// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
