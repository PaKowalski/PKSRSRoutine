using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using log4net;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Common;
using Loki.Game;
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
        private int _animateWeaponSlot = -1;
        private int _animateGuardianSlot = -1;
        private int _flameblastSlot = -1;
        private int _enduringCrySlot = -1;
        private int _moltenShellSlot = -1;
        private int _bloodRageSlot = -1;
        private int _rfSlot = -1;
        private readonly List<int> _curseSlots = new List<int>();
        private int _auraSlot = -1;
        private int _totemSlot = -1;
        private int _trapSlot = -1;
        private int _mineSlot = -1;
        private int _summonSkeletonsSlot = -1;
        private int _summonRagingSpiritSlot = -1;
        private int _coldSnapSlot = -1;
        private int _contagionSlot = -1;
        private int _witherSlot = -1;
        private int _bladeVortexSlot = -1;
        private int _bladeVortexCount = 0;

        private bool _isCasting;
        private int _castingSlot;

        private int _currentLeashRange = -1;


        private readonly Stopwatch _trapStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _totemStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _mineStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _animateWeaponStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _animateGuardianStopwatch = Stopwatch.StartNew();
        private readonly Stopwatch _moltenShellStopwatch = Stopwatch.StartNew();
        private readonly List<int> _ignoreAnimatedItems = new List<int>();
        private readonly Stopwatch _vaalStopwatch = Stopwatch.StartNew();

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



        #region IRoutine Implementations
        public UserControl Control { get; }
        public JsonSettings Settings { get; }
        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Tick()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public string Author { get; }
        public string Description { get; }
        public string Version { get; }
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Deinitialize()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Logic(string type, params dynamic[] param)
        {
            throw new NotImplementedException();
        }

        public object Execute(string name, params dynamic[] param)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}
