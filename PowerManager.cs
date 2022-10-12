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

namespace IngameScript {
    partial class Program : MyGridProgram {

        //POWER MANAGER
        bool togglePB = true;//enable/disable PB
        bool logger = true;//enable/disable logging
        bool activeManagement = false;//enable/disable active power managment

        bool solarPowerOnce = true;
        bool greenPowerOnce = true;
        bool hydrogenPowerOnce = true;
        bool fullSteamOnce = true;

        string powerStatus;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;

        float battsCurrentInput;
        float battsCurrentOutput;
        float battsMaxOutput;
        float battsCurrentStoredPower;
        float battsMaxStoredPower;

        float reactorsCurrentOutput;
        float reactorsMaxOutput;

        float hEngCurrentOutput;
        float hEngMaxOutput;

        float solarMaxOutput;
        float turbineMaxOutput;

        double tankCapacityPercent;

        int sendCount = 0;
        int electricsIndex = 0;
        int blocksToScanPerRun = 0;

        public List<Electric> ELECTRICS = new List<Electric>();
        public List<Electric> TEMPELECTRICS = new List<Electric>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyReactor> REACTORS = new List<IMyReactor>();
        public List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();
        public List<IMyPowerProducer> TURBINES = new List<IMyPowerProducer>();
        public List<IMyPowerProducer> HENGINES = new List<IMyPowerProducer>();
        public List<IMyGasTank> HTANKS = new List<IMyGasTank>();

        IMyTextPanel LCDACTIVEMANAGMENT;

        IEnumerator<bool> stateMachine;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
            foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
            foreach (IMyBatteryBlock block in BATTERIES) { block.Enabled = true; block.ChargeMode = ChargeMode.Auto; }
            GetBatteriesMaxOut();
            GetHydrogenEnginesMaxOutput();
            GetReactorsMaxOutput();
            if (!activeManagement) { powerStatus = "Management Deactivated"; }
            if (LCDACTIVEMANAGMENT != null) { LCDACTIVEMANAGMENT.BackgroundColor = activeManagement ? new Color(25, 0, 100) : new Color(0, 0, 0); }
            Me.GetSurface(0).BackgroundColor = togglePB ? new Color(25, 0, 100) : new Color(0, 0, 0);
            blocksToScanPerRun = (int)Math.Ceiling(ELECTRICS.Count / 4d);
            stateMachine = RunOverTime();
        }

        public void Main(string argument, UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(argument)) {
                    ProcessArgument(argument);
                    if (!togglePB) {
                        CalcPower();
                        SendBroadcastMessage();
                        return;
                    }
                } else {
                    if (activeManagement) {
                        CalcPower();
                        PowerFlow();
                        if (logger) {
                            if (sendCount >= 10) {
                                SendBroadcastMessage();
                                sendCount = 0;
                            }
                            sendCount++;
                        }
                    } else {
                        if ((updateType & UpdateType.Update10) == UpdateType.Update10) {
                            RunStateMachine();
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
                        foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                        foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                        foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                        powerStatus = "Full Steam";
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
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                    foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                    powerStatus = "Full Steam";
                    Runtime.UpdateFrequency = UpdateFrequency.None;
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
                case "WriteLog":
                    CalcPower();
                    SendBroadcastMessage();
                    break;
                case "ToggleActiveManagment":
                    activeManagement = !activeManagement;
                    if (!activeManagement) {
                        powerStatus = "Management Deactivated";
                        foreach (IMyReactor block in REACTORS) { block.Enabled = true; }
                        foreach (IMyPowerProducer block in HENGINES) { block.Enabled = true; }
                        foreach (IMyBatteryBlock block in BATTERIES) { block.Enabled = true; block.ChargeMode = ChargeMode.Auto; }
                        if (LCDACTIVEMANAGMENT != null) { LCDACTIVEMANAGMENT.BackgroundColor = new Color(0, 0, 0); }
                    } else {
                        if (LCDACTIVEMANAGMENT != null) { LCDACTIVEMANAGMENT.BackgroundColor = new Color(25, 0, 100); }
                    }
                    break;
                default:
                    break;
            }
        }

        public IEnumerator<bool> RunOverTime() {
            GetBatteriesCurrentInOut();
            yield return true;

            GetSolarsCurrentOutput();
            GetTurbinesCurrentOutput();
            yield return true;

            GetHydrogenEnginesCurrentOutput();
            GetPercentTanksCapacity();
            yield return true;

            GetReactorsCurrentOutput();
            yield return true;

            while (!ScanElectrics()) {
                yield return true;
            }

            powerStatus = "Management Deactivated";
            IMyBatteryBlock battery = GetBatteryWithLowerCharge();
            if (battery != null) {
                foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                battery.ChargeMode = ChargeMode.Recharge;
            } else {
                foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
            }
            SendBroadcastMessage();
            yield return true;
        }

        public void RunStateMachine() {
            if (stateMachine != null) {
                bool hasMoreSteps = stateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                } else {
                    stateMachine.Dispose();
                    stateMachine = RunOverTime();//stateMachine = null;
                }
            }
        }

        void PowerFlow() {
            float storedPow = battsCurrentStoredPower / battsMaxStoredPower * 100f;
            if (terminalCurrentInput < (solarMaxOutput + turbineMaxOutput)) {
                if (solarPowerOnce) {
                    greenPowerOnce = true;
                    hydrogenPowerOnce = true;
                    fullSteamOnce = true;
                    powerStatus = "Solar Power";
                    foreach (IMyPowerProducer block in HENGINES) { block.Enabled = false; }
                    foreach (IMyReactor block in REACTORS) { block.Enabled = false; }
                    IMyBatteryBlock battery = GetBatteryWithLowerCharge();
                    if (battery != null) {
                        foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                        battery.ChargeMode = ChargeMode.Recharge;
                    } else {
                        foreach (IMyBatteryBlock block in BATTERIES) { block.ChargeMode = ChargeMode.Auto; }
                    }
                    solarPowerOnce = false;
                }
            } else if (terminalCurrentInput < (solarMaxOutput + turbineMaxOutput + battsMaxOutput) && storedPow > 5f) {
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
            } else if (terminalCurrentInput < (hEngMaxOutput + solarMaxOutput + turbineMaxOutput + battsMaxOutput) && tankCapacityPercent > 20d && storedPow > 5f) {
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

        bool ScanElectrics() {
            TEMPELECTRICS.Clear();
            bool scanComplete;
            if (electricsIndex >= ELECTRICS.Count - 1) {
                electricsIndex = 0;
                return true;
            } else {
                scanComplete = false;
            }
            if (electricsIndex == 0) {
                terminalCurrentInput = 0f;
                terminalMaxRequiredInput = 0f;
            }
            int count = 0;
            for (int i = electricsIndex; i < ELECTRICS.Count; i++) {
                if (i == ELECTRICS.Count - 1) {
                    TEMPELECTRICS.Add(ELECTRICS[i]);
                    electricsIndex = i + 1;
                } else {
                    if (count <= blocksToScanPerRun) {
                        TEMPELECTRICS.Add(ELECTRICS[i]);
                    } else {
                        electricsIndex = i;
                        break;
                    }
                }
                count++;
            }
            foreach (Electric block in TEMPELECTRICS) {
                terminalCurrentInput += block.GetCurrentInput();
                terminalMaxRequiredInput += block.GetMaxInput();
            }
            return scanComplete;
        }

        IMyBatteryBlock GetBatteryWithLowerCharge() {
            IMyBatteryBlock battery = null;
            float storedPower = 10000f;
            foreach (IMyBatteryBlock block in BATTERIES) {
                if (block.CurrentStoredPower == block.MaxStoredPower) {
                    continue;
                }
                if (block.CurrentStoredPower < storedPower) {
                    storedPower = block.CurrentStoredPower;
                    battery = block;
                }
            }
            return battery;
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
            terminalCurrentInput = 0f;
            terminalMaxRequiredInput = 0f;
            foreach (Electric block in ELECTRICS) {
                terminalCurrentInput += block.GetCurrentInput();
                terminalMaxRequiredInput += block.GetMaxInput();
            }
        }

        void GetSolarsCurrentOutput() {
            solarMaxOutput = 0f;
            foreach (IMySolarPanel block in SOLARS) {
                solarMaxOutput += block.MaxOutput;
            }
        }

        void GetTurbinesCurrentOutput() {
            turbineMaxOutput = 0f;
            foreach (IMyPowerProducer block in TURBINES) {
                turbineMaxOutput += block.MaxOutput;
            }
        }

        void GetHydrogenEnginesCurrentOutput() {
            hEngCurrentOutput = 0f;
            foreach (IMyPowerProducer block in HENGINES) {
                hEngCurrentOutput += block.CurrentOutput;
            }
        }

        void GetHydrogenEnginesMaxOutput() {
            hEngMaxOutput = 0f;
            foreach (IMyPowerProducer block in HENGINES) {
                hEngMaxOutput += block.MaxOutput;
            }
        }

        void GetReactorsCurrentOutput() {
            reactorsCurrentOutput = 0f;
            foreach (IMyPowerProducer block in REACTORS) {
                reactorsCurrentOutput += block.CurrentOutput;
            }
        }

        void GetReactorsMaxOutput() {
            reactorsMaxOutput = 0f;
            foreach (IMyPowerProducer block in REACTORS) {
                reactorsMaxOutput += block.MaxOutput;
            }
        }

        void GetBatteriesCurrentInOut() {
            battsCurrentStoredPower = 0f;
            battsMaxStoredPower = 0f;
            battsCurrentInput = 0f;
            battsCurrentOutput = 0f;
            foreach (IMyBatteryBlock block in BATTERIES) {
                battsCurrentInput += block.CurrentInput;
                battsCurrentOutput += block.CurrentOutput;
                battsCurrentStoredPower += block.CurrentStoredPower;
                battsMaxStoredPower += block.MaxStoredPower;
            }
        }

        void GetBatteriesMaxOut() {
            battsMaxOutput = 0f;
            foreach (IMyBatteryBlock block in BATTERIES) {
                battsMaxOutput += block.MaxOutput;
            }
        }

        void GetPercentTanksCapacity() {
            tankCapacityPercent = 0d;
            double totCapacity = 0d;
            double totFill = 0d;
            foreach (IMyGasTank tank in HTANKS) {
                double capacity = (double)tank.Capacity;
                totCapacity += capacity;
                totFill += capacity * tank.FilledRatio;
            }
            if (totFill > 0 && totCapacity > 0d) { tankCapacityPercent = (totFill / totCapacity) * 100d; }
        }

        void SendBroadcastMessage() {
            var tuple = MyTuple.Create(
                MyTuple.Create(powerStatus, terminalCurrentInput, terminalMaxRequiredInput),
                MyTuple.Create(battsCurrentInput, battsCurrentOutput, battsMaxOutput, battsCurrentStoredPower, battsMaxStoredPower),
                MyTuple.Create(reactorsCurrentOutput, reactorsMaxOutput),
                MyTuple.Create(hEngCurrentOutput, hEngMaxOutput),
                MyTuple.Create(solarMaxOutput, turbineMaxOutput),
                tankCapacityPercent
                );
            IGC.SendBroadcastMessage("[LOGGER]", tuple, TransmissionDistance.ConnectedConstructs);
        }

        void GetBlocks() {
            ELECTRICS.Clear();
            MyResourceSinkComponent sink;
            List<IMyTerminalBlock> terminals = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(terminals, block => block.CustomName.Contains("[CRX]") && !(block is IMyPowerProducer) && !(block is IMySolarPanel) && !(block is IMyBatteryBlock) && !(block is IMyReactor) && !block.CustomName.Contains("[CRX] HThruster"));
            foreach (IMyTerminalBlock block in terminals) {
                if (block.Components.TryGet<MyResourceSinkComponent>(out sink)) {
                    ELECTRICS.Add(new Electric(sink));
                }
            }
            BATTERIES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(BATTERIES, block => block.CustomName.Contains("[CRX] Battery"));
            REACTORS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(REACTORS, block => block.CustomName.Contains("[CRX] Reactor"));
            SOLARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SOLARS, block => block.CustomName.Contains("[CRX] Solar"));
            TURBINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(TURBINES, block => block.CustomName.Contains("[CRX] Wind Turbine"));
            HENGINES.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(HENGINES, block => block.CustomName.Contains("[CRX] HEngine"));
            HTANKS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(HTANKS, block => block.CustomName.Contains("[CRX] HTank"));
            LCDACTIVEMANAGMENT = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Active Power Managment") as IMyTextPanel;
        }

        public class Electric {
            readonly MyDefinitionId electricity = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
            readonly MyResourceSinkComponent electricSinkComponent;

            public Electric(MyResourceSinkComponent sink) {
                electricSinkComponent = sink;
            }

            public float GetCurrentInput() {
                return electricSinkComponent.CurrentInputByType(electricity);
            }

            public float GetMaxInput() {
                return electricSinkComponent.MaxRequiredInputByType(electricity);
            }
        }

    }
}
