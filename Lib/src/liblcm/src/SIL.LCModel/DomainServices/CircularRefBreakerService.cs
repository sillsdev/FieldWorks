// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CircularRefBreakerService.cs
// Responsibility:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace SIL.LCModel.DomainServices
{
	/// <summary>
	/// Circular reference breaker service.
	/// Go through all the PrimaryLexeme lists of complex form LexEntryRefs searching for and fixing any circular references.
	/// If a circular reference is found, the entry with the longer headword is removed as a component (and primary lexeme)
	/// of the other one.
	/// </summary>
	public class CircularRefBreakerService
	{
		private static HashSet<Guid> m_entriesEncountered = new HashSet<Guid>();
		private static List<ILexEntryRef> m_refsProcessed = new List<ILexEntryRef>();
		private int m_count;
		private int m_circular;
		private string m_report;

		public static void ReferenceBreaker(LcmCache cache, out int m_count, out int m_circular, out string m_report)
		{
			m_count = cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances().Count(r => r.RefType == LexEntryRefTags.krtComplexForm);
			var list = cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances().Where(r => r.RefType == LexEntryRefTags.krtComplexForm);
			var bldr = new StringBuilder();
			var circularRef = 0;
			NonUndoableUnitOfWorkHelper.Do(cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					foreach (var ler in list)
					{
						if (!ler.IsValidObject)
							continue;	// we can remove LexEntryRef objects during processing, making them invalid.
						m_refsProcessed.Clear();
						m_entriesEncountered.Clear();
						if (CheckForCircularRef(ler))
						{
							#if DEBUG
							Debug.Assert(m_refsProcessed.Count > 1);
							ShowFullProcessedRefs();
							#endif
							++circularRef;
							var lim = m_refsProcessed.Count - 1;
							var entry1 = m_refsProcessed[0].OwningEntry;
							var entry2 = m_refsProcessed[lim].OwningEntry;
							// Assume that the entry with the longer headword is probably the actual complex form, so remove that one
							// from the references owned by the other entry.  If this assumption is somehow wrong, at least the user
							// is going to be notified of what happened.
							if (entry1.HeadWord.Text.Length > entry2.HeadWord.Text.Length)
								RemoveEntryFromLexEntryRef(m_refsProcessed[lim], entry1, bldr);
							else
								RemoveEntryFromLexEntryRef(m_refsProcessed[0], entry2, bldr);
							bldr.AppendLine();
						}
					}
				});
			if (bldr.Length > 0)
				bldr.Insert(0, Environment.NewLine);
			m_circular = circularRef;
			bldr.Insert(0, String.Format(Strings.ksFoundNCircularReferences, m_circular, m_count));
			m_report = bldr.ToString();
		}

		/// <summary>
		/// Remove the given entry (or any sense owned by that entry) from the given LexEntryRef.  If the LexEntryRef no
		/// longer points to anything, remove it from its owner.
		/// </summary>
		private static void RemoveEntryFromLexEntryRef(ILexEntryRef ler, ILexEntry entry, StringBuilder bldrLog)
		{
			// Remove from the Components list as well as the PrimaryLexemes list
			RemoveEntryFromList(ler.PrimaryLexemesRS, entry);
			RemoveEntryFromList(ler.ComponentLexemesRS, entry);
			ILexEntry owner = ler.OwningEntry;
			bldrLog.AppendFormat(Strings.ksRemovingCircularComponentLexeme, entry.HeadWord.Text, owner.HeadWord.Text);
			// If the Components list is now empty, delete the LexEntryRef
			if (ler.ComponentLexemesRS.Count == 0)
			{
				owner.EntryRefsOS.Remove(ler);
				bldrLog.AppendLine();
				bldrLog.AppendFormat(Strings.ksAlsoEmptyComplexFormInfo, owner.HeadWord.Text);
			}
		}

		/// <summary>
		/// Remove either the given entry or any sense owned by that entry from the list.
		/// </summary>
		private static void RemoveEntryFromList(ILcmReferenceSequence<ICmObject> list, ILexEntry entry)
		{
			var objsToRemove = new List<ICmObject>();
			foreach (var item in list)
			{
				if ((item as ILexEntry) == entry)
					objsToRemove.Add(item);
				else if (item is ILexSense && item.OwnerOfClass<ILexEntry>() == entry)
					objsToRemove.Add(item);
			}
			foreach (var item in objsToRemove)
				list.Remove(item);
		}

		/// <summary>
		/// Check whether this LexEntryRef has a circular reference in its PrimaryLexemesRS collection.
		/// The m_refsProcessed class variable is set as a side-effect of this method, and used by later
		/// processing if the method returns true.  (Using a class variable saves the noise of allocated
		/// a new list thousands of times.)  The m_entriesEncountered class variable is also set as a
		/// side-effect, but is used only by this recursive method to detect a circular reference.
		/// </summary>
		private static bool CheckForCircularRef(ILexEntryRef ler)
		{
			m_entriesEncountered.Add(ler.OwningEntry.Guid);
			m_refsProcessed.Add(ler);
			foreach (var item in ler.PrimaryLexemesRS)
			{
				var entry = item as ILexEntry ?? ((ILexSense)item).Entry;
				if (m_entriesEncountered.Contains(entry.Guid))
					return true;
				foreach (var leref in entry.ComplexFormEntryRefs)
				{
					if (CheckForCircularRef(leref))
						return true;
				}
			}
			m_refsProcessed.RemoveAt(m_refsProcessed.Count - 1);
			m_entriesEncountered.Remove(ler.OwningEntry.Guid);
			return false;
		}

		#if DEBUG
		private static void ShowFullProcessedRefs()
		{
			var bldr = new StringBuilder();
			foreach (var ler in m_refsProcessed)
			{
				bldr.AppendFormat("LexEntryRef<{0}>[Owner=\"{1}\"]:", ler.Hvo, ler.OwningEntry.HeadWord.Text);
				foreach (var item in ler.PrimaryLexemesRS)
				{
					var entry = item as ILexEntry ?? ((ILexSense)item).Entry;
					bldr.AppendFormat("  \"{0}\"", entry.HeadWord.Text);
					var sense = item as ILexSense;
					if (sense != null)
						bldr.AppendFormat("/{0}", sense.LexSenseOutline.Text);
				}
				Debug.WriteLine(bldr.ToString());
				bldr.Clear();
			}
			Debug.WriteLine("");
		}
		#endif
	}
}
