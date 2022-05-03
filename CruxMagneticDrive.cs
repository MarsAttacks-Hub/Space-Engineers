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
        //TODO add security system where if uncontroller and a turret detect a eneemy the ship fly away from it
        //or calculate the enemy trajectory vector and if it's going toward the ship then move away (evasive manouvre)
        //NAVIGATOR

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
        readonly string deadManPanelName = "[CRX] LCD DeadMan Toggle";
        readonly string idleThrusterPanelName = "[CRX] LCD IdleThrusters Toggle";
        
        const string argDeadMan = "DeadMan";
        const string argMagneticDrive = "ToggleMagneticDrive";
        const string argIdleThrusters = "ToggleIdleThrusters";
        const string argGyroStabilizeOff = "StabilizeOff";
        const string argGyroStabilizeOn = "StabilizeOn";

        const string argSunchaseOff = "SunchaseOff";
        
        bool magneticDrive = true;
        bool controlDampeners = true;
        bool useGyrosToStabilize = true;//If the script will override gyros to try and combat torque
        readonly bool useRoll = true;
        bool idleThrusters = false;
        readonly float maxSpeed = 105f;
        readonly float minSpeed = 5f;
        readonly float deadManMinSpeed = 0.1f;
        readonly float targetVel = 29 * rpsOverRpm;
        readonly float syncSpeed = 1 * rpsOverRpm;
        readonly int tickDelay = 100;

        int tickCount = 0;
        bool magneticDriveManOnce = true;
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
        IMyThrust UPTHRUST;
        IMyThrust DOWNTHRUST;
        IMyThrust LEFTTHRUST;
        IMyThrust RIGHTTHRUST;
        IMyThrust FORWARDTHRUST;
        IMyThrust BACKWARDTHRUST;
        IMyShipController CONTROLLER = null;
        IMyProgrammableBlock MANAGERPB;
        IMyRemoteControl REMOTE;
        IMyTextPanel LCDDEADMAN;
        IMyTextPanel LCDIDLETHRUSTERS;

        //public IMyTextPanel DEBUG;
        //public StringBuilder debugLog = new StringBuilder("");
        //readonly int writeDelay = 100;
        //int writeCount = 0;
        //readonly string debugPanelName = "[CRX] Debug";

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

        public void Main(string argument)
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
            //debugLog.Clear();
            if (magneticDrive)
            {
                if (magneticDriveManOnce)
                {
                    InitMagneticDrive();
                    magneticDriveManOnce = false;
                }

                bool isControlled = GetController();

                if (!isControlled)
                {
                    if (!setOnce)
                    {
                        IdleMagneticDrive();
                        setOnce = true;
                    }
                }
                else
                {
                    if (setOnce)
                    {
                        InitMagneticDrive();
                        setOnce = false;
                    }
                    if (!sunChaseOff && MANAGERPB.CustomData.Contains("SunChaser=true"))
                    {
                        sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                    }

                    SyncRotors();

                    if (REMOTE.IsAutoPilotEnabled)
                    {                
                        AutoMagneticDrive();
                        GyroStabilize(false, REMOTE, REMOTE.RotationIndicator, REMOTE.RollIndicator);
                    }
                    else
                    {
                        if (!initAutoThrustOnce) {
                            foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
                            initAutoThrustOnce = true;
                        }
                        
                        MagneticDrive();
                        GyroStabilize(false, CONTROLLER, CONTROLLER.RotationIndicator, CONTROLLER.RollIndicator);
                    }
                }
            }
            else
            {
                if (!magneticDriveManOnce)
                {
                    IdleMagneticDrive();
                    magneticDriveManOnce = true;
                }
                if (tickCount == tickDelay) 
                { 
                    if (controlDampeners) 
                    { 
                        DeadMan();
                        LCDDEADMAN.BackgroundColor = new Color(0, 255, 255); 
                    }
                    else { LCDDEADMAN.BackgroundColor = new Color(0, 0, 0); }
                    if (idleThrusters) { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255); }
                    else { LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 0, 0); }

                    tickCount = 0; 
                } 
                tickCount++;
            }
            //if (writeCount == writeDelay)  { DEBUG.WriteText(debugLog); writeCount = 0; } writeCount++;
        }

        void InitMagneticDrive()
        {
            foreach (IMyMotorStator block in ROTORS) { block.Enabled = true; }
            foreach (IMyMotorStator block in ROTORSINV) { block.Enabled = true; }
            if (MANAGERPB != null)
            {
                if (MANAGERPB.CustomData.Contains("SunChaser=true"))
                {
                    sunChaseOff = MANAGERPB.TryRun(argSunchaseOff);
                }
            }
            foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void IdleMagneticDrive()
        {
            SetPow(Vector3D.Zero);
            foreach (IMyMotorStator block in ROTORS)
            {
                block.TargetVelocityRPM = 0;
                block.Enabled = false;
            }
            foreach (IMyMotorStator block in ROTORSINV)
            {
                block.TargetVelocityRPM = 0;
                block.Enabled = false;
            }
            if (idleThrusters) 
            { 
                foreach (IMyThrust thrust in THRUSTERS) { thrust.Enabled = false; }
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argIdleThrusters: 
                    idleThrusters = !idleThrusters;
                    if (idleThrusters) {
                        foreach(IMyThrust thrust in THRUSTERS) { thrust.Enabled = false; }
                        LCDIDLETHRUSTERS.BackgroundColor = new Color(0, 255, 255);
                    } else {
                        foreach(IMyThrust thrust in THRUSTERS) { thrust.Enabled = true; }
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
                case argMagneticDrive:
                    magneticDrive = !magneticDrive;
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
                    }
                }
            }
            bool controlled;
            if (CONTROLLER == null)
            {
                controlled = false;

                IMyShipController controller = null;
                foreach (IMyShipController contr in CONTROLLERS)
                {
                    if (contr.IsFunctional && contr.CanControlShip && contr.ControlThrusters && !(contr is IMyRemoteControl))
                    {
                        controller = contr;
                    }
                }
                if (REMOTE.IsAutoPilotEnabled)
                {
                    CONTROLLER = controller;
                    controlled = true;
                }
                else
                {
                    Vector3D velocityVec = controller.GetShipVelocities().LinearVelocity;
                    double speed = velocityVec.Length();
                    if (speed > 1)
                    {
                        CONTROLLER = controller;
                        controlled = true;
                    }
                }
            }
            else
            {
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
            if (useGyrosToStabilize)
            {
                if (!hasVector)
                {
                    hasVector = true;
                    lastForwardVector = reference.WorldMatrix.Forward;
                    lastUpVector = reference.WorldMatrix.Up;
                }

                double pitchAngle, yawAngle, rollAngle;
                if (!useRoll) { lastUpVector = Vector3D.Zero; };
                GetRotationAnglesSimultaneous(lastForwardVector, lastUpVector, reference.WorldMatrix, out pitchAngle, out yawAngle, out rollAngle);

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
                        //block.GyroPower = 100f;//im assuming this is a percentage
                    }
                    else
                    {
                        block.GyroOverride = false;
                    }
                }

                lastForwardVector = reference.WorldMatrix.Forward;
                if (Vector3D.IsZero(CONTROLLER.GetNaturalGravity())) { lastUpVector = CONTROLLER.WorldMatrix.Up; }
                else { lastUpVector = -CONTROLLER.GetNaturalGravity(); }
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
                    if (speed > deadManMinSpeed) {
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
            MANAGERPB = GridTerminalSystem.GetBlockWithName(managerName) as IMyProgrammableBlock;
            LCDDEADMAN = GridTerminalSystem.GetBlockWithName(deadManPanelName) as IMyTextPanel;
            LCDIDLETHRUSTERS = GridTerminalSystem.GetBlockWithName(idleThrusterPanelName) as IMyTextPanel;
            //DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
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
