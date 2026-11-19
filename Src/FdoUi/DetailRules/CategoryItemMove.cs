// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Move command of an affix slot or an affix template (message DataTreeMove): the
	/// category that holds it, and its move to another category.
	/// </summary>
	public static class CategoryItemMove
	{
		/// <summary>
		/// The category that owns <paramref name="target"/> when it is an affix slot or an
		/// affix template; null for anything else.
		/// </summary>
		public static IPartOfSpeech CategoryOf(ICmObject target)
		{
			if (target == null || !target.IsValidObject)
				return null;
			return target is IMoInflAffixSlot || target is IMoInflAffixTemplate ? target.Owner as IPartOfSpeech : null;
		}

		/// <summary>
		/// Moves the slot or template to <paramref name="destination"/> in one undoable step.
		/// The affixes of a moved slot take the new category. A template moved to the category
		/// that owns its own takes along its slots from its old category, except a slot whose
		/// name the new category or one above it already uses.
		/// </summary>
		/// <returns>True when it moved; false for the category it is already in, and for an
		/// object that is no slot or template.</returns>
		public static bool Move(ICmObject target, IPartOfSpeech destination, string undoText, string redoText)
		{
			var from = CategoryOf(target);
			if (from == null || destination == null || !destination.IsValidObject || from == destination)
				return false;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(undoText, redoText, target.Cache.ActionHandlerAccessor, () =>
			{
				if (target is IMoInflAffixSlot slot)
					MoveSlot(slot, destination);
				else
					MoveTemplate((IMoInflAffixTemplate)target, from, destination);
			});
			return true;
		}

		/// <summary>
		/// The affix slot named <paramref name="name"/> that <paramref name="category"/> or a
		/// category above it offers; null when none does.
		/// </summary>
		public static IMoInflAffixSlot SlotNamed(IPartOfSpeech category, string name)
		{
			for (var pos = category; pos != null; pos = pos.Owner as IPartOfSpeech)
			{
				// BestAnalysisVernacularAlternative always returns something.
				var match = pos.AllAffixSlots.FirstOrDefault(s => s.Name.BestAnalysisVernacularAlternative.Text == name);
				if (match != null)
					return match;
			}
			return null;
		}

		private static void MoveSlot(IMoInflAffixSlot slot, IPartOfSpeech destination)
		{
			destination.AffixSlotsOC.Add(slot);
			foreach (var msa in slot.Affixes.ToList())
				msa.PartOfSpeechRA = destination;
		}

		private static void MoveTemplate(IMoInflAffixTemplate template, IPartOfSpeech from, IPartOfSpeech destination)
		{
			destination.AffixTemplatesOS.Add(template);
			if (from.Owner != destination)
				return;
			foreach (var slot in template.PrefixSlotsRS.Concat(template.SuffixSlotsRS).ToList())
			{
				if (slot.Owner == from
					&& SlotNamed(destination, slot.Name.BestAnalysisVernacularAlternative.Text) == null)
				{
					MoveSlot(slot, destination);
				}
			}
		}
	}
}
