// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal sealed class LexReferenceSequenceSlice : CustomReferenceVectorSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceSequenceSlice"/> class.
		/// </summary>
		public LexReferenceSequenceSlice()
			: base(new LexReferenceSequenceLauncher())
		{
		}
		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			Debug.Assert(Cache != null);
			Debug.Assert(ConfigurationNode != null);

			base.FinishInit();

			var hvoDisplayParent = XmlUtils.GetMandatoryIntegerAttributeValue(ConfigurationNode, "hvoDisplayParent");
			((LexReferenceSequenceLauncher)Control).DisplayParent = hvoDisplayParent != 0
				? Cache.ServiceLocator.GetObject(hvoDisplayParent) : null;
		}

		#region ILexReferenceSlice Members

#if RANDYTODO
		public override bool HandleDeleteCommand(Command cmd)
		{
			((LexReferenceMultiSlice)m_parentSlice).DeleteFromReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}
#endif

		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			((LexReferenceSequenceLauncher)Control).LaunchChooser();
		}

		/// <summary />
		public override void HandleEditCommand()
		{
			((LexReferenceMultiSlice)ParentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}
