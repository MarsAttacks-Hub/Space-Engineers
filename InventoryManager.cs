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
using System.Collections.Immutable;

namespace IngameScript {
    partial class Program : MyGridProgram {

        //INVENTORY MANAGER
        readonly Dictionary<MyDefinitionId, MyTuple<string, double>> componentsDefBpQuota = new Dictionary<MyDefinitionId, MyTuple<string, double>>() {
            { MyItemType.MakeAmmo("Missile200mm"),              MyTuple.Create("Missile200mm",                  5d) },
            { MyItemType.MakeAmmo("NATO_25x184mm"),             MyTuple.Create("NATO_25x184mmMagazine",         5d) },
            { MyItemType.MakeAmmo("AutocannonClip"),            MyTuple.Create("AutocannonClip",                5d) },
            { MyItemType.MakeAmmo("LargeCalibreAmmo"),          MyTuple.Create("LargeCalibreAmmo",              5d) },
            { MyItemType.MakeAmmo("MediumCalibreAmmo"),         MyTuple.Create("MediumCalibreAmmo",             5d) },
            { MyItemType.MakeAmmo("LargeRailgunAmmo"),          MyTuple.Create("LargeRailgunAmmo",              5d) },
            { MyItemType.MakeAmmo("SmallRailgunAmmo"),          MyTuple.Create("SmallRailgunAmmo",              5d) },
            { MyItemType.MakeComponent("BulletproofGlass"),     MyTuple.Create("BulletproofGlass",              5d) },
            { MyItemType.MakeComponent("Canvas"),               MyTuple.Create("Canvas",                        5d) },
            { MyItemType.MakeComponent("Computer"),             MyTuple.Create("ComputerComponent",             5d) },
            { MyItemType.MakeComponent("Construction"),         MyTuple.Create("ConstructionComponent",         5d) },
            { MyItemType.MakeComponent("Detector"),             MyTuple.Create("DetectorComponent",             5d) },
            { MyItemType.MakeComponent("Display"),              MyTuple.Create("Display",                       5d) },
            { MyItemType.MakeComponent("Explosives"),           MyTuple.Create("ExplosivesComponent",           5d) },
            { MyItemType.MakeComponent("Girder"),               MyTuple.Create("GirderComponent",               5d) },
            { MyItemType.MakeComponent("GravityGenerator"),     MyTuple.Create("GravityGeneratorComponent",     5d) },
            { MyItemType.MakeComponent("InteriorPlate"),        MyTuple.Create("InteriorPlate",                 5d) },
            { MyItemType.MakeComponent("LargeTube"),            MyTuple.Create("LargeTube",                     5d) },
            { MyItemType.MakeComponent("Medical"),              MyTuple.Create("MedicalComponent",              5d) },
            { MyItemType.MakeComponent("MetalGrid"),            MyTuple.Create("MetalGrid",                     5d) },
            { MyItemType.MakeComponent("Motor"),                MyTuple.Create("MotorComponent",                5d) },
            { MyItemType.MakeComponent("PowerCell"),            MyTuple.Create("PowerCell",                     5d) },
            { MyItemType.MakeComponent("RadioCommunication"),   MyTuple.Create("RadioCommunicationComponent",   5d) },
            { MyItemType.MakeComponent("Reactor"),              MyTuple.Create("ReactorComponent",              5d) },
            { MyItemType.MakeComponent("SmallTube"),            MyTuple.Create("SmallTube",                     5d) },
            { MyItemType.MakeComponent("SolarCell"),            MyTuple.Create("SolarCell",                     5d) },
            { MyItemType.MakeComponent("SteelPlate"),           MyTuple.Create("SteelPlate",                    5d) },
            { MyItemType.MakeComponent("Superconductor"),       MyTuple.Create("Superconductor",                5d) },
            { MyItemType.MakeComponent("Thrust"),               MyTuple.Create("ThrustComponent",               5d) }
        };

        bool togglePB = true;//enable/disable PB
        bool logger = true;//enable/disable logging

        double cargoPercentage = 0;
        int ticks = 0;
        int productionTicks = 0;

        public List<IMyInventory> INVENTORIES = new List<IMyInventory>();
        public List<IMyCargoContainer> CONTAINERS = new List<IMyCargoContainer>();
        public List<IMyInventory> CARGOINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> CONNECTORSINVENTORIES = new List<IMyInventory>();
        public List<IMyRefinery> REFINERIES = new List<IMyRefinery>();
        public List<IMyInventory> REFINERIESINVENTORIES = new List<IMyInventory>();
        public List<IMyAssembler> ASSEMBLERS = new List<IMyAssembler>();
        public List<IMyInventory> ASSAULTTURRETSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> GATLINGSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> LAUNCHERSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> RAILGUNSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> SMALLRAILGUNSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> ARTILLERYINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> ASSAULTINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> AUTOCANNONSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> REACTORSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> GASINVENTORIES = new List<IMyInventory>();

        IMyTextPanel LCDINVENTORYMANAGER;

        public Dictionary<MyDefinitionId, double> oreDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeOre("Cobalt"),0d},
            {MyItemType.MakeOre("Gold"),0d},
            {MyItemType.MakeOre("Ice"),0d},
            {MyItemType.MakeOre("Iron"),0d},
            {MyItemType.MakeOre("Magnesium"),0d},
            {MyItemType.MakeOre("Nickel"),0d},
            {MyItemType.MakeOre("Organic"),0d},
            {MyItemType.MakeOre("Platinum"),0d},
            {MyItemType.MakeOre("Scrap"),0d},
            {MyItemType.MakeOre("Silicon"),0d},
            {MyItemType.MakeOre("Silver"),0d},
            {MyItemType.MakeOre("Stone"),0d},
            {MyItemType.MakeOre("Uranium"),0d}
        };

        public Dictionary<MyDefinitionId, double> refineryOreDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Gold"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d},
            {MyItemType.MakeOre("Platinum"),0d}, {MyItemType.MakeOre("Scrap"),0d}, {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Silver"),0d}, {MyItemType.MakeOre("Stone"),0d},
            {MyItemType.MakeOre("Uranium"),0d}
        };

        public Dictionary<MyDefinitionId, double> baseRefineryOreDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d}, {MyItemType.MakeOre("Scrap"),0d},
            {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Stone"),0d},
        };

        public Dictionary<MyDefinitionId, double> ingotsDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeIngot("Cobalt"),0d}, {MyItemType.MakeIngot("Gold"),0d}, {MyItemType.MakeIngot("Stone"),0d}, {MyItemType.MakeIngot("Iron"),0d}, {MyItemType.MakeIngot("Magnesium"),0d},
            {MyItemType.MakeIngot("Nickel"),0d}, {MyItemType.MakeIngot("Scrap"),0d}, {MyItemType.MakeIngot("Platinum"),0d}, {MyItemType.MakeIngot("Silicon"),0d}, {MyItemType.MakeIngot("Silver"),0d},
            {MyItemType.MakeIngot("Uranium"),0d}
        };

        public Dictionary<MyDefinitionId, double> componentsDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeComponent("BulletproofGlass"),0d}, {MyItemType.MakeComponent("Canvas"),0d}, {MyItemType.MakeComponent("Computer"),0d}, {MyItemType.MakeComponent("Construction"),0d},
            {MyItemType.MakeComponent("Detector"),0d}, {MyItemType.MakeComponent("Display"),0d}, {MyItemType.MakeComponent("Explosives"),0d}, {MyItemType.MakeComponent("Girder"),0d},
            {MyItemType.MakeComponent("GravityGenerator"),0d}, {MyItemType.MakeComponent("InteriorPlate"),0d}, {MyItemType.MakeComponent("LargeTube"),0d}, {MyItemType.MakeComponent("Medical"),0d},
            {MyItemType.MakeComponent("MetalGrid"),0d}, {MyItemType.MakeComponent("Motor"),0d}, {MyItemType.MakeComponent("PowerCell"),0d}, {MyItemType.MakeComponent("RadioCommunication"),0d},
            {MyItemType.MakeComponent("Reactor"),0d}, {MyItemType.MakeComponent("SmallTube"),0d}, {MyItemType.MakeComponent("SolarCell"),0d}, {MyItemType.MakeComponent("SteelPlate"),0d},
            {MyItemType.MakeComponent("Superconductor"),0d}, {MyItemType.MakeComponent("Thrust"),0d}, {MyItemType.MakeComponent("ZoneChip"),0d}
        };

        public Dictionary<MyDefinitionId, double> ammosDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
            {MyItemType.MakeAmmo("NATO_25x184mm"),0d},
            {MyItemType.MakeAmmo("AutocannonClip"),0d},
            {MyItemType.MakeAmmo("Missile200mm"),0d},
            {MyItemType.MakeAmmo("LargeCalibreAmmo"),0d},
            {MyItemType.MakeAmmo("MediumCalibreAmmo"),0d},
            {MyItemType.MakeAmmo("LargeRailgunAmmo"),0d},
            {MyItemType.MakeAmmo("SmallRailgunAmmo"),0d}
        };

        public Dictionary<MyDefinitionId, Dictionary<MyDefinitionId, double>> componentsPartsDict = new Dictionary<MyDefinitionId, Dictionary<MyDefinitionId, double>>() {
            { MyItemType.MakeAmmo("Missile200mm"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Magnesium"), 3d },
                    { MyItemType.MakeIngot("Platinum"), 0.04d },
                    { MyItemType.MakeIngot("Uranium"), 0.1d },
                    { MyItemType.MakeIngot("Silicon"), 0.2d },
                    { MyItemType.MakeIngot("Nickel"), 7d },
                    { MyItemType.MakeIngot("Iron"), 55d } } },
            { MyItemType.MakeAmmo("NATO_25x184mm"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Magnesium"), 3d },
                    { MyItemType.MakeIngot("Nickel"), 5d },
                    { MyItemType.MakeIngot("Iron"), 40d } } },
            { MyItemType.MakeAmmo("AutocannonClip"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 25d },
                    { MyItemType.MakeIngot("Nickel"), 3d },
                    { MyItemType.MakeIngot("Magnesium"), 2d } } },
            { MyItemType.MakeAmmo("LargeCalibreAmmo"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 60d },
                    { MyItemType.MakeIngot("Nickel"), 8d },
                    { MyItemType.MakeIngot("Magnesium"), 5d },
                    { MyItemType.MakeIngot("Uranium"), 0.1d } } },
            { MyItemType.MakeAmmo("MediumCalibreAmmo"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 15d },
                    { MyItemType.MakeIngot("Nickel"), 2d },
                    { MyItemType.MakeIngot("Magnesium"), 1.2d } } },
            { MyItemType.MakeAmmo("LargeRailgunAmmo"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 20d },
                    { MyItemType.MakeIngot("Nickel"), 3d },
                    { MyItemType.MakeIngot("Silicon"), 30d },
                    { MyItemType.MakeIngot("Uranium"), 1d } } },
            { MyItemType.MakeAmmo("SmallRailgunAmmo"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 4d },
                    { MyItemType.MakeIngot("Nickel"), 0.5d },
                    { MyItemType.MakeIngot("Silicon"), 5d },
                    { MyItemType.MakeIngot("Uranium"), 0.2d } } },
            { MyItemType.MakeComponent("BulletproofGlass"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Silicon"), 15d} } },
            { MyItemType.MakeComponent("Canvas"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Silicon"), 35d},
                    { MyItemType.MakeIngot("Iron"), 2d} } },
            { MyItemType.MakeComponent("Computer"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 0.5d},
                    { MyItemType.MakeIngot("Silicon"), 0.2d} } },
            { MyItemType.MakeComponent("Construction"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 8d} } },
            { MyItemType.MakeComponent("Detector"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 5d},
                    { MyItemType.MakeIngot("Nickel"), 15d} } },
            { MyItemType.MakeComponent("Display"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 1d},
                    { MyItemType.MakeIngot("Silicon"), 5d} } },
            { MyItemType.MakeComponent("Explosives"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Silicon"), 0.5d},
                    { MyItemType.MakeIngot("Magnesium"), 2d} } },
            { MyItemType.MakeComponent("Girder"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 6d} } },
            { MyItemType.MakeComponent("GravityGenerator"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Silver"), 5d },
                    { MyItemType.MakeIngot("Gold"), 10d },
                    { MyItemType.MakeIngot("Cobalt"), 220d },
                    { MyItemType.MakeIngot("Iron"), 600d } } },
            { MyItemType.MakeComponent("InteriorPlate"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 3d} } },
            { MyItemType.MakeComponent("LargeTube"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 30d} } },
            { MyItemType.MakeComponent("Medical"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 60d},
                    { MyItemType.MakeIngot("Nickel"), 70d },
                    { MyItemType.MakeIngot("Silver"), 20d } } },
            { MyItemType.MakeComponent("MetalGrid"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 12d},
                    { MyItemType.MakeIngot("Nickel"), 5d },
                    { MyItemType.MakeIngot("Cobalt"), 3d } } },
            { MyItemType.MakeComponent("Motor"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 20d},
                    { MyItemType.MakeIngot("Nickel"), 5d } } },
            { MyItemType.MakeComponent("PowerCell"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 10d},
                    { MyItemType.MakeIngot("Silicon"), 1d },
                    { MyItemType.MakeIngot("Nickel"), 2d } } },
            { MyItemType.MakeComponent("RadioCommunication"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 8d},
                    { MyItemType.MakeIngot("Silicon"), 1d } } },
            { MyItemType.MakeComponent("Reactor"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 15d},
                    { MyItemType.MakeIngot("Scrap"), 20d },
                    { MyItemType.MakeIngot("Silver"), 5d } } },
            { MyItemType.MakeComponent("SmallTube"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 5d} } },
            { MyItemType.MakeComponent("SolarCell"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Nickel"), 3d},
                    { MyItemType.MakeIngot("Silicon"), 6d } } },
            { MyItemType.MakeComponent("SteelPlate"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 21d} } },
            { MyItemType.MakeComponent("Superconductor"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 10d},
                    { MyItemType.MakeIngot("Gold"), 2d } } },
            { MyItemType.MakeComponent("Thrust"), new Dictionary<MyDefinitionId, double> {
                    { MyItemType.MakeIngot("Iron"), 30d},
                    { MyItemType.MakeIngot("Cobalt"), 10d },
                    { MyItemType.MakeIngot("Gold"), 1d },
                    { MyItemType.MakeIngot("Platinum"), 0.4d } } }
        };

        public Dictionary<MyDefinitionId, Dictionary<MyItemType, double>> ingotsPartsDict = new Dictionary<MyDefinitionId, Dictionary<MyItemType, double>>() {
            { MyItemType.MakeIngot("Cobalt"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Cobalt"), 3.4d } } },
            { MyItemType.MakeIngot("Gold"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Gold"), 100d } } },
            { MyItemType.MakeIngot("Iron"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Iron"), 1.43d } } },
            { MyItemType.MakeIngot("Magnesium"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Magnesium"), 142.9d } } },
            { MyItemType.MakeIngot("Nickel"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Nickel"), 2.5d } } },
            { MyItemType.MakeIngot("Platinum"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Platinum"), 200d } } },
            { MyItemType.MakeIngot("Silicon"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Silicon"), 1.5d } } },
            { MyItemType.MakeIngot("Silver"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Silver"), 10d } } },
            { MyItemType.MakeIngot("Stone"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Stone"), 71.5d } } },
            { MyItemType.MakeIngot("Uranium"), new Dictionary<MyItemType, double> { { MyItemType.MakeOre("Uranium"), 100d } } }
        };

        readonly Dictionary<MyDefinitionId, string> ingotsDefBp = new Dictionary<MyDefinitionId, string>() {
            { MyItemType.MakeIngot("Cobalt"), "CobaltOreToIngot" },
            { MyItemType.MakeIngot("Gold"), "GoldOreToIngot" },
            { MyItemType.MakeIngot("Stone"), "StoneOreToIngot" },//{ MyItemType.MakeIngot("Stone"), "StoneOreToIngot_Deconstruction" },
            { MyItemType.MakeIngot("Iron"), "IronOreToIngot" },
            { MyItemType.MakeIngot("Magnesium"), "MagnesiumOreToIngot" },
            { MyItemType.MakeIngot("Nickel"), "NickelOreToIngot" },
            { MyItemType.MakeIngot("Platinum"), "PlatinumOreToIngot" },
            { MyItemType.MakeIngot("Silicon"), "SiliconOreToIngot" },
            { MyItemType.MakeIngot("Silver"), "SilverOreToIngot" },
            { MyItemType.MakeIngot("Uranium"), "UraniumOreToIngot" },
        };

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            if (LCDINVENTORYMANAGER != null) { LCDINVENTORYMANAGER.BackgroundColor = togglePB ? new Color(0, 0, 50) : new Color(0, 0, 0); };
        }

        public void Main(string argument) {
            try {
                Echo($"ticks:{ticks}");
                Echo($"productionTicks:{productionTicks}");
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(argument)) {
                    ProcessArgument(argument);
                    if (!togglePB) { return; }
                }

                if (ticks == 1) {
                    MoveItemsIntoCargo(CONNECTORSINVENTORIES);
                } else if (ticks == 3) {
                    CompactInventory(INVENTORIES);
                } else if (ticks == 5) {
                    SortCargos();
                } else if (ticks == 7) {
                    ReadInventoryInfos();
                } else if (ticks >= 9) {
                    if (logger) {
                        SendBroadcastMessage();
                    }
                    ticks = -1;
                }
                ticks++;

                if (productionTicks == 2) {
                    AutoAssemblers();
                } else if (productionTicks == 4) {
                    AutoRefineries();
                } else if (productionTicks == 6) {
                    MoveProductionOutputsToMainInventory();
                } else if (productionTicks >= 100) {
                    productionTicks = -1;
                }
                productionTicks++;

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case "TogglePB":
                    togglePB = !togglePB;
                    if (togglePB) {
                        if (LCDINVENTORYMANAGER != null) { LCDINVENTORYMANAGER.BackgroundColor = new Color(0, 0, 50); };
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    } else {
                        if (LCDINVENTORYMANAGER != null) { LCDINVENTORYMANAGER.BackgroundColor = new Color(0, 0, 0); };
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    break;
                case "PBOn":
                    togglePB = true;
                    if (LCDINVENTORYMANAGER != null) { LCDINVENTORYMANAGER.BackgroundColor = new Color(0, 0, 50); };
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    break;
                case "PBOff":
                    togglePB = false;
                    if (LCDINVENTORYMANAGER != null) { LCDINVENTORYMANAGER.BackgroundColor = new Color(0, 0, 0); };
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    break;
                case "AutoRefineries":
                    AutoRefineries();
                    break;
                case "AutoAssemblers":
                    ReadAllItems(CARGOINVENTORIES);
                    AutoAssemblers();
                    break;
                case "CompactInventories":
                    CompactInventory(INVENTORIES);
                    break;
                case "SortCargos":
                    SortCargos();
                    break;
                case "MoveToCargo":
                    MoveProductionOutputsToMainInventory();
                    MoveItemsIntoCargo(CONNECTORSINVENTORIES);
                    break;
                case "WriteLog":
                    ReadInventoryInfos();
                    SendBroadcastMessage();
                    break;
                case "ToggleLogger":
                    logger = !logger;
                    break;
                case "LoggerOn":
                    logger = true;
                    break;
                case "LoggerOff":
                    logger = false;
                    break;
            }
        }

        void SendBroadcastMessage() {
            StringBuilder oresLog = new StringBuilder("");
            StringBuilder ingotsLog = new StringBuilder("");
            StringBuilder ammosLog = new StringBuilder("");
            StringBuilder componentsLog = new StringBuilder("");

            int count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ammosDict) {
                if (count == ammosDict.Count - 1) {
                    ammosLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    ammosLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in oreDict) {
                if (count == oreDict.Count - 1) {
                    oresLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    oresLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ingotsDict) {
                if (count == ingotsDict.Count - 1) {
                    ingotsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    ingotsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in componentsDict) {
                if (count == componentsDict.Count - 1) {
                    componentsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    componentsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            var immArray = ImmutableArray.CreateBuilder<MyTuple<
                double,
                string,
                string,
                string,
                string
                >>();

            var tuple = MyTuple.Create(
                cargoPercentage,
                ammosLog.ToString(),
                oresLog.ToString(),
                ingotsLog.ToString(),
                componentsLog.ToString()
                );
            immArray.Add(tuple);
            IGC.SendBroadcastMessage("[LOGGER]", immArray.ToImmutable(), TransmissionDistance.ConnectedConstructs);
        }

        void ReadInventoryInfos() {
            ReadInventoriesFillPercentage(CARGOINVENTORIES, out cargoPercentage);
            ReadAllItems(INVENTORIES);
        }

        void ReadAllItems(List<IMyInventory> inventories) {
            ResetComponentsDict();
            ResetIngotDict();
            ResetOreDict();
            ResetRefineryOreDict();
            ResetAmmosDict();
            foreach (IMyInventory inventory in inventories) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    if (item.Type.GetItemInfo().IsOre) {
                        double num;
                        if (oreDict.TryGetValue(item.Type, out num)) { oreDict[item.Type] = num + (double)item.Amount; }
                        if (refineryOreDict.TryGetValue(item.Type, out num)) { refineryOreDict[item.Type] = num + (double)item.Amount; }
                        if (baseRefineryOreDict.TryGetValue(item.Type, out num)) { baseRefineryOreDict[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsIngot) {
                        double num;
                        if (ingotsDict.TryGetValue(item.Type, out num)) { ingotsDict[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsComponent) {
                        double num;
                        if (componentsDict.TryGetValue(item.Type, out num)) { componentsDict[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsAmmo) {
                        double num;
                        if (ammosDict.TryGetValue(item.Type, out num)) { ammosDict[item.Type] = num + (double)item.Amount; }
                    }
                }
            }
        }

        void ReadInventoriesFillPercentage(List<IMyInventory> inventories, out double invPercent) {
            invPercent = 0d;
            foreach (IMyInventory inventory in inventories) {
                double inventoriesPercent = 0d;
                double currentVolume = (double)inventory.CurrentVolume;
                double maxVolume = (double)inventory.MaxVolume;
                if (maxVolume != 0d) {
                    inventoriesPercent = currentVolume / maxVolume * 100d;
                }
                invPercent += inventoriesPercent;
            }
        }

        void MoveProductionOutputsToMainInventory() {
            foreach (IMyRefinery block in REFINERIES) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.OutputInventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                        if (block.OutputInventory.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type)) {
                            block.OutputInventory.TransferItemTo(cargoInv, item);
                            break;
                        }
                    }
                }
            }
            foreach (IMyAssembler block in ASSEMBLERS) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.OutputInventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                        if (block.OutputInventory.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type)) {
                            block.OutputInventory.TransferItemTo(cargoInv, item);
                            break;
                        }
                    }
                }
            }
        }

        void CompactInventory(List<IMyInventory> inventories) {
            foreach (IMyInventory inventory in inventories) {
                for (int i = inventory.ItemCount - 1; i > 0; i--) { inventory.TransferItemTo(inventory, i, stackIfPossible: true); }
            }
        }

        void AutoAssemblers() {
            int clearQueue = 0;
            foreach (KeyValuePair<MyDefinitionId, MyTuple<string, double>> element in componentsDefBpQuota) {
                MyDefinitionId component = element.Key;
                string componentBp = element.Value.Item1;
                double componentQuota = element.Value.Item2;
                MyDefinitionId blueprintDef = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + componentBp);
                double cargoAmount = 0d;
                bool itemFound = componentsDict.TryGetValue(component, out cargoAmount);
                if (!itemFound) { itemFound = ammosDict.TryGetValue(component, out cargoAmount); }
                Dictionary<MyDefinitionId, double> ingotsNeeded = new Dictionary<MyDefinitionId, double>();
                bool ingotNeededFound = componentsPartsDict.TryGetValue(component, out ingotsNeeded);
                bool enoughIngots = false;
                foreach (KeyValuePair<MyDefinitionId, double> ingots in ingotsNeeded) {
                    double ingotsAvailable = 0d;
                    bool ingotsFound = ingotsDict.TryGetValue(ingots.Key, out ingotsAvailable);
                    if (ingotsFound) {
                        if (ingotsAvailable > ingots.Value) {
                            enoughIngots = true;
                        }
                    }
                }
                if (itemFound) {
                    if ((int)cargoAmount < (int)componentQuota && enoughIngots) {
                        foreach (IMyAssembler assembler in ASSEMBLERS) {
                            List<MyProductionItem> AssemblerQueue = new List<MyProductionItem>();
                            assembler.GetQueue(AssemblerQueue);
                            bool alreadyQueued = false;
                            foreach (MyProductionItem prodItem in AssemblerQueue) {
                                if (prodItem.BlueprintId == blueprintDef) {
                                    alreadyQueued = true;
                                    break;
                                }
                            }
                            if (!alreadyQueued) {
                                double amount = componentQuota - cargoAmount;
                                assembler.AddQueueItem(blueprintDef, amount);
                            }
                        }
                    } else {
                        foreach (IMyAssembler assembler in ASSEMBLERS) {
                            List<MyProductionItem> AssemblerQueue = new List<MyProductionItem>();
                            assembler.GetQueue(AssemblerQueue);
                            for (int i = 0; i < AssemblerQueue.Count; i++) {
                                if (AssemblerQueue[0].BlueprintId == blueprintDef) {
                                    assembler.RemoveQueueItem(i, AssemblerQueue[0].Amount);
                                }
                            }
                        }
                        clearQueue++;
                    }
                } else { clearQueue++; }
            }
            if (clearQueue == componentsDefBpQuota.Count) {
                foreach (IMyAssembler assembler in ASSEMBLERS) { assembler.ClearQueue(); }
            }
        }

        void AutoRefineries() {
            MoveItemsIntoCargo(REFINERIESINVENTORIES);
            ReadAllItems(CARGOINVENTORIES);
            MyDefinitionId blueprintDef = default(MyDefinitionId);
            MyDefinitionId ingotToQueue = default(MyDefinitionId);
            double ingotToQueueAmount = 100000d;
            bool unprintable = false;
            foreach (KeyValuePair<MyDefinitionId, double> availableIngots in ingotsDict) {
                if (availableIngots.Value < ingotToQueueAmount) {
                    Dictionary<MyItemType, double> neededOreDict;
                    bool ingotFound = ingotsPartsDict.TryGetValue(availableIngots.Key, out neededOreDict);
                    if (ingotFound) {
                        double availableOreAmount;
                        bool oreFound = refineryOreDict.TryGetValue(neededOreDict.First().Key, out availableOreAmount);
                        if (oreFound && neededOreDict.First().Value < availableOreAmount) {
                            string bpName;
                            bool ingotBpFound = ingotsDefBp.TryGetValue(availableIngots.Key, out bpName);
                            if (ingotBpFound) {
                                ingotToQueue = availableIngots.Key;
                                ingotToQueueAmount = availableIngots.Value;
                                blueprintDef = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + bpName);
                            }
                        }
                    }
                }
            }
            foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                cargoInv.GetItems(cargoItems, item => item.Type.TypeId == ingotToQueue.TypeId.ToString());
                foreach (MyInventoryItem item in cargoItems) {
                    foreach (IMyRefinery refinery in REFINERIES) {
                        if (refinery.CanUseBlueprint(blueprintDef)) {
                            List<IMyInventory> refineryInventories = new List<IMyInventory>();
                            refineryInventories.AddRange(REFINERIES.Select(block => block.InputInventory));
                            foreach (IMyInventory refInv in refineryInventories) {
                                if (cargoInv.CanTransferItemTo(refInv, item.Type) && refInv.CanItemsBeAdded(item.Amount, item.Type)) {
                                    cargoInv.TransferItemTo(refInv, item);
                                }
                            }
                        } else { unprintable = true; }
                    }
                }
            }
            if (unprintable) {
                blueprintDef = default(MyDefinitionId);
                ingotToQueue = default(MyItemType);
                ingotToQueueAmount = 100000d;
                foreach (KeyValuePair<MyDefinitionId, double> availableIngots in ingotsDict) {
                    if (availableIngots.Value < ingotToQueueAmount) {
                        Dictionary<MyItemType, double> neededOreDict;
                        bool ingotFound = ingotsPartsDict.TryGetValue(availableIngots.Key, out neededOreDict);
                        if (ingotFound) {
                            double availableOreAmount;
                            bool oreFound = baseRefineryOreDict.TryGetValue(neededOreDict.First().Key, out availableOreAmount);
                            if (oreFound && neededOreDict.First().Value < availableOreAmount) {
                                string bpName;
                                bool ingotBpFound = ingotsDefBp.TryGetValue(availableIngots.Key, out bpName);
                                if (ingotBpFound) {
                                    ingotToQueue = availableIngots.Key;
                                    ingotToQueueAmount = availableIngots.Value;
                                    blueprintDef = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + bpName);
                                }
                            }
                        }
                    }
                }
                foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                    List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                    cargoInv.GetItems(cargoItems, item => item.Type.TypeId == ingotToQueue.TypeId.ToString());
                    foreach (MyInventoryItem item in cargoItems) {
                        foreach (IMyRefinery refinery in REFINERIES) {
                            if (refinery.CanUseBlueprint(blueprintDef)) {
                                List<IMyInventory> refineryInventories = new List<IMyInventory>();
                                refineryInventories.AddRange(REFINERIES.Select(block => block.InputInventory));
                                foreach (IMyInventory refInv in refineryInventories) {
                                    if (cargoInv.CanTransferItemTo(refInv, item.Type) && refInv.CanItemsBeAdded(item.Amount, item.Type)) {
                                        cargoInv.TransferItemTo(refInv, item);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void SortCargos() {
            List<IMyCargoContainer> reversedCargo = CONTAINERS;
            reversedCargo.Reverse();
            foreach (IMyCargoContainer rCargo in reversedCargo) {
                IMyInventory rInventory = rCargo.GetInventory();
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                rInventory.GetItems(items);
                if (items.Count > 0) {
                    foreach (IMyCargoContainer cargo in CONTAINERS) {
                        if (cargo.EntityId != rCargo.EntityId) {
                            IMyInventory inventory = cargo.GetInventory();
                            foreach (MyInventoryItem item in items) {
                                if (rInventory.CanTransferItemTo(inventory, item.Type) && inventory.CanItemsBeAdded(item.Amount, item.Type)) {
                                    rInventory.TransferItemTo(inventory, item);
                                }
                            }
                        }
                    }
                }
            }
        }

        void MoveItemsIntoCargo(List<IMyInventory> inventories) {
            foreach (IMyInventory inv in inventories) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inv.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                        if (inv.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type)) {
                            inv.TransferItemTo(cargoInv, item);
                        }
                    }
                }
            }
        }

        int CompareCargoDistanceFromCenter(IMyCargoContainer firstCargo, IMyCargoContainer secondCargo) {
            double firstCargoDistance = Vector3D.DistanceSquared(firstCargo.GetPosition(), firstCargo.CubeGrid.WorldVolume.Center);
            double secondCargoDistance = Vector3D.DistanceSquared(secondCargo.GetPosition(), secondCargo.CubeGrid.WorldVolume.Center);
            return firstCargoDistance.CompareTo(secondCargoDistance);
        }

        void GetBlocks() {
            REFINERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(REFINERIES, block => block.CustomName.Contains("[CRX] Refinery"));
            REFINERIESINVENTORIES.Clear();
            REFINERIESINVENTORIES.AddRange(REFINERIES.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSEMBLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(ASSEMBLERS, block => block.CustomName.Contains("[CRX] Assembler"));
            List<IMyLargeTurretBase> turrets = new List<IMyLargeTurretBase>();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(turrets, block => block.CustomName.Contains("[CRX] Turret Assault"));
            ASSAULTTURRETSINVENTORIES.Clear();
            ASSAULTTURRETSINVENTORIES.AddRange(turrets.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            turrets.Clear();
            List<IMyUserControllableGun> guns = new List<IMyUserControllableGun>();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Gatling"));
            GATLINGSINVENTORIES.Clear();
            GATLINGSINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Rocket"));
            LAUNCHERSINVENTORIES.Clear();
            LAUNCHERSINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Railgun"));
            RAILGUNSINVENTORIES.Clear();
            RAILGUNSINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Small Railgun"));
            SMALLRAILGUNSINVENTORIES.Clear();
            SMALLRAILGUNSINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Artillery"));
            ARTILLERYINVENTORIES.Clear();
            ARTILLERYINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Autocannon"));
            AUTOCANNONSINVENTORIES.Clear();
            AUTOCANNONSINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(guns, block => block.CustomName.Contains("[CRX] Assault"));
            ASSAULTINVENTORIES.Clear();
            ASSAULTINVENTORIES.AddRange(guns.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            guns.Clear();
            CONTAINERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CONTAINERS, block => block.CustomName.Contains("[CRX] Cargo"));
            CONTAINERS.Sort(CompareCargoDistanceFromCenter);
            CARGOINVENTORIES.Clear();
            CARGOINVENTORIES.AddRange(CONTAINERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            List<IMyShipConnector> connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors, block => block.CustomName.Contains("[CRX] Connector"));
            CONNECTORSINVENTORIES.Clear();
            CONNECTORSINVENTORIES.AddRange(connectors.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            connectors.Clear();
            List<IMyGasGenerator> gasGenerators = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(gasGenerators, block => block.CustomName.Contains("[CRX] Gas Generator"));
            GASINVENTORIES.Clear();
            GASINVENTORIES.AddRange(gasGenerators.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            gasGenerators.Clear();
            List<IMyReactor> reactors = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors, block => block.CustomName.Contains("[CRX] Reactor"));
            REACTORSINVENTORIES.Clear();
            REACTORSINVENTORIES.AddRange(reactors.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            reactors.Clear();
            List<IMyTerminalBlock> blocksWithInventory = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocksWithInventory, block => block.HasInventory && block.CustomName.Contains("[CRX] "));//&& block.IsSameConstructAs(Me)
            INVENTORIES.Clear();
            INVENTORIES.AddRange(blocksWithInventory.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            blocksWithInventory.Clear();
            LCDINVENTORYMANAGER = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Inventory Manager") as IMyTextPanel;
        }

        void ResetOreDict() {
            oreDict = new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Gold"),0d}, {MyItemType.MakeOre("Ice"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d},
                {MyItemType.MakeOre("Nickel"),0d}, {MyItemType.MakeOre("Organic"),0d}, {MyItemType.MakeOre("Platinum"),0d}, {MyItemType.MakeOre("Scrap"),0d}, {MyItemType.MakeOre("Silicon"),0d},
                {MyItemType.MakeOre("Silver"),0d}, {MyItemType.MakeOre("Stone"),0d}, {MyItemType.MakeOre("Uranium"),0d}
            };
        }

        void ResetRefineryOreDict() {
            refineryOreDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Gold"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d},
                {MyItemType.MakeOre("Platinum"),0d}, {MyItemType.MakeOre("Scrap"),0d}, {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Silver"),0d}, {MyItemType.MakeOre("Stone"),0d},
                {MyItemType.MakeOre("Uranium"),0d}
            };

            baseRefineryOreDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d}, {MyItemType.MakeOre("Scrap"),0d},
                {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Stone"),0d},
            };
        }

        void ResetIngotDict() {
            ingotsDict = new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeIngot("Cobalt"),0d}, {MyItemType.MakeIngot("Gold"),0d}, {MyItemType.MakeIngot("Stone"),0d}, {MyItemType.MakeIngot("Iron"),0d}, {MyItemType.MakeIngot("Magnesium"),0d},
                {MyItemType.MakeIngot("Nickel"),0d}, {MyItemType.MakeIngot("Scrap"),0d}, {MyItemType.MakeIngot("Platinum"),0d}, {MyItemType.MakeIngot("Silicon"),0d},
                {MyItemType.MakeIngot("Silver"),0d}, {MyItemType.MakeIngot("Uranium"),0d}
            };
        }

        void ResetComponentsDict() {
            componentsDict = new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeComponent("BulletproofGlass"),0d}, {MyItemType.MakeComponent("Canvas"),0d}, {MyItemType.MakeComponent("Computer"),0d}, {MyItemType.MakeComponent("Construction"),0d},
                {MyItemType.MakeComponent("Detector"),0d}, {MyItemType.MakeComponent("Display"),0d}, {MyItemType.MakeComponent("Explosives"),0d}, {MyItemType.MakeComponent("Girder"),0d},
                {MyItemType.MakeComponent("GravityGenerator"),0d}, {MyItemType.MakeComponent("InteriorPlate"),0d}, {MyItemType.MakeComponent("LargeTube"),0d}, {MyItemType.MakeComponent("Medical"),0d},
                {MyItemType.MakeComponent("MetalGrid"),0d}, {MyItemType.MakeComponent("Motor"),0d}, {MyItemType.MakeComponent("PowerCell"),0d}, {MyItemType.MakeComponent("RadioCommunication"),0d},
                {MyItemType.MakeComponent("Reactor"),0d}, {MyItemType.MakeComponent("SmallTube"),0d}, {MyItemType.MakeComponent("SolarCell"),0d}, {MyItemType.MakeComponent("SteelPlate"),0d},
                {MyItemType.MakeComponent("Superconductor"),0d}, {MyItemType.MakeComponent("Thrust"),0d}, {MyItemType.MakeComponent("ZoneChip"),0d}
            };
        }

        void ResetAmmosDict() {
            ammosDict = new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeAmmo("NATO_25x184mm"),0d},
                {MyItemType.MakeAmmo("AutocannonClip"),0d},
                {MyItemType.MakeAmmo("Missile200mm"),0d},
                {MyItemType.MakeAmmo("LargeCalibreAmmo"),0d},
                {MyItemType.MakeAmmo("MediumCalibreAmmo"),0d},
                {MyItemType.MakeAmmo("LargeRailgunAmmo"),0d},
                {MyItemType.MakeAmmo("SmallRailgunAmmo"),0d}
            };
        }

    }
}
