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
        readonly string lcdOreName = "[CRX] LCD Ores";
        readonly string cockpitsName = "[CRX] Controller Cockpit";
        readonly string hThrustersName = "[CRX] HThruster";
        readonly string iThrustersName = "[CRX] IonThruster";
        readonly string aThrustersName = "[CRX] AtmoThruster";
        readonly string refineriesName = "[CRX] Refinery";
        readonly string assemblersName = "[CRX] Assembler";
        readonly string containersName = "[CRX] Cargo";
        readonly string mainContainerName = "Main";
        readonly string gatlingTurretsName = "[CRX] Turret Gatling";
        readonly string missileTurretsName = "[CRX] Turret Missile";
        readonly string connectorsName = "[CRX] Connector";
        readonly string shipPrefix = "[CRX] ";

        const string argSunchaseToggle = "SunchaseToggle";
        const string argSunchaseOn = "SunchaseOn";
        const string argSunchaseOff = "SunchaseOff";
        const string argDeadMan = "DeadMan";
        const string argSetup = "Setup";
        const string argTogglePB = "TogglePB";
        const string argCompactInventories = "CompactInventories";
        const string argBalance = "Balance";
        const string argAutoProduction = "AutoProduction";
        const string argInventoryInfos = "InventoryInfos";

        readonly string sectionTag = "ManagerSettings";
        readonly string cockpitPowerSurfaceKey = "cockpitPowerSurface";

        int cockpitPowerSurface = 2;
        readonly bool findTheLight = false; // Search for the Sun in the shadows
        readonly double tankThresold = 20;
        readonly int lcdNameColumns = 25;
        readonly float solarPanelMaxRatio = 1;  // multiplier for modded panels
        readonly double minSpeed = 0.1;

        bool controlDampeners = true;
        bool sunChaserPaused = true;
        float shipSize = .16f;  //.04f  small blocks
        float maxPwr;
        double moveP = .01;
        double moveY = .01;
        float lastPwr;
        int step = 0;
        int next;
        string powerStatus;
        int firstRun = 1;
        bool doOnce = false;
        bool togglePB = false;
        int ticks = 0;

        float terminalCurrentInput;
        float terminalMaxRequiredInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        double tankCapacityPercent;
        float hEngMaxOutput;
        float solarMaxOutput;
        float turbineMaxOutput;
        float registeredhEngMaxOutput;
        double uraniumKg;

        public List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();
        public List<IMyPowerProducer> TURBINES = new List<IMyPowerProducer>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyGasTank> HTANKS = new List<IMyGasTank>();
        public List<IMyPowerProducer> HENGINES = new List<IMyPowerProducer>();
        public List<IMyGasGenerator> GASGENERATORS = new List<IMyGasGenerator>();
        public List<IMyReactor> REACTORS = new List<IMyReactor>();
        public List<IMyGyro> GYROS = new List<IMyGyro>();
        public List<IMyShipController> CONTROLLERS = new List<IMyShipController>();
        public List<IMyThrust> THRUSTERS = new List<IMyThrust>();
        public List<IMyTerminalBlock> TERMINALS = new List<IMyTerminalBlock>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();
        public List<IMyTerminalBlock> BLOCKSWITHINVENTORY = new List<IMyTerminalBlock>();
        public List<IMyInventory> INVENTORIES = new List<IMyInventory>();
        public List<IMyCargoContainer> CONTAINERS = new List<IMyCargoContainer>();
        public List<IMyRefinery> REFINERIES = new List<IMyRefinery>();
        public List<IMyAssembler> ASSEMBLERS = new List<IMyAssembler>();
        public List<IMyLargeGatlingTurret> GATLINGTURRETS = new List<IMyLargeGatlingTurret>();
        public List<IMyLargeMissileTurret> MISSILETURRETS = new List<IMyLargeMissileTurret>();
        public List<IMyTextPanel> LCDSSTATUS = new List<IMyTextPanel>();
        public List<IMyTextSurface> POWERSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> INVENTORYSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> COMPONENTSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> ORESURFACES = new List<IMyTextSurface>();
        public List<IMyInventory> GASINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> CARGOINVENTORIES = new List<IMyInventory>();
        public List<IMyShipConnector> CONNECTORS = new List<IMyShipConnector>();
        public List<IMyInventory> CONNECTORSINVENTORIES = new List<IMyInventory>();
        public List<IMyInventory> REACTORSINVENTORIES = new List<IMyInventory>();

        readonly MyIni myIni = new MyIni();

        readonly MyDefinitionId electricityId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
        readonly MyDefinitionId hydrogenId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
        //readonly MyDefinitionId oxygenId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen");

        readonly MyItemType iceOre = MyItemType.MakeOre("Ice");
        readonly MyItemType uraniumIngot = MyItemType.MakeIngot("Uranium");
        readonly MyItemType missileAmmo = MyItemType.MakeAmmo("Missile200mm");
        readonly MyItemType gatlingAmmo = MyItemType.MakeAmmo("NATO_25x184mm");

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

        public Dictionary<MyDefinitionId, int> oresDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer)
        {
            {MyItemType.MakeOre("Cobalt"),0},
            {MyItemType.MakeOre("Gold"),0},
            {MyItemType.MakeOre("Ice"),0},
            {MyItemType.MakeOre("Iron"),0},
            {MyItemType.MakeOre("Magnesium"),0},
            {MyItemType.MakeOre("Nickel"),0},
            {MyItemType.MakeOre("Organic"),0},
            {MyItemType.MakeOre("Platinum"),0},
            {MyItemType.MakeOre("Scrap"),0},
            {MyItemType.MakeOre("Silicon"),0},
            {MyItemType.MakeOre("Silver"),0},
            {MyItemType.MakeOre("Stone"),0},
            {MyItemType.MakeOre("Uranium"),0}
        };

        public Dictionary<MyDefinitionId, int> ingotsDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer)
        {
            {MyItemType.MakeIngot("Cobalt"),0},
            {MyItemType.MakeIngot("Gold"),0},
            {MyItemType.MakeIngot("Stone"),0},
            {MyItemType.MakeIngot("Iron"),0},
            {MyItemType.MakeIngot("Magnesium"),0},
            {MyItemType.MakeIngot("Nickel"),0},
            {MyItemType.MakeIngot("Scrap"),0},
            {MyItemType.MakeIngot("Platinum"),0},
            {MyItemType.MakeIngot("Silicon"),0},
            {MyItemType.MakeIngot("Silver"),0},
            {MyItemType.MakeIngot("Uranium"),0}
        };

        public Dictionary<MyDefinitionId, int> componentsDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer)
        {
            {MyItemType.MakeComponent("BulletproofGlass"),0},
            {MyItemType.MakeComponent("Canvas"),0},
            {MyItemType.MakeComponent("Computer"),0},
            {MyItemType.MakeComponent("Construction"),0},
            {MyItemType.MakeComponent("Detector"),0},
            {MyItemType.MakeComponent("Display"),0},
            {MyItemType.MakeComponent("Explosives"),0},
            {MyItemType.MakeComponent("Girder"),0},
            {MyItemType.MakeComponent("GravityGenerator"),0},
            {MyItemType.MakeComponent("InteriorPlate"),0},
            {MyItemType.MakeComponent("LargeTube"),0},
            {MyItemType.MakeComponent("Medical"),0},
            {MyItemType.MakeComponent("MetalGrid"),0},
            {MyItemType.MakeComponent("Motor"),0},
            {MyItemType.MakeComponent("PowerCell"),0},
            {MyItemType.MakeComponent("RadioCommunication"),0},
            {MyItemType.MakeComponent("Reactor"),0},
            {MyItemType.MakeComponent("SmallTube"),0},
            {MyItemType.MakeComponent("SolarCell"),0},
            {MyItemType.MakeComponent("SteelPlate"),0},
            {MyItemType.MakeComponent("Superconductor"),0},
            {MyItemType.MakeComponent("Thrust"),0},
            {MyItemType.MakeComponent("ZoneChip"),0}
        };

        public Dictionary<MyDefinitionId, int> ammosDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer)
        {
            {MyItemType.MakeIngot("NATO_25x184mm"),0},
            {MyItemType.MakeIngot("NATO_5p56x45mm"),0},
            {MyItemType.MakeIngot("Missile200mm"),0}
        };

        readonly Dictionary<string, int> componentsQuota = new Dictionary<string, int>()
        {
            {"Missile",         20} ,
            {"NATO_5",          100} ,
            {"NATO_25",         100} ,
            {"Glass",           100} ,
            {"Canvas",          4} ,
            {"Computer",        500} ,
            {"Construction",    1000} ,
            {"Detector",        10} ,
            {"Display",         200} ,
            {"Explosives",      10} ,
            {"Girder",          500} ,
            {"Gravity",         10} ,
            {"Interior",        1000} ,
            {"LargeTube",       100} ,
            {"Medical",         20} ,
            {"MetalGrid",       500} ,
            {"Motor",           1000} ,
            {"PowerCell",       100} ,
            {"Radio",           10} ,
            {"Reactor",         100} ,
            {"SmallTube",       500} ,
            {"SolarCell",       100} ,
            {"SteelPlate",      2000} ,
            {"Superconduct",    100} ,
            {"Thrust",          100}
        };

        readonly List<string> componentBlueprints = new List<string>()
        {
            "BulletproofGlass",
            "Computer",
            "Construction",
            "Detector",
            "Display",
            "Explosives",
            "Girder",
            "GravityGenerator",
            "InteriorPlate",
            "LargeTube",
            "Medical",
            "MetalGrid",
            "Motor",
            "PowerCell",
            "RadioCommunication",
            "Reactor",
            "SmallTube",
            "SolarCell",
            "SteelPlate",
            "Superconductor",
            "Thrust"
        };

        readonly List<string> oreList = new List<string>
        {
            "Uranium",
            "Silicon",
            "Silver",
            "Gold",
            "Platinum",
            "Magnesium",
            "Iron",
            "Nickel",
            "Cobalt",
            "Stone"
        };

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup()
        {
            GetBlocks();

            maxPwr = shipSize * solarPanelMaxRatio;
            if (SOLARS.Count > 0)
            {
                if (SOLARS[0] != null)
                {
                    lastPwr = SOLARS[0].MaxOutput;
                }
            }

            if (CONTROLLERS[0].CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                shipSize = .16f;
            }
            else
            {
                shipSize = .04f;
            }

            foreach (IMyCockpit cockpit in COCKPITS)
            {
                ParseCockpitConfigData(cockpit);
            }

            if (!sunChaserPaused)
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
            Echo($"SOLARS:{SOLARS.Count}");
            Echo($"TURBINES:{TURBINES.Count}");
            Echo($"BATTERIES:{BATTERIES.Count}");
            Echo($"HTANKS:{HTANKS.Count}");
            Echo($"HENGINES:{HENGINES.Count}");
            Echo($"GASGENERATORS:{GASGENERATORS.Count}");
            Echo($"REACTORS:{REACTORS.Count}");
            Echo($"GYROS:{GYROS.Count}");
            Echo($"CONTROLLERS:{CONTROLLERS.Count}");
            Echo($"THRUSTERS:{THRUSTERS.Count}");
            Echo($"COCKPITS:{COCKPITS.Count}");
            Echo($"REFINERIES:{REFINERIES.Count}");
            Echo($"ASSEMBLERS:{ASSEMBLERS.Count}");
            Echo($"CONTAINERS:{CONTAINERS.Count}");
            Echo($"GATLINGTURRETS:{GATLINGTURRETS.Count}");
            Echo($"MISSILETURRETS:{MISSILETURRETS.Count}");
            Echo($"LCDSSTATUS:{LCDSSTATUS.Count}");
            Echo($"POWERSURFACES:{POWERSURFACES.Count}");
            Echo($"INVENTORYSURFACES:{INVENTORYSURFACES.Count}");
            Echo($"COMPONENTSURFACES:{COMPONENTSURFACES.Count}");
            Echo($"ORESURFACES:{ORESURFACES.Count}");
            Echo($"TERMINALS:{TERMINALS.Count}");
            Echo($"BLOCKSWITHINVENTORY:{BLOCKSWITHINVENTORY.Count}");
            Echo($"INVENTORIES:{INVENTORIES.Count}");

            if (!string.IsNullOrEmpty(argument))
            {
                ProcessArgument(argument);
            }

            if (!IsInGravity() && !sunChaserPaused)
            {
                SunChase();
            }

            if (controlDampeners)
            {
                DeadMan();
            }

            CalcPower();
            PowerManager();
            ReadPowerInfos();
            WritePowerInfo();

            if (ticks == 1)
            {
                MoveProductionOutputsToMainInventory();
                MoveItemsFromConnectors();
            }
            else if (ticks == 5)
            {
                CompactInventory();
                CompactMainCargos();//TODO
            }
            else if (ticks == 10)
            {
                FillHidrogenGenerators();
                FillReactors();
            }
            else if (ticks == 15)
            {
                BalanceAmmo();
            }
            else if (ticks == 20)
            {
                BalanceIce();
            }
            else if (ticks == 25)
            {
                BalanceUranium();
            }
            else if (ticks == 30)
            {
                AutoAssemblers();
            }
            else if (ticks == 35)
            {
                AutoRefineries();
            }
            else if (ticks >= 40)
            {
                ReadInventoryInfos();
                WriteInventoryInfo();
                WriteOreComponentsInfo();
                ticks = 0;
            }
            ticks++;
        }

        void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case argSunchaseToggle:
                    sunChaserPaused = !sunChaserPaused;
                    if (!sunChaserPaused)
                    {
                        Me.CustomData = "GyroStabilize=true";
                    }
                    else
                    {
                        foreach (IMyGyro block in GYROS) { block.GyroOverride = false; };
                        Me.CustomData = "GyroStabilize=false";
                    }
                    break;
                case argSunchaseOff:
                    sunChaserPaused = true;
                    foreach (IMyGyro block in GYROS) { block.GyroOverride = false; };
                    Me.CustomData = "GyroStabilize=false";
                    break;
                case argSunchaseOn:
                    sunChaserPaused = false;
                    Me.CustomData = "GyroStabilize=true";
                    break;
                case argDeadMan: controlDampeners = !controlDampeners; break;
                case argSetup: Setup(); break;
                case argTogglePB: togglePB = !togglePB; if (togglePB) { foreach (IMyTextPanel block in LCDSSTATUS) { block.BackgroundColor = new Color(0, 255, 255); }; Runtime.UpdateFrequency = UpdateFrequency.Update10; } else { foreach (IMyTextPanel block in LCDSSTATUS) { block.BackgroundColor = new Color(0, 0, 0); }; Runtime.UpdateFrequency = UpdateFrequency.None; } break;
                case argCompactInventories: MoveProductionOutputsToMainInventory(); CompactInventory(); break;
                case argBalance: BalanceAmmo(); BalanceUranium(); BalanceIce(); break;
                case argAutoProduction: AutoAssemblers(); AutoRefineries(); break;
                case argInventoryInfos: ReadInventoryInfos(); WriteInventoryInfo(); WriteOreComponentsInfo(); break;
            }
        }

        void ParseCockpitConfigData(IMyCockpit cockpit)
        {
            if (!cockpit.CustomData.Contains(sectionTag))
            {
                cockpit.CustomData += $"[{ sectionTag}]\n{cockpitPowerSurfaceKey}={cockpitPowerSurface}\n";
            }
            MyIniParseResult result;
            myIni.TryParse(cockpit.CustomData, sectionTag, out result);

            if (!string.IsNullOrEmpty(myIni.Get(sectionTag, cockpitPowerSurfaceKey).ToString()))
            {
                cockpitPowerSurface = myIni.Get(sectionTag, cockpitPowerSurfaceKey).ToInt32();

                POWERSURFACES.Add(cockpit.GetSurface(cockpitPowerSurface));
            }
        }

        void PowerManager()
        {
            if (!IsPiloted())
            {
                PowerFlow(terminalCurrentInput + battsCurrentInput);
            }
            else
            {
                PowerFlow(terminalMaxRequiredInput + battsCurrentInput);
            }
        }

        void PowerFlow(float shipInput)
        {
            if (firstRun == 1 && hEngMaxOutput > 1)
            {
                registeredhEngMaxOutput = hEngMaxOutput;
                firstRun = 0;
            }
            float battThresold = BATTERIES[0].MaxStoredPower / 20;
            float greenEnergy = solarMaxOutput + turbineMaxOutput + battsCurrentOutput;
            if (shipInput < greenEnergy)
            {
                powerStatus = "Green Power";
                foreach (IMyPowerProducer block in HENGINES)
                {
                    block.Enabled = false;
                }
                foreach (IMyReactor block in REACTORS)
                {
                    block.Enabled = false;
                }
                foreach (IMyBatteryBlock block in BATTERIES)
                {
                    block.Enabled = true;
                    if (block.CurrentStoredPower + battThresold < block.MaxStoredPower)
                    {
                        block.ChargeMode = ChargeMode.Recharge;
                    }
                    else
                    {
                        block.ChargeMode = ChargeMode.Auto;
                    }
                }
            }
            else if (shipInput < (registeredhEngMaxOutput + greenEnergy) && tankCapacityPercent > tankThresold)
            {
                powerStatus = "Hydrogen Power";
                foreach (IMyPowerProducer block in HENGINES)
                {
                    block.Enabled = true;
                }
                foreach (IMyReactor block in REACTORS)
                {
                    block.Enabled = false;
                }
                foreach (IMyBatteryBlock block in BATTERIES)
                {
                    block.Enabled = true;
                    if (block.CurrentStoredPower + battThresold < block.MaxStoredPower)
                    {
                        block.ChargeMode = ChargeMode.Recharge;
                    }
                    else
                    {
                        block.ChargeMode = ChargeMode.Auto;
                    }
                }
            }
            else
            {
                powerStatus = "Full Steam";
                foreach (IMyPowerProducer block in HENGINES)
                {
                    block.Enabled = true;
                }
                foreach (IMyBatteryBlock block in BATTERIES)
                {
                    block.Enabled = true;
                    block.ChargeMode = ChargeMode.Auto;
                }
                foreach (IMyReactor block in REACTORS)
                {
                    block.Enabled = true;
                }
            }
        }

        void SunChase()
        {
            if (!SOLARS[0].IsFunctional || !SOLARS[0].Enabled || !SOLARS[0].IsWorking)
            {
                SetGyroRotation(SOLARS[0], GYROS, 0, 0, 0);
                return;
            }
            if (GetSkipTrigger())
            {
                SetGyroRotation(SOLARS[0], GYROS, 0, 0, 0);
                return;
            }
            double P = 0; double Y = 0;
            float Pwr = SOLARS[0].MaxOutput;
            if (Pwr < maxPwr * .02)
            {
                if (findTheLight)
                {
                    SetGyroRotation(SOLARS[0], GYROS, .1, .4);
                }
                else
                {
                    SetGyroRotation(SOLARS[0], GYROS, 0, 0, 0);
                }
                return;
            }
            int D = Math.Sign(Pwr - lastPwr);
            double V = 2 * maxPwr / Pwr;
            if (Pwr > maxPwr * .98)
            {
                if (step > 0)
                {
                    step = 0;
                    SetGyroRotation(SOLARS[0], GYROS, 0, 0, 0);
                }
                return;
            }
            switch (step)
            {
                case 0:
                    next = 0;
                    step++;
                    break;

                case 1:
                    if (D < 0)
                    {
                        moveP = -moveP;
                        next++;
                        if (next > 2)
                        {
                            step++; next = 0;
                        }
                    }
                    P = moveP;
                    break;

                case 2:
                    if (D < 0)
                    {
                        moveY = -moveY;
                        next++;
                        if (next > 2)
                        {
                            SetGyroRotation(SOLARS[0], GYROS, 0, 0, 0); step = 0; next = 0;
                        }
                    }
                    Y = moveY;
                    break;
            }
            SetGyroRotation(SOLARS[0], GYROS, P * V, Y * V, 0);
            lastPwr = Pwr;
        }

        void SetGyroRotation(IMyTerminalBlock Master, List<IMyGyro> GYROS, double Pitch = 0, double Yaw = 0, double Roll = 0)
        {
            Vector3D R = Vector3D.TransformNormal(new Vector3D(Pitch, Yaw, Roll), Master.WorldMatrix);
            Vector3D T;
            bool A = !(Pitch == 0 && Yaw == 0 && Roll == 0);
            foreach (IMyGyro G in GYROS)
            {
                T = Vector3D.TransformNormal(R, Matrix.Transpose(G.WorldMatrix));
                G.Pitch = (float)T.X;
                G.Yaw = (float)T.Y;
                G.Roll = (float)T.Z;
                G.GyroOverride = A;
            }
        }

        bool GetSkipTrigger()
        {
            bool piloted = false;
            foreach (IMyShipController block in CONTROLLERS)
            {
                if (block.CanControlShip)
                {
                    piloted = piloted || block.IsUnderControl;
                    if (block is IMyRemoteControl)
                    {
                        piloted = piloted || (block as IMyRemoteControl).IsAutoPilotEnabled;
                    }
                }
            }
            return piloted || sunChaserPaused;
        }

        bool IsPiloted()
        {
            bool isPiloted = false;
            foreach (IMyShipController block in CONTROLLERS)
            {
                if (block.IsFunctional && block.IsUnderControl && block.CanControlShip && block.ControlThrusters)
                {
                    isPiloted = true;
                    break;
                }
                if (block is IMyRemoteControl)
                {
                    if ((block as IMyRemoteControl).IsAutoPilotEnabled)
                    {
                        isPiloted = true;
                        break;
                    }
                }
            }
            return isPiloted;
        }

        void DeadMan()
        {
            bool undercontrol = IsPiloted();
            if (!undercontrol)
            {
                IMyShipController cntrllr = null;
                foreach (IMyShipController block in CONTROLLERS)
                {
                    if (block.CanControlShip)
                    {
                        cntrllr = block;
                        break;
                    }
                }
                if (cntrllr != null)
                {
                    double speed = cntrllr.GetShipSpeed();
                    if (speed > minSpeed)
                    {
                        foreach (IMyThrust thrst in THRUSTERS)
                        {
                            thrst.Enabled = true;
                        }
                        cntrllr.DampenersOverride = true;
                    }
                    else
                    {
                        if (!doOnce)
                        {
                            foreach (IMyThrust thrst in THRUSTERS)
                            {
                                thrst.Enabled = false;
                            }
                            doOnce = true;
                        }
                    }
                }
            }
            else
            {
                if (doOnce)
                {
                    foreach (IMyThrust thrst in THRUSTERS)
                    {
                        thrst.Enabled = true;
                    }
                    doOnce = false;
                }
            }
        }

        bool IsInGravity()
        {
            IMyShipController cntrllr = CONTROLLERS[0];
            Vector3D grav = cntrllr.GetNaturalGravity();
            if (Vector3D.IsZero(grav))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void CalcPower()
        {
            GetPowInOut();
            GetBatteriesInOut();
            GetSolarsOutput();
            GetTurbinesOutput();
            GetHydrogenEnginesOutput();
            GetPercentTanksCapacity();
            GetReactorsOutput();
        }

        void GetPowInOut()
        {
            terminalCurrentInput = 0;
            terminalMaxRequiredInput = 0;
            foreach (IMyTerminalBlock block in TERMINALS)
            {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink))
                {
                    if (block is IMyJumpDrive)
                    {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.CurrentInputByType(electricityId);
                    }
                    else
                    {
                        terminalCurrentInput += sink.CurrentInputByType(electricityId);
                        terminalMaxRequiredInput += sink.MaxRequiredInputByType(electricityId);
                    }
                }
            }
        }

        void GetSolarsOutput()
        {
            solarMaxOutput = 0;
            foreach (IMySolarPanel block in SOLARS)
            {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source))
                {
                    solarMaxOutput += source.MaxOutputByType(electricityId);
                }
            }
        }

        void GetTurbinesOutput()
        {
            turbineMaxOutput = 0;
            foreach (IMyPowerProducer block in TURBINES)
            {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source))
                {
                    //turbineCurrentOutput += source.CurrentOutputByType(electricityId);
                    turbineMaxOutput += source.MaxOutputByType(electricityId);
                }
            }
        }

        void GetHydrogenEnginesOutput()
        {
            hEngMaxOutput = 0;
            float maxOutput = 0;
            foreach (IMyPowerProducer block in HENGINES)
            {
                if (!block.IsWorking) continue;
                if (block.Components.TryGet<MyResourceSourceComponent>(out source))
                {
                    maxOutput += source.MaxOutputByType(electricityId);
                }
            }
            hEngMaxOutput = maxOutput;
        }

        void GetReactorsOutput()
        {
            uraniumKg = 0;
            foreach (IMyReactor reactor in REACTORS)
            {
                uraniumKg += (double)reactor.GetInventory(0).CurrentMass;
            }
        }

        void GetBatteriesInOut()
        {
            battsCurrentInput = 0;
            battsCurrentOutput = 0;
            foreach (IMyBatteryBlock block in BATTERIES)
            {
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink))
                {
                    battsCurrentInput += sink.CurrentInputByType(electricityId);
                }
                if (block.Components.TryGet<MyResourceSourceComponent>(out source))
                {
                    battsCurrentOutput += source.CurrentOutputByType(electricityId);
                }
            }
        }

        void GetPercentTanksCapacity()
        {
            tankCapacityPercent = 0;
            double totCapacity = 0;
            double totFill = 0;
            foreach (IMyGasTank tank in HTANKS)
            {
                if (tank.Components.TryGet<MyResourceSinkComponent>(out sink))
                {
                    ListReader<MyDefinitionId> definitions = sink.AcceptedResources;
                    for (int y = 0; y < definitions.Count; y++)
                    {
                        if (string.Compare(definitions[y].SubtypeId.ToString(), hydrogenId.SubtypeId.ToString(), true) == 0)
                        {
                            double capacity = (double)tank.Capacity;
                            totCapacity += capacity;
                            totFill += capacity * tank.FilledRatio;
                            break;
                        }
                    }
                }
            }
            if (totFill > 0 && totCapacity > 0)
            {
                tankCapacityPercent = (totFill / totCapacity) * 100;
            }
        }

        void ReadPowerInfos()
        {
            powerLog.Clear();

            powerLog.Append("Status: ").Append(powerStatus).Append("\n");
            if (sunChaserPaused)
            {
                powerLog.Append("SunChase OFF\n");
            }
            else
            {
                powerLog.Append("SunChase ON\n");
            }
            if (controlDampeners)
            {
                powerLog.Append("DeadMan ON\n");
            }
            else
            {
                powerLog.Append("DeadMan OFF\n");
            }
            powerLog.Append("Current Input: ").Append(terminalCurrentInput.ToString("0.00")).Append("\n");
            powerLog.Append("Max Req. Input: ").Append(terminalMaxRequiredInput.ToString("0.00")).Append("\n");
            powerLog.Append("Solar Power: ").Append(solarMaxOutput.ToString("0.00")).Append("\n");
            if (turbineMaxOutput > 0)
            {
                powerLog.Append("Turbines Power: ").Append(turbineMaxOutput.ToString("0.00")).Append("\n");
            }
            powerLog.Append("Batteries Curr. In: ").Append(battsCurrentInput.ToString("0.00")).Append("\n");
            powerLog.Append("Batteries Curr. Out: ").Append(battsCurrentOutput.ToString("0.00")).Append("\n");
            powerLog.Append("H2Tanks Fill: ").Append(tankCapacityPercent.ToString("0.00")).Append("%\n");
            powerLog.Append("H2Engine Max Out: ").Append(registeredhEngMaxOutput.ToString("0.00")).Append("\n");
            powerLog.Append("Uranium Kg: ").Append(uraniumKg.ToString("0.00")).Append("\n");
        }

        void ReadInventoryInfos()
        {
            ReadInventoriesFillPercent(BLOCKSWITHINVENTORY);
            ReadAllItems(INVENTORIES);
            ReadAssemblersItems(ASSEMBLERS);
            ReadRefineriesItems(REFINERIES);
        }

        void ReadAllItems(List<IMyInventory> inventories)
        {
            ResetComponentsDict();
            ResetIngotDict();
            ResetOresDict();
            ResetAmmosDict();

            foreach (IMyInventory inventory in inventories)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    if (item.Type.GetItemInfo().IsOre)
                    {
                        int num;
                        if (oresDict.TryGetValue(item.Type, out num))
                        {
                            oresDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsIngot)
                    {
                        int num;
                        if (ingotsDict.TryGetValue(item.Type, out num))
                        {
                            ingotsDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsComponent)
                    {
                        int num;
                        if (componentsDict.TryGetValue(item.Type, out num))
                        {
                            componentsDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsAmmo)
                    {
                        int num;
                        if (ammosDict.TryGetValue(item.Type, out num))
                        {
                            ammosDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                }
            }
            ammosLog.Clear();
            oresLog.Clear();
            ingotsLog.Clear();
            componentsLog.Clear();
            foreach (KeyValuePair<MyDefinitionId, int> entry in ammosDict)
            {
                ammosLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
            }
            foreach (KeyValuePair<MyDefinitionId, int> entry in oresDict)
            {
                oresLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
            }
            foreach (KeyValuePair<MyDefinitionId, int> entry in ingotsDict)
            {
                ingotsLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
            }
            foreach (KeyValuePair<MyDefinitionId, int> entry in componentsDict)
            {
                componentsLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
            }
        }

        void ReadRefineriesItems(List<IMyRefinery> refineries)
        {
            refineriesInputLog.Clear();

            foreach (IMyRefinery block in refineries)
            {
                ResetIngotDict();
                ResetOresDict();

                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.InputInventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    if (item.Type.GetItemInfo().IsOre)
                    {
                        int num;
                        if (oresDict.TryGetValue(item.Type, out num))
                        {
                            oresDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsIngot)
                    {
                        int num;
                        if (ingotsDict.TryGetValue(item.Type, out num))
                        {
                            ingotsDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                }

                refineriesInputLog.Append(block.CustomName.Replace(shipPrefix, "")).Append(" Input\n");
                foreach (KeyValuePair<MyDefinitionId, int> entry in oresDict)
                {
                    if (entry.Value != 0)
                    {
                        refineriesInputLog.Append($"{entry.Key.SubtypeId} Ore: ").Append($"{entry.Value}\n");
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, int> entry in ingotsDict)
                {
                    if (entry.Value != 0)
                    {
                        refineriesInputLog.Append($"{entry.Key.SubtypeId} Ingot: ").Append($"{entry.Value}\n");
                    }
                }
            }
        }

        void ReadAssemblersItems(List<IMyAssembler> assemblers)
        {
            assemblersInputLog.Clear();

            foreach (IMyAssembler block in assemblers)
            {
                ResetComponentsDict();
                ResetIngotDict();
                ResetAmmosDict();

                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.InputInventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    if (item.Type.GetItemInfo().IsComponent)
                    {
                        int num;
                        if (componentsDict.TryGetValue(item.Type, out num))
                        {
                            componentsDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsIngot)
                    {
                        int num;
                        if (ingotsDict.TryGetValue(item.Type, out num))
                        {
                            ingotsDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                    else if (item.Type.GetItemInfo().IsAmmo)
                    {
                        int num;
                        if (ammosDict.TryGetValue(item.Type, out num))
                        {
                            ammosDict[item.Type] = num + item.Amount.ToIntSafe();
                        }
                    }
                }

                assemblersInputLog.Append(block.CustomName.Replace(shipPrefix, "")).Append(" Input\n");
                foreach (KeyValuePair<MyDefinitionId, int> entry in ammosDict)
                {
                    if (entry.Value != 0)
                    {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, int> entry in componentsDict)
                {
                    if (entry.Value != 0)
                    {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
                    }
                }
                foreach (KeyValuePair<MyDefinitionId, int> entry in ingotsDict)
                {
                    if (entry.Value != 0)
                    {
                        assemblersInputLog.Append($"{entry.Key.SubtypeId}: ").Append($"{entry.Value}\n");
                    }
                }
            }
        }

        void ReadInventoriesFillPercent(List<IMyTerminalBlock> blocksWithInventory)
        {
            inventoriesPercentLog.Clear();
            foreach (IMyTerminalBlock block in blocksWithInventory)
            {
                if (!(block is IMyShipWelder) && !(block is IMyShipDrill) && !(block is IMyShipGrinder)
                    && !(block is IMyRefinery) && !(block is IMyAssembler) && !(block is IMyGasTank)
                    && !(block is IMyShipConnector) && !(block is IMyCockpit) && !(block is IMyCryoChamber))
                {
                    List<IMyInventory> inventories = new List<IMyInventory>();
                    inventories.AddRange(Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory));
                    foreach (IMyInventory inventory in inventories)
                    {
                        double inventoriesPercent = 0;
                        double currentVolume = (double)inventory.CurrentVolume;
                        double maxVolume = (double)inventory.MaxVolume;
                        if (currentVolume != 0 && maxVolume != 0)
                        {
                            inventoriesPercent = currentVolume / maxVolume * 100;
                        }
                        string blockName = block.CustomName.Replace(shipPrefix, "");
                        if (blockName.Length >= lcdNameColumns - 1)
                        {
                            blockName.Substring(0, lcdNameColumns - 1);
                        }

                        inventoriesPercentLog.Append($"{blockName}: ").Append($"{inventoriesPercent}%\n");
                    }
                }
            }
        }

        void WritePowerInfo()
        {
            foreach (IMyTextSurface surface in POWERSURFACES)
            {
                StringBuilder text = new StringBuilder();
                text.Append(powerLog.ToString());
                surface.WriteText(text);
            }
        }

        void WriteInventoryInfo()
        {
            foreach (IMyTextSurface surface in INVENTORYSURFACES)
            {
                StringBuilder text = new StringBuilder();
                text.Append(refineriesInputLog.ToString());
                text.Append(assemblersInputLog.ToString());
                text.Append(inventoriesPercentLog.ToString());
                surface.WriteText(text);
            }
        }

        void WriteOreComponentsInfo()
        {
            foreach (IMyTextSurface surface in ORESURFACES)
            {
                StringBuilder text = new StringBuilder();
                text.Append("ORE: \n");
                text.Append(oresLog.ToString());
                text.Append("INGOTS: \n");
                text.Append(ingotsLog.ToString());
                surface.WriteText(text);
            }

            foreach (IMyTextSurface surface in COMPONENTSURFACES)
            {
                StringBuilder text = new StringBuilder();
                text.Append(ammosLog.ToString());
                text.Append(componentsLog.ToString());
                surface.WriteText(text);
            }
        }

        void MoveProductionOutputsToMainInventory()
        {
            foreach (IMyRefinery block in REFINERIES)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.OutputInventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES)
                    {
                        if (block.OutputInventory.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type))
                        {
                            block.OutputInventory.TransferItemTo(cargoInv, item);
                            break;
                        }
                    }
                }
            }
            foreach (IMyAssembler block in ASSEMBLERS)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.OutputInventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES)
                    {
                        if (block.OutputInventory.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type))
                        {
                            block.OutputInventory.TransferItemTo(cargoInv, item);
                            break;
                        }
                    }
                }
            }
        }

        void CompactInventory()
        {
            foreach (var inventory in INVENTORIES)
            {
                for (var i = inventory.ItemCount - 1; i > 0; i--)
                {
                    inventory.TransferItemTo(inventory, i, stackIfPossible: true);
                }
            }
        }

        void BalanceUranium()
        {
            List<IMyInventory> reactorsInventories = new List<IMyInventory>();
            int totUranium = 0;
            foreach (IMyReactor block in REACTORS)
            {
                reactorsInventories.Add(block.GetInventory());
                totUranium += block.GetInventory().GetItemAmount(uraniumIngot).ToIntSafe();
            }
            int dividedAmount = 0;
            int k = 0;
            if (REACTORS.Count > 0)
            {
                dividedAmount = totUranium / REACTORS.Count;
                reactorsInventories.Sort(CompareReactorsInventories);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(reactorsInventories);
                reversedInventories.Reverse();
                for (int i = 0; i < reactorsInventories.Count && k < reversedInventories.Count - i;)
                {
                    int currentAmount = reactorsInventories[i].GetItemAmount(uraniumIngot).ToIntSafe();
                    int availableAmount = reversedInventories[k].GetItemAmount(uraniumIngot).ToIntSafe();
                    if (currentAmount < dividedAmount + 1)
                    {
                        if (availableAmount <= 2 * dividedAmount + 1 - currentAmount)
                        {
                            reactorsInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(uraniumIngot) ?? default(MyInventoryItem), availableAmount - dividedAmount);
                            k++;
                        }
                        else
                        {
                            reactorsInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(uraniumIngot) ?? default(MyInventoryItem), dividedAmount + 1 - currentAmount);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        void BalanceIce()
        {
            List<IMyInventory> gasInventories = new List<IMyInventory>();
            int totIce = 0;

            foreach (IMyGasGenerator block in GASGENERATORS)
            {
                gasInventories.Add(block.GetInventory());
                totIce += block.GetInventory().GetItemAmount(iceOre).ToIntSafe();
            }

            int dividedAmount = 0;
            int k = 0;
            if (GASGENERATORS.Count > 0)
            {

                dividedAmount = totIce / GASGENERATORS.Count;
                gasInventories.Sort(CompareGasInventories);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(gasInventories);
                reversedInventories.Reverse();

                for (int i = 0; i < gasInventories.Count && k < reversedInventories.Count - i;)
                {
                    int currentAmount = gasInventories[i].GetItemAmount(iceOre).ToIntSafe();
                    int availableAmount = reversedInventories[k].GetItemAmount(iceOre).ToIntSafe();
                    if (currentAmount < dividedAmount + 1)
                    {
                        if (availableAmount <= 2 * dividedAmount + 1 - currentAmount)
                        {
                            gasInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(iceOre) ?? default(MyInventoryItem), availableAmount - dividedAmount);
                            k++;
                        }
                        else
                        {
                            gasInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(iceOre) ?? default(MyInventoryItem), dividedAmount + 1 - currentAmount);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            gasInventories.Clear();
            totIce = 0;
            foreach (IMyPowerProducer block in HENGINES)
            {
                if (block.HasInventory)
                {

                    gasInventories.Add(block.GetInventory());

                    totIce += block.GetInventory().GetItemAmount(iceOre).ToIntSafe();

                }
            }

            dividedAmount = 0;
            k = 0;
            if (HENGINES.Count > 0)
            {

                dividedAmount = totIce / HENGINES.Count;
                gasInventories.Sort(CompareGasInventories);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(gasInventories);
                reversedInventories.Reverse();

                for (int i = 0; i < gasInventories.Count && k < reversedInventories.Count - i;)
                {
                    int currentAmount = gasInventories[i].GetItemAmount(iceOre).ToIntSafe();
                    int availableAmount = reversedInventories[k].GetItemAmount(iceOre).ToIntSafe();
                    if (currentAmount < dividedAmount + 1)
                    {
                        if (availableAmount <= 2 * dividedAmount + 1 - currentAmount)
                        {
                            gasInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(iceOre) ?? default(MyInventoryItem), availableAmount - dividedAmount);
                            k++;
                        }
                        else
                        {
                            gasInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(iceOre) ?? default(MyInventoryItem), dividedAmount + 1 - currentAmount);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }

            }
        }

        void BalanceAmmo()
        {
            List<IMyInventory> gatlingInventories = new List<IMyInventory>();
            List<IMyInventory> missileInventories = new List<IMyInventory>();
            int totGatlingAmmo = 0;
            int totMissileAmmo = 0;
            foreach (IMyLargeGatlingTurret gatlingsTurret in GATLINGTURRETS)
            {
                gatlingInventories.Add(gatlingsTurret.GetInventory());
                totGatlingAmmo += gatlingsTurret.GetInventory().GetItemAmount(gatlingAmmo).ToIntSafe();
            }
            foreach (IMyLargeMissileTurret missileTurret in MISSILETURRETS)
            {
                missileInventories.Add(missileTurret.GetInventory());
                totMissileAmmo += missileTurret.GetInventory().GetItemAmount(missileAmmo).ToIntSafe();
            }

            int dividedAmmoAmount = 0;
            int k = 0;
            if (GATLINGTURRETS.Count > 0)
            {
                dividedAmmoAmount = totGatlingAmmo / GATLINGTURRETS.Count;
                gatlingInventories.Sort(CompareGatlingsInventories);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(gatlingInventories);
                reversedInventories.Reverse();
                for (int i = 0; i < gatlingInventories.Count && k < reversedInventories.Count - i;)
                {
                    int currentAmmoAmount = gatlingInventories[i].GetItemAmount(gatlingAmmo).ToIntSafe();
                    int availableAmmoAmount = reversedInventories[k].GetItemAmount(gatlingAmmo).ToIntSafe();
                    if (currentAmmoAmount < dividedAmmoAmount + 1)
                    {
                        if (availableAmmoAmount <= 2 * dividedAmmoAmount + 1 - currentAmmoAmount)
                        {
                            gatlingInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(gatlingAmmo) ?? default(MyInventoryItem), availableAmmoAmount - dividedAmmoAmount);
                            k++;
                        }
                        else
                        {
                            gatlingInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(gatlingAmmo) ?? default(MyInventoryItem), dividedAmmoAmount + 1 - currentAmmoAmount);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            dividedAmmoAmount = 0;
            k = 0;
            if (MISSILETURRETS.Count > 0)
            {
                dividedAmmoAmount = totMissileAmmo / MISSILETURRETS.Count;
                missileInventories.Sort(CompareMissileInventories);
                List<IMyInventory> reversedInventories = new List<IMyInventory>(missileInventories);
                reversedInventories.Reverse();
                for (int i = 0; i < missileInventories.Count && k < reversedInventories.Count - i;)
                {
                    int currentAmmoAmount = missileInventories[i].GetItemAmount(missileAmmo).ToIntSafe();
                    int availableAmmoAmount = reversedInventories[k].GetItemAmount(missileAmmo).ToIntSafe();
                    if (currentAmmoAmount < dividedAmmoAmount + 1)
                    {
                        if (availableAmmoAmount <= 2 * dividedAmmoAmount + 1 - currentAmmoAmount)
                        {
                            missileInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(missileAmmo) ?? default(MyInventoryItem), availableAmmoAmount - dividedAmmoAmount);
                            k++;
                        }
                        else
                        {
                            missileInventories[i].TransferItemFrom(reversedInventories[k], reversedInventories[k].FindItem(missileAmmo) ?? default(MyInventoryItem), dividedAmmoAmount + 1 - currentAmmoAmount);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        int CompareGasInventories(IMyInventory firstInventory, IMyInventory secondInventory)
        {
            if (firstInventory.GetItemAmount(iceOre).ToIntSafe() > secondInventory.GetItemAmount(iceOre).ToIntSafe())
            { return 1; }
            else { return -1; }
        }

        int CompareReactorsInventories(IMyInventory firstInventory, IMyInventory secondInventory)
        {
            if (firstInventory.GetItemAmount(uraniumIngot).ToIntSafe() > secondInventory.GetItemAmount(uraniumIngot).ToIntSafe())
            { return 1; }
            else { return -1; }
        }

        int CompareGatlingsInventories(IMyInventory firstInventory, IMyInventory secondInventory)
        {
            if (firstInventory.GetItemAmount(gatlingAmmo).ToIntSafe() > secondInventory.GetItemAmount(gatlingAmmo).ToIntSafe())
            { return 1; }
            else { return -1; }
        }

        int CompareMissileInventories(IMyInventory firstInventory, IMyInventory secondInventory)
        {
            if (firstInventory.GetItemAmount(missileAmmo).ToIntSafe() > secondInventory.GetItemAmount(missileAmmo).ToIntSafe())
            { return 1; }
            else { return -1; }
        }

        void AutoAssemblers()
        {
            foreach (IMyInventory mainInv in CARGOINVENTORIES)
            {
                List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                mainInv.GetItems(cargoItems);
                foreach (var element in componentsQuota)
                {
                    string component = element.Key;
                    int componentAmountRequired = element.Value;
                    int tempIndex = componentsQuota.Keys.ToList().IndexOf(component);
                    string componentBpSubtype = componentBlueprints[tempIndex];
                    MyDefinitionId blueprintDef = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + componentBpSubtype);

                    bool enoughOfThisComponent = false;
                    foreach (MyInventoryItem item in cargoItems)
                    {
                        string itemType = item.Type.ToString();
                        if (itemType.Contains(component))
                        {
                            if (item.Amount > componentAmountRequired) // First look if we have enough of the component already
                            {
                                enoughOfThisComponent = true;
                                break; // Enough of this component, go to next
                            }
                            else { componentAmountRequired -= (int)item.Amount; }// Not enough of this component : modify production target
                            break; // We found the component so we can stop the loop on items
                        }
                    }
                    if (enoughOfThisComponent) continue;
                    foreach (IMyAssembler assembler in ASSEMBLERS)// Now check if the component is already in assembler queue, or add it
                    {
                        List<MyProductionItem> AssemblerQueue = new List<MyProductionItem>();
                        assembler.GetQueue(AssemblerQueue);
                        int nAlreadyQueued = 0;
                        foreach (MyProductionItem prodItem in AssemblerQueue)
                        {
                            if (prodItem.BlueprintId == blueprintDef)
                            {
                                nAlreadyQueued += (int)prodItem.Amount;
                            }
                        }
                        int amountWeShouldAddToQueue = componentAmountRequired - nAlreadyQueued;
                        if (amountWeShouldAddToQueue > 0)
                        {
                            assembler.AddQueueItem(blueprintDef, (MyFixedPoint)amountWeShouldAddToQueue);
                        }
                    }
                }
            }
        }

        void AutoRefineries()
        {
            foreach (IMyInventory mainInv in CARGOINVENTORIES)
            {
                List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                mainInv.GetItems(cargoItems);

                foreach (IMyRefinery refinery in REFINERIES)
                {
                    IMyInventory blockInventory = refinery.GetInventory(0);
                    foreach (MyInventoryItem item in cargoItems)// Send scrap and stone to refineries
                    {
                        string itemType = item.Type.ToString();
                        if (itemType.Contains("Scrap") || (itemType.Contains("Stone") && itemType.Contains("Ore")))
                        {
                            mainInv.TransferItemTo(blockInventory
                                                , cargoItems.IndexOf(item)
                                                , 0
                                                , true
                                                , Math.Min((int)item.Amount, 1000));
                            cargoItems.Clear();
                            mainInv.GetItems(cargoItems);
                            break;
                        }
                    }

                    if (refinery.BlockDefinition.ToString().Contains("Furnace"))
                    {
                        continue;
                    }

                    List<MyInventoryItem> ingredients = new List<MyInventoryItem>();
                    blockInventory.GetItems(ingredients);
                    if (ingredients.Count > 0)// Also do not do what follows if the refinery is working on scrap or stone
                    {
                        if (ingredients[0].Type.ToString().Contains("Scrap") ||
                            ingredients[0].Type.ToString().Contains("Stone"))
                        {
                            continue;
                        }
                    }

                    foreach (string ore in oreList)
                    {
                        bool jobDone = false;
                        bool enoughOfThisIngot = false;
                        foreach (var item in cargoItems)
                        {
                            string itemType = item.Type.ToString();
                            if (itemType.Contains(ore) && itemType.Contains("Ingot") && item.Amount > 10) // Make sure we have at least 10 of each ingot in cargo 
                            {
                                enoughOfThisIngot = true;
                                break; // Enough of this ore, go to next ore
                            }
                        }
                        if (enoughOfThisIngot) continue; // Stop here if this ingot is already being produced

                        ingredients.Clear();
                        blockInventory.GetItems(ingredients);
                        if (ingredients.Count > 0 && ingredients[0].Type.ToString().Contains(ore)) { break; }

                        foreach (MyInventoryItem item in ingredients)// Maybe there is ore in the refinery but not 1st rank
                        {
                            string itemType = item.Type.ToString();
                            if (itemType.Contains(ore) && itemType.Contains("Ore"))// Now move this ore to 1st rank
                            {
                                blockInventory.TransferItemTo(blockInventory
                                            , ingredients.IndexOf(item)
                                            , 0
                                            , true
                                            , item.Amount);
                                jobDone = true;
                                break;
                            }
                        }
                        if (jobDone) break;

                        foreach (MyInventoryItem item in cargoItems)// Check if we have the ore to produce this ingot
                        {
                            string itemType = item.Type.ToString();
                            if (itemType.Contains(ore) && itemType.Contains("Ore") && item.Amount > 1)// Now move this ore to refinery for ingots
                            {
                                mainInv.TransferItemTo(blockInventory
                                            , cargoItems.IndexOf(item)
                                            , 0
                                            , true
                                            , Math.Min((int)(item.Amount - 1), 1000));
                                jobDone = true;
                                break;
                            }
                        }
                        if (jobDone) break;
                    }
                    cargoItems.Clear();
                }
            }
        }

        IMyCargoContainer GetMainEmptyCargo()
        {
            //float margin = 0.1f;
            IMyCargoContainer mainCargo = null;
            int cargoIndex = 1000;
            foreach (IMyCargoContainer cargo in CONTAINERS)
            {
                int cargoNum = int.Parse(cargo.CustomName.Replace(containersName, "").Trim());
                if (cargoNum < cargoIndex)
                {
                    for (int i = 0; i < cargo.InventoryCount; i++)
                    {
                        IMyInventory inv = cargo.GetInventory(i);
                        if (!inv.IsFull) // || ((float)inv.CurrentVolume < ((float)inv.MaxVolume - margin)))
                        {
                            cargoIndex = cargoNum;
                            mainCargo = cargo;
                            break;
                        }
                    }
                }
            }
            return mainCargo;
        }

        void CompactMainCargos()
        {
            //float margin = 0.1f;
            IMyCargoContainer mainCargo = GetMainEmptyCargo();
            if (mainCargo != null)
            {
                List<IMyInventory> CONTAINERSINVENTORIES = new List<IMyInventory>();
                CONTAINERSINVENTORIES.AddRange(CONTAINERS.Where(block => block.CustomName != mainCargo.CustomName).SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));

                foreach (IMyInventory inventory in CONTAINERSINVENTORIES)
                {
                    List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                    inventory.GetItems(cargoItems);
                    foreach (MyInventoryItem item in cargoItems)
                    {
                        bool transferred = false;
                        for (int i = 0; i < mainCargo.InventoryCount; i++)
                        {
                            IMyInventory maninInv = mainCargo.GetInventory(i);
                            if (!maninInv.IsFull) // || ((float)maninInv.CurrentVolume > ((float)maninInv.MaxVolume - margin)))
                            {
                                MyFixedPoint amount = maninInv.MaxVolume - maninInv.CurrentVolume;
                                transferred = inventory.TransferItemTo(maninInv, cargoItems.IndexOf(item), 0, true, amount);
                                break;
                            }
                        }
                        if (transferred)
                        {
                            //cargoItems.Clear();//TODO
                            //inventory.GetItems(cargoItems);
                            break;
                        }
                    }
                }
            }
        }

        void FillReactors()
        {
            foreach (IMyInventory reInv in REACTORSINVENTORIES)
            {
                foreach (IMyInventory cargoInv in CARGOINVENTORIES)
                {
                    MyInventoryItem? itemFound = cargoInv.FindItem(uraniumIngot);
                    if (itemFound.HasValue)
                    {
                        MyFixedPoint availableVolume = reInv.MaxVolume - reInv.CurrentVolume;
                        //MyFixedPoint itemAmount = cargoInv.GetItemAmount(iceOre);
                        if (cargoInv.CanTransferItemTo(reInv, uraniumIngot))
                        {
                            cargoInv.TransferItemTo(reInv, itemFound.Value, availableVolume);
                            break;
                        }
                    }
                }
            }
        }

        void FillHidrogenGenerators()
        {
            foreach (IMyInventory gasInv in GASINVENTORIES)
            {
                foreach (IMyInventory cargoInv in CARGOINVENTORIES)
                {
                    MyInventoryItem? itemFound = cargoInv.FindItem(iceOre);
                    if (itemFound.HasValue)
                    {
                        MyFixedPoint availableVolume = gasInv.MaxVolume - gasInv.CurrentVolume;
                        //MyFixedPoint itemAmount = cargoInv.GetItemAmount(iceOre);
                        if (cargoInv.CanTransferItemTo(gasInv, iceOre))
                        {
                            cargoInv.TransferItemTo(gasInv, itemFound.Value, availableVolume);
                            break;
                        }
                    }
                }
            }
        }

        void MoveItemsFromConnectors()
        {
            foreach (IMyInventory connInv in CONNECTORSINVENTORIES)
            {
                List<MyInventoryItem> cargoItems = new List<MyInventoryItem>();
                connInv.GetItems(cargoItems);
                foreach (MyInventoryItem item in cargoItems)
                {
                    foreach (IMyInventory cargoInv in CARGOINVENTORIES)
                    {
                        if (connInv.CanTransferItemTo(cargoInv, item.Type) && cargoInv.CanItemsBeAdded(item.Amount, item.Type))
                        {
                            connInv.TransferItemTo(cargoInv, item);
                        }
                    }
                }
            }
        }

        void GetBlocks()
        {
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
            GASGENERATORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(GASGENERATORS, block => block.CustomName.Contains(gasGeneratorsName));
            REACTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(REACTORS, block => block.CustomName.Contains(reactorsName));
            REACTORSINVENTORIES.Clear();
            REACTORSINVENTORIES.AddRange(REACTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            GYROS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(GYROS, block => block.CustomName.Contains(gyrosName));
            CONTROLLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(CONTROLLERS, block => block.CustomName.Contains(controllersName));
            TERMINALS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TERMINALS, block => block.CustomName.Contains(terminalsName) && !(block is IMyPowerProducer) && !(block is IMySolarPanel) && !(block is IMyBatteryBlock) && !(block is IMyReactor) && !block.CustomName.Contains(hThrustersName));
            THRUSTERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(THRUSTERS, block => block.CustomName.Contains(hThrustersName) || block.CustomName.Contains(iThrustersName) || block.CustomName.Contains(aThrustersName));
            BLOCKSWITHINVENTORY.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(BLOCKSWITHINVENTORY, block => block.HasInventory && block.CustomName.Contains(shipPrefix)); //&& block.IsSameConstructAs(Me)
            INVENTORIES.Clear();
            INVENTORIES.AddRange(BLOCKSWITHINVENTORY.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            REFINERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(REFINERIES, block => block.CustomName.Contains(refineriesName));
            ASSEMBLERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(ASSEMBLERS, block => block.CustomName.Contains(assemblersName));
            CONTAINERS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CONTAINERS, block => block.CustomName.Contains(containersName));
            GATLINGTURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(GATLINGTURRETS, block => block.CustomName.Contains(gatlingTurretsName));
            MISSILETURRETS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(MISSILETURRETS, block => block.CustomName.Contains(missileTurretsName));
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains(cockpitsName));
            LCDSSTATUS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCDSSTATUS, block => block.CustomName.Contains(lcdStatusName));
            GASINVENTORIES.Clear();
            GASINVENTORIES.AddRange(GASGENERATORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CARGOINVENTORIES.Clear();
            GASINVENTORIES.AddRange(CONTAINERS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            CONNECTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(CONNECTORS, block => block.CustomName.Contains(connectorsName));
            CONNECTORSINVENTORIES.Clear();
            CONNECTORSINVENTORIES.AddRange(CONNECTORS.SelectMany(block => Enumerable.Range(0, block.InventoryCount).Select(block.GetInventory)));
            POWERSURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdPowerName));
            foreach (IMyTextPanel panel in panels)
            {
                POWERSURFACES.Add(panel as IMyTextSurface);
            }
            INVENTORYSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdInventoryName));
            foreach (IMyTextPanel panel in panels)
            {
                INVENTORYSURFACES.Add(panel as IMyTextSurface);
            }
            COMPONENTSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdComponentsName));
            foreach (IMyTextPanel panel in panels)
            {
                COMPONENTSURFACES.Add(panel as IMyTextSurface);
            }
            ORESURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains(lcdOreName));
            foreach (IMyTextPanel panel in panels)
            {
                ORESURFACES.Add(panel as IMyTextSurface);
            }
        }

        void ResetOresDict()
        {
            oresDict = new Dictionary<MyDefinitionId, int>()
            {
                {MyItemType.MakeOre("Cobalt"),0},
                {MyItemType.MakeOre("Gold"),0},
                {MyItemType.MakeOre("Ice"),0},
                {MyItemType.MakeOre("Iron"),0},
                {MyItemType.MakeOre("Magnesium"),0},
                {MyItemType.MakeOre("Nickel"),0},
                {MyItemType.MakeOre("Organic"),0},
                {MyItemType.MakeOre("Platinum"),0},
                {MyItemType.MakeOre("Scrap"),0},
                {MyItemType.MakeOre("Silicon"),0},
                {MyItemType.MakeOre("Silver"),0},
                {MyItemType.MakeOre("Stone"),0},
                {MyItemType.MakeOre("Uranium"),0}
            };
        }

        void ResetIngotDict()
        {
            ingotsDict = new Dictionary<MyDefinitionId, int>()
            {
                {MyItemType.MakeIngot("Cobalt"),0},
                {MyItemType.MakeIngot("Gold"),0},
                {MyItemType.MakeIngot("Stone"),0},
                {MyItemType.MakeIngot("Iron"),0},
                {MyItemType.MakeIngot("Magnesium"),0},
                {MyItemType.MakeIngot("Nickel"),0},
                {MyItemType.MakeIngot("Scrap"),0},
                {MyItemType.MakeIngot("Platinum"),0},
                {MyItemType.MakeIngot("Silicon"),0},
                {MyItemType.MakeIngot("Silver"),0},
                {MyItemType.MakeIngot("Uranium"),0}
            };
        }

        void ResetComponentsDict()
        {
            componentsDict = new Dictionary<MyDefinitionId, int>()
            {
                {MyItemType.MakeComponent("BulletproofGlass"),0},
                {MyItemType.MakeComponent("Canvas"),0},
                {MyItemType.MakeComponent("Computer"),0},
                {MyItemType.MakeComponent("Construction"),0},
                {MyItemType.MakeComponent("Detector"),0},
                {MyItemType.MakeComponent("Display"),0},
                {MyItemType.MakeComponent("Explosives"),0},
                {MyItemType.MakeComponent("Girder"),0},
                {MyItemType.MakeComponent("GravityGenerator"),0},
                {MyItemType.MakeComponent("InteriorPlate"),0},
                {MyItemType.MakeComponent("LargeTube"),0},
                {MyItemType.MakeComponent("Medical"),0},
                {MyItemType.MakeComponent("MetalGrid"),0},
                {MyItemType.MakeComponent("Motor"),0},
                {MyItemType.MakeComponent("PowerCell"),0},
                {MyItemType.MakeComponent("RadioCommunication"),0},
                {MyItemType.MakeComponent("Reactor"),0},
                {MyItemType.MakeComponent("SmallTube"),0},
                {MyItemType.MakeComponent("SolarCell"),0},
                {MyItemType.MakeComponent("SteelPlate"),0},
                {MyItemType.MakeComponent("Superconductor"),0},
                {MyItemType.MakeComponent("Thrust"),0},
                {MyItemType.MakeComponent("ZoneChip"),0}
            };
        }

        void ResetAmmosDict()
        {
            ammosDict = new Dictionary<MyDefinitionId, int>()
            {
                {MyItemType.MakeIngot("NATO_25x184mm"),0},
                {MyItemType.MakeIngot("NATO_5p56x45mm"),0},
                {MyItemType.MakeIngot("Missile200mm"),0}
            };
        }

    }
}
