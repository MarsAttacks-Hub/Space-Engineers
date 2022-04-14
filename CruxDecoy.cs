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

        readonly string ProjectorsDecoyName = "Decoy";
        readonly string ProjectorsBombName = "Bomb";
        readonly string GravGensName = "Decoy";
        readonly string MergesName = "Drop";
        readonly string WeldersName = "Drop";

        readonly string idL1 = "L1";
        readonly string idR1 = "R1";

        const string argSetup = "Setup";
        const string argToggle = "Toggle";
        const string argSwitch = "Switch";

        int selectedDrop = 0;   //0 decoys - 1 bombs

        public List<IMyProjector> PROJECTORSDECOY = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSBOMB = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyShipMergeBlock> MERGES = new List<IMyShipMergeBlock>();
        public List<IMyGravityGenerator> GRAVGENS = new List<IMyGravityGenerator>();
        public List<IMyShipWelder> WELDERS = new List<IMyShipWelder>();

        bool toggle = false;

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup()
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

        void Main(string argument)
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

            if (toggle)
            {
                foreach (IMyProjector block in TEMPPROJECTORS)
                {
                    if (block.RemainingBlocks == 0)
                    {
                        foreach (IMyShipMergeBlock merge in MERGES)
                        {
                            if (block.CustomName.Contains(idL1) && merge.CustomName.Contains(idL1) ||
                                block.CustomName.Contains(idR1) && merge.CustomName.Contains(idR1))
                            {
                                merge.Enabled = false;
                            }
                        }
                    }
                    else
                    {
                        foreach (IMyShipMergeBlock merge in MERGES)
                        {
                            if (block.CustomName.Contains(idL1) && merge.CustomName.Contains(idL1) ||
                                block.CustomName.Contains(idR1) && merge.CustomName.Contains(idR1))
                            {
                                merge.Enabled = true;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (IMyGravityGenerator block in GRAVGENS)
                {
                    block.Enabled = false;
                }
                foreach (IMyShipMergeBlock merge in MERGES)
                {
                    merge.Enabled = true;
                }
                foreach (IMyShipWelder block in WELDERS)
                {
                    block.Enabled = false;
                }
                foreach (IMyProjector block in TEMPPROJECTORS)
                {
                    block.Enabled = false;
                }
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argSetup: Setup(); break;
                case argToggle:
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    if (toggle)
                    {
                        toggle = false;
                    }
                    else
                    {
                        toggle = true;
                        foreach (IMyProjector block in TEMPPROJECTORS)
                        {
                            block.Enabled = true;
                        }
                        if (selectedDrop == 0)
                        {
                            foreach (IMyGravityGenerator block in GRAVGENS)
                            {
                                block.Enabled = true;
                            }
                        }
                        foreach (IMyShipWelder block in WELDERS)
                        {
                            block.Enabled = true;
                        }
                    }
                    break;
                case argSwitch:
                    if (selectedDrop == 1)
                    {
                        selectedDrop = 0;
                        TEMPPROJECTORS = PROJECTORSDECOY;
                        foreach (IMyProjector block in PROJECTORSBOMB)
                        {
                            block.Enabled = false;
                        }
                    }
                    else if (selectedDrop == 0)
                    {
                        selectedDrop = 1;
                        TEMPPROJECTORS = PROJECTORSBOMB;
                        foreach (IMyProjector block in PROJECTORSDECOY)
                        {
                            block.Enabled = false;
                        }
                    }
                    break;
            }
        }

        void GetBlocks()
        {
            PROJECTORSDECOY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDECOY, block => block.CustomName.Contains(ProjectorsDecoyName));
            PROJECTORSBOMB.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSBOMB, block => block.CustomName.Contains(ProjectorsBombName));
            MERGES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES, block => block.CustomName.Contains(MergesName));
            GRAVGENS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(GRAVGENS, block => block.CustomName.Contains(GravGensName));
            WELDERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERS, block => block.CustomName.Contains(WeldersName));
        }

    }
}
