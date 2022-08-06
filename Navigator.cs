﻿using Sandbox.Game.EntityComponents;
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
        //lock landing gear when landing, unlock when taking off
        //land doesn't work
        //thrusters doesn't turn off when idling in gravity
        //when autofighting the ship goes down when in gravity, impact avoidance is useless
        //NAVIGATOR
        readonly bool useRoll = false;//enable/disable roll when gyro stabilizing
        bool magneticDrive = true;//enable/disable magnetic drive
        bool controlDampeners = true;//enable/disable safety dampeners (deadman), valid only if magneticDrive is false
        bool idleThrusters = false;//enable/disable thrusters
        bool useGyrosToStabilize = true;//enable/disable gyro stabilization on planets and when driving
        bool autocombat = true;//enable/disable automatic fighting when ship is not controlled, valid only if magneticDrive is true
        bool impactsAvoidance = true;//enable/disable detection of obstacles while driving, valid only if magneticDrive is true
        bool collisionDetection = true;//enable/disable detection of incoming ram attacks, valid only if magneticDrive is true
        bool enemyEvasion = true;//enable/disable evasion from enemy aim, valid only if magneticDrive is true
        bool keepAltitude = true;//enable/disable keeping altitude on planets, valid only if magneticDrive is true
        bool isModdedSensor = false;//define if is using modded sensors, valid only if impactsAvoidance is true and magneticDrive is true
        bool closeRangeCombat = false;//set the fight distance for the automatic fight, valid only if autocombat is true and magneticDrive is true
        bool sunChasing = false;//enable/disable sun chase on space

        bool configChanged = false;
        bool aimTarget = false;
        bool targFound = false;
        bool assaultCanShoot = true;
        bool artilleryCanShoot = true;
        bool railgunsCanShoot = true;
        bool smallRailgunsCanShoot = true;
        bool checkAllTicks = false;
        bool unlockGyrosOnce = true;
        bool deadManOnce = false;
        bool toggleThrustersOnce = false;
        bool returnOnce = true;
        bool isPilotedOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool initRandomMagneticDriveOnce = true;
        bool initEvasionMagneticDriveOnce = true;
        bool sunChaseOnce = true;
        bool unlockSunChaseOnce = true;
        bool keepAltitudeOnce = true;
        bool sensorDetectionOnce = true;
        bool updateOnce = true;
        string selectedPlanet = "";
        double maxScanRange = 0d;
        double altitudeToKeep = 0d;
        double movePitch = .01;
        double moveYaw = .01;
        float prevSunPower = 0f;
        int planetSelector = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;
        int turretsDetectionDelay = 5;
        int collisionCheckDelay = 10;
        int checkAllTicksCount = 0;
        int turretsDetectionCount = 5;
        int tickCount = 0;
        int randomCount = 50;
        int collisionCheckCount = 0;
        int keepAltitudeCount = 0;
        long targId;

        readonly float targetVel = 29 * (float)(Math.PI / 30);//rpsOverRpm
        readonly float syncSpeed = 1 * (float)(Math.PI / 30);

        const float globalTimestep = 10.0f / 60.0f;
        const float circle = (float)(2 * Math.PI);
        const double rad2deg = 180 / Math.PI;

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
        IMyTextPanel LCDMAGNETICDRIVE;
        IMyTextPanel LCDAUTOCOMBAT;
        IMyTextPanel LCDIMPACTS;
        IMyTextPanel LCDCOLLISIONS;
        IMyTextPanel LCDEVASION;
        IMyTextPanel LCDSTABILIZER;
        IMyTextPanel LCDALTITUDE;
        IMyTextPanel LCDMODDEDSENSOR;
        IMyTextPanel LCDCLOSECOMBAT;
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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[NAVIGATOR]");
            ParseCockpitConfigData(CONTROLLER as IMyCockpit);
            foreach (IMyCameraBlock cam in LIDARS) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSBACK) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSUP) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSDOWN) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSLEFT) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSRIGHT) { cam.EnableRaycast = true; }
            if (LIDARS.Count != 0) { maxScanRange = LIDARS[0].RaycastDistanceLimit; }
            selectedPlanet = planetsList.ElementAt(0).Key;
            InitPIDControllers();
            if (LCDSUNCHASER != null) { LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0); }
            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDDEADMAN != null) { LCDDEADMAN.BackgroundColor = controlDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autocombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDIMPACTS != null) { LCDIMPACTS.BackgroundColor = impactsAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDCOLLISIONS != null) { LCDCOLLISIONS.BackgroundColor = collisionDetection ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDSTABILIZER != null) { LCDSTABILIZER.BackgroundColor = useGyrosToStabilize ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDALTITUDE != null) { LCDALTITUDE.BackgroundColor = keepAltitude ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMODDEDSENSOR != null) { LCDMODDEDSENSOR.BackgroundColor = isModdedSensor ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDCLOSECOMBAT != null) { LCDCLOSECOMBAT.BackgroundColor = closeRangeCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                GetBroadcastMessages();

                Vector3D gravity = CONTROLLER.GetNaturalGravity();
                if (!string.IsNullOrEmpty(arg)) { 
                    ProcessArgument(arg, gravity);
                    if (arg == "RangeFinder") { return; }
                    UpdateConfigParams();
                }
                
                TurretsDetection(targFound);
                if (collisionDetection) { ManageCollisions(targFound); }

                Vector3D myVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;
                bool isTargetEmpty = targetInfo.IsEmpty();
                bool isAutoPiloted = REMOTE.IsAutoPilotEnabled;
                bool needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || isAutoPiloted
                    || !Vector3D.IsZero(gravity) || myVelocity.Length() > 2d || !isTargetEmpty;
                SendBroadcastControllerMessage(needControl);

                if (aimTarget) {
                    bool aligned;
                    AimAtTarget(targetPosition, 0.1d, out aligned);
                    if (!aligned) { return; }
                }

                if (!needControl && sunChasing && Vector3D.IsZero(gravity) && isTargetEmpty) {
                    SunChase();
                    return;
                } else {
                    if (!sunChaseOnce) {
                        UnlockGyros();
                        if (LCDSUNCHASER != null) { LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0); }
                        prevSunPower = 0f;
                        sunChaseOnce = true;
                    }
                }

                bool isUnderControl = IsPiloted(true);
                ManageWaypoints(isUnderControl, targFound, isTargetEmpty);

                ManagePIDControllers(isTargetEmpty, targFound);

                double mySpeed = myVelocity.Length();
                GyroStabilize(targFound, aimTarget, isAutoPiloted, useRoll, gravity, mySpeed, isTargetEmpty);

                ManageMagneticDrive(needControl, isUnderControl, isAutoPiloted, targFound, idleThrusters, keepAltitude, gravity, myVelocity, mySpeed);

                ReadLidarInfos();
                ReadJumpersInfos();

                WriteInfo();

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
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
                case "RangeFinder":
                    if (Vector3D.IsZero(gravity)) {
                        RangeFinder();
                    } else {
                        Land(gravity);
                    }
                    break;
                case "ChangePlanet":
                    planetSelector++;
                    if (planetSelector >= planetsList.Count()) {
                        planetSelector = 0;
                    }
                    selectedPlanet = planetsList.ElementAt(planetSelector).Key;
                    break;
                case "AimTarget": if (!Vector3D.IsZero(targetPosition)) { aimTarget = true; }; break;
                case "SetPlanet":
                    if (!aimTarget) {
                        MyTuple<Vector3D, double, double> planet;
                        planetsList.TryGetValue(selectedPlanet, out planet);
                        double planetSize = planet.Item2 + planet.Item3 + 1000d;
                        Vector3D safeJumpPosition = planet.Item1 - (Vector3D.Normalize(planet.Item1 - REMOTE.CubeGrid.WorldVolume.Center) * planetSize);
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
                case "SunChaserToggle":
                    sunChasing = !sunChasing;
                    break;
                case "ToggleIdleThrusters":
                    idleThrusters = !idleThrusters;
                    if (idleThrusters) {
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(25, 0, 100); }
                    } else {
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }
                    }
                    configChanged = true;
                    break;
                case "ToggleMagneticDrive":
                    magneticDrive = !magneticDrive;
                    if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "DeadMan":
                    controlDampeners = !controlDampeners;
                    if (LCDDEADMAN != null) { LCDDEADMAN.BackgroundColor = controlDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "AutocombatToggle":
                    autocombat = !autocombat;
                    if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autocombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ImpactsAvoidanceToggle":
                    impactsAvoidance = !impactsAvoidance;
                    if (LCDIMPACTS != null) { LCDIMPACTS.BackgroundColor = impactsAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "CollisionDetectionToggle":
                    collisionDetection = !collisionDetection;
                    if (LCDCOLLISIONS != null) { LCDCOLLISIONS.BackgroundColor = collisionDetection ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "EnemyEvasionToggle":
                    enemyEvasion = !enemyEvasion;
                    if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "GyroStabilizeToggle":
                    useGyrosToStabilize = !useGyrosToStabilize;
                    if (LCDSTABILIZER != null) { LCDSTABILIZER.BackgroundColor = useGyrosToStabilize ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "KeepAltitudeToggle":
                    keepAltitude = !keepAltitude;
                    if (LCDALTITUDE != null) { LCDALTITUDE.BackgroundColor = keepAltitude ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ModdedSensorToggle":
                    isModdedSensor = !isModdedSensor;
                    if (LCDMODDEDSENSOR != null) { LCDMODDEDSENSOR.BackgroundColor = isModdedSensor ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "CloseRangeCombatToggle":
                    closeRangeCombat = !closeRangeCombat;
                    if (LCDCLOSECOMBAT != null) { LCDCLOSECOMBAT.BackgroundColor = closeRangeCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
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
            IGC.SendBroadcastMessage("[MANAGER]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void GyroStabilize(bool targetFound, bool aimingTarget, bool isAutoPiloted, bool useRoll, Vector3D gravity, double mySpeed, bool isTargetEmpty) {
            if (useGyrosToStabilize && !targetFound && !aimingTarget && !isAutoPiloted && isTargetEmpty) {
                if (!Vector3D.IsZero(gravity)) {
                    Vector3D horizonVec = Vector3D.Cross(gravity, Vector3D.Cross(CONTROLLER.WorldMatrix.Forward, gravity));//left vector
                    double pitchAngle, rollAngle, yawAngle;
                    GetRotationAnglesSimultaneous(horizonVec, -gravity, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                    double mouseYaw = CONTROLLER.RotationIndicator.Y;
                    double mousePitch = CONTROLLER.RotationIndicator.X;
                    double mouseRoll = CONTROLLER.RollIndicator;
                    if (mousePitch != 0d) {
                        mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                    }
                    mousePitch = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                    if (mouseRoll != 0d) {
                        mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                    }
                    mouseRoll = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
                    yawAngle = 0d;
                    if (mySpeed > 2d) {
                        if (Vector3D.IsZero(lastForwardVector)) {
                            lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                            lastUpVector = CONTROLLER.WorldMatrix.Up;
                        }
                        if (!useRoll) { lastUpVector = Vector3D.Zero; };
                        GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                        lastUpVector = CONTROLLER.WorldMatrix.Up;
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
                        ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, CONTROLLER.WorldMatrix);
                        unlockGyrosOnce = true;
                    }
                } else {
                    if (mySpeed > 2d) {
                        double pitchAngle, yawAngle, rollAngle;
                        if (Vector3D.IsZero(lastForwardVector)) {
                            lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                            lastUpVector = CONTROLLER.WorldMatrix.Up;
                        }
                        if (!useRoll) { lastUpVector = Vector3D.Zero; };
                        GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        double mouseYaw = CONTROLLER.RotationIndicator.Y;
                        double mousePitch = CONTROLLER.RotationIndicator.X;
                        double mouseRoll = CONTROLLER.RollIndicator;
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
                            ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, CONTROLLER.WorldMatrix);
                            unlockGyrosOnce = true;
                        }
                        lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                        lastUpVector = CONTROLLER.WorldMatrix.Up;
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

        void ManageMagneticDrive(bool isControlled, bool isUnderControl, bool isAutoPiloted, bool targetFound, bool idleThrusters, bool keepAltitude, Vector3D gravity, Vector3D myVelocity, double mySpeed) {
            if (magneticDrive && isControlled) {
                Vector3D dir = Vector3D.Zero;
                if (initMagneticDriveOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    sunChasing = false;
                    initMagneticDriveOnce = false;
                }
                SyncRotors();
                double altitude = 60d;
                if (!Vector3D.IsZero(gravity)) {
                    CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                }
                if (!Vector3D.IsZero(collisionDir)) {
                    if (initEvasionMagneticDriveOnce) {
                        CONTROLLER.DampenersOverride = false;
                        initEvasionMagneticDriveOnce = false;
                    }
                    dir = collisionDir;
                } else {
                    if (!initEvasionMagneticDriveOnce) {
                        randomDir = Vector3D.Zero;
                        CONTROLLER.DampenersOverride = true;
                        initEvasionMagneticDriveOnce = true;
                    }
                    if (isAutoPiloted) {
                        dir = AutoMagneticDrive(dir);
                    } else {
                        if (!initAutoMagneticDriveOnce) {
                            foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                            initAutoMagneticDriveOnce = true;
                        }
                        if (autocombat && !isUnderControl && targetFound) {
                            if (initRandomMagneticDriveOnce) {
                                CONTROLLER.DampenersOverride = false;
                                initRandomMagneticDriveOnce = false;
                            }
                            RandomMagneticDrive();
                            dir = randomDir;
                            Vector3D dirNN = Vector3D.Zero;
                            foreach (MyDetectedEntityInfo target in targetsInfo) {
                                Vector3D escapeDir = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - target.Position);
                                escapeDir = Vector3D.TransformNormal(escapeDir, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                                dirNN = SetResultVector(dirNN, escapeDir);
                            }
                            Vector3D dirNew = KeepRightDistance(targPosition);
                            dirNew = MergeDirectionValues(dirNN, dirNew);//TODO do not overwrite
                            dir = MergeDirectionValues(dir, dirNew);
                        } else {
                            if (!initRandomMagneticDriveOnce) {
                                randomDir = Vector3D.Zero;
                                CONTROLLER.DampenersOverride = true;
                                initRandomMagneticDriveOnce = true;
                            }
                            Matrix mtrx;
                            dir = MagneticDrive(out mtrx);
                            dir = MagneticDampeners(dir, myVelocity, gravity, mtrx);
                            Vector3D dirNew = KeepAltitude(isUnderControl, idleThrusters, keepAltitude, gravity, altitude);
                            dir = MergeDirectionValues(dir, dirNew);
                        }
                    }
                }

                if (enemyEvasion) {
                    Vector3D dirN = EvadeEnemy(targOrientation, targVelVec, targPosition, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targetFound);
                    foreach (MyDetectedEntityInfo target in targetsInfo) {
                        Vector3D escapeDir = EvadeEnemy(target.Orientation, target.Velocity, target.Position, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targetFound);
                        dirN = SetResultVector(dirN, escapeDir);
                    }
                    dir = MergeDirectionValues(dir, dirN);
                }

                if (impactsAvoidance && mySpeed > 10f && !isAutoPiloted && (Vector3D.IsZero(gravity) || (!Vector3D.IsZero(gravity) && altitude > 60d))) {
                    if (sensorDetectionOnce) {
                        SetSensorsExtend();
                        sensorDetectionOnce = false;
                    }
                    if (checkAllTicks) {
                        collisionCheckDelay = 1;
                        checkAllTicksCount++;
                        if (checkAllTicksCount == 200) {
                            checkAllTicks = false;
                            checkAllTicksCount = 0;
                        }
                    }
                    UpdateAcceleration(Runtime.TimeSinceLastRun.TotalSeconds, myVelocity);
                    if (collisionCheckCount >= collisionCheckDelay) {
                        double stopDistance = CalculateStopDistance(myVelocity);
                        RaycastStopPosition(stopDistance, myVelocity);

                        if (isModdedSensor) { SetSensorsStopDistance((float)stopDistance); }
                        SensorDetection();

                        collisionCheckCount = 0;
                    }
                    collisionCheckCount++;
                    if (!Vector3D.IsZero(stopDir)) {
                        dir = MergeDirectionValues(dir, stopDir);
                        checkAllTicks = true;
                    }
                    if (!Vector3D.IsZero(sensorDir)) {
                        dir = MergeDirectionValues(dir, sensorDir);
                    }
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

                if (!isAutoPiloted) {
                    IdleThrusters(dir, idleThrusters);
                }

                SetPower(dir);

            } else {
                if (!initMagneticDriveOnce) {
                    IdleMagneticDrive(idleThrusters);
                    initMagneticDriveOnce = true;
                }
                if (tickCount == 50) {
                    if (controlDampeners) {
                        DeadMan(IsPiloted(true), mySpeed);
                    }
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

        Vector3D MagneticDrive(out Matrix mtrx) {
            Vector3D direction = CONTROLLER.MoveIndicator;
            CONTROLLER.Orientation.GetMatrix(out mtrx);
            direction = Vector3D.Transform(direction, mtrx);
            if (!Vector3D.IsZero(direction)) {
                direction = Vector3D.Normalize(direction);//direction /= direction.Length();
            }
            return direction;
        }

        Vector3D MagneticDampeners(Vector3D direction, Vector3D myVelocity, Vector3D gravity, MatrixD mtrx) {
            if (Vector3D.IsZero(gravity) && !CONTROLLER.DampenersOverride && direction.LengthSquared() == 0d) {
                return Vector3D.Zero;
            }
            Vector3D vel = myVelocity;
            vel = Vector3D.Transform(vel, MatrixD.Transpose(CONTROLLER.WorldMatrix.GetOrientation()));
            vel = direction * 105d - Vector3D.Transform(vel, mtrx);
            if (Math.Abs(vel.X) < 2d) { vel.X = 0d; }
            if (Math.Abs(vel.Y) < 2d) { vel.Y = 0d; }
            if (Math.Abs(vel.Z) < 2d) { vel.Z = 0d; }
            return vel;
        }

        void IdleThrusters(Vector3D direction, bool idleThrusters) {
            if (!Vector3D.IsZero(direction)) {
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
            if (randomCount >= 10) {
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
        }

        Vector3D KeepRightDistance(Vector3D targPos) {
            Vector3D dir = Vector3D.Zero;
            double minDistance = 1400d;
            double maxDistance = 2000d;
            if (!railgunsCanShoot && !artilleryCanShoot) {
                minDistance = 800d;
                maxDistance = 1400d;
                if (!assaultCanShoot && !smallRailgunsCanShoot) {
                    if (closeRangeCombat) {
                        minDistance = 500d;
                        maxDistance = 800d;
                    } else {
                        minDistance = 800d;
                        maxDistance = 1400d;
                    }
                }
            }
            Vector3D direction = targPos - CONTROLLER.CubeGrid.WorldVolume.Center;
            double distance = direction.Length();
            direction = Vector3D.Normalize(direction);
            if (distance > maxDistance) {
                dir = Vector3D.TransformNormal(direction, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            } else if (distance < minDistance) {
                dir = -Vector3D.TransformNormal(direction, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            }
            return dir;
        }

        bool TurretsDetection(bool targFound) {
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
            return targetFound;
        }

        void ManageCollisions(bool targFound) {
            if (!targFound) {
                if (!targetInfo.IsEmpty() && targetInfo.HitPosition.HasValue) {
                    Vector3D targetVelocity = targetInfo.Velocity;
                    collisionDir = CheckCollisions(targetInfo.Position, targetVelocity);
                }
            } else {
                collisionDir = Vector3D.Zero;
            }
            foreach (MyDetectedEntityInfo target in targetsInfo) {
                Vector3D escapeDir = CheckCollisions(target.Position, target.Velocity);
                collisionDir = SetResultVector(collisionDir, escapeDir);
            }
        }

        Vector3D CheckCollisions(Vector3D targetPos, Vector3D targetVelocity) {
            if (!Vector3D.IsZero(targetVelocity)) {
                targetVelocity = Vector3D.Normalize(targetVelocity);
                double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, targetPos);
                double angle = AngleBetween(targetVelocity, Vector3D.Normalize(REMOTE.CubeGrid.WorldVolume.Center - targetPos)) * rad2deg;
                if (angle < (9000d / distance)) {
                    Vector3D enemyDirectionPosition = targetPos + (targetVelocity * distance);
                    Vector3D escapeDirection = Vector3D.Normalize(REMOTE.CubeGrid.WorldVolume.Center - enemyDirectionPosition);//toward my center
                    escapeDirection = Vector3D.TransformNormal(escapeDirection, MatrixD.Transpose(REMOTE.WorldMatrix));
                    return escapeDirection;
                } else {
                    return Vector3D.Zero;
                }
            } else {
                return Vector3D.Zero;
            }
        }

        Vector3D EvadeEnemy(MatrixD targOrientation, Vector3D targVel, Vector3D targPos, Vector3D myPosition, Vector3D myVelocity, Vector3D gravity, bool targetFound) {
            if (targetFound) {
                Base6Directions.Direction enemyForward = targOrientation.GetClosestDirection(CONTROLLER.WorldMatrix.Backward);
                Base6Directions.Direction perpendicular = Base6Directions.GetPerpendicular(enemyForward);
                Vector3D enemyForwardVec = targOrientation.GetDirectionVector(enemyForward);
                Vector3D enemyPerpendicularVec = targOrientation.GetDirectionVector(perpendicular);
                targOrientation = MatrixD.CreateFromDir(enemyForwardVec, enemyPerpendicularVec);
                targOrientation.Translation = targPos;
                double distance = Vector3D.Distance(myPosition, targPos);
                Vector3D enemyAim;
                if (distance <= 800d) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, 400f, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, 400f, enemyAim, gravity); }
                } else if (distance <= 1400d) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, 1000f, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, 1000f, enemyAim, gravity); }
                } else if (distance <= 2000d) {
                    enemyAim = ComputeEnemyLeading(targPos, targVel, 2000f, myPosition, myVelocity);
                    if (!Vector3D.IsZero(gravity)) { enemyAim = BulletDrop(distance, 2000f, enemyAim, gravity); }
                } else {
                    return Vector3D.Zero;
                }
                double angle = AngleBetween(enemyForwardVec, enemyAim) * rad2deg;
                if (angle < (4500d / distance)) {
                    Vector3D evadeDirection = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - (targPos + (enemyForwardVec * distance)));//toward my center
                    evadeDirection = Vector3D.TransformNormal(evadeDirection, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                    return evadeDirection;
                }
            }
            return Vector3D.Zero;
        }

        Vector3D ComputeEnemyLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D myPosition, Vector3D myVelocity) {
            Vector3D aimPosition = GetEnemyAim(targetPosition, targetVelocity, myPosition, myVelocity, projectileSpeed);
            return aimPosition - targetPosition;//normalize?
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

        void UpdateAcceleration(double timeStep, Vector3D myVelocity) {
            Vector3D acceleration = (myVelocity - lastVelocity) / timeStep;
            lastVelocity = myVelocity;
            MatrixD worldMatrix = CONTROLLER.WorldMatrix;
            Vector3D localAcceleration = Vector3D.TransformNormal(acceleration, MatrixD.Transpose(worldMatrix));
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

        double CalculateStopDistance(Vector3D myVelocity) {
            Vector3D localVelocity = Vector3D.TransformNormal(myVelocity, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            Vector3D stopDistanceLocal = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {//Now we break the current velocity apart component by component
                double velocityComponent = localVelocity.GetDim(i);
                double stopDistComponent = velocityComponent >= 0d
                    ? (velocityComponent * velocityComponent) / (2d * minAccel.GetDim(i))
                    : (velocityComponent * velocityComponent) / (2d * maxAccel.GetDim(i));
                stopDistanceLocal.SetDim(i, stopDistComponent);
            }
            return stopDistanceLocal.Length();//Stop distance is just the magnitude of our result vector now
        }

        void RaycastStopPosition(double stopDistance, Vector3D myVelocity) {
            Vector3D normalizedVelocity = Vector3D.Normalize(myVelocity);
            Base6Directions.Direction direction = Base6Directions.GetClosestDirection(Vector3D.TransformNormal(normalizedVelocity, MatrixD.Transpose(CONTROLLER.WorldMatrix)));//relative
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
            stopDistance += 1000d;
            MyDetectedEntityInfo entityInfo = lidar.Raycast(CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance));
            stopDir = Vector3D.Zero;
            if (!entityInfo.IsEmpty()) {
                //dir = -normalizedVelocity;
                if (CONTROLLER.WorldMatrix.Forward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = 1f;//go backward
                } else if (CONTROLLER.WorldMatrix.Backward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = -1f;
                }
                if (CONTROLLER.WorldMatrix.Up.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = -1f;//go down
                } else if (CONTROLLER.WorldMatrix.Down.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = 1f;
                }
                if (CONTROLLER.WorldMatrix.Left.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = 1f;//go right
                } else if (CONTROLLER.WorldMatrix.Right.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = -1f;
                }
            }
        }

        Vector3D KeepAltitude(bool isUnderControl, bool idleThrusters, bool keepAltitude, Vector3D gravity, double altitude) {
            Vector3D dir = Vector3D.Zero;
            if (!isUnderControl && !Vector3D.IsZero(gravity) && idleThrusters && keepAltitude) {
                if (keepAltitudeOnce) {
                    hoverPosition = CONTROLLER.CubeGrid.WorldVolume.Center;
                    keepAltitudeOnce = false;
                }
                if (altitude > 60d) {
                    if (keepAltitudeCount >= 50) {
                        if (Vector3D.Distance(hoverPosition, CONTROLLER.CubeGrid.WorldVolume.Center) > 300d) {
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
                        dir = -Vector3D.TransformNormal(Vector3D.Normalize(gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));
                    } else if (altitude > altitudeToKeep + 30d) {
                        dir = Vector3D.TransformNormal(Vector3D.Normalize(gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));
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

        Vector3D MergeDirectionValues(Vector3D dirToKeep, Vector3D dirNew) {
            dirToKeep.X = dirNew.X != 0d ? dirNew.X : dirToKeep.X;
            dirToKeep.Y = dirNew.Y != 0d ? dirNew.Y : dirToKeep.Y;
            dirToKeep.Z = dirNew.Z != 0d ? dirNew.Z : dirToKeep.Z;
            return dirToKeep;
        }

        Vector3D SetResultVector(Vector3D direction, Vector3D otherDirection) {
            if (!Vector3D.IsZero(otherDirection)) {
                if (!Vector3D.IsZero(direction)) {
                    direction = (direction + otherDirection) / 2;
                } else {
                    direction = otherDirection;
                }
            }
            return direction;
        }

        void DeadMan(bool isUnderControl, double mySpeed) {
            if (!isUnderControl) {
                IMyShipController cntrllr = null;
                if (CONTROLLER.CanControlShip) {
                    cntrllr = CONTROLLER;
                } else if (REMOTE.CanControlShip) {
                    cntrllr = REMOTE;
                }
                if (cntrllr != null) {
                    if (mySpeed > 0.1d) {
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

        void ManageWaypoints(bool isUnderControl, bool targFound, bool isTargetEmpty) {
            if (!isUnderControl) {
                isPilotedOnce = true;
            } else {
                if (isPilotedOnce) {
                    REMOTE.ClearWaypoints();
                    returnPosition = Vector3D.Zero;
                    hoverPosition = Vector3D.Zero;
                    landPosition = Vector3D.Zero;
                    isPilotedOnce = false;
                }
            }
            if (targFound || !isTargetEmpty) {
                if (REMOTE.IsAutoPilotEnabled) {
                    REMOTE.SetAutoPilotEnabled(false);
                }
            } else {
                if (returnOnce && Vector3D.IsZero(returnPosition) && !isUnderControl) {
                    returnPosition = REMOTE.CubeGrid.WorldVolume.Center;
                    returnOnce = false;
                }
                if (!Vector3D.IsZero(returnPosition)) {
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(returnPosition, "returnPosition");
                    REMOTE.SetAutoPilotEnabled(true);
                    returnOnce = true;
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition)) {
                    if (Vector3D.Distance(returnPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        returnPosition = Vector3D.Zero;
                        returnOnce = true;
                    }
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(hoverPosition)) {
                    if (Vector3D.Distance(hoverPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        hoverPosition = Vector3D.Zero;
                    }
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(landPosition)) {
                    if (CONTROLLER.IsUnderControl && !Vector3D.IsZero(CONTROLLER.MoveIndicator)) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        //landPosition = Vector3D.Zero;
                    }
                    if (Vector3D.Distance(landPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        landPosition = Vector3D.Zero;
                    }
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(targetPosition)) {
                    if (Vector3D.Distance(targetPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.SetAutoPilotEnabled(false);
                        targetPosition = Vector3D.Zero;
                    }
                }
            }
        }

        void RangeFinder() {
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
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * atmosphereRange);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, selectedPlanet);
                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    targetLog.Append("Radius: ").Append(Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value).ToString("0.0")).Append(", ");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                    targetLog.Append("Ground Dist.: ").Append(Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Atmo. Height: ").Append(Vector3D.Distance(TARGET.HitPosition.Value, safeJumpPosition).ToString("0.0")).Append("\n");
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Asteroid");
                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    targetLog.Append($"GPS:Asteroid:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Distance: ").Append(distance.ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 3000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    targetPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    targetLog.Append($"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    targetLog.Append("Distance: ").Append(Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                } else {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    targetPosition = safeJumpPosition;
                    targetLog.Append($"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}\n");
                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");
                    targetLog.Append("Distance: ").Append(Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value).ToString("0.0")).Append("\n");
                    targetLog.Append("Diameter: ").Append(Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max).ToString("0.0")).Append("\n");
                }
            } else {
                targetLog.Append("Nothing Detected!\n");
            }
        }

        void Land(Vector3D gravity) {
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);//TODO
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    landPosition = TARGET.HitPosition.Value - Vector3D.Normalize(-gravity) * 50d;
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(landPosition, "landPosition");
                    REMOTE.SetAutoPilotEnabled(true);
                }
            }
        }

        void AimAtTarget(Vector3D targetPos, double tolerance, out bool aligned) {
            aligned = false;
            Vector3D aimDirection = targetPos - CONTROLLER.CubeGrid.WorldVolume.Center;
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
            if (AngleBetween(CONTROLLER.WorldMatrix.Forward, Vector3D.Normalize(aimDirection)) * rad2deg <= tolerance) {
                aimTarget = false;
                aligned = true;
                UnlockGyros();
            }
        }

        void SunChase() {
            if (SOLAR.IsFunctional && SOLAR.Enabled && SOLAR.IsWorking) {
                float power = SOLAR.MaxOutput;
                if (sunChaseOnce) {
                    if (LCDSUNCHASER != null) { LCDSUNCHASER.BackgroundColor = new Color(25, 0, 100); }
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

        bool IsPiloted(bool remotePiloted) {
            bool isPiloted = false;
            if (CONTROLLER.IsUnderControl) {
                isPiloted = true;
            }
            if (remotePiloted) {
                if (REMOTE.IsUnderControl) {
                    isPiloted = true;
                }
            }
            return isPiloted;
        }

        void SetSensorsExtend() {
            if (UPSENSOR != null) { UPSENSOR.LeftExtend = 30f; UPSENSOR.RightExtend = 30f; UPSENSOR.BottomExtend = 28.5f; UPSENSOR.TopExtend = 40f; BACKWARDSENSOR.BackExtend = 0.1f; UPSENSOR.FrontExtend = 50f; }
            if (DOWNSENSOR != null) { DOWNSENSOR.LeftExtend = 30f; DOWNSENSOR.RightExtend = 30f; DOWNSENSOR.BottomExtend = 38.5f; DOWNSENSOR.TopExtend = 30f; DOWNSENSOR.BackExtend = 5f; DOWNSENSOR.FrontExtend = 50f; }
            if (FORWARDSENSOR != null) { FORWARDSENSOR.LeftExtend = 30f; FORWARDSENSOR.RightExtend = 30f; FORWARDSENSOR.BottomExtend = 12.5f; FORWARDSENSOR.TopExtend = 8.5f; FORWARDSENSOR.BackExtend = 50f; FORWARDSENSOR.FrontExtend = 0.1f; }
            if (BACKWARDSENSOR != null) { BACKWARDSENSOR.LeftExtend = 30f; BACKWARDSENSOR.RightExtend = 30f; BACKWARDSENSOR.BottomExtend = 15f; BACKWARDSENSOR.TopExtend = 6f; BACKWARDSENSOR.BackExtend = 0.1f; BACKWARDSENSOR.FrontExtend = 50f; }
            if (LEFTSENSOR != null) { LEFTSENSOR.LeftExtend = 33f; LEFTSENSOR.RightExtend = 36f; LEFTSENSOR.BottomExtend = 7.5f; LEFTSENSOR.TopExtend = 13.5f; LEFTSENSOR.BackExtend = 0.1f; LEFTSENSOR.FrontExtend = 50f; }
            if (RIGHTSENSOR != null) { RIGHTSENSOR.LeftExtend = 36f; RIGHTSENSOR.RightExtend = 33f; RIGHTSENSOR.BottomExtend = 7.5f; RIGHTSENSOR.TopExtend = 13.5f; RIGHTSENSOR.BackExtend = 0.1f; RIGHTSENSOR.FrontExtend = 50f; }
        }

        void SetSensorsStopDistance(float stopDistance) {
            stopDistance += 250f;
            if (UPSENSOR != null && stopDistance < UPSENSOR.MaxRange) {
                if (UPSENSOR != null) { UPSENSOR.FrontExtend = stopDistance; }
                if (DOWNSENSOR != null) { DOWNSENSOR.FrontExtend = stopDistance; }
                if (FORWARDSENSOR != null) { FORWARDSENSOR.BackExtend = stopDistance; }
                if (BACKWARDSENSOR != null) { BACKWARDSENSOR.FrontExtend = stopDistance; }
                if (LEFTSENSOR != null) { LEFTSENSOR.FrontExtend = stopDistance; }
                if (RIGHTSENSOR != null) { RIGHTSENSOR.FrontExtend = stopDistance; }
            } else {
                if (UPSENSOR != null) { UPSENSOR.FrontExtend = UPSENSOR.MaxRange; }
                if (DOWNSENSOR != null) { DOWNSENSOR.FrontExtend = DOWNSENSOR.MaxRange; }
                if (FORWARDSENSOR != null) { FORWARDSENSOR.BackExtend = FORWARDSENSOR.MaxRange; }
                if (BACKWARDSENSOR != null) { BACKWARDSENSOR.FrontExtend = BACKWARDSENSOR.MaxRange; }
                if (LEFTSENSOR != null) { LEFTSENSOR.FrontExtend = LEFTSENSOR.MaxRange; }
                if (RIGHTSENSOR != null) { RIGHTSENSOR.FrontExtend = RIGHTSENSOR.MaxRange; }
            }
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
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, "RangeFinderSettings", out result);
            if (!string.IsNullOrEmpty(myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToString())) {
                int cockpitRangeFinderSurface = myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToInt32();
                SURFACES.Add(cockpit.GetSurface(cockpitRangeFinderSurface));//4
            }
        }

        void UpdateConfigParams() {
            if (configChanged) {
                if (!magneticDrive) {
                    autocombat = false;
                    impactsAvoidance = false;
                    collisionDetection = false;
                    enemyEvasion = false;
                    idleThrusters = false;
                    controlDampeners = true;
                }
                if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDDEADMAN != null) { LCDDEADMAN.BackgroundColor = controlDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autocombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDIMPACTS != null) { LCDIMPACTS.BackgroundColor = impactsAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDCOLLISIONS != null) { LCDCOLLISIONS.BackgroundColor = collisionDetection ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                configChanged = false;
            }
        }

        void GetBlocks() {
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains("[CRX] Gyro"));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains("[CRX] HThruster"));
            UPTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("UP")));
            DOWNTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("DOWN")));
            LEFTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("LEFT")));
            RIGHTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("RIGHT")));
            FORWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("FORWARD")));
            BACKWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains("BACKWARD")));
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, block => block.CustomName.Contains("[CRX] Camera Lidar"));
            LIDARSBACK.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSBACK, block => block.CustomName.Contains("[CRX] Camera Back"));
            LIDARSUP.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSUP, block => block.CustomName.Contains("[CRX] Camera Up"));
            LIDARSDOWN.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSDOWN, block => block.CustomName.Contains("[CRX] Camera Down"));
            LIDARSLEFT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSLEFT, block => block.CustomName.Contains("[CRX] Camera Left"));
            LIDARSRIGHT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARSRIGHT, block => block.CustomName.Contains("[CRX] Camera Right"));
            JUMPERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(JUMPERS, block => block.CustomName.Contains("[CRX] Jump"));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains("[CRX] Turret"));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, block => block.CustomName.Contains("[CRX] Alarm Lidar"));
            ROTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORS, block => block.CustomName.Contains("Rotor_MD_A"));
            ROTORSINV.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORSINV, block => block.CustomName.Contains("Rotor_MD_B"));
            MERGESPLUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSX, block => block.CustomName.Contains("Merge_MD-X"));//TODO
            MERGESPLUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSY, block => block.CustomName.Contains("Merge_MD+Z"));//TODO
            MERGESPLUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSZ, block => block.CustomName.Contains("Merge_MD+Y"));//TODO
            MERGESMINUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSX, block => block.CustomName.Contains("Merge_MD+X"));//TODO
            MERGESMINUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSY, block => block.CustomName.Contains("Merge_MD-Z"));//TODO
            MERGESMINUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSZ, block => block.CustomName.Contains("Merge_MD-Y"));//TODO
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD RangeFinder"));
            foreach (IMyTextPanel panel in panels) { SURFACES.Add(panel as IMyTextSurface); }
            LCDDEADMAN = GridTerminalSystem.GetBlockWithName("[CRX] LCD DeadMan Toggle") as IMyTextPanel;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName("[CRX] LCD IdleThrusters Toggle") as IMyTextPanel;
            LCDSUNCHASER = GridTerminalSystem.GetBlockWithName("[CRX] LCD SunChaser Toggle") as IMyTextPanel;
            LCDMAGNETICDRIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD MagneticDrive Toggle") as IMyTextPanel;
            LCDAUTOCOMBAT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Autocombat Toggle") as IMyTextPanel;
            LCDIMPACTS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Impacts Toggle") as IMyTextPanel;
            LCDCOLLISIONS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Collisions Toggle") as IMyTextPanel;
            LCDEVASION = GridTerminalSystem.GetBlockWithName("[CRX] LCD Evasion Toggle") as IMyTextPanel;
            LCDSTABILIZER = GridTerminalSystem.GetBlockWithName("[CRX] LCD Stabilizer Toggle") as IMyTextPanel;
            LCDALTITUDE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Keep Altitude Toggle") as IMyTextPanel;
            LCDMODDEDSENSOR = GridTerminalSystem.GetBlockWithName("[CRX] LCD Modded Sensor Toggle") as IMyTextPanel;
            LCDCLOSECOMBAT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Close Combat Toggle") as IMyTextPanel;
            SENSORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(SENSORS, block => block.CustomName.Contains("[CRX] Sensor"));
            foreach (IMySensorBlock sensor in SENSORS) {
                if (sensor.CustomName.Contains("UP")) { UPSENSOR = sensor; } else if (sensor.CustomName.Contains("DOWN")) { DOWNSENSOR = sensor; } else if (sensor.CustomName.Contains("LEFT")) { LEFTSENSOR = sensor; } else if (sensor.CustomName.Contains("RIGHT")) { RIGHTSENSOR = sensor; } else if (sensor.CustomName.Contains("FORWARD")) { FORWARDSENSOR = sensor; } else if (sensor.CustomName.Contains("BACKWARD")) { BACKWARDSENSOR = sensor; }
            }
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains("[CRX] Solar"));
            foreach (IMySolarPanel solar in SOLARS) { if (solar.IsFunctional && solar.Enabled && solar.IsWorking) { SOLAR = solar; } }
            REMOTE = GridTerminalSystem.GetBlockWithName("[CRX] Controller Remote") as IMyRemoteControl;
            CONTROLLER = GridTerminalSystem.GetBlockWithName("[CRX] Controller Cockpit") as IMyShipController;
        }

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, 0, -0, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, 0, -0, globalTimestep);
            rollController = new PID(1d, 0d, 1d, 0, -0, globalTimestep);
        }

        void ManagePIDControllers(bool isTargetEmpty, bool targetFound) {
            if (!isTargetEmpty || targetFound) {
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
            public double kP = 0d;
            public double kI = 0d;
            public double kD = 0d;
            public double integralDecayRatio = 0d;
            public double lowerBound = 0d;
            public double upperBound = 0d;
            double timeStep = 0d;
            double inverseTimeStep = 0d;
            double errorSum = 0d;
            double lastError = 0d;
            bool firstRun = true;
            public bool integralDecay = false;
            public double Value { get; private set; }

            public PID(double _kP, double _kI, double _kD, double _lowerBound, double _upperBound, double _timeStep) {
                kP = _kP;
                kI = _kI;
                kD = _kD;
                lowerBound = _lowerBound;
                upperBound = _upperBound;
                timeStep = _timeStep;
                inverseTimeStep = 1d / timeStep;
                integralDecay = false;
            }

            public PID(double _kP, double _kI, double _kD, double _integralDecayRatio, double _timeStep) {
                kP = _kP;
                kI = _kI;
                kD = _kD;
                timeStep = _timeStep;
                inverseTimeStep = 1d / timeStep;
                integralDecayRatio = _integralDecayRatio;
                integralDecay = true;
            }

            public void Update(double _kP, double _kI, double _kD) {
                kP = _kP;
                kI = _kI;
                kD = _kD;
                firstRun = true;
            }

            public double Control(double _error) {
                double errorDerivative = (_error - lastError) * inverseTimeStep;//Compute derivative term
                if (firstRun) {
                    errorDerivative = 0d;
                    firstRun = false;
                }
                if (!integralDecay) {//Compute integral term
                    errorSum += _error * timeStep;
                    if (errorSum > upperBound) {//Clamp integral term
                        errorSum = upperBound;
                    } else if (errorSum < lowerBound) {
                        errorSum = lowerBound;
                    }
                } else {
                    errorSum = errorSum * (1.0 - integralDecayRatio) + _error * timeStep;
                }
                lastError = _error;//Store this error as last error
                this.Value = kP * _error + kI * errorSum + kD * errorDerivative;//Construct output
                return this.Value;
            }

            public double Control(double _error, double _timeStep) {
                timeStep = _timeStep;
                inverseTimeStep = 1d / timeStep;
                return Control(_error);
            }

            public void Reset() {
                errorSum = 0d;
                lastError = 0d;
                firstRun = true;
            }
        }

    }
}
