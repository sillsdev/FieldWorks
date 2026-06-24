// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Phonology;
using SIL.LCModel.Core.Text;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — the phonological-environment editor plugin: claims the
	/// legacy <c>SIL.FieldWorks.XWorks.MorphologyEditor.PhEnvStrRepresentationSlice</c> and renders the
	/// <c>IPhEnvironment.StringRepresentation</c> as a validated editable field (<see cref="PhEnvironmentEditor"/>).
	/// The LCModel read/write + the <c>PhonEnvRecognizer</c> validator live here (design Decision 1).
	/// </summary>
	public sealed class PhEnvironmentRegionEditorPlugin : IRegionEditorPlugin
	{
		public const string PhEnvStrRepresentationSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.PhEnvStrRepresentationSlice";

		public string LegacyClassName => PhEnvStrRepresentationSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var env = context?.Target as IPhEnvironment;
			var cache = context?.Cache;
			if (env == null || cache == null)
				return null;
			try
			{
				var representation = env.StringRepresentation?.Text ?? string.Empty;
				var host = context.EditContext;
				var sink = host == null ? null : new PhEnvironmentEditSink(env, cache, host);
				return new PhEnvironmentEditor(representation, sink);
			}
			catch (Exception e)
			{
				Logger.WriteEvent($"PhEnvironmentRegionEditorPlugin: environment editor unavailable for '{env.Guid}': {e}");
				return null;
			}
		}
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — validates an environment string through the same
	/// <see cref="PhonEnvRecognizer"/> the legacy slice used, and writes the validated
	/// <c>StringRepresentation</c> (vernacular) through the region's shared fenced session as one undo step.
	/// </summary>
	internal sealed class PhEnvironmentEditSink : IPhEnvironmentCommandSink
	{
		private readonly IPhEnvironment _env;
		private readonly LcmCache _cache;
		private readonly IRegionEditContext _host;
		private readonly PhonEnvRecognizer _validator;

		public PhEnvironmentEditSink(IPhEnvironment env, LcmCache cache, IRegionEditContext host)
		{
			_env = env;
			_cache = cache;
			_host = host;
			var phonData = cache.LangProject.PhonologicalDataOA;
			_validator = new PhonEnvRecognizer(
				phonData.AllPhonemes().ToArray(),
				phonData.AllNaturalClassAbbrs().ToArray());
		}

		public bool Validate(string representation)
			=> _validator.Recognize(representation ?? string.Empty);

		public bool Commit(string representation)
		{
			bool Write()
			{
				_env.StringRepresentation = TsStringUtils.MakeString(representation ?? string.Empty, _cache.DefaultVernWs);
				return true;
			}

			if (_host is RegionEditContextBase fenced)
			{
				var ok = fenced.Stage(Write, "Environment");
				if (ok)
					fenced.Commit();
				return ok;
			}
			return Write();
		}
	}
}
