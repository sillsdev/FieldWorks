// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The specification of what to show on one interlinear line.
	/// Indicates what line (typically a field of a wordform, bundle, or segment)
	/// and which it is a field of.
	/// MorphemeLevel annotations are always also Word level.
	/// </summary>
	internal sealed class InterlinLineSpec : ICloneable
	{
		private int m_ws;
		private bool m_fMorpheme;
		private bool m_fWord;
		private int m_flidString; // the string property to use with m_ws
		private ITsString m_tssWsLabel;

		/// <summary>
		/// Compare the public property getters to the given spec.
		/// </summary>
		/// <returns>true if all the public getter values match.</returns>
		internal bool SameSpec(InterlinLineSpec spec)
		{
			return ReflectionHelper.HaveSamePropertyValues(this, spec);
		}

		internal int Flid { get; set; }

		internal WsComboContent ComboContent { get; set; }

		/// <summary>
		/// The flid referring to the string associated with the WritingSystem.
		/// </summary>
		internal int StringFlid
		{
			get => m_flidString == 0 ? Flid : m_flidString;
			set => m_flidString = value;
		}

		/// <summary>
		/// Could be a magic writing system
		/// </summary>
		internal int WritingSystem
		{
			get => m_ws;
			set
			{
				if (m_ws == value)
				{
					return;
				}
				m_ws = value;
				m_tssWsLabel = null;
			}
		}

		/// <summary>
		/// Indicate whether WritingSystem is a magic value.
		/// </summary>
		internal bool IsMagicWritingSystem => WritingSystem < 0;

		/// <summary>
		/// Get the actual ws of the WritingSystem based on the given hvo, if it refers to a non-empty alternative;
		/// otherwise, get the actual ws for wsFallback
		/// </summary>
		internal int GetActualWs(LcmCache cache, int hvo, int wsFallback)
		{
			if (StringFlid == -1)
			{
				// we depend upon someone else to determine the ws.
				return 0;
			}
			// While displaying the morph bundle, we need the ws used by the Form. If we can't get the Form from the MoForm, we
			// substitute with the Form stored in the WfiMorphBundle. But our system incorrectly assumes that this object
			// is a MoForm, so we specify that if our object is a WfiMorphBundle, use the relevant flid.
			WritingSystemServices.TryWs(cache, WritingSystem, wsFallback, hvo, cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo) is IWfiMorphBundle ? WfiMorphBundleTags.kflidForm : StringFlid, out var wsActual, out _);
			return wsActual;
		}

		internal bool LexEntryLevel => MorphemeLevel && Flid != InterlinLineChoices.kflidMorphemes;

		internal bool MorphemeLevel
		{
			get => m_fMorpheme;
			set
			{
				m_fMorpheme = value;
				if (value)
				{
					m_fWord = true;
				}
			}
		}

		internal bool WordLevel
		{
			get => m_fWord;
			set
			{
				m_fWord = value;
				if (!value)
				{
					m_fMorpheme = false;
				}
			}
		}

		internal bool Enabled { get; set; }

		internal ITsString WsLabel(LcmCache cache)
		{
			if (m_tssWsLabel != null)
			{
				return m_tssWsLabel;
			}
			string label;
			switch (m_ws)
			{
				case WritingSystemServices.kwsFirstAnal:
				case WritingSystemServices.kwsAnal:
					label = LanguageExplorerResources.ksBstAn;
					break;
				case WritingSystemServices.kwsFirstVern:
				case WritingSystemServices.kwsVern:
					label = LanguageExplorerResources.ksBstVn;
					break;
				case WritingSystemServices.kwsVernInParagraph:
					label = LanguageExplorerResources.ksBaselineAbbr;
					break;
				default:
					label = cache.ServiceLocator.WritingSystemManager.Get(m_ws).Abbreviation;
					break;
			}
			var tsb = TsStringUtils.MakeStrBldr();
			tsb.Replace(0, tsb.Length, label, FwUtils.LanguageCodeTextProps(cache.DefaultUserWs));
			m_tssWsLabel = tsb.GetString();
			return m_tssWsLabel;
		}

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}
		#endregion
	}
}