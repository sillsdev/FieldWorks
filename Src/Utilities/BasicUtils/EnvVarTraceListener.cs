// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EnvVarTraceListener.cs
// Responsibility: EberhardB

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception that is fired when an assertion failed and displaying the assertion message
	/// box is disabled.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AssertionFailedException: ApplicationException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AssertionFailedException"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AssertionFailedException(string message, string detailedMessage):
			base(message + Environment.NewLine + detailedMessage)
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Customized trace listener that enhances DefaultTraceListener. It reads the value for
	/// AssertUiEnabled from the environment variable AssertUiEnabled, thus allowing to switch
	/// more easily between allowing and not allowing UI for asserts. This is helpful when
	/// different users on the same machine need to use different settings (i.e. automated build
	/// and developer on same machine).
	/// It also replaces any variable in the log file name (everything between %...%) with the
	/// corresponding environment variable.
	/// If AssertUiEnabled is <c>false</c> an AssertionFailedException will be thrown unless
	/// AssertExceptionEnabled is <c>false</c>.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EnvVarTraceListener : DefaultTraceListener
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="EnvVarTraceListener"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EnvVarTraceListener()
			: this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="EnvVarTraceListener"/> class.
		/// </summary>
		/// <param name="initializeData">The initialization data, e.g.
		/// "assertuienabled='true' logfilename='%temp%/MyAssert.log' assertionexceptionenabled='false'".</param>
		/// ------------------------------------------------------------------------------------
		public EnvVarTraceListener(string initializeData)
		{
			AssertionExceptionEnabled = true; // default to true

			if (!string.IsNullOrEmpty(initializeData))
			{
				// On Mono the settings from the <asserts> element don't get applied when we
				// derive from DefaultTraceListener, so we have to apply them manually
				var regex = new Regex("assertuienabled=('|\")(?<value>[^ '\"]*)");
				if (regex.IsMatch(initializeData))
				{
					SetEnabled(regex.Match(initializeData).Groups["value"].Value,
						f => AssertUiEnabled = f);
				}

				regex = new Regex("assertexceptionenabled=('|\")(?<value>[^ '\"]*)");
				if (regex.IsMatch(initializeData))
				{
					SetEnabled(regex.Match(initializeData).Groups["value"].Value,
						f => AssertionExceptionEnabled = f);
				}

				regex = new Regex("logfilename=('|\")(?<value>[^ '\"]*)");
				if (regex.IsMatch(initializeData))
				{
					var val = regex.Match(initializeData).Groups["value"].Value;
					if (!string.IsNullOrEmpty(val))
						LogFileName = val;
				}
			}

			SetEnabled(Environment.GetEnvironmentVariable("AssertUiEnabled"),
				f => AssertUiEnabled = f);

			SetEnabled(Environment.GetEnvironmentVariable("AssertExceptionEnabled"),
				f => AssertionExceptionEnabled = f);

			if (!string.IsNullOrEmpty(LogFileName))
				LogFileName = new Regex("%(?<name>[^%]+)%").Replace(LogFileName, EnvVarMatchEvaluator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the environment variable with its value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string EnvVarMatchEvaluator(Match match)
		{
			var envVar = match.Groups["name"].Value;
			var val = Environment.GetEnvironmentVariable(envVar);
			if (string.IsNullOrEmpty(val))
				return "%" + envVar + "%";
			return val;
		}

		private delegate void BoolProperty(bool value);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the property to value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void SetEnabled(string value, BoolProperty property)
		{
			if (string.IsNullOrEmpty(value))
				return;

			try
			{
				property(bool.Parse(value));
			}
			catch (FormatException)
			{
				// just ignore
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether a failed assertion will throw an
		/// AssertionFailedException if displaying the message box is disabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool AssertionExceptionEnabled { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Emits or displays detailed messages and a stack trace for an assertion that always
		/// fails.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Fail(string message, string detailMessage)
		{
			base.Fail(message, detailMessage);

			if (!AssertUiEnabled && AssertionExceptionEnabled)
				throw new AssertionFailedException(message, detailMessage);
		}
	}
}
