//-------------------------------------------------------------------------------------------------
// <copyright file="Tracer.cs" company="Microsoft">
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
// Utility class for working with the trace log.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Globalization;
	using System.Reflection;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.Win32;

	/// <summary>
	/// Contains utility methods for working with the trace log.
	/// </summary>
	public sealed class Tracer
	{
		#region Member Variables
		//=========================================================================================
		// Member Variables
		//=========================================================================================

		/// <summary>
		/// The name of a constructor method exposed as a field to promote naming consistency.
		/// </summary>
		public static readonly string ConstructorMethodName = "Constructor";

		/// <summary>
		/// The name of a finalizer method exposed as a field to promote naming consistency.
		/// </summary>
		public static readonly string FinalizerMethodName = "Finalizer";

		/// <summary>
		/// Used for trace messages of our own methods.
		/// </summary>
		private static readonly Type classType = typeof(Tracer);

		/// <summary>
		/// Directory under the user's temp directory for the Votive logs.
		/// </summary>
		private const string DirectoryName = "VotiveLogs";

		/// <summary>
		/// The name of the Votive trace log.
		/// </summary>
		private const string FileName = "VotiveTrace.log";

		/// <summary>
		/// The format string for the date/time log printing.
		/// </summary>
		private const string HeaderDateFormat = "MM/dd/yyyy HH:mm:ss";

		/// <summary>
		/// The maximum width of the ClassName.Method column in the trace log.
		/// </summary>
		private const int HeaderSourceInfoWidth = 70;

		/// <summary>
		/// A lot of times the exact same message is logged multiple times in a row.
		/// In order to trim down the log a little we keep track of the last logged
		/// message so that we can just append a (xN) to the end of the message.
		/// </summary>
		private static string lastMessage;
		private static int lastMessageCount = 1;

		/// <summary>
		/// Defines the level of messages to log. By default we'll only log critical events.
		/// </summary>
		private static Level level = Level.Critical;

		/// <summary>
		/// The path to the log file.
		/// </summary>
		private static string logPath;

		/// <summary>
		/// Indicates whether there were errors in initialization.
		/// </summary>
		private static Exception initializationException;

		/// <summary>
		/// Flag indicating whether to prepend the time and method information to the trace message.
		/// This is useful for the summary section to indicate that no headers are written.
		/// </summary>
		private static bool prependHeader = true;
		#endregion

		#region Constructors
		//=========================================================================================
		// Constructors
		//=========================================================================================

		/// <summary>
		/// Static class initializer.
		/// </summary>
		static Tracer()
		{
			// Get the full path to the log file. We'll create a directory in the user's
			// temp directory for the Votive log. This is to fix SourceForge bug #1122213
			// where a non-administrator would have insufficient access to write to the
			// same directory as the assembly (which is Program Files by default).
			string tempDir = Path.Combine(Path.GetTempPath(), DirectoryName);
			if (!Directory.Exists(tempDir))
			{
				try
				{
					Directory.CreateDirectory(tempDir);
				}
				catch (Exception e)
				{
					initializationException = e;
					return;
				}
			}
			logPath = Path.Combine(tempDir, FileName);

			// Delete the existing log file by creating a zero-length log file.
			if (File.Exists(logPath))
			{
				FileStream stream = null;
				try
				{
					stream = File.Open(logPath, FileMode.Truncate, FileAccess.Write, FileShare.Write);
				}
				catch (Exception e)
				{
					initializationException = e;
					return;
				}
				finally
				{
					if (stream != null)
					{
						stream.Close();
					}
				}
			}

			// We'll use the default listener as our logger. Create it if it doesn't exist.
			DefaultTraceListener listener = (DefaultTraceListener)Trace.Listeners["Default"];
			if (listener == null)
			{
				listener = new DefaultTraceListener();
				Trace.Listeners.Add(listener);
			}
			listener.LogFileName = logPath;

			Trace.IndentSize = 0;
			Trace.AutoFlush = true;

			try
			{
				// Write the first line to the trace log to make sure we can write to it.
				string firstLine = PackageUtility.SafeStringFormatInvariant("Trace started on {0}.", DateTime.Now.ToString(HeaderDateFormat, CultureInfo.InvariantCulture));
				Trace.WriteLine(firstLine);
			}
			catch (Exception e)
			{
				initializationException = e;
			}
		}

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private Tracer()
		{
		}
		#endregion

		#region Events
		//=========================================================================================
		// Events
		//=========================================================================================

		/// <summary>
		/// Raised when the trace log is ready to write the summary section. Listeners then have
		/// a chance to write specific things to the summary section.
		/// </summary>
		public static event EventHandler WritingSummarySection;
		#endregion

		#region Enums
		//=========================================================================================
		// Enums
		//=========================================================================================

		/// <summary>
		/// Trace levels for determining what to log to the trace file. These levels can be
		/// controlled at runtime via the registry key.
		/// </summary>
		public enum Level
		{
			/// <summary>
			/// Critical traces indicate a crash or other full-stop error. It is recommended that
			/// these are always traced.
			/// </summary>
			Critical,

			/// <summary>
			/// Summary information provides a way to see the results of the entire running
			/// instance of the package.
			/// </summary>
			Summary,

			/// <summary>
			/// Warnings indicate a possible failure or future failure of some operation or state.
			/// </summary>
			Warning,

			/// <summary>
			/// Information messages are useful for indicating success or expected failures of some operation or state.
			/// </summary>
			Information,

			/// <summary>
			/// Verbose messages are used only for debugging purposes and should be filtered out for ship builds.
			/// </summary>
			Verbose,
		}
		#endregion

		#region Properties
		//=========================================================================================
		// Properties
		//=========================================================================================

		/// <summary>
		/// Gets the absolute path to the trace log file.
		/// </summary>
		public static string LogPath
		{
			get { return logPath; }
		}

		/// <summary>
		/// Gets or sets the threshold
		/// </summary>
		public static Level TraceLevel
		{
			get { return level; }
			set { level = value; }
		}
		#endregion

		#region Methods
		//=========================================================================================
		// Methods
		//=========================================================================================

		/// <summary>
		/// Asserts that the specified condition is true. If it's not, a dialog box is shown with
		/// the chance to debug into the process.
		/// </summary>
		/// <param name="condition">The condition to test.</param>
		/// <param name="message">The message to show in the dialog box and to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void Assert(bool condition, string message, params object[] args)
		{
			if (!condition)
			{
				string traceMessage;
				if (args == null || args.Length == 0)
				{
					traceMessage = message;
				}
				else
				{
					traceMessage = PackageUtility.SafeStringFormatInvariant(message, args);
				}
				Trace.Fail(traceMessage);
			}
		}

		/// <summary>
		/// Pops up an assertion failure message box.
		/// </summary>
		/// <param name="message">The message to show in the dialog box.</param>
		/// <param name="args">An array of arguments used to format the message.</param>
		[Conditional("TRACE")]
		public static void Fail(string message, params object[] args)
		{
			Assert(false, message, args);
		}

		/// <summary>
		/// Indents the next trace message.
		/// </summary>
		[Conditional("TRACE")]
		public static void Indent()
		{
			Trace.Indent();
		}

		/// <summary>
		/// Initializes the tracer by reading the trace settings from the registry. Note that this
		/// can't be part of the static constructor because we need to have a <see cref="PackageContext"/>
		/// object before reading the settings from the registry.
		/// </summary>
		/// <param name="context">The package context that provides the registry information.</param>
		[Conditional("TRACE")]
		public static void Initialize(PackageContext context)
		{
			if (initializationException == null)
			{
				ReadRegistrySettings(context);
			}
			else
			{
				string title = context.NativeResources.GetString(ResourceId.IDS_E_TRACELOG_CREATION_TITLE, LogPath);
				string message = context.NativeResources.GetString(ResourceId.IDS_E_TRACELOG_CREATION, initializationException.Message);
				context.ShowErrorMessageBox(title, message);
			}
		}

		/// <summary>
		/// Decreases the indentation level by one for the next trace message.
		/// </summary>
		[Conditional("TRACE")]
		public static void Unindent()
		{
			Trace.Unindent();
		}

		/// <summary>
		/// Verifies that the specified argument is within bounds, asserting if it is not and throwing
		/// a new <see cref="ArgumentOutOfRangeException"/>. This should not be marked with [Conditional("DEBUG")].
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		/// <param name="minValue">The minimum value that the argument can be (inclusive).</param>
		/// <param name="maxValue">The maximum value that the argument can be (inclusive).</param>
		public static void VerifyArgumentBounds(int argument, string argumentName, int minValue, int maxValue)
		{
			if (argument < minValue || argument > maxValue)
			{
				string message = PackageUtility.SafeStringFormatInvariant("The argument '{0}' is not within bounds.", argumentName);
				Trace.Fail(message);
				throw new ArgumentOutOfRangeException(argumentName, argument, message);
			}
		}

		/// <summary>
		/// Verifies that the specified argument is a valid enum value using reflection, asserting if it is not and throwing
		/// a new <see cref="InvalidEnumArgumentException"/>. This should not be marked with [Conditional("DEBUG")].
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		/// <param name="enumType">The type of enum to verify against.</param>
		public static void VerifyEnumArgument(int argument, string argumentName, Type enumType)
		{
			if (!Enum.IsDefined(enumType, argument))
			{
				Fail("The argument '{0}' is not a valid {1} type.", argumentName, enumType.Name);
				throw new InvalidEnumArgumentException(argumentName, argument, enumType);
			}
		}

		/// <summary>
		/// Verifies that the specified array argument is non-null and not empty (zero length), asserting if
		/// it is not and throwing either an <see cref="ArgumentNullException"/> or <see cref="ArgumentException"/>.
		/// This should not be marked with [Conditional("DEBUG")].
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		public static void VerifyNonEmptyArrayArgument(Array argument, string argumentName)
		{
			VerifyNonNullArgument(argument, argumentName);

			if (argument.Length == 0)
			{
				string message = PackageUtility.SafeStringFormatInvariant("The argument '{0}' is an empty array.", argumentName);
				Fail(message);
				throw new ArgumentException(message, argumentName);
			}
		}

		/// <summary>
		/// Verifies that the specified argument is non-null, asserting if it is not and throwing a new
		/// <see cref="ArgumentNullException"/>. This should not be marked with [Conditional("DEBUG")].
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		public static void VerifyNonNullArgument(object argument, string argumentName)
		{
			if (argument == null)
			{
				Fail("The argument '{0}' is null.", argumentName);
				throw new ArgumentNullException(argumentName);
			}
		}

		/// <summary>
		/// Verifies that the specified string argument is non-null and non-empty, asserting if it
		/// is not and throwing a new <see cref="ArgumentException"/>. This should not be marked
		/// with the [Conditional("DEBUG")] attribute, since we want it to be called all of the time.
		/// </summary>
		/// <param name="argument">The argument to check.</param>
		/// <param name="argumentName">The name of the argument.</param>
		public static void VerifyStringArgument(string argument, string argumentName)
		{
			if (argument == null || argument.Trim().Length == 0)
			{
				string message = PackageUtility.SafeStringFormatInvariant("The string argument '{0}' is null or empty.", argumentName);
				Trace.Fail("Invalid string argument", message);
				throw new ArgumentException(message, argumentName);
			}
		}

		/// <summary>
		/// Writes a line to the trace file if the category and filter should be written.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="level">The <see cref="Level"/> of the message. </param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void WriteLine(Type classType, string methodName, Level level, string message, params object[] args)
		{
			if (ShouldTrace(level))
			{
				string traceMessage = ConstructMessage(classType, methodName, message, args);
				if (String.Equals(traceMessage, lastMessage, StringComparison.Ordinal))
				{
					lastMessageCount++;
				}
				else
				{
					if (lastMessageCount > 1)
					{
						// Write how many times the message was repeated.
						Trace.WriteLine(" (x" + lastMessageCount + ") ");
					}
					else
					{
						// We're starting a new line because the message was not repeated.
						Trace.WriteLine(String.Empty);
					}
					lastMessage = traceMessage;
					lastMessageCount = 1;
					Trace.Write(traceMessage);
				}
			}
		}

		/// <summary>
		/// Writes a line to the trace file if the specified condition is true and if the
		/// filter should be written.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="level">The <see cref="Level"/> of the message. </param>
		/// <param name="condition">The condition to verify before writing to the trace log.</param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void WriteLineIf(Type classType, string methodName, Level level, bool condition, string message, params object[] args)
		{
			if (condition)
			{
				WriteLine(classType, methodName, level, message, args);
			}
		}

		/// <summary>
		/// Writes an information line to the trace file if the filter should be written.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void WriteLineInformation(Type classType, string methodName, string message, params object[] args)
		{
			WriteLine(classType, methodName, Level.Information, message, args);
		}

		/// <summary>
		/// Writes a verbose line to the trace file if the filter should be written.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void WriteLineVerbose(Type classType, string methodName, string message, params object[] args)
		{
			WriteLine(classType, methodName, Level.Verbose, message, args);
		}

		/// <summary>
		/// Writes a warning line to the trace file if the filter should be written.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		[Conditional("TRACE")]
		public static void WriteLineWarning(Type classType, string methodName, string message, params object[] args)
		{
			WriteLine(classType, methodName, Level.Warning, message, args);
		}

		/// <summary>
		/// Writes the summary section to the trace log, giving listeners a chance to write custom information.
		/// </summary>
		[Conditional("TRACE")]
		public static void WriteSummary()
		{
			if (TraceLevel < Level.Summary)
			{
				return;
			}

			int indentLevel = Trace.IndentLevel;
			int indentSize = Trace.IndentSize;
			string divider = new string('=', 80);

			prependHeader = false;
			Trace.IndentSize = 2;
			Trace.IndentLevel = 0;
			Trace.WriteLine("");
			Trace.WriteLine("");
			Trace.WriteLine(divider);
			Trace.WriteLine("Summary Information");
			Trace.WriteLine("");
			Trace.Indent();

			if (WritingSummarySection != null)
			{
				// Give everybody a chance to write out their summary information.
				WritingSummarySection(typeof(Tracer), EventArgs.Empty);
			}
			Trace.Unindent();
			Trace.WriteLine("");
			Trace.WriteLine(divider);
			Trace.IndentLevel = indentLevel;
			Trace.IndentSize = indentSize;
			prependHeader = true;
		}

		/// <summary>
		/// Constructs the trace message to write to the log.
		/// </summary>
		/// <param name="classType">The type of the class containing the method.</param>
		/// <param name="methodName">The name of the method that is being entered.</param>
		/// <param name="message">The message to trace to the log.</param>
		/// <param name="args">Optional arguments to use when formatting the message.</param>
		/// <returns>The formatted message to write to the trace log.</returns>
		private static string ConstructMessage(Type classType, string methodName, string message, params object[] args)
		{
			string traceMessage;

			if (prependHeader)
			{
				string now = DateTime.Now.ToString(HeaderDateFormat, CultureInfo.InvariantCulture);
				string formattedMessage = PackageUtility.SafeStringFormatInvariant(message, args);
				string classAndMethod = classType.Name + "." + methodName;
				// Trim the class name down to the correct number of characters.
				if (classAndMethod.Length > HeaderSourceInfoWidth)
				{
					classAndMethod = classAndMethod.Substring(0, HeaderSourceInfoWidth - 3) + "...";
				}
				else
				{
					classAndMethod = classAndMethod.PadRight(HeaderSourceInfoWidth, ' ');
				}
				Debug.Assert(classAndMethod.Length == HeaderSourceInfoWidth);
				traceMessage = PackageUtility.SafeStringFormatInvariant("[{0}] {1} {2}", now, classAndMethod, formattedMessage);
			}
			else
			{
				traceMessage = PackageUtility.SafeStringFormatInvariant(message, args);
			}

			return traceMessage;
		}

		/// <summary>
		/// Reads the tracer settings from the registry.
		/// </summary>
		/// <param name="context">The <see cref="PackageContext"/> to use for the registry access.</param>
		private static void ReadRegistrySettings(PackageContext context)
		{
			level = context.Settings.TraceLevel;
		}

		/// <summary>
		/// Returns a value indicating whether a trace message should be written to the log.
		/// </summary>
		/// <param name="level">The level of the message.</param>
		/// <returns><see langword="true"/> if the message should be traced; otherwise, <see langword="false"/>.</returns>
		private static bool ShouldTrace(Level level)
		{
			// We don't write to the log if there were initialization errors or if the level
			// does not fall within the limits that the user has specified via the registry.
			return (initializationException == null && level <= TraceLevel);
		}
		#endregion
	}
}
