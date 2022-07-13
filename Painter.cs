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
        //revisit LoadMissiles(),make it universal
        //PAINTER

        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string antennasName = "T";
        readonly string lightsName = "[CRX] Rotating Light";
        readonly string turretsName = "[CRX] Turret";
        readonly string lcdsTargetName = "[CRX] LCD Target";
        readonly string controllersName = "[CRX] Controller";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string weldersName = "Missile";
        readonly string rocketsName = "[CRX] Rocket Launcher";
        readonly string gatlingsName = "[CRX] Gatling Gun";
        readonly string railgunsName = "[CRX] Railgun";
        readonly string smallRailgunsName = "[CRX] Small Railgun";
        readonly string autocannonsName = "[CRX] Autocannon";
        readonly string artilleryName = "[CRX] Artillery";
        readonly string assaultName = "[CRX] Assault";
        readonly string projectorsMissilesName = "Missile";
        readonly string projectorsDronesName = "Drone";
        readonly string missileAntennasName = "A [M]";
        readonly string missilePrefix = "[M]";
        readonly string painterTag = "[PAINTER]";
        readonly string missileAntennaTag = "[MISSILE]";
        readonly string navigatorTag = "[NAVIGATOR]";
        readonly string decoyName = "[CRX] PB Shooter";
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

        const string argFireDecoy = "LaunchDecoy";
        const string argFireJolt = "FireJolt";

        readonly string sectionTag = "MissilesSettings";
        readonly string cockpitTargetSurfaceKey = "cockpitTargetSurface";

        readonly bool creative = true;//set true if playing creative mode
        readonly bool sequenceWeapons = false;//set true to sequence Gun lists
        readonly int missilesCount = 2;
        readonly int autoMissilesDelay = 9;
        readonly int writeDelay = 1;
        readonly int fudgeAttempts = 8;
        readonly int rocketRounds = 19;
        readonly int singleRound = 1;
        readonly float joltSpeed = 958.21f;
        readonly float rocketSpeed = 200f;
        readonly float gatlingSpeed = 400f;
        readonly float autocannonSpeed = 400f;
        readonly float assaultSpeed = 500f;
        readonly float artillerySpeed = 500f;
        readonly float railgunSpeed = 2000f;
        readonly float smallRailgunSpeed = 1000f;
        readonly double angleTolerance = 1d;//degrees - threshold where guns start/stop to fire when aligning to the target
        readonly double gunsRange = 2000d;//maximum guns range
        double rocketRange = 500d;
        readonly double autocannonRange = 800d;//gatlingRange
        readonly double assaultRange = 1400d;
        readonly double artilleryRange = 2000d;
        readonly double railgunRange = 2000d;
        readonly double smallRailgunRange = 1400d;
        readonly double rocketROF = 0.583;//4 - 120rpm - 2rps - 0.03 rptick - 30 ticks
        readonly double assaultROF = 0.33;//2 - 200rpm - 3.33rps - 0.055 rptick - 19 ticks
        readonly double artilleryROF = 0.83;//5 - 80rpm - 1.33rps - 0.022 rptick - 46 ticks
        readonly double railgunsROF = 3.083;//19 - 20rpm - 0.33rps - 0.0055 rptick - 181 ticks
        readonly double smallRailgunsROF = 3.083;//19 - 20rpm - 0.33rps - 0.0055 rptick - 181 ticks
        readonly double rocketReload = 4d;//24 - 4000ms - 240 ticks
        readonly double assaultReload = 6d;//36 - 6000ms - 360 ticks
        readonly double artilleryReload = 12d;//72 - 120000ms - 720 ticks
        readonly double railgunsReload = 4d;//24 - 4000ms - 240 ticks
        readonly double smallRailgunsReload = 4d;//24 - 4000ms - 240 ticks
        readonly double yawAimP = 5d;
        readonly double yawAimI = 0d;
        readonly double yawAimD = 5d;
        readonly double pitchAimP = 5d;
        readonly double pitchAimI = 0d;
        readonly double pitchAimD = 5d;
        readonly double rollAimP = 1d;
        readonly double rollAimI = 0d;
        readonly double rollAimD = 1d;
        readonly double integralWindupLimit = 0d;

        bool autoFire = true;//set true to fire guns automatically
        bool autoMissiles = false;//set true to fire missiles automatically
        bool autoSwitchGuns = true;//set true to switch guns automatically depending on range etc.
        int weaponType = 2;//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
        int selectedPayLoad = 0;//0 Missiles - 1 Drones
        int fudgeCount = 0;
        int rocketDelay = 1;
        int assaultDelay = 1;
        int artilleryDelay = 1;
        int railgunsDelay = 1;
        int smallRailgunsDelay = 1;
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
        int selectedMissile = 1;
        int cockpitTargetSurface = 0;
        int autoMissilesCounter = 0;
        int writeCount = 0;
        double fudgeFactor = 5d;
        double timeSinceLastLock = 0d;
        double targetDiameter;
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
        bool doOnce = false;
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
        string targetName = null;

        Vector3D targetPosition;
        MyDetectedEntityInfo targetInfo;
        readonly Random random = new Random();

        const float globalTimestep = 10.0f / 60.0f;
        const double rad2deg = 180 / Math.PI;

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

        public StringBuilder targetLog = new StringBuilder("");
        public StringBuilder missileLog = new StringBuilder("");
        public StringBuilder debugLog = new StringBuilder("");

        readonly MyIni myIni = new MyIni();

        PID yawController;
        PID pitchController;
        PID rollController;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            UNILISTENER = IGC.UnicastListener;
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(painterTag);
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
            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }
            autoMissilesCounter = autoMissilesDelay + 1;
            SetGunsDelay();
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                RemoveLostMissiles();
                GetBroadcastMessages();
                GetMessages();
                ReadMessages();
                CanShootGuns();

                double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;

                if (targetName != null) {
                    if (fudgeCount > fudgeAttempts) {//if lidars or turrets doesn't detect a enemy for some time reset the script
                        ResetTargeter();
                        return;
                    }

                    ActivateTargeter();//things to run once when a enemy is detected

                    double lastLock = timeSinceLastLock + timeSinceLastRun;

                    bool targetFound = TurretsDetection(true);

                    if (!targetFound) {
                        targetFound = AcquireTarget(lastLock);
                    }

                    if (targetFound) {//send message to missiles
                        SendBroadcastTargetMessage(true, targetPosition, targetInfo.Velocity);

                        foreach (var id in MissileIDs) {
                            SendMissileUnicastMessage(commandUpdate, id.Key);
                        }

                        if (autoMissiles) {
                            if (autoMissilesCounter > autoMissilesDelay) {
                                arg = commandLaunch;
                                autoMissilesCounter = 0;
                            }
                            autoMissilesCounter++;
                        }
                    }

                    IMyTerminalBlock refBlock;
                    if (LIDARS.Count != 0) {
                        refBlock = LIDARS[0];
                    } else { refBlock = CONTROLLER; }
                    LockOnTarget(refBlock, lastLock);

                    if (writeCount == writeDelay) {
                        CheckGunsAmmo();
                    }

                    ManageGuns(timeSinceLastRun);

                    if (targetFound) {
                        timeSinceLastLock = 0;
                    } else { timeSinceLastLock += timeSinceLastRun; }

                    ReadTargetInfo();
                } else {
                    if (doOnce) {//things to run once when a enemy is lost
                        ResetTargeter();
                        return;
                    }

                    bool targetFound = TurretsDetection(false);

                    if (!targetFound) {
                        if (scanFudge && fudgeCount <= fudgeAttempts) {
                            arg = argLock;
                        } else if (fudgeCount > fudgeAttempts) {
                            scanFudge = false;
                            fudgeCount = 0;
                        }
                    }
                }

                SyncGuns(timeSinceLastRun);

                bool completed = CheckProjectors();
                if (!completed) {
                    missilesLoaded = false;
                } else {
                    if (!missilesLoaded) {
                        missilesLoaded = LoadMissiles();
                    }
                }

                if (!String.IsNullOrEmpty(arg)) {
                    ProcessArgs(arg, (double)globalTimestep);
                }

                if (writeCount == writeDelay) {
                    WriteInfo();
                    writeCount = 0;
                }
                writeCount++;
            } catch (Exception e) {
                DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                debugLog = new StringBuilder("");
                //DEBUG.ReadText(debugLog, true);
                debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                DEBUG.WriteText(debugLog);
                Setup();
            }
        }

        void ProcessArgs(string arg, double timeSinceLastRun) {
            switch (arg) {
                case argLock: AcquireTarget(timeSinceLastRun); break;
                case commandLaunch:
                    if (missilesLoaded && targetName != null) {
                        foreach (IMyRadioAntenna block in MISSILEANTENNAS) {
                            string antennaName = missileAntennasName + selectedMissile.ToString();
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
                        SendMissileBroadcastMessage(commandLaunch);
                    }
                    foreach (var id in MissileIDs) {
                        if (targetName != null) {
                            if (id.Value.Contains(commandLost)) {
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
                    weaponType++;
                    if (weaponType > 4) {
                        weaponType = 0;
                    }
                    break;
                case argAutoFire:
                    autoFire = !autoFire;
                    break;
                case argAutoMissiles:
                    autoMissiles = !autoMissiles;
                    break;
                case argSwitchPayLoad:
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
                case argToggleAllGuns:
                    autoSwitchGuns = !autoSwitchGuns;
                    break;
                case commandSpiral:
                    if (targetName != null) {
                        foreach (var id in MissileIDs) {
                            if (id.Value.Contains(commandUpdate) && !id.Value.Contains("Drone")) {
                                SendMissileUnicastMessage(commandSpiral, id.Key);
                            }
                        }
                    }
                    break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit) {
            if (!cockpit.CustomData.Contains(sectionTag)) {
                cockpit.CustomData += $"[{sectionTag}]\n{cockpitTargetSurfaceKey}={cockpitTargetSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);
            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToString())) {
                cockpitTargetSurface = myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToInt32();
                SURFACES.Add(cockpit.GetSurface(cockpitTargetSurface));
            }
        }

        bool TurretsDetection(bool sameId) {
            bool targetFound = false;
            foreach (IMyLargeTurretBase turret in TURRETS) {
                MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                if (!targ.IsEmpty()) {
                    if (IsValidLidarTarget(ref targ)) {
                        if (sameId) {
                            if (targ.EntityId == targetInfo.EntityId) {
                                targetDiameter = Vector3D.Distance(targ.BoundingBox.Min, targ.BoundingBox.Max);
                                targetInfo = targ;
                                targetFound = true;
                                break;
                            }
                        } else {
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

        bool AcquireTarget(double timeSinceLastRun) {
            bool targetFound = false;
            if (targetName == null) {//case argLock
                if (!scanFudge) {
                    targetFound = ScanTarget(false);
                    if (!targetFound) {
                        scanFudge = true;
                    }
                } else {
                    targetFound = ScanFudgeTarget((double)globalTimestep);
                    if (!targetFound) {
                        fudgeCount++;
                    } else {
                        scanFudge = true;
                        fudgeCount = 0;
                    }
                }
            } else {
                if (scanCenter && hasCenter) {
                    targetFound = ScanDelayedTarget(targetInfo.Position, timeSinceLastRun);
                    if (!targetFound) {
                        hasCenter = false;
                    }
                } else {
                    if (!scanFudge) {
                        targetFound = ScanDelayedTarget(targetPosition, timeSinceLastRun);
                        scanCenter = true;
                        if (!targetFound) {
                            scanFudge = true;
                        }
                    }
                }
                if (!hasCenter && !targetFound && scanFudge) {
                    targetFound = ScanFudgeDelayedTarget(timeSinceLastRun);
                    if (!targetFound) {
                        fudgeCount++;
                    } else {
                        scanFudge = false;
                        fudgeCount = 0;
                    }
                }
            }
            return targetFound;
        }

        bool ScanTarget(bool sameId) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            MyDetectedEntityInfo entityInfo = lidar.Raycast(lidar.AvailableScanRange, 0, 0);
            if (!entityInfo.IsEmpty()) {
                if (IsValidLidarTarget(ref entityInfo)) {
                    if (sameId) {
                        if (entityInfo.EntityId == targetInfo.EntityId) {
                            targetName = entityInfo.Name;
                            targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                            targetInfo = entityInfo;
                            DetermineTargetPostion();
                            targetFound = true;
                        }
                    } else {
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

        bool ScanDelayedTarget(Vector3D enemyPos, double timeSinceLastRun) {
            bool targetFound = false;
            Vector3D trgtPos = enemyPos;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            Vector3D targetPos = trgtPos + (targetInfo.Velocity * (float)timeSinceLastRun);
            double overshootDistance = targetDiameter / 2;
            Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * overshootDistance);
            double dist = Vector3D.Distance(testTargetPosition, lidar.GetPosition());
            if (lidar.CanScan(dist)) {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(testTargetPosition);
                if (!entityInfo.IsEmpty()) {
                    if (entityInfo.EntityId == targetInfo.EntityId) {
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

        bool ScanFudgeDelayedTarget(double timeSinceLastRun) {
            bool targetFound = false;
            Vector3D trgtPos = targetPosition;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            Vector3D scanPosition = trgtPos + targetInfo.Velocity * (float)timeSinceLastRun;
            scanPosition += CalculateFudgeVector(scanPosition - lidar.GetPosition(), timeSinceLastRun);
            double overshootDistance = targetDiameter / 2;
            scanPosition += Vector3D.Normalize(scanPosition - lidar.GetPosition()) * overshootDistance;
            double dist = Vector3D.Distance(scanPosition, lidar.GetPosition());
            if (lidar.CanScan(dist)) {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(scanPosition);
                if (!entityInfo.IsEmpty()) {
                    if (entityInfo.EntityId == targetInfo.EntityId) {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        DetermineTargetPostion();
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

        bool ScanFudgeTarget(double timeSinceLastRun) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            Vector3D scanPosition = Vector3D.Normalize(lidar.WorldMatrix.Forward) * 5000d;
            scanPosition += CalculateFudgeVector(scanPosition - lidar.GetPosition(), timeSinceLastRun);
            double dist = Vector3D.Distance(scanPosition, lidar.GetPosition());
            if (lidar.CanScan(dist)) {
                MyDetectedEntityInfo entityInfo = lidar.Raycast(scanPosition);
                if (!entityInfo.IsEmpty()) {
                    if (entityInfo.EntityId == targetInfo.EntityId) {
                        targetName = entityInfo.Name;
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);
                        targetInfo = entityInfo;
                        DetermineTargetPostion();
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
            var perpVector1 = Vector3D.CalculatePerpendicularVector(targetDirection);
            var perpVector2 = Vector3D.Cross(perpVector1, targetDirection);
            if (!Vector3D.IsUnit(ref perpVector2)) {
                perpVector2.Normalize();
            }
            var randomVector = (2.0 * random.NextDouble() - 1.0) * perpVector1 + (2.0 * random.NextDouble() - 1.0) * perpVector2;
            return randomVector * fudgeFactor * (float)timeSinceLastRun;
        }

        void DetermineTargetPostion() {
            if (targetInfo.HitPosition.HasValue) {
                targetPosition = targetInfo.HitPosition.Value;
            } else { targetPosition = targetInfo.Position; }
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

        void LockOnTarget(IMyTerminalBlock REF, double timeSinceLastRun) {
            Vector3D targetPos = targetPosition + (targetInfo.Velocity * (float)timeSinceLastRun);
            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetPos, REF.GetPosition());
            if (distanceFromTarget > gunsRange) {
                aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, joltSpeed, REF);
                if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                    aimDirection = BulletDrop(distanceFromTarget, joltSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                }
            } else {
                switch (weaponType) {//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
                    case 0:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, joltSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, joltSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 1:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, rocketSpeed, REF);
                        break;
                    case 2:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, gatlingSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, gatlingSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 3:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, autocannonSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, autocannonSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 4:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, assaultSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, assaultSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 5:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, artillerySpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, artillerySpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 6:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, railgunSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, railgunSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    case 7:
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetInfo.Velocity, smallRailgunSpeed, REF);
                        if (!Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) {
                            aimDirection = BulletDrop(distanceFromTarget, smallRailgunSpeed, aimDirection, CONTROLLER.GetNaturalGravity());
                        }
                        break;
                    default:
                        aimDirection = targetPos - REF.GetPosition();
                        break;
                }
            }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle); //double rollSpeed = rollController.Control(rollAngle);
            double userRoll = 0d;
            foreach (IMyShipController cntrllr in CONTROLLERS) {
                if (cntrllr.IsUnderControl) {
                    userRoll = (double)cntrllr.RollIndicator;
                    break;
                }
            }
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
            double angle = VectorMath.AngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= angleTolerance) {
                readyToFire = true;
            } else {
                readyToFire = false;
            }
        }

        Vector3D ComputeInterceptWithLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, IMyTerminalBlock muzzle) {
            MatrixD refWorldMatrix = muzzle.WorldMatrix;
            //MatrixD refLookAtMatrix = MatrixD.CreateLookAt(Vector3D.Zero, refWorldMatrix.Forward, refWorldMatrix.Up);
            Vector3D aimDirection = GetPredictedTargetPosition(muzzle, CONTROLLER, targetPosition, targetVelocity, projectileSpeed);
            aimDirection -= refWorldMatrix.Translation;
            //aimDirection = Vector3D.Normalize(Vector3D.TransformNormal(aimDirection, refLookAtMatrix));
            return aimDirection;
        }

        Vector3D GetPredictedTargetPosition(IMyTerminalBlock gun, IMyShipController shooter, Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed) {
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
            if (t1 > t2 && t2 > 0) {
                t = t2;
            } else {
                t = t1;
            }
            t += shootDelay;
            Vector3D predictedPosition = targetPosition + diffVelocity * t;
            //Vector3 bulletPath = predictedPosition - muzzlePosition;
            //timeToHit = bulletPath.Length() / projectileSpeed;
            return predictedPosition;
        }

        Vector3D BulletDrop(double distanceFromTarget, double projectileMaxSpeed, Vector3D desiredDirection, Vector3D gravity) {
            double timeToTarget = distanceFromTarget / projectileMaxSpeed;
            desiredDirection -= 0.5 * gravity * timeToTarget * timeToTarget;
            return desiredDirection;
        }

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll) {
            desiredForwardVector = VectorMath.SafeNormalize(desiredForwardVector);
            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);
            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector)) {
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            } else {
                leftVector = VectorMath.SafeNormalize(leftVector);
                Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);
                MatrixD targetMatrix = MatrixD.Zero;//Create matrix
                targetMatrix.Forward = desiredForwardVector;
                targetMatrix.Left = leftVector;
                targetMatrix.Up = upVector;
                axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
                                    targetMatrix.M31 - targetMatrix.M13,
                                    targetMatrix.M12 - targetMatrix.M21);
                double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
            }
            if (Vector3D.IsZero(axis)) {
                angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
                yaw = angle;
                pitch = 0;
                roll = 0;
                return;
            }
            axis = VectorMath.SafeNormalize(axis);
            yaw = -axis.Y * angle;//Because gyros rotate about -X -Y -Z, we need to negate our angles
            pitch = -axis.X * angle;
            roll = -axis.Z * angle;
        }

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix) {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
            foreach (var thisGyro in gyroList) {
                if (thisGyro.Closed) { continue; }
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        bool GetMessages() {
            bool received = false;
            if (UNILISTENER.HasPendingMessage) {
                Dictionary<long, string> tempMissileIDs = new Dictionary<long, string>();
                Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
                while (UNILISTENER.HasPendingMessage) {
                    var igcMessage = UNILISTENER.AcceptMessage();
                    long missileId = igcMessage.Source;
                    if (igcMessage.Data is ImmutableArray<MyTuple<string, Vector3D, double, double>>) {
                        var data = (ImmutableArray<MyTuple<string, Vector3D, double, double>>)igcMessage.Data;
                        received = true;
                        var tup = MyTuple.Create(data[0].Item4, data[0].Item3, data[0].Item1);
                        if (!tempMissilesInfo.ContainsKey(missileId)) {
                            tempMissilesInfo.Add(missileId, tup);
                        }
                        if (!tempMissileIDs.ContainsKey(missileId)) {
                            tempMissileIDs.Add(missileId, data[0].Item1);
                        }
                    }
                    if (igcMessage.Data is MyTuple<string, string>) {//error message from missile
                        var data = (MyTuple<string, string>)igcMessage.Data;
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

        bool GetBroadcastMessages() {
            bool received = false;
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    var igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<string, bool>) {
                        var data = (MyTuple<string, bool>)igcMessage.Data;
                        string variable = data.Item1;
                        if (variable == "readyToFire") {
                            joltReady = data.Item2;
                            received = true;
                        }
                    }
                }
            }
            return received;
        }

        void ReadMessages() {
            missileLog.Clear();
            if (MissileIDs.Count() > 0) {
                missileLog.Append("Active Missiles: ").Append(MissileIDs.Count().ToString()).Append("\n");
            }
            foreach (var inf in missilesInfo) {
                missileLog.Append("Status: ").Append(inf.Value.Item3).Append(", Missile ID: ").Append(inf.Key.ToString()).Append("\n");
                if (inf.Value.Item3.Contains(commandUpdate) || inf.Value.Item3.Contains(commandSpiral)) {
                    missileLog.Append("Dist. From Target: ").Append(inf.Value.Item1.ToString("0.0")).Append(", ");
                }
                missileLog.Append("Speed: ").Append(inf.Value.Item2.ToString("0.0")).Append("\n");
            }
        }

        void SendMissileBroadcastMessage(string cmd) {
            Vector3D targetPos = targetPosition;
            long fakeId = 0;
            var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3, Vector3D>>>();
            var tuple = MyTuple.Create(MyTuple.Create(fakeId, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative), MyTuple.Create(targetInfo.Velocity, targetPos));
            immArray.Add(tuple);
            IGC.SendBroadcastMessage(missileAntennaTag, immArray.ToImmutable());
        }

        bool SendMissileUnicastMessage(string cmd, long id) {
            var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3, Vector3D>>>();
            var tuple = MyTuple.Create(MyTuple.Create(id, cmd, CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.WorldMatrix, creative), MyTuple.Create(targetInfo.Velocity, targetPosition));
            immArray.Add(tuple);
            bool uniMessageSent = IGC.SendUnicastMessage(id, missileAntennaTag, immArray.ToImmutable());
            if (!uniMessageSent) {
                LostMissileIDs.Add(id);
            }
            return uniMessageSent;
        }

        void SendBroadcastTargetMessage(bool targFound, Vector3D targPos, Vector3D targVel) {
            var tuple = MyTuple.Create(targFound, targPos, targVel);
            IGC.SendBroadcastMessage(navigatorTag, tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastGunsMessage(int weaponType, bool assaultCanShoot, bool artilleryCanShoot, bool railgunsCanShoot, bool smallRailgunsCanShoot) {
            var tuple = MyTuple.Create(weaponType, assaultCanShoot, artilleryCanShoot, railgunsCanShoot, smallRailgunsCanShoot);
            IGC.SendBroadcastMessage(navigatorTag, tuple, TransmissionDistance.ConnectedConstructs);
        }

        void RemoveLostMissiles() {
            Dictionary<long, string> TempMissileIDs = new Dictionary<long, string>();
            foreach (var entry in MissileIDs) {
                bool found = false;
                foreach (var id in LostMissileIDs) {
                    if (entry.Key == id) {
                        found = true;
                    }
                }
                if (!found) {
                    TempMissileIDs.Add(entry.Key, entry.Value);
                }
            }
            Dictionary<long, MyTuple<double, double, string>> tempMissilesInfo = new Dictionary<long, MyTuple<double, double, string>>();
            foreach (var entry in missilesInfo) {
                bool found = false;
                foreach (var id in LostMissileIDs) {
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
            UnlockShip();
            TurnAlarmOff();
            ResetGuns();
            ClearLogs();
            targetName = null;
            targetDiameter = 0;
            targetInfo = default(MyDetectedEntityInfo);
            targetPosition = default(Vector3D);
            selectedMissile = 1;
            doOnce = false;
            autoMissilesCounter = autoMissilesDelay + 1;
            missilesLoaded = false;
            fudgeFactor = 5;
            scanFudge = false;
            fudgeCount = 0;
            timeSinceLastLock = 0d;
            hasCenter = true;
            scanCenter = false;
            foreach (var id in MissileIDs) {
                if (!id.Value.Contains(commandLost)) {
                    SendMissileUnicastMessage(commandLost, id.Key);
                }
            }
            SendBroadcastTargetMessage(false, Vector3D.Zero, Vector3D.Zero);
        }

        void ActivateTargeter() {
            if (!doOnce) {
                TurnAlarmOn();
                doOnce = true;
                scanFudge = false;
                fudgeCount = 0;
                fudgeFactor = 5d;
                timeSinceLastLock = 0d;
            }
        }

        void UnlockShip() {
            foreach (IMyGyro block in GYROS) { block.GyroOverride = false; block.GyroPower = 1; }
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

        void ManageGuns(double timeSinceLastRun) {
            if (autoFire) {
                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                if (autoSwitchGuns) {
                    if (distanceFromTarget <= gunsRange) {
                        maxRangeOnce = true;
                        if (!decoyRanOnce && distanceFromTarget < 800d) {
                            decoyRanOnce = SHOOTERPB.TryRun(argFireDecoy);
                        }
                        if (weaponType != 2 && weaponType != 3 && (gatlingsOnce || autocannonOnce)) {
                            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                            foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = false; }
                            gatlingsOnce = false;
                            autocannonOnce = false;
                        }
                        rocketRange = 500d;
                        if (distanceFromTarget < railgunRange && railgunAmmoFound && railgunsCanShoot) {
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
                        } else if (distanceFromTarget < artilleryRange && artilleryAmmoFound && artilleryCanShoot) {
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
                        } else if (distanceFromTarget < smallRailgunRange && smallRailgunAmmoFound && smallRailgunsCanShoot) {
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
                        } else if (distanceFromTarget < assaultRange && assaultAmmoFound && assaultCanShoot) {
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
                        } else if (distanceFromTarget < autocannonRange && (autocannonAmmoFound || gatlingAmmoFound)) {
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
                        } else if (distanceFromTarget < rocketRange && missileAmmoFound && rocketsCanShoot) {
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
                        SHOOTERPB.TryRun(argFireJolt);
                    }
                } else {
                    if (distanceFromTarget < gunsRange) {
                        maxRangeOnce = true;
                        if (!decoyRanOnce && distanceFromTarget < autocannonRange) {
                            decoyRanOnce = SHOOTERPB.TryRun(argFireDecoy);
                        }
                        if (readyToFire) {
                            readyToFireOnce = true;
                            if (weaponType != 2 && weaponType != 3 && (gatlingsOnce || autocannonOnce)) {
                                foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                                foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = false; }
                                gatlingsOnce = false;
                                autocannonOnce = false;
                            }
                            rocketRange = 800d;
                            if (weaponType == 1 && distanceFromTarget < rocketRange && missileAmmoFound && rocketsCanShoot) {
                                cannotFireOnce = true;
                                if (!rocketsOnce) { SwitchGun(false, true, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ROCKETS, rocketDelay, ref rocketCount, ref rocketIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ROCKETS) { gun.Shoot(timeSinceLastRun); } }
                            } else if ((weaponType == 2 || weaponType == 3) && distanceFromTarget < autocannonRange && (autocannonAmmoFound || gatlingAmmoFound)) {
                                cannotFireOnce = true;
                                if (!gatlingsOnce && gatlingAmmoFound) {
                                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                                    gatlingsOnce = true;
                                }
                                if (!autocannonOnce && autocannonAmmoFound) {
                                    foreach (IMyUserControllableGun block in AUTOCANNONS) { block.Shoot = true; }
                                    autocannonOnce = true;
                                }
                            } else if (weaponType == 4 && distanceFromTarget < assaultRange && assaultAmmoFound && assaultCanShoot) {
                                cannotFireOnce = true;
                                if (!assaultOnce) { SwitchGun(true, false, true, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ASSAULT, assaultDelay, ref assaultCount, ref assaultIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ASSAULT) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 7 && distanceFromTarget < smallRailgunRange && smallRailgunAmmoFound && smallRailgunsCanShoot) {
                                cannotFireOnce = true;
                                if (!smallRailgunsOnce) { SwitchGun(true, true, true, true, false); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(SMALLRAILGUNS, smallRailgunsDelay, ref smallRailgunsCount, ref smallRailgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in SMALLRAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 5 && distanceFromTarget < artilleryRange && artilleryAmmoFound && artilleryCanShoot) {
                                cannotFireOnce = true;
                                if (!artilleryOnce) { SwitchGun(true, true, false, true, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(ARTILLERY, artilleryDelay, ref artilleryCount, ref artilleryIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in ARTILLERY) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 6 && distanceFromTarget < railgunRange && railgunAmmoFound && railgunsCanShoot) {
                                cannotFireOnce = true;
                                if (!railgunsOnce) { SwitchGun(true, true, true, false, true); }
                                if (sequenceWeapons) {
                                    SequenceWeapons(RAILGUNS, railgunsDelay, ref railgunsCount, ref railgunsIndex, timeSinceLastRun);
                                } else { foreach (Gun gun in RAILGUNS) { gun.Shoot(timeSinceLastRun); } }
                            } else if (weaponType == 0 && joltReady) {
                                SHOOTERPB.TryRun(argFireJolt);
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
                rocketDelay = (int)Math.Ceiling(rocketROF / (double)ROCKETS.Count);
                if (rocketDelay == 0) { rocketDelay = 1; }
            }
            if (ASSAULT.Count > 0) {
                assaultDelay = (int)Math.Ceiling(assaultROF / (double)ASSAULT.Count);
                if (assaultDelay == 0) { assaultDelay = 1; }
            }
            if (ARTILLERY.Count > 0) {
                artilleryDelay = (int)Math.Ceiling(artilleryROF / (double)ARTILLERY.Count);
                if (artilleryDelay == 0) { artilleryDelay = 1; }
            }
            if (RAILGUNS.Count > 0) {
                railgunsDelay = (int)Math.Ceiling(railgunsROF / (double)RAILGUNS.Count);
                if (railgunsDelay == 0) { railgunsDelay = 1; }
            }
            if (SMALLRAILGUNS.Count > 0) {
                smallRailgunsDelay = (int)Math.Ceiling(smallRailgunsROF / (double)SMALLRAILGUNS.Count);
                if (smallRailgunsDelay == 0) { smallRailgunsDelay = 1; }
            }
        }

        bool LoadMissiles() {
            List<IMyShipConnector> MISSILECONNECTORS = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(MISSILECONNECTORS, b => b.CustomName.Contains(missilePrefix));
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
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(missilePrefix) && block is IMyGasGenerator);
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
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(missilePrefix) && (block is IMyGasGenerator || block is IMySmallGatlingGun || block is IMyLargeGatlingTurret));//&& !(block is IMyGasTank)); IMyLargeTurretBase
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

        void WriteInfo() {
            foreach (IMyTextSurface surface in SURFACES) {
                StringBuilder text = new StringBuilder("");
                string gun;
                switch (weaponType) {//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
                    case 0: gun = "None"; break;
                    case 1: gun = "Rockets"; break;
                    case 2: gun = "Gatlings"; break;
                    case 3: gun = "Autocannon"; break;
                    case 4: gun = "Assault"; break;
                    case 5: gun = "Artillery"; break;
                    case 6: gun = "Railguns"; break;
                    case 7: gun = "Small Railguns"; break;
                    default: gun = "None"; break;
                }
                text.Append("Guns: " + gun + ", ");
                text.Append("autoFire: " + autoFire + ", autoSwitchGuns: " + autoSwitchGuns + "\n");
                text.Append("PayLoad: " + (selectedPayLoad == 0 ? "Missiles" : "Drones") + ", autoMissiles: " + autoMissiles + "\n");
                text.Append(targetLog.ToString());
                text.Append(missileLog.ToString());
                surface.WriteText(text);
            }
        }

        void ReadTargetInfo() {
            targetLog.Clear();
            targetLog.Append("Launching: ").Append(missileAntennasName).Append(selectedMissile.ToString());
            if (!missilesLoaded) {
                targetLog.Append(" Not Loaded\n");
            } else { targetLog.Append(" Loaded\n"); }
            targetLog.Append("Target Name: ").Append(targetInfo.Name).Append(", ");
            string targX = targetInfo.Position.X.ToString("0.00");
            string targY = targetInfo.Position.Y.ToString("0.00");
            string targZ = targetInfo.Position.Z.ToString("0.00");
            targetLog.Append($"X:{targX} Y:{targY} Z:{targZ}").Append("\n");
            targetLog.Append("Target Speed: ").Append(targetInfo.Velocity.Length().ToString("0.0")).Append(", ");
            double targetDistance = Vector3D.Distance(targetInfo.Position, CONTROLLER.CubeGrid.WorldVolume.Center);
            targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");
        }

        void ClearLogs() {
            targetLog.Clear();
            missileLog.Clear();
            foreach (IMyTextSurface surface in SURFACES) { surface.WriteText(""); }
        }

        void GetBlocks() {
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
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, b => b.CustomName.Contains(gatlingsName));
            AUTOCANNONS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(AUTOCANNONS, block => block.CustomName.Contains(autocannonsName));
            List<IMyUserControllableGun> guns = new List<IMyUserControllableGun>();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, b => b.CustomName.Contains(rocketsName));
            ROCKETS.Clear();
            foreach (IMyUserControllableGun gun in guns) { ROCKETS.Add(new Gun(gun, rocketRounds, rocketReload, rocketROF)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains(railgunsName));
            RAILGUNS.Clear();
            foreach (IMyUserControllableGun gun in guns) { RAILGUNS.Add(new Gun(gun, singleRound, railgunsReload, railgunsROF)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains(artilleryName));
            ARTILLERY.Clear();
            foreach (IMyUserControllableGun gun in guns) { ARTILLERY.Add(new Gun(gun, singleRound, artilleryReload, artilleryROF)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains(smallRailgunsName));
            SMALLRAILGUNS.Clear();
            foreach (IMyUserControllableGun gun in guns) { SMALLRAILGUNS.Add(new Gun(gun, singleRound, smallRailgunsReload, smallRailgunsROF)); }
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains(assaultName));
            ASSAULT.Clear();
            foreach (IMyUserControllableGun gun in guns) { ASSAULT.Add(new Gun(gun, singleRound, assaultReload, assaultROF)); }
            CARGOS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CARGOS, b => b.CustomName.Contains(cargoName));
            CARGOINVENTORIES.Clear();
            CARGOINVENTORIES.AddRange(CARGOS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsTargetName));
            foreach (IMyTextPanel panel in panels) { SURFACES.Add(panel as IMyTextSurface); }
            ANTENNA = GridTerminalSystem.GetBlockWithName(antennasName) as IMyRadioAntenna;
            CONTROLLER = CONTROLLERS[0];
            SHOOTERPB = GridTerminalSystem.GetBlockWithName(decoyName) as IMyProgrammableBlock;
            DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
        }

        void SetBlocks() {
            foreach (IMyCameraBlock cam in LIDARS) {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }
            foreach (IMyGyro block in GYROS) {
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

        void GetMissileAntennas() {
            MISSILEANTENNAS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(MISSILEANTENNAS, b => b.CustomName.Contains(missileAntennasName));
        }

        void SetMissileAntennas() {
            foreach (IMyRadioAntenna block in MISSILEANTENNAS) {
                block.EnableBroadcasting = false;
                block.Enabled = false;
            }
        }

        public static class VectorMath {
            public static Vector3D SafeNormalize(Vector3D a) {
                if (Vector3D.IsZero(a)) { return Vector3D.Zero; }
                if (Vector3D.IsUnit(ref a)) { return a; }
                return Vector3D.Normalize(a);
            }

            public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1) {//reflect a over b
                Vector3D project_a = Projection(a, b);
                Vector3D reject_a = a - project_a;
                return project_a - reject_a * rejectionFactor;
            }

            public static Vector3D Rejection(Vector3D a, Vector3D b) {//reject a on b
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return Vector3D.Zero; }
                return a - a.Dot(b) / b.LengthSquared() * b;
            }

            public static Vector3D Projection(Vector3D a, Vector3D b) {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return Vector3D.Zero; }
                return a.Dot(b) / b.LengthSquared() * b;
            }

            public static double ScalarProjection(Vector3D a, Vector3D b) {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return 0; }
                if (Vector3D.IsUnit(ref b)) { return a.Dot(b); }
                return a.Dot(b) / b.Length();
            }

            public static double AngleBetween(Vector3D a, Vector3D b) {//returns radians
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) {
                    return 0;
                } else { return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1)); }
            }

            public static double CosBetween(Vector3D a, Vector3D b) {//returns radians
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) {
                    return 0;
                } else { return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1); }
            }
        }

        void InitPIDControllers() {
            yawController = new PID(yawAimP, yawAimI, yawAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(pitchAimP, pitchAimI, pitchAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(rollAimP, rollAimI, rollAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
        }

        public class PID {
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

            public PID(double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _lowerBound = lowerBound;
                _upperBound = upperBound;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecay = false;
            }

            public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control(double error) {
                var errorDerivative = (error - _lastError) * _inverseTimeStep;//Compute derivative term
                if (_firstRun) {
                    errorDerivative = 0;
                    _firstRun = false;
                }
                if (!_integralDecay) {//Compute integral term
                    _errorSum += error * _timeStep;
                    if (_errorSum > _upperBound) {//Clamp integral term
                        _errorSum = _upperBound;
                    } else if (_errorSum < _lowerBound) {
                        _errorSum = _lowerBound;
                    }
                } else {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }
                _lastError = error;//Store this error as last error
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;//Construct output
                return this.Value;
            }

            public double Control(double error, double timeStep) {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                return Control(error);
            }

            public void Reset() {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
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
