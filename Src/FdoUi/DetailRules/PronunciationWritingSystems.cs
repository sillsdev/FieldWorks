// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The project's current pronunciation writing systems, which a Pronunciation field's
	/// writing-system choice also sets (LT-9620). Changing which writing systems that field
	/// shows is a data change, not only a view preference, so both detail views must make it.
	/// </summary>
	public static class PronunciationWritingSystems
	{
		/// <summary>
		/// Brings the project's current pronunciation writing systems into line with
		/// <paramref name="shown"/>. Does nothing when they already match. The write is not
		/// undoable, and joins a unit of work already open rather than failing inside one.
		/// </summary>
		public static void Sync(LcmCache cache, IEnumerable<CoreWritingSystemDefinition> shown)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));
			if (shown == null)
				throw new ArgumentNullException(nameof(shown));

			var wanted = shown.ToList();
			var current = cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems;
			if (wanted.Count == current.Count && current.SequenceEqual(wanted))
				return;

			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				current.Clear();
				foreach (var ws in wanted)
					current.Add(ws);
			});
		}
	}
}
