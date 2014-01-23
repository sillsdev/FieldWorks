// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ComUtils.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Helper classes for use with COM interfaces. The structs are already defined in COM, but we
// re-define them so that we can provide conversion operators to/from .NET native types.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.Utils;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Redefine VwSelLevInfo struct, so that it can be serialized.
	/// </summary>
	[Serializable]
	public struct SelLevInfo
	{
		/// <summary>The tag</summary>
		public int tag;
		/// <summary>Number of previous occurences of the property</summary>
		public int cpropPrevious;
		/// <summary>Index of hvo (-1 for string property)</summary>
		public int ihvo;
		/// <summary> The actual hvo (only when reading info).</summary>
		public int hvo;
		/// <summary>
		/// If the property is a multitext one, gives the identifier of the alternative.
		/// Value is meaningless unless ihvo == -1.
		/// </summary>
		public int ws;
		/// <summary>
		/// If the property is a text (or multitext) one, gives the char index of the ORC
		/// that 'contains' the embedded object containing the selection.
		/// Value is meaningless unless ihvo == -1.
		/// </summary>
		public int ich;

		/// <summary>
		/// Get an array of SelLevInfo structs from the given selection.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="cvsli"></param>
		/// <param name="ihvoRoot"></param>
		/// <param name="tagTextProp"></param>
		/// <param name="cpropPrevious"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// <param name="ws"></param>
		/// <param name="fAssocPrev"></param>
		/// <param name="ihvoEnd"></param>
		/// <param name="ttp"></param>
		/// <returns></returns>
		public static SelLevInfo[] AllTextSelInfo(IVwSelection vwsel, int cvsli,
			out int ihvoRoot, out int tagTextProp, out int cpropPrevious, out int ichAnchor,
			out int ichEnd, out int ws, out bool fAssocPrev, out int ihvoEnd, out ITsTextProps ttp)
		{
			Debug.Assert(vwsel != null);

			using (ArrayPtr rgvsliPtr = MarshalEx.ArrayToNative<SelLevInfo>(cvsli))
			{
				vwsel.AllTextSelInfo(out ihvoRoot, cvsli, rgvsliPtr,
					out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);
				return MarshalEx.NativeToArray<SelLevInfo>(rgvsliPtr, cvsli);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			return obj is SelLevInfo && ((SelLevInfo)obj) == this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator == (SelLevInfo left, SelLevInfo right)
		{
			return (left.hvo == right.hvo && left.ich == right.ich && left.ihvo == right.ihvo &&
				left.tag == right.tag && left.ws == right.ws &&
				left.cpropPrevious == right.cpropPrevious);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator != (SelLevInfo left, SelLevInfo right)
		{
			return !(left == right);
		}
	}

	/// <summary>
	///
	/// </summary>
	public static class VwConstructorServices
	{

		/// <summary>
		/// Use for opening and closing an InnerPile within a using block.
		/// </summary>
		public class InnerPileHelper : IDisposable
		{
			private readonly IVwEnv m_vwenv;

			/// <summary>
			/// Initializes a new instance of the <see cref="InnerPileHelper"/> class.
			/// </summary>
			/// <param name="vwenv">The vwenv.</param>
			public InnerPileHelper(IVwEnv vwenv)
				: this(vwenv, null)
			{

			}

			/// <summary>
			/// Opens the InnerPile on the vwenv after setting the given pile properties.
			/// </summary>
			/// <param name="vwenv">The vwenv.</param>
			/// <param name="setPileProperties">The set pile properties.</param>
			public InnerPileHelper(IVwEnv vwenv, Action setPileProperties)
			{
				m_vwenv = vwenv;
				if (setPileProperties != null)
					setPileProperties();
				m_vwenv.OpenInnerPile();
			}

			#region IDisposable Members
			#if DEBUG
			/// <summary/>
			~InnerPileHelper()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary>
			/// Closes the InnerPile
			/// </summary>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_vwenv.CloseInnerPile();
				}
				IsDisposed = true;
			}
			#endregion
		}

		/// <summary>
		/// Use for opening and closing a Paragraph (box) within a using block.
		/// </summary>
		public class ParagraphBoxHelper : IDisposable
		{
			private readonly IVwEnv m_vwenv;

			/// <summary>
			/// Initializes a new instance of the <see cref="ParagraphBoxHelper"/> class.
			/// </summary>
			/// <param name="vwenv">The vwenv.</param>
			public ParagraphBoxHelper(IVwEnv vwenv)
				: this(vwenv, null)
			{

			}

			/// <summary>
			/// Opens the ParagraphBox on the vwenv after setting the given pile properties.
			/// </summary>
			/// <param name="vwenv">The vwenv.</param>
			/// <param name="setPileProperties">The set pile properties.</param>
			public ParagraphBoxHelper(IVwEnv vwenv, Action setPileProperties)
			{
				m_vwenv = vwenv;
				if (setPileProperties != null)
					setPileProperties();
				m_vwenv.OpenParagraph();
			}

			#region IDisposable Members
			#if DEBUG
			/// <summary/>
			~ParagraphBoxHelper()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary>
			/// Close the Paragraph
			/// </summary>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_vwenv.CloseParagraph();
				}
				IsDisposed = true;
			}
			#endregion
		}

		/// <summary>
		/// Converts the image to (an OLECvt) IPicture picture and wraps it with a disposable object.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns></returns>
		static public ComPictureWrapper ConvertImageToComPicture(Image image)
		{
			return new ComPictureWrapper((IPicture) OLECvt.ToOLE_IPictureDisp(image));
		}

	}
}
