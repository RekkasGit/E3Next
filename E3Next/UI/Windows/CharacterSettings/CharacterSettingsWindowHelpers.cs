using E3Core.Data;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;
using window = E3Core.UI.Windows.CharacterSettings.CharacterSettingsWindow;

namespace E3Core.UI.Windows.CharacterSettings
{
	public class CharacterSettingsWindowHelpers
	{

		public static Dictionary<string, SpellData> _spellCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_discCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_aaCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_skillCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase), // guess we are not showing much from skills :P 
		_itemCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);

		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _catalog_Spells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_AA = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Disc = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Skills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Items = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

		public static readonly string[] _spellKeyOutputOrder = new[]
		{
			"Gem", "Ifs", "CheckFor", "CastIF", "HealPct", "HealthMax", "Zone", "MinSick",
			"BeforeSpell", "AfterSpell", "BeforeEvent", "AfterEvent", "MinMana", "MaxMana", "MinEnd",
			"MinDurationBeforeRecast", "MaxTries", "Reagent", "CastType", "PctAggro", "Delay", "RecastDelay",
			"AfterEventDelay", "AfterSpellDelay", "BeforeEventDelay", "BeforeSpellDelay", "AfterCastDelay",
			"AfterCastCompletedDelay", "SongRefreshTime", "StackRequestItem", "StackRequestTargets",
			"StackCheckInterval", "StackRecastDelay", "MinHP", "MinHPTotal", "GiveUpTimer", "TriggerSpell",
			"MinAggro", "MaxAggro",
			"ExcludedClasses", "ExcludedNames"
		};
		public static readonly string[] _spellFlagOutputOrder = new[]
		{
			"NoInterrupt", "IgnoreStackRules", "NoTarget", "NoAggro", "NoBurn", "Rotate",
			"NoMidSongCast", "GoM", "AllowSpellSwap", "NoEarlyRecast", "NoStack", "Debug", "IsDoT", "IsDebuff"
		};
		public static readonly HashSet<string> _spellKnownKeys = new HashSet<string>(_spellKeyOutputOrder, StringComparer.OrdinalIgnoreCase);
		public static readonly HashSet<string> _spellKnownFlags = new HashSet<string>(_spellFlagOutputOrder, StringComparer.OrdinalIgnoreCase);
		public static readonly Dictionary<string, string> _spellKeyAliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"AfterCast", "AfterSpell"},
			{"BeforeCast", "BeforeSpell"},
			{"DelayAfterCast", "AfterCastCompletedDelay"},
			{"AfterCastCompletedDelay", "AfterCastCompletedDelay"},
			{"MinHpTotal", "MinHPTotal"},
			{"MinHp", "MinHP"}
		};

		public static readonly string[] _spellCastTypeOptions = new[] { "Spell", "AA", "Disc", "Ability", "Item", "None" };


		public static Int32 GetIconFromIniString(string value)
		{
			Int32 indexOfSlash = value.IndexOf('/');

			string spellName = value;
			if (indexOfSlash != -1)
			{
				spellName = value.Substring(0, indexOfSlash);
			}
			Int32 iconID = 0;
			if (_spellCatalogLookup.TryGetValue(spellName, out var tspell))
			{
				iconID = tspell.SpellIcon;
			}
			else if (_aaCatalogLookup.TryGetValue(spellName, out var taa))
			{
				iconID = taa.SpellIcon;
			}
			else if (_itemCatalogLookup.TryGetValue(spellName, out var titem))
			{
				iconID = titem.SpellIcon;

			}
			else if (_discCatalogLookup.TryGetValue(spellName, out var tdisc))
			{
				iconID = tdisc.SpellIcon;
			}

			return iconID;

		}

		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = Convert.ToDouble(s.MyCastTimeInSeconds),
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = s.MyCastTimeInSeconds,
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingSkillsCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				list.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}

		// Organize skills like e3config: force into Skill/Basic and list by spell name
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeSkillsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				list.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}

		public static Dictionary<string, SpellData> ConvertSpellsToSpellDataLookup(List<Spell> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				var tspell = spell.ToProto();
				if (!returnValue.ContainsKey(tspell.CastName))
				{
					returnValue.Add(tspell.CastName, tspell);
				}
			}
			return returnValue;
		}
		public static Dictionary<string, SpellData> ConvertToSpellDataLookup(Google.Protobuf.Collections.RepeatedField<SpellData> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				if (!returnValue.ContainsKey(spell.CastName))
				{
					returnValue.Add(spell.CastName, spell);
				}

			}
			return returnValue;
		}
		public static string GenerateUniqueKey(KeyDataCollection keys, string baseName)
		{
			string unique = baseName;
			int idx = 1;
			while (keys.ContainsKey(unique))
			{
				unique = $"{baseName} ({idx})";
				idx++;
				if (idx > 1000)
				{
					return null;
				}
			}
			return unique;
		}
		public static void RefreshEditableSpellState(bool force = false)
		{


			var mainWindowState = window._state.GetState<State_MainWindow>();
			var spellEditorState = window._state.GetState<State_SpellEditor>();

			if (force) { mainWindowState.Currently_EditableSpell = null; }

			//check if this has changed from what we were before
			if (String.IsNullOrWhiteSpace(mainWindowState.SelectedSection))
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			if (String.IsNullOrWhiteSpace(mainWindowState.SelectedKey))
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			if (mainWindowState.SelectedValueIndex == -1)
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			//lets get the actual entry
			var kd = GetCurrentEditedSpellKeyData();
			if (kd == null) return;

			var rawValue = kd.ValueList[mainWindowState.SelectedValueIndex];

			string entryLabel = $"[{mainWindowState.SelectedSection}] {mainWindowState.SelectedKey} entry #{mainWindowState.SelectedValueIndex + 1}";

			if (!String.Equals(mainWindowState.Signature_CurrentEditedSpell, entryLabel) || mainWindowState.Currently_EditableSpell == null)
			{
				mainWindowState.Signature_CurrentEditedSpell = entryLabel;
				mainWindowState.Currently_EditableSpell = new Spell(rawValue, mainWindowState.CurrentINIData, false);
				spellEditorState.Reset();
			}


		}
		public static KeyData GetCurrentEditedSpellKeyData()
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			var data = mainWindowState.CurrentINIData;
			if (data == null) return null;
			var sectionData = data.Sections.GetSectionData(mainWindowState.SelectedSection);
			if (sectionData == null) return null;
			var kd = sectionData.Keys.GetKeyData(mainWindowState.SelectedKey);
			if (kd == null) return null;
			if (kd.ValueList.Count <= mainWindowState.SelectedValueIndex) return null;

			return kd;
		}

	}
}
