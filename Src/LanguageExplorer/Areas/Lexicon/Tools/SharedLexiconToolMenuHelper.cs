// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;

namespace LanguageExplorer.Areas.Lexicon.Tools
{
#if RANDYTODO
	// TODO: Remove this class, if there is nothing shared at the tool level.
#endif
	/// <summary>
	/// Commonly shared for all lexicon area tools.
	/// </summary>
	internal sealed class SharedLexiconToolMenuHelper : IDisposable
	{
		internal SharedLexiconToolMenuHelper()
		{
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
		}

		#region IDisposable
		private bool _isDisposed;

		~SharedLexiconToolMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}

			_isDisposed = true;
		}
		#endregion
	}
}