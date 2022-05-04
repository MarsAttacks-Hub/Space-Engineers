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
        //DECOY

        readonly string projectorsDecoyName = "Decoy";
        readonly string projectorsBombName = "Bomb";
        readonly string gravGensName = "Decoy";
        readonly string mergesName = "Drop";
        readonly string weldersName = "Drop";
        readonly string warHeadsName = "Decoy";
        readonly string debugPanelName = "[CRX] Debug";

        const string argToggle = "Toggle";
        const string argSwitch = "Switch";
        const string argLaunchOne = "Launch";

        readonly int launchDelay = 100;

        int selectedDrop = 0;//0 decoys - 1 bombs
        bool toggle = false;
        bool launchOnce = false;
        bool ready = false;
        int launchTick = 0;

        public List<IMyProjector> PROJECTORSDECOY = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSBOMB = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyShipMergeBlock> MERGES = new List<IMyShipMergeBlock>();
        public List<IMyGravityGenerator> GRAVGENS = new List<IMyGravityGenerator>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();

        Program()
        {
            GetBlocks();
            if (selectedDrop == 0)
            {
                TEMPPROJECTORS = PROJECTORSDECOY;
            }
            else if (selectedDrop == 1)
            {
                TEMPPROJECTORS = PROJECTORSBOMB;
            }
        }

        public void Main(string argument)
        {
            try
            {
                Echo($"PROJECTORSDECOY:{PROJECTORSDECOY.Count}");
                Echo($"PROJECTORSBOMB:{PROJECTORSBOMB.Count}");
                Echo($"MERGES:{MERGES.Count}");
                Echo($"GRAVGENS:{GRAVGENS.Count}");
                Echo($"WELDERS:{WELDERS.Count}");

                if (selectedDrop == 0)
                {
                    Echo("Selected Drop: Decoy");
                }
                else if (selectedDrop == 1)
                {
                    Echo("Selected Drop: Bombs");
                }

                if (!string.IsNullOrEmpty(argument))
                {
                    ProcessArgument(argument);
                }

                if (launchOnce)
                {
                    if (launchTick == 0)
                    {
                        foreach (IMyProjector block in TEMPPROJECTORS) { block.Enabled = true; }
                        foreach (IMyShipWelder block in WELDERS) { block.Enabled = true; }

                        launchTick++;
                    }

                    if (!ready)
                    {
                        ready = CheckProjectors();
                    }
                    else
                    {
                        if (launchTick == 1)
                        {
                            if (selectedDrop == 0)
                            {
                                foreach (IMyGravityGenerator block in GRAVGENS) { block.Enabled = true; }
                            }
                            else if (selectedDrop == 1)
                            {
                                List<IMyWarhead> warHeads = new List<IMyWarhead>();
                                GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warHeads, block => block.CustomName.Contains(warHeadsName));
                                foreach (IMyWarhead war in warHeads) { war.IsArmed = true; }
                            }
                            foreach (IMyShipMergeBlock merge in MERGES) { merge.Enabled = false; }
                        }
                        else if (launchTick == launchDelay)
                        {
                            foreach (IMyGravityGenerator block in GRAVGENS) { block.Enabled = false; }
                            foreach (IMyShipMergeBlock merge in MERGES) { merge.Enabled = true; }
                            if (toggle)
                            {
                                Runtime.UpdateFrequency = UpdateFrequency.None;
                                launchOnce = false;
                            }
                            launchTick = 0;
                            ready = false;
                            return;
                        }

                        launchTick++;
                    }
                }
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
                case argToggle:
                    toggle = !toggle;
                    break;
                case argSwitch:
                    if (!launchOnce && launchTick == 0)
                    {
                        if (selectedDrop == 1)
                        {
                            selectedDrop = 0;
                            TEMPPROJECTORS = PROJECTORSDECOY;
                            foreach (IMyProjector block in PROJECTORSBOMB) { block.Enabled = false; }
                        }
                        else if (selectedDrop == 0)
                        {
                            selectedDrop = 1;
                            TEMPPROJECTORS = PROJECTORSBOMB;
                            foreach (IMyProjector block in PROJECTORSDECOY) { block.Enabled = false; }
                        }
                    }
                    break;
                case argLaunchOne:
                    if (!launchOnce && launchTick == 0)
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        launchOnce = true;
                        launchTick = 0;
                    }
                    break;
            }
        }

        bool CheckProjectors()
        {
            bool completed = false;
            int blocksCount = 0;
            foreach (IMyProjector block in TEMPPROJECTORS)
            {
                blocksCount += block.RemainingBlocks;//BuildableBlocksCount;
            }
            if (blocksCount == 0)
            {
                foreach (IMyGravityGenerator block in GRAVGENS) { block.Enabled = false; }
                foreach (IMyShipWelder block in WELDERS) { block.Enabled = false; }
                completed = true;
            }
            return completed;
        }

        void GetBlocks()
        {
            PROJECTORSDECOY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDECOY, block => block.CustomName.Contains(projectorsDecoyName));
            PROJECTORSBOMB.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSBOMB, block => block.CustomName.Contains(projectorsBombName));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, block => block.CustomName.Contains(mergesName));
            GRAVGENS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(GRAVGENS, block => block.CustomName.Contains(gravGensName));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, block => block.CustomName.Contains(weldersName));
        }

    }
}
