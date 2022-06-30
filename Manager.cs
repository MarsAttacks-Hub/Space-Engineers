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
        //TODO check damaged/destroyed blocks
        //MANAGER

        readonly string solarsName = "[CRX] Solar";
        readonly string turbinesName = "[CRX] Wind Turbine";
        readonly string batteriesName = "[CRX] Battery";
        readonly string hTanksName = "[CRX] HTank";
        readonly string hEnginesName = "[CRX] HEngine";
        readonly string gasGeneratorsName = "[CRX] Gas Generator";
        readonly string reactorsName = "[CRX] Reactor";
        readonly string gyrosName = "[CRX] Gyroscope";
        readonly string controllersName = "[CRX] Controller";
        readonly string terminalsName = "[CRX]";
        readonly string lcdPowerName = "[CRX] LCD Power";
        readonly string lcdInventoryName = "[CRX] LCD Inventory";
        readonly string lcdStatusName = "[CRX] LCD Manager Status";
        readonly string lcdComponentsName = "[CRX] LCD Components";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string hThrustersName = "[CRX] HThruster";
        readonly string iThrustersName = "[CRX] IonThruster";
        readonly string aThrustersName = "[CRX] AtmoThruster";
        readonly string refineriesName = "[CRX] Refinery";
        readonly string assemblersName = "[CRX] Assembler";
        readonly string containersName = "[CRX] Cargo";
        //readonly string gatlingTurretsName = "[CRX] Turret Gatling";
        //readonly string missileTurretsName = "[CRX] Turret Missile";
        readonly string assaultTurretsName = "[CRX] Turret Assault";
        readonly string connectorsName = "[CRX] Connector";
        readonly string shipPrefix = "[CRX] ";
        readonly string launchersName = "[CRX] Rocket";
        readonly string gatlingsName = "[CRX] Gatling";
        readonly string railgunsName = "[CRX] Railgun";
        readonly string smallRailgunsName = "[CRX] Small Railgun";
        readonly string artilleryName = "[CRX] Artillery";
        readonly string autocannonName = "[CRX] Autocannon";
        readonly string assaultName = "[CRX] Assault";
        readonly string debugPanelName = "[CRX] Debug";
        readonly string sectionTag = "ManagerSettings";
        readonly string cockpitPowerSurfaceKey = "cockpitPowerSurface";
        readonly string managerTag = "[MANAGER]";
        readonly double tankThresold = 20;
        //readonly float batteryChargeDrain = 12f;//12Mw
        //readonly int chargeDelay = 50;

        const string argTogglePB = "TogglePB";

        //int chargeCount = 50; 
        int cockpitPowerSurface = 2;
        int firstRun = 1;
        int ticks = 0;
        bool togglePB = false;
        bool isControlled = true;
        string powerStatus;

        double tankCapacityPercent;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        float hEngMaxOutput;
        float solarMaxOutput;
        float turbineMaxOutput;
        float registeredhEngMaxOutput;

        public List<IMyTerminalBlock> TERMINALS = new List<IMyTerminalBlock>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
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
        //public List<IMyLargeGatlingTurret> GATLINGTURRETS = new List<IMyLargeGatlingTurret>();
        //public List<IMyInventory> GATLINGTURRETSINVENTORIES = new List<IMyInventory>();
        //public List<IMyLargeMissileTurret> MISSILETURRETS = new List<IMyLargeMissileTurret>();
        //public List<IMyInventory> MISSILETURRETSINVENTORIES = new List<IMyInventory>();
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
        public List<IMyTextPanel> LCDSSTATUS = new List<IMyTextPanel>();
        public List<IMyTextSurface> POWERSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> INVENTORYSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> COMPONENTSURFACES = new List<IMyTextSurface>();
        public List<IMyTerminalBlock> terminalblocks = new List<IMyTerminalBlock>();

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

        MyResourceSourceComponent source;
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

        readonly Dictionary<MyDefinitionId, MyTuple<string, double>> componentsDefBpQuota = new Dictionary<MyDefinitionId, MyTuple<string, double>>() {//TODO
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

            BROADCASTLISTENER = IGC.RegisterBroadcastListener(managerTag);

            foreach (IMyCockpit cockpit in COCKPITS) { ParseCockpitConfigData(cockpit); }
        }

        public void Main(string argument) {
            try {
                Echo($"ticks:{ticks}");
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(argument)) { ProcessArgument(argument); }

                GetBroadcastMessages();

                CalcPower();
                PowerManager();
                ReadPowerInfos();
                WritePowerInfo();

                if (ticks == 1) {
                    MoveProductionOutputsToMainInventory();
                } else if (ticks == 3) {
                    MoveItemsIntoCargo(CONNECTORSINVENTORIES);
                } else if (ticks == 5) {
                    CompactInventory(INVENTORIES);
                } else if (ticks == 7) {
                    CompactMainCargos();
                } else if (ticks == 9) {
                    FillFromCargo(GASINVENTORIES, "Ice");
                } else if (ticks == 11) {
                    FillFromCargo(REACTORSINVENTORIES, "Uranium");
                } else if (ticks == 13) {
                    FillFromCargo(GATLINGSINVENTORIES, "NATO_25x184mm");
                } else if (ticks == 15) {
                    //FillFromCargo(GATLINGTURRETSINVENTORIES, "NATO_25x184mm");
                    FillFromCargo(ASSAULTINVENTORIES, "MediumCalibreAmmo");
                } else if (ticks == 17) {
                    FillFromCargo(LAUNCHERSINVENTORIES, "Missile200mm");
                } else if (ticks == 19) {
                    //FillFromCargo(MISSILETURRETSINVENTORIES, "Missile200mm");
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
                    //terminalblocks.Clear();
                    //terminalblocks.AddRange(GATLINGTURRETS);
                    //BalanceInventories(terminalblocks, gatlingAmmo);
                    terminalblocks.Clear();
                    terminalblocks.AddRange(AUTOCANNONS);
                    BalanceInventories(terminalblocks, autocannonAmmo);
                } else if (ticks == 31) {
                    //terminalblocks.Clear();
                    //terminalblocks.AddRange(MISSILETURRETS);
                    //BalanceInventories(terminalblocks, missileAmmo);
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
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                    Setup();
                }
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case argTogglePB:
                    togglePB = !togglePB;
                    if (togglePB) {
                        foreach (IMyTextPanel block in LCDSSTATUS) { block.BackgroundColor = new Color(0, 255, 255); };
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    } else {
                        foreach (IMyTextPanel block in LCDSSTATUS) { block.BackgroundColor = new Color(0, 0, 0); };
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    break;
            }
        }

        bool GetBroadcastMessages() {
            bool received = false;
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    var igcMessage = BROADCASTLISTENER.AcceptMessage();
                    if (igcMessage.Data is MyTuple<string, bool>) {
                        var data = (MyTuple<string, bool>)igcMessage.Data;
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

        void ParseCockpitConfigData(IMyCockpit cockpit) {
            if (!cockpit.CustomData.Contains(sectionTag)) { cockpit.CustomData += $"[{sectionTag}]\n{cockpitPowerSurfaceKey}={cockpitPowerSurface}\n"; }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);
            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitPowerSurfaceKey).ToString())) {
                cockpitPowerSurface = myIni.Get(sectionTag, cockpitPowerSurfaceKey).ToInt32();
                POWERSURFACES.Add(cockpit.GetSurface(cockpitPowerSurface));
            }
        }

        void PowerManager() {
            if (!isControlled) { PowerFlow(terminalCurrentInput); } else { PowerFlow(terminalMaxRequiredInput); }//!IsPiloted()
        }

        void PowerFlow(float shipInput) {
            if (firstRun == 1 && hEngMaxOutput > 1) {
                registeredhEngMaxOutput = hEngMaxOutput;
                firstRun = 0;
            }
            float greenEnergy = solarMaxOutput + turbineMaxOutput + battsCurrentOutput;
            float solarEnergy = solarMaxOutput + turbineMaxOutput;
            if (shipInput < solarEnergy) {//TODO
                powerStatus = "Solar Power";
                foreach (IMyPowerProducer block in HENGINES) { block.Enabled = false; }
                foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                foreach (IMyBatteryBlock block in BATTERIES) {
                    block.ChargeMode = ChargeMode.Recharge;
                }
                /*
                if (chargeCount >= chargeDelay) {
                    BATTERIES.Sort((bat, bat2) => (bat.CurrentStoredPower > bat2.CurrentStoredPower ? 1 : -1));//asc
                    float powerDifference = solarEnergy - shipInput;
                    int battsToRecharge = (int)(powerDifference / batteryChargeDrain);
                    battsToRecharge = battsToRecharge == 0 ? 1 : battsToRecharge;
                    int count = 0;
                    foreach (IMyBatteryBlock block in BATTERIES) {
                        if (count < battsToRecharge) {
                            block.ChargeMode = ChargeMode.Recharge;
                        } else {
                            block.ChargeMode = ChargeMode.Auto;
                        }
                        count++;
                    }
                    chargeCount = 0;
                }
                chargeCount++;
                */
            } else if ((shipInput + battsCurrentInput) < greenEnergy) {
                //chargeCount = chargeDelay;
                powerStatus = "Green Power";
                foreach (IMyPowerProducer block in HENGINES) { block.Enabled = false; }
                foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                foreach (IMyBatteryBlock block in BATTERIES) {
                    block.ChargeMode = ChargeMode.Auto;
                }
            } else if ((shipInput + battsCurrentInput) < (registeredhEngMaxOutput + greenEnergy) && tankCapacityPercent > tankThresold) {
                //chargeCount = chargeDelay;
                powerStatus = "Hydrogen Power";
                foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                foreach (IMyBatteryBlock block in BATTERIES) {
                    block.ChargeMode = ChargeMode.Auto;
                }
            } else {
                //chargeCount = chargeDelay;
                powerStatus = "Full Steam";
                foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                foreach (IMyBatteryBlock block in BATTERIES) {
                    block.ChargeMode = ChargeMode.Auto;
                }
                foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
            }
        }

        void CalcPower() {
            GetPowInOut();
            GetBatteriesInOut();
            GetSolarsOutput();
            GetTurbinesOutput();
            GetHydrogenEnginesOutput();
            GetPercentTanksCapacity();
        }

        void GetPowInOut() {
            terminalCurrentInput = 0;
            terminalMaxRequiredInput = 0;
            foreach (IMyTerminalBlock block in TERMINALS) {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) {
                    if (block is IMyJumpDrive) {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.CurrentInputByType(electricityId);
                    } else {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.MaxRequiredInputByType(electricityId);
                    }
                }
            }
        }

        void GetSolarsOutput() {
            solarMaxOutput = 0;
            foreach (IMySolarPanel block in SOLARS) {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { solarMaxOutput += source.MaxOutputByType(electricityId); }
            }
        }

        void GetTurbinesOutput() {
            turbineMaxOutput = 0;
            foreach (IMyPowerProducer block in TURBINES) {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source)) {
                    //turbineCurrentOutput += source.CurrentOutputByType(electricityId);
                    turbineMaxOutput += source.MaxOutputByType(electricityId);
                }
            }
        }

        void GetHydrogenEnginesOutput() {
            hEngMaxOutput = 0;
            float maxOutput = 0;
            foreach (IMyPowerProducer block in HENGINES) {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { maxOutput += source.MaxOutputByType(electricityId); }
            }
            hEngMaxOutput = maxOutput;
        }

        void GetBatteriesInOut() {
            battsCurrentInput = 0;
            battsCurrentOutput = 0;
            foreach (IMyBatteryBlock block in BATTERIES) {
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) { battsCurrentInput += sink.CurrentInputByType(electricityId); }
                if (block.Components.TryGet<MyResourceSourceComponent>(out source)) { battsCurrentOutput += source.CurrentOutputByType(electricityId); }
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
            powerLog.Append("Current Input: ").Append(terminalCurrentInput.ToString("0.00")).Append(", ");
            powerLog.Append("Max Req. Input: ").Append(terminalMaxRequiredInput.ToString("0.00")).Append("\n");
            powerLog.Append("Solar Power: ").Append(solarMaxOutput.ToString("0.00")).Append("\n");
            if (turbineMaxOutput > 0) { powerLog.Append("Turbines Power: ").Append(turbineMaxOutput.ToString("0.00")).Append("\n"); }
            powerLog.Append("Batteries Curr. In: ").Append(battsCurrentInput.ToString("0.00")).Append(", ");
            powerLog.Append("Out: ").Append(battsCurrentOutput.ToString("0.00")).Append("\n");
            powerLog.Append("H2Tanks Fill: ").Append(tankCapacityPercent.ToString("0.00")).Append("%\n");
            powerLog.Append("H2Engine Max Out: ").Append(registeredhEngMaxOutput.ToString("0.00")).Append("\n");
            double num;
            oreDict.TryGetValue(iceOre, out num);
            powerLog.Append("Ice: ").Append(num.ToString("0.0")).Append(", ");
            ingotsDict.TryGetValue(uraniumIngot, out num);
            powerLog.Append("Uranium: ").Append(num.ToString("0.0")).Append("\n");
            ammosDict.TryGetValue(gatlingAmmo, out num);
            powerLog.Append("Gatling: ").Append(num.ToString("0.0")).Append(", ");
            ammosDict.TryGetValue(autocannonAmmo, out num);
            powerLog.Append("Autocannon: ").Append(num.ToString("0.0")).Append(", ");
            ammosDict.TryGetValue(missileAmmo, out num);
            powerLog.Append("Rockets: ").Append(num.ToString("0.0")).Append("\n");
            ammosDict.TryGetValue(assaultAmmo, out num);
            powerLog.Append("Assault: ").Append(num.ToString("0.0")).Append(", ");
            ammosDict.TryGetValue(artilleryAmmo, out num);
            powerLog.Append("Artillery: ").Append(num.ToString("0.0")).Append(", ");
            ammosDict.TryGetValue(railgunAmmo, out num);
            powerLog.Append("Sabot: ").Append(num.ToString("0.0")).Append(", ");
            ammosDict.TryGetValue(smallRailgunAmmo, out num);
            powerLog.Append("Small sabot: ").Append(num.ToString("0.0")).Append("\n");
        }

        void ReadInventoryInfos() {
            inventoriesPercentLog.Clear();
            terminalblocks.Clear();
            terminalblocks.AddRange(CONTAINERS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(GASGENERATORS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(REACTORS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(GATLINGS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(LAUNCHERS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(ASSAULT);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(ARTILLERY);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(AUTOCANNONS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(RAILGUNS);
            ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(SMALLRAILGUNS);
            ReadInventoriesFillPercent(terminalblocks);
            //terminalblocks.Clear();
            //terminalblocks.AddRange(GATLINGTURRETS);
            //ReadInventoriesFillPercent(terminalblocks);
            //terminalblocks.Clear();
            //terminalblocks.AddRange(MISSILETURRETS);
            //ReadInventoriesFillPercent(terminalblocks);
            terminalblocks.Clear();
            terminalblocks.AddRange(ASSAULTTURRETS);
            ReadInventoriesFillPercent(terminalblocks);
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
                if (count > 3) {
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
                refineriesInputLog.Append("\n" + block.CustomName.Replace(shipPrefix, "")).Append(" Input: \n");
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
                assemblersInputLog.Append("\n" + block.CustomName.Replace(shipPrefix, "")).Append(" Input: \n");
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

        void ReadInventoriesFillPercent(List<IMyTerminalBlock> blocksWithInventory) {
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
                    string blockName = block.CustomName.Replace(shipPrefix, "");
                    inventoriesPercentLog.Append(blockName + ": " + inventoriesPercent.ToString("0.0") + "% ");
                }
                count++;
                if (count > 2) {
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
                foreach (IMyUserControllableGun block in blocks) {
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
            foreach (var element in componentsDefBpQuota) {
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
                foreach (var ingots in ingotsNeeded) {
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
            foreach (var availableIngots in ingotsDict) {
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
                    foreach (var refinery in REFINERIES) {
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
                foreach (var availableIngots in ingotsDict) {
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
                        foreach (var refinery in REFINERIES) {
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

        void CompactMainCargos() {
            IMyCargoContainer mainCargo = null;
            int cargoIndex = 1000;
            foreach (IMyCargoContainer cargo in CONTAINERS) {
                int cargoNum;
                bool parsed = int.TryParse(cargo.CustomName.Replace(containersName, "").Trim(), out cargoNum);
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
                    bool parsed = int.TryParse(cargo.CustomName.Replace(containersName, "").Trim(), out cargoNum);
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
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TERMINALS, block => block.CustomName.Contains(terminalsName) && !(block is IMyPowerProducer) && !(block is IMySolarPanel) && !(block is IMyBatteryBlock) && !(block is IMyReactor) && !block.CustomName.Contains(hThrustersName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, block => block.CustomName.Contains(controllersName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains(cockpitsName));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains(hThrustersName) || block.CustomName.Contains(iThrustersName) || block.CustomName.Contains(aThrustersName));
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains(solarsName));
            TURBINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(TURBINES, block => block.CustomName.Contains(turbinesName));
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, block => block.CustomName.Contains(batteriesName));
            HTANKS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(HTANKS, block => block.CustomName.Contains(hTanksName));
            HENGINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(HENGINES, block => block.CustomName.Contains(hEnginesName));
            REFINERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(REFINERIES, block => block.CustomName.Contains(refineriesName));
            REFINERIESINVENTORIES.Clear();
            REFINERIESINVENTORIES.AddRange(REFINERIES.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSEMBLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(ASSEMBLERS, block => block.CustomName.Contains(assemblersName));
            //GATLINGTURRETS.Clear();
            //GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(GATLINGTURRETS, block => block.CustomName.Contains(gatlingTurretsName));
            //GATLINGTURRETSINVENTORIES.Clear();
            //GATLINGTURRETSINVENTORIES.AddRange(GATLINGTURRETS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            //MISSILETURRETS.Clear();
            //GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(MISSILETURRETS, block => block.CustomName.Contains(missileTurretsName));
            //MISSILETURRETSINVENTORIES.Clear();
            //MISSILETURRETSINVENTORIES.AddRange(MISSILETURRETS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSAULTTURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(ASSAULTTURRETS, block => block.CustomName.Contains(assaultTurretsName));
            ASSAULTTURRETSINVENTORIES.Clear();
            ASSAULTTURRETSINVENTORIES.AddRange(ASSAULTTURRETS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            GATLINGS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(GATLINGS, block => block.CustomName.Contains(gatlingsName));
            GATLINGSINVENTORIES.Clear();
            GATLINGSINVENTORIES.AddRange(GATLINGS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            LAUNCHERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(LAUNCHERS, block => block.CustomName.Contains(launchersName));
            LAUNCHERSINVENTORIES.Clear();
            LAUNCHERSINVENTORIES.AddRange(LAUNCHERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            RAILGUNS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(RAILGUNS, block => block.CustomName.Contains(railgunsName));
            RAILGUNSINVENTORIES.Clear();
            RAILGUNSINVENTORIES.AddRange(RAILGUNS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            SMALLRAILGUNS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(SMALLRAILGUNS, block => block.CustomName.Contains(smallRailgunsName));
            SMALLRAILGUNSINVENTORIES.Clear();
            SMALLRAILGUNSINVENTORIES.AddRange(SMALLRAILGUNS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ARTILLERY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ARTILLERY, block => block.CustomName.Contains(artilleryName));
            ARTILLERYINVENTORIES.Clear();
            ARTILLERYINVENTORIES.AddRange(ARTILLERY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            AUTOCANNONS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(AUTOCANNONS, block => block.CustomName.Contains(autocannonName));
            AUTOCANNONSINVENTORIES.Clear();
            AUTOCANNONSINVENTORIES.AddRange(AUTOCANNONS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            ASSAULT.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ASSAULT, block => block.CustomName.Contains(assaultName));
            ASSAULTINVENTORIES.Clear();
            ASSAULTINVENTORIES.AddRange(ASSAULT.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CONTAINERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CONTAINERS, block => block.CustomName.Contains(containersName));
            CARGOINVENTORIES.Clear();
            CARGOINVENTORIES.AddRange(CONTAINERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CONNECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(CONNECTORS, block => block.CustomName.Contains(connectorsName));
            CONNECTORSINVENTORIES.Clear();
            CONNECTORSINVENTORIES.AddRange(CONNECTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            GASGENERATORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(GASGENERATORS, block => block.CustomName.Contains(gasGeneratorsName));
            GASINVENTORIES.Clear();
            GASINVENTORIES.AddRange(GASGENERATORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            REACTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(REACTORS, block => block.CustomName.Contains(reactorsName));
            REACTORSINVENTORIES.Clear();
            REACTORSINVENTORIES.AddRange(REACTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            BLOCKSWITHINVENTORY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(BLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(shipPrefix));//&& block.IsSameConstructAs(Me)
            INVENTORIES.Clear();
            INVENTORIES.AddRange(BLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            LCDSSTATUS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCDSSTATUS, block => block.CustomName.Contains(lcdStatusName));
            POWERSURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdPowerName));
            foreach (IMyTextPanel panel in panels) { POWERSURFACES.Add(panel as IMyTextSurface); }
            INVENTORYSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdInventoryName));
            foreach (IMyTextPanel panel in panels) { INVENTORYSURFACES.Add(panel as IMyTextSurface); }
            COMPONENTSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdComponentsName));
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
