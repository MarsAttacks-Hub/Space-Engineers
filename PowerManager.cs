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

        bool solarPowerOnce = true;
        bool greenPowerOnce = true;
        bool hydrogenPowerOnce = true;
        bool fullSteamOnce = true;
        bool isControlled = false;

        string powerStatus;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;

        float battsCurrentInput;
        float battsCurrentOutput;
        float battsMaxOutput;
        public List<float> battsCurrentStoredPower = new List<float>();

        float reactorsCurrentOutput;
        float reactorsMaxOutput;

        float hEngCurrentOutput;
        float hEngMaxOutput;

        float solarMaxOutput;
        float turbineMaxOutput;

        double tankCapacityPercent;
        int sendCount = 0;

        public List<Electric> ELECTRICS = new List<Electric>();
        public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
        public List<IMyReactor> REACTORS = new List<IMyReactor>();
        public List<IMySolarPanel> SOLARS = new List<IMySolarPanel>();
        public List<IMyPowerProducer> TURBINES = new List<IMyPowerProducer>();
        public List<IMyPowerProducer> HENGINES = new List<IMyPowerProducer>();
        public List<IMyGasTank> HTANKS = new List<IMyGasTank>();

        IMyBroadcastListener BROADCASTLISTENER;

        public StringBuilder oresLog = new StringBuilder("");
        public StringBuilder ingotsLog = new StringBuilder("");
        public StringBuilder ammosLog = new StringBuilder("");
        public StringBuilder componentsLog = new StringBuilder("");
        public StringBuilder refineriesInputLog = new StringBuilder("");
        public StringBuilder assemblersInputLog = new StringBuilder("");
        public StringBuilder inventoriesPercentLog = new StringBuilder("");
        public StringBuilder powerLog = new StringBuilder("");

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
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[POWERMANAGER]");
            Me.GetSurface(0).BackgroundColor = togglePB ? new Color(0, 0, 50) : new Color(0, 0, 0);
        }

        public void Main(string argument) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(argument)) {
                    ProcessArgument(argument);
                    if (!togglePB) { CalcPower(); SendBroadcastMessage(); return; }
                }

                GetBroadcastMessages();

                CalcPower();
                PowerFlow();

                if (logger) {
                    if (sendCount == 10) {
                        SendBroadcastMessage();
                        sendCount = 0;
                    }
                    sendCount++;
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
                //Setup();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case "TogglePB":
                    togglePB = !togglePB;
                    if (togglePB) {
                        Me.GetSurface(0).BackgroundColor = new Color(0, 0, 50);
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
                    Me.GetSurface(0).BackgroundColor = new Color(0, 0, 50);
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
            }
        }

        void PowerFlow() {
            float shipInput;
            if (!isControlled) { shipInput = terminalCurrentInput; } else { shipInput = terminalMaxRequiredInput; }
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
            battsCurrentStoredPower.Clear();
            battsCurrentInput = 0f;
            battsCurrentOutput = 0f;
            foreach (IMyBatteryBlock block in BATTERIES) {
                battsCurrentInput += block.CurrentInput;
                battsCurrentOutput += block.CurrentOutput;
                battsCurrentStoredPower.Add(block.CurrentStoredPower);
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

        void SendBroadcastMessage() {
            var immArray = ImmutableArray.CreateBuilder<MyTuple<
                    MyTuple<string, float, float>,
                    MyTuple<float, float, float, int, string>,
                    MyTuple<float, float, int>,
                    MyTuple<float, float, int>,
                    MyTuple<float, int, float, int>,
                    double
                    >>();

            StringBuilder battStoredPow = new StringBuilder("");
            int count = 1;
            foreach (float pow in battsCurrentStoredPower) {
                if (count == battsCurrentStoredPower.Count) {
                    battStoredPow.Append($"{pow:0.0}");
                } else {
                    battStoredPow.Append($"{pow:0.0},");
                }
                count++;
            }

            var tuple = MyTuple.Create(
                MyTuple.Create(powerStatus, terminalCurrentInput, terminalMaxRequiredInput),
                MyTuple.Create(battsCurrentInput, battsCurrentOutput, battsMaxOutput, BATTERIES.Count, battStoredPow.ToString()),
                MyTuple.Create(reactorsCurrentOutput, reactorsMaxOutput, REACTORS.Count),
                MyTuple.Create(hEngCurrentOutput, hEngMaxOutput, HENGINES.Count),
                MyTuple.Create(solarMaxOutput, SOLARS.Count, turbineMaxOutput, TURBINES.Count),
                tankCapacityPercent
                );
            immArray.Add(tuple);
            IGC.SendBroadcastMessage("[LOGGER]", immArray.ToImmutable(), TransmissionDistance.ConnectedConstructs);
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
