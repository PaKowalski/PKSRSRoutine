using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Buddy.Coroutines;
using log4net;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Common;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;

namespace PKSRSRoutine
{
    public class PKSRSRoutine : IRoutine
    {
        #region Temp Compatibility 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint"></param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns></returns>
        public static int NumberOfMobsBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 5,
            bool dontLeaveFrame = false)
        {
            var mobs = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(d => d.IsActive).ToList();
            if (!mobs.Any())
                return 0;

            var path = ExilePather.GetPointsOnSegment(start.Position, end.Position, dontLeaveFrame);

            var count = 0;
            for (var i = 0; i < path.Count; i += 10)
            {
                foreach (var mob in mobs)
                {
                    if (mob.Position.Distance(path[i]) <= distanceFromPoint)
                    {
                        ++count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 10,
            int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(NetworkObject start, Vector2i end, int distanceFromPoint = 10,
            int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(Vector2i start, NetworkObject end, int distanceFromPoint = 10,
            int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(Vector2i start, Vector2i end, int distanceFromPoint = 10, int stride = 10,
            bool dontLeaveFrame = false)
        {
            var doors = LokiPoe.ObjectManager.Doors.Where(d => !d.IsOpened).ToList();

            if (!doors.Any())
                return false;

            var path = ExilePather.GetPointsOnSegment(start, end, dontLeaveFrame);

            for (var i = 0; i < path.Count; i += stride)
            {
                foreach (var door in doors)
                {
                    if (door.Position.Distance(path[i]) <= distanceFromPoint)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the number of mobs near a target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <param name="dead"></param>
        /// <returns></returns>
        public static int NumberOfMobsNear(NetworkObject target, float distance, bool dead = false)
        {
            var mpos = target.Position;

            var curCount = 0;

            foreach (var mob in LokiPoe.ObjectManager.Objects.OfType<Monster>())
            {
                if (mob.Id == target.Id)
                {
                    continue;
                }

                // If we're only checking for dead mobs... then... yeah...
                if (dead)
                {
                    if (!mob.IsDead)
                    {
                        continue;
                    }
                }
                else if (!mob.IsActive)
                {
                    continue;
                }

                if (mob.Position.Distance(mpos) < distance)
                {
                    curCount++;
                }
            }

            return curCount;
        }

        #endregion

        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        // Auto-set, you do not have to change these.
        private int _sgSlot;
        private int _summonChaosGolemSlot = -1;
        private int _summonIceGolemSlot = -1;
        private int _summonFlameGolemSlot = -1;
        private int _summonStoneGolemSlot = -1;
        private int _summonLightningGolemSlot = -1;
        private int _raiseZombieSlot = -1;
        private int _raiseSpectreSlot = -1;
        private int _enduringCrySlot = -1;
        private int _bloodRageSlot = -1;
        private int _rfSlot = -1;
        private readonly List<int> _curseSlots = new List<int>();
        private int _auraSlot = -1;
        private int _totemSlot = -1;
        private int _summonRagingSpiritSlot = -1;
        private int _totalCursesAllowed;

        private bool _isCasting;
        private int _castingSlot;

        private int _currentLeashRange = -1;
        
        private readonly Stopwatch _totemStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _vaalStopwatch = Stopwatch.StartNew();
        

        private readonly Stopwatch _summonGolemStopwatch = Stopwatch.StartNew();

        private int _summonRagingSpiritCount;
        private readonly Stopwatch _summonRagingSpiritStopwatch = Stopwatch.StartNew();
        
        private bool _needsUpdate;

        private readonly Targeting _combatTargeting = new Targeting();

        private Dictionary<string, Func<dynamic[], object>> _exposedSettings;

        private void RegisterExposedSettings()
        {
            if (_exposedSettings != null)
                return;

            _exposedSettings = new Dictionary<string, Func<dynamic[], object>>();

            // Not a part of settings, so do it manually
            _exposedSettings.Add("SetLeash", param =>
            {
                _currentLeashRange = (int)param[0];
                return null;
            });

            _exposedSettings.Add("GetLeash", param =>
            {
                return _currentLeashRange;
            });

            // Automatically handle all settings

            PropertyInfo[] properties = typeof(PKSRSRoutineSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in properties)
            {
                // Only work with ints
                if (p.PropertyType != typeof(int) && p.PropertyType != typeof(bool))
                {
                    continue;
                }

                // If not writable then cannot null it; if not readable then cannot check it's value
                if (!p.CanWrite || !p.CanRead)
                {
                    continue;
                }

                MethodInfo mget = p.GetGetMethod(false);
                MethodInfo mset = p.GetSetMethod(false);

                // Get and set methods have to be public
                if (mget == null)
                {
                    continue;
                }
                if (mset == null)
                {
                    continue;
                }

                Log.InfoFormat("Name: {0} ({1})", p.Name, p.PropertyType);

                _exposedSettings.Add("Set" + p.Name, param =>
                {
                    p.SetValue(PKSRSRoutineSettings.Instance, param[0]);
                    return null;
                });

                _exposedSettings.Add("Get" + p.Name, param =>
                {
                    return p.GetValue(PKSRSRoutineSettings.Instance);
                });
            }
        }

        private bool IsBlacklistedSkill(int id)
        {
            var tokens = PKSRSRoutineSettings.Instance.BlacklistedSkillIds.Split(new[]
            {
                ' ', ',', ';', '-'
            }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                int result;
                if (int.TryParse(token, out result))
                {
                    if (result == id)
                        return true;
                }
            }
            return false;
        }

        public Targeting CombatTargeting
        {
            get
            {
                return _combatTargeting;
            }
        }

        #region Targeting

        private void CombatTargetingOnWeightCalculation(NetworkObject entity, ref float weight)
        {
            weight -= entity.Distance / 2;

            var m = entity as Monster;
            if (m == null)
                return;

            // If the monster is the source of Allies Cannot Die, we really want to kill it fast.
            if (m.HasAura("monster_aura_cannot_die"))
                weight += 40;

            if (m.IsTargetingMe)
            {
                weight += 20;
            }

            if (m.Rarity == Rarity.Magic)
            {
                weight += 5;
            }
            else if (m.Rarity == Rarity.Rare)
            {
                weight += 10;
            }
            else if (m.Rarity == Rarity.Unique)
            {
                weight += 15;
            }

            // Minions that get in the way.
            switch (m.Name)
            {
                case "Summoned Skeleton":
                    weight -= 15;
                    break;

                case "Raised Zombie":
                    weight -= 15;
                    break;
            }

            if (m.Rarity == Rarity.Normal && m.Type.Contains("/Totems/"))
            {
                weight -= 15;
            }

            // Necros
            if (m.ExplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")) ||
                m.ImplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")))
            {
                weight += 30;
            }

            // Ignore these mostly, as they just respawn.
            if (m.Type.Contains("TaniwhaTail"))
            {
                weight -= 30;
            }

            // Ignore mobs that expire and die
            if (m.Components.DiesAfterTimeComponent != null)
            {
                weight -= 15;
            }
        }

        private readonly string[] _aurasToIgnore = new[]
        {
            "shrine_godmode", // Ignore any mob near Divine Shrine
			"bloodlines_invulnerable", // Ignore Phylacteral Link
			"god_mode", // Ignore Animated Guardian
			"bloodlines_necrovigil",
        };
        

        private bool CombatTargetingOnInclusionCalcuation(NetworkObject entity)
        {
            try
            {
                var m = entity as Monster;
                if (m == null)
                    return false;

                if (Blacklist.Contains(m))
                    return false;

                // Do not consider inactive/dead mobs.
                if (!m.IsActive)
                    return false;

                // Ignore any mob that cannot die.
                if (m.CannotDie)
                    return false;

                // Ignore mobs that are too far to care about.
                if (m.Distance > (_currentLeashRange != -1 ? _currentLeashRange : PKSRSRoutineSettings.Instance.CombatRange))
                    return false;

                // Ignore mobs with special aura/buffs
                if (m.HasAura(_aurasToIgnore))
                    return false;

                // Ignore Voidspawn of Abaxoth: thanks ExVault!
                if (m.ExplicitAffixes.Any(a => a.DisplayName == "Voidspawn of Abaxoth"))
                    return false;

                // Ignore these mobs when trying to transition in the dom fight.
                // Flag1 has been seen at 5 or 6 at times, so need to work out something more reliable.
                if (m.Name == "Miscreation")
                {
                    var dom = LokiPoe.ObjectManager.GetObjectByName<Monster>("Dominus, High Templar");
                    if (dom != null && !dom.IsDead &&
                        (dom.Components.TransitionableComponent.Flag1 == 6 || dom.Components.TransitionableComponent.Flag1 == 5))
                    {
                        Blacklist.Add(m.Id, TimeSpan.FromHours(1), "Miscreation");
                        return false;
                    }
                }

                // Ignore Piety's portals.
                if (m.Name == "Chilling Portal" || m.Name == "Burning Portal")
                {
                    Blacklist.Add(m.Id, TimeSpan.FromHours(1), "Piety portal");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[CombatOnInclusionCalcuation]", ex);
                return false;
            }
            return true;
        }

        #endregion

        #region Implementation of IBase

        /// <summary>Initializes this routine.</summary>
        public void Initialize()
        {
            Log.DebugFormat("[PKSRSRoutine] Initialize");

            _combatTargeting.InclusionCalcuation += CombatTargetingOnInclusionCalcuation;
            _combatTargeting.WeightCalculation += CombatTargetingOnWeightCalculation;

            RegisterExposedSettings();
        }

        /// <summary> </summary>
        public void Deinitialize()
        {
        }

        #endregion

        #region Implementation of IAuthored

        /// <summary>The name of the routine.</summary>
        public string Name
        {
            get
            {
                return "PKSRSRoutine";
            }
        }

        /// <summary>The description of the routine.</summary>
        public string Description
        {
            get
            {
                return "An example routine for Exilebuddy.";
            }
        }

        /// <summary>
        /// The author of this object.
        /// </summary>
        public string Author
        {
            get
            {
                return "Bossland GmbH";
            }
        }

        /// <summary>
        /// The version of this routone.
        /// </summary>
        public string Version
        {
            get
            {
                return "0.0.1.1";
            }
        }

        #endregion

        #region Implementation of IRunnable

        /// <summary> The routine start callback. Do any initialization here. </summary>
        public void Start()
        {
            Log.DebugFormat("[PKSRSRoutine] Start");

            _needsUpdate = true;

            if (PKSRSRoutineSettings.Instance.SingleTargetMeleeSlot == -1 &&
                PKSRSRoutineSettings.Instance.SingleTargetRangedSlot == -1 &&
                PKSRSRoutineSettings.Instance.AoeMeleeSlot == -1 &&
                PKSRSRoutineSettings.Instance.AoeRangedSlot == -1 &&
                PKSRSRoutineSettings.Instance.FallbackSlot == -1
                )
            {
                Log.ErrorFormat(
                    "[Start] Please configure the PKSRSRoutine settings (Settings -> PKSRSRoutine) before starting!");
                BotManager.Stop();
            }
        }

        private bool IsCastableHelper(Skill skill)
        {
            return skill != null && skill.IsCastable && !skill.IsTotem && !skill.IsTrap && !skill.IsMine;
        }

        private bool IsAuraName(string name)
        {
            // This makes sure auras on items don't get used, since they don't have skill gems, and won't have an Aura tag.
            if (!PKSRSRoutineSettings.Instance.EnableAurasFromItems)
            {
                return false;
            }

            var auraNames = new string[]
            {
                "Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Purity of Elements",
                "Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath"
            };

            return auraNames.Contains(name);
        }

        /// <summary> The routine tick callback. Do any update logic here. </summary>
        public void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;

            if (_needsUpdate)
            {
                _sgSlot = -1;
                _summonChaosGolemSlot = -1;
                _summonFlameGolemSlot = -1;
                _summonIceGolemSlot = -1;
                _summonStoneGolemSlot = -1;
                _summonLightningGolemSlot = -1;
                _raiseZombieSlot = -1;
                _raiseSpectreSlot = -1;
                _enduringCrySlot = -1;
                _auraSlot = -1;
                _totemSlot = -1;
                _bloodRageSlot = -1;
                _rfSlot = -1;
                _summonRagingSpiritSlot = -1;
                _summonRagingSpiritCount = 0;
                _curseSlots.Clear();
                _totalCursesAllowed = LokiPoe.Me.TotalCursesAllowed;

                // Register curses.
                foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
                {
                    var tags = skill.SkillTags;
                    var name = skill.Name;

                    if (tags.Contains("curse"))
                    {
                        var slot = skill.Slot;
                        if (slot != -1 && skill.IsCastable && !skill.IsAurifiedCurse)
                        {
                            _curseSlots.Add(slot);
                        }
                    }

                    if (_auraSlot == -1 &&
                        ((tags.Contains("aura") && !tags.Contains("vaal")) || IsAuraName(name) || skill.IsAurifiedCurse ||
                        skill.IsConsideredAura))
                    {
                        _auraSlot = skill.Slot;
                    }

                    if (skill.IsTotem && _totemSlot == -1)
                    {
                        _totemSlot = skill.Slot;
                    }
                }
                
                var srs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Raging Spirit");
                if (IsCastableHelper(srs))
                {
                    _summonRagingSpiritSlot = srs.Slot;
                }

                var rf = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Righteous Fire");
                if (IsCastableHelper(rf))
                {
                    _rfSlot = rf.Slot;
                }

                var br = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Blood Rage");
                if (IsCastableHelper(br))
                {
                    _bloodRageSlot = br.Slot;
                }
                

                var ec = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Enduring Cry");
                if (IsCastableHelper(ec))
                {
                    _enduringCrySlot = ec.Slot;
                }

                var scg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Chaos Golem");
                if (IsCastableHelper(scg))
                {
                    _summonChaosGolemSlot = scg.Slot;
                    _sgSlot = _summonChaosGolemSlot;
                }

                var sig = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Ice Golem");
                if (IsCastableHelper(sig))
                {
                    _summonIceGolemSlot = sig.Slot;
                    _sgSlot = _summonIceGolemSlot;
                }

                var sfg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Flame Golem");
                if (IsCastableHelper(sfg))
                {
                    _summonFlameGolemSlot = sfg.Slot;
                    _sgSlot = _summonFlameGolemSlot;
                }

                var ssg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Stone Golem");
                if (IsCastableHelper(ssg))
                {
                    _summonStoneGolemSlot = ssg.Slot;
                    _sgSlot = _summonStoneGolemSlot;
                }

                var slg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Lightning Golem");
                if (IsCastableHelper(slg))
                {
                    _summonLightningGolemSlot = slg.Slot;
                    _sgSlot = _summonLightningGolemSlot;
                }

                var rz = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Raise Zombie");
                if (IsCastableHelper(rz))
                {
                    _raiseZombieSlot = rz.Slot;
                }
                
                var rs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Raise Spectre");
                if (IsCastableHelper(rs))
                {
                    _raiseSpectreSlot = rs.Slot;
                }
                

                _needsUpdate = false;
            }
        }

        /// <summary> The routine stop callback. Do any pre-dispose cleanup here. </summary>
        public void Stop()
        {
            Log.DebugFormat("[PKSRSRoutine] Stop");
        }

        #endregion

        #region Implementation of IConfigurable

        /// <summary> The bot's settings control. This will be added to the Exilebuddy Settings tab.</summary>
        public UserControl Control
        {
            get
            {

                using (
                    var fs = new FileStream(Path.Combine(ThirdPartyLoader.GetInstance("PKSRSRoutine").ContentPath, "SettingsGui.xaml"),
                        FileMode.Open))
                {
                    var root = (UserControl)XamlReader.Load(fs);

                    // Your settings binding here.

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "SkipShrinesCheckBox", "SkipShrines",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'SkipShrinesCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "LeaveFrameCheckBox", "LeaveFrame",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'LeaveFrameCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "EnableAurasFromItemsCheckBox", "EnableAurasFromItems",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'EnableAurasFromItemsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "AlwaysAttackInPlaceCheckBox", "AlwaysAttackInPlace",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'AlwaysAttackInPlaceCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "DebugAurasCheckBox", "DebugAuras",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'DebugAurasCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "AutoCastVaalSkillsCheckBox", "AutoCastVaalSkills",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'AutoCastVaalSkillsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "SingleTargetMeleeSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'SingleTargetMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "SingleTargetMeleeSlotComboBox",
                            "SingleTargetMeleeSlot", BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'SingleTargetMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "SingleTargetRangedSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'SingleTargetRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "SingleTargetRangedSlotComboBox",
                            "SingleTargetRangedSlot", BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'SingleTargetRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "AoeMeleeSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'AoeMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "AoeMeleeSlotComboBox",
                            "AoeMeleeSlot", BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'AoeMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "AoeRangedSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'AoeRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "AoeRangedSlotComboBox",
                            "AoeRangedSlot", BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'AoeRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "FallbackSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'FallbackSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "FallbackSlotComboBox",
                            "FallbackSlot", BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'FallbackSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "CombatRangeTextBox", "CombatRange",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'CombatRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MaxMeleeRangeTextBox", "MaxMeleeRange",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxMeleeRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MaxRangeRangeTextBox", "MaxRangeRange",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxRangeRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MaxFlameBlastChargesTextBox", "MaxFlameBlastCharges",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'MaxFlameBlastChargesTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MoltenShellDelayMsTextBox", "MoltenShellDelayMs",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MoltenShellDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "TotemDelayMsTextBox", "TotemDelayMs",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'TotemDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                         !Wpf.SetupTextBoxBinding(root, "BladeVortexCountTextBox", "BladeVortexCount",
                         BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'BladeVortexCountTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupTextBoxBinding(root, "SummonRagingSpiritCountPerDelayTextBox",
                            "SummonRagingSpiritCountPerDelay",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonRagingSpiritCountPerDelayTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "SummonRagingSpiritDelayMsTextBox", "SummonRagingSpiritDelayMs",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonRagingSpiritDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupTextBoxBinding(root, "SummonSkeletonCountPerDelayTextBox",
                            "SummonSkeletonCountPerDelay",
                            BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonSkeletonCountPerDelayTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "SummonSkeletonDelayMsTextBox", "SummonSkeletonDelayMs",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonSkeletonDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MineDelayMsTextBox", "MineDelayMs",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'MineDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "BlacklistedSkillIdsTextBox", "BlacklistedSkillIds",
                        BindingMode.TwoWay, PKSRSRoutineSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'BlacklistedSkillIdsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    // Your settings event handlers here.

                    return root;
                }
            }
        }

        /// <summary>The settings object. This will be registered in the current configuration.</summary>
        public JsonSettings Settings
        {
            get
            {
                return PKSRSRoutineSettings.Instance;
            }
        }

        #endregion

        #region Implementation of ILogic

        public async Task<bool> TryUseAura(Skill skill)
        {
            var doCast = true;

            while (skill.Slot == -1)
            {
                Log.InfoFormat("[TryUseAura] Now assigning {0} to the skillbar.", skill.Name);

                var sserr = LokiPoe.InGameState.SkillBarHud.SetSlot(_auraSlot, skill);

                if (sserr != LokiPoe.InGameState.SetSlotResult.None)
                {
                    Log.ErrorFormat("[TryUseAura] SetSlot returned {0}.", sserr);

                    doCast = false;

                    break;
                }

                await Coroutines.LatencyWait();

                await Coroutine.Sleep(1000);
            }

            if (!doCast)
            {
                return false;
            }

            await Coroutines.FinishCurrentAction();

            await Coroutines.LatencyWait();
            //Probably The error is happening here
            var err1 = LokiPoe.InGameState.SkillBarHud.Use(skill.Slot, false);
            if (err1 == LokiPoe.InGameState.UseResult.None)
            {
                await Coroutines.LatencyWait();

                await Coroutines.FinishCurrentAction(false);

                await Coroutines.LatencyWait();

                return true;
            }

            Log.ErrorFormat("[TryUseAura] Use returned {0} for {1}.", err1, skill.Name);

            return false;
        }

        private TimeSpan EnduranceChargesTimeLeft
        {
            get
            {
                Aura aura = LokiPoe.Me.EnduranceChargeAura;
                if (aura != null)
                {
                    return aura.TimeLeft;
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Coroutine logic to execute.
        /// </summary>
        /// <param name="type">The requested type of logic to execute.</param>
        /// <param name="param">Data sent to the object from the caller.</param>
        /// <returns>true if logic was executed to handle this type and false otherwise.</returns>
        public async Task<bool> Logic(string type, params dynamic[] param)
        {
            if (type == "core_area_changed_event")
            {
                var oldSeed = (uint)param[0];
                var newSeed = (uint)param[1];
                var oldArea = (DatWorldAreaWrapper)param[2];
                var newArea = (DatWorldAreaWrapper)param[3];
                
                _shrineTries.Clear();

                return true;
            }

            if (type == "core_player_died_event")
            {
                var totalDeathsForInstance = (int)param[0];

                return true;
            }

            if (type == "core_player_leveled_event")
            {
                Log.InfoFormat("[Logic] We are now level {0}!", (int)param[0]);
                return true;
            }

            if (type == "combat")
            {
                // Update targeting.
                CombatTargeting.Update();

                // We now signal always highlight needs to be disabled, but won't actually do it until we cast something.
                if (
                    LokiPoe.ObjectManager.GetObjectsByType<Chest>()
                        .Any(c => c.Distance < 70 && !c.IsOpened && c.IsStrongBox))
                {
                    _needsToDisableAlwaysHighlight = true;
                }
                else
                {
                    _needsToDisableAlwaysHighlight = false;
                }

                var myPos = LokiPoe.MyPosition;

                // If we have flameblast, we need to use special logic for it. - Disabled for SRSRoutine
                
                //Also removing Code for AnimateGuardian/Weapon
                
                

                // If we have Raise Spectre, we can look for dead bodies to use for our army as we move around.
                if (_raiseSpectreSlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_raiseSpectreSlot);
                    if (skill.CanUse())
                    {
                        var max = skill.GetStat(StatTypeGGG.NumberOfSpectresAllowed);
                        if (skill.NumberDeployed < max)
                        {
                            // Check for a target near us.
                            var target = BestDeadTarget;
                            if (target != null)
                            {
                                await DisableAlwaysHiglight();

                                await Coroutines.FinishCurrentAction();

                                Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                                var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseSpectreSlot, false,
                                    target.Position);
                                if (uaerr == LokiPoe.InGameState.UseResult.None)
                                {
                                    await Coroutines.LatencyWait();

                                    await Coroutines.FinishCurrentAction(false);

                                    await Coroutines.LatencyWait();

                                    return true;
                                }

                                Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", uaerr, skill.Name);
                            }
                        }
                    }
                }

                // If we have a Summon Golem skill, we can cast it if we havn't cast it recently.
                if (_sgSlot != -1 &&
                    _summonGolemStopwatch.ElapsedMilliseconds >
                    10000)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_sgSlot);
                    if (skill.CanUse())
                    {
                        var max = skill.GetStat(StatTypeGGG.NumberOfGolemsAllowed);
                        if (skill.NumberDeployed < max)
                        {
                            await DisableAlwaysHiglight();

                            await Coroutines.FinishCurrentAction();

                            var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_sgSlot, true, myPos);
                            if (err1 == LokiPoe.InGameState.UseResult.None)
                            {
                                _summonGolemStopwatch.Restart();

                                await Coroutines.FinishCurrentAction(false);

                                return true;
                            }

                            Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                        }
                    }
                }

                // If we have Raise Zombie, we can look for dead bodies to use for our army as we move around.
                if (_raiseZombieSlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_raiseZombieSlot);
                    if (skill.CanUse())
                    {
                        var max = skill.GetStat(StatTypeGGG.NumberOfZombiesAllowed);
                        if (skill.NumberDeployed < max)
                        {
                            // Check for a target near us.
                            var target = BestDeadTarget;
                            if (target != null)
                            {
                                await DisableAlwaysHiglight();

                                await Coroutines.FinishCurrentAction();

                                Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                                var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseZombieSlot, false,
                                    target.Position);
                                if (uaerr == LokiPoe.InGameState.UseResult.None)
                                {
                                    await Coroutines.LatencyWait();

                                    await Coroutines.FinishCurrentAction(false);

                                    await Coroutines.LatencyWait();

                                    return true;
                                }

                                Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", uaerr, skill.Name);
                            }
                        }
                    }
                }

                // Since EC has a cooldown, we can just cast it when mobs are in range to keep our endurance charges refreshed.
                if (_enduringCrySlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_enduringCrySlot);
                    if (skill.CanUse())
                    {
                        if (EnduranceChargesTimeLeft.TotalSeconds < 5 && NumberOfMobsNear(LokiPoe.Me, 30) > 0)
                        {
                            await Coroutines.FinishCurrentAction();

                            var err1 = LokiPoe.InGameState.SkillBarHud.Use(_enduringCrySlot, true);
                            if (err1 == LokiPoe.InGameState.UseResult.None)
                            {
                                await Coroutines.LatencyWait();

                                await Coroutines.FinishCurrentAction(false);

                                await Coroutines.LatencyWait();

                                return true;
                            }

                            Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                        }
                    }
                }

                
                // Handle aura logic.
                if (_auraSlot != -1)
                {
                    foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
                    {
                        if (IsBlacklistedSkill(skill.Id))
                            continue;

                        if (skill.IsAurifiedCurse)
                        {
                            if (!skill.AmICursingWithThis && skill.CanUse(PKSRSRoutineSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                        else if (skill.IsConsideredAura)
                        {
                            if (!skill.AmIUsingConsideredAuraWithThis && skill.CanUse(PKSRSRoutineSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                        else if ((skill.SkillTags.Contains("aura") && !skill.SkillTags.Contains("vaal")) || IsAuraName(skill.Name))
                        {
                            if (!LokiPoe.Me.HasAura(skill.Name) && skill.CanUse(PKSRSRoutineSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                

                // TODO: _currentLeashRange of -1 means we need to use a cached location system to prevent back and forth issues of mobs despawning.

                // This is pretty important. Otherwise, components can go invalid and exceptions are thrown.
                var bestTarget = CombatTargeting.Targets<Monster>().FirstOrDefault();

                // No monsters, we can execute non-critical combat logic, like buffs, auras, etc...
                // For this example, just going to continue executing bot logic.
                if (bestTarget == null)
                {
                    if (await HandleShrines())
                    {
                        return true;
                    }

                    return await CombatLogicEnd();
                }

                var cachedPosition = bestTarget.Position;
                var targetPosition = bestTarget.InteractCenterWorld;
                var cachedId = bestTarget.Id;
                var cachedName = bestTarget.Name;
                var cachedRarity = bestTarget.Rarity;
                var cachedDistance = bestTarget.Distance;
                var cachedIsCursable = bestTarget.IsCursable;
                var cachedCurseCount = bestTarget.CurseCount;
                var cachedHasCurseFrom = new Dictionary<string, bool>();
                var cachedNumberOfMobsNear = NumberOfMobsNear(bestTarget, 20);
                var cachedProxShield = bestTarget.HasProximityShield;
                var cachedMobsNearForAoe = NumberOfMobsNear(LokiPoe.Me,
                    PKSRSRoutineSettings.Instance.MaxMeleeRange);
                var cachedMobsNearForCurse = NumberOfMobsNear(bestTarget, 20);

                foreach (var curseSlot in _curseSlots)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(curseSlot);
                    cachedHasCurseFrom.Add(skill.Name, bestTarget.HasCurseFrom(skill.Name));
                }

                if (await HandleShrines())
                {
                    return true;
                }

                var canSee = ExilePather.CanObjectSee(LokiPoe.Me, bestTarget, !PKSRSRoutineSettings.Instance.LeaveFrame);
                var pathDistance = ExilePather.PathDistance(myPos, cachedPosition, false, !PKSRSRoutineSettings.Instance.LeaveFrame);
                var blockedByDoor = ClosedDoorBetween(LokiPoe.Me, bestTarget, 10, 10,
                    !PKSRSRoutineSettings.Instance.LeaveFrame);

                if (pathDistance.CompareTo(float.MaxValue) == 0)
                {
                    Log.ErrorFormat(
                        "[Logic] Could not determine the path distance to the best target. Now blacklisting it.");
                    Blacklist.Add(cachedId, TimeSpan.FromMinutes(1), "Unable to pathfind to.");
                    return true;
                }

                // Prevent combat loops from happening by preventing combat outside CombatRange.
                if (pathDistance > PKSRSRoutineSettings.Instance.CombatRange)
                {
                    await EnableAlwaysHiglight();

                    return false;
                }

                if (!canSee || blockedByDoor)
                {
                    Log.InfoFormat(
                        "[Logic] Now moving towards the monster {0} because [canSee: {1}][pathDistance: {2}][blockedByDoor: {3}]",
                        cachedName, canSee, pathDistance, blockedByDoor);

                    if (!PlayerMover.MoveTowards(cachedPosition))
                    {
                        Log.ErrorFormat("[Logic] MoveTowards failed for {0}.", cachedPosition);
                        await Coroutines.FinishCurrentAction();
                    }

                    return true;
                }

                // Handle totem logic.
                if (_totemSlot != -1 &&
                    _totemStopwatch.ElapsedMilliseconds > PKSRSRoutineSettings.Instance.TotemDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_totemSlot);
                    if (skill.CanUse() &&
                        skill.DeployedObjects.Select(o => o as Monster).Count(t => !t.IsDead && t.Distance < 60) <
                        LokiPoe.Me.MaxTotemCount)
                    {
                        await DisableAlwaysHiglight();

                        await Coroutines.FinishCurrentAction();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_totemSlot, true,
                            myPos.GetPointAtDistanceAfterThis(cachedPosition,
                                cachedDistance / 2));

                        _totemStopwatch.Restart();

                        if (err1 == LokiPoe.InGameState.UseResult.None)
                            return true;

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Handle trap logic.
                

                // Handle curse logic - curse magic+ and packs of 4+, but only cast within MaxRangeRange.
                var checkCurses = myPos.Distance(cachedPosition) < PKSRSRoutineSettings.Instance.MaxRangeRange &&
                                (cachedRarity >= Rarity.Magic || cachedMobsNearForCurse >= 3);
                if (checkCurses)
                {
                    foreach (var curseSlot in _curseSlots)
                    {
                        var skill = LokiPoe.InGameState.SkillBarHud.Slot(curseSlot);
                        if (skill.CanUse() &&
                            cachedIsCursable &&
                            cachedCurseCount < _totalCursesAllowed &&
                            !cachedHasCurseFrom[skill.Name])
                        {
                            await DisableAlwaysHiglight();

                            await Coroutines.FinishCurrentAction();

                            var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(curseSlot, true, cachedPosition);
                            if (err1 == LokiPoe.InGameState.UseResult.None)
                                return true;

                            Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                        }
                    }
                }

                // Simply cast Blood Rage if we have it.
                if (_bloodRageSlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_bloodRageSlot);
                    if (skill.CanUse() && !LokiPoe.Me.HasBloodRageBuff && cachedDistance < PKSRSRoutineSettings.Instance.CombatRange)
                    {
                        await Coroutines.FinishCurrentAction();

                        var err1 = LokiPoe.InGameState.SkillBarHud.Use(_bloodRageSlot, true);
                        if (err1 == LokiPoe.InGameState.UseResult.None)
                        {
                            await Coroutines.LatencyWait();

                            await Coroutines.FinishCurrentAction(false);

                            await Coroutines.LatencyWait();

                            return true;
                        }

                        Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Simply cast RF if we have it.
                if (_rfSlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_rfSlot);
                    if (skill.CanUse() && !LokiPoe.Me.HasRighteousFireBuff)
                    {
                        await Coroutines.FinishCurrentAction();

                        var err1 = LokiPoe.InGameState.SkillBarHud.Use(_rfSlot, true);
                        if (err1 == LokiPoe.InGameState.UseResult.None)
                        {
                            await Coroutines.LatencyWait();

                            await Coroutines.FinishCurrentAction(false);

                            await Coroutines.LatencyWait();

                            return true;
                        }
                    }
                }

                //Debug this shit
                if (_summonRagingSpiritSlot != -1 &&
                    _summonRagingSpiritStopwatch.ElapsedMilliseconds >
                    PKSRSRoutineSettings.Instance.SummonRagingSpiritDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonRagingSpiritSlot);
                    var max = skill.GetStat(StatTypeGGG.NumberOfRagingSpiritsAllowed);
                    if (skill.NumberDeployed < max && skill.CanUse())
                    {
                        ++_summonRagingSpiritCount;

                        await Coroutines.FinishCurrentAction();

                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        var err1 = LokiPoe.InGameState.SkillBarHud.BeginUseAt(_summonRagingSpiritSlot, false, targetPosition);

                        if (_summonRagingSpiritCount >=
                            PKSRSRoutineSettings.Instance.SummonRagingSpiritCountPerDelay)
                        {
                            _summonRagingSpiritCount = 0;
                            _summonRagingSpiritStopwatch.Restart();
                        }

                        if (err1 == LokiPoe.InGameState.UseResult.None)
                        {
                            await Coroutines.LatencyWait();

                            await Coroutines.FinishCurrentAction(false);

                            await Coroutines.LatencyWait();

                            return true;
                        }

                        Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                    }
                }
                

                // Auto-cast any vaal skill at the best target as soon as it's usable.
                if (PKSRSRoutineSettings.Instance.AutoCastVaalSkills && _vaalStopwatch.ElapsedMilliseconds > 1000)
                {
                    foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
                    {
                        if (skill.SkillTags.Contains("vaal"))
                        {
                            if (skill.CanUse())
                            {
                                await DisableAlwaysHiglight();

                                await Coroutines.FinishCurrentAction();

                                var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(skill.Slot, false, cachedPosition);
                                if (err1 == LokiPoe.InGameState.UseResult.None)
                                {
                                    await Coroutines.LatencyWait();

                                    await Coroutines.FinishCurrentAction(false);

                                    await Coroutines.LatencyWait();

                                    return true;
                                }

                                Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                            }
                        }
                    }
                    _vaalStopwatch.Restart();
                }

                var aip = false;

                var aoe = false;
                var melee = false;

                int slot = -1;

                // Logic for figuring out if we should use an AoE skill or single target.
                if (cachedNumberOfMobsNear > 2 && cachedRarity < Rarity.Rare)
                {
                    aoe = true;
                }

                // Logic for figuring out if we should use an AoE skill instead.
                if (myPos.Distance(cachedPosition) < PKSRSRoutineSettings.Instance.MaxMeleeRange)
                {
                    melee = true;
                    if (cachedMobsNearForAoe >= 3)
                    {
                        aoe = true;
                    }
                    else
                    {
                        aoe = false;
                    }
                }

                // This sillyness is for making sure we always use a skill, and is why generic code is a PITA
                // when it can be configured like so.
                if (aoe)
                {
                    if (melee)
                    {
                        slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeMeleeSlot);
                        if (slot == -1)
                        {
                            slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetMeleeSlot);
                            if (slot == -1)
                            {
                                melee = false;
                                slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeRangedSlot);
                                if (slot == -1)
                                {
                                    slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetRangedSlot);
                                }
                            }
                        }
                    }
                    else
                    {
                        slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeRangedSlot);
                        if (slot == -1)
                        {
                            slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetRangedSlot);
                            if (slot == -1)
                            {
                                melee = true;
                                slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeMeleeSlot);
                                if (slot == -1)
                                {
                                    slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetMeleeSlot);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (melee)
                    {
                        slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetMeleeSlot);
                        if (slot == -1)
                        {
                            slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeMeleeSlot);
                            if (slot == -1)
                            {
                                melee = false;
                                slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetRangedSlot);
                                if (slot == -1)
                                {
                                    slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeRangedSlot);
                                }
                            }
                        }
                    }
                    else
                    {
                        slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetRangedSlot);
                        if (slot == -1)
                        {
                            slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeRangedSlot);
                            if (slot == -1)
                            {
                                melee = true;
                                slot = EnsurceCast(PKSRSRoutineSettings.Instance.SingleTargetMeleeSlot);
                                if (slot == -1)
                                {
                                    slot = EnsurceCast(PKSRSRoutineSettings.Instance.AoeMeleeSlot);
                                }
                            }
                        }
                    }
                }

                if (PKSRSRoutineSettings.Instance.AlwaysAttackInPlace)
                    aip = true;

                if (slot == -1)
                {
                    slot = PKSRSRoutineSettings.Instance.FallbackSlot;
                    melee = true;
                }

                if (slot == -1)
                {
                    Log.ErrorFormat("[Logic] There is no slot configured to use.");
                    return true;
                }

                if (melee || cachedProxShield)
                {
                    var dist = LokiPoe.MyPosition.Distance(cachedPosition);
                    if (dist > PKSRSRoutineSettings.Instance.MaxMeleeRange)
                    {
                        Log.InfoFormat("[Logic] Now moving towards {0} because [dist ({1}) > MaxMeleeRange ({2})]",
                            cachedPosition, dist, PKSRSRoutineSettings.Instance.MaxMeleeRange);

                        if (!PlayerMover.MoveTowards(cachedPosition))
                        {
                            Log.ErrorFormat("[Logic] MoveTowards failed for {0}.", cachedPosition);
                            await Coroutines.FinishCurrentAction();
                        }
                        return true;
                    }
                }
                else
                {
                    var dist = LokiPoe.MyPosition.Distance(cachedPosition);
                    if (dist > PKSRSRoutineSettings.Instance.MaxRangeRange)
                    {
                        Log.InfoFormat("[Logic] Now moving towards {0} because [dist ({1}) > MaxRangeRange ({2})]",
                            cachedPosition, dist, PKSRSRoutineSettings.Instance.MaxRangeRange);

                        if (!PlayerMover.MoveTowards(cachedPosition))
                        {
                            Log.ErrorFormat("[Logic] MoveTowards failed for {0}.", cachedPosition);
                            await Coroutines.FinishCurrentAction();
                        }
                        return true;
                    }
                }

                await DisableAlwaysHiglight();

                var slotSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
                if (slotSkill == null)
                {
                    Log.ErrorFormat("[Logic] There is no skill in the slot configured to use.");
                    return true;
                }

                if (_isCasting && slot == _castingSlot && (LokiPoe.ProcessHookManager.GetKeyState(slotSkill.BoundKey) & 0x8000) != 0)
                {
                    var ck = LokiPoe.Me.CurrentAction.Skill;
                    if (LokiPoe.Me.HasCurrentAction && ck != null &&
                        !ck.InternalId.Equals("Interaction") &&
                        !ck.InternalId.Equals("Move"))
                    {
                        LokiPoe.Input.SetMousePos(cachedPosition, false);

                        return true;
                    }
                }

                await Coroutines.FinishCurrentAction();

                var err = LokiPoe.InGameState.SkillBarHud.BeginUseAt(slot, aip, cachedPosition);
                if (err != LokiPoe.InGameState.UseResult.None)
                {
                    Log.ErrorFormat("[Logic] UseAt returned {0}.", err);
                }

                _isCasting = true;
                _castingSlot = slot;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Non-coroutine logic to execute.
        /// </summary>
        /// <param name="name">The name of the logic to invoke.</param>
        /// <param name="param">The data passed to the logic.</param>
        /// <returns>Data from the executed logic.</returns>
        public object Execute(string name, params dynamic[] param)
        {
            Func<dynamic[], object> f;
            if (_exposedSettings.TryGetValue(name, out f))
            {
                return f(param);
            }

            if (name == "GetCombatTargeting")
            {
                return CombatTargeting;
            }

            if (name == "ResetCombatTargeting")
            {
                _combatTargeting.ResetInclusionCalcuation();
                _combatTargeting.ResetWeightCalculation();
                _combatTargeting.InclusionCalcuation += CombatTargetingOnInclusionCalcuation;
                _combatTargeting.WeightCalculation += CombatTargetingOnWeightCalculation;
                return null;
            }

            return null;
        }

        #endregion

        private Monster BestDeadTarget
        {
            get
            {
                var myPos = LokiPoe.MyPosition;
                return LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                    .Where(
                        m =>
                            m.Distance < 30 && m.IsActiveDead && m.Rarity != Rarity.Unique && m.CorpseUsable &&
                            ExilePather.PathDistance(myPos, m.Position, false, !PKSRSRoutineSettings.Instance.LeaveFrame) < 30)
                    .OrderBy(m => m.Distance).FirstOrDefault();
            }
        }

        private async Task<bool> CombatLogicEnd()
        {
            await EnableAlwaysHiglight();

            if (_isCasting)
            {
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                _isCasting = false;
                _castingSlot = -1;
            }

            return false;
        }

        private readonly Dictionary<int, int> _shrineTries = new Dictionary<int, int>();

        #region ShrineThings
        private async Task<bool> HandleShrines()
        {
            // If the user wants to avoid shrine logic due to stuck issues, simply return without doing anything.
            if (PKSRSRoutineSettings.Instance.SkipShrines)
                return false;

            // TODO: Shrines need speical CR logic, because it's now the CRs responsibility for handling all combaat situations,
            // and shrines are now considered a combat situation due their nature.

            // Check for any active shrines.
            var shrines =
                LokiPoe.ObjectManager.Objects.OfType<Shrine>()
                    .Where(s => !Blacklist.Contains(s.Id) && !s.IsDeactivated && s.Distance < 50)
                    .OrderBy(s => s.Distance)
                    .ToList();

            if (!shrines.Any())
                return false;

            Log.InfoFormat("[HandleShrines]");

            // For now, just take the first shrine found.

            var shrine = shrines[0];
            int tries;

            if (!_shrineTries.TryGetValue(shrine.Id, out tries))
            {
                tries = 0;
                _shrineTries.Add(shrine.Id, tries);
            }

            if (tries > 10)
            {
                Blacklist.Add(shrine.Id, TimeSpan.FromHours(1), "Could not interact with the shrine.");

                return true;
            }

            // Handle Skeletal Shrine in a special way, or handle priority between multiple shrines at the same time.
            var skellyOverride = shrine.ShrineId == "Skeletons";

            // Try and only move to touch it when we have a somewhat navigable path.
            if ((NumberOfMobsBetween(LokiPoe.Me, shrine, 5, true) < 5 &&
                NumberOfMobsNear(LokiPoe.Me, 20) < 3) || skellyOverride)
            {
                var myPos = LokiPoe.MyPosition;

                var pos = ExilePather.FastWalkablePositionFor(shrine);

                // We need to filter out based on pathfinding, since otherwise, a large gap will lockup the bot.
                var pathDistance = ExilePather.PathDistance(myPos, pos, false, !PKSRSRoutineSettings.Instance.LeaveFrame);

                Log.DebugFormat("[HandleShrines] Now moving towards the Shrine {0} [pathPos: {1} pathDis: {2}].", shrine.Id, pos,
                    pathDistance);

                if (pathDistance > 50)
                {
                    Log.DebugFormat("[HandleShrines] Not attempting to move towards Shrine [{0}] because the path distance is: {1}.",
                        shrine.Id, pathDistance);
                    return false;
                }

                //var canSee = ExilePather.CanObjectSee(LokiPoe.Me, pos, !PKSRSRoutineSettings.Instance.LeaveFrame);

                // We're in distance when we're sure we're close to the position, but also that the path we need to take to the position
                // isn't too much further. This prevents problems with things on higher ground when we are on lower, and vice-versa.
                var inDistance = myPos.Distance(pos) < 20 && pathDistance < 25;
                if (inDistance)
                {
                    Log.DebugFormat("[HandleShrines] Now attempting to interact with the Shrine {0}.", shrine.Id);

                    await Coroutines.FinishCurrentAction();

                    await Coroutines.InteractWith(shrine);

                    _shrineTries[shrine.Id]++;
                }
                else
                {
                    if (!PlayerMover.MoveTowards(pos))
                    {
                        Log.ErrorFormat("[HandleShrines] MoveTowards failed for {0}.", pos);

                        Blacklist.Add(shrine.Id, TimeSpan.FromHours(1), "Could not move towards the shrine.");

                        await Coroutines.FinishCurrentAction();
                    }
                }

                return true;
            }

            return false;
        }
        #endregion

        private bool _needsToDisableAlwaysHighlight;

        // This logic is now CR specific, because Strongbox gui labels interfere with targeting,
        // but not general movement using Move only.
        private async Task DisableAlwaysHiglight()
        {
            if (_needsToDisableAlwaysHighlight && LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
            {
                Log.InfoFormat("[DisableAlwaysHiglight] Now disabling Always Highlight to avoid skill use issues.");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
                await Coroutine.Sleep(16);
            }
        }

        // This logic is now CR specific, because Strongbox gui labels interfere with targeting,
        // but not general movement using Move only.
        private async Task EnableAlwaysHiglight()
        {
            if (!LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
            {
                Log.InfoFormat("[EnableAlwaysHiglight] Now enabling Always Highlight.");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
                await Coroutine.Sleep(16);
            }
        }

        private int EnsurceCast(int slot)
        {
            if (slot == -1)
                return slot;

            var slotSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
            if (slotSkill == null || !slotSkill.CanUse())
            {
                return -1;
            }

            return slot;
        }

        #region Override of Object

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name + ": " + Description;
        }

        #endregion

    }
}
