using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //TODO when locking on target use roll user inputs
        //if a enemy comes in range (800m) start building decoy
        //automatic fire jolts if not rotating and not moving left or right
        //lock on target calculate aim vector if is in gravity or not to fire jolts
        //LIDAR TARGETER

        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string antennasName = "T";
        readonly string lightsName = "[CRX] Rotating Light";
        readonly string turretsName = "[CRX] Turret";
        readonly string lcdsTargetName = "[CRX] LCD Target";
        readonly string controllersName = "Reference";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string weldersName = "Missile";
        readonly string rocketsName = "[CRX] Rocket Launcher";
        readonly string gatlingsName = "[CRX] Gatling Gun";
        readonly string projectorsMissilesName = "Missile";
        readonly string projectorsDronesName = "Drone";
        readonly string missileAntennasName = "A [M]";
        readonly string missilePrefix = "[M]";
        readonly string antennaTag = "[RELAY]";
        readonly string missileAntennaTag = "[MISSILE]";
        readonly string magneticDriveName = "[CRX] PB Magnetic Drive";
        readonly string managerName = "[CRX] PB Manager";
        readonly string cargoName = "[CRX] Cargo";
        readonly string debugPanelName = "[CRX] Debug";

        const string commandLaunch = "Launch";
        const string commandUpdate = "Update";
        const string commandLost = "Lost";
        const string commandSpiral = "Spiral";

        const string argClear = "Clear";
        const string argLock = "Lock";
        const string argSwitchWeapon = "SwitchGun";
        const string argAutoFire = "AutoFire";
        const string argAutoMissiles = "AutoMissiles";
        const string argSwitchPayLoad = "SwitchPayLoad";
        const string argToggleAllGuns = "ToggleAllGuns";

        const string argMDGyroStabilizeOff = "StabilizeOff";
        const string argMDGyroStabilizeOn = "StabilizeOn";
        const string argSunchaseOff = "SunchaseOff";

        readonly string sectionTag = "MissilesSettings";
        readonly string cockpitTargetSurfaceKey = "cockpitTargetSurface";

        readonly int fireDelay = 5;
        readonly bool creative = true;
        readonly int missilesCount = 2;
        readonly int autoMissilesDelay = 9;
        readonly int writeDelay = 1;
        readonly float joltProjectileMaxSpeed = 933f;
        readonly float rocketProjectileMaxSpeed = 200f;
        double rocketProjectileMaxRange = 500d;
        readonly float gatlingProjectileMaxSpeed = 400f;
        readonly double gatlingProjectileMaxRange = 800d;
        readonly double gunsMaxRange = 900d;
        readonly int fudgeAttempts = 8;
        readonly double aimP = 1;
        readonly double aimI = 0;
        readonly double aimD = 1;
        readonly double integralWindupLimit = 0;

        int weaponType = 2;//0 None - 1 Rockets - 2 Gatlings
        int selectedPayLoad = 0;//0 Missiles - 1 Drones
        bool autoFire = true;
        bool autoMissiles = false;
        bool useAllGuns = true;
        bool sequenceWeapons = false;
        int fireCount = 0;
        int weaponIndex = 0;
        int selectedMissile = 1;
        int cockpitTargetSurface = 0;
        int autoMissilesCounter = 0;
        long currentTick = 1;
        long lostTicks = 0;
        bool doOnce = false;
        bool farShootOnce = false;
        bool shootOnce = false;
        bool missilesLoaded = false;
        bool MDOff = false;
        bool sunChaseOff = false;
        int writeCount = 0;
        bool fudgeVectorSwitch = false;
        double fudgeFactor = 5;
        bool readyToFire = false;
        bool readyToFireOnce = true;
        bool gatlingAmmoFound = true;
        bool missileAmmoFound = true;

        Vector3D targetPosition;
        string targetName = null;
        double targetDiameter;
        MyDetectedEntityInfo targetInfo;

        readonly Random random = new Random();
        const float globalTimestep = 10.0f / 60.0f;
        const long ticksScanDelay = 1;
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 1;//degrees

        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyLightingBlock> LIGHTS = new List<IMyLightingBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();
        public List<IMyProjector> PROJECTORSDRONES = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSMISSILES = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyRadioAntenna> MISSILEANTENNAS = new List<IMyRadioAntenna>();
        public List<IMyUserControllableGun> ROCKETS = new List<IMyUserControllableGun>();
        public List<IMyUserControllableGun> GATLINGS = new List<IMyUserControllableGun>();
        public List<IMyCargoContainer> CARGOS = new List<IMyCargoContainer>();
        public List<IMyInventory> INVENTORIES = new List<IMyInventory>();

        IMyShipController CONTROLLER;
        IMyRadioAntenna ANTENNA;
        IMyProgrammableBlock MAGNETICDRIVEPB;
        IMyProgrammableBlock MANAGERPB;
        IMyTextPanel DEBUG;

        IMyUnicastListener UNILISTENER;
        public IMyBroadcastListener BROADCASTLISTENER;

        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");

        Dictionary<long, string> MissileIDs = new Dictionary<long, string>();
        public List<double> LostMissileIDs = new List<double>();
        Dictionary<long, MyTuple<double, double, string>> missilesInfo = new Dictionary<long, MyTuple<double, double, string>>();

        public StringBuilder targetLog = new StringBuilder("");
        public StringBuilder missileLog = new StringBuilder("");
        public StringBuilder debugLog = new StringBuilder("");

        readonly MyIni myIni = new MyIni();

        PID yawController;
        PID pitchController;
        PID rollController;

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup()
        {
            UNILISTENER = IGC.UnicastListener;
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(antennaTag);

            GetBlocks();
            SetBlocks();
            GetMissileAntennas();
            SetMissileAntennas();

            InitPIDControllers();

            if (selectedPayLoad == 0)
            {
                TEMPPROJECTORS = PROJECTORSMISSILES;
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
            }
            else if (selectedPayLoad == 1)
            {
                TEMPPROJECTORS = PROJECTORSDRONES;
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
            }

            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }

            autoMissilesCounter = autoMissilesDelay + 1;
        }

        public void Main(string arg)
        {
            try
            {
                Echo($"MISSILEANTENNAS:{MISSILEANTENNAS.Count}");
                Echo($"LIDARS:{LIDARS.Count}");
                Echo($"CONTROLLERS:{CONTROLLERS.Count}");
                Echo($"COCKPITS:{COCKPITS.Count}");
                Echo($"TURRETS:{TURRETS.Count}");
                Echo($"LIGHTS:{LIGHTS.Count}");
                Echo($"SURFACES:{SURFACES.Count}");
                Echo($"ALARMS:{ALARMS.Count}");
                Echo($"GYROS:{GYROS.Count}");
                Echo($"WELDERS:{WELDERS.Count}");
                Echo($"ROCKETS:{ROCKETS.Count}");
                Echo($"GATLINGS:{GATLINGS.Count}");
                Echo($"PROJECTORSMISSILES:{PROJECTORSMISSILES.Count}");
                Echo($"PROJECTORSDRONES:{PROJECTORSDRONES.Count}");
                Echo($"CARGOS:{CARGOS.Count}");

                RemoveLostMissiles();
                GetMessages();
                ReadMessages();

                if (targetName != null)
                {
                    if (lostTicks > ticksScanDelay)//if lidars or turrets doesn't detect a enemy for some time reset the script
                    {
                        ResetTargeter();
                        return;
                    }

                    ActivateTargeter();//things to run once when a enemy is detected

                    DeactivateOtherScriptsGyros();

                    bool targetFound = TurretsDetection(true);

                    if (!targetFound)
                    {
                        targetFound = AcquireTarget();
                    }

                    if (targetFound && currentTick == ticksScanDelay)//send message to missiles every some ticks
                    {
                        foreach (var id in MissileIDs)
                        {
                            SendMissileUnicastMessage(commandUpdate, id.Key);
                        }

                        if (autoMissiles)
                        {
                            if (autoMissilesCounter > autoMissilesDelay)
                            {
                                arg = commandLaunch;
                                autoMissilesCounter = 0;
                            }
                            autoMissilesCounter++;
                        }
                    }

                    LockOnTarget(LIDARS[0]);

                    CalculateTicks(targetFound);

                    if (writeCount == writeDelay)
                    {
                        CheckGunsAmmo();
                    }

                    ManageGuns();

                    ReadTargetInfo();
                }
                else
                {
                    if (doOnce)//things to run once when a enemy is lost
                    {
                        ResetTargeter();
                        return;
                    }

                    ActivateOtherScriptsGyros();

                    TurretsDetection(false);
                }

                bool completed = CheckProjectors();//TODO this logic has flaws
                if (!completed)
                {
                    missilesLoaded = false;
                }
                else
                {
                    if (!missilesLoaded)
                    {
                        missilesLoaded = LoadMissiles();
                    }
                }

                if (!String.IsNullOrEmpty(arg))
                {
                    ProcessArgs(arg);
                }

                if (writeCount == writeDelay)
                {
                    WriteInfo();
                    writeCount = 0;
                }
                writeCount++;
            }
            catch (Exception e)
            {
                DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                debugLog = new StringBuilder("");
                DEBUG.ReadText(debugLog, true);
                debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                DEBUG.WriteText(debugLog);
            }
        }

        void ProcessArgs(string arg)
        {
            switch (arg)
            {
                case argLock: AcquireTarget(); break;
                case commandLaunch:
                    if (missilesLoaded && targetName != null)
                    {
                        foreach (IMyRadioAntenna block in MISSILEANTENNAS)
                        {
                            string antennaName = missileAntennasName + selectedMissile.ToString();
                            if (block.CustomName.Equals(antennaName))
                            {
                                block.Enabled = true;
                                block.EnableBroadcasting = true;
                            }
                        }
                        selectedMissile++;
                        if (selectedMissile > missilesCount + 1)
                        {
                            GetMissileAntennas();
                            SetMissileAntennas();
                            selectedMissile = 1;
                        }
                        SendMissileBroadcastMessage(commandLaunch);
                    }
                    foreach (var id in MissileIDs)
                    {
                        if (targetName != null)
                        {
                            if (id.Value.Contains(commandLost))
                            {
                                SendMissileUnicastMessage(commandUpdate, id.Key);
                            }
                        }
                    }
                    break;
                case argClear:
                    ResetTargeter();
                    return;
                //break;
                case argSwitchWeapon:
                    weaponType = (weaponType == 1 ? 2 : 1);
                    break;
                case argAutoFire:
                    autoFire = !autoFire;
                    break;
                case argAutoMissiles:
                    autoMissiles = !autoMissiles;
                    break;
                case argSwitchPayLoad:
                    int blockCount = 0;
                    foreach (IMyProjector proj in TEMPPROJECTORS)
                    {
                        blockCount += proj.BuildableBlocksCount;//TODO RemainingBlocks;
                    }
                    if (blockCount == 0)
                    {
                        if (selectedPayLoad == 1)
                        {
                            selectedPayLoad = 0;
                            TEMPPROJECTORS = PROJECTORSMISSILES;
                            foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
                        }
                        else if (selectedPayLoad == 0)
                        {
                            selectedPayLoad = 1;
                            TEMPPROJECTORS = PROJECTORSDRONES;
                            foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
                        }
                    }
                    break;
                case argToggleAllGuns:
                    useAllGuns = !useAllGuns;
                    break;
                case commandSpiral:
                    if (targetName != null)
                    {
                        foreach (var id in MissileIDs)
                        {
                            if (id.Value.Contains(commandUpdate) && !id.Value.Contains("Drone"))
                            {
                                SendMissileUnicastMessage(commandSpiral, id.Key);
                            }
                        }
                    }
                    break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit)
        {
            if (!cockpit.CustomData.Contains(sectionTag))
            {
                cockpit.CustomData += $"[{sectionTag}]\n{cockpitTargetSurfaceKey}={cockpitTargetSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);

            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToString()))
            {
                cockpitTargetSurface = myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToInt32();
                SURFACES.Add(cockpit.GetSurface(cockpitTargetSurface));
            }
        }

        bool TurretsDetection(bool sameId)
        {
            bool targetFound = false;
            foreach (IMyLargeTurretBase turret in TURRETS)
            {
                MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                if (!targ.IsEmpty())
                {
                    if (IsValidLidarTarget(ref targ))
                    {
                        if (sameId)
                        {
                            if (targ.EntityId == targetInfo.EntityId)
                            {
                                targetDiameter = Vector3D.Distance(targ.BoundingBox.Min, targ.BoundingBox.Max);
                                targetInfo = targ;
                                targetFound = true;
                                break;
                            }
                        }
                        else
                        {
                            targetDiameter = Vector3D.Distance(targ.BoundingBox.Min, targ.BoundingBox.Max);
                            targetInfo = targ;
                            targetFound = true;
                            break;
                        }
                    }
                }
            }
            return targetFound;
        }

        bool AcquireTarget()
        {
            bool targetFound = false;
            if (targetName == null)//case argLock
            {
                targetFound = ScanTarget(false);
                if (targetFound)
                {
                    targetFound = ScanTargetCenter(true);

                    if (!targetFound)
                    {
                        if (targetInfo.HitPosition.HasValue)
                        {
                            targetPosition = targetInfo.HitPosition.Value;
                        }
                    }
                }
            }
            else
            {
                if (currentTick == ticksScanDelay)//if target is not lost scan every some ticks
                {
                    targetFound = ScanDelayedTarget();
                    if (!targetFound)
                    {
                        for (int i = 0; i < fudgeAttempts; i++)
                        {
                            targetFound = ScanFudgeTarget();
                            if (targetFound)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return targetFound;
        }

        bool ScanTarget(bool sameId)
        {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            MyDetectedEntityInfo entityInfo = lidar.Raycast(lidar.AvailableScanRange, 0, 0);
            if (!entityInfo.IsEmpty())
            {
                if (IsValidLidarTarget(ref entityInfo))
                {
                    if (sameId)
                    {
                        if (entityInfo.EntityId == targetInfo.EntityId)
                        {
                            targetName = entityInfo.Name;
                            targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                            targetInfo = entityInfo;
                            DetermineTargetPostion();
                            targetFound = true;
                        }
                    }
                    else
                    {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        DetermineTargetPostion();
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool ScanTargetCenter(bool sameId)
        {
            bool targetFound = false;
            Vector3D targetPos = targetInfo.Position;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            double overshootDistance = targetDiameter / 2;
            Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * overshootDistance);
            double dist = Vector3D.Distance(testTargetPosition, lidar.GetPosition());
            if (lidar.CanScan(dist))
            {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(testTargetPosition);
                if (!entityInfo.IsEmpty())
                {
                    if (sameId)
                    {
                        if (entityInfo.EntityId == targetInfo.EntityId)
                        {
                            targetName = entityInfo.Name;
                            targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                            targetInfo = entityInfo;
                            targetPosition = targetInfo.Position;
                            targetFound = true;
                        }
                    }
                    else
                    {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        targetPosition = targetInfo.Position;
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool ScanDelayedTarget()
        {
            bool targetFound = false;
            Vector3D trgtPos = targetPosition;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            float elapsedTime = (currentTick + lostTicks) * globalTimestep;
            Vector3D targetPos = trgtPos + (targetInfo.Velocity * elapsedTime);
            double overshootDistance = targetDiameter / 2;
            Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * overshootDistance);
            double dist = Vector3D.Distance(testTargetPosition, lidar.GetPosition());
            if (lidar.CanScan(dist))
            {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(testTargetPosition);
                if (!entityInfo.IsEmpty())
                {
                    if (entityInfo.EntityId == targetInfo.EntityId)
                    {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        DetermineTargetPostion();
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool ScanFudgeTarget()
        {
            bool targetFound = false;
            Vector3D trgtPos = targetPosition;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            float elapsedTime = (currentTick + lostTicks) * globalTimestep;
            Vector3D scanPosition = trgtPos + targetInfo.Velocity * elapsedTime;
            scanPosition += CalculateFudgeVector(scanPosition - lidar.GetPosition());
            double overshootDistance = targetDiameter / 2;
            scanPosition += Vector3D.Normalize(scanPosition - lidar.GetPosition()) * overshootDistance;
            double dist = Vector3D.Distance(scanPosition, lidar.GetPosition());
            if (lidar.CanScan(dist))
            {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(scanPosition);
                if (!entityInfo.IsEmpty())
                {
                    if (entityInfo.EntityId == targetInfo.EntityId)
                    {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        DetermineTargetPostion();
                        targetFound = true;
                    }
                }
            }

            if (!targetFound)
            {
                fudgeFactor++;
            }
            else
            {
                fudgeFactor = 5;
            }
            return targetFound;
        }

        Vector3D CalculateFudgeVector(Vector3D targetDirection)
        {
            fudgeVectorSwitch = !fudgeVectorSwitch;

            if (!fudgeVectorSwitch)
            {
                return Vector3D.Zero;
            }

            var perpVector1 = Vector3D.CalculatePerpendicularVector(targetDirection);
            var perpVector2 = Vector3D.Cross(perpVector1, targetDirection);
            if (!Vector3D.IsUnit(ref perpVector2))
            {
                perpVector2.Normalize();
            }

            float elapsedTime = (currentTick + lostTicks) * globalTimestep;
            var randomVector = (2.0 * random.NextDouble() - 1.0) * perpVector1 + (2.0 * random.NextDouble() - 1.0) * perpVector2;
            return randomVector * fudgeFactor * elapsedTime;
        }

        void DetermineTargetPostion()
        {
            if (targetInfo.HitPosition.HasValue)
            {
                targetPosition = targetInfo.HitPosition.Value;
            }
            else
            {
                targetPosition = targetInfo.Position;
            }
        }

        void CalculateTicks(bool targetAquired)
        {
            if (currentTick == ticksScanDelay)
            {
                if (!targetAquired)
                {
                    currentTick--;
                    lostTicks++;
                }
                else
                {
                    lostTicks = 0;
                    currentTick = 0;
                }
            }
            currentTick++;
        }

        IMyCameraBlock GetCameraWithMaxRange(List<IMyCameraBlock> cameraList)
        {
            double maxRange = 0d;
            IMyCameraBlock maxRangeCamera = cameraList[0];
            foreach (IMyCameraBlock thisCamera in cameraList)
            {
                if (thisCamera.AvailableScanRange > maxRange)
                {
                    maxRangeCamera = thisCamera;
                    maxRange = maxRangeCamera.AvailableScanRange;
                }
            }
            return maxRangeCamera;
        }

        bool IsValidLidarTarget(ref MyDetectedEntityInfo entityInfo)
        {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid)
            {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral)
                {
                    return true;
                }
            }
            return false;
        }

        void LockOnTarget(IMyTerminalBlock REF)
        {
            float elapsedTime = (currentTick + lostTicks) * globalTimestep;
            Vector3D targetPos = targetPosition + (targetInfo.Velocity * elapsedTime);
            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetPos, REF.GetPosition());
            if (distanceFromTarget > gunsMaxRange)
            {
                aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, joltProjectileMaxSpeed, REF);
            }
            else
            {
                switch (weaponType)
                {
                    case 0:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, joltProjectileMaxSpeed, REF);
                        break;
                    case 1:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, rocketProjectileMaxSpeed, REF);
                        break;
                    case 2:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, gatlingProjectileMaxSpeed, REF);
                        break;
                    default:
                        aimDirection = targetPos - REF.GetPosition();
                        break;
                }
            }
            Vector3D UpVector;
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) { UpVector = CONTROLLER.WorldMatrix.Up; }
            else { UpVector = -CONTROLLER.GetNaturalGravity(); }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, UpVector, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);

            Vector3D forwardVec = CONTROLLER.WorldMatrix.Forward;
            double angle = VectorMath.AngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= angleTolerance)
            {
                readyToFire = true;
            }
            else
            {
                readyToFire = false;
            }
        }

        Vector3D ComputeInterceptWithLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, IMyTerminalBlock muzzle)
        {
            MatrixD refWorldMatrix = muzzle.WorldMatrix;
            //MatrixD refLookAtMatrix = MatrixD.CreateLookAt(Vector3D.Zero, refWorldMatrix.Forward, refWorldMatrix.Up);
            Vector3D aimDirection = GetPredictedTargetPosition(muzzle, CONTROLLER, targetPosition, targetVelocity, projectileSpeed);
            aimDirection -= refWorldMatrix.Translation;
            //aimDirection = Vector3D.Normalize(Vector3D.TransformNormal(aimDirection, refLookAtMatrix));
            return aimDirection;
        }

        Vector3D GetPredictedTargetPosition(IMyTerminalBlock gun, IMyShipController shooter, Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed)
        {
            float shootDelay = 0;

            Vector3D muzzlePosition = gun.GetPosition();

            Vector3D toTarget = targetPosition - muzzlePosition;
            Vector3D shooterVelocity = shooter.GetShipVelocities().LinearVelocity;

            Vector3D diffVelocity = targetVelocity - shooterVelocity;

            float a = (float)diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2 * Vector3.Dot(diffVelocity, toTarget);
            float c = (float)toTarget.LengthSquared();

            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);

            float t1 = p - q;
            float t2 = p + q;
            float t;

            if (t1 > t2 && t2 > 0)
            {
                t = t2;
            }
            else
            {
                t = t1;
            }

            t += shootDelay;

            Vector3D predictedPosition = targetPosition + diffVelocity * t;
            //Vector3 bulletPath = predictedPosition - muzzlePosition;
            //timeToHit = bulletPath.Length() / projectileSpeed;
            return predictedPosition;
        }

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll)
        {
            desiredForwardVector = VectorMath.SafeNormalize(desiredForwardVector);

            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
            {
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            }
            else
            {
                leftVector = VectorMath.SafeNormalize(leftVector);
                Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);

                //Create matrix
                MatrixD targetMatrix = MatrixD.Zero;
                targetMatrix.Forward = desiredForwardVector;
                targetMatrix.Left = leftVector;
                targetMatrix.Up = upVector;

                axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
                                    targetMatrix.M31 - targetMatrix.M13,
                                    targetMatrix.M12 - targetMatrix.M21);

                double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
            }

            if (Vector3D.IsZero(axis))
            {
                angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
                yaw = angle;
                pitch = 0;
                roll = 0;
                return;
            }

            axis = VectorMath.SafeNormalize(axis);
            //Because gyros rotate about -X -Y -Z, we need to negate our angles
            yaw = -axis.Y * angle;
            pitch = -axis.X * angle;
            roll = -axis.Z * angle;
        }

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
            foreach (var thisGyro in gyroList)
            {
                if (thisGyro.Closed)
                    continue;
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        bool GetMessages()
        {
            bool received = false;
            if (UNILISTENER.HasPendingMessage)
            {
                Dictionary<long, string> tempMissileIDs = new Dictionary<long, string>();
                Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
                while (UNILISTENER.HasPendingMessage)
                {
                    var igcMessage = UNILISTENER.AcceptMessage();

                    long missileId = igcMessage.Source;

                    if (igcMessage.Data is ImmutableArray<MyTuple<string, Vector3D, double, double>>)
                    {
                        var data = (ImmutableArray<MyTuple<string, Vector3D, double, double>>)igcMessage.Data;

                        received = true;

                        var tup = MyTuple.Create(data[0].Item4, data[0].Item3, data[0].Item1);
                        if (!tempMissilesInfo.ContainsKey(missileId))
                        {
                            tempMissilesInfo.Add(missileId, tup);
                        }
                        if (!tempMissileIDs.ContainsKey(missileId))
                        {
                            tempMissileIDs.Add(missileId, data[0].Item1);
                        }
                    }

                    if (igcMessage.Data is ImmutableArray<MyTuple<string>>)//error message from missile
                    {
                        var data = (ImmutableArray<MyTuple<string>>)igcMessage.Data;

                        DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                        debugLog = new StringBuilder("");
                        DEBUG.ReadText(debugLog, true);
                        debugLog.Append("\n" + data[0].Item1 + "\n");
                        DEBUG.WriteText(debugLog);
                    }
                }
                //eliminate duplicates by preferring entries from the first dictionary
                missilesInfo = tempMissilesInfo.Concat(missilesInfo.Where(kvp => !tempMissilesInfo.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                MissileIDs = tempMissileIDs.Concat(MissileIDs.Where(kvp => !tempMissileIDs.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            return received;
        }

        void ReadMessages()
        {
            missileLog.Clear();
            if (MissileIDs.Count() > 0)
            {
                missileLog.Append("Active Missiles: ").Append(MissileIDs.Count().ToString()).Append("\n");
            }
            foreach (var inf in missilesInfo)
            {
                missileLog.Append("Status: ").Append(inf.Value.Item3).Append(", Missile ID: ").Append(inf.Key.ToString()).Append("\n");
                if (inf.Value.Item3.Contains(commandUpdate) || inf.Value.Item3.Contains(commandSpiral))
                {
                    missileLog.Append("Dist. From Target: ").Append(inf.Value.Item1.ToString("0.0")).Append(", ");
                }
                missileLog.Append("Speed: ").Append(inf.Value.Item2.ToString("0.0")).Append("\n");
            }
        }

        void SendMissileBroadcastMessage(string cmd)
        {
            Vector3D targetPos = targetPosition;

            long fakeId = 0;
            var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                MyTuple<Vector3, Vector3D>>>();

            var tuple = MyTuple.Create(MyTuple.Create(fakeId, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative),
              MyTuple.Create(targetInfo.Velocity, targetPos));

            immArray.Add(tuple);

            IGC.SendBroadcastMessage(missileAntennaTag, immArray.ToImmutable());
        }

        bool SendMissileUnicastMessage(string cmd, long id)
        {
            Vector3D targetPos = targetPosition;

            var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                MyTuple<Vector3, Vector3D>>>();

            var tuple = MyTuple.Create(MyTuple.Create(id, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative),
                MyTuple.Create(targetInfo.Velocity, targetPos));

            immArray.Add(tuple);

            bool uniMessageSent = IGC.SendUnicastMessage(id, missileAntennaTag, immArray.ToImmutable());

            if (!uniMessageSent)
            {
                LostMissileIDs.Add(id);
            }
            return uniMessageSent;
        }

        void RemoveLostMissiles()
        {
            Dictionary<long, string> TempMissileIDs = new Dictionary<long, string>();
            foreach (var entry in MissileIDs)
            {
                bool found = false;
                foreach (var id in LostMissileIDs)
                {
                    if (entry.Key == id)
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    TempMissileIDs.Add(entry.Key, entry.Value);
                }
            }
            Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
            foreach (var entry in missilesInfo)
            {
                bool found = false;
                foreach (var id in LostMissileIDs)
                {
                    if (entry.Key == id)
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    tempMissilesInfo.Add(entry.Key, entry.Value);
                }
            }
            missilesInfo = tempMissilesInfo;
            MissileIDs = TempMissileIDs;
        }

        void ResetTargeter()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;

            UnlockShip();
            TurnAlarmOff();
            TurnGunsOff();
            ClearLogs();

            targetName = null;
            targetDiameter = 0;
            targetInfo = default(MyDetectedEntityInfo);
            targetPosition = default(Vector3D);

            currentTick = 1;
            lostTicks = 0;
            selectedMissile = 1;
            doOnce = false;
            shootOnce = false;
            farShootOnce = false;
            autoMissilesCounter = autoMissilesDelay + 1;
            missilesLoaded = false;
            fudgeFactor = 5;

            foreach (var id in MissileIDs)
            {
                if (!id.Value.Contains(commandLost))
                {
                    SendMissileUnicastMessage(commandLost, id.Key);
                }
            }
        }

        void ActivateTargeter()
        {
            if (!doOnce)//things to run once when a enemy is detected
            {
                //Runtime.UpdateFrequency = UpdateFrequency.Update1;

                TurnAlarmOn();

                doOnce = true;

                if (MAGNETICDRIVEPB != null)
                {
                    if (MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                    {
                        MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                    }
                }
                if (MANAGERPB != null)
                {
                    if (MANAGERPB.CustomData.Contains("SunChaser=true"))
                    {
                        sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                    }
                }
            }
        }

        void ActivateOtherScriptsGyros()
        {
            if (MDOff && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
            {
                bool mdRun = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOn);
                MDOff = !mdRun;
            }
        }

        void DeactivateOtherScriptsGyros()
        {
            if (!MDOff && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
            {
                MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
            }
            if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true"))
            {
                sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
            }
        }

        void UnlockShip()
        {
            foreach (IMyGyro block in GYROS) { block.GyroOverride = false; block.GyroPower = 1; }
        }

        void TurnAlarmOn()
        {
            foreach (IMySoundBlock block in ALARMS) { block.Play(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = true; }
        }

        void TurnAlarmOff()
        {
            foreach (IMySoundBlock block in ALARMS) { block.Stop(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = false; }
        }

        bool CheckProjectors()
        {
            string selection = "";
            switch (selectedMissile)
            {
                case 1:
                    selection = "R2";
                    break;
                case 2:
                    selection = "L2";
                    break;
            }
            bool completed = false;
            int blocksCount = 1000;
            foreach (IMyProjector block in TEMPPROJECTORS)
            {
                if (block.CustomName.Contains(selection))
                {
                    blocksCount = block.BuildableBlocksCount;//RemainingBlocks;
                }
            }
            if (blocksCount == 0)
            {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = false; }
                completed = true;
            }
            else
            {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = true; }
            }
            return completed;
        }

        void CheckGunsAmmo()
        {
            if (creative)
            {
                gatlingAmmoFound = true;
                missileAmmoFound = true;
            }
            else
            {
                gatlingAmmoFound = false;
                missileAmmoFound = false;
                List<IMyInventory> ROCKETSINVENTORIES = new List<IMyInventory>();
                ROCKETSINVENTORIES.AddRange(ROCKETS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                foreach (IMyInventory sourceInventory in ROCKETSINVENTORIES)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    sourceInventory.GetItems(items, item => item.Type.TypeId == missileAmmo.TypeId.ToString());
                    if (items.Count > 0)
                    {
                        missileAmmoFound = true;
                        break;
                    }
                }
                List<IMyInventory> GATLINGSINVENTORIES = new List<IMyInventory>();
                GATLINGSINVENTORIES.AddRange(GATLINGS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                foreach (IMyInventory sourceInventory in GATLINGSINVENTORIES)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    sourceInventory.GetItems(items, item => item.Type.TypeId == gatlingAmmo.TypeId.ToString());
                    if (items.Count > 0)
                    {
                        gatlingAmmoFound = true;
                        break;
                    }
                }
            }
            if (useAllGuns)
            {
                rocketProjectileMaxRange = 500d;
            }
            else
            {
                rocketProjectileMaxRange = 800d;
            }
            if (!gatlingAmmoFound)
            {
                rocketProjectileMaxRange = 800d;
                weaponType = 1;
            }
            if (!missileAmmoFound)
            {
                weaponType = 2;
            }
        }

        void ManageGuns()
        {
            if (autoFire)
            {
                if (readyToFire)
                {
                    readyToFireOnce = true;
                    double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                    if (distanceFromTarget < gunsMaxRange)
                    {
                        if (useAllGuns)
                        {
                            if (distanceFromTarget < rocketProjectileMaxRange && missileAmmoFound)
                            {
                                if (!shootOnce)
                                {
                                    weaponType = 1;
                                    shootOnce = true;
                                    farShootOnce = false;
                                    sequenceWeapons = true;
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                                }
                                if (sequenceWeapons) { SequenceWeapons(ROCKETS); }
                            }
                            else if (distanceFromTarget < gatlingProjectileMaxRange && gatlingAmmoFound)
                            {
                                if (!farShootOnce)
                                {
                                    weaponType = 2;
                                    farShootOnce = true;
                                    shootOnce = false;
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                                    foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                                }
                            }
                            else if (shootOnce || farShootOnce)
                            {
                                foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                                foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                                shootOnce = false;
                                farShootOnce = false;
                                sequenceWeapons = false;
                            }
                        }
                        else
                        {
                            if (weaponType == 1 && distanceFromTarget < rocketProjectileMaxRange && missileAmmoFound)
                            {
                                if (!shootOnce)
                                {
                                    shootOnce = true;
                                    farShootOnce = false;
                                    sequenceWeapons = true;
                                }
                                if (sequenceWeapons) { SequenceWeapons(ROCKETS); }
                            }
                            else if (weaponType == 2 && distanceFromTarget < gatlingProjectileMaxRange && gatlingAmmoFound)
                            {
                                if (!farShootOnce)
                                {
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                                    farShootOnce = true;
                                    shootOnce = false;
                                }
                            }
                            else if (shootOnce || farShootOnce)
                            {
                                foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                                foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                                shootOnce = false;
                                farShootOnce = false;
                                sequenceWeapons = false;
                            }
                        }
                    }
                    else
                    {
                        if (shootOnce || farShootOnce)
                        {
                            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                            foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                            shootOnce = false;
                            farShootOnce = false;
                            sequenceWeapons = false;
                        }
                    }
                }
                else
                {
                    if (readyToFireOnce)
                    {
                        readyToFireOnce = false;
                        foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                        foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                        shootOnce = false;
                        farShootOnce = false;
                        sequenceWeapons = false;
                    }
                }
            }
        }

        void SequenceWeapons(List<IMyUserControllableGun> weapons)
        {
            if (fireCount == fireDelay)
            {
                weapons[weaponIndex].ShootOnce();
                fireCount = 0;
            }
            weaponIndex++;
            if (weaponIndex > weapons.Count) { weaponIndex = 0; }
            fireCount++;
        }

        void TurnGunsOff()
        {
            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
            foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
        }

        bool LoadMissiles()//meant to load missiles or drones with hidrogen thrusters and gatling guns/turrets
        {
            List<IMyShipConnector> MISSILECONNECTORS = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(MISSILECONNECTORS, b => b.CustomName.Contains(missilePrefix));
            if (MISSILECONNECTORS.Count == 0) { return false; }
            foreach (IMyShipConnector block in MISSILECONNECTORS) { block.Connect(); }

            bool allFilled = false;
            if (creative)
            {
                allFilled = true;
            }
            else
            {
                List<IMyTerminalBlock> MISSILEBLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
                List<IMyInventory> MISSILEINVENTORIES = new List<IMyInventory>();
                if (selectedPayLoad == 0)//missiles
                {
                    MISSILEBLOCKSWITHINVENTORY.Clear();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(missilePrefix) && block is IMyGasGenerator);
                    MISSILEINVENTORIES.Clear();
                    MISSILEINVENTORIES.AddRange(MISSILEBLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                    int loaded = 0;
                    foreach (IMyInventory inventory in MISSILEINVENTORIES)
                    {
                        MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                        if (inventory.CanItemsBeAdded(availableVolume, iceOre))
                        {
                            TransferItems(inventory, iceOre);
                        }
                        double currentVolume = (double)inventory.CurrentVolume;
                        double maxVolume = (double)inventory.MaxVolume;
                        double percent = 0;
                        if (maxVolume > 0 && currentVolume > 0)
                        {
                            percent = currentVolume / maxVolume * 100;
                        }
                        if (percent >= 95)
                        {
                            loaded++;
                        }
                    }
                    if (loaded == MISSILEINVENTORIES.Count)
                    {
                        allFilled = true;
                    }
                }
                else if (selectedPayLoad == 1)//drones
                {
                    MISSILEBLOCKSWITHINVENTORY.Clear();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(missilePrefix) && (block is IMyGasGenerator || block is IMySmallGatlingGun || block is IMyLargeGatlingTurret));//&& !(block is IMyGasTank)); IMyLargeTurretBase
                    int inventoriesCount = 0;
                    int loaded = 0;
                    foreach (IMyTerminalBlock block in MISSILEBLOCKSWITHINVENTORY)
                    {
                        if (block is IMyGasGenerator)
                        {
                            MISSILEINVENTORIES.Clear();
                            MISSILEINVENTORIES.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                            inventoriesCount += MISSILEINVENTORIES.Count;
                            foreach (IMyInventory inventory in MISSILEINVENTORIES)
                            {
                                MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                                if (inventory.CanItemsBeAdded(availableVolume, iceOre))
                                {
                                    TransferItems(inventory, iceOre);
                                }
                                double currentVolume = (double)inventory.CurrentVolume;
                                double maxVolume = (double)inventory.MaxVolume;
                                double percent = 0;
                                if (maxVolume > 0 && currentVolume > 0)
                                {
                                    percent = currentVolume / maxVolume * 100;
                                }
                                if (percent >= 95)
                                {
                                    loaded++;
                                }
                            }
                        }
                        else if (block is IMySmallGatlingGun || block is IMyLargeGatlingTurret)
                        {
                            MISSILEINVENTORIES.Clear();
                            MISSILEINVENTORIES.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                            inventoriesCount += MISSILEINVENTORIES.Count;
                            foreach (IMyInventory inventory in MISSILEINVENTORIES)
                            {
                                MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                                if (inventory.CanItemsBeAdded(availableVolume, gatlingAmmo))
                                {
                                    TransferItems(inventory, gatlingAmmo);
                                }
                                double currentVolume = (double)inventory.CurrentVolume;
                                double maxVolume = (double)inventory.MaxVolume;
                                double percent = 0;
                                if (maxVolume > 0 && currentVolume > 0)
                                {
                                    percent = currentVolume / maxVolume * 100;
                                }
                                if (percent >= 75)
                                {
                                    loaded++;
                                }
                            }
                        }
                    }
                    if (loaded == inventoriesCount)
                    {
                        allFilled = true;
                    }
                }
            }
            return allFilled;
        }

        bool TransferItems(IMyInventory inventory, MyItemType itemToFind)
        {
            bool transferred = false;
            foreach (IMyInventory sourceInventory in INVENTORIES)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                sourceInventory.GetItems(items, item => item.Type.TypeId == itemToFind.TypeId.ToString());
                foreach (MyInventoryItem item in items)
                {
                    transferred = false;
                    if (sourceInventory.CanTransferItemTo(inventory, item.Type) && inventory.CanItemsBeAdded(item.Amount, item.Type)) { transferred = sourceInventory.TransferItemTo(inventory, item); }
                    if (!transferred)
                    {
                        MyFixedPoint amount = inventory.MaxVolume - inventory.CurrentVolume;
                        transferred = sourceInventory.TransferItemTo(inventory, item, amount);
                    }
                    if (!transferred) { sourceInventory.TransferItemTo(inventory, item, item.Amount); }
                    if (transferred) { break; }
                }
            }
            return transferred;
        }

        void WriteInfo()
        {
            foreach (IMyTextSurface surface in SURFACES)
            {
                StringBuilder text = new StringBuilder("");
                string gun;
                switch (weaponType)
                {
                    case 0: gun = "None"; break;
                    case 1: gun = "Rockets"; break;
                    case 2: gun = "Gatlings"; break;
                    default: gun = "None"; break;
                }
                text.Append("Guns: " + gun + ", ");
                text.Append("autoFire: " + autoFire + ", useAllGuns: " + useAllGuns + "\n");
                text.Append("PayLoad: " + (selectedPayLoad == 0 ? "Missiles" : "Drones") + ", autoMissiles: " + autoMissiles + "\n");
                text.Append(targetLog.ToString());
                text.Append(missileLog.ToString());
                surface.WriteText(text);
            }
        }

        void ReadTargetInfo()
        {
            if (currentTick == ticksScanDelay)
            {
                targetLog.Clear();
                targetLog.Append("Launching: ").Append(missileAntennasName).Append(selectedMissile.ToString());//.Append("\n");
                if (!missilesLoaded)
                {
                    targetLog.Append(" Not Loaded\n");
                }
                else
                {
                    targetLog.Append(" Loaded\n");
                }

                //targetLog.Append("ID: ").Append(targetInfo.EntityId.ToString()).Append("\n");
                targetLog.Append("Target Name: ").Append(targetInfo.Name).Append(", ");

                string targX = targetInfo.Position.X.ToString("0.00");
                string targY = targetInfo.Position.Y.ToString("0.00");
                string targZ = targetInfo.Position.Z.ToString("0.00");
                targetLog.Append($"X:{targX} Y:{targY} Z:{targZ}").Append("\n");

                //long targetLastDetected = targetInfo.TimeStamp / 1000;
                //targetLog.Append("Detected Since: ").Append(targetLastDetected).Append(" s\n");

                targetLog.Append("Target Speed: ").Append(targetInfo.Velocity.Length().ToString("0.0")).Append(", ");

                double targetDistance = Vector3D.Distance(targetInfo.Position, CONTROLLER.CubeGrid.WorldVolume.Center);
                targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");

                //double targetRadius = Vector3D.Distance(targetInfo.BoundingBox.Min, targetInfo.BoundingBox.Max);
                //targetLog.Append("Radius: ").Append(targetRadius.ToString("0.0")).Append("\n");
            }
        }

        void ClearLogs()
        {
            targetLog.Clear();
            missileLog.Clear();
            foreach (IMyTextSurface surface in SURFACES) { surface.WriteText(""); }
        }

        void GetBlocks()
        {
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, b => b.CustomName.Contains(lidarsName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CustomName.Contains(controllersName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(turretsName));
            LIGHTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(LIGHTS, b => b.CustomName.Contains(lightsName));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, b => b.CustomName.Contains(alarmsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, b => b.CustomName.Contains(gyrosName));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, b => b.CustomName.Contains(weldersName));
            PROJECTORSMISSILES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSMISSILES, b => b.CustomName.Contains(projectorsMissilesName));
            PROJECTORSDRONES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDRONES, b => b.CustomName.Contains(projectorsDronesName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, b => b.CustomName.Contains(cockpitsName));
            ROCKETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ROCKETS, b => b.CustomName.Contains(rocketsName));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, b => b.CustomName.Contains(gatlingsName));
            CARGOS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CARGOS, b => b.CustomName.Contains(cargoName));
            INVENTORIES.Clear();
            INVENTORIES.AddRange(CARGOS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsTargetName));
            foreach (IMyTextPanel panel in panels) { SURFACES.Add(panel as IMyTextSurface); }
            ANTENNA = GridTerminalSystem.GetBlockWithName(antennasName) as IMyRadioAntenna;
            CONTROLLER = CONTROLLERS[0];
            MAGNETICDRIVEPB = GridTerminalSystem.GetBlockWithName(magneticDriveName) as IMyProgrammableBlock;
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
            DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
        }

        void SetBlocks()
        {
            foreach (IMyCameraBlock cam in LIDARS)
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }

            foreach (IMyGyro block in GYROS)
            {
                block.Enabled = true;
                block.Yaw = 0f;
                block.Pitch = 0f;
                block.Roll = 0f;
                block.GyroOverride = false;
                block.GyroPower = 1;
            }

            ANTENNA.Enabled = true;
            ANTENNA.EnableBroadcasting = true;
        }

        void GetMissileAntennas()
        {
            MISSILEANTENNAS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(MISSILEANTENNAS, b => b.CustomName.Contains(missileAntennasName));
        }

        void SetMissileAntennas()
        {
            foreach (IMyRadioAntenna block in MISSILEANTENNAS)
            {
                block.EnableBroadcasting = false;
                block.Enabled = false;
            }
        }

        public static class VectorMath
        {
            public static Vector3D SafeNormalize(Vector3D a)
            {
                if (Vector3D.IsZero(a))
                    return Vector3D.Zero;

                if (Vector3D.IsUnit(ref a))
                    return a;

                return Vector3D.Normalize(a);
            }

            public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1)//reflect a over b
            {
                Vector3D project_a = Projection(a, b);
                Vector3D reject_a = a - project_a;
                return project_a - reject_a * rejectionFactor;
            }

            public static Vector3D Rejection(Vector3D a, Vector3D b)//reject a on b
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a - a.Dot(b) / b.LengthSquared() * b;
            }

            public static Vector3D Projection(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a.Dot(b) / b.LengthSquared() * b;
            }

            public static double ScalarProjection(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;

                if (Vector3D.IsUnit(ref b))
                    return a.Dot(b);

                return a.Dot(b) / b.Length();
            }

            public static double AngleBetween(Vector3D a, Vector3D b)//returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }

            public static double CosBetween(Vector3D a, Vector3D b)//returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
            }
        }

        void InitPIDControllers()
        {
            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
        }

        public class PID
        {
            public double _kP = 0;
            public double _kI = 0;
            public double _kD = 0;
            public double _integralDecayRatio = 0;
            public double _lowerBound = 0;
            public double _upperBound = 0;
            double _timeStep = 0;
            double _inverseTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;
            public bool _integralDecay = false;
            public double Value { get; private set; }

            public PID(double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _lowerBound = lowerBound;
                _upperBound = upperBound;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecay = false;
            }

            public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control(double error)
            {
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun)
                {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Compute integral term
                if (!_integralDecay)
                {
                    _errorSum += error * _timeStep;

                    //Clamp integral term
                    if (_errorSum > _upperBound)
                        _errorSum = _upperBound;
                    else if (_errorSum < _lowerBound)
                        _errorSum = _lowerBound;
                }
                else
                {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
                return this.Value;
            }

            public double Control(double error, double timeStep)
            {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                return Control(error);
            }

            public void Reset()
            {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

    }
}
