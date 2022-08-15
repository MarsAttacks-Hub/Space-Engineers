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

        //MULTI
        bool creative = true;//define if is playing creative mode or not
        bool magneticDrive = true;//enable/disable magnetic drive
        bool autoFire = true;//enable/disable automatic fire
        bool idleThrusters = false;//enable/disable thrusters
        bool togglePB = true;//enable/disable PB
        bool sunAlign = false;//enable/disable sun chase on space
        bool logger = true;//enable/disable logging

        bool hasCenter = true;
        bool scanCenter = false;
        bool scanFudge = false;
        bool fudgeVectorSwitch = false;
        bool aimTarget = false;
        bool readyToFire = false;
        bool joltReady = true;
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
        bool sunAlignOnce = true;
        bool unlockSunAlignOnce = true;
        bool toggleThrustersOnce = false;
        bool updateOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool activateTargeterOnce = false;
        bool decoyRanOnce = false;
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
        int rocketDelay = 1;
        int assaultDelay = 1;
        int artilleryDelay = 1;
        int railgunsDelay = 1;
        int smallRailgunsDelay = 1;
        double timeSinceLastLock = 0d;
        double fudgeFactor = 5d;
        double movePitch = .01;
        double moveYaw = .01;
        double rangeFinderDiameter = 0d;
        double rangeFinderDistance = 0d;
        float prevSunPower = 0f;
        int weaponType = 2;//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
        int missedScan = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;
        int checkGunsCount;
        int sendMessageCount = 0;
        string rangeFinderName = "";

        readonly float rotorVel = 29 * (float)(Math.PI / 30);//rpsOverRpm
        readonly float syncSpeed = 1 * (float)(Math.PI / 30);

        const float globalTimestep = 10.0f / 60.0f;
        const float circle = (float)(2 * Math.PI);
        const double rad2deg = 180 / Math.PI;

        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        public List<IMyUserControllableGun> GATLINGS = new List<IMyUserControllableGun>();
        public List<IMyUserControllableGun> AUTOCANNONS = new List<IMyUserControllableGun>();
        public List<Gun> ROCKETS = new List<Gun>();
        public List<Gun> ARTILLERY = new List<Gun>();
        public List<Gun> RAILGUNS = new List<Gun>();
        public List<Gun> SMALLRAILGUNS = new List<Gun>();
        public List<Gun> ASSAULT = new List<Gun>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> UPTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> DOWNTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> LEFTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> RIGHTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> FORWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> BACKWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyMotorStator> ROTORS = new List<IMyMotorStator>();
        public List<IMyMotorStator> ROTORSINV = new List<IMyMotorStator>();
        public List<IMyShipMergeBlock> MERGESMINUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSY = new List<IMyShipMergeBlock>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyLightingBlock> LIGHTS = new List<IMyLightingBlock>();
        public List<IMyInventory> inventories = new List<IMyInventory>();

        IMyShipController CONTROLLER;
        IMyRemoteControl REMOTE;
        IMyThrust UPTHRUST;
        IMyThrust DOWNTHRUST;
        IMyThrust LEFTTHRUST;
        IMyThrust RIGHTTHRUST;
        IMyThrust FORWARDTHRUST;
        IMyThrust BACKWARDTHRUST;
        IMySolarPanel SOLAR;
        IMyProgrammableBlock SHOOTERPB;
        IMyTextPanel LCDSUNALIGN;
        IMyTextPanel LCDMAGNETICDRIVE;
        IMyTextPanel LCDIDLETHRUSTERS;
        IMyTextPanel LCDAUTOFIRE;
        IMyTextPanel LCDCREATIVE;

        PID yawController;
        PID pitchController;
        PID rollController;

        MyDetectedEntityInfo targetInfo;
        readonly Random random = new Random();
        Vector3D rangeFinderPosition;
        Vector3D landPosition;
        public IMyBroadcastListener BROADCASTLISTENER;

        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType autocannonAmmo = MyItemType.MakeAmmo("AutocannonClip");
        readonly MyItemType assaultAmmo = MyItemType.MakeAmmo("MediumCalibreAmmo");
        readonly MyItemType artilleryAmmo = MyItemType.MakeAmmo("LargeCalibreAmmo");
        readonly MyItemType railgunAmmo = MyItemType.MakeAmmo("LargeRailgunAmmo");
        readonly MyItemType smallRailgunAmmo = MyItemType.MakeAmmo("SmallRailgunAmmo");

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            InitPIDControllers();
            SetGunsDelay();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[MULTI]");
            foreach (IMyCameraBlock cam in LIDARS) { cam.EnableRaycast = true; }
            if (LCDSUNALIGN != null) { LCDSUNALIGN.BackgroundColor = new Color(0, 0, 0); }
            if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = idleThrusters ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            if (LCDAUTOFIRE != null) { LCDAUTOFIRE.BackgroundColor = autoFire ? new Color(20, 10, 0) : new Color(0, 0, 0); }
            if (LCDCREATIVE != null) { LCDCREATIVE.BackgroundColor = creative ? new Color(20, 10, 0) : new Color(0, 0, 0); }
            Me.GetSurface(0).BackgroundColor = togglePB ? new Color(20, 0, 0) : new Color(0, 0, 0);
        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;

                Vector3D gravity = CONTROLLER.GetNaturalGravity();
                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg, gravity);
                    if (arg == "RangeFinder") { return; } else if (!togglePB) { return; }
                }

                if (aimTarget) {
                    bool aligned;
                    AimAtTarget(rangeFinderPosition, 0.1d, out aligned);
                    if (!aligned) { return; }
                }

                Vector3D myVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;

                bool targetFound = TurretsDetection(targetInfo.IsEmpty());
                bool isTargetEmpty = targetInfo.IsEmpty();

                bool isAutopiloted = REMOTE.IsAutoPilotEnabled;
                bool needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || isAutopiloted
                    || !Vector3D.IsZero(gravity) || myVelocity.Length() > 2d || !isTargetEmpty;
                SendBroadcastControllerMessage(needControl);

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

                GetBroadcastMessages();

                ManagePIDControllers(isTargetEmpty);

                ManageMagneticDrive(needControl, isAutopiloted, idleThrusters, gravity, myVelocity);

                ManageWaypoints(isTargetEmpty);

                if (!isTargetEmpty) {
                    if (missedScan > 8) {//if lidars or turrets doesn't detect a enemy for some time reset the script
                        ResetTargeter();
                        return;
                    }

                    ActivateTargeter();//things to run once when a enemy is detected

                    double lastLock = timeSinceLastLock + timeSinceLastRun;

                    Vector3D targetVelocity = targetInfo.Velocity;
                    Vector3D targetHitPosition = targetInfo.HitPosition.Value;

                    Vector3D aimDirection = Vector3D.Normalize(targetInfo.Position - CONTROLLER.CubeGrid.WorldVolume.Center);
                    if (AngleBetween(CONTROLLER.WorldMatrix.Forward, aimDirection) * rad2deg <= 43d) {
                        if (!targetFound) {
                            targetFound = AcquireTarget(lastLock, targetInfo.Position, targetVelocity, targetInfo.HitPosition.Value);
                        }
                    }

                    LockOnTarget(lastLock, targetHitPosition, targetVelocity, gravity, myVelocity, LIDARS[0].GetPosition());

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

                SyncGuns(timeSinceLastRun);

                if (logger) {
                    if (sendMessageCount >= 10) {
                        SendBroadcastLogRangeFinderMessage();
                        SendBroadcastLogTargetMessage();
                        sendMessageCount = 0;
                    }
                    sendMessageCount++;
                }
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument, Vector3D gravity) {
            switch (argument) {
                case "Lock": AcquireTarget(globalTimestep, Vector3D.Zero, Vector3D.Zero, Vector3D.Zero); break;
                case "Clear": ResetTargeter(); return;
                case "RangeFinder":
                    if (Vector3D.IsZero(gravity)) {
                        RangeFinder();
                    } else {
                        Land(gravity);
                    }
                    sendMessageCount = 10;
                    break;
                case "AimTarget": if (!Vector3D.IsZero(rangeFinderPosition)) { aimTarget = true; }; break;
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
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = false; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(25, 0, 100); }
                    } else {
                        foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                        if (LCDIDLETHRUSTERS != null) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }
                    }
                    break;
                case "ToggleAutoFire":
                    autoFire = !autoFire;
                    if (LCDAUTOFIRE != null) { LCDAUTOFIRE.BackgroundColor = autoFire ? new Color(20, 10, 0) : new Color(0, 0, 0); }
                    break;
                case "ToggleCreative":
                    creative = !creative;
                    if (LCDCREATIVE != null) { LCDCREATIVE.BackgroundColor = creative ? new Color(20, 10, 0) : new Color(0, 0, 0); }
                    break;
                case "PBOn":
                    togglePB = true;
                    Me.GetSurface(0).BackgroundColor = new Color(20, 0, 0);
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    InitMulti();
                    break;
                case "PBOff":
                    togglePB = false;
                    Me.GetSurface(0).BackgroundColor = new Color(0, 0, 0);
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    ShutDownMulti();
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

        void SendBroadcastControllerMessage(bool isControlled) {
            MyTuple<string, bool> tuple = MyTuple.Create("isControlled", isControlled);
            IGC.SendBroadcastMessage("[POWERMANAGER]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void SendBroadcastLogTargetMessage() {
            if (targetInfo.IsEmpty() && targetInfo.HitPosition.HasValue) {
                var immArray = ImmutableArray.CreateBuilder<MyTuple<
                    MyTuple<string, Vector3D, Vector3D, Vector3D>,
                    string
                >>();

                Vector3D targetVelocity = targetInfo.Velocity;
                var tuple = MyTuple.Create(
                    MyTuple.Create(targetInfo.Name, targetInfo.HitPosition.Value, targetInfo.Position, targetVelocity),
                    ""
                );

                immArray.Add(tuple);
                IGC.SendBroadcastMessage("[LOGGER]", immArray.ToImmutable(), TransmissionDistance.ConnectedConstructs);
            }
        }

        void SendBroadcastLogRangeFinderMessage() {
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

            var immArray = ImmutableArray.CreateBuilder<MyTuple<
                    MyTuple<string, int, int, double, double, double>,
                    MyTuple<Vector3D, string, double, double>
                    >>();

            var tuple = MyTuple.Create(
                MyTuple.Create(timeRemaining, maxJump, currentJump, totJumpPercent, currentStoredPower, maxStoredPower),
                MyTuple.Create(rangeFinderPosition, rangeFinderName, rangeFinderDistance, rangeFinderDiameter)
                );
            immArray.Add(tuple);
            IGC.SendBroadcastMessage("[LOGGER]", immArray.ToImmutable(), TransmissionDistance.ConnectedConstructs);
        }

        void ManageMagneticDrive(bool needControl, bool isAutoPiloted, bool idleThrusters, Vector3D gravity, Vector3D myVelocity) {
            if (magneticDrive && needControl) {
                Vector3D dir = Vector3D.Zero;
                if (initMagneticDriveOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    //sunAlign = false;
                    initMagneticDriveOnce = false;
                }

                SyncRotors();

                if (isAutoPiloted) {
                    dir = AutoMagneticDrive(dir);
                } else {
                    if (!initAutoMagneticDriveOnce) {
                        foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                        initAutoMagneticDriveOnce = true;
                    }
                    Matrix mtrx;
                    dir = MagneticDrive(out mtrx);
                    dir = MagneticDampeners(dir, myVelocity, gravity, mtrx);
                    IdleThrusters(dir, idleThrusters);
                }

                SetPower(dir);

            } else {
                if (!initMagneticDriveOnce) {
                    IdleMagneticDrive(idleThrusters);
                    initMagneticDriveOnce = true;
                }
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
            if (Vector3D.IsZero(gravity) && !CONTROLLER.DampenersOverride && direction.LengthSquared() == 0f) {
                return Vector3D.Zero;
            }
            Vector3D vel = myVelocity;
            vel = Vector3D.Transform(vel, MatrixD.Transpose(CONTROLLER.WorldMatrix.GetOrientation()));
            vel = direction * 105d - Vector3D.Transform(vel, mtrx);//maxSpeed
            if (Math.Abs(vel.X) < 2d) { vel.X = 0d; }//minSpeed
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
                rotor.TargetVelocityRad = asyncAngle > 0f ? rotorVel - syncSpeed : rotorVel + syncSpeed;
            }
            foreach (IMyMotorStator rotor in ROTORSINV) {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angleInv, Smallest(rotorAngle - angleInv + circle, rotorAngle - angleInv - circle));
                rotor.TargetVelocityRad = asyncAngle > 0f ? -rotorVel - syncSpeed : -rotorVel + syncSpeed;
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
            Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * 250d);
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
            scanPosition += Vector3D.Normalize(scanPosition - lidar.GetPosition()) * 250d;
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
            } else if (REMOTE.IsUnderControl) {
                userRoll = (double)REMOTE.RollIndicator;
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

        bool IsNotFriendly(MyRelationsBetweenPlayerAndBlock relationship) {
            return relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != MyRelationsBetweenPlayerAndBlock.Owner;
        }

        void ResetTargeter() {
            UnlockGyros();
            TurnAlarmOff();
            ResetGuns();
            targetInfo = default(MyDetectedEntityInfo);
            activateTargeterOnce = false;
            fudgeFactor = 5d;
            scanFudge = false;
            missedScan = 0;
            timeSinceLastLock = 0d;
            hasCenter = true;
            scanCenter = false;
        }

        void ActivateTargeter() {
            if (!activateTargeterOnce) {
                TurnAlarmOn();
                activateTargeterOnce = true;
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

        bool AimAtTarget(Vector3D targetPos, double tolerance, out bool aligned) {
            aligned = false;
            Vector3D aimDirection = targetPos - CONTROLLER.CubeGrid.WorldVolume.Center;//get normalized later
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
            if (AngleBetween(CONTROLLER.WorldMatrix.Forward, aimDirection) * rad2deg <= tolerance) {
                aligned = true;
                UnlockGyros();
            }
            return aligned;
        }

        void RangeFinder() {
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            if (lidar == null) { return; }
            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);//TODO
            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue) {
                foreach (IMySoundBlock block in ALARMS) { block.Play(); }
                if (TARGET.Type == MyDetectedEntityType.Planet) {
                    double planetRadius = Vector3D.Distance(TARGET.Position, TARGET.HitPosition.Value);
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 43000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Planet");
                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
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
            }
        }

        void ManageWaypoints(bool isTargetEmpty) {
            if (!isTargetEmpty) {
                if (REMOTE.IsAutoPilotEnabled) {
                    REMOTE.SetAutoPilotEnabled(false);
                }
            } else {
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(rangeFinderPosition)) {
                    if (Vector3D.Distance(rangeFinderPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.SetAutoPilotEnabled(false);
                        rangeFinderPosition = Vector3D.Zero;
                        rangeFinderName = "";
                        rangeFinderDistance = 0d;
                        rangeFinderDiameter = 0d;
                    }
                }
                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(landPosition)) {
                    if (Vector3D.Distance(landPosition, REMOTE.CubeGrid.WorldVolume.Center) < 50d) {
                        REMOTE.ClearWaypoints();
                        REMOTE.SetAutoPilotEnabled(false);
                        landPosition = Vector3D.Zero;
                    }
                }
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

        float Smallest(float rotorAngle, float b) {
            return Math.Abs(rotorAngle) > Math.Abs(b) ? b : rotorAngle;
        }

        void InitMulti() {
            /*IMyTextSurfaceProvider cockpit = CONTROLLER as IMyTextSurfaceProvider;
            int surfaceCount = cockpit.SurfaceCount;
            for (int i = 0; i < surfaceCount; i++) {
                if (i == 0 || i == 4 || i == 2) {
                    cockpit.GetSurface(i).WriteText("");
                }
            }
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Target")
            || block.CustomName.Contains("[CRX] LCD RangeFinder"));
            foreach (IMyTextPanel block in panels) {
                block.WriteText("");
            }
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Auto Missiles Toggle")
            || block.CustomName.Contains("[CRX] LCD Auto Switch Guns Toggle") || block.CustomName.Contains("[CRX] LCD Sequence Guns Toggle")
            || block.CustomName.Contains("[CRX] LCD Toggle Safety Dampeners") || block.CustomName.Contains("[CRX] LCD Toggle Stabilizer")
            || block.CustomName.Contains("[CRX] LCD Toggle Auto Combat") || block.CustomName.Contains("[CRX] LCD Toggle Obstacles")
            || block.CustomName.Contains("[CRX] LCD Toggle Collisions") || block.CustomName.Contains("[CRX] LCD Toggle Evasion")
            || block.CustomName.Contains("[CRX] LCD Toggle Keep Altitude") || block.CustomName.Contains("[CRX] LCD Toggle Modded Sensor")
            || block.CustomName.Contains("[CRX] LCD Toggle Close Combat") );
            foreach (IMyTextPanel block in panels) {
                block.BackgroundColor = new Color(0, 0, 0);
            }
            panels.Clear();*/
            List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, block => block.CustomName.Contains("[CRX] Sensor"));
            foreach (IMySensorBlock sensor in sensors) {
                sensor.BackExtend = 0.1f;
                sensor.BottomExtend = 0.1f;
                sensor.FrontExtend = 0.1f;
                sensor.LeftExtend = 0.1f;
                sensor.RightExtend = 0.1f;
                sensor.TopExtend = 0.1f;
            }
            sensors.Clear();
            IMyProgrammableBlock pb = GridTerminalSystem.GetBlockWithName("[CRX] PB Painter") as IMyProgrammableBlock;
            pb.Enabled = false;
            pb = GridTerminalSystem.GetBlockWithName("[CRX] PB Navigator") as IMyProgrammableBlock;
            pb.Enabled = false;
            List<IMyFunctionalBlock> buttons = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(buttons, block => block.CustomName.Contains("[CRX] Button Toggle Auto Missiles")
                        || block.CustomName.Contains("[CRX] Button Toggle Auto Switch Guns") || block.CustomName.Contains("[CRX] Button Toggle Sequence Guns")
                        || block.CustomName.Contains("[CRX] Button Toggle Auto Combat") || block.CustomName.Contains("[CRX] Button Toggle Close Combat")
                        || block.CustomName.Contains("[CRX] Button Toggle Collisions") || block.CustomName.Contains("[CRX] Button Toggle Safety Dampeners")
                        || block.CustomName.Contains("[CRX] Button Toggle Evasion") || block.CustomName.Contains("[CRX] Button Toggle Obstacles")
                        || block.CustomName.Contains("[CRX] Button Toggle Stabilizer") || block.CustomName.Contains("[CRX] Button Toggle Modded Sensor")
                        || block.CustomName.Contains("[CRX] Button Toggle Keep Altitude"));
            foreach (IMyFunctionalBlock block in buttons) {
                block.Enabled = false;
            }
            buttons.Clear();
        }

        void ShutDownMulti() {
            IMyProgrammableBlock pb = GridTerminalSystem.GetBlockWithName("[CRX] PB Painter") as IMyProgrammableBlock;
            pb.Enabled = true;
            pb = GridTerminalSystem.GetBlockWithName("[CRX] PB Navigator") as IMyProgrammableBlock;
            pb.Enabled = true;
            List<IMyFunctionalBlock> buttons = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(buttons, block => block.CustomName.Contains("[CRX] Button Toggle Auto Missiles")
                        || block.CustomName.Contains("[CRX] Button Toggle Auto Switch Guns") || block.CustomName.Contains("[CRX] Button Toggle Sequence Guns")
                        || block.CustomName.Contains("[CRX] Button Toggle Auto Combat") || block.CustomName.Contains("[CRX] Button Toggle Close Combat")
                        || block.CustomName.Contains("[CRX] Button Toggle Collisions") || block.CustomName.Contains("[CRX] Button Toggle Safety Dampeners")
                        || block.CustomName.Contains("[CRX] Button Toggle Evasion") || block.CustomName.Contains("[CRX] Button Toggle Obstacles")
                        || block.CustomName.Contains("[CRX] Button Toggle Stabilizer") || block.CustomName.Contains("[CRX] Button Toggle Modded Sensor")
                        || block.CustomName.Contains("[CRX] Button Toggle Keep Altitude"));
            foreach (IMyFunctionalBlock block in buttons) {
                block.Enabled = true;
            }
            buttons.Clear();
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
            JUMPERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(JUMPERS, block => block.CustomName.Contains("[CRX] Jump"));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains("[CRX] Turret"));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, block => block.CustomName.Contains("[CRX] Alarm Lidar"));
            LIGHTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(LIGHTS, b => b.CustomName.Contains("[CRX] Rotating Light"));
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
            List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains("[CRX] Solar"));
            foreach (IMySolarPanel solar in SOLARS) { if (solar.IsFunctional && solar.Enabled && solar.IsWorking) { SOLAR = solar; } }
            REMOTE = GridTerminalSystem.GetBlockWithName("[CRX] Controller Remote Reference") as IMyRemoteControl;
            CONTROLLER = GridTerminalSystem.GetBlockWithName("[CRX] Controller Cockpit 1") as IMyShipController;
            SHOOTERPB = GridTerminalSystem.GetBlockWithName("[CRX] PB Shooter") as IMyProgrammableBlock;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Thrusters") as IMyTextPanel;
            LCDSUNALIGN = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Sun Align") as IMyTextPanel;
            LCDMAGNETICDRIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Magnetic Drive") as IMyTextPanel;
            LCDAUTOFIRE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Auto Fire") as IMyTextPanel;
            LCDCREATIVE = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Creative") as IMyTextPanel;
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

        void ManageGuns(double timeSinceLastRun, Vector3D trgtPos) {
            if (autoFire) {
                double distanceFromTarget = Vector3D.Distance(trgtPos, CONTROLLER.CubeGrid.WorldVolume.Center);
                if (distanceFromTarget <= 2000d) {
                    maxRangeOnce = true;
                    if (!decoyRanOnce && distanceFromTarget < 800d) {//TODO
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
                            foreach (Gun gun in RAILGUNS) { gun.Shoot(timeSinceLastRun); }
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
                            foreach (Gun gun in ARTILLERY) { gun.Shoot(timeSinceLastRun); }
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
                            foreach (Gun gun in SMALLRAILGUNS) { gun.Shoot(timeSinceLastRun); }
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
                            foreach (Gun gun in ASSAULT) { gun.Shoot(timeSinceLastRun); }
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
                            foreach (Gun gun in ROCKETS) { gun.Shoot(timeSinceLastRun); }
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

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, globalTimestep);
            rollController = new PID(5d, 0d, 5d, globalTimestep);
        }

        void ManagePIDControllers(bool isTargetEmpty) {
            if (!isTargetEmpty) {//mySpeed > 10d
                if (updateOnce) {
                    UpdatePIDControllers(5d, 0d, 5d, 5d, 0d, 5d, 5d, 0d, 5d);
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


    }
}
