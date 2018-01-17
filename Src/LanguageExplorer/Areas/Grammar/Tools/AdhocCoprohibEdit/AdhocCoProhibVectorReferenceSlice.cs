// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary>
	/// Summary description for AdhocCoProhibVectorReferenceSlice.
	/// </summary>
	internal class AdhocCoProhibVectorReferenceSlice : CustomReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AdhocCoProhibVectorReferenceSlice"/> class.
		/// </summary>
		public AdhocCoProhibVectorReferenceSlice()
			: base(new AdhocCoProhibVectorLauncher())
		{
		}
	}
}