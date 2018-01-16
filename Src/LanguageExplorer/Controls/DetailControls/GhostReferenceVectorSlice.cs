// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class is used (e.g., in the Info tab of Texts/Words) where a Reference Vector Slice would normally appear,
	/// except that the object that has the property does not yet exist. In fact, we do not want to create the object
	/// until the user runs the chooser and clicks OK.
	/// </summary>
	internal class GhostReferenceVectorSlice : FieldSlice
	{
		internal GhostReferenceVectorSlice(LcmCache cache, ICmObject obj, XElement configNode)
			: base(new GhostReferenceVectorLauncher(), cache, obj, GetFieldId(cache, configNode))
		{
		}

		protected override void UpdateDisplayFromDatabase()
		{
		}

		private static int GetFieldId(LcmCache cache, XElement configurationParameters)
		{
			return cache.MetaDataCacheAccessor.GetFieldId(XmlUtils.GetMandatoryAttributeValue(configurationParameters, "ghostClass"),
				XmlUtils.GetMandatoryAttributeValue(configurationParameters, "ghostField"), true);
		}

		public override void FinishInit()
		{
			base.FinishInit();

			// I (RBR) used to call InitializeFlexComponent here, but that was a second call for the slice, so it failed the checks that don't like repeat calls.
			((GhostReferenceVectorLauncher)Control).Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, DisplayNameProperty, BestWsName);
		}

		// Copied from ReferenceVectorSlice for initializing GhostReferenceVectorLauncher...may not be used.
		protected string BestWsName
		{
			get
			{
				var parameters = ConfigurationNode.Element("deParams");
				return parameters == null ? "analysis" : XmlUtils.GetOptionalAttributeValue(parameters, "ws", "analysis");
			}
		}

		// Copied from ReferenceVectorSlice for initializing GhostReferenceVectorLauncher...may not be used.
		protected string DisplayNameProperty
		{
			get
			{
				var parameters = ConfigurationNode.Element("deParams");
				return parameters == null ? string.Empty : XmlUtils.GetOptionalAttributeValue(parameters, "displayProperty", string.Empty);
			}
		}
	}
}