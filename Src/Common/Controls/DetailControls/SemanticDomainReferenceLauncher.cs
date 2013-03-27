// --------------------------------------------------------------------------------------------
// Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
// </copyright>
//
// File: SemanticDomainReferenceLauncher.cs
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal class SemanticDomainReferenceLauncher : PossibilityVectorReferenceLauncher
	{
		#region Construction, Initialization, and Disposing

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			const string displayWs = "best analysis";
			var sense = m_obj as ILexSense;
			if (sense == null)
			{
				Debug.Assert(sense != null, "This chooser can only be applied to senses");
				// ReSharper disable HeuristicUnreachableCode
				//reachable in release mode you usually intelligent program.
				return;
				// ReSharper restore HeuristicUnreachableCode
			}
			var linkCommandNode = m_configurationNode.SelectSingleNode("descendant::chooserLink");
			var chooser = new SemanticDomainsChooser
				{ Mediator = m_mediator, Cache = m_cache, DisplayWs = displayWs, Sense = sense,
					LinkNode = linkCommandNode, HelpTopicProvider = m_mediator.HelpTopicProvider
			};

			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid),
				m_displayNameProperty, displayWs);
			chooser.Initialize(labels, sense.SemanticDomainsRC);
			var result = chooser.ShowDialog();
			if(result == DialogResult.OK)
			{
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoSet,
																Resources.DetailControlsStrings.ksRedoSet,
																m_cache.ActionHandlerAccessor,
																() => sense.SemanticDomainsRC.Replace(sense.SemanticDomainsRC, chooser.SemanticDomains));
			}
		}
	}
}
