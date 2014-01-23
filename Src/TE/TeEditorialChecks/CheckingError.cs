// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckingError.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.ScriptureUtils;
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
		/// Initializes a new instance of the <see cref="T:CheckingStatus"/> struct.
		/// </summary>
		/// <param name="status">The status.</param>
		/// --------------------------------------------------------------------------------
		public CheckingStatus(NoteStatus status): this((int)status)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckingStatus"/> struct.
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
		/// Performs an implicit conversion from <see cref="T:System.Int32"/> to
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
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
		/// Performs an implicit conversion from <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>
		/// to <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/>.
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
		/// Performs an implicit conversion from <see cref="T:System.Int32"/> to
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
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
		/// Performs an implicit conversion from <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>
		/// to <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/>.
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
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus.StatusEnum"/> to
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.CheckingStatus"/>.
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
	public class CheckingError : INotifyPropertyChanged, ICheckGridRowObject
	{
		#region Implementation of INotifyPropertyChanged interface
		/// <summary>Notifies clients that a property value has changed.</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		private static CheckingError s_EmptyCheckingError;
		private IScrScriptureNote m_note;
		private string m_scrRef;
		private string m_chapVerseSepr;
		private string m_verseBridge;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckingError"/> class.
		/// </summary>
		/// <param name="note">The note being decorated</param>
		/// ------------------------------------------------------------------------------------
		private CheckingError(IScrScriptureNote note)
		{
			m_note = note;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an instance of a CheckingError object if the hvo is for an annotation
		/// that really is a CheckingError.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static CheckingError Create(IScrScriptureNote error)
		{
			if (error == null)
				throw new ArgumentNullException("Null note cannot be used");
			return (error.AnnotationType != NoteType.CheckingError ? null : new CheckingError(error));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an instance of a CheckingError object if the hvo is for an annotation
		/// that really is a CheckingError.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static CheckingError Create(FdoCache cache, int hvo)
		{
			return Create(cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(hvo));
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets my note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote MyNote
		{
			get { return m_note; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_note.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ID of the check this checking error object is associated with.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId
		{
			get { return (m_note.AnnotationTypeRA != null ? m_note.AnnotationTypeRA.Guid : Guid.Empty); }
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
					s_EmptyCheckingError = new CheckingError(null);
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
				if (Cache != null)
				{
					// Get the chapter/verse separator and the verse bridge character.
					if (m_chapVerseSepr == null || m_verseBridge == null)
					{
						IScripture scr = Cache.LangProject.TranslatedScriptureOA;
						m_chapVerseSepr = scr.ChapterVerseSepr;
						m_verseBridge = scr.Bridge;
					}

					if (m_scrRef == null)
					{
						m_scrRef = BCVRef.MakeReferenceString(
							m_note.BeginRef, m_note.EndRef, m_chapVerseSepr, m_verseBridge);
					}
				}

				if (!string.IsNullOrEmpty(m_scrRef))
					return m_scrRef;

				// We need to check if the cache is null because it is possible to create an
				// "Empty" CheckingError with no cache.
				return (Cache == null ? string.Empty : BCVRef.ToString(m_note.BeginRef));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start Scripture reference as string. This is used for sort purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Reference
		{
			get { return (Cache == null ? string.Empty : BCVRef.ToString(m_note.BeginRef)); }
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
				if (Cache == null)
					return null;

				// Get the own ords of the paragraph and section, if available.
				int iSection = -1;
				int iPara = -1;
				if (m_note.BeginObjectRA != null)
				{
					IStTxtPara para = m_note.BeginObjectRA as IStTxtPara;
					if (para != null)
					{
						iPara = para.IndexInOwner;
						if (para.Owner != null && para.Owner.Owner is IScrSection)
							iSection = para.Owner.Owner.IndexInOwner;
					}
				}

				return new ReferencePositionType(m_note.BeginRef, iSection, iPara, m_note.BeginOffset);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TypeOfCheck
		{
			get
			{
				return (m_note.AnnotationTypeRA != null && m_note.AnnotationTypeRA.Name != null ?
					m_note.AnnotationTypeRA.Name.UserDefaultWritingSystem.Text : string.Empty);
			}
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
				Debug.Assert(m_note.DiscussionOA.ParagraphsOS.Count == 1);
				IStTxtPara para = (m_note.DiscussionOA != null && m_note.DiscussionOA.ParagraphsOS != null ?
					m_note.DiscussionOA.ParagraphsOS[0] as IStTxtPara : null);

				return (para != null ? para.Contents.Text : string.Empty);
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
				Debug.Assert(m_note.QuoteOA == null || m_note.QuoteOA.ParagraphsOS.Count == 1);
				IStTxtPara para = (m_note.QuoteOA != null && m_note.QuoteOA.ParagraphsOS != null ?
					m_note.QuoteOA.ParagraphsOS[0] as IStTxtPara : null);

				return (para != null ? para.Contents : null);
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
			get { return new CheckingStatus(m_note.ResolutionStatus); }
			set
			{
				m_note.ResolutionStatus = value;
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
