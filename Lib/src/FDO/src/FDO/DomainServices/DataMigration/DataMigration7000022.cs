// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000022.cs
// Responsibility: FW team
//
// <remarks>
// </remarks>

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000021 to 7000022.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000022 : IDataMigration
	{
		private readonly string[,] m_listNames = {
			{"0", "ChartMarkers", "Text Chart Markers", "Marcadores para la tabla de texto",
				"نشانه جدول متن", "Étiquettes de tableau de texte", "Penanda-penanda Bagan Teks",
				"Marcadores de Quadro de Textos", "Метки схемы текста", "文本图表标记"},
			{"0", "ConstChartTempl", "Text Constituent Chart Templates", "Plantillas para tabla de texto",
				"الگوهای جدول ساختار متن", "Modèles pour tableaux des constituants d’un texte",
				"Pola-pola Acuan untuk Bagan Unsur Teks", "Modelos de Quadros de Constituintes do Texto",
				"Шаблоны схемы НС текста", "文本成分图表模板"},
			{"1", "AffixCategories", "Affix Categories", "", "", "", "", "", "", ""},
			{"1", "AnalysisStatus", "Status", "Estado", "وضعیت", "Statut", "Status", "Status", "Состояние", "状态"},
			{"1", "AnnotationDefs", "Annotation Definitions", "", "", "", "", "", "", ""},
			{"1", "AnthroList", "Anthropology Categories", "Categorías antropológicas", "مقوله‌های انسان‌شناسی‌",
				"Catégories anthropologiques", "Kategori Antropologi", "Categorias Antropológicas",
				"Антропологические категории", "人类学类别"},
			{"1", "ConfidenceLevels", "Confidence Levels", "Niveles de confiabilidad", "سطوح اطمینان",
				"Niveaux de confiance", "Tingkat Kepercayaan", "Níveis de Confiança", "Уровни конфиденциальности",
				"信心水平"},
			{"1", "Education", "Education Levels", "Niveles de educación", "سطوح تحصیلات", "Niveaux d'education",
				"Tingkat Pendidikan", "Níveis de Educação", "Уровни образования", "教育水平"},
			{"1", "GenreList", "Genres", "Géneros", "انواع", "Genres", "Jenis Teks", "Gêneros", "Жанры", "体裁"},
			{"1", "Locations", "Locations", "Ubicaciones", "مکانها", "Emplacements", "Lokasi-lokasi", "Localizações",
				"Места", "地点"},
			{"1", "People", "People", "", "اشخاص", "Gens", "Orang", "Pessoas", "Люди", "民族"},
			{"1", "Positions", "Positions", "", "شغلها", "Positions", "Posisi", "Posições", "Позиции", "位置"},
			{"1", "Restrictions", "Restrictions", "Restricciones", "محدودیتها و موانع", "Restrictions",
				"Batasan", "Restrições", "Ограничения", "限制"},
			{"1", "SemanticDomainList", "Semantic Domains", "Dominios semánticos", "حوضه معنایی", "Domaines sémantiques",
				"Medan Makna", "Domínios Semânticos", "Семантические Области", "语义范围"},
			{"1", "TextMarkupTags", "Text Markup Tags", "Etiquetas para marcar textos", "علامت گذاری متن",
				"Étiquettes de texte", "Tanda-tanda Markah Teks", "Etiquetas de Marcação de Texto",
				"Теги разметки текста", "文本标记"},
			{"1", "TimeOfDay", "Time Of Day", "", "", "Moment de la journée", "", "", "", ""},
			{"1", "TranslationTags", "Translation Types", "Tipos de traducción", "انواع ترجمه", "Types de traduction",
				"Jenis Terjemahan", "Tipos de Tradução", "Типы перевода", "翻译类型"},
			{"2", "ComplexEntryTypes", "Complex Form Types", "Tipos de forma compleja", "شکلهای پیچیده",
				"Types de forme complexe", "Tipe-tipe Bentuk Kompleks", "Tipos de Forma Complexa",
				"Тип сложной формы", "复合类型"},
			{"2", "DomainTypes", "Academic Domains", "Dominios académicos", "حوضه‌های دانشگاهی و علمی",
				"Domaines techniques", "Ranah Akademis", "Domínios Acadêmicos", "Области знания", "学术领域"},
			{"2", "MorphTypes", "Morpheme Types", "Tipos de morfemas", "انواع تکواژ", "Types de morphèmes",
				"Jenis Morfem", "Tipos de morfema", "Типы морфем", "语素类型"},
			{"2", "References", "Lexical Relations", "Relaciones léxicas", "روابط واژه‌ای", "Relations lexicales",
				"Hubungan Kata", "Relações Lexicais", "Лексические отношения", "语义关系"},
			{"2", "SenseTypes", "Sense Types", "Tipos de acepción", "انواع معنی", "Types de sens", "Jenis Pengertian",
				"Tipos de Significado", "Типы значений", "词义类型"},
			{"2", "Status", "Sense Status", "Estatus de la acepción", "وضعیت معنی", "Statut de sens", "Status Pengertian",
				"Status do Significado", "Статус значения", "词义状态"},
			{"2", "UsageTypes", "Usages", "Usos", "کاربردها", "Utilisations", "Penggunaan", "Usos", "Применения","用法"},
			{"2", "VariantEntryTypes", "Variant Types", "Tipos de variante", "انواع گونۀ متبوع", "Types de variante",
				"Tipe-tipe Varian", "Tipos de Variante", "Типы вариантов", "变体类型"},
			{"3", "RecTypes", "Notebook Record Types", "", "", "Types d'enregistrement de carnet", "", "", "", ""}};

		private enum ListOwner
		{
			Discourse,
			LangProj,
			LexDb,
			RNbk
		}

		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Name property for several CmPossibilityLists to match what is displayed
		/// in the Lists area. Sets Name for en, es, fa, fr, id, pt, ru and zh_CN wss.
		/// </summary>
		/// <param name="dtoRepos">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of it work.
		///
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		///
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository dtoRepos)
		{
			DataMigrationServices.CheckVersionNumber(dtoRepos, 7000021);

			// We need 'LangProject' and its DsDiscourseData, LexDb and RnResearchNbk objects.
			var lpDto = dtoRepos.AllInstancesSansSubclasses("LangProject").FirstOrDefault();
			var lpElement = lpDto == null ? null : XElement.Parse(lpDto.Xml);

			var discourseDataDto = dtoRepos.AllInstancesSansSubclasses("DsDiscourseData").FirstOrDefault();
			var dsDataElement = discourseDataDto == null ? null : XElement.Parse(discourseDataDto.Xml);

			var lexDbDto = dtoRepos.AllInstancesSansSubclasses("LexDb").FirstOrDefault();
			var lexDbElement = lexDbDto == null ? null : XElement.Parse(lexDbDto.Xml);

			var nbkDto = dtoRepos.AllInstancesSansSubclasses("RnResearchNbk").FirstOrDefault();
			var nbkElement = nbkDto == null ? null : XElement.Parse(nbkDto.Xml);

			for (var i = 0; i < m_listNames.GetLength(0); i++)
			{
				var owner = (ListOwner)Int32.Parse(m_listNames[i, 0]);
				XElement owningElem;
				switch (owner)
				{
					case ListOwner.Discourse:
						owningElem = dsDataElement;
						break;
					case ListOwner.LexDb:
						owningElem = lexDbElement;
						break;
					case ListOwner.RNbk:
						owningElem = nbkElement;
						break;
					default:
						owningElem = lpElement;
						break;
				}
				if (owningElem == null)
					continue;
				var listDto = GetListDto(dtoRepos, owningElem, m_listNames[i, 1]);
				if (listDto == null || listDto.Xml == null)
					continue;
				var listElement = XElement.Parse(listDto.Xml);
				var nameElem = GetListNameElement(listElement) ?? MakeEmptyNameElementInList(listElement);
				for (var j = 2; j < m_listNames.GetLength(1); j++)
					AddReplaceLocalizedListName(nameElem, GetWsStrForColumn(j), i, j);

				DataMigrationServices.UpdateDTO(dtoRepos, listDto, listElement.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(dtoRepos);
		}

		private static string GetWsStrForColumn(int column)
		{
			string result;
			switch (column)
			{
				default:
					result = "en";
					break;
				case 3:
					result = "es";
					break;
				case 4:
					result = "fa";
					break;
				case 5:
					result = "fr";
					break;
				case 6:
					result = "id";
					break;
				case 7:
					result = "pt";
					break;
				case 8:
					result = "ru";
					break;
				case 9:
					result = "zh-CN";
					break;
			}
			return result;
		}

		private static XElement MakeEmptyNameElementInList(XElement listElement)
		{
			var nameElem = XElement.Parse("<Name/>");
			listElement.AddFirst(nameElem);
			return nameElem;
		}

		private static DomainObjectDTO GetListDto(IDomainObjectDTORepository dtoRepos,
			XElement owningElem, string flidName)
		{
			var xPath = flidName + "/objsur";
			var objsurElem = owningElem.XPathSelectElement(xPath);
			if (objsurElem == null)
				return null;
			var guid = objsurElem.Attribute("guid").Value;
			DomainObjectDTO dto;
			if(dtoRepos.TryGetValue(guid, out dto))
				return dto;
			return null;
		}

		private static XElement GetListNameElement(XElement listElem)
		{
			return listElem.XPathSelectElement("Name");
		}

		/// <summary>
		/// Adds a localized name string to the Name field for every non-empty string in the
		/// localized name array. Skips empty strings. If a particular ws already exists, we
		/// overwrite it.
		/// </summary>
		/// <param name="listNameElem"></param>
		/// <param name="wsStr"></param>
		/// <param name="row"></param>
		/// <param name="column"></param>
		private void AddReplaceLocalizedListName(XElement listNameElem, string wsStr, int row, int column)
		{
			if (m_listNames[row, column] == "")
				return;
			var aUniElem = XElement.Parse("<AUni ws =\"" + wsStr + "\">" + m_listNames[row, column] + "</AUni>");
			var existingAUniElem = listNameElem.XPathSelectElement("AUni[@ws = \"" + wsStr + "\"]");
			if (existingAUniElem == null)
				listNameElem.Add(aUniElem);
			else
			{
				if (existingAUniElem != aUniElem)
					existingAUniElem.ReplaceWith(aUniElem);
			}
		}

		#endregion
	}
}
