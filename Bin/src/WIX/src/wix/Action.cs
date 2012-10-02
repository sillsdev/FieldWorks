//-------------------------------------------------------------------------------------------------
// <copyright file="Action.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Action object used by the linker to order the sequence tables.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Sequence the action belongs to.
	/// </summary>
	public enum SequenceType
	{
		/// <summary>AdminUISequence</summary>
		adminUI,
		/// <summary>AdminExecuteSequence</summary>
		adminExecute,
		/// <summary>AdvertiseExecuteSequence</summary>
		advertiseExecute,
		/// <summary>InstallUISequence</summary>
		installUI,
		/// <summary>InstallExecuteSequence</summary>
		installExecute
	}

	/// <summary>
	/// Action object used by the linker to order the sequence tables.
	/// </summary>
	public class Action
	{
		private static string[] sequenceNames =
		{
			"AdminUISequence",
			"AdminExecuteSequence",
			"AdvertiseExecuteSequence",
			"InstallUISequence",
			"InstallExecuteSequence"
		};

		private SequenceType sequenceType;
		private string id;
		private string condition;
		private int sequence;

		private string before;
		private string after;
		private bool resolved;

		/// <summary>
		/// Creates an action with a specified sequence number.
		/// </summary>
		/// <param name="sequenceType">Sequence table the action belongs to.</param>
		/// <param name="id">Name of the action.</param>
		/// <param name="condition">Any condition on the action.</param>
		/// <param name="sequence">Sequence number for action</param>
		public Action(SequenceType sequenceType, string id, string condition, int sequence) :
			this(sequenceType, id, condition, sequence, null, null)
		{
		}

		/// <summary>
		/// Creates an action based on another action's sequence number
		/// </summary>
		/// <param name="sequenceType">Sequence table the action belongs to.</param>
		/// <param name="id">Name of the action.</param>
		/// <param name="condition">Any condition on the action.</param>
		/// <param name="sequence">Sequence number for action</param>
		/// <param name="before">Action this action should be sequenced before.</param>
		/// <param name="after">Action this action should be sequenced after.</param>
		public Action(SequenceType sequenceType, string id, string condition, int sequence, string before, string after)
		{
			this.sequenceType = sequenceType;
			this.id = id;
			this.condition = condition;
			this.sequence = sequence;

			this.before = before;
			this.after = after;
		}

		/// <summary>
		/// Gets the sequence type for this action.
		/// </summary>
		/// <value>Sequence type.</value>
		public SequenceType SequenceType
		{
			get { return this.sequenceType; }
		}

		/// <summary>
		/// Gets the name of the action.
		/// </summary>
		/// <value>Action name.</value>
		public string Id
		{
			get { return this.id; }
		}

		/// <summary>
		/// Gets or sets the condition of the action.
		/// </summary>
		/// <value>Action condition.</value>
		public string Condition
		{
			get { return this.condition; }
			set { this.condition = value; }
		}

		/// <summary>
		/// Gets or sets the sequence number for this action.
		/// </summary>
		/// <value>Action sequence.</value>
		public int SequenceNumber
		{
			get { return this.sequence; }
			set { this.sequence = value; }
		}

		/// <summary>
		/// Gets the action this action should be sequenced before.
		/// </summary>
		/// <value>Action before this action.</value>
		public string Before
		{
			get { return this.before; }
		}

		/// <summary>
		/// Gets the action this action should be sequenced after.
		/// </summary>
		/// <value>Action after this action.</value>
		public string After
		{
			get { return this.after; }
		}

		/// <summary>
		/// Gets and sets if the action sequence has been resolved.
		/// </summary>
		/// <value>Flag if action sequence is resolved.</value>
		public bool Resolved
		{
			get { return this.resolved; }
			set { this.resolved = value; }
		}

		/// <summary>
		/// Converts a sequence type to the string table name for it.
		/// </summary>
		/// <param name="sequenceType">Sequence type to convert.</param>
		/// <returns>String name of table for sequence type.</returns>
		public static string SequenceTypeToString(SequenceType sequenceType)
		{
			return sequenceNames[(int)sequenceType];
		}

		/// <summary>
		/// Gets the symbol for this action.
		/// </summary>
		/// <param name="section">Section the symbol should reference since actions don't belong to sections.</param>
		/// <returns>Symbol for this action.</returns>
		internal Symbol GetSymbol(Section section)
		{
			Symbol symbol = new Symbol(section, "Actions", String.Concat(Action.SequenceTypeToString(this.sequenceType), "/", this.id));
			return symbol;
		}
	}
}
