// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.XPath;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class SemanticDomainReferenceLauncher : PossibilityVectorReferenceLauncher
	{
		#region Construction, Initialization, and Disposing

		/// <summary />
		public SemanticDomainReferenceLauncher()
		{
		}

		#endregion // Construction, Initialization, and Disposing

		/// <summary>
		/// Handle launching of the standard chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method, if the SimpleListChooser is not suitable.
		/// </remarks>
		protected override void HandleChooser()
		{
			const string displayWs = "best analysis";
			var sense = m_obj as ILexSense;
			if (sense == null)
			{
				Debug.Assert(sense != null, "This chooser can only be applied to senses");
				return;
			}
			var linkCommandNode = m_configurationNode.XPathSelectElement("descendant::chooserLink");
			using (var chooser = new SemanticDomainsChooser
			{
				Cache = m_cache,
				DisplayWs = displayWs,
				Sense = sense,
				LinkNode = linkCommandNode,
				HelpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)
			})
			{
				var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid), m_displayNameProperty, displayWs);
				chooser.Initialize(labels, sense.SemanticDomainsRC, PropertyTable, Publisher);
				var result = chooser.ShowDialog();
				if (result == DialogResult.OK)
				{
					UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoSet,
						Resources.DetailControlsStrings.ksRedoSet,
						m_cache.ActionHandlerAccessor,
						() => sense.SemanticDomainsRC.Replace(sense.SemanticDomainsRC, chooser.SemanticDomains));
				}
			}
		}
	}
}