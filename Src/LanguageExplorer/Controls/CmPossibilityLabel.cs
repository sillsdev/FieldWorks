// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Structure used when returning a list of objects for a UI Widget that wants to list them.
	/// </summary>
	public class CmPossibilityLabel : ObjectLabel
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public CmPossibilityLabel(LcmCache cache, ICmPossibility pos, string displayNameProperty, string displayWs)
			: base(cache, pos, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public CmPossibilityLabel(LcmCache cache, ICmPossibility pos, string displayNameProperty)
			: base(cache, pos, displayNameProperty)
		{
		}

		/// <summary>
		/// Gets the possibility.
		/// </summary>
		/// <value>The possibility.</value>
		public ICmPossibility Possibility => (ICmPossibility)Object;

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public override ITsString AsTss
		{
			get
			{
				var cp = Possibility;
				var muaName = cp.Name;
				Debug.Assert(muaName != null);
				var muaAbbr = cp.Abbreviation;
				Debug.Assert(muaAbbr != null);
				var tisb = TsStringUtils.MakeIncStrBldr();
				var userWs = Cache.WritingSystemFactory.UserWs;
				if (m_bestWs != null)
				{
					ITsString tssAbbr = null;
					switch (m_bestWs)
					{
						case "best analysis":
							tssAbbr = muaAbbr.BestAnalysisAlternative;
							break;
						case "best vernacular":
							tssAbbr = muaAbbr.BestVernacularAlternative;
							break;
						case "best analorvern":
							tssAbbr = muaAbbr.BestAnalysisVernacularAlternative;
							break;
						case "best vernoranal":
							tssAbbr = muaAbbr.BestVernacularAnalysisAlternative;
							break;
					}
					if (m_displayNameProperty != null)
					{
						switch (m_displayNameProperty)
						{
							case "LongName":
							case "AbbrAndNameTSS":
								tisb.AppendTsString(tssAbbr);
								tisb.AppendTsString(TsStringUtils.MakeString(" - ", userWs));
								break;
							case "BestAbbreviation":
								tisb.AppendTsString(tssAbbr);
								return tisb.GetString(); // don't want any more than this
						}
					}
					switch (m_bestWs)
					{
						case "best analysis":
							tisb.AppendTsString(muaName.BestAnalysisAlternative);
							break;
						case "best vernacular":
							tisb.AppendTsString(muaName.BestVernacularAlternative);
							break;
						case "best analorvern":
							tisb.AppendTsString(muaName.BestAnalysisVernacularAlternative);
							break;
						case "best vernoranal":
							tisb.AppendTsString(muaName.BestVernacularAnalysisAlternative);
							break;
					}
				}
				else
				{
					string name = null;
					var nameWs = 0;
					string abbr = null;
					var abbrWs = 0;
					foreach (var ws in m_writingSystemIds)
					{
						var alt = muaAbbr.get_String(ws).Text;
						if (abbrWs == 0 && !string.IsNullOrEmpty(alt))
						{
							// Save abbr and ws
							abbrWs = ws;
							abbr = alt;
						}
						alt = muaName.get_String(ws).Text;
						if (nameWs == 0 && !string.IsNullOrEmpty(alt))
						{
							// Save name and ws
							nameWs = ws;
							name = alt;
						}
					}
					if (string.IsNullOrEmpty(name))
					{
						name = LanguageExplorerControls.ksQuestionMarks;
						nameWs = userWs;
					}
					if (string.IsNullOrEmpty(abbr))
					{
						abbr = LanguageExplorerControls.ksQuestionMarks;
						abbrWs = userWs;
					}
					if ((m_displayNameProperty != null)
					    && (m_displayNameProperty == "LongName"))
					{
						Debug.Assert(!string.IsNullOrEmpty(abbr));
						tisb.AppendTsString(TsStringUtils.MakeString(abbr, abbrWs));
						tisb.AppendTsString(TsStringUtils.MakeString(" - ", userWs));
					}
					Debug.Assert(!string.IsNullOrEmpty(name));
					tisb.AppendTsString(TsStringUtils.MakeString(name, nameWs));
				}

				return tisb.GetString();
			}
		}

		#endregion ITssValue Implementation

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override IEnumerable<ObjectLabel> SubItems => CreateObjectLabels(Cache, Possibility.SubPossibilitiesOS, m_displayNameProperty, m_displayWs);

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		public override bool HaveSubItems => Possibility.SubPossibilitiesOS.Count > 0;
	}
}