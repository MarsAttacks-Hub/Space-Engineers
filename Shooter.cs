﻿using Sandbox.Game.EntityComponents;
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

        //SHOOTER
        int launchDelay = 25;
        int selectedDrop = 0;//0 decoys - 1 bombs
        bool toggleDecoy = true;
        bool launchOnce = false;
        bool readyDecoy = false;
        int launchTick = 0;
        int buildTick = 0;
        int fireTick = 0;
        bool firing = false;
        bool readyJolt = false;
        bool toggleJolt = true;
        bool build1 = false;
        bool build2 = false;
        bool build3 = false;

        public List<IMyProjector> PROJECTORSDECOY = new List<IMyProjector>();
        public List<IMyProjector> PROJECTORSBOMB = new List<IMyProjector>();
        public List<IMyProjector> TEMPPROJECTORS = new List<IMyProjector>();
        public List<IMyShipMergeBlock> MERGESDECOY = new List<IMyShipMergeBlock>();
        public List<IMyGravityGenerator> GRAVGENS = new List<IMyGravityGenerator>();
        public List<IMyShipWelder> WELDERSDECOY = new List<IMyShipWelder>();
        public List<IMyMotorBase> HINGESDETACH = new List<IMyMotorBase>();
        public List<IMyMotorBase> HINGESFRONT = new List<IMyMotorBase>();
        public List<IMyMotorBase> HINGESJOLT = new List<IMyMotorBase>();
        public List<IMyExtendedPistonBase> PISTONSDOUBLEOUTER = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSDOUBLEINNER = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSFRONT = new List<IMyExtendedPistonBase>();
        public List<IMyExtendedPistonBase> PISTONSJOLT = new List<IMyExtendedPistonBase>();
        public List<IMyProjector> PROJECTORS = new List<IMyProjector>();
        public List<IMyShipWelder> WELDERSJOLT = new List<IMyShipWelder>();
        public List<IMyWarhead> WARHEADS = new List<IMyWarhead>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyShipMergeBlock> MERGESJOLT = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGES1 = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGES2 = new List<IMyShipMergeBlock>();
        public List<IMyShipMergeBlock> MERGES3 = new List<IMyShipMergeBlock>();
        public List<IMySensorBlock> JOLTSENSORS = new List<IMySensorBlock>();

        public List<MyDetectedEntityInfo> detectedEntities = new List<MyDetectedEntityInfo>();

        Program() {
            Setup();
        }

        void Setup() {
            GetBlocks();
            foreach (IMySensorBlock block in JOLTSENSORS) { block.Enabled = true; }
            if (selectedDrop == 0) {
                TEMPPROJECTORS = PROJECTORSDECOY;
                foreach (IMyProjector block in PROJECTORSDECOY) { block.Enabled = true; }
                foreach (IMyProjector block in PROJECTORSBOMB) { block.Enabled = false; }
            } else if (selectedDrop == 1) {
                TEMPPROJECTORS = PROJECTORSBOMB;
                foreach (IMyProjector block in PROJECTORSDECOY) { block.Enabled = false; }
                foreach (IMyProjector block in PROJECTORSBOMB) { block.Enabled = true; }
            }
        }

        public void Main(string arg, UpdateType updateSource) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (arg == "LaunchDecoy" || arg == "FireJolt") { return; }
                }

                if (updateSource == UpdateType.Update1) { launchDelay = 250; } else { launchDelay = 25; }

                if (launchOnce) {
                    LaunchDecoy();
                }

                if (firing) {
                    detectedEntities.Clear();
                    foreach (IMySensorBlock block in JOLTSENSORS) {
                        block.DetectedEntities(detectedEntities);
                    }
                    if (detectedEntities.Count == 0) {
                        FireJolt();
                    }
                }

                if (build1) {
                    Build_1();
                } else if (build2) {
                    Build_2();
                } else if (build3) {
                    Build_3();
                }

                if (!launchOnce && !firing && !build1 && !build2 && !build3) {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                Setup();
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case "ToggleDecoy":
                    toggleDecoy = !toggleDecoy;
                    break;
                case "Switch":
                    if (!launchOnce && launchTick == 0) {
                        if (selectedDrop == 1) {
                            selectedDrop = 0;
                            TEMPPROJECTORS = PROJECTORSDECOY;
                            foreach (IMyProjector block in PROJECTORSDECOY) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSBOMB) { block.Enabled = false; }
                        } else if (selectedDrop == 0) {
                            selectedDrop = 1;
                            TEMPPROJECTORS = PROJECTORSBOMB;
                            foreach (IMyProjector block in PROJECTORSBOMB) { block.Enabled = true; }
                            foreach (IMyProjector block in PROJECTORSDECOY) { block.Enabled = false; }
                        }
                    }
                    break;
                case "LaunchDecoy":
                    if (!launchOnce && launchTick == 0) {
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        launchOnce = true;
                        launchTick = 0;
                    }
                    break;
                case "ToggleJolt":
                    toggleJolt = !toggleJolt;
                    break;
                case "FireJolt":
                    if (!firing && fireTick == 0) {
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        firing = true;
                        fireTick = 0;
                    }
                    break;
                case "1":
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    buildTick = 0;
                    build1 = true;
                    break;
                case "2":
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    buildTick = 0;
                    build2 = true;
                    break;
                case "3":
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    buildTick = 0;
                    build3 = true;
                    break;
            }
        }

        void LaunchDecoy() {
            if (launchTick == 0) {
                SendBroadcastMessage(false);
                foreach (IMyShipWelder block in WELDERSDECOY) { block.Enabled = true; }
                launchTick++;
            }

            if (!readyDecoy) {
                readyDecoy = CheckBuildable(TEMPPROJECTORS, WELDERSDECOY);
            }
            if (readyDecoy) {
                if (launchTick == 1) {
                    if (selectedDrop == 0) {
                        foreach (IMyGravityGenerator block in GRAVGENS) { block.Enabled = true; }
                    } else if (selectedDrop == 1) {
                        List<IMyWarhead> warHeads = new List<IMyWarhead>();
                        GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warHeads, block => block.CustomName.Contains("Decoy"));
                        foreach (IMyWarhead war in warHeads) { war.IsArmed = true; }
                    }
                    foreach (IMyShipMergeBlock merge in MERGESDECOY) { merge.Enabled = false; }
                } else if (launchTick == launchDelay) {
                    foreach (IMyGravityGenerator block in GRAVGENS) { block.Enabled = false; }
                    foreach (IMyShipMergeBlock merge in MERGESDECOY) { merge.Enabled = true; }
                    if (selectedDrop == 1) {
                        if (toggleDecoy) {
                            launchOnce = false;
                        }
                        SendBroadcastMessage(true);
                        readyDecoy = false;
                        launchTick = -1;
                    }
                } else if (launchTick > launchDelay) {
                    foreach (IMyShipWelder block in WELDERSDECOY) { block.Enabled = true; }
                    bool ready = CheckBuildable(TEMPPROJECTORS, WELDERSDECOY);
                    if (ready) {
                        foreach (IMyShipWelder block in WELDERSDECOY) { block.Enabled = false; }
                        if (toggleDecoy) {
                            launchOnce = false;
                        }
                        SendBroadcastMessage(true);
                        readyDecoy = false;
                        launchTick = -1;
                    }
                }
                if (launchTick <= launchDelay + 1) {
                    launchTick++;
                }
            }
        }

        void FireJolt() {
            if (fireTick == 0) {
                SendBroadcastMessage(false);
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                foreach (IMyShipWelder block in WELDERSJOLT) { block.Enabled = true; }
                fireTick++;
            }

            if (!readyJolt) {
                readyJolt = CheckProjectors(PROJECTORS, WELDERSJOLT);
            }
            if (readyJolt) {
                if (fireTick == 1) {
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;

                    GetJoltAmmoBlocks();

                    foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Attach(); }
                } else if (fireTick == 2) {
                    foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Attach(); }
                } else if (fireTick == 3) {
                    foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Retract(); }
                    foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }
                } else if (fireTick == 120) {
                    foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Detach(); }
                    foreach (IMyWarhead warhead in WARHEADS) { warhead.IsArmed = true; }
                    foreach (IMyBatteryBlock battery in BATTERIES) { battery.Enabled = true; }
                } else if (fireTick == 121) {
                    foreach (IMyShipMergeBlock merge in MERGESJOLT) { merge.Enabled = false; }
                } else if (fireTick == 140) {
                    foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Extend(); }
                    foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Detach(); }
                } else if (fireTick == 200) {
                    foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }
                } else if (fireTick >= 260) {
                    if (toggleJolt) {
                        firing = false;
                    }
                    SendBroadcastMessage(true);
                    readyJolt = false;
                    fireTick = -1;
                }
                if (fireTick < 261) {
                    fireTick++;
                }
            }
        }

        void SendBroadcastMessage(bool readyToFire) {
            string variable = "readyToFire";
            MyTuple<string, bool> tuple = MyTuple.Create(variable, readyToFire);
            IGC.SendBroadcastMessage("[PAINTER]", tuple, TransmissionDistance.ConnectedConstructs);
            IGC.SendBroadcastMessage("[MULTI]", tuple, TransmissionDistance.ConnectedConstructs);

            MyTuple<int, bool, bool, bool, bool> aTuple = MyTuple.Create(selectedDrop, readyJolt, !toggleJolt, readyDecoy, !toggleDecoy);
            IGC.SendBroadcastMessage("[LOGGER]", aTuple, TransmissionDistance.ConnectedConstructs);
        }

        bool CheckProjectors(List<IMyProjector> projectors, List<IMyShipWelder> welders) {
            bool completed = false;
            int blocksCount = 0;
            foreach (IMyProjector block in projectors) {
                blocksCount += block.RemainingBlocks;
            }
            if (blocksCount == 0) {
                foreach (IMyShipWelder block in welders) { block.Enabled = false; }
                completed = true;
            }
            return completed;
        }

        bool CheckBuildable(List<IMyProjector> projectors, List<IMyShipWelder> welders) {
            bool completed = false;
            int blocksCount = 0;
            foreach (IMyProjector block in projectors) {
                blocksCount += block.BuildableBlocksCount;
            }
            if (blocksCount == 0) {
                foreach (IMyShipWelder block in welders) { block.Enabled = false; }
                completed = true;
            }
            return completed;
        }

        void GetJoltAmmoBlocks() {
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, block => block.CustomName.Contains("Ammo Jolt"));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, battery => battery.CustomName.Contains("Ammo Jolt"));
            MERGESJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESJOLT, battery => battery.CustomName.Contains("Ammo Jolt"));
        }

        void GetBlocks() {
            PROJECTORSDECOY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSDECOY, block => block.CustomName.Contains("Decoy"));
            PROJECTORSBOMB.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORSBOMB, block => block.CustomName.Contains("Bomb"));
            MERGESDECOY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESDECOY, block => block.CustomName.Contains("Drop"));
            GRAVGENS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(GRAVGENS, block => block.CustomName.Contains("Decoy"));
            WELDERSDECOY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERSDECOY, block => block.CustomName.Contains("Drop"));
            HINGESDETACH.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESDETACH, block => block.CustomName.Contains("Detach Jolt"));
            HINGESFRONT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESFRONT, block => block.CustomName.Contains("Front Jolt"));
            HINGESJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyMotorBase>(HINGESJOLT, block => block.CustomName.Contains("Jolt"));
            PISTONSDOUBLEOUTER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSDOUBLEOUTER, block => block.CustomName.Contains("Double Outer"));
            PISTONSDOUBLEINNER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSDOUBLEINNER, block => block.CustomName.Contains("Double Inner"));
            PISTONSFRONT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSFRONT, block => block.CustomName.Contains("Front Jolt"));
            PISTONSJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(PISTONSJOLT, block => block.CustomName.Contains("Jolt"));
            PROJECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(PROJECTORS, block => block.CustomName.Contains("Jolt"));
            WELDERSJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(WELDERSJOLT, block => block.CustomName.Contains("Jolt"));
            WARHEADS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(WARHEADS, block => block.CustomName.Contains("Ammo Jolt"));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, block => block.CustomName.Contains("Ammo Jolt"));
            MERGESJOLT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGESJOLT, block => block.CustomName.Contains("Ammo Jolt"));
            MERGES1.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES1, block => block.CustomName.Contains("1 Jolt"));
            MERGES2.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES2, block => block.CustomName.Contains("2 Jolt"));
            MERGES3.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(MERGES3, block => block.CustomName.Contains("3 Jolt"));
            JOLTSENSORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(JOLTSENSORS, block => block.CustomName.Contains("[CRX] Sensor Jolt"));
        }

        void Build_1() {
            if (buildTick == 0) {
                foreach (IMyExtendedPistonBase piston in PISTONSJOLT) { piston.Extend(); }
            } else if (buildTick == 10) {
                foreach (IMyShipMergeBlock merge in MERGES1) { merge.Enabled = false; }
            } else if (buildTick == 11) {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) { piston.Attach(); }
            } else if (buildTick == 20) {
                foreach (IMyShipMergeBlock merge in MERGES2) { merge.Enabled = false; }
            } else if (buildTick == 21) {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.Attach(); }

                build1 = false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            buildTick++;
        }

        void Build_2() {
            if (buildTick == 0) {
                foreach (IMyShipMergeBlock merge in MERGES3) { merge.Enabled = false; }
            } else if (buildTick == 1) {
                foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Attach(); }
            } else if (buildTick == 5) {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.MaxLimit = 7.34f; }

                build2 = false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            buildTick++;
        }

        void Build_3() {
            if (buildTick == 0) {
                foreach (IMyMotorBase hinge in HINGESFRONT) { hinge.Attach(); }
            } else if (buildTick == 3) {
                foreach (IMyMotorBase hinge in HINGESDETACH) { hinge.Detach(); }
            } else if (buildTick == 4) {
                foreach (IMyExtendedPistonBase piston in PISTONSFRONT) { piston.MaxLimit = 9.7f; }
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) {
                    piston.Retract();
                    piston.MaxLimit = 2.34f;
                }
            } else if (buildTick == 10) {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) {
                    piston.Extend();
                    piston.MinLimit = 0f;
                }
            } else if (buildTick == 16) {
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEINNER) { piston.Velocity = 1.25f; }
                foreach (IMyExtendedPistonBase piston in PISTONSDOUBLEOUTER) { piston.Velocity = 1.25f; }
                foreach (IMyMotorBase hinge in HINGESJOLT) { hinge.ApplyAction("ShareInertiaTensor"); }

                build3 = false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            buildTick++;
        }

    }
}
