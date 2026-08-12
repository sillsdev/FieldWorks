// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>Thin UI-thread scheduling seam.</summary>
	public interface IUiScheduler
	{
		/// <summary>Whether the caller is on the UI thread.</summary>
		bool IsOnUiThread { get; }

		/// <summary>Posts work to run on the UI thread.</summary>
		void Post(Action action);
	}
}
