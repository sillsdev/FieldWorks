// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.AllomorphGenerator
{
	public partial class EnvironmentsChooser : Form
	{
		LcmCache Cache { get; set; }
		public List<AlloGenModel.Environment> Environments { get; set; }
		public List<AlloGenModel.Environment> SelectedEnvironments { get; set; }

		public EnvironmentsChooser(LcmCache cache)
		{
			Cache = cache;
			Environments = new List<AlloGenModel.Environment>();
			SelectedEnvironments = new List<AlloGenModel.Environment>();
			InitializeComponent();
			clbEnvironments.CheckOnClick = true;
			clbEnvironments.Sorted = true;
			CreateEnvironments();
		}

		void CreateEnvironments()
		{
			ILcmOwningSequence<IPhEnvironment> allEnvs = Cache
				.LangProject
				.PhonologicalDataOA
				.EnvironmentsOS;
			foreach (IPhEnvironment env in allEnvs)
			{
				LCModel.DomainServices.ConstraintFailure failure;
				if (
					env.CheckConstraints(
						PhEnvironmentTags.kflidStringRepresentation,
						false,
						out failure
					)
				)
				{
					AlloGenModel.Environment environ = new AlloGenModel.Environment();
					environ.Guid = env.Guid.ToString();
					environ.Name = env.StringRepresentation.Text;
					Environments.Add(environ);
				}
			}
		}

		public void setSelected(List<AlloGenModel.Environment> selectedOnes)
		{
			SelectedEnvironments.Clear();
			SelectedEnvironments.AddRange(selectedOnes);
		}

		public void FillEnvironmentsListBox()
		{
			clbEnvironments.Items.Clear();
			foreach (AlloGenModel.Environment env in Environments)
			{
				if (!string.IsNullOrEmpty(env.Name))
					clbEnvironments.Items.Add(env);
			}
			for (int i = 0; i < clbEnvironments.Items.Count; i++)
			{
				AlloGenModel.Environment env = clbEnvironments.Items[i] as AlloGenModel.Environment;
				for (int j = 0; j < SelectedEnvironments.Count; j++)
				{
					if (SelectedEnvironments[j].Guid == env.Guid)
					{
						clbEnvironments.SetItemChecked(i, true);
						break;
					}
				}
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedEnvironments.Clear();
			for (int i = 0; i < clbEnvironments.Items.Count; i++)
			{
				if (clbEnvironments.GetItemChecked(i))
				{
					AlloGenModel.Environment mt =
						clbEnvironments.Items[i] as AlloGenModel.Environment;
					SelectedEnvironments.Add(mt);
				}
			}
		}
	}
}
