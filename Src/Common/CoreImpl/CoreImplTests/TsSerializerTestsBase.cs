// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl
{
	[TestFixture]
	public abstract class TsSerializerTestsBase
	{
		#region Member Variables
		protected int EnWS { get; private set; }
		protected int EsWS { get; private set; }
		protected WritingSystemManager WritingSystemManager { get; set; }
		#endregion

		#region Test Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			Icu.InitIcuDataDir();
			WritingSystemManager = new WritingSystemManager();

			CoreWritingSystemDefinition enWs;
			WritingSystemManager.GetOrSet("en", out enWs);
			EnWS = enWs.Handle;

			CoreWritingSystemDefinition esWs;
			WritingSystemManager.GetOrSet("es", out esWs);
			EsWS = esWs.Handle;
		}

		#endregion

		#region Helper methods
		protected string StripNewLines(string xml)
		{
			return xml.Replace(Environment.NewLine, "");
		}

		protected byte[] CreateObjData(FwObjDataTypes objDataType, string data)
		{
			var sb = new StringBuilder();
			sb.Append((char) objDataType);
			sb.Append(data);
			return Encoding.Unicode.GetBytes(sb.ToString());
		}

		protected byte[] CreateObjData(FwObjDataTypes objDataType, byte[] bytes)
		{
			byte[] typeBytes = BitConverter.GetBytes((char) objDataType);
			var value = new byte[bytes.Length + typeBytes.Length];
			Buffer.BlockCopy(typeBytes, 0, value, 0, typeBytes.Length);
			Buffer.BlockCopy(bytes, 0, value, typeBytes.Length, bytes.Length);
			return value;
		}
		#endregion
	}
}
