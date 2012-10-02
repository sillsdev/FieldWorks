// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTxtParaBldr.cs
// Responsibility: FieldWorks Team
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.Cellar
{
	#region IParaStylePropsProxy interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParaStylePropsProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph properties for this proxy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsTextProps Props
		{
			get;
		}
	}
	#endregion

	#region StTxtParaBldr class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class makes it fun to build paragraphs as a hobby
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StTxtParaBldr : IFWDisposable
	{
		#region Data members

		private FdoCache m_cache;

		/// <summary>Holds the paragraph style name to be used for creating new paragraphs</summary>
		protected IParaStylePropsProxy m_ParaStyle;
		/// <summary>String builder to construct paragraph strings.</summary>
		protected ITsStrBldr m_ParaStrBldr;
		/// <summary>TsTextProps for the paragraph.</summary>
		protected ITsTextProps m_ParaProps;
		/// <summary>Unicode character properties engine</summary>
		private ILgCharacterPropertyEngine m_cpe = null;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTxtParaBldr"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtParaBldr(FdoCache cache)
		{
			System.Diagnostics.Debug.Assert(cache != null);
			m_cache = cache;

			ITsStrFactory tsStringFactory = TsStrFactoryClass.Create();
			m_ParaStrBldr = tsStringFactory.GetBldr();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the paragraph style proxy to be used for creating the new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IParaStylePropsProxy ParaStylePropsProxy
		{
			get
			{
				CheckDisposed();
				return m_ParaStyle;
			}
			set
			{
				CheckDisposed();
				m_ParaStyle = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the paragraph props to be used for creating the new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps ParaProps
		{
			get
			{
				CheckDisposed();
				return m_ParaProps;
			}
			set
			{
				CheckDisposed();
				m_ParaProps = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the text in the ParaStrBldr
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Length
		{
			get
			{
				CheckDisposed();
				return m_ParaStrBldr.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the underlying ITsStrBldr
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr StringBuilder
		{
			get
			{
				CheckDisposed();
				return m_ParaStrBldr;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The current final character sent to the StTxtPara builder
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public char FinalCharInPara
		{
			get
			{
				CheckDisposed();
				string s = m_ParaStrBldr.Text;
				if (s == null)
					return (char)0;
				return s[s.Length - 1];
			}
		}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an empty paragraph with the given paragraph style and writing system.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="ownerHvo"></param>
		/// <param name="paraStyle"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public static void CreateEmptyPara(FdoCache cache, int ownerHvo, string paraStyle, int ws)
		{
			using (StTxtParaBldr bldr = new StTxtParaBldr(cache))
			{
				bldr.ParaProps = StyleUtils.ParaStyleTextProps(paraStyle);
				bldr.AppendRun(String.Empty, StyleUtils.CharStyleTextProps(null, ws));
				bldr.CreateParagraph(ownerHvo);
			} // Dispose() frees ICU resources.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a run of text with the given TsTextProps.
		/// </summary>
		/// <param name="sRun">The text to append</param>
		/// <param name="props">The properties to use</param>
		/// ------------------------------------------------------------------------------------
		public void AppendRun(string sRun, ITsTextProps props)
		{
			CheckDisposed();
			//note: For efficiency, we usually skip the Replace() if the string is empty.
			// However, if the builder is has Length == 0, then we want to replace the
			// properties on the empty run of the TsString.
			// A TsString always has at least one run, even if it is empty, and this controls
			// the props when the user begins to enter text in an empty para.
			if (sRun != string.Empty || Length == 0)
			{
				System.Diagnostics.Debug.Assert(props != null);
				int var;
				int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);

				// Make sure we handle the magic writing systems
				if (ws == (int)CellarModuleDefns.kwsAnal)
				{
					// default analysis writing system
					ITsPropsBldr bldr = props.GetBldr();
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
						m_cache.DefaultAnalWs);
					props = bldr.GetTextProps();
				}
				else if (ws == (int)CellarModuleDefns.kwsVern)
				{
					// default vernacular writing system
					ITsPropsBldr bldr = props.GetBldr();
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
						m_cache.DefaultVernWs);
					props = bldr.GetTextProps();
				}
				else
				{
					System.Diagnostics.Debug.Assert(ws > 0);	// not quite right if >2G objects.
				}
				m_ParaStrBldr.Replace(Length, Length, sRun, props);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="StTxtPara"/>, owned by the given <see cref="StText"/>.
		/// Set it with data accumulated in this builder.
		/// </summary>
		/// <param name="hvoOwner">HVO of the <see cref="StText"/> that is to own the new
		/// paragraph</param>
		/// <param name="iPos">0-based index of the position in the sequence of paragraphs where the
		/// new paragraph is to be inserted. If a paragraph is already in this position, the new
		/// paragraph will be inserted before the existing paragraph.</param>
		/// <returns>A new StTextPara whose contents are built up from the prior calls
		/// to <see cref="AppendRun"/> and whose style is set based on the current value of
		/// <see cref="ParaStylePropsProxy"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public StTxtPara CreateParagraph(int hvoOwner, int iPos)
		{
			CheckDisposed();
			// ENHANCE: Could maybe squeeze a little more performance out of this by calling the
			// stored procedure that used to be called by TeImporter to create paragraphs.

			// insert a new para in the owner's collection
			StTxtPara para = new StTxtPara();
			StText text = new StText(m_cache, hvoOwner);
			text.ParagraphsOS.InsertAt(para, iPos);

			SetStTxtParaPropertiesAndClearBuilder(para, iPos);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new <see cref="StTxtPara"/> to the given <see cref="StText"/>.
		/// Set it with data accumulated in this builder.
		/// </summary>
		/// <param name="hvoOwner">HVO of the <see cref="StText"/> that is to own the new
		/// paragraph</param>
		/// <returns>A new StTextPara whose contents are built up from the prior calls
		/// to <see cref="AppendRun"/> and whose style is set based on the current value of
		/// <see cref="ParaStylePropsProxy"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public StTxtPara CreateParagraph(int hvoOwner)
		{
			CheckDisposed();
			// insert a new para in the owner's collection
			StTxtPara para = new StTxtPara();
			StText text = new StText(m_cache, hvoOwner);
			text.ParagraphsOS.Append(para);

			SetStTxtParaPropertiesAndClearBuilder(para, 0);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the StyleRules and Contents properties for the new <see cref="StTxtPara"/>;
		/// then clears the builder.
		/// </summary>
		/// <param name="para">The <see cref="StTxtPara"/> that was just created</param>
		/// <param name="iPos">index of new paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void SetStTxtParaPropertiesAndClearBuilder(StTxtPara para, int iPos)
		{
			// sets the new StTxtPara properties, with contents built up from prior calls
			ITsTextProps props = (ParaStylePropsProxy == null ?
				ParaProps : ParaStylePropsProxy.Props);
			para.StyleRules = props;

			para.Contents.UnderlyingTsString = m_ParaStrBldr.GetString();

			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				para.OwnerHVO,
				(int)StText.StTextTags.kflidParagraphs, iPos, 1, 0);

			// Clear the builder, for new paragraph
			m_ParaStrBldr.Replace(0, Length, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After last call to AppendRun for the current paragraph, but before calling
		/// CreateParagraph, call this method to trim the last character in the builder
		/// if it is a trailing space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void TrimTrailingSpaceInPara()
		{
			CheckDisposed();
			if (m_cpe == null)
				m_cpe = m_cache.UnicodeCharProps;
			// check if the last char sent to the builder is a space
			if (Length != 0 && m_cpe.get_IsSeparator(FinalCharInPara))
				m_ParaStrBldr.Replace(Length - 1, Length, null, null);
		}
		#endregion

		#region IDisposable implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~StTxtParaBldr()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// Dispose of unmanaged resources hidden in member variables.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
			}
			if (m_cpe != null)
			{
				if (Marshal.IsComObject(m_cpe))
					Marshal.ReleaseComObject(m_cpe);
				m_cpe = null;
			}
			m_cache = null;
			m_ParaStyle = null;
			m_ParaStrBldr = null;
			m_ParaProps = null;

			m_isDisposed = true;
		}
		#endregion

	}
	#endregion
}
