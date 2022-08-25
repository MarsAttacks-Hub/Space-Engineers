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

namespace IngameScript {
    partial class Program : MyGridProgram {
        //TODO
        //add missile beam ride guidance
        //HOMING MISSILE
        readonly string antennaName = "A [M]1";
        readonly string missilePrefix = "[M]1";
        readonly string sideThrustersName = "Rwr";//Drone 1 Rrw - Drone 2 Lrw

        readonly int weaponType = 0;//0 None - 1 Rockets - 2 Gatlings
        readonly double spiralDegrees = 2d;//radius of the spiral pattern
        readonly double timeMaxSpiral = 3d;//time it takes the missile to complete a full spiral cycle
        readonly double navConstant = 5d;//Recommended value is 3-5 Higher values make the missile compensate faster but can lead to more overshoot/instability
        readonly double navAccelConstant = 0d;
        readonly float globalTimestep = 10.0f / 60.0f;//UpdateFrequency.Update10

        int missileType = 1;//0 kinetic - 1 explosive - 2 drone
        bool useSpiral = false;//enable/disable spiral guidance
        bool creative = true;
        bool isLargeGrid = false;
        bool launched = false;
        bool init = false;
        bool startThrusters = false;
        bool startTargeting = false;
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
        bool readyToFire = false;
        bool readyToFireOnce = true;
        double fuseDistance = 7d;
        double maxSpeed = 99d;
        double timeSpiral = 0d;
        double timeSinceLastMessage = 0d;
        long platFormId;
        int countStartThrusters = 0;
        int countStartTargeting = 0;
        int checkLoad = 0;
        string status = "";
        string command = "";

        const double updatesPerSecond = 6d;//UpdateFrequency.Update10
        const double deg2Rad = Math.PI / 180d;
        const double rad2deg = 180d / Math.PI;
        const double angleTolerance = 2d;//degrees
        //readonly double missileSpinRPM = 0d;//this specifies how fast the missile will spin when flying(only in space)
        //const double rpm2Rad = Math.PI / 30;
        //const double gyroSlowdownAngle = Math.PI / 36;

        Vector3D platformPosition;
        Vector3D targetPosition;
        Vector3D targetVelocity;
        Vector3D prevTargetVelocity = new Vector3D();

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
        public List<IMyPowerProducer> GENERATORS = new List<IMyPowerProducer>();//IMyGasGenerator
        public List<IMyShipConnector> CONNECTORS = new List<IMyShipConnector>();
        public List<IMyTerminalBlock> TBLOCKS = new List<IMyTerminalBlock>();
        public List<IMyUserControllableGun> ROCKETS = new List<IMyUserControllableGun>();
        public List<IMyUserControllableGun> GATLINGS = new List<IMyUserControllableGun>();
        public List<IMyLargeTurretBase> TURRETS = new List<IMyLargeTurretBase>();

        IMyRadioAntenna ANTENNA;
        IMyShipController CONTROLLER;

        IMyUnicastListener UNICASTLISTENER;
        IMyBroadcastListener BROADCASTLISTENER;

        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");

        PID yawController;
        PID pitchController;
        PID rollController;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[MISSILE]");
            UNICASTLISTENER = IGC.UnicastListener;

            GetAntenna();
            ANTENNA.Enabled = false;
            GetBlocks();
            foreach (IMyThrust block in ALLTHRUSTERS) { block.Enabled = false; }

            InitPIDControllers();

            isLargeGrid = CONTROLLER.CubeGrid.GridSizeEnum == MyCubeSize.Large;
            fuseDistance = isLargeGrid ? 16 : 7;
        }

        public void Main() {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (ANTENNA.Enabled) {

                    GetMessages();

                    Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center;
                    double speed = CONTROLLER.GetShipSpeed();

                    if (!launched && command.Equals("Launch")) {
                        if (!init) {
                            GetAntenna();
                            GetBlocks();
                            init = true;
                        }

                        if (launchOnce) {
                            InitiateLaunch(speed, position);
                        }

                        UpdateBroadcastRange(platformPosition, position);
                    } else if (launched && (command.Equals("Update")) || command.Equals("Spiral")) {

                        if (command.Equals("Spiral")) { useSpiral = !useSpiral; }

                        if (updateOnce) {
                            InitiateUpdate();
                        }

                        double timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds;
                        timeSinceLastMessage += timeSinceLastRun;

                        UpdateBroadcastRange(platformPosition, position);

                        SendUnicastMessage(speed, position);

                        if (!startTargeting) {
                            InitiateThrusters();
                        } else {
                            UpdateMaxSpeed(speed);

                            bool targetFound = TurretsDetection();

                            if (targetFound) { timeSinceLastMessage = timeSinceLastRun; }
                            ManageMissileType(timeSinceLastMessage, CONTROLLER.GetNaturalGravity(), position, CONTROLLER.GetShipVelocities().LinearVelocity);

                            prevTargetVelocity = targetVelocity;
                            //prevTargetPosition = targetPosition;
                            //prevPosition = position;
                        }
                    } else if (launched && command.Equals("Lost")) {
                        if (lostOnce) {
                            InitiateLost();
                        }

                        UpdateBroadcastRange(platformPosition, position);

                        SendUnicastMessage(speed, position);

                        ManageBrakes(CONTROLLER.GetShipVelocities().LinearVelocity);
                    }
                }
            } catch (Exception e) {
                StringBuilder debugLog = new StringBuilder("");
                debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                SendErrorMessage(debugLog.ToString());
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void GetMessages() {
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    MyIGCMessage msg = BROADCASTLISTENER.AcceptMessage();
                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>) {
                        ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>> data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>)msg.Data;
                        timeSinceLastMessage = 0d;
                        platFormId = msg.Source;//platformTag = msg.Tag;
                        for (int i = 0; i < data.Length; i++) {
                            MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>> temp = data[i];
                            MyTuple<long, string, Vector3D, MatrixD, bool> tup1 = temp.Item1;
                            MyTuple<Vector3D, Vector3D> tup2 = temp.Item2;
                            long myId = tup1.Item1;
                            string cmd = tup1.Item2;
                            if (cmd.Equals("Launch") && !launched) {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;
                                //platformMatrix = tup1.Item4;
                                creative = tup1.Item5;
                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                            }
                            if (myId == Me.EntityId) {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;
                                //platformMatrix = tup1.Item4;
                                creative = tup1.Item5;
                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                            }
                        }
                    }
                }
            }
            if (UNICASTLISTENER.HasPendingMessage) {
                while (UNICASTLISTENER.HasPendingMessage) {
                    MyIGCMessage msg = UNICASTLISTENER.AcceptMessage();
                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>) {
                        ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>> data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>>>)msg.Data;
                        timeSinceLastMessage = 0d;
                        platFormId = msg.Source;//platformTag = msg.Tag;
                        for (int i = 0; i < data.Length; i++) {
                            MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>, MyTuple<Vector3D, Vector3D>> temp = data[i];
                            MyTuple<long, string, Vector3D, MatrixD, bool> tup1 = temp.Item1;
                            MyTuple<Vector3D, Vector3D> tup2 = temp.Item2;
                            command = tup1.Item2;
                            platformPosition = tup1.Item3;
                            //platformMatrix = tup1.Item4;
                            creative = tup1.Item5;
                            targetVelocity = tup2.Item1;
                            targetPosition = tup2.Item2;
                        }
                    }
                }
            }
        }

        bool SendUnicastMessage(double speed, Vector3D position) {
            double distanceFromTarget = Vector3D.Distance(targetPosition, position);
            string type = "";
            if (missileType == 0) {//0 kinetic - 1 explosive - 2 drone
                type = "Kinetic";
            } else if (missileType == 1) {
                type = "Explosive";
            } else if (missileType == 2) {
                type = "Drone";
            }
            string info = "command=" + command + ",status=" + status + ",type=" + type;
            ImmutableArray<MyTuple<string, Vector3D, double, double>>.Builder immArray = ImmutableArray.CreateBuilder<MyTuple<string, Vector3D, double, double>>();
            MyTuple<string, Vector3D, double, double> tuple = MyTuple.Create(info, position, speed, distanceFromTarget);
            immArray.Add(tuple);
            bool messageSent = IGC.SendUnicastMessage(platFormId, "[PAINTER]", immArray.ToImmutable());
            return messageSent;
        }

        bool SendErrorMessage(String msg) {
            MyTuple<string, string> tuple = MyTuple.Create("ERROR", msg);
            bool messageSent = IGC.SendUnicastMessage(platFormId, "[PAINTER]", tuple);
            return messageSent;
        }

        void InitiateLaunch(double speed, Vector3D position) {
            SendUnicastMessage(speed, position);
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
            timeSinceLastMessage = 0d;
            status = "Launched";
        }

        void InitiateUpdate() {
            if (missileType == 1) {
                foreach (IMyWarhead warHead in WARHEADS) {
                    warHead.IsArmed = true;
                }
            }
            lostOnce = true;
            updateOnce = false;
            launchOnce = true;
            startThrustersOnce = true;
            approaching = true;
            rightDistance = true;
            tooClose = true;
            tooFar = true;
            rightAltitude = true;
            tooAbove = true;
            tooBelow = true;
            timeSinceLastMessage = 0d;
            status = "Cruising";
        }

        void InitiateLost() {
            foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
            foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
            foreach (IMyThrust block in ALLTHRUSTERS) { block.ThrustOverride = 0f; }
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
            timeSinceLastMessage = 0d;
            status = "Lost";
        }

        void InitiateThrusters() {
            if (startThrusters) {
                if (startThrustersOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    startThrustersOnce = false;
                }
                if (!startTargeting) {
                    if (countStartTargeting > 10) { startTargeting = true; }
                    countStartTargeting++;
                }
            } else {
                if (countStartThrusters > 5) { startThrusters = true; }
                countStartThrusters++;
            }
        }

        void PrepareForLaunch() {
            foreach (IMyPowerProducer block in GENERATORS) {
                block.Enabled = true;
                if (block is IMyBatteryBlock) { (block as IMyBatteryBlock).ChargeMode = ChargeMode.Auto; }
            }
            foreach (IMyShipMergeBlock block in MERGES) { block.Enabled = false; }
            foreach (IMyShipConnector block in CONNECTORS) { block.Disconnect(); }//block.Enabled = false; 
            foreach (IMyThrust block in ALLTHRUSTERS) { block.Enabled = true; }
        }

        void ManageMissileType(double timeSinceLastRun, Vector3D gravity, Vector3D position, Vector3D myVelocity) {
            if (missileType == 0) {//kinetic
                double missileMass = CONTROLLER.CalculateShipMass().PhysicalMass;
                double distanceFromTarget = Vector3D.Distance(targetPosition, position);
                double distanceFromShip = Vector3D.Distance(platformPosition, position);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                if (distanceFromTarget > 1000f && distanceFromShip < 200f) {
                    MissileGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                } else {
                    if (useSpiral && distanceFromTarget < 1000f && distanceFromShip > 200f) {
                        SpiralGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                    } else {
                        MissileGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                    }
                }
            } else if (missileType == 1) {//explosive
                double missileMass = CONTROLLER.CalculateShipMass().PhysicalMass;
                double distanceFromTarget = Vector3D.Distance(targetPosition, position);
                double distanceFromShip = Vector3D.Distance(platformPosition, position);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                if (distanceFromTarget <= fuseDistance) {
                    foreach (IMyWarhead block in WARHEADS) { block.Detonate(); }
                }
                if (distanceFromTarget > 1000f && distanceFromShip < 200f) {
                    MissileGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                } else {
                    if (useSpiral && distanceFromTarget < 1000f && distanceFromShip > 200f) {
                        SpiralGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                    } else {
                        MissileGuidance(timeSinceLastRun, gravity, myVelocity, missileMass);
                    }
                }
            } else if (missileType == 2) {//drone
                LockOnTarget(timeSinceLastRun, gravity, position, myVelocity);
                ManageDrone(gravity, position);
                if (!creative) {
                    if (checkLoad >= 100) {
                        bool hasAmmo = CheckAmmo();
                        double hasIce = CheckIce();
                        if (!hasAmmo || hasIce < 5d) {
                            missileType = 0;//suicide
                        }
                        checkLoad = 0;
                    }
                    checkLoad++;
                }
            }
        }

        bool CheckAmmo() {
            bool gatlingAmmoFound = false;
            List<IMyInventory> GATLINGSINVENTORIES = new List<IMyInventory>();
            GATLINGSINVENTORIES.AddRange(GATLINGS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            foreach (IMyInventory sourceInventory in GATLINGSINVENTORIES) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                sourceInventory.GetItems(items, item => item.Type.TypeId == gatlingAmmo.TypeId.ToString());
                if (items.Count > 0) {
                    gatlingAmmoFound = true;
                    break;
                }
            }
            return gatlingAmmoFound;
        }

        double CheckIce() {
            double currentVolume = 0d;
            double maxVolume = 0d;
            //double iceAmount = 0d;
            List<IMyInventory> GENERATORSINVENTORIES = new List<IMyInventory>();
            GENERATORSINVENTORIES.AddRange(GENERATORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            foreach (IMyInventory sourceInventory in GENERATORSINVENTORIES) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                sourceInventory.GetItems(items, item => item.Type.TypeId == iceOre.TypeId.ToString());
                if (items.Count > 0) {
                    currentVolume += (double)sourceInventory.CurrentVolume;
                    maxVolume += (double)sourceInventory.MaxVolume;
                    //foreach (var item in items) { iceAmount += (double)item.Amount; }
                }
            }
            double percent = 0d;
            if (maxVolume > 0d && currentVolume > 0d) {
                percent = currentVolume / maxVolume * 100d;
            }
            return percent;
        }

        void ManageDrone(Vector3D gravity, Vector3D position) {
            double distanceFromTarget = Vector3D.Distance(targetPosition, position);
            if (distanceFromTarget >= 600d && distanceFromTarget < 800d) {
                if (approaching) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    approaching = false;
                    rightDistance = true;
                    tooClose = true;
                    tooFar = true;
                }
            } else if (distanceFromTarget > 400d && distanceFromTarget < 600d) {
                if (rightDistance) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    approaching = true;
                    rightDistance = false;
                    tooClose = true;
                    tooFar = true;
                }
            } else if (distanceFromTarget <= 400d) {
                if (tooClose) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    approaching = true;
                    rightDistance = true;
                    tooClose = false;
                    tooFar = true;
                }
            } else {
                if (tooFar) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    approaching = true;
                    rightDistance = true;
                    tooClose = true;
                    tooFar = false;
                }
            }
            if (readyToFire && distanceFromTarget < 800d) {
                if (readyToFireOnce) {
                    readyToFireOnce = false;
                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = true; }
                    foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = true; }
                }
            } else {
                if (!readyToFireOnce) {
                    readyToFireOnce = true;
                    foreach (IMyUserControllableGun block in GATLINGS) { block.Shoot = false; }
                    foreach (IMyUserControllableGun block in ROCKETS) { block.Shoot = false; }
                }
            }
            Vector3D planetPosition;
            if (CONTROLLER.TryGetPlanetPosition(out planetPosition)) {
                Vector3D myAltitude = position - planetPosition;
                Vector3D targetAltitude = targetPosition - planetPosition;
                double altitude = myAltitude.LengthSquared();
                double targAltitude = targetAltitude.LengthSquared();
                if (altitude < targAltitude - 3.0) {
                    if (tooBelow) {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        tooBelow = false;
                        tooAbove = true;
                        rightAltitude = true;
                    }
                } else if (altitude > targAltitude + 3.0) {
                    if (tooAbove) {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                        tooBelow = true;
                        tooAbove = false;
                        rightAltitude = true;
                    }
                } else {
                    if (rightAltitude) {
                        foreach (IMyThrust block in UPWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        foreach (IMyThrust block in DOWNWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                        tooBelow = true;
                        tooAbove = true;
                        rightAltitude = false;
                    }
                }
                Vector3D downVector = CONTROLLER.WorldMatrix.Down;
                double rollAngle = AngleBetween(gravity, downVector);
                if (rollAngle != 0) {
                    double rollSpeed = rollController.Control(rollAngle);
                    Vector3D rotationVec = new Vector3D(0, 0, rollSpeed);
                    MatrixD refMatrix = CONTROLLER.WorldMatrix;
                    Vector3D relativeRotationVec = Vector3D.TransformNormal(rotationVec, refMatrix);
                    foreach (IMyGyro gyro in GYROS) {
                        Vector3D transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyro.WorldMatrix));
                        gyro.Pitch = (float)transformedRotationVec.X;
                        gyro.Yaw = (float)transformedRotationVec.Y;
                        gyro.Roll = (float)transformedRotationVec.Z;
                        if (!gyro.GyroOverride) {
                            gyro.GyroOverride = true;
                        }
                    }
                }
            }
        }

        void ManageBrakes(Vector3D velocity) {
            double speed = velocity.Length();
            if (speed > 1) {
                StartBraking(velocity);
            } else {
                foreach (IMyGyro gyro in GYROS) {
                    gyro.Yaw = 0f;
                    gyro.Pitch = 0f;
                    gyro.Roll = 0f;
                    gyro.GyroOverride = false;
                }
            }
        }

        void StartBraking(Vector3D velocityVec) {
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(-velocityVec, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
            //double brakingAngle = AngleBetween(CONTROLLER.WorldMatrix.Forward, -velocityVec);
            //if (brakingAngle * rad2deg <= brakingAngleTolerance) {
            if (!CONTROLLER.DampenersOverride) {
                CONTROLLER.DampenersOverride = true;
            }
            //} else { if (CONTROLLER.DampenersOverride) { CONTROLLER.DampenersOverride = false; } }
        }

        public static double VectorProjectionScalar(Vector3D IN, Vector3D Axis_norm) {//Use For Magnitudes Of Vectors In Directions (0-IN.length)
            double OUT = Vector3D.Dot(IN, Axis_norm);
            if (OUT == double.NaN) { OUT = 0; }
            return OUT;
        }

        void MissileGuidance(double timeSinceLastRun, Vector3D gravity, Vector3D velocity, double missileMass) {
            Vector3D targetVel = targetVelocity;//(targetPosition - prevTargetPosition) / elapsedTime;
            Vector3D targetPos = targetPosition + targetVelocity * (float)timeSinceLastRun;//targetPosition + (targetVelocity * elapsedTime);
            Vector3D targetAcceleration = (targetVelocity - prevTargetVelocity) * (float)updatesPerSecond;
            double missileThrust = CalculateMissileThrust(THRUSTERS);
            double missileAcceleration = missileThrust / missileMass;
            Vector3D headingVec = GetPointingVector(CONTROLLER.CenterOfMass, velocity, missileAcceleration, targetPos, targetVel, targetAcceleration, gravity);
            if (status.Equals("Cruising")) {
                double headingDeviation = CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }
            if (!Vector3D.IsZero(gravity)) {
                headingVec = GravityCompensation(missileAcceleration, headingVec, gravity);
            }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(headingVec, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            /*double rollSpeed;
            if (Math.Abs(missileSpinRPM) > 1e-3 && status.Equals("Cruising") && Vector3D.IsZero(gravityVec)) { rollSpeed = missileSpinRPM * rpm2Rad;//converts RPM to rad/s }
            else { rollSpeed = rollAngle; }
            if (Math.Abs(yawAngle) < gyroSlowdownAngle) { yawSpeed = updatesPerSecond * .5 * yawAngle; }
            if (Math.Abs(pitchAngle) < gyroSlowdownAngle) { pitchSpeed = updatesPerSecond * .5 * pitchAngle; }*/
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
        }

        void SpiralGuidance(double timeSinceLastRun, Vector3D gravity, Vector3D velocity, double missileMass) {
            Vector3D targetVel = targetVelocity;//(targetPosition - prevTargetPosition) / elapsedTime;
            Vector3D targetPos = targetPosition + targetVelocity * (float)timeSinceLastRun;//targetPosition + (targetVelocity * elapsedTime);
            Vector3D targetAcceleration = (targetVelocity - prevTargetVelocity) * (float)updatesPerSecond;
            double missileThrust = CalculateMissileThrust(THRUSTERS);
            double missileAcceleration = missileThrust / missileMass;
            Vector3D headingVec = GetPointingVector(CONTROLLER.CenterOfMass, velocity, missileAcceleration, targetPos, targetVel, targetAcceleration, gravity);
            headingVec = missileAcceleration * SpiralTrajectory(headingVec, CONTROLLER.WorldMatrix.Up);
            if (status.Equals("Cruising")) {
                double headingDeviation = CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }
            if (!Vector3D.IsZero(gravity)) {
                headingVec = GravityCompensation(missileAcceleration, headingVec, gravity);
            }
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(headingVec, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            /*double rollSpeed;
            if (Math.Abs(missileSpinRPM) > 1e-3 && status.Equals("Cruising") && Vector3D.IsZero(gravityVec)) { rollSpeed = missileSpinRPM * rpm2Rad;//converts RPM to rad/s }
            else { rollSpeed = rollAngle; }
            if (Math.Abs(yawAngle) < gyroSlowdownAngle) { yawSpeed = updatesPerSecond * .5 * yawAngle; }
            if (Math.Abs(pitchAngle) < gyroSlowdownAngle) { pitchSpeed = updatesPerSecond * .5 * pitchAngle; }*/
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
        }

        Vector3D SpiralTrajectory(Vector3D desiredForwardVector, Vector3D desiredUpVector) {
            if (timeSpiral > timeMaxSpiral) { timeSpiral = 0; }
            double angle = 2 * Math.PI * timeSpiral / timeMaxSpiral;
            Vector3D forward = SafeNormalize(desiredForwardVector);
            Vector3D right = SafeNormalize(Vector3D.Cross(forward, desiredUpVector));
            Vector3D up = Vector3D.Cross(right, forward);
            double lateralProportion = Math.Sin(spiralDegrees * deg2Rad);
            double forwardProportion = Math.Sqrt(1 - lateralProportion * lateralProportion);
            return forward * forwardProportion + lateralProportion * (Math.Sin(angle) * up + Math.Cos(angle) * right);
        }

        Vector3D GetPointingVector(Vector3D missilePosition, Vector3D missileVelocity, double missileAcceleration, Vector3D targetPosition, Vector3D targetVelocity, Vector3D targetAcceleration, Vector3D gravity) {
            Vector3D missileToTarget = targetPosition - missilePosition;
            Vector3D missileToTargetNorm = Vector3D.Normalize(missileToTarget);
            Vector3D relativeVelocity = targetVelocity - missileVelocity;
            Vector3D lateralTargetAcceleration = (targetAcceleration - Vector3D.Dot(targetAcceleration, missileToTargetNorm) * missileToTargetNorm);
            Vector3D gravityCompensationTerm = 1.1 * -(gravity - Vector3D.Dot(gravity, missileToTargetNorm) * missileToTargetNorm);
            Vector3D lateralAcceleration = GetLatax(missileToTarget, missileToTargetNorm, relativeVelocity, lateralTargetAcceleration, gravityCompensationTerm);
            if (Vector3D.IsZero(lateralAcceleration)) { return missileToTarget; }
            double diff = missileAcceleration * missileAcceleration - lateralAcceleration.LengthSquared();
            if (diff < 0) {//fly parallel to the target
                return lateralAcceleration;
            }
            return lateralAcceleration + Math.Sqrt(diff) * missileToTargetNorm;
        }

        Vector3D GetLatax(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm) {
            Vector3D omega = Vector3D.Cross(missileToTarget, relativeVelocity) / Math.Max(missileToTarget.LengthSquared(), 1);//to combat instability at close range
            return navConstant * relativeVelocity.Length() * Vector3D.Cross(omega, missileToTargetNorm)
                 + navAccelConstant * lateralTargetAcceleration
                 + gravityCompensationTerm;//normal to LOS
        }

        double CalculateMissileThrust(List<IMyThrust> mainThrusters) {
            double thrust = 0;
            foreach (IMyThrust block in mainThrusters) {
                if (block.Closed) { continue; }
                thrust += block.IsFunctional ? block.MaxEffectiveThrust : 0;
            }
            return thrust;
        }

        void ApplyThrustOverride(List<IMyThrust> thrusters, float overrideValue, bool turnOn = true) {
            float thrustProportion = overrideValue * 0.01f;
            foreach (IMyThrust thisThrust in thrusters) {
                if (thisThrust.Closed) { continue; }
                if (thisThrust.Enabled != turnOn) {
                    thisThrust.Enabled = turnOn;
                }
                if (thrustProportion != thisThrust.ThrustOverridePercentage) {
                    thisThrust.ThrustOverridePercentage = thrustProportion;
                }
            }
        }

        void LockOnTarget(double timeSinceLastRun, Vector3D gravity, Vector3D position, Vector3D myVelocity) {
            Vector3D targetPos = targetPosition + (targetVelocity * (float)timeSinceLastRun);
            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetPos, position);
            if (distanceFromTarget > 800d) {
                aimDirection = targetPos - position;
            } else {
                switch (weaponType) {
                    case 0://none
                        aimDirection = targetPos - position;
                        break;
                    case 1://rockets
                        aimDirection = ComputeLeading(targetPos, targetVelocity, 200f, position, myVelocity);
                        break;
                    case 2://gatlings
                        aimDirection = ComputeLeading(targetPos, targetVelocity, 400f, position, myVelocity);
                        if (!Vector3D.IsZero(gravity)) {
                            aimDirection = BulletDrop(distanceFromTarget, 400f, aimDirection, gravity);
                        }
                        break;
                    default:
                        aimDirection = targetPos - position;
                        break;
                }
            }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);
            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
            if (missileType == 2) {
                Vector3D forwardVec = CONTROLLER.WorldMatrix.Forward;
                double angle = AngleBetween(forwardVec, aimDirection);
                if (angle * rad2deg <= angleTolerance) {
                    readyToFire = true;
                } else {
                    readyToFire = false;
                }
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

        public static Vector3D GravityCompensation(double missileAcceleration, Vector3D desiredDirection, Vector3D gravity) {
            Vector3D directionNorm = SafeNormalize(desiredDirection);
            Vector3D gravityCompensationVec = -(Rejection(gravity, desiredDirection));
            double diffSq = missileAcceleration * missileAcceleration - gravityCompensationVec.LengthSquared();
            if (diffSq < 0) {// Impossible to hover
                return desiredDirection - gravity; // We will sink, but at least approach the target.
            }
            return directionNorm * Math.Sqrt(diffSq) + gravityCompensationVec;
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

        bool TurretsDetection() {
            bool targetFound = false;
            foreach (IMyLargeTurretBase turret in TURRETS) {
                MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                if (!targ.IsEmpty()) {
                    if (IsValidTarget(ref targ)) {
                        targetPosition = targ.Position;
                        targetVelocity = targ.Velocity;
                        targetFound = true;
                        break;
                    }
                }
            }
            return targetFound;
        }

        bool IsValidTarget(ref MyDetectedEntityInfo entityInfo) {
            if (entityInfo.Type == MyDetectedEntityType.LargeGrid || entityInfo.Type == MyDetectedEntityType.SmallGrid) {
                if (entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                || entityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) {
                    return true;
                }
            }
            return false;
        }

        void UpdateMaxSpeed(double speed) {
            if (speed > maxSpeed) {
                maxSpeed = speed;
            }
        }

        void UpdateBroadcastRange(Vector3D platformPosition, Vector3D position) {
            float distance = (float)Vector3D.Distance(platformPosition, position);
            ANTENNA.Radius = distance + 100f;
        }

        void GetAntenna() {
            ANTENNA = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
        }

        void GetBlocks() {
            TBLOCKS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TBLOCKS, b => b.CustomName.Contains(missilePrefix));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CustomName.Contains(missilePrefix));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, b => b.CustomName.Contains(missilePrefix));
            ALLTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(ALLTHRUSTERS, b => b.CustomName.Contains(missilePrefix));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Frw"));
            SIDETHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(SIDETHRUSTERS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains(sideThrustersName));
            BACKWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(BACKWARDTHRUSTERS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Bkw"));
            UPWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(UPWARDTHRUSTERS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Upw"));
            DOWNWARDTHRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(DOWNWARDTHRUSTERS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Dnw"));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, b => b.CustomName.Contains(missilePrefix));
            GENERATORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(GENERATORS, b => b.CustomName.Contains(missilePrefix));
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, b => b.CustomName.Contains(missilePrefix));
            CONNECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(CONNECTORS, b => b.CustomName.Contains(missilePrefix));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Gatling"));
            ROCKETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ROCKETS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Rocket"));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(missilePrefix) && b.CustomName.Contains("Turret"));
            CONTROLLER = CONTROLLERS[0];
        }

        void InitPIDControllers() {
            yawController = new PID(1d, 0d, 1d, globalTimestep);
            pitchController = new PID(1d, 0d, 1d, globalTimestep);
            rollController = new PID(1d, 0d, 1d, globalTimestep);
        }

        public class PID {
            readonly double _kP = 0;
            readonly double _kI = 0;
            readonly double _kD = 0;

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
                return errorSum = errorSum * (1.0 - _decayRatio) + currentError * timeStep;
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
                errorSum = errorSum + currentError * timeStep;
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

        public static Vector3D SafeNormalize(Vector3D a) {
            if (Vector3D.IsZero(a)) { return Vector3D.Zero; }
            if (Vector3D.IsUnit(ref a)) { return a; }
            return Vector3D.Normalize(a);
        }

        public static Vector3D Rejection(Vector3D a, Vector3D b) {//reject a on b
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) { return Vector3D.Zero; }
            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        public static double AngleBetween(Vector3D a, Vector3D b) {//returns radians
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) {
                return 0;
            } else {
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }
        }

        public static double CosBetween(Vector3D a, Vector3D b) {//returns radians
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) {
                return 0;
            } else {
                return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
            }
        }


    }
}
