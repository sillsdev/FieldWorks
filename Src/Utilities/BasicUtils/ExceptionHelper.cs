// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExceptionHelper.cs
// Responsibility: Eberhard Beilharz
//
// <remarks>
// Implements helper methods for getting information from nested exceptions
// </remarks>

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SIL.Utils
{
	/// <summary>
	/// Helper class that makes it easier to get information out of nested exceptions to
	/// display in the UI.
	/// </summary>
	public static class ExceptionHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the messages from all nested exceptions
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with the messages of all nested exceptions</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetAllExceptionMessages(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			while (e != null)
			{
				if (strB.Length > 0)
					strB.AppendFormat("{0}\t", Environment.NewLine);
				strB.Append(e.Message);
				e = e.InnerException;
			}

			return strB.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string with the stack traces of all nested exceptions. The stack
		/// for the inner most exception is displayed first. Each stack is preceded
		/// by the exception type, module name, method name and message.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with stack traces of all nested exceptions.</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetAllStackTraces(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			while (e != null)
			{
				strB = new StringBuilder(
					string.Format("Stack trace for {0} in module {1}, {2} ({3}):{6}{4}{6}{6}{5}",
					e.GetType().Name, e.Source, e.TargetSite.Name, e.Message, e.StackTrace,
					strB.ToString(), Environment.NewLine));
				e = e.InnerException;
			}

			return strB.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inner most exception
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>Returns the inner most exception.</returns>
		/// ------------------------------------------------------------------------------------
		public static Exception GetInnerMostException(Exception e)
		{
			while (e.InnerException != null)
				e = e.InnerException;

			return e;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help string.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>The help link</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetHelpLink(Exception e)
		{
			string helpLink = string.Empty;
			while (e != null)
			{
				if (!string.IsNullOrEmpty(e.HelpLink))
					helpLink = e.HelpLink;
				e = e.InnerException;
			}

			return helpLink;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hiearchical exception info.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="innerMostException">The inner most exception or null if the error is
		/// the inner most exception</param>
		/// <returns>A string containing the text of the specified error</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetHiearchicalExceptionInfo(Exception error,
			out Exception innerMostException)
		{
			innerMostException = error.InnerException;
			var x = new StringBuilder();
			x.Append(GetExceptionText(error));

			if (error.InnerException != null)
			{
				x.AppendLine("**Inner Exception:");
				x.Append(GetHiearchicalExceptionInfo(error.InnerException, out innerMostException));
			}
			return x.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="error"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetExceptionText(Exception error)
		{
			StringBuilder txt = new StringBuilder();

			txt.Append("Msg: ");
			txt.AppendLine(error.Message);

			try
			{
				if (error is COMException)
				{
					txt.Append("COM message: ");
					txt.AppendLine(new Win32Exception(((COMException)error).ErrorCode).Message);
				}
			}
			catch
			{
			}

			try
			{
				txt.Append("Source: ");
				txt.AppendLine(error.Source);
			}
			catch
			{
			}

			try
			{
				if (error.TargetSite != null)
				{
					txt.Append("Assembly: ");
					txt.AppendLine(error.TargetSite.DeclaringType.Assembly.FullName);
				}
			}
			catch
			{
			}

			try
			{
				txt.Append("Stack: ");
				txt.AppendLine(error.StackTrace);
			}
			catch
			{
			}
			txt.AppendFormat("Thread: {0}", Thread.CurrentThread.Name);
			txt.AppendLine();

			txt.AppendFormat("Thread UI culture: {0}", Thread.CurrentThread.CurrentUICulture);
			txt.AppendLine();

			txt.AppendFormat("Exception: {0}", error.GetType());
			txt.AppendLine();

			try
			{
				if (error.Data.Count > 0)
				{
					txt.AppendLine("Additional Exception Information:");
					foreach (DictionaryEntry de in error.Data)
					{
						txt.AppendFormat("{0}={1}", de.Key, de.Value);
						txt.AppendLine();
					}
				}
			}
			catch
			{
			}

			return txt.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the action, logging and ignoring any exceptions.
		/// </summary>
		/// <param name="action">The action.</param>
		/// ------------------------------------------------------------------------------------
		public static void LogAndIgnoreErrors(Action action)
		{
			LogAndIgnoreErrors(action, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the action, logging and ignoring any exceptions.
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="onError">Additional action to perform after logging the error.</param>
		/// ------------------------------------------------------------------------------------
		public static void LogAndIgnoreErrors(Action action, Action<Exception> onError)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				if (onError != null)
					onError(e);
			}
		}
	}
}
