// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>Command-bridge seam over the xCore mediator. Routes command ids to handlers.</summary>
	public interface IXCoreCommandBridge
	{
		/// <summary>Whether a command id can currently be executed.</summary>
		bool CanExecute(string commandId);

		/// <summary>Executes a command id; returns true if a handler accepted it.</summary>
		bool Execute(string commandId, object argument = null);
	}
}
