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
        bool togglePB = true;//enable/disable PB
        bool logger = true;//enable/disable logging

        bool proceed = false;
        double cargoPercentage = 0;
        int readerCount = 0;
        int productionCount = 0;

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

        IMyTextPanel LCDUPDATEQUOTA;

        readonly MyIni myIni = new MyIni();

        IEnumerator<bool> inventoryStateMachine;
        IEnumerator<bool> productionStateMachine;
        IEnumerator<bool> readerStateMachine;

        public Dictionary<MyDefinitionId, MyTuple<string, double>> componentsDefBpQuota = new Dictionary<MyDefinitionId, MyTuple<string, double>>() {
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

        public Dictionary<MyDefinitionId, double> ammoDict = new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
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
            Me.GetSurface(0).BackgroundColor = togglePB ? new Color(25, 0, 100) : new Color(0, 0, 0);
            inventoryStateMachine = RunInventoryOverTime();
            productionStateMachine = RunProductionOverTime();
            readerStateMachine = RunReaderOverTime();
        }

        public void Main(string argument, UpdateType updateType) {
            try {
                Echo($"LastRunTime:{Runtime.LastRunTimeMs}");
                Echo($"proceed:{proceed}, reader:{readerCount}");

                if (!string.IsNullOrEmpty(argument)) {
                    ProcessArgument(argument);
                    if (!togglePB) { return; } else if (argument == "UpdateQuota") { return; }
                } else {
                    if ((updateType & UpdateType.Update10) == UpdateType.Update10) {
                        bool executed = RunInventoryStateMachine();

                        bool read = RunReaderStateMachine();
                        if (read) {
                            proceed = true;
                        } else if (executed && read) {
                            proceed = true;
                        } else {
                            proceed = false;
                        }

                        if (!executed) {
                            productionCount++;
                            RunProductionStateMachine();
                        }
                    }
                }
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
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
                        Me.GetSurface(0).BackgroundColor = new Color(25, 0, 100);
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    } else {
                        Me.GetSurface(0).BackgroundColor = new Color(0, 0, 0);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    break;
                case "PBOn":
                    togglePB = true;
                    Me.GetSurface(0).BackgroundColor = new Color(25, 0, 100);
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    break;
                case "PBOff":
                    togglePB = false;
                    Me.GetSurface(0).BackgroundColor = new Color(0, 0, 0);
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
                case "UpdateQuota":
                    UpdateQuota();
                    break;
            }
        }

        public IEnumerator<bool> RunInventoryOverTime() {

            foreach (IMyInventory inv in CONNECTORSINVENTORIES) {
                //if (inv.ItemCount > 0) {
                Echo($"MoveItemsIntoCargo");

                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inv.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                        if (inv.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type)) {
                            inv.TransferItemTo(cargoInv, item);
                        }
                    }
                }
                yield return true;
                yield return false;
                //}
            }

            foreach (IMyInventory inventory in INVENTORIES) {
                Echo($"CompactInventory");

                for (int i = inventory.ItemCount - 1; i > 0; i--) {
                    inventory.TransferItemTo(inventory, i, stackIfPossible: true);
                }
                yield return true;
                yield return false;
            }

            List<IMyCargoContainer> reversedCargo = CONTAINERS;
            reversedCargo.Reverse();
            foreach (IMyCargoContainer rCargo in reversedCargo) {
                IMyInventory rInventory = rCargo.GetInventory();
                //if (rInventory.ItemCount > 0) {
                Echo($"SortCargos");

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
                yield return true;
                yield return false;
                //}
            }
        }

        public IEnumerator<bool> RunProductionOverTime() {
            if (proceed && productionCount >= 50) {
                AutoAssemblers();
                yield return true;

                AutoRefineries();
                yield return true;

                MoveProductionOutputsToMainInventory();
                proceed = false;
                productionCount = 0;
                yield return true;
            } else {
                yield return true;
            }
        }

        public IEnumerator<bool> RunReaderOverTime() {

            Dictionary<MyDefinitionId, double> components = ResetComponentsDict();
            Dictionary<MyDefinitionId, double> ingots = ResetIngotDict();
            Dictionary<MyDefinitionId, double> ore = ResetOreDict();
            Dictionary<MyDefinitionId, double> refineryOre = ResetRefineryOreDict();
            Dictionary<MyDefinitionId, double> baseRefineryOre = ResetBaseRefineryOreDict();
            Dictionary<MyDefinitionId, double> ammo = ResetAmmosDict();
            foreach (IMyInventory inventory in INVENTORIES) {
                //if (inventory.ItemCount > 0) {
                Echo($"ReadAllItems");

                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    if (item.Type.GetItemInfo().IsOre) {
                        double num;
                        if (ore.TryGetValue(item.Type, out num)) { ore[item.Type] = num + (double)item.Amount; }
                        if (refineryOre.TryGetValue(item.Type, out num)) { refineryOre[item.Type] = num + (double)item.Amount; }
                        if (baseRefineryOre.TryGetValue(item.Type, out num)) { baseRefineryOre[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsIngot) {
                        double num;
                        if (ingots.TryGetValue(item.Type, out num)) { ingots[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsComponent) {
                        double num;
                        if (components.TryGetValue(item.Type, out num)) { components[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsAmmo) {
                        double num;
                        if (ammo.TryGetValue(item.Type, out num)) { ammo[item.Type] = num + (double)item.Amount; }
                    }
                }
                if (logger) { readerCount++; }
                yield return false;
                //}
            }
            componentsDict = components;
            ingotsDict = ingots;
            oreDict = ore;
            refineryOreDict = refineryOre;
            baseRefineryOreDict = baseRefineryOre;
            ammoDict = ammo;

            if (logger) {
                readerCount++;

                if (readerCount >= 9) {
                    ReadInventoriesFillPercentage(CARGOINVENTORIES, out cargoPercentage);
                    yield return false;

                    SendBroadcastMessage();
                    readerCount = 0;
                    yield return true;
                } else {
                    yield return true;
                }
            } else {
                yield return true;
            }
        }

        public bool RunInventoryStateMachine() {
            if (inventoryStateMachine != null) {
                bool hasMoreSteps = inventoryStateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                } else {
                    Echo($"InventoryDispose");

                    inventoryStateMachine.Dispose();
                    inventoryStateMachine = RunInventoryOverTime();//stateMachine = null;
                }

                return inventoryStateMachine.Current;
            }
            return false;
        }

        public void RunProductionStateMachine() {
            if (productionStateMachine != null) {
                bool hasMoreSteps = productionStateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                } else {
                    Echo($"ProductionDispose");

                    productionStateMachine.Dispose();
                    productionStateMachine = RunProductionOverTime();
                }
            }
        }

        public bool RunReaderStateMachine() {
            if (readerStateMachine != null) {
                bool hasMoreSteps = readerStateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                } else {
                    Echo($"ReaderDispose");

                    readerStateMachine.Dispose();
                    readerStateMachine = RunReaderOverTime();
                }

                return readerStateMachine.Current;
            }
            return false;
        }

        void SendBroadcastMessage() {
            Echo($"SendBroadcastMessage");

            StringBuilder oresLog = new StringBuilder("");
            StringBuilder ingotsLog = new StringBuilder("");
            StringBuilder ammosLog = new StringBuilder("");
            StringBuilder componentsLog = new StringBuilder("");

            int count = 1;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ammoDict) {
                if (count == ammoDict.Count) {
                    ammosLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    ammosLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 1;
            foreach (KeyValuePair<MyDefinitionId, double> entry in oreDict) {
                if (count == oreDict.Count) {
                    oresLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    oresLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 1;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ingotsDict) {
                if (count == ingotsDict.Count) {
                    ingotsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    ingotsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }

            count = 1;
            foreach (KeyValuePair<MyDefinitionId, double> entry in componentsDict) {
                if (count == componentsDict.Count) {
                    componentsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value}");
                } else {
                    componentsLog.Append($"{entry.Key.SubtypeId}={(int)entry.Value},");
                }
                count++;
            }
            var tuple = MyTuple.Create(
                cargoPercentage,
                ammosLog.ToString(),
                oresLog.ToString(),
                ingotsLog.ToString(),
                componentsLog.ToString()
                );
            IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void ReadInventoryInfos() {
            Echo($"ReadInventoryInfos");

            ReadInventoriesFillPercentage(CARGOINVENTORIES, out cargoPercentage);
            ReadAllItems(INVENTORIES);
        }

        void ReadAllItems(List<IMyInventory> inventories) {
            componentsDict = ResetComponentsDict();
            ingotsDict = ResetIngotDict();
            oreDict = ResetOreDict();
            refineryOreDict = ResetRefineryOreDict();
            baseRefineryOreDict = ResetBaseRefineryOreDict();
            ammoDict = ResetAmmosDict();
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
                        if (ammoDict.TryGetValue(item.Type, out num)) { ammoDict[item.Type] = num + (double)item.Amount; }
                    }
                }
            }
        }

        void ReadInventoriesFillPercentage(List<IMyInventory> inventories, out double invPercent) {
            Echo($"ReadInventoriesFillPercentage");

            invPercent = 0d;
            foreach (IMyInventory inventory in inventories) {
                if (inventory.ItemCount > 0) {
                    double inventoriesPercent = 0d;
                    double currentVolume = (double)inventory.CurrentVolume;
                    double maxVolume = (double)inventory.MaxVolume;
                    if (maxVolume != 0d) {
                        inventoriesPercent = currentVolume / maxVolume * 100d;
                    }
                    invPercent += inventoriesPercent;
                }
            }
        }

        void MoveProductionOutputsToMainInventory() {
            Echo($"MoveProductionOutputsToMainInventory");

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
            Echo($"CompactInventory");

            foreach (IMyInventory inventory in inventories) {
                for (int i = inventory.ItemCount - 1; i > 0; i--) { inventory.TransferItemTo(inventory, i, stackIfPossible: true); }
            }
        }

        void AutoAssemblers() {
            Echo($"AutoAssemblers");

            int clearQueue = 0;
            foreach (KeyValuePair<MyDefinitionId, MyTuple<string, double>> element in componentsDefBpQuota) {
                MyDefinitionId component = element.Key;
                string componentBp = element.Value.Item1;
                double componentQuota = element.Value.Item2;
                MyDefinitionId blueprintDef = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + componentBp);
                double cargoAmount = 0d;
                bool itemFound = componentsDict.TryGetValue(component, out cargoAmount);
                if (!itemFound) { itemFound = ammoDict.TryGetValue(component, out cargoAmount); }
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
            Echo($"AutoRefineries");

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
            Echo($"SortCargos");

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
            Echo($"MoveItemsIntoCargo");

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

        void UpdateQuota() {
            MyIniParseResult result;
            myIni.TryParse(LCDUPDATEQUOTA.CustomData, "QuotaSettings", out result);
            if (!string.IsNullOrEmpty(myIni.Get("QuotaSettings", "cockpitOverviewSurface").ToString())) {
                MyTuple<string, double> res;
                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("Missile200mm"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Missile200mm").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("Missile200mm")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("NATO_25x184mm"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "NATO_25x184mm").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("NATO_25x184mm")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("AutocannonClip"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "AutocannonClip").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("AutocannonClip")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("LargeCalibreAmmo"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "LargeCalibreAmmo").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("LargeCalibreAmmo")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("MediumCalibreAmmo"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "MediumCalibreAmmo").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("MediumCalibreAmmo")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("LargeRailgunAmmo"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "LargeRailgunAmmo").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("LargeRailgunAmmo")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeAmmo("SmallRailgunAmmo"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "SmallRailgunAmmo").ToInt32();
                componentsDefBpQuota[MyItemType.MakeAmmo("SmallRailgunAmmo")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("BulletproofGlass"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "BulletproofGlass").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("BulletproofGlass")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Canvas"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Canvas").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Canvas")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Computer"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Computer").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Computer")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Construction"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Construction").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Construction")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Detector"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Detector").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Detector")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Display"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Display").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Display")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Explosives"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Explosives").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Explosives")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Girder"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Girder").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Girder")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("GravityGenerator"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "GravityGenerator").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("GravityGenerator")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("InteriorPlate"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "InteriorPlate").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("InteriorPlate")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("LargeTube"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "LargeTube").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("LargeTube")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Medical"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Medical").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Medical")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("MetalGrid"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "MetalGrid").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("MetalGrid")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Motor"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Motor").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Motor")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("PowerCell"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "PowerCell").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("PowerCell")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("RadioCommunication"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "RadioCommunication").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("RadioCommunication")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Reactor"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Reactor").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Reactor")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("SmallTube"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "SmallTube").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("SmallTube")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("SolarCell"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "SolarCell").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("SolarCell")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("SteelPlate"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "SteelPlate").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("SteelPlate")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Superconductor"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Superconductor").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Superconductor")] = res;

                componentsDefBpQuota.TryGetValue(MyItemType.MakeComponent("Thrust"), out res);
                res.Item2 = myIni.Get("QuotaSettings", "Thrust").ToInt32();
                componentsDefBpQuota[MyItemType.MakeComponent("Thrust")] = res;
            }
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
            LCDUPDATEQUOTA = GridTerminalSystem.GetBlockWithName("[CRX] LCD Update Quota") as IMyTextPanel;
        }

        Dictionary<MyDefinitionId, double> ResetOreDict() {
            return new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Gold"),0d}, {MyItemType.MakeOre("Ice"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d},
                {MyItemType.MakeOre("Nickel"),0d}, {MyItemType.MakeOre("Organic"),0d}, {MyItemType.MakeOre("Platinum"),0d}, {MyItemType.MakeOre("Scrap"),0d}, {MyItemType.MakeOre("Silicon"),0d},
                {MyItemType.MakeOre("Silver"),0d}, {MyItemType.MakeOre("Stone"),0d}, {MyItemType.MakeOre("Uranium"),0d}
            };
        }

        Dictionary<MyDefinitionId, double> ResetRefineryOreDict() {
            return new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Gold"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d},
                {MyItemType.MakeOre("Platinum"),0d}, {MyItemType.MakeOre("Scrap"),0d}, {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Silver"),0d}, {MyItemType.MakeOre("Stone"),0d},
                {MyItemType.MakeOre("Uranium"),0d}
            };
        }

        Dictionary<MyDefinitionId, double> ResetBaseRefineryOreDict() {
            return new Dictionary<MyDefinitionId, double>(MyDefinitionId.Comparer) {
                {MyItemType.MakeOre("Cobalt"),0d}, {MyItemType.MakeOre("Iron"),0d}, {MyItemType.MakeOre("Magnesium"),0d}, {MyItemType.MakeOre("Nickel"),0d}, {MyItemType.MakeOre("Scrap"),0d},
                {MyItemType.MakeOre("Silicon"),0d}, {MyItemType.MakeOre("Stone"),0d},
            };
        }

        Dictionary<MyDefinitionId, double> ResetIngotDict() {
            return new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeIngot("Cobalt"),0d}, {MyItemType.MakeIngot("Gold"),0d}, {MyItemType.MakeIngot("Stone"),0d}, {MyItemType.MakeIngot("Iron"),0d}, {MyItemType.MakeIngot("Magnesium"),0d},
                {MyItemType.MakeIngot("Nickel"),0d}, {MyItemType.MakeIngot("Scrap"),0d}, {MyItemType.MakeIngot("Platinum"),0d}, {MyItemType.MakeIngot("Silicon"),0d},
                {MyItemType.MakeIngot("Silver"),0d}, {MyItemType.MakeIngot("Uranium"),0d}
            };
        }

        Dictionary<MyDefinitionId, double> ResetComponentsDict() {
            return new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeComponent("BulletproofGlass"),0d}, {MyItemType.MakeComponent("Canvas"),0d}, {MyItemType.MakeComponent("Computer"),0d}, {MyItemType.MakeComponent("Construction"),0d},
                {MyItemType.MakeComponent("Detector"),0d}, {MyItemType.MakeComponent("Display"),0d}, {MyItemType.MakeComponent("Explosives"),0d}, {MyItemType.MakeComponent("Girder"),0d},
                {MyItemType.MakeComponent("GravityGenerator"),0d}, {MyItemType.MakeComponent("InteriorPlate"),0d}, {MyItemType.MakeComponent("LargeTube"),0d}, {MyItemType.MakeComponent("Medical"),0d},
                {MyItemType.MakeComponent("MetalGrid"),0d}, {MyItemType.MakeComponent("Motor"),0d}, {MyItemType.MakeComponent("PowerCell"),0d}, {MyItemType.MakeComponent("RadioCommunication"),0d},
                {MyItemType.MakeComponent("Reactor"),0d}, {MyItemType.MakeComponent("SmallTube"),0d}, {MyItemType.MakeComponent("SolarCell"),0d}, {MyItemType.MakeComponent("SteelPlate"),0d},
                {MyItemType.MakeComponent("Superconductor"),0d}, {MyItemType.MakeComponent("Thrust"),0d}, {MyItemType.MakeComponent("ZoneChip"),0d}
            };
        }

        Dictionary<MyDefinitionId, double> ResetAmmosDict() {
            return new Dictionary<MyDefinitionId, double>() {
                {MyItemType.MakeAmmo("NATO_25x184mm"),0d},
                {MyItemType.MakeAmmo("AutocannonClip"),0d},
                {MyItemType.MakeAmmo("Missile200mm"),0d},
                {MyItemType.MakeAmmo("LargeCalibreAmmo"),0d},
                {MyItemType.MakeAmmo("MediumCalibreAmmo"),0d},
                {MyItemType.MakeAmmo("LargeRailgunAmmo"),0d},
                {MyItemType.MakeAmmo("SmallRailgunAmmo"),0d}
            };
        }

        /*
        float GetMyTerminalBlockHealth(IMyTerminalBlock block)
        {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            float maxIntegrity = slimblock.MaxIntegrity;
            float buildIntegrity = slimblock.BuildIntegrity;
            float currentDamage = slimblock.CurrentDamage;
            float health = (buildIntegrity - currentDamage) / maxIntegrity;
            return health;
        }
        */
    }
}
