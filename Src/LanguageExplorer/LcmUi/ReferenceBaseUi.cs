// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// This is the base class for handling references.
	/// </summary>
	public class ReferenceBaseUi : CmObjectUi
	{
		protected int m_flid;
		protected int m_hvoTarget;
		protected CmObjectUi m_targetUi;
		public ReferenceBaseUi(LcmCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
		{
			// determine whether this is an atomic or vector relationship.
			Debug.Assert(cache.IsReferenceProperty(referenceFlid));
			Debug.Assert(rootObj != null);

			m_cache = cache;
			m_hvo = rootObj.Hvo;
			m_cmObject = rootObj;
			m_flid = referenceFlid;
			m_hvoTarget = targetHvo;
			m_targetUi = MakeUi(m_cache, m_hvoTarget);
		}

		/// <summary>
		/// This is the ReferenceUi factory.
		/// We currently exclude ReferenceSequenceUi (see that class for reason).
		/// </summary>
		public static ReferenceBaseUi MakeUi(LcmCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
		{
			var iType = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(referenceFlid);
			switch (iType)
			{
				case CellarPropertyType.ReferenceSequence:
				case CellarPropertyType.ReferenceCollection:
					return new VectorReferenceUi(cache, rootObj, referenceFlid, targetHvo);
				case CellarPropertyType.ReferenceAtomic:
					return new ReferenceBaseUi(cache, rootObj, referenceFlid, targetHvo);
			}

			return null;
		}

		#region Overrides of CmObjectUi
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);
			m_targetUi.InitializeFlexComponent(flexComponentParameters);
		}
		#endregion

		public override string ContextMenuId => "mnuReferenceChoices";

		public ReferenceBaseUi(ICmObject rootObj) : base(rootObj) { }
		public ReferenceBaseUi() { }

#if RANDYTODO
		public override bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_targetUi != null)
				return m_targetUi.OnDisplayJumpToTool(commandObject, ref display);
			return base.OnDisplayJumpToTool(commandObject, ref display);
		}

		/// <summary>
		/// JohnT: Transferred this from FW 6 but don't really understand what it's all about.
		/// The overridden method is about various "Show X in Y" commands.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override bool OnJumpToTool(object commandObject)
		{
			if (m_targetUi != null)
				return m_targetUi.OnJumpToTool(commandObject);
				return base.OnJumpToTool(commandObject);
		}

		/// <summary>
		/// Overridden by ReferenceSequenceUi.
		/// We put these OnDisplayMove... routines in the base class, so that we can make them explicitly not show up
		/// for non ReferenceSequence classes. Otherwise, they might appear as disabled for commands under mnuReferenceChoices.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveTargetUpInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = display.Enabled = false;
			return true;
		}

		/// <summary>
		/// Overriden by ReferenceSequenceUi.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveTargetDownInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = display.Enabled = false;
			return true;
		}
#endif
	}
}