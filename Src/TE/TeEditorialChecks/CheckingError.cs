// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CheckingError.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Cellar;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using System.Drawing;
using System.ComponentModel;
using SIL.FieldWorks.Common.COMInterfaces;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	#region Class CheckingStatus
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// The CheckingStatus. This can be converted either to an integer or to a bitmap.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class CheckingStatus : BitmapStatus
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public enum StatusEnum
		{
			/// <summary>The error is marked as a potential inconsistency</summary>
			Inconsistency = 0,
			/// <summary>The error is ignored</summary>
			Ignored = 1,
			/// <summary>The error is no longer relevant.</summary>
			Irrelevant = 2,
		}

		#region Constructor
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckingStatus"/> struct.
		/// </summary>
		/// <param name="status">The status.</param>
		/// --------------------------------------------------------------------------------
		public CheckingStatus(NoteStatus status): this((int)status)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckingStatus"/> struct.
		/// </summary>
		/// <param name="status">The status.</param>
		/// --------------------------------------------------------------------------------
		public CheckingStatus(int status)
			: base(status)
		{
		}
		#endregion

		#region Public properties

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the checking status.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StatusEnum Status
		{
			get { return (StatusEnum)m_Status; }
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
				case StatusEnum.Ignored:
					return EditorialChecksControl.IgnoredInconsistenciesImage;
				case StatusEnum.Inconsistency:
					return EditorialChecksControl.InconsistenciesImage;
				case StatusEnum.Irrelevant:
					return TeResourceHelper.CheckErrorIrrelevant;
			}
		}

		#endregion

		#region Implicit cast methods

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="System.Int32"/> to
		/// <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// --------------------------------------------------------------------------------
		public static implicit operator CheckingStatus(int status)
		{
			return new CheckingStatus(status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>
		/// to <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator StatusEnum(CheckingStatus status)
		{
			return status.Status;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="System.Int32"/> to
		/// <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// --------------------------------------------------------------------------------
		public static implicit operator CheckingStatus(NoteStatus status)
		{
			return new CheckingStatus(status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>
		/// to <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator NoteStatus(CheckingStatus status)
		{
			return (NoteStatus)status.Status;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from
		/// <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/> to
		/// <see cref="SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator CheckingStatus(StatusEnum status)
		{
			return new CheckingStatus((int)status);
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

	#region CheckingStatusComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fixes TE-6328.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckingStatusComparer : IComparer
	{
		int IComparer.Compare(object x, object y)
		{
			CheckingStatus csx = x as CheckingStatus;
			CheckingStatus csy = y as CheckingStatus;

			if (csx == csy)
				return 0;

			if (csx == null)
				return -1;

			if (csy == null)
				return 1;

			return ((int)csx.Status - (int)csy.Status);
		}
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents an error from a Editorial Check
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckingError : ScrScriptureNote, INotifyPropertyChanged, ICheckGridRowObject
	{
		#region Implementation of INotifyPropertyChanged interface
		/// <summary>Notifies clients that a property value has changed.</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		private static CheckingError s_EmptyCheckingError;
		private Guid m_checkId = Guid.Empty;
		private string m_scrRef;
		private string m_chapVerseSepr;
		private string m_verseBridge;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckingError"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckingError() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckingError"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The HVO.</param>
		/// ------------------------------------------------------------------------------------
		private CheckingError(FdoCache cache, int hvo)
			: base(cache, hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckingError"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The HVO.</param>
		/// <param name="fCheckValidity"><c>true</c> to check if HVO is valid.</param>
		/// <param name="fLoadIntoCache"><c>true</c> to load into cache.</param>
		/// ------------------------------------------------------------------------------------
		private CheckingError(FdoCache cache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
			: base(cache, hvo, fCheckValidity, fLoadIntoCache)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an instance of a CheckingError object if the hvo is for an annotation
		/// that really is a CheckingError.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static CheckingError Create(FdoCache cache, int hvo)
		{
			CheckingError error = new CheckingError(cache, hvo);
			return (error.AnnotationType != NoteType.CheckingError ? null : error);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an instance of a CheckingError object if the hvo is for an annotation
		/// that really is a CheckingError.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static CheckingError Create(FdoCache cache, int hvo, bool fCheckValidity,
			bool fLoadIntoCache)
		{
			CheckingError error = new CheckingError(cache, hvo, fCheckValidity, fLoadIntoCache);
			return (error.AnnotationType != NoteType.CheckingError ? null : error);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ID of the check this checking error object is associated with.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId
		{
			get { return (AnnotationTypeRA != null ? AnnotationTypeRA.Guid : Guid.Empty); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static CheckingError Empty
		{
			get
			{
				if (s_EmptyCheckingError == null)
					s_EmptyCheckingError = new CheckingError();
				return s_EmptyCheckingError;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the value for the specified property. I would use reflection but that takes
		/// more overhead since this will be used in sorting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object GetPropValue(string propName)
		{
			switch (propName)
			{
				case "DisplayReference": return DisplayReference;
				case "Reference": return ReferencePosition;
				case "TypeOfCheck": return TypeOfCheck;
				case "Message": return Message;
				case "Details": return Details;
				case "Status": return Status;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scripture reference or reference range as string.
		/// This is used for display purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DisplayReference
		{
			get
			{
				if (m_cache != null)
				{
					// Get the chapter/verse separator and the verse bridge character.
					if (m_chapVerseSepr == null || m_verseBridge == null)
					{
						IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
						m_chapVerseSepr = scr.ChapterVerseSepr;
						m_verseBridge = scr.Bridge;
					}

					if (m_scrRef == null)
					{
						m_scrRef = BCVRef.MakeReferenceString(
							BeginRef, EndRef, m_chapVerseSepr, m_verseBridge);
					}
				}

				if (!string.IsNullOrEmpty(m_scrRef))
					return m_scrRef;

				// We need to check if the cache is null because it is possible to create an
				// "Empty" CheckingError with no cache.
				return (m_cache == null ? string.Empty : BCVRef.ToString(BeginRef));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start Scripture reference as string. This is used for sort purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Reference
		{
			get { return (m_cache == null ? string.Empty : BCVRef.ToString(BeginRef)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture reference and position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ReferencePositionType ReferencePosition
		{
			get
			{
				if (m_cache == null)
					return null;

				// Get the own ords of the paragraph and section, if available.
				int iSection = -1;
				int iPara = -1;
				if (BeginObjectRA != null)
				{
					StTxtPara para = BeginObjectRA as StTxtPara;
					if (para != null)
					{
						iPara = para.OwnOrd;
						if (para.Owner != null)
						{
							try
							{
								iSection = m_cache.GetIntProperty(para.Owner.OwnerHVO,
									(int)CmObjectFields.kflidCmObject_OwnOrd);
							}
							catch
							{
								// Failed to get the section OwnOrd because the section did not
								// exist. iSection will be -1.
							}
						}
					}
				}

				return new ReferencePositionType(BeginRef, iSection, iPara, BeginOffset);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TypeOfCheck
		{
			get { return (AnnotationTypeRA != null && AnnotationTypeRA.Name != null ?
				AnnotationTypeRA.Name.UserDefaultWritingSystem : string.Empty); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				Debug.Assert(DiscussionOA == null || DiscussionOA.ParagraphsOS.Count == 1);
				StTxtPara para = (DiscussionOA != null && DiscussionOA.ParagraphsOS != null ?
					DiscussionOA.ParagraphsOS.FirstItem as StTxtPara : null);

				return (para != null ? para.Contents.UnderlyingTsString.Text : string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the details.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString Details
		{
			get
			{
				Debug.Assert(QuoteOA == null || QuoteOA.ParagraphsOS.Count == 1);
				StTxtPara para = (QuoteOA != null && QuoteOA.ParagraphsOS != null ?
					QuoteOA.ParagraphsOS.FirstItem as StTxtPara : null);

				return (para != null ? para.Contents.UnderlyingTsString : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the status
		/// </summary>
		/// <remarks>This property needs to be public in order to work with a
		/// DataGridViewImageCell.</remarks>
		/// ------------------------------------------------------------------------------------
		public CheckingStatus Status
		{
			get { return new CheckingStatus(ResolutionStatus); }
			set
			{
				ResolutionStatus = value;
				OnPropertyChanged("Status");
			}
		}

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
	}
}
