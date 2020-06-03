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
        // spyral doesn't work

        readonly string antennaName = "A [M]1";
        readonly string missileTag = "[M]1";
        readonly string rocketsName = "Rocket";
        readonly string gatlingsName = "Gatling";
        readonly string turretsName = "Turret";
        readonly string forwardThrustersName = "Frw";
        readonly string sideThrustersName = "Sd";
        readonly string backwardThrustersName = "Bkw";
        readonly string upwardThrustersName = "Upw";
        readonly string downwardThrustersName = "Dnw";

        readonly string antennaTag = "[MISSILE]";
        readonly string platformTag = "[RELAY]";

        const string commandLaunch = "Launch";
        const string commandUpdate = "Update";
        const string commandLost = "Lost";

        readonly int missileType = 0;   //0 kinetic - 1 explosive - 2 drone
        readonly int weaponType = 0;    //0 None - 1 Rockets - 2 Gatlings
        readonly int startThrustersDelay = 50;
        readonly int startTargetingDelay = 100;
        readonly bool useSpiral = false;
        readonly bool useRoll = false;
        readonly float spiralStart = 1000f; // distance to target at which missile starts to spiral
        readonly double maxSpiralTime = 3;  // # seconds for 1 full rotation
        readonly double spiralAngle = 5;    // deviation from aim vector
        readonly bool excludeFriendly = true;
        readonly double rocketProjectileForwardOffset = 4;  //By default, rockets are spawn 4 meters in front of the rocket launcher's tip
        readonly double rocketProjectileInitialSpeed = 100;
        readonly double rocketProjectileAccelleration = 600;
        readonly double rocketProjectileMaxSpeed = 200;
        readonly double rocketProjectileMaxRange = 800;
        readonly double gatlingProjectileForwardOffset = 0;
        readonly double gatlingProjectileInitialSpeed = 400;
        readonly double gatlingProjectileAccelleration = 0;
        readonly double gatlingProjectileMaxSpeed = 400;

        const double brakingAngleTolerance = 10;    //degrees
        const double rad2deg = 180 / Math.PI;
        const double toRadians = Math.PI / 180.0;

        float globalTimestep = 1.0f / 60.0f;
        int currentTick = 1;
        double maxSpeed = 99;
        bool isLargeGrid = false;
        double fuseDistance = 7;
        string status = "";
        bool launched = false;
        bool init = false;
        long platFormId;
        string command = "";
        bool startThrusters = false;
        int countStartThrusters = 0;
        bool startTargeting = false;
        int countStartTargeting = 0;
        bool startThrustersOnce = true;
        bool launchOnce = true;
        bool lostOnce = true;
        bool updateOnce = true;
        bool approaching = true;
        bool rightDistance = true;
        bool tooClose = true;
        bool tooFar = true;
        bool rightAltitude = true;
        bool tooAbove = true;
        bool tooBelow = true;
        double spiralTime = 0;
        double rollSpeed = 0;   // maximum of 3

        Vector3D platformPosition;
        Vector3D prevVelocity = new Vector3();
        Vector3D currentAcceleration;
        Vector3D targetPosition;
        Vector3 targetVelocity;
        Vector3 prevTargetVelocity = new Vector3();
        Vector3D targetAccelerationVector;

        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyThrust> ALLTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> SIDETHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> BACKWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> UPWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> DOWNWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyShipMergeBlock> MERGES = new List<IMyShipMergeBlock>();
        public List<IMyWarhead> WARHEADS = new List<IMyWarhead>();
        public List<IMyPowerProducer> GENERATORS = new List<IMyPowerProducer>();
        public List<IMyShipConnector> CONNECTORS = new List<IMyShipConnector>();
        public List<IMyTerminalBlock> TBLOCKS = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> ROCKETS = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> GATLINGS = new List<IMyTerminalBlock>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();

        IMyRadioAntenna ANTENNA;
        IMyShipController CONTROLLER;

        public IMyUnicastListener UNICASTLISTENER;
        public IMyBroadcastListener BROADCASTLISTENER;

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
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(antennaTag);
            UNICASTLISTENER = IGC.UnicastListener;

            GetAntenna();
            ANTENNA.Enabled = false;
            GetBlocks();
            foreach (IMyThrust block in ALLTHRUSTERS) { block.Enabled = false; }

            InitPIDControllers(CONTROLLER as IMyTerminalBlock);

            isLargeGrid = CONTROLLER.CubeGrid.GridSizeEnum == MyCubeSize.Large;
            fuseDistance = isLargeGrid ? 16 : 7;
        }

        void Main(string argument, UpdateType updateSource)
        {
            Echo($"TBLOCKS:{TBLOCKS.Count}");
            Echo($"CONTROLLERS:{CONTROLLERS.Count}");
            Echo($"GYROS:{GYROS.Count}");
            Echo($"THRUSTERS:{THRUSTERS.Count}");
            Echo($"ALLTHRUSTERS:{ALLTHRUSTERS.Count}");
            Echo($"SIDETHRUSTERS:{SIDETHRUSTERS.Count}");
            Echo($"MERGES:{MERGES.Count}");
            Echo($"CONNECTORS:{CONNECTORS.Count}");
            Echo($"GENERATORS:{GENERATORS.Count}");
            Echo($"WARHEADS:{WARHEADS.Count}");
            Echo($"ROCKETS:{ROCKETS.Count}");
            Echo($"GATLINGS:{GATLINGS.Count}");
            Echo($"TURRETS:{TURRETS.Count}");

            if (ANTENNA.Enabled)
            {
                GetMessages();

                if (command.Equals(commandLaunch) && !launched)
                {
                    if (!init)
                    {
                        GetAntenna();
                        GetBlocks();
                        init = true;
                    }

                    if (launchOnce)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        UpdateGlobalTimeStep();
                        status = "Launched";
                        SendUnicastMessage();
                        PrepareForLaunch();
                        launched = true;

                        launchOnce = false;
                        updateOnce = true;
                        lostOnce = true;
                        startThrustersOnce = true;
                        approaching = true;
                        rightDistance = true;
                        tooClose = true;
                        tooFar = true;
                        rightAltitude = true;
                        tooAbove = true;
                        tooBelow = true;
                    }

                    UpdateBroadcastRange(platformPosition);
                }
                else if (command.Equals(commandUpdate))
                {
                    if (updateOnce)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        UpdateGlobalTimeStep();
                        status = "Cruising";
                        currentTick = 1;

                        updateOnce = false;
                        launchOnce = true;
                        lostOnce = true;
                        startThrustersOnce = true;
                        approaching = true;
                        rightDistance = true;
                        tooClose = true;
                        tooFar = true;
                        rightAltitude = true;
                        tooAbove = true;
                        tooBelow = true;
                    }

                    UpdateBroadcastRange(platformPosition);

                    SendUnicastMessage();

                    if (startThrusters)
                    {
                        if (startThrustersOnce)
                        {
                            foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                            startThrustersOnce = false;
                        }

                        if (startTargeting)
                        {
                            Update();

                            bool targetFound = false;
                            if (TURRETS.Count > 0)
                            {
                                foreach (IMyLargeTurretBase turret in TURRETS)
                                {
                                    if (!turret.GetTargetedEntity().IsEmpty())
                                    {
                                        MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                                        if (IsValidTarget(ref targ))
                                        {
                                            targetPosition = targ.Position;
                                            targetVelocity = targ.Velocity;
                                            currentTick = 1;
                                            if (!Vector3D.IsZero(targetVelocity))
                                            {
                                                targetAccelerationVector = (targetVelocity - prevTargetVelocity) / globalTimestep;
                                            }
                                            else
                                            {
                                                targetAccelerationVector = Vector3D.Zero;
                                            }
                                            targetFound = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!targetFound)
                            {
                                PredictTarget();
                            }

                            if (missileType == 0)
                            {
                                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                                if (useSpiral && distanceFromTarget <= spiralStart)
                                {
                                    double speed = CONTROLLER.GetShipVelocities().LinearVelocity.LengthSquared();
                                    Vector3D headingVec = ComputeInterceptPoint(targetPosition, targetVelocity, targetAccelerationVector, CONTROLLER.CubeGrid.WorldVolume.Center, speed, currentAcceleration.LengthSquared(), maxSpeed);
                                    if (THRUSTERS[0].ThrustOverridePercentage >= 0.9f && speed > 0 && headingVec.Length() / speed <= 2)
                                    {
                                        rollSpeed = 3;
                                        SpiralIntercept(headingVec);
                                    }
                                    else
                                    {
                                        LockOnTarget(CONTROLLER);
                                    }
                                }
                                else
                                {
                                    LockOnTarget(CONTROLLER);
                                }
                            }
                            else if (missileType == 1)
                            {
                                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                                if (distanceFromTarget <= fuseDistance)
                                {
                                    foreach (IMyWarhead block in WARHEADS) { block.Detonate(); }
                                }
                                LockOnTarget(CONTROLLER);
                            }
                            else if (missileType == 2)
                            {
                                LockOnTarget(CONTROLLER);

                                ManageDrone();
                            }

                            currentTick++;
                            prevTargetVelocity = targetVelocity;
                            prevVelocity = CONTROLLER.GetShipVelocities().LinearVelocity;
                        }
                        else
                        {
                            if (countStartTargeting > startTargetingDelay) { startTargeting = true; }
                            countStartTargeting++;
                        }
                    }
                    else
                    {
                        if (countStartThrusters > startThrustersDelay) { startThrusters = true; }
                        countStartThrusters++;
                    }
                }
                else if (command.Equals(commandLost))
                {
                    if (lostOnce)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        UpdateGlobalTimeStep();
                        foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                        foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                        foreach (IMyThrust block in ALLTHRUSTERS) { block.ThrustOverride = 0f; }
                        status = "Lost";
                        //SendUnicastMessage();

                        lostOnce = false;
                        updateOnce = true;
                        launchOnce = true;
                        startThrustersOnce = true;
                        approaching = true;
                        rightDistance = true;
                        tooClose = true;
                        tooFar = true;
                        rightAltitude = true;
                        tooAbove = true;
                        tooBelow = true;
                    }

                    SendUnicastMessage();

                    UpdateBroadcastRange(platformPosition);

                    Vector3D velocityVec = CONTROLLER.GetShipVelocities().LinearVelocity;
                    double speedSquared = velocityVec.Length();
                    if (speedSquared > 1)
                    {
                        StartBraking(velocityVec);
                    }
                    else
                    {
                        foreach (IMyGyro gyro in GYROS)
                        {
                            gyro.Yaw = 0f;
                            gyro.Pitch = 0f;
                            gyro.Roll = 0f;
                            gyro.GyroOverride = false;
                        }
                    }
                }
            }
        }

        void GetMessages()
        {
            if (UNICASTLISTENER.HasPendingMessage)
            {
                while (UNICASTLISTENER.HasPendingMessage)
                {
                    var msg = UNICASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D>,
                            MyTuple<Vector3, Vector3D>>>)
                    {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D>,
                                    MyTuple<Vector3, Vector3D>>>)msg.Data;

                        currentTick = 1;

                        platFormId = msg.Source;//platformTag = msg.Tag;

                        for (int i = 0; i < data.Length; i++)
                        {
                            var temp = data[i];

                            var tup1 = temp.Item1;
                            var tup2 = temp.Item2;

                            command = tup1.Item2;
                            platformPosition = tup1.Item3;

                            targetVelocity = tup2.Item1;
                            targetPosition = tup2.Item2;
                        }
                    }
                }
            }
            else if (BROADCASTLISTENER.HasPendingMessage)
            {
                while (BROADCASTLISTENER.HasPendingMessage)
                {
                    var msg = BROADCASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D>,
                            MyTuple<Vector3, Vector3D>>>)
                    {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D>,
                                    MyTuple<Vector3, Vector3D>>>)msg.Data;

                        currentTick = 1;

                        platFormId = msg.Source;//platformTag = msg.Tag;

                        for (int i = 0; i < data.Length; i++)
                        {
                            var temp = data[i];

                            var tup1 = temp.Item1;
                            var tup2 = temp.Item2;

                            long myId = tup1.Item1;
                            string cmd = tup1.Item2;
                            if (cmd.Equals(commandLaunch) && status.Equals(""))
                            {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;

                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                            }
                            else if (myId == Me.EntityId)
                            {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;

                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                            }
                        }
                    }
                }
            }
        }

        void SendUnicastMessage()
        {
            Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center;
            double distanceFromTarget = Vector3D.Distance(targetPosition, position);
            double speed = CONTROLLER.GetShipSpeed();
            string info = command + " " + status;

            var immArray = ImmutableArray.CreateBuilder<MyTuple<string, Vector3D, double, double>>();

            var tuple = MyTuple.Create(info, position, speed, distanceFromTarget);

            immArray.Add(tuple);

            IGC.SendUnicastMessage(platFormId, platformTag, immArray.ToImmutable());
        }

        void LockOnTarget(IMyShipController REF)
        {
            MatrixD refWorldMatrix = REF.WorldMatrix;
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetPos = targetPosition + (targetVelocity * elapsedTime);

            if (Vector3D.IsZero(prevTargetVelocity))
            {
                prevTargetVelocity = targetVelocity;
            }

            Vector3D targetAccel = Vector3D.Zero;
            if ((!Vector3D.IsZero(targetVelocity) || !Vector3D.IsZero(prevTargetVelocity)) && !Vector3D.IsZero((targetVelocity - prevTargetVelocity)))
            {
                targetAccel = (targetVelocity - prevTargetVelocity) / elapsedTime;
            }

            Vector3D aimDirection;
            switch (weaponType)
            {
                case 0://none
                    aimDirection = ComputeInterceptPoint(targetPos, targetVelocity - REF.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
                    break;
                case 1://rockets
                    aimDirection = ComputeInterceptPointWithInheritSpeed(targetPos, targetVelocity, targetAccel, (rocketProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * rocketProjectileForwardOffset)), REF.GetShipVelocities().LinearVelocity, rocketProjectileInitialSpeed, rocketProjectileAccelleration, rocketProjectileMaxSpeed, rocketProjectileMaxRange);
                    break;
                case 2://gatlings
                    aimDirection = ComputeInterceptPoint(targetPos, targetVelocity - REF.GetShipVelocities().LinearVelocity, targetAccel, (gatlingProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * gatlingProjectileForwardOffset)), gatlingProjectileInitialSpeed, gatlingProjectileAccelleration, gatlingProjectileMaxSpeed);
                    break;
                default:
                    aimDirection = ComputeInterceptPoint(targetPos, targetVelocity - REF.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
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

        Vector3D ComputeInterceptPoint(Vector3D targetPos, Vector3D targetVel, Vector3D targetAccel, Vector3D projectilePosition, double projectileInitialSpeed, double projectileAcceleration, double projectileMaxSpeed)
        {
            Vector3D z = targetPos - projectilePosition;
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

        Vector3D ComputeInterceptPointWithInheritSpeed(Vector3D targetPos, Vector3D targetVel, Vector3D targetAccel, Vector3D projectilePosition, Vector3D direction, double projectileInitialSpeed, double projectileAcceleration, double projectileMaxSpeed, double projectileMaxRange)
        {
            Vector3D z = targetPos - projectilePosition;
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

            aimDirection = Vector3D.Normalize(targetPoint - projectilePosition);
            stepAcceleration = (aimDirection * projectileAcceleration) / 60;

            currentPosition = projectilePosition;
            currentDirection = direction + (aimDirection * projectileInitialSpeed);

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
                    if (Vector3D.Distance(projectilePosition, currentPosition) > projectileMaxRange)
                    {
                        return targetPoint;
                    }
                }
            }

            return targetPoint + targetPoint - currentPosition;
        }

        public double SolveQuadratic(double a, double b, double c)
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

        void StartBraking(Vector3D velocityVec)
        {
            Vector3D forwardVec = CONTROLLER.WorldMatrix.Forward;
            Vector3D leftVec = CONTROLLER.WorldMatrix.Left;
            Vector3D upVec = CONTROLLER.WorldMatrix.Up;

            double yawAngle, pitchAngle;
            GetRotationAngles(-velocityVec, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            ApplyGyroOverride(pitchSpeed, yawSpeed, 0);

            double brakingAngle = GetAngleBetween(forwardVec, -velocityVec);
            if (brakingAngle * rad2deg <= brakingAngleTolerance)
            {
                if (!CONTROLLER.DampenersOverride)
                {
                    CONTROLLER.DampenersOverride = true;
                }
            }
            else
            {
                if (CONTROLLER.DampenersOverride)
                {
                    CONTROLLER.DampenersOverride = false;
                }
            }
        }

        public void SpiralIntercept(Vector3D headingVec)
        {
            GetSpiralHeading(spiralAngle, headingVec, CONTROLLER.WorldMatrix.Forward, CONTROLLER.WorldMatrix.Up, out headingVec);
            headingVec = CalculateHeadingVector(CONTROLLER.GetShipVelocities().LinearVelocity, headingVec);
            ApplyRotation(headingVec);
        }

        public void GetSpiralHeading(double angle, Vector3D axisVec, Vector3D fwdVec, Vector3D upVec, out Vector3D rotVec)
        {
            double radius = Math.Tan(angle * toRadians);
            Vector3D axis_Norm = Vector3D.Normalize(axisVec);

            if ((spiralTime += globalTimestep) > maxSpiralTime)
            {
                spiralTime = 0;
            }

            double theta = MathHelper.TwoPi * spiralTime / maxSpiralTime;

            if (fwdVec.Dot(axis_Norm) > 0)
            {
                Vector3D cross_X = Vector3D.Normalize(upVec.Cross(axis_Norm));
                Vector3D cross_Y = Vector3D.Normalize(cross_X.Cross(axis_Norm));
                Vector3D rotation = radius * (cross_X * Math.Cos(theta) + cross_Y * Math.Sin(theta));
                rotVec = axis_Norm + rotation;
            }
            else
            {
                rotVec = axis_Norm;
            }
        }

        void ApplyRotation(Vector3D headingVec)
        {
            double yawAngle, pitchAngle;
            GetRotationAngles(headingVec, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            double rollSd = 0;
            if (useRoll)
            {
                rollSd = rollSpeed;
            }

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSd);
        }

        public Vector3D CalculateHeadingVector(Vector3D vel, Vector3D interceptVec)
        {
            var pos = CONTROLLER.CubeGrid.WorldVolume.Center;
            double altitude;// Check altitude vs distance to target - pull up if closer to ground than target, unless target is on the ground
            if (CONTROLLER.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude))
            {
                if (altitude < 50 && interceptVec.Dot(CONTROLLER.GetNaturalGravity()) < 0)
                {
                    double dSqd = interceptVec.LengthSquared();

                    if (altitude * altitude < dSqd)
                    {
                        return Vector3D.Normalize(-CONTROLLER.GetNaturalGravity()) * Math.Min(Math.Sqrt(dSqd), 50) + interceptVec;
                    }
                }
            }

            if (vel.LengthSquared() < 1000 || vel.Dot(interceptVec) < 0)
            {
                return interceptVec;
            }

            return ReflectVector(vel, interceptVec);
        }

        public static Vector3D ReflectVector(Vector3D a, Vector3D b, double reflectionFactor = 3)
        {
            Vector3D proj_a = ProjectVector(a, b);
            Vector3D aOrth_b = a - proj_a;
            return (proj_a - aOrth_b * reflectionFactor);
        }

        public static Vector3D ProjectVector(Vector3D a, Vector3D b)// Project a onto b
        {
            return a.Dot(b) / b.LengthSquared() * b;
        }

        void PrepareForLaunch()
        {
            foreach (IMyPowerProducer block in GENERATORS)
            {
                block.Enabled = true;
                if (block is IMyBatteryBlock) { (block as IMyBatteryBlock).ChargeMode = ChargeMode.Discharge; }
            }
            foreach (IMyShipMergeBlock item in MERGES) { item.Enabled = false; }
            foreach (IMyShipConnector item in CONNECTORS) { item.Enabled = false; }
            foreach (IMyThrust item in ALLTHRUSTERS) { item.Enabled = true; }
        }

        void PredictTarget()
        {
            if (!Vector3.IsZero(targetVelocity))
            {
                float elapsedTime = currentTick * globalTimestep;
                targetPosition += targetVelocity * elapsedTime;
                targetAccelerationVector = (Vector3D)(targetVelocity - prevTargetVelocity) / elapsedTime;
            }
            else
            {
                targetAccelerationVector = Vector3D.Zero;
            }
        }

        void Update()
        {
            double speed = CONTROLLER.GetShipVelocities().LinearVelocity.LengthSquared();
            if (speed > maxSpeed)
            {
                maxSpeed = speed;
            }
            currentAcceleration = (CONTROLLER.GetShipVelocities().LinearVelocity - prevVelocity) / globalTimestep;
        }

        void UpdateGlobalTimeStep()
        {
            float tick = 1.0f / 60.0f;
            if ((Runtime.UpdateFrequency & UpdateFrequency.Update1) != 0) { globalTimestep = tick; }
            else if ((Runtime.UpdateFrequency & UpdateFrequency.Update10) != 0) { globalTimestep = tick * 10; }
            else if ((Runtime.UpdateFrequency & UpdateFrequency.Update100) != 0) { globalTimestep = tick * 100; }
        }

        bool IsValidTarget(ref MyDetectedEntityInfo entityInfo)
        {
            if (entityInfo.Type != MyDetectedEntityType.Asteroid && entityInfo.Type != MyDetectedEntityType.Planet)
            {
                if (!excludeFriendly || IsNotFriendly(entityInfo.Relationship))
                {
                    return true;
                }
            }
            return false;
        }

        bool IsNotFriendly(VRage.Game.MyRelationsBetweenPlayerAndBlock relationship)
        {
            return (relationship != VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare && relationship != VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner);
        }

        void UpdateBroadcastRange(Vector3D platformPosition)
        {
            var distance = Vector3.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            ANTENNA.Radius = distance + 100;
        }

        void ManageDrone()
        {
            double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            if (distanceFromTarget >= 600 && distanceFromTarget < 800)
            {
                if (approaching)
                {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    approaching = false;
                    rightDistance = true;
                    tooClose = true;
                    tooFar = true;
                }
            }
            else if (distanceFromTarget > 400 && distanceFromTarget < 600)
            {
                if (rightDistance)
                {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    approaching = true;
                    rightDistance = false;
                    tooClose = true;
                    tooFar = true;
                }
            }
            else if (distanceFromTarget <= 400)
            {
                if (tooClose)
                {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_On")) { block.ApplyAction("Shoot_On"); } }
                    approaching = true;
                    rightDistance = true;
                    tooClose = false;
                    tooFar = true;
                }
            }
            else
            {
                if (tooFar)
                {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                    foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
                    approaching = true;
                    rightDistance = true;
                    tooClose = true;
                    tooFar = false;
                }
            }

            Vector3D planetPosition;
            if (CONTROLLER.TryGetPlanetPosition(out planetPosition))
            {
                Vector3D myAltitude = CONTROLLER.CubeGrid.WorldVolume.Center - planetPosition;
                Vector3D targetAltitude = targetPosition - planetPosition;
                double altitude = myAltitude.LengthSquared();
                double targAltitude = targetAltitude.LengthSquared();
                if (altitude < targAltitude - 3.0)
                {
                    if (tooBelow)
                    {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        tooBelow = false;
                        tooAbove = true;
                        rightAltitude = true;
                    }
                }
                else if (altitude > targAltitude + 3.0)
                {
                    if (tooAbove)
                    {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                        tooBelow = true;
                        tooAbove = false;
                        rightAltitude = true;
                    }
                }
                else
                {
                    if (rightAltitude)
                    {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        tooBelow = true;
                        tooAbove = true;
                        rightAltitude = false;
                    }
                }

                Vector3D grav = CONTROLLER.GetNaturalGravity();
                Vector3D downVector = CONTROLLER.WorldMatrix.Down;
                double rollAngle = GetAngleBetween(grav, downVector);
                if (rollAngle != 0)
                {
                    double rollSpeed = rollController.Control(rollAngle, globalTimestep);
                    Vector3D rotationVec = new Vector3D(0, 0, rollSpeed);
                    MatrixD refMatrix = CONTROLLER.WorldMatrix;
                    Vector3D relativeRotationVec = Vector3D.TransformNormal(rotationVec, refMatrix);
                    foreach (IMyGyro gyro in GYROS)
                    {
                        Vector3D transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyro.WorldMatrix));
                        gyro.Pitch = (float)transformedRotationVec.X;
                        gyro.Yaw = (float)transformedRotationVec.Y;
                        gyro.Roll = (float)transformedRotationVec.Z;
                        if (!gyro.GyroOverride)
                        {
                            gyro.GyroOverride = true;
                        }
                    }
                }
            }
        }

        void GetAntenna()
        {
            ANTENNA = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
        }

        void GetBlocks()
        {
            TBLOCKS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TBLOCKS, b => b.CustomName.Contains(missileTag));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CustomName.Contains(missileTag));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, b => b.CustomName.Contains(missileTag));
            ALLTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(ALLTHRUSTERS, b => b.CustomName.Contains(missileTag));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(forwardThrustersName));
            SIDETHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(SIDETHRUSTERS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(sideThrustersName));
            BACKWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(BACKWARDTHRUSTERS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(backwardThrustersName));
            UPWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(UPWARDTHRUSTERS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(upwardThrustersName));
            DOWNWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(DOWNWARDTHRUSTERS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(downwardThrustersName));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, b => b.CustomName.Contains(missileTag));
            GENERATORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(GENERATORS, b => b.CustomName.Contains(missileTag));
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, b => b.CustomName.Contains(missileTag));
            CONNECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(CONNECTORS, b => b.CustomName.Contains(missileTag));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(GATLINGS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(gatlingsName));
            ROCKETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(ROCKETS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(rocketsName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(turretsName));

            CONTROLLER = CONTROLLERS[0];
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
