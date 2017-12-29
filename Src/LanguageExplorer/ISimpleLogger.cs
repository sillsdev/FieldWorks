// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// This interface provides some of the functionality of TextWriter with out the IDisposable baggage.
	/// The implementation wraps a TextWriter so more of that interface can readily be added as needed.
	/// It also manages an indentation.
	/// </summary>
	internal interface ISimpleLogger : IDisposable
	{
		/// <summary>
		/// For logging nested structures, increments the current indent level.
		/// </summary>
		void IncreaseIndent();

		/// <summary>
		/// For logging nested structures, decrements the current indent level.
		/// </summary>
		void DecreaseIndent();

		/// <summary>
		/// Write a line of text to the log (preceded by the current indent).
		/// </summary>
		/// <param name="value"></param>
		void WriteLine(string value);
	}
}