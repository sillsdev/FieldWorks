// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenModel
{
	public class Action
	{
		public List<string> ReplaceOpRefs { get; set; }

		// Environments and StemName used in Allomorph Generator
		public List<Environment> Environments { get; set; }
		public StemName StemName { get; set; } = new StemName();

		// Variant Types, ShowInMinorEntry, and PublishEntryIn used in Variant Generator
		public List<VariantType> VariantTypes { get; set; }
		public bool ShowMinorEntry { get; set; } = true;
		public List<PublishEntryInItem> PublishEntryInItems { get; set; } = new List<PublishEntryInItem>();

		public Action()
		{
			ReplaceOpRefs = new List<string>();
			Environments = new List<Environment>();
			VariantTypes = new List<VariantType>();
		}

		public Action Duplicate()
		{
			Action newAction = new Action();
			List<string> newReplaceOpRefs = new List<string>();
			foreach (string repRef in ReplaceOpRefs)
			{
				newReplaceOpRefs.Add(repRef);
			}
			newAction.ReplaceOpRefs = newReplaceOpRefs;
			List<Environment> newEnvironments = new List<Environment>();
			foreach (Environment env in Environments)
			{
				var newEnv = new Environment();
				newEnv.Active = env.Active;
				newEnv.Guid = env.Guid;
				newEnv.Name = env.Name;
				newEnvironments.Add(newEnv);
			}
			newAction.Environments = newEnvironments;
			var newSN = new StemName();
			newSN.Active = StemName.Active;
			newSN.Guid = StemName.Guid;
			newSN.Name = StemName.Name;
			newAction.StemName = newSN;
			List<VariantType> newVariantTypes = new List<VariantType>();
			foreach (VariantType vt in VariantTypes)
			{
				var newVT = new VariantType();
				newVT.Active = vt.Active;
				newVT.Guid = vt.Guid;
				newVT.Name = vt.Name;
				newVariantTypes.Add(newVT);
			}
			newAction.VariantTypes = newVariantTypes;
			newAction.ShowMinorEntry = ShowMinorEntry;
			List<PublishEntryInItem> newPublishEntryIn = new List<PublishEntryInItem>();
			foreach (PublishEntryInItem pubItem in PublishEntryInItems)
			{
				var newPubItem = new PublishEntryInItem();
				newPubItem.Active = pubItem.Active;
				newPubItem.Guid = pubItem.Guid;
				newPubItem.Name = pubItem.Name;
				newPublishEntryIn.Add(newPubItem);
			}
			newAction.PublishEntryInItems = newPublishEntryIn;
			return newAction;
		}

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
			{
				return false;
			}
			else
			{
				Action act = (Action)obj;
				return (ReplaceOpRefs.SequenceEqual(act.ReplaceOpRefs))
					&& (Environments.SequenceEqual(act.Environments))
					&& (VariantTypes.SequenceEqual(act.VariantTypes))
					&& (PublishEntryInItems.SequenceEqual(act.PublishEntryInItems))
					&& (StemName.Equals(act.StemName))
					&& (ShowMinorEntry == act.ShowMinorEntry);
			}
		}

		public override int GetHashCode()
		{
			return Tuple
				.Create(
					ReplaceOpRefs,
					Environments,
					StemName,
					VariantTypes,
					ShowMinorEntry,
					PublishEntryInItems
				)
				.GetHashCode();
		}
	}
}
