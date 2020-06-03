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
using System.Collections.Immutable;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {


        //TODO
        // method that calculate the delay between scans based on the distance of the target and the number of lidars
        // when a missile hit the target the tracking is lost, rescan the target doesn't work ---> problem caused by the sudden accelleration caused by the impact???
        // turn off PB sunChase also

        readonly string lidarsName = "[CRX] Camera Lidar";
        readonly string antennasName = "T";
        readonly string lightsName = "[CRX] Rotating Light";
        readonly string turretsName = "[CRX] Turret";
        readonly string lcdsTargetName = "[CRX] LCD Target";
        readonly string controllersName = "Reference";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string alarmsName = "[CRX] Alarm Lidar";
        readonly string weldersName = "Missile";
        readonly string rocketsName = "[CRX] Rocket Launcher";
        readonly string gatlingsName = "[CRX] Gatling Gun";
        readonly string projectorsMissilesName = "Missile";
        readonly string projectorsDronesName = "Drone";
        readonly string missileAntennasName = "A [M]";
        readonly string shipPrefix = "[CRX] ";
        readonly string missilePrefix = "[M]";
        readonly string antennaTag = "[RELAY]";
        readonly string missileAntennaTag = "[MISSILE]";
        readonly string magneticDriveName = "[CRX] PB Magnetic Drive";

        const string commandLaunch = "Launch";
        const string commandUpdate = "Update";
        const string commandLost = "Lost";

        const string argClear = "Clear";
        const string argLock = "Lock";
        const string argSwitchWeapon = "SwitchGun";
        const string argSwitchPayLoad = "SwitchPayLoad";
        const string argLoadMissiles = "LoadMissiles";
        const string argSetup = "Setup";
        const string argMDGyroStabilizeOff = "StabilizeOff";
        const string argMDGyroStabilizeOn = "StabilizeOn";

        readonly string sectionTag = "MissilesSettings";
        readonly string cockpitTargetSurfaceKey = "cockpitTargetSurface";

        int weaponType = 1; //0 None - 1 Rockets - 2 Gatlings
        int selectedPayLoad = 0;    //0 Missiles - 1 Drones
        readonly int missilesCount = 2;
        readonly int autoMissilesDelay = 91;
        readonly bool autoMissiles = true;
        readonly bool autoRockets = true;
        readonly double rocketProjectileForwardOffset = 4;  //By default, rockets are spawn 4 meters in front of the rocket launcher's tip
        readonly double rocketProjectileInitialSpeed = 100;
        readonly double rocketProjectileAccelleration = 600;
        readonly double rocketProjectileMaxSpeed = 200;
        readonly double rocketProjectileMaxRange = 800;
        readonly double gatlingProjectileForwardOffset = 0;
        readonly double gatlingProjectileInitialSpeed = 400;
        readonly double gatlingProjectileAccelleration = 0;
        readonly double gatlingProjectileMaxSpeed = 400;

        int selectedMissile = 1;
        int cockpitTargetSurface = 0;
        int autoMissilesCounter = 0;
        long currentTick = 1;
        bool doOnce = false;
        bool shootOnce = false;
        bool missilesLoaded = false;
        double targetDiameter;
        bool MDOn = false;
        bool MDOff = false;

        const float globalTimestep = 1.0f / 60.0f;  //0.016f;
        const long ticksToScan = 11;

        public List<IMyCameraBlock> LIDARS = new List<IMyCameraBlock>();
        public List<IMyLightingBlock> LIGHTS = new List<IMyLightingBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();
        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMySoundBlock> ALARMS = new List<IMySoundBlock>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();
        public List<IMyProjector> PROJECTORSDRONES = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSMISSILES = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyRadioAntenna> MISSILEANTENNAS = new List<IMyRadioAntenna>();
        public List<IMyTerminalBlock> ROCKETS = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> GATLINGS = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> BLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
        public List<IMyInventory> INVENTORIES = new List<IMyInventory>();
        public List<IMyTerminalBlock> MISSILEBLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
        public List<IMyInventory> MISSILEINVENTORIES = new List<IMyInventory>();

        IMyShipController CONTROLLER;
        IMyRadioAntenna ANTENNA;
        IMyProgrammableBlock MAGNETICDRIVEPB;

        public IMyUnicastListener UNILISTENER;
        public IMyBroadcastListener BROADCASTLISTENER;

        MyDetectedEntityInfo TARGET;
        MyDetectedEntityInfo PREV_TARGET;

        readonly MyIni myIni = new MyIni();

        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");

        public List<long> MissileIDs = new List<long>();

        public StringBuilder targetLog = new StringBuilder("");
        public StringBuilder missileLog = new StringBuilder("");

        PID yawController;
        PID pitchController;
        PID rollController;

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup()
        {
            UNILISTENER = IGC.UnicastListener;
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(antennaTag);

            GetBlocks();
            SetBlocks();
            GetMissileAntennas();
            SetMissileAntennas();

            InitPIDControllers(CONTROLLER as IMyTerminalBlock);

            if (selectedPayLoad == 0)
            {
                TEMPPROJECTORS = PROJECTORSMISSILES;
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
            }
            else if (selectedPayLoad == 1)
            {
                TEMPPROJECTORS = PROJECTORSDRONES;
                foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
            }

            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }

            autoMissilesCounter = autoMissilesDelay + 1;
        }

        void Main(string arg)
        {
            Echo($"MISSILEANTENNAS:{MISSILEANTENNAS.Count}");
            Echo($"LIDARS:{LIDARS.Count}");
            Echo($"CONTROLLERS:{CONTROLLERS.Count}");
            Echo($"COCKPITS:{COCKPITS.Count}");
            Echo($"TURRETS:{TURRETS.Count}");
            Echo($"LIGHTS:{LIGHTS.Count}");
            Echo($"SURFACES:{SURFACES.Count}");
            Echo($"ALARMS:{ALARMS.Count}");
            Echo($"GYROS:{GYROS.Count}");
            Echo($"WELDERS:{WELDERS.Count}");
            Echo($"ROCKETS:{ROCKETS.Count}");
            Echo($"GATLINGS:{GATLINGS.Count}");
            Echo($"PROJECTORSMISSILES:{PROJECTORSMISSILES.Count}");
            Echo($"PROJECTORSDRONES:{PROJECTORSDRONES.Count}");

            targetLog.Clear();
            missileLog.Clear();

            GetMessages();

            if (!TARGET.IsEmpty())
            {
                if (currentTick > ticksToScan)
                {
                    TARGET = new MyDetectedEntityInfo();
                    PREV_TARGET = new MyDetectedEntityInfo();
                    return;
                }

                ReadTargetInfo();

                //------------------------------------

                if (!doOnce)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    TurnAlarmOn();
                    doOnce = true;

                    if (MAGNETICDRIVEPB != null)
                    {
                        if (MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                        {
                            MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                        }
                    }
                }
                if (!MDOff && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                {
                    MDOff = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOff);
                }

                //------------------------------------

                bool targetFound = false;
                foreach (IMyLargeTurretBase turret in TURRETS)
                {
                    if (!turret.GetTargetedEntity().IsEmpty())
                    {
                        MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                        if (IsValidLidarTarget(ref targ))
                        {
                            TARGET = targ;
                            targetFound = true;
                            break;
                        }
                    }
                }

                //------------------------------------

                if (!targetFound)
                {
                    if (currentTick == ticksToScan)
                    {
                        targetFound = AcquireTarget();
                    }
                }
                else
                {
                    if (autoMissiles)
                    {
                        if (autoMissilesCounter > autoMissilesDelay)
                        {
                            arg = commandLaunch;
                            autoMissilesCounter = 0;
                        }
                        autoMissilesCounter++;
                    }
                }

                //------------------------------------

                if (!targetFound)
                {
                    currentTick++;
                }
                else
                {
                    currentTick = 1;
                    if (MissileIDs.Count > 0)
                    {
                        SendMissileUnicastMessage(commandUpdate);
                    }
                }

                //------------------------------------

                LockOnTarget(CONTROLLER);

                //------------------------------------

                PREV_TARGET = TARGET;

                if (autoRockets)
                {
                    double targetDistance = Vector3D.Distance(TARGET.Position, CONTROLLER.CubeGrid.WorldVolume.Center);
                    ManageGuns(targetDistance);
                }
            }
            else
            {
                if (doOnce)
                {
                    UnlockShip();
                    TurnAlarmOff();
                    doOnce = false;
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    if (MissileIDs.Count > 0)
                    {
                        SendMissileUnicastMessage(commandLost);
                    }

                    if (MAGNETICDRIVEPB != null)
                    {
                        if (MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                        {
                            MDOn = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOn);
                        }
                    }
                }
                if (!MDOn && MAGNETICDRIVEPB.CustomData.Contains("GyroStabilize=true"))
                {
                    MDOn = MAGNETICDRIVEPB.TryRun(argMDGyroStabilizeOn);
                }

                //------------------------------------

                foreach (IMyLargeTurretBase turret in TURRETS)
                {
                    if (!turret.GetTargetedEntity().IsEmpty())
                    {
                        MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                        if (IsValidLidarTarget(ref targ))
                        {
                            TARGET = targ;
                            break;
                        }
                    }
                }
            }

            bool completed = CheckProjectors();
            if (completed)
            {
                if (!missilesLoaded)
                {
                    missilesLoaded = LoadMissiles();
                }
            }
            else
            {
                missilesLoaded = false;
            }

            ProcessArgs(arg);

            WriteInfo();
        }

        void ProcessArgs(string arg)
        {
            switch (arg)
            {
                case argSetup: Setup(); break;
                case argLock: AcquireTarget(); break;
                case commandLaunch:
                    if (!TARGET.IsEmpty())
                    {
                        foreach (IMyRadioAntenna block in MISSILEANTENNAS)
                        {
                            string antennaName = missileAntennasName + selectedMissile.ToString();
                            if (block.CustomName.Equals(antennaName))
                            {
                                block.Enabled = true;
                                block.EnableBroadcasting = true;
                            }
                        }
                        selectedMissile++;
                        if (selectedMissile > missilesCount + 1)
                        {
                            GetMissileAntennas();
                            SetMissileAntennas();
                            selectedMissile = 1;
                        }
                        SendMissileBroadcastMessage(commandLaunch);
                    }
                    break;
                case argClear:
                    GetBlocks();
                    SetBlocks();
                    TARGET = new MyDetectedEntityInfo();
                    PREV_TARGET = new MyDetectedEntityInfo();
                    TurnAlarmOff();
                    GetMissileAntennas();
                    SetMissileAntennas();
                    selectedMissile = 1;
                    doOnce = false;
                    break;
                case argSwitchWeapon:
                    weaponType = (weaponType == 1 ? 2 : 1);
                    break;
                case argSwitchPayLoad:
                    if (selectedPayLoad == 1)
                    {
                        selectedPayLoad = 0;
                        TEMPPROJECTORS = PROJECTORSMISSILES;
                        foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = true; }
                        foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = false; }
                    }
                    else if (selectedPayLoad == 0)
                    {
                        selectedPayLoad = 1;
                        TEMPPROJECTORS = PROJECTORSDRONES;
                        foreach (IMyProjector block in PROJECTORSDRONES) { block.Enabled = true; }
                        foreach (IMyProjector block in PROJECTORSMISSILES) { block.Enabled = false; }
                    }
                    break;
                case argLoadMissiles: LoadMissiles(); break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit)
        {
            if (!cockpit.CustomData.Contains(sectionTag))
            {
                cockpit.CustomData += $"[{sectionTag}]\n{cockpitTargetSurfaceKey}={cockpitTargetSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);

            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToString()))
            {
                cockpitTargetSurface = myIni.Get(sectionTag, cockpitTargetSurfaceKey).ToInt32();
                SURFACES.Add(cockpit.GetSurface(cockpitTargetSurface));
            }
        }

        bool AcquireTarget()
        {
            bool targetFound = false;
            if (TARGET.IsEmpty())
            {
                IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
                MyDetectedEntityInfo entityInfo = lidar.Raycast(lidar.AvailableScanRange, 0, 0);
                if (!entityInfo.IsEmpty())
                {
                    if (IsValidLidarTarget(ref entityInfo))
                    {
                        double lidarTargetSpeed = entityInfo.Velocity.Length();
                        targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);

                        TARGET = entityInfo;

                        targetFound = true;
                    }
                }
            }
            else
            {
                float elapsedTime = currentTick * globalTimestep;
                Vector3D targetPos = TARGET.Position + (TARGET.Velocity * elapsedTime);

                double overshootDistance = targetDiameter / 2;
                IMyCameraBlock lidar = GetCameraWithMaxRange(LIDARS);
                double dist = Vector3D.Distance(targetPos, lidar.GetPosition());
                if (lidar.CanScan(dist))
                {
                    Vector3D testTargetPosition = targetPos + (Vector3D.Normalize(targetPos - lidar.GetPosition()) * overshootDistance);

                    MyDetectedEntityInfo entityInfo = lidar.Raycast(testTargetPosition);
                    if (!entityInfo.IsEmpty())
                    {
                        if (entityInfo.EntityId == TARGET.EntityId)
                        {
                            targetDiameter = Vector3D.Distance(entityInfo.BoundingBox.Min, entityInfo.BoundingBox.Max);

                            TARGET = entityInfo;

                            targetFound = true;
                        }
                        else
                        {
                            currentTick -= 1;
                        }
                    }
                }
            }
            return targetFound;
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

        bool IsValidLidarTarget(ref MyDetectedEntityInfo entityInfo)//TODO
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

        void LockOnTarget(IMyShipController REF)
        {
            MatrixD refWorldMatrix = REF.WorldMatrix;
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetPos = TARGET.Position + (TARGET.Velocity * elapsedTime);
            Vector3D aimDirection;

            MyDetectedEntityInfo prevtarget;
            if (!PREV_TARGET.IsEmpty())
            {
                prevtarget = PREV_TARGET;
            }
            else
            {
                prevtarget = TARGET;
            }

            Vector3D targetAccel = Vector3D.Zero;
            if ((!Vector3D.IsZero(TARGET.Velocity) || !Vector3D.IsZero(prevtarget.Velocity)) && !Vector3D.IsZero((TARGET.Velocity - prevtarget.Velocity)))
            {
                targetAccel = (TARGET.Velocity - prevtarget.Velocity) / elapsedTime;
            }

            switch (weaponType)
            {
                case 0://none
                    aimDirection = ComputeInterceptPoint(targetPos, TARGET.Velocity - REF.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
                    break;
                case 1://rockets
                    aimDirection = ComputeInterceptPointWithInheritSpeed(targetPos, TARGET.Velocity, targetAccel, (rocketProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * rocketProjectileForwardOffset)), REF.GetShipVelocities().LinearVelocity, rocketProjectileInitialSpeed, rocketProjectileAccelleration, rocketProjectileMaxSpeed, rocketProjectileMaxRange);
                    break;
                case 2://gatlings
                    aimDirection = ComputeInterceptPoint(targetPos, TARGET.Velocity - REF.GetShipVelocities().LinearVelocity, targetAccel, (gatlingProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * gatlingProjectileForwardOffset)), gatlingProjectileInitialSpeed, gatlingProjectileAccelleration, gatlingProjectileMaxSpeed);
                    break;
                default:
                    aimDirection = ComputeInterceptPoint(targetPos, TARGET.Velocity - REF.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
                    break;
            }

            aimDirection -= refWorldMatrix.Translation;

            double yawAngle, pitchAngle;
            GetRotationAngles(aimDirection, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            ApplyGyroOverride(pitchSpeed, yawSpeed, 0);
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
            MatrixD refMatrix = CONTROLLER.WorldMatrix;
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

        Vector3D ComputeInterceptPoint(Vector3D targetPos, Vector3D targetVel, Vector3D targetAccel, Vector3D projectileLocation, double projectileInitialSpeed, double projectileAcceleration, double projectileMaxSpeed)
        {
            Vector3D z = targetPos - projectileLocation;
            double k = (projectileAcceleration == 0 ? 0 : (projectileMaxSpeed - projectileInitialSpeed) / projectileAcceleration);
            double p = (0.5 * projectileAcceleration * k * k) + (projectileInitialSpeed * k) - (projectileMaxSpeed * k);

            double a = (projectileMaxSpeed * projectileMaxSpeed) - targetVel.LengthSquared();
            double b = 2 * ((p * projectileMaxSpeed) - targetVel.Dot(z));
            double c = (p * p) - z.LengthSquared();

            double t = SolveQuadratic(a, b, c);

            if (Double.IsNaN(t) || t < 0)
            {
                return new Vector3D(Double.NaN, Double.NaN, Double.NaN);
            }
            else
            {
                if (targetAccel.Sum > 0.001)
                {
                    return targetPos + (targetVel * t) + (0.5 * targetAccel * t * t);
                }
                else
                {
                    return targetPos + (targetVel * t);
                }
            }
        }

        Vector3D ComputeInterceptPointWithInheritSpeed(Vector3D targetPos, Vector3D targetVel, Vector3D targetAccel, Vector3D projectileLocation, Vector3D shipDirection, double projectileInitialSpeed, double projectileAcceleration, double projectileMaxSpeed, double projectileMaxRange)
        {
            Vector3D z = targetPos - projectileLocation;
            double k = (projectileAcceleration == 0 ? 0 : (projectileMaxSpeed - projectileInitialSpeed) / projectileAcceleration);
            double p = (0.5 * projectileAcceleration * k * k) + (projectileInitialSpeed * k) - (projectileMaxSpeed * k);

            double a = (projectileMaxSpeed * projectileMaxSpeed) - targetVel.LengthSquared();
            double b = 2 * ((p * projectileMaxSpeed) - targetVel.Dot(z));
            double c = (p * p) - z.LengthSquared();

            double t = SolveQuadratic(a, b, c);

            if (Double.IsNaN(t) || t < 0)
            {
                return new Vector3D(Double.NaN, Double.NaN, Double.NaN);
            }

            int u = (int)Math.Ceiling(t * 60);

            Vector3D targetPoint;
            if (targetAccel.Sum > 0.001)
            {
                targetPoint = targetPos + (targetVel * t) + (0.5 * targetAccel * t * t);
            }
            else
            {
                targetPoint = targetPos + (targetVel * t);
            }

            Vector3D aimDirection;
            Vector3D stepAcceleration;
            Vector3D currentPosition;
            Vector3D currentDirection;

            aimDirection = Vector3D.Normalize(targetPoint - projectileLocation);
            stepAcceleration = (aimDirection * projectileAcceleration) / 60;

            currentPosition = projectileLocation;
            currentDirection = shipDirection + (aimDirection * projectileInitialSpeed);

            for (int i = 0; i < u; i++)
            {
                currentDirection += stepAcceleration;

                double speed = currentDirection.Length();
                if (speed > projectileMaxSpeed)
                {
                    currentDirection = currentDirection / speed * projectileMaxSpeed;
                }

                currentPosition += (currentDirection / 60);

                if ((i + 1) % 60 == 0)
                {
                    if (Vector3D.Distance(projectileLocation, currentPosition) > projectileMaxRange)
                    {
                        return targetPoint;
                    }
                }
            }

            return targetPoint + targetPoint - currentPosition;
        }

        double SolveQuadratic(double a, double b, double c)
        {
            double u = (b * b) - (4 * a * c);
            if (u < 0)
            {
                return Double.NaN;
            }
            u = Math.Sqrt(u);

            double t1 = (-b + u) / (2 * a);
            double t2 = (-b - u) / (2 * a);
            return (t1 > 0 ? (t2 > 0 ? (t1 < t2 ? t1 : t2) : t1) : t2);
        }

        void GetMessages()
        {
            string status = "";
            //Vector3D missilePos = Vector3D.Zero;
            double missileVel = 0;
            double missileDistanceFromTarget = 0;
            long missileId = 0;
            bool received = false;

            if (UNILISTENER.HasPendingMessage)
            {
                while (UNILISTENER.HasPendingMessage)
                {
                    var igcMessage = UNILISTENER.AcceptMessage();

                    missileId = igcMessage.Source;

                    int count = 0;
                    foreach (long id in MissileIDs)
                    {
                        if (id == missileId)
                        {
                            count++;
                        }
                    }
                    if (count == 0)
                    {
                        MissileIDs.Add(missileId);
                    }

                    if (igcMessage.Data is ImmutableArray<MyTuple<string, Vector3D, double, double>>)
                    {
                        var data = (ImmutableArray<MyTuple<string, Vector3D, double, double>>)igcMessage.Data;

                        status = data[0].Item1;
                        //missilePos = data[0].Item2;
                        missileVel = data[0].Item3;
                        missileDistanceFromTarget = data[0].Item4;

                        received = true;
                    }
                }
            }

            //missileLog.Clear();
            missileLog.Append("Active Missiles: ").Append(MissileIDs.Count().ToString()).Append("\n");
            if (received)
            {
                missileLog.Append("Missile ID: ").Append(missileId.ToString()).Append("\n");
                missileLog.Append("Missile status: ").Append(status.ToString()).Append("\n");
                missileLog.Append("Dist. From Target: ").Append(missileDistanceFromTarget.ToString()).Append("\n");
                missileLog.Append("Missile Speed: ").Append(missileVel.ToString()).Append("\n");
            }
        }

        void SendMissileBroadcastMessage(string cmd)
        {
            Vector3D targetPos;
            if (TARGET.HitPosition.HasValue)
            {
                targetPos = TARGET.HitPosition.Value;
            }
            else
            {
                targetPos = TARGET.Position;
            }

            long fakeId = 0;
            var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D>,
                MyTuple<Vector3, Vector3D>>>();

            var tuple = MyTuple.Create(MyTuple.Create(fakeId, cmd, CONTROLLER.CubeGrid.WorldVolume.Center),
              MyTuple.Create(TARGET.Velocity, targetPos));

            immArray.Add(tuple);

            IGC.SendBroadcastMessage(missileAntennaTag, immArray.ToImmutable());
        }

        void SendMissileUnicastMessage(string cmd)
        {
            Vector3D targetPos;
            if (TARGET.HitPosition.HasValue)
            {
                targetPos = TARGET.HitPosition.Value;
            }
            else
            {
                targetPos = TARGET.Position;
            }

            List<long> lostMissiles = new List<long>();
            foreach (long id in MissileIDs)
            {
                var immArray = ImmutableArray.CreateBuilder<MyTuple<MyTuple<long, string, Vector3D>,
                    MyTuple<Vector3, Vector3D>>>();

                var tuple = MyTuple.Create(MyTuple.Create(id, cmd, CONTROLLER.CubeGrid.WorldVolume.Center),
                    MyTuple.Create(TARGET.Velocity, targetPos));

                immArray.Add(tuple);

                bool uniMessageSent = IGC.SendUnicastMessage(id, missileAntennaTag, immArray.ToImmutable());

                if (!uniMessageSent)
                {
                    lostMissiles.Add(id);
                }
            }
            foreach (long id in lostMissiles)
            {
                MissileIDs.Remove(id);
            }
        }

        void UnlockShip()
        {
            foreach (IMyGyro block in GYROS) { block.GyroOverride = false; }
        }

        void TurnAlarmOn()
        {
            foreach (IMySoundBlock block in ALARMS)
            {
                //block.Enabled = true;
                block.Play();
            }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = true; }
        }

        void TurnAlarmOff()
        {
            foreach (IMySoundBlock block in ALARMS)
            {
                block.Stop();
                //block.Enabled = false;
            }
            foreach (IMyLightingBlock block in LIGHTS) { block.Enabled = false; }
        }

        bool CheckProjectors()
        {
            bool completed = false;
            int blocksCount = 0;
            foreach (IMyProjector block in TEMPPROJECTORS)
            {
                blocksCount += block.BuildableBlocksCount;
            }
            if (blocksCount == 0)
            {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = false; }
                completed = true;
            }
            else
            {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = true; }
            }
            return completed;
        }

        void ManageGuns(double distanceFromTarget)
        {
            if (distanceFromTarget < 800)
            {
                if (!shootOnce)
                {
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    shootOnce = true;
                }
            }
            else
            {
                if (shootOnce)
                {
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                    shootOnce = false;
                }
            }
        }

        bool LoadMissiles()
        {
            bool allFilled = false;
            int filled = 0;
            for (int i = 1; i <= missilesCount; i++)
            {
                MISSILEBLOCKSWITHINVENTORY.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(MISSILEBLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(missilePrefix + i) && !(block is IMyShipConnector) && !(block is IMyCargoContainer) && !(block is IMyGasTank));
                MISSILEINVENTORIES.Clear();
                MISSILEINVENTORIES.AddRange(MISSILEBLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));

                foreach (IMyInventory inventory in MISSILEINVENTORIES)
                {
                    if (!inventory.IsFull)
                    {
                        MyFixedPoint availableVolume = inventory.MaxVolume - inventory.CurrentVolume;
                        if (inventory.CanItemsBeAdded(availableVolume, iceOre))
                        {
                            foreach (IMyInventory sourceInventory in INVENTORIES)
                            {
                                MyInventoryItem? itemFound = sourceInventory.FindItem(iceOre);
                                if (itemFound.HasValue)
                                {
                                    MyFixedPoint itemAmount = sourceInventory.GetItemAmount(iceOre);
                                    if (sourceInventory.CanTransferItemTo(inventory, iceOre))
                                    {
                                        sourceInventory.TransferItemTo(inventory,
                                            itemFound.Value,
                                            Math.Min(availableVolume.ToIntSafe(), itemAmount.ToIntSafe()));
                                        if (inventory.IsFull)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (inventory.CanItemsBeAdded(availableVolume, missileAmmo))
                        {
                            foreach (IMyInventory sourceInventory in INVENTORIES)
                            {
                                MyInventoryItem? itemFound = sourceInventory.FindItem(missileAmmo);
                                if (itemFound.HasValue)
                                {
                                    MyFixedPoint itemAmount = sourceInventory.GetItemAmount(missileAmmo);
                                    if (sourceInventory.CanTransferItemTo(inventory, missileAmmo))
                                    {
                                        sourceInventory.TransferItemTo(inventory,
                                            itemFound.Value,
                                            Math.Min(availableVolume.ToIntSafe(), itemAmount.ToIntSafe()));
                                        if (inventory.IsFull)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (inventory.CanItemsBeAdded(availableVolume, gatlingAmmo))
                        {
                            foreach (IMyInventory sourceInventory in INVENTORIES)
                            {
                                MyInventoryItem? itemFound = sourceInventory.FindItem(gatlingAmmo);
                                if (itemFound.HasValue)
                                {
                                    MyFixedPoint itemAmount = sourceInventory.GetItemAmount(gatlingAmmo);
                                    if (sourceInventory.CanTransferItemTo(inventory, gatlingAmmo))
                                    {
                                        sourceInventory.TransferItemTo(inventory,
                                            itemFound.Value,
                                            Math.Min(availableVolume.ToIntSafe(), itemAmount.ToIntSafe()));
                                        if (inventory.IsFull)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        filled++;
                    }
                }
            }
            if (filled == MISSILEINVENTORIES.Count)
            {
                allFilled = true;
            }
            return allFilled;
        }

        void WriteInfo()
        {
            foreach (IMyTextSurface surface in SURFACES)
            {
                StringBuilder text = new StringBuilder("");
                text.Append(targetLog.ToString());
                text.Append(missileLog.ToString());
                surface.WriteText(text);
            }
        }

        void ReadTargetInfo()
        {
            //targetLog.Clear();
            targetLog.Append("Launching: ").Append(missileAntennasName).Append(selectedMissile.ToString()).Append("\n");
            if (!missilesLoaded)
            {
                targetLog.Append("Missiles Not Loaded\n");
            }
            else
            {
                targetLog.Append("Missiles Loaded\n");
            }

            targetLog.Append("ID: ").Append(TARGET.EntityId.ToString()).Append("\n");

            targetLog.Append("Name: ").Append(TARGET.Name).Append("\n");

            long targetLastDetected = TARGET.TimeStamp / 1000;
            targetLog.Append("Detected Since: ").Append(targetLastDetected).Append(" s\n");

            targetLog.Append("Speed: ").Append(TARGET.Velocity.Length().ToString("0.0")).Append("\n");

            double targetDistance = Vector3D.Distance(TARGET.Position, CONTROLLER.CubeGrid.WorldVolume.Center);
            targetLog.Append("Distance: ").Append(targetDistance.ToString("0.0")).Append("\n");

            double targetRadius = Vector3D.Distance(TARGET.BoundingBox.Min, TARGET.BoundingBox.Max);
            targetLog.Append("Radius: ").Append(targetRadius.ToString("0.0")).Append("\n");

            string targX = TARGET.Position.X.ToString("0.00");
            string targY = TARGET.Position.Y.ToString("0.00");
            string targZ = TARGET.Position.Z.ToString("0.00");
            targetLog.Append($"X:{targX} Y:{targY} Z:{targZ}").Append("\n");
        }

        void GetBlocks()
        {
            LIDARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(LIDARS, b => b.CustomName.Contains(lidarsName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CustomName.Contains(controllersName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(turretsName));
            LIGHTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(LIGHTS, b => b.CustomName.Contains(lightsName));
            ALARMS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(ALARMS, b => b.CustomName.Contains(alarmsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, b => b.CustomName.Contains(gyrosName));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, b => b.CustomName.Contains(weldersName));
            PROJECTORSMISSILES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSMISSILES, b => b.CustomName.Contains(projectorsMissilesName));
            PROJECTORSDRONES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDRONES, b => b.CustomName.Contains(projectorsDronesName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, b => b.CustomName.Contains(cockpitsName));
            ROCKETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(ROCKETS, b => b.CustomName.Contains(rocketsName));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(GATLINGS, b => b.CustomName.Contains(gatlingsName));
            BLOCKSWITHINVENTORY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(BLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(shipPrefix)); //&& block.IsSameConstructAs(Me)
            INVENTORIES.Clear();
            INVENTORIES.AddRange(BLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));

            SURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdsTargetName));
            foreach (IMyTextPanel panel in panels)
            {
                SURFACES.Add(panel as IMyTextSurface);
            }

            ANTENNA = GridTerminalSystem.GetBlockWithName(antennasName) as IMyRadioAntenna;
            CONTROLLER = CONTROLLERS[0];

            MAGNETICDRIVEPB = GridTerminalSystem.GetBlockWithName(magneticDriveName) as IMyProgrammableBlock;
        }

        void SetBlocks()
        {
            foreach (IMyCameraBlock cam in LIDARS)
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }

            foreach (IMyGyro block in GYROS)
            {
                block.Enabled = true;
                block.Yaw = 0f;
                block.Pitch = 0f;
                block.Roll = 0f;
                block.GyroOverride = false;
            }

            ANTENNA.Enabled = true;
            ANTENNA.EnableBroadcasting = true;
        }

        void GetMissileAntennas()
        {
            MISSILEANTENNAS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(MISSILEANTENNAS, b => b.CustomName.Contains(missileAntennasName));
        }

        void SetMissileAntennas()
        {
            foreach (IMyRadioAntenna block in MISSILEANTENNAS)
            {
                block.EnableBroadcasting = false;
                block.Enabled = false;
            }
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
