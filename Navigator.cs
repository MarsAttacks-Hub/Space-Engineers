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
using SpaceEngineers.Game.Entities.Blocks;

namespace IngameScript {
    partial class Program : MyGridProgram {
        //TODO add generate orbital gps function
        //generate orbital gps above target 
        //land function
        //NAVIGATOR

        readonly string controllersName = "[CRX] Controller";
        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string jumpersName = "[CRX] Jump";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string turretsName = "[CRX] Turret";
        readonly string rotorsName = "Rotor_MD_A";
        readonly string rotorsInvName = "Rotor_MD_B";
        readonly string plusXname = "Merge_MD-X";
        readonly string plusYname = "Merge_MD+Z";
        readonly string plusZname = "Merge_MD+Y";
        readonly string minusXname = "Merge_MD+X";
        readonly string minusYname = "Merge_MD-Z";
        readonly string minusZname = "Merge_MD-Y";
        readonly string thrustersName = "[CRX] HThruster";
        readonly string upThrustersName = "UP";
        readonly string downThrustersName = "DOWN";
        readonly string leftThrustersName = "LEFT";
        readonly string rightThrustersName = "RIGHT";
        readonly string forwardThrustersName = "FORWARD";
        readonly string backwardThrustersName = "BACKWARD";
        readonly string deadManPanelName = "[CRX] LCD DeadMan Toggle";
        readonly string idleThrusterPanelName = "[CRX] LCD IdleThrusters Toggle";
        readonly string lcdsRangeFinderName = "[CRX] LCD RangeFinder";
        readonly string debugPanelName = "[CRX] Debug";
        readonly string painterName = "[CRX] PB Painter";
        readonly string shooterName = "[CRX] PB Shooter";
        readonly string managerName = "[CRX] PB Manager";

        readonly string sectionTag = "RangeFinderSettings";
        readonly string cockpitRangeFinderKey = "cockpitRangeFinderSurface";

        const string argRangeFinder = "RangeFinder";
        const string argAimTarget = "AimTarget";
        const string argChangePlanet = "ChangePlanet";
        const string argDeadMan = "DeadMan";
        const string argMagneticDrive = "ToggleMagneticDrive";
        const string argIdleThrusters = "ToggleIdleThrusters";
        const string argSetPlanet = "SetPlanet";

        const string argSunchaseOff = "SunchaseOff";
        const string argUnlockFromTarget = "Clear";
        const string argLockTarget = "Lock";
        const string argLaunchDecoy = "LaunchDecoy";
        const string argGyroStabilizeOff = "StabilizeOff";
        const string argGyroStabilizeOn = "StabilizeOn";

        readonly double aimP = 1d;
        readonly double aimI = 0d;
        readonly double aimD = 1d;
        readonly double integralWindupLimit = 0d;
        readonly bool useRoll = true;
        readonly double escapeDistance = 250d;
        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;
        readonly double targetStopDistance = 50d;
        readonly double escapeStopDistance = 50d;
        readonly double returnStopDistance = 50d;
        readonly float maxSpeed = 105f;
        readonly float minSpeed = 2f;
        readonly float deadManMinSpeed = 0.1f;
        readonly float targetVel = 29 * rpsOverRpm;
        readonly float syncSpeed = 1 * rpsOverRpm;
        readonly int tickDelay = 50;
        readonly int escapeDelay = 10;
        int impactDetectionDelay = 5;

        bool magneticDrive = true;
        bool sunChaseOff = false;
        bool controlDampeners = true;
        bool idleThrusters = false;
        bool aimTarget = false;
        bool useGyrosToStabilize = true;
        string selectedPlanet = "";
        int impactDetectionCount = 5;
        int escapeCount = 10;
        int cockpitRangeFinderSurface = 4;
        int planetSelector = 0;
        int tickCount = 0;
        double maxScanRange = 0d;
        double altitudeToKeep = 0d;
        bool unlockOnce = true;
        bool magneticDriveOnce = true;
        bool deadManOnce = false;
        bool switchOnce = false;
        bool setOnce = false;
        bool initAutoThrustOnce = true;
        bool runOnce = false;
        bool returnOnce = true;
        bool lockTargetOnce = true;
        bool launchDecoyOnce = true;
        bool stabilizeOnce = true;

        const float globalTimestep = 10.0f / 60.0f;
        const float rpsOverRpm = (float)(Math.PI / 30);
        const float circle = (float)(2 * Math.PI);
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 0.1d;//degrees

        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyMotorStator> ROTORS = new List<IMyMotorStator>();
        public List<IMyMotorStator> ROTORSINV = new List<IMyMotorStator>();
        public List<IMyShipMergeBlock> MERGESPLUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSZ = new List<IMyShipMergeBlock>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> UPTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> DOWNTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> LEFTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> RIGHTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> FORWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> BACKWARDTHRUSTERS = new List<IMyThrust>();

        IMyShipController CONTROLLER = null;
        IMyRemoteControl REMOTE;
        IMyThrust UPTHRUST;
        IMyThrust DOWNTHRUST;
        IMyThrust LEFTTHRUST;
        IMyThrust RIGHTTHRUST;
        IMyThrust FORWARDTHRUST;
        IMyThrust BACKWARDTHRUST;
        IMyProgrammableBlock MANAGERPB;
        IMyProgrammableBlock PAINTERPB;
        IMyProgrammableBlock SHOOTERPB;
        IMyTextPanel LCDDEADMAN;
        IMyTextPanel LCDIDLETHRUSTERS;

        MyDetectedEntityInfo targetInfo;
        Vector3D targetPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D escapePosition = Vector3D.Zero;
        Vector3D lastForwardVector = Vector3D.Zero;
        Vector3D lastUpVector = Vector3D.Zero;

        public StringBuilder jumpersLog = new StringBuilder("");
        public StringBuilder lidarsLog = new StringBuilder("");
        public StringBuilder targetLog = new StringBuilder("");

        PID yawController;
        PID pitchController;
        PID rollController;

        readonly MyIni myIni = new MyIni();

        readonly Dictionary<String, MyTuple<Vector3D, double, double>> planetsList = new Dictionary<String, MyTuple<Vector3D, double, double>>() {
            //string PlanetName, Vector3D PlanetPosition, double PlanetRadius, double AtmosphereDistance
            { "Earth",  MyTuple.Create(new Vector3D(0,          0,          0),         61250d,     41843.4d) },
            { "Moon",   MyTuple.Create(new Vector3D(16384,      136384,     -113616),   9500d,      2814.416d) },
            { "Triton", MyTuple.Create(new Vector3D(-284463.5,  -2434463.5, 365536.5),  40127.5d,   33735.39d) },
            { "Mars",   MyTuple.Create(new Vector3D(1031072,    131072,     1631072),   61500d,     40053.3d) },
            { "Europa", MyTuple.Create(new Vector3D(916384,     16384,      1616384),   9600d,      12673.088d) },
            { "Alien",  MyTuple.Create(new Vector3D(131072,     131072,     5731072),   60000d,     44506.7d) },
            { "Titan",  MyTuple.Create(new Vector3D(36384,      226384,     5796384),   9500d,      2814.416d) },
            { "Pertam", MyTuple.Create(new Vector3D(-3967231.5, -32231.5,   -767231.5), 30000d,     18500d) }
        };

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();

            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }

            maxScanRange = LIDARS[0].RaycastDistanceLimit;

            selectedPlanet = planetsList.ElementAt(0).Key;

            InitPIDControllers();

            if (useGyrosToStabilize) {
                Me.CustomData = "GyroStabilize=true";//TODO
            } else {
                Me.CustomData = "GyroStabilize=false";//TODO
            }
        }

        public void Main(string arg) {
            try {
                Echo($"CONTROLLERS:{CONTROLLERS.Count}");
                Echo($"REMOTES:{REMOTES.Count}");
                Echo($"COCKPITS:{COCKPITS.Count}");
                Echo($"GYROS:{GYROS.Count}");
                Echo($"THRUSTERS:{THRUSTERS.Count}");
                Echo($"JUMPERS:{JUMPERS.Count}");
                Echo($"LIDARS:{LIDARS.Count}");
                Echo($"ALARMS:{ALARMS.Count}");
                Echo($"SURFACES:{SURFACES.Count}");
                Echo($"ROTORS:{ROTORS.Count}");
                Echo($"ROTORSINV:{ROTORSINV.Count}");
                Echo($"MERGESPLUSX:{MERGESPLUSX.Count}");
                Echo($"MERGESPLUSY:{MERGESPLUSY.Count}");
                Echo($"MERGESPLUSZ:{MERGESPLUSZ.Count}");
                Echo($"MERGESMINUSX:{MERGESMINUSX.Count}");
                Echo($"MERGESMINUSY:{MERGESMINUSY.Count}");
                Echo($"MERGESMINUSZ:{MERGESMINUSZ.Count}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                }

                if (aimTarget) {
                    if (!runOnce) {
                        useGyrosToStabilize = false;
                        Me.CustomData = "GyroStabilize=false";//TODO
                        if (MANAGERPB.CustomData.Contains("SunChaser=true")) {
                            sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                        }
                        runOnce = true;
                    }
                    if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true")) {
                        sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                    }

                    AimAtTarget(targetPosition);
                } else {
                    if (runOnce) {
                        useGyrosToStabilize = true;
                        Me.CustomData = "GyroStabilize=true";//TODO
                        runOnce = false;
                    }
                }

                if (!IsPiloted(true)) {
                    TurretsImpactDetection();

                    AlignToGround(REMOTE);
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(targetPosition)) {
                    double dist = Vector3D.Distance(targetPosition, REMOTE.GetPosition());
                    if (dist < targetStopDistance) {
                        REMOTE.SetAutoPilotEnabled(false);
                        targetPosition = Vector3D.Zero;
                    }
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(escapePosition)) {
                    double dist = Vector3D.Distance(escapePosition, REMOTE.GetPosition());
                    if (dist < escapeStopDistance) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        escapePosition = Vector3D.Zero;
                    }
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition) && Vector3D.IsZero(escapePosition)) {
                    double dist = Vector3D.Distance(returnPosition, REMOTE.GetPosition());
                    if (dist < returnStopDistance) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        returnPosition = Vector3D.Zero;
                        returnOnce = true;
                    }
                }

                ReadLidarInfos();
                ReadJumpersInfos();

                WriteInfo();

                if (magneticDrive) {
                    if (magneticDriveOnce) {
                        InitMagneticDrive();
                        magneticDriveOnce = false;
                    }

                    bool isControlled = GetController();

                    if (!isControlled) {
                        if (!setOnce) {
                            IdleMagneticDrive();
                            setOnce = true;
                        }
                    } else {
                        if (setOnce) {
                            InitMagneticDrive();
                            if (MANAGERPB != null) {
                                if (MANAGERPB.CustomData.Contains("SunChaser=true")) {
                                    sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                                }
                            }
                            setOnce = false;
                        }
                        if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true")) {
                            sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                        }

                        SyncRotors();

                        if (REMOTE.IsAutoPilotEnabled) {
                            AutoMagneticDrive();
                            GyroStabilize(REMOTE);
                        } else {
                            if (!initAutoThrustOnce) {
                                foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                                initAutoThrustOnce = true;
                            }

                            MagneticDrive();
                            GyroStabilize(CONTROLLER);
                        }
                    }
                } else {
                    if (!magneticDriveOnce) {
                        IdleMagneticDrive();
                        magneticDriveOnce = true;
                    }
                    if (tickCount == tickDelay) {
                        if (controlDampeners) {
                            DeadMan();
                            LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                        } else { LCDDEADMAN.BackgroundColor = new Color(0, 0, 0); }
                        if (idleThrusters) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255); } else { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }

                        tickCount = 0;
                    }
                    tickCount++;
                }
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case argRangeFinder: RangeFinder(); break;
                case argChangePlanet:
                    planetSelector++;
                    if (planetSelector > planetsList.Count()) {
                        planetSelector = 0;
                    }
                    selectedPlanet = planetsList.ElementAt(planetSelector).Key;
                    break;
                case argAimTarget: if (!Vector3D.IsZero(targetPosition)) { aimTarget = true; }; break;
                case argIdleThrusters:
                    idleThrusters = !idleThrusters;
                    if (idleThrusters) {
                        foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = false; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0);
                    }
                    break;
                case argGyroStabilizeOn:
                    useGyrosToStabilize = true;
                    Me.CustomData = "GyroStabilize=true";//TODO
                    break;
                case argGyroStabilizeOff:
                    useGyrosToStabilize = false;
                    Me.CustomData = "GyroStabilize=false";//TODO
                    break;
                case argDeadMan:
                    controlDampeners = !controlDampeners;
                    if (controlDampeners) {
                        LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        LCDDEADMAN.BackgroundColor = new Color(0, 0, 0);
                    }
                    break;
                case argMagneticDrive:
                    magneticDrive = !magneticDrive;
                    break;
                case argSetPlanet:
                    if (!aimTarget) {
                        MyTuple<Vector3D, double, double> planet;
                        planetsList.TryGetValue(selectedPlanet, out planet);
                        double planetSize = planet.Item2 + planet.Item3 + 1000d;
                        Vector3D safeJumpPosition = planet.Item1 - (Vector3D.Normalize(planet.Item1 - REMOTE.GetPosition()) * planetSize);

                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(safeJumpPosition, selectedPlanet);

                        double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);

                        JUMPERS[0].JumpDistanceMeters = (float)distance;

                        targetPosition = safeJumpPosition;

                        targetLog.Clear();
                        targetLog.Append("Safe Dist. for: ").Append(selectedPlanet).Append("\n");

                        string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                        targetLog.Append(safeJumpGps).Append("\n");

                        targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");

                        targetLog.Append("Radius: ").Append(planet.Item2.ToString("0.0")).Append(", ");
                        targetLog.Append("Diameter: ").Append((planet.Item2 * 2d).ToString("0.0")).Append("\n");

                        targetLog.Append("Atmo. Height: ").Append(planet.Item3.ToString("0.0")).Append("\n");
                    }
                    break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit) {
            if (!cockpit.CustomData.Contains(sectionTag)) {
                cockpit.CustomData += $"[{sectionTag}]\n{cockpitRangeFinderKey}={cockpitRangeFinderSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);

            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitRangeFinderKey).ToString())) {
                cockpitRangeFinderSurface = myIni.Get(sectionTag, cockpitRangeFinderKey).ToInt32();

                SURFACES.Add(cockpit.GetSurface(cockpitRangeFinderSurface));
            }
        }

        void RangeFinder() {
            targetLog.Clear();

            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);

            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);

            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                foreach (var block in ALARMS) {
                    block.Play();
                }
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    Vector3D planetCenter = TARGET.Position;
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    double planetRadius = Vector3D.Distance(planetCenter, hitPosition);
                    string planetName = "Earth";
                    MyTuple<Vector3D, double, double> planet;
                    planetsList.TryGetValue(planetName, out planet);
                    double aRadius = Math.Abs(planet.Item2 - planetRadius);
                    foreach (var planetElement in planetsList) {
                        double dictPlanetRadius = planetElement.Value.Item2;
                        double bRadius = Math.Abs(dictPlanetRadius - planetRadius);
                        if (bRadius < aRadius) {
                            planetName = planetElement.Key;
                            aRadius = bRadius;
                        }
                    }
                    selectedPlanet = planetName;
                    planetsList.TryGetValue(planetName, out planet);
                    double atmosphereRange = planet.Item3 + 1000d;

                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * atmosphereRange);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, selectedPlanet);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);

                    JUMPERS[0].JumpDistanceMeters = (float)distance;

                    targetPosition = safeJumpPosition;

                    targetLog.Append("Safe Dist. for: ").Append(selectedPlanet).Append("\n");

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");

                    double targetRadius = Vector3D.Distance(TARGET.Position, hitPosition);
                    targetLog.Append("Radius: ").Append(targetRadius.ToString("0.0")).Append(", ");
                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");

                    double targetGroundDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Ground Dist.: ").Append(targetGroundDistance.ToString("0.0")).Append("\n");

                    double targetAtmoHeight = Vector3D.Distance(hitPosition, safeJumpPosition);
                    targetLog.Append("Atmo. Height: ").Append(targetAtmoHeight.ToString("0.0")).Append("\n");
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Asteroid");

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);

                    JUMPERS[0].JumpDistanceMeters = (float)distance;

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Asteroid:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * enemySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);

                    JUMPERS[0].JumpDistanceMeters = (float)distance;

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");

                    double targetDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                } else {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);

                    JUMPERS[0].JumpDistanceMeters = (float)distance;

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");

                    double targetDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
            } else {
                targetLog.Append("Nothing Detected!\n");
            }
        }

        bool AimAtTarget(Vector3D targetPos) {
            bool aligned = false;
            Vector3D aimDirection = targetPos - REMOTE.GetPosition();

            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, REMOTE.WorldMatrix.Up, REMOTE.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, REMOTE.WorldMatrix);

            Vector3D forwardVec = REMOTE.WorldMatrix.Forward;
            double angle = VectorMath.AngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= angleTolerance) {
                aimTarget = false;
                aligned = true;
                UnlockGyros();
            }
            return aligned;
        }

        void TurretsImpactDetection() {
            if (impactDetectionCount == impactDetectionDelay) {
                bool targetFound = false;
                foreach (IMyLargeTurretBase turret in TURRETS) {
                    MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                    if (!targ.IsEmpty()) {
                        if (IsValidTarget(ref targ)) {
                            targetInfo = targ;
                            targetFound = true;
                            break;
                        }
                    }
                }
                if (!targetFound) {
                    if (!lockTargetOnce) {
                        useGyrosToStabilize = true;
                        Me.CustomData = "GyroStabilize=true";//TODO
                        stabilizeOnce = true;
                        PAINTERPB.TryRun(argUnlockFromTarget);
                        lockTargetOnce = true;
                    }
                    if (!launchDecoyOnce) {
                        launchDecoyOnce = true;
                    }
                    if (!Vector3D.IsZero(returnPosition)) {
                        escapePosition = Vector3D.Zero;
                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(returnPosition, "returnPosition");
                        REMOTE.SetAutoPilotEnabled(true);
                        returnOnce = true;
                    }
                } else {
                    bool aligned = false;
                    if (lockTargetOnce) {
                        if (stabilizeOnce) {
                            useGyrosToStabilize = false;
                            Me.CustomData = "GyroStabilize=false";//TODO
                            if (MANAGERPB.CustomData.Contains("SunChaser=true")) {
                                sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                            }
                            stabilizeOnce = false;
                        }
                        aligned = AimAtTarget(targetInfo.Position);
                        impactDetectionDelay = 1;
                    }
                    if (aligned) {
                        PAINTERPB.TryRun(argLockTarget);
                        lockTargetOnce = false;
                        impactDetectionDelay = 5;
                    }
                    if (launchDecoyOnce) {
                        SHOOTERPB.TryRun(argLaunchDecoy);
                        launchDecoyOnce = false;
                    }
                    CheckCollisions(targetInfo.Position, targetInfo.Velocity);
                }
                impactDetectionCount = 0;
            }
            impactDetectionCount++;
        }

        void CheckCollisions(Vector3D targetPos, Vector3D targetVelocity) {
            BoundingBoxD gridLocalBB = new BoundingBoxD(REMOTE.CubeGrid.Min * REMOTE.CubeGrid.GridSize, REMOTE.CubeGrid.Max * REMOTE.CubeGrid.GridSize);
            MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(gridLocalBB, REMOTE.CubeGrid.WorldMatrix);
            double time = 80.0d;
            Vector3D targetFuturePosition = targetPos + (targetVelocity * time);
            LineD line = new LineD(targetPos, targetFuturePosition);
            double? hitDist = obb.Intersects(ref line);
            if (hitDist.HasValue) {
                if (returnOnce) {
                    if (Vector3D.IsZero(returnPosition)) {
                        returnPosition = REMOTE.GetPosition();
                    }
                    returnOnce = false;
                }
                if (escapeCount == escapeDelay) {
                    Vector3D escapePos = targetPos + (Vector3D.Normalize(targetPos - REMOTE.GetPosition()) * escapeDistance);
                    escapePosition = Vector3D.Cross(escapePos, REMOTE.WorldMatrix.Down);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(escapePosition, "escapePosition");
                    REMOTE.SetAutoPilotEnabled(true);
                    escapeCount = 0;
                }
                escapeCount++;
            }
        }

        void AlignToGround(IMyShipController controller) {//TODO align upsideDown
            Vector3D gravityVec = controller.GetNaturalGravity();
            if (!Vector3D.IsZero(gravityVec)) {
                MatrixD matrix = controller.WorldMatrix;
                Vector3D leftVec = Vector3D.Cross(matrix.Forward, gravityVec);
                Vector3D horizonVec = Vector3D.Cross(gravityVec, leftVec);
                double pitchAngle, rollAngle, yawAngle;
                GetRotationAnglesSimultaneous(horizonVec, -gravityVec, matrix, out pitchAngle, out yawAngle, out rollAngle);

                yawController.Reset();
                double pitchSpeed = pitchController.Control(pitchAngle);
                double rollSpeed = rollController.Control(rollAngle);

                ApplyGyroOverride(pitchSpeed, 0, rollSpeed, GYROS, matrix);

                bool forwardAligned = false;
                bool upAligned = false;
                double angle = VectorMath.AngleBetween(matrix.Forward, horizonVec);
                if (angle * rad2deg <= 1d) {//angleTolerance
                    forwardAligned = true;
                }
                angle = VectorMath.AngleBetween(matrix.Up, -gravityVec);
                if (angle * rad2deg <= 1d) {//angleTolerance
                    upAligned = true;
                }
                if (forwardAligned && upAligned) {
                    UnlockGyros();
                }
            }
        }

        Vector3 KeepAltitude(Vector3 dir, IMyShipController controller) {
            Vector3D gravity = controller.GetNaturalGravity();
            if (!Vector3D.IsZero(gravity) && idleThrusters) {
                if (!Vector3.IsZero(dir)) {
                    double altitude;
                    controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                    if (altitudeToKeep == 0d) {
                        altitudeToKeep = altitude;
                    }
                    if (altitude < altitudeToKeep - 15d) {
                        Vector3D gravTransformed = -Vector3D.Sign(Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix)));
                        dir += Vector3D.IsZeroVector(dir) * gravTransformed;
                    } else if (altitude > altitudeToKeep + 15d) {
                        Vector3D gravTransformed = Vector3D.Sign(Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix)));
                        dir += Vector3D.IsZeroVector(dir) * gravTransformed;
                    }
                } else {
                    altitudeToKeep = 0d;
                }
            }
            return dir;
        }

        void DeadMan() {
            bool undercontrol = IsPiloted(true);
            if (!undercontrol) {
                IMyShipController cntrllr = null;
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.CanControlShip) {
                        cntrllr = block;
                        break;
                    }
                }
                if (cntrllr != null) {
                    double speed = cntrllr.GetShipSpeed();
                    if (speed > deadManMinSpeed) {
                        foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = true; }
                        cntrllr.DampenersOverride = true;
                    } else {
                        if (!deadManOnce) {
                            if (idleThrusters) {
                                foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = false; }
                            }
                            deadManOnce = true;
                        }
                    }
                }
            } else {
                if (deadManOnce) {
                    foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = true; }
                    deadManOnce = false;
                }
            }
        }

        bool GetController() {
            if (CONTROLLER != null && (!CONTROLLER.IsUnderControl || !CONTROLLER.CanControlShip || !CONTROLLER.ControlThrusters)) {
                CONTROLLER = null;
            }
            if (CONTROLLER == null) {
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.IsUnderControl && block.IsFunctional && block.CanControlShip && block.ControlThrusters && !(block is IMyRemoteControl)) {
                        CONTROLLER = block;
                    }
                }
            }
            bool controlled;
            if (CONTROLLER == null) {
                controlled = false;
                if (REMOTE.IsAutoPilotEnabled) {
                    CONTROLLER = REMOTE;
                    controlled = true;
                } else {
                    IMyShipController controller = null;
                    foreach (IMyShipController contr in CONTROLLERS) {
                        if (contr.IsFunctional && contr.CanControlShip && contr.ControlThrusters && !(contr is IMyRemoteControl)) {
                            controller = contr;
                        }
                    }
                    if (CONTROLLER == null) { controller = REMOTE; }
                    Vector3D velocityVec = controller.GetShipVelocities().LinearVelocity;
                    double speed = velocityVec.Length();
                    if (speed > 1) {
                        CONTROLLER = controller;
                        controlled = true;
                    }
                    if (!Vector3D.IsZero(controller.GetNaturalGravity())) {
                        CONTROLLER = controller;
                        controlled = true;
                    }
                }
            } else {
                controlled = true;
            }
            return controlled;
        }

        void SyncRotors() {
            float angle = 0;
            foreach (IMyMotorStator rotor in ROTORS) { angle += rotor.Angle; }
            foreach (IMyMotorStator rotor in ROTORSINV) { angle += circle - rotor.Angle; }
            angle /= ROTORS.Count() + ROTORSINV.Count();
            float angleInv = circle - angle;
            foreach (IMyMotorStator rotor in ROTORS) {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angle, Smallest(rotorAngle - angle + circle, rotorAngle - angle - circle));
                if (asyncAngle > 0) {
                    rotor.TargetVelocityRad = (targetVel - syncSpeed);
                } else {
                    rotor.TargetVelocityRad = (targetVel + syncSpeed);
                }
            }
            foreach (IMyMotorStator rotor in ROTORSINV) {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angleInv, Smallest(rotorAngle - angleInv + circle, rotorAngle - angleInv - circle));
                if (asyncAngle > 0) {
                    rotor.TargetVelocityRad = (-targetVel - syncSpeed);
                } else {
                    rotor.TargetVelocityRad = (-targetVel + syncSpeed);
                }
            }
        }

        void InitMagneticDrive() {
            foreach (IMyMotorStator block in ROTORS) { block.Enabled = true; }
            foreach (IMyMotorStator block in ROTORSINV) { block.Enabled = true; }
            foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
        }

        void IdleMagneticDrive() {
            SetPow(Vector3D.Zero);
            foreach (IMyMotorStator block in ROTORS) {
                block.TargetVelocityRPM = 0;
                block.Enabled = false;
            }
            foreach (IMyMotorStator block in ROTORSINV) {
                block.TargetVelocityRPM = 0;
                block.Enabled = false;
            }
            if (idleThrusters) {
                foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = false; }
            }
        }

        IMyThrust AutopilotThrustInitializer(List<IMyThrust> thrusters) {
            IMyThrust thruster = null;
            int i = 0;
            foreach (IMyThrust thrust in thrusters) {
                if (i == 0) {
                    thruster = thrust;
                    thrust.Enabled = true;
                } else {
                    thrust.Enabled = false;
                }
                i++;
            }
            return thruster;
        }

        void AutoMagneticDrive() {
            if (initAutoThrustOnce) {
                UPTHRUST = AutopilotThrustInitializer(UPTHRUSTERS);
                DOWNTHRUST = AutopilotThrustInitializer(DOWNTHRUSTERS);
                LEFTTHRUST = AutopilotThrustInitializer(LEFTTHRUSTERS);
                RIGHTTHRUST = AutopilotThrustInitializer(RIGHTTHRUSTERS);
                FORWARDTHRUST = AutopilotThrustInitializer(FORWARDTHRUSTERS);
                BACKWARDTHRUST = AutopilotThrustInitializer(BACKWARDTHRUSTERS);

                initAutoThrustOnce = false;
            }

            Vector3 dir = new Vector3();
            if (FORWARDTHRUST.CurrentThrust > 0) { dir.Z = -1; } else if (BACKWARDTHRUST.CurrentThrust > 0) { dir.Z = 1; }

            if (UPTHRUST.CurrentThrust > 0) { dir.Y = 1; } else if (DOWNTHRUST.CurrentThrust > 0) { dir.Y = -1; }

            if (LEFTTHRUST.CurrentThrust > 0) { dir.X = -1; } else if (RIGHTTHRUST.CurrentThrust > 0) { dir.X = 1; }

            SetPow(dir);
        }

        void MagneticDrive() {
            Matrix mtrx;
            Vector3 dir;
            dir = CONTROLLER.MoveIndicator;
            CONTROLLER.Orientation.GetMatrix(out mtrx);
            dir = Vector3.Transform(dir, mtrx);
            if (!Vector3.IsZero(dir)) {
                //debugLog.Append("dir X: " + dir.X + "\ndir Y: " + dir.Y + "\ndir Z: " + dir.Z + "\n\n");
                dir /= dir.Length();
                if (!switchOnce) {
                    foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = false; }
                    switchOnce = true;
                }
            } else {
                if (switchOnce) {
                    foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                    switchOnce = false;
                }
            }
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity()) && !CONTROLLER.DampenersOverride && dir.LengthSquared() == 0) {
                SetPow(Vector3.Zero);
                return;
            }

            CONTROLLER.Orientation.GetMatrix(out mtrx);
            Vector3 vel = CONTROLLER.GetShipVelocities().LinearVelocity;
            vel = Vector3.Transform(vel, MatrixD.Transpose(CONTROLLER.WorldMatrix.GetOrientation()));
            vel = dir * maxSpeed - Vector3.Transform(vel, mtrx);
            if (Math.Abs(vel.X) < minSpeed) { vel.X = 0; }
            if (Math.Abs(vel.Y) < minSpeed) { vel.Y = 0; }
            if (Math.Abs(vel.Z) < minSpeed) { vel.Z = 0; }

            vel = KeepAltitude(vel, CONTROLLER);

            SetPow(vel);
        }

        void SetPow(Vector3 pow) {
            if (pow.X != 0) {
                if (pow.X > 0) {
                    foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
                } else {
                    foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = true; }
                }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
            }
            if (pow.Y != 0) {
                if (pow.Y > 0) {
                    foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
                } else {
                    foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = true; }
                }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
            }
            if (pow.Z != 0) {
                if (pow.Z > 0) {
                    foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
                } else {
                    foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = true; }
                }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
            }
        }

        void GyroStabilize(IMyShipController controller) {
            double speed = controller.GetShipSpeed();
            if (useGyrosToStabilize && speed > minSpeed) {
                if (Vector3D.IsZero(lastForwardVector) || Vector3D.IsZero(lastUpVector)) {
                    lastForwardVector = controller.WorldMatrix.Forward;
                    lastUpVector = controller.WorldMatrix.Up;
                } else {
                    double pitchAngle, yawAngle, rollAngle;
                    if (!useRoll) { lastUpVector = Vector3D.Zero; };
                    GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

                    double mouseYaw = controller.RotationIndicator.Y;
                    double mousePitch = controller.RotationIndicator.X;
                    double mouseRoll = controller.RollIndicator;

                    if (mouseYaw != 0) {
                        mouseYaw = mouseYaw < 0 ? MathHelper.Clamp(mouseYaw, -10, -2) : MathHelper.Clamp(mouseYaw, 2, 10);
                    }
                    if (mouseYaw == 0) {
                        mouseYaw = yawController.Control(yawAngle);
                    } else {
                        mouseYaw = yawController.Control(mouseYaw);
                    }

                    if (mousePitch != 0) {
                        mousePitch = mousePitch < 0 ? MathHelper.Clamp(mousePitch, -10, -2) : MathHelper.Clamp(mousePitch, 2, 10);
                    }
                    if (mousePitch == 0) {
                        mousePitch = pitchController.Control(pitchAngle);
                    } else {
                        mousePitch = pitchController.Control(mousePitch);
                    }

                    if (mouseRoll != 0) {
                        mouseRoll = mouseRoll < 0 ? MathHelper.Clamp(mouseRoll, -10, -2) : MathHelper.Clamp(mouseRoll, 2, 10);
                    }
                    if (mouseRoll == 0) {
                        mouseRoll = rollController.Control(rollAngle);
                    } else {
                        mouseRoll = rollController.Control(mouseRoll);
                    }

                    ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, controller.WorldMatrix);

                    lastForwardVector = controller.WorldMatrix.Forward;
                    lastUpVector = controller.WorldMatrix.Up;

                    unlockOnce = true;
                }
            } else {
                if (unlockOnce) {
                    UnlockGyros();
                    lastForwardVector = Vector3D.Zero;
                    lastUpVector = Vector3D.Zero;
                    unlockOnce = false;
                }
            }
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

            if (Vector3D.IsZero(axis)) {
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

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix) {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList) {
                if (thisGyro.Closed)
                    continue;
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        void UnlockGyros() {
            foreach (var gyro in GYROS) {
                gyro.Pitch = 0f;
                gyro.Yaw = 0f;
                gyro.Roll = 0f;
                gyro.GyroOverride = false;
            }
        }

        bool IsPiloted(bool autopiloted) {
            bool isPiloted = false;
            foreach (IMyShipController block in CONTROLLERS) {
                if (block.IsFunctional && block.IsUnderControl && block.CanControlShip && block.ControlThrusters) {
                    isPiloted = true;
                    break;
                }
                if (block is IMyRemoteControl && autopiloted) {
                    if ((block as IMyRemoteControl).IsAutoPilotEnabled) {
                        isPiloted = true;
                        break;
                    }
                }
            }
            return isPiloted;
        }

        public double GetMaxJumpDistance(IMyJumpDrive jumpDrive) {
            double maxDistance = (double)jumpDrive.MaxJumpDistanceMeters;
            return maxDistance;
        }

        IMyCameraBlock GetCameraWithMaxRange(List<IMyCameraBlock> cameraList) {
            double maxRange = 0d;
            IMyCameraBlock maxRangeCamera = cameraList[0];
            foreach (IMyCameraBlock thisCamera in cameraList) {
                if (thisCamera.AvailableScanRange > maxRange) {
                    maxRangeCamera = thisCamera;
                    maxRange = maxRangeCamera.AvailableScanRange;
                }
            }
            return maxRangeCamera;
        }

        bool IsValidTarget(ref MyDetectedEntityInfo entityInfo) {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid) {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) {
                    return true;
                }
            }
            return false;
        }

        bool IsNotFriendly(MyRelationsBetweenPlayerAndBlock relationship) {
            return (relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != MyRelationsBetweenPlayerAndBlock.Owner);
        }

        float Smallest(float rotorAngle, float b) {
            return Math.Abs(rotorAngle) > Math.Abs(b) ? b : rotorAngle;
        }

        void ReadLidarInfos() {
            lidarsLog.Clear();
            lidarsLog.Append("Max Range: ").Append(maxScanRange.ToString("0.0")).Append("\n");
            IMyCameraBlock cam = GetCameraWithMaxRange(LIDARS);
            lidarsLog.Append("Avail. Range: ").Append(cam.AvailableScanRange.ToString("0.0")).Append("\n");
        }

        void ReadJumpersInfos() {
            jumpersLog.Clear();
            double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
            jumpersLog.Append("Max Jump: ").Append(maxDistance.ToString("0.0")).Append("\n");

            double currentJumpPercent = (double)JUMPERS[0].GetValueFloat("JumpDistance");
            double currentJump = maxDistance / 100d * currentJumpPercent;
            jumpersLog.Append("Curr. Jump: ").Append(currentJump.ToString("0.0")).Append(" (").Append(currentJumpPercent.ToString("0.00")).Append("%)\n");

            double currentStoredPower = 0d;
            double maxStoredPower = 0d;
            StringBuilder timeRemainingBlldr = new StringBuilder();
            foreach (IMyJumpDrive block in JUMPERS) {
                MyJumpDriveStatus status = block.Status;

                if (status == MyJumpDriveStatus.Charging) {
                    string timeRemaining = block.DetailedInfo.ToString().Split('\n')[5];
                    timeRemainingBlldr.Append(status.ToString()).Append(": ").Append(timeRemaining).Append("s, ");
                } else {
                    timeRemainingBlldr.Append(status.ToString()).Append(", ");
                }

                currentStoredPower += block.CurrentStoredPower;
                maxStoredPower += block.MaxStoredPower;
            }
            jumpersLog.Append("Status: ").Append(timeRemainingBlldr.ToString());

            if (currentStoredPower > 0) {
                double totJumpPercent = currentStoredPower / maxStoredPower * 100;
                jumpersLog.Append("Stored Power: ").Append(totJumpPercent.ToString("0.00")).Append("%\n");
            } else {
                jumpersLog.Append("Stored Power: ").Append("0%\n");
            }
        }

        void WriteInfo() {
            foreach (IMyTextSurface surface in SURFACES) {
                StringBuilder text = new StringBuilder();
                text.Append(lidarsLog.ToString());
                text.Append(jumpersLog.ToString());
                text.Append("Selected Planet: " + selectedPlanet + "\n");
                text.Append(targetLog.ToString());
                surface.WriteText(text);
            }
        }

        void GetBlocks() {
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, block => block.CustomName.Contains(controllersName));
            REMOTES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(REMOTES, block => block.CustomName.Contains(remotesName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains(cockpitsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains(thrustersName));
            UPTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(upThrustersName)));
            DOWNTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(downThrustersName)));
            LEFTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(leftThrustersName)));
            RIGHTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(rightThrustersName)));
            FORWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(forwardThrustersName)));
            BACKWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(backwardThrustersName)));
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, block => block.CustomName.Contains(lidarsName));
            JUMPERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(JUMPERS, block => block.CustomName.Contains(jumpersName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(turretsName));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, block => block.CustomName.Contains(alarmsName));
            ROTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORS, block => block.CustomName.Contains(rotorsName));
            ROTORSINV.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORSINV, block => block.CustomName.Contains(rotorsInvName));
            MERGESPLUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSX, block => block.CustomName.Contains(plusXname));
            MERGESPLUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSY, block => block.CustomName.Contains(plusYname));
            MERGESPLUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSZ, block => block.CustomName.Contains(plusZname));
            MERGESMINUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSX, block => block.CustomName.Contains(minusXname));
            MERGESMINUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSY, block => block.CustomName.Contains(minusYname));
            MERGESMINUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSZ, block => block.CustomName.Contains(minusZname));
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsRangeFinderName));
            foreach (IMyTextPanel panel in panels) { SURFACES.Add(panel as IMyTextSurface); }
            LCDDEADMAN = GridTerminalSystem.GetBlockWithName(deadManPanelName) as IMyTextPanel;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName(idleThrusterPanelName) as IMyTextPanel;
            REMOTE = REMOTES[0];
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
            PAINTERPB = GridTerminalSystem.GetBlockWithName(painterName) as IMyProgrammableBlock;
            SHOOTERPB = GridTerminalSystem.GetBlockWithName(shooterName) as IMyProgrammableBlock;
        }

        void InitPIDControllers() {
            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
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
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun) {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Compute integral term
                if (!_integralDecay) {
                    _errorSum += error * _timeStep;

                    //Clamp integral term
                    if (_errorSum > _upperBound)
                        _errorSum = _upperBound;
                    else if (_errorSum < _lowerBound)
                        _errorSum = _lowerBound;
                } else {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
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

        public static class VectorMath {
            public static Vector3D SafeNormalize(Vector3D a) {
                if (Vector3D.IsZero(a))
                    return Vector3D.Zero;

                if (Vector3D.IsUnit(ref a))
                    return a;

                return Vector3D.Normalize(a);
            }

            public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1) {//reflect a over b
                Vector3D project_a = Projection(a, b);
                Vector3D reject_a = a - project_a;
                return project_a - reject_a * rejectionFactor;
            }

            public static Vector3D Rejection(Vector3D a, Vector3D b) {//reject a on b
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a - a.Dot(b) / b.LengthSquared() * b;
            }

            public static Vector3D Projection(Vector3D a, Vector3D b) {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a.Dot(b) / b.LengthSquared() * b;
            }

            public static double ScalarProjection(Vector3D a, Vector3D b) {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;

                if (Vector3D.IsUnit(ref b))
                    return a.Dot(b);

                return a.Dot(b) / b.Length();
            }

            public static double AngleBetween(Vector3D a, Vector3D b) {//returns radians
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }

            public static double CosBetween(Vector3D a, Vector3D b) {//returns radians
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
            }
        }

    }
}
