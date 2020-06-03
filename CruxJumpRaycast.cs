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


        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string jumpersName = "[CRX] Jump";
        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string lcdsRangeFinderName = "[CRX] LCD RangeFinder";
        readonly string magneticDriveName = "[CRX] PB Magnetic Drive";
        readonly string alarmsName = "[CRX] Alarm Lidar";

        readonly string sectionTag = "RangeFinderSettings";
        readonly string cockpitRangeFinderKey = "cockpitRangeFinderSurface";

        const string argSetup = "Setup";
        const string argRangeFinder = "RangeFinder";
        const string argChangeSafetyOffset = "ChangeSafetyOffset";
        const string argIncreaseJump = "IncreaseJump";
        const string argDecreaseJump = "DecreaseJump";
        const string argAimTarget = "AimTarget";
        const string argMDGyroStabilizeOff = "StabilizeOff";
        const string argMDGyroStabilizeOn = "StabilizeOn";

        readonly double enemySafeDistance = 3000d;
        readonly double friendlySafeDistance = 1000d;

        int cockpitRangeFinderSurface = 4;
        bool aimTarget = false;
        double maxScanRange = 0d;
        int planetSelector = 0;
        string selectedPlanet = "";
        double planetAtmosphereRange = 0d;
        bool MDOn = false;
        bool MDOff = false;
        bool runMDOnce = false;

        Vector3D targetPosition = new Vector3D();

        const float globalTimestep = 1.0f / 60.0f;
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 5;   // degrees

        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyJumpDrive> JUMPERS = new List<IMyJumpDrive>();
        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public IMyRemoteControl REMOTE;
        IMyProgrammableBlock MAGNETICDRIVEPB;

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

        void Main(string arg)
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
                    runMDOnce = true;
                }
                if (!MDOff && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                {
                    MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                }
                //if (MDOn) { Echo("Magnetic drive turned OFF"); } else { Echo("Magnetic drive failed to turn OFF"); }

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

            ReadLidarInfos();
            ReadJumpersInfos();

            WriteInfo();
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argSetup: Setup(); break;
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
                case argAimTarget: if (!Vector3D.IsZero(targetPosition)) { aimTarget = true; Runtime.UpdateFrequency = UpdateFrequency.Update1; }; break;
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
                    Vector3D hitPosition = (Vector3D)TARGET.HitPosition;
                    Vector3D safetyOffset = Vector3D.Normalize(REMOTE.CubeGrid.WorldVolume.Center - hitPosition) * planetAtmosphereRange;
                    Vector3D safeJumpPosition = hitPosition + safetyOffset;

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
                    Vector3D hitPosition = (Vector3D)TARGET.HitPosition;

                    REMOTE.ClearWaypoints();
                    REMOTE.AddWaypoint(hitPosition, "Asteroid");

                    double distance = Vector3D.Distance(REMOTE.CubeGrid.WorldVolume.Center, hitPosition);
                    double maxDistance = GetMaxJumpDistance(JUMPERS[0]);
                    if (maxDistance != 0d && distance != 0d)
                    {
                        JUMPERS[0].SetValueFloat("JumpDistance", (float)(distance / maxDistance * 100d));
                    }

                    targetPosition = hitPosition;

                    string safeJumpGps = $"GPS:Asteroid:{Math.Round(hitPosition.X)}:{Math.Round(hitPosition.Y)}:{Math.Round(hitPosition.Z)}";
                    targetLog.Append(safeJumpGps).Append("\n");

                    targetLog.Append("Dist.: ").Append(distance.ToString("0.0")).Append("\n");

                    double targetDiameter = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
                    targetLog.Append("Diameter: ").Append(targetDiameter.ToString("0.0")).Append("\n");
                }
                else if (IsNotFriendly(TARGET.Relationship))
                {
                    Vector3D hitPosition = (Vector3D)TARGET.HitPosition;
                    Vector3D safetyOffset = Vector3D.Normalize(REMOTE.CubeGrid.WorldVolume.Center - hitPosition) * enemySafeDistance;
                    Vector3D safeJumpPosition = hitPosition + safetyOffset;

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
                    Vector3D hitPosition = (Vector3D)TARGET.HitPosition;
                    Vector3D safetyOffset = Vector3D.Normalize(REMOTE.CubeGrid.WorldVolume.Center - hitPosition) * friendlySafeDistance;
                    Vector3D safeJumpPosition = hitPosition + safetyOffset;

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
            string maxDist = jumpDrive.DetailedInfo.ToString().Split('\n')[6];
            maxDist = maxDist.Split(':')[1];
            double maxDistance = GetNum(maxDist);
            return maxDistance;
        }

        double GetNum(string input)
        {
            const string regExpr = @"(?<Num>[0-9.]+) (?<Unit>[a-zA-Z]+)";
            var match = System.Text.RegularExpressions.Regex.Match(input, regExpr);
            if (!match.Success) { throw new Exception("Input has an invalid format"); }
            return double.Parse(match.Groups["Num"].Value) * UnitToDouble(match.Groups["Unit"].Value);
        }

        double UnitToDouble(string unit)
        {
            if (unit.ToLower().StartsWith("k"))
            {
                return 1000d;
            }
            return 1d;
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
            Vector3D aimDirection = REMOTE.GetPosition() - targetPosition;
            //Vector3D aimDirectionLocal = Vector3D.TransformNormal(aimDirection, MatrixD.Transpose(REMOTE.WorldMatrix));

            //Vector3D aimDirection = Vector3D.Normalize(REMOTE.GetPosition() - targetPosition);

            double yawAngle;
            double pitchAngle;
            GetRotationAngles(aimDirection, REMOTE.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            ApplyGyroOverride(pitchSpeed, yawSpeed, 0);

            Vector3D forwardVec = REMOTE.WorldMatrix.Forward;
            double angle = GetAngleBetween(forwardVec, aimDirection);
            if (angle * rad2deg <= angleTolerance)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
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

        void GetRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch)
        {
            Vector3D localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            Vector3D flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = GetAngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive

            if (Math.Abs(pitch) < 1E-6 && localTargetVector.Z > 0)
            {
                pitch = Math.PI;
            }
            if (Vector3D.IsZero(flattenedTargetVector))
            {
                yaw = MathHelper.PiOver2 * Math.Sign(localTargetVector.X);
            }
            else
            {
                yaw = GetAngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
            }
        }

        public static double GetAngleBetween(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
            {
                return 0;
            }
            if (Vector3D.IsUnit(ref a) && Vector3D.IsUnit(ref b))
            {
                return Math.Acos(MathHelperD.Clamp(a.Dot(b), -1, 1));
            }
            return Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed)
        {
            Vector3D rotationVec = new Vector3D(-pitchSpeed, yawSpeed, rollSpeed); //because keen does some weird stuff with signs 
            MatrixD refMatrix = REMOTE.WorldMatrix;
            Vector3D relativeRotationVec = Vector3D.TransformNormal(rotationVec, refMatrix);

            foreach (var gyro in GYROS)
            {
                Vector3D transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyro.WorldMatrix));

                gyro.Pitch = (float)transformedRotationVec.X;
                gyro.Yaw = (float)transformedRotationVec.Y;
                gyro.Roll = (float)transformedRotationVec.Z;
                gyro.GyroOverride = true;
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
        }

        void InitPIDControllers(IMyTerminalBlock block)
        {
            double aimP, aimI, aimD;
            double integralWindupLimit = 0;
            float second = 60f;

            if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                aimP = 15;
                aimI = 0;
                aimD = 7;
            }
            else
            {
                aimP = 40;
                aimI = 0;
                aimD = 13;
            }

            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, second);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, second);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, second);
        }

        public class PID
        {
            double _kP = 0;
            double _kI = 0;
            double _kD = 0;
            double _integralDecayRatio = 0;
            double _lowerBound = 0;
            double _upperBound = 0;
            double _timeStep = 0;
            double _inverseTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;
            bool _integralDecay = false;
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

            public double Filter(double input, int round_d_digits)
            {
                double roundedInput = Math.Round(input, round_d_digits);

                _integralDecayRatio += (input / _timeStep);
                _integralDecayRatio = (_upperBound > 0 && _integralDecayRatio > _upperBound ? _upperBound : _integralDecayRatio);
                _integralDecayRatio = (_lowerBound < 0 && _integralDecayRatio < _lowerBound ? _lowerBound : _integralDecayRatio);

                double derivative = (roundedInput - _lastError) * _timeStep;
                _lastError = roundedInput;

                return (_kP * input) + (_kI * _integralDecayRatio) + (_kD * derivative);
            }

            public double Control(double error)
            {
                var errorDerivative = (error - _lastError) * _inverseTimeStep;//Compute derivative term

                if (_firstRun)
                {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                if (!_integralDecay)//Compute integral term
                {
                    _errorSum += error * _timeStep;

                    if (_errorSum > _upperBound)//Clamp integral term
                    {
                        _errorSum = _upperBound;
                    }
                    else if (_errorSum < _lowerBound)
                    {
                        _errorSum = _lowerBound;
                    }
                }
                else
                {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                _lastError = error;//Store this error as last error

                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;//Construct output
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


    }
}
