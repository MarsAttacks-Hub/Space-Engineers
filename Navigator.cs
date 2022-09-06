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
        //acceleration is calculated equally for all direction (max)
        //NAVIGATOR
        DebugAPI Debug;

        bool magneticDrive = true;//enable/disable magnetic drive
        bool idleThrusters = false;//enable/disable thrusters
        bool safetyDampeners = true;//enable/disable safety dampeners
        bool useGyrosToStabilize = true;//enable/disable gyro stabilization on planets and when driving
        bool autoCombat = true;//enable/disable automatic fighting when ship is not controlled, valid only if magneticDrive is true
        bool obstaclesAvoidance = true;//enable/disable detection of obstacles while driving, valid only if magneticDrive is true
        bool collisionDetection = true;//enable/disable detection of incoming ram attacks, valid only if magneticDrive is true
        bool enemyEvasion = true;//enable/disable evasion from enemy aim, valid only if magneticDrive is true
        bool keepAltitude = true;//enable/disable keeping altitude on planets, valid only if magneticDrive is true
        bool moddedSensor = false;//define if is using modded sensors, valid only if obstaclesAvoidance is true and magneticDrive is true
        bool closeRangeCombat = false;//set the fight distance for the automatic fight, valid only if autoCombat is true and magneticDrive is true
        bool sunAlign = false;//enable/disable sun chase on space
        bool logger = true;//enable/disable logging

        bool configChanged = false;
        bool aimTarget = false;
        bool targFound = false;
        bool assaultCanShoot = true;
        bool artilleryCanShoot = true;
        bool railgunsCanShoot = true;
        bool smallRailgunsCanShoot = true;
        bool checkAllTicks = false;
        bool descend = false;
        bool unlockGyrosOnce = true;
        bool returnOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool initRandomMagneticDriveOnce = true;
        bool initlandMagneticDriveOnce = true;
        bool initCollisionMagneticDriveOnce = true;
        bool sunAlignOnce = true;
        bool unlockSunAlignOnce = true;
        bool keepAltitudeOnce = true;
        bool sensorDetectionOnce = true;
        bool updateOnce = true;
        bool controlThrustersOnce = false;
        bool safetyDampenersOnce = false;
        string selectedPlanet = "";
        string rangeFinderName = "";
        double altitudeToKeep = 0d;
        double movePitch = .01;
        double moveYaw = .01;
        double rangeFinderDiameter = 0d;
        double rangeFinderDistance = 0d;
        float prevSunPower = 0f;
        int planetSelector = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;
        int turretsDetectionDelay = 5;
        int collisionCheckDelay = 10;
        int checkAllTicksCount = 0;
        int turretsDetectionCount = 5;
        int randomCount = 50;
        int collisionCheckCount = 0;
        int keepAltitudeCount = 0;
        int sendMessageCount = 0;
        int keepRightAltitudeCount = 0;
        int manageDampenersCount = 0;
        int manageThrustersCount = 0;
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
        IMyTextPanel LCDSAFETYDAMPENERS;
        IMyTextPanel LCDSUNALIGN;
        IMyTextPanel LCDMAGNETICDRIVE;
        IMyTextPanel LCDAUTOCOMBAT;
        IMyTextPanel LCDOBSTACLES;
        IMyTextPanel LCDCOLLISIONS;
        IMyTextPanel LCDEVASION;
        IMyTextPanel LCDSTABILIZER;
        IMyTextPanel LCDALTITUDE;
        IMyTextPanel LCDMODDEDSENSOR;
        IMyTextPanel LCDCLOSECOMBAT;
        IMyTextPanel LCDIDLETHRUSTERS;
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
        Matrix mtrx;
        MatrixD targOrientation = new MatrixD();
        Vector3D rangeFinderPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D hoverPosition = Vector3D.Zero;
        Vector3D landPosition = Vector3D.Zero;
        Vector3D lastForwardVector = Vector3D.Zero;
        Vector3D lastUpVector = Vector3D.Zero;
        Vector3D targPosition = Vector3D.Zero;
        Vector3D targVelVec = Vector3D.Zero;
        Vector3D lastVelocity = Vector3D.Zero;
        Vector3D maxAccel = Vector3D.Zero;
        Vector3D minAccel = Vector3D.Zero;
        Vector3D randomDir = Vector3D.Zero;
        Vector3D sensorDir = Vector3D.Zero;
        Vector3D collisionDir = Vector3D.Zero;
        Vector3D stopDir = Vector3D.Zero;

        MyThrustList myThrustList;

        PID yawController;
        PID pitchController;
        PID rollController;

        readonly Random random = new Random();

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

            Debug = new DebugAPI(this);
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[NAVIGATOR]");
            foreach (IMyCameraBlock cam in LIDARS) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSBACK) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSUP) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSDOWN) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSLEFT) { cam.EnableRaycast = true; }
            foreach (IMyCameraBlock cam in LIDARSRIGHT) { cam.EnableRaycast = true; }
            selectedPlanet = planetsList.ElementAt(0).Key;
            InitPIDControllers();
            CONTROLLER.Orientation.GetMatrix(out mtrx);
            if (LCDSUNALIGN != null) { LCDSUNALIGN.BackgroundColor = new Color(0, 0, 0); }
            if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = safetyDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autoCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDOBSTACLES != null) { LCDOBSTACLES.BackgroundColor = obstaclesAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDCOLLISIONS != null) { LCDCOLLISIONS.BackgroundColor = collisionDetection ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDSTABILIZER != null) { LCDSTABILIZER.BackgroundColor = useGyrosToStabilize ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDALTITUDE != null) { LCDALTITUDE.BackgroundColor = keepAltitude ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMODDEDSENSOR != null) { LCDMODDEDSENSOR.BackgroundColor = moddedSensor ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDCLOSECOMBAT != null) { LCDCLOSECOMBAT.BackgroundColor = closeRangeCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            List<MyThrustBlock> thrusterList = new List<MyThrustBlock>();
            foreach (IMyThrust block in THRUSTERS) {//TODO
                MatrixD transposed = MatrixD.Transpose(CONTROLLER.WorldMatrix);
                Vector3D thrustForward = Vector3D.Rotate(block.WorldMatrix.Forward, transposed);
                Vector3D thrustUp = Vector3D.Rotate(block.WorldMatrix.Up, transposed);
                //Vector3D thrustForward = Vector3D.TransformNormal(block.WorldMatrix.Forward, transposed);
                //Vector3D thrustUp = Vector3D.TransformNormal(block.WorldMatrix.Up, transposed);
                MatrixD thrustLocalMatrix = MatrixD.CreateFromDir(thrustForward, thrustUp);
                thrusterList.Add(new MyThrustBlock(thrustLocalMatrix, block));//TODO or just Matrix.Transpose(CONTROLLER.WorldMatrix)?
            }
            myThrustList = new MyThrustList(MatrixD.Transpose(CONTROLLER.WorldMatrix), thrusterList);
            thrusterList.Clear();
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                Debug.RemoveDraw();

                GetBroadcastMessages();

                Vector3D gravity = CONTROLLER.GetNaturalGravity();
                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg, gravity);
                    if (arg == "RangeFinder") { return; }
                    UpdateConfigParams();
                }

                TurretsDetection();
                if (collisionDetection) { ManageCollisions(); }

                Vector3D myVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;
                double mySpeed = myVelocity.Length();

                bool isTargetEmpty = targetInfo.IsEmpty();
                bool isAutoPiloted = REMOTE.IsAutoPilotEnabled;
                bool isUnderControl = IsPiloted(true);

                bool needControl;
                if (magneticDrive) {
                    needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || isAutoPiloted || mySpeed > 2d || !isTargetEmpty;
                } else {
                    needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || !isTargetEmpty;
                }

                SendBroadcastControllerMessage(needControl);//TODO set a delay?

                if (aimTarget) {
                    bool aligned;
                    AimAtTarget(rangeFinderPosition, 0.1d, out aligned);
                    if (!aligned) { return; }
                }

                if (!needControl && sunAlign && Vector3D.IsZero(gravity) && isTargetEmpty) {
                    SunChase();
                    return;
                } else {
                    if (!sunAlignOnce) {
                        UnlockGyros();
                        if (LCDSUNALIGN != null) { LCDSUNALIGN.BackgroundColor = new Color(0, 0, 0); }
                        prevSunPower = 0f;
                        sunAlignOnce = true;
                    }
                }

                ManageWaypoints(isUnderControl, isTargetEmpty);

                ManagePIDControllers(isTargetEmpty);//TODO make it dinamic based on angles

                GyroStabilize(isAutoPiloted, gravity, mySpeed, isTargetEmpty);//TODO do GyroStabilize when landing

                ManageThrusters(mySpeed, isAutoPiloted);

                if (magneticDrive) {
                    initMagneticDriveOnce = false;
                    if (descend) {
                        MagneticDescend(myVelocity, gravity);
                    } else {
                        if (needControl) {
                            ManageMagneticDrive(isUnderControl, isAutoPiloted, gravity, myVelocity, mySpeed);
                        }
                    }
                } else {
                    if (!initMagneticDriveOnce) {
                        IdleMagneticDrive();
                        initMagneticDriveOnce = true;
                    }
                    if (descend) {
                        Descend(myVelocity, gravity);
                    } else {
                        if (needControl) {
                            ManageDrive(isUnderControl, isAutoPiloted, gravity, myVelocity, mySpeed);
                        }
                    }
                }
                //TODO applyGyroOverride to face position?

                ManageDampeners(mySpeed, isUnderControl, isAutoPiloted);

                SendBroadcastLogMessage();

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument, Vector3D gravity) {
            switch (argument) {
                case "RangeFinder":
                    if (Vector3D.IsZero(gravity)) {
                        RangeFinder();
                    } else {
                        if (!Vector3D.IsZero(landPosition)) {
                            landPosition = Vector3D.Zero;
                            break;
                        }
                        Land(gravity);
                    }
                    sendMessageCount = 10;
                    break;
                case "ChangePlanet":
                    planetSelector++;
                    if (planetSelector >= planetsList.Count()) {
                        planetSelector = 0;
                    }
                    selectedPlanet = planetsList.ElementAt(planetSelector).Key;
                    break;
                case "AimTarget": if (!Vector3D.IsZero(rangeFinderPosition)) { aimTarget = true; }; break;
                case "SetPlanet":
                    if (!aimTarget) {
                        MyTuple<Vector3D, double, double> planet;
                        planetsList.TryGetValue(selectedPlanet, out planet);
                        double planetSize = planet.Item2 + planet.Item3 + 1000d;
                        Vector3D safeJumpPosition = planet.Item1 - (Vector3D.Normalize(planet.Item1 - REMOTE.CubeGrid.WorldVolume.Center) * planetSize);
                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(safeJumpPosition, selectedPlanet);
                        double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                        rangeFinderPosition = safeJumpPosition;
                        if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                        rangeFinderName = selectedPlanet;
                        rangeFinderDiameter = planet.Item2 * 2d;
                        rangeFinderDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, planet.Item1);
                        sendMessageCount = 10;
                    }
                    break;
                case "ToggleSunAlign":
                    sunAlign = !sunAlign;
                    break;
                case "ToggleMagneticDrive":
                    magneticDrive = !magneticDrive;
                    if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleIdleThrusters":
                    idleThrusters = !idleThrusters;
                    if (idleThrusters) {
                        //REMOTE.ControlThrusters = false;
                        //CONTROLLER.ControlThrusters = false;
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(25, 0, 100); }
                    } else {
                        //REMOTE.ControlThrusters = true;
                        //CONTROLLER.ControlThrusters = true;
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }
                    }
                    break;
                case "ToggleSafetyDampeners":
                    safetyDampeners = !safetyDampeners;
                    if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = safetyDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleAutoCombat":
                    autoCombat = !autoCombat;
                    if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autoCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleObstaclesAvoidance":
                    obstaclesAvoidance = !obstaclesAvoidance;
                    if (LCDOBSTACLES != null) { LCDOBSTACLES.BackgroundColor = obstaclesAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleCollisionDetection":
                    collisionDetection = !collisionDetection;
                    if (LCDCOLLISIONS != null) { LCDCOLLISIONS.BackgroundColor = collisionDetection ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleEnemyEvasion":
                    enemyEvasion = !enemyEvasion;
                    if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    configChanged = true;
                    break;
                case "ToggleGyroStabilize":
                    useGyrosToStabilize = !useGyrosToStabilize;
                    if (LCDSTABILIZER != null) { LCDSTABILIZER.BackgroundColor = useGyrosToStabilize ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ToggleKeepAltitude":
                    keepAltitude = !keepAltitude;
                    if (LCDALTITUDE != null) { LCDALTITUDE.BackgroundColor = keepAltitude ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ToggleModdedSensor":
                    moddedSensor = !moddedSensor;
                    if (LCDMODDEDSENSOR != null) { LCDMODDEDSENSOR.BackgroundColor = moddedSensor ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ToggleCloseCombat":
                    closeRangeCombat = !closeRangeCombat;
                    if (LCDCLOSECOMBAT != null) { LCDCLOSECOMBAT.BackgroundColor = closeRangeCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
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
            IGC.SendBroadcastMessage("[POWERMANAGER]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastLogMessage() {
            if (logger) {
                if (sendMessageCount >= 10) {
                    string timeRemaining = "";
                    int maxJump = 0;
                    int currentJump = 0;
                    double totJumpPercent = 0d;
                    double currentStoredPower = 0d;
                    double maxStoredPower = 0d;
                    if (JUMPERS.Count != 0) {
                        maxJump = (int)JUMPERS[0].MaxJumpDistanceMeters;
                        currentJump = (int)(JUMPERS[0].MaxJumpDistanceMeters / 100f * JUMPERS[0].JumpDistanceMeters);
                        foreach (IMyJumpDrive block in JUMPERS) {
                            MyJumpDriveStatus status = block.Status;
                            if (status == MyJumpDriveStatus.Charging) {
                                timeRemaining = block.DetailedInfo.ToString().Split('\n')[5];
                            }
                            currentStoredPower += block.CurrentStoredPower;
                            maxStoredPower += block.MaxStoredPower;
                        }
                        if (maxStoredPower > 0d) {
                            totJumpPercent = currentStoredPower / maxStoredPower * 100d;
                        }
                    }
                    var tuple = MyTuple.Create(
                        MyTuple.Create(timeRemaining, maxJump, currentJump, totJumpPercent, currentStoredPower, maxStoredPower),
                        MyTuple.Create(rangeFinderPosition, !string.IsNullOrEmpty(rangeFinderName) ? rangeFinderName : selectedPlanet, rangeFinderDistance, rangeFinderDiameter),
                        MyTuple.Create(magneticDrive, idleThrusters, sunAlign, safetyDampeners, useGyrosToStabilize, autoCombat),
                        MyTuple.Create(obstaclesAvoidance, collisionDetection, enemyEvasion, keepAltitude, moddedSensor, closeRangeCombat)
                        );
                    IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
                    sendMessageCount = 0;
                }
                sendMessageCount++;
            }
        }

        void GyroStabilize(bool isAutoPiloted, Vector3D gravity, double mySpeed, bool isTargetEmpty) {
            if (useGyrosToStabilize && !targFound && !aimTarget && !isAutoPiloted && isTargetEmpty) {//TODO || !Vector3D.IsZero(landPosition)
                if (!Vector3D.IsZero(gravity)) {
                    double pitchAngle, rollAngle, yawAngle;
                    double mouseYaw = CONTROLLER.RotationIndicator.Y;
                    double mousePitch = CONTROLLER.RotationIndicator.X;
                    double mouseRoll = CONTROLLER.RollIndicator;
                    if (mySpeed > 2d) {
                        if (Vector3D.IsZero(lastForwardVector)) {
                            lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                            lastUpVector = CONTROLLER.WorldMatrix.Up;
                        }
                        GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        lastForwardVector = CONTROLLER.WorldMatrix.Forward;
                        lastUpVector = CONTROLLER.WorldMatrix.Up;
                        if (mousePitch != 0d) {
                            mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                        }
                        mousePitch = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                        if (mouseRoll != 0d) {
                            mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                        }
                        mouseRoll = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
                        if (mouseYaw != 0d) {
                            mouseYaw = mouseYaw < 0d ? MathHelper.Clamp(mouseYaw, -10d, -2d) : MathHelper.Clamp(mouseYaw, 2d, 10d);
                        }
                        mouseYaw = mouseYaw == 0d ? yawController.Control(yawAngle) : yawController.Control(mouseYaw);
                    } else {
                        Vector3D horizonVec = Vector3D.Cross(gravity, Vector3D.Cross(CONTROLLER.WorldMatrix.Forward, gravity));//left vector
                        GetRotationAnglesSimultaneous(horizonVec, -gravity, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        if (mousePitch != 0d) {
                            mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                        }
                        mousePitch = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                        if (mouseRoll != 0d) {
                            mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                        }
                        mouseRoll = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
                        if (mouseYaw != 0d) {
                            mouseYaw = mouseYaw < 0d ? MathHelper.Clamp(mouseYaw, -10d, -2d) : MathHelper.Clamp(mouseYaw, 2d, 10d);
                        }
                        mouseYaw = mouseYaw == 0d ? yawController.Control(yawAngle) : yawController.Control(mouseYaw);
                    }
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

        void ManageThrusters(double mySpeed, bool isAutoPiloted) {
            if (manageThrustersCount >= 10) {
                if (magneticDrive && !isAutoPiloted && idleThrusters) {
                    controlThrustersOnce = false;
                    if (mySpeed <= 0.1d) {
                        //if (REMOTE.ControlThrusters) { REMOTE.ControlThrusters = false; }
                        //if (CONTROLLER.ControlThrusters) { CONTROLLER.ControlThrusters = false; }
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                    } else if (mySpeed > 0.1d && mySpeed < 10d) {
                        //if (!REMOTE.ControlThrusters) { REMOTE.ControlThrusters = true; }
                        //if (!CONTROLLER.ControlThrusters) { CONTROLLER.ControlThrusters = true; }
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    } else {
                        //if (REMOTE.ControlThrusters) { REMOTE.ControlThrusters = false; }
                        //if (CONTROLLER.ControlThrusters) { CONTROLLER.ControlThrusters = false; }
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                    }
                } else {
                    if (!controlThrustersOnce) {
                        //REMOTE.ControlThrusters = true;
                        //CONTROLLER.ControlThrusters = true;
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                        controlThrustersOnce = true;
                    }
                }
                manageThrustersCount = 0;
            }
            manageThrustersCount++;
        }

        void ManageDampeners(double mySpeed, bool isUnderControl, bool isAutoPiloted) {
            if (manageDampenersCount >= 10) {
                if (descend && !magneticDrive) {
                    if (REMOTE.DampenersOverride) {
                        REMOTE.DampenersOverride = false;
                    }
                    if (CONTROLLER.DampenersOverride) {
                        CONTROLLER.DampenersOverride = false;
                    }
                } else if (!Vector3D.IsZero(collisionDir) && magneticDrive) {
                    if (REMOTE.DampenersOverride) {
                        REMOTE.DampenersOverride = false;
                    }
                    if (CONTROLLER.DampenersOverride) {
                        CONTROLLER.DampenersOverride = false;
                    }
                } else if (autoCombat && !isUnderControl && targFound && magneticDrive) {
                    if (REMOTE.DampenersOverride) {
                        REMOTE.DampenersOverride = false;
                    }
                    if (CONTROLLER.DampenersOverride) {
                        CONTROLLER.DampenersOverride = false;
                    }
                } else if (isAutoPiloted && magneticDrive) {
                    if (!REMOTE.DampenersOverride) {
                        REMOTE.DampenersOverride = true;
                    }
                    if (!CONTROLLER.DampenersOverride) {
                        CONTROLLER.DampenersOverride = true;
                    }
                } else {
                    if (safetyDampeners) {
                        safetyDampenersOnce = false;
                        if (mySpeed <= 0.1d) {
                            if (REMOTE.DampenersOverride) {
                                REMOTE.DampenersOverride = false;
                            }
                            if (CONTROLLER.DampenersOverride) {
                                CONTROLLER.DampenersOverride = false;
                            }
                        } else {
                            if (!REMOTE.DampenersOverride) {
                                REMOTE.DampenersOverride = true;
                            }
                            if (!CONTROLLER.DampenersOverride) {
                                CONTROLLER.DampenersOverride = true;
                            }
                        }
                    } else {
                        if (!safetyDampenersOnce) {
                            REMOTE.DampenersOverride = true;
                            CONTROLLER.DampenersOverride = true;
                            safetyDampenersOnce = true;
                        }
                    }
                }
                manageDampenersCount = 0;
            }
            manageDampenersCount++;
        }

        void ManageMagneticDrive(bool isUnderControl, bool isAutoPiloted, Vector3D gravity, Vector3D myVelocity, double mySpeed) {
            //if (magneticDrive && needControl) {
            Vector3D dir = Vector3D.Zero;
            //initMagneticDriveOnce = false;
            SyncRotors();
            double altitude = 60d;
            if (!Vector3D.IsZero(gravity)) {
                CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            }
            if (!Vector3D.IsZero(collisionDir)) {
                initCollisionMagneticDriveOnce = false;
                dir = collisionDir;
            } else {
                if (!initCollisionMagneticDriveOnce) {
                    randomDir = Vector3D.Zero;
                    initCollisionMagneticDriveOnce = true;
                }
                if (isAutoPiloted) {
                    dir = AutoMagneticDrive(dir);
                } else {
                    if (!initAutoMagneticDriveOnce) {
                        foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                        initAutoMagneticDriveOnce = true;
                    }
                    if (autoCombat && !isUnderControl && targFound) {
                        initRandomMagneticDriveOnce = false;
                        RandomMagneticDrive();
                        dir = randomDir;
                        Vector3D dirNN = Vector3D.Zero;
                        foreach (MyDetectedEntityInfo target in targetsInfo) {
                            Vector3D escapeDir = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - target.Position);
                            escapeDir = Vector3D.TransformNormal(escapeDir, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                            dirNN = SetResultVector(dirNN, escapeDir);
                        }
                        Vector3D dirNew = KeepRightDistance(targPosition);

                        dirNew = MergeDirectionValues(dirNN, dirNew);
                        dir = MergeDirectionValues(dir, dirNew);

                        if (keepRightAltitudeCount >= 10) {
                            dirNew = KeepRightAltitude(gravity, altitude);
                            dir = MergeDirectionValues(dir, dirNew);
                            keepRightAltitudeCount = 0;
                        }
                        keepRightAltitudeCount++;
                    } else {
                        if (!initRandomMagneticDriveOnce) {
                            randomDir = Vector3D.Zero;
                            initRandomMagneticDriveOnce = true;
                        }
                        if (!Vector3D.IsZero(landPosition)) {
                            initlandMagneticDriveOnce = false;
                            double stopDistance = CalculateStopDistance(myVelocity);
                            Vector3D landPos = landPosition + (Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - landPosition) * stopDistance);
                            Vector3D landDir = Vector3D.Normalize(landPos - CONTROLLER.CubeGrid.WorldVolume.Center);
                            landDir = Vector3D.TransformNormal(landDir, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                            dir = MergeDirectionValues(dir, landDir);
                            if (Vector3D.Distance(landPos, REMOTE.CubeGrid.WorldVolume.Center) <= 50d) {//TODO
                                dir = Vector3D.Zero;
                                dir = MagneticDampeners(dir, myVelocity, gravity);
                                if (Vector3D.IsZero(myVelocity)) {
                                    landPosition = Vector3D.Zero;
                                    descend = true;
                                    //magneticDrive = false;
                                }
                            }

                            //----------------------------------
                            Debug.DrawPoint(landPos, Color.Red, 50f, onTop: true);
                            Vector3D normalized = Vector3D.Normalize(landDir);
                            Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center + (normalized * 2000d);
                            Debug.DrawLine(CONTROLLER.CubeGrid.WorldVolume.Center, position, Color.Magenta, thickness: 2f, onTop: true);
                            Debug.PrintHUD($"landDir: {landDir.X:0.##},{landDir.Y:0.##},{landDir.Z:0.##}");
                            Debug.PrintHUD($"landPos: {landPos.X:0.##},{landPos.Y:0.##},{landPos.Z:0.##}");
                            //----------------------------------

                        } else {
                            if (!initlandMagneticDriveOnce) {
                                landPosition = Vector3D.Zero;
                                initlandMagneticDriveOnce = true;
                            }
                            dir = MagneticDrive();
                            dir = MagneticDampeners(dir, myVelocity, gravity);
                            Vector3D dirNew = KeepAltitude(isUnderControl, keepAltitude, gravity, altitude);
                            dir = MergeDirectionValues(dir, dirNew);
                        }
                    }
                }
            }

            if (enemyEvasion) {
                Vector3D dirN = EvadeEnemy(targOrientation, targVelVec, targPosition, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targFound);
                foreach (MyDetectedEntityInfo target in targetsInfo) {
                    Vector3D escapeDir = EvadeEnemy(target.Orientation, target.Velocity, target.Position, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targFound);
                    dirN = SetResultVector(dirN, escapeDir);
                }
                dir = MergeDirectionValues(dir, dirN);
            }

            if (obstaclesAvoidance && mySpeed > 10f && !isAutoPiloted && Vector3D.IsZero(landPosition)) {//TODO && (Vector3D.IsZero(gravity) || (!Vector3D.IsZero(gravity) && altitude > 60d))
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
                    RaycastStopPosition(stopDistance, myVelocity, mySpeed);

                    if (moddedSensor) { SetSensorsStopDistance((float)stopDistance, (float)mySpeed); }
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

            SetPower(dir);

            /*} else {
                if (!initMagneticDriveOnce) {
                    IdleMagneticDrive();
                    initMagneticDriveOnce = true;
                }
            }*/
        }

        void ManageDrive(bool isUnderControl, bool isAutoPiloted, Vector3D gravity, Vector3D myVelocity, double mySpeed) {
            Vector3D dir = Vector3D.Zero;
            double altitude = 60d;
            if (!Vector3D.IsZero(gravity)) {
                CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            }
            if (!Vector3D.IsZero(collisionDir)) {
                initCollisionMagneticDriveOnce = false;
                dir = collisionDir;
            } else {
                if (!initCollisionMagneticDriveOnce) {
                    randomDir = Vector3D.Zero;
                    initCollisionMagneticDriveOnce = true;
                }
                if (autoCombat && !isUnderControl && targFound) {
                    initRandomMagneticDriveOnce = false;
                    RandomMagneticDrive();
                    dir = randomDir;
                    Vector3D dirNN = Vector3D.Zero;
                    foreach (MyDetectedEntityInfo target in targetsInfo) {
                        Vector3D escapeDir = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - target.Position);
                        escapeDir = Vector3D.TransformNormal(escapeDir, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                        dirNN = SetResultVector(dirNN, escapeDir);
                    }
                    Vector3D dirNew = KeepRightDistance(targPosition);

                    dirNew = MergeDirectionValues(dirNN, dirNew);
                    dir = MergeDirectionValues(dir, dirNew);

                    if (keepRightAltitudeCount >= 10) {
                        dirNew = KeepRightAltitude(gravity, altitude);
                        dir = MergeDirectionValues(dir, dirNew);
                        keepRightAltitudeCount = 0;
                    }
                    keepRightAltitudeCount++;
                } else {
                    if (!initRandomMagneticDriveOnce) {
                        randomDir = Vector3D.Zero;
                        initRandomMagneticDriveOnce = true;
                    }
                    if (!Vector3D.IsZero(landPosition)) {
                        initlandMagneticDriveOnce = false;
                        Vector3D stopDirection = CalculateThrustStopVector(Runtime.TimeSinceLastRun.TotalSeconds, myVelocity);
                        double stopDistance = stopDirection.Length();
                        Vector3D landPos = landPosition + (Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - landPosition) * stopDistance);
                        Vector3D landDir = Vector3D.Normalize(landPos - CONTROLLER.CubeGrid.WorldVolume.Center);
                        landDir = Vector3D.TransformNormal(landDir, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                        dir = MergeDirectionValues(dir, landDir);
                        if (Vector3D.Distance(landPos, REMOTE.CubeGrid.WorldVolume.Center) <= 50d) {//TODO
                            dir = Vector3D.Zero;
                            if (Vector3D.IsZero(myVelocity)) {
                                landPosition = Vector3D.Zero;
                                descend = true;
                                //magneticDrive = false;
                            }
                        }

                        //----------------------------------
                        Debug.DrawPoint(landPos, Color.Red, 50f, onTop: true);
                        Vector3D normalized = Vector3D.Normalize(landDir);
                        Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center + (normalized * 2000d);
                        Debug.DrawLine(CONTROLLER.CubeGrid.WorldVolume.Center, position, Color.Magenta, thickness: 2f, onTop: true);
                        Debug.PrintHUD($"landDir: {landDir.X:0.##},{landDir.Y:0.##},{landDir.Z:0.##}");
                        Debug.PrintHUD($"landPos: {landPos.X:0.##},{landPos.Y:0.##},{landPos.Z:0.##}");
                        //----------------------------------

                    } else {
                        if (!initlandMagneticDriveOnce) {
                            landPosition = Vector3D.Zero;
                            initlandMagneticDriveOnce = true;
                        }

                        Vector3D direction = CONTROLLER.MoveIndicator;
                        direction = Vector3D.Transform(direction, mtrx);
                        if (!Vector3D.IsZero(direction)) {
                            dir = Vector3D.Normalize(direction);
                        }
                        //Vector3D dirNew = KeepAltitude(isUnderControl, keepAltitude, gravity, altitude);
                        //dir = MergeDirectionValues(dir, dirNew);
                    }
                }
            }

            if (enemyEvasion) {
                Vector3D dirN = EvadeEnemy(targOrientation, targVelVec, targPosition, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targFound);
                foreach (MyDetectedEntityInfo target in targetsInfo) {
                    Vector3D escapeDir = EvadeEnemy(target.Orientation, target.Velocity, target.Position, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity, targFound);
                    dirN = SetResultVector(dirN, escapeDir);
                }
                dir = MergeDirectionValues(dir, dirN);
            }

            if (obstaclesAvoidance && mySpeed > 10f && !isAutoPiloted && Vector3D.IsZero(landPosition)) {//TODO && (Vector3D.IsZero(gravity) || (!Vector3D.IsZero(gravity) && altitude > 60d))
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
                if (collisionCheckCount >= collisionCheckDelay) {
                    Vector3D stopDirection = CalculateThrustStopVector(Runtime.TimeSinceLastRun.TotalSeconds, myVelocity);
                    double stopDistance = stopDirection.Length();
                    RaycastStopPosition(stopDistance, myVelocity, mySpeed);

                    if (moddedSensor) { SetSensorsStopDistance((float)stopDistance, (float)mySpeed); }
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
            ManageThrustersDrive(dir);//TODO
        }

        void ManageThrustersDrive(Vector3D direction) {//TODO set right power
            if (!Vector3D.IsZero(direction)) {
                if (direction.Z < 0f) {
                    foreach (IMyThrust thuster in FORWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in BACKWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else if (direction.Z > 0f) {
                    foreach (IMyThrust thuster in BACKWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in FORWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else {
                    foreach (IMyThrust thuster in BACKWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                    foreach (IMyThrust thuster in FORWARDTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                }

                if (direction.Y < 0f) {
                    foreach (IMyThrust thuster in DOWNTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in UPTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else if (direction.Y > 0f) {
                    foreach (IMyThrust thuster in UPTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in DOWNTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else {
                    foreach (IMyThrust thuster in UPTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                    foreach (IMyThrust thuster in DOWNTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                }

                if (direction.X < 0f) {
                    foreach (IMyThrust thuster in LEFTTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in RIGHTTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else if (direction.X > 0f) {
                    foreach (IMyThrust thuster in RIGHTTHRUSTERS) { thuster.ThrustOverridePercentage = 1f; }
                    foreach (IMyThrust thuster in LEFTTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                } else {
                    foreach (IMyThrust thuster in RIGHTTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                    foreach (IMyThrust thuster in LEFTTHRUSTERS) { thuster.ThrustOverridePercentage = 0f; }
                }
            } else {
                foreach (IMyThrust thuster in THRUSTERS) {
                    thuster.ThrustOverridePercentage = 0f;
                }
            }
        }

        Vector3D CalculateThrustStopVector(double timeStep, Vector3D myVelocity) {
            Vector3D localVelocity = Vector3D.TransformNormal(myVelocity, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            //Vector3D thrustVector = myThrustList.GetThrustVectorByDirection(localVelocity);//myVelocity
            Vector3D acceleration = (myVelocity - lastVelocity) / timeStep;
            lastVelocity = myVelocity;
            double accel = acceleration.Length();
            Vector3D localAcceleration = Vector3D.TransformNormal(Vector3D.Normalize(acceleration), MatrixD.Transpose(CONTROLLER.WorldMatrix));
            localAcceleration *= accel;
            Vector3D min = Vector3D.Zero;
            Vector3D max = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {
                double component = localAcceleration.GetDim(i);
                if (component >= 0d) {
                    max.SetDim(i, component);
                } else {//if negative
                    component = Math.Abs(component);
                    min.SetDim(i, component);
                }
            }
            Vector3D stopVector = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {//Now we break the current velocity apart component by component
                double velocityComponent = localVelocity.GetDim(i);
                double stopDistComponent = velocityComponent >= 0d
                    ? velocityComponent * velocityComponent / (2d * min.GetDim(i))
                    : velocityComponent * velocityComponent / (2d * max.GetDim(i));
                stopVector.SetDim(i, stopDistComponent);
            }
            return stopVector;//Stop distance is just the magnitude of our result vector
        }

        void Descend(Vector3D myVelocity, Vector3D gravity) {
            float mass = CONTROLLER.CalculateShipMass().PhysicalMass;
            double weight = mass * gravity.Length();
            Vector3D localGravity = Vector3D.TransformNormal(Vector3D.Normalize(gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));
            myThrustList.GetThrustVectorByDirection(localGravity);
            weight = (weight / 10d) * 8d;//80% thrust
            double gThrust = weight / myThrustList.tempThrusters.Count;
            foreach (IMyThrust thruster in myThrustList.tempThrusters) {
                thruster.ThrustOverride = (float)gThrust;
            }
            double altitude;
            CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            if (altitude < 30 && Vector3D.IsZero(myVelocity)) {
                descend = false;
                foreach (MyThrustBlock thruster in myThrustList.thrusters) {
                    thruster.thruster.ThrustOverride = 0f;
                }
            }
        }

        void MagneticDescend(Vector3D myVelocity, Vector3D gravity) {//TODO
            idleThrusters = true;
            foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(25, 0, 100); }
            safetyDampeners = true;
            if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = new Color(25, 0, 100); }
            Vector3D dir = Vector3D.Zero;
            dir = MagneticDampeners(dir, myVelocity, gravity);
            SetPower(dir);
            double altitude;
            CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            if (altitude < 30 && Vector3D.IsZero(myVelocity)) {
                descend = false;
                idleThrusters = false;
                foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = Color.Black; }
                safetyDampeners = false;
                if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = Color.Black; }
            }
        }

        void IdleMagneticDrive() {
            SetPower(Vector3D.Zero);
            foreach (IMyMotorStator block in ROTORS) { block.TargetVelocityRPM = 0f; }
            foreach (IMyMotorStator block in ROTORSINV) { block.TargetVelocityRPM = 0f; }
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

        Vector3D MagneticDrive() {
            Vector3D direction = CONTROLLER.MoveIndicator;
            //CONTROLLER.Orientation.GetMatrix(out mtrx);
            direction = Vector3D.Transform(direction, mtrx);
            if (!Vector3D.IsZero(direction)) {
                direction = Vector3D.Normalize(direction);//direction /= direction.Length();
            }
            return direction;
        }

        Vector3D MagneticDampeners(Vector3D direction, Vector3D myVelocity, Vector3D gravity) {
            if (Vector3D.IsZero(gravity) && !CONTROLLER.DampenersOverride && direction.LengthSquared() == 0d) {
                return Vector3D.Zero;
            }
            Vector3D vel = myVelocity;
            vel = Vector3D.Transform(vel, MatrixD.Transpose(CONTROLLER.WorldMatrix.GetOrientation()));
            vel = direction * 105d - Vector3D.Transform(vel, mtrx);//TODO
            if (Math.Abs(vel.X) < 2d) { vel.X = 0d; }
            if (Math.Abs(vel.Y) < 2d) { vel.Y = 0d; }
            if (Math.Abs(vel.Z) < 2d) { vel.Z = 0d; }
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
            if (randomCount >= 10) {//TODO
                randomDir = Vector3D.Zero;
                float randomFloat;
                randomFloat = (float)random.Next(-1, 2);
                randomDir.X = randomFloat;
                randomFloat = (float)random.Next(-1, 2);
                randomDir.Y = randomFloat;
                randomFloat = (float)random.Next(-1, 2);
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

            Debug.PrintHUD($"KeepRightDistance: {dir.X:0.##},{dir.Y:0.##},{dir.Z:0.##}");

            return dir;//TODO multiply by value?
        }

        bool TurretsDetection() {
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

        void ManageCollisions() {
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

                    //------------------------------------
                    Vector3D normalizedVec = Vector3D.Normalize(escapeDirection);
                    Vector3D position = REMOTE.CubeGrid.WorldVolume.Center + (normalizedVec * 1000d);
                    Debug.DrawLine(REMOTE.CubeGrid.WorldVolume.Center, position, Color.LimeGreen, thickness: 1f, onTop: true);
                    Debug.PrintHUD($"CheckCollisions: {escapeDirection.X:0.##},{escapeDirection.Y:0.##},{escapeDirection.Z:0.##}");
                    //------------------------------------

                    return escapeDirection;//TODO multiply by value?
                } else {
                    return Vector3D.Zero;
                }
            } else {
                return Vector3D.Zero;
            }
        }

        Vector3D EvadeEnemy(MatrixD targOrientation, Vector3D targVel, Vector3D targPos, Vector3D myPosition, Vector3D myVelocity, Vector3D gravity, bool targetFound) {
            if (targetFound) {
                Base6Directions.Direction enemyForward = targOrientation.GetClosestDirection(CONTROLLER.WorldMatrix.Backward);//TODO it's not backward if the target isn't in front of me
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

                //---------------------------------------------------------------------------
                Vector3D position = targPos + (enemyForwardVec * 2000d);
                Debug.DrawLine(targPos, position, Color.Orange, thickness: 2f, onTop: true);

                Vector3D normalizedVec = Vector3D.Normalize(enemyAim);
                position = targPos + (normalizedVec * 2000d);
                Debug.DrawLine(targPos, position, Color.Magenta, thickness: 2f, onTop: true);
                Debug.PrintHUD($"EvadeEnemy, angle:{angle:0.##}, safety:{4500d / distance:0.##}");
                //---------------------------------------------------------------------------

                if (angle < (4500d / distance)) {
                    Vector3D evadeDirection = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - (targPos + (enemyForwardVec * distance)));//toward my center
                    evadeDirection = Vector3D.TransformNormal(evadeDirection, MatrixD.Transpose(CONTROLLER.WorldMatrix));

                    //---------------------------------------------------------------------------
                    Debug.DrawPoint(targPos + (enemyForwardVec * distance), Color.Green, 4f, onTop: true);
                    Debug.DrawPoint(CONTROLLER.CubeGrid.WorldVolume.Center, Color.Yellow, 4f, onTop: true);
                    Debug.DrawLine(CONTROLLER.CubeGrid.WorldVolume.Center, targPos + (enemyForwardVec * distance), Color.Purple, thickness: 1f, onTop: true);
                    Debug.PrintHUD($"EvadeEnemy: {evadeDirection.X:0.##},{evadeDirection.Y:0.##},{evadeDirection.Z:0.##}");
                    //---------------------------------------------------------------------------

                    return evadeDirection;//TODO multiply by value?
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
            Vector3D localAcceleration = Vector3D.TransformNormal(acceleration, MatrixD.Transpose(CONTROLLER.WorldMatrix));
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

        double CalculateStopDistance(Vector3D myVelocity) {//TODO check direction, accounting that all dirction have same accel/decel
            Vector3D localVelocity = Vector3D.TransformNormal(myVelocity, MatrixD.Transpose(CONTROLLER.WorldMatrix));
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

        void RaycastStopPosition(double stopDistance, Vector3D myVelocity, double mySpeed) {
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
            stopDistance += (mySpeed * 10d);//TODO

            //----------------------------------
            Vector3D stop = CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance);
            Debug.PrintHUD($"stopDistance:{stopDistance:0.##}");
            Debug.DrawPoint(stop, Color.Blue, 5f, onTop: true);
            Debug.DrawPoint(CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance), Color.Orange, 5f, onTop: true);
            //----------------------------------

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

        Vector3D KeepRightAltitude(Vector3D gravity, double altitude) {
            Vector3D dir = Vector3D.Zero;
            if (!Vector3D.IsZero(gravity)) {
                if (altitude < 100d) {
                    dir = Vector3D.TransformNormal(Vector3D.Normalize(-gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));//TODO multiply by value?

                    Debug.PrintHUD($"KeepRightAltitude: {dir.X:0.##},{dir.Y:0.##},{dir.Z:0.##}");
                }
            }
            return dir;
        }

        Vector3D KeepAltitude(bool isUnderControl, bool keepAltitude, Vector3D gravity, double altitude) {
            Vector3D dir = Vector3D.Zero;
            if (!isUnderControl && !Vector3D.IsZero(gravity) && keepAltitude) {
                if (keepAltitudeOnce) {
                    hoverPosition = CONTROLLER.CubeGrid.WorldVolume.Center;
                    keepAltitudeOnce = false;
                }
                if (altitude > 60d) {
                    if (keepAltitudeCount >= 50) {
                        if (!Vector3D.IsZero(hoverPosition) && Vector3D.Distance(hoverPosition, CONTROLLER.CubeGrid.WorldVolume.Center) > 300d) {
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
                        dir = Vector3D.TransformNormal(Vector3D.Normalize(-gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));//TODO multiply by value?

                        Debug.PrintHUD($"KeepAltitude: {dir.X:0.##},{dir.Y:0.##},{dir.Z:0.##}");

                    } else if (altitude > altitudeToKeep + 30d) {
                        dir = Vector3D.TransformNormal(Vector3D.Normalize(gravity), MatrixD.Transpose(CONTROLLER.WorldMatrix));//TODO multiply by value?

                        Debug.PrintHUD($"KeepAltitude: {dir.X:0.##},{dir.Y:0.##},{dir.Z:0.##}");
                    }
                }
            } else {
                if (!keepAltitudeOnce) {
                    keepAltitudeOnce = true;
                    altitudeToKeep = 0d;
                }
            }
            return dir;
        }

        Vector3D MergeDirectionValues(Vector3D dirToKeep, Vector3D dirNew) {//TODO
            //dirToKeep.X = Math.Abs(dirNew.X) > 0.1d ? dirNew.X : dirToKeep.X;
            //dirToKeep.Y = Math.Abs(dirNew.Y) > 0.1d ? dirNew.Y : dirToKeep.Y;
            //dirToKeep.Z = Math.Abs(dirNew.Z) > 0.1d ? dirNew.Z : dirToKeep.Z;
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

        void ManageWaypoints(bool isUnderControl, bool isTargetEmpty) {
            if (targFound || !isTargetEmpty) {
                //TODO do once
                landPosition = Vector3D.Zero;
                descend = false;
                aimTarget = false;
                sunAlign = false;

                if (REMOTE.IsAutoPilotEnabled) {
                    REMOTE.SetAutoPilotEnabled(false);
                }
                if (returnOnce && Vector3D.IsZero(returnPosition) && !isUnderControl) {
                    returnPosition = REMOTE.CubeGrid.WorldVolume.Center;
                    returnOnce = false;
                }
            } else {
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition)) {
                    if (Vector3D.Distance(returnPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        returnPosition = Vector3D.Zero;
                        returnOnce = true;
                    }
                }
                if (!REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition)) {
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(returnPosition, "returnPosition");
                    REMOTE.SetAutoPilotEnabled(true);
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(hoverPosition)) {
                    if (Vector3D.Distance(hoverPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        hoverPosition = Vector3D.Zero;
                        keepAltitudeOnce = true;
                    }
                }
                /*if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(landPosition)) {
                    if (Vector3D.Distance(landPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        if (mySpeed < 2d) {
                            REMOTE.ClearWaypoints();
                            REMOTE.SetAutoPilotEnabled(false);
                            landPosition = Vector3D.Zero;
                            idleThrusters = true;
                            foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(25, 0, 100); }
                            safetyDampeners = true;
                            if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = new Color(25, 0, 100); }
                        }
                    }
                }
                if (!Vector3D.IsZero(landPosition)) {//TODO
                    if (Vector3D.Distance(landPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        if (Vector3D.IsZero(myVelocity)) {
                            landPosition = Vector3D.Zero;
                            descend = true;
                            //magneticDrive = false;
                        }
                    }
                }*/

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(rangeFinderPosition)) {
                    if (Vector3D.Distance(rangeFinderPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.SetAutoPilotEnabled(false);
                        rangeFinderPosition = Vector3D.Zero;
                        rangeFinderName = "";
                        rangeFinderDistance = 0d;
                        rangeFinderDiameter = 0d;
                    }
                }
            }
        }

        void RangeFinder() {
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            double raycastDistance = lidar.AvailableScanRange < 40000d ? lidar.AvailableScanRange : 40000d;
            MyDetectedEntityInfo TARGET = lidar.Raycast(raycastDistance);
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
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    rangeFinderName = TARGET.Name;
                    rangeFinderDiameter = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value) * 2d;
                    rangeFinderDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value);
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Asteroid");
                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                    rangeFinderName = TARGET.Name;
                    rangeFinderDiameter = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value) * 2d;
                    rangeFinderDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value);
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 3000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    rangeFinderName = TARGET.Name;
                    rangeFinderDiameter = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value) * 2d;
                    rangeFinderDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value);
                } else {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    rangeFinderPosition = safeJumpPosition;
                    rangeFinderName = TARGET.Name;
                    rangeFinderDiameter = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value) * 2d;
                    rangeFinderDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, TARGET.HitPosition.Value);
                }
            } else {
                rangeFinderName = "Nothing Detected!";
            }
        }

        void Land(Vector3D gravity) {
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            double raycastDistance = lidar.AvailableScanRange < 5000d ? lidar.AvailableScanRange : 5000d;
            MyDetectedEntityInfo TARGET = lidar.Raycast(raycastDistance);
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    landPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(-gravity) * 100d);//TODO
                    //REMOTE.ClearWaypoints();
                    //REMOTE.AddWaypoint(landPosition, "landPosition");
                    //REMOTE.SetAutoPilotEnabled(true);
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
                if (sunAlignOnce) {
                    if (LCDSUNALIGN != null) { LCDSUNALIGN.BackgroundColor = new Color(25, 0, 100); }
                    prevSunPower = power;
                    unlockSunAlignOnce = true;
                    sunAlignOnce = false;
                }
                double pitch = 0d;
                double yaw = 0d;
                if (power < .02) {
                    if (unlockSunAlignOnce) {
                        UnlockGyros();
                        unlockSunAlignOnce = false;
                    }
                    return;
                }
                if (power > .98) {
                    if (sunAlignmentStep > 0) {
                        sunAlignmentStep = 0;
                        if (unlockSunAlignOnce) {
                            UnlockGyros();
                            unlockSunAlignOnce = false;
                        }
                    }
                    return;
                }
                unlockSunAlignOnce = true;
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
            if (UPSENSOR != null) {
                UPSENSOR.LeftExtend = 36f;
                UPSENSOR.RightExtend = 36f;
                UPSENSOR.BottomExtend = 28.5f;
                UPSENSOR.TopExtend = 42f;
                BACKWARDSENSOR.BackExtend = 0.1f;
                UPSENSOR.FrontExtend = 50f;
            }
            if (DOWNSENSOR != null) {
                DOWNSENSOR.LeftExtend = 36f;
                DOWNSENSOR.RightExtend = 36f;
                DOWNSENSOR.BottomExtend = 43.5f;
                DOWNSENSOR.TopExtend = 27f;
                DOWNSENSOR.BackExtend = 0.1f;
                DOWNSENSOR.FrontExtend = 50f;
            }
            if (FORWARDSENSOR != null) {
                FORWARDSENSOR.LeftExtend = 36f;
                FORWARDSENSOR.RightExtend = 36f;
                FORWARDSENSOR.BottomExtend = 16f;
                FORWARDSENSOR.TopExtend = 11f;
                FORWARDSENSOR.BackExtend = 50f;
                FORWARDSENSOR.FrontExtend = 0.1f;
            }
            if (BACKWARDSENSOR != null) {
                BACKWARDSENSOR.LeftExtend = 36f;
                BACKWARDSENSOR.RightExtend = 36f;
                BACKWARDSENSOR.BottomExtend = 18f;
                BACKWARDSENSOR.TopExtend = 8.5f;
                BACKWARDSENSOR.BackExtend = 0.1f;
                BACKWARDSENSOR.FrontExtend = 50f;
            }
            if (LEFTSENSOR != null) {
                LEFTSENSOR.LeftExtend = 33.5f;
                LEFTSENSOR.RightExtend = 37f;
                LEFTSENSOR.BottomExtend = 13f;
                LEFTSENSOR.TopExtend = 13.5f;
                LEFTSENSOR.BackExtend = 0.1f;
                LEFTSENSOR.FrontExtend = 50f;
            }
            if (RIGHTSENSOR != null) {
                RIGHTSENSOR.LeftExtend = 37f;
                RIGHTSENSOR.RightExtend = 33.5f;
                RIGHTSENSOR.BottomExtend = 13f;
                RIGHTSENSOR.TopExtend = 13.5f;
                RIGHTSENSOR.BackExtend = 0.1f;
                RIGHTSENSOR.FrontExtend = 50f;
            }
        }

        void SetSensorsStopDistance(float stopDistance, float mySpeed) {
            stopDistance += (mySpeed * 2.5f);
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

        void UpdateConfigParams() {
            if (configChanged) {
                if (!magneticDrive) {
                    autoCombat = false;
                    obstaclesAvoidance = false;
                    collisionDetection = false;
                    enemyEvasion = false;
                }
                if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autoCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                if (LCDOBSTACLES != null) { LCDOBSTACLES.BackgroundColor = obstaclesAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
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
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSX, block => block.CustomName.Contains("Merge_MD+X"));
            MERGESPLUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSY, block => block.CustomName.Contains("Merge_MD+Y"));
            MERGESPLUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSZ, block => block.CustomName.Contains("Merge_MD+Z"));
            MERGESMINUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSX, block => block.CustomName.Contains("Merge_MD-X"));
            MERGESMINUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSY, block => block.CustomName.Contains("Merge_MD-Y"));
            MERGESMINUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSZ, block => block.CustomName.Contains("Merge_MD-Z"));
            LCDSAFETYDAMPENERS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Safety Dampeners") as IMyTextPanel;
            LCDSUNALIGN = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Sun Align") as IMyTextPanel;
            LCDMAGNETICDRIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Magnetic Drive") as IMyTextPanel;
            LCDAUTOCOMBAT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Combat") as IMyTextPanel;
            LCDOBSTACLES = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Obstacles") as IMyTextPanel;
            LCDCOLLISIONS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Collisions") as IMyTextPanel;
            LCDEVASION = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Evasion") as IMyTextPanel;
            LCDSTABILIZER = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Stabilizer") as IMyTextPanel;
            LCDALTITUDE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Keep Altitude") as IMyTextPanel;
            LCDMODDEDSENSOR = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Modded Sensor") as IMyTextPanel;
            LCDCLOSECOMBAT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Close Combat") as IMyTextPanel;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Thrusters") as IMyTextPanel;
            SENSORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(SENSORS, block => block.CustomName.Contains("[CRX] Sensor Obstacles"));
            foreach (IMySensorBlock sensor in SENSORS) {
                if (sensor.CustomName.Contains("UP")) { UPSENSOR = sensor; } else if (sensor.CustomName.Contains("DOWN")) { DOWNSENSOR = sensor; } else if (sensor.CustomName.Contains("LEFT")) { LEFTSENSOR = sensor; } else if (sensor.CustomName.Contains("RIGHT")) { RIGHTSENSOR = sensor; } else if (sensor.CustomName.Contains("FORWARD")) { FORWARDSENSOR = sensor; } else if (sensor.CustomName.Contains("BACKWARD")) { BACKWARDSENSOR = sensor; }
            }
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains("[CRX] Solar"));
            foreach (IMySolarPanel solar in SOLARS) { if (solar.IsFunctional && solar.Enabled && solar.IsWorking) { SOLAR = solar; } }
            REMOTE = GridTerminalSystem.GetBlockWithName("[CRX] Controller Remote Reference") as IMyRemoteControl;
            CONTROLLER = GridTerminalSystem.GetBlockWithName("[CRX] Controller Cockpit 1") as IMyShipController;
        }

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, globalTimestep);
            rollController = new PID(1d, 0d, 1d, globalTimestep);
        }

        void ManagePIDControllers(bool isTargetEmpty) {
            if (!isTargetEmpty || targFound) {
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

        public struct MyThrustBlock {
            public MatrixD localMatrix;
            public IMyThrust thruster;
            public MyThrustBlock(MatrixD _localMatrix, IMyThrust _thruster) {
                localMatrix = _localMatrix;
                thruster = _thruster;
            }
        }

        public class MyThrustList {
            public List<MyThrustBlock> thrusters;
            public MatrixD transposedReferenceWorldMatrix;
            public List<IMyThrust> tempThrusters = null;

            public MyThrustList(MatrixD _transposedReferenceWorldMatrix, List<MyThrustBlock> _thrusters) {
                thrusters = _thrusters;
                transposedReferenceWorldMatrix = _transposedReferenceWorldMatrix;
            }

            public Vector3D GetThrustVectorByDirection(Vector3D localDirection) {
                tempThrusters.Clear();
                Vector3D thrustSum = Vector3D.Zero;
                float maxThrust = 0f;
                foreach (MyThrustBlock block in thrusters) {
                    double dot = Vector3D.Dot(block.localMatrix.Backward, localDirection);//TODO Backward?
                    if (dot > 0d) {
                        maxThrust += block.thruster.MaxEffectiveThrust;
                        thrustSum += block.localMatrix.Backward;
                        tempThrusters.Add(block.thruster);
                    }
                }
                thrustSum = Vector3D.TransformNormal(Vector3D.Normalize(thrustSum), transposedReferenceWorldMatrix);
                return thrustSum * maxThrust;
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

                var methods = _pb.GetProperty("DebugAPI")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(_pb);
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

    }
}
