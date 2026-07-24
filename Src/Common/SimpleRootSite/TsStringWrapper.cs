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
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wraps a ITsString object so that it can be serialized to/from the clipboard.
	/// This type (with the <see cref="TsStringFormat"/> OS clipboard format) is the cross-framework
	/// rich-text clipboard contract: legacy native-Views surfaces write/read it in
	/// <c>EditingHelper</c>, and the Avalonia coexistence bridge (<c>FwTsStringClipboard</c> in
	/// xWorks, task 3.13) speaks the same format so copy/paste round-trips between frameworks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class TsStringWrapper : ISerializable
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
		/// Creates a wrapper directly from an already-serialized TsString XML representation
		/// (as produced by <c>TsStringUtils.GetXmlRep</c>). Used by the cross-framework
		/// clipboard bridge, which carries the serialized XML without an ITsString in hand.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static TsStringWrapper FromXml(string xml)
		{
			return new TsStringWrapper(xml);
		}

		private TsStringWrapper(string xml)
		{
			m_Xml = xml;
		}

		/// <summary>The serialized TsString XML representation this wrapper carries.</summary>
		public string Xml
		{
			get { return m_Xml; }
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
