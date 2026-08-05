// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Record navigation context seam: the bidirectional selection bridge over the
	/// xCore <c>RecordClerk</c>/<c>PropertyTable</c> "current record" bus. An Avalonia view *follows*
	/// the bus through <see cref="CurrentRecordChanged"/>/<see cref="CurrentRecord"/> and *publishes* its
	/// own selection back through <see cref="PublishSelection"/> (and the movement methods), so legacy
	/// and Avalonia views running concurrently stay on the same record. This bridge is coexistence
	/// infrastructure, not throwaway: the selection concept outlives WinForms.
	/// </summary>
	public interface IRecordNavigationContext
	{
		/// <summary>The record the bus currently considers selected (an <c>ICmObject</c> at the product edge).</summary>
		object CurrentRecord { get; }

		/// <summary>Raised after the bus broadcasts a new current record (follow direction).</summary>
		event EventHandler CurrentRecordChanged;

		/// <summary>Moves the bus selection to the next record, broadcasting to all views.</summary>
		bool MoveNext();

		/// <summary>Moves the bus selection to the previous record, broadcasting to all views.</summary>
		bool MovePrevious();

		/// <summary>
		/// Publishes this view's selection back to the bus (publish direction). The key identifies the
		/// record (an hvo or <c>ICmObject</c> at the product edge). Returns false if the key is not
		/// understood or the record cannot be selected.
		/// </summary>
		bool PublishSelection(object recordKey);
	}
}
