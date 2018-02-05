// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// The specification of what to show on one interlinear line.
	/// Indicates what line (typically a field of a wordform, bundle, or segment)
	/// and which it is a field of.
	/// MorphemeLevel annotations are always also Word level.
	/// </summary>
	public class InterlinLineSpec : ICloneable
	{
		int m_ws;
		bool m_fMorpheme;
		bool m_fWord;
		int m_flidString; // the string property to use with m_ws
		ITsString m_tssWsLabel;

		public InterlinLineSpec()
		{
		}

		/// <summary>
		/// Compare the public property getters to the given spec.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns>true if all the public getter values match.</returns>
		public bool SameSpec(InterlinLineSpec spec)
		{
			return ReflectionHelper.HaveSamePropertyValues(this, spec);
		}

		public int Flid { get; set; }

		public WsComboContent ComboContent { get; set; }

		/// <summary>
		/// The flid referring to the string associated with the WritingSystem.
		/// </summary>
		public int StringFlid
		{
			get
			{
				return m_flidString == 0 ? Flid : m_flidString;
			}
			set { m_flidString = value; }
		}

		/// <summary>
		/// Could be a magic writing system
		/// </summary>
		public int WritingSystem
		{
			get { return m_ws; }
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
		public bool IsMagicWritingSystem => WritingSystem < 0;

		/// <summary>
		/// Get the actual ws of the WritingSystem based on the given hvo, if it refers to a non-empty alternative;
		/// otherwise, get the actual ws for wsFallback
		/// </summary>
		public int GetActualWs(LcmCache cache, int hvo, int wsFallback)
		{
			if (StringFlid == -1)
			{
				// we depend upon someone else to determine the ws.
				return 0;
			}
			int wsActual;
			ITsString dummy;
			WritingSystemServices.TryWs(cache, WritingSystem, wsFallback, hvo, StringFlid, out wsActual, out dummy);
			return wsActual;
		}

		public bool LexEntryLevel => MorphemeLevel && Flid != InterlinLineChoices.kflidMorphemes;

		public bool MorphemeLevel
		{
			get { return m_fMorpheme; }
			set
			{
				m_fMorpheme = value;
				if (value)
				{
					m_fWord = true;
				}
			}
		}

		public bool WordLevel
		{
			get { return m_fWord; }
			set
			{
				m_fWord = value;
				if (!value)
				{
					m_fMorpheme = false;
				}
			}
		}
		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		public ITsString WsLabel(LcmCache cache)
		{
			if (m_tssWsLabel != null)
			{
				return m_tssWsLabel;
			}
			string label;
			switch (m_ws)
			{
				case WritingSystemServices.kwsFirstAnal:
					label = ITextStrings.ksBstAn;
					break;
				case WritingSystemServices.kwsVernInParagraph:
					label = ITextStrings.ksBaselineAbbr;
					break;
				default:
					label = cache.ServiceLocator.WritingSystemManager.Get(m_ws).Abbreviation;
					break;
			}
			var tsb = TsStringUtils.MakeStrBldr();
			tsb.Replace(0, tsb.Length, label, WsListManager.LanguageCodeTextProps(cache.DefaultUserWs));
			m_tssWsLabel = tsb.GetString();
			return m_tssWsLabel;
		}
		#endregion
	}
}