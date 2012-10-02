//-------------------------------------------------------------------------------------------------
// <copyright file="BinderExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
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
// The base binder extension.  Any of these methods can be overridden to change
// the behavior of the binder.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	/// <summary>
	/// Options for building the cabinet.
	/// </summary>
	public enum CabinetBuildOption
	{
		/// <summary>
		/// Build the cabinet and move it to the target location.
		/// </summary>
		BuildAndMove,

		/// <summary>
		/// Build the cabinet and copy it to the target location.
		/// </summary>
		BuildAndCopy,

		/// <summary>
		/// Just copy the cabinet to the target location.
		/// </summary>
		Copy
	}

	/// <summary>
	/// Base class for creating a binder extension.
	/// </summary>
	public class BinderExtension
	{
		private ExtensionMessages messages;

		/// <summary>
		/// Gets and sets the message handling object.
		/// </summary>
		/// <value>Object to use when sending messages.</value>
		public ExtensionMessages Messages
		{
			get { return this.messages; }
			set { this.messages = value; }
		}

		/// <summary>
		/// Callback which allows host to adjust file source paths.
		/// </summary>
		/// <param name="source">Original source value.</param>
		/// <param name="fileType">Import file type.</param>
		/// <returns>Should return a valid path for the stream to be imported.</returns>
		public virtual string FileResolutionHandler(string source, FileResolutionType fileType)
		{
			return source;
		}

		/// <summary>
		/// Callback which allows host to adjust cabinet building.
		/// </summary>
		/// <param name="fileIds">Array of file identifiers that will be compressed into cabinet.</param>
		/// <param name="filesToCompress">Array of file paths that will be compressed.  Paired with fileIds array.</param>
		/// <param name="cabinetPath">Path to cabinet to generate.  Path may be modified by delegate.</param>
		/// <returns>The CabinetBuildOption.  By default the cabinet is built and moved to its target location.</returns>
		public virtual CabinetBuildOption CabinetResolutionHandler(string[] fileIds, string[] filesToCompress, ref string cabinetPath)
		{
			return CabinetBuildOption.BuildAndMove;
		}
	}
}