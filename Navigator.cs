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
using System.Collections.Immutable;

namespace IngameScript {
    partial class Program : MyGridProgram {
        //TODO add generate orbital gps function
        //generate orbital gps above target 
        //when autopilot is on the ship is vulnerable to attacks
        //land function
        //do randomdrive for thrusters
        //NAVIGATOR

        readonly string controllersName = "[CRX] Controller";
        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string lidarsBackName = "[CRX] Camera Back";
        readonly string lidarsUpName = "[CRX] Camera Up";
        readonly string lidarsDownName = "[CRX] Camera Down";
        readonly string lidarsLeftName = "[CRX] Camera Left";
        readonly string lidarsRightName = "[CRX] Camera Right";
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
        readonly string sensorsName = "[CRX] Sensor";
        readonly string solarsName = "[CRX] Solar";
        readonly string upName = "UP";
        readonly string downName = "DOWN";
        readonly string leftName = "LEFT";
        readonly string rightName = "RIGHT";
        readonly string forwardName = "FORWARD";
        readonly string backwardName = "BACKWARD";
        readonly string deadManPanelName = "[CRX] LCD DeadMan Toggle";
        readonly string idleThrusterPanelName = "[CRX] LCD IdleThrusters Toggle";
        readonly string lcdsRangeFinderName = "[CRX] LCD RangeFinder";
        readonly string sunChaserPanelName = "[CRX] LCD SunChaser Toggle";
        readonly string debugPanelName = "[CRX] Debug";
        readonly string painterName = "[CRX] PB Painter";

        readonly string navigatorTag = "[NAVIGATOR]";
        readonly string managerTag = "[MANAGER]";
        readonly string sectionTag = "RangeFinderSettings";
        readonly string cockpitRangeFinderKey = "cockpitRangeFinderSurface";

        const string argRangeFinder = "RangeFinder";
        const string argAimTarget = "AimTarget";
        const string argDeadMan = "DeadMan";
        const string argMagneticDrive = "ToggleMagneticDrive";
        const string argIdleThrusters = "ToggleIdleThrusters";
        const string argChangePlanet = "ChangePlanet";
        const string argSetPlanet = "SetPlanet";

        const string argSunChaserToggle = "SunChaserToggle";
        const string argSunchaseOn = "SunchaseOn";
        const string argSunchaseOff = "SunchaseOff";
        const string argUnlockFromTarget = "Clear";
        const string argLockTarget = "Lock";
        const string argGyroStabilizeOff = "StabilizeOff";
        const string argGyroStabilizeOn = "StabilizeOn";

        readonly bool keepAltitude = true;
        readonly bool useRoll = false;
        readonly bool stopOnPlayerDetection = false;
        readonly double aimP = 1d;
        readonly double aimI = 0d;
        readonly double aimD = 1d;
        readonly double integralWindupLimit = 0d;
        readonly double gunsMaxRange = 2000d;
        readonly double gunsCloseRange = 800d;
        readonly double gunsMidRange = 1400d;
        readonly double escapeDistance = 250d;
        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;
        readonly double stopDistance = 50d;
        readonly float rocketSpeed = 200f;
        readonly float autocannonGatlingSpeed = 400f;
        readonly float assaultArtillerySpeed = 500f;
        readonly float railgunSpeed = 2000f;
        readonly float smallRailgunSpeed = 1000f;
        readonly float maxSpeed = 105f;
        readonly float minSpeed = 2f;
        readonly float deadManMinSpeed = 0.1f;
        readonly float targetVel = 29 * rpsOverRpm;
        readonly float syncSpeed = 1 * rpsOverRpm;
        readonly int tickDelay = 50;
        readonly int escapeDelay = 10;
        readonly int randomDelay = 10;
        readonly int sensorsDelay = 5;
        readonly int collisionCheckDelay = 10;
        readonly int keepAltitudeDelay = 50;
        int impactDetectionDelay = 5;

        bool magneticDrive = true;
        bool controlDampeners = true;
        bool idleThrusters = false;
        bool aimTarget = false;
        bool useGyrosToStabilize = true;
        bool targFound = false;
        bool assaultCanShoot = true;
        bool artilleryCanShoot = true;
        bool railgunsCanShoot = true;
        bool smallRailgunsCanShoot = true;
        bool sunChasing = false;
        bool unlockGyrosOnce = true;
        bool deadManOnce = false;
        bool toggleThrustersOnce = false;
        bool returnOnce = true;
        bool lockTargetOnce = true;
        bool isPilotedOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool initRandomMagneticDriveOnce = true;
        bool sunChaseOnce = true;
        bool unlockSunChaseOnce = true;
        bool keepAltitudeOnce = true;
        string selectedPlanet = "";
        double maxScanRange = 0d;
        double altitudeToKeep = 0d;
        double movePitch = .01;
        double moveYaw = .01;
        float prevSunPower = 0f;
        int cockpitRangeFinderSurface = 4;
        int planetSelector = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;
        int impactDetectionCount = 5;
        int escapeCount = 10;
        int tickCount = 0;
        int randomCount = 50;
        int sensorsCount = 10;
        int collisionCheckCount = 0;
        int keepAltitudeCount = 0;

        const float globalTimestep = 10.0f / 60.0f;
        const float rpsOverRpm = (float)(Math.PI / 30);
        const float circle = (float)(2 * Math.PI);
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 0.1d;//degrees
        const double evasionAngleTolerance = 2d;//degrees

        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyCameraBlock> LIDARSBACK = new List<IMyCameraBlock>();
        public List<IMyCameraBlock> LIDARSUP = new List<IMyCameraBlock>();
        public List<IMyCameraBlock> LIDARSDOWN = new List<IMyCameraBlock>();
        public List<IMyCameraBlock> LIDARSLEFT = new List<IMyCameraBlock>();
        public List<IMyCameraBlock> LIDARSRIGHT = new List<IMyCameraBlock>();
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
        public List<IMySensorBlock> SENSORS = new List<IMySensorBlock>();
        public List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();

        IMyShipController CONTROLLER = null;
        IMyRemoteControl REMOTE;
        IMyThrust UPTHRUST;
        IMyThrust DOWNTHRUST;
        IMyThrust LEFTTHRUST;
        IMyThrust RIGHTTHRUST;
        IMyThrust FORWARDTHRUST;
        IMyThrust BACKWARDTHRUST;
        IMyProgrammableBlock PAINTERPB;
        IMyTextPanel LCDDEADMAN;
        IMyTextPanel LCDIDLETHRUSTERS;
        IMyTextPanel LCDSUNCHASER;
        IMySensorBlock UPSENSOR;
        IMySensorBlock DOWNSENSOR;
        IMySensorBlock LEFTSENSOR;
        IMySensorBlock RIGHTSENSOR;
        IMySensorBlock FORWARDSENSOR;
        IMySensorBlock BACKWARDSENSOR;
        IMySolarPanel SOLAR;

        IMyBroadcastListener BROADCASTLISTENER;
        MyDetectedEntityInfo targetInfo;
        MatrixD targOrientation = new MatrixD();
        Vector3D targetPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D escapePosition = Vector3D.Zero;
        Vector3D hoverPosition = Vector3D.Zero;
        Vector3D lastForwardVector = Vector3D.Zero;
        Vector3D lastUpVector = Vector3D.Zero;
        Vector3D targPos = Vector3D.Zero;
        Vector3D targVelVec = Vector3D.Zero;
        Vector3 randomDir = new Vector3();
        Vector3D lastVelocity = Vector3D.Zero;
        Vector3D maxAccel;
        Vector3D minAccel;

        public StringBuilder jumpersLog = new StringBuilder("");
        public StringBuilder lidarsLog = new StringBuilder("");
        public StringBuilder targetLog = new StringBuilder("");

        PID yawController;
        PID pitchController;
        PID rollController;

        readonly Random random = new Random();
        readonly MyIni myIni = new MyIni();

        readonly Dictionary<String, MyTuple<Vector3D, double, double>> planetsList = new Dictionary<String, MyTuple<Vector3D, double, double>>() {//string PlanetName, Vector3D PlanetPosition, double PlanetRadius, double AtmosphereRadius
            { "Earth",  MyTuple.Create(new Vector3D(0d,          0d,          0d),         61250d,     41843.4d) },
            { "Moon",   MyTuple.Create(new Vector3D(16384d,      136384d,     -113616d),   9500d,      2814.416d) },
            { "Triton", MyTuple.Create(new Vector3D(-284463.5,   -2434463.5,  365536.5),   40127.5d,   33735.39d) },
            { "Mars",   MyTuple.Create(new Vector3D(1031072d,    131072d,     1631072d),   61500d,     40053.3d) },
            { "Europa", MyTuple.Create(new Vector3D(916384d,     16384d,      1616384d),   9600d,      12673.088d) },
            { "Alien",  MyTuple.Create(new Vector3D(131072d,     131072d,     5731072d),   60000d,     44506.7d) },
            { "Titan",  MyTuple.Create(new Vector3D(36384d,      226384d,     5796384d),   9500d,      2814.416d) },
            { "Pertam", MyTuple.Create(new Vector3D(-3967231.5,  -32231.5,    -767231.5),  30000d,     18500d) }
        };

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(navigatorTag);
            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }
            if (LIDARS.Count != 0) { maxScanRange = LIDARS[0].RaycastDistanceLimit; }
            selectedPlanet = planetsList.ElementAt(0).Key;
            InitPIDControllers();
            LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0);
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                }

                GetBroadcastMessages();

                bool isControlled = GetController();
                SendBroadcastControllerMessage(isControlled);
                bool isAutoPiloted = IsAutoPiloted();
                bool isUnderControl = IsPiloted(false);//TODO

                IMyShipController controller = CONTROLLER ?? REMOTE;
                Vector3D gravity = controller.GetNaturalGravity();

                if (!isUnderControl) {
                    TurretsDetection();
                    Vector3D trgP, trgV;
                    ManageTarget(out trgP, out trgV);
                    CheckTarget(controller, REMOTE, trgP, trgV);
                }

                ManageWaypoints(REMOTE, isUnderControl);

                GyroStabilize(controller, targFound, aimTarget, isAutoPiloted, useRoll, gravity);

                ManageMagneticDrive(controller, isControlled, isUnderControl, isAutoPiloted, targFound, idleThrusters, keepAltitude, gravity);

                if (aimTarget) {
                    AimAtTarget(controller, targetPosition);
                }

                SunChase(isControlled, gravity, targFound);

                ReadLidarInfos();
                ReadJumpersInfos();

                WriteInfo();

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                Setup();
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case argRangeFinder: RangeFinder(REMOTE); break;
                case argChangePlanet:
                    planetSelector++;
                    if (planetSelector >= planetsList.Count()) {
                        planetSelector = 0;
                    }
                    selectedPlanet = planetsList.ElementAt(planetSelector).Key;
                    break;
                case argAimTarget: if (!Vector3D.IsZero(targetPosition)) { aimTarget = true; }; break;
                case argIdleThrusters:
                    idleThrusters = !idleThrusters;
                    if (idleThrusters) {
                        foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = false; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = true; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0);
                    }
                    break;
                case argGyroStabilizeOn:
                    useGyrosToStabilize = true;
                    break;
                case argGyroStabilizeOff:
                    useGyrosToStabilize = false;
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
                        targetPosition = safeJumpPosition;
                        if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                        targetLog.Clear();
                        string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                        targetLog.Append(safeJumpGps).Append("\n");
                        targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                        targetLog.Append("Radius: ").Append(planet.Item2.ToString("0.0")).Append(", ");
                        targetLog.Append("Diameter: ").Append((planet.Item2 * 2d).ToString("0.0")).Append("\n");
                        targetLog.Append("Atmo. Height: ").Append(planet.Item3.ToString("0.0")).Append("\n");
                    }
                    break;
                case argSunChaserToggle:
                    sunChasing = !sunChasing;
                    break;
                case argSunchaseOff:
                    sunChasing = false;
                    break;
                case argSunchaseOn:
                    sunChasing = true;
                    break;
            }
        }

        bool GetBroadcastMessages() {
            bool received = false;
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    var igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<bool, Vector3D, Vector3D, MatrixD>) {
                        var data = (MyTuple<bool, Vector3D, Vector3D, MatrixD>)igcMessage.Data;
                        targFound = data.Item1;
                        targPos = data.Item2;
                        targVelVec = data.Item3;
                        targOrientation = data.Item4;
                        received = true;
                    }
                    if (igcMessage.Data is MyTuple<int, bool, bool, bool, bool>) {
                        var data = (MyTuple<int, bool, bool, bool, bool>)igcMessage.Data;
                        //weaponType = data.Item1;
                        assaultCanShoot = data.Item2;
                        artilleryCanShoot = data.Item3;
                        railgunsCanShoot = data.Item4;
                        smallRailgunsCanShoot = data.Item5;
                    }
                }
            }
            return received;
        }

        void SendBroadcastControllerMessage(bool isControlled) {
            var tuple = MyTuple.Create("isControlled", isControlled);
            IGC.SendBroadcastMessage(managerTag, tuple, TransmissionDistance.ConnectedConstructs);
        }

        bool GetController() {
            if (CONTROLLER != null && (!CONTROLLER.IsUnderControl || !CONTROLLER.CanControlShip)) {
                CONTROLLER = null;
            }
            if (CONTROLLER == null) {
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.IsUnderControl && block.IsFunctional && block.CanControlShip && !(block is IMyRemoteControl)) {
                        CONTROLLER = block;
                        break;
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
                        if (contr.IsFunctional && contr.CanControlShip && !(contr is IMyRemoteControl)) {
                            controller = contr;
                            break;
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
                    if (targFound) {
                        CONTROLLER = controller;
                        controlled = true;
                    }
                }
            } else {
                controlled = true;
            }
            return controlled;
        }

        void GyroStabilize(IMyShipController controller, bool targetFound, bool aimingTarget, bool isAutoPiloted, bool useRoll, Vector3D gravity) {
            if (useGyrosToStabilize && !targetFound && !aimingTarget && !isAutoPiloted) {
                if (!Vector3D.IsZero(gravity)) {
                    MatrixD matrix = controller.WorldMatrix;
                    Vector3D leftVec = Vector3D.Cross(matrix.Forward, gravity);
                    Vector3D horizonVec = Vector3D.Cross(gravity, leftVec);
                    double pitchAngle, rollAngle, yawAngle;
                    GetRotationAnglesSimultaneous(horizonVec, -gravity, matrix, out pitchAngle, out yawAngle, out rollAngle);
                    double mouseYaw = controller.RotationIndicator.Y;
                    double mousePitch = controller.RotationIndicator.X;
                    double mouseRoll = controller.RollIndicator;
                    if (mousePitch != 0d) {
                        mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                    }
                    if (mousePitch == 0d) {
                        mousePitch = pitchController.Control(pitchAngle);
                    } else {
                        mousePitch = pitchController.Control(mousePitch);
                    }
                    if (mouseRoll != 0d) {
                        mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                    }
                    if (mouseRoll == 0d) {
                        mouseRoll = rollController.Control(rollAngle);
                    } else {
                        mouseRoll = rollController.Control(mouseRoll);
                    }
                    double speed = controller.GetShipSpeed();
                    yawAngle = 0d;
                    if (speed > minSpeed) {
                        if (Vector3D.IsZero(lastForwardVector)) {
                            lastForwardVector = controller.WorldMatrix.Forward;
                            lastUpVector = controller.WorldMatrix.Up;
                        }
                        if (!useRoll) { lastUpVector = Vector3D.Zero; };
                        GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        lastForwardVector = controller.WorldMatrix.Forward;
                        lastUpVector = controller.WorldMatrix.Up;
                    }
                    if (mouseYaw != 0d) {
                        mouseYaw = mouseYaw < 0d ? MathHelper.Clamp(mouseYaw, -10d, -2d) : MathHelper.Clamp(mouseYaw, 2d, 10d);
                    }
                    if (mouseYaw == 0d) {
                        mouseYaw = yawController.Control(yawAngle);
                    } else {
                        mouseYaw = yawController.Control(mouseYaw);
                    }
                    if (mousePitch == 0 && mouseYaw == 0 && mouseRoll == 0) {
                        if (unlockGyrosOnce) {
                            UnlockGyros();
                            lastForwardVector = Vector3D.Zero;
                            lastUpVector = Vector3D.Zero;
                            unlockGyrosOnce = false;
                        }
                    } else {
                        ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, matrix);
                        unlockGyrosOnce = true;
                    }
                } else {
                    double speed = controller.GetShipSpeed();
                    if (speed > minSpeed) {
                        double pitchAngle, yawAngle, rollAngle;
                        if (Vector3D.IsZero(lastForwardVector)) {
                            lastForwardVector = controller.WorldMatrix.Forward;
                            lastUpVector = controller.WorldMatrix.Up;
                        }
                        if (!useRoll) { lastUpVector = Vector3D.Zero; };
                        GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        double mouseYaw = controller.RotationIndicator.Y;
                        double mousePitch = controller.RotationIndicator.X;
                        double mouseRoll = controller.RollIndicator;
                        if (mouseYaw != 0d) {
                            mouseYaw = mouseYaw < 0d ? MathHelper.Clamp(mouseYaw, -10d, -2d) : MathHelper.Clamp(mouseYaw, 2d, 10d);
                        }
                        if (mouseYaw == 0d) {
                            mouseYaw = yawController.Control(yawAngle);
                        } else {
                            mouseYaw = yawController.Control(mouseYaw);
                        }
                        if (mousePitch != 0d) {
                            mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                        }
                        if (mousePitch == 0d) {
                            mousePitch = pitchController.Control(pitchAngle);
                        } else {
                            mousePitch = pitchController.Control(mousePitch);
                        }
                        if (mouseRoll != 0d) {
                            mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                        }
                        if (mouseRoll == 0d) {
                            mouseRoll = rollController.Control(rollAngle);
                        } else {
                            mouseRoll = rollController.Control(mouseRoll);
                        }
                        if (mousePitch == 0d && mouseYaw == 0d && mouseRoll == 0d) {
                            if (unlockGyrosOnce) {
                                UnlockGyros();
                                lastForwardVector = Vector3D.Zero;
                                lastUpVector = Vector3D.Zero;
                                unlockGyrosOnce = false;
                            }
                        } else {
                            ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, controller.WorldMatrix);
                            unlockGyrosOnce = true;
                        }
                        lastForwardVector = controller.WorldMatrix.Forward;
                        lastUpVector = controller.WorldMatrix.Up;
                    }
                    if (unlockGyrosOnce) {
                        UnlockGyros();
                        lastForwardVector = Vector3D.Zero;
                        lastUpVector = Vector3D.Zero;
                        unlockGyrosOnce = false;
                    }
                }
            } else {
                if (unlockGyrosOnce) {
                    UnlockGyros();
                    lastForwardVector = Vector3D.Zero;
                    lastUpVector = Vector3D.Zero;
                    unlockGyrosOnce = false;
                }
            }
        }

        void ManageMagneticDrive(IMyShipController controller, bool isControlled, bool isUnderControl, bool isAutoPiloted, bool targetFound, bool idleThrusters, bool keepAltitude, Vector3D gravity) {
            if (magneticDrive && isControlled) {
                if (initMagneticDriveOnce) {
                    foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = true; }
                    sunChasing = false;
                    initMagneticDriveOnce = false;
                }
                SyncRotors();
                if (isAutoPiloted) {
                    Vector3 dir = AutoMagneticDrive();
                    SetPower(dir);
                } else {
                    if (!initAutoMagneticDriveOnce) {
                        foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                        initAutoMagneticDriveOnce = true;
                    }
                    if (!isUnderControl && targetFound) {
                        if (initRandomMagneticDriveOnce) {
                            SetSensorsExtend();
                            controller.DampenersOverride = false;
                            initRandomMagneticDriveOnce = false;
                        }
                        Vector3 dir = RandomMagneticDrive(targPos, controller);
                        SetPower(dir);
                    } else {
                        if (!initRandomMagneticDriveOnce) {
                            randomDir = Vector3.Zero;
                            foreach (IMySensorBlock sensor in SENSORS) {
                                sensor.BackExtend = 0.1f;
                                sensor.BottomExtend = 0.1f;
                                sensor.FrontExtend = 0.1f;
                                sensor.LeftExtend = 0.1f;
                                sensor.RightExtend = 0.1f;
                                sensor.TopExtend = 0.1f;
                            }
                            controller.DampenersOverride = true;
                            initRandomMagneticDriveOnce = true;
                        }
                        Vector3 dir = MagneticDrive(controller, gravity);

                        dir = KeepAltitude(isUnderControl, dir, controller, idleThrusters, keepAltitude, gravity);

                        SetPower(dir);
                    }
                }
            } else {//TODO randomDrive with thrusters
                if (!initMagneticDriveOnce) {
                    IdleMagneticDrive(idleThrusters);
                    initMagneticDriveOnce = true;
                }
                if (tickCount == tickDelay) {
                    if (controlDampeners) {
                        DeadMan(IsPiloted(true));
                        LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                    } else { LCDDEADMAN.BackgroundColor = new Color(0, 0, 0); }
                    if (idleThrusters) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255); } else { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }
                    tickCount = 0;
                }
                tickCount++;
            }
        }

        void IdleMagneticDrive(bool idleThrusters) {
            SetPower(Vector3D.Zero);
            foreach (IMyMotorStator block in ROTORS) {
                block.TargetVelocityRPM = 0f;
            }
            foreach (IMyMotorStator block in ROTORSINV) {
                block.TargetVelocityRPM = 0f;
            }
            if (idleThrusters) {
                foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = false; }
            }
        }

        IMyThrust InitAutopilotMagneticDrive(List<IMyThrust> thrusters) {
            IMyThrust thruster = null;
            int i = 0;
            foreach (IMyThrust thrust in thrusters) {
                if (i == 0) {
                    thruster = thrust;
                    thrust.Enabled = true;
                } else { thrust.Enabled = false; }
                i++;
            }
            return thruster;
        }

        Vector3 AutoMagneticDrive() {
            if (initAutoMagneticDriveOnce) {
                UPTHRUST = InitAutopilotMagneticDrive(UPTHRUSTERS);
                DOWNTHRUST = InitAutopilotMagneticDrive(DOWNTHRUSTERS);
                LEFTTHRUST = InitAutopilotMagneticDrive(LEFTTHRUSTERS);
                RIGHTTHRUST = InitAutopilotMagneticDrive(RIGHTTHRUSTERS);
                FORWARDTHRUST = InitAutopilotMagneticDrive(FORWARDTHRUSTERS);
                BACKWARDTHRUST = InitAutopilotMagneticDrive(BACKWARDTHRUSTERS);
                initAutoMagneticDriveOnce = false;
            }
            Vector3 dir = new Vector3();
            if (FORWARDTHRUST.CurrentThrust > 0f) { dir.Z = -1f; } else if (BACKWARDTHRUST.CurrentThrust > 0f) { dir.Z = 1f; }
            if (UPTHRUST.CurrentThrust > 0f) { dir.Y = 1f; } else if (DOWNTHRUST.CurrentThrust > 0f) { dir.Y = -1f; }
            if (LEFTTHRUST.CurrentThrust > 0f) { dir.X = -1f; } else if (RIGHTTHRUST.CurrentThrust > 0f) { dir.X = 1f; }
            return dir;
        }

        Vector3 RandomMagneticDrive(Vector3D targPos, IMyShipController controller) {
            bool detectedOwner;
            if (!stopOnPlayerDetection) {
                detectedOwner = false;
            } else {
                detectedOwner = false;
                if (sensorsCount >= sensorsDelay) {
                    List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();
                    foreach (IMySensorBlock sensor in SENSORS) {
                        entities.Clear();
                        sensor.DetectedEntities(entities);
                        if (entities.Count > 0) {
                            foreach (var entity in entities) {
                                if (entity.Relationship == MyRelationsBetweenPlayerAndBlock.Owner) {
                                    detectedOwner = true;
                                    break;
                                }
                            }
                            randomCount = randomDelay;
                            break;
                        }
                    }
                    sensorsCount = 0;
                }
                sensorsCount++;
            }
            if (!detectedOwner) {
                if (randomCount >= randomDelay) {
                    randomDir = new Vector3();
                    float randomFloat;
                    List<MyDetectedEntityInfo> entitiesA = new List<MyDetectedEntityInfo>();
                    List<MyDetectedEntityInfo> entitiesB = new List<MyDetectedEntityInfo>();
                    LEFTSENSOR.DetectedEntities(entitiesA);
                    RIGHTSENSOR.DetectedEntities(entitiesB);
                    bool goUp = false;
                    foreach (var entity in entitiesA) {
                        if (entity.Type == MyDetectedEntityType.SmallGrid && (entity.Relationship == MyRelationsBetweenPlayerAndBlock.Friends || entity.Relationship == MyRelationsBetweenPlayerAndBlock.Owner)) {
                            goUp = true;
                            break;
                        }
                    }
                    foreach (var entity in entitiesB) {
                        if (entity.Type == MyDetectedEntityType.SmallGrid && (entity.Relationship == MyRelationsBetweenPlayerAndBlock.Friends || entity.Relationship == MyRelationsBetweenPlayerAndBlock.Owner)) {
                            goUp = true;
                            break;
                        }
                    }
                    if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                        randomDir.X = 0f;
                    } else if (entitiesA.Count > 0) {
                        randomDir.X = 1f;
                    } else if (entitiesB.Count > 0) {
                        randomDir.X = -1f;
                    } else {
                        randomFloat = (float)random.Next(-1, 1);
                        randomDir.X = randomFloat;
                    }
                    entitiesA.Clear();
                    entitiesB.Clear();
                    UPSENSOR.DetectedEntities(entitiesA);
                    DOWNSENSOR.DetectedEntities(entitiesB);
                    if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                        randomDir.Y = 0f;
                    } else if (entitiesA.Count > 0) {
                        randomDir.Y = -1f;
                    } else if (entitiesB.Count > 0) {
                        randomDir.Y = 1f;
                    } else {
                        if (goUp) {
                            randomDir.Y = 1f;
                        } else {
                            randomFloat = (float)random.Next(-1, 1);
                            randomDir.Y = randomFloat;
                        }
                    }
                    double minDistance = gunsMaxRange;
                    if (!railgunsCanShoot && !artilleryCanShoot) {
                        minDistance = gunsMidRange;
                        if (!assaultCanShoot && !smallRailgunsCanShoot) {
                            minDistance = gunsCloseRange;
                        }
                    }
                    entitiesA.Clear();
                    entitiesB.Clear();
                    FORWARDSENSOR.DetectedEntities(entitiesA);
                    BACKWARDSENSOR.DetectedEntities(entitiesB);
                    if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                        randomDir.Z = 0f;
                    } else if (entitiesA.Count > 0) {
                        randomDir.Z = 1f;
                    } else if (entitiesB.Count > 0) {
                        randomDir.Z = -1f;
                    } else {
                        double distance = Vector3D.Distance(controller.GetPosition(), targPos);
                        if (distance > minDistance) {
                            randomDir.Z = -1f;
                        } else if (distance <= minDistance) {
                            randomDir.Z = 1f;
                        } else if (distance < 800d) {
                            randomDir.Z = 1f;
                        } else {
                            randomFloat = (float)random.Next(-1, 1);
                            randomDir.Z = randomFloat;
                        }
                    }
                    randomCount = 0;
                }
                randomCount++;
                UpdateAcceleration(controller, Runtime.TimeSinceLastRun.TotalSeconds);
                if (collisionCheckCount >= collisionCheckDelay) {
                    double stopDistance = CalculateStopDistance(controller);
                    RaycastStopPosition(controller, stopDistance, randomDir);
                    collisionCheckCount = 0;
                }
                collisionCheckCount++;

                randomDir = EvadeEnemy(randomDir, controller, targOrientation, targVelVec, targPos, controller.CubeGrid.WorldVolume.Center, controller.GetShipVelocities().LinearVelocity, controller.GetNaturalGravity());

                return randomDir;
            } else {
                randomCount = 0;
                randomDir = Vector3.Zero;
                return randomDir;
            }
        }

        Vector3 MagneticDrive(IMyShipController controller, Vector3D gravity) {
            Matrix mtrx;
            Vector3 dir;
            dir = controller.MoveIndicator;
            controller.Orientation.GetMatrix(out mtrx);
            dir = Vector3.Transform(dir, mtrx);
            if (!Vector3.IsZero(dir)) {
                //debugLog.Append("dir X: " + dir.X + "\ndir Y: " + dir.Y + "\ndir Z: " + dir.Z + "\n\n");
                dir /= dir.Length();
                if (!toggleThrustersOnce) {
                    foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = false; }
                    toggleThrustersOnce = true;
                }
            } else {
                if (toggleThrustersOnce) {
                    foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = true; }
                    toggleThrustersOnce = false;
                }
            }
            if (Vector3D.IsZero(gravity) && !controller.DampenersOverride && dir.LengthSquared() == 0f) {
                return Vector3.Zero;
            }
            controller.Orientation.GetMatrix(out mtrx);
            Vector3 vel = controller.GetShipVelocities().LinearVelocity;
            vel = Vector3.Transform(vel, MatrixD.Transpose(controller.WorldMatrix.GetOrientation()));
            vel = dir * maxSpeed - Vector3.Transform(vel, mtrx);
            if (Math.Abs(vel.X) < minSpeed) { vel.X = 0f; }
            if (Math.Abs(vel.Y) < minSpeed) { vel.Y = 0f; }
            if (Math.Abs(vel.Z) < minSpeed) { vel.Z = 0f; }
            return vel;
        }

        void SetPower(Vector3 pow) {
            if (pow.X != 0f) {
                if (pow.X > 0f) {
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
            if (pow.Y != 0f) {
                if (pow.Y > 0f) {
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
            if (pow.Z != 0f) {
                if (pow.Z > 0f) {
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

        void SyncRotors() {
            float angle = 0f;
            foreach (IMyMotorStator rotor in ROTORS) { angle += rotor.Angle; }
            foreach (IMyMotorStator rotor in ROTORSINV) { angle += circle - rotor.Angle; }
            angle /= ROTORS.Count() + ROTORSINV.Count();
            float angleInv = circle - angle;
            foreach (IMyMotorStator rotor in ROTORS) {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angle, Smallest(rotorAngle - angle + circle, rotorAngle - angle - circle));
                rotor.TargetVelocityRad = asyncAngle > 0f ? targetVel - syncSpeed : targetVel + syncSpeed;
            }
            foreach (IMyMotorStator rotor in ROTORSINV) {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angleInv, Smallest(rotorAngle - angleInv + circle, rotorAngle - angleInv - circle));
                rotor.TargetVelocityRad = asyncAngle > 0f ? -targetVel - syncSpeed : -targetVel + syncSpeed;
            }
        }

        Vector3 EvadeEnemy(Vector3 dir, IMyShipController controller, MatrixD targOrientation, Vector3D targVel, Vector3D targPos, Vector3D myPosition, Vector3D myVelocity, Vector3D gravity) {
            Base6Directions.Direction enemyForward = targOrientation.GetClosestDirection(myPosition);
            Vector3D enemyForwardVec = targOrientation.GetDirectionVector(enemyForward);
            targOrientation = MatrixD.CreateFromDir(enemyForwardVec);//targOrientation.SetDirectionVector(Base6Directions.Direction.Forward, enemyForwardVec);
            double distance = Vector3D.Distance(myPosition, targPos);
            List<Vector3D> enemyAims = new List<Vector3D>();
            if (distance <= gunsCloseRange) {
                Vector3D enemyAim = ComputeEnemyLeading(targPos, targVel, rocketSpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, rocketSpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
                enemyAim = ComputeEnemyLeading(targPos, targVel, autocannonGatlingSpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, autocannonGatlingSpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
            } else if (distance <= gunsMidRange) {
                Vector3D enemyAim = ComputeEnemyLeading(targPos, targVel, assaultArtillerySpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, assaultArtillerySpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
                enemyAim = ComputeEnemyLeading(targPos, targVel, smallRailgunSpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, smallRailgunSpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
            } else if (distance <= gunsMaxRange) {
                Vector3D enemyAim = ComputeEnemyLeading(targPos, targVel, railgunSpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, railgunSpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
                enemyAim = ComputeEnemyLeading(targPos, targVel, assaultArtillerySpeed, targOrientation, myPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, assaultArtillerySpeed, enemyAim, gravity); }
                enemyAims.Add(enemyAim);
            } else {
                return dir;
            }
            foreach (Vector3D aim in enemyAims) {
                double angle = VectorMath.AngleBetween(targOrientation.Forward, aim);
                if (angle * rad2deg <= evasionAngleTolerance) {
                    Vector3D evadeDirection = Vector3D.Cross(aim, controller.WorldMatrix.Down);
                    dir = Vector3D.Normalize(evadeDirection);
                }
            }
            return dir;
        }

        Vector3D ComputeEnemyLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, MatrixD targMatrix, Vector3D myPosition, Vector3D myVelocity) {
            Vector3D aimDirection = GetEnemyAim(targetPosition, targetVelocity, myPosition, myVelocity, projectileSpeed);
            aimDirection -= targMatrix.Translation;
            return aimDirection;
        }

        Vector3D GetEnemyAim(Vector3D targPosition, Vector3D targVelocity, Vector3D myPosition, Vector3D myVelocity, float projectileSpeed) {
            float shootDelay = 0f;
            Vector3D toMe = myPosition - targPosition;
            Vector3D diffVelocity = myVelocity - targVelocity;
            float a = (float)diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2 * Vector3.Dot(diffVelocity, toMe);
            float c = (float)toMe.LengthSquared();
            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);
            float t1 = p - q;
            float t2 = p + q;
            float t;
            if (t1 > t2 && t2 > 0f) { t = t2; } else { t = t1; }
            t += shootDelay;
            Vector3D predictedPosition = targPosition + diffVelocity * t;
            return predictedPosition;
        }

        Vector3D BulletDrop(double distanceFromTarget, double projectileMaxSpeed, Vector3D desiredDirection, Vector3D gravity) {
            double timeToTarget = distanceFromTarget / projectileMaxSpeed;
            desiredDirection -= 0.5 * gravity * timeToTarget * timeToTarget;
            return desiredDirection;
        }

        void UpdateAcceleration(IMyShipController controller, double timeStep) {
            Vector3D currentVelocity = controller.GetShipVelocities().LinearVelocity;
            Vector3D acceleration = (currentVelocity - lastVelocity) / timeStep;
            lastVelocity = currentVelocity;
            MatrixD worldMatrix = controller.WorldMatrix;
            var localAcceleration = Vector3D.TransformNormal(acceleration, MatrixD.Transpose(worldMatrix));
            for (int i = 0; i < 3; ++i) {// Now we store off the components if they are larger (in magnitude) than what we have stored
                double component = localAcceleration.GetDim(i);
                if (component >= 0d) {
                    if (component > maxAccel.GetDim(i)) {// Bigger than what we have stored
                        maxAccel.SetDim(i, component);
                    }
                } else {// if negative
                    component = Math.Abs(component);
                    if (component > minAccel.GetDim(i)) {// Bigger (in magnitude) than what we have stored
                        minAccel.SetDim(i, component);
                    }
                }
            }
        }

        double CalculateStopDistance(IMyShipController controller) {
            Vector3D currentVelocity = controller.GetShipVelocities().LinearVelocity;
            MatrixD worldMatrix = controller.WorldMatrix;
            var localVelocity = Vector3D.TransformNormal(currentVelocity, MatrixD.Transpose(worldMatrix));
            Vector3D stopDistanceLocal = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {// Now we break the current velocity apart component by component
                double velocityComponent = localVelocity.GetDim(i);
                double stopDistComponent;
                if (velocityComponent >= 0d) {// We do MIN accel when the velocity is positive because we want the acceleration in the OPPOSITE direction
                    stopDistComponent = (velocityComponent * velocityComponent) / (2d * minAccel.GetDim(i));
                } else {
                    stopDistComponent = (velocityComponent * velocityComponent) / (2d * maxAccel.GetDim(i));
                }
                stopDistanceLocal.SetDim(i, stopDistComponent);
            }
            return stopDistanceLocal.Length();// Stop distance is just the magnitude of our result vector now
        }

        Vector3 RaycastStopPosition(IMyShipController controller, double stopDistance, Vector3 dir) {
            Vector3D normalizedVelocity = Vector3D.Normalize(controller.GetShipVelocities().LinearVelocity);
            Vector3D position = normalizedVelocity * stopDistance;
            Vector3D stopPosition = position + (Vector3D.Normalize(position - controller.GetPosition()) * 250d);
            Vector3D relative = Vector3D.TransformNormal(normalizedVelocity, MatrixD.Transpose(controller.WorldMatrix));
            Base6Directions.Direction direction = Base6Directions.GetClosestDirection(relative);
            IMyCameraBlock lidar = null;
            switch (direction) {
                case Base6Directions.Direction.Forward:
                    lidar = GetCameraWithMaxRange(LIDARS);
                    break;
                case Base6Directions.Direction.Backward:
                    lidar = GetCameraWithMaxRange(LIDARSBACK);
                    break;
                case Base6Directions.Direction.Up:
                    lidar = GetCameraWithMaxRange(LIDARSUP);
                    break;
                case Base6Directions.Direction.Down:
                    lidar = GetCameraWithMaxRange(LIDARSDOWN);
                    break;
                case Base6Directions.Direction.Left:
                    lidar = GetCameraWithMaxRange(LIDARSLEFT);
                    break;
                case Base6Directions.Direction.Right:
                    lidar = GetCameraWithMaxRange(LIDARSRIGHT);
                    break;
            }
            if (lidar == null) { return dir; }
            MyDetectedEntityInfo entityInfo = lidar.Raycast(stopPosition);//TODO
            if (!entityInfo.IsEmpty()) {
                //dir = -normalizedVelocity;
                if (controller.WorldMatrix.Forward.Dot(normalizedVelocity) > 0d) {
                    dir.Z = 1f;//go backward
                } else if (controller.WorldMatrix.Backward.Dot(normalizedVelocity) > 0d) {
                    dir.Z = -1f;
                }
                if (controller.WorldMatrix.Up.Dot(normalizedVelocity) > 0d) {
                    dir.Y = -1f;//go down
                } else if (controller.WorldMatrix.Down.Dot(normalizedVelocity) > 0d) {
                    dir.Y = 1f;
                }
                if (controller.WorldMatrix.Left.Dot(normalizedVelocity) > 0d) {
                    dir.X = 1f;//go right
                } else if (controller.WorldMatrix.Right.Dot(normalizedVelocity) > 0d) {
                    dir.X = -1f;
                }
            }
            return dir;
        }

        Vector3 KeepAltitude(bool isUnderControl, Vector3 dir, IMyShipController controller, bool idleThrusters, bool keepAltitude, Vector3D gravity) {
            if (!isUnderControl && !Vector3D.IsZero(gravity) && idleThrusters && keepAltitude) {
                if (keepAltitudeOnce) {
                    hoverPosition = controller.CubeGrid.WorldVolume.Center;
                    keepAltitudeOnce = false;
                }
                if (keepAltitudeCount >= keepAltitudeDelay) {
                    double distance = Vector3D.Distance(hoverPosition, controller.CubeGrid.WorldVolume.Center);
                    if (distance > 300d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(hoverPosition, "hoverPosition");
                        REMOTE.SetAutoPilotEnabled(true);
                        keepAltitudeCount = 0;
                        return Vector3.Zero;
                    }
                    keepAltitudeCount = 0;
                }
                keepAltitudeCount++;
                double altitude;
                controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                if (altitudeToKeep == 0d) {
                    altitudeToKeep = altitude;
                }
                if (altitude < altitudeToKeep - 30d) {
                    Vector3D gravTransformed = -Vector3D.Sign(Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix)));
                    dir += Vector3D.IsZeroVector(dir) * gravTransformed;
                } else if (altitude > altitudeToKeep + 30d) {
                    Vector3D gravTransformed = Vector3D.Sign(Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix)));
                    dir += Vector3D.IsZeroVector(dir) * gravTransformed;
                }
            } else {
                if (!keepAltitudeOnce) {
                    keepAltitudeOnce = true;
                    altitudeToKeep = 0d;
                    //hoverPosition = Vector3D.Zero;
                }
            }
            return dir;
        }

        void DeadMan(bool isUnderControl) {
            if (!isUnderControl) {
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
                        foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = true; }
                        cntrllr.DampenersOverride = true;
                    } else {
                        if (!deadManOnce) {
                            if (idleThrusters) {
                                foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = false; }
                            }
                            deadManOnce = true;
                        }
                    }
                }
            } else {
                if (deadManOnce) {
                    foreach (IMyShipController block in CONTROLLERS) { block.ControlThrusters = true; }
                    deadManOnce = false;
                }
            }
        }

        bool TurretsDetection() {
            bool targetFound = false;
            if (impactDetectionCount == impactDetectionDelay) {
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
                    targetInfo = default(MyDetectedEntityInfo);
                }
                impactDetectionCount = 0;
            }
            impactDetectionCount++;
            return targetFound;
        }

        void ManageTarget(out Vector3D trgP, out Vector3D trgV) {
            trgP = Vector3D.Zero;
            trgV = Vector3D.Zero;
            if (targFound) {
                trgP = targPos;
                trgV = targVelVec;
            } else if (!targetInfo.IsEmpty() && targetInfo.HitPosition.HasValue) {
                trgP = targetInfo.HitPosition.Value;
                trgV = targetInfo.Velocity;
            }
        }

        void CheckTarget(IMyShipController controller, IMyRemoteControl remote, Vector3D trgP, Vector3D trgV) {
            if (!Vector3D.IsZero(trgP)) {
                if (lockTargetOnce) {
                    bool aligned;
                    if (!targFound) {
                        aligned = AimAtTarget(controller, trgP);
                    } else {
                        aligned = true;
                    }
                    impactDetectionDelay = 1;
                    if (aligned) {
                        if (!targFound) {
                            PAINTERPB.TryRun(argLockTarget);
                        }
                        lockTargetOnce = false;
                        impactDetectionDelay = 5;
                    }
                }
                CheckCollisions(remote, trgP, trgV);
            } else {
                if (!lockTargetOnce) {
                    PAINTERPB.TryRun(argUnlockFromTarget);
                    lockTargetOnce = true;
                }
                if (!Vector3D.IsZero(returnPosition)) {
                    escapePosition = Vector3D.Zero;
                    remote.ClearWaypoints();
                    remote.AddWaypoint(returnPosition, "returnPosition");
                    remote.SetAutoPilotEnabled(true);
                    returnOnce = true;
                }
            }
        }

        void CheckCollisions(IMyRemoteControl remote, Vector3D targetPos, Vector3D targetVelocity) {
            BoundingBoxD gridLocalBB = new BoundingBoxD(remote.CubeGrid.Min * remote.CubeGrid.GridSize, remote.CubeGrid.Max * remote.CubeGrid.GridSize);
            MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(gridLocalBB, remote.CubeGrid.WorldMatrix);
            double time = 30.0d;//TODO
            Vector3D targetFuturePosition = targetPos + (targetVelocity * time);
            LineD line = new LineD(targetPos, targetFuturePosition);
            double? hitDist = obb.Intersects(ref line);
            if (hitDist.HasValue) {
                if (returnOnce) {
                    if (Vector3D.IsZero(returnPosition)) {
                        returnPosition = remote.GetPosition();
                    }
                    returnOnce = false;
                }
                if (escapeCount == escapeDelay) {//TODO
                    Vector3D escapePos = targetPos + (Vector3D.Normalize(targetPos - remote.GetPosition()) * escapeDistance);
                    escapePosition = Vector3D.Cross(escapePos, remote.WorldMatrix.Down);
                    remote.ClearWaypoints();
                    remote.AddWaypoint(escapePosition, "escapePosition");
                    remote.SetAutoPilotEnabled(true);
                    escapeCount = 0;
                }
                escapeCount++;
            }
        }

        void ManageWaypoints(IMyRemoteControl remote, bool isUnderControl) {
            if (!isUnderControl) {
                isPilotedOnce = true;
            } else {
                if (isPilotedOnce) {
                    remote.ClearWaypoints();
                    returnPosition = Vector3D.Zero;
                    escapePosition = Vector3D.Zero;
                    isPilotedOnce = false;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(targetPosition)) {
                double dist = Vector3D.Distance(targetPosition, remote.GetPosition());
                if (dist < stopDistance) {
                    remote.SetAutoPilotEnabled(false);
                    targetPosition = Vector3D.Zero;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(escapePosition)) {
                double dist = Vector3D.Distance(escapePosition, remote.GetPosition());
                if (dist < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    escapePosition = Vector3D.Zero;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition) && Vector3D.IsZero(escapePosition)) {
                double dist = Vector3D.Distance(returnPosition, remote.GetPosition());
                if (dist < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    returnPosition = Vector3D.Zero;
                    returnOnce = true;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(hoverPosition)) {
                double dist = Vector3D.Distance(hoverPosition, remote.GetPosition());
                if (dist < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    hoverPosition = Vector3D.Zero;
                }
            }
        }

        void RangeFinder(IMyRemoteControl remote) {
            targetLog.Clear();
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                foreach (var block in ALARMS) { block.Play(); }
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
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, selectedPlanet);
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    double targetRadius = Vector3D.Distance(TARGET.Position, hitPosition);
                    targetLog.Append("Radius: ").Append(targetRadius.ToString("0.0")).Append(", ");
                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                    double targetGroundDistance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Ground Dist.: ").Append(targetGroundDistance.ToString("0.0")).Append("\n");
                    double targetAtmoHeight = Vector3D.Distance(hitPosition, safeJumpPosition);
                    targetLog.Append("Atmo. Height: ").Append(targetAtmoHeight.ToString("0.0")).Append("\n");
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, "Asteroid");
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Asteroid:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * enemySafeDistance);
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, TARGET.Name);
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    double targetDistance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");
                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                } else {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, TARGET.Name);
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    targetPosition = safeJumpPosition;
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    double targetDistance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");
                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
            } else {
                targetLog.Append("Nothing Detected!\n");
            }
        }

        bool AimAtTarget(IMyShipController controller, Vector3D targetPos) {
            bool aligned = false;
            Vector3D aimDirection = targetPos - controller.GetPosition();
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, controller.WorldMatrix.Up, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, controller.WorldMatrix);
            Vector3D forwardVec = controller.WorldMatrix.Forward;
            double angle = VectorMath.AngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= angleTolerance) {
                aimTarget = false;
                aligned = true;
                UnlockGyros();
            }
            return aligned;
        }

        void SunChase(bool isControlled, Vector3D gravity, bool targFound) {
            if (!isControlled && sunChasing && Vector3D.IsZero(gravity) && !targFound) {
                if (SOLAR.IsFunctional && SOLAR.Enabled && SOLAR.IsWorking) {
                    if (sunChaseOnce) {
                        LCDSUNCHASER.BackgroundColor = new Color(0, 255, 255);
                        prevSunPower = SOLAR.MaxOutput;
                        unlockSunChaseOnce = true;
                        sunChaseOnce = false;
                    }
                    double pitch = 0d;
                    double yaw = 0d;
                    float power = SOLAR.MaxOutput;
                    int powerDifference = Math.Sign(power - prevSunPower);
                    if (power < .02) {
                        if (unlockSunChaseOnce) {
                            UnlockGyros();
                            unlockSunChaseOnce = false;
                        }
                        return;
                    }
                    if (power > .98) {
                        if (sunAlignmentStep > 0) {
                            sunAlignmentStep = 0;
                            if (unlockSunChaseOnce) {
                                UnlockGyros();
                                unlockSunChaseOnce = false;
                            }
                        }
                        return;
                    }
                    unlockSunChaseOnce = true;
                    switch (sunAlignmentStep) {
                        case 0:
                            selectedSunAlignmentStep = 0;
                            sunAlignmentStep++;
                            break;
                        case 1:
                            if (powerDifference < 0) {
                                movePitch = -movePitch;
                                selectedSunAlignmentStep++;
                                if (selectedSunAlignmentStep > 2) {
                                    sunAlignmentStep++;
                                    selectedSunAlignmentStep = 0;
                                }
                            }
                            pitch = movePitch;
                            break;
                        case 2:
                            if (powerDifference < 0) {
                                moveYaw = -moveYaw;
                                selectedSunAlignmentStep++;
                                if (selectedSunAlignmentStep > 2) {
                                    UnlockGyros();
                                    sunAlignmentStep = 0;
                                    selectedSunAlignmentStep = 0;
                                }
                            }
                            yaw = moveYaw;
                            break;
                    }
                    double yawSpeed = yawController.Control(yaw);
                    double pitchSpeed = pitchController.Control(pitch);
                    ApplyGyroOverride(pitchSpeed, yawSpeed, 0d, GYROS, SOLAR.WorldMatrix);
                    prevSunPower = power;
                } else {
                    foreach (IMySolarPanel solar in SOLARS) {
                        if (solar.IsFunctional && solar.Enabled && solar.IsWorking) {
                            SOLAR = solar;
                        }
                    }
                }
            } else {
                if (!sunChaseOnce) {
                    UnlockGyros();
                    LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0);
                    prevSunPower = 0f;
                    sunChaseOnce = true;
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
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0d);
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
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1d, 1d));
            }
            if (Vector3D.IsZero(axis)) {
                angle = desiredForwardVector.Z < 0d ? 0d : Math.PI;
                yaw = angle;
                pitch = 0d;
                roll = 0d;
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
                if (thisGyro.Closed) { continue; }
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
                if (block.IsUnderControl && block.IsFunctional && block.CanControlShip) {
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

        bool IsAutoPiloted() {
            bool isAutoPiloted = false;
            foreach (IMyRemoteControl block in REMOTES) {
                if (block.IsAutoPilotEnabled) {
                    isAutoPiloted = true;
                    break;
                }
            }
            return isAutoPiloted;
        }

        void SetSensorsExtend() {
            if (UPSENSOR != null) { UPSENSOR.LeftExtend = 30f; UPSENSOR.RightExtend = 30f; UPSENSOR.BottomExtend = 28.5f; UPSENSOR.TopExtend = 40f; BACKWARDSENSOR.BackExtend = 0.1f; UPSENSOR.FrontExtend = UPSENSOR.MaxRange; }
            if (DOWNSENSOR != null) { DOWNSENSOR.LeftExtend = 30f; DOWNSENSOR.RightExtend = 30f; DOWNSENSOR.BottomExtend = 38.5f; DOWNSENSOR.TopExtend = 30f; DOWNSENSOR.BackExtend = 5f; DOWNSENSOR.FrontExtend = DOWNSENSOR.MaxRange; }
            if (FORWARDSENSOR != null) { FORWARDSENSOR.LeftExtend = 30f; FORWARDSENSOR.RightExtend = 30f; FORWARDSENSOR.BottomExtend = 12.5f; FORWARDSENSOR.TopExtend = 8.5f; FORWARDSENSOR.BackExtend = FORWARDSENSOR.MaxRange; FORWARDSENSOR.FrontExtend = 0.1f; }
            if (BACKWARDSENSOR != null) { BACKWARDSENSOR.LeftExtend = 30f; BACKWARDSENSOR.RightExtend = 30f; BACKWARDSENSOR.BottomExtend = 15f; BACKWARDSENSOR.TopExtend = 6f; BACKWARDSENSOR.BackExtend = 0.1f; BACKWARDSENSOR.FrontExtend = BACKWARDSENSOR.MaxRange; }
            if (LEFTSENSOR != null) { LEFTSENSOR.LeftExtend = 33f; LEFTSENSOR.RightExtend = 36f; LEFTSENSOR.BottomExtend = 7.5f; LEFTSENSOR.TopExtend = 13.5f; LEFTSENSOR.BackExtend = 0.1f; LEFTSENSOR.FrontExtend = LEFTSENSOR.MaxRange; }
            if (RIGHTSENSOR != null) { RIGHTSENSOR.LeftExtend = 36f; RIGHTSENSOR.RightExtend = 33f; RIGHTSENSOR.BottomExtend = 7.5f; RIGHTSENSOR.TopExtend = 13.5f; RIGHTSENSOR.BackExtend = 0.1f; RIGHTSENSOR.FrontExtend = RIGHTSENSOR.MaxRange; }
        }

        double GetMaxJumpDistance(IMyJumpDrive jumpDrive) {
            double maxDistance = (double)jumpDrive.MaxJumpDistanceMeters;
            return maxDistance;
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
            if (cam != null) { lidarsLog.Append("Avail. Range: ").Append(cam.AvailableScanRange.ToString("0.0")).Append("\n"); }
        }

        void ReadJumpersInfos() {
            jumpersLog.Clear();
            if (JUMPERS.Count != 0) {
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
                if (currentStoredPower > 0d) {
                    double totJumpPercent = currentStoredPower / maxStoredPower * 100d;
                    jumpersLog.Append("Stored Power: ").Append(totJumpPercent.ToString("0.00")).Append("%\n");
                } else {
                    jumpersLog.Append("Stored Power: ").Append("0%\n");
                }
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

        void GetBlocks() {
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, block => block.CustomName.Contains(controllersName));
            REMOTES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(REMOTES, block => block.CustomName.Contains(remotesName));
            int count = 0;
            foreach (IMyRemoteControl remote in REMOTES) {
                if (count == 0) { REMOTE = remote; } else if (remote.IsMainCockpit) { REMOTE = remote; }
                count++;
            }
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains(cockpitsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains(thrustersName));
            UPTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(upName)));
            DOWNTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(downName)));
            LEFTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(leftName)));
            RIGHTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(rightName)));
            FORWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(forwardName)));
            BACKWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(backwardName)));
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, block => block.CustomName.Contains(lidarsName));
            LIDARSBACK.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSBACK, block => block.CustomName.Contains(lidarsBackName));
            LIDARSUP.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSUP, block => block.CustomName.Contains(lidarsUpName));
            LIDARSDOWN.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSDOWN, block => block.CustomName.Contains(lidarsDownName));
            LIDARSLEFT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSLEFT, block => block.CustomName.Contains(lidarsLeftName));
            LIDARSRIGHT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSRIGHT, block => block.CustomName.Contains(lidarsRightName));
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
            LCDSUNCHASER = GridTerminalSystem.GetBlockWithName(sunChaserPanelName) as IMyTextPanel;
            PAINTERPB = GridTerminalSystem.GetBlockWithName(painterName) as IMyProgrammableBlock;
            SENSORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(SENSORS, block => block.CustomName.Contains(sensorsName));
            foreach (IMySensorBlock sensor in SENSORS) {
                if (sensor.CustomName.Contains(upName)) { UPSENSOR = sensor; } else if (sensor.CustomName.Contains(downName)) { DOWNSENSOR = sensor; } else if (sensor.CustomName.Contains(leftName)) { LEFTSENSOR = sensor; } else if (sensor.CustomName.Contains(rightName)) { RIGHTSENSOR = sensor; } else if (sensor.CustomName.Contains(forwardName)) { FORWARDSENSOR = sensor; } else if (sensor.CustomName.Contains(backwardName)) { BACKWARDSENSOR = sensor; }
            }
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains(solarsName));
            foreach (IMySolarPanel solar in SOLARS) { if (solar.IsFunctional && solar.Enabled && solar.IsWorking) { SOLAR = solar; } }
        }

        void InitPIDControllers() {
            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
        }

        public class PID {
            public double _kP = 0d;
            public double _kI = 0d;
            public double _kD = 0d;
            public double _integralDecayRatio = 0d;
            public double _lowerBound = 0d;
            public double _upperBound = 0d;
            double _timeStep = 0d;
            double _inverseTimeStep = 0d;
            double _errorSum = 0d;
            double _lastError = 0d;
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
                _inverseTimeStep = 1d / _timeStep;
                _integralDecay = false;
            }

            public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1d / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control(double error) {
                double errorDerivative = (error - _lastError) * _inverseTimeStep;//Compute derivative term
                if (_firstRun) {
                    errorDerivative = 0d;
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
                _inverseTimeStep = 1d / _timeStep;
                return Control(error);
            }

            public void Reset() {
                _errorSum = 0d;
                _lastError = 0d;
                _firstRun = true;
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
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return 0d; } else { return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1)); }
            }

            public static double CosBetween(Vector3D a, Vector3D b) {//returns radians
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return 0d; } else { return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1); }
            }
        }


    }
}
