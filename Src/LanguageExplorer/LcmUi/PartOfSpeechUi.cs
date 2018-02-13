// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// PartOfSpeechUi provides UI-specific methods for the PartOfSpeech class.
	/// </summary>
	public class PartOfSpeechUi : CmPossibilityUi
	{
		/// <summary>
		/// Create one. Argument must be a PartOfSpeech.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		public PartOfSpeechUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IPartOfSpeech);
		}

		internal PartOfSpeechUi() { }

		/// <summary>
		/// Handle the context menu for inserting a POS.
		/// </summary>
		public static PartOfSpeechUi CreateNewUiObject(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			if (cache == null)
			{
				throw new ArgumentNullException(nameof(cache));
			}

			PartOfSpeechUi posUi = null;
			using (var dlg = new MasterCategoryListDlg())
			{
				var newOwner = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoOwner);
				dlg.SetDlginfo(newOwner.OwningList, propertyTable, true, newOwner);
				switch (dlg.ShowDialog(propertyTable.GetValue<Form>("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						posUi = new PartOfSpeechUi(dlg.SelectedPOS);
						publisher.Publish("JumpToRecord", dlg.SelectedPOS.Hvo);
						break;
				}
			}
			return posUi;
		}

#if RANDYTODO
		/// <summary>
		/// Override to handle case of improper menu in the reversal cat list tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			Command command = (Command)commandObject;
			string tool = Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool");
			string toolChoice = PropTable.GetValue<string>(AreaServices.ToolChoice);

			if (tool == AreaServices.PosEditMachineName && toolChoice == AreaServices.ReversalToolReversalIndexPOSMachineName)
			{
				display.Visible = display.Enabled = false; // we're already there!
				return true;
			}
			else
			{
				return base.OnDisplayJumpToTool(commandObject, ref display);
			}
		}
#endif
	}
}
