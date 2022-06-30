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
        //TODO add gravityConpensation
        //add other missile guidances
        //make a better drome AI
        //HOMING MISSILE

        readonly string antennaName = "A [M]1";
        readonly string missileTag = "[M]1";
        readonly string rocketsName = "Rocket";
        readonly string gatlingsName = "Gatling";
        readonly string turretsName = "Turret";
        readonly string forwardThrustersName = "Frw";
        readonly string sideThrustersName = "Lwr";//Drone 1 Rrw - Drone 2 Lrw
        readonly string backwardThrustersName = "Bkw";
        readonly string upwardThrustersName = "Upw";
        readonly string downwardThrustersName = "Dnw";

        readonly string antennaTag = "[MISSILE]";
        readonly string platformTag = "[RELAY]";

        const string commandLaunch = "Launch";
        const string commandUpdate = "Update";
        const string commandLost = "Lost";
        const string commandSpiral = "Spiral";

        readonly string statusLost = "Lost";
        readonly string statusCruising = "Cruising";
        readonly string statusLaunched = "Launched";

        int missileType = 1;//0 kinetic - 1 explosive - 2 drone
        readonly int weaponType = 0;//0 None - 1 Rockets - 2 Gatlings
        readonly int startThrustersDelay = 5;
        readonly int startTargetingDelay = 10;
        readonly float rocketProjectileMaxSpeed = 200f;
        readonly float gatlingProjectileMaxSpeed = 400f;
        readonly double gunsMaxRange = 800d;
        readonly int checkLoadDelay = 100;//delay after wich the drone check if has ammo and ice
        readonly float spiralStart = 1000f;//distance to target at which missile starts to spiral
        readonly float spiralSafe = 200f;//safe distance from ship at which missile starts to spiral
        readonly double spiralDegrees = 2d;//radius of the spiral pattern
        readonly double timeMaxSpiral = 3d;//time it takes the missile to complete a full spiral cycle
        readonly double navConstant = 5d;//Recommended value is 3-5 Higher values make the missile compensate faster but can lead to more overshoot/instability
        readonly double navAccelConstant = 0d;
        readonly float globalTimestep = 10.0f / 60.0f;//UpdateFrequency.Update10
        readonly double aimP = 1;
        readonly double aimI = 0;
        readonly double aimD = 1;
        readonly double integralWindupLimit = 0;

        const double updatesPerSecond = 6.0;//UpdateFrequency.Update10
        const double deg2Rad = Math.PI / 180;
        const double rad2deg = 180 / Math.PI;
        const double angleTolerance = 2;//degrees

        double fuseDistance = 7d;
        int currentTick = 1;
        double maxSpeed = 99d;
        bool isLargeGrid = false;
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
        bool readyToFire = false;
        bool readyToFireOnce = true;
        int checkLoad = 0;
        bool creative = true;
        bool useSpiral = false;
        double timeSpiral = 0d;

        Vector3D platformPosition;
        Vector3D targetPosition;
        Vector3 targetVelocity;
        Vector3 prevTargetVelocity = new Vector3();

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

        //readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
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
            BROADCASTLISTENER = IGC.RegisterBroadcastListener(antennaTag);
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

                    if (!launched && command.Equals(commandLaunch)) {
                        if (!init) {
                            GetAntenna();
                            GetBlocks();
                            init = true;
                        }

                        if (launchOnce) {
                            InitiateLaunch();
                        }

                        UpdateBroadcastRange(platformPosition);
                    } else if (launched && (command.Equals(commandUpdate)) || command.Equals(commandSpiral)) {
                        if (command.Equals(commandSpiral)) { useSpiral = !useSpiral; }

                        if (updateOnce) {
                            InitiateUpdate();
                        }

                        UpdateBroadcastRange(platformPosition);

                        SendUnicastMessage();

                        if (!startTargeting) {
                            InitiateThrusters();
                        } else {
                            UpdateMaxSpeed();

                            bool targetFound = TurretsDetection();

                            ManageMissileType();

                            currentTick++;
                            prevTargetVelocity = targetVelocity;
                            //prevTargetPosition = targetPosition;
                            //prevPosition = CONTROLLER.CubeGrid.WorldVolume.Center;
                        }
                    } else if (launched && command.Equals(commandLost)) {
                        if (lostOnce) {
                            InitiateLost();
                        }

                        UpdateBroadcastRange(platformPosition);

                        SendUnicastMessage();

                        ManageBrakes();
                    }
                }
            } catch (Exception e) {
                StringBuilder debugLog = new StringBuilder("");
                debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                SendErrorMessage(debugLog.ToString());
            }
        }

        void GetMessages() {
            if (UNICASTLISTENER.HasPendingMessage) {
                while (UNICASTLISTENER.HasPendingMessage) {
                    var msg = UNICASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                            MyTuple<Vector3, Vector3D>>>) {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                                    MyTuple<Vector3, Vector3D>>>)msg.Data;

                        currentTick = 1;

                        platFormId = msg.Source;//platformTag = msg.Tag;

                        for (int i = 0; i < data.Length; i++) {
                            var temp = data[i];

                            var tup1 = temp.Item1;
                            var tup2 = temp.Item2;

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
            //else 
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    var msg = BROADCASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                            MyTuple<Vector3, Vector3D>>>) {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD, bool>,
                                    MyTuple<Vector3, Vector3D>>>)msg.Data;

                        currentTick = 1;

                        platFormId = msg.Source;//platformTag = msg.Tag;

                        for (int i = 0; i < data.Length; i++) {
                            var temp = data[i];

                            var tup1 = temp.Item1;
                            var tup2 = temp.Item2;

                            long myId = tup1.Item1;
                            string cmd = tup1.Item2;
                            if (cmd.Equals(commandLaunch) && !launched) {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;
                                //platformMatrix = tup1.Item4;
                                creative = tup1.Item5;

                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                            }
                            //else 
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
        }

        bool SendUnicastMessage() {
            Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center;
            double distanceFromTarget = Vector3D.Distance(targetPosition, position);
            double speed = CONTROLLER.GetShipSpeed();
            string type = "";
            if (missileType == 0)//0 kinetic - 1 explosive - 2 drone
            {
                type = "Kinetic";
            } else if (missileType == 1) {
                type = "Explosive";
            } else if (missileType == 2) {
                type = "Drone";
            }
            string info = command + "," + status + "," + type;
            var immArray = ImmutableArray.CreateBuilder<MyTuple<string, Vector3D, double, double>>();
            var tuple = MyTuple.Create(info, position, speed, distanceFromTarget);
            immArray.Add(tuple);
            bool messageSent = IGC.SendUnicastMessage(platFormId, platformTag, immArray.ToImmutable());
            return messageSent;
        }

        bool SendErrorMessage(String msg) {
            var tuple = MyTuple.Create("ERROR", msg);
            bool messageSent = IGC.SendUnicastMessage(platFormId, platformTag, tuple);
            return messageSent;
        }

        void InitiateLaunch() {
            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
            //UpdateGlobalTimeStep();
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

            status = statusLaunched;
        }

        void InitiateUpdate() {
            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
            //UpdateGlobalTimeStep();
            currentTick = 1;

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

            status = statusCruising;
        }

        void InitiateLost() {
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
            //UpdateGlobalTimeStep();
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

            status = statusLost;
        }

        void InitiateThrusters() {
            if (startThrusters) {
                if (startThrustersOnce) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    startThrustersOnce = false;
                }
                if (!startTargeting) {
                    if (countStartTargeting > startTargetingDelay) { startTargeting = true; }
                    countStartTargeting++;
                }
            } else {
                if (countStartThrusters > startThrustersDelay) { startThrusters = true; }
                countStartThrusters++;
            }
        }

        void PrepareForLaunch() {
            foreach (IMyPowerProducer block in GENERATORS) {
                block.Enabled = true;
                if (block is IMyBatteryBlock) { (block as IMyBatteryBlock).ChargeMode = ChargeMode.Discharge; }
            }
            foreach (IMyShipMergeBlock item in MERGES) { item.Enabled = false; }
            foreach (IMyShipConnector item in CONNECTORS) { item.Disconnect(); item.Enabled = false; }
            foreach (IMyThrust item in ALLTHRUSTERS) { item.Enabled = true; }
        }

        void ManageMissileType() {
            if (missileType == 0)//kinetic
            {
                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceFromShip = Vector3D.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                if (distanceFromTarget > spiralStart && distanceFromShip < spiralSafe) {
                    MissileGuidance();
                } else {
                    if (useSpiral && distanceFromTarget < spiralStart && distanceFromShip > spiralSafe) {
                        SpiralGuidance();
                    } else {
                        MissileGuidance();
                    }
                }
            } else if (missileType == 1)//explosive
              {
                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceFromShip = Vector3D.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                if (distanceFromTarget <= fuseDistance) {
                    foreach (IMyWarhead block in WARHEADS) { block.Detonate(); }
                }
                if (distanceFromTarget > spiralStart && distanceFromShip < spiralSafe) {
                    MissileGuidance();
                } else {
                    if (useSpiral && distanceFromTarget < spiralStart && distanceFromShip > spiralSafe) {
                        SpiralGuidance();
                    } else {
                        MissileGuidance();
                    }
                }
            } else if (missileType == 2)//drone
              {
                LockOnTarget(CONTROLLER);

                ManageDrone();

                if (!creative) {
                    if (checkLoad >= checkLoadDelay) {
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
            double percent = 0;
            if (maxVolume > 0 && currentVolume > 0) {
                percent = currentVolume / maxVolume * 100;
            }
            return percent;
        }

        void ManageDrone() {
            double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            if (distanceFromTarget >= 600 && distanceFromTarget < 800) {
                if (approaching) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    approaching = false;
                    rightDistance = true;
                    tooClose = true;
                    tooFar = true;
                }
            } else if (distanceFromTarget > 400 && distanceFromTarget < 600) {
                if (rightDistance) {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = 0f; }
                    foreach (IMyThrust block in SIDETHRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    foreach (IMyThrust block in BACKWARDTHRUSTERS) { block.ThrustOverride = 0f; }
                    approaching = true;
                    rightDistance = false;
                    tooClose = true;
                    tooFar = true;
                }
            } else if (distanceFromTarget <= 400) {
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

            if (readyToFire && distanceFromTarget < 800) {
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
                Vector3D myAltitude = CONTROLLER.CubeGrid.WorldVolume.Center - planetPosition;
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

                Vector3D grav = CONTROLLER.GetNaturalGravity();
                Vector3D downVector = CONTROLLER.WorldMatrix.Down;
                double rollAngle = VectorMath.AngleBetween(grav, downVector);
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

        void ManageBrakes() {
            Vector3D velocityVec = CONTROLLER.GetShipVelocities().LinearVelocity;
            double speed = velocityVec.Length();
            if (speed > 1) {
                StartBraking(velocityVec);
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
            Vector3D UpVector;
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) { UpVector = CONTROLLER.WorldMatrix.Up; } else { UpVector = -CONTROLLER.GetNaturalGravity(); }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(-velocityVec, UpVector, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);

            //double brakingAngle = VectorMath.AngleBetween(CONTROLLER.WorldMatrix.Forward, -velocityVec);
            //if (brakingAngle * rad2deg <= brakingAngleTolerance) {
            if (!CONTROLLER.DampenersOverride) {
                CONTROLLER.DampenersOverride = true;
            }
            //} else { if (CONTROLLER.DampenersOverride) { CONTROLLER.DampenersOverride = false; } }
        }

        public static double VectorProjectionScalar(Vector3D IN, Vector3D Axis_norm)//Use For Magnitudes Of Vectors In Directions (0-IN.length)
        {
            double OUT = Vector3D.Dot(IN, Axis_norm);
            if (OUT == double.NaN) { OUT = 0; }
            return OUT;
        }

        //readonly double missileSpinRPM = 0d;//this specifies how fast the missile will spin when flying(only in space)
        //const double rpm2Rad = Math.PI / 30;
        //const double gyroSlowdownAngle = Math.PI / 36;
        void MissileGuidance() {
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetVel = targetVelocity;//(targetPosition - prevTargetPosition) / elapsedTime;
            Vector3D targetPos = targetPosition + targetVelocity * elapsedTime;//targetPosition + (targetVelocity * elapsedTime);
            Vector3D targetAcceleration = (targetVelocity - prevTargetVelocity) * (float)updatesPerSecond;
            double missileThrust = CalculateMissileThrust(THRUSTERS);
            double missileMass = CONTROLLER.CalculateShipMass().PhysicalMass;
            double missileAcceleration = missileThrust / missileMass;
            Vector3D gravityVec = CONTROLLER.GetNaturalGravity();
            Vector3D headingVec = GetPointingVector(CONTROLLER.CenterOfMass, CONTROLLER.GetShipVelocities().LinearVelocity, missileAcceleration, targetPos, targetVel, targetAcceleration, gravityVec);

            if (status.Equals(statusCruising)) {
                var headingDeviation = VectorMath.CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }

            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(headingVec, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);

            /*double rollSpeed;
            if (Math.Abs(missileSpinRPM) > 1e-3 && status.Equals(statusCruising) && Vector3D.IsZero(gravityVec)) { rollSpeed = missileSpinRPM * rpm2Rad;//converts RPM to rad/s }
            else { rollSpeed = rollAngle; }
            if (Math.Abs(yawAngle) < gyroSlowdownAngle) { yawSpeed = updatesPerSecond * .5 * yawAngle; }
            if (Math.Abs(pitchAngle) < gyroSlowdownAngle) { pitchSpeed = updatesPerSecond * .5 * pitchAngle; }*/

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
        }

        void SpiralGuidance() {
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetVel = targetVelocity;//(targetPosition - prevTargetPosition) / elapsedTime;
            Vector3D targetPos = targetPosition + targetVelocity * elapsedTime;//targetPosition + (targetVelocity * elapsedTime);
            Vector3D targetAcceleration = (targetVelocity - prevTargetVelocity) * (float)updatesPerSecond;
            double missileThrust = CalculateMissileThrust(THRUSTERS);
            double missileMass = CONTROLLER.CalculateShipMass().PhysicalMass;
            double missileAcceleration = missileThrust / missileMass;
            Vector3D gravityVec = CONTROLLER.GetNaturalGravity();
            Vector3D headingVec = GetPointingVector(CONTROLLER.CenterOfMass, CONTROLLER.GetShipVelocities().LinearVelocity, missileAcceleration, targetPos, targetVel, targetAcceleration, gravityVec);

            headingVec = missileAcceleration * SpiralTrajectory(headingVec, CONTROLLER.WorldMatrix.Up);

            if (status.Equals(statusCruising)) {
                var headingDeviation = VectorMath.CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }

            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(headingVec, CONTROLLER.WorldMatrix.Up, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);

            /*double rollSpeed;
            if (Math.Abs(missileSpinRPM) > 1e-3 && status.Equals(statusCruising) && Vector3D.IsZero(gravityVec)) { rollSpeed = missileSpinRPM * rpm2Rad;//converts RPM to rad/s }
            else { rollSpeed = rollAngle; }
            if (Math.Abs(yawAngle) < gyroSlowdownAngle) { yawSpeed = updatesPerSecond * .5 * yawAngle; }
            if (Math.Abs(pitchAngle) < gyroSlowdownAngle) { pitchSpeed = updatesPerSecond * .5 * pitchAngle; }*/

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
        }

        Vector3D SpiralTrajectory(Vector3D desiredForwardVector, Vector3D desiredUpVector) {
            if (timeSpiral > timeMaxSpiral)
                timeSpiral = 0;

            double angle = 2 * Math.PI * timeSpiral / timeMaxSpiral;

            Vector3D forward = VectorMath.SafeNormalize(desiredForwardVector);
            Vector3D right = VectorMath.SafeNormalize(Vector3D.Cross(forward, desiredUpVector));
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

            if (Vector3D.IsZero(lateralAcceleration))
                return missileToTarget;

            double diff = missileAcceleration * missileAcceleration - lateralAcceleration.LengthSquared();
            if (diff < 0)
                return lateralAcceleration;//fly parallel to the target
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
            foreach (var block in mainThrusters) {
                if (block.Closed)
                    continue;
                thrust += block.IsFunctional ? block.MaxEffectiveThrust : 0;
                //compensate for grav later
            }
            return thrust;
        }

        void ApplyThrustOverride(List<IMyThrust> thrusters, float overrideValue, bool turnOn = true) {
            float thrustProportion = overrideValue * 0.01f;
            foreach (IMyThrust thisThrust in thrusters) {
                if (thisThrust.Closed)
                    continue;

                if (thisThrust.Enabled != turnOn)
                    thisThrust.Enabled = turnOn;

                if (thrustProportion != thisThrust.ThrustOverridePercentage)
                    thisThrust.ThrustOverridePercentage = thrustProportion;
            }
        }

        void LockOnTarget(IMyTerminalBlock REF) {
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetPos = targetPosition + (targetVelocity * elapsedTime);
            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetPos, REF.GetPosition());
            if (distanceFromTarget > gunsMaxRange) {
                aimDirection = targetPos - REF.GetPosition();
            } else {
                switch (weaponType) {
                    case 0://none
                        aimDirection = targetPos - REF.GetPosition();
                        break;
                    case 1://rockets
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetVelocity, rocketProjectileMaxSpeed, REF);
                        break;
                    case 2://gatlings
                        aimDirection = ComputeInterceptWithLeading(targetPos, targetVelocity, gatlingProjectileMaxSpeed, REF);
                        break;
                    default:
                        aimDirection = targetPos - REF.GetPosition();
                        break;
                }
            }
            Vector3D UpVector;
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) { UpVector = CONTROLLER.WorldMatrix.Up; } else { UpVector = -CONTROLLER.GetNaturalGravity(); }
            double yawAngle, pitchAngle, rollAngle;
            GetRotationAnglesSimultaneous(aimDirection, UpVector, CONTROLLER.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed = rollController.Control(rollAngle);

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);

            if (missileType == 2) {
                Vector3D forwardVec = REF.WorldMatrix.Forward;
                double angle = VectorMath.AngleBetween(forwardVec, aimDirection);
                if (angle * rad2deg <= angleTolerance) {
                    readyToFire = true;
                } else {
                    readyToFire = false;
                }
            }
        }

        Vector3D ComputeInterceptWithLeading(Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed, IMyTerminalBlock muzzle) {
            MatrixD refWorldMatrix = muzzle.WorldMatrix;
            //MatrixD refLookAtMatrix = MatrixD.CreateLookAt(Vector3D.Zero, refWorldMatrix.Forward, refWorldMatrix.Up);
            Vector3D aimDirection = GetPredictedTargetPosition(muzzle, CONTROLLER, targetPosition, targetVelocity, projectileSpeed);
            aimDirection -= refWorldMatrix.Translation;
            //aimDirection = Vector3D.Normalize(Vector3D.TransformNormal(aimDirection, refLookAtMatrix));
            return aimDirection;
        }

        public Vector3D GetPredictedTargetPosition(IMyTerminalBlock gun, IMyShipController shooter, Vector3D targetPosition, Vector3D targetVelocity, float projectileSpeed) {
            float shootDelay = 0;
            Vector3D muzzlePosition = gun.GetPosition();
            Vector3D toTarget = targetPosition - muzzlePosition;
            Vector3D shooterVelocity = shooter.GetShipVelocities().LinearVelocity;
            Vector3D diffVelocity = targetVelocity - shooterVelocity;
            float a = (float)diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2 * Vector3.Dot(diffVelocity, toTarget);
            float c = (float)toTarget.LengthSquared();
            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);
            float t1 = p - q;
            float t2 = p + q;
            float t;
            if (t1 > t2 && t2 > 0) { t = t2; } else { t = t1; }
            t += shootDelay;
            Vector3D predictedPosition = targetPosition + diffVelocity * t;
            //Vector3 bulletPath = predictedPosition - muzzlePosition;
            //timeToHit = bulletPath.Length() / projectileSpeed;
            return predictedPosition;
        }

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll) {
            desiredForwardVector = VectorMath.SafeNormalize(desiredForwardVector);

            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector)) {
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            } else {
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

            if (Vector3D.IsZero(axis)) {
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

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix) {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList) {
                if (thisGyro.Closed)
                    continue;
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
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
                        currentTick = 1;
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

        void UpdateMaxSpeed() {
            double speed = CONTROLLER.GetShipSpeed();
            if (speed > maxSpeed) {
                maxSpeed = speed;
            }
        }

        void UpdateBroadcastRange(Vector3D platformPosition) {
            var distance = Vector3.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            ANTENNA.Radius = distance + 100;
        }

        void GetAntenna() {
            ANTENNA = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
        }

        void GetBlocks() {
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
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(gatlingsName));
            ROCKETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ROCKETS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(rocketsName));
            TURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(TURRETS, b => b.CustomName.Contains(missileTag) && b.CustomName.Contains(turretsName));
            CONTROLLER = CONTROLLERS[0];
        }

        void InitPIDControllers() {
            yawController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            pitchController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
            rollController = new PID(aimP, aimI, aimD, integralWindupLimit, -integralWindupLimit, globalTimestep);
        }

        public class PID {
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

            public PID(double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _lowerBound = lowerBound;
                _upperBound = upperBound;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecay = false;
            }

            public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep) {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control(double error) {
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun) {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Compute integral term
                if (!_integralDecay) {
                    _errorSum += error * _timeStep;

                    //Clamp integral term
                    if (_errorSum > _upperBound)
                        _errorSum = _upperBound;
                    else if (_errorSum < _lowerBound)
                        _errorSum = _lowerBound;
                } else {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
                return this.Value;
            }

            public double Control(double error, double timeStep) {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                return Control(error);
            }

            public void Reset() {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

        public static class VectorMath {
            public static Vector3D SafeNormalize(Vector3D a) {
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

            public static Vector3D Projection(Vector3D a, Vector3D b) {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a.Dot(b) / b.LengthSquared() * b;
            }

            public static double ScalarProjection(Vector3D a, Vector3D b) {
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
        Vector3D GravityCompensation(double maxAccel, Vector3D desiredDirection, Vector3D gravity)
        {
            desiredDirection = Vector3D.Normalize(desiredDirection) * maxAccel;
            return desiredDirection - gravity;
        }

        Vector3D GravityCompensation(double missileAcceleration, Vector3D desiredDirection, Vector3D gravity)
        {
            Vector3D directionNorm = VectorMath.SafeNormalize(desiredDirection);
            Vector3D gravityCompensationVec = -(VectorMath.Rejection(gravity, desiredDirection));

            double diffSq = missileAcceleration * missileAcceleration - gravityCompensationVec.LengthSquared();
            if (diffSq < 0) // Impossible to hover
            {
                return desiredDirection - gravity; // We will sink, but at least approach the target.
            }
            return directionNorm * Math.Sqrt(diffSq) + gravityCompensationVec;
        }
        
        void UpdateGlobalTimeStep()
        {
            float tick = 1.0f / 60.0f;
            if ((Runtime.UpdateFrequency & UpdateFrequency.Update1) != 0) { globalTimestep = tick; }
            else if ((Runtime.UpdateFrequency & UpdateFrequency.Update10) != 0) { globalTimestep = tick * 10; }
            else if ((Runtime.UpdateFrequency & UpdateFrequency.Update100) != 0) { globalTimestep = tick * 100; }
        }

        const string commandBeamRide = "BeamRide";
        readonly double navAccelConstant = 0d;
        const double rpm2Rad = Math.PI / 30;
        const double updatesPerSecond = 10.0;
        const double gyroSlowdownAngle = Math.PI / 36;
        bool hasPassed = false;
        bool beamRideOnce = true;
        MatrixD platformMatrix;
        Vector3 prevTargetVelocity = new Vector3();

        //MAIN
        else if (launched && command.Equals(commandBeamRide))
        {
            if (beamRideOnce)
            {
                InitiateBeamRide();
            }

            if (!startTargeting)
            {
                InitiateThrusters();
            }
            else
            {
                BeamRide();
            }

            UpdateBroadcastRange(platformPosition);

            SendUnicastMessage();
        }
        else if (!launched && command.Equals(commandBeamRide))
        {
            if (!init)
            {
                GetAntenna();
                GetBlocks();
                init = true;
            }

            if (launchOnce)
            {
                InitiateLaunch();
            }

            if (beamRideOnce)
            {
                InitiateBeamRide();
            }

            if (!startTargeting)
            {
                InitiateThrusters();
            }
            else
            {
                BeamRide();
            }

            UpdateBroadcastRange(platformPosition);

            SendUnicastMessage();
        }
        
        void InitiateBeamRide()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            UpdateGlobalTimeStep();

            lostOnce = true;
            beamRideOnce = false;
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

            status = statusCruising;
        }

        void BeamRide()
        {
            float elapsedTime = currentTick * globalTimestep;
            double missileThrust = CalculateMissileThrust(THRUSTERS);
            double missileMass = CONTROLLER.CalculateShipMass().PhysicalMass;
            double missileAcceleration = missileThrust / missileMass;
            Vector3D headingVec = BeamRideGuidance(CONTROLLER.CenterOfMass, CONTROLLER.GetShipVelocities().LinearVelocity, CONTROLLER.GetNaturalGravity(), CONTROLLER.WorldMatrix.Forward, missileAcceleration);

            if (status.Equals(statusCruising))
            {
                var headingDeviation = VectorMath.CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }

            double yawAngle, pitchAngle, rollAngle;
            Vector3D UpVector;
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) { UpVector = CONTROLLER.WorldMatrix.Up; }
            else { UpVector = -CONTROLLER.GetNaturalGravity(); }
            GetRotationAnglesSimultaneous(headingVec, UpVector, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);

            double yawSpeed = yawController.Control(yawAngle);
            double pitchSpeed = pitchController.Control(pitchAngle);
            double rollSpeed;
            if (Math.Abs(missileSpinRPM) > 1e-3 && status.Equals(statusCruising) && Vector3D.IsZero(CONTROLLER.GetNaturalGravity()))
            {
                rollSpeed = missileSpinRPM * rpm2Rad;//converts RPM to rad/s
            }
            else
            {
                rollSpeed = rollAngle;
            }
            if (Math.Abs(yawAngle) < gyroSlowdownAngle)
            {
                yawSpeed = updatesPerSecond * .5 * yawAngle;
            }
            if (Math.Abs(pitchAngle) < gyroSlowdownAngle)
            {
                pitchSpeed = updatesPerSecond * .5 * pitchAngle;
            }

            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, GYROS, CONTROLLER.WorldMatrix);
        }

        Vector3D BeamRideGuidance(Vector3D missilePos, Vector3D missileVel, Vector3D gravity, Vector3D missileForwardVec, double missileAcceleration)
        {
            //Find vector from shooter to missile
            var shooterToMissileVec = missilePos - platformPosition;

            if (Vector3D.IsZero(platformMatrix.Forward))//this is to avoid NaN cases when the shooterForwardVec isnt cached yet
                platformMatrix.Forward = missileForwardVec;//messy but stops my code from breaking lol

            //Calculate perpendicular distance from shooter vector
            var projectionVec = VectorMath.Projection(shooterToMissileVec, platformMatrix.Forward);

            //Determine scaling factor
            double missileSpeed = missileVel.Length();
            Vector3D destinationVec = platformPosition + projectionVec + platformMatrix.Forward * Math.Max(2 * missileSpeed, 200);

            if (platformMatrix.Forward.Dot(shooterToMissileVec) > 0)// Missile is in front of the shooter
            {
                if (!hasPassed)
                {
                    hasPassed = true;
                }
            }
            else if (hasPassed)// If behind shooter and we have already passed the shooter before
            {
                int signLeft = Math.Sign(shooterToMissileVec.Dot(platformMatrix.Left));
                int signUp = Math.Sign(shooterToMissileVec.Dot(platformMatrix.Up));

                destinationVec += signLeft * 100 * platformMatrix.Left + signUp * 100 * platformMatrix.Up;
            }

            //Find vector from missile to destinationVec
            Vector3D missileToTargetVec = destinationVec - missilePos;

            //Drift compensation
            Vector3D headingVec;
            if (status.Equals(statusCruising))
            {
                headingVec = CalculateDriftCompensation(missileVel, missileToTargetVec, missileAcceleration, 0.5, gravity, 60);
            }
            else
            {
                headingVec = missileToTargetVec;
            }

            return headingVec;
        }

        Vector3D CalculateDriftCompensation(Vector3D velocity, Vector3D directHeading, double accel, double timeConstant, Vector3D gravityVec, double maxDriftAngle = 60)
        {
            if (directHeading.LengthSquared() == 0)
                return velocity;

            if (Vector3D.Dot(velocity, directHeading) < 0)
                return directHeading;

            if (velocity.LengthSquared() < 100)
                return directHeading;

            var normalVelocity = VectorMath.Rejection(velocity, directHeading);
            var normal = VectorMath.SafeNormalize(normalVelocity);
            var parallel = VectorMath.SafeNormalize(directHeading);

            var normalAccel = Vector3D.Dot(normal, normalVelocity) / timeConstant;
            normalAccel = Math.Min(normalAccel, accel * Math.Sin(MathHelper.ToRadians(maxDriftAngle)));

            var gravityCompensationTerm = 1.1 * -(VectorMath.Rejection(gravityVec, directHeading));
            var normalAccelerationVector = normalAccel * normal + gravityCompensationTerm;

            double parallelAccel = 0;
            var diff = accel * accel - normalAccelerationVector.LengthSquared();
            if (diff > 0)
                parallelAccel = Math.Sqrt(diff);

            return parallelAccel * parallel - normal * normalAccel;
        }

        const double secondsPerUpdate = 1.0 / updatesPerSecond;
        double prevSpeed = 0d;
        Vector3D prevTargetPosition = new Vector3D();
        prevTargetPosition = targetPosition;
        prevSpeed = CONTROLLER.GetShipSpeed();
        CalculateAcceleration();

        const double brakingAngleTolerance = 10; //degrees
        const double rad2deg = 180 / Math.PI;
        double missileAccel = 10d;
        double missileMass = 0d;
        double missileThrust = 0d;
        readonly double NavConstant = 5;
        readonly double NavAccelConstant = 0;

        void CalculateBaseAcceleration()
        {
            missileMass = 0;
            missileThrust = 0;
            float totalMass = CONTROLLER.CalculateShipMass().TotalMass;
            foreach (IMyTerminalBlock block in TBLOCKS) { missileMass += block.Mass; }
            foreach (IMyThrust item in THRUSTERS) { missileThrust += (double)item.MaxThrust; }
            missileAccel = missileThrust / missileMass;
        }

        void CalculateAcceleration()
        {
            missileMass = CONTROLLER.CalculateShipMass().TotalMass;
            missileThrust = 0;
            foreach (IMyThrust item in THRUSTERS) { missileThrust += (double)item.MaxThrust;  }
            missileAccel = missileThrust / missileMass;
        }

        public Vector3D GetLatax(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm)
        {
            Vector3D omega = Vector3D.Cross(missileToTarget, relativeVelocity) / Math.Max(missileToTarget.LengthSquared(), 1); //to combat instability at close range
            Vector3D parallelVelocity = relativeVelocity.Dot(missileToTargetNorm) * missileToTargetNorm; //bootleg vector projection
            Vector3D normalVelocity = (relativeVelocity - parallelVelocity);
            return navConstant * (relativeVelocity.Length() * Vector3D.Cross(omega, missileToTargetNorm) + 0.1 * normalVelocity) + navAccelConstant * lateralTargetAcceleration + gravityCompensationTerm; //normal to LOS
        }

        Vector3D GetLataxWhip(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm)
        {
            Vector3D parallelVelocity = relativeVelocity.Dot(missileToTargetNorm) * missileToTargetNorm; //bootleg vector projection
            Vector3D normalVelocity = (relativeVelocity - parallelVelocity);
            return NavConstant * 0.1 * normalVelocity
                 + NavAccelConstant * lateralTargetAcceleration
                 + gravityCompensationTerm; //normal to LOS
        }

        Vector3D GetLataxHybrid(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm)
        {
            Vector3D omega = Vector3D.Cross(missileToTarget, relativeVelocity) / Math.Max(missileToTarget.LengthSquared(), 1); //to combat instability at close range
            Vector3D parallelVelocity = relativeVelocity.Dot(missileToTargetNorm) * missileToTargetNorm; //bootleg vector projection
            Vector3D normalVelocity = (relativeVelocity - parallelVelocity);
            return NavConstant * (relativeVelocity.Length() * Vector3D.Cross(omega, missileToTargetNorm) + 0.1 * normalVelocity)
                 + NavAccelConstant * lateralTargetAcceleration
                 + gravityCompensationTerm; //normal to LOS
        }

        Vector3D GetLataxZeroEffort(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm)
        {
            double distToTarget = Vector3D.Dot(missileToTarget, missileToTargetNorm);
            // Equation (8) with sign modification to keep time positive and not NaN
            double tau = distToTarget / Math.Max(1, relativeVelocity.Length());
            // Equation (6)
            Vector3D z = missileToTarget + relativeVelocity * tau;
            // Equation (7)
            return NavConstant * z / (tau * tau)
                    + NavAccelConstant * lateralTargetAcceleration
                    + gravityCompensationTerm;
        }
        */

    }
}
