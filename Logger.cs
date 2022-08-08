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

        //LOGGER
        string timeRemaining;
        int maxJump;
        int currentJump;
        double totJumpPercent;
        double currentStoredPower;
        double maxStoredPower;

        Vector3D rangeFinderPosition;
        string rangeFinderName;
        double rangeFinderDistance;
        double rangeFinderDiameter;


        string targetName;
        Vector3D targetHitPosition;
        Vector3D targetPosition;
        Vector3D targetVelocity;

        List<MyTuple<string, string, string, string, string>> missilesLog = new List<MyTuple<string, string, string, string, string>>();


        string powerStatus;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;
        float terminalMaxInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        float battsMaxOutput;
        int batteriesCount;
        float[] battsCurrentStoredPower;
        float reactorsCurrentOutput;
        float reactorsMaxOutput;
        int reactorsCount;
        float hEngCurrentOutput;
        float hEngMaxOutput;
        int hEnginesCount;
        float solarMaxOutput;
        int solarsCount;
        float turbineMaxOutput;
        int turbinesCount;
        double tankCapacityPercent;


        double cargoPercentage;
        Dictionary<string, string> ammosLog = new Dictionary<string, string>();
        Dictionary<string, string> oresLog = new Dictionary<string, string>();
        Dictionary<string, string> ingotsLog = new Dictionary<string, string>();
        Dictionary<string, string> componentsLog = new Dictionary<string, string>();

        public List<IMyTextSurface> SURFACES = new List<IMyTextSurface>();
        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();

        readonly MyIni myIni = new MyIni();
        public IMyBroadcastListener BROADCASTLISTENER;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            ParseCockpitConfigData(COCKPITS);
        }

        public void Main(string arg) {
            try {

                GetBroadcastMessages();

            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    //DEBUG.ReadText(debugLog, true);
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog);
                }
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void GetBroadcastMessages() {
            if (BROADCASTLISTENER.HasPendingMessage) {
                while (BROADCASTLISTENER.HasPendingMessage) {
                    MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                    //NAVIGATOR
                    if (igcMessage.Data is ImmutableArray<MyTuple<
                        MyTuple<string, int, int, double, double, double>,
                        MyTuple<Vector3D, string, double, double>
                    >>) {
                        var data = (ImmutableArray<MyTuple<
                            MyTuple<string, int, int, double, double, double>,
                            MyTuple<Vector3D, string, double, double>
                        >>)igcMessage.Data;

                        timeRemaining = data[0].Item1.Item1;
                        maxJump = data[0].Item1.Item2;
                        currentJump = data[0].Item1.Item3;
                        totJumpPercent = data[0].Item1.Item4;
                        currentStoredPower = data[0].Item1.Item5;
                        maxStoredPower = data[0].Item1.Item6;

                        rangeFinderPosition = data[0].Item2.Item1;
                        rangeFinderName = data[0].Item2.Item2;
                        rangeFinderDistance = data[0].Item2.Item3;
                        rangeFinderDiameter = data[0].Item2.Item4;
                    }
                    //PAINTER
                    else if (igcMessage.Data is ImmutableArray<MyTuple<
                        MyTuple<string, Vector3D, Vector3D, Vector3D>,
                        string
                    >>) {
                        var data = (ImmutableArray<MyTuple<
                            MyTuple<string, Vector3D, Vector3D, Vector3D>,
                            string
                        >>)igcMessage.Data;

                        targetName = data[0].Item1.Item1;
                        targetHitPosition = data[0].Item1.Item2;
                        targetPosition = data[0].Item1.Item3;
                        targetVelocity = data[0].Item1.Item4;

                        //missilesLog;
                        //toTarget=toTarget,speed=speed,command=command,status=status,type=type\n


                    }
                    //POWERMANAGER
                    else if (igcMessage.Data is ImmutableArray<MyTuple<
                        MyTuple<string, float, float, float>,
                        MyTuple<float, float, float, int, float[]>,
                        MyTuple<float, float, int>,
                        MyTuple<float, float, int>,
                        MyTuple<float, int, float, int>,
                        double
                    >>) {
                        var data = (ImmutableArray<MyTuple<
                            MyTuple<string, float, float, float>,
                            MyTuple<float, float, float, int, float[]>,
                            MyTuple<float, float, int>,
                            MyTuple<float, float, int>,
                            MyTuple<float, int, float, int>,
                            double
                        >>)igcMessage.Data;

                        powerStatus = data[0].Item1.Item1;
                        terminalCurrentInput = data[0].Item1.Item2;
                        terminalMaxRequiredInput = data[0].Item1.Item3;
                        terminalMaxInput = data[0].Item1.Item4;
                        battsCurrentInput = data[0].Item2.Item1;
                        battsCurrentOutput = data[0].Item2.Item2;
                        battsMaxOutput = data[0].Item2.Item3;
                        batteriesCount = data[0].Item2.Item4;
                        battsCurrentStoredPower = data[0].Item2.Item5;
                        reactorsCurrentOutput = data[0].Item3.Item1;
                        reactorsMaxOutput = data[0].Item3.Item2;
                        reactorsCount = data[0].Item3.Item3;
                        hEngCurrentOutput = data[0].Item4.Item1;
                        hEngMaxOutput = data[0].Item4.Item2;
                        hEnginesCount = data[0].Item4.Item3;
                        solarMaxOutput = data[0].Item5.Item1;
                        solarsCount = data[0].Item5.Item2;
                        turbineMaxOutput = data[0].Item5.Item3;
                        turbinesCount = data[0].Item5.Item4;
                        tankCapacityPercent = data[0].Item6;
                    }
                    //INVENTORYMANAGER
                    else if (igcMessage.Data is ImmutableArray<MyTuple<double, string, string, string, string>>) {
                        var data = (ImmutableArray<MyTuple<double, string, string, string, string>>)igcMessage.Data;

                        double cargoPercentage = data[0].Item1;

                        //SubtypeId=Value,
                        //cargoPercentage, ammosLog, oresLog, ingotsLog, componentsLog


                    }
                }
            }
        }


        void ParseCockpitConfigData(List<IMyCockpit> cockpits) {//"[MissilesSettings]\ncockpitTargetSurface=0\n"
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "MissilesSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("MissilesSettings", "cockpitTargetSurface").ToString())) {
                    int cockpitTargetSurface = myIni.Get("MissilesSettings", "cockpitTargetSurface").ToInt32();
                    SURFACES.Add(cockpit.GetSurface(cockpitTargetSurface));
                }
                myIni.TryParse(cockpit.CustomData, "RangeFinderSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToString())) {
                    int cockpitRangeFinderSurface = myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToInt32();
                    SURFACES.Add(cockpit.GetSurface(cockpitRangeFinderSurface));//4
                }
                //TODO

            }
        }



    }
}
