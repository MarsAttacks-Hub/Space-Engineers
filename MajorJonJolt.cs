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
        //JOLT

        readonly string joltName = "Jolt";
        readonly string hingeDetachName = "Detach Jolt";
        readonly string hingeFrontName = "Front Jolt";
        readonly string pistonsDoubleOuterName = "Double Outer";
        readonly string pistonsDoubleInnerName = "Double Inner";
        readonly string pistonsFrontName = "Front Jolt";
        readonly string ammoName = "Ammo Jolt";
        readonly string merges1Name = "1 Jolt";
        readonly string merges2Name = "2 Jolt";
        readonly string merges3Name = "3 Jolt";

        const string argFire = "FireJolt";
        const string argToggle = "Toggle";

        int inittick = 0;
        int firetick = 0;
        bool firing = false;
        bool ready = false;
        bool toggle = true;

        public List<IMyMotorBase> HINGESDETACH = new List<IMyMotorBase>();
        public List<IMyMotorBase> HINGESFRONT = new List<IMyMotorBase>();
        public List<IMyMotorBase> HINGESJOLT = new List<IMyMotorBase>();

        public List<IMyExtendedPistonBase> PISTONSDOUBLEOUTER = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSDOUBLEINNER = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSFRONT = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSJOLT = new List<IMyExtendedPistonBase>();

        public List<IMyProjector> PROJECTORS = new List<IMyProjector>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();
        public List<IMyWarhead> WARHEADS = new List<IMyWarhead>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyShipMergeBlock> MERGES = new List<IMyShipMergeBlock>();

        public List<IMyShipMergeBlock> MERGES1 = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGES2 = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGES3 = new List<IMyShipMergeBlock>();

        Program()
        {
            HINGESDETACH.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESDETACH, block => block.CustomName.Contains(hingeDetachName));
            HINGESFRONT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESFRONT, block => block.CustomName.Contains(hingeFrontName));
            HINGESJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESJOLT, block => block.CustomName.Contains(joltName));

            PISTONSDOUBLEOUTER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSDOUBLEOUTER, block => block.CustomName.Contains(pistonsDoubleOuterName));
            PISTONSDOUBLEINNER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSDOUBLEINNER, block => block.CustomName.Contains(pistonsDoubleInnerName));
            PISTONSFRONT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSFRONT, block => block.CustomName.Contains(pistonsFrontName));
            PISTONSJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSJOLT, block => block.CustomName.Contains(joltName));

            PROJECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORS, block => block.CustomName.Contains(joltName));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, block => block.CustomName.Contains(joltName));
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, block => block.CustomName.Contains(ammoName));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, block => block.CustomName.Contains(ammoName));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, block => block.CustomName.Contains(ammoName));

            MERGES1.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES1, block => block.CustomName.Contains(merges1Name));
            MERGES2.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES2, block => block.CustomName.Contains(merges2Name));
            MERGES3.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES3, block => block.CustomName.Contains(merges3Name));
        }

        public void Main(string arg, UpdateType updateSource)
        {
            Echo($"HINGESDETACH:{HINGESDETACH.Count}");
            Echo($"HINGESFRONT:{HINGESFRONT.Count}");
            Echo($"HINGESJOLT:{HINGESJOLT.Count}");
            Echo($"PISTONSDOUBLEOUTER:{PISTONSDOUBLEOUTER.Count}");
            Echo($"PISTONSDOUBLEINNER:{PISTONSDOUBLEINNER.Count}");
            Echo($"PISTONSFRONT:{PISTONSFRONT.Count}");
            Echo($"PISTONSJOLT:{PISTONSJOLT.Count}");
            Echo($"PROJECTORS:{PROJECTORS.Count}");
            Echo($"WELDERS:{WELDERS.Count}");

            if (!String.IsNullOrEmpty(arg))
            {
                ProcessArgs(arg);
            }

            if (firing)
            {
                Fire();
            }

            else if (Me.CustomData == "0")
            {
                Init_1();
                inittick = 0;
                Me.CustomData = "1";
            }
            else if (updateSource == UpdateType.Update1 && Me.CustomData == "1") { Init_1(); }
            else if (Me.CustomData == "1")
            {
                Init_2();
                inittick = 0;
                Me.CustomData = "2";
            }
            else if (updateSource == UpdateType.Update1 && Me.CustomData == "2") { Init_2(); }
            else if (Me.CustomData == "2")
            {
                Init_3();
                inittick = 0;
                Me.CustomData = "3";
            }
            else if (updateSource == UpdateType.Update1 && Me.CustomData == "3") { Init_3(); }
        }

        public void Fire()
        {
            if (firetick == 0)
            {
                foreach (IMyProjector block in PROJECTORS) { block.Enabled = true; }
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = true; }

                firetick++;
            }

            if (!ready)
            {
                ready = CheckProjectors();
            }
            else
            {
                if (firetick == 1)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;

                    GetJoltAmmoBlocks();

                    foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Attach(); }
                }
                if (firetick == 2)
                {
                    foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Attach(); }
                }
                if (firetick == 3)
                {
                    foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Retract(); }
                    foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }
                }
                else if (firetick == 120)
                {
                    foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Detach(); }
                    foreach (IMyWarhead warhead in WARHEADS) { warhead.IsArmed = true; }
                    foreach (IMyBatteryBlock battery in BATTERIES) { battery.Enabled = true; }
                }
                else if (firetick == 121)
                {
                    foreach (IMyShipMergeBlock merge in MERGES) { merge.Enabled = false; }
                }
                else if (firetick == 140)
                {
                    foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Extend(); }
                    foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Detach(); }
                }
                else if (firetick == 200)
                {
                    foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }
                }
                else if (firetick == 260)
                {
                    if (toggle)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        firing = false;
                    }
                    ready = false;
                    firetick = 0;
                    return;
                }
                firetick++;
            }
        }


        void ProcessArgs(string arg)
        {
            switch (arg)
            {
                case argToggle:
                    toggle = !toggle;
                    break;
                case argFire:
                    if (!firing && firetick == 0)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        firing = true;
                        firetick = 0;
                    }
                    break;
            }
        }

        bool CheckProjectors()
        {
            bool completed = false;
            int blocksCount = 0;
            foreach (IMyProjector block in PROJECTORS)
            {
                blocksCount += block.BuildableBlocksCount;
            }
            if (blocksCount == 0)
            {
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = false; }
                foreach (IMyProjector block in PROJECTORS) { block.Enabled = false; }
                completed = true;
            }
            return completed;
        }

        void GetJoltAmmoBlocks()
        {
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, block => block.CustomName.Contains(ammoName));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, battery => battery.CustomName.Contains(ammoName));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, battery => battery.CustomName.Contains(ammoName));
        }

        public void Init_1()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            if (inittick == 0)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Extend(); }
            }
            else if (inittick == 100)
            {
                foreach (IMyShipMergeBlock merge in MERGES1) { merge.Enabled = false; }
            }
            else if (inittick == 101)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) { piston.Attach(); }
            }
            else if (inittick == 200)
            {
                foreach (IMyShipMergeBlock merge in MERGES2) { merge.Enabled = false; }
            }
            else if (inittick == 201)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.Attach(); }

                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            inittick++;
        }
        public void Init_2()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            if (inittick == 0)
            {
                foreach (IMyShipMergeBlock merge in MERGES3) { merge.Enabled = false; }
            }
            if (inittick == 1)
            {
                foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Attach(); }
            }
            if (inittick == 10)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.MaxLimit = 7.34f; }

                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            inittick++;
        }
        public void Init_3()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            if (inittick == 0)
            {
                foreach (IMyMotorBase hinge in HINGESFRONT) { hinge.Attach(); }
            }
            if (inittick == 30)
            {
                foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Detach(); }
            }
            if (inittick == 31)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.MaxLimit = 9.7f; }
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER)
                {
                    piston.Retract();
                    piston.MaxLimit = 2.34f;
                }
            }
            if (inittick == 100)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER)
                {
                    piston.Extend();
                    piston.MinLimit = 0f;
                }
            }
            if (inittick == 160)
            {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Velocity = 1.25f; }
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) { piston.Velocity = 1.25f; }
                foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }

                Me.CustomData = "";
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            inittick++;
        }

    }
}
