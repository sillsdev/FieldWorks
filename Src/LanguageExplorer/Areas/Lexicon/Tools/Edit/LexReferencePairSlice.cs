// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// LexReferencePairSlice is used to support selecting
	/// of a Sense or Entry tree.
	/// </summary>
	internal sealed class LexReferencePairSlice : CustomAtomicReferenceSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferencePairSlice"/> class.
		/// Constructor must be public (and with no arguments) for creation by reflection
		/// based on mention in XML configuration files.
		/// </summary>
		public LexReferencePairSlice()
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
			((LexReferencePairLauncher)Control).DisplayParent = hvoDisplayParent != 0
				? Cache.ServiceLocator.GetObject(hvoDisplayParent) : null;
		}

		#region ILexReferenceSlice Members

		public override bool HandleDeleteCommand()
		{
			((LexReferenceMultiSlice)ParentSlice).DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}

		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			((LexReferencePairLauncher)Control).LaunchChooser();
		}

		/// <summary />
		public override void HandleEditCommand()
		{
			((LexReferenceMultiSlice)ParentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}
