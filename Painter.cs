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

namespace IngameScript {
    partial class Program : MyGridProgram {
        //TODO
        //check speed and roll be4 detaching the decoy or launch the missile
        //PAINTER
        readonly int missilesCount = 2;
        bool creative = true;//define if is playing creative mode or not
        bool sequenceWeapons = false;//enable/disable guns sequencing
        bool autoFire = true;//enable/disable automatic fire
        bool autoMissiles = false;//enable/disable automatic missiles launch
        bool autoSwitchGuns = true;//enable/disable automatic guns switch, depending on range etc.
        bool logger = true;//enable/disable logging

        int weaponType = 2;//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
        int selectedPayLoad = 0;//0 Missiles - 1 Drones
        int rocketDelay = 1;
        int assaultDelay = 1;
        int artilleryDelay = 1;
        int railgunsDelay = 1;
        int smallRailgunsDelay = 1;
        int selectedMissile = 1;
        int autoMissilesCounter = 0;
        int checkGunsCount = 0;
        int missedScan = 0;
        int rocketCount = 0;
        int rocketIndex = 0;
        int assaultCount = 0;
        int assaultIndex = 0;
        int artilleryCount = 0;
        int artilleryIndex = 0;
        int railgunsCount = 0;
        int railgunsIndex = 0;
        int smallRailgunsCount = 0;
        int smallRailgunsIndex = 0;
        int sendMessageCount = 0;
        double fudgeFactor = 5d;
        double timeSinceLastLock = 0d;
        bool hasCenter = true;
        bool scanCenter = false;
        bool scanFudge = false;
        bool joltReady = true;
        bool missilesLoaded = false;
        bool fudgeVectorSwitch = false;
        bool readyToFire = false;
        bool gatlingAmmoFound = true;
        bool missileAmmoFound = true;
        bool artilleryAmmoFound = true;
        bool railgunAmmoFound = true;
        bool smallRailgunAmmoFound = true;
        bool assaultAmmoFound = true;
        bool autocannonAmmoFound = true;
        bool rocketsCanShoot = true;
        bool assaultCanShoot = true;
        bool artilleryCanShoot = true;
        bool railgunsCanShoot = true;
        bool smallRailgunsCanShoot = true;
        bool decoyRanOnce = false;
        bool activateOnce = false;
        bool gatlingsOnce = false;
        bool readyToFireOnce = true;
        bool maxRangeOnce = false;
        bool cannotFireOnce = true;
        bool rocketsOnce = false;
        bool artilleryOnce = false;
        bool railgunsOnce = false;
        bool smallRailgunsOnce = false;
        bool assaultOnce = false;
        bool autocannonOnce = false;
        bool updateOnce = true;

        MyDetectedEntityInfo targetInfo;
        readonly Random random = new Random();

        const float globalTimestep = 10.0f / 60.0f;
        const double rad2deg = 180 / Math.PI;

        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyLightingBlock> LIGHTS = new List<IMyLightingBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();
        public List<IMyProjector> PROJECTORSDRONES = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSMISSILES = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyRadioAntenna> MISSILEANTENNAS = new List<IMyRadioAntenna>();
        public List<IMyUserControllableGun> GATLINGS = new List<IMyUserControllableGun>();
        public List<IMyUserControllableGun> AUTOCANNONS = new List<IMyUserControllableGun>();
        public List<Gun> ROCKETS = new List<Gun>();
        public List<Gun> ARTILLERY = new List<Gun>();
        public List<Gun> RAILGUNS = new List<Gun>();
        public List<Gun> SMALLRAILGUNS = new List<Gun>();
        public List<Gun> ASSAULT = new List<Gun>();
        public List<IMyCargoContainer> CARGOS = new List<IMyCargoContainer>();
        public List<IMyInventory> CARGOINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> inventories = new List<IMyInventory>();

        IMyShipController CONTROLLER;
        IMyRadioAntenna ANTENNA;
        IMyProgrammableBlock SHOOTERPB;
        IMyTextPanel DEBUG;
        IMyTextPanel LCDAUTOSWITCHGUNS;
        IMyTextPanel LCDAUTOFIRE;
        IMyTextPanel LCDAUTOMISSILES;
        IMyTextPanel LCDCREATIVE;
        IMyTextPanel LCDSEQUENCEGUNS;

        IMyUnicastListener UNILISTENER;
        public IMyBroadcastListener BROADCASTLISTENER;

        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType autocannonAmmo = MyItemType.MakeAmmo("AutocannonClip");
        readonly MyItemType assaultAmmo = MyItemType.MakeAmmo("MediumCalibreAmmo");
        readonly MyItemType artilleryAmmo = MyItemType.MakeAmmo("LargeCalibreAmmo");
        readonly MyItemType railgunAmmo = MyItemType.MakeAmmo("LargeRailgunAmmo");
        readonly MyItemType smallRailgunAmmo = MyItemType.MakeAmmo("SmallRailgunAmmo");
        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");

        Dictionary<long, string> MissileIDs = new Dictionary<long, string>();
        public List<double> LostMissileIDs = new List<double>();
        Dictionary<long, MyTuple<double, double, string>> missilesInfo = new Dictionary<long, MyTuple<double, double, string>>();

        public StringBuilder debugLog = new StringBuilder("");

        PID yawController;
        PID pitchController;
        PID rollController;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            UNILISTENER = IGC.UnicastListener;
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[PAINTER]");
            GetBlocks();
            SetBlocks();
            GetMissileAntennas();
            SetMissileAntennas();
            InitPIDControllers();
            if (selectedPayLoad == 0) {
                TEMPPROJECTORS = PROJECTORSMISSILES;
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
            } else if (selectedPayLoad == 1) {
                TEMPPROJECTORS = PROJECTORSDRONES;
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
            }
            autoMissilesCounter = 9 + 1;
            SetGunsDelay();
            if (LCDAUTOSWITCHGUNS != null) { LCDAUTOSWITCHGUNS.BackgroundColor = autoSwitchGuns ? new Color(0, 0, 50) : new Color(0, 0, 0); }
            if (LCDAUTOFIRE != null) { LCDAUTOFIRE.BackgroundColor = autoFire ? new Color(0, 0, 50) : new Color(0, 0, 0); }
            if (LCDAUTOMISSILES != null) { LCDAUTOMISSILES.BackgroundColor = autoMissiles ? new Color(0, 0, 50) : new Color(0, 0, 0); }
            if (LCDCREATIVE != null) { LCDCREATIVE.BackgroundColor = creative ? new Color(0, 0, 50) : new Color(0, 0, 0); }
            if (LCDSEQUENCEGUNS != null) { LCDSEQUENCEGUNS.BackgroundColor = sequenceWeapons ? new Color(0, 0, 50) : new Color(0, 0, 0); }
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                RemoveLostMissiles();
                GetBroadcastMessages();
                GetMessages();

                double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;

                if (!String.IsNullOrEmpty(arg)) { ProcessArgs(arg); }

                bool targetFound = TurretsDetection(targetInfo.IsEmpty());
                bool isTargetEmpty = targetInfo.IsEmpty();

                if (!isTargetEmpty) {
                    if (missedScan > 8) {//if lidars or turrets doesn't detect a enemy for some time reset the script
                        ResetTargeter();
                        return;
                    }

                    ActivateTargeter();//things to run once when a enemy is detected

                    double lastLock = timeSinceLastLock + timeSinceLastRun;
                    Vector3D targetVelocity = targetInfo.Velocity;

                    Vector3D aimDirection = Vector3D.Normalize(targetInfo.Position - CONTROLLER.CubeGrid.WorldVolume.Center);
                    if (AngleBetween(CONTROLLER.WorldMatrix.Forward, aimDirection) * rad2deg <= 43d) {
                        if (!targetFound) {
                            targetFound = AcquireTarget(lastLock, targetInfo.Position, targetVelocity, targetInfo.HitPosition.Value);
                        }
                    }

                    IMyTerminalBlock refBlock;
                    if (LIDARS.Count != 0) { refBlock = LIDARS[0]; } else { refBlock = CONTROLLER; }
                    LockOnTarget(lastLock, targetInfo.HitPosition.Value, targetVelocity, CONTROLLER.GetNaturalGravity(), CONTROLLER.GetShipVelocities().LinearVelocity, refBlock.GetPosition());

                    SendBroadcastTargetMessage(true, targetInfo.HitPosition.Value, targetVelocity, targetInfo.Orientation, targetInfo.Position, targetInfo.EntityId);

                    foreach (KeyValuePair<long, string> id in MissileIDs) {
                        SendMissileUnicastMessage("Update", id.Key, targetInfo.HitPosition.Value, targetVelocity);
                    }

                    if (autoMissiles) {
                        if (autoMissilesCounter > 9) {
                            LaunchMissile();
                            autoMissilesCounter = 0;
                        }
                        autoMissilesCounter++;
                    }

                    CanShootGuns();

                    if (checkGunsCount >= 10) {
                        CheckGunsAmmo();
                        checkGunsCount = 0;
                    }
                    checkGunsCount++;

                    ManageGuns(timeSinceLastRun, targetInfo.Position);

                    if (targetFound) {
                        timeSinceLastLock = 0;
                        missedScan = 0;
                    } else {
                        timeSinceLastLock += timeSinceLastRun;
                        missedScan++;
                    }
                }

                ManagePIDControllers(isTargetEmpty);

                SyncGuns(timeSinceLastRun);

                bool completed = CheckProjectors();
                if (!completed) {
                    missilesLoaded = false;
                } else {
                    if (!missilesLoaded) {
                        missilesLoaded = LoadMissiles();
                    }
                }

                if (logger) {
                    if (sendMessageCount >= 10) {
                        SendBroadcastLogMessage();
                        sendMessageCount = 0;
                    }
                    sendMessageCount++;
                }
            } catch (Exception e) {
                DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                debugLog = new StringBuilder("");
                debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                DEBUG.WriteText(debugLog);
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgs(string arg) {
            switch (arg) {
                case "Lock": AcquireTarget(globalTimestep, Vector3D.Zero, Vector3D.Zero, Vector3D.Zero); break;
                case "Launch":
                    LaunchMissile();
                    break;
                case "Clear":
                    ResetTargeter();
                    return;
                //break;
                case "SwitchGun":
                    weaponType++;
                    if (weaponType > 4) {
                        weaponType = 0;
                    }
                    break;
                case "SwitchPayLoad":
                    int blockCount = 0;
                    foreach (IMyProjector proj in TEMPPROJECTORS) {
                        blockCount += proj.BuildableBlocksCount;//RemainingBlocks;
                    }
                    if (blockCount == 0) {
                        if (selectedPayLoad == 1) {
                            selectedPayLoad = 0;
                            TEMPPROJECTORS = PROJECTORSMISSILES;
                            foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
                        } else if (selectedPayLoad == 0) {
                            selectedPayLoad = 1;
                            TEMPPROJECTORS = PROJECTORSDRONES;
                            foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
                        }
                    }
                    break;
                case "Spiral":
                    if (!targetInfo.IsEmpty()) {
                        foreach (KeyValuePair<long, string> id in MissileIDs) {
                            if (id.Value.Contains("Update") && !id.Value.Contains("Drone")) {
                                SendMissileUnicastMessage("Spiral", id.Key, targetInfo.HitPosition.Value, targetInfo.Velocity);
                            }
                        }
                    }
                    break;
                case "ToggleAutoSwitchGuns":
                    autoSwitchGuns = !autoSwitchGuns;
                    if (LCDAUTOSWITCHGUNS != null) { LCDAUTOSWITCHGUNS.BackgroundColor = autoSwitchGuns ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "ToggleAutoFire":
                    autoFire = !autoFire;
                    if (LCDAUTOFIRE != null) { LCDAUTOFIRE.BackgroundColor = autoFire ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "ToggleAutoMissiles":
                    autoMissiles = !autoMissiles;
                    if (LCDAUTOMISSILES != null) { LCDAUTOMISSILES.BackgroundColor = autoMissiles ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "ToggleCreative":
                    creative = !creative;
                    if (LCDCREATIVE != null) { LCDCREATIVE.BackgroundColor = creative ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "ToggleSequenceGuns":
                    sequenceWeapons = !sequenceWeapons;
                    if (LCDSEQUENCEGUNS != null) { LCDSEQUENCEGUNS.BackgroundColor = sequenceWeapons ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "ToggleLogger":
                    logger = !logger;
                    break;
                case "LoggerOn":
                    logger = true;
                    break;
                case "LoggerOff":
                    logger = false;
                    break;
            }
        }

        bool TurretsDetection(bool isTargetEmpty) {
            bool targetFound = false;
            if (!isTargetEmpty) {
                foreach (IMyLargeTurretBase turret in TURRETS) {
                    MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                    if (!targ.IsEmpty()) {
                        if (targ.EntityId == targetInfo.EntityId) {
                            targetInfo = targ;
                            targetFound = true;
                            break;
                        }
                    }
                }
            } else {
                foreach (IMyLargeTurretBase turret in TURRETS) {
                    MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                    if (!targ.IsEmpty()) {
                        if (IsValidLidarTarget(ref targ)) {
                            targetInfo = targ;
                            targetFound = true;
                            break;
                        }
                    }
                }
            }
            return targetFound;
        }

        bool AcquireTarget(double timeSinceLastRun, Vector3D trgPstn, Vector3D trgVl, Vector3D trgHitPstn) {
            bool targetFound = false;
            if (Vector3D.IsZero(trgHitPstn)) {//case argLock
                targetFound = ScanTarget(Vector3D.Zero, Vector3D.Zero, 0d);
            } else {
                if (scanCenter && hasCenter) {
                    targetFound = ScanDelayedTarget(trgPstn, timeSinceLastRun, trgVl);
                    if (!targetFound) {
                        hasCenter = false;
                    }
                } else {
                    if (!scanFudge) {
                        targetFound = ScanDelayedTarget(trgHitPstn, timeSinceLastRun, trgVl);
                        scanCenter = true;
                        if (!targetFound) {
                            scanFudge = true;
                        }
                    }
                }
                if (!hasCenter && !targetFound && scanFudge) {
                    targetFound = ScanFudgeDelayedTarget(timeSinceLastRun, trgHitPstn, trgVl);
                    if (targetFound) {
                        scanFudge = false;
                    }
                }
            }
            return targetFound;
        }

        bool ScanTarget(Vector3D trgPstn, Vector3D trgVl, double timeSinceLastRun) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            MyDetectedEntityInfo entityInfo;
            if (!Vector3D.IsZero(trgPstn)) {
                trgPstn += (Vector3D.Normalize(trgVl) * timeSinceLastRun);
                entityInfo = lidar.Raycast(trgPstn);
            } else {
                double scanDistance = 10000d;
                if (lidar.AvailableScanRange < scanDistance) { scanDistance = lidar.AvailableScanRange; }
                entityInfo = lidar.Raycast(scanDistance);
            }
            if (!entityInfo.IsEmpty() && entityInfo.HitPosition.HasValue) {
                if (IsValidLidarTarget(ref entityInfo)) {
                    if (!targetInfo.IsEmpty()) {
                        if (entityInfo.EntityId == targetInfo.EntityId) {
                            targetInfo = entityInfo;
                            targetFound = true;
                        }
                    } else {
                        targetInfo = entityInfo;
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool ScanDelayedTarget(Vector3D trgtPos, double timeSinceLastRun, Vector3D trgtVel) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            Vector3D targetPos = trgtPos + (trgtVel * (float)timeSinceLastRun);
            Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * 200d);
            double dist = Vector3D.Distance(testTargetPosition, lidar.GetPosition());
            if (lidar.CanScan(dist)) {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(testTargetPosition);
                if (!entityInfo.IsEmpty() && entityInfo.HitPosition.HasValue) {
                    if (entityInfo.EntityId == targetInfo.EntityId) {
                        targetInfo = entityInfo;
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool ScanFudgeDelayedTarget(double timeSinceLastRun, Vector3D trgtPos, Vector3D trgtVel) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            Vector3D scanPosition = trgtPos + trgtVel * (float)timeSinceLastRun;
            scanPosition += CalculateFudgeVector(scanPosition - lidar.GetPosition(), timeSinceLastRun);
            scanPosition += Vector3D.Normalize(scanPosition - lidar.GetPosition()) * 200d;
            double dist = Vector3D.Distance(scanPosition, lidar.GetPosition());
            if (lidar.CanScan(dist)) {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(scanPosition);
                if (!entityInfo.IsEmpty() && entityInfo.HitPosition.HasValue) {
                    if (entityInfo.EntityId == targetInfo.EntityId) {
                        targetInfo = entityInfo;
                        targetFound = true;
                    }
                }
            }
            if (!targetFound) {
                fudgeFactor++;
            } else {
                fudgeFactor = 5;
            }
            return targetFound;
        }

        Vector3D CalculateFudgeVector(Vector3D targetDirection, double timeSinceLastRun) {
            fudgeVectorSwitch = !fudgeVectorSwitch;
            if (!fudgeVectorSwitch) {
                return Vector3D.Zero;
            }
            Vector3D perpVector1 = Vector3D.CalculatePerpendicularVector(targetDirection);
            Vector3D perpVector2 = Vector3D.Cross(perpVector1, targetDirection);
            if (!Vector3D.IsUnit(ref perpVector2)) {
                perpVector2.Normalize();
            }
            Vector3D randomVector = (2.0 * random.NextDouble() - 1.0) * perpVector1 + (2.0 * random.NextDouble() - 1.0) * perpVector2;
            return randomVector * fudgeFactor * (float)timeSinceLastRun;
        }

        IMyCameraBlock GetCameraWithMaxRange(List<IMyCameraBlock> cameraList) {
            double maxRange = 0d;
            IMyCameraBlock maxRangeCamera = null;
            foreach (IMyCameraBlock thisCamera in cameraList) {
                if (thisCamera.AvailableScanRange > maxRange) {
                    maxRangeCamera = thisCamera;
                    maxRange = maxRangeCamera.AvailableScanRange;
                }
            }
            return maxRangeCamera;
        }

        bool IsValidLidarTarget(ref MyDetectedEntityInfo entityInfo) {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid) {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) {
                    return true;
                }
            }
            return false;
        }

        void LockOnTarget(double timeSinceLastRun, Vector3D targetHitPosition, Vector3D targetVelocity, Vector3D gravity, Vector3D myVelocity, Vector3D refPosition) {
            Vector3D targetHitPos = targetHitPosition + (targetVelocity * (float)timeSinceLastRun);
            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetHitPos, refPosition);
            if (distanceFromTarget > 2000d) {
                aimDirection = ComputeLeading(targetHitPos, targetVelocity, 958.21f, refPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) {
                    aimDirection = BulletDrop(distanceFromTarget, 958.21f, aimDirection, gravity);
                }
            } else {
                switch (weaponType) {
                    case 0://Jolt
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 958.21f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 958.21f, aimDirection, gravity);
                        }
                        break;
                    case 1://Rockets
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 200f, refPosition, myVelocity);
                        break;
                    case 2://Gatlings
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 400f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 400f, aimDirection, gravity);
                        }
                        break;
                    case 3://Autocannon
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 400f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 400f, aimDirection, gravity);
                        }
                        break;
                    case 4://Assault
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 500f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 500f, aimDirection, gravity);
                        }
                        break;
                    case 5://Artillery
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 500f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 500f, aimDirection, gravity);
                        }
                        break;
                    case 6://Railguns
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 2000f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 2000f, aimDirection, gravity);
                        }
                        break;
                    case 7://Small Railguns
                        aimDirection = ComputeLeading(targetHitPos, targetVelocity, 1000f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 1000f, aimDirection, gravity);
                        }
                        break;
                    default://none
                        aimDirection = targetHitPos - refPosition;//get normalized later
                        break;
                }
            }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double userRoll = 0;
            if (CONTROLLER.IsUnderControl) {
                userRoll = (double)CONTROLLER.RollIndicator;
            }
            //else if (REMOTE.IsUnderControl) { userRoll = (double)REMOTE.RollIndicator; }//TODO
            if (userRoll != 0d) {
                userRoll = userRoll < 0d ? MathHelper.Clamp(userRoll, -10d, -2d) : MathHelper.Clamp(userRoll, 2d, 10d);
            }
            if (userRoll == 0d) {
                userRoll = rollController.Control(rollAngle);
            } else {
                userRoll = rollController.Control(userRoll);
            }
            ApplyGyroOverride(pitchSpeed, yawSpeed, userRoll, GYROS, CONTROLLER.WorldMatrix);
            Vector3D forwardVec = CONTROLLER.WorldMatrix.Forward;
            double angle = AngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= 1d) {
                readyToFire = true;
            } else {
                readyToFire = false;
            }
        }

        Vector3D ComputeLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D refPosition, Vector3D myVelocity) {
            Vector3D aimPosition = PredictTargetPosition(refPosition, targetPosition, targetVelocity, projectileSpeed, myVelocity);
            return aimPosition - CONTROLLER.CubeGrid.WorldVolume.Center;//normalize?
        }

        Vector3D PredictTargetPosition(Vector3D refPosition, Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D myVelocity) {
            Vector3D toTarget = targetPosition - refPosition;//normalize?
            Vector3D diffVelocity = targetVelocity - myVelocity;
            float a = (float)diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2f * (float)Vector3D.Dot(diffVelocity, toTarget);
            float c = (float)toTarget.LengthSquared();
            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);
            float t1 = p - q;
            float t2 = p + q;
            float t;
            if (t1 > t2 && t2 > 0) { t = t2; } else { t = t1; }
            Vector3D predictedPosition = targetPosition + diffVelocity * t;
            return predictedPosition;
        }

        Vector3D BulletDrop(double distanceFromTarget, double projectileMaxSpeed, Vector3D desiredDirection, Vector3D gravity) {
            double timeToTarget = distanceFromTarget / projectileMaxSpeed;
            desiredDirection -= 0.5 * gravity * timeToTarget * timeToTarget;
            return desiredDirection;
        }

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll) {
            desiredForwardVector = SafeNormalize(desiredForwardVector);

            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;

            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector)) {
                axis = new Vector3D(-desiredForwardVector.Y, desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            } else {
                leftVector = SafeNormalize(leftVector);
                Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);
                MatrixD targetOrientation = new MatrixD() {
                    Forward = desiredForwardVector,
                    Left = leftVector,
                    Up = upVector,
                };

                axis = new Vector3D(targetOrientation.M32 - targetOrientation.M23,
                                    targetOrientation.M13 - targetOrientation.M31,
                                    targetOrientation.M21 - targetOrientation.M12);

                double trace = targetOrientation.M11 + targetOrientation.M22 + targetOrientation.M33;
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1.0, 1.0));
            }

            if (Vector3D.IsZero(axis)) {
                angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
                yaw = angle;
                pitch = 0;
                roll = 0;
                return;
            }

            Vector3D axisAngle = SafeNormalize(axis) * angle;
            yaw = axisAngle.Y;
            pitch = axisAngle.X;
            roll = axisAngle.Z;
        }

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix) {
            Vector3D rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            Vector3D relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
            foreach (IMyGyro thisGyro in gyroList) {
                if (thisGyro.Closed) { continue; }
                Vector3D transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        void LaunchMissile() {
            if (missilesLoaded && !targetInfo.IsEmpty()) {
                foreach (IMyRadioAntenna block in MISSILEANTENNAS) {
                    string antennaName = "A [M]" + selectedMissile.ToString();
                    if (block.CustomName.Equals(antennaName)) {
                        block.Enabled = true;
                        block.EnableBroadcasting = true;
                    }
                }
                selectedMissile++;
                if (selectedMissile > missilesCount + 1) {
                    GetMissileAntennas();
                    SetMissileAntennas();
                    selectedMissile = 1;
                }
                SendMissileBroadcastMessage("Launch", targetInfo.HitPosition.Value, targetInfo.Velocity);
            }
            foreach (KeyValuePair<long, string> id in MissileIDs) {
                if (!targetInfo.IsEmpty()) {
                    if (id.Value.Contains("Lost")) {
                        SendMissileUnicastMessage("Update", id.Key, targetInfo.HitPosition.Value, targetInfo.Velocity);
                    }
                }
            }
        }

        bool GetMessages() {
            bool received = false;
            if (UNILISTENER.HasPendingMessage) {
                Dictionary<long, string> tempMissileIDs = new Dictionary<long, string>();
                Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
                while (UNILISTENER.HasPendingMessage) {
                    MyIGCMessage igcMessage = UNILISTENER.AcceptMessage();
                    long missileId = igcMessage.Source;
                    if (igcMessage.Data is ImmutableArray<MyTuple<string, Vector3D, double, double>>) {
                        ImmutableArray<MyTuple<string, Vector3D, double, double>> data = (ImmutableArray<MyTuple<string, Vector3D, double, double>>)igcMessage.Data;
                        received = true;
                        MyTuple<double, double, string> tup = MyTuple.Create(data[0].Item4, data[0].Item3, data[0].Item1);//distanceFromTarget, speed, info
                        if (!tempMissilesInfo.ContainsKey(missileId)) {
                            tempMissilesInfo.Add(missileId, tup);
                        }
                        if (!tempMissileIDs.ContainsKey(missileId)) {
                            tempMissileIDs.Add(missileId, data[0].Item1);
                        }
                    }
                    if (igcMessage.Data is MyTuple<string, string>) {//error message from missile
                        MyTuple<string, string> data = (MyTuple<string, string>)igcMessage.Data;
                        if (data.Item1 == "ERROR") {
                            DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                            debugLog = new StringBuilder("");
                            //DEBUG.ReadText(debugLog, true);
                            debugLog.Append("\n" + data.Item2 + "\n");
                            DEBUG.WriteText(debugLog);
                        }
                    }
                }
                //eliminate duplicates by preferring entries from the first dictionary
                missilesInfo = tempMissilesInfo.Concat(missilesInfo.Where(kvp => !tempMissilesInfo.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                MissileIDs = tempMissileIDs.Concat(MissileIDs.Where(kvp => !tempMissileIDs.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            return received;
        }

        void GetBroadcastMessages() {
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<string, bool>) {
                        MyTuple<string, bool> data = (MyTuple<string, bool>)igcMessage.Data;
                        string variable = data.Item1;
                        if (variable == "readyToFire") {
                            joltReady = data.Item2;
                        }
                    }
                }
            }
        }

        void SendMissileBroadcastMessage(string cmd, Vector3D targetPos, Vector3D targetVel) {
            long fakeId = 0;
            ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>.Builder immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>();
            MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>> tuple = MyTuple.Create(MyTuple.Create(fakeId, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative), MyTuple.Create(targetVel, targetPos));
            immArray.Add(tuple);
            IGC.SendBroadcastMessage("[MISSILE]", immArray.ToImmutable());
        }

        bool SendMissileUnicastMessage(string cmd, long id, Vector3D targetPos, Vector3D targetVel) {
            ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>.Builder immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>();
            MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>> tuple = MyTuple.Create(MyTuple.Create(id, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative), MyTuple.Create(targetVel, targetPos));
            immArray.Add(tuple);
            bool uniMessageSent = IGC.SendUnicastMessage(id, "[MISSILE]", immArray.ToImmutable());
            if (!uniMessageSent) {
                LostMissileIDs.Add(id);
            }
            return uniMessageSent;
        }

        void SendBroadcastTargetMessage(bool targFound, Vector3D targHitPos, Vector3D targVel, MatrixD targOrientation, Vector3D targPos, long targId) {
            MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, long> tuple = MyTuple.Create(targFound, targHitPos, targVel, targOrientation, targPos, targId);
            IGC.SendBroadcastMessage("[NAVIGATOR]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastGunsMessage(int weaponType, bool assaultCanShoot, bool artilleryCanShoot, bool railgunsCanShoot, bool smallRailgunsCanShoot) {
            MyTuple<int, bool, bool, bool, bool> tuple = MyTuple.Create(weaponType, assaultCanShoot, artilleryCanShoot, railgunsCanShoot, smallRailgunsCanShoot);
            IGC.SendBroadcastMessage("[NAVIGATOR]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastLogMessage() {
            if (!targetInfo.IsEmpty() && targetInfo.HitPosition.HasValue) {
                StringBuilder missileLog = new StringBuilder("");
                int count = 1;
                foreach (KeyValuePair<long, MyTuple<double, double, string>> inf in missilesInfo) {
                    //(info)"command=" + command + ",status=" + status + ",type=" + type;
                    if (count == missilesInfo.Count) {
                        missileLog.Append("toTarget=" + inf.Value.Item1.ToString("0.0") + ",speed=" + inf.Value.Item2.ToString("0.0") + "," + inf.Value.Item3);
                    } else {
                        missileLog.Append("toTarget=" + inf.Value.Item1.ToString("0.0") + ",speed=" + inf.Value.Item2.ToString("0.0") + "," + inf.Value.Item3 + "\n");
                    }
                    count++;
                }
                Vector3D targetVelocity = targetInfo.Velocity;
                var tuple = MyTuple.Create(
                    MyTuple.Create(targetInfo.Name, targetInfo.Position, targetVelocity),
                    missileLog.ToString(),
                    MyTuple.Create(weaponType, readyToFire, creative, autoFire),
                    MyTuple.Create(selectedPayLoad, autoMissiles, autoSwitchGuns, sequenceWeapons, missilesLoaded)
                );
                IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
            } else {
                StringBuilder missileLog = new StringBuilder("");
                if (missilesInfo.Count > 0) {
                    int count = 1;
                    foreach (KeyValuePair<long, MyTuple<double, double, string>> inf in missilesInfo) {
                        //(info)"command=" + command + ",status=" + status + ",type=" + type;
                        if (count == missilesInfo.Count) {
                            missileLog.Append("toTarget=" + inf.Value.Item1.ToString("0.0") + ",speed=" + inf.Value.Item2.ToString("0.0") + "," + inf.Value.Item3);
                        } else {
                            missileLog.Append("toTarget=" + inf.Value.Item1.ToString("0.0") + ",speed=" + inf.Value.Item2.ToString("0.0") + "," + inf.Value.Item3 + "\n");
                        }
                        count++;
                    }
                }
                var tuple = MyTuple.Create(
                        MyTuple.Create("", Vector3D.Zero, Vector3D.Zero),
                        missileLog.ToString(),
                        MyTuple.Create(weaponType, readyToFire, creative, autoFire),
                        MyTuple.Create(selectedPayLoad, autoMissiles, autoSwitchGuns, sequenceWeapons, missilesLoaded)
                    );
                IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
            }
        }

        void RemoveLostMissiles() {
            Dictionary<long, string> TempMissileIDs = new Dictionary<long, string>();
            foreach (KeyValuePair<long, string> entry in MissileIDs) {
                bool found = false;
                foreach (double id in LostMissileIDs) {
                    if (entry.Key == id) {
                        found = true;
                    }
                }
                if (!found) {
                    TempMissileIDs.Add(entry.Key, entry.Value);
                }
            }
            Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
            foreach (KeyValuePair<long, MyTuple<double, double, string>> entry in missilesInfo) {
                bool found = false;
                foreach (double id in LostMissileIDs) {
                    if (entry.Key == id) {
                        found = true;
                    }
                }
                if (!found) {
                    tempMissilesInfo.Add(entry.Key, entry.Value);
                }
            }
            missilesInfo = tempMissilesInfo;
            MissileIDs = TempMissileIDs;
        }

        void ResetTargeter() {
            UnlockGyros();
            TurnAlarmOff();
            ResetGuns();
            targetInfo = default(MyDetectedEntityInfo);
            selectedMissile = 1;
            activateOnce = false;
            autoMissilesCounter = 9 + 1;
            missilesLoaded = false;
            fudgeFactor = 5d;
            scanFudge = false;
            missedScan = 0;
            timeSinceLastLock = 0d;
            hasCenter = true;
            scanCenter = false;
            foreach (KeyValuePair<long, string> id in MissileIDs) {
                if (!id.Value.Contains("Lost")) {
                    SendMissileUnicastMessage("Lost", id.Key, Vector3D.Zero, Vector3D.Zero);
                }
            }
            SendBroadcastTargetMessage(false, Vector3D.Zero, Vector3D.Zero, default(MatrixD), Vector3D.Zero, 0);
        }

        void ActivateTargeter() {
            if (!activateOnce) {
                TurnAlarmOn();
                activateOnce = true;
                scanFudge = false;
                missedScan = 0;
                fudgeFactor = 5d;
                timeSinceLastLock = 0d;
            }
        }

        void UnlockGyros() {
            foreach (IMyGyro gyro in GYROS) {
                gyro.Pitch = 0f;
                gyro.Yaw = 0f;
                gyro.Roll = 0f;
                gyro.GyroOverride = false;
            }
        }

        void TurnAlarmOn() {
            foreach (IMySoundBlock block in ALARMS) { block.Play(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = true; }
        }

        void TurnAlarmOff() {
            foreach (IMySoundBlock block in ALARMS) { block.Stop(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = false; }
        }

        bool CheckProjectors() {
            string selection = "";
            switch (selectedMissile) {
                case 1:
                    selection = "R2";
                    break;
                case 2:
                    selection = "L2";
                    break;
            }
            bool completed = false;
            int blocksCount = 1000;
            foreach (IMyProjector block in TEMPPROJECTORS) {
                if (block.CustomName.Contains(selection)) {
                    blocksCount = block.BuildableBlocksCount;//RemainingBlocks;
                }
            }
            if (blocksCount == 0) {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = false; }
                completed = true;
            } else {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = true; }
            }
            return completed;
        }

        void CheckGunsAmmo() {
            if (creative) {
                gatlingAmmoFound = true;
                missileAmmoFound = true;
                artilleryAmmoFound = true;
                railgunAmmoFound = true;
                smallRailgunAmmoFound = true;
                assaultAmmoFound = true;
                autocannonAmmoFound = true;
            } else {
                gatlingAmmoFound = false;
                missileAmmoFound = false;
                artilleryAmmoFound = false;
                railgunAmmoFound = false;
                smallRailgunAmmoFound = false;
                assaultAmmoFound = false;
                autocannonAmmoFound = false;
                inventories.Clear();
                inventories.AddRange(ROCKETS.SelectMany(block => Enumerable.Range(0, block.gun.InventoryCount).Select(block.gun.GetInventory)));
                missileAmmoFound = CheckItems(inventories, missileAmmo);
                inventories.Clear();
                inventories.AddRange(GATLINGS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                gatlingAmmoFound = CheckItems(inventories, gatlingAmmo);
                inventories.Clear();
                inventories.AddRange(AUTOCANNONS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                autocannonAmmoFound = CheckItems(inventories, autocannonAmmo);
                inventories.Clear();
                inventories.AddRange(ARTILLERY.SelectMany(block => Enumerable.Range(0, block.gun.InventoryCount).Select(block.gun.GetInventory)));
                artilleryAmmoFound = CheckItems(inventories, artilleryAmmo);
                inventories.Clear();
                inventories.AddRange(RAILGUNS.SelectMany(block => Enumerable.Range(0, block.gun.InventoryCount).Select(block.gun.GetInventory)));
                railgunAmmoFound = CheckItems(inventories, railgunAmmo);
                inventories.Clear();
                inventories.AddRange(SMALLRAILGUNS.SelectMany(block => Enumerable.Range(0, block.gun.InventoryCount).Select(block.gun.GetInventory)));
                smallRailgunAmmoFound = CheckItems(inventories, smallRailgunAmmo);
                inventories.Clear();
                inventories.AddRange(ASSAULT.SelectMany(block => Enumerable.Range(0, block.gun.InventoryCount).Select(block.gun.GetInventory)));
                assaultAmmoFound = CheckItems(inventories, assaultAmmo);
            }
        }

        bool CheckItems(List<IMyInventory> inventories, MyItemType itemType) {
            bool itemFound = false;
            foreach (IMyInventory sourceInventory in inventories) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                sourceInventory.GetItems(items, item => item.Type.TypeId == itemType.TypeId.ToString());
                if (items.Count > 0) {
                    itemFound = true;
                    break;
                }
            }
            return itemFound;
        }

        void ManageGuns(double timeSinceLastRun, Vector3D trgtPos) {
            if (autoFire) {
                double distanceFromTarget = Vector3D.Distance(trgtPos, CONTROLLER.CubeGrid.WorldVolume.Center);
                if (autoSwitchGuns) {
                    if (distanceFromTarget <= 2000d) {
                        maxRangeOnce = true;
                        if (!decoyRanOnce && distanceFromTarget < 800d) {
                            decoyRanOnce = SHOOTERPB.TryRun("LaunchDecoy");
                        }
                        if (weaponType != 2 && weaponType != 3 && (gatlingsOnce || autocannonOnce)) {
                            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                            foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = false; }
                            gatlingsOnce = false;
                            autocannonOnce = false;
                        }
                        if (distanceFromTarget < 2000d && railgunAmmoFound && railgunsCanShoot) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 6) {
                                    weaponType = 6;
                                    return;
                                }
                                if (!railgunsOnce) { SwitchGun(true, true, true, false, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(RAILGUNS, railgunsDelay, ref railgunsCount, ref railgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in RAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else if (distanceFromTarget < 2000d && artilleryAmmoFound && artilleryCanShoot) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 5) {
                                    weaponType = 5;
                                    return;
                                }
                                if (!artilleryOnce) { SwitchGun(true, true, false, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ARTILLERY, artilleryDelay, ref artilleryCount, ref artilleryIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ARTILLERY) { gun.Shoot(timeSinceLastRun); } }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else if (distanceFromTarget < 1400d && smallRailgunAmmoFound && smallRailgunsCanShoot) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 7) {
                                    weaponType = 7;
                                    return;
                                }
                                if (!smallRailgunsOnce) { SwitchGun(true, true, true, true, false); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(SMALLRAILGUNS, smallRailgunsDelay, ref smallRailgunsCount, ref smallRailgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in SMALLRAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else if (distanceFromTarget < 1400d && assaultAmmoFound && assaultCanShoot) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 4) {
                                    weaponType = 4;
                                    return;
                                }
                                if (!assaultOnce) { SwitchGun(true, false, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ASSAULT, assaultDelay, ref assaultCount, ref assaultIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ASSAULT) { gun.Shoot(timeSinceLastRun); } }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else if (distanceFromTarget < 800d && (autocannonAmmoFound || gatlingAmmoFound)) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 3) {
                                    weaponType = 3;
                                    return;
                                }
                                if (!gatlingsOnce && gatlingAmmoFound) {
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                                    gatlingsOnce = true;
                                }
                                if (!autocannonOnce && autocannonAmmoFound) {
                                    foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = true; }
                                    autocannonOnce = true;
                                }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else if (distanceFromTarget < 500d && missileAmmoFound && rocketsCanShoot) {
                            cannotFireOnce = true;
                            if (readyToFire) {
                                readyToFireOnce = true;
                                if (weaponType != 5) {
                                    weaponType = 5;
                                    return;
                                }
                                if (!rocketsOnce) { SwitchGun(false, true, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ROCKETS, rocketDelay, ref rocketCount, ref rocketIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ROCKETS) { gun.Shoot(timeSinceLastRun); } }
                            } else {
                                if (readyToFireOnce) {
                                    readyToFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else {
                            if (cannotFireOnce) {
                                cannotFireOnce = false;
                                weaponType = 0;
                                ResetGuns();
                                return;
                            }
                        }
                    } else {
                        if (maxRangeOnce) {
                            maxRangeOnce = false;
                            weaponType = 0;
                            ResetGuns();
                            return;
                        }
                    }
                    if (readyToFire && joltReady && weaponType == 0) {
                        SHOOTERPB.TryRun("FireJolt");
                    }
                } else {
                    if (distanceFromTarget < 2000d) {
                        maxRangeOnce = true;
                        if (!decoyRanOnce && distanceFromTarget < 800d) {
                            decoyRanOnce = SHOOTERPB.TryRun("LaunchDecoy");
                        }
                        if (readyToFire) {
                            readyToFireOnce = true;
                            if (weaponType != 2 && weaponType != 3 && (gatlingsOnce || autocannonOnce)) {
                                foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                                foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = false; }
                                gatlingsOnce = false;
                                autocannonOnce = false;
                            }
                            if (weaponType == 1 && distanceFromTarget < 800d && missileAmmoFound && rocketsCanShoot) {
                                cannotFireOnce = true;
                                if (!rocketsOnce) { SwitchGun(false, true, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ROCKETS, rocketDelay, ref rocketCount, ref rocketIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ROCKETS) { gun.Shoot(timeSinceLastRun); } }
                            } else if ((weaponType == 2 || weaponType == 3) && distanceFromTarget < 800d && (autocannonAmmoFound || gatlingAmmoFound)) {
                                cannotFireOnce = true;
                                if (!gatlingsOnce && gatlingAmmoFound) {
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                                    gatlingsOnce = true;
                                }
                                if (!autocannonOnce && autocannonAmmoFound) {
                                    foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = true; }
                                    autocannonOnce = true;
                                }
                            } else if (weaponType == 4 && distanceFromTarget < 1400d && assaultAmmoFound && assaultCanShoot) {
                                cannotFireOnce = true;
                                if (!assaultOnce) { SwitchGun(true, false, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ASSAULT, assaultDelay, ref assaultCount, ref assaultIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ASSAULT) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 7 && distanceFromTarget < 1400d && smallRailgunAmmoFound && smallRailgunsCanShoot) {
                                cannotFireOnce = true;
                                if (!smallRailgunsOnce) { SwitchGun(true, true, true, true, false); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(SMALLRAILGUNS, smallRailgunsDelay, ref smallRailgunsCount, ref smallRailgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in SMALLRAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 5 && distanceFromTarget < 2000d && artilleryAmmoFound && artilleryCanShoot) {
                                cannotFireOnce = true;
                                if (!artilleryOnce) { SwitchGun(true, true, false, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ARTILLERY, artilleryDelay, ref artilleryCount, ref artilleryIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ARTILLERY) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 6 && distanceFromTarget < 2000d && railgunAmmoFound && railgunsCanShoot) {
                                cannotFireOnce = true;
                                if (!railgunsOnce) { SwitchGun(true, true, true, false, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(RAILGUNS, railgunsDelay, ref railgunsCount, ref railgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in RAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 0 && joltReady) {
                                SHOOTERPB.TryRun("FireJolt");
                            } else {
                                if (cannotFireOnce) {
                                    cannotFireOnce = false;
                                    ResetGuns();
                                }
                            }
                        } else {
                            if (readyToFireOnce) {
                                readyToFireOnce = false;
                                ResetGuns();
                            }
                        }
                    } else {
                        if (maxRangeOnce) {
                            maxRangeOnce = false;
                            ResetGuns();
                        }
                    }
                }
            }
        }

        void SwitchGun(bool rockets, bool assault, bool artillery, bool railguns, bool smallRailguns) {
            foreach (Gun block in ROCKETS) { block.paused = rockets; }
            foreach (Gun block in ASSAULT) { block.paused = assault; }
            foreach (Gun block in ARTILLERY) { block.paused = artillery; }
            foreach (Gun block in RAILGUNS) { block.paused = railguns; }
            foreach (Gun block in SMALLRAILGUNS) { block.paused = smallRailguns; }
            rocketsOnce = !rockets;
            assaultOnce = !assault;
            artilleryOnce = !artillery;
            railgunsOnce = !railguns;
            smallRailgunsOnce = !smallRailguns;
        }

        void CanShootGuns() {
            int count = 0;
            foreach (Gun gun in ROCKETS) { if (!gun.CanShoot()) { count++; } }
            if (count == ROCKETS.Count) { rocketsCanShoot = false; } else { rocketsCanShoot = true; }
            count = 0;
            foreach (Gun gun in ASSAULT) { if (!gun.CanShoot()) { count++; } }
            if (count == ASSAULT.Count) { assaultCanShoot = false; } else { assaultCanShoot = true; }
            count = 0;
            foreach (Gun gun in ARTILLERY) { if (!gun.CanShoot()) { count++; } }
            if (count == ARTILLERY.Count) { artilleryCanShoot = false; } else { artilleryCanShoot = true; }
            count = 0;
            foreach (Gun gun in RAILGUNS) { if (!gun.CanShoot()) { count++; } }
            if (count == RAILGUNS.Count) { railgunsCanShoot = false; } else { railgunsCanShoot = true; }
            count = 0;
            foreach (Gun gun in SMALLRAILGUNS) { if (!gun.CanShoot()) { count++; } }
            if (count == SMALLRAILGUNS.Count) { smallRailgunsCanShoot = false; } else { smallRailgunsCanShoot = true; }
            SendBroadcastGunsMessage(weaponType, assaultCanShoot, artilleryCanShoot, railgunsCanShoot, smallRailgunsCanShoot);
        }

        void SyncGuns(double timeSinceLastRun) {
            foreach (Gun gun in ROCKETS) { gun.Update(timeSinceLastRun); }
            foreach (Gun gun in ASSAULT) { gun.Update(timeSinceLastRun); }
            foreach (Gun gun in ARTILLERY) { gun.Update(timeSinceLastRun); }
            foreach (Gun gun in RAILGUNS) { gun.Update(timeSinceLastRun); }
            foreach (Gun gun in SMALLRAILGUNS) { gun.Update(timeSinceLastRun); }
        }

        void ResetGuns() {
            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
            foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = false; }
            SwitchGun(true, true, true, true, true);
            ResetGunsInit();
            gatlingsOnce = false;
            autocannonOnce = false;
            decoyRanOnce = false;
        }

        void ResetGunsInit() {
            rocketsOnce = false;
            assaultOnce = false;
            artilleryOnce = false;
            railgunsOnce = false;
            smallRailgunsOnce = false;
        }

        void SequenceWeapons(List<Gun> guns, int delay, ref int count, ref int index, double timeSinceLastRun) {
            if (count == delay) {
                for (int i = 0; i < guns.Count; i++) {
                    if (i == index) {
                        guns[i].Shoot(timeSinceLastRun);
                    } else { guns[i].Update(timeSinceLastRun); }
                }
                count = 0;
            } else { foreach (Gun gun in guns) { gun.Update(timeSinceLastRun); } }
            index++;
            if (index >= guns.Count) { index = 0; }
            count++;
        }

        void SetGunsDelay() {
            if (ROCKETS.Count > 0) {
                rocketDelay = (int)Math.Ceiling(0.583 / (double)ROCKETS.Count);
                if (rocketDelay == 0) { rocketDelay = 1; }
            }
            if (ASSAULT.Count > 0) {
                assaultDelay = (int)Math.Ceiling(0.33 / (double)ASSAULT.Count);
                if (assaultDelay == 0) { assaultDelay = 1; }
            }
            if (ARTILLERY.Count > 0) {
                artilleryDelay = (int)Math.Ceiling(0.83 / (double)ARTILLERY.Count);
                if (artilleryDelay == 0) { artilleryDelay = 1; }
            }
            if (RAILGUNS.Count > 0) {
                railgunsDelay = (int)Math.Ceiling(3.083 / (double)RAILGUNS.Count);
                if (railgunsDelay == 0) { railgunsDelay = 1; }
            }
            if (SMALLRAILGUNS.Count > 0) {
                smallRailgunsDelay = (int)Math.Ceiling(3.083 / (double)SMALLRAILGUNS.Count);
                if (smallRailgunsDelay == 0) { smallRailgunsDelay = 1; }
            }
        }

        bool LoadMissiles() {
            List<IMyShipConnector> MISSILECONNECTORS = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(MISSILECONNECTORS, b => b.CustomName.Contains("[M]"));
            if (MISSILECONNECTORS.Count == 0) { return false; }
            foreach (IMyShipConnector block in MISSILECONNECTORS) { block.Connect(); }
            bool allFilled = false;
            if (creative) {
                allFilled = true;
            } else {
                List<IMyTerminalBlock> MISSILEBLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
                List<IMyInventory> MISSILEINVENTORIES = new List<IMyInventory>();
                if (selectedPayLoad == 0) {//missiles
                    MISSILEBLOCKSWITHINVENTORY.Clear();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains("[M]") && block is IMyGasGenerator);
                    MISSILEINVENTORIES.Clear();
                    MISSILEINVENTORIES.AddRange(MISSILEBLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                    int loaded = 0;
                    foreach (IMyInventory inventory in MISSILEINVENTORIES) {
                        MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                        if (inventory.CanItemsBeAdded(availableVolume, iceOre)) {
                            TransferItems(inventory, iceOre);
                        }
                        double currentVolume = (double)inventory.CurrentVolume;
                        double maxVolume = (double)inventory.MaxVolume;
                        double percent = 0;
                        if (maxVolume > 0 && currentVolume > 0) {
                            percent = currentVolume / maxVolume * 100;
                        }
                        if (percent >= 95) {
                            loaded++;
                        }
                    }
                    if (loaded == MISSILEINVENTORIES.Count) {
                        allFilled = true;
                    }
                } else if (selectedPayLoad == 1) {//drones
                    MISSILEBLOCKSWITHINVENTORY.Clear();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains("[M]") && (block is IMyGasGenerator || block is IMySmallGatlingGun || block is IMyLargeGatlingTurret));//&& !(block is IMyGasTank)); IMyLargeTurretBase
                    int inventoriesCount = 0;
                    int loaded = 0;
                    foreach (IMyTerminalBlock block in MISSILEBLOCKSWITHINVENTORY) {
                        if (block is IMyGasGenerator) {
                            MISSILEINVENTORIES.Clear();
                            MISSILEINVENTORIES.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                            inventoriesCount += MISSILEINVENTORIES.Count;
                            foreach (IMyInventory inventory in MISSILEINVENTORIES) {
                                MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                                if (inventory.CanItemsBeAdded(availableVolume, iceOre)) {
                                    TransferItems(inventory, iceOre);
                                }
                                double currentVolume = (double)inventory.CurrentVolume;
                                double maxVolume = (double)inventory.MaxVolume;
                                double percent = 0;
                                if (maxVolume > 0 && currentVolume > 0) {
                                    percent = currentVolume / maxVolume * 100;
                                }
                                if (percent >= 95) {
                                    loaded++;
                                }
                            }
                        } else if (block is IMySmallGatlingGun || block is IMyLargeGatlingTurret) {
                            MISSILEINVENTORIES.Clear();
                            MISSILEINVENTORIES.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                            inventoriesCount += MISSILEINVENTORIES.Count;
                            foreach (IMyInventory inventory in MISSILEINVENTORIES) {
                                MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                                if (inventory.CanItemsBeAdded(availableVolume, gatlingAmmo)) {
                                    TransferItems(inventory, gatlingAmmo);
                                }
                                double currentVolume = (double)inventory.CurrentVolume;
                                double maxVolume = (double)inventory.MaxVolume;
                                double percent = 0;
                                if (maxVolume > 0 && currentVolume > 0) {
                                    percent = currentVolume / maxVolume * 100;
                                }
                                if (percent >= 75) {
                                    loaded++;
                                }
                            }
                        }
                    }
                    if (loaded == inventoriesCount) {
                        allFilled = true;
                    }
                }
            }
            return allFilled;
        }

        bool TransferItems(IMyInventory inventory, MyItemType itemToFind) {
            bool transferred = false;
            foreach (IMyInventory sourceInventory in CARGOINVENTORIES) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                sourceInventory.GetItems(items, item => item.Type.TypeId == itemToFind.TypeId.ToString());
                foreach (MyInventoryItem item in items) {
                    transferred = false;
                    if (sourceInventory.CanTransferItemTo(inventory, item.Type) && inventory.CanItemsBeAdded(item.Amount, item.Type)) { transferred = sourceInventory.TransferItemTo(inventory, item); }
                    if (!transferred) {
                        MyFixedPoint amount = inventory.MaxVolume - inventory.CurrentVolume;
                        transferred = sourceInventory.TransferItemTo(inventory, item, amount);
                    }
                    if (!transferred) { sourceInventory.TransferItemTo(inventory, item, item.Amount); }
                    if (transferred) { break; }
                }
            }
            return transferred;
        }

        void GetBlocks() {
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, b => b.CustomName.Contains("[CRX] Camera Lidar"));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains("[CRX] Turret"));
            LIGHTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(LIGHTS, b => b.CustomName.Contains("[CRX] Rotating Light"));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, b => b.CustomName.Contains("[CRX] Alarm Lidar"));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, b => b.CustomName.Contains("[CRX] Gyro"));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, b => b.CustomName.Contains("Missile"));
            PROJECTORSMISSILES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSMISSILES, b => b.CustomName.Contains("Missile"));
            PROJECTORSDRONES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDRONES, b => b.CustomName.Contains("Drone"));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, b => b.CustomName.Contains("[CRX] Gatling Gun"));
            AUTOCANNONS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(AUTOCANNONS, block => block.CustomName.Contains("[CRX] Autocannon"));
            List<IMyUserControllableGun> guns = new List<IMyUserControllableGun>();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, b => b.CustomName.Contains("[CRX] Rocket Launcher"));
            ROCKETS.Clear();
            foreach (IMyUserControllableGun gun in guns) { ROCKETS.Add(new Gun(gun, 19, 4d, 0.583)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Railgun"));
            RAILGUNS.Clear();
            foreach (IMyUserControllableGun gun in guns) { RAILGUNS.Add(new Gun(gun, 1, 4d, 3.083)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Artillery"));
            ARTILLERY.Clear();
            foreach (IMyUserControllableGun gun in guns) { ARTILLERY.Add(new Gun(gun, 1, 12d, 0.83)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Small Railgun"));
            SMALLRAILGUNS.Clear();
            foreach (IMyUserControllableGun gun in guns) { SMALLRAILGUNS.Add(new Gun(gun, 1, 4d, 3.083)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Assault"));
            ASSAULT.Clear();
            foreach (IMyUserControllableGun gun in guns) { ASSAULT.Add(new Gun(gun, 1, 6d, 0.33)); }
            CARGOS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CARGOS, b => b.CustomName.Contains("[CRX] Cargo"));
            CARGOINVENTORIES.Clear();
            CARGOINVENTORIES.AddRange(CARGOS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ANTENNA = GridTerminalSystem.GetBlockWithName("T") as IMyRadioAntenna;
            CONTROLLER = GridTerminalSystem.GetBlockWithName("[CRX] Controller Cockpit 1") as IMyShipController;
            SHOOTERPB = GridTerminalSystem.GetBlockWithName("[CRX] PB Shooter") as IMyProgrammableBlock;
            DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
            LCDAUTOSWITCHGUNS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Switch Guns") as IMyTextPanel;
            LCDAUTOFIRE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Fire") as IMyTextPanel;
            LCDAUTOMISSILES = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Missiles") as IMyTextPanel;
            LCDCREATIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Creative") as IMyTextPanel;
            LCDSEQUENCEGUNS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Sequence Guns") as IMyTextPanel;
        }

        void SetBlocks() {
            foreach (IMyCameraBlock cam in LIDARS) {
                cam.EnableRaycast = true;
            }
            foreach (IMyGyro block in GYROS) {
                block.Yaw = 0f;
                block.Pitch = 0f;
                block.Roll = 0f;
                block.GyroOverride = false;
            }
            ANTENNA.Enabled = true;
            ANTENNA.EnableBroadcasting = true;
        }

        void GetMissileAntennas() {
            MISSILEANTENNAS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(MISSILEANTENNAS, b => b.CustomName.Contains("A [M]"));
        }

        void SetMissileAntennas() {
            foreach (IMyRadioAntenna block in MISSILEANTENNAS) {
                block.EnableBroadcasting = false;
                block.Enabled = false;
            }
        }

        public static Vector3D SafeNormalize(Vector3D a) {
            if (Vector3D.IsZero(a)) { return Vector3D.Zero; }
            if (Vector3D.IsUnit(ref a)) { return a; }
            return Vector3D.Normalize(a);
        }

        public static double AngleBetween(Vector3D a, Vector3D b) {//returns radians
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) {
                return 0;
            } else { return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1)); }
        }

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, globalTimestep);
            rollController = new PID(1d, 0d, 1d, globalTimestep);
        }

        void ManagePIDControllers(bool isTargetEmpty) {
            if (!isTargetEmpty) {
                if (updateOnce) {
                    UpdatePIDControllers(5d, 0d, 5d, 5d, 0d, 5d, 1d, 0d, 1d);
                    updateOnce = false;
                }
            } else {
                if (!updateOnce) {
                    UpdatePIDControllers(1d, 0d, 1d, 1d, 0d, 1d, 1d, 0d, 1d);
                    updateOnce = true;
                }
            }
        }

        void UpdatePIDControllers(double yawAimP, double yawAimI, double yawAimD, double pitchAimP, double pitchAimI, double pitchAimD, double rollAimP, double rollAimI, double rollAimD) {
            yawController.Update(yawAimP, yawAimI, yawAimD);
            pitchController.Update(pitchAimP, pitchAimI, pitchAimD);
            rollController.Update(rollAimP, rollAimI, rollAimD);
        }

        public class PID {
            double _kP = 0;
            double _kI = 0;
            double _kD = 0;

            double _timeStep = 0;
            double _inverseTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;

            public double Value { get; private set; }

            public PID(double kP, double kI, double kD, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
            }

            protected virtual double GetIntegral(double currentError, double errorSum, double timeStep) {
                return errorSum + currentError * timeStep;
            }

            public void Update(double kP, double kI, double kD) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _firstRun = true;
            }

            public double Control(double error) {
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun) {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Get error sum
                _errorSum = GetIntegral(error, _errorSum, _timeStep);

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
                return this.Value;
            }

            public double Control(double error, double timeStep) {
                if (timeStep != _timeStep) {
                    _timeStep = timeStep;
                    _inverseTimeStep = 1 / _timeStep;
                }
                return Control(error);
            }

            public void Reset() {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

        public class DecayingIntegralPID : PID {
            readonly double _decayRatio;

            public DecayingIntegralPID(double kP, double kI, double kD, double timeStep, double decayRatio) : base(kP, kI, kD, timeStep) {
                _decayRatio = decayRatio;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                //return errorSum = errorSum * (1.0 - _decayRatio) + currentError * timeStep;
                return errorSum * (1.0 - _decayRatio) + currentError * timeStep;
            }
        }

        public class ClampedIntegralPID : PID {
            readonly double _upperBound;
            readonly double _lowerBound;

            public ClampedIntegralPID(double kP, double kI, double kD, double timeStep, double lowerBound, double upperBound) : base(kP, kI, kD, timeStep) {
                _upperBound = upperBound;
                _lowerBound = lowerBound;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                errorSum += currentError * timeStep;
                return Math.Min(_upperBound, Math.Max(errorSum, _lowerBound));
            }
        }

        public class BufferedIntegralPID : PID {
            readonly Queue<double> _integralBuffer = new Queue<double>();
            readonly int _bufferSize = 0;

            public BufferedIntegralPID(double kP, double kI, double kD, double timeStep, int bufferSize) : base(kP, kI, kD, timeStep) {
                _bufferSize = bufferSize;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                if (_integralBuffer.Count == _bufferSize)
                    _integralBuffer.Dequeue();
                _integralBuffer.Enqueue(currentError * timeStep);
                return _integralBuffer.Sum();
            }
        }

        public class Gun {
            public IMyUserControllableGun gun;
            public bool isReloading;
            public bool hasShot;
            public bool paused;
            static readonly MyDefinitionId electricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
            readonly MyResourceSinkComponent sink;
            const float idlePowerDraw = 0.0002f;
            const float epsilon = 1e-6f;
            readonly int rounds;
            int roundsCount = 0;
            readonly double reloadTime;
            double reloadTimeCount = 0d;
            readonly double fireRate;
            double fireRateCount = 0d;

            public Gun(IMyUserControllableGun weapon, int _rounds, double _reloadTime, double rof) {
                gun = weapon;
                sink = gun.Components.Get<MyResourceSinkComponent>();
                rounds = _rounds;
                reloadTime = _reloadTime;
                fireRate = rof;
                isReloading = false;
                hasShot = false;
            }

            public void Shoot(double timeSinceLastRun) {
                if (hasShot) {
                    fireRateCount += timeSinceLastRun;
                    if (fireRateCount >= fireRate) {
                        fireRateCount = 0d;
                        hasShot = false;
                    }
                }
                if (!IsRecharging() && !isReloading && !hasShot) {
                    gun.ShootOnce();
                    roundsCount++;
                    hasShot = true;
                    return;
                }
                if (roundsCount == rounds) {
                    isReloading = true;
                    reloadTimeCount += timeSinceLastRun;
                }
                if (reloadTimeCount >= reloadTime) {
                    isReloading = false;
                    reloadTimeCount = 0d;
                    roundsCount = 0;
                }
            }

            public void Update(double timeSinceLastRun) {
                if (paused) {
                    if (hasShot) {
                        fireRateCount += timeSinceLastRun;
                        if (fireRateCount >= fireRate) {
                            fireRateCount = 0d;
                            hasShot = false;
                        }
                    }
                    if (roundsCount == rounds) {
                        isReloading = true;
                        reloadTimeCount += timeSinceLastRun;
                    }
                    if (reloadTimeCount >= reloadTime) {
                        isReloading = false;
                        reloadTimeCount = 0d;
                        roundsCount = 0;
                    }
                }
            }

            public bool CanShoot() {
                if (IsRecharging() || isReloading || hasShot) {
                    return false;
                } else {
                    return true;
                }
            }

            bool IsRecharging() {
                if (sink == null) { return false; }
                return sink.MaxRequiredInputByType(electricityId) > (idlePowerDraw + epsilon);
            }
        }


    }
}
