/*
 *    WorldPadAppModel.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.IO;
using System.Collections;

namespace SIL.FieldWorks.WorldPad
{
	public class AppModelChangedEventArgs : EventArgs, IAppModelChangedEventArgs
	{
		public readonly int hour;
		public readonly int minute;
		public readonly int second;

		public AppModelChangedEventArgs(int hour, int minute, int second)
		{
			this.hour = hour;
			this.minute = minute;
			this.second = second;
		}
	}

	public class WorldPadAppModel : IWorldPadAppModel
	{
		public event AppModelChangedEventHandler ModelChanged;

		private ArrayList docModels = new ArrayList();

		public WorldPadAppModel()
		{
			Console.WriteLine("WorldPadAppModel.ctor invoked");
		}

		public void ActionPerformed()
		{
			Console.WriteLine("WorldPadAppModel.ActionPerformed() invoked");

			/*if (OnModelInfoChange != null)
			{*/
				System.DateTime dt = System.DateTime.Now;
				/*ModelInfoEventArgs modelInformation =
					new ModelInfoEventArgs(dt.Hour, dt.Minute, dt.Second);
				OnModelInfoChange(this, modelInformation);*/
				AppModelChangedEventArgs e =
					new AppModelChangedEventArgs(dt.Hour, dt.Minute, dt.Second);
				OnModelChanged(e);
			/*}*/
		}

		protected virtual void OnModelChanged(AppModelChangedEventArgs e)
		{
			Console.WriteLine("WorldPadAppModel.OnModelChanged() invoked");

			if (ModelChanged != null)
			{
				ModelChanged(this, e);
			}
		}

		public IWorldPadDocModel AddDoc()
		{
			Console.WriteLine("WorldPadAppModel.AddDoc() invoked");

			IWorldPadDocModel docModel =
				(IWorldPadDocModel) new WorldPadDocModel(this);
				/*(IWorldPadDocModel) new WorldPadDocModelGtk(this);*/

			docModels.Add(docModel);

			return docModel;
		}

		public IWorldPadDocModel AddDoc(string fileName)
		{
			fileName = AssureFullName(fileName);
			Console.WriteLine("WorldPadAppModel.AddDoc({0}) invoked", fileName);

			IWorldPadDocModel docModel =
				(IWorldPadDocModel) new WorldPadDocModel(this, fileName);
				/*(IWorldPadDocModel) new WorldPadDocModelGtk(this, fileName);*/

			docModels.Add(docModel);

			return docModel;
		}

		public string AssureFullName(string fileName)
		{
			int lastIndex = fileName.LastIndexOf(Path.DirectorySeparatorChar);
			if (lastIndex == -1)
				fileName = System.Environment.CurrentDirectory
					+ Path.DirectorySeparatorChar + fileName;
			return fileName;
		}

		public void Init()
		{
			Console.WriteLine("WorldPadAppModel.Init() invoked");
		}

		/*public void Subscribe(ModelInfoEventHandler handler)*/
		public void Subscribe(AppModelChangedEventHandler handler)
		{
			Console.WriteLine("WorldPadAppModel.Subscribe() invoked");

			/*OnModelInfoChange += handler;*/
			ModelChanged += handler;
		}
	}
}
