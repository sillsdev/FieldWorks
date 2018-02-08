// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.DomainServices.SemanticDomainSearch;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class SemanticDomainChooserBEditControl : ComplexListChooserBEditControl, IDisposable
	{
		private Button m_suggestButton;
		private bool m_doingSuggest; // as opposed to 'regular' Preview
		private ICmSemanticDomainRepository m_semDomRepo;
		private BulkEditBar m_bar;
		private SemDomSearchCache m_searchCache;
		private ToolTip m_toolTip;

		// Cache suggestions from FakeDoIt so DoIt is faster.
		private Dictionary<int, List<ICmObject>> m_suggestionCache;

		internal SemanticDomainChooserBEditControl(LcmCache cache, IPropertyTable propertyTable, BulkEditBar bar, XElement colSpec) :
			base(cache, propertyTable, colSpec)
		{
			m_suggestButton = new Button
			{
				Text = XMLViewsStrings.ksSuggestButtonText,
				Image = ResourceHelper.SuggestLightbulb,
				ImageAlign = ContentAlignment.MiddleRight
			};
			m_toolTip = new ToolTip();
			m_toolTip.SetToolTip(m_suggestButton, XMLViewsStrings.ksSuggestButtonToolTip);
			m_doingSuggest = false;
			m_semDomRepo = cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_bar = bar;
			m_searchCache = new SemDomSearchCache(cache);
		}

		public override Button SuggestButton
		{
			get
			{
				CheckDisposed();
				return m_suggestButton;
			}
		}

		protected override void ComputeValue(List<ICmObject> chosenObjs, int hvoItem, out List<ICmObject> oldVals,
			out List<ICmObject> newVal)
		{
			if (!m_doingSuggest)
			{
				base.ComputeValue(chosenObjs, hvoItem, out oldVals, out newVal);
				return;
			}

			oldVals = GetOldVals(hvoItem);

			// ComputeValue() is used by FakeDoIt to put values in the suggestion cache,
			// and by DoIt to get values from the cache (and thereby not repeat the search).

			if (m_suggestionCache.TryGetValue(hvoItem, out newVal))
			{
				return; // This must be the DoIt pass; MakeSuggestions clears out the cache each time.
			}

			// resist the temptation to do "newVal = oldVals"
			// if we change newVal we don't want oldVals to change
			newVal = new List<ICmObject>();
			newVal.AddRange(oldVals);

			var curObject = m_cache.ServiceLocator.GetObject(hvoItem) as ILexSense;
			if (curObject == null)
			{
				return;
			}

			var matches = m_semDomRepo.FindCachedDomainsThatMatchWordsInSense(m_searchCache, curObject);

			foreach (var domain in matches)
			{
				if (!newVal.Contains(domain))
				{
					newVal.Add(domain);
				}
			}

			m_suggestionCache[hvoItem] = newVal; // This must be the FakeDoIt pass; cache semantic domains.
		}

		/// <summary>
		/// Tells SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public override void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			m_doingSuggest = true;
			ChosenObjects = new List<ICmObject>(0);
			// Unfortunately ProgressState is from FwControls which depends on LCM, so passing it as a parameter
			// to the searchCache's InitializeCache method would result in a circular dependency.
			state.PercentDone = 15;
			state.Breath(); // give the user a LITTLE hope that things are happening!
			// Should be the only time we need to loop through all the Semantic Domains
			m_searchCache.InitializeCache();
			m_suggestionCache = new Dictionary<int, List<ICmObject>>();
			base.FakeDoit(itemsToChange, tagMadeUpFieldIdentifier, tagEnabled, state);
			if (SomeChangesAreWaiting(itemsToChange, tagEnabled))
			{
				EnableButtonsIfChangesWaiting();
			}
		}

		private bool SomeChangesAreWaiting(IEnumerable<int> itemsToChange, int tagEnabled)
		{
			var itemsAsList = itemsToChange.ToList();
			return itemsAsList.Any() && itemsAsList.Any(hvo => DataAccess.get_IntProp(hvo, tagEnabled) == 1);
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			m_doingSuggest = false;
			base.FakeDoit(itemsToChange, tagMadeUpFieldIdentifier, tagEnabled, state);
		}

		protected override void m_launcher_Click(object sender, EventArgs e)
		{
			base.m_launcher_Click(sender, e);

			// Automatically launch preview if Choose... button actually chose something
			if (ChosenObjects.Any())
			{
				LaunchPreview();
			}
		}

		private void LaunchPreview()
		{
			m_bar.LaunchPreview();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				m_toolTip?.Dispose();
				m_suggestButton.Dispose();
				IsDisposed = true;
			}
			m_toolTip = null;
			m_suggestButton = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SemanticDomainChooserBEditControl()
		{
			Dispose(false);
		}
	}
}