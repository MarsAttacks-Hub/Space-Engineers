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
        //RANGE FINDER

        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string jumpersName = "[CRX] Jump";
        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string lcdsRangeFinderName = "[CRX] LCD RangeFinder";
        readonly string magneticDriveName = "[CRX] PB Magnetic Drive";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string managerName = "[CRX] PB Manager";
        readonly string debugPanelName = "[CRX] Debug";

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

        const float globalTimestep = 10.0f / 60.0f;
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 0.1;//degrees

        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;
        readonly double aimP = 1;
        readonly double aimI = 0;
        readonly double aimD = 1;
        readonly double integralWindupLimit = 0;

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

        Vector3D targetPosition = new Vector3D();

        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        IMyRemoteControl REMOTE;
        IMyProgrammableBlock MAGNETICDRIVEPB;
        IMyProgrammableBlock MANAGERPB;

        PID yawController;
        PID pitchController;
        PID rollController;

        readonly MyIni myIni = new MyIni();

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

            InitPIDControllers(REMOTE);
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
                    //if (MDOn) { Echo("Magnetic drive turned ON"); } else { Echo("Magnetic drive failed to turn ON"); }
                }

                if (REMOTE.IsAutoPilotEnabled && targetPosition != null)
                {
                    double dist = Vector3D.Distance(targetPosition, REMOTE.GetPosition());
                    if (dist < 150)
                    {
                        REMOTE.SetAutoPilotEnabled(false);
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

            Vector3D UpVector;
            if (Vector3D.IsZero(REMOTE.GetNaturalGravity())) { UpVector = REMOTE.WorldMatrix.Up; }
            else { UpVector = -REMOTE.GetNaturalGravity(); }
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, UpVector, REMOTE.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

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
            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsRangeFinderName));
            foreach (IMyTextPanel panel in panels)
            {
                SURFACES.Add(panel as IMyTextSurface);
            }

            MAGNETICDRIVEPB = GridTerminalSystem.GetBlockWithName(magneticDriveName) as IMyProgrammableBlock;
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
        }

        void InitPIDControllers(IMyTerminalBlock block)
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

    }
}
