// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>Region lifetime/disposal seam.</summary>
	public interface IRegionLifetime : IDisposable
	{
		/// <summary>Whether the region has been disposed.</summary>
		bool IsDisposed { get; }

		/// <summary>Registers a disposable to be disposed once when the region is disposed.</summary>
		void Register(IDisposable disposable);
	}
}
