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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //TODO add orbital function
        //NAVIGATOR

        readonly string controllersName = "[CRX] Controller";
        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string jumpersName = "[CRX] Jump";
        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string lcdsRangeFinderName = "[CRX] LCD RangeFinder";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string debugPanelName = "[CRX] Debug";
        readonly string turretsName = "[CRX] Turret";
        readonly string targeterName = "[CRX] PB Targeter";
        readonly string decoyName = "[CRX] PB Decoy";
        readonly string magneticDriveName = "[CRX] PB Magnetic Drive";
        readonly string managerName = "[CRX] PB Manager";

        readonly string sectionTag = "RangeFinderSettings";
        readonly string cockpitRangeFinderKey = "cockpitRangeFinderSurface";

        const string argRangeFinder = "RangeFinder";
        const string argAimTarget = "AimTarget";
        const string argChangeSafetyOffset = "ChangeSafetyOffset";
        const string argIncreaseJump = "IncreaseJump";
        const string argDecreaseJump = "DecreaseJump";

        const string argMDGyroStabilizeOff = "StabilizeOff";
        const string argMDGyroStabilizeOn = "StabilizeOn";
        const string argSunchaseOff = "SunchaseOff";
        const string argUnlockFromTarget = "Clear";
        const string argLaunchDecoy = "Launch";

        const float globalTimestep = 10.0f / 60.0f;
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 0.1;//degrees

        readonly double escapeDistance = 250d;
        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;
        readonly double aimP = 1d;
        readonly double aimI = 0d;
        readonly double aimD = 1d;
        readonly double integralWindupLimit = 0d;
        readonly int escapeDelay = 10;
        readonly double targetStopDistance = 150d;
        readonly double escapeStopDistance = 30d;
        readonly double returnStopDistance = 50d;
        readonly int impactDetectionDelay = 5;

        int impactDetectionCount = 5;
        int escapeCount = 10;
        int cockpitRangeFinderSurface = 4;
        bool aimTarget = false;
        double maxScanRange = 0d;
        int planetSelector = 0;
        string selectedPlanet = "";
        double planetAtmosphereRange = 0d;
        bool MDOn = false;
        bool MDOff = false;
        bool runMDOnce = false;
        bool sunChaseOff = false;
        bool returnOnce = true;

        Vector3D targetPosition = Vector3D.Zero;
        Vector3D returnPosition = Vector3D.Zero;
        Vector3D escapePosition = Vector3D.Zero;

        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        IMyRemoteControl REMOTE;
        IMyProgrammableBlock MAGNETICDRIVEPB;
        IMyProgrammableBlock MANAGERPB;
        IMyProgrammableBlock TARGETERPB;
        IMyProgrammableBlock DECOYPB;

        PID yawController;
        PID pitchController;
        PID rollController;

        readonly MyIni myIni = new MyIni();

        MyDetectedEntityInfo targetInfo;

        public StringBuilder jumpersLog = new StringBuilder("");
        public StringBuilder lidarsLog = new StringBuilder("");
        public StringBuilder targetLog = new StringBuilder("");

        public Dictionary<string, double> planetsDict = new Dictionary<string, double>()
        {
           {"Earth", 42860d},
           {"Moon", 2678d},
           {"Mars ", 39311d},
           {"Alien planet", 39870d}
        };

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup()
        {
            GetBlocks();

            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }

            maxScanRange = LIDARS[0].RaycastDistanceLimit;

            planetAtmosphereRange = planetsDict.ElementAt(0).Value;
            selectedPlanet = planetsDict.ElementAt(0).Key;

            REMOTE = REMOTES[0];

            InitPIDControllers();
        }

        public void Main(string arg)
        {
            try
            {
                Echo($"REMOTES:{REMOTES.Count}");
                Echo($"COCKPITS:{COCKPITS.Count}");
                Echo($"JUMPERS:{JUMPERS.Count}");
                Echo($"LIDARS:{LIDARS.Count}");
                Echo($"GYROS:{GYROS.Count}");
                Echo($"ALARMS:{ALARMS.Count}");
                Echo($"SURFACES:{SURFACES.Count}");

                if (!string.IsNullOrEmpty(arg))
                {
                    ProcessArgument(arg);
                }

                if (aimTarget)
                {
                    if (!runMDOnce)
                    {
                        if (MAGNETICDRIVEPB != null)
                        {
                            if (MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                            {
                                MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                            }
                        }
                        if (MANAGERPB != null)
                        {
                            if (MANAGERPB.CustomData.Contains("SunChaser=true"))
                            {
                                sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                            }
                        }
                        runMDOnce = true;
                    }
                    if (!MDOff && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                    {
                        MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                    }
                    if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true"))
                    {
                        sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                    }

                    AimAtTarget();
                }
                else
                {
                    if (runMDOnce)
                    {
                        if (MAGNETICDRIVEPB != null)
                        {
                            if (MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                            {
                                MDOn = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOn);
                            }
                        }
                        runMDOnce = false;
                    }
                    if (!MDOn && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                    {
                        MDOn = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOn);
                    }
                }

                if (!IsPiloted())
                {
                    TurretsImpactDetection();
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(targetPosition))
                {
                    double dist = Vector3D.Distance(targetPosition, REMOTE.GetPosition());
                    if (dist < targetStopDistance)
                    {
                        REMOTE.SetAutoPilotEnabled(false);
                        targetPosition = Vector3D.Zero;
                    }
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(escapePosition))
                {
                    double dist = Vector3D.Distance(escapePosition, REMOTE.GetPosition());
                    if (dist < escapeStopDistance)
                    {
                        REMOTE.SetAutoPilotEnabled(false);
                        escapePosition = Vector3D.Zero;
                    }
                }

                if (REMOTE.IsAutoPilotEnabled && !Vector3D.IsZero(returnPosition) && Vector3D.IsZero(escapePosition))
                {
                    double dist = Vector3D.Distance(returnPosition, REMOTE.GetPosition());
                    if (dist < returnStopDistance)
                    {
                        REMOTE.SetAutoPilotEnabled(false);
                        returnPosition = Vector3D.Zero;
                        returnOnce = true;
                    }
                }

                ReadLidarInfos();
                ReadJumpersInfos();

                WriteInfo();
            }
            catch (Exception e)
            {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
                if (DEBUG != null)
                {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
            }
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argRangeFinder: RangeFinder(); break;
                case argChangeSafetyOffset:
                    planetSelector++;
                    if (planetSelector > planetsDict.Count())
                    {
                        planetSelector = 0;
                    }
                    planetAtmosphereRange = planetsDict.ElementAt(planetSelector).Value;
                    selectedPlanet = planetsDict.ElementAt(planetSelector).Key;
                    break;
                case argIncreaseJump: IncreaseJumpDistance(); break;
                case argDecreaseJump: DecreaseJumpDistance(); break;
                case argAimTarget: if (!Vector3D.IsZero(targetPosition)) { aimTarget = true; }; break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit)
        {
            if (!cockpit.CustomData.Contains(sectionTag))
            {
                cockpit.CustomData += $"[{sectionTag}]\n{cockpitRangeFinderKey}={cockpitRangeFinderSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);

            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitRangeFinderKey).ToString()))
            {
                cockpitRangeFinderSurface = myIni.Get(sectionTag, cockpitRangeFinderKey).ToInt32();

                SURFACES.Add(cockpit.GetSurface(cockpitRangeFinderSurface));
            }
        }

        void RangeFinder()
        {
            targetLog.Clear();

            IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);

            MyDetectedEntityInfo TARGET = lidar.Raycast(lidar.AvailableScanRange);

            if (!TARGET.IsEmpty() && TARGET.HitPosition.HasValue)
            {
                foreach (var block in ALARMS)
                {
                    block.Play();
                }
                if (TARGET.Type == MyDetectedEntityType.Planet)
                {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * planetAtmosphereRange);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, selectedPlanet);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
                    if (maxDistance != 0 && distance != 0)
                    {
                        JUMPERS[0].SetValueFloat("JumpDistance", (float)(distance / maxDistance * 100d));
                    }

                    targetPosition = safeJumpPosition;

                    targetLog.Append("Safe Dist. for: ").Append(selectedPlanet).Append("\n");

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Atmo. Dist.: ").Append(distance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");

                    double targetRadius = Vector3D.Distance(TARGET.Position, hitPosition);
                    targetLog.Append("Radius: ").Append(targetRadius.ToString("0.0")).Append("\n");

                    double targetGroundDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Ground Dist.: ").Append(targetGroundDistance.ToString("0.0")).Append("\n");

                    double targetAtmoHeight = Vector3D.Distance(hitPosition, safeJumpPosition);
                    targetLog.Append("Atmo. Height: ").Append(targetAtmoHeight.ToString("0.0")).Append("\n");
                }
                else if (TARGET.Type == MyDetectedEntityType.Asteroid)
                {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, "Asteroid");

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
                    if (maxDistance != 0d && distance != 0d)
                    {
                        JUMPERS[0].SetValueFloat("JumpDistance", (float)(distance / maxDistance * 100d));
                    }

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Asteroid:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Dist.: ").Append(distance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
                else if (IsNotFriendly(TARGET.Relationship))
                {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * enemySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
                    if (maxDistance != 0d && distance != 0d)
                    {
                        JUMPERS[0].SetValueFloat("JumpDistance", (float)(distance / maxDistance * 100d));
                    }

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");

                    double targetDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Dist: ").Append(targetDistance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
                else
                {
                    Vector3D hitPosition = TARGET.HitPosition.Value;
                    Vector3D safeJumpPosition = hitPosition - (Vector3D.Normalize(hitPosition - lidar.GetPosition()) * friendlySafeDistance);

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(safeJumpPosition, TARGET.Name);

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, safeJumpPosition);
                    double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
                    if (maxDistance != 0d && distance != 0d)
                    {
                        JUMPERS[0].SetValueFloat("JumpDistance", (float)(distance / maxDistance * 100d));
                    }

                    targetPosition = safeJumpPosition;

                    string safeJumpGps = $"GPS:Safe Jump Pos:{Math.Round(safeJumpPosition.X)}:{Math.Round(safeJumpPosition.Y)}:{Math.Round(safeJumpPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");

                    double targetDistance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    targetLog.Append("Dist: ").Append(targetDistance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
            }
            else
            {
                targetLog.Append("Nothing Detected!\n");
            }
        }

        public double GetMaxJumpDistance(IMyJumpDrive jumpDrive)
        {
            double maxDistance = (double)jumpDrive.MaxJumpDistanceMeters;
            return maxDistance;
        }

        void IncreaseJumpDistance()
        {
            float currentJumpPercent = JUMPERS[0].GetValueFloat("JumpDistance") + 5f;
            if (currentJumpPercent > 100f)
            {
                currentJumpPercent = 100f;
            }
            JUMPERS[0].SetValueFloat("JumpDistance", currentJumpPercent);
        }

        void DecreaseJumpDistance()
        {
            float currentJumpPercent = JUMPERS[0].GetValueFloat("JumpDistance") - 5f;
            if (currentJumpPercent < 0f)
            {
                currentJumpPercent = 0f;
            }
            JUMPERS[0].SetValueFloat("JumpDistance", currentJumpPercent);
        }

        IMyCameraBlock GetCameraWithMaxRange(List<IMyCameraBlock> cameraList)
        {
            double maxRange = 0d;
            IMyCameraBlock maxRangeCamera = cameraList[0];
            foreach (IMyCameraBlock thisCamera in cameraList)
            {
                if (thisCamera.AvailableScanRange > maxRange)
                {
                    maxRangeCamera = thisCamera;
                    maxRange = maxRangeCamera.AvailableScanRange;
                }
            }
            return maxRangeCamera;
        }

        bool IsNotFriendly(VRage.Game.MyRelationsBetweenPlayerAndBlock relationship)
        {
            return (relationship != VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner);
        }

        void AimAtTarget()
        {
            Vector3D aimDirection = targetPosition - REMOTE.GetPosition();

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
            if (angle * rad2deg <= angleTolerance)
            {
                aimTarget = false;
                foreach (var gyro in GYROS)
                {
                    gyro.Pitch = 0f;
                    gyro.Yaw = 0f;
                    gyro.Roll = 0f;
                    gyro.GyroOverride = false;
                }
            }
        }

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll)
        {
            desiredForwardVector = VectorMath.SafeNormalize(desiredForwardVector);

            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
            {
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            }
            else
            {
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

            if (Vector3D.IsZero(axis))
            {
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

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList)
            {
                if (thisGyro.Closed)
                    continue;
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        void ReadLidarInfos()
        {
            lidarsLog.Clear();
            lidarsLog.Append("Max Range: ").Append(maxScanRange.ToString("0.0")).Append("\n");
            IMyCameraBlock cam = GetCameraWithMaxRange(LIDARS);
            lidarsLog.Append("Avail. Range: ").Append(cam.AvailableScanRange.ToString("0.0")).Append("\n");
        }

        void ReadJumpersInfos()
        {
            jumpersLog.Clear();
            double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
            jumpersLog.Append("Max Jump: ").Append(maxDistance.ToString("0.0")).Append("\n");

            double currentJumpPercent = (double)JUMPERS[0].GetValueFloat("JumpDistance");
            double currentJump = maxDistance / 100d * currentJumpPercent;
            jumpersLog.Append("Curr. Jump: ").Append(currentJump.ToString("0.0")).Append(" (").Append(currentJumpPercent.ToString("0.00")).Append("%)\n");

            double currentStoredPower = 0d;
            double maxStoredPower = 0d;
            StringBuilder timeRemainingBlldr = new StringBuilder();
            foreach (IMyJumpDrive block in JUMPERS)
            {
                MyJumpDriveStatus status = block.Status;

                if (status == MyJumpDriveStatus.Charging)
                {
                    string timeRemaining = block.DetailedInfo.ToString().Split('\n')[5];
                    timeRemainingBlldr.Append(status.ToString()).Append(": ").Append(timeRemaining).Append("s, ");
                }
                else
                {
                    timeRemainingBlldr.Append(status.ToString()).Append(", ");
                }

                currentStoredPower += block.CurrentStoredPower;
                maxStoredPower += block.MaxStoredPower;
            }
            jumpersLog.Append("Status: ").Append(timeRemainingBlldr.ToString());

            if (currentStoredPower > 0)
            {
                double totJumpPercent = currentStoredPower / maxStoredPower * 100;
                jumpersLog.Append("Stored Power: ").Append(totJumpPercent.ToString("0.00")).Append("%\n");
            }
            else
            {
                jumpersLog.Append("Stored Power: ").Append("0%\n");
            }
        }

        void WriteInfo()
        {
            foreach (IMyTextSurface surface in SURFACES)
            {
                StringBuilder text = new StringBuilder();
                text.Append(lidarsLog.ToString());
                text.Append(jumpersLog.ToString());
                text.Append("Selected Planet Safety: " + selectedPlanet + "(" + planetAtmosphereRange + ")\n");
                text.Append(targetLog.ToString());
                surface.WriteText(text);
            }
        }

        void TurretsImpactDetection()
        {
            if (impactDetectionCount == impactDetectionDelay)
            {
                bool targetFound = false;
                foreach (IMyLargeTurretBase turret in TURRETS)
                {
                    MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                    if (!targ.IsEmpty())
                    {
                        if (IsValidTarget(ref targ))
                        {
                            targetInfo = targ;
                            CheckCollisions(targetInfo.Position, targetInfo.Velocity);
                            targetFound = true;
                            break;
                        }
                    }
                }
                if (!targetFound)
                {
                    if (!Vector3D.IsZero(returnPosition))
                    {
                        REMOTE.ClearWaypoints();
                        REMOTE.AddWaypoint(returnPosition, "returnPosition");
                        REMOTE.SetAutoPilotEnabled(true);
                        returnOnce = true;
                    }
                }
                impactDetectionCount = 0;
            }
            impactDetectionCount++;
        }

        void CheckCollisions(Vector3D targetPos, Vector3D targetVelocity)
        {
            BoundingBoxD gridLocalBB = new BoundingBoxD(Me.CubeGrid.Min * Me.CubeGrid.GridSize, Me.CubeGrid.Max * Me.CubeGrid.GridSize);
            MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(gridLocalBB, Me.CubeGrid.WorldMatrix);
            //Vector3 halfExtents = (Vector3)(Me.CubeGrid.Max - Me.CubeGrid.Min) * Me.CubeGrid.GridSize;
            //MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(Me.CubeGrid.WorldMatrix.Translation, halfExtents, Quaternion.CreateFromRotationMatrix(Me.CubeGrid.WorldMatrix));
            double time = 8.0;
            Vector3D targetFuturePosition = targetPos + (targetVelocity * time);
            LineD line = new LineD(targetPos, targetFuturePosition);
            double? hitDist = obb.Intersects(ref line);
            if (hitDist.HasValue)
            {
                if (returnOnce)
                {
                    TARGETERPB.TryRun(argUnlockFromTarget);
                    DECOYPB.TryRun(argLaunchDecoy);
                    returnPosition = REMOTE.GetPosition();
                    returnOnce = false;
                }

                if (escapeCount == escapeDelay)
                {
                    Vector3D escapePosition = targetPos - (Vector3D.Normalize(targetPos - REMOTE.GetPosition()) * escapeDistance);
                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(escapePosition, "escapePosition");
                    REMOTE.SetAutoPilotEnabled(true);
                    escapeCount = 0;
                }
                escapeCount++;
            }
        }

        bool IsValidTarget(ref MyDetectedEntityInfo entityInfo)
        {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid)
            {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsPiloted()
        {
            bool isPiloted = false;
            foreach (IMyShipController block in CONTROLLERS)
            {
                if (block.IsFunctional && block.IsUnderControl && block.CanControlShip && block.ControlThrusters)
                {
                    isPiloted = true;
                    break;
                }
                /*if (block is IMyRemoteControl)
                {
                    if ((block as IMyRemoteControl).IsAutoPilotEnabled)
                    {
                        isPiloted = true;
                        break;
                    }
                }*/
            }
            return isPiloted;
        }

        void AlignToGround(IMyShipController controller)
        {
            Vector3D gravityVec = controller.GetNaturalGravity();
            if (!Vector3D.IsZero(gravityVec))
            {
                var matrix = controller.WorldMatrix;
                Vector3D leftVec = Vector3D.Cross(matrix.Forward, gravityVec);
                Vector3D horizonVec = Vector3D.Cross(gravityVec, leftVec);
                double pitchAngle, rollAngle, yawAngle;
                GetRotationAnglesSimultaneous(horizonVec, -gravityVec, matrix, out pitchAngle, out yawAngle, out rollAngle);
                double mouseYaw = controller.RotationIndicator.Y;
                double mousePitch = controller.RotationIndicator.X;
                double mouseRoll = controller.RollIndicator;

                if (mouseYaw != 0)
                {
                    mouseYaw = mouseYaw < 0 ? MathHelper.Clamp(mouseYaw, -10, -2) : MathHelper.Clamp(mouseYaw, 2, 10);
                }
                if (mouseYaw == 0)
                {
                    yawController.Reset();
                }
                else
                {
                    mouseYaw = yawController.Control(mouseYaw);
                }

                if (mousePitch != 0)
                {
                    mousePitch = mousePitch < 0 ? MathHelper.Clamp(mousePitch, -10, -2) : MathHelper.Clamp(mousePitch, 2, 10);
                }
                if (mousePitch == 0)
                {
                    mousePitch = pitchController.Control(pitchAngle);
                }
                else
                {
                    mousePitch = yawController.Control(mousePitch);
                }

                if (mouseRoll != 0)
                {
                    mouseRoll = mouseRoll < 0 ? MathHelper.Clamp(mouseRoll, -10, -2) : MathHelper.Clamp(mouseRoll, 2, 10);
                }
                if (mouseRoll == 0)
                {
                    mouseRoll = pitchController.Control(pitchAngle);
                }
                else
                {
                    mouseRoll = yawController.Control(mouseRoll);
                }

                ApplyGyroOverride(mousePitch, mouseYaw, mouseRoll, GYROS, matrix);
            }
        }

        void GetBlocks()
        {
            REMOTES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(REMOTES, block => block.CustomName.Contains(remotesName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains(cockpitsName));
            JUMPERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(JUMPERS, block => block.CustomName.Contains(jumpersName));
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, block => block.CustomName.Contains(lidarsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, block => block.CustomName.Contains(alarmsName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(turretsName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CustomName.Contains(controllersName));
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsRangeFinderName));
            foreach (IMyTextPanel panel in panels) { SURFACES.Add(panel as IMyTextSurface); }
            TARGETERPB = GridTerminalSystem.GetBlockWithName(targeterName) as IMyProgrammableBlock;
            MAGNETICDRIVEPB = GridTerminalSystem.GetBlockWithName(magneticDriveName) as IMyProgrammableBlock;
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
            DECOYPB = GridTerminalSystem.GetBlockWithName(decoyName) as IMyProgrammableBlock;
        }

        void InitPIDControllers()
        {
            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
        }

        public class PID
        {
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

            public PID(double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _lowerBound = lowerBound;
                _upperBound = upperBound;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecay = false;
            }

            public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control(double error)
            {
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun)
                {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Compute integral term
                if (!_integralDecay)
                {
                    _errorSum += error * _timeStep;

                    //Clamp integral term
                    if (_errorSum > _upperBound)
                        _errorSum = _upperBound;
                    else if (_errorSum < _lowerBound)
                        _errorSum = _lowerBound;
                }
                else
                {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
                return this.Value;
            }

            public double Control(double error, double timeStep)
            {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                return Control(error);
            }

            public void Reset()
            {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

        public static class VectorMath
        {
            public static Vector3D SafeNormalize(Vector3D a)
            {
                if (Vector3D.IsZero(a))
                    return Vector3D.Zero;

                if (Vector3D.IsUnit(ref a))
                    return a;

                return Vector3D.Normalize(a);
            }

            public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1)//reflect a over b
            {
                Vector3D project_a = Projection(a, b);
                Vector3D reject_a = a - project_a;
                return project_a - reject_a * rejectionFactor;
            }

            public static Vector3D Rejection(Vector3D a, Vector3D b)//reject a on b
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a - a.Dot(b) / b.LengthSquared() * b;
            }

            public static Vector3D Projection(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a.Dot(b) / b.LengthSquared() * b;
            }

            public static double ScalarProjection(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;

                if (Vector3D.IsUnit(ref b))
                    return a.Dot(b);

                return a.Dot(b) / b.Length();
            }

            public static double AngleBetween(Vector3D a, Vector3D b)//returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }

            public static double CosBetween(Vector3D a, Vector3D b)//returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
            }
        }

        /*
        // This multiplier allows for faster reaction when a ship encounters a hill. Higher values will cause the ship to pitch up / down faster
        // Small / light ships (such as a hover bike), use 1. Really big / heavy ships (land cruiser), use a higher number (ie 10)
        // Play with it until you're happy
        const double DAMPING_MULTIPLIER = 1;

        // This is max angle deviation from the horizon the ship is allowed to pitch and roll (in degrees)
        // This value ensures that the ship doesn't become pitched or tilted too steep when going down a hill, for instance
        // NOTE: This will not correct pitch when going UP a hill so as to keep from driving the nose into the ground
        const int ANGLE_TOLERANCE = 20;
        void CalculateGyroOverrides(MatrixD shipMatrix, Vector3D alignVec, Vector3D gravityVec, IMyShipController controller, out double pitch, out double roll)
        {
            double angleFwd = 90;
            double angleLft = 90;
            if (alignVec != gravityVec)
            {
                angleFwd = MathHelper.ToDegrees(VectorMath.AngleBetween(shipMatrix.Forward, gravityVec));
                angleLft = MathHelper.ToDegrees(VectorMath.AngleBetween(shipMatrix.Left, gravityVec));
            }
            var localAlignVec = Vector3D.TransformNormal(alignVec, MatrixD.Transpose(shipMatrix));
            
            pitch = -localAlignVec.Z * DAMPING_MULTIPLIER; // pitch < 0 if pitching down
            var deviation = 90 - angleFwd;
            var velocityDir = controller.GetShipVelocities().LinearVelocity.Dot(shipMatrix.Forward);
            if (velocityDir > 0 && deviation > ANGLE_TOLERANCE)
            {
                pitch = MathHelper.PiOver4;
            }
            else if (velocityDir < 0 && deviation < -ANGLE_TOLERANCE)
            {
                pitch = -MathHelper.PiOver4;
            }
            else if (Math.Abs(pitch) < 0.015 * DAMPING_MULTIPLIER)
            {
                pitch = 0;
            }
            pitch = pitchController.Control(pitch);
            
            roll = -2 * localAlignVec.X; // roll < 0 if rolling right
            if (shipMatrix.Down.Dot(alignVec) < 0)
            {
                roll = Math.PI * Math.Sign(shipMatrix.Left.Dot(alignVec));
            }
            else if (Math.Abs(90 - angleLft) > ANGLE_TOLERANCE)
            {
                roll = MathHelper.PiOver4 * Math.Sign(shipMatrix.Left.Dot(gravityVec));
            }
            else if (Math.Abs(roll) < 0.015 * DAMPING_MULTIPLIER)
            {
                roll = 0;
            }
            roll = rollController.Control(roll);
        }
        
        you want the direction?
        or the actual magnitude?
        var leftVec = Vector3D.Cross(shipForward, gravity);
        var horizonVec = Vector3D.Cross(gravity, left);
        // Then you can normalize horizonVec or whatever
        Or alternatively...
        var horizonVec = Vector3D.Reject(shipForward, gravity);
        // Then you can normalize horizonVec or whatever
        I use the latter in my gravity compensation methods
        
        // Gravity Variables 
        bool shouldAlign = true; //If the script should attempt to stabilize by default	   
        const double angleTolerance = 3; //How many degrees the code will allow before it overrides user control 
        bool gyroToggle;
        bool firstToggle;
        void AlignWithGravity()
        {
            //---Set appropriate gyro override
            double rollSpeed;
            double pitchSpeed;
            bool canTolerate = CanTolerate(out pitchSpeed, out rollSpeed);
            if (shouldAlign && !canTolerate)
            {
                ApplyGyroOverride(pitchSpeed, 0, -rollSpeed, GYROS, REMOTE.WorldMatrix);
                gyroToggle = true;
            }
            else if (gyroToggle)
            {
                gyroToggle = false;
                foreach (IMyGyro gyro in GYROS)
                {
                    gyro.GyroOverride = false;
                }
            }
        }

        //---PID Constants 
        const double proportionalConstant = 10;
        const double derivativeConstant = 5;
        double lastAngleRoll;
        double lastAnglePitch;
        bool CanTolerate(out double pitchSpeed, out double rollSpeed)
        {
            pitchSpeed = 0;
            rollSpeed = 0;
            if (!shouldAlign)
            {
                return true;
            }
            //---Get gravity vector 
            var referenceOrigin = REMOTE.GetPosition();
            var gravityVec = REMOTE.GetNaturalGravity();
            if (!Vector3D.IsZero(REMOTE.GetNaturalGravity()))
            {
                if (firstToggle)
                {
                    foreach (IMyGyro thisGyro in GYROS)
                    {
                        thisGyro.SetValue("Override", false);
                    }
                }
                firstToggle = false;
                return true;
            }
            firstToggle = true;
            //---Dir'n vectors of the reference block 
            var referenceForward = REMOTE.WorldMatrix.Forward;
            var referenceLeft = REMOTE.WorldMatrix.Left;
            var referenceUp = REMOTE.WorldMatrix.Up;

            //---Get Roll and Pitch Angles  
            double anglePitch = Math.Acos(MathHelper.Clamp(gravityVec.Dot(referenceForward) / gravityVec.Length(), -1, 1)) - Math.PI / 2;

            Vector3D planetRelativeLeftVec = referenceForward.Cross(gravityVec);
            double angleRoll = VectorMath.AngleBetween(referenceLeft, planetRelativeLeftVec);
            angleRoll *= Math.Sign(VectorMath.Projection(referenceLeft, gravityVec).Dot(gravityVec));

            anglePitch *= -1;
            angleRoll *= -1;

            //---Get Raw Deviation angle	 
            double rawDevAngle = Math.Acos(MathHelper.Clamp(gravityVec.Dot(referenceForward) / gravityVec.Length() * 180 / Math.PI, -1, 1));

            //---Angle controller	 
            rollSpeed = Math.Round(angleRoll * proportionalConstant + (angleRoll - lastAngleRoll) / globalTimestep * derivativeConstant, 2);
            pitchSpeed = Math.Round(anglePitch * proportionalConstant + (anglePitch - lastAnglePitch) / globalTimestep * derivativeConstant, 2);                                                                                                                                                          //w.H]i\p 

            rollSpeed /= GYROS.Count;
            pitchSpeed /= GYROS.Count;

            //store old angles   
            lastAngleRoll = angleRoll;
            lastAnglePitch = anglePitch;

            //---Check if we are inside our tolerances   
            if (Math.Abs(anglePitch * 180 / Math.PI) > angleTolerance)
            {
                return false;
            }
            if (Math.Abs(angleRoll * 180 / Math.PI) > angleTolerance)
            {
                return false;
            }
            return true;
        }
        */

    }
}
