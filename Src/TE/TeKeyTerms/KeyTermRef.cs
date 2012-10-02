// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermRef.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.TE.TeEditorialChecks;
using System.Collections.Generic;
using SILUBS.SharedScrUtils;
using System.Collections;
using System.Diagnostics;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	#region RenderingStatusComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RenderingStatusComparer : IComparer
	{
		int IComparer.Compare(object x, object y)
		{
			RenderingStatus ktrx = x as RenderingStatus;
			RenderingStatus ktry = y as RenderingStatus;

			if (ktrx == ktry)
				return 0;

			if (ktrx == null)
				return -1;

			if (ktry == null)
				return 1;

			return ((int)ktrx.Status - (int)ktry.Status);
		}
	}

	#endregion

	#region Class RenderingStatus
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// The RenderingStatus. This can be converted either to an integer or to a bitmap.
	/// </summary>
	/// <remarks>Public for tests</remarks>
	/// ------------------------------------------------------------------------------------
	public class RenderingStatus : BitmapStatus
	{
		#region Constructor
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RenderingStatus"/> struct.
		/// </summary>
		/// <param name="status">The status.</param>
		/// --------------------------------------------------------------------------------
		public RenderingStatus(int status): base(status)
		{
		}
		#endregion

		#region Public properties
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the rendering status.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public KeyTermRenderingStatus Status
		{
			get { return (KeyTermRenderingStatus)m_Status; }
			set { m_Status = (int)value; }
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts to image.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override Image ConvertToImage()
		{
			switch (Status)
			{
				default:
				case KeyTermRenderingStatus.Unassigned:
					return TeResourceHelper.KeyTermNotRenderedImage;
				case KeyTermRenderingStatus.Assigned:
					return TeResourceHelper.KeyTermRenderedImage;
				case KeyTermRenderingStatus.AutoAssigned:
					return TeResourceHelper.KeyTermAutoAssignedImage;
				case KeyTermRenderingStatus.Ignored:
					return TeResourceHelper.KeyTermIgnoreRenderingImage;
				case KeyTermRenderingStatus.Missing:
					return TeResourceHelper.KeyTermMissingRenderedImage;
			}
		}
		#endregion

		#region Implicit cast methods

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="System.Int32"/> to
		/// <see cref="SIL.FieldWorks.TE.KeyTermRef.RenderingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// --------------------------------------------------------------------------------
		public static implicit operator RenderingStatus(int status)
		{
			return new RenderingStatus(status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="SIL.FieldWorks.TE.RenderingStatus"/>
		/// to <see cref="SIL.FieldWorks.FDO.KeyTermRenderingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator KeyTermRenderingStatus(RenderingStatus status)
		{
			return status.Status;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from
		/// <see cref="SIL.FieldWorks.FDO.KeyTermRenderingStatus"/> to
		/// <see cref="SIL.FieldWorks.TE.RenderingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator RenderingStatus(KeyTermRenderingStatus status)
		{
			return new RenderingStatus((int)status);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the fully qualified type name of this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> containing a fully qualified type name.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Status.ToString();
		}
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A KeyTerm reference, i.e. an occurrence of a key term.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermRef : ChkRef, INotifyPropertyChanged, ICheckGridRowObject
	{
		#region Implementation of INotifyPropertyChanged interface
		/// <summary>Notifies clients that a property value has changed.</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Members
		private IScripture m_scr;

		/// <summary>hashtable of empty strings for each cache</summary>
		private static Dictionary<FdoCache,ITsString> s_HtEmptyString = new Dictionary<FdoCache,ITsString>();
		private static KeyTermRef s_EmptyKeyTermRef;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRef"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private KeyTermRef() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRef"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The HVO.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermRef(FdoCache cache, int hvo)
			: base(cache, hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermRef"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The HVO.</param>
		/// <param name="fCheckValidity"><c>true</c> to check if HVO is valid.</param>
		/// <param name="fLoadIntoCache"><c>true</c> to load into cache.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermRef(FdoCache cache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
			: base(cache, hvo, fCheckValidity, fLoadIntoCache)
		{
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FDO scripture object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScripture Scripture
		{
			get
			{
				if (m_scr == null && m_cache != null)
					m_scr = m_cache.LangProject.TranslatedScriptureOA;
				return m_scr;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static KeyTermRef Empty
		{
			get
			{
				if (s_EmptyKeyTermRef == null)
					s_EmptyKeyTermRef = new KeyTermRef();
				return s_EmptyKeyTermRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rendering.
		/// </summary>
		/// <value>The rendering.</value>
		/// ------------------------------------------------------------------------------------
		public ITsString Rendering
		{
			get
			{
				int wsDummy;

				if (RenderingRAHvo <= 0)
					return EmptyString;
				return RenderingRA.Form.GetAlternativeOrBestTss(m_cache.DefaultVernWs,
					out wsDummy);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an empty TsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ITsString EmptyString
		{
			get
			{
				if (m_cache != null)
				{
					ITsString tss;
					if (!s_HtEmptyString.TryGetValue(m_cache, out tss))
					{
						// If an empty string has not been created using the current database's
						// id of the default vernacular writing system, create one.
						s_HtEmptyString[m_cache] = TsStrFactoryClass.Create().MakeString(
							string.Empty, m_cache.DefaultVernWs);
					}
					return s_HtEmptyString[m_cache];
				}
				else
					return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key word as ITsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString KeyWordString
		{
			get { return KeyWord.UnderlyingTsString; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture reference in the current versification.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference RefInCurrVersification
		{
			get
			{
				if (Scripture == null)
					throw new InvalidOperationException("Cannot call RefInCurrVersification for a KeyTermRef object that does not have a cache set");
				// REVIEW (TE-6532): For now, all Key Term Refs in the DB use the Original
				// versisifcation. Should we support other versifications?
				return new ScrReference(Ref, Paratext.ScrVers.Original,
					Scripture.Versification);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scripture reference as string, after converting to the versification in use
		/// in the current Scripture project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Reference
		{
			get { return (Scripture == null) ? string.Empty : RefInCurrVersification.ToString(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the status
		/// </summary>
		/// <remarks>This property needs to be public in order to work with a
		/// DataGridViewImageCell.</remarks>
		/// ------------------------------------------------------------------------------------
		public RenderingStatus RenderingStatus
		{
			get { return new RenderingStatus((int)Status); }
			set
			{
				Status = value;
				OnPropertyChanged("RenderingStatus");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is valid.
		/// </summary>
		/// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsValid
		{
			get { return (m_hvo != 0 && Cache != null &&  Ref != 0); }
		}
		#endregion

		#region Methods related to INotifyPropertyChanged interface
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a property changed.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// ------------------------------------------------------------------------------------
		protected internal void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region ICheckGridRowObject Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object GetPropValue(string propName)
		{
			switch (propName)
			{
				case "Reference": return Reference;
				case "Rendering": return Rendering;
				case "RenderingStatus": return RenderingStatus;
				case "KeyWordString": return KeyWordString;
			}

			return null;
		}

		#endregion

		#region Other nice methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assigns a rendering for this check reference.
		/// </summary>
		/// <param name="wordForm">The word form.</param>
		/// ------------------------------------------------------------------------------------
		internal void AssignRendering(IWfiWordform wordForm)
		{
			RenderingStatus = KeyTermRenderingStatus.Assigned;
			RenderingRA = wordForm;

			ChkTerm owningChkTerm = new ChkTerm(Cache, OwnerHVO);
			bool fChkTermRenderingAlreadySet = false;
			foreach (IChkRendering rendering in owningChkTerm.RenderingsOC)
			{
				if (rendering.SurfaceFormRAHvo == wordForm.Hvo)
				{
					fChkTermRenderingAlreadySet = true;
					break;
				}
			}
			if (!fChkTermRenderingAlreadySet)
			{
				ChkRendering newRendering = new ChkRendering();
				owningChkTerm.RenderingsOC.Add(newRendering);
				newRendering.SurfaceFormRAHvo = wordForm.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether the specified kt ref is null or empty.
		/// </summary>
		/// <param name="ktRef">The key terms reference.</param>
		/// ------------------------------------------------------------------------------------
		internal static bool IsNullOrEmpty(KeyTermRef ktRef)
		{
			return (ktRef == null || ktRef == Empty);
		}
		#endregion
	}
}
