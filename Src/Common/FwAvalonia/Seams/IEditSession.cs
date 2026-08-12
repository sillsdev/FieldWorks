// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Fenced edit-session boundary. The product implementation fences a
	/// real LCModel undo task; both the legacy adapter and the Avalonia editors drive commit/cancel
	/// through this contract.
	/// </summary>
	public interface IEditSession
	{
		bool IsOpen { get; }

		void Commit();

		void Cancel();
	}
}
