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
        //TODO
        //manage multi target
        //do logic for thrusters and !magneticDrive
        //when landing lock landing gear, when taking of unlock landing gear
        //NAVIGATOR
        DebugAPI Debug;

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

        readonly string navigatorTag = "[NAVIGATOR]";
        readonly string managerTag = "[MANAGER]";
        readonly string painterTag = "[PAINTER]";
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
        readonly double gunsMaxRange = 2000d;
        readonly double gunsCloseRange = 800d;
        readonly double gunsMidRange = 1400d;
        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;
        readonly double stopDistance = 50d;
        readonly double minAltitude = 60d;
        readonly float autocannonGatlingSpeed = 400f;
        readonly float railgunSpeed = 2000f;
        readonly float smallRailgunSpeed = 1000f;
        readonly float maxSpeed = 105f;
        readonly float securitySpeed = 10f;
        readonly float minSpeed = 2f;
        readonly float deadManMinSpeed = 0.1f;
        readonly float targetVel = 29 * rpsOverRpm;
        readonly float syncSpeed = 1 * rpsOverRpm;
        readonly int tickDelay = 50;
        readonly int randomDelay = 10;
        readonly int sensorsDelay = 5;
        readonly int collisionCheckDelay = 10;
        readonly int keepAltitudeDelay = 50;
        int turretsDetectionDelay = 5;

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
        bool unlockOnce = true;
        bool isPilotedOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool initRandomMagneticDriveOnce = true;
        bool initEvasionMagneticDriveOnce = true;
        bool sunChaseOnce = true;
        bool unlockSunChaseOnce = true;
        bool keepAltitudeOnce = true;
        bool sensorDetectionOnce = true;
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
        int turretsDetectionCount = 5;
        int tickCount = 0;
        int randomCount = 50;
        int sensorsCount = 50;
        int collisionCheckCount = 0;
        int keepAltitudeCount = 0;
        long targId;

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
        public List<MyDetectedEntityInfo> targetsInfo = new List<MyDetectedEntityInfo>();
        MatrixD targOrientation = new MatrixD();
        Vector3D targetPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D hoverPosition = Vector3D.Zero;
        Vector3D landPosition = Vector3D.Zero;
        Vector3D lastForwardVector = Vector3D.Zero;
        Vector3D lastUpVector = Vector3D.Zero;
        Vector3D targPosition = Vector3D.Zero;
        Vector3D targVelVec = Vector3D.Zero;
        Vector3D lastVelocity = Vector3D.Zero;
        Vector3D maxAccel;
        Vector3D minAccel;
        Vector3D randomDir = Vector3D.Zero;
        Vector3D sensorDir = Vector3D.Zero;
        Vector3D collisionDir = Vector3D.Zero;
        Vector3D stopDir = Vector3D.Zero;

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
            Debug = new DebugAPI(this);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(navigatorTag);
            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }
            foreach (IMyCameraBlock cam in LIDARS) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSBACK) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSUP) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSDOWN) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSLEFT) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSRIGHT) { cam.EnableRaycast = true; }
            if (LIDARS.Count != 0) { maxScanRange = LIDARS[0].RaycastDistanceLimit; }
            selectedPlanet = planetsList.ElementAt(0).Key;
            InitPIDControllers();
            LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0);
        }

        public void Main(string arg) {
            try {
                Debug.RemoveDraw();

                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                //Debug.PrintHUD($"LastRunTimeMs: {Runtime.LastRunTimeMs:0.00}");

                GetBroadcastMessages();

                Debug.PrintHUD($"targFound:{targFound}");

                TurretsDetection(targFound);
                bool aiming = ManageCollisions(targFound, REMOTE);
                bool isControlled = GetController();
                SendBroadcastControllerMessage(isControlled);
                bool isAutoPiloted = IsAutoPiloted();
                bool isUnderControl = IsPiloted(false);

                IMyShipController controller = CONTROLLER ?? REMOTE;
                Vector3D gravity = controller.GetNaturalGravity();
                Vector3D myVelocity = controller.GetShipVelocities().LinearVelocity;
                double mySpeed = controller.GetShipSpeed();

                if (!string.IsNullOrEmpty(arg)) { ProcessArgument(arg, gravity); }

                ManageWaypoints(REMOTE, isUnderControl);

                GyroStabilize(controller, targFound, aimTarget, isAutoPiloted, useRoll, gravity, mySpeed, aiming);

                ManageMagneticDrive(controller, isControlled, isUnderControl, isAutoPiloted, targFound, idleThrusters, keepAltitude, gravity, myVelocity, mySpeed);

                if (aimTarget) { AimAtTarget(controller, targetPosition, angleTolerance); }

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

        void ProcessArgument(string argument, Vector3D gravity) {
            switch (argument) {
                case argRangeFinder:
                    if (Vector3D.IsZero(gravity)) {
                        RangeFinder(REMOTE);
                    } else {
                        Land(REMOTE, gravity);
                    }
                    break;
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
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
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
                    LCDDEADMAN.BackgroundColor = controlDampeners ? new Color(0, 255, 255) : new Color(0, 0, 0);
                    break;
                case argMagneticDrive:
                    magneticDrive = !magneticDrive;
                    break;
                case argSetPlanet:
                    if (!aimTarget) {
                        MyTuple<Vector3D, double, double> planet;
                        planetsList.TryGetValue(selectedPlanet, out planet);
                        double planetSize = planet.Item2 + planet.Item3 + 1000d;
                        Vector3D safeJumpPosition = planet.Item1 - (Vector3D.Normalize(planet.Item1 - REMOTE.CubeGrid.WorldVolume.Center) * planetSize);//normalize?
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
                    MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, long>) {
                        MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, long> data = (MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, long>)igcMessage.Data;
                        targFound = data.Item1;
                        //targHitPos = data.Item2;
                        targVelVec = data.Item3;
                        targOrientation = data.Item4;
                        targPosition = data.Item5;
                        targId = data.Item6;
                        received = true;
                    }
                    if (igcMessage.Data is MyTuple<int, bool, bool, bool, bool>) {
                        MyTuple<int, bool, bool, bool, bool> data = (MyTuple<int, bool, bool, bool, bool>)igcMessage.Data;
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
            MyTuple<string, bool> tuple = MyTuple.Create("isControlled", isControlled);
            IGC.SendBroadcastMessage(managerTag, tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastLockTargetMessage(bool lockTarget, Vector3D targetPos, Vector3D targetVel) {
            if (lockTarget) {
                MyTuple<string, string, Vector3D, Vector3D> tuple = MyTuple.Create("Lock", argLockTarget, targetPos, targetVel);
                IGC.SendBroadcastMessage(painterTag, tuple, TransmissionDistance.ConnectedConstructs);
            } else {
                MyTuple<string, string, Vector3D, Vector3D> tuple = MyTuple.Create("Clear", argUnlockFromTarget, targetPos, targetVel);
                IGC.SendBroadcastMessage(painterTag, tuple, TransmissionDistance.ConnectedConstructs);
            }
        }

        bool GetController() {
            if (CONTROLLER != null && (!CONTROLLER.IsUnderControl || !CONTROLLER.CanControlShip)) {
                CONTROLLER = null;
            }
            if (CONTROLLER == null) {
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.IsUnderControl && block.IsFunctional && block.CanControlShip && !(block is IMyRemoteControl)) {
                        CONTROLLER = block;
                        return true;
                    }
                }
            }
            bool controlled = false;
            if (CONTROLLER == null) {
                if (REMOTE.IsAutoPilotEnabled) {
                    CONTROLLER = REMOTE;
                    return true;
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
                        return true;
                    } else if (!Vector3D.IsZero(controller.GetNaturalGravity())) {
                        CONTROLLER = controller;
                        return true;
                    } else if (targFound) {
                        CONTROLLER = controller;
                        return true;
                    } else if (!Vector3D.IsZero(collisionDir)) {
                        CONTROLLER = controller;
                        return true;
                    }
                }
            } else {
                return true;
            }
            return controlled;
        }

        void GyroStabilize(IMyShipController controller, bool targetFound, bool aimingTarget, bool isAutoPiloted, bool useRoll, Vector3D gravity, double mySpeed, bool aiming) {
            if (useGyrosToStabilize && !targetFound && !aimingTarget && !isAutoPiloted && !aiming) {

                //Debug.PrintHUD($"GyroStabilize");

                if (!Vector3D.IsZero(gravity)) {
                    Vector3D horizonVec = Vector3D.Cross(gravity, Vector3D.Cross(controller.WorldMatrix.Forward, gravity));//left vector
                    double pitchAngle, rollAngle, yawAngle;
                    GetRotationAnglesSimultaneous(horizonVec, -gravity, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                    double mouseYaw = controller.RotationIndicator.Y;
                    double mousePitch = controller.RotationIndicator.X;
                    double mouseRoll = controller.RollIndicator;
                    if (mousePitch != 0d) {
                        mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                    }
                    mousePitch = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                    if (mouseRoll != 0d) {
                        mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                    }
                    mouseRoll = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
                    yawAngle = 0d;
                    if (mySpeed > minSpeed) {
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
                    mouseYaw = mouseYaw == 0d ? yawController.Control(yawAngle) : yawController.Control(mouseYaw);
                    if (mousePitch == 0 && mouseYaw == 0 && mouseRoll == 0) {
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
                } else {
                    if (mySpeed > minSpeed) {
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
                        mouseYaw = mouseYaw == 0d ? yawController.Control(yawAngle) : yawController.Control(mouseYaw);
                        if (mousePitch != 0d) {
                            mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                        }
                        mousePitch = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                        if (mouseRoll != 0d) {
                            mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                        }
                        mouseRoll = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
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

        void ManageMagneticDrive(IMyShipController controller, bool isControlled, bool isUnderControl, bool isAutoPiloted, bool targetFound, bool idleThrusters, bool keepAltitude, Vector3D gravity, Vector3D myVelocity, double mySpeed) {
            if (magneticDrive && isControlled) {
                Vector3D dir = Vector3D.Zero;
                if (initMagneticDriveOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    sunChasing = false;
                    initMagneticDriveOnce = false;
                }
                SyncRotors();
                double altitude = minAltitude;
                if (!Vector3D.IsZero(gravity)) {
                    controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                }
                if (!Vector3D.IsZero(collisionDir)) {
                    if (initEvasionMagneticDriveOnce) {
                        controller.DampenersOverride = false;
                        initEvasionMagneticDriveOnce = false;
                    }
                    dir = collisionDir;

                    //Debug.PrintHUD($"collisionDir: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                } else {
                    if (!initEvasionMagneticDriveOnce) {
                        randomDir = Vector3D.Zero;
                        controller.DampenersOverride = true;
                        initEvasionMagneticDriveOnce = true;
                    }
                    if (isAutoPiloted) {
                        dir = AutoMagneticDrive(dir);

                        //Debug.PrintHUD($"AutoMagneticDrive: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                    } else {
                        if (!initAutoMagneticDriveOnce) {
                            foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                            initAutoMagneticDriveOnce = true;
                        }
                        if (!isUnderControl && targetFound) {
                            if (initRandomMagneticDriveOnce) {
                                controller.DampenersOverride = false;
                                initRandomMagneticDriveOnce = false;
                            }
                            RandomMagneticDrive();
                            dir = randomDir;

                            //Debug.PrintHUD($"RandomMagneticDrive: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                            Vector3D dirNN = Vector3D.Zero;
                            foreach (MyDetectedEntityInfo target in targetsInfo) {
                                Vector3D escapeDir = controller.CubeGrid.WorldVolume.Center - target.Position;//normalize?
                                escapeDir = Vector3D.Transform(escapeDir, MatrixD.Transpose(controller.WorldMatrix));
                                dirNN = SetResultVector(dirNN, escapeDir);
                            }
                            Vector3D dirNew = KeepRightDistance(targPosition, controller);
                            dirNew = MergeDirectionValues(dirNN, dirNew);//TODO do not overwrite
                            dir = MergeDirectionValues(dir, dirNew);

                            //Debug.PrintHUD($"KeepRightDistance: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                        } else {
                            if (!initRandomMagneticDriveOnce) {
                                randomDir = Vector3D.Zero;
                                controller.DampenersOverride = true;
                                initRandomMagneticDriveOnce = true;
                            }
                            dir = MagneticDrive(controller, gravity, myVelocity, idleThrusters);

                            //Debug.PrintHUD($"MagneticDrive: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                            Vector3D dirNew = KeepAltitude(isUnderControl, controller, idleThrusters, keepAltitude, gravity, altitude);
                            dir = MergeDirectionValues(dir, dirNew);

                            //Debug.PrintHUD($"KeepAltitude: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");
                        }
                    }
                }

                Vector3D dirN = EvadeEnemy(controller, targOrientation, targVelVec, targPosition, controller.CubeGrid.WorldVolume.Center, myVelocity, gravity, targetFound);//, targHitPos
                foreach (MyDetectedEntityInfo target in targetsInfo) {
                    Vector3D escapeDir = EvadeEnemy(controller, target.Orientation, target.Velocity, target.Position, controller.CubeGrid.WorldVolume.Center, myVelocity, gravity, targetFound);
                    dirN = SetResultVector(dirN, escapeDir);
                }

                dir = MergeDirectionValues(dir, dirN);

                if (mySpeed > securitySpeed && !isAutoPiloted && (Vector3D.IsZero(gravity) || (!Vector3D.IsZero(gravity) && altitude > minAltitude))) {
                    if (sensorDetectionOnce) {
                        SetSensorsExtend();
                        sensorDetectionOnce = false;
                    }
                    SensorDetection();
                    dir = MergeDirectionValues(dir, sensorDir);

                    //Debug.PrintHUD($"SensorDetection: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                    UpdateAcceleration(controller, Runtime.TimeSinceLastRun.TotalSeconds, myVelocity);
                    if (collisionCheckCount >= collisionCheckDelay) {
                        double stopDistance = CalculateStopDistance(controller, myVelocity);
                        RaycastStopPosition(controller, stopDistance, myVelocity);
                        collisionCheckCount = 0;
                    }
                    if (!Vector3D.IsZero(stopDir)) {
                        dir = MergeDirectionValues(dir, stopDir);

                        //Debug.PrintHUD($"RaycastStopPosition: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");
                    }
                    collisionCheckCount++;
                } else {
                    if (!sensorDetectionOnce) {
                        foreach (IMySensorBlock sensor in SENSORS) {
                            sensor.BackExtend = 0.1f;
                            sensor.BottomExtend = 0.1f;
                            sensor.FrontExtend = 0.1f;
                            sensor.LeftExtend = 0.1f;
                            sensor.RightExtend = 0.1f;
                            sensor.TopExtend = 0.1f;
                        }
                        sensorDetectionOnce = true;
                    }
                }

                Debug.PrintHUD($"SetPower: X:{dir.X:0.00}, X:{dir.X:0.00}, Z:{dir.Z:0.00}");

                SetPower(dir);

            } else {
                if (!initMagneticDriveOnce) {
                    IdleMagneticDrive(idleThrusters);
                    initMagneticDriveOnce = true;
                }
                if (tickCount == tickDelay) {
                    if (controlDampeners) {
                        DeadMan(IsPiloted(true), mySpeed);
                        LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                    } else { LCDDEADMAN.BackgroundColor = new Color(0, 0, 0); }
                    LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(0, 255, 255) : new Color(0, 0, 0);
                    tickCount = 0;
                }
                tickCount++;
            }
        }

        void IdleMagneticDrive(bool idleThrusters) {
            SetPower(Vector3D.Zero);
            foreach (IMyMotorStator block in ROTORS) { block.TargetVelocityRPM = 0f; }
            foreach (IMyMotorStator block in ROTORSINV) { block.TargetVelocityRPM = 0f; }
            if (idleThrusters) {
                foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
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

        Vector3D AutoMagneticDrive(Vector3D dir) {
            if (initAutoMagneticDriveOnce) {
                UPTHRUST = InitAutopilotMagneticDrive(UPTHRUSTERS);
                DOWNTHRUST = InitAutopilotMagneticDrive(DOWNTHRUSTERS);
                LEFTTHRUST = InitAutopilotMagneticDrive(LEFTTHRUSTERS);
                RIGHTTHRUST = InitAutopilotMagneticDrive(RIGHTTHRUSTERS);
                FORWARDTHRUST = InitAutopilotMagneticDrive(FORWARDTHRUSTERS);
                BACKWARDTHRUST = InitAutopilotMagneticDrive(BACKWARDTHRUSTERS);
                initAutoMagneticDriveOnce = false;
            }
            if (FORWARDTHRUST.CurrentThrust > 0f) { dir.Z = -1f; } else if (BACKWARDTHRUST.CurrentThrust > 0f) { dir.Z = 1f; }
            if (UPTHRUST.CurrentThrust > 0f) { dir.Y = 1f; } else if (DOWNTHRUST.CurrentThrust > 0f) { dir.Y = -1f; }
            if (LEFTTHRUST.CurrentThrust > 0f) { dir.X = -1f; } else if (RIGHTTHRUST.CurrentThrust > 0f) { dir.X = 1f; }
            return dir;
        }

        Vector3D MagneticDrive(IMyShipController controller, Vector3D gravity, Vector3D myVelocity, bool idleThrusters) {//TODO
            Matrix mtrx;
            Vector3D direction = controller.MoveIndicator;
            controller.Orientation.GetMatrix(out mtrx);
            direction = Vector3D.Transform(direction, mtrx);
            if (!Vector3D.IsZero(direction)) {
                //debugLog.Append("dir X: " + dir.X + "\ndir Y: " + dir.Y + "\ndir Z: " + dir.Z + "\n\n");
                direction /= direction.Length();
                if (!toggleThrustersOnce && idleThrusters) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                    toggleThrustersOnce = true;
                }
            } else {
                if (toggleThrustersOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    toggleThrustersOnce = false;
                }
            }
            if (Vector3D.IsZero(gravity) && !controller.DampenersOverride && direction.LengthSquared() == 0f) {
                return Vector3D.Zero;
            }
            Vector3D vel = myVelocity;
            vel = Vector3D.Transform(vel, MatrixD.Transpose(controller.WorldMatrix.GetOrientation()));
            vel = direction * maxSpeed - Vector3D.Transform(vel, mtrx);
            if (Math.Abs(vel.X) < minSpeed) { vel.X = 0f; }
            if (Math.Abs(vel.Y) < minSpeed) { vel.Y = 0f; }
            if (Math.Abs(vel.Z) < minSpeed) { vel.Z = 0f; }
            return vel;
        }

        void SetPower(Vector3D pow) {
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

        void RandomMagneticDrive() {
            if (randomCount >= randomDelay) {
                randomDir = Vector3D.Zero;
                float randomFloat;
                randomFloat = (float)random.Next(-1, 1);
                randomDir.X = randomFloat;
                randomFloat = (float)random.Next(-1, 1);
                randomDir.Y = randomFloat;
                randomFloat = (float)random.Next(-1, 1);
                randomDir.Z = randomFloat;
                randomCount = 0;
            }
            randomCount++;
        }

        void SensorDetection() {
            if (sensorsCount >= sensorsDelay) {
                sensorDir = new Vector3D();
                List<MyDetectedEntityInfo> entitiesA = new List<MyDetectedEntityInfo>();
                List<MyDetectedEntityInfo> entitiesB = new List<MyDetectedEntityInfo>();
                LEFTSENSOR.DetectedEntities(entitiesA);
                RIGHTSENSOR.DetectedEntities(entitiesB);
                if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                    sensorDir.X = 0f;
                } else if (entitiesA.Count > 0) {
                    sensorDir.X = 1f;
                } else if (entitiesB.Count > 0) {
                    sensorDir.X = -1f;
                }
                entitiesA.Clear();
                entitiesB.Clear();
                UPSENSOR.DetectedEntities(entitiesA);
                DOWNSENSOR.DetectedEntities(entitiesB);
                if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                    sensorDir.Y = 0f;
                } else if (entitiesA.Count > 0) {
                    sensorDir.Y = -1f;
                } else if (entitiesB.Count > 0) {
                    sensorDir.Y = 1f;
                }
                entitiesA.Clear();
                entitiesB.Clear();
                FORWARDSENSOR.DetectedEntities(entitiesA);
                BACKWARDSENSOR.DetectedEntities(entitiesB);
                if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                    sensorDir.Z = 0f;
                } else if (entitiesA.Count > 0) {
                    sensorDir.Z = 1f;
                } else if (entitiesB.Count > 0) {
                    sensorDir.Z = -1f;
                }
                sensorsCount = 0;
            }
            sensorsCount++;
        }

        Vector3D KeepRightDistance(Vector3D targPos, IMyShipController controller) {
            Vector3D dir = Vector3D.Zero;
            double minDistance = gunsMidRange;
            double maxDistance = gunsMaxRange;
            if (!railgunsCanShoot && !artilleryCanShoot) {
                minDistance = gunsCloseRange;
                maxDistance = gunsMidRange;
                if (!assaultCanShoot && !smallRailgunsCanShoot) {
                    minDistance = 500d;
                    maxDistance = gunsCloseRange;
                }
            }
            Vector3D direction = targPos - controller.CubeGrid.WorldVolume.Center;//normalize?
            double distance = direction.Length();
            direction = Vector3D.Normalize(direction);
            if (distance > maxDistance) {
                dir = Vector3D.TransformNormal(direction, MatrixD.Transpose(controller.WorldMatrix));
            } else if (distance < minDistance) {
                dir = -Vector3D.TransformNormal(direction, MatrixD.Transpose(controller.WorldMatrix));
            }
            return dir;
        }

        void TurretsDetection(bool targFound) {
            bool targetFound = false;
            turretsDetectionDelay = Vector3D.IsZero(collisionDir) ? 5 : 1;
            if (turretsDetectionCount >= turretsDetectionDelay) {
                targetsInfo.Clear();
                if (targFound) {
                    foreach (IMyLargeTurretBase turret in TURRETS) {
                        MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                        if (!targ.IsEmpty()) {
                            if (targ.EntityId == targId) {
                                targetInfo = targ;
                                targetFound = true;
                            } else {
                                if (IsValidTarget(targ)) {
                                    targetsInfo.Add(targ);
                                }
                            }
                        }
                    }
                } else {
                    if (targetInfo.IsEmpty()) {
                        foreach (IMyLargeTurretBase turret in TURRETS) {
                            MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                            if (!targ.IsEmpty()) {
                                if (IsValidTarget(targ)) {
                                    if (!targetFound) {
                                        targetInfo = targ;
                                        targetFound = true;
                                    } else {
                                        targetsInfo.Add(targ);
                                    }
                                }
                            }
                        }
                    } else {
                        foreach (IMyLargeTurretBase turret in TURRETS) {
                            MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                            if (!targ.IsEmpty()) {
                                if (IsValidTarget(targ)) {
                                    if (targ.EntityId == targetInfo.EntityId) {
                                        targetInfo = targ;
                                        targetFound = true;
                                    } else {
                                        targetsInfo.Add(targ);
                                    }
                                }
                            }
                        }
                    }
                }
                if (!targetFound) {
                    targetInfo = default(MyDetectedEntityInfo);
                }
                turretsDetectionCount = 0;
            }
            turretsDetectionCount++;
        }

        bool ManageCollisions(bool targFound, IMyRemoteControl remote) {
            bool aiming = false;
            if (!targFound) {//!isUnderControl
                if (!targetInfo.IsEmpty() && targetInfo.HitPosition.HasValue) {
                    aiming = true;
                    unlockOnce = false;
                    CheckTarget(remote, targetInfo.Position, targetInfo.Velocity);
                    collisionDir = CheckCollisions(remote, targetInfo.Position, targetInfo.Velocity);
                }
            } else {
                if (!unlockOnce) {
                    UnlockGyros();
                    unlockOnce = true;
                    collisionDir = Vector3D.Zero;
                    SendBroadcastLockTargetMessage(false, Vector3D.Zero, Vector3D.Zero);
                }
                if (!Vector3D.IsZero(returnPosition)) {
                    remote.ClearWaypoints();
                    remote.AddWaypoint(returnPosition, "returnPosition");
                    remote.SetAutoPilotEnabled(true);
                    returnOnce = true;
                }
            }
            foreach (MyDetectedEntityInfo target in targetsInfo) {
                Vector3D escapeDir = CheckCollisions(remote, target.Position, target.Velocity);
                collisionDir = SetResultVector(collisionDir, escapeDir);
            }
            return aiming;
        }

        void CheckTarget(IMyRemoteControl remote, Vector3D trgP, Vector3D trgV) {
            Vector3D targetPos = trgP + (trgV * (Runtime.TimeSinceLastRun.TotalSeconds));
            bool aligned = AimAtTarget(remote, targetPos, 30d);
            if (aligned) { SendBroadcastLockTargetMessage(true, targetPos, trgV); }
        }

        Vector3D CheckCollisions(IMyRemoteControl remote, Vector3D targetPos, Vector3D targetVelocity) {
            if (!Vector3D.IsZero(targetVelocity)) {
                targetVelocity = Vector3D.Normalize(targetVelocity);
                double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, targetPos);

                double angle = AngleBetween(targetVelocity, Vector3D.Normalize(remote.CubeGrid.WorldVolume.Center - targetPos)) * rad2deg;//normalize?

                Debug.PrintHUD($"EvadeEnemy, angle:{angle:0.00}, safety:{4500d / distance:0.00}");

                if (angle < (9000d / distance)) {//TODO
                    if (returnOnce) {
                        if (Vector3D.IsZero(returnPosition)) {
                            returnPosition = remote.CubeGrid.WorldVolume.Center;
                        }
                        returnOnce = false;
                    }
                    Vector3D enemyDirectionPosition = targetPos + (targetVelocity * distance);
                    Vector3D escapeDirection = Vector3D.Normalize(remote.CubeGrid.WorldVolume.Center - enemyDirectionPosition);//toward my center normalize?
                    escapeDirection = Vector3D.TransformNormal(escapeDirection, MatrixD.Transpose(remote.WorldMatrix));
                    //------------------------------------
                    Vector3D normalizedVec = Vector3D.Normalize(escapeDirection);
                    Vector3D position = remote.CubeGrid.WorldVolume.Center + (normalizedVec * 1000d);
                    Debug.DrawLine(remote.CubeGrid.WorldVolume.Center, position, Color.LimeGreen, thickness: 1f, onTop: true);
                    //------------------------------------
                    return escapeDirection;
                } else {
                    return Vector3D.Zero;
                }
            } else {
                return Vector3D.Zero;
            }
        }

        Vector3D EvadeEnemy(IMyShipController controller, MatrixD targOrientation, Vector3D targVel, Vector3D targPos, Vector3D myPosition, Vector3D myVelocity, Vector3D gravity, bool targetFound) {//, Vector3D targHitPos
            if (targetFound) {
                Base6Directions.Direction enemyForward = targOrientation.GetClosestDirection(controller.WorldMatrix.Backward);
                Base6Directions.Direction perpendicular = Base6Directions.GetPerpendicular(enemyForward);
                Vector3D enemyForwardVec = targOrientation.GetDirectionVector(enemyForward);
                Vector3D enemyPerpendicularVec = targOrientation.GetDirectionVector(perpendicular);
                targOrientation = MatrixD.CreateFromDir(enemyForwardVec, enemyPerpendicularVec);
                targOrientation.Translation = targPos;
                double distance = Vector3D.Distance(myPosition, targPos);
                Vector3D enemyAim;
                if (distance <= gunsCloseRange) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, autocannonGatlingSpeed, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, autocannonGatlingSpeed, enemyAim, gravity); }
                } else if (distance <= gunsMidRange) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, smallRailgunSpeed, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, smallRailgunSpeed, enemyAim, gravity); }
                } else if (distance <= gunsMaxRange) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, railgunSpeed, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, railgunSpeed, enemyAim, gravity); }
                } else {
                    return Vector3D.Zero;
                }
                //---------------------------------------------------------------------------
                Vector3D position = targPos + (enemyForwardVec * 2000d);
                Debug.DrawLine(targPos, position, Color.Orange, thickness: 2f, onTop: true);

                Vector3D normalizedVec = Vector3D.Normalize(enemyAim);
                position = targPos + (normalizedVec * 2000d);
                Debug.DrawLine(targPos, position, Color.Magenta, thickness: 2f, onTop: true);
                //---------------------------------------------------------------------------

                double angle = AngleBetween(enemyForwardVec, enemyAim) * rad2deg;

                Debug.PrintHUD($"EvadeEnemy, angle:{angle:0.00}, safety:{4500d / distance:0.00}");

                if (angle < (4500d / distance)) {
                    Vector3D evadeDirection = Vector3D.Normalize(controller.CubeGrid.WorldVolume.Center - (targPos + (enemyForwardVec * distance)));//toward my center - normalize?
                    evadeDirection = Vector3D.TransformNormal(evadeDirection, MatrixD.Transpose(controller.WorldMatrix));
                    //---------------------------------------------------------------------------
                    Debug.DrawPoint(targPos + (enemyForwardVec * distance), Color.Green, 4f, onTop: true);
                    Debug.DrawPoint(controller.CubeGrid.WorldVolume.Center, Color.Yellow, 4f, onTop: true);
                    Debug.DrawLine(controller.CubeGrid.WorldVolume.Center, targPos + (enemyForwardVec * distance), Color.Purple, thickness: 1f, onTop: true);
                    //---------------------------------------------------------------------------
                    return evadeDirection;
                }
            }
            return Vector3D.Zero;
        }

        Vector3D ComputeEnemyLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D myPosition, Vector3D myVelocity) {
            Vector3D aimPosition = GetEnemyAim(targetPosition, targetVelocity, myPosition, myVelocity, projectileSpeed);

            Debug.DrawPoint(aimPosition, Color.Red, 4f, onTop: true);

            return aimPosition - targetPosition;//normalize direction?
        }

        Vector3D GetEnemyAim(Vector3D targPosition, Vector3D targVelocity, Vector3D myPosition, Vector3D myVelocity, float projectileSpeed) {
            Vector3D toMe = myPosition - targPosition;//normalize?
            Vector3D diffVelocity = myVelocity - targVelocity;
            float a = (float)diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2f * (float)Vector3D.Dot(diffVelocity, toMe);
            float c = (float)toMe.LengthSquared();
            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);
            float t1 = p - q;
            float t2 = p + q;
            float t;
            if (t1 > t2 && t2 > 0) { t = t2; } else { t = t1; }
            Vector3D predictedPosition = myPosition + diffVelocity * t;
            return predictedPosition;
        }

        Vector3D BulletDrop(double distanceFromTarget, double projectileMaxSpeed, Vector3D desiredDirection, Vector3D gravity) {
            double timeToTarget = distanceFromTarget / projectileMaxSpeed;
            desiredDirection -= 0.5 * gravity * timeToTarget * timeToTarget;
            return desiredDirection;
        }

        void UpdateAcceleration(IMyShipController controller, double timeStep, Vector3D myVelocity) {
            lastVelocity = myVelocity;
            Vector3D localAcceleration = Vector3D.TransformNormal((myVelocity - lastVelocity) / timeStep, MatrixD.Transpose(controller.WorldMatrix));//acceleration - normalize?
            for (int i = 0; i < 3; ++i) {//Now we store off the components if they are larger (in magnitude) than what we have stored
                double component = localAcceleration.GetDim(i);
                if (component >= 0d) {
                    if (component > maxAccel.GetDim(i)) {//Bigger than what we have stored
                        maxAccel.SetDim(i, component);
                    }
                } else {//if negative
                    component = Math.Abs(component);
                    if (component > minAccel.GetDim(i)) {//Bigger (in magnitude) than what we have stored
                        minAccel.SetDim(i, component);
                    }
                }
            }
        }

        double CalculateStopDistance(IMyShipController controller, Vector3D myVelocity) {
            Vector3D localVelocity = Vector3D.TransformNormal(myVelocity, MatrixD.Transpose(controller.WorldMatrix));//normalize?
            Vector3D stopDistanceLocal = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {//Now we break the current velocity apart component by component
                double velocityComponent = localVelocity.GetDim(i);
                double stopDistComponent = velocityComponent >= 0d
                    ? velocityComponent * velocityComponent / (2d * minAccel.GetDim(i))
                    : velocityComponent * velocityComponent / (2d * maxAccel.GetDim(i));
                stopDistanceLocal.SetDim(i, stopDistComponent);
            }
            return stopDistanceLocal.Length();//Stop distance is just the magnitude of our result vector now
        }

        void RaycastStopPosition(IMyShipController controller, double stopDistance, Vector3D myVelocity) {
            Vector3D normalizedVelocity = Vector3D.Normalize(myVelocity);
            Base6Directions.Direction direction = Base6Directions.GetClosestDirection(Vector3D.TransformNormal(normalizedVelocity, MatrixD.Transpose(controller.WorldMatrix)));//relative - normalize?
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
            //----------------------------------
            Vector3D stop = controller.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance);
            Debug.PrintHUD($"stopDistance:{stopDistance:0.00}");
            Debug.DrawPoint(stop, Color.Blue, 5f, onTop: true);
            //----------------------------------

            stopDistance += 200d;//TODO

            Debug.DrawPoint(controller.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance), Color.Orange, 5f, onTop: true);

            MyDetectedEntityInfo entityInfo = lidar.Raycast(controller.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance));
            stopDir = Vector3D.Zero;
            if (!entityInfo.IsEmpty()) {
                //dir = -normalizedVelocity;
                if (controller.WorldMatrix.Forward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = 1f;//go backward
                } else if (controller.WorldMatrix.Backward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = -1f;
                }
                if (controller.WorldMatrix.Up.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = -1f;//go down
                } else if (controller.WorldMatrix.Down.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = 1f;
                }
                if (controller.WorldMatrix.Left.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = 1f;//go right
                } else if (controller.WorldMatrix.Right.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = -1f;
                }
            }
        }

        Vector3D KeepAltitude(bool isUnderControl, IMyShipController controller, bool idleThrusters, bool keepAltitude, Vector3D gravity, double altitude) {
            Vector3D dir = Vector3D.Zero;
            if (!isUnderControl && !Vector3D.IsZero(gravity) && idleThrusters && keepAltitude) {
                if (keepAltitudeOnce) {
                    hoverPosition = controller.CubeGrid.WorldVolume.Center;
                    keepAltitudeOnce = false;
                }
                if (altitude > minAltitude) {
                    if (keepAltitudeCount >= keepAltitudeDelay) {
                        if (Vector3D.Distance(hoverPosition, controller.CubeGrid.WorldVolume.Center) > 300d) {
                            REMOTE.ClearWaypoints();
                            REMOTE.AddWaypoint(hoverPosition, "hoverPosition");
                            REMOTE.SetAutoPilotEnabled(true);
                            keepAltitudeCount = 0;
                            return Vector3D.Zero;
                        }
                        keepAltitudeCount = 0;
                    }
                    keepAltitudeCount++;
                    if (altitudeToKeep == 0d) {
                        altitudeToKeep = altitude;
                    }
                    if (altitude < altitudeToKeep - 30d) {
                        dir = -Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix));//normalize?
                    } else if (altitude > altitudeToKeep + 30d) {
                        dir = Vector3D.TransformNormal(gravity, MatrixD.Transpose(controller.WorldMatrix));//normalize?
                    }
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

        Vector3D MergeDirectionValues(Vector3D dirToKeep, Vector3D dirNew) {//TODO
            //if (Math.Abs(dirNew.X) > minSpeed) { dirOld.X = dirNew.X; }
            //if (Math.Abs(dirNew.Y) > minSpeed) { dirOld.Y = dirNew.Y; }
            //if (Math.Abs(dirNew.Z) > minSpeed) { dirOld.Z = dirNew.Z; }
            dirToKeep.X = dirNew.X != 0d ? dirNew.X : dirToKeep.X;
            dirToKeep.Y = dirNew.Y != 0d ? dirNew.Y : dirToKeep.Y;
            dirToKeep.Z = dirNew.Z != 0d ? dirNew.Z : dirToKeep.Z;
            return dirToKeep;
        }

        Vector3D SetResultVector(Vector3D direction, Vector3D escapeDirection) {//TODO
            if (!Vector3D.IsZero(escapeDirection)) {
                if (!Vector3D.IsZero(direction)) {
                    direction = (direction + escapeDirection) / 2;
                } else {
                    direction = escapeDirection;
                }
            }
            return direction;
        }

        void DeadMan(bool isUnderControl, double mySpeed) {
            if (!isUnderControl) {
                IMyShipController cntrllr = null;
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.CanControlShip) {
                        cntrllr = block;
                        break;
                    }
                }
                if (cntrllr != null) {
                    if (mySpeed > deadManMinSpeed) {
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                        cntrllr.DampenersOverride = true;
                    } else {
                        if (!deadManOnce) {
                            if (idleThrusters) {
                                foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                            }
                            deadManOnce = true;
                        }
                    }
                }
            } else {
                if (deadManOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    deadManOnce = false;
                }
            }
        }

        void ManageWaypoints(IMyRemoteControl remote, bool isUnderControl) {
            if (!isUnderControl) {
                isPilotedOnce = true;
            } else {
                if (isPilotedOnce) {
                    remote.ClearWaypoints();
                    returnPosition = Vector3D.Zero;
                    hoverPosition = Vector3D.Zero;
                    landPosition = Vector3D.Zero;
                    isPilotedOnce = false;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(targetPosition)) {
                if (Vector3D.Distance(targetPosition, remote.CubeGrid.WorldVolume.Center) < stopDistance) {
                    remote.SetAutoPilotEnabled(false);
                    targetPosition = Vector3D.Zero;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition)) {
                if (Vector3D.Distance(returnPosition, remote.CubeGrid.WorldVolume.Center) < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    returnPosition = Vector3D.Zero;
                    returnOnce = true;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(hoverPosition)) {
                if (Vector3D.Distance(hoverPosition, remote.CubeGrid.WorldVolume.Center) < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    hoverPosition = Vector3D.Zero;
                }
            }
            if (remote.IsAutoPilotEnabled && !Vector3D.IsZero(landPosition)) {
                foreach (IMyCockpit cockpit in COCKPITS) {
                    if (cockpit.IsUnderControl && !Vector3D.IsZero(cockpit.MoveIndicator)) {
                        remote.ClearWaypoints();
                        remote.SetAutoPilotEnabled(false);
                        landPosition = Vector3D.Zero;
                        break;
                    }
                }
                if (Vector3D.Distance(landPosition, remote.CubeGrid.WorldVolume.Center) < stopDistance) {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                    landPosition = Vector3D.Zero;
                }
            }
        }

        void RangeFinder(IMyRemoteControl remote) {
            targetLog.Clear();
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);//TODO
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                foreach (IMySoundBlock block in ALARMS) { block.Play(); }
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    double planetRadius = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value);
                    string planetName = "Earth";
                    MyTuple<Vector3D, double, double> planet;
                    planetsList.TryGetValue(planetName, out planet);
                    double aRadius = Math.Abs(planet.Item2 - planetRadius);
                    foreach (KeyValuePair<string, MyTuple<Vector3D, double, double>> planetElement in planetsList) {
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
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value - (Vector3D.Normalize(TARGET.HitPosition.Value - lidar.GetPosition()) * atmosphereRange);//normalize?
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, selectedPlanet);
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    targetLog.Append("Radius: ").Append(Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value).ToString("0.0")).Append(", ");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                    targetLog.Append("Ground Dist.: ").Append(Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Atmo. Height: ").Append(Vector3D.Distance(TARGET.HitPosition.Value, safeJumpPosition).ToString("0.0")).Append("\n");
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value - (Vector3D.Normalize(TARGET.HitPosition.Value - lidar.GetPosition()) * friendlySafeDistance);//normalize?
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, "Asteroid");
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    targetLog.Append($"GPS:Asteroid:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value - (Vector3D.Normalize(TARGET.HitPosition.Value - lidar.GetPosition()) * enemySafeDistance);//normalize?
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, TARGET.Name);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    targetLog.Append($"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    targetLog.Append("Distance: ").Append(Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                } else {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value - (Vector3D.Normalize(TARGET.HitPosition.Value - lidar.GetPosition()) * friendlySafeDistance);//normalize?
                    remote.ClearWaypoints();
                    remote.AddWaypoint(safeJumpPosition, TARGET.Name);
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    targetPosition = safeJumpPosition;
                    targetLog.Append($"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    targetLog.Append("Distance: ").Append(Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                }
            } else {
                targetLog.Append("Nothing Detected!\n");
            }
        }

        void Land(IMyRemoteControl remote, Vector3D gravity) {//TODO
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    landPosition = TARGET.HitPosition.Value - gravity * 50d;//normalize gravity?
                    remote.ClearWaypoints();
                    remote.AddWaypoint(landPosition, "landPosition");
                    remote.SetAutoPilotEnabled(true);
                }
            }
        }

        bool AimAtTarget(IMyShipController controller, Vector3D targetPos, double tolerance) {
            bool aligned = false;
            Vector3D aimDirection = targetPos - controller.CubeGrid.WorldVolume.Center;//normalize?
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, controller.WorldMatrix.Up, controller.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, controller.WorldMatrix);

            Debug.PrintHUD($"AimAtTarget, angle:{AngleBetween(controller.WorldMatrix.Forward, aimDirection) * rad2deg:0.00}, angleTolerance:{tolerance:0.00}");

            if (AngleBetween(controller.WorldMatrix.Forward, aimDirection) * rad2deg <= tolerance) {
                aimTarget = false;
                aligned = true;
                UnlockGyros();
            }
            return aligned;
        }

        void SunChase(bool isControlled, Vector3D gravity, bool targFound) {
            if (!isControlled && sunChasing && Vector3D.IsZero(gravity) && !targFound) {
                if (SOLAR.IsFunctional && SOLAR.Enabled && SOLAR.IsWorking) {
                    float power = SOLAR.MaxOutput;
                    if (sunChaseOnce) {
                        LCDSUNCHASER.BackgroundColor = new Color(0, 255, 255);
                        prevSunPower = power;
                        unlockSunChaseOnce = true;
                        sunChaseOnce = false;
                    }
                    double pitch = 0d;
                    double yaw = 0d;
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
                            if (Math.Sign(power - prevSunPower) < 0) {//powerDifference
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
                            if (Math.Sign(power - prevSunPower) < 0) {//powerDifference
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
                    ApplyGyroOverride(pitchController.Control(pitch), yawController.Control(yaw), 0d, GYROS, SOLAR.WorldMatrix);
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
            desiredForwardVector = SafeNormalize(desiredForwardVector);
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
                leftVector = SafeNormalize(leftVector);
                MatrixD targetMatrix = MatrixD.Zero;//Create matrix
                targetMatrix.Forward = desiredForwardVector;
                targetMatrix.Left = leftVector;
                targetMatrix.Up = Vector3D.Cross(desiredForwardVector, leftVector);
                axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
                                    targetMatrix.M31 - targetMatrix.M13,
                                    targetMatrix.M12 - targetMatrix.M21);
                angle = Math.Acos(MathHelper.Clamp((targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33 - 1) * 0.5, -1d, 1d));
            }
            if (Vector3D.IsZero(axis)) {
                angle = desiredForwardVector.Z < 0d ? 0d : Math.PI;
                yaw = angle;
                pitch = 0d;
                roll = 0d;
                return;
            }
            axis = SafeNormalize(axis);
            //Because gyros rotate about -X -Y -Z, we need to negate our angles
            yaw = -axis.Y * angle;
            pitch = -axis.X * angle;
            roll = -axis.Z * angle;
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

        void UnlockGyros() {
            foreach (IMyGyro gyro in GYROS) {
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

        bool IsValidTarget(MyDetectedEntityInfo entityInfo) {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid) {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) {
                    return true;
                }
            }
            return false;
        }

        bool IsNotFriendly(MyRelationsBetweenPlayerAndBlock relationship) {
            return relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != MyRelationsBetweenPlayerAndBlock.Owner;
        }

        float Smallest(float rotorAngle, float b) {
            return Math.Abs(rotorAngle) > Math.Abs(b) ? b : rotorAngle;
        }

        double AngleBetween(Vector3D a, Vector3D b) {//returns radians
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return 0d; } else { return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1)); }
        }

        Vector3D SafeNormalize(Vector3D a) {
            if (Vector3D.IsZero(a)) { return Vector3D.Zero; }
            if (Vector3D.IsUnit(ref a)) { return a; }
            return Vector3D.Normalize(a);
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
                jumpersLog.Append("Max Jump: ").Append(JUMPERS[0].MaxJumpDistanceMeters.ToString("0.0")).Append("\n");
                jumpersLog.Append("Curr. Jump: ").Append((JUMPERS[0].MaxJumpDistanceMeters / 100f * JUMPERS[0].JumpDistanceMeters).ToString("0.0")).Append(" (").Append(JUMPERS[0].JumpDistanceMeters.ToString("0.00")).Append("%)\n");
                double currentStoredPower = 0d;
                double maxStoredPower = 0d;
                StringBuilder timeRemainingBlldr = new StringBuilder();
                foreach (IMyJumpDrive block in JUMPERS) {
                    MyJumpDriveStatus status = block.Status;
                    if (status == MyJumpDriveStatus.Charging) {
                        timeRemainingBlldr.Append(status.ToString()).Append(": ").Append(block.DetailedInfo.ToString().Split('\n')[5]).Append("s, ");//TODO timeRemaining
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
            yawController = new PID(yawAimP, yawAimI, yawAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(pitchAimP, pitchAimI, pitchAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(rollAimP, rollAimI, rollAimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
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

        public class DebugAPI {
            public readonly bool ModDetected;

            public void RemoveDraw() => _removeDraw?.Invoke(_pb);
            Action<IMyProgrammableBlock> _removeDraw;

            public void RemoveAll() => _removeAll?.Invoke(_pb);
            Action<IMyProgrammableBlock> _removeAll;

            public void Remove(int id) => _remove?.Invoke(_pb, id);
            Action<IMyProgrammableBlock, int> _remove;

            public int DrawPoint(Vector3D origin, Color color, float radius = 0.2f, float seconds = DefaultSeconds, bool? onTop = null) => _point?.Invoke(_pb, origin, color, radius, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int> _point;

            public int DrawLine(Vector3D start, Vector3D end, Color color, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _line?.Invoke(_pb, start, end, color, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int> _line;

            public int DrawAABB(BoundingBoxD bb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _aabb?.Invoke(_pb, bb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int> _aabb;

            public int DrawOBB(MyOrientedBoundingBoxD obb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _obb?.Invoke(_pb, obb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int> _obb;

            public int DrawSphere(BoundingSphereD sphere, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, int lineEveryDegrees = 15, float seconds = DefaultSeconds, bool? onTop = null) => _sphere?.Invoke(_pb, sphere, color, (int)style, thickness, lineEveryDegrees, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int> _sphere;

            public int DrawMatrix(MatrixD matrix, float length = 1f, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _matrix?.Invoke(_pb, matrix, length, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
            Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int> _matrix;

            public int DrawGPS(string name, Vector3D origin, Color? color = null, float seconds = DefaultSeconds) => _gps?.Invoke(_pb, name, origin, color, seconds) ?? -1;
            Func<IMyProgrammableBlock, string, Vector3D, Color?, float, int> _gps;

            public int PrintHUD(string message, Font font = Font.Debug, float seconds = 2) => _printHUD?.Invoke(_pb, message, font.ToString(), seconds) ?? -1;
            Func<IMyProgrammableBlock, string, string, float, int> _printHUD;

            public void PrintChat(string message, string sender = null, Color? senderColor = null, Font font = Font.Debug) => _chat?.Invoke(_pb, message, sender, senderColor, font.ToString());
            Action<IMyProgrammableBlock, string, string, Color?, string> _chat;

            public void DeclareAdjustNumber(out int id, double initial, double step = 0.05, Input modifier = Input.Control, string label = null) => id = _adjustNumber?.Invoke(_pb, initial, step, modifier.ToString(), label) ?? -1;
            Func<IMyProgrammableBlock, double, double, string, string, int> _adjustNumber;

            public double GetAdjustNumber(int id, double noModDefault = 1) => _getAdjustNumber?.Invoke(_pb, id) ?? noModDefault;
            Func<IMyProgrammableBlock, int, double> _getAdjustNumber;

            public int GetTick() => _tick?.Invoke() ?? -1;
            Func<int> _tick;

            public enum Style { Solid, Wireframe, SolidAndWireframe }
            public enum Input { MouseLeftButton, MouseRightButton, MouseMiddleButton, MouseExtraButton1, MouseExtraButton2, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Tab, Shift, Control, Alt, Space, PageUp, PageDown, End, Home, Insert, Delete, Left, Up, Right, Down, D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9, Multiply, Add, Separator, Subtract, Decimal, Divide, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }
            public enum Font { Debug, White, Red, Green, Blue, DarkBlue }

            const float DefaultThickness = 0.02f;
            const float DefaultSeconds = -1;

            IMyProgrammableBlock _pb;
            bool _defaultOnTop;

            public DebugAPI(MyGridProgram program, bool drawOnTopDefault = false) {
                if (program == null)
                    throw new Exception("Pass `this` into the API, not null.");

                _defaultOnTop = drawOnTopDefault;
                _pb = program.Me;

                IReadOnlyDictionary<string, Delegate> methods = _pb.GetProperty("DebugAPI")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(_pb);
                if (methods != null) {
                    Assign(out _removeAll, methods["RemoveAll"]);
                    Assign(out _removeDraw, methods["RemoveDraw"]);
                    Assign(out _remove, methods["Remove"]);
                    Assign(out _point, methods["Point"]);
                    Assign(out _line, methods["Line"]);
                    Assign(out _aabb, methods["AABB"]);
                    Assign(out _obb, methods["OBB"]);
                    Assign(out _sphere, methods["Sphere"]);
                    Assign(out _matrix, methods["Matrix"]);
                    Assign(out _gps, methods["GPS"]);
                    Assign(out _printHUD, methods["HUDNotification"]);
                    Assign(out _chat, methods["Chat"]);
                    Assign(out _adjustNumber, methods["DeclareAdjustNumber"]);
                    Assign(out _getAdjustNumber, methods["GetAdjustNumber"]);
                    Assign(out _tick, methods["Tick"]);
                    RemoveAll();
                    ModDetected = true;
                }
            }

            void Assign<T>(out T field, object method) => field = (T)method;
        }

        /*
        
        void CheckCollisions(IMyRemoteControl remote, Vector3D targetPos, Vector3D targetVelocity, bool targFound) {
            if (!targFound) {
                BoundingBoxD gridLocalBB = new BoundingBoxD(remote.CubeGrid.Min * remote.CubeGrid.GridSize, remote.CubeGrid.Max * remote.CubeGrid.GridSize);
                if (isColliding) {
                    gridLocalBB = new BoundingBoxD(gridLocalBB.Center - gridLocalBB.Extents * 2, gridLocalBB.Center + gridLocalBB.Extents * 2);
                }
                MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(gridLocalBB, remote.CubeGrid.WorldMatrix);
                Vector3D targetFuturePosition = targetPos + (targetVelocity * collisionPredictionTime);
                LineD line = new LineD(targetPos, targetFuturePosition);

                Debug.DrawOBB(obb, Color.Blue);
                Debug.DrawLine(targetPos, targetFuturePosition, Color.Red, thickness: 1f, onTop: true);

                double? hitDist = obb.Intersects(ref line);
                if (hitDist.HasValue) {
                    if (returnOnce) {
                        if (Vector3D.IsZero(returnPosition)) {
                            returnPosition = remote.CubeGrid.WorldVolume.Center;
                        }
                        returnOnce = false;
                    }
                    double distance = Vector3D.Distance(remote.CubeGrid.WorldVolume.Center, targetPos);
                    Vector3D enemyDirectionPosition = targetPos + (Vector3D.Normalize(targetVelocity) * distance);
                    Vector3D escapeDirection = Vector3D.Normalize(remote.CubeGrid.WorldVolume.Center - enemyDirectionPosition);//toward my center
                    escapeDirection = Vector3D.TransformNormal(escapeDirection, MatrixD.Transpose(remote.WorldMatrix));
                    
                    Vector3D normalizedVec = Vector3D.Normalize(escapeDirection);
                    Vector3D position = remote.CubeGrid.WorldVolume.Center + (normalizedVec * 1000d);
                    Debug.DrawLine(remote.CubeGrid.WorldVolume.Center, position, Color.LimeGreen, thickness: 1f, onTop: true);

                    collisionDir = Vector3D.Normalize(escapeDirection);// should i normalize it?
                    isColliding = true;
                } else {
                    collisionDir = Vector3D.Zero;
                    isColliding = false;
                }
            } else {
                collisionDir = Vector3D.Zero;
                isColliding = false;
            }
        }
        */

    }
}
