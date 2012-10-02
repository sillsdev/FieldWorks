using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;

namespace SilEncConverters31
{
	/// <summary>
	/// This is the base class for the Compound (serial) EncConverter and the Primary-Fallback (parallel)
	/// EncConverter AutoConfigDialogs. Since both of these types are virtual converters and rely on some
	/// combination of other (existing) converters, there's probably some code common to both. This class
	/// is the place for that common code. (if there is none, then you can just get rid of this class)
	/// </summary>
	public partial class MetaCmpdAutoConfigDialog : SilEncConverters31.AutoConfigDialog
	{
		protected string[] m_astrStepFriendlyNames = null;
		protected bool[] m_abDirectionForwards = null;
		protected NormalizeFlags[] m_aeNormalizeFlags = null;

		public MetaCmpdAutoConfigDialog()
		{
			m_bQueryForConvType = false;    // the converter determines this itself.
		}

		protected override bool OnApply()
		{
			if (String.IsNullOrEmpty(FriendlyName))
				if (!QueryForFriendlyName() || String.IsNullOrEmpty(FriendlyName))
					return false;

			if (m_bIsModified)
				AddConverterMapping();

			// if it was okay, then go ahead and try to instantiate the converter (this'll display
			//  an error if the configuration is bad).
			m_aEC = InitializeEncConverter;
			if (m_aEC != null)
				return base.OnApply();

			return false;
		}

		protected override IEncConverter InitializeEncConverter
		{
			// we have to override this, because the base implementaton will try to construct
			//  a converter and initialize it from the ConverterIdentifier. But with Compound
			//  converters: a) they're already in the repository (by definition), and b) are
			//  not initialized based on ConverterIdentifier (but via the specialized
			//  AddConverterStep... methods. So we just want to get it based on the name
			get
			{
				// gotta have the converter identifier to call this property
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(FriendlyName));

				// if it changed, then start over again (here, 'changed' means a different friendly name or steps (which are
				//  implicit in the converter identifier)
				if ((m_aEC != null) && ((FriendlyName != m_aEC.Name) || (ConverterIdentifier != m_aEC.ConverterIdentifier)))
					m_aEC = null;

				if (m_aEC == null)
				{
					m_aEC = m_aECs[FriendlyName];
					ConverterIdentifier = m_aEC.ConverterIdentifier;
					LhsEncodingId = m_aEC.LeftEncodingID;
					RhsEncodingId = m_aEC.RightEncodingID;
					ConversionType = m_aEC.ConversionType;
					ProcessType = m_aEC.ProcessType;
				}

				return m_aEC;
			}
		}

		protected void QueryStepData()
		{
			// the EncConverter must be defined to call this method
			System.Diagnostics.Debug.Assert(m_aEC != null);

			// update the details about the array of steps
			m_astrStepFriendlyNames = m_aEC.ConverterNameEnum;
			if (m_astrStepFriendlyNames.Length == 0)
			{
				m_aEC = null;
			}
			else
			{
				m_abDirectionForwards = new bool[m_astrStepFriendlyNames.Length];
				m_aeNormalizeFlags = new NormalizeFlags[m_astrStepFriendlyNames.Length];

				for (int i = 0; i < m_astrStepFriendlyNames.Length; i++)
				{
					string strStepFriendlyName = m_astrStepFriendlyNames[i];
					int nIndex = strStepFriendlyName.IndexOf(CmpdEncConverter.cstrNormalizationFullyComposed);
					if (nIndex != -1)
					{
						m_aeNormalizeFlags[i] = NormalizeFlags.FullyComposed;
						strStepFriendlyName = strStepFriendlyName.Substring(0, nIndex);
					}
					else if ((nIndex = strStepFriendlyName.IndexOf(CmpdEncConverter.cstrNormalizationFullyDecomposed)) != -1)
					{
						m_aeNormalizeFlags[i] = NormalizeFlags.FullyDecomposed;
						strStepFriendlyName = strStepFriendlyName.Substring(0, nIndex);
					}
					else
						m_aeNormalizeFlags[i] = NormalizeFlags.None;

					// now check for Direction
					nIndex = strStepFriendlyName.IndexOf(CmpdEncConverter.cstrDirectionReversed);
					m_abDirectionForwards[i] = (nIndex != -1) ? false : true;
					m_astrStepFriendlyNames[i] = (nIndex != -1) ? strStepFriendlyName.Substring(0, nIndex) : strStepFriendlyName;
				}
			}
		}

		protected virtual void UpdateCompoundConverterNameLabel(string strFriendlyName)
		{
		}

		protected bool QueryForFriendlyName()
		{
			string strFriendlyName = (String.IsNullOrEmpty(FriendlyName)) ? DefaultFriendlyName : FriendlyName;
			QueryConverterNameForm dlg = new QueryConverterNameForm(strFriendlyName);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				// means we're saving in the repository
				// update the values from those the dialog box queried
				FriendlyName = dlg.FriendlyName;
				UpdateCompoundConverterNameLabel(FriendlyName);
				return true;
			}
			return false;
		}
	}
}
