//-------------------------------------------------------------------------------------------------
// <copyright file="VsLogicalView.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains Visual Studio logical view GUIDs for use in opening an editor.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Contains Visual Studio logical view GUIDs for use in opening an editor.
	/// </summary>
	public sealed class VsLogicalView
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		public static readonly VsLogicalView Any             = new VsLogicalView(new Guid(0xffffffff, 0xffff, 0xffff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff));
		public static readonly VsLogicalView Primary         = new VsLogicalView(Guid.Empty);
		public static readonly VsLogicalView Debugging       = new VsLogicalView(new Guid("{7651a700-06e5-11d1-8ebd-00a0c90f26ea}"));
		public static readonly VsLogicalView Code            = new VsLogicalView(new Guid("{7651a701-06e5-11d1-8ebd-00a0c90f26ea}"));
		public static readonly VsLogicalView Designer        = new VsLogicalView(new Guid("{7651a702-06e5-11d1-8ebd-00a0c90f26ea}"));
		public static readonly VsLogicalView TextView        = new VsLogicalView(new Guid("{7651a703-06e5-11d1-8ebd-00a0c90f26ea}"));
		public static readonly VsLogicalView UserChooseView  = new VsLogicalView(new Guid("{7651a704-06e5-11d1-8ebd-00a0c90f26ea}"));

		private static readonly VsLogicalView[] validLogicalViews = new VsLogicalView[]
			{
				Any, Primary, Debugging, Code, Designer, TextView, UserChooseView
			};

		private Guid value;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private VsLogicalView(Guid value)
		{
			this.value = value;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the GUID value of this logical view.
		/// </summary>
		public Guid Value
		{
			get { return this.value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Attempts to convert the specified GUID into one of the predetermined <see cref="VsLogicalView"/> objects.
		/// </summary>
		/// <param name="value">The GUID to attempt to convert.</param>
		/// <param name="logicalView">The converted <see cref="VsLogicalView"/> object if successful.</param>
		/// <returns><see langword="true"/> if successful; otherwise <see langword="false"/>.</returns>
		public static bool TryFromGuid(Guid value, out VsLogicalView logicalView)
		{
			bool found = false;
			logicalView = null;

			foreach (VsLogicalView view in validLogicalViews)
			{
				if (view.Value == value)
				{
					logicalView = view;
					found = true;
					break;
				}
			}

			return found;
		}

		/// <summary>
		/// Returns a string representation of this object.
		/// </summary>
		/// <returns>A string representation of this object.</returns>
		public override string ToString()
		{
			return this.Value.ToString("B", CultureInfo.InvariantCulture);
		}
		#endregion
	}
}
