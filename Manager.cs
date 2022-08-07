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

namespace IngameScript {
    partial class Program : MyGridProgram {

        //MANAGER
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

        bool automatedManagment = false;//enable/disable automatic managment
        bool togglePB = true;//enable/disable PB

        bool isControlled = false;
        bool solarPowerOnce = true;
        bool greenPowerOnce = true;
        bool hydrogenPowerOnce = true;
        bool fullSteamOnce = true;
        int ticks = 0;
        string powerStatus;
        double tankCapacityPercent;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        float hEngMaxOutput;
        float solarMaxOutput;
        float turbineMaxOutput;
        float hEngCurrentOutput;
        float reactorsMaxOutput;
        float reactorsCurrentOutput;
        float battsMaxOutput;
        public Dictionary<string, float> battsCurrentStoredPower = new Dictionary<string, float>();

        public List<IMyTerminalBlock> TERMINALS = new List<IMyTerminalBlock>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();
        public List<IMyPowerProducer> TURBINES = new List<IMyPowerProducer>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyGasTank> HTANKS = new List<IMyGasTank>();
        public List<IMyRefinery> REFINERIES = new List<IMyRefinery>();
        public List<IMyInventory> REFINERIESINVENTORIES = new List<IMyInventory>();
        public List<IMyAssembler> ASSEMBLERS = new List<IMyAssembler>();
        public List<IMyLargeTurretBase> ASSAULTTURRETS = new List<IMyLargeTurretBase>();
        public List<IMyInventory> ASSAULTTURRETSINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> GATLINGS = new List<IMyUserControllableGun>();
        public List<IMyInventory> GATLINGSINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> LAUNCHERS = new List<IMyUserControllableGun>();
        public List<IMyInventory> LAUNCHERSINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> RAILGUNS = new List<IMyUserControllableGun>();
        public List<IMyInventory> RAILGUNSINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> SMALLRAILGUNS = new List<IMyUserControllableGun>();
        public List<IMyInventory> SMALLRAILGUNSINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> ARTILLERY = new List<IMyUserControllableGun>();
        public List<IMyInventory> ARTILLERYINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> ASSAULT = new List<IMyUserControllableGun>();
        public List<IMyInventory> ASSAULTINVENTORIES = new List<IMyInventory>();
        public List<IMyUserControllableGun> AUTOCANNONS = new List<IMyUserControllableGun>();
        public List<IMyInventory> AUTOCANNONSINVENTORIES = new List<IMyInventory>();
        public List<IMyCargoContainer> CONTAINERS = new List<IMyCargoContainer>();
        public List<IMyInventory> CARGOINVENTORIES = new List<IMyInventory>();
        public List<IMyShipConnector> CONNECTORS = new List<IMyShipConnector>();
        public List<IMyInventory> CONNECTORSINVENTORIES = new List<IMyInventory>();
        public List<IMyReactor> REACTORS = new List<IMyReactor>();
        public List<IMyInventory> REACTORSINVENTORIES = new List<IMyInventory>();
        public List<IMyPowerProducer> HENGINES = new List<IMyPowerProducer>();
        public List<IMyInventory> HINVENTORIES = new List<IMyInventory>();
        public List<IMyGasGenerator> GASGENERATORS = new List<IMyGasGenerator>();
        public List<IMyInventory> GASINVENTORIES = new List<IMyInventory>();
        public List<IMyTerminalBlock> BLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
        public List<IMyInventory> INVENTORIES = new List<IMyInventory>();
        public List<IMyTextSurface> POWERSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> INVENTORYSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> COMPONENTSURFACES = new List<IMyTextSurface>();
        public List<IMyTerminalBlock> terminalblocks = new List<IMyTerminalBlock>();

        IMyTextPanel LCDMANAGER;
        IMyTextPanel LCDAUTO;

        readonly MyIni myIni = new MyIni();

        IMyBroadcastListener BROADCASTLISTENER;

        readonly MyDefinitionId electricityId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
        readonly MyDefinitionId hydrogenId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
        //readonly MyDefinitionId oxygenId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen");

        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");
        readonly MyItemType uraniumIngot = MyItemType.MakeIngot("Uranium");

        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");
        readonly MyItemType autocannonAmmo = MyItemType.MakeAmmo("AutocannonClip");
        readonly MyItemType assaultAmmo = MyItemType.MakeAmmo("MediumCalibreAmmo");
        readonly MyItemType artilleryAmmo = MyItemType.MakeAmmo("LargeCalibreAmmo");
        readonly MyItemType railgunAmmo = MyItemType.MakeAmmo("LargeRailgunAmmo");
        readonly MyItemType smallRailgunAmmo = MyItemType.MakeAmmo("SmallRailgunAmmo");

        //MyResourceSourceComponent source;
        MyResourceSinkComponent sink;

        public StringBuilder oresLog = new StringBuilder("");
        public StringBuilder ingotsLog = new StringBuilder("");
        public StringBuilder ammosLog = new StringBuilder("");
        public StringBuilder componentsLog = new StringBuilder("");
        public StringBuilder refineriesInputLog = new StringBuilder("");
        public StringBuilder assemblersInputLog = new StringBuilder("");
        public StringBuilder inventoriesPercentLog = new StringBuilder("");
        public StringBuilder powerLog = new StringBuilder("");

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
            foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
            foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
            foreach (IMyBatteryBlock block in BATTERIES) { block.Enabled = true; block.ChargeMode = ChargeMode.Auto; }
            GetBatteriesMaxInOut();
            GetHydrogenEnginesMaxOutput();
            GetReactorsMaxOutput();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[MANAGER]");
            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }
            if (LCDMANAGER != null) { LCDMANAGER.BackgroundColor = togglePB ? new Color(0, 0, 50) : new Color(0, 0, 0); };
            if (LCDAUTO != null) { LCDAUTO.BackgroundColor = automatedManagment ? new Color(0, 0, 50) : new Color(0, 0, 0); };
        }

        public void Main(string argument) {
            try {
                Echo($"ticks:{ticks}");
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(argument)) {
                    ProcessArgument(argument);
                    if (!togglePB) { return; }
                }

                GetBroadcastMessages();

                CalcPower();
                PowerManager();

                if (automatedManagment) {
                    if (ticks == 10 || ticks == 20 || ticks == 30 || ticks == 40 || ticks == 50) {
                        ReadPowerInfos();
                        WritePowerInfo();
                    }
                    if (ticks == 1) {
                        MoveProductionOutputsToMainInventory();
                    } else if (ticks == 3) {
                        MoveItemsIntoCargo(CONNECTORSINVENTORIES);
                    } else if (ticks == 5) {
                        CompactInventory(INVENTORIES);
                    } else if (ticks == 7) {
                        SortCargos();
                    } else if (ticks == 9) {
                        FillFromCargo(GASINVENTORIES, "Ice");
                    } else if (ticks == 11) {
                        FillFromCargo(REACTORSINVENTORIES, "Uranium");
                    } else if (ticks == 13) {
                        FillFromCargo(GATLINGSINVENTORIES, "NATO_25x184mm");
                    } else if (ticks == 15) {
                        FillFromCargo(ASSAULTINVENTORIES, "MediumCalibreAmmo");
                    } else if (ticks == 17) {
                        FillFromCargo(LAUNCHERSINVENTORIES, "Missile200mm");
                    } else if (ticks == 19) {
                        FillFromCargo(AUTOCANNONSINVENTORIES, "AutocannonClip");
                    } else if (ticks == 21) {
                        FillFromCargo(ASSAULTTURRETSINVENTORIES, "MediumCalibreAmmo");
                    } else if (ticks == 23) {
                        FillFromCargo(RAILGUNSINVENTORIES, "LargeRailgunAmmo");
                    } else if (ticks == 25) {
                        FillFromCargo(SMALLRAILGUNSINVENTORIES, "SmallRailgunAmmo");
                    } else if (ticks == 27) {
                        FillFromCargo(ARTILLERYINVENTORIES, "LargeCalibreAmmo");
                    } else if (ticks == 29) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(AUTOCANNONS);
                        BalanceInventories(terminalblocks, autocannonAmmo);
                    } else if (ticks == 31) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(ASSAULT);
                        BalanceInventories(terminalblocks, assaultAmmo);
                    } else if (ticks == 33) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(GATLINGS);
                        BalanceInventories(terminalblocks, gatlingAmmo);
                    } else if (ticks == 35) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(LAUNCHERS);
                        BalanceInventories(terminalblocks, missileAmmo);
                    } else if (ticks == 37) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(GASGENERATORS);
                        BalanceInventories(terminalblocks, iceOre);
                    } else if (ticks == 39) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(REACTORS);
                        BalanceInventories(terminalblocks, uraniumIngot);
                    } else if (ticks == 41) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(ASSAULTTURRETS);
                        BalanceInventories(terminalblocks, assaultAmmo);
                    } else if (ticks == 43) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(RAILGUNS);
                        BalanceInventories(terminalblocks, railgunAmmo);
                    } else if (ticks == 45) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(ARTILLERY);
                        BalanceInventories(terminalblocks, artilleryAmmo);
                    } else if (ticks == 47) {
                        terminalblocks.Clear();
                        terminalblocks.AddRange(SMALLRAILGUNS);
                        BalanceInventories(terminalblocks, smallRailgunAmmo);
                    } else if (ticks == 49) {
                        ReadAllItems(CARGOINVENTORIES);
                        AutoAssemblers();
                    } else if (ticks == 51) {
                        AutoRefineries();
                    } else if (ticks >= 53) {
                        ReadInventoryInfos();
                        WriteInventoryInfo();
                        WriteComponentsInfo();
                        ticks = 0;
                    }
                    ticks++;
                }

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                Setup();
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case "TogglePB":
                    togglePB = !togglePB;
                    if (togglePB) {
                        if (LCDMANAGER != null) { LCDMANAGER.BackgroundColor = new Color(0, 0, 50); };
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    } else {
                        if (LCDMANAGER != null) { LCDMANAGER.BackgroundColor = new Color(0, 0, 0); };
                        foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                        foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                        foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                        powerStatus = "Full Steam";
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    break;
                case "PBOn":
                    togglePB = true;
                    if (LCDMANAGER != null) { LCDMANAGER.BackgroundColor = new Color(0, 0, 50); };
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    break;
                case "PBOff":
                    togglePB = false;
                    if (LCDMANAGER != null) { LCDMANAGER.BackgroundColor = new Color(0, 0, 0); };
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                    powerStatus = "Full Steam";
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
                case "MoveProductionToMain":
                    MoveProductionOutputsToMainInventory();
                    break;
                case "ToggleAutoManager":
                    automatedManagment = !automatedManagment;
                    if (LCDAUTO != null) { LCDAUTO.BackgroundColor = automatedManagment ? new Color(0, 0, 50) : new Color(0, 0, 0); }
                    break;
                case "WritePower":
                    CalcPower();
                    ReadPowerInfos();
                    WritePowerInfo();
                    break;
                case "WriteInventory":
                    ReadInventoryInfos();
                    WriteInventoryInfo();
                    WriteComponentsInfo();
                    break;
            }
        }

        bool GetBroadcastMessages() {
            bool received = false;
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<string, bool>) {
                        MyTuple<string, bool> data = (MyTuple<string, bool>)igcMessage.Data;
                        string variable = data.Item1;
                        if (variable == "isControlled") {
                            isControlled = data.Item2;
                            received = true;
                        }
                    }
                }
            }
            return received;
        }

        void ParseCockpitConfigData(IMyCockpit cockpit) {//"[ManagerSettings]\ncockpitPowerSurface=2\n"
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, "ManagerSettings", out result);
            if (!string.IsNullOrEmpty(myIni.Get("ManagerSettings", "cockpitPowerSurface").ToString())) {
                int cockpitPowerSurface = myIni.Get("ManagerSettings", "cockpitPowerSurface").ToInt32();
                POWERSURFACES.Add(cockpit.GetSurface(cockpitPowerSurface));
            }
        }

        void PowerManager() {
            if (!isControlled) { PowerFlow(terminalCurrentInput); } else { PowerFlow(terminalMaxRequiredInput); }
        }

        void PowerFlow(float shipInput) {
            if (shipInput < (solarMaxOutput + turbineMaxOutput)) {
                if (solarPowerOnce) {
                    greenPowerOnce = true;
                    hydrogenPowerOnce = true;
                    fullSteamOnce = true;
                    powerStatus = "Solar Power";
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = false; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Recharge; }
                    solarPowerOnce = false;
                }
            } else if (shipInput < (solarMaxOutput + turbineMaxOutput + battsMaxOutput)) {
                if (greenPowerOnce) {
                    solarPowerOnce = true;
                    hydrogenPowerOnce = true;
                    fullSteamOnce = true;
                    powerStatus = "Green Power";
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = false; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    greenPowerOnce = false;
                }
            } else if (shipInput < (hEngMaxOutput + solarMaxOutput + turbineMaxOutput + battsMaxOutput) && tankCapacityPercent > 20d) {
                if (hydrogenPowerOnce) {
                    greenPowerOnce = true;
                    solarPowerOnce = true;
                    fullSteamOnce = true;
                    powerStatus = "Hydrogen Power";
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    hydrogenPowerOnce = false;
                }
            } else {
                if (fullSteamOnce) {
                    greenPowerOnce = true;
                    solarPowerOnce = true;
                    hydrogenPowerOnce = true;
                    powerStatus = "Full Steam";
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                    fullSteamOnce = false;
                }
            }
        }

        void CalcPower() {
            GetPowInput();
            GetBatteriesCurrentInOut();
            GetSolarsCurrentOutput();
            GetTurbinesCurrentOutput();
            GetHydrogenEnginesCurrentOutput();
            GetReactorsCurrentOutput();
            GetPercentTanksCapacity();
        }

        void GetPowInput() {
            terminalCurrentInput = 0;
            terminalMaxRequiredInput = 0;
            foreach (IMyTerminalBlock block in TERMINALS) {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) {
                    if (block is IMyJumpDrive || block.CustomName.Contains("Railgun")) {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.CurrentInputByType(electricityId);
                    } else {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.MaxRequiredInputByType(electricityId);
                    }
                }
            }
        }

        void GetSolarsCurrentOutput() {
            solarMaxOutput = 0;
            foreach (IMySolarPanel block in SOLARS) {
                if (!block.IsWorking) continue;
                solarMaxOutput += block.MaxOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { solarMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetTurbinesCurrentOutput() {
            turbineMaxOutput = 0;
            foreach (IMyPowerProducer block in TURBINES) {
                if (!block.IsWorking) continue;
                turbineMaxOutput += block.MaxOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { turbineMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetHydrogenEnginesCurrentOutput() {
            hEngCurrentOutput = 0;
            foreach (IMyPowerProducer block in HENGINES) {
                if (!block.IsWorking) continue;
                hEngCurrentOutput += block.CurrentOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { hEngCurrentOutput += source.CurrentOutputByType(electricityId); }
            }
        }

        void GetHydrogenEnginesMaxOutput() {
            hEngMaxOutput = 0;
            foreach (IMyPowerProducer block in HENGINES) {
                if (!block.IsWorking) continue;
                hEngMaxOutput += block.MaxOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { hEngMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetReactorsCurrentOutput() {
            reactorsCurrentOutput = 0;
            foreach (IMyPowerProducer block in REACTORS) {
                if (!block.IsWorking) continue;
                reactorsCurrentOutput += block.CurrentOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { reactorsCurrentOutput += source.CurrentOutputByType(electricityId); }
            }
        }

        void GetReactorsMaxOutput() {
            reactorsMaxOutput = 0;
            foreach (IMyPowerProducer block in REACTORS) {
                if (!block.IsWorking) continue;
                reactorsMaxOutput += block.MaxOutput;
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { reactorsMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetBatteriesCurrentInOut() {
            battsCurrentStoredPower.Clear();
            battsCurrentInput = 0;
            battsCurrentOutput = 0;
            foreach (IMyBatteryBlock block in BATTERIES) {
                if (!block.IsWorking) continue;
                battsCurrentInput += block.CurrentInput;
                battsCurrentOutput += block.CurrentOutput;
                //if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) { battsCurrentInput += sink.CurrentInputByType(electricityId); }
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { battsCurrentOutput += source.CurrentOutputByType(electricityId); }
                battsCurrentStoredPower.Add(block.CustomName, block.CurrentStoredPower);
            }
        }

        void GetBatteriesMaxInOut() {
            //battsMaxInput = 0;
            battsMaxOutput = 0;
            foreach (IMyBatteryBlock block in BATTERIES) {
                if (!block.IsWorking) continue;
                battsMaxOutput += block.MaxOutput;
                //if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) { battsMaxInput += sink.MaxRequiredInputByType(electricityId); }
                //if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { battsMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetPercentTanksCapacity() {
            tankCapacityPercent = 0;
            double totCapacity = 0;
            double totFill = 0;
            foreach (IMyGasTank tank in HTANKS) {
                if (tank.Components.TryGet<MyResourceSinkComponent>(out sink)) {
                    ListReader<MyDefinitionId> definitions = sink.AcceptedResources;
                    for (int y = 0; y < definitions.Count; y++) {
                        if (string.Compare(definitions[y].SubtypeId.ToString(), hydrogenId.SubtypeId.ToString(), true) == 0) {
                            double capacity = (double)tank.Capacity;
                            totCapacity += capacity;
                            totFill += capacity * tank.FilledRatio;
                            break;
                        }
                    }
                }
            }
            if (totFill > 0 && totCapacity > 0) { tankCapacityPercent = (totFill / totCapacity) * 100; }
        }

        void ReadPowerInfos() {
            powerLog.Clear();
            powerLog.Append("Status: ").Append(powerStatus).Append(", ");
            powerLog.Append("\n");
            powerLog.Append("Current Input: ").Append(terminalCurrentInput.ToString("0.0")).Append(", ");
            powerLog.Append("Max Req. Input: ").Append(terminalMaxRequiredInput.ToString("0.0")).Append("\n");
            powerLog.Append("Solar Power: ").Append(solarMaxOutput.ToString("0.0")).Append("\n");
            if (turbineMaxOutput > 0) { powerLog.Append("Turbines Power: ").Append(turbineMaxOutput.ToString("0.0")).Append("\n"); }
            powerLog.Append("Batteries Curr. In: ").Append(battsCurrentInput.ToString("0.0")).Append(", ");
            powerLog.Append("Curr. Out: ").Append(battsCurrentOutput.ToString("0.0")).Append("\n");
            powerLog.Append("Batteries Max Out: ").Append(battsMaxOutput.ToString("0.0")).Append("\n");
            powerLog.Append("H2Engines Curr. Out: ").Append(hEngCurrentOutput.ToString("0.0")).Append(", ");
            powerLog.Append("Max Out: ").Append(hEngMaxOutput.ToString("0.0")).Append("\n");
            powerLog.Append("Reactors Curr. Out: ").Append(reactorsCurrentOutput.ToString("0.0")).Append(", ");
            powerLog.Append("Max Out: ").Append(reactorsMaxOutput.ToString("0.0")).Append("\n");
            powerLog.Append("H2Tanks Fill: ").Append(tankCapacityPercent.ToString("0")).Append("%\n");
            double num;
            oreDict.TryGetValue(iceOre, out num);
            powerLog.Append("Ice: ").Append(num.ToString("0.0")).Append(", ");
            ingotsDict.TryGetValue(uraniumIngot, out num);
            powerLog.Append("Uranium: ").Append(num.ToString("0.0")).Append("\n");
            powerLog.Append("Batteries stored power:\n");
            int count = 0;
            foreach (KeyValuePair<string, float> storedPow in battsCurrentStoredPower) {
                powerLog.Append(storedPow.Value.ToString("0.0"));
                if (count > 5) {
                    powerLog.Append("\n");
                    count = 0;
                } else {
                    powerLog.Append(", ");
                }
                count++;
            }
            if (count != 0) {
                powerLog.Append("\n");
            }
            ammosDict.TryGetValue(gatlingAmmo, out num);
            powerLog.Append("Gatling: ").Append(num.ToString("0")).Append(", ");
            ammosDict.TryGetValue(autocannonAmmo, out num);
            powerLog.Append("Autocannon: ").Append(num.ToString("0")).Append("\n");
            ammosDict.TryGetValue(missileAmmo, out num);
            powerLog.Append("Rockets: ").Append(num.ToString("0")).Append(", ");
            ammosDict.TryGetValue(assaultAmmo, out num);
            powerLog.Append("Assault: ").Append(num.ToString("0")).Append("\n");
            ammosDict.TryGetValue(artilleryAmmo, out num);
            powerLog.Append("Artillery: ").Append(num.ToString("0")).Append(", ");
            ammosDict.TryGetValue(railgunAmmo, out num);
            powerLog.Append("Sabot: ").Append(num.ToString("0")).Append(", ");
            ammosDict.TryGetValue(smallRailgunAmmo, out num);
            powerLog.Append("S. Sabot: ").Append(num.ToString("0")).Append("\n");
        }

        void ReadInventoryInfos() {
            inventoriesPercentLog.Clear();
            terminalblocks.Clear();
            terminalblocks.AddRange(CONTAINERS);
            ReadInventoriesFillPercent(terminalblocks, 4);
            terminalblocks.Clear();
            terminalblocks.AddRange(GASGENERATORS);
            ReadInventoriesFillPercent(terminalblocks, 2);
            terminalblocks.Clear();
            terminalblocks.AddRange(REACTORS);
            ReadInventoriesFillPercent(terminalblocks, 3);
            terminalblocks.Clear();
            terminalblocks.AddRange(GATLINGS);
            ReadInventoriesFillPercent(terminalblocks, 2);
            terminalblocks.Clear();
            terminalblocks.AddRange(AUTOCANNONS);
            ReadInventoriesFillPercent(terminalblocks, 2);
            terminalblocks.Clear();
            terminalblocks.AddRange(LAUNCHERS);
            ReadInventoriesFillPercent(terminalblocks, 2);
            terminalblocks.Clear();
            terminalblocks.AddRange(ASSAULT);
            ReadInventoriesFillPercent(terminalblocks, 2);
            terminalblocks.Clear();
            terminalblocks.AddRange(ARTILLERY);
            terminalblocks.AddRange(RAILGUNS);
            terminalblocks.AddRange(SMALLRAILGUNS);
            ReadInventoriesFillPercent(terminalblocks, 3);
            terminalblocks.Clear();
            terminalblocks.AddRange(ASSAULTTURRETS);
            ReadInventoriesFillPercent(terminalblocks, 1);
            ReadAllItems(INVENTORIES);
            ReadAssemblersItems(ASSEMBLERS);
            ReadRefineriesItems(REFINERIES);
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
            ammosLog.Clear();
            oresLog.Clear();
            ingotsLog.Clear();
            componentsLog.Clear();
            int count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ammosDict) {
                ammosLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                count++;
                if (count > 1) {
                    ammosLog.Append("\n");
                    count = 0;
                }
            }
            ammosLog.Append("\n");
            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in oreDict) {
                oresLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                count++;
                if (count > 3) {
                    oresLog.Append("\n");
                    count = 0;
                }
            }
            oresLog.Append("\n");
            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in ingotsDict) {
                ingotsLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                count++;
                if (count > 3) {
                    ingotsLog.Append("\n");
                    count = 0;
                }
            }
            ingotsLog.Append("\n");
            count = 0;
            foreach (KeyValuePair<MyDefinitionId, double> entry in componentsDict) {
                componentsLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                count++;
                if (count > 3) {
                    componentsLog.Append("\n");
                    count = 0;
                }
            }
            componentsLog.Append("\n");
        }

        void ReadRefineriesItems(List<IMyRefinery> refineries) {
            refineriesInputLog.Clear();
            foreach (IMyRefinery block in refineries) {
                ResetIngotDict();
                ResetOreDict();
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.InputInventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    if (item.Type.GetItemInfo().IsOre) {
                        double num;
                        if (oreDict.TryGetValue(item.Type, out num)) {
                            oreDict[item.Type] = num + (double)item.Amount;
                        }
                    } else if (item.Type.GetItemInfo().IsIngot) {
                        double num;
                        if (ingotsDict.TryGetValue(item.Type, out num)) {
                            ingotsDict[item.Type] = num + (double)item.Amount;
                        }
                    }
                }
                refineriesInputLog.Append("\n" + block.CustomName.Replace("[CRX] ", "")).Append(" Input: \n");
                int count = 0;
                foreach (KeyValuePair<MyDefinitionId, double> entry in oreDict) {
                    if (entry.Value != 0) {
                        refineriesInputLog.Append($"{entry.Key.SubtypeId} Ore: ").Append($"{(int)entry.Value}, ");
                        count++;
                        if (count > 4) {
                            refineriesInputLog.Append("\n");
                            count = 0;
                        }
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, double> entry in ingotsDict) {
                    if (entry.Value != 0) {
                        refineriesInputLog.Append($"{entry.Key.SubtypeId} Ingot: ").Append($"{(int)entry.Value}, ");
                        count++;
                        if (count > 4) {
                            refineriesInputLog.Append("\n");
                            count = 0;
                        }
                    }
                }
                if (count > 0) { refineriesInputLog.Append("\n"); }
            }
        }

        void ReadAssemblersItems(List<IMyAssembler> assemblers) {
            assemblersInputLog.Clear();
            foreach (IMyAssembler block in assemblers) {
                ResetComponentsDict();
                ResetIngotDict();
                ResetAmmosDict();
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.InputInventory.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    if (item.Type.GetItemInfo().IsComponent) {
                        double num;
                        if (componentsDict.TryGetValue(item.Type, out num)) { componentsDict[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsIngot) {
                        double num;
                        if (ingotsDict.TryGetValue(item.Type, out num)) { ingotsDict[item.Type] = num + (double)item.Amount; }
                    } else if (item.Type.GetItemInfo().IsAmmo) {
                        double num;
                        if (ammosDict.TryGetValue(item.Type, out num)) { ammosDict[item.Type] = num + (double)item.Amount; }
                    }
                }
                assemblersInputLog.Append("\n" + block.CustomName.Replace("[CRX] ", "")).Append(" Input: \n");
                int count = 0;
                foreach (KeyValuePair<MyDefinitionId, double> entry in ammosDict) {
                    if (entry.Value != 0) {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                        count++;
                        if (count > 4) {
                            assemblersInputLog.Append("\n");
                            count = 0;
                        }
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, double> entry in componentsDict) {
                    if (entry.Value != 0) {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                        count++;
                        if (count > 4) {
                            assemblersInputLog.Append("\n");
                            count = 0;
                        }
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, double> entry in ingotsDict) {
                    if (entry.Value != 0) {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{(int)entry.Value}, ");
                        count++;
                        if (count > 4) {
                            assemblersInputLog.Append("\n");
                            count = 0;
                        }
                    }
                }
                if (count > 0) { assemblersInputLog.Append("\n"); }
            }
        }

        void ReadInventoriesFillPercent(List<IMyTerminalBlock> blocksWithInventory, int spacing) {
            int count = 0;
            foreach (IMyTerminalBlock block in blocksWithInventory) {
                List<IMyInventory> inventories = new List<IMyInventory>();
                inventories.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                foreach (IMyInventory inventory in inventories) {
                    double inventoriesPercent = 0;
                    double currentVolume = (double)inventory.CurrentVolume;
                    double maxVolume = (double)inventory.MaxVolume;
                    if (currentVolume != 0 && maxVolume != 0) {
                        inventoriesPercent = currentVolume / maxVolume * 100;
                    }
                    string blockName = block.CustomName.Replace("[CRX] ", "");
                    inventoriesPercentLog.Append(blockName + ": " + inventoriesPercent.ToString("0") + "% ");
                }
                count++;
                if (count > spacing) {
                    inventoriesPercentLog.Append("\n");
                    count = 0;
                }
            }
            if (count != 0) { inventoriesPercentLog.Append("\n"); }
        }

        void WritePowerInfo() {
            foreach (IMyTextSurface surface in POWERSURFACES) {
                StringBuilder text = new StringBuilder();
                text.Append(powerLog.ToString());
                surface.WriteText(text);
            }
        }

        void WriteInventoryInfo() {
            foreach (IMyTextSurface surface in INVENTORYSURFACES) {
                StringBuilder text = new StringBuilder();
                text.Append("INVENTORIES: \n");
                text.Append(inventoriesPercentLog.ToString());
                text.Append(refineriesInputLog.ToString());
                text.Append(assemblersInputLog.ToString());
                surface.WriteText(text);
            }
        }

        void WriteComponentsInfo() {
            foreach (IMyTextSurface surface in COMPONENTSURFACES) {
                StringBuilder text = new StringBuilder();
                text.Append("ORE: \n");
                text.Append(oresLog.ToString());
                text.Append("\nINGOTS: \n");
                text.Append(ingotsLog.ToString());
                text.Append("\nAMMO: \n");
                text.Append(ammosLog.ToString());
                text.Append("\nCOMPONENTS: \n");
                text.Append(componentsLog.ToString());
                surface.WriteText(text);
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

        void BalanceInventories(List<IMyTerminalBlock> blocks, MyItemType item) {
            if (blocks.Count > 0) {
                int totItems = 0;
                List<IMyInventory> inventories = new List<IMyInventory>();
                foreach (IMyTerminalBlock block in blocks) {
                    inventories.Add(block.GetInventory());
                    totItems += block.GetInventory().GetItemAmount(item).ToIntSafe();
                }
                int k = 0;
                int dividedAmount = totItems / blocks.Count;
                SortInventories(inventories, item);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(inventories);
                reversedInventories.Reverse();
                for (int i = 0; i < inventories.Count && k < reversedInventories.Count - i;) {
                    int currentAmount = inventories[i].GetItemAmount(item).ToIntSafe();
                    int availableAmount = reversedInventories[k].GetItemAmount(item).ToIntSafe();
                    if (currentAmount < dividedAmount + 1) {
                        if (availableAmount <= 2 * dividedAmount + 1 - currentAmount) {
                            inventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(item) ?? default(MyInventoryItem), availableAmount - dividedAmount);
                            k++;
                        } else {
                            inventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(item) ?? default(MyInventoryItem), dividedAmount + 1 - currentAmount);
                            i++;
                        }
                    } else { i++; }
                }
            }
        }

        void SortInventories(List<IMyInventory> inventories, MyItemType item) {
            if (item.TypeId == iceOre.TypeId) {
                inventories.Sort(CompareGasInventories);
            } else if (item.TypeId == uraniumIngot.TypeId) {
                inventories.Sort(CompareReactorsInventories);
            } else if (item.TypeId == autocannonAmmo.TypeId) {
                inventories.Sort(CompareAutocannonInventories);
            } else if (item.TypeId == gatlingAmmo.TypeId) {
                inventories.Sort(CompareGatlingsInventories);
            } else if (item.TypeId == missileAmmo.TypeId) {
                inventories.Sort(CompareMissileInventories);
            } else if (item.TypeId == artilleryAmmo.TypeId) {
                inventories.Sort(CompareArtilleryInventories);
            } else if (item.TypeId == assaultAmmo.TypeId) {
                inventories.Sort(CompareAssaultInventories);
            } else if (item.TypeId == railgunAmmo.TypeId) {
                inventories.Sort(CompareRailgunInventories);
            } else if (item.TypeId == smallRailgunAmmo.TypeId) {
                inventories.Sort(CompareSmallRailgunInventories);
            }
        }

        int CompareGasInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(iceOre).ToIntSafe() > secondInventory.GetItemAmount(iceOre).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareReactorsInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(uraniumIngot).ToIntSafe() > secondInventory.GetItemAmount(uraniumIngot).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareGatlingsInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(gatlingAmmo).ToIntSafe() > secondInventory.GetItemAmount(gatlingAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareAutocannonInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(autocannonAmmo).ToIntSafe() > secondInventory.GetItemAmount(autocannonAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareMissileInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(missileAmmo).ToIntSafe() > secondInventory.GetItemAmount(missileAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareArtilleryInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(artilleryAmmo).ToIntSafe() > secondInventory.GetItemAmount(artilleryAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareAssaultInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(assaultAmmo).ToIntSafe() > secondInventory.GetItemAmount(assaultAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareRailgunInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(railgunAmmo).ToIntSafe() > secondInventory.GetItemAmount(railgunAmmo).ToIntSafe()) { return 1; } else { return -1; }
        }

        int CompareSmallRailgunInventories(IMyInventory firstInventory, IMyInventory secondInventory) {
            if (firstInventory.GetItemAmount(smallRailgunAmmo).ToIntSafe() > secondInventory.GetItemAmount(smallRailgunAmmo).ToIntSafe()) { return 1; } else { return -1; }
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
                                bool transferred = false;
                                if (cargoInv.CanTransferItemTo(refInv, item.Type) && refInv.CanItemsBeAdded(item.Amount, item.Type)) {
                                    transferred = cargoInv.TransferItemTo(refInv, item);
                                }
                                if (!transferred) {
                                    MyFixedPoint amount = refInv.MaxVolume - refInv.CurrentVolume;
                                    transferred = cargoInv.TransferItemTo(refInv, item, amount);
                                }
                                if (!transferred) {
                                    cargoInv.TransferItemTo(refInv, item, item.Amount);
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
                                    bool transferred = false;
                                    if (cargoInv.CanTransferItemTo(refInv, item.Type) && refInv.CanItemsBeAdded(item.Amount, item.Type)) {
                                        transferred = cargoInv.TransferItemTo(refInv, item);
                                    }
                                    if (!transferred) {
                                        MyFixedPoint amount = refInv.MaxVolume - refInv.CurrentVolume;
                                        transferred = cargoInv.TransferItemTo(refInv, item, amount);
                                    }
                                    if (!transferred) {
                                        cargoInv.TransferItemTo(refInv, item, item.Amount);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void SortCargos() {
            IMyCargoContainer mainCargo = null;
            int cargoIndex = 1000;
            foreach (IMyCargoContainer cargo in CONTAINERS) {
                int cargoNum;
                bool parsed = int.TryParse(cargo.CustomName.Replace("[CRX] Cargo", "").Trim(), out cargoNum);
                if (parsed && cargoNum < cargoIndex) {
                    for (int i = 0; i < cargo.InventoryCount; i++) {
                        IMyInventory inv = cargo.GetInventory(i);
                        double currentVol = (double)cargo.GetInventory(i).CurrentVolume;
                        double maxVol = (double)cargo.GetInventory(i).MaxVolume;
                        double inventoriesPercent = 0;
                        if (currentVol != 0 && maxVol != 0) { inventoriesPercent = currentVol / maxVol * 100d; }
                        if (!inv.IsFull && inventoriesPercent < 99d) {
                            cargoIndex = cargoNum;
                            mainCargo = cargo;
                        }
                    }
                }
            }
            if (mainCargo != null) {
                List<IMyCargoContainer> EMPTYCARGOS = new List<IMyCargoContainer>();
                foreach (IMyCargoContainer cargo in CONTAINERS) {
                    int cargoNum;
                    bool parsed = int.TryParse(cargo.CustomName.Replace("[CRX] Cargo", "").Trim(), out cargoNum);
                    if (parsed && cargoNum > cargoIndex) { EMPTYCARGOS.Add(cargo); }
                }
                List<IMyInventory> CONTAINERSINVENTORIES = new List<IMyInventory>();
                CONTAINERSINVENTORIES.AddRange(EMPTYCARGOS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
                List<IMyInventory> MAININVENTORIES = new List<IMyInventory>();
                for (int i = 0; i < mainCargo.InventoryCount; i++) { MAININVENTORIES.Add(mainCargo.GetInventory(i)); }
                foreach (IMyInventory cargoInv in CONTAINERSINVENTORIES) {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    cargoInv.GetItems(items);
                    foreach (MyInventoryItem item in items) {
                        foreach (IMyInventory mainInv in MAININVENTORIES) {
                            bool transferred = false;
                            if (cargoInv.CanTransferItemTo(mainInv, item.Type) && mainInv.CanItemsBeAdded(item.Amount, item.Type)) { transferred = cargoInv.TransferItemTo(mainInv, item); }
                            if (!transferred) {
                                MyFixedPoint amount = mainInv.MaxVolume - mainInv.CurrentVolume;
                                transferred = cargoInv.TransferItemTo(mainInv, item, amount);
                            }
                            if (!transferred) { cargoInv.TransferItemTo(mainInv, item, item.Amount); }
                        }
                    }
                }
            }
        }

        void FillFromCargo(List<IMyInventory> inventories, String itemToFind) {
            foreach (IMyInventory cargoInv in CARGOINVENTORIES) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                cargoInv.GetItems(items);
                foreach (MyInventoryItem item in items) {
                    if (item.Type.SubtypeId.ToString().Contains(itemToFind)) {
                        foreach (IMyInventory inv in inventories) {
                            bool transferred = false;
                            if (cargoInv.CanTransferItemTo(inv, item.Type) && inv.CanItemsBeAdded(item.Amount, item.Type)) { transferred = cargoInv.TransferItemTo(inv, item); }
                            if (!transferred) {
                                MyFixedPoint amount = inv.MaxVolume - inv.CurrentVolume;
                                transferred = cargoInv.TransferItemTo(inv, item, amount);
                            }
                            if (!transferred) { cargoInv.TransferItemTo(inv, item, item.Amount); }
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
                        bool tranferred = false;
                        if (inv.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type)) { tranferred = inv.TransferItemTo(cargoInv, item); }
                        if (!tranferred) {
                            MyFixedPoint amount = cargoInv.MaxVolume - cargoInv.CurrentVolume;
                            tranferred = inv.TransferItemTo(cargoInv, item, amount);
                        }
                        if (!tranferred) { inv.TransferItemTo(cargoInv, item, item.Amount); }
                    }
                }
            }
        }

        void GetBlocks() {
            TERMINALS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TERMINALS, block => block.CustomName.Contains("[CRX]") && !(block is IMyPowerProducer) && !(block is IMySolarPanel) && !(block is IMyBatteryBlock) && !(block is IMyReactor) && !block.CustomName.Contains("[CRX] HThruster"));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains("[CRX] Controller Cockpit"));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains("[CRX] Gyroscope"));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains("[CRX] HThruster") || block.CustomName.Contains("[CRX] IonThruster") || block.CustomName.Contains("[CRX] AtmoThruster"));
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains("[CRX] Solar"));
            TURBINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(TURBINES, block => block.CustomName.Contains("[CRX] Wind Turbine"));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, block => block.CustomName.Contains("[CRX] Battery"));
            HTANKS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(HTANKS, block => block.CustomName.Contains("[CRX] HTank"));
            HENGINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(HENGINES, block => block.CustomName.Contains("[CRX] HEngine"));
            REFINERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(REFINERIES, block => block.CustomName.Contains("[CRX] Refinery"));
            REFINERIESINVENTORIES.Clear();
            REFINERIESINVENTORIES.AddRange(REFINERIES.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSEMBLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(ASSEMBLERS, block => block.CustomName.Contains("[CRX] Assembler"));
            ASSAULTTURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(ASSAULTTURRETS, block => block.CustomName.Contains("[CRX] Turret Assault"));
            ASSAULTTURRETSINVENTORIES.Clear();
            ASSAULTTURRETSINVENTORIES.AddRange(ASSAULTTURRETS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, block => block.CustomName.Contains("[CRX] Gatling"));
            GATLINGSINVENTORIES.Clear();
            GATLINGSINVENTORIES.AddRange(GATLINGS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            LAUNCHERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(LAUNCHERS, block => block.CustomName.Contains("[CRX] Rocket"));
            LAUNCHERSINVENTORIES.Clear();
            LAUNCHERSINVENTORIES.AddRange(LAUNCHERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            RAILGUNS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(RAILGUNS, block => block.CustomName.Contains("[CRX] Railgun"));
            RAILGUNSINVENTORIES.Clear();
            RAILGUNSINVENTORIES.AddRange(RAILGUNS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            SMALLRAILGUNS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(SMALLRAILGUNS, block => block.CustomName.Contains("[CRX] Small Railgun"));
            SMALLRAILGUNSINVENTORIES.Clear();
            SMALLRAILGUNSINVENTORIES.AddRange(SMALLRAILGUNS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ARTILLERY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ARTILLERY, block => block.CustomName.Contains("[CRX] Artillery"));
            ARTILLERYINVENTORIES.Clear();
            ARTILLERYINVENTORIES.AddRange(ARTILLERY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            AUTOCANNONS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(AUTOCANNONS, block => block.CustomName.Contains("[CRX] Autocannon"));
            AUTOCANNONSINVENTORIES.Clear();
            AUTOCANNONSINVENTORIES.AddRange(AUTOCANNONS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSAULT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ASSAULT, block => block.CustomName.Contains("[CRX] Assault"));
            ASSAULTINVENTORIES.Clear();
            ASSAULTINVENTORIES.AddRange(ASSAULT.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CONTAINERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CONTAINERS, block => block.CustomName.Contains("[CRX] Cargo"));
            CARGOINVENTORIES.Clear();
            CARGOINVENTORIES.AddRange(CONTAINERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CONNECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(CONNECTORS, block => block.CustomName.Contains("[CRX] Connector"));
            CONNECTORSINVENTORIES.Clear();
            CONNECTORSINVENTORIES.AddRange(CONNECTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            GASGENERATORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(GASGENERATORS, block => block.CustomName.Contains("[CRX] Gas Generator"));
            GASINVENTORIES.Clear();
            GASINVENTORIES.AddRange(GASGENERATORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            REACTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(REACTORS, block => block.CustomName.Contains("[CRX] Reactor"));
            REACTORSINVENTORIES.Clear();
            REACTORSINVENTORIES.AddRange(REACTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            BLOCKSWITHINVENTORY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(BLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains("[CRX] "));//&& block.IsSameConstructAs(Me)
            INVENTORIES.Clear();
            INVENTORIES.AddRange(BLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            LCDMANAGER = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Manager") as IMyTextPanel;
            LCDAUTO = GridTerminalSystem.GetBlockWithName("[CRX] LCD Auto Manager") as IMyTextPanel;
            POWERSURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Power"));
            foreach (IMyTextPanel panel in panels) { POWERSURFACES.Add(panel as IMyTextSurface); }
            INVENTORYSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Inventory"));
            foreach (IMyTextPanel panel in panels) { INVENTORYSURFACES.Add(panel as IMyTextSurface); }
            COMPONENTSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Components"));
            foreach (IMyTextPanel panel in panels) { COMPONENTSURFACES.Add(panel as IMyTextSurface); }
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
