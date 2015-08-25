// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ComWrapper.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Contains definition of COM interfaces and data structures used by these interfaces.
// Somehow these interfaces aren't provided by Microsoft as you would expect.
// </remarks>

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Accessibility;
using System.Drawing;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	#region Data structures and enums
	/// <summary></summary>
	public enum ClipFormat : uint
	{
		/// <summary></summary>
		Text =		1,
		/// <summary></summary>
		Bitmap =		2,
		/// <summary></summary>
		MetaFilePict = 3,
		/// <summary></summary>
		Sylk =		4,
		/// <summary></summary>
		Dif =		5,
		/// <summary></summary>
		Tiff =		6,
		/// <summary></summary>
		OemText =	7,
		/// <summary></summary>
		Dib =		8,
		/// <summary></summary>
		Palette =	9,
		/// <summary></summary>
		PenData =	10,
		/// <summary></summary>
		Riff =		11,
		/// <summary></summary>
		Wave =		12,
		/// <summary></summary>
		UnicodeText = 13,
		/// <summary></summary>
		EnhMetaFile = 14,
		/// <summary></summary>
		HDrop =		15,
		/// <summary></summary>
		Locale =		16,
		/// <summary></summary>
		Max =		17,

		/// <summary></summary>
		OwnerDisplay    = 0x0080,
		/// <summary></summary>
		DspText         = 0x0081,
		/// <summary></summary>
		DspBitmap       = 0x0082,
		/// <summary></summary>
		DspMetaFilePict = 0x0083,
		/// <summary></summary>
		DspEnhMetaFile  = 0x008E,

		/// <summary></summary>
		PrivateFirst    = 0x0200,
		/// <summary></summary>
		PrivateLast     = 0x02FF,

		/// <summary></summary>
		GdiObjFirst     = 0x0300,
		/// <summary></summary>
		GdiObjLast      = 0x03FF
	}
	#endregion

	#region COM Interfaces

	/// <summary>
	/// Defines the <c>IServiceProvider</c> interfaces. Because there is already a different
	/// <c>IServiceProvider</c> defined in .NET, we have renamed it to <c>IOleServiceProvider</c>.
	/// </summary>
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa")]
	public interface IOleServiceProvider
	{
		/// <summary>
		/// Standard method for obtaining a service...rather like QueryInterface,
		/// but the result doesn't have to be the 'same' object.
		/// </summary>
		[PreserveSig()]
		void QueryService(ref System.Guid guidService, ref System.Guid riid,
			[MarshalAs(UnmanagedType.Interface)] out object ppvObject);
	}
	#endregion

	/// <summary>
	/// Encapsulates IPicture objects, so that it will get disposed properly.
	/// </summary>
	public class ComPictureWrapper : IDisposable
	{
		/// <summary>
		///
		/// </summary>
		private readonly IPicture m_picture;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComPictureWrapper"/> class.
		/// </summary>
		/// <param name="picture">The picture.</param>
		public ComPictureWrapper(IPicture picture)
		{
			m_picture = picture;
		}

		/// <summary>
		/// Gets the picture.
		/// </summary>
		/// <value>The picture.</value>
		public IPicture Picture { get { return m_picture; } }

		#region IDisposable Members
		#if DEBUG
		/// <summary/>
		~ComPictureWrapper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			if (fDisposing)
			{
				// dispose managed and unmanaged objects
				var disposable = m_picture as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
				if (m_picture != null && Marshal.IsComObject(m_picture))
					Marshal.ReleaseComObject(m_picture);
			IsDisposed = true;
		}
		#endregion
	}

	#region IPicture and IPictureDisp interfaces from stdole
	#endregion
}
