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

        //NAVIGATOR
        //----------------------------------
        DebugAPI Debug;
        //----------------------------------

        bool magneticDrive = true;//enable/disable magnetic drive
        bool idleThrusters = false;//enable/disable thrusters
        bool safetyDampeners = false;//enable/disable safety dampeners
        bool useGyrosToStabilize = true;//enable/disable gyro stabilization on planets and when driving
        bool autoCombat = true;//enable/disable automatic fighting when ship is not controlled
        bool obstaclesAvoidance = true;//enable/disable detection of obstacles while driving
        bool enemyEvasion = true;//enable/disable evasion from enemy aim
        bool keepAltitude = true;//enable/disable keeping altitude on planets
        bool moddedSensor = false;//define if is using modded sensors, valid only if obstaclesAvoidance is true
        bool closeRangeCombat = false;//set the fight distance for the automatic fight, valid only if autoCombat is true
        bool sunAlign = false;//enable/disable sun chase on space
        bool logger = true;//enable/disable logging

        bool aimTarget = false;
        bool targFound = false;
        bool follow = false;
        bool assaultCanShoot = true;
        bool artilleryCanShoot = true;
        bool railgunsCanShoot = true;
        bool smallRailgunsCanShoot = true;
        bool checkAllTicks = false;
        bool descend = false;
        bool initDescend = false;
        bool recoverMagneticDrive = false;
        bool unlockGyrosOnce = true;
        bool returnOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool initRandomMagneticDriveOnce = true;
        bool initPositionalDriveOnce = true;
        bool sunAlignOnce = true;
        bool unlockSunAlignOnce = true;
        bool keepAltitudeOnce = true;
        bool sensorDetectionOnce = true;
        bool updateOnce = true;
        bool controlThrustersOnce = false;
        bool safetyDampenersOnce = false;
        bool thrustOverrideOnce = true;
        bool targetFoundOnce = false;
        bool initFollowOnce = true;
        string selectedPlanet = "";
        string rangeFinderName = "";
        double altitudeToKeep = 0d;
        double movePitch = .01;
        double moveYaw = .01;
        double rangeFinderDiameter = 0d;
        double rangeFinderDistance = 0d;
        double timeSinceLastRaycast = 0d;
        double distanceToKeep = 0d;
        float prevSunPower = 0f;
        int planetSelector = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;
        int obstaclesCheckDelay = 10;
        int checkAllTicksCount = 0;
        int randomCount = 50;
        int obstaclesCheckCount = 0;
        int keepAltitudeCount = 0;
        int sendMessageCount = 0;
        int manageDampenersCount = 0;
        int manageThrustersCount = 0;
        int failedConnectionCount = 0;

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
        public List<IMyLandingGear> LANDINGGEARS = new List<IMyLandingGear>();

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
        IMyShipConnector DOCKCONNECTOR;

        IMyBroadcastListener BROADCASTLISTENER;
        MatrixD targOrientation = new MatrixD();
        MatrixD dockOrientation = new MatrixD();
        Vector3D rangeFinderPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D hoverPosition = Vector3D.Zero;
        Vector3D landPosition = Vector3D.Zero;
        Vector3D dockPosition = Vector3D.Zero;
        Vector3D dockHitPosition = Vector3D.Zero;
        Vector3D dockVelocity = Vector3D.Zero;
        Vector3D targHitPos = Vector3D.Zero;
        Vector3D targPosition = Vector3D.Zero;
        Vector3D targVelVec = Vector3D.Zero;
        Vector3D lastVelocity = Vector3D.Zero;
        Vector3D maxAccel = Vector3D.Zero;
        Vector3D minAccel = Vector3D.Zero;
        Vector3D randomDir = Vector3D.Zero;
        Vector3D sensorDir = Vector3D.Zero;
        Vector3D stopDir = Vector3D.Zero;

        public List<MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long>> targetsInfo = new List<MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long>>();//HitPosition, Velocity, Orientation, Position, EntityId

        readonly Vector3D[] baseDirection = new Vector3D[3] {
            Vector3D.Right,
            Vector3D.Up,
            Vector3D.Backward
        };

        readonly Vector3D[] directions = new Vector3D[6] {
            Vector3D.Right,
            Vector3D.Left,
            Vector3D.Up,
            Vector3D.Down,
            Vector3D.Backward,
            Vector3D.Forward,
        };

        double[] thrustSums = new double[6];

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

            //----------------------------------
            Debug = new DebugAPI(this);
            //----------------------------------
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
            CalculateBaseThrust();
            if (LCDSUNALIGN != null) { LCDSUNALIGN.BackgroundColor = new Color(0, 0, 0); }
            if (LCDSAFETYDAMPENERS != null) { LCDSAFETYDAMPENERS.BackgroundColor = safetyDampeners ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autoCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDOBSTACLES != null) { LCDOBSTACLES.BackgroundColor = obstaclesAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDSTABILIZER != null) { LCDSTABILIZER.BackgroundColor = useGyrosToStabilize ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDALTITUDE != null) { LCDALTITUDE.BackgroundColor = keepAltitude ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDMODDEDSENSOR != null) { LCDMODDEDSENSOR.BackgroundColor = moddedSensor ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDCLOSECOMBAT != null) { LCDCLOSECOMBAT.BackgroundColor = closeRangeCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(25, 0, 100) : new Color(0, 0, 0); }
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                //----------------------------------
                Debug.RemoveDraw();
                //Debug.PrintHUD($"LastRunTimeMs:{Runtime.LastRunTimeMs:0.####}");
                //----------------------------------

                GetBroadcastMessages();

                Vector3D gravity = CONTROLLER.GetNaturalGravity();
                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg, gravity);
                    if (arg == "RangeFinder") { return; }
                }

                Vector3D myVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;
                double mySpeed = myVelocity.Length();

                bool isAutoPiloted = REMOTE.IsAutoPilotEnabled;
                bool isUnderControl = IsPiloted(true);

                bool needControl;
                if (magneticDrive) {
                    needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || isAutoPiloted || mySpeed > 2d
                        || targFound || !Vector3D.IsZero(returnPosition) || !Vector3D.IsZero(dockPosition)
                        || !Vector3D.IsZero(dockHitPosition) || !Vector3D.IsZero(landPosition) || !Vector3D.IsZero(rangeFinderPosition);
                } else {
                    needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || targFound
                        || !Vector3D.IsZero(returnPosition) || !Vector3D.IsZero(dockPosition) || !Vector3D.IsZero(dockHitPosition)
                        || !Vector3D.IsZero(landPosition) || !Vector3D.IsZero(rangeFinderPosition);
                }

                if (aimTarget) {
                    bool aligned;
                    AimAtTarget(rangeFinderPosition, 0.1d, out aligned);
                    if (!aligned) { return; }
                }

                if (!needControl && sunAlign && Vector3D.IsZero(gravity) && !targFound) {
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

                ManageWaypoints(isUnderControl, myVelocity);

                ManagePIDControllers(gravity);

                GyroStabilize(isAutoPiloted, gravity);

                ManageThrusters(mySpeed, isAutoPiloted);

                if (magneticDrive) {
                    if (needControl) {
                        ManageMagneticDrive(isUnderControl, isAutoPiloted, gravity, myVelocity, mySpeed);
                        initMagneticDriveOnce = false;
                    } else {
                        if (!initMagneticDriveOnce) {
                            IdleMagneticDrive();
                            initMagneticDriveOnce = true;
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
                        if (needControl && !isAutoPiloted) {
                            ManageThrustersDrive(isUnderControl, gravity, myVelocity, mySpeed);
                        }
                    }
                }

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
                        if (!Vector3D.IsZero(rangeFinderPosition)) {
                            rangeFinderPosition = Vector3D.Zero;
                            if (magneticDrive) {
                                REMOTE.ClearWaypoints();
                                REMOTE.SetAutoPilotEnabled(false);
                            }
                            break;
                        }
                        RangeFinder();
                    } else {
                        if (!Vector3D.IsZero(landPosition) || descend) {
                            landPosition = Vector3D.Zero;
                            descend = false;
                            initDescend = false;
                            foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                            if (recoverMagneticDrive) {
                                magneticDrive = true;
                                REMOTE.ClearWaypoints();
                                REMOTE.SetAutoPilotEnabled(false);
                                REMOTE.SetCollisionAvoidance(true);
                                recoverMagneticDrive = false;
                            }
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
                    break;
                case "ToggleAutoCombat":
                    autoCombat = !autoCombat;
                    if (LCDAUTOCOMBAT != null) { LCDAUTOCOMBAT.BackgroundColor = autoCombat ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ToggleObstaclesAvoidance":
                    obstaclesAvoidance = !obstaclesAvoidance;
                    if (LCDOBSTACLES != null) { LCDOBSTACLES.BackgroundColor = obstaclesAvoidance ? new Color(25, 0, 100) : new Color(0, 0, 0); }
                    break;
                case "ToggleEnemyEvasion":
                    enemyEvasion = !enemyEvasion;
                    if (LCDEVASION != null) { LCDEVASION.BackgroundColor = enemyEvasion ? new Color(25, 0, 100) : new Color(0, 0, 0); }
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
                case "Dock":
                    if (!Vector3D.IsZero(dockPosition) || !Vector3D.IsZero(dockHitPosition)) {
                        dockPosition = Vector3D.Zero;
                        dockHitPosition = Vector3D.Zero;
                        dockOrientation = default(MatrixD);
                        dockVelocity = Vector3D.Zero;
                        foreach (IMyLandingGear block in LANDINGGEARS) {
                            block.AutoLock = true;
                        }
                        if (recoverMagneticDrive) {
                            magneticDrive = true;
                            recoverMagneticDrive = false;
                        }
                    } else {
                        if (magneticDrive) {
                            recoverMagneticDrive = true;
                            magneticDrive = false;
                        }
                        Dock();
                    }
                    break;
                default:
                    break;
            }
        }

        bool GetBroadcastMessages() {
            bool received = false;
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, bool>) {//TODO
                        MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, bool> data = (MyTuple<bool, Vector3D, Vector3D, MatrixD, Vector3D, bool>)igcMessage.Data;
                        targFound = data.Item1;
                        targHitPos = data.Item2;
                        targVelVec = data.Item3;
                        targOrientation = data.Item4;
                        targPosition = data.Item5;
                        follow = data.Item6;
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
                    targetsInfo.Clear();
                    if (igcMessage.Data is ImmutableArray<MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long>>) {
                        ImmutableArray<MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long>> data = (ImmutableArray<MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long>>)igcMessage.Data;
                        foreach (MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> info in data) {
                            MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> tuple = MyTuple.Create(
                                info.Item1,
                                info.Item2,
                                info.Item3,
                                info.Item4,
                                info.Item5
                            );
                            targetsInfo.Add(tuple);
                        }
                    }
                }
            }
            return received;
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
                        currentJump = (int)(JUMPERS[0].JumpDistanceMeters / JUMPERS[0].MaxJumpDistanceMeters * 100f);
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
                        MyTuple.Create(obstaclesAvoidance, enemyEvasion, keepAltitude, moddedSensor, closeRangeCombat)//TODO
                        );
                    IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
                    sendMessageCount = 0;
                }
                sendMessageCount++;
            }
        }

        void GyroStabilize(bool isAutoPiloted, Vector3D gravity) {
            if (useGyrosToStabilize && !targFound && !aimTarget && !isAutoPiloted && !Vector3D.IsZero(gravity) && Vector3D.IsZero(dockHitPosition)) {
                unlockGyrosOnce = true;
                double pitchAngle, rollAngle, yawAngle;
                double mouseYaw = CONTROLLER.RotationIndicator.Y;
                double mousePitch = CONTROLLER.RotationIndicator.X;
                double mouseRoll = CONTROLLER.RollIndicator;
                //Vector3D horizonVec = Vector3D.Cross(gravity, Vector3D.Cross(CONTROLLER.WorldMatrix.Forward, gravity));//left vector
                //double dot = Vector3D.Dot(CONTROLLER.WorldMatrix.Forward, Vector3D.Normalize(horizonVec));
                /*double dot = Vector3D.Dot(CONTROLLER.WorldMatrix.Down, Vector3D.Normalize(gravity));
                if (mousePitch == 0 && mouseYaw == 0 && mouseRoll == 0 && dot > 0.999d) {
                    if (unlockGyrosOnce) {
                        UnlockGyros();
                        unlockGyrosOnce = false;
                    }
                    return;
                }*/
                Vector3D horizonVec = Vector3D.Cross(gravity, Vector3D.Cross(CONTROLLER.WorldMatrix.Forward, gravity));//left vector
                GetRotationAnglesSimultaneous(horizonVec, -gravity, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                double yawSpeed;
                double pitchSpeed;
                double rollSpeed;
                if (mousePitch != 0d) {
                    mousePitch = mousePitch < 0d ? MathHelper.Clamp(mousePitch, -10d, -2d) : MathHelper.Clamp(mousePitch, 2d, 10d);
                }
                pitchSpeed = mousePitch == 0d ? pitchController.Control(pitchAngle) : pitchController.Control(mousePitch);
                if (mouseRoll != 0d) {
                    mouseRoll = mouseRoll < 0d ? MathHelper.Clamp(mouseRoll, -10d, -2d) : MathHelper.Clamp(mouseRoll, 2d, 10d);
                }
                rollSpeed = mouseRoll == 0d ? rollController.Control(rollAngle) : rollController.Control(mouseRoll);
                if (mouseYaw != 0d) {
                    mouseYaw = mouseYaw < 0d ? MathHelper.Clamp(mouseYaw, -10d, -2d) : MathHelper.Clamp(mouseYaw, 2d, 10d);
                }
                yawSpeed = mouseYaw == 0d ? yawController.Control(yawAngle) : yawController.Control(mouseYaw);
                ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
            } else {
                if (unlockGyrosOnce) {
                    UnlockGyros();
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
                if (autoCombat && !isUnderControl && targFound && magneticDrive) {
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
            Vector3D dir = Vector3D.Zero;
            SyncRotors();

            double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;
            UpdateAcceleration(timeSinceLastRun, myVelocity);//world (unused)

            if (isAutoPiloted) {
                dir = AutoMagneticDrive(dir);//world normal
            } else {
                if (!initAutoMagneticDriveOnce) {
                    foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                    initAutoMagneticDriveOnce = true;
                }
                if (follow) {
                    if (initFollowOnce) {
                        distanceToKeep = Vector3D.Distance(targPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                        initFollowOnce = false;
                    }
                    dir = KeepSameDistance(targPosition, distanceToKeep);//world normal

                    Vector3D dirMinAlt = KeepMinAltitude(gravity);//world normal
                    dir = !Vector3D.IsZero(dirMinAlt) ? dirMinAlt : dir;
                } else {
                    if (!initFollowOnce) {
                        distanceToKeep = 0d;
                        initFollowOnce = true;
                    }
                    if (autoCombat && !isUnderControl && targFound) {
                        initRandomMagneticDriveOnce = false;
                        RandomDrive(myVelocity);//world normal
                        dir = randomDir;
                        Vector3D dirEsc = Vector3D.Zero;
                        foreach (MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> target in targetsInfo) {//HitPosition, Velocity, Orientation, Position, EntityId
                            Vector3D escapeDir = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - target.Item4);//world normal
                            dirEsc = SetResultVector(dirEsc, escapeDir);
                        }
                        dir = !Vector3D.IsZero(dirEsc) ? dirEsc : dir;

                        Vector3D dirDist = KeepRightDistance(targPosition);//world normal
                        dir = !Vector3D.IsZero(dirDist) ? dirDist : dir;

                        Vector3D dirMinAlt = KeepMinAltitude(gravity);//world normal
                        dir = !Vector3D.IsZero(dirMinAlt) ? dirMinAlt : dir;
                    } else {
                        if (!initRandomMagneticDriveOnce) {
                            randomDir = Vector3D.Zero;
                            initRandomMagneticDriveOnce = true;
                        }
                        dir = MagneticDrive();//world normal
                        dir = MagneticDampeners(dir, myVelocity, gravity);//world normal

                        Vector3D dirAlt = KeepAltitude(isUnderControl, keepAltitude, gravity);//world normal
                        dir = !Vector3D.IsZero(dirAlt) ? dirAlt : dir;
                    }
                }
            }

            if (enemyEvasion && !isAutoPiloted && targFound) {
                Vector3D dirEva = EvadeEnemy(targOrientation, targVelVec, targPosition, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity);//world normal
                foreach (MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> target in targetsInfo) {//HitPosition, Velocity, Orientation, Position, EntityId
                    Vector3D evasionDir = EvadeEnemy(target.Item3, target.Item2, target.Item4, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity);//world normal
                    dirEva = SetResultVector(dirEva, evasionDir);
                }
                dir = !Vector3D.IsZero(dirEva) ? dirEva : dir;
            }

            if (obstaclesAvoidance && mySpeed > 10d && !isAutoPiloted && Vector3D.IsZero(landPosition)) {
                if (sensorDetectionOnce) {
                    SetSensorsExtend();
                    sensorDetectionOnce = false;
                }
                if (checkAllTicks) {
                    obstaclesCheckDelay = 1;
                    checkAllTicksCount++;
                    if (checkAllTicksCount == 200) {
                        checkAllTicks = false;
                        checkAllTicksCount = 0;
                    }
                }
                timeSinceLastRaycast += timeSinceLastRun;
                if (obstaclesCheckCount >= obstaclesCheckDelay) {
                    Vector3D stopDirection = CalculateStopDistance(myVelocity);//world
                    double stopDistance = stopDirection.Length();
                    RaycastStopPosition(stopDistance, stopDirection, myVelocity);//stopDir world normal

                    if (moddedSensor) { SetSensorsStopDistance((float)stopDistance, (float)mySpeed); }
                    SensorDetection();//sensorDir world normal

                    obstaclesCheckCount = 0;
                    timeSinceLastRaycast = 0d;
                }
                obstaclesCheckCount++;
                if (!Vector3D.IsZero(stopDir)) {
                    dir = stopDir;
                    checkAllTicks = true;
                }

                dir = !Vector3D.IsZero(sensorDir) ? sensorDir : dir;

            } else {
                if (!sensorDetectionOnce) {
                    timeSinceLastRaycast = 0d;
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

            if (!Vector3D.IsZero(gravity) && !Vector3D.IsZero(dir) && !isAutoPiloted) {
                dir = GravityCompensation(maxAccel.Length(), dir, gravity);//world normal
            }

            SetMagneticDrive(dir);
        }

        void ManageThrustersDrive(bool isUnderControl, Vector3D gravity, Vector3D myVelocity, double mySpeed) {
            Vector3D dir = Vector3D.Zero;
            double mass = CONTROLLER.CalculateShipMass().PhysicalMass;

            mass += mass / 100d * 75d;//TODO not good

            double acceleration;
            Vector3D stopDirection = CalculateStopVectorAndAccelerationByDirection(myVelocity, mass, out acceleration);
            double stopDistance = stopDirection.Length();

            if (follow) {
                if (initFollowOnce) {
                    distanceToKeep = Vector3D.Distance(targPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                    initFollowOnce = false;
                }
                dir = KeepSameDistance(targPosition, distanceToKeep);//world normal

                Vector3D dirMinAlt = KeepMinAltitude(gravity);//world normal
                dir = !Vector3D.IsZero(dirMinAlt) ? dirMinAlt : dir;
            } else {
                if (!initFollowOnce) {
                    distanceToKeep = 0d;
                    initFollowOnce = true;
                }
                if (autoCombat && !isUnderControl && targFound) {
                    initRandomMagneticDriveOnce = false;
                    RandomDrive(myVelocity);//world normal
                    dir = randomDir;
                    Vector3D dirEsc = Vector3D.Zero;
                    foreach (MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> target in targetsInfo) {//HitPosition, Velocity, Orientation, Position, EntityId
                        Vector3D escapeDir = Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - target.Item4);//world normal
                        dirEsc = SetResultVector(dirEsc, escapeDir);
                    }
                    dir = !Vector3D.IsZero(dirEsc) ? dirEsc : dir;

                    Vector3D dirDist = KeepRightDistance(targPosition);//world normal
                    dir = !Vector3D.IsZero(dirDist) ? dirDist : dir;

                    Vector3D dirMinAlt = KeepMinAltitude(gravity);//world normal
                    dir = !Vector3D.IsZero(dirMinAlt) ? dirMinAlt : dir;
                } else {
                    if (!initRandomMagneticDriveOnce) {
                        randomDir = Vector3D.Zero;
                        initRandomMagneticDriveOnce = true;
                    }
                    if (!Vector3D.IsZero(returnPosition)) {
                        initPositionalDriveOnce = false;
                        Vector3D pos = returnPosition + (Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - returnPosition) * stopDistance);
                        dir = Vector3D.Normalize(pos - CONTROLLER.CubeGrid.WorldVolume.Center);//world normal
                        dir = CalculateDriftCompensation(myVelocity, dir, acceleration);
                        dir = Vector3D.Normalize(dir);
                        if (Vector3D.Distance(returnPosition, CONTROLLER.CubeGrid.WorldVolume.Center) <= 5d) {
                            dir = Vector3D.Zero;
                            CONTROLLER.DampenersOverride = true;
                            if (mySpeed < 0.2d) {
                                returnPosition = Vector3D.Zero;
                            }
                        }
                    } else if (!Vector3D.IsZero(dockPosition)) {
                        if (initPositionalDriveOnce) {
                            foreach (IMyLandingGear block in LANDINGGEARS) {
                                block.AutoLock = false;
                            }
                            initPositionalDriveOnce = false;
                        }

                        RaycastDockPosition(dockOrientation.Translation, dockVelocity, Runtime.TimeSinceLastRun.TotalSeconds);
                        dockPosition = dockOrientation.Translation + (dockOrientation.Forward * 250d);

                        Vector3D pos = dockPosition + (Vector3D.Normalize(DOCKCONNECTOR.GetPosition() - dockPosition) * stopDistance);
                        dir = Vector3D.Normalize(pos - DOCKCONNECTOR.GetPosition());//world normal
                        dir = CalculateDriftCompensation(myVelocity, dir, acceleration);
                        dir = Vector3D.Normalize(dir);

                        //----------------------------------
                        Debug.DrawLine(DOCKCONNECTOR.GetPosition(), DOCKCONNECTOR.GetPosition() + stopDirection, Color.Red, thickness: 2f, onTop: true);
                        Debug.DrawLine(DOCKCONNECTOR.GetPosition(), DOCKCONNECTOR.GetPosition() + (dir * 500d), Color.Yellow, thickness: 2f, onTop: true);
                        Debug.DrawPoint(dockPosition, Color.Red, 10f, onTop: true);
                        Debug.DrawPoint(pos, Color.Yellow, 10f, onTop: true);
                        Debug.PrintHUD($"acceleration:{acceleration:0.####}, stopDistance:{stopDistance:0.####}");
                        //----------------------------------

                        if (Vector3D.Distance(dockPosition, DOCKCONNECTOR.GetPosition()) <= 20d) {
                            dir = Vector3D.Zero;
                            CONTROLLER.DampenersOverride = true;
                            if (mySpeed < 0.2d) {
                                dockPosition = Vector3D.Zero;
                                dockHitPosition = dockOrientation.Translation;
                            }
                        }
                    } else if (!Vector3D.IsZero(dockHitPosition)) {
                        RaycastDockPosition(dockOrientation.Translation, dockVelocity, Runtime.TimeSinceLastRun.TotalSeconds);
                        dockHitPosition = dockOrientation.Translation;

                        double pitchAngle, rollAngle, yawAngle;
                        GetRotationAnglesSimultaneous(dockOrientation.Up, dockOrientation.Forward, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
                        double yawSpeed = yawController.Control(yawAngle);
                        double pitchSpeed = pitchController.Control(pitchAngle);
                        double rollSpeed = rollController.Control(rollAngle);
                        ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);

                        Vector3D connectorPosition = DOCKCONNECTOR.GetPosition() + DOCKCONNECTOR.WorldMatrix.Forward * 2.6d;
                        Vector3D pos = dockHitPosition + (Vector3D.Normalize(connectorPosition - dockHitPosition) * stopDistance);
                        dir = Vector3D.Normalize(pos - connectorPosition);//world normal
                        dir = CalculateDriftCompensation(myVelocity, dir, acceleration);
                        dir = Vector3D.Normalize(dir);

                        //----------------------------------
                        Debug.DrawLine(DOCKCONNECTOR.GetPosition(), DOCKCONNECTOR.GetPosition() + stopDirection, Color.Red, thickness: 2f, onTop: true);
                        Debug.DrawLine(DOCKCONNECTOR.GetPosition(), DOCKCONNECTOR.GetPosition() + (dir * 500d), Color.Yellow, thickness: 2f, onTop: true);
                        Debug.DrawPoint(dockHitPosition, Color.Red, 10f, onTop: true);
                        Debug.DrawPoint(pos, Color.Yellow, 10f, onTop: true);
                        Debug.PrintHUD($"acceleration:{acceleration:0.####}, stopDistance:{stopDistance:0.####}");
                        //----------------------------------

                        if (Vector3D.Distance(dockHitPosition, connectorPosition) <= 0.5d) {
                            dir = Vector3D.Zero;
                            CONTROLLER.DampenersOverride = true;
                            if (mySpeed < 0.2d) {
                                DOCKCONNECTOR.Connect();
                                if (DOCKCONNECTOR.Status == MyShipConnectorStatus.Connected || failedConnectionCount >= 100) {
                                    foreach (IMyLandingGear block in LANDINGGEARS) {
                                        block.AutoLock = true;
                                    }
                                    dockHitPosition = Vector3D.Zero;
                                    dockOrientation = default(MatrixD);
                                    dockVelocity = Vector3D.Zero;
                                    if (recoverMagneticDrive) {
                                        magneticDrive = true;
                                        recoverMagneticDrive = false;
                                    }
                                    failedConnectionCount = 0;
                                } else {
                                    failedConnectionCount++;
                                }
                            }
                        }
                    } else if (!Vector3D.IsZero(landPosition)) {
                        initPositionalDriveOnce = false;
                        Vector3D pos = landPosition + (Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - landPosition) * stopDistance);
                        dir = Vector3D.Normalize(pos - CONTROLLER.CubeGrid.WorldVolume.Center);//world normal
                        dir = CalculateDriftCompensation(myVelocity, dir, acceleration);
                        dir = Vector3D.Normalize(dir);
                        if (Vector3D.Distance(landPosition, CONTROLLER.CubeGrid.WorldVolume.Center) <= 5d) {
                            dir = Vector3D.Zero;
                            CONTROLLER.DampenersOverride = true;
                            if (mySpeed < 0.2d) {
                                landPosition = Vector3D.Zero;
                            }
                        }
                    } else if (!Vector3D.IsZero(rangeFinderPosition)) {
                        initPositionalDriveOnce = false;
                        Vector3D pos = rangeFinderPosition + (Vector3D.Normalize(CONTROLLER.CubeGrid.WorldVolume.Center - rangeFinderPosition) * stopDistance);
                        dir = Vector3D.Normalize(pos - CONTROLLER.CubeGrid.WorldVolume.Center);//world normal
                        dir = CalculateDriftCompensation(myVelocity, dir, acceleration);
                        dir = Vector3D.Normalize(dir);
                        if (Vector3D.Distance(rangeFinderPosition, CONTROLLER.CubeGrid.WorldVolume.Center) <= 5d) {
                            dir = Vector3D.Zero;
                            CONTROLLER.DampenersOverride = true;
                            if (mySpeed < 0.2d) {
                                rangeFinderPosition = Vector3D.Zero;
                            }
                        }
                    } else {
                        if (!initPositionalDriveOnce) {
                            landPosition = Vector3D.Zero;
                            rangeFinderPosition = Vector3D.Zero;
                            returnPosition = Vector3D.Zero;
                            dockPosition = Vector3D.Zero;
                            dockHitPosition = Vector3D.Zero;
                            dockOrientation = default(MatrixD);
                            foreach (IMyLandingGear block in LANDINGGEARS) {
                                block.AutoLock = true;
                            }
                            initPositionalDriveOnce = true;
                        }
                    }
                }
            }

            if (enemyEvasion && targFound) {
                Vector3D dirEva = EvadeEnemy(targOrientation, targVelVec, targPosition, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity);//world normal
                foreach (MyTuple<Vector3D, Vector3D, MatrixD, Vector3D, long> target in targetsInfo) {//HitPosition, Velocity, Orientation, Position, EntityId
                    Vector3D evasionDir = EvadeEnemy(target.Item3, target.Item2, target.Item4, CONTROLLER.CubeGrid.WorldVolume.Center, myVelocity, gravity);//world normal
                    dirEva = SetResultVector(dirEva, evasionDir);
                }
                dir = !Vector3D.IsZero(dirEva) ? dirEva : dir;
            }

            if (obstaclesAvoidance && mySpeed > 10d && Vector3D.IsZero(landPosition) && Vector3D.IsZero(dockHitPosition) && Vector3D.IsZero(dockPosition)) {
                if (sensorDetectionOnce) {
                    SetSensorsExtend();
                    sensorDetectionOnce = false;
                }
                if (checkAllTicks) {
                    obstaclesCheckDelay = 1;
                    checkAllTicksCount++;
                    if (checkAllTicksCount == 200) {
                        checkAllTicks = false;
                        checkAllTicksCount = 0;
                    }
                }
                double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;
                timeSinceLastRaycast += timeSinceLastRun;
                if (obstaclesCheckCount >= obstaclesCheckDelay) {
                    RaycastStopPosition(stopDistance, stopDirection, myVelocity);//stopDir world normal

                    if (moddedSensor) { SetSensorsStopDistance((float)stopDistance, (float)mySpeed); }
                    SensorDetection();//sensorDir world normal

                    timeSinceLastRaycast = 0d;
                    obstaclesCheckCount = 0;
                }
                obstaclesCheckCount++;
                if (!Vector3D.IsZero(stopDir)) {
                    dir = stopDir;
                    checkAllTicks = true;
                }

                dir = !Vector3D.IsZero(sensorDir) ? sensorDir : dir;

            } else {
                if (!sensorDetectionOnce) {
                    timeSinceLastRaycast = 0d;
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

            if (!Vector3D.IsZero(gravity) && !Vector3D.IsZero(dir)) {
                dir = GravityCompensation(acceleration, dir, gravity);
            }

            SetThrustersDrive(dir, myVelocity);
        }

        void SetThrustersDrive(Vector3D direction, Vector3D myVelocity) {
            if (!Vector3D.IsZero(direction)) {
                thrustOverrideOnce = false;
                Vector3D position = Vector3D.Zero;
                if (!Vector3D.IsZero(returnPosition)) {
                    position = returnPosition;
                } else if (!Vector3D.IsZero(dockPosition)) {
                    position = dockPosition;
                } else if (!Vector3D.IsZero(dockHitPosition)) {
                    position = dockHitPosition;
                } else if (!Vector3D.IsZero(landPosition)) {
                    position = landPosition;
                } else if (!Vector3D.IsZero(rangeFinderPosition)) {
                    position = rangeFinderPosition;
                }
                if (!Vector3D.IsZero(position)) {
                    double distance = Vector3D.Distance(position, CONTROLLER.CubeGrid.WorldVolume.Center);
                    foreach (IMyThrust thuster in THRUSTERS) {
                        double dot = Vector3D.Dot(thuster.WorldMatrix.Backward, direction);//stopDirection
                        if (dot > 0.1d) {

                            //-------------------------------------------------------------------------------------------
                            //Debug.DrawPoint(thuster.GetPosition(), Color.Red, 4f, onTop: true);
                            //-------------------------------------------------------------------------------------------

                            if (distance < 100d && myVelocity.Length() >= lastVelocity.Length() && myVelocity.Length() > (2d + dockVelocity.Length())) {
                                thuster.ThrustOverridePercentage = 0f;
                            } else {
                                thuster.ThrustOverride = thuster.MaxEffectiveThrust * (float)dot;
                            }
                        } else {
                            thuster.ThrustOverridePercentage = 0f;
                        }
                    }
                    lastVelocity = myVelocity;
                } else {
                    foreach (IMyThrust thuster in THRUSTERS) {
                        double dot = Vector3D.Dot(thuster.WorldMatrix.Backward, direction);
                        if (dot > 0.1d) {
                            thuster.ThrustOverride = thuster.MaxEffectiveThrust * (float)dot;
                        } else {
                            thuster.ThrustOverridePercentage = 0f;
                        }
                    }
                }
            } else {
                if (!thrustOverrideOnce) {
                    thrustOverrideOnce = true;
                    foreach (IMyThrust thuster in THRUSTERS) {
                        thuster.ThrustOverridePercentage = 0f;
                    }
                }
            }
        }

        Vector3D CalculateStopVectorAndAccelerationByDirection(Vector3D worldDirection, double mass, out double accelerationByDirection) {
            Vector3D localDirection = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            Vector3D thrustSumVector = GetThrustByDirection(localDirection);
            Vector3D displacementVector = Vector3D.Zero;
            accelerationByDirection = 0d;
            if (!Vector3D.IsZero(thrustSumVector)) {
                thrustSumVector = Vector3D.TransformNormal(thrustSumVector, MatrixD.Transpose(CONTROLLER.WorldMatrix));
                accelerationByDirection = thrustSumVector.Length() / mass;
                for (int i = 0; i < 3; ++i) {
                    double thrustSum = thrustSumVector.GetDim(i);
                    Vector3D direction = baseDirection[i] * Math.Sign(thrustSum);
                    thrustSum = Math.Abs(thrustSum);
                    double acceleration = thrustSum / mass;
                    double relevantSpeed = Math.Abs(localDirection.GetDim(i));
                    double timeToStop = acceleration == 0d ? 0d : relevantSpeed / acceleration;
                    double distToStop = (relevantSpeed * timeToStop) - (0.5d * acceleration * timeToStop * timeToStop);
                    displacementVector += direction * distToStop;
                }
                displacementVector = Vector3D.TransformNormal(displacementVector, CONTROLLER.WorldMatrix);
            }
            return displacementVector;
        }

        Vector3D GetThrustByDirection(Vector3D localDirection) {
            Vector3D thrustSum = Vector3D.Zero;
            localDirection = Vector3D.Normalize(localDirection);
            for (int i = 0; i < 6; ++i) {
                Vector3D thrustDir = directions[i] * thrustSums[i];
                double dot = Vector3D.Dot(directions[i], localDirection);
                if (dot > 0d) {
                    double length = thrustDir.Length();
                    length *= dot;
                    thrustSum += Vector3D.Normalize(thrustDir) * length;
                }
            }
            return Vector3D.Rotate(thrustSum, CONTROLLER.WorldMatrix);
        }

        void CalculateBaseThrust() {
            MatrixD transposedWm = MatrixD.Transpose(CONTROLLER.WorldMatrix);
            thrustSums = new double[6];
            foreach (IMyThrust thrust in THRUSTERS) {
                Vector3D dirn = Vector3D.Rotate(thrust.WorldMatrix.Forward * thrust.MaxEffectiveThrust, transposedWm);
                if (dirn.X >= 0) {
                    thrustSums[0] += dirn.X;
                } else {
                    thrustSums[1] -= dirn.X;
                }
                if (dirn.Y >= 0) {
                    thrustSums[2] += dirn.Y;
                } else {
                    thrustSums[3] -= dirn.Y;
                }
                if (dirn.Z >= 0) {
                    thrustSums[4] += dirn.Z;
                } else {
                    thrustSums[5] -= dirn.Z;
                }
            }
        }

        Vector3D GravityCompensation(double acceleration, Vector3D desiredDirection, Vector3D gravity) {
            Vector3D directionNorm = SafeNormalize(desiredDirection);
            Vector3D gravityCompensationVec = -Rejection(gravity, desiredDirection);
            double diffSq = (acceleration * acceleration) - gravityCompensationVec.LengthSquared();
            if (diffSq < 0d) {// Impossible to hover
                return desiredDirection - gravity; // We will sink, but at least approach the target.
            }
            return (directionNorm * Math.Sqrt(diffSq)) + gravityCompensationVec;
        }

        Vector3D CalculateDriftCompensation(Vector3D velocity, Vector3D directHeading, double accel, double timeConstant = 0.5d, double maxDriftAngle = 60d) {
            if (directHeading.LengthSquared() == 0d) {
                return velocity;
            }
            if (Vector3D.Dot(velocity, directHeading) < 0d) {
                return directHeading;
            }
            if (velocity.LengthSquared() < 100d) {
                return directHeading;
            }
            Vector3D normalVelocity = Rejection(velocity, directHeading);
            Vector3D normal = SafeNormalize(normalVelocity);
            Vector3D parallel = SafeNormalize(directHeading);
            double normalAccel = Vector3D.Dot(normal, normalVelocity) / timeConstant;
            normalAccel = Math.Min(normalAccel, accel * Math.Sin(MathHelper.ToRadians(maxDriftAngle)));
            Vector3D normalAccelerationVector = normalAccel * normal;
            double parallelAccel = 0d;
            double diff = (accel * accel) - normalAccelerationVector.LengthSquared();
            if (diff > 0d) {
                parallelAccel = Math.Sqrt(diff);
            }
            return (parallelAccel * parallel) - (normal * normalAccel);
        }

        void Descend(Vector3D myVelocity, Vector3D gravity) {
            if (myVelocity.Length() > 5d) {
                CONTROLLER.DampenersOverride = true;
                foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
            } else {
                CONTROLLER.DampenersOverride = false;
                float mass = CONTROLLER.CalculateShipMass().PhysicalMass;
                double weight = mass * gravity.Length();
                int count = 0;
                gravity = Vector3D.Normalize(-gravity);
                foreach (IMyThrust block in THRUSTERS) {
                    double dot = Vector3D.Dot(block.WorldMatrix.Backward, gravity);
                    if (dot > 0.1d) {
                        count++;
                    }
                }
                weight /= count;
                weight = weight / 10d * 9d;
                foreach (IMyThrust block in THRUSTERS) {
                    double dot = Vector3D.Dot(block.WorldMatrix.Backward, gravity);
                    if (dot > 0.1d) {
                        block.ThrustOverride = (float)(weight * dot);
                    } else {
                        block.ThrustOverride = 0f;
                    }
                }
            }
            double altitude;
            CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            if (altitude < 30d && Vector3D.IsZero(myVelocity)) {
                CONTROLLER.DampenersOverride = true;
                descend = false;
                initDescend = false;
                if (recoverMagneticDrive) {
                    magneticDrive = true;
                    recoverMagneticDrive = false;
                }
                foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
            }
        }

        void IdleMagneticDrive() {
            SetMagneticDrive(Vector3D.Zero);
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
            if (!Vector3D.IsZero(dir)) {
                return Vector3D.TransformNormal(Vector3D.Normalize(dir), CONTROLLER.WorldMatrix);
            } else {
                return Vector3D.Zero;
            }
        }

        Vector3D MagneticDrive() {
            Vector3D direction = Vector3D.TransformNormal(CONTROLLER.MoveIndicator, CONTROLLER.WorldMatrix);
            return !Vector3D.IsZero(direction) ? Vector3D.Normalize(direction) : Vector3D.Zero;
        }

        Vector3D MagneticDampeners(Vector3D direction, Vector3D myVelocity, Vector3D gravity) {
            if (Vector3D.IsZero(gravity) && !CONTROLLER.DampenersOverride && direction.LengthSquared() == 0d) {
                return Vector3D.Zero;
            }
            direction = (direction * 104.38d) - myVelocity;
            if (Math.Abs(direction.X) < 2d) { direction.X = 0d; }
            if (Math.Abs(direction.Y) < 2d) { direction.Y = 0d; }
            if (Math.Abs(direction.Z) < 2d) { direction.Z = 0d; }
            return !Vector3D.IsZero(direction) ? Vector3D.Normalize(direction) : Vector3D.Zero;
        }

        void SetMagneticDrive(Vector3D dir) {
            double dot = Vector3D.Dot(CONTROLLER.WorldMatrix.Backward, dir);
            if (dot > 0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = true; }
                foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
            } else if (dot < -0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = true; }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
            }
            dot = Vector3D.Dot(CONTROLLER.WorldMatrix.Up, dir);
            if (dot > 0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = true; }
                foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
            } else if (dot < -0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = true; }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
            }
            dot = Vector3D.Dot(CONTROLLER.WorldMatrix.Right, dir);
            if (dot > 0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = true; }
                foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
            } else if (dot < -0.1d) {
                foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = true; }
            } else {
                foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
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

        void RandomDrive(Vector3D myVelocity) {
            if (magneticDrive) {
                if (randomCount >= 10) {
                    randomDir = Vector3D.Zero;
                    randomCount = 0;
                    float randomFloat;
                    randomFloat = (float)random.Next(-1, 2);
                    randomDir.X = randomFloat;
                    randomFloat = (float)random.Next(-1, 2);
                    randomDir.Y = randomFloat;
                    randomFloat = (float)random.Next(-1, 2);
                    randomDir.Z = randomFloat;
                    if (!Vector3D.IsZero(randomDir)) {
                        randomDir = Vector3D.TransformNormal(Vector3D.Normalize(randomDir), CONTROLLER.WorldMatrix);
                    }
                }
                randomCount++;
            } else {
                if (randomCount >= 50) {
                    randomDir = Vector3D.Zero;
                    randomCount = 0;
                    double angle = random.NextDouble() * Math.PI * 2d;
                    Vector3D perpendicular = Vector3D.CalculatePerpendicularVector(myVelocity);
                    MatrixD matrix = MatrixD.CreateFromDir(Vector3D.Normalize(myVelocity), Vector3D.Normalize(perpendicular));
                    matrix.Translation = CONTROLLER.CubeGrid.WorldVolume.Center;
                    randomDir = Math.Sin(angle) * matrix.Up + Math.Cos(angle) * matrix.Right;
                    randomDir = Vector3D.Normalize(randomDir);
                }
                randomCount++;
            }
        }

        void SensorDetection() {
            sensorDir = Vector3D.Zero;
            List<MyDetectedEntityInfo> entitiesA = new List<MyDetectedEntityInfo>();
            List<MyDetectedEntityInfo> entitiesB = new List<MyDetectedEntityInfo>();
            LEFTSENSOR.DetectedEntities(entitiesA);
            RIGHTSENSOR.DetectedEntities(entitiesB);
            if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                sensorDir.X = 0d;
            } else if (entitiesA.Count > 0) {
                sensorDir.X = 1d;
            } else if (entitiesB.Count > 0) {
                sensorDir.X = -1d;
            }
            entitiesA.Clear();
            entitiesB.Clear();
            UPSENSOR.DetectedEntities(entitiesA);
            DOWNSENSOR.DetectedEntities(entitiesB);
            if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                sensorDir.Y = 0d;
            } else if (entitiesA.Count > 0) {
                sensorDir.Y = -1d;
            } else if (entitiesB.Count > 0) {
                sensorDir.Y = 1d;
            }
            entitiesA.Clear();
            entitiesB.Clear();
            FORWARDSENSOR.DetectedEntities(entitiesA);
            BACKWARDSENSOR.DetectedEntities(entitiesB);
            if (entitiesA.Count > 0 && entitiesB.Count > 0) {
                sensorDir.Z = 0d;
            } else if (entitiesA.Count > 0) {
                sensorDir.Z = 1d;
            } else if (entitiesB.Count > 0) {
                sensorDir.Z = -1d;
            }
            if (!Vector3D.IsZero(sensorDir)) {
                sensorDir = Vector3D.TransformNormal(Vector3D.Normalize(sensorDir), CONTROLLER.WorldMatrix);
            }
        }

        Vector3D KeepSameDistance(Vector3D targPos, double distanceToKeep) {
            Vector3D dir = Vector3D.Zero;
            double minDistance = distanceToKeep - 50d;
            double maxDistance = distanceToKeep + 50d;
            Vector3D direction = targPos - CONTROLLER.CubeGrid.WorldVolume.Center;
            double distance = direction.Length();
            if (distance > maxDistance) {
                dir = Vector3D.Normalize(direction);
            } else if (distance < minDistance) {
                dir = -Vector3D.Normalize(direction);
            }
            return dir;
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
            if (distance > maxDistance) {
                dir = Vector3D.Normalize(direction);
            } else if (distance < minDistance) {
                dir = -Vector3D.Normalize(direction);
            }
            return dir;
        }

        Vector3D EvadeEnemy(MatrixD targOrientation, Vector3D targVel, Vector3D targPos, Vector3D myPosition, Vector3D myVelocity, Vector3D gravity) {
            Base6Directions.Direction closeDirection = CONTROLLER.WorldMatrix.GetClosestDirection(Vector3D.Normalize(targPos - CONTROLLER.CubeGrid.WorldVolume.Center));
            closeDirection = Base6Directions.GetFlippedDirection(closeDirection);
            Vector3D flippedDirection = CONTROLLER.WorldMatrix.GetDirectionVector(closeDirection);
            Base6Directions.Direction enemyForward = targOrientation.GetClosestDirection(flippedDirection);//CONTROLLER.WorldMatrix.Backward
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
            double angle = AngleBetween(enemyForwardVec, enemyAim) * rad2deg;//use dot?

            //---------------------------------------------------------------------------
            Debug.DrawLine(targPos, targPos + (enemyForwardVec * 2000d), Color.Orange, thickness: 2f, onTop: true);
            Debug.DrawLine(targPos, targPos + (Vector3D.Normalize(enemyAim) * 2000d), Color.Magenta, thickness: 2f, onTop: true);
            Debug.PrintHUD($"EvadeEnemy, angle:{angle:0.##}, safety:{4500d / distance:0.##}");
            //---------------------------------------------------------------------------

            if (angle < (4500d / distance)) {//not good, TODO 9000d if dot with forward and toEnemy isn't > 0.999d

                //---------------------------------------------------------------------------
                Debug.DrawPoint(targPos + (enemyForwardVec * distance), Color.Green, 4f, onTop: true);
                Debug.DrawPoint(CONTROLLER.CubeGrid.WorldVolume.Center, Color.Yellow, 4f, onTop: true);
                Debug.DrawLine(CONTROLLER.CubeGrid.WorldVolume.Center, targPos + (enemyForwardVec * distance), Color.Purple, thickness: 1f, onTop: true);
                //---------------------------------------------------------------------------

                Vector3D evadeDirection = CONTROLLER.CubeGrid.WorldVolume.Center - (targPos + (enemyForwardVec * distance));//toward my center
                return Vector3D.Normalize(evadeDirection);
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
            float a = (float)diffVelocity.LengthSquared() - (projectileSpeed * projectileSpeed);
            float b = 2f * (float)Vector3D.Dot(diffVelocity, toMe);
            float c = (float)toMe.LengthSquared();
            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - (4 * a * c)) / (2 * a);
            float t1 = p - q;
            float t2 = p + q;
            float t = t1 > t2 && t2 > 0 ? t2 : t1;
            Vector3D predictedPosition = myPosition + (diffVelocity * t);
            return predictedPosition;
        }

        Vector3D BulletDrop(double distanceFromTarget, double projectileMaxSpeed, Vector3D desiredDirection, Vector3D gravity) {
            double timeToTarget = distanceFromTarget / projectileMaxSpeed;
            desiredDirection -= 0.5 * gravity * timeToTarget * timeToTarget;
            return desiredDirection;
        }

        Vector3D UpdateAcceleration(double timeStep, Vector3D myVelocity) {
            Vector3D acceleration = (myVelocity - lastVelocity) / timeStep;
            lastVelocity = myVelocity;
            acceleration = Vector3D.TransformNormal(acceleration, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            for (int i = 0; i < 3; ++i) {
                double component = acceleration.GetDim(i);
                if (component >= 0d) {
                    if (component > maxAccel.GetDim(i)) {
                        maxAccel.SetDim(i, component);
                    }
                } else {
                    component = Math.Abs(component);
                    if (component > minAccel.GetDim(i)) {
                        minAccel.SetDim(i, component);
                    }
                }
            }
            return Vector3D.TransformNormal(acceleration, CONTROLLER.WorldMatrix);
        }

        Vector3D CalculateStopDistance(Vector3D myVelocity) {
            myVelocity = Vector3D.TransformNormal(myVelocity, MatrixD.Transpose(CONTROLLER.WorldMatrix));
            Vector3D stopDistance = Vector3D.Zero;
            for (int i = 0; i < 3; ++i) {
                double velocityComponent = myVelocity.GetDim(i);
                double accel = velocityComponent >= 0d ? minAccel.GetDim(i) : -maxAccel.GetDim(i);
                double stopDistComponent = velocityComponent * velocityComponent / (2d * accel);
                stopDistance.SetDim(i, stopDistComponent);
            }
            return Vector3D.TransformNormal(stopDistance, CONTROLLER.WorldMatrix);
        }

        void RaycastStopPosition(double stopDistance, Vector3D stopDirection, Vector3D myVelocity) {
            Vector3D normalizedVelocity = Vector3D.Normalize(stopDirection);
            IMyCameraBlock lidar = GetLidarByDirection(normalizedVelocity);
            Vector3D scanPos = CONTROLLER.CubeGrid.WorldVolume.Center + (myVelocity * timeSinceLastRaycast);
            double dist = Vector3D.Distance(scanPos, CONTROLLER.CubeGrid.WorldVolume.Center);
            stopDistance += dist;

            //----------------------------------
            Debug.DrawPoint(CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance), Color.Blue, 5f, onTop: true);
            Debug.DrawLine(CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance), Color.Cyan, thickness: 2f, onTop: true);
            Debug.PrintHUD($"raycast stopDistance:{stopDirection.Length():0.##}, multiplied:{stopDistance:0.##}");
            //----------------------------------

            MyDetectedEntityInfo entityInfo = lidar.Raycast(CONTROLLER.CubeGrid.WorldVolume.Center + (normalizedVelocity * stopDistance));
            stopDir = Vector3D.Zero;
            if (!entityInfo.IsEmpty()) {
                //dir = -normalizedVelocity;
                if (CONTROLLER.WorldMatrix.Forward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = 1d;//go backward
                } else if (CONTROLLER.WorldMatrix.Backward.Dot(normalizedVelocity) > 0d) {
                    stopDir.Z = -1d;
                }
                if (CONTROLLER.WorldMatrix.Up.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = -1d;//go down
                } else if (CONTROLLER.WorldMatrix.Down.Dot(normalizedVelocity) > 0d) {
                    stopDir.Y = 1d;
                }
                if (CONTROLLER.WorldMatrix.Left.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = 1d;//go right
                } else if (CONTROLLER.WorldMatrix.Right.Dot(normalizedVelocity) > 0d) {
                    stopDir.X = -1d;
                }
                if (!Vector3D.IsZero(stopDir)) {
                    stopDir = Vector3D.TransformNormal(Vector3D.Normalize(stopDir), CONTROLLER.WorldMatrix);
                }
            }
        }

        void RaycastDockPosition(Vector3D dockHitPos, Vector3D dockVel, double timeSinceLastRun) {
            dockHitPos += dockVel * (float)timeSinceLastRun;
            Vector3D toTarget = Vector3D.Normalize(dockHitPos - CONTROLLER.CubeGrid.WorldVolume.Center);
            IMyCameraBlock lidar = GetLidarByDirection(toTarget);
            MyDetectedEntityInfo TARGET = lidar.Raycast(dockHitPos + (toTarget * 1d));//TODO
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                if (TARGET.Type == MyDetectedEntityType.LargeGrid || TARGET.Type == MyDetectedEntityType.SmallGrid) {
                    MatrixD orientation = TARGET.Orientation;
                    Base6Directions.Direction closeDirection = CONTROLLER.WorldMatrix.GetClosestDirection(Vector3D.Normalize(TARGET.HitPosition.Value - CONTROLLER.CubeGrid.WorldVolume.Center));
                    closeDirection = Base6Directions.GetFlippedDirection(closeDirection);
                    Vector3D flippedDirection = CONTROLLER.WorldMatrix.GetDirectionVector(closeDirection);
                    Base6Directions.Direction forward = orientation.GetClosestDirection(flippedDirection);//CONTROLLER.WorldMatrix.Backward
                    Base6Directions.Direction perpendicular = Base6Directions.GetPerpendicular(forward);
                    Vector3D forwardVec = orientation.GetDirectionVector(forward);
                    Vector3D perpendicularVec = orientation.GetDirectionVector(perpendicular);
                    dockOrientation = MatrixD.CreateFromDir(forwardVec, perpendicularVec);
                    dockOrientation.Translation = TARGET.HitPosition.Value;
                    dockVelocity = TARGET.Velocity;
                }
            }
        }

        IMyCameraBlock GetLidarByDirection(Vector3D normalizedDir) {
            Base6Directions.Direction direction = Base6Directions.GetClosestDirection(Vector3D.TransformNormal(normalizedDir, MatrixD.Transpose(CONTROLLER.WorldMatrix)));
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
                default:
                    break;
            }
            return lidar;
        }

        Vector3D KeepMinAltitude(Vector3D gravity) {
            Vector3D dir = Vector3D.Zero;
            if (!Vector3D.IsZero(gravity)) {
                double altitude;
                CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                if (altitude < 100d) {
                    dir = Vector3D.Normalize(-gravity);
                }
            }
            return dir;
        }

        Vector3D KeepAltitude(bool isUnderControl, bool keepAltitude, Vector3D gravity) {
            Vector3D dir = Vector3D.Zero;
            if (!isUnderControl && !Vector3D.IsZero(gravity) && keepAltitude) {
                if (keepAltitudeOnce) {
                    hoverPosition = CONTROLLER.CubeGrid.WorldVolume.Center;
                    keepAltitudeOnce = false;
                }
                double altitude;
                CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
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
                        dir = Vector3D.Normalize(-gravity);
                    } else if (altitude > altitudeToKeep + 30d) {
                        dir = Vector3D.Normalize(gravity);
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

        Vector3D SetResultVector(Vector3D direction, Vector3D otherDirection) {
            if (!Vector3D.IsZero(otherDirection)) {
                direction = !Vector3D.IsZero(direction) ? Vector3D.Normalize(direction + otherDirection) : otherDirection;
            }
            return direction;
        }

        void ManageWaypoints(bool isUnderControl, Vector3D myVelocity) {
            if (targFound) {
                if (!targetFoundOnce) {
                    targetFoundOnce = true;
                    rangeFinderPosition = Vector3D.Zero;
                    landPosition = Vector3D.Zero;
                    descend = false;
                    initDescend = false;
                    aimTarget = false;
                    sunAlign = false;
                    if (recoverMagneticDrive) {
                        magneticDrive = true;
                        recoverMagneticDrive = false;
                    }
                    if (REMOTE.IsAutoPilotEnabled) {
                        REMOTE.SetAutoPilotEnabled(false);
                        REMOTE.SetCollisionAvoidance(true);
                    }
                    if (returnOnce && Vector3D.IsZero(returnPosition) && !isUnderControl) {
                        returnPosition = REMOTE.CubeGrid.WorldVolume.Center;
                        returnOnce = false;
                    }
                }
            } else {
                targetFoundOnce = false;
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
                if (!Vector3D.IsZero(landPosition) && magneticDrive) {
                    Vector3D stopDirection = CalculateStopDistance(myVelocity);
                    double stopDistance = stopDirection.Length();
                    if (Vector3D.Distance(landPosition, REMOTE.CubeGrid.WorldVolume.Center) < stopDistance) {
                        REMOTE.SetAutoPilotEnabled(false);
                        REMOTE.ClearWaypoints();
                        REMOTE.SetCollisionAvoidance(true);
                        CONTROLLER.DampenersOverride = true;
                        initDescend = true;
                    }
                    if (myVelocity.Length() < 2d && initDescend) {
                        initDescend = false;
                        landPosition = Vector3D.Zero;
                        descend = true;
                        magneticDrive = false;
                        recoverMagneticDrive = true;
                    }
                }
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
                    landPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(-gravity) * 100d);
                    if (magneticDrive) {
                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(landPosition, "landPosition");
                        REMOTE.SetAutoPilotEnabled(true);
                        REMOTE.SetCollisionAvoidance(false);
                    }
                }
            }
        }

        void Dock() {
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            double raycastDistance = lidar.AvailableScanRange < 5000d ? lidar.AvailableScanRange : 5000d;
            MyDetectedEntityInfo TARGET = lidar.Raycast(raycastDistance);
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                if (TARGET.Type == MyDetectedEntityType.LargeGrid || TARGET.Type == MyDetectedEntityType.SmallGrid) {
                    MatrixD orientation = TARGET.Orientation;
                    Base6Directions.Direction closeDirection = CONTROLLER.WorldMatrix.GetClosestDirection(Vector3D.Normalize(TARGET.HitPosition.Value - CONTROLLER.CubeGrid.WorldVolume.Center));
                    closeDirection = Base6Directions.GetFlippedDirection(closeDirection);
                    Vector3D flippedDirection = CONTROLLER.WorldMatrix.GetDirectionVector(closeDirection);
                    Base6Directions.Direction forward = orientation.GetClosestDirection(flippedDirection);//CONTROLLER.WorldMatrix.Backward
                    Base6Directions.Direction perpendicular = Base6Directions.GetPerpendicular(forward);
                    Vector3D forwardVec = orientation.GetDirectionVector(forward);
                    Vector3D perpendicularVec = orientation.GetDirectionVector(perpendicular);
                    dockOrientation = MatrixD.CreateFromDir(forwardVec, perpendicularVec);
                    dockOrientation.Translation = TARGET.HitPosition.Value;
                    dockPosition = dockOrientation.Translation + (dockOrientation.Forward * 250d);
                    dockVelocity = TARGET.Velocity;
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
            if (AngleBetween(CONTROLLER.WorldMatrix.Forward, Vector3D.Normalize(aimDirection)) * rad2deg <= tolerance) {//use dot?
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

        bool IsNotFriendly(MyRelationsBetweenPlayerAndBlock relationship) {
            return relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != MyRelationsBetweenPlayerAndBlock.Owner;
        }

        float Smallest(float rotorAngle, float b) {
            return Math.Abs(rotorAngle) > Math.Abs(b) ? b : rotorAngle;
        }

        double AngleBetween(Vector3D a, Vector3D b) {//returns radians
            return Vector3D.IsZero(a) || Vector3D.IsZero(b)
                ? 0d
                : Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        Vector3D SafeNormalize(Vector3D a) {
            if (Vector3D.IsZero(a)) { return Vector3D.Zero; }
            return Vector3D.IsUnit(ref a) ? a : Vector3D.Normalize(a);
        }

        Vector3D Rejection(Vector3D a, Vector3D b) {//reject a on b
            return Vector3D.IsZero(a) || Vector3D.IsZero(b) ? Vector3D.Zero : a - (a.Dot(b) / b.LengthSquared() * b);
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
            LANDINGGEARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(LANDINGGEARS, block => block.CustomName.Contains("[CRX] Landing Gear"));

            LCDSAFETYDAMPENERS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Safety Dampeners") as IMyTextPanel;
            LCDSUNALIGN = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Sun Align") as IMyTextPanel;
            LCDMAGNETICDRIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Magnetic Drive") as IMyTextPanel;
            LCDAUTOCOMBAT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Combat") as IMyTextPanel;
            LCDOBSTACLES = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Obstacles") as IMyTextPanel;
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
            DOCKCONNECTOR = GridTerminalSystem.GetBlockWithName("[CRX] Connector Dock D") as IMyShipConnector;
        }

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, globalTimestep);
            rollController = new PID(5d, 0d, 5d, globalTimestep);
        }

        void ManagePIDControllers(Vector3D gravity) {
            if (targFound) {
                Vector3D toTarget = Vector3D.Normalize(targHitPos - CONTROLLER.WorldVolume.Center);
                double angle = AngleBetween(CONTROLLER.WorldMatrix.Forward, toTarget) * rad2deg;//use dot?
                angle = MathHelper.Clamp(angle, 0d, 10d);
                UpdatePIDControllers(angle);
            } else if (!Vector3D.IsZero(dockHitPosition)) {
                double angle = AngleBetween(CONTROLLER.WorldMatrix.Up, dockOrientation.Forward) * rad2deg;//use dot?
                angle = MathHelper.Clamp(angle, 0d, 10d);
                UpdatePIDControllers(angle);
            } else if (!Vector3D.IsZero(gravity)) {
                double angle = AngleBetween(CONTROLLER.WorldMatrix.Down, Vector3D.Normalize(gravity)) * rad2deg;//use dot?
                angle = MathHelper.Clamp(angle, 0d, 10d);
                UpdatePIDControllers(angle);
            } else {
                double mouseYaw = CONTROLLER.RotationIndicator.Y;
                double mousePitch = CONTROLLER.RotationIndicator.X;
                double mouseRoll = CONTROLLER.RollIndicator;
                if (mouseYaw != 0d || mousePitch != 0d || mouseRoll != 0d) {
                    if (updateOnce) {
                        UpdatePIDControllers(10d);
                        updateOnce = false;
                    }
                } else {
                    if (!updateOnce) {
                        UpdatePIDControllers(1d);
                        updateOnce = true;
                    }
                }
            }

            //----------------------------------------------
            Debug.PrintHUD($"yawPID:{yawController.KP:0.####}, {yawController.KI:0.####}, {yawController.KD:0.####}");
            Debug.PrintHUD($"pitchPID:{pitchController.KP:0.####}, {pitchController.KI:0.####}, {pitchController.KD:0.####}");
            Debug.PrintHUD($"rollPID:{rollController.KP:0.####}, {rollController.KI:0.####}, {rollController.KD:0.####}");
            //----------------------------------------------
        }

        void UpdatePIDControllers(double aim) {
            yawController.KP = aim;
            yawController.KD = aim;
            pitchController.KP = aim;
            pitchController.KD = aim;
            rollController.KP = aim;
            rollController.KD = aim;
        }

        public class PID {
            public double KP { get; set; }
            public double KI { get; set; }
            public double KD { get; set; }

            double timeStep = 0;
            double inverseTimeStep = 0;
            double errorSum = 0;
            double lastError = 0;
            bool firstRun = true;

            public double Value { get; private set; }

            public PID(double _kP, double _kI, double _kD, double _timeStep) {
                KP = _kP;
                KI = _kI;
                KD = _kD;
                timeStep = _timeStep;
                inverseTimeStep = 1 / timeStep;
            }

            protected virtual double GetIntegral(double currentError, double errorSum, double timeStep) {
                return errorSum + (currentError * timeStep);
            }

            public void Update(double _kP, double _kI, double _kD) {
                KP = _kP;
                KI = _kI;
                KD = _kD;
                Reset();
            }

            public double Control(double error) {
                double errorDerivative = (error - lastError) * inverseTimeStep;//Compute derivative term
                if (firstRun) {
                    errorDerivative = 0;
                    firstRun = false;
                }
                errorSum = GetIntegral(error, errorSum, timeStep);//Get error sum
                lastError = error;//Store this error as last error
                Value = (KP * error) + (KI * errorSum) + (KD * errorDerivative);//Construct output
                return Value;
            }

            public double Control(double error, double _timeStep) {
                if (_timeStep != timeStep) {
                    timeStep = _timeStep;
                    inverseTimeStep = 1 / timeStep;
                }
                return Control(error);
            }

            public void Reset() {
                errorSum = 0;
                lastError = 0;
                firstRun = true;
            }
        }

        public class DecayingIntegralPID : PID {
            readonly double decayRatio;

            public DecayingIntegralPID(double kP, double kI, double kD, double timeStep, double _decayRatio) : base(kP, kI, kD, timeStep) {
                decayRatio = _decayRatio;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                //return errorSum = errorSum * (1.0 - _decayRatio) + currentError * timeStep;
                return (errorSum * (1.0 - decayRatio)) + (currentError * timeStep);
            }
        }

        public class ClampedIntegralPID : PID {
            readonly double upperBound;
            readonly double lowerBound;

            public ClampedIntegralPID(double kP, double kI, double kD, double timeStep, double _lowerBound, double _upperBound) : base(kP, kI, kD, timeStep) {
                upperBound = _upperBound;
                lowerBound = _lowerBound;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                errorSum += currentError * timeStep;
                return Math.Min(upperBound, Math.Max(errorSum, lowerBound));
            }
        }

        public class BufferedIntegralPID : PID {
            readonly Queue<double> integralBuffer = new Queue<double>();
            readonly int bufferSize = 0;

            public BufferedIntegralPID(double kP, double kI, double kD, double timeStep, int _bufferSize) : base(kP, kI, kD, timeStep) {
                bufferSize = _bufferSize;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                if (integralBuffer.Count == bufferSize) {
                    integralBuffer.Dequeue();
                }
                integralBuffer.Enqueue(currentError * timeStep);
                return integralBuffer.Sum();
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
                if (program == null) {
                    throw new Exception("Pass `this` into the API, not null.");
                }
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
