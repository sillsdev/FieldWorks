// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ComWrapper.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Contains definition of COM interfaces and data structures used by these interfaces.
// Somehow these interfaces aren't provided by Microsoft as you would expect.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using Accessibility;
using System.Drawing;

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


	/// <summary></summary>
	public enum DvAspect: uint
	{
		/// <summary></summary>
		Content = 1,
		/// <summary></summary>
		ThumbNail = 2,
		/// <summary></summary>
		Icon = 4,
		/// <summary></summary>
		DocPrint = 8
	}

	/// <summary></summary>
	public enum Tymed: uint
	{
		/// <summary></summary>
		HGlobal = 1,
		/// <summary></summary>
		File = 2,
		/// <summary></summary>
		IStream = 4,
		/// <summary></summary>
		IStorage = 8,
		/// <summary></summary>
		Gdi =		16,
		/// <summary></summary>
		MfPict =	32,
		/// <summary></summary>
		EnhMf =	64,
		/// <summary></summary>
		Null =		0
	}

	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct FormatEtc
	{
		/// <summary></summary>
		public uint cfFormat;
		/// <summary></summary>
		public uint ptd;
		/// <summary></summary>
		public DvAspect dwAspect;
		/// <summary></summary>
		public int lindex;
		/// <summary></summary>
		public Tymed tymed;
	}

	/// <summary></summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct StgMedium
	{
		/// <summary></summary>
		[FieldOffset(0)]
		public Tymed tymed;

		/*
		 *
		 * The following is the error message that is created when .NET SP 1 is installed.
		 *
		 *   build:
		 *
		 *        [echo] nant.project.buildfile=file:///c:/work/fw/Obj/NAntBuild/COMInterfaces.build
		 *   [versionex] Generating GeneratedAssemblyInfo.cs from AssemblyInfo.cs
		 *         [csc] Compiling 8 files to c:\work\fw\Output\Debug\COMInterfaces.dll.
		 *        [copy] Copying 1 file to c:\work\fw\Output\Debug.
		 *      [regasm] Registering c:\work\fw\Output\Debug\COMInterfaces.dll for COM Interop
		 *               RegAsm warning: Registering an unsigned assembly with /codebase can cause your assembly to interfere with other
		 *				 applications that may be installed on the same computer. The /codebase switch is intended to be used only with
		 *				 signed assemblies. Please give your assembly a strong name and re-register it.
		 *               RegAsm error: Could not load type SIL.FieldWorks.Common.COMInterfaces.StgMedium from assembly COMInterfaces,
		 *				 Version=1.2.0.9, Culture=neutral, PublicKeyToken=null because it contains an object field at offset 4 that is
		 *				 incorrectly aligned or overlapped by a non-object field.
		 *
		 *   BUILD FAILED
		 */

		/*
		 * To get around this problem, since only the one member is used IStorage pstg,
		 * all the other members are being commented out (for now).  If these are needed in the future, hopefully
		 * there will be a cleaner C# / .NET way to get the information from the clipboard.
		 */

		/// <summary></summary>
		[FieldOffset(4)]
		public IStorage pstg;

		// these fields aren't used so, comment them out for now to get the build working
#if false
		/// <summary></summary>
		[FieldOffset(4)]
		public uint hBitmap;
		/// <summary></summary>
		[FieldOffset(4)]
		public uint hMetaFilePict;
		/// <summary></summary>
		[FieldOffset(4)]
		public uint hEnhMetaFile;
		/// <summary></summary>
		[FieldOffset(4)]
		public uint hGlobal;
		/// <summary></summary>
		[FieldOffset(4)]
		private StgMediumUnion u;
#endif
		/// <summary></summary>
		[FieldOffset(8)]
		public uint pUnkForRelease;

#if false
		// now add properties for the variables that we couldn't directly include

		/// <summary></summary>
		public IStorage pstg
		{
			get { return u.pstg; }
			set { u.pstg = value; }
		}
		/// <summary></summary>
		public string lpszFileName
		{
			get { return u.lpszFileName; }
			set { u.lpszFileName = value; }
		}
		/// <summary></summary>
		public IStream pstm
		{
			get { return u.pstm; }
			set { u.pstm = value; }
		}
#endif
	}

	/// <summary>We need this because value and reference types can't overlap.</summary>
	[StructLayout(LayoutKind.Explicit)]
	internal struct StgMediumUnion
	{
		/// <summary></summary>
		[FieldOffset(0)]
		public IStorage pstg;
		/// <summary></summary>
		[FieldOffset(0)]
		public string lpszFileName;
		/// <summary></summary>
		[FieldOffset(0)]
		public IStream pstm;
	}
	#endregion

	#region COM Interfaces

	/// <summary>
	/// Defines the <c>IDataObject</c> interfaces. Because there is already a different
	/// <c>IDataObject</c> defined in .NET, we have renamed it to <c>IOleDataObject</c>.
	/// </summary>
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[GuidAttribute("0000010e-0000-0000-C000-000000000046")]
	public interface IOleDataObject
	{
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		[PreserveSig]
		int GetData(ref FormatEtc a, ref StgMedium b);
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		[PreserveSig]
		void GetDataHere(int a, ref StgMedium b);
		/// <summary></summary>
		/// <param name="a"></param>
		[PreserveSig]
		int QueryGetData(int a);
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		[PreserveSig]
		int GetCanonicalFormatEtc(int a, ref int b);
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		[PreserveSig]
		int SetData(int a, int b, int c);
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		[PreserveSig]
		int EnumFormatEtc(uint a, ref Object b);
		/// <summary></summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		[PreserveSig]
		int DAdvise(int a, uint b, Object c, ref uint d);
		/// <summary></summary>
		/// <param name="a"></param>
		[PreserveSig]
		int DUnadvise(uint a);
		/// <summary></summary>
		/// <param name="a"></param>
		[PreserveSig]
		int EnumDAdvise(ref Object a);
	}

	/// <summary>
	/// Likewise IServiceProvider has a new .NET replacement.
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
	/// Added by JohnT. Not sure this is the most appropriate place...but it is wrapping
	/// a COM interface, IAccessible, with the required .NET class AccessibleObject.
	/// </summary>
	public class AccessibleObjectFromIAccessible : AccessibleObject
	{
		IAccessible m_acc;
		/// <summary>
		/// Make one from an IAccessible
		/// </summary>
		/// <param name="acc"></param>
		public AccessibleObjectFromIAccessible(IAccessible acc)
		{
			m_acc = acc;
		}

		/// <summary>
		/// Get the bounding rectangle. For location, this is definitely in screen coords;
		/// the doc for Bounds does not say, so I assume it is the same.
		/// </summary>
		public override Rectangle Bounds {
			get
			{
				int left, top, width, height;
				m_acc.accLocation(out left, out top, out width, out height, null);
				return new Rectangle(left, top, width, height);
			}
		}

		/// <summary>
		/// Get the string describing the default action.
		/// </summary>
		public override string DefaultAction {
			get
			{
				return m_acc.get_accDefaultAction(null);
			}
		}

		/// <summary>
		/// Get a longer description of the object, if there is anything relevant available.
		/// </summary>
		public override string Description
		{
			get
			{
				return m_acc.get_accDescription(null);
			}
		}
		// Help and KeyboardShortcut not yet delegated as we don't have any useful impl.

		/// <summary>
		/// Get a short identifying name (the type of Box, in the Views implementation)
		/// </summary>
		public override string Name
		{
			get
			{
				return m_acc.get_accName(null);
			}
			// set not yet delegated.
		}

		/// <summary>
		/// The parent 'window'. Current Views impl will only go as high as the root box.
		/// </summary>
		public override AccessibleObject Parent
		{
			get
			{
				return new AccessibleObjectFromIAccessible((IAccessible)(m_acc.accParent));
			}
		}

		/// <summary>
		/// The role. I hope the enumeration values are parallel to the old constants!
		/// </summary>
		public override AccessibleRole Role
		{
			get
			{
				return (AccessibleRole)(m_acc.get_accRole(null));
			}
		}

		/// <summary>
		/// The state. I hope the bit field uses the same values!
		/// </summary>
		public override AccessibleStates State
		{
			get
			{
				return (AccessibleStates)(m_acc.get_accState(null));
			}
		}

		/// <summary>
		/// The value of the control, whatever that means...
		/// </summary>
		public override string Value
		{
			get
			{
				return m_acc.get_accValue(null);
			}
			// set not yet delegated.
		}

		// CreateObjRef not overridden; not sure of relevance.
		// DoDefaultAction not yet delegated.
		// Equals not delegated.

		/// <summary>
		/// The nth child. Apparently they don't have to be indexed sequentially...
		/// perhaps MS expects you to use Navigate(next)? The Views impl indexes
		/// children sequentially starting from 1.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public override AccessibleObject GetChild(int index)
		{
			return new AccessibleObjectFromIAccessible((IAccessible)(m_acc.get_accChild(index)));
		}

		/// <summary>
		/// How many children there are.
		/// </summary>
		/// <returns></returns>
		public override int GetChildCount()
		{
			return m_acc.accChildCount;
		}
		// GetHelptopic not yet delegated.
		// GetLifetimeService not delegated.
		// GetSelected not yet delegated.
		// GetType not delegated.

		/// <summary>
		/// Which child (or the recipient), if any, contains the point.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public override AccessibleObject HitTest(int x, int y)
		{
			IAccessible acc = (IAccessible)(m_acc.accHitTest(x, y));
			if (acc == null)
				return null;
			if (acc == m_acc)
				return this;
			return new AccessibleObjectFromIAccessible(acc);
		}

		// InitializeLifetimeService not delgated.

		/// <summary>
		/// Move in various directions from the recipient.
		/// </summary>
		/// <param name="navdir"></param>
		/// <returns></returns>
		public override AccessibleObject Navigate(AccessibleNavigation navdir)
		{
			IAccessible acc = (IAccessible)(m_acc.accNavigate((int) navdir, null));
			if (acc == null)
				return null;
			if (acc == m_acc) // probably unlikely...
				return this;
			return new AccessibleObjectFromIAccessible(acc);
		}

		// Select not yet delegated.
		// ToString, Finalize, MemberwiseClone not delegated.
	}
}
