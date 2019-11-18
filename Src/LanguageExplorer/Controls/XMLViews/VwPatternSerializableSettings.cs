// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Wrapper to serialize/deserialize basic settings for VwPattern
	/// </summary>
	/// <remarks>This class has to be public in order to serialize it in BulkEditTabPageSettings.</remarks>
	public sealed class VwPatternSerializableSettings
	{
		private bool m_fNewlyCreated;
		private IVwPattern m_pattern;
		private int m_patternWs;
		private string m_patternString = string.Empty;
		private string m_replaceWithString = string.Empty;
		private int m_replaceWithWs;

		/// <summary>
		/// use this interface to deserialize settings to new pattern
		/// </summary>
		public VwPatternSerializableSettings()
		{
			// create a new pattern to capture deserialized settings.
			m_pattern = VwPatternClass.Create();
			m_fNewlyCreated = true;
		}

		/// <summary>
		/// use this interface to serialize the given pattern
		/// </summary>
		public VwPatternSerializableSettings(IVwPattern pattern)
		{
			m_pattern = pattern;
		}

		/// <summary>
		/// When class is used with deserializer,
		/// use this to get the pattern that was (or is to be) setup with
		/// the deserialized settings.
		/// returns null, if we haven't created one.
		/// </summary>
		public IVwPattern NewPattern => m_fNewlyCreated ? m_pattern : null;

		/// <summary />
		public string IcuCollatingRules
		{
			get { return m_pattern.IcuCollatingRules; }
			set { m_pattern.IcuCollatingRules = value; }
		}

		/// <summary />
		public string IcuLocale
		{
			get { return m_pattern.IcuLocale; }
			set { m_pattern.IcuLocale = value; }
		}

		/// <summary />
		public bool MatchCase
		{
			get { return m_pattern.MatchCase; }
			set { m_pattern.MatchCase = value; }
		}

		/// <summary />
		public bool MatchCompatibility
		{
			get { return m_pattern.MatchCompatibility; }
			set { m_pattern.MatchCompatibility = value; }
		}

		/// <summary />
		public bool MatchDiacritics
		{
			get { return m_pattern.MatchDiacritics; }
			set { m_pattern.MatchDiacritics = value; }
		}

		/// <summary />
		public bool MatchExactly
		{
			get { return m_pattern.MatchExactly; }
			set { m_pattern.MatchExactly = value; }
		}

		/// <summary />
		public bool MatchOldWritingSystem
		{
			get { return m_pattern.MatchOldWritingSystem; }
			set { m_pattern.MatchOldWritingSystem = value; }
		}

		/// <summary />
		public bool MatchWholeWord
		{
			get { return m_pattern.MatchWholeWord; }
			set { m_pattern.MatchWholeWord = value; }
		}

		/// <summary>
		/// the (first) ws used to construct the Pattern tss.
		/// </summary>
		public int PatternWs
		{
			get
			{
				if (m_patternWs == 0 && m_pattern.Pattern != null)
				{
					m_patternWs = TsStringUtils.GetWsAtOffset(m_pattern.Pattern, 0);
				}
				return m_patternWs;
			}
			set
			{
				m_patternWs = value;
				TryCreatePatternTss();
			}
		}

		private void TryCreatePatternTss()
		{
			if (m_patternWs != 0)
			{
				// create a monoWs pattern text for the new pattern.
				m_pattern.Pattern = TsStringUtils.MakeString(m_patternString, m_patternWs);
			}
		}

		/// <summary />
		public string PatternAsString
		{
			get
			{
				if (string.IsNullOrEmpty(m_patternString) && m_pattern.Pattern != null)
				{
					m_patternString = m_pattern.Pattern.Text;
				}
				return m_patternString;
			}
			set
			{
				m_patternString = value ?? string.Empty;
				TryCreatePatternTss();
			}
		}

		/// <summary />
		public string ReplaceWithAsString
		{
			get
			{
				if (string.IsNullOrEmpty(m_replaceWithString) && m_pattern.ReplaceWith != null)
				{
					m_replaceWithString = m_pattern.ReplaceWith.Text;
				}
				return m_replaceWithString;
			}
			set
			{
				m_replaceWithString = value ?? string.Empty;
				TryCreateReplaceWithTss();
			}
		}

		private void TryCreateReplaceWithTss()
		{
			if (m_replaceWithWs != 0)
			{
				// create a monoWs pattern text for the new pattern.
				m_pattern.ReplaceWith = TsStringUtils.MakeString(m_replaceWithString, m_replaceWithWs);
			}
		}

		/// <summary>
		/// the (first) ws used to construct the ReplaceWith tss.
		/// </summary>
		public int ReplaceWithWs
		{
			get
			{
				if (m_replaceWithWs == 0 && m_pattern.ReplaceWith != null)
				{
					m_replaceWithWs = TsStringUtils.GetWsAtOffset(m_pattern.ReplaceWith, 0);
				}
				return m_replaceWithWs;
			}
			set
			{
				m_replaceWithWs = value;
				TryCreateReplaceWithTss();
			}
		}

		/// <summary />
		public bool ShowMore
		{
			get { return m_pattern.ShowMore; }
			set { m_pattern.ShowMore = value; }
		}

		/// <summary />
		public bool StoppedAtLimit
		{
			get { return m_pattern.StoppedAtLimit; }
			set { m_pattern.StoppedAtLimit = value; }
		}

		/// <summary />
		public bool UseRegularExpressions
		{
			get { return m_pattern.UseRegularExpressions; }
			set { m_pattern.UseRegularExpressions = value; }
		}
	}
}