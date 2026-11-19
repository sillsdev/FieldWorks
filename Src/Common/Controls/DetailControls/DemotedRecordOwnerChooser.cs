// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Asks the user which record takes a demoted notebook record, with the chooser both
	/// detail views show.
	/// </summary>
	public static class DemotedRecordOwnerChooser
	{
		private const string HelpTopic = "khtpDataNotebook-ChooseOwnerOfDemotedRecord";

		/// <summary>Asks which record takes a demoted top-level record; null when the user
		/// cancels.</summary>
		public static IRnGenericRec ChooseForRecord(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IReadOnlyList<IRnGenericRec> candidates)
			=> Choose(cache, mediator, propertyTable, candidates, DetailControlsStrings.ksChooseOwnerOfDemotedRecord);

		/// <summary>Asks which sibling takes a demoted subrecord; null when the user
		/// cancels.</summary>
		public static IRnGenericRec ChooseForSubrecord(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IReadOnlyList<IRnGenericRec> candidates)
			=> Choose(cache, mediator, propertyTable, candidates, DetailControlsStrings.ksChooseOwnerOfDemotedSubrecord);

		private static IRnGenericRec Choose(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IReadOnlyList<IRnGenericRec> candidates, string title)
		{
			var persistProvider = new PersistenceProvider(mediator, propertyTable);
			var labels = ObjectLabel.CreateObjectLabels(cache, candidates.Cast<ICmObject>(), "ShortName",
				cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs));
			using (var dlg = new ReallySimpleListChooser(persistProvider, labels, string.Empty,
				propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.Text = title;
				dlg.SetHelpTopic(HelpTopic);
				return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedObject as IRnGenericRec : null;
			}
		}
	}
}
