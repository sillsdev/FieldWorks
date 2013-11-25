// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsStringWrapper.cs
// Responsibility: EberhardB
//
// <remarks>
// </remarks>

using System;
using System.Runtime.Serialization;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wraps a ITsString object so that it can be serialized to/from the clipboard
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	internal class TsStringWrapper : ISerializable
	{
		[NonSerialized]
		private readonly string m_Xml;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringWrapper"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public TsStringWrapper(ITsString tsString, ILgWritingSystemFactory writingSystemFactory)
		{
			m_Xml = TsStringUtils.GetXmlRep(tsString, writingSystemFactory, 0);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringWrapper"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		protected TsStringWrapper(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			m_Xml = (string)info.GetValue("Xml", typeof(string));
		}

		#region ISerializable Members

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with
		/// the data needed to serialize the target object.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/>
		/// to populate with data.</param>
		/// <param name="context">The destination (see
		/// <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this
		/// serialization.</param>
		/// <exception cref="T:System.Security.SecurityException">The caller does not have
		/// the required permission. </exception>
		/// --------------------------------------------------------------------------------
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			info.AddValue("Xml", m_Xml);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data format that is used to store a ITsString on the clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TsStringFormat
		{
			get { return DataFormats.GetFormat("TsString").Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TsString that this instance wraps.
		/// </summary>
		/// <param name="writingSystemFactory">The writing system factory which will be used to
		/// add missing writing systems.</param>
		/// <returns>A TsString object</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetTsString(ILgWritingSystemFactory writingSystemFactory)
		{
			return TsStringSerializer.DeserializeTsStringFromXml(m_Xml, writingSystemFactory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TsString that this instance wraps.
		/// </summary>
		/// <param name="ws">The WS to force the TsString to use.</param>
		/// <param name="writingSystemFactory">The writing system factory which will be used to
		/// add missing writing systems.</param>
		/// <returns>A TsString object</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetTsStringUsingWs(int ws, ILgWritingSystemFactory writingSystemFactory)
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(m_Xml, writingSystemFactory);
			if (tss.Length > 0)
			{
				ITsStrBldr bldr = tss.GetBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, 0, ws);
				tss = bldr.GetString();
			}
			return tss;
		}
	}
}
