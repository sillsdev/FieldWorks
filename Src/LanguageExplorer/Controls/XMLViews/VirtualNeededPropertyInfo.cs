// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class VirtualNeededPropertyInfo : NeededPropertyInfo
	{
		public VirtualNeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq, int dstClsId)
			: base(flidSource, parent, fSeq)
		{
			m_targetClass = dstClsId;
		}

		/// <summary>
		/// Override: this class knows the appropriate destination class.
		/// </summary>
		public override int TargetClass(ISilDataAccess sda)
		{
			return m_targetClass;
		}
	}
}