// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;

namespace Simian
{
	/// <summary>
	/// Actions are implemented in the application that uses Sensact.
	/// This interface regulates how Sensact stimulates actions.
	/// @author  Michael Lastufka
	/// @version Oct 2, 2008
	/// </summary>
	public interface IActionExec
	{
		/// <summary>
		/// Determines the result of an action via the application.
		///
		/// </summary>
		/// <param name="actionRef">An action in a rule.</param>
		/// <returns>true if the action was initiated successfully.</returns>
		bool doAction(EmptyElement actionRef);
	}
}
