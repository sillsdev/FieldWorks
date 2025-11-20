// Copyright (c) 2002-2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using System.Threading;
using SIL.LCModel.Core.KernelInterfaces;
using LgCharRenderProps = SIL.LCModel.Core.KernelInterfaces.LgCharRenderProps;

namespace SIL.FieldWorks.Common.ViewsInterfaces
{
	/// <summary/>
	public sealed class VwPropertyStoreManaged : IDisposable
	{
		private const string _viewsDllPath = "views.dll";
		private IntPtr pVwPropStore;

		// Detect redundant Dispose() calls in a thread-safe manner.
		// _isDisposed == 0 means Dispose(bool) has not been called yet.
		// _isDisposed == 1 means Dispose(bool) has already been called.
		private int _isDisposed;

		/// <summary/>
		public VwPropertyStoreManaged()
		{
			pVwPropStore = VwPropertyStore_Create();
		}

		/// <summary/>
		public IVwStylesheet Stylesheet
		{
			set { VwPropertyStore_Stylesheet(pVwPropStore, value); }
		}

		/// <summary/>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set { VwPropertyStore_WritingSystemFactory(pVwPropStore, value); }
		}

		/// <summary/>
		public LgCharRenderProps get_ChrpFor(ITsTextProps ttp)
		{
			IntPtr pInt = VwPropertyStore_get_ChrpFor(pVwPropStore, ttp);
			return (LgCharRenderProps)Marshal.PtrToStructure(pInt, typeof(LgCharRenderProps));
		}

		#region Disposable stuff
		/// <summary/>
		~VwPropertyStoreManaged()
		{
			Dispose(false);
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
			{
				System.Diagnostics.Debug.WriteLineIf(
					!disposing,
					"****** Missing Dispose() call for " + GetType().Name + " ******"
				);

				// Dispose managed resources (if there are any).
				if (disposing) { }

				// Dispose unmanaged resources.
				VwPropertyStore_Delete(pVwPropStore);
			}
		}
		#endregion

		#region DLLImport stuff
		[DllImport(_viewsDllPath, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr VwPropertyStore_Create();

		[DllImport(_viewsDllPath, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VwPropertyStore_Delete(IntPtr pVwPropStore);

		[DllImport(_viewsDllPath, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VwPropertyStore_Stylesheet(
			IntPtr pVwPropStore,
			[MarshalAs(UnmanagedType.Interface)] IVwStylesheet pss
		);

		[DllImport(_viewsDllPath, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VwPropertyStore_WritingSystemFactory(
			IntPtr pVwPropStore,
			[MarshalAs(UnmanagedType.Interface)] ILgWritingSystemFactory pwsf
		);

		[DllImport(_viewsDllPath, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr VwPropertyStore_get_ChrpFor(
			IntPtr pVwPropStore,
			[MarshalAs(UnmanagedType.Interface)] ITsTextProps _ttp
		);
		#endregion
	}
}
