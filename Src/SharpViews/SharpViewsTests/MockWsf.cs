using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	class MockWsf : ILgWritingSystemFactory
	{
		#region ILgWritingSystemFactory Members

		public void AddEngine(ILgWritingSystem _wseng)
		{
			throw new NotImplementedException();
		}

		public void AddWritingSystem(int ws, string bstrIcuLocale)
		{
			throw new NotImplementedException();
		}

		public bool BypassInstall
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		Dictionary<string, int> m_IcuWs = new Dictionary<string, int>();
		private int m_nextWs = 1;
		Dictionary<int, string> m_WsIcu = new Dictionary<int, string>();

		public string GetStrFromWs(int wsId)
		{
			string result;
			if (m_WsIcu.TryGetValue(wsId, out result))
				return result;
			throw new ArgumentException("Writing system ID " + wsId + " not known.");
		}

		public void GetWritingSystems(ArrayPtr rgws, int cws)
		{
			throw new NotImplementedException();
		}

		public int GetWsFromStr(string key)
		{
			int result;
			if (m_IcuWs.TryGetValue(key, out result))
				return result;
			result = m_nextWs++;
			m_IcuWs[key] = result;
			m_WsIcu[result] = key;
			return result;
		}

		public bool IsShutdown
		{
			get { throw new NotImplementedException(); }
		}

		public int NumberOfWs
		{
			get { throw new NotImplementedException(); }
		}

		public void RemoveEngine(int ws)
		{
			throw new NotImplementedException();
		}

		public void SaveWritingSystems()
		{
			throw new NotImplementedException();
		}

		public void Serialize(IStorage _stg)
		{
			throw new NotImplementedException();
		}

		public void Shutdown()
		{

		}

		public ILgCharacterPropertyEngine UnicodeCharProps
		{
			get { throw new NotImplementedException(); }
		}

		public int UserWs
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public ILgCharacterPropertyEngine get_CharPropEngine(int ws)
		{
			throw new NotImplementedException();
		}

		public ILgCollatingEngine get_DefaultCollater(int ws)
		{
			throw new NotImplementedException();
		}

		public ILgWritingSystem get_Engine(string bstrIcuLocale)
		{
			throw new NotImplementedException();
		}

		public ILgWritingSystem get_EngineOrNull(int ws)
		{
			MockWsEngine result;
			m_wsToEngine.TryGetValue(ws, out result);
			return result;
		}

		Dictionary<int, MockWsEngine> m_wsToEngine = new Dictionary<int, MockWsEngine>();

		internal MockWsEngine MakeMockEngine(int ws, string id, IRenderEngine renderer)
		{
			var result = new MockWsEngine() {RenderEngine = renderer};
			m_IcuWs[id] = ws;
			m_WsIcu[ws] = id;
			m_wsToEngine[ws] = result;
			return result;
		}

		public IRenderEngine get_Renderer(int ws, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public IRenderEngine get_RendererFromChrp(ref LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	class MockWsEngine : ILgWritingSystem
	{
		internal IRenderEngine RenderEngine { get; set; }
		public IRenderEngine get_Renderer(IVwGraphics _vg)
		{
			return RenderEngine;
		}

		public void InterpretChrp(ref LgCharRenderProps chrp)
		{
			if (AssembledStyles.FaceNameFromChrp(chrp) == AssembledStyles.DefaultFontName)
				AssembledStyles.SetFaceName(ref chrp, "MockFont");
		}

		public string Id
		{
			get { throw new NotImplementedException(); }
		}

		public int Handle
		{
			get { throw new NotImplementedException(); }
		}

		public string LanguageName
		{
			get { throw new NotImplementedException(); }
		}

		public string ISO3
		{
			get { throw new NotImplementedException(); }
		}

		public int LCID
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string SpellCheckingId
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public bool RightToLeftScript
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string DefaultFontFeatures
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string DefaultFontName
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public ILgCharacterPropertyEngine CharPropEngine
		{
			get { throw new NotImplementedException(); }
		}

		public string Keyboard
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int CurrentLCID
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}
}
