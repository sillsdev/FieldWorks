// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExceptionHelper.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implements helper methods for getting information from nested exceptions
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// Helper class that makes it easier to get information out of nested exceptions to
	/// display in the UI.
	/// </summary>
	public class ExceptionHelper
	{
		/// <summary>
		/// Not intended to be instantiated, because it contains only static methods
		/// </summary>
		private ExceptionHelper()
		{
		}

		/// <summary>
		/// Get the messages from all nested exceptions
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with the messages of all nested exceptions</returns>
		public static string GetAllExceptionMessages(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			while (e != null)
			{
				strB.Append("\n\t");
				strB.Append(e.Message);
				e = e.InnerException;
			}

			strB.Remove(0, 2); // remove \n\t from beginning
			return strB.ToString();
		}

		/// <summary>
		/// Gets the exception types of all nested exceptions.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with the types of all nested exceptions. The string has the
		/// form type1(type2(type3...)).</returns>
		public static string GetExceptionTypes(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			int nTypes = 0;
			while (e != null)
			{
				if (nTypes > 0)
					strB.Append("(");
				strB.Append(e.GetType());
				nTypes++;
			}

			for (; nTypes > 1; nTypes--) // don't need ) for first type
				strB.Append(")");

			return strB.ToString();
		}

		/// <summary>
		/// Gets a string with the stack traces of all nested exceptions. The stack
		/// for the inner most exception is displayed first. Each stack is preceded
		/// by the exception type, module name, method name and message.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with stack traces of all nested exceptions.</returns>
		public static string GetAllStackTraces(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			while (e != null)
			{
				strB = new StringBuilder(
					string.Format("Stack trace for {0} in module {1}, {2} ({3}):\n{4}\n\n{5}",
					e.GetType().Name, e.Source, e.TargetSite.Name, e.Message, e.StackTrace,
					strB.ToString()));
				e = e.InnerException;
			}

			return strB.ToString();
		}

		/// <summary>
		/// Gets the names of all the target sites of nested exceptions.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>String with the names of all the target sites.</returns>
		public static string GetTargetSiteNames(Exception e)
		{
			StringBuilder strB = new StringBuilder();
			int nSite = 0;
			while (e != null)
			{
				if (nSite > 0)
					strB.Append("/");
				strB.Append(e.TargetSite.Name);
			}

			return strB.ToString();
		}

		/// <summary>
		/// Gets the inner most exception
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>Returns the inner most exception.</returns>
		public static Exception GetInnerMostException(Exception e)
		{
			while (e.InnerException != null)
				e = e.InnerException;

			return e;
		}

		/// <summary>
		/// Gets the help string.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <returns>The help link</returns>
		public static string GetHelpLink(Exception e)
		{
			string helpLink = string.Empty;
			while (e != null)
			{
				if (e.HelpLink != null && e.HelpLink != string.Empty)
					helpLink = e.HelpLink;
				e = e.InnerException;
			}

			return helpLink;
		}
	}
}
