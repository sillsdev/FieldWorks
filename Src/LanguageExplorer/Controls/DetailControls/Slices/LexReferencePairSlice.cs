// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary>
	/// LexReferencePairSlice is used to support selecting
	/// of a Sense or Entry tree.
	/// </summary>
	internal sealed class LexReferencePairSlice : CustomAtomicReferenceSlice, ILexReferenceSlice
	{
		/// <summary />
		internal LexReferencePairSlice()
			: base(new LexReferencePairLauncher())
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
			ControlAsLexReferencePairLauncher.DisplayParent = hvoDisplayParent != 0 ? Cache.ServiceLocator.GetObject(hvoDisplayParent) : null;
		}

		public override bool HandleDeleteCommand()
		{
			ParentSliceAsLexReferenceMultiSlice.DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}

		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			ControlAsLexReferencePairLauncher.LaunchChooser();
		}

		/// <summary />
		public override void HandleEditCommand()
		{
			ParentSliceAsLexReferenceMultiSlice.EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		private LexReferencePairLauncher ControlAsLexReferencePairLauncher => (LexReferencePairLauncher)Control;

		private LexReferenceMultiSlice ParentSliceAsLexReferenceMultiSlice => (LexReferenceMultiSlice)ParentSlice;
	}
}