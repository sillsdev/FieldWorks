// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product selection bridge: implements <see cref="IRecordNavigationContext"/> over
	/// a real xCore <see cref="RecordClerk"/> so an Avalonia view follows and publishes the same
	/// "current record" bus the legacy views use.
	///
	/// Follow direction: <see cref="RecordClerk"/> broadcasts record changes through the mediator as
	/// <c>RecordNavigation</c> messages, which only reach colleagues through a sponsoring content
	/// control. The owning host (<see cref="RecordEditView"/>) therefore feeds this bridge from its
	/// <c>OnRecordNavigation</c> handler via <see cref="NotifyCurrentRecordChanged"/> -- the event is
	/// driven by the real broadcast path, not by polling or a parallel channel.
	///
	/// Publish direction: <see cref="PublishSelection"/> routes through the clerk's real
	/// <c>OnJumpToRecord</c> handler, and the movement methods through <c>OnNextRecord</c>/
	/// <c>OnPreviousRecord</c>, so a selection made on an Avalonia view broadcasts to every legacy
	/// gacy view exactly as a legacy navigation would.
	/// </summary>
	public sealed class RecordClerkNavigationContext : IRecordNavigationContext
	{
		private readonly RecordClerk _clerk;

		public RecordClerkNavigationContext(RecordClerk clerk)
		{
			_clerk = clerk ?? throw new ArgumentNullException(nameof(clerk));
		}

		/// <inheritdoc />
		public object CurrentRecord => _clerk.CurrentObject;

		/// <inheritdoc />
		public event EventHandler CurrentRecordChanged;

		/// <inheritdoc />
		public bool MoveNext()
		{
			return _clerk.OnNextRecord(null);
		}

		/// <inheritdoc />
		public bool MovePrevious()
		{
			return _clerk.OnPreviousRecord(null);
		}

		/// <inheritdoc />
		public bool PublishSelection(object recordKey)
		{
			switch (recordKey)
			{
				case int hvo:
					return _clerk.OnJumpToRecord(hvo);
				case ICmObject obj:
					return _clerk.OnJumpToRecord(obj.Hvo);
				default:
					return false;
			}
		}

		/// <summary>
		/// Called by the sponsoring host when the real mediator broadcast delivers a record navigation
		/// for this bridge's clerk (follow direction).
		/// </summary>
		internal void NotifyCurrentRecordChanged()
		{
			CurrentRecordChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
