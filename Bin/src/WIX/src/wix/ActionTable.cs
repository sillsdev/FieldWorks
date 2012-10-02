//-------------------------------------------------------------------------------------------------
// <copyright file="ActionTable.cs" company="Microsoft">
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
// Collection of actions indexed by table an name.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Xml;

	/// <summary>
	/// Collection of actions indexed by table an name.
	/// </summary>
	public class ActionTable : IEnumerable
	{
		private Hashtable actions;

		/// <summary>
		/// Creates a new action table object.
		/// </summary>
		public ActionTable()
		{
			this.actions = new Hashtable();
		}

		/// <summary>
		/// Creates a new action table object and populates it from an Xml stream.
		/// </summary>
		/// <param name="reader">Xml reader that contains serialized actions.</param>
		public ActionTable(XmlReader reader)
		{
			if (null == reader)
			{
				throw new ArgumentNullException("reader");
			}

			this.actions = new Hashtable();

			reader.ReadStartElement();

			// loop through the rest of the xml building up the hash of TableDefinition objects
			do
			{
				if (0 == reader.Depth)
				{
					break;
				}
				else if (1 != reader.Depth)
				{
					break; // TODO: throw exception since we should only be processing tables
				}

				if (XmlNodeType.Element == reader.NodeType && "action" == reader.Name)
				{
					this.ParseAction(reader);
				}
			}
			while (reader.Read());
		}

		/// <summary>
		/// Gets an action out of the table by action table and action name.
		/// </summary>
		/// <param name="sequenceType">Sequence the action belongs to.</param>
		/// <param name="actionId">Name of the action to find.</param>
		/// <value>Action matching description.</value>
		public Action this[SequenceType sequenceType, string actionId]
		{
			get { return (Action)this.actions[String.Concat(sequenceType.ToString(), actionId)]; }
		}

		/// <summary>
		/// Adds an action to the action table.
		/// </summary>
		/// <param name="action">Action to add to the table.</param>
		public void Add(Action action)
		{
			if (null == action)
			{
				throw new ArgumentNullException("action");
			}

			// if this action hasn't already been added to the table, add it
			string key = String.Concat(action.SequenceType.ToString(), action.Id);
			if (!this.actions.Contains(key))
			{
				this.actions.Add(key, action);
			}
		}

		/// <summary>
		/// Adds an action to the action table, and optionally overrides an existing action.
		/// </summary>
		/// <param name="action">Action to add to the table.</param>
		/// <param name="overwrite">Flag to overwrite action if it already exists.</param>
		public void Add(Action action, bool overwrite)
		{
			if (null == action)
			{
				throw new ArgumentNullException("action");
			}

			// if this action hasn't already been added to the table, add it
			string key = String.Concat(action.SequenceType.ToString(), action.Id);
			if (overwrite)
			{
				this.actions[key] = action;
			}
			else if (!this.actions.Contains(key))
			{
				this.actions.Add(key, action);
			}
		}

		/// <summary>
		/// Checks to see if an action is contained by the action table.
		/// </summary>
		/// <param name="action">Action to try to locate in the table.</param>
		/// <returns>True if table contains the specified action.</returns>
		public bool Contains(Action action)
		{
			if (null == action)
			{
				throw new ArgumentNullException("action");
			}

			string key = String.Concat(action.SequenceType.ToString(), action.Id);
			return this.actions.Contains(key);
		}

		/// <summary>
		/// Gets the enumerator for the hash table collection.
		/// </summary>
		/// <returns>Enumerator for table.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.actions.Values.GetEnumerator();
		}

		/// <summary>
		/// Removes an action from the action table.
		/// </summary>
		/// <param name="action">Action to remove from the table.</param>
		public void Remove(Action action)
		{
			if (null == action)
			{
				throw new ArgumentNullException("action");
			}

			string key = String.Concat(action.SequenceType.ToString(), action.Id);
			this.actions.Remove(key);
			return;
		}

		/// <summary>
		/// Populates the hashtable from the Xml reader.
		/// </summary>
		/// <param name="reader">Xml reader that contains serialized actions.</param>
		private void ParseAction(XmlReader reader)
		{
			string id = null;
			string condition = null;
			int sequence = 0;

			SequenceType[] sequenceTypes = new SequenceType[5];
			int sequenceCount = 0;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.Name)
				{
					case "name":
						id = reader.Value;
						break;
					case "condition":
						condition = reader.Value;
						break;
					case "sequence":
						sequence = Convert.ToInt32(reader.Value);
						break;
					case "AdminExecuteSequence":
						if (Common.IsYes(reader.Value, null, reader.Name, null, null))
						{
							sequenceTypes[sequenceCount] = SequenceType.adminExecute;
							++sequenceCount;
						}
						break;
					case "AdminUISequence":
						if (Common.IsYes(reader.Value, null, reader.Name, null, null))
						{
							sequenceTypes[sequenceCount] = SequenceType.adminUI;
							++sequenceCount;
						}
						break;
					case "AdvtExecuteSequence":
						if (Common.IsYes(reader.Value, null, reader.Name, null, null))
						{
							sequenceTypes[sequenceCount] = SequenceType.advertiseExecute;
							++sequenceCount;
						}
						break;
					case "InstallExecuteSequence":
						if (Common.IsYes(reader.Value, null, reader.Name, null, null))
						{
							sequenceTypes[sequenceCount] = SequenceType.installExecute;
							++sequenceCount;
						}
						break;
					case "InstallUISequence":
						if (Common.IsYes(reader.Value, null, reader.Name, null, null))
						{
							sequenceTypes[sequenceCount] = SequenceType.installUI;
							++sequenceCount;
						}
						break;
				}
			}
			if (null == id)
			{
				throw new ApplicationException("cannot have null id attribute on action element");
			}
			else if (0 == sequence)
			{
				throw new ApplicationException("must have sequence attribute with value greater than zero on action element");
			}
			else if (0 == sequenceCount)
			{
				throw new ApplicationException("must have one sequence allowed on action element");
			}

			// now add all the sequences
			for (int i = 0; i < sequenceCount; i++)
			{
				Action action = new Action(sequenceTypes[i], id, condition, sequence);
				this.actions.Add(String.Concat(sequenceTypes[i].ToString(), id), action);
			}
		}
	}
}
