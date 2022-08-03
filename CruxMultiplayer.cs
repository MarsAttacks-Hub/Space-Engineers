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



        const float globalTimestep = 10.0f / 60.0f;
        const float circle = (float)(2 * Math.PI);
        const double rad2deg = 180 / Math.PI;

        readonly float rotorVel = 29 * (float)(Math.PI / 30);//rpsOverRpm
        readonly float syncSpeed = 1 * (float)(Math.PI / 30);

        bool magneticDrive = true;
        bool idleThrusters = false;

        bool hasCenter = true;
        bool scanCenter = false;
        bool scanFudge = false;
        bool fudgeVectorSwitch = false;
        bool aimTarget = false;
        bool sunChasing = false;
        bool sunChaseOnce = true;
        bool unlockSunChaseOnce = true;
        bool toggleThrustersOnce = false;
        bool updateOnce = true;
        bool initMagneticDriveOnce = true;
        bool initAutoMagneticDriveOnce = true;
        bool activateTargeterOnce = false;

        double timeSinceLastLock = 0d;
        double fudgeFactor = 5d;
        double movePitch = .01;
        double moveYaw = .01;

        float prevSunPower = 0f;

        int weaponType = 2;//0 None - 1 Rockets - 2 Gatlings - 3 Autocannon - 4 Assault - 5 Artillery - 6 Railguns - 7 Small Railguns
        int fudgeCount = 0;
        int sunAlignmentStep = 0;
        int selectedSunAlignmentStep;

        //public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyMotorStator> ROTORS = new List<IMyMotorStator>();
        public List<IMyMotorStator> ROTORSINV = new List<IMyMotorStator>();
        public List<IMyShipMergeBlock> MERGESPLUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSZ = new List<IMyShipMergeBlock>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyLightingBlock> LIGHTS = new List<IMyLightingBlock>();
        public List<IMyThrust> UPTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> DOWNTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> LEFTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> RIGHTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> FORWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> BACKWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();

        IMyShipController CONTROLLER = null;
        IMyRemoteControl REMOTE;
        IMySolarPanel SOLAR;
        IMyTextPanel LCDSUNCHASER;
        IMyTextPanel LCDMAGNETICDRIVE;
        IMyTextPanel LCDIDLETHRUSTERS;
        IMyThrust UPTHRUST;
        IMyThrust DOWNTHRUST;
        IMyThrust LEFTTHRUST;
        IMyThrust RIGHTTHRUST;
        IMyThrust FORWARDTHRUST;
        IMyThrust BACKWARDTHRUST;

        PID yawController;
        PID pitchController;
        PID rollController;

        MyDetectedEntityInfo targetInfo;
        readonly Random random = new Random();
        Vector3D rangeFinderPosition;



        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            InitPIDControllers();
            if (LCDSUNCHASER != null) { LCDSUNCHASER.BackgroundColor = new Color(0, 0, 0); }
            if (LCDMAGNETICDRIVE != null) { LCDMAGNETICDRIVE.BackgroundColor = magneticDrive ? new Color(25, 0, 100) : new Color(0, 0, 0); }

        }

        public void Main(string arg) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;

                Vector3D gravity = CONTROLLER.GetNaturalGravity();
                ProcessArgument(arg, gravity);
                if (arg == "RangeFinder") { return; }

                if (aimTarget) {
                    bool aligned;
                    AimAtTarget(rangeFinderPosition, 0.1d, out aligned);
                    if (!aligned) { return; }
                }

                Vector3D myVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;
                double mySpeed = myVelocity.Length();

                bool targetFound = TurretsDetection(targetInfo.IsEmpty());
                bool isTargetEmpty = targetInfo.IsEmpty();

                bool isAutopiloted = REMOTE.IsAutoPilotEnabled;
                bool needControl = CONTROLLER.IsUnderControl || REMOTE.IsUnderControl || isAutopiloted
                    || !Vector3D.IsZero(gravity) || mySpeed > 2d || !isTargetEmpty;

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

                ManagePIDControllers(mySpeed);

                ManageMagneticDrive(needControl, isAutopiloted, idleThrusters, gravity, myVelocity);

                ManageWaypoints(isTargetEmpty);

                if (!isTargetEmpty) {
                    if (fudgeCount > 8) {//if lidars or turrets doesn't detect a enemy for some time reset the script
                        ResetTargeter();
                        return;
                    }

                    ActivateTargeter();//things to run once when a enemy is detected

                    double lastLock = timeSinceLastLock + timeSinceLastRun;

                    Vector3D targetVelocity = targetInfo.Velocity;
                    Vector3D targetHitPosition = targetInfo.HitPosition.Value;

                    if (!targetFound) {
                        targetFound = AcquireTarget(lastLock, targetInfo.Position, targetVelocity, targetInfo.HitPosition.Value, isTargetEmpty);
                    }

                    LockOnTarget(lastLock, targetHitPosition, targetVelocity, gravity, myVelocity, LIDARS[0].GetPosition());

                    if (targetFound) { timeSinceLastLock = 0; } else { timeSinceLastLock += timeSinceLastRun; }
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
                Setup();
            }
        }

        void ProcessArgument(string argument, Vector3D gravity) {
            switch (argument) {
                case "Lock": AcquireTarget(globalTimestep, Vector3D.Zero, Vector3D.Zero, Vector3D.Zero, true); break;
                case "Clear": ResetTargeter(); return;
                case "RangeFinder":
                    if (Vector3D.IsZero(gravity)) {
                        RangeFinder();
                    }
                    //else { Land(gravity); }//TODO
                    break;
                case "AimTarget": if (!Vector3D.IsZero(rangeFinderPosition)) { aimTarget = true; }; break;
                case "SunChaserToggle":
                    sunChasing = !sunChasing;
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
            }
        }

        void ManageMagneticDrive(bool needControl, bool isAutoPiloted, bool idleThrusters, Vector3D gravity, Vector3D myVelocity) {
            if (magneticDrive && needControl) {
                Vector3D dir = Vector3D.Zero;
                if (initMagneticDriveOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.Enabled = true; }
                    //sunChasing = false;
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
                /*if (tickCount == tickDelay) {//TODO
                    if (controlDampeners) {
                        DeadMan(IsPiloted(true), mySpeed);
                    }
                    tickCount = 0;
                }
                tickCount++;*/
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

        bool AcquireTarget(double timeSinceLastRun, Vector3D trgPstn, Vector3D trgVl, Vector3D trgHitPstn, bool isTargetEmpty) {
            bool targetFound = false;
            if (isTargetEmpty) {//case argLock
                if (!Vector3D.IsZero(trgHitPstn)) {
                    targetFound = ScanTarget(trgHitPstn, trgVl, timeSinceLastRun);
                } else {
                    targetFound = ScanTarget(Vector3D.Zero, Vector3D.Zero, 0d);
                }
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

        bool ScanTarget(Vector3D trgPstn, Vector3D trgVl, double timeSinceLastRun) {
            bool targetFound = false;
            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
            MyDetectedEntityInfo entityInfo;
            if (!Vector3D.IsZero(trgPstn)) {
                trgPstn += (Vector3D.Normalize(trgVl) * timeSinceLastRun);//TODO
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
            var perpVector1 = Vector3D.CalculatePerpendicularVector(targetDirection);
            var perpVector2 = Vector3D.Cross(perpVector1, targetDirection);
            if (!Vector3D.IsUnit(ref perpVector2)) {
                perpVector2.Normalize();
            }
            var randomVector = (2.0 * random.NextDouble() - 1.0) * perpVector1 + (2.0 * random.NextDouble() - 1.0) * perpVector2;
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
                aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 958.21f, refPosition, myVelocity);
                if (!Vector3D.IsZero(gravity)) {
                    aimDirection = BulletDrop(distanceFromTarget, 958.21f, aimDirection, gravity);
                }
            } else {
                switch (weaponType) {
                    case 0://Jolt
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 958.21f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 958.21f, aimDirection, gravity);
                        }
                        break;
                    case 1://Rockets
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 200f, refPosition, myVelocity);
                        break;
                    case 2://Gatlings
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 400f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 400f, aimDirection, gravity);
                        }
                        break;
                    case 3://Autocannon
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 400f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 400f, aimDirection, gravity);
                        }
                        break;
                    case 4://Assault
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 500f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 500f, aimDirection, gravity);
                        }
                        break;
                    case 5://Artillery
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 500f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 500f, aimDirection, gravity);
                        }
                        break;
                    case 6://Railguns
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 2000f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 2000f, aimDirection, gravity);
                        }
                        break;
                    case 7://Small Railguns
                        aimDirection = ComputeInterceptWithLeading(targetHitPos, targetVelocity, 1000f, refPosition, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 1000f, aimDirection, gravity);
                        }
                        break;
                    default://none
                        aimDirection = targetHitPos - refPosition;//normalize?
                        break;
                }
            }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle); //double rollSpeed = rollController.Control(rollAngle);
            //double userRoll = 0d;
            /*foreach (IMyShipController cntrllr in CONTROLLERS) {//TODO CONTROLLER
                if (cntrllr.IsUnderControl) {
                    userRoll = (double)cntrllr.RollIndicator;
                    break;
                }
            }*/
            double userRoll = (double)CONTROLLER.RollIndicator;//TODO
            if (userRoll != 0d) {
                userRoll = userRoll < 0d ? MathHelper.Clamp(userRoll, -10d, -2d) : MathHelper.Clamp(userRoll, 2d, 10d);
            }
            if (userRoll == 0d) {
                userRoll = rollController.Control(rollAngle);
            } else {
                userRoll = rollController.Control(userRoll);
            }
            ApplyGyroOverride(pitchSpeed, yawSpeed, userRoll, GYROS, CONTROLLER.WorldMatrix);
            //TODO
            //Vector3D forwardVec = CONTROLLER.WorldMatrix.Forward;
            //double angle = AngleBetween(forwardVec, aimDirection);
            //if (angle * rad2deg <= 1d) { readyToFire = true; } else { readyToFire = false; }
        }

        Vector3D ComputeInterceptWithLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D refPosition, Vector3D myVelocity) {
            Vector3D aimPosition = GetPredictedTargetPosition(refPosition, targetPosition, targetVelocity, projectileSpeed, myVelocity);
            return aimPosition - CONTROLLER.CubeGrid.WorldVolume.Center;//normalize?
        }

        Vector3D GetPredictedTargetPosition(Vector3D refPosition, Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, Vector3D myVelocity) {
            Vector3D toTarget = targetPosition - refPosition;
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

        bool AimAtTarget(Vector3D targetPos, double tolerance, out bool aligned) {
            aligned = false;
            Vector3D aimDirection = targetPos - CONTROLLER.CubeGrid.WorldVolume.Center;//normalize?
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
                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                } else if (TARGET.Type == MyDetectedEntityType.Asteroid) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Asteroid");
                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)distance; }
                } else if (IsNotFriendly(TARGET.Relationship)) {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 3000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    rangeFinderPosition = safeJumpPosition;
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                } else {
                    Vector3D safeJumpPosition = TARGET.HitPosition.Value + (Vector3D.Normalize(lidar.GetPosition() - TARGET.HitPosition.Value) * 1000d);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);
                    if (JUMPERS.Count != 0) { JUMPERS[0].JumpDistanceMeters = (float)Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition); }
                    rangeFinderPosition = safeJumpPosition;
                }
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

        bool IsNotFriendly(MyRelationsBetweenPlayerAndBlock relationship) {
            return relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != MyRelationsBetweenPlayerAndBlock.Owner;
        }

        void TurnAlarmOn() {
            foreach (IMySoundBlock block in ALARMS) { block.Play(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = true; }
        }

        void TurnAlarmOff() {
            foreach (IMySoundBlock block in ALARMS) { block.Stop(); }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = false; }
        }

        void ResetTargeter() {
            UnlockGyros();
            TurnAlarmOff();
            //ResetGuns();
            //ClearLogs();
            targetInfo = default(MyDetectedEntityInfo);
            //selectedMissile = 1;
            activateTargeterOnce = false;
            //autoMissilesCounter = autoMissilesDelay + 1;
            //missilesLoaded = false;
            fudgeFactor = 5d;
            scanFudge = false;
            fudgeCount = 0;
            timeSinceLastLock = 0d;
            hasCenter = true;
            scanCenter = false;
            /*foreach (var id in MissileIDs) {
                if (!id.Value.Contains(commandLost)) {
                    SendMissileUnicastMessage(commandLost, id.Key, Vector3D.Zero, Vector3D.Zero);
                }
            }*/
            //SendBroadcastTargetMessage(false, Vector3D.Zero, Vector3D.Zero, default(MatrixD), Vector3D.Zero, 0);
        }

        void ActivateTargeter() {
            if (!activateTargeterOnce) {
                TurnAlarmOn();
                activateTargeterOnce = true;
                scanFudge = false;
                fudgeCount = 0;
                fudgeFactor = 5d;
                timeSinceLastLock = 0d;
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
                    }
                }
                /*if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(landPosition)) {
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
                }*/
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

        void GetBlocks() {

        }

        void InitPIDControllers() {
            yawController = new PID(5d, 0d, 5d, 0d, -0d, globalTimestep);
            pitchController = new PID(5d, 0d, 5d, 0d, -0d, globalTimestep);
            rollController = new PID(5d, 0d, 5d, 0d, -0d, globalTimestep);
        }

        void ManagePIDControllers(double mySpeed) {
            if (mySpeed > 10d) {
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
