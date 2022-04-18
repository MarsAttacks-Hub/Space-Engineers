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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        
        readonly string rotorsName = "Rotor_MD_A";
        readonly string rotorsInvName = "Rotor_MD_B";
        readonly string plusXname = "Merge_MD-X";
        readonly string plusYname = "Merge_MD+Z";
        readonly string plusZname = "Merge_MD+Y";
        readonly string minusXname = "Merge_MD+X";
        readonly string minusYname = "Merge_MD-Z";
        readonly string minusZname = "Merge_MD-Y";
        readonly string thrustersName = "[CRX] HThruster";
        readonly string upThrustersName = "UP";
        readonly string downThrustersName = "DOWN";
        readonly string leftThrustersName = "LEFT";
        readonly string rightThrustersName = "RIGHT";
        readonly string forwardThrustersName = "FORWARD";
        readonly string backwardThrustersName = "BACKWARD";
        readonly string controllersName = "[CRX] Controller";
        readonly string gyrosName = "[CRX] Gyro";
        readonly string managerName = "[CRX] PB Manager";
        readonly string remotesName = "[CRX] Controller Remote";
        readonly string cockpitName = "[CRX] Controller Cockpit";
        readonly string deadManPanelName = "[CRX] LCD DeadMan Toggle";
        readonly string idleThrusterPanelName = "[CRX] LCD IdleThrusters Toggle";
        //readonly string debugPanelName = "[CRX] Debug";

        const string argSetup = "Setup";
        const string argIdleThrusters = "ToggleIdleThrusters";
        const string argGyroStabilizeOff = "StabilizeOff";
        const string argGyroStabilizeOn = "StabilizeOn";
        const string argSunchaseOff = "SunchaseOff";
        const string argDeadMan = "DeadMan";

        bool controlDampeners = true;
        bool useGyrosToStabilize = true;    //If the script will override gyros to try and combat torque
        readonly bool useRoll = true;
        bool idleThrusters = false;
        readonly float maxSpeed = 105f;
        readonly float minSpeed = 5f;
        readonly float targetVel = 29 * rpsOverRpm;
        readonly float syncSpeed = 1 * rpsOverRpm;
        readonly int writeDelay = 100;
        
        int writeCount = 0;
        bool deadManOnce = false;
        bool sunChaseOff = false;
        bool switchOnce = false;
        bool setOnce = false;
        bool initAutoThrustOnce = true;
        bool hasVector = false;
        Vector3D lastForwardVector = Vector3D.Zero;
        Vector3D lastUpVector = Vector3D.Zero;

        static readonly float rpsOverRpm = (float)(Math.PI / 30);
        static readonly float circle = (float)(2 * Math.PI);

        public List<IMyMotorStator> ROTORS = new List<IMyMotorStator>();
        public List<IMyMotorStator> ROTORSINV = new List<IMyMotorStator>();
        public List<IMyShipMergeBlock> MERGESPLUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESPLUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSX = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSY = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGESMINUSZ = new List<IMyShipMergeBlock>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> UPTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> DOWNTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> LEFTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> RIGHTTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> FORWARDTHRUSTERS = new List<IMyThrust>();
        public List<IMyThrust> BACKWARDTHRUSTERS = new List<IMyThrust>();
        public IMyThrust UPTHRUST;
        public IMyThrust DOWNTHRUST;
        public IMyThrust LEFTTHRUST;
        public IMyThrust RIGHTTHRUST;
        public IMyThrust FORWARDTHRUST;
        public IMyThrust BACKWARDTHRUST;
        public IMyShipController CONTROLLER = null;
        public IMyShipController COCKPIT;
        public IMyProgrammableBlock MANAGERPB;
        public IMyRemoteControl REMOTE;
        public IMyTextPanel LCDDEADMAN;
        public IMyTextPanel LCDIDLETHRUSTERS;
        //public IMyTextPanel DEBUG;
        //public StringBuilder debugLog = new StringBuilder("");
        

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Setup();
        }

        void Setup()
        {
            GetBlocks();

            if (useGyrosToStabilize)
            {
                Me.CustomData = "GyroStabilize=true";
            }
            else
            {
                Me.CustomData = "GyroStabilize=false";
            }
        }

        void Main(string argument)
        {
            Echo($"ROTORS:{ROTORS.Count}");
            Echo($"ROTORSINV:{ROTORSINV.Count}");
            Echo($"THRUSTERS:{THRUSTERS.Count}");
            Echo($"GYROS:{GYROS.Count}");
            Echo($"CONTROLLERS:{CONTROLLERS.Count}");
            Echo($"MERGESPLUSX:{MERGESPLUSX.Count}");
            Echo($"MERGESPLUSY:{MERGESPLUSY.Count}");
            Echo($"MERGESPLUSZ:{MERGESPLUSZ.Count}");
            Echo($"MERGESMINUSX:{MERGESMINUSX.Count}");
            Echo($"MERGESMINUSY:{MERGESMINUSY.Count}");
            Echo($"MERGESMINUSZ:{MERGESMINUSZ.Count}");

            if (!string.IsNullOrEmpty(argument))
            {
                ProcessArgument(argument);
            }

            bool isControlled = GetController();

            //debugLog.Clear();

            if (!isControlled)
            {
                if (!setOnce)
                {
                    SetPow(Vector3D.Zero);
                    foreach (IMyMotorStator block in ROTORS)
                    {
                        block.TargetVelocityRPM = 0;
                        //block.RotorLock = true;
                        block.Enabled = false;
                    }
                    foreach (IMyMotorStator block in ROTORSINV)
                    {
                        block.TargetVelocityRPM = 0;
                        //block.RotorLock = true;
                        block.Enabled = false;
                    }
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    setOnce = true;
                }
                return;
            }
            else
            {
                if (setOnce)
                {
                    foreach (IMyMotorStator block in ROTORS)
                    {
                        //block.RotorLock = false;
                        block.Enabled = true;
                    }
                    foreach (IMyMotorStator block in ROTORSINV)
                    {
                        //block.RotorLock = false;
                        block.Enabled = true;
                    }
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    setOnce = false;

                    if (MANAGERPB != null)
                    {
                        if (MANAGERPB.CustomData.Contains("SunChaser=true"))
                        {
                            sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                        }
                    }
                }

                if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true"))
                {
                    sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                }
            }

            SyncRotors();

            if (REMOTE.IsAutoPilotEnabled)
            {                
                AutoMagneticDrive();

                if (useGyrosToStabilize && REMOTE != null)
                {
                    GyroStabilize(false, REMOTE, REMOTE.RotationIndicator, REMOTE.RollIndicator);
                }
            }
            else
            {
                if (!initAutoThrustOnce) {
                    foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }

                    initAutoThrustOnce = true;
                }
                
                MagneticDrive();

                if (useGyrosToStabilize && CONTROLLER != null)
                {
                    GyroStabilize(false, CONTROLLER, CONTROLLER.RotationIndicator, CONTROLLER.RollIndicator);
                }
            }

            if (writeCount == writeDelay) 
            { 
                //DEBUG.WriteText(debugLog);
                if (controlDampeners) 
                { 
                    DeadMan();
                    LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                }
                else
                {
                    LCDDEADMAN.BackgroundColor = new Color(0, 0, 0);
                }
                if (idleThrusters) 
                { 
                    LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                }
                else
                {
                    LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0);
                }
                writeCount = 0; 
            } 
            writeCount++;
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argSetup: Setup(); break;
                case argIdleThrusters: 
                    idleThrusters = !idleThrusters; 
                    if (idleThrusters) {
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0);
                    }
                    break;
                case argGyroStabilizeOn:
                    useGyrosToStabilize = true;
                    Me.CustomData = "GyroStabilize=true";
                    break;
                case argGyroStabilizeOff:
                    useGyrosToStabilize = false;
                    Me.CustomData = "GyroStabilize=false";
                    break;
                case argDeadMan: 
                    controlDampeners = !controlDampeners; 
                    if (controlDampeners) {
                        LCDDEADMAN.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        LCDDEADMAN.BackgroundColor = new Color(0, 0, 0);
                    }
                    break;
            }
        }

        bool GetController()
        {
            if (CONTROLLER != null && (!CONTROLLER.IsUnderControl || !CONTROLLER.CanControlShip || !CONTROLLER.ControlThrusters))
            {
                CONTROLLER = null;
            }
            if (CONTROLLER == null)
            {
                foreach (IMyShipController block in CONTROLLERS)
                {
                    if (block.IsFunctional && block.IsUnderControl && block.CanControlShip && block.ControlThrusters && !(block is IMyRemoteControl))
                    {
                        CONTROLLER = block;
                        COCKPIT = CONTROLLER;
                    }
                }
            }
            bool controlled;
            if (CONTROLLER == null)
            {
                controlled = false;
            }
            else
            {
                controlled = true;
            }
            if (REMOTE.IsAutoPilotEnabled)
            {
                CONTROLLER = COCKPIT;
                controlled = true;
            }
            Vector3D velocityVec = COCKPIT.GetShipVelocities().LinearVelocity;
            double speed = velocityVec.Length();
            if (speed > 1)
            {
                CONTROLLER = COCKPIT;
                controlled = true;
            }
            return controlled;
        }

        void SyncRotors()
        {
            float angle = 0;
            foreach (IMyMotorStator rotor in ROTORS) { angle += rotor.Angle; }
            foreach (IMyMotorStator rotor in ROTORSINV) { angle += circle - rotor.Angle; }
            angle /= ROTORS.Count() + ROTORSINV.Count();
            float angleInv = circle - angle;
            foreach (IMyMotorStator rotor in ROTORS)
            {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angle, Smallest(rotorAngle - angle + circle, rotorAngle - angle - circle));
                if (asyncAngle > 0)
                {
                    rotor.TargetVelocityRad = (targetVel - syncSpeed);
                }
                else
                {
                    rotor.TargetVelocityRad = (targetVel + syncSpeed);
                }
            }
            foreach (IMyMotorStator rotor in ROTORSINV)
            {
                float rotorAngle = rotor.Angle;
                float asyncAngle = Smallest(rotorAngle - angleInv, Smallest(rotorAngle - angleInv + circle, rotorAngle - angleInv - circle));
                if (asyncAngle > 0)
                {
                    rotor.TargetVelocityRad = (-targetVel - syncSpeed);
                }
                else
                {
                    rotor.TargetVelocityRad = (-targetVel + syncSpeed);
                }
            }
        }

        IMyThrust AutopilotThrustInitializer(List<IMyThrust> thrusters)
        {
            IMyThrust thruster = null;
            int i = 0;
            foreach(IMyThrust thrust in thrusters)
            {
                if (i == 0)
                {
                    thruster = thrust;
                    thrust.Enabled = true;
                }
                else
                {
                    thrust.Enabled = false;
                }
                i++;
            }
            return thruster;
        }

        void AutoMagneticDrive()
        {
            if (initAutoThrustOnce) { 
                UPTHRUST = AutopilotThrustInitializer(UPTHRUSTERS);
                DOWNTHRUST = AutopilotThrustInitializer(DOWNTHRUSTERS);
                LEFTTHRUST = AutopilotThrustInitializer(LEFTTHRUSTERS);
                RIGHTTHRUST = AutopilotThrustInitializer(RIGHTTHRUSTERS);
                FORWARDTHRUST = AutopilotThrustInitializer(FORWARDTHRUSTERS);
                BACKWARDTHRUST = AutopilotThrustInitializer(BACKWARDTHRUSTERS);

                initAutoThrustOnce = false;
            }

            Vector3 dir = new Vector3();
            if (FORWARDTHRUST.CurrentThrust > 0)
            {
                dir.Z = -1;
            }
            else if (BACKWARDTHRUST.CurrentThrust > 0)
            {
                dir.Z = 1;
            }

            if (UPTHRUST.CurrentThrust > 0)
            {
                dir.Y = 1;
            }
            else if (DOWNTHRUST.CurrentThrust > 0)
            {
                dir.Y = -1;
            }

            if (LEFTTHRUST.CurrentThrust > 0)
            {
                dir.X = -1;
            }
            else if (RIGHTTHRUST.CurrentThrust > 0)
            {
                dir.X = 1;
            }

            SetPow(dir);
        }

        void MagneticDrive()
        {
            Matrix mtrx;
            Vector3 dir;
            dir = CONTROLLER.MoveIndicator;
            CONTROLLER.Orientation.GetMatrix(out mtrx);
            dir = Vector3.Transform(dir, mtrx);
            if (dir.X != 0 || dir.Y != 0 || dir.Z != 0)
            {
                //debugLog.Append("dir X: " + dir.X + "\ndir Y: " + dir.Y + "\ndir Z: " + dir.Z + "\n\n");
                dir /= dir.Length();
                if (!switchOnce)
                {
                    foreach (IMyThrust thrust in THRUSTERS)
                    {
                        thrust.Enabled = false;
                    }
                    switchOnce = true;
                }
            }
            else
            {
                if (switchOnce)
                {
                    foreach (IMyThrust thrust in THRUSTERS)
                    {
                        thrust.Enabled = true;
                    }
                    switchOnce = false;
                }
            }
            if (!CONTROLLER.DampenersOverride && dir.LengthSquared() == 0)
            {
                SetPow(Vector3.Zero);
                return;
            }

            CONTROLLER.Orientation.GetMatrix(out mtrx);
            Vector3 vel = CONTROLLER.GetShipVelocities().LinearVelocity;
            vel = Vector3.Transform(vel, MatrixD.Transpose(CONTROLLER.WorldMatrix.GetOrientation()));
            vel = dir * maxSpeed - Vector3.Transform(vel, mtrx);
            if (Math.Abs(vel.X) < minSpeed)
            {
                vel.X = 0;
            }
            if (Math.Abs(vel.Y) < minSpeed)
            {
                vel.Y = 0;
            }
            if (Math.Abs(vel.Z) < minSpeed)
            {
                vel.Z = 0;
            }
            SetPow(vel);
        }

        void SetPow(Vector3 pow)
        {
            if (pow.X != 0)
                if (pow.X > 0)
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
                }
                else
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = true; }
                }
            else
            {
                foreach (IMyShipMergeBlock block in MERGESPLUSX) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSX) { block.Enabled = false; }
            }
            if (pow.Y != 0)
                if (pow.Y > 0)
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
                }
                else
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = true; }
                }
            else
            {
                foreach (IMyShipMergeBlock block in MERGESPLUSY) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSY) { block.Enabled = false; }
            }
            if (pow.Z != 0)
                if (pow.Z > 0)
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = true; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
                }
                else
                {
                    foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = true; }
                }
            else
            {
                foreach (IMyShipMergeBlock block in MERGESPLUSZ) { block.Enabled = false; }
                foreach (IMyShipMergeBlock block in MERGESMINUSZ) { block.Enabled = false; }
            }
        }

        float Smallest(float rotorAngle, float b)
        {
            return Math.Abs(rotorAngle) > Math.Abs(b) ? b : rotorAngle;
        }

        void GyroStabilize(bool overrideOn, IMyShipController reference, Vector2 mouseInput, float rollInput)
        {
            if (!hasVector)
            {
                hasVector = true;
                lastForwardVector = reference.WorldMatrix.Forward;
                lastUpVector = reference.WorldMatrix.Up;
            }

            double pitchAngle, yawAngle, rollAngle;
            if (!useRoll) { lastUpVector = Vector3D.Zero; };
            GetRotationAngles(lastForwardVector, lastUpVector, reference.WorldMatrix, out yawAngle, out pitchAngle, out rollAngle);

            var updatesPerSecond = 10;
            var timeMaxCycle = 1 / updatesPerSecond;

            var localAngularDeviation = new Vector3D(-pitchAngle, yawAngle, rollAngle);
            var worldAngularDeviation = Vector3D.TransformNormal(localAngularDeviation, reference.WorldMatrix);
            var worldAngularVelocity = worldAngularDeviation / timeMaxCycle;

            var localMouseInput = new Vector3(mouseInput.X, mouseInput.Y, rollInput);

            if (!Vector3D.IsZero(localMouseInput, 1E-3))
            {
                overrideOn = false;
            }

            foreach (var block in GYROS)
            {
                if (overrideOn)
                {
                    var gyroAngularVelocity = Vector3D.TransformNormal(worldAngularVelocity, MatrixD.Transpose(block.WorldMatrix));
                    gyroAngularVelocity *= updatesPerSecond / 60.0;

                    block.Pitch = (float)Math.Round(gyroAngularVelocity.X, 2);
                    block.Yaw = (float)Math.Round(gyroAngularVelocity.Y, 2);
                    block.Roll = (float)Math.Round(gyroAngularVelocity.Z, 2);
                    block.GyroOverride = true;
                    //block.GyroPower = 100f; //im assuming this is a percentage
                }
                else
                {
                    block.GyroOverride = false;
                }
            }

            lastForwardVector = reference.WorldMatrix.Forward;
            lastUpVector = reference.WorldMatrix.Up;
        }

        static void GetRotationAngles(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double yaw, out double pitch, out double roll)
        {
            var localTargetVector = Vector3D.Rotate(desiredForwardVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            int yawSign = localTargetVector.X >= 0 ? 1 : -1;
            yaw = GetAngleBetween(Vector3D.Forward, flattenedTargetVector) * yawSign; //right is positive

            int pitchSign = Math.Sign(localTargetVector.Y);
            if (Vector3D.IsZero(flattenedTargetVector))//check for straight up case
            {
                pitch = MathHelper.PiOver2 * pitchSign;
            }
            else
            {
                pitch = GetAngleBetween(localTargetVector, flattenedTargetVector) * pitchSign; //up is positive
            }
            if (Vector3D.IsZero(desiredUpVector))
            {
                roll = 0;
                return;
            }
            Vector3D orthagonalUp;// Since there is a relationship between roll and the orientation of forward we need to ensure that the up we are comparing is orthagonal to forward.
            Vector3D orthagonalLeft = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            if (Vector3D.Dot(desiredForwardVector, desiredUpVector) == 0)// Already orthagonal
            {
                orthagonalUp = desiredUpVector;
            }
            else
            {
                orthagonalUp = Vector3D.Cross(desiredForwardVector, orthagonalLeft);
            }
            var localUpVector = Vector3D.Rotate(orthagonalUp, MatrixD.Transpose(worldMatrix));
            int signRoll = Vector3D.Dot(localUpVector, Vector3D.Right) >= 0 ? 1 : -1;

            if (Vector3D.IsZero(flattenedTargetVector))// Desired forward and current up are parallel This implies pitch is ±90° and yaw is 0°.
            {
                var localUpFlattenedY = new Vector3D(localUpVector.X, 0, localUpVector.Z);

                var referenceDirection = Vector3D.Dot(Vector3D.Up, localTargetVector) >= 0 ? Vector3D.Backward : Vector3D.Forward;// If straight up, reference direction would be backward, if straight down, reference direction would be forward. This is because we are simply doing a ±90° pitch rotation of the axes.

                roll = GetAngleBetween(localUpFlattenedY, referenceDirection) * signRoll;
                return;
            }
            var intermediateFront = flattenedTargetVector;// We are going to try and construct new intermediate axes where: Up = Vector3D.Up Front = flattenedTargetVector This will let us create a plane that contains Vector3D.Up and  whose normal equals flattenedTargetVector

            var localUpProjOnIntermediateForward = Vector3D.Dot(intermediateFront, localUpVector) / intermediateFront.LengthSquared() * intermediateFront;// Reject up vector onto the plane normal
            var flattenedUpVector = localUpVector - localUpProjOnIntermediateForward;

            var intermediateRight = Vector3D.Cross(intermediateFront, Vector3D.Up);
            int rollSign = Vector3D.Dot(flattenedUpVector, intermediateRight) >= 0 ? 1 : -1;
            roll = GetAngleBetween(flattenedUpVector, Vector3D.Up) * rollSign;
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

        bool IsPiloted() {
            bool isPiloted = false;
            foreach (IMyShipController block in CONTROLLERS) {
                if (block.IsFunctional && block.IsUnderControl && block.CanControlShip && block.ControlThrusters) {
                    isPiloted = true;
                    break;
                }
                if (block is IMyRemoteControl) {
                    if ((block as IMyRemoteControl).IsAutoPilotEnabled) {
                        isPiloted = true;
                        break;
                    }
                }
            }
            return isPiloted;
        }

        void DeadMan() {
            bool undercontrol = IsPiloted();
            if (!undercontrol) {
                IMyShipController cntrllr = null;
                foreach (IMyShipController block in CONTROLLERS) {
                    if (block.CanControlShip) {
                        cntrllr = block;
                        break;
                    }
                }
                if (cntrllr != null) {
                    double speed = cntrllr.GetShipSpeed();
                    if (speed > minSpeed) {
                        foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = true; }
                        cntrllr.DampenersOverride = true;
                    } else {
                        if (!deadManOnce) {
                            if (idleThrusters)
                            { foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = false; } }
                            deadManOnce = true;
                        }
                    }
                }
            } else {
                if (deadManOnce) {
                    foreach (IMyThrust thrst in THRUSTERS) { thrst.Enabled = true; }
                    deadManOnce = false;
                }
            }
        }

        void GetBlocks()
        {
            ROTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORS, block => block.CustomName.Contains(rotorsName));
            ROTORSINV.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(ROTORSINV, block => block.CustomName.Contains(rotorsInvName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, block => block.CustomName.Contains(controllersName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            MERGESPLUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSX, block => block.CustomName.Contains(plusXname));
            MERGESPLUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSY, block => block.CustomName.Contains(plusYname));
            MERGESPLUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESPLUSZ, block => block.CustomName.Contains(plusZname));
            MERGESMINUSX.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSX, block => block.CustomName.Contains(minusXname));
            MERGESMINUSY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSY, block => block.CustomName.Contains(minusYname));
            MERGESMINUSZ.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESMINUSZ, block => block.CustomName.Contains(minusZname));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains(thrustersName));
            UPTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(upThrustersName)));
            DOWNTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(downThrustersName)));
            LEFTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(leftThrustersName)));
            RIGHTTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(rightThrustersName)));
            FORWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(forwardThrustersName)));
            BACKWARDTHRUSTERS.AddRange(THRUSTERS.Where(block => block.CustomName.Contains(backwardThrustersName)));
            List<IMyRemoteControl> REMOTES = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(REMOTES, block => block.CustomName.Contains(remotesName));
            REMOTE = REMOTES[0];
            List<IMyShipController> COCKPITS = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(COCKPITS, block => block.CustomName.Contains(cockpitName));
            COCKPIT = COCKPITS[0];
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
            LCDDEADMAN = GridTerminalSystem.GetBlockWithName(deadManPanelName) as IMyTextPanel;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName(idleThrusterPanelName) as IMyTextPanel;
            //DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
        }


    }
}
