using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Buddy.Coroutines;
using log4net;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;
using Loki.Common;

namespace Ouroboros
{
    /// <summary> </summary>
    public class Ouroboros : IRoutine
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

        private int _totalCursesAllowed;

        // Auto-set, you do not have to change these.
        private int _sgSlot;
        private int _summonChaosGolemSlot = -1;
        private int _summonIceGolemSlot = -1;
        private int _summonFlameGolemSlot = -1;
        private int _raiseZombieSlot = -1;
        private int _raiseSpectreSlot = -1;
        private int _animateWeaponSlot = -1;
        private int _animateGuardianSlot = -1;
        private int _flameblastSlot = -1;
        private int _enduringCrySlot = -1;
        private int _moltenShellSlot = -1;
        private int _bloodRageSlot = -1;
        private int _rfSlot = -1;
        private readonly List<int> _curseSlots = new List<int>();
        private int _curseOnHitSlot = -1;
        private int _auraSlot = -1;
        private int _totemSlot = -1;
        private int _trapSlot = -1;
        private int _mineSlot = -1;
        private int _summonSkeletonsSlot = -1;
        private int _summonRagingSpiritSlot = -1;
        private int _coldSnapSlot = -1;
        private int _contagionSlot = -1;
        private int _witherSlot = -1;
        private int _desecrateSlot = -1;
        private int _fleshOfferingSlot = -1;
        private int _boneOfferingSlot = -1;
        private int _frenzySlot = -1;
        private int _causticArrowSlot = -1;

        private int _currentLeashRange = -1;

        private readonly Stopwatch _trapStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _totemStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _mineStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _animateWeaponStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _animateGuardianStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _moltenShellStopwatch = Stopwatch.StartNew();
        private readonly List<int> _ignoreAnimatedItems = new List<int>();
        private readonly Stopwatch _vaalStopwatch = Stopwatch.StartNew();
		
        private readonly List<string> _defensiveVaalSkills = new List<string>(new string[] {"Vaal Immortal Call","Vaal Grace","Vaal Discipline"});

        private int _summonSkeletonCount;
        private readonly Stopwatch _summonSkeletonsStopwatch = Stopwatch.StartNew();

        private readonly Stopwatch _summonGolemStopwatch = Stopwatch.StartNew();

        private int _summonRagingSpiritCount;
        private readonly Stopwatch _summonRagingSpiritStopwatch = Stopwatch.StartNew();

        private bool _castingFlameblast;
        private int _lastFlameblastCharges;
        private bool _needsUpdate;

        private readonly Targeting _combatTargeting = new Targeting();

        private Dictionary<string, Func<dynamic[], object>> _exposedSettings;

        // http://stackoverflow.com/a/824854
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

            PropertyInfo[] properties = typeof(OuroborosSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                    p.SetValue(OuroborosSettings.Instance, param[0]);
                    return null;
                });

                _exposedSettings.Add("Get" + p.Name, param =>
                {
                    return p.GetValue(OuroborosSettings.Instance);
                });
            }
        }

        private bool IsBlacklistedSkill(int id)
        {
            var tokens = OuroborosSettings.Instance.BlacklistedSkillIds.Split(new[]
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

        private Targeting CombatTargeting
        {
            get
            {
                return _combatTargeting;
            }
        }

        // Do not implement a ctor and do stuff in it.

        #region Targeting

        private void CombatTargetingOnWeightCalculation(NetworkObject entity, ref float weight)
        {
            weight -= entity.Distance / 2;

            var m = entity as Monster;
            if (m == null)
                return;

			// If we're using caustic arrow and they're already in a cloud, focus on targets that aren't.
            if (m.HasAura("caustic_cloud") && _causticArrowSlot != -1 && (_causticArrowSlot == OuroborosSettings.Instance.AoeRangedSlot || _causticArrowSlot == OuroborosSettings.Instance.SingleTargetRangedSlot))
                weight -= 50;
				
            // If the monster is the source of Allies Cannot Die, we really want to kill it fast.
            if (m.HasAura("monster_aura_cannot_die"))
                weight += 50;

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
                if (m.Distance > (_currentLeashRange != -1 ? OuroborosSettings.Instance.CombatRange : 300))
                    return false;

                // Ignore mobs with special aura/buffs
                if (m.HasAura(_aurasToIgnore))
                    return false;
				
				// Ignore animated weapons
				if (m.Type.Contains("AnimatedWeapon"))
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
				
				// If we're using caustic arrow and they're already in a cloud, ignore them.
				//if (m.HasAura("caustic_cloud") && _causticArrowSlot != -1 && (_causticArrowSlot == OuroborosSettings.Instance.AoeRangedSlot || _causticArrowSlot == OuroborosSettings.Instance.SingleTargetRangedSlot))
				//	return false;
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
            Log.DebugFormat("[Ouroboros] Initialize");

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
                return "Ouroboros";
            }
        }

        /// <summary>The description of the routine.</summary>
        public string Description
        {
            get
            {
                return "An extended version of OldRoutine for Exilebuddy.";
            }
        }

        /// <summary>
        /// The author of this object.
        /// </summary>
        public string Author
        {
            get
            {
                return "Bossland GmbH & Infinite Monkeys";
            }
        }

        /// <summary>
        /// The version of this routone.
        /// </summary>
        public string Version
        {
            get
            {
                return "1.7";
            }
        }

        #endregion

        #region Implementation of IRunnable

        /// <summary> The routine start callback. Do any initialization here. </summary>
        public void Start()
        {
            Log.DebugFormat("[Ouroboros] Start");

            _needsUpdate = true;

            if (OuroborosSettings.Instance.SingleTargetMeleeSlot == -1 &&
                OuroborosSettings.Instance.SingleTargetRangedSlot == -1 &&
                OuroborosSettings.Instance.AoeMeleeSlot == -1 &&
                OuroborosSettings.Instance.AoeRangedSlot == -1)
            {
                Log.ErrorFormat(
                    "[Start] Please configure the Ouroboros settings (Settings -> Ouroboros) before starting!");
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
            if (!OuroborosSettings.Instance.EnableAurasFromItems)
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
                _raiseZombieSlot = -1;
                _raiseSpectreSlot = -1;
                _animateWeaponSlot = -1;
                _animateGuardianSlot = -1;
                _flameblastSlot = -1;
                _enduringCrySlot = -1;
                _moltenShellSlot = -1;
                _auraSlot = -1;
                _totemSlot = -1;
                _trapSlot = -1;
                _coldSnapSlot = -1;
                _contagionSlot = -1;
                _witherSlot = -1;
                _desecrateSlot = -1;
                _fleshOfferingSlot = -1;
                _boneOfferingSlot = -1;
                _bloodRageSlot = -1;
                _rfSlot = -1;
                _summonSkeletonsSlot = -1;
                _summonRagingSpiritSlot = -1;
                _summonSkeletonCount = 0;
                _summonRagingSpiritCount = 0;
                _mineSlot = -1;
                _frenzySlot = -1;
                _causticArrowSlot = -1;
                _curseSlots.Clear();
                _curseOnHitSlot = -1;
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
                    } //By Fujiyama
					else if (skill.Name == "Essence Drain")
					{
						var slot = skill.Slot;
                        if (slot != -1 && skill.IsCastable && !skill.IsAurifiedCurse)
                        {
                            _curseSlots.Add(slot);
                        }
					}
					
                    if ((tags.Contains("aura") && !tags.Contains("vaal")) || IsAuraName(name) || skill.IsAurifiedCurse || skill.IsConsideredAura)
                    {
						if(_auraSlot == -1) { _auraSlot = skill.Slot; }
                    }
					else if (skill.IsCastable && skill.LinkedDisplayString.Contains("Curse On Hit") && !tags.Contains("curse")) { _curseOnHitSlot = skill.Slot; }

                    if (skill.IsTotem && _totemSlot == -1)
                    {
                        _totemSlot = skill.Slot;
                    }

                    if (skill.IsTrap && _trapSlot == -1)
                    {
                        _trapSlot = skill.Slot;
                    }

                    if (skill.IsMine && _mineSlot == -1)
                    {
                        _mineSlot = skill.Slot;
                    }
                }

                var cs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Cold Snap");
                if (IsCastableHelper(cs))
                {
                    _coldSnapSlot = cs.Slot;
                }

                var con = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Contagion");
                if (IsCastableHelper(con))
                {
                    _contagionSlot = con.Slot;
                }

                var wither = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Wither");
                if (IsCastableHelper(wither))
                {
                    _witherSlot = wither.Slot;
                }

                var fleshOffering = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Flesh Offering");
                if (IsCastableHelper(fleshOffering))
                {
                    _fleshOfferingSlot = fleshOffering.Slot;
                }

                var boneOffering = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Bone Offering");
                if (IsCastableHelper(boneOffering))
                {
                    _boneOfferingSlot = boneOffering.Slot;
                }

                var desecrate = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Desecrate");
                if (IsCastableHelper(desecrate))
                {
                    _desecrateSlot = desecrate.Slot;
                }

                var ss = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Skeletons");
                if (IsCastableHelper(ss))
                {
                    _summonSkeletonsSlot = ss.Slot;
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

                var mc = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Molten Shell");
                if (IsCastableHelper(mc))
                {
                    _moltenShellSlot = mc.Slot;
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

                var fb = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Flameblast");
                if (IsCastableHelper(fb))
                {
                    _flameblastSlot = fb.Slot;
                }

                var ag = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Animate Guardian");
                if (IsCastableHelper(ag))
                {
                    _animateGuardianSlot = ag.Slot;
                }

                var aw = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Animate Weapon");
                if (IsCastableHelper(aw))
                {
                    _animateWeaponSlot = aw.Slot;
                }
				
                var frenzy = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Frenzy");
                if (IsCastableHelper(frenzy))
                {
                    _frenzySlot = frenzy.Slot;
                }
				
                var causticArrow = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Caustic Arrow");
                if (IsCastableHelper(causticArrow))
                {
                    _causticArrowSlot = causticArrow.Slot;
                }

                _needsUpdate = false;
            }
        }

        /// <summary> The routine stop callback. Do any pre-dispose cleanup here. </summary>
        public void Stop()
        {
            Log.DebugFormat("[Ouroboros] Stop");
        }

        #endregion

        #region Implementation of IConfigurable

        /// <summary> The bot's settings control. This will be added to the Exilebuddy Settings tab.</summary>
        public UserControl Control
        {
            get
            {
                using (var fs = new FileStream(Path.Combine(ThirdPartyLoader.GetInstance("Ouroboros").ContentPath, "SettingsGui.xaml"), FileMode.Open))
                {
                    var root = (UserControl)XamlReader.Load(fs);

                    // Your settings binding here.

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "LeaveFrameCheckBox", "LeaveFrame",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'LeaveFrameCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "EnableAurasFromItemsCheckBox", "EnableAurasFromItems",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'EnableAurasFromItemsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "AlwaysAttackInPlaceCheckBox", "AlwaysAttackInPlace",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'AlwaysAttackInPlaceCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupCheckBoxBinding(root, "DebugAurasCheckBox", "DebugAuras",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'DebugAurasCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "SingleTargetMeleeSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'SingleTargetMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "SingleTargetMeleeSlotComboBox",
                            "SingleTargetMeleeSlot", BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'SingleTargetMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "SingleTargetRangedSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'SingleTargetRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "SingleTargetRangedSlotComboBox",
                            "SingleTargetRangedSlot", BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'SingleTargetRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "AoeMeleeSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'AoeMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "AoeMeleeSlotComboBox",
                            "AoeMeleeSlot", BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'AoeMeleeSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "AoeRangedSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'AoeRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "AoeRangedSlotComboBox",
                            "AoeRangedSlot", BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'AoeRangedSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxItemsBinding(root, "FallbackSlotComboBox", "AllSkillSlots",
                            BindingMode.OneWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxItemsBinding failed for 'FallbackSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupComboBoxSelectedItemBinding(root, "FallbackSlotComboBox",
                            "FallbackSlot", BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'FallbackSlotComboBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "CombatRangeTextBox", "CombatRange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'CombatRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MaxMeleeRangeTextBox", "MaxMeleeRange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxMeleeRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MaxRangeRangeTextBox", "MaxRangeRange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxRangeRangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    //AOE RANGES

                    if (!Wpf.SetupTextBoxBinding(root, "MeleeAOERangeTextBox", "MeleeAOERange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MeleeAOERangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "RangedAOERangeTextBox", "RangedAOERange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'RangedAOERangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "CurseAOERangeTextBox", "CurseAOERange",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'CurseAOERangeTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    //END AOE RANGES

                    if (!Wpf.SetupTextBoxBinding(root, "MaxFlameBlastChargesTextBox", "MaxFlameBlastCharges",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'MaxFlameBlastChargesTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MoltenShellDelayMsTextBox", "MoltenShellDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MoltenShellDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "TotemDelayMsTextBox", "TotemDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'TotemDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "TrapDelayMsTextBox", "TrapDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'TrapDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupTextBoxBinding(root, "SummonRagingSpiritCountPerDelayTextBox",
                            "SummonRagingSpiritCountPerDelay",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonRagingSpiritCountPerDelayTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "SummonRagingSpiritDelayMsTextBox", "SummonRagingSpiritDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonRagingSpiritDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (
                        !Wpf.SetupTextBoxBinding(root, "SummonSkeletonCountPerDelayTextBox",
                            "SummonSkeletonCountPerDelay",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonSkeletonCountPerDelayTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "SummonSkeletonDelayMsTextBox", "SummonSkeletonDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'SummonSkeletonDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MineDelayMsTextBox", "MineDelayMs",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'MineDelayMsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "BlacklistedSkillIdsTextBox", "BlacklistedSkillIds",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'BlacklistedSkillIdsTextBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    //NUMBER OF MOBS TO AOE

                    if (!Wpf.SetupTextBoxBinding(root, "NormalMobsToAOETextBox", "NormalMobsToAOE",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'NormalMobsToAOE'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "MagicMobsToAOETextBox", "MagicMobsToAOE",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'MagicMobsToAOE'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "RareMobsToAOETextBox", "RareMobsToAOE",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'RareMobsToAOE'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupTextBoxBinding(root, "UniqueMobsToAOETextBox", "UniqueMobsToAOE",
                        BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupTextBoxBinding failed for 'UniqueMobsToAOE'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    //END NUMBER OF MOBS TO AOE

                    //VAAL SKILLS

                    if (!Wpf.SetupCheckBoxBinding(root, "VaalNormalMobsCheckBox", "VaalNormalMobs",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'VaalNormalMobsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupCheckBoxBinding(root, "VaalMagicMobsCheckBox", "VaalMagicMobs",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'VaalMagicMobsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupCheckBoxBinding(root, "VaalRareMobsCheckBox", "VaalRareMobs",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'VaalRareMobsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    if (!Wpf.SetupCheckBoxBinding(root, "VaalUniqueMobsCheckBox", "VaalUniqueMobs",
                            BindingMode.TwoWay, OuroborosSettings.Instance))
                    {
                        Log.DebugFormat(
                            "[SettingsControl] SetupCheckBoxBinding failed for 'VaalUniqueMobsCheckBox'.");
                        throw new Exception("The SettingsControl could not be created.");
                    }

                    //END VAAL SKILLS

                    return root;
                }
            }
        }

        /// <summary>The settings object. This will be registered in the current configuration.</summary>
        public JsonSettings Settings
        {
            get
            {
                return OuroborosSettings.Instance;
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

                await Coroutines.ReactionWait();
            }

            if (!doCast)
            {
                return false;
            }

            await Coroutines.FinishCurrentAction();

            await Coroutines.LatencyWait();

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

                _ignoreAnimatedItems.Clear();

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

				/*/Here be daemons. work in progress
				var daemon = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(m => m.Name == "Daemon" && m.Distance < 50).OrderBy(m => m.Distance).FirstOrDefault();
				if (daemon != null)
				{
					int directionsToTry = 8;
				
					//Cycle through some dodge directions to find the best
					Vector2i bestPos = new Vector2i(0,0);
					var bestDistance = 0;
					var bestMobsNearby = 1000;
					for (int i = 0; i < directionsToTry; i++)
					{
						//Get a position 30 from us in the right direction
						var angle = i*360/directionsToTry;
						Vector2i pos = ExilePather.WalkablePositionFor(new Vector2i(myPos.X + Convert.ToInt32(30*Math.Sin(angle)), myPos.Y + Convert.ToInt32(30*Math.Cos(angle))), 40);
						
						//See how far this position is from the nearest daemon and how many mobs are near it
						var nearbyDaemon = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(m => m.Name == "Daemon").OrderBy(m => m.Position.Distance(pos)).FirstOrDefault();
						var distance = 0;
						if(nearbyDaemon == null) { distance = OuroborosSettings.Instance.CombatRange; }
						else { distance = nearbyDaemon.Position.Distance(pos); }
						var mobsNearby = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(m => m.Position.Distance(pos) < 30).Count();
						
						//If it's further than our current best effort or far enough from daemons and has less mobs nearby, it's our new best
						if ((bestDistance < 50 && distance > bestDistance) || (distance > 50 && mobsNearby < bestMobsNearby))
						{
							bestPos = pos;
							bestDistance = distance;
							bestMobsNearby = mobsNearby;
						}
					}
					
					if(bestPos != new Vector2i(0,0))
					{
						PlayerMover.MoveTowards(bestPos);
					}
				}
				*/
				
                var myPos = LokiPoe.MyPosition;
				
                // If we have flameblast, we need to use special logic for it.
                if (_flameblastSlot != -1)
                {
                    if (_castingFlameblast)
                    {
                        var c = LokiPoe.Me.FlameblastCharges;

                        // Handle being cast interrupted.
                        if (c < _lastFlameblastCharges)
                        {
                            _castingFlameblast = false;
                            return true;
                        }
                        _lastFlameblastCharges = c;

                        if (c >= OuroborosSettings.Instance.MaxFlameBlastCharges)
                        {
                            // Stop using the skill, so it's cast.
                            await Coroutines.FinishCurrentAction();

                            _castingFlameblast = false;
                        }
                        else
                        {
                            await DisableAlwaysHiglight();

                            // Keep using the skill to build up charges.
                            var buaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_flameblastSlot, false,
                                myPos);
                            if (buaerr != LokiPoe.InGameState.UseResult.None)
                            {
                                Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", buaerr, "Flameblast");
                            }
                        }

                        return true;
                    }
                }

                // Limit this logic once per second, because it can get expensive and slow things down if run too fast.
                if (_animateGuardianSlot != -1 && _animateGuardianStopwatch.ElapsedMilliseconds > 1000)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_animateGuardianSlot);
                    if (skill.CanUse())
                    {
                        // Check for a target near us.
                        var target = BestAnimateGuardianTarget(skill.DeployedObjects.FirstOrDefault() as Monster,
                            skill.GetStat(StatTypeGGG.AnimateItemMaximumLevelRequirement));
                        if (target != null)
                        {
                            await DisableAlwaysHiglight();

                            Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                            var uaerr = LokiPoe.InGameState.SkillBarHud.UseOn(_animateGuardianSlot, true, target);
                            if (uaerr == LokiPoe.InGameState.UseResult.None)
                            {
                                // We need to remove the item highlighting.
                                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                                return true;
                            }

                            Log.ErrorFormat("[Logic] UseOn returned {0} for {1}.", uaerr, skill.Name);
                        }

                        _animateGuardianStopwatch.Restart();
                    }
                }

                // Limit this logic once per second, because it can get expensive and slow things down if run too fast.
                if (_animateWeaponSlot != -1 && _animateWeaponStopwatch.ElapsedMilliseconds > 1000)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_animateWeaponSlot);
                    if (skill.CanUse())
                    {
                        // Check for a target near us.
                        var target = BestAnimateWeaponTarget(skill.GetStat(StatTypeGGG.AnimateItemMaximumLevelRequirement));
                        if (target != null)
                        {
                            await DisableAlwaysHiglight();

                            Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                            var uaerr = LokiPoe.InGameState.SkillBarHud.UseOn(_animateWeaponSlot, true, target);
                            if (uaerr == LokiPoe.InGameState.UseResult.None)
                            {
								// We need to remove the item highlighting.
                                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                                _animateWeaponStopwatch.Restart();

                                return true;
                            }

                            Log.ErrorFormat("[Logic] UseOn returned {0} for {1}.", uaerr, skill.Name);
                        }

                        _animateWeaponStopwatch.Restart();
                    }
                }

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

                                Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                                var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseSpectreSlot, false,
                                    target.Position);
                                if (uaerr == LokiPoe.InGameState.UseResult.None) { return true; }

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

                            var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_sgSlot, true, myPos);
                            if (err1 == LokiPoe.InGameState.UseResult.None)
                            {
                                _summonGolemStopwatch.Restart();
								
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

                                Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

                                var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseZombieSlot, false,
                                    target.Position);
                                if (uaerr == LokiPoe.InGameState.UseResult.None) { return true; }

                                Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", uaerr, skill.Name);
                            }
							else if(_desecrateSlot != -1) //No bodies but we have Desecrate, so make some if it's usable.
							{
								var desecrate = LokiPoe.InGameState.SkillBarHud.Slot(_desecrateSlot);
								if(desecrate.CanUse())
								{
									await DisableAlwaysHiglight();

									Log.InfoFormat("[Logic] Using Desecrate so that we can raise more zombies.");

									var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_desecrateSlot, true,
										LokiPoe.MyPosition);
									if (uaerr == LokiPoe.InGameState.UseResult.None) { return true; }

									Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", uaerr, skill.Name);
								}
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
                            var err1 = LokiPoe.InGameState.SkillBarHud.Use(_enduringCrySlot, true);
                            if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                            Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                        }
                    }
                }

                // For Molten Shell, we want to limit cast time, since mobs that break the shield often would cause the CR to cast it over and over.
                if (_moltenShellSlot != -1 &&
                    _moltenShellStopwatch.ElapsedMilliseconds >= OuroborosSettings.Instance.MoltenShellDelayMs)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_moltenShellSlot);
                    if (!LokiPoe.Me.HasMoltenShellBuff && skill.CanUse())
                    {
                        if (NumberOfMobsNear(LokiPoe.Me, OuroborosSettings.Instance.CombatRange) > 0)
                        {
                            var err1 = LokiPoe.InGameState.SkillBarHud.Use(_moltenShellSlot, true);

                            _moltenShellStopwatch.Restart();

                            if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

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
                            if (!skill.AmICursingWithThis && skill.CanUse(OuroborosSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                        else if (skill.IsConsideredAura)
                        {
                            if (!skill.AmIUsingConsideredAuraWithThis && skill.CanUse(OuroborosSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                        else if ((skill.SkillTags.Contains("aura") && !skill.SkillTags.Contains("vaal")) || IsAuraName(skill.Name))
                        {
                            if (!LokiPoe.Me.HasAura(skill.Name) && skill.CanUse(OuroborosSettings.Instance.DebugAuras, true))
                            {
                                if (await TryUseAura(skill))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
				
                // Check for a surround to use flameblast, just example logic.
                if (_flameblastSlot != -1)
                {
                    if (NumberOfMobsNear(LokiPoe.Me, 15) >= 4)
                    {
                        _castingFlameblast = true;
                        _lastFlameblastCharges = 0;
                        return true;
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
                var cachedNumberOfMobsNear = NumberOfMobsNear(bestTarget, OuroborosSettings.Instance.RangedAOERange);
                var cachedProxShield = bestTarget.HasProximityShield;
                var cachedContagion = bestTarget.HasContagion;
                var cachedWither = bestTarget.HasWither;
                var cachedMobsNearForAoe = NumberOfMobsNear(LokiPoe.Me, OuroborosSettings.Instance.MeleeAOERange);
                var cachedMobsNearForCurse = NumberOfMobsNear(bestTarget, OuroborosSettings.Instance.CurseAOERange);
				//By Fujiyama
                var cachedED = bestTarget.HasAura("Essence Drain");

                foreach (var curseSlot in _curseSlots)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(curseSlot);
                    cachedHasCurseFrom.Add(skill.Name, bestTarget.HasCurseFrom(skill.Name));
                }

                if (await HandleShrines())
                {
                    return true;
                }

				//Flesh and bone offerings. Don't bother using if there's no nearby target, which is why it goes here.
				var offeringSlot = -1;
				var summon = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(m => m.Reaction == Reaction.Friendly).OrderBy(m => m.Distance).FirstOrDefault();
				if (_fleshOfferingSlot != -1 && summon != null && !summon.HasAura("offering_offensive")) { offeringSlot = _fleshOfferingSlot; }
				else if (_boneOfferingSlot != -1 && summon != null && !summon.HasAura("offering_defensive")) { offeringSlot = _boneOfferingSlot; }
				
				if (offeringSlot != -1)
				{
					// Check for a corpse near us.
					var target = BestDeadTarget;
					if (target != null)
					{
						await DisableAlwaysHiglight();
						
						var skill = LokiPoe.InGameState.SkillBarHud.Slot(offeringSlot);

						Log.InfoFormat("[Logic] Using {0} on {1}.", skill.Name, target.Name);

						var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(offeringSlot, false,
							target.Position);
						if (uaerr == LokiPoe.InGameState.UseResult.None) { return true; }

						Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", uaerr, skill.Name);
					}
				}

                var canSee = ExilePather.CanObjectSee(LokiPoe.Me, bestTarget, !OuroborosSettings.Instance.LeaveFrame);
                var pathDistance = ExilePather.PathDistance(myPos, cachedPosition, true, !OuroborosSettings.Instance.LeaveFrame);
                var blockedByDoor = ClosedDoorBetween(LokiPoe.Me, bestTarget, 10, 10,
                    !OuroborosSettings.Instance.LeaveFrame);

                if (pathDistance.CompareTo(float.MaxValue) == 0)
                {
                    Log.ErrorFormat(
                        "[Logic] Could not determine the path distance to the best target. Now blacklisting it.");
                    Blacklist.Add(cachedId, TimeSpan.FromMinutes(1), "Unable to pathfind to.");
                    return true;
                }

                // Prevent combat loops from happening by preventing combat outside CombatRange.
                if (pathDistance > OuroborosSettings.Instance.CombatRange)
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
                    }

                    return true;
                }

                // Handle totem logic.
                if (_totemSlot != -1 &&
                    _totemStopwatch.ElapsedMilliseconds > OuroborosSettings.Instance.TotemDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_totemSlot);
                    if (skill.CanUse() &&
                        skill.DeployedObjects.Select(o => o as Monster).Count(t => !t.IsDead && t.Distance < 60) <
                        LokiPoe.Me.MaxTotemCount)
                    {
                        await DisableAlwaysHiglight();

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
                if (_trapSlot != -1 &&
                    _trapStopwatch.ElapsedMilliseconds > OuroborosSettings.Instance.TrapDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_trapSlot);
                    if (skill.CanUse())
                    {
                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_trapSlot, true,
                            myPos.GetPointAtDistanceAfterThis(cachedPosition,
                                cachedDistance / 2));

                        _trapStopwatch.Restart();

                        if (err1 == LokiPoe.InGameState.UseResult.None)
                            return true;

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Handle curse logic - curse magic+ and packs of 4+, but only cast within MaxRangeRange.
                var checkCurses = myPos.Distance(cachedPosition) < OuroborosSettings.Instance.MaxRangeRange &&
                                (cachedRarity >= Rarity.Magic || cachedMobsNearForCurse >= 5);
                if (checkCurses)
                {
					//Curse on hit
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_curseOnHitSlot);
					if (skill != null &&
						skill.CanUse() &&
						cachedIsCursable &&
						cachedCurseCount < 1) //_totalCursesAllowed &&
						//!cachedHasCurseFrom[skill.Name])
						//Can't use because cachedHasCurseFrom probably returns false all the time since the skill isn't
						//the actual curse, so we can only COH if they have no curses until a solution is found.
					{
						await DisableAlwaysHiglight();

						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_curseOnHitSlot, true, cachedPosition);
						if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

						Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
					}
				
					//Cast curses
                    foreach (var curseSlot in _curseSlots)
                    {
                        var curseSkill = LokiPoe.InGameState.SkillBarHud.Slot(curseSlot);
                        if (curseSkill.CanUse() &&
                            (cachedIsCursable &&
                            cachedCurseCount < _totalCursesAllowed &&
                            !cachedHasCurseFrom[curseSkill.Name])
							|| //By Fujiyama
							(curseSkill.Name == "Essence Drain" &&
							!cachedED &&
							(cachedRarity >= Rarity.Magic ||
							cachedMobsNearForCurse <= 2)))
                        {
                            await DisableAlwaysHiglight();

                            var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(curseSlot, true, cachedPosition);
                            if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                            Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, curseSkill.Name);
                        }
                    }
                }

                // Simply cast Blood Rage if we have it.
                if (_bloodRageSlot != -1)
                {
                    // See if we can use the skill.
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_bloodRageSlot);
                    if (skill.CanUse() && !LokiPoe.Me.HasBloodRageBuff && cachedDistance < OuroborosSettings.Instance.CombatRange)
                    {
                        var err1 = LokiPoe.InGameState.SkillBarHud.Use(_bloodRageSlot, true);
                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

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
                        var err1 = LokiPoe.InGameState.SkillBarHud.Use(_rfSlot, true);
                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                    }
                }

                if (_summonRagingSpiritSlot != -1 &&
                    _summonRagingSpiritStopwatch.ElapsedMilliseconds >
                    OuroborosSettings.Instance.SummonRagingSpiritDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonRagingSpiritSlot);
                    var max = skill.GetStat(StatTypeGGG.NumberOfRagingSpiritsAllowed);
                    if (skill.NumberDeployed < max && skill.CanUse())
                    {
                        ++_summonRagingSpiritCount;

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_summonRagingSpiritSlot, false, targetPosition);

                        if (_summonRagingSpiritCount >=
                            OuroborosSettings.Instance.SummonRagingSpiritCountPerDelay)
                        {
                            _summonRagingSpiritCount = 0;
                            _summonRagingSpiritStopwatch.Restart();
                        }

                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
                    }
                }

                if (_summonSkeletonsSlot != -1 &&
                    _summonSkeletonsStopwatch.ElapsedMilliseconds >
                    OuroborosSettings.Instance.SummonSkeletonDelayMs)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonSkeletonsSlot);
                    var max = skill.GetStat(StatTypeGGG.NumberOfSkeletonsAllowed);
                    if (skill.NumberDeployed < max && skill.CanUse())
                    {
                        ++_summonSkeletonCount;

                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_summonSkeletonsSlot, true,
                            myPos.GetPointAtDistanceAfterThis(cachedPosition,
                                cachedDistance / 2));

                        if (_summonSkeletonCount >= OuroborosSettings.Instance.SummonSkeletonCountPerDelay)
                        {
                            _summonSkeletonCount = 0;
                            _summonSkeletonsStopwatch.Restart();
                        }

                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                if (_mineSlot != -1 && _mineStopwatch.ElapsedMilliseconds >
                    OuroborosSettings.Instance.MineDelayMs &&
                    myPos.Distance(cachedPosition) < OuroborosSettings.Instance.MaxMeleeRange)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_mineSlot);
                    var max = skill.GetStat(StatTypeGGG.SkillDisplayNumberOfRemoteMinesAllowed);
                    var insta = skill.GetStat(StatTypeGGG.MineDetonationIsInstant) == 1;
                    if (skill.NumberDeployed < max && skill.CanUse())
                    {
                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.Use(_mineSlot, true);

                        if (err1 == LokiPoe.InGameState.UseResult.None)
                        {
                            if (!insta)
                            {
                                await Coroutines.LatencyWait();
                                
                                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.detonate_mines, true, false, false);
                            }

                            _mineStopwatch.Restart();

                            return true;
                        }

                        _mineStopwatch.Restart();

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Handle Wither logic.
                if (_witherSlot != -1)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_witherSlot);
                    if (skill.CanUse(false, false, false) && !cachedWither)
                    {
                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_witherSlot, true, cachedPosition);

                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Handle contagion logic.
                if (_contagionSlot != -1)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_contagionSlot);
                    if (skill.CanUse(false, false, false) && !cachedContagion)
                    {
                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_contagionSlot, true, cachedPosition);

                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Handle cold snap logic. Only use when power charges won't be consumed.
                if (_coldSnapSlot != -1)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_coldSnapSlot);
                    if (skill.CanUse(false, false, false))
                    {
                        await DisableAlwaysHiglight();

                        var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_coldSnapSlot, true, cachedPosition);

                        if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

                        Log.ErrorFormat("[Logic] UseAt returned {0} for {1}.", err1, skill.Name);
                    }
                }

                // Auto-cast vaal skills when appropriate.
                if ((LokiPoe.Me.EnergyShieldPercent < 30 || LokiPoe.Me.HealthPercent < 30)
					||
					(OuroborosSettings.Instance.VaalNormalMobs && cachedRarity == Rarity.Normal ||
                    OuroborosSettings.Instance.VaalMagicMobs && cachedRarity == Rarity.Magic ||
                    OuroborosSettings.Instance.VaalRareMobs && cachedRarity == Rarity.Rare ||
                    OuroborosSettings.Instance.VaalUniqueMobs && cachedRarity == Rarity.Unique)
					&&
                    _vaalStopwatch.ElapsedMilliseconds > 1000)
                {
                    foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
                    {
                        if (skill.SkillTags.Contains("vaal"))
                        {
                            if (skill.CanUse())
                            {
								bool use = true;
								if(_defensiveVaalSkills.Contains(skill.Name))
								{
									use = false;
									switch (skill.Name) //Todo: add full es/health compatibility for all of these. Requires fiddling with the settings gui
									{
										case "Vaal Discipline":
											if(LokiPoe.Me.EnergyShieldPercent < 30) { use = true; }
											break;
										case "Vaal Immortal Call":
											if(LokiPoe.Me.HealthPercent < 30) { use = true; }
											break;
										case "Vaal Grace":
											if(LokiPoe.Me.HealthPercent < 30) { use = true; }
											break;
									}
								}
							
								if(use == true)
								{
									await DisableAlwaysHiglight();

									var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(skill.Slot, false, cachedPosition);
									if (err1 == LokiPoe.InGameState.UseResult.None) { return true; }

									Log.ErrorFormat("[Logic] Use returned {0} for {1}.", err1, skill.Name);
								}
                            }
                        }
                    }
                    _vaalStopwatch.Restart();
                }

                var aip = false;

                var aoe = false;
                var melee = false;

                int slot;

				// Use frenzy for charges if it's not one of our main skills and we aren't at max, or we're at 0 if using flicker strike.
				var flicker = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Flicker Strike");
				
				if (_frenzySlot != -1 && _frenzySlot != OuroborosSettings.Instance.SingleTargetMeleeSlot && _frenzySlot != OuroborosSettings.Instance.AoeMeleeSlot
					&& _frenzySlot != OuroborosSettings.Instance.SingleTargetRangedSlot && _frenzySlot != OuroborosSettings.Instance.AoeRangedSlot
					&& ((!IsCastableHelper(flicker) && LokiPoe.ObjectManager.Me.GetStat(StatTypeGGG.CurrentFrenzyCharges) != LokiPoe.ObjectManager.Me.GetStat(StatTypeGGG.MaxFrenzyCharges))
					|| (LokiPoe.ObjectManager.Me.GetStat(StatTypeGGG.CurrentFrenzyCharges) < 1)))
				{
					slot = _frenzySlot;
					// If we have a melee skill then frenzy is probably (definitely?) melee rather than ranged. Otherwise it's hard to tell.
					if (OuroborosSettings.Instance.SingleTargetMeleeSlot != -1 || OuroborosSettings.Instance.AoeMeleeSlot != -1) melee = true;
				}
				else
				{
					// Logic for figuring out if we should use an AoE skill or single target.
					if ((cachedRarity < Rarity.Magic && cachedNumberOfMobsNear >= OuroborosSettings.Instance.NormalMobsToAOE && OuroborosSettings.Instance.NormalMobsToAOE != -1)
						|| (cachedRarity < Rarity.Rare && cachedNumberOfMobsNear >= OuroborosSettings.Instance.MagicMobsToAOE && OuroborosSettings.Instance.MagicMobsToAOE != -1)
						|| (cachedRarity < Rarity.Unique && cachedNumberOfMobsNear >= OuroborosSettings.Instance.RareMobsToAOE && OuroborosSettings.Instance.RareMobsToAOE != -1)
						|| (cachedNumberOfMobsNear >= OuroborosSettings.Instance.UniqueMobsToAOE && OuroborosSettings.Instance.UniqueMobsToAOE != -1))
					{
						aoe = true;
					}
					
					//Logic for deciding whether to use a melee or ranged attack.
					if (myPos.Distance(cachedPosition) < OuroborosSettings.Instance.MaxMeleeRange && (
						(OuroborosSettings.Instance.AoeMeleeSlot != -1 && aoe == true)
						|| (OuroborosSettings.Instance.SingleTargetMeleeSlot != -1 && aoe == false)))
					{
						melee = true;
					}

					// This sillyness is for making sure we always use a skill, and is why generic code is a PITA
					// when it can be configured like so.
					if (aoe)
					{
						if (melee)
						{
							slot = EnsurceCast(OuroborosSettings.Instance.AoeMeleeSlot);
							if (slot == -1)
							{
								slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetMeleeSlot);
								if (slot == -1)
								{
									melee = false;
									slot = EnsurceCast(OuroborosSettings.Instance.AoeRangedSlot);
									if (slot == -1)
									{
										slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetRangedSlot);
									}
								}
							}
						}
						else
						{
							slot = EnsurceCast(OuroborosSettings.Instance.AoeRangedSlot);
							if (slot == -1)
							{
								slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetRangedSlot);
								if (slot == -1)
								{
									melee = true;
									slot = EnsurceCast(OuroborosSettings.Instance.AoeMeleeSlot);
									if (slot == -1)
									{
										slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetMeleeSlot);
									}
								}
							}
						}
					}
					else
					{
						if (melee)
						{
							slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetMeleeSlot);
							if (slot == -1)
							{
								slot = EnsurceCast(OuroborosSettings.Instance.AoeMeleeSlot);
								if (slot == -1)
								{
									melee = false;
									slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetRangedSlot);
									if (slot == -1)
									{
										slot = EnsurceCast(OuroborosSettings.Instance.AoeRangedSlot);
									}
								}
							}
						}
						else
						{
							slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetRangedSlot);
							if (slot == -1)
							{
								slot = EnsurceCast(OuroborosSettings.Instance.AoeRangedSlot);
								if (slot == -1)
								{
									melee = true;
									slot = EnsurceCast(OuroborosSettings.Instance.SingleTargetMeleeSlot);
									if (slot == -1)
									{
										slot = EnsurceCast(OuroborosSettings.Instance.AoeMeleeSlot);
									}
								}
							}
						}
					}
				}

                if (OuroborosSettings.Instance.AlwaysAttackInPlace)
                    aip = true;

                if (slot == -1)
                {
                    slot = OuroborosSettings.Instance.FallbackSlot;
                    melee = true;
                }

                if (melee || cachedProxShield)
                {
                    var dist = LokiPoe.MyPosition.Distance(cachedPosition);
                    if (dist > OuroborosSettings.Instance.MaxMeleeRange)
                    {
                        Log.InfoFormat("[Logic] Now moving towards {0} because [dist ({1}) > MaxMeleeRange ({2})]",
                            cachedPosition, dist, OuroborosSettings.Instance.MaxMeleeRange);

                        if (!PlayerMover.MoveTowards(cachedPosition))
                        {
                            Log.ErrorFormat("[Logic] MoveTowards failed for {0}.", cachedPosition);
                        }
                        return true;
                    }
                }
                else
                {
                    var dist = LokiPoe.MyPosition.Distance(cachedPosition);
                    if (dist > OuroborosSettings.Instance.MaxRangeRange)
                    {
                        Log.InfoFormat("[Logic] Now moving towards {0} because [dist ({1}) > MaxRangeRange ({2})]",
                            cachedPosition, dist, OuroborosSettings.Instance.MaxRangeRange);

                        if (!PlayerMover.MoveTowards(cachedPosition))
                        {
                            Log.ErrorFormat("[Logic] MoveTowards failed for {0}.", cachedPosition);
                        }
                        return true;
                    }
                }

                await DisableAlwaysHiglight();
				
				if (cachedDistance > 50 && LokiPoe.InGameState.SkillBarHud.Slot(slot).Name != "Caustic Arrow") //They aren't on our screen but are but within max ranged range, so try to offscreen them
				{
					Log.DebugFormat("[Logic] Attempting to offscreen target.");
					
					var err = LokiPoe.InGameState.SkillBarHud.UseAt(slot, aip, myPos.GetPointAtDistanceAfterThis(cachedPosition, 40));
					if (err != LokiPoe.InGameState.UseResult.None)
					{
						Log.ErrorFormat("[Logic] UseAt returned {0}.", err);
					}
				}
				else
				{
					var err = LokiPoe.InGameState.SkillBarHud.UseAt(slot, aip, targetPosition);
					if (err != LokiPoe.InGameState.UseResult.None)
					{
						Log.ErrorFormat("[Logic] UseAt returned {0}.", err);
					}
				}

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

            return null;
        }

        #endregion

        private WorldItem BestAnimateGuardianTarget(Monster monster, int maxLevel)
        {
            var worldItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => !_ignoreAnimatedItems.Contains(wi.Id) && wi.Distance < 30)
                .OrderBy(wi => wi.Distance);
            foreach (var wi in worldItems)
            {
                var item = wi.Item;
                if (item.RequiredLevel <= maxLevel &&
                    item.IsIdentified &&
                    !item.IsChromatic &&
                    item.SocketCount < 5 &&
                    item.MaxLinkCount < 5 &&
                    item.Rarity <= Rarity.Magic
                    )
                {
                    if (monster == null || monster.LeftHandWeaponVisual == "")
                    {
                        if (item.IsClawType || item.IsOneHandAxeType || item.IsOneHandMaceType ||
                            item.IsOneHandSwordType ||
                            item.IsOneHandThrustingSwordType || item.IsTwoHandAxeType || item.IsTwoHandMaceType ||
                            item.IsTwoHandSwordType)
                        {
                            _ignoreAnimatedItems.Add(wi.Id);
                            return wi;
                        }
                    }

                    if (monster == null || monster.ChestVisual == "")
                    {
                        if (item.IsBodyArmorType)
                        {
                            _ignoreAnimatedItems.Add(wi.Id);
                            return wi;
                        }
                    }

                    if (monster == null || monster.HelmVisual == "")
                    {
                        if (item.IsHelmetType)
                        {
                            _ignoreAnimatedItems.Add(wi.Id);
                            return wi;
                        }
                    }

                    if (monster == null || monster.BootsVisual == "")
                    {
                        if (item.IsBootType)
                        {
                            _ignoreAnimatedItems.Add(wi.Id);
                            return wi;
                        }
                    }

                    if (monster == null || monster.GlovesVisual == "")
                    {
                        if (item.IsGloveType)
                        {
                            _ignoreAnimatedItems.Add(wi.Id);
                            return wi;
                        }
                    }
                }
            }

            return null;
        }

        private WorldItem BestAnimateWeaponTarget(int maxLevel)
        {
            var worldItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => !_ignoreAnimatedItems.Contains(wi.Id) && wi.Distance < 30)
                .OrderBy(wi => wi.Distance);
            foreach (var wi in worldItems)
            {
                var item = wi.Item;
                if (item.IsIdentified &&
                    item.RequiredLevel <= maxLevel &&
                    !item.IsChromatic &&
                    item.SocketCount < 5 &&
                    item.MaxLinkCount < 5 &&
                    item.Rarity <= Rarity.Magic &&
                    (item.IsClawType || item.IsOneHandAxeType || item.IsOneHandMaceType || item.IsOneHandSwordType ||
                    item.IsOneHandThrustingSwordType || item.IsTwoHandAxeType || item.IsTwoHandMaceType ||
                    item.IsTwoHandSwordType || item.IsDaggerType || item.IsStaffType))
                {
                    _ignoreAnimatedItems.Add(wi.Id);
                    return wi;
                }
            }
            return null;
        }

        private Monster BestDeadTarget
        {
            get
            {
                var myPos = LokiPoe.MyPosition;
                return LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                    .Where(
                        m =>
                            m.Distance < 30 && m.IsActiveDead && m.Rarity != Rarity.Unique && m.CorpseUsable &&
                            ExilePather.PathDistance(myPos, m.Position, true, !OuroborosSettings.Instance.LeaveFrame) < 30)
                    .OrderBy(m => m.Distance).FirstOrDefault();
            }
        }

        private async Task<bool> CombatLogicEnd()
        {
            await EnableAlwaysHiglight();

            return false;
        }

        private async Task<bool> HandleShrines()
        {
            // TODO: Shrines need special CR logic, because it's now the CRs responsibility for handling all combat situations,
            // and shrines are now considered a combat situation due their nature.
			
            // Check for any active shrines.
            var shrines =
                LokiPoe.ObjectManager.Objects.OfType<Shrine>()
                    .Where(s => !s.IsDeactivated && s.Distance < 50)
                    .OrderBy(s => s.Distance)
                    .ToList();

            if (!shrines.Any())
                return false;

            // For now, just take the first shrine found.

            var shrine = shrines[0];

            // Try and only move to touch it when we have a somewhat navigable path.
            if (NumberOfMobsBetween(LokiPoe.Me, shrine, 5, true) < 2 &&
                NumberOfMobsNear(LokiPoe.Me, 20) < 3)
            {
                Log.DebugFormat("[HandleShrines] Now moving towards the Shrine {0}.", shrine.Id);

                var myPos = LokiPoe.MyPosition;

                var pos = ExilePather.FastWalkablePositionFor(shrine);

                // We need to filter out based on pathfinding, since otherwise, a large gap will lockup the bot.
                var pathDistance = ExilePather.PathDistance(myPos, pos, true, !OuroborosSettings.Instance.LeaveFrame);
                if (pathDistance > 50)
                {
                    return false;
                }

                var canSee = ExilePather.CanObjectSee(LokiPoe.Me, pos, !OuroborosSettings.Instance.LeaveFrame);
                var inDistance = myPos.Distance(pos) < 20;
                if (canSee && inDistance)
                {
                    Log.DebugFormat("[HandleShrines] Now attempting to interact with the Shrine {0}.", shrine.Id);

                    await Coroutines.FinishCurrentAction();

                    await Coroutines.InteractWith(shrine);
                }
                else
                {
                    Log.DebugFormat("[HandleShrines] Moving towards {0}. [canSee: {1} | inDistance: {2}]", pos, canSee,
                        inDistance);
                    if (!PlayerMover.MoveTowards(pos))
                    {
                        Log.ErrorFormat("[HandleShrines] MoveTowards failed for {0}.", pos);
                    }
                }

                return true;
            }

            return false;
        }

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
