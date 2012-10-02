//-------------------------------------------------------------------------------------------------
// <copyright file="MessageEventArgs.cs" company="Microsoft">
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
// Event args for message events.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Reflection;
	using System.Resources;

	/// <summary>
	/// Event args for message events.
	/// </summary>
	public abstract class MessageEventArgs : EventArgs
	{
		private SourceLineNumberCollection sourceLineNumbers;
		private int id;
		private int level;
		private string resourceName;
		private object[] messageArgs;

		/// <summary>
		/// Creates a new MessageEventArgs.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers for the message.</param>
		/// <param name="id">Id for the message.</param>
		/// <param name="level">Level for the message.</param>
		/// <param name="resourceName">Name of the resource.</param>
		/// <param name="messageArgs">Arguments for the format string.</param>
		public MessageEventArgs(SourceLineNumberCollection sourceLineNumbers, int id, int level, string resourceName, params object[] messageArgs)
		{
			this.sourceLineNumbers = sourceLineNumbers;
			this.id = id;
			this.level = level;
			this.resourceName = resourceName;
			this.messageArgs = messageArgs;
		}

		/// <summary>
		/// Gets the resource manager for this event args.
		/// </summary>
		public abstract ResourceManager ResourceManager
		{
			get;
		}

		/// <summary>
		/// Source line numbers.
		/// </summary>
		public SourceLineNumberCollection SourceLineNumbers
		{
			get { return this.sourceLineNumbers; }
		}

		/// <summary>
		/// Id for the message.
		/// </summary>
		public int Id
		{
			get { return this.id; }
		}

		/// <summary>
		/// Level for the message.
		/// </summary>
		public int Level
		{
			get { return this.level; }
		}

		/// <summary>
		/// Name of the resource.
		/// </summary>
		public string ResourceName
		{
			get { return this.resourceName; }
		}

		/// <summary>
		/// Arguments for the format string.
		/// </summary>
		public object[] MessageArgs
		{
			get { return this.messageArgs; }
		}
	}
}