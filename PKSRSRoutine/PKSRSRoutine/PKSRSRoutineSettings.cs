using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loki;
using Loki.Common;
using Newtonsoft.Json;

namespace PKSRSRoutine
{
    public class PKSRSRoutineSettings : JsonSettings
    {
        private static PKSRSRoutineSettings _instance;

        /// <summary>The current instance for this class. </summary>
        public static PKSRSRoutineSettings Instance
        {
            get { return _instance ?? (_instance = new PKSRSRoutineSettings()); }
        }

        /// <summary>The default ctor. Will use the settings path "VortexRoutine".</summary>
        public PKSRSRoutineSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, string.Format("{0}.json", "PKSRSRoutine")))
        {
        }

        private int _singleTargetMeleeSlot;
        private int _singleTargetRangedSlot;
        private int _aoeMeleeSlot;
        private int _aoeRangedSlot;
        private int _fallbackSlot;
        private int _combatRange;
        private int _maxMeleeRange;
        private int _maxRangeRange;
        private bool _alwaysAttackInPlace;

        private int _totemDelayMs;

        private int _summonSkeletonCountPerDelay;
        private int _summonSkeletonDelayMs;

        private int _summonRagingSpiritCountPerDelay;
        private int _summonRagingSpiritDelayMs;

        private bool _autoCastVaalSkills;

        private bool _enableAurasFromItems;

        private bool _debugAuras;

        private bool _leaveFrame;

        private string _blacklistedSkillIds;

        private bool _skipShrines;


        /// <summary>
		/// Should the CR leave the current frame to do pathfinds and other frame intensive tasks.
		/// NOTE: This might cause random memory exceptions due to memory no longer being valid in the CR.
		/// </summary>
		[DefaultValue(false)]
        public bool LeaveFrame
        {
            get { return _leaveFrame; }
            set
            {
                if (value.Equals(_leaveFrame))
                {
                    return;
                }
                _leaveFrame = value;
                NotifyPropertyChanged(() => LeaveFrame);
            }
        }

        /// <summary>
		/// Should the CR skip shrines?
		/// </summary>
		[DefaultValue(false)]
        public bool SkipShrines
        {
            get { return _skipShrines; }
            set
            {
                if (value.Equals(_skipShrines))
                {
                    return;
                }
                _skipShrines = value;
                NotifyPropertyChanged(() => SkipShrines);
            }
        }

        /// <summary>
        /// Should the CR use auras granted by items rather than skill gems?
        /// </summary>
        [DefaultValue(true)]
        public bool EnableAurasFromItems
        {
            get { return _enableAurasFromItems; }
            set
            {
                if (value.Equals(_enableAurasFromItems))
                {
                    return;
                }
                _enableAurasFromItems = value;
                NotifyPropertyChanged(() => EnableAurasFromItems);
            }
        }

        /// <summary>
        /// Should the CR output casting errors for auras?
        /// </summary>
        [DefaultValue(false)]
        public bool DebugAuras
        {
            get { return _debugAuras; }
            set
            {
                if (value.Equals(_debugAuras))
                {
                    return;
                }
                _debugAuras = value;
                NotifyPropertyChanged(() => DebugAuras);
            }
        }

        /// <summary>
        /// Should vaal skills be auto-cast during combat.
        /// </summary>
        [DefaultValue(true)]
        public bool AutoCastVaalSkills
        {
            get { return _autoCastVaalSkills; }
            set
            {
                if (value.Equals(_autoCastVaalSkills))
                {
                    return;
                }
                _autoCastVaalSkills = value;
                NotifyPropertyChanged(() => AutoCastVaalSkills);
            }
        }

        /// <summary>
        /// How many casts to perform before the delay happens.
        /// </summary>
        [DefaultValue(3)]
        public int SummonRagingSpiritCountPerDelay
        {
            get { return _summonRagingSpiritCountPerDelay; }
            set
            {
                if (value.Equals(_summonRagingSpiritCountPerDelay))
                {
                    return;
                }
                _summonRagingSpiritCountPerDelay = value;
                NotifyPropertyChanged(() => SummonRagingSpiritCountPerDelay);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(5000)]
        public int SummonRagingSpiritDelayMs
        {
            get { return _summonRagingSpiritDelayMs; }
            set
            {
                if (value.Equals(_summonRagingSpiritDelayMs))
                {
                    return;
                }
                _summonRagingSpiritDelayMs = value;
                NotifyPropertyChanged(() => SummonRagingSpiritDelayMs);
            }
        }

        /// <summary>
        /// Should the CR always attack in place.
        /// </summary>
        [DefaultValue(false)]
        public bool AlwaysAttackInPlace
        {
            get { return _alwaysAttackInPlace; }
            set
            {
                if (value.Equals(_alwaysAttackInPlace))
                {
                    return;
                }
                _alwaysAttackInPlace = value;
                NotifyPropertyChanged(() => AlwaysAttackInPlace);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int SingleTargetMeleeSlot
        {
            get { return _singleTargetMeleeSlot; }
            set
            {
                if (value.Equals(_singleTargetMeleeSlot))
                {
                    return;
                }
                _singleTargetMeleeSlot = value;
                NotifyPropertyChanged(() => SingleTargetMeleeSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int SingleTargetRangedSlot
        {
            get { return _singleTargetRangedSlot; }
            set
            {
                if (value.Equals(_singleTargetRangedSlot))
                {
                    return;
                }
                _singleTargetRangedSlot = value;
                NotifyPropertyChanged(() => SingleTargetRangedSlot);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int AoeMeleeSlot
        {
            get { return _aoeMeleeSlot; }
            set
            {
                if (value.Equals(_aoeMeleeSlot))
                {
                    return;
                }
                _aoeMeleeSlot = value;
                NotifyPropertyChanged(() => AoeMeleeSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int AoeRangedSlot
        {
            get { return _aoeRangedSlot; }
            set
            {
                if (value.Equals(_aoeRangedSlot))
                {
                    return;
                }
                _aoeRangedSlot = value;
                NotifyPropertyChanged(() => AoeRangedSlot);
            }
        }

        /// <summary>
        /// The skill slot to use as a fallback if the desired skill cannot be cast.
        /// </summary>
        [DefaultValue(-1)]
        public int FallbackSlot
        {
            get { return _fallbackSlot; }
            set
            {
                if (value.Equals(_fallbackSlot))
                {
                    return;
                }
                _fallbackSlot = value;
                NotifyPropertyChanged(() => FallbackSlot);
            }
        }

        /// <summary>
        /// Only attack mobs within this range.
        /// </summary>
        [DefaultValue(70)]
        public int CombatRange
        {
            get { return _combatRange; }
            set
            {
                if (value.Equals(_combatRange))
                {
                    return;
                }
                _combatRange = value;
                NotifyPropertyChanged(() => CombatRange);
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Melee skill.
        /// Do not set too high, as the cursor will overlap the GUI.
        /// </summary>
        [DefaultValue(10)]
        public int MaxMeleeRange
        {
            get { return _maxMeleeRange; }
            set
            {
                if (value.Equals(_maxMeleeRange))
                {
                    return;
                }
                _maxMeleeRange = value;
                NotifyPropertyChanged(() => MaxMeleeRange);
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Ranged skill.
        /// Do not set too high, as the cursor will overlap the GUI.
        /// </summary>
        [DefaultValue(35)]
        public int MaxRangeRange
        {
            get { return _maxRangeRange; }
            set
            {
                if (value.Equals(_maxRangeRange))
                {
                    return;
                }
                _maxRangeRange = value;
                NotifyPropertyChanged(() => MaxRangeRange);
            }
        }

        /// <summary>
        /// The delay between casting totems in combat.
        /// </summary>
        [DefaultValue(5000)]
        public int TotemDelayMs
        {
            get { return _totemDelayMs; }
            set
            {
                if (value.Equals(_totemDelayMs))
                {
                    return;
                }
                _totemDelayMs = value;
                NotifyPropertyChanged(() => TotemDelayMs);
            }
        }

        [JsonIgnore]
        private List<int> _allSkillSlots;

        /// <summary> </summary>
        [JsonIgnore]
        public List<int> AllSkillSlots
        {
            get
            {
                return _allSkillSlots ?? (_allSkillSlots = new List<int>
                {
                    -1,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8
                });
            }
        }
    }
}
