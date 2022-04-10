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

        readonly string antennaName = "A [M]1";
        readonly string missileTag = "[M]1";
        readonly string rocketsName = "Rocket";
        readonly string gatlingsName = "Gatling";
        readonly string turretsName = "Turret";
        readonly string forwardThrustersName = "Frw";
        readonly string sideThrustersName = "Lwr";
        readonly string backwardThrustersName = "Bkw";
        readonly string upwardThrustersName = "Upw";
        readonly string downwardThrustersName = "Dnw";
        //readonly string debugPanelName = "[M]1 Debug Panel";

        readonly string antennaTag = "[MISSILE]";
        readonly string platformTag = "[RELAY]";

        const string commandLaunch = "Launch";
        const string commandUpdate = "Update";
        const string commandLost = "Lost";
        const string commandSpiral = "Spiral";
        const string commandBeamRide = "BeamRide";

        readonly string statusLost = "Lost";
        readonly string statusCruising = "Cruising";
        readonly string statusLaunched = "Launched";

        readonly int missileType = 0;   //0 kinetic - 1 explosive - 2 drone
        readonly int weaponType = 0;    //0 None - 1 Rockets - 2 Gatlings
        readonly int startThrustersDelay = 50;
        readonly int startTargetingDelay = 100;
        readonly float spiralStart = 1000f; // distance to target at which missile starts to spiral
        readonly float spiralSafe = 200f; // safe distance from ship at which missile starts to spiral
        readonly double missileSpinRPM = 0d; //this specifies how fast the missile will spin when flying(only in space)
        readonly double spiralDegrees = 15d; // radius of the spiral pattern
        readonly double timeMaxSpiral = 3d; // time it takes the missile to complete a full spiral cycle
        readonly double navConstant = 5d; //Recommended value is 3-5 Higher values make the missile compensate faster but can lead to more overshoot/instability
        readonly double navAccelConstant = 0d;
        readonly double rocketProjectileForwardOffset = 4d;  //By default, rockets are spawn 4 meters in front of the rocket launcher's tip
        readonly double rocketProjectileInitialSpeed = 100d;
        readonly double rocketProjectileAccelleration = 600d;
        readonly double rocketProjectileMaxSpeed = 180d;
        readonly double rocketProjectileMaxRange = 800d;
        readonly double gatlingProjectileForwardOffset = 0d;
        readonly double gatlingProjectileInitialSpeed = 400d;
        readonly double gatlingProjectileAccelleration = 0d;
        readonly double gatlingProjectileMaxSpeed = 380d;
        readonly double gunsMaxRange = 1000d;
        readonly double PNGain = 3d;
        //readonly int writeDelay = 10;

        const double brakingAngleTolerance = 10; //degrees
        const double rad2deg = 180 / Math.PI;
        const double rpm2Rad = Math.PI / 30;
        const double deg2Rad = Math.PI / 180;
        const double updatesPerSecond = 10.0;
        const double secondsPerUpdate = 1.0 / updatesPerSecond;
        const double gyroSlowdownAngle = Math.PI / 36;

        bool useSpiral = true;
        double fuseDistance = 7d;
        bool hasPassed = false;
        double timeSpiral = 0d;
        float globalTimestep = 1.0f / 60.0f;
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
        bool beamRideOnce = true;
        bool updateOnce = true;
        bool approaching = true;
        bool rightDistance = true;
        bool tooClose = true;
        bool tooFar = true;
        bool rightAltitude = true;
        bool tooAbove = true;
        bool tooBelow = true;
        double missileAccel = 10d;
        double missileMass = 0d;
        double missileThrust = 0d;
        double prevYaw = 0d;
        double prevPitch = 0d;
        //int writeCount = 0;

        Vector3D platformPosition;
        MatrixD platformMatrix;
        Vector3D prevPosition = new Vector3D();
        Vector3D targetPosition;
        Vector3D prevTargetPosition = new Vector3D();
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
        //IMyTextPanel DEBUG;

        public IMyUnicastListener UNICASTLISTENER;
        public IMyBroadcastListener BROADCASTLISTENER;

        //public StringBuilder uniSenderLog = new StringBuilder("");
        //public StringBuilder uniListenerLog = new StringBuilder("");
        //public StringBuilder broadListenerLog = new StringBuilder("");
        //public StringBuilder initLaunchLog = new StringBuilder("");
        //public StringBuilder initLostLog = new StringBuilder("");
        //public StringBuilder initBeamLog = new StringBuilder("");
        //public StringBuilder initUpdateLog = new StringBuilder("");
        //public StringBuilder initThrustersLog = new StringBuilder("");
        //public StringBuilder antennaRadiusLog = new StringBuilder("");
        //public StringBuilder guidanceLog = new StringBuilder("");

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

            CalculateBaseAcceleration();

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
            Echo($"BACKWARDTHRUSTERS:{BACKWARDTHRUSTERS.Count}");
            Echo($"UPWARDTHRUSTERS:{UPWARDTHRUSTERS.Count}");
            Echo($"DOWNWARDTHRUSTERS:{DOWNWARDTHRUSTERS.Count}");
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
                        InitiateLaunch();
                    }

                    UpdateBroadcastRange(platformPosition);
                }
                else if (command.Equals(commandUpdate) || command.Equals(commandSpiral))
                {
                    if (command.Equals(commandSpiral))
                    {
                        useSpiral = !useSpiral;
                    }

                    if (updateOnce)
                    {
                        InitiateUpdate();
                    }

                    UpdateBroadcastRange(platformPosition);

                    SendUnicastMessage();

                    if (!startTargeting)
                    {
                        InitiateThrusters();
                    }
                    else
                    {
                        UpdateMaxSpeed();

                        bool targetFound = TurretsDetection();

                        if (!targetFound)
                        {
                            PredictTarget();
                        }

                        ManageMissileType();

                        currentTick++;
                        prevTargetVelocity = targetVelocity;
                        prevTargetPosition = targetPosition;
                        prevPosition = CONTROLLER.CubeGrid.WorldVolume.Center;
                    }
                }
                else if (command.Equals(commandLost))
                {
                    if (lostOnce)
                    {
                        InitiateLost();
                    }

                    UpdateBroadcastRange(platformPosition);

                    SendUnicastMessage();

                    ManageBrakes();
                }
                else if (command.Equals(commandBeamRide))
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
                //if (writeCount == writeDelay)
                //{
                //WriteDebug();
                //writeCount = 0;
                //}
                //writeCount++;
            }
        }

        void GetMessages()
        {
            if (UNICASTLISTENER.HasPendingMessage)
            {
                //uniListenerLog.Clear();
                //uniListenerLog.Append("Unicast Listener has message...\n");
                while (UNICASTLISTENER.HasPendingMessage)
                {
                    var msg = UNICASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD>,
                            MyTuple<Vector3, Vector3D>>>)
                    {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD>,
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
                            platformMatrix = tup1.Item4;

                            targetVelocity = tup2.Item1;
                            targetPosition = tup2.Item2;
                            //uniListenerLog.Append("command: " + command + "\n");
                        }
                    }
                }
            }
            //else 
            if (BROADCASTLISTENER.HasPendingMessage)
            {
                //broadListenerLog.Clear();
                //broadListenerLog.Append("Broadcast Listener has message...\n");
                while (BROADCASTLISTENER.HasPendingMessage)
                {
                    var msg = BROADCASTLISTENER.AcceptMessage();

                    if (msg.Data is ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD>,
                            MyTuple<Vector3, Vector3D>>>)
                    {

                        var data = (ImmutableArray<MyTuple<MyTuple<long, string, Vector3D, MatrixD>,
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
                            if (cmd.Equals(commandLaunch) && !launched)
                            {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;
                                platformMatrix = tup1.Item4;

                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                                //broadListenerLog.Append("cmd.Equals(commandLaunch) && !launched \n");
                                //broadListenerLog.Append("command: " + command + "\n");
                            }
                            //else 
                            if (myId == Me.EntityId)
                            {
                                command = tup1.Item2;
                                platformPosition = tup1.Item3;
                                platformMatrix = tup1.Item4;

                                targetVelocity = tup2.Item1;
                                targetPosition = tup2.Item2;
                                //broadListenerLog.Append("myId: " + myId + "equal to Me.EntityId: " + Me.EntityId + "\n");
                                //broadListenerLog.Append("command: " + command + "\n");
                            }
                        }
                    }
                }
            }
        }

        bool SendUnicastMessage()
        {
            Vector3D position = CONTROLLER.CubeGrid.WorldVolume.Center;
            double distanceFromTarget = Vector3D.Distance(targetPosition, position);
            double speed = CONTROLLER.GetShipSpeed();
            string type = "";
            if (missileType == 0)//0 kinetic - 1 explosive - 2 drone
            {
                type = "Kinetic";
            }
            else if (missileType == 1)
            {
                type = "Explosive";
            }
            else if (missileType == 2)
            {
                type = "Drone";
            }
            string info = command + "," + status + "," + type;

            var immArray = ImmutableArray.CreateBuilder<MyTuple<string, Vector3D, double, double>>();

            var tuple = MyTuple.Create(info, position, speed, distanceFromTarget);

            immArray.Add(tuple);

            bool messageSent = IGC.SendUnicastMessage(platFormId, platformTag, immArray.ToImmutable());

            //WriteUniSenderLog(messageSent);

            return messageSent;
        }

        void InitiateLaunch()
        {
            //ClearInitLog();
            //initLaunchLog.Append("Initiate Launch \n");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            UpdateGlobalTimeStep();
            SendUnicastMessage();
            PrepareForLaunch();

            launched = true;

            launchOnce = false;
            beamRideOnce = true;
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
            //initLaunchLog.Append("status: " + status + "\n");
        }

        void InitiateUpdate()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            UpdateGlobalTimeStep();

            CalculateAcceleration();
            currentTick = 1;

            if (missileType == 1)
            {
                foreach (IMyWarhead warHead in WARHEADS)
                {
                    warHead.IsArmed = true;
                }
            }

            lostOnce = true;
            beamRideOnce = true;
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
            //ClearInitLog();
            //initUpdateLog.Append("Initiate Update, status: " + status + "\n");
        }

        void InitiateLost()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            UpdateGlobalTimeStep();

            foreach (IMyTerminalBlock block in GATLINGS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
            foreach (IMyTerminalBlock block in ROCKETS) { if (block.HasAction("Shoot_Off")) { block.ApplyAction("Shoot_Off"); } }
            foreach (IMyThrust block in ALLTHRUSTERS) { block.ThrustOverride = 0f; }

            lostOnce = false;
            beamRideOnce = true;
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
            //ClearInitLog();
            //initLostLog.Append("Initiate Lost, status: " + status + "\n");
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
            //ClearInitLog();
            //initBeamLog.Append("Initiate BeamRide, status: " + status + "\n");
        }

        void InitiateThrusters()
        {
            if (startThrusters)
            {
                if (startThrustersOnce)
                {
                    foreach (IMyThrust block in THRUSTERS) { block.ThrustOverride = block.MaxThrust; }
                    startThrustersOnce = false;
                }
                if (!startTargeting)
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
            //WriteThrustersLog(startThrusters, startThrustersOnce, startTargeting);
        }

        void ManageMissileType()
        {
            //guidanceLog.Clear();
            if (missileType == 0)//kinetic
            {
                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceFromShip = Vector3D.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                //if (distanceShip2Target <= spiralStart + spiralSafe)
                if (distanceFromTarget > spiralStart && distanceFromShip < spiralSafe)
                {
                    //guidanceLog.Append("kinetic, MissileGuidance \n");
                    MissileGuidance();
                }
                else
                {
                    if (useSpiral && distanceFromTarget < spiralStart && distanceFromShip > spiralSafe)
                    {
                        //guidanceLog.Append("kinetic, SpiralGuidance \n");
                        SpiralGuidance();
                    }
                    else
                    {
                        //guidanceLog.Append("kinetic, MissileGuidance \n");
                        MissileGuidance();
                    }
                }
            }
            else if (missileType == 1)//explosive
            {
                double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceFromShip = Vector3D.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
                double distanceShip2Target = Vector3D.Distance(platformPosition, targetPosition);
                if (distanceFromTarget <= fuseDistance)
                {
                    foreach (IMyWarhead block in WARHEADS) { block.Detonate(); }
                }
                //if (distanceShip2Target <= spiralStart + spiralSafe)
                if (distanceFromTarget > spiralStart && distanceFromShip < spiralSafe)
                {
                    //guidanceLog.Append("explosive, MissileGuidance \n");
                    MissileGuidance();
                }
                else
                {
                    if (useSpiral && distanceFromTarget < spiralStart && distanceFromShip > spiralSafe)
                    {
                        //guidanceLog.Append("explosive, SpiralGuidance \n");
                        SpiralGuidance();
                    }
                    else
                    {
                        //guidanceLog.Append("explosive, MissileGuidance \n");
                        MissileGuidance();
                    }
                }
            }
            else if (missileType == 2)//drone
            {
                //guidanceLog.Append("drone, LockOnTarget - ManageDrone \n");
                LockOnTarget();

                ManageDrone();
            }
        }

        void ManageBrakes()
        {
            Vector3D velocityVec = CONTROLLER.GetShipVelocities().LinearVelocity;
            double speed = velocityVec.Length();
            if (speed > 1)
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

        bool TurretsDetection()
        {
            bool targetFound = false;
            foreach (IMyLargeTurretBase turret in TURRETS)
            {
                MyDetectedEntityInfo targ = turret.GetTargetedEntity();
                if (!targ.IsEmpty())
                {
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
            return targetFound;
        }

        void MissileGuidance()
        {
            Vector3D MissilePos = CONTROLLER.CubeGrid.WorldVolume.Center;
            Vector3D MissileVel = (MissilePos - prevPosition) / globalTimestep;
            Vector3D TargetVel = (targetPosition - prevTargetPosition) / globalTimestep;

            //Setup LOS rates and PN system
            Vector3D LOS_Old = Vector3D.Normalize(prevTargetPosition - prevPosition);
            Vector3D LOS_New = Vector3D.Normalize(targetPosition - MissilePos);
            Vector3D Rel_Vel = Vector3D.Normalize(TargetVel - MissileVel);

            //And Assigners
            Vector3D am = new Vector3D(1, 0, 0);
            double LOS_Rate;
            Vector3D LOS_Delta;
            Vector3D MissileForwards = THRUSTERS[0].WorldMatrix.Backward;

            //Vector/Rotation Rates
            if (LOS_Old.Length() == 0)
            { LOS_Delta = new Vector3D(0, 0, 0); LOS_Rate = 0.0; }
            else
            { LOS_Delta = LOS_New - LOS_Old; LOS_Rate = LOS_Delta.Length() / globalTimestep; }

            //Closing Velocity
            double Vclosing = (TargetVel - MissileVel).Length();

            //If Under Gravity Use Gravitational Accel
            Vector3D GravityComp = -CONTROLLER.GetNaturalGravity();

            //Calculate the final lateral acceleration
            Vector3D LateralDirection = Vector3D.Normalize(Vector3D.Cross(Vector3D.Cross(Rel_Vel, LOS_New), Rel_Vel));
            Vector3D LateralAccelerationComponent = LateralDirection * PNGain * LOS_Rate * Vclosing + LOS_Delta * 9.8 * (0.5 * PNGain); //Eases Onto Target Collision LOS_Delta * 9.8 * (0.5 * Gain)

            //If Impossible Solution (ie maxes turn rate) Use Drift Cancelling For Minimum T
            double OversteerReqt = (LateralAccelerationComponent).Length() / missileAccel;
            if (OversteerReqt > 0.98)
            {
                LateralAccelerationComponent = missileAccel * Vector3D.Normalize(LateralAccelerationComponent + (OversteerReqt * Vector3D.Normalize(-MissileVel)) * 40);
            }

            //Calculates And Applies Thrust In Correct Direction (Performs own inequality check)
            double ThrustPower = VectorProjectionScalar(MissileForwards, Vector3D.Normalize(LateralAccelerationComponent)); //TESTTESTTEST
            ThrustPower = isLargeGrid ? MathHelper.Clamp(ThrustPower, 0.9, 1) : ThrustPower;

            ThrustPower = MathHelper.Clamp(ThrustPower, 0.4, 1); //for improved thrust performance on the get-go
            foreach (IMyThrust thruster in THRUSTERS)
            {
                if (thruster.ThrustOverride != (thruster.MaxThrust * ThrustPower)) //12 increment inequality to help conserve on performance
                { thruster.ThrustOverride = (float)(thruster.MaxThrust * ThrustPower); }
            }

            //Calculates Remaining Force Component And Adds Along LOS
            double RejectedAccel = Math.Sqrt(missileAccel * missileAccel - LateralAccelerationComponent.LengthSquared()); //Accel has to be determined whichever way you slice it
            if (double.IsNaN(RejectedAccel)) { RejectedAccel = 0; }
            LateralAccelerationComponent += LOS_New * RejectedAccel;

            //-----------------------------------------------

            //Guides To Target Using Gyros
            am = Vector3D.Normalize(LateralAccelerationComponent + GravityComp);
            double Yaw; double Pitch;
            GyroTurn(am, 18, 0.3, THRUSTERS[0], prevYaw, prevPitch, out Pitch, out Yaw);

            //Updates For Next Tick Round
            prevYaw = Yaw;
            prevPitch = Pitch;
        }

        void GyroTurn(Vector3D targetVect, double gain, double dampingGain, IMyTerminalBlock REF, double YawPrev, double PitchPrev, out double NewPitch, out double NewYaw)
        {
            //Pre Setting Factors
            NewYaw = 0;
            NewPitch = 0;

            //Retrieving Forwards And Up
            Vector3D ShipUp = REF.WorldMatrix.Up;
            Vector3D ShipForward = REF.WorldMatrix.Backward; //Backward for thrusters

            //Create And Use Inverse Quatinion                   
            Quaternion Quat_Two = Quaternion.CreateFromForwardUp(ShipForward, ShipUp);
            var InvQuat = Quaternion.Inverse(Quat_Two);

            Vector3D DirectionVector = targetVect; //RealWorld Target Vector
            Vector3D RCReferenceFrameVector = Vector3D.Transform(DirectionVector, InvQuat); //Target Vector In Terms Of RC Block

            //Convert To Local Azimuth And Elevation
            double ShipForwardAzimuth = 0;
            double ShipForwardElevation = 0;
            Vector3D.GetAzimuthAndElevation(RCReferenceFrameVector, out ShipForwardAzimuth, out ShipForwardElevation);

            //Post Setting Factors
            NewYaw = ShipForwardAzimuth;
            NewPitch = ShipForwardElevation;

            //Applies Some PID Damping
            ShipForwardAzimuth += dampingGain * ((ShipForwardAzimuth - YawPrev) / globalTimestep);
            ShipForwardElevation += dampingGain * ((ShipForwardElevation - PitchPrev) / globalTimestep);

            //Does Some Rotations To Provide For any Gyro-Orientation
            var REF_Matrix = MatrixD.CreateWorld(REF.GetPosition(), (Vector3)ShipForward, (Vector3)ShipUp).GetOrientation();
            var Vector = Vector3.Transform((new Vector3D(ShipForwardElevation, ShipForwardAzimuth, 0)), REF_Matrix); //Converts To World

            foreach (IMyGyro gyro in GYROS)
            {
                var TRANS_VECT = Vector3.Transform(Vector, Matrix.Transpose(gyro.WorldMatrix.GetOrientation()));  //Converts To Gyro Local

                //Logic Checks for NaN's
                if (double.IsNaN(TRANS_VECT.X) || double.IsNaN(TRANS_VECT.Y) || double.IsNaN(TRANS_VECT.Z))
                { return; }

                //Applies To Scenario
                gyro.Pitch = (float)MathHelper.Clamp((-TRANS_VECT.X) * gain, -1000, 1000);
                gyro.Yaw = (float)MathHelper.Clamp(((-TRANS_VECT.Y)) * gain, -1000, 1000);
                gyro.Roll = (float)MathHelper.Clamp(((-TRANS_VECT.Z)) * gain, -1000, 1000);
                gyro.GyroOverride = true;
            }
        }

        public static double VectorProjectionScalar(Vector3D IN, Vector3D Axis_norm)//Use For Magnitudes Of Vectors In Directions (0-IN.length)
        {
            double OUT = 0;
            OUT = Vector3D.Dot(IN, Axis_norm);
            if (OUT == double.NaN)
            { OUT = 0; }
            return OUT;
        }

        void LockOnTarget()
        {
            MatrixD refWorldMatrix = CONTROLLER.WorldMatrix;
            float elapsedTime = currentTick * globalTimestep;
            Vector3D targetPos = targetPosition + (targetVelocity * elapsedTime);

            if (Vector3.IsZero(prevTargetVelocity))
            {
                prevTargetVelocity = targetVelocity;
            }

            Vector3D targetAccel = Vector3D.Zero;
            if ((!Vector3.IsZero(targetVelocity) || !Vector3.IsZero(prevTargetVelocity)) && !Vector3.IsZero((targetVelocity - prevTargetVelocity)))
            {
                targetAccel = (targetVelocity - prevTargetVelocity) / elapsedTime;
            }

            Vector3D aimDirection;
            double distanceFromTarget = Vector3D.Distance(targetPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            if (distanceFromTarget > gunsMaxRange)
            {
                aimDirection = ComputeInterceptPoint(targetPos, (Vector3D)targetVelocity - CONTROLLER.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
            }
            else
            {
                switch (weaponType)
                {
                    case 0://none
                        aimDirection = ComputeInterceptPoint(targetPos, (Vector3D)targetVelocity - CONTROLLER.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
                        break;
                    case 1://rockets
                        aimDirection = ComputeInterceptPointWithInheritSpeed(targetPos, (Vector3D)targetVelocity, targetAccel, (rocketProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * rocketProjectileForwardOffset)), CONTROLLER.GetShipVelocities().LinearVelocity, rocketProjectileInitialSpeed, rocketProjectileAccelleration, rocketProjectileMaxSpeed, rocketProjectileMaxRange);
                        break;
                    case 2://gatlings
                        aimDirection = ComputeInterceptPoint(targetPos, (Vector3D)targetVelocity - CONTROLLER.GetShipVelocities().LinearVelocity, targetAccel, (gatlingProjectileForwardOffset == 0 ? refWorldMatrix.Translation : refWorldMatrix.Translation + (refWorldMatrix.Forward * gatlingProjectileForwardOffset)), gatlingProjectileInitialSpeed, gatlingProjectileAccelleration, gatlingProjectileMaxSpeed);
                        break;
                    default:
                        aimDirection = ComputeInterceptPoint(targetPos, (Vector3D)targetVelocity - CONTROLLER.GetShipVelocities().LinearVelocity, targetAccel, refWorldMatrix.Translation, 9999, 9999, 9999);
                        break;
                }
            }

            aimDirection -= refWorldMatrix.Translation;

            double yawAngle, pitchAngle;
            GetRotationAngles(aimDirection, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            ApplyGyroOverride(pitchSpeed, yawSpeed, 0);
        }

        void SpiralGuidance()
        {
            Vector3D gravityVec = CONTROLLER.GetNaturalGravity();
            Vector3D headingVec = GetPointingVector(CONTROLLER.CubeGrid.WorldVolume.Center, CONTROLLER.GetShipVelocities().LinearVelocity, targetPosition, targetVelocity, targetAccelerationVector, gravityVec == null ? Vector3D.Zero : gravityVec);
            headingVec = SpiralTrajectory(headingVec, CONTROLLER.WorldMatrix.Forward, CONTROLLER.WorldMatrix.Up);
            if (status.Equals(statusCruising))
            {
                var headingDeviation = CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }
            // Get pitch and yaw angles
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(headingVec, -gravityVec, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);
            // Angle controller
            double yawSpeed = yawController.Control(yawAngle, secondsPerUpdate);
            double pitchSpeed = pitchController.Control(pitchAngle, secondsPerUpdate);
            // Handle roll more simply
            double rollSpeed = 0;
            if (Vector3D.IsZero(gravityVec))// && _missileStage == 4
            {
                rollSpeed = missileSpinRPM * rpm2Rad; //converts RPM to rad/s
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
            //Set appropriate gyro override
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed);
        }

        Vector3D SpiralTrajectory(Vector3D v_target, Vector3D v_front, Vector3D v_up)
        {
            double spiralRadius = Math.Tan(spiralDegrees * deg2Rad);
            if (Vector3D.IsZero(v_target)) return v_target;
            Vector3D v_targ_norm = Vector3D.Normalize(v_target);
            if (timeSpiral > timeMaxSpiral) timeSpiral = 0;
            double angle_theta = 2 * Math.PI * timeSpiral / timeMaxSpiral;
            if (v_front.Dot(v_targ_norm) > 0)
            {
                Vector3D v_x = Vector3D.Normalize(v_up.Cross(v_targ_norm));
                Vector3D v_y = Vector3D.Normalize(v_x.Cross(v_targ_norm));
                Vector3D v_target_adjusted = v_targ_norm + spiralRadius * (v_x * Math.Cos(angle_theta) + v_y * Math.Sin(angle_theta));
                return v_target_adjusted;
            }
            else
            {
                return v_targ_norm;
            }
        }

        public Vector3D GetPointingVector(Vector3D missilePosition, Vector3D missileVelocity, Vector3D targetPosition, Vector3D targetVelocity, Vector3D targetAcceleration, Vector3D gravity)
        {
            Vector3D missileToTarget = targetPosition - missilePosition;
            Vector3D missileToTargetNorm = Vector3D.Normalize(missileToTarget);
            Vector3D relativeVelocity = targetVelocity - missileVelocity;
            Vector3D lateralTargetAcceleration = (targetAcceleration - Vector3D.Dot(targetAcceleration, missileToTargetNorm) * missileToTargetNorm);
            Vector3D gravityCompensationTerm = 1.1 * -(gravity - Vector3D.Dot(gravity, missileToTargetNorm) * missileToTargetNorm);
            Vector3D lateralAcceleration = GetLatax(missileToTarget, missileToTargetNorm, relativeVelocity, lateralTargetAcceleration, gravityCompensationTerm);
            if (Vector3D.IsZero(lateralAcceleration)) return missileToTarget;
            double diff = missileAccel * missileAccel - lateralAcceleration.LengthSquared();
            if (diff < 0) return lateralAcceleration; //fly parallel to the target
            return lateralAcceleration + Math.Sqrt(diff) * missileToTargetNorm;
        }

        public Vector3D GetLatax(Vector3D missileToTarget, Vector3D missileToTargetNorm, Vector3D relativeVelocity, Vector3D lateralTargetAcceleration, Vector3D gravityCompensationTerm)
        {
            Vector3D omega = Vector3D.Cross(missileToTarget, relativeVelocity) / Math.Max(missileToTarget.LengthSquared(), 1); //to combat instability at close range
            Vector3D parallelVelocity = relativeVelocity.Dot(missileToTargetNorm) * missileToTargetNorm; //bootleg vector projection
            Vector3D normalVelocity = (relativeVelocity - parallelVelocity);
            return navConstant * (relativeVelocity.Length() * Vector3D.Cross(omega, missileToTargetNorm) + 0.1 * normalVelocity) + navAccelConstant * lateralTargetAcceleration + gravityCompensationTerm; //normal to LOS
        }

        public static Vector3D SafeNormalize(Vector3D a)
        {
            if (Vector3D.IsZero(a)) return Vector3D.Zero;
            if (Vector3D.IsUnit(ref a)) return a;
            return Vector3D.Normalize(a);
        }

        void BeamRide()
        {
            //Find vector from shooter to missile
            var shooterToMissileVec = CONTROLLER.WorldVolume.Center - platformPosition;
            if (Vector3D.IsZero(platformMatrix.Forward)) //this is to avoid NaN cases when the shooterForwardVec isnt cached yet
                platformMatrix.Forward = CONTROLLER.WorldMatrix.Forward; //messy but stops my code from breaking lol
                                                                         //Calculate perpendicular distance from shooter vector
            var projectionVec = Projection(shooterToMissileVec, platformMatrix.Forward);
            //Determine scaling factor
            double scalingFactor;
            Vector3D destinationVec;

            if (platformMatrix.Forward.Dot(shooterToMissileVec) > 0)
            {
                scalingFactor = projectionVec.Length() + Math.Max(2 * CONTROLLER.GetShipVelocities().LinearVelocity.Length(), 200); //travel approx. 200m from current position in direction of target vector
                destinationVec = platformPosition + scalingFactor * platformMatrix.Forward;
                if (!hasPassed) hasPassed = true;
            }
            else if (hasPassed)
            {
                int signLeft = Math.Sign(shooterToMissileVec.Dot(platformMatrix.Left));
                int signUp = Math.Sign(shooterToMissileVec.Dot(platformMatrix.Up));
                scalingFactor = -projectionVec.Length() + Math.Max(2 * CONTROLLER.GetShipVelocities().LinearVelocity.Length(), 200); //added the Math.Max part for modded speed worlds
                destinationVec = platformPosition + scalingFactor * platformMatrix.Forward + signLeft * 100 * platformMatrix.Left + signUp * 100 * platformMatrix.Up;
            }
            else
            {
                scalingFactor = -projectionVec.Length() + Math.Max(2 * CONTROLLER.GetShipVelocities().LinearVelocity.Length(), 200);
                destinationVec = platformPosition + scalingFactor * platformMatrix.Forward;
            }
            //Find vector from missile to destinationVec
            Vector3D missileToTargetVec = destinationVec - CONTROLLER.WorldVolume.Center;
            Vector3D headingVec;
            //Drift compensation
            if (status.Equals(statusCruising))
            {
                headingVec = CalculateDriftCompensation(CONTROLLER.GetShipVelocities().LinearVelocity, missileToTargetVec, missileAccel, 0.5, CONTROLLER.GetNaturalGravity(), 60);
            }
            else
            {
                headingVec = destinationVec - CONTROLLER.WorldVolume.Center;
            }
            if (status.Equals(statusCruising))
            {
                var headingDeviation = CosBetween(headingVec, CONTROLLER.WorldMatrix.Forward);
                ApplyThrustOverride(THRUSTERS, (float)MathHelper.Clamp(headingDeviation, 0.25f, 1f) * 100f);
            }
            // Get pitch and yaw angles
            double yawAngle;
            double pitchAngle;
            double rollAngle;
            GetRotationAnglesSimultaneous(headingVec, -CONTROLLER.GetNaturalGravity(), CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);
            // Angle controller
            double yawSpeed = yawController.Control(yawAngle, secondsPerUpdate);
            double pitchSpeed = pitchController.Control(pitchAngle, secondsPerUpdate);
            // Handle roll more simply
            double rollSpeed = 0;
            if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity()) && status.Equals(statusCruising))
            {
                rollSpeed = missileSpinRPM * rpm2Rad; //converts RPM to rad/s
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
            //Set appropriate gyro override
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed);
        }

        static Vector3D CalculateDriftCompensation(Vector3D velocity, Vector3D directHeading, double accel, double timeConstant, Vector3D gravityVec, double maxDriftAngle = 60)
        {
            if (directHeading.LengthSquared() == 0) return velocity;
            if (Vector3D.Dot(velocity, directHeading) < 0) return directHeading;
            if (velocity.LengthSquared() < 100) return directHeading;
            var normalVelocity = Rejection(velocity, directHeading);
            var normal = SafeNormalize(normalVelocity);
            var parallel = SafeNormalize(directHeading);
            var normalAccel = Vector3D.Dot(normal, normalVelocity) / timeConstant;
            normalAccel = Math.Min(normalAccel, accel * Math.Sin(MathHelper.ToRadians(maxDriftAngle)));
            var gravityCompensationTerm = 1.1 * -(Rejection(gravityVec, directHeading));
            var normalAccelerationVector = normalAccel * normal + gravityCompensationTerm;
            double parallelAccel = 0;
            var diff = accel * accel - normalAccelerationVector.LengthSquared();
            if (diff > 0) parallelAccel = Math.Sqrt(diff);
            return parallelAccel * parallel - normal * normalAccel;
        }

        void ApplyThrustOverride(List<IMyThrust> thrusters, float overrideValue, bool turnOn = true)
        {
            float thrustProportion = overrideValue * 0.01f;
            foreach (IMyThrust thisThrust in thrusters)
            {
                if (thisThrust.Enabled != turnOn) thisThrust.Enabled = turnOn;
                if (thrustProportion != thisThrust.ThrustOverridePercentage) thisThrust.ThrustOverridePercentage = thrustProportion;
            }
        }

        public static Vector3D Rejection(Vector3D a, Vector3D b) //reject a on b
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) return Vector3D.Zero;
            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        public static Vector3D Projection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) return Vector3D.Zero;
            return a.Dot(b) / b.LengthSquared() * b;
        }

        public static double CosBetween(Vector3D a, Vector3D b, bool useSmallestAngle = false) //returns radians
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b)) return 0;
            else return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
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

        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double yaw, out double pitch, out double roll)
        {
            desiredForwardVector = SafeNormalize(desiredForwardVector);
            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);
            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            double angle;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
            {
                axis = Vector3D.Cross(Vector3D.Forward, desiredForwardVector);
                angle = Math.Asin(MathHelper.Clamp(axis.Length(), -1, 1));
            }
            else
            {
                leftVector = SafeNormalize(leftVector);
                Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);
                // Create matrix
                MatrixD targetMatrix = MatrixD.Zero;
                targetMatrix.Forward = desiredForwardVector;
                targetMatrix.Left = leftVector;
                targetMatrix.Up = upVector;
                axis = Vector3D.Cross(Vector3D.Backward, targetMatrix.Backward) + Vector3D.Cross(Vector3D.Up, targetMatrix.Up) + Vector3D.Cross(Vector3D.Right, targetMatrix.Right);
                double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
            }
            axis = SafeNormalize(axis);
            yaw = -axis.Y * angle;
            pitch = axis.X * angle;
            roll = -axis.Z * angle;
        }

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed)
        {
            Vector3D rotationVec = new Vector3D(-pitchSpeed, yawSpeed, rollSpeed); //because keen does some weird stuff with signs 
            Vector3D relativeRotationVec = Vector3D.TransformNormal(rotationVec, CONTROLLER.WorldMatrix);
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
            double yawAngle, pitchAngle;
            GetRotationAngles(-velocityVec, CONTROLLER.WorldMatrix, out yawAngle, out pitchAngle);

            double yawSpeed = yawController.Control(yawAngle, globalTimestep);
            double pitchSpeed = pitchController.Control(pitchAngle, globalTimestep);

            ApplyGyroOverride(pitchSpeed, yawSpeed, 0);

            double brakingAngle = GetAngleBetween(CONTROLLER.WorldMatrix.Forward, -velocityVec);
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

        void PrepareForLaunch()
        {
            foreach (IMyPowerProducer block in GENERATORS)
            {
                block.Enabled = true;
                if (block is IMyBatteryBlock) { (block as IMyBatteryBlock).ChargeMode = ChargeMode.Discharge; }
            }
            foreach (IMyShipMergeBlock item in MERGES) { item.Enabled = false; }
            foreach (IMyShipConnector item in CONNECTORS) { item.Disconnect(); item.Enabled = false; }
            foreach (IMyThrust item in ALLTHRUSTERS) { item.Enabled = true; }
        }

        void CalculateBaseAcceleration()
        {
            missileMass = 0;
            missileThrust = 0;

            float totalMass = CONTROLLER.CalculateShipMass().TotalMass;
            foreach (IMyTerminalBlock block in TBLOCKS)
            {
                missileMass += block.Mass;
            }

            foreach (IMyThrust item in THRUSTERS)
            {
                missileThrust += (double)item.MaxThrust;
            }

            missileAccel = missileThrust / missileMass;
        }

        void CalculateAcceleration()
        {
            missileMass = CONTROLLER.CalculateShipMass().TotalMass;
            missileThrust = 0;

            foreach (IMyThrust item in THRUSTERS)
            {
                missileThrust += (double)item.MaxThrust;
            }

            missileAccel = missileThrust / missileMass;
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

        void UpdateMaxSpeed()
        {
            double speed = CONTROLLER.GetShipSpeed();
            if (speed > maxSpeed)
            {
                maxSpeed = speed;
            }
            //currentAcceleration = (CONTROLLER.GetShipVelocities().LinearVelocity - prevVelocity) / globalTimestep;
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

        void UpdateBroadcastRange(Vector3D platformPosition)
        {
            var distance = Vector3.Distance(platformPosition, CONTROLLER.CubeGrid.WorldVolume.Center);
            ANTENNA.Radius = distance + 100;
            //WriteAntennaRangeLog(ANTENNA.Radius);
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

        void WriteDebug()
        {
            //if (DEBUG != null)
            //{
            //StringBuilder text = new StringBuilder("");
            //text.Append(uniListenerLog);
            //text.Append(broadListenerLog);
            //text.Append(initLaunchLog);
            //text.Append(initUpdateLog);
            //text.Append(initLostLog);
            //text.Append(initBeamLog);
            //text.Append(initThrustersLog);
            //text.Append(uniSenderLog);
            //text.Append(antennaRadiusLog);
            //text.Append(guidanceLog);
            //DEBUG.WriteText(text);
            //}
        }

        void WriteAntennaRangeLog(float antennaRadius)
        {
            //if (writeCount == writeDelay)
            //{
            //antennaRadiusLog.Clear();
            //antennaRadiusLog.Append("Antenna Radius: " + antennaRadius + "\n");
            //}
        }

        void WriteUniSenderLog(bool messageSent)
        {
            //if (writeCount == writeDelay)
            //{
            //uniSenderLog.Clear();
            //uniSenderLog.Append("Unicast Message Sent: " + messageSent + "\n");
            //}
        }

        void WriteThrustersLog(bool startThrusters, bool startThrustersOnce, bool startTargeting)
        {
            //initThrustersLog.Clear();
            //initThrustersLog.Append("Thrusters, startThrusters:" + startThrusters + ", startThrustersOnce:" + startThrustersOnce + ", startTargeting:" + startTargeting + "\n");
        }

        void ClearInitLog()
        {
            //initLaunchLog.Clear();
            //initUpdateLog.Clear();
            //initLostLog.Clear();
            //initBeamLog.Clear();
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

            //DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
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
