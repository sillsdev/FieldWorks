// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class handles the MorphEntry line when there is a current entry. Currently it
	/// is very nearly the same.
	/// </summary>
	internal class IhMorphEntry : IhMissingEntry
	{
		internal IhMorphEntry(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
		{
		}

		internal override int WasReal()
		{
			return 1;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}