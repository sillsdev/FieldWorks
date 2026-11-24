// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A record clerk's own answers to the commands that act on its record list: inserting
	/// and duplicating an item, and moving to the first, previous, next or last record. It
	/// answers only while its clerk is the active clerk of the window.
	/// </summary>
	internal sealed class RecordClerkCommandAuthority : ICommandAuthority
	{
		/// <summary>The message of the commands that insert an item into the list.</summary>
		internal const string InsertItemMessage = "InsertItemInVector";

		/// <summary>The message of the commands that duplicate the current item.</summary>
		internal const string DuplicateItemMessage = "DuplicateItemInVector";

		/// <summary>The message of the First Record command.</summary>
		internal const string FirstRecordMessage = "FirstRecord";

		/// <summary>The message of the Previous Record command.</summary>
		internal const string PreviousRecordMessage = "PreviousRecord";

		/// <summary>The message of the Next Record command.</summary>
		internal const string NextRecordMessage = "NextRecord";

		/// <summary>The message of the Last Record command.</summary>
		internal const string LastRecordMessage = "LastRecord";

		private delegate void DisplayAnswer(RecordClerk clerk, Command command, ref UIItemDisplayProperties display);

		private static readonly Dictionary<string, Answer> s_answers = new Dictionary<string, Answer>(StringComparer.Ordinal)
		{
			{
				InsertItemMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.OnDisplayInsertItemInVector(command, ref display),
					(clerk, command) => clerk.OnInsertItemInVector(command))
			},
			{
				DuplicateItemMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.DisplayDuplicateItemInVector(command, display),
					(clerk, command) => clerk.DuplicateItemInVector(command))
			},
			{
				FirstRecordMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.OnDisplayFirstRecord(command, ref display),
					(clerk, command) => clerk.OnFirstRecord(command))
			},
			{
				PreviousRecordMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.OnDisplayPreviousRecord(command, ref display),
					(clerk, command) => clerk.OnPreviousRecord(command))
			},
			{
				NextRecordMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.OnDisplayNextRecord(command, ref display),
					(clerk, command) => clerk.OnNextRecord(command))
			},
			{
				LastRecordMessage, new Answer(
					(RecordClerk clerk, Command command, ref UIItemDisplayProperties display)
						=> clerk.OnDisplayLastRecord(command, ref display),
					(clerk, command) => clerk.OnLastRecord(command))
			},
		};

		private readonly RecordClerk m_clerk;

		public RecordClerkCommandAuthority(RecordClerk clerk)
		{
			m_clerk = clerk ?? throw new ArgumentNullException(nameof(clerk));
		}

		/// <summary>The messages a record clerk answers.</summary>
		internal static IEnumerable<string> AnsweredMessages => s_answers.Keys;

		public bool UpdateDisplay(Command command, UIItemDisplayProperties display)
		{
			if (!TryGetAnswer(command, out var answer))
				return false;
			answer.Display(m_clerk, command, ref display);
			return true;
		}

		public bool Execute(Command command)
		{
			if (!TryGetAnswer(command, out var answer))
				return false;
			answer.Execute(m_clerk, command);
			return true;
		}

		public bool UpdatePropertyDisplay(string propertyName, UIItemDisplayProperties display) => false;

		public bool ToggleProperty(string propertyName) => false;

		private bool TryGetAnswer(Command command, out Answer answer)
		{
			answer = null;
			return command != null && s_answers.TryGetValue(command.MessageString ?? string.Empty, out answer)
				&& m_clerk.IsActiveClerk;
		}

		private sealed class Answer
		{
			public Answer(DisplayAnswer display, Action<RecordClerk, Command> execute)
			{
				Display = display;
				Execute = execute;
			}

			public DisplayAnswer Display { get; }
			public Action<RecordClerk, Command> Execute { get; }
		}
	}
}
