//-------------------------------------------------------------------------------------------------
// <copyright file="MessageHandler.cs" company="Microsoft">
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
// Message handling class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Reflection;
	using System.Resources;
	using System.Text;

	/// <summary>
	/// Message event handler delegate.
	/// </summary>
	/// <param name="sender">Sender of the message.</param>
	/// <param name="e">Arguments for the message event.</param>
	public delegate void MessageEventHandler(object sender, MessageEventArgs e);

	/// <summary>
	/// Error levels for messages.
	/// </summary>
	public enum ErrorLevel
	{
		/// <summary>Normal error level, processing can continue.</summary>
		Normal,
		/// <summary>Fatal error level, processing cannot continue.</summary>
		Fatal
	}

	/// <summary>
	/// Warning levels for messages.
	/// </summary>
	public enum WarningLevel
	{
		/// <summary>Minor warning level.</summary>
		Minor,
		/// <summary>Moderate warning level.</summary>
		Moderate,
		/// <summary>Major warning level.</summary>
		Major,
		/// <summary>Deprecated warning level.  This cannot be suppressed.</summary>
		Deprecated
	}

	/// <summary>
	/// Verbosity levels for messages.
	/// </summary>
	public enum VerboseLevel
	{
		/// <summary>Debug verbosity level.</summary>
		Debug,
		/// <summary>Trace verbosity level.</summary>
		Trace,
		/// <summary>Verbose verbosity level.</summary>
		Verbose,
		/// <summary>Off verbosity level.  This will not be displayed.</summary>
		Off
	}

	/// <summary>
	/// Pedantic levels (inspired by Halo).
	/// </summary>
	public enum PedanticLevel
	{
		/// <summary>No pedantic checks.</summary>
		Easy,
		/// <summary>Normal pedantic checks.</summary>
		Heroic,
		/// <summary>Overzealous pedantic checks.</summary>
		Legendary,
	}

	/// <summary>
	/// Message handling class.
	/// </summary>
	public class MessageHandler
	{
		private const int SuccessErrorNumber = 0;

		private string shortAppName;
		private string longAppName;

		private int lastErrorNumber;

		private bool sourceTrace;
		private VerboseLevel verbosityLevel;
		private WarningLevel warningLevel;
		private bool warningAsError;

		private Hashtable suppressedWarnings;

		/// <summary>
		/// Instantiate a new message handler.
		/// </summary>
		/// <param name="shortAppName">Short application name; usually 4 uppercase characters.</param>
		/// <param name="longAppName">Long application name; usually the executable name.</param>
		public MessageHandler(string shortAppName, string longAppName)
		{
			this.shortAppName = shortAppName;
			this.longAppName = longAppName;

			this.lastErrorNumber = SuccessErrorNumber;

			this.sourceTrace = false;
			this.verbosityLevel = VerboseLevel.Off;
			this.warningLevel = WarningLevel.Minor;
			this.warningAsError = false;

			this.suppressedWarnings = new Hashtable();
		}

		/// <summary>
		/// Enum for message to display.
		/// </summary>
		protected enum MessageLevel
		{
			/// <summary>Display nothing.</summary>
			Nothing,
			/// <summary>Display information.</summary>
			Information,
			/// <summary>Display warning.</summary>
			Warning,
			/// <summary>Display error.</summary>
			Error,
			/// <summary>Display fatal error.</summary>
			FatalError
		}

		/// <summary>
		/// Gets the application error code.
		/// </summary>
		/// <value>The application error code.</value>
		public int ErrorCode
		{
			get { return this.lastErrorNumber; }
		}

		/// <summary>
		/// Gets a bool indicating whether an error has been found.
		/// </summary>
		/// <value>A bool indicating whether an error has been found.</value>
		public bool FoundError
		{
			get { return (SuccessErrorNumber != this.lastErrorNumber); }
		}

		/// <summary>
		/// Gets and sets the option to show a full source trace when messages are output.
		/// </summary>
		/// <value>The option to show a full source trace when messages are output.</value>
		public bool SourceTrace
		{
			get { return this.sourceTrace; }
			set { this.sourceTrace = value; }
		}

		/// <summary>
		/// Gets and sets the maximum verbosity level to display.
		/// </summary>
		/// <value>The maximum verbosity level to display.</value>
		public VerboseLevel MaximumVerbosityLevel
		{
			get { return this.verbosityLevel; }
			set { this.verbosityLevel = value; }
		}

		/// <summary>
		/// Gets and sets the minimum warning level to display.
		/// </summary>
		/// <value>The minimum warning level to display.</value>
		public WarningLevel MinimumWarningLevel
		{
			get { return this.warningLevel; }
			set { this.warningLevel = value; }
		}

		/// <summary>
		/// Gets and sets the option to treat warnings as errors.
		/// </summary>
		/// <value>Option to treat warnings as errors.</value>
		public bool WarningAsError
		{
			get { return this.warningAsError; }
			set { this.warningAsError = value; }
		}

		/// <summary>
		/// Called directly before exit. Allows extending classes to do logging or reporting.
		/// </summary>
		/// <returns>The exit code for the process.</returns>
		public virtual int PostProcess()
		{
			return this.ErrorCode;
		}

		/// <summary>
		/// Adds a warning message id to be suppressed in message output.
		/// </summary>
		/// <param name="warningNumber">Id of the message to suppress.</param>
		public void SuppressWarningMessage(int warningNumber)
		{
			if (!this.suppressedWarnings.ContainsKey(warningNumber))
			{
				this.suppressedWarnings.Add(warningNumber, null);
			}
		}

		/// <summary>
		/// Display a message to the console.
		/// </summary>
		/// <param name="sender">Sender of the message.</param>
		/// <param name="mea">Arguments for the message event.</param>
		public virtual void Display(object sender, MessageEventArgs mea)
		{
			MessageLevel messageLevel = this.CalculateMessageLevel(mea);
			if (MessageLevel.Nothing != messageLevel)
			{
				Console.WriteLine(this.GenerateMessageString(sender, messageLevel, mea));
			}

			// fatal errors immediately halt execution after displaying
			if (MessageLevel.FatalError == messageLevel)
			{
				throw new WixFatalErrorException(mea);
			}
		}

		/// <summary>
		/// Display a WixException to the console.
		/// </summary>
		/// <param name="errorFileName">Default file name to display in the error.</param>
		/// <param name="shortAppName">Short app name (usually uppercase and 4 letter or less).</param>
		/// <param name="we">WixException to display.</param>
		public virtual void Display(string errorFileName, string shortAppName, WixException we)
		{
			if (!(we is WixFatalErrorException)) // don't display anything for fatal error exceptions
			{
				Console.WriteLine(this.GenerateMessageString(errorFileName, shortAppName, we));
			}
		}

		/// <summary>
		/// Determines the level of this message, when taking into account warning-as-error,
		/// warning level, verbosity level and message suppressed by the caller.
		/// </summary>
		/// <param name="mea">Event arguments for the message.</param>
		/// <returns>MessageLevel representing the level of this message.</returns>
		protected MessageLevel CalculateMessageLevel(MessageEventArgs mea)
		{
			if (null == mea)
			{
				throw new ArgumentNullException("mea");
			}

			MessageLevel messageLevel = MessageLevel.Nothing;

			if (mea is WixVerbose)
			{
				if (mea.Level >= (int)this.verbosityLevel)
				{
					messageLevel = MessageLevel.Information;
				}
			}
			else if (mea is WixWarning)
			{
				if (this.suppressedWarnings.ContainsKey(mea.Id))
				{
					return MessageLevel.Nothing;
				}

				if (mea.Level >= (int)this.warningLevel)
				{
					if (this.warningAsError)
					{
						this.lastErrorNumber = mea.Id;
						messageLevel = MessageLevel.Error;
					}
					else
					{
						messageLevel = MessageLevel.Warning;
					}
				}
			}
			else if (mea is WixError)
			{
				this.lastErrorNumber = mea.Id;
				if ((int)ErrorLevel.Fatal == mea.Level)
				{
					messageLevel = MessageLevel.FatalError;
				}
				else
				{
					messageLevel = MessageLevel.Error;
				}
			}
			else
			{
				Debug.Assert(false, String.Format("Unknown MessageEventArgs type: {0}.", mea.GetType().ToString()));
			}

			return messageLevel;
		}

		/// <summary>
		/// Creates a properly formatted message string.
		/// </summary>
		/// <param name="sender">The object sending the message.</param>
		/// <param name="messageLevel">Level of the message, as generated by MessageLevel(MessageEventArgs).</param>
		/// <param name="mea">Event arguments for the message.</param>
		/// <returns>String containing the formatted message.</returns>
		protected string GenerateMessageString(object sender, MessageLevel messageLevel, MessageEventArgs mea)
		{
			if (null == mea)
			{
				throw new ArgumentNullException("mea");
			}

			if (MessageLevel.Nothing == messageLevel)
			{
				return String.Empty;
			}

			StringBuilder messageBuilder = new StringBuilder();

			string messageType = String.Empty;

			if (MessageLevel.Warning == messageLevel)
			{
				messageType = "warning";
			}
			else if (MessageLevel.Error == messageLevel)
			{
				this.lastErrorNumber = mea.Id;
				messageType = "error";
			}
			else if (MessageLevel.FatalError == messageLevel)
			{
				this.lastErrorNumber = mea.Id;
				messageType = "fatal error";
			}

			string message = String.Format(mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs);
			string errorFileName = this.longAppName;
			ArrayList fileNames = new ArrayList();

			if (null != mea.SourceLineNumbers && 0 < mea.SourceLineNumbers.Count)
			{
				bool first = true;
				foreach (SourceLineNumber sln in mea.SourceLineNumbers)
				{
					if (sln.HasLineNumber)
					{
						if (first)
						{
							first = false;
							errorFileName = String.Format("{0}({1})", sln.FileName, sln.LineNumber);
						}
						fileNames.Add(String.Format("{0}: line {1}", sln.FileName, sln.LineNumber));
					}
					else
					{
						if (first)
						{
							first = false;
							errorFileName = sln.FileName;
						}
						fileNames.Add(sln.FileName);
					}
				}
			}

			if (MessageLevel.Information == messageLevel)
			{
				messageBuilder.AppendFormat("{0}", message);
			}
			else
			{
				messageBuilder.AppendFormat("{0} : {1} {2}{3:0000} : {4}", errorFileName, messageType, this.shortAppName, mea.Id, message);
			}

			if (this.sourceTrace)
			{
				if (0 == fileNames.Count)
				{
					messageBuilder.AppendFormat("Source trace unavailable.{0}", Environment.NewLine);
				}
				else
				{
					messageBuilder.AppendFormat("Source trace:{0}", Environment.NewLine);
					foreach (string fileName in fileNames)
					{
						messageBuilder.AppendFormat("   at {0}{1}", fileName, Environment.NewLine);
					}
				}

				messageBuilder.Append(Environment.NewLine);
			}

			return messageBuilder.ToString();
		}

		/// <summary>
		/// Creates a properly formatted message string.
		/// </summary>
		/// <param name="errorFileName">Default file name to display in the error.</param>
		/// <param name="shortAppName">Short app name (usually uppercase and 4 letter or less).</param>
		/// <param name="we">WixException to display.</param>
		/// <returns>Returns the application error code.</returns>
		protected string GenerateMessageString(string errorFileName, string shortAppName, WixException we)
		{
			StringBuilder messageBuilder = new StringBuilder();

			ArrayList fileNames = new ArrayList();

			if (null != we.SourceLineNumbers && 0 < we.SourceLineNumbers.Count)
			{
				bool first = true;
				foreach (SourceLineNumber sln in we.SourceLineNumbers)
				{
					if (sln.HasLineNumber)
					{
						if (first)
						{
							first = false;
							errorFileName = String.Format("{0}({1})", sln.FileName, sln.LineNumber);
						}
						fileNames.Add(String.Format("{0}: line {1}", sln.FileName, sln.LineNumber));
					}
					else
					{
						if (first)
						{
							first = false;
							errorFileName = sln.FileName;
						}
						fileNames.Add(sln.FileName);
					}
				}
			}

			messageBuilder.AppendFormat("{0} : fatal error {1}{2:0000}: {3}", errorFileName, shortAppName, (int)we.Type, we.Message);

			if (this.sourceTrace)
			{
				messageBuilder.Append(Environment.NewLine);
				messageBuilder.Append(Environment.NewLine);

				if (0 == fileNames.Count)
				{
					messageBuilder.AppendFormat("Source trace unavailable.{0}", Environment.NewLine);
				}
				else
				{
					messageBuilder.AppendFormat("Source Trace:{0}", Environment.NewLine);
					foreach (string fileName in fileNames)
					{
						messageBuilder.AppendFormat("   at {0}{1}", fileName, Environment.NewLine);
					}
				}
			}

			return messageBuilder.ToString();
		}
	}
}
