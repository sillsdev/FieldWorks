using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.CoreImpl
{
	[TestFixture]
	public abstract class TsSerializerTestsBase
	// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		#region Member Variables
		protected int EnWS { get; private set; }
		protected int EsWS { get; private set; }
		private DebugProcs m_DebugProcs;
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
			m_DebugProcs = new DebugProcs();
			RegistryHelper.CompanyName = "SIL";

			Icu.InitIcuDataDir();
			WritingSystemManager = new WritingSystemManager();

			CoreWritingSystemDefinition enWs;
			WritingSystemManager.GetOrSet("en", out enWs);
			EnWS = enWs.Handle;

			CoreWritingSystemDefinition esWs;
			WritingSystemManager.GetOrSet("es", out esWs);
			EsWS = esWs.Handle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up the writing system factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
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
