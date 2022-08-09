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
        bool logger = true;

        bool navigator = false;
        bool painter = false;
        bool power = false;
        bool inventory = false;

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
        //Vector3D targetHitPosition;
        Vector3D targetPosition;
        Vector3D targetVelocity;
        public List<MyTuple<string, string, string, string, string>> missilesLog = new List<MyTuple<string, string, string, string, string>>();

        string powerStatus;
        float terminalCurrentInput;
        //float terminalMaxRequiredInput;
        float terminalMaxInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        //float battsMaxOutput;
        //int batteriesCount;
        float[] battsCurrentStoredPower;
        float reactorsCurrentOutput;
        float reactorsMaxOutput;
        //int reactorsCount;
        float hEngCurrentOutput;
        float hEngMaxOutput;
        //int hEnginesCount;
        float solarMaxOutput;
        //int solarsCount;
        float turbineMaxOutput;
        //int turbinesCount;
        double tankCapacityPercent;

        double cargoPercentage;
        Dictionary<string, string> ammoLogDict = new Dictionary<string, string>();//SubtypeId=value
        Dictionary<string, string> oreLogDict = new Dictionary<string, string>();
        Dictionary<string, string> ingotsLogDict = new Dictionary<string, string>();
        Dictionary<string, string> componentsLogDict = new Dictionary<string, string>();

        public List<IMyCockpit> COCKPITS = new List<IMyCockpit>();

        public List<IMyTextSurface> POWERSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> NAVIGATORSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> PAINTERSURFACES = new List<IMyTextSurface>();

        public List<IMyTextSurface> INVENTORYOREINGOTSSURFACES = new List<IMyTextSurface>();
        public List<IMyTextSurface> INVENTORYCOMPONENTSAMMOSURFACES = new List<IMyTextSurface>();

        readonly MyIni myIni = new MyIni();
        public IMyBroadcastListener BROADCASTLISTENER;

        Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            ParseCockpitConfigData(COCKPITS);
        }

        public void Main(string arg) {
            try {

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (!logger) { Runtime.UpdateFrequency = UpdateFrequency.None; return; } else { Runtime.UpdateFrequency = UpdateFrequency.Update10; }
                }

                GetBroadcastMessages();

                if (navigator) {
                    LogNavigator();
                }
                if (painter) {
                    LogPainter();
                }
                if (power) {
                    LogPower();
                }
                if (inventory) {
                    LogInventory();
                }

                navigator = false;
                painter = false;
                power = false;
                inventory = false;

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

        void ProcessArgument(string argument) {
            switch (argument) {
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

                        navigator = true;
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
                        //targetHitPosition = data[0].Item1.Item2;
                        targetPosition = data[0].Item1.Item3;
                        targetVelocity = data[0].Item1.Item4;

                        missilesLog.Clear();
                        if (!string.IsNullOrEmpty(data[0].Item2)) {
                            char[] c = new char[] { '\n' };
                            string[] missilesLogArray = data[0].Item2.Split(c);
                            foreach (string element in missilesLogArray) {
                                char[] a = new char[] { ',' };
                                string[] elementArray = element.Split(a);
                                List<string> values = new List<string>();
                                foreach (string entry in elementArray) {
                                    char[] b = new char[] { '=' };
                                    string[] entryArray = entry.Split(b);
                                    values.Add(entryArray[1]);
                                }
                                var tuple = MyTuple.Create(values.ElementAt(0), values.ElementAt(1), values.ElementAt(2), values.ElementAt(3), values.ElementAt(4));
                                missilesLog.Add(tuple);
                            }
                        }

                        painter = true;
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
                        //terminalMaxRequiredInput = data[0].Item1.Item3;
                        terminalMaxInput = data[0].Item1.Item4;
                        battsCurrentInput = data[0].Item2.Item1;
                        battsCurrentOutput = data[0].Item2.Item2;
                        //battsMaxOutput = data[0].Item2.Item3;
                        //batteriesCount = data[0].Item2.Item4;
                        battsCurrentStoredPower = data[0].Item2.Item5;
                        reactorsCurrentOutput = data[0].Item3.Item1;
                        reactorsMaxOutput = data[0].Item3.Item2;
                        //reactorsCount = data[0].Item3.Item3;
                        hEngCurrentOutput = data[0].Item4.Item1;
                        hEngMaxOutput = data[0].Item4.Item2;
                        //hEnginesCount = data[0].Item4.Item3;
                        solarMaxOutput = data[0].Item5.Item1;
                        //solarsCount = data[0].Item5.Item2;
                        turbineMaxOutput = data[0].Item5.Item3;
                        //turbinesCount = data[0].Item5.Item4;
                        tankCapacityPercent = data[0].Item6;

                        power = true;
                    }
                    //INVENTORYMANAGER
                    else if (igcMessage.Data is ImmutableArray<MyTuple<double, string, string, string, string>>) {
                        var data = (ImmutableArray<MyTuple<double, string, string, string, string>>)igcMessage.Data;

                        cargoPercentage = data[0].Item1;

                        ammoLogDict.Clear();
                        oreLogDict.Clear();
                        ingotsLogDict.Clear();
                        componentsLogDict.Clear();
                        if (!string.IsNullOrEmpty(data[0].Item2)) {
                            char[] c = new char[] { ',' };
                            string[] ammoLogArray = data[0].Item2.Split(c);
                            string[] oreLogArray = data[0].Item3.Split(c);
                            string[] ingotsLogArray = data[0].Item4.Split(c);
                            string[] componentsLogArray = data[0].Item5.Split(c);

                            ParseLog(ref ammoLogDict, ammoLogArray);
                            ParseLog(ref oreLogDict, oreLogArray);
                            ParseLog(ref ingotsLogDict, ingotsLogArray);
                            ParseLog(ref componentsLogDict, componentsLogArray);
                        }

                        inventory = true;
                    }
                }
            }
        }

        void ParseLog(ref Dictionary<string, string> dictionary, string[] array) {
            foreach (string element in array) {
                char[] a = new char[] { '=' };
                string[] i = element.Split(a);
                dictionary.Add(i[0], i[1]);
            }
        }

        void ParseCockpitConfigData(List<IMyCockpit> cockpits) {
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "MissilesSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("MissilesSettings", "cockpitTargetSurface").ToString())) {
                    int cockpitTargetSurface = myIni.Get("MissilesSettings", "cockpitTargetSurface").ToInt32();
                    PAINTERSURFACES.Add(cockpit.GetSurface(cockpitTargetSurface));//0
                }
                myIni.TryParse(cockpit.CustomData, "RangeFinderSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToString())) {
                    int cockpitRangeFinderSurface = myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToInt32();
                    NAVIGATORSURFACES.Add(cockpit.GetSurface(cockpitRangeFinderSurface));//4
                }
                myIni.TryParse(cockpit.CustomData, "ManagerSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("ManagerSettings", "cockpitPowerSurface").ToString())) {
                    int cockpitPowerSurface = myIni.Get("ManagerSettings", "cockpitPowerSurface").ToInt32();
                    POWERSURFACES.Add(cockpit.GetSurface(cockpitPowerSurface));//2
                }
            }
        }

        void LogNavigator() {
            foreach (IMyTextSurface surface in NAVIGATORSURFACES) {
                RectangleF Left = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, new Vector2(surface.SurfaceSize.X / 2, surface.SurfaceSize.Y));
                RectangleF Right = new RectangleF(Left.X + (surface.SurfaceSize.X / 2), Left.Y, Left.Width, Left.Height);

                string data = $"JUMP DRIVE:\n"
                    + $"Reload: {timeRemaining}\n"
                    + $"Curr. Jump: {currentJump}\n"
                    + $"Max Jump: {maxJump}\n"
                    + $"Jump %: {totJumpPercent:0.0}\n"
                    + $"Power: {currentStoredPower:0.0}\n"
                    + $"Max Power: {maxStoredPower:0.0}";
                WriteText(surface, new Vector2(Left.X, Left.Y), data, "Default", 1f, new Color(0, 100, 0));

                data = $"RANGE FINDER:\n"
                    + $"Name: {rangeFinderName}\n"
                    + $"Distance: {(int)rangeFinderDistance}\n"
                    + $"Diameter: {(int)rangeFinderDiameter}\n"
                    + $"Position: X:{rangeFinderPosition.X:0.0}, Y:{rangeFinderPosition.Y:0.0}, Z:{rangeFinderPosition.Z:0.0}";
                WriteText(surface, new Vector2(Right.X, Right.Y), data, "Default", 1f, new Color(0, 100, 0));
            }
        }

        void LogPainter() {
            foreach (IMyTextSurface surface in PAINTERSURFACES) {
                RectangleF Left = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, new Vector2(surface.SurfaceSize.X / 2, surface.SurfaceSize.Y));
                RectangleF Right = new RectangleF(Left.X + (surface.SurfaceSize.X / 2), Left.Y, Left.Width, Left.Height);

                StringBuilder data = new StringBuilder("");
                data.Append($"PAINTER:\n"
                    + $"{targetName}\n"
                    + $"Velocity: {targetVelocity.Length():0.0}\n"
                    + $"\n");

                StringBuilder data2 = new StringBuilder("");
                data2.Append($"\n"
                    + $"\n"
                    + $"Position: X:{targetPosition.X:0.0}, Y:{targetPosition.Y:0.0}, Z:{targetPosition.Z:0.0}"
                    + $"\n");

                if (missilesLog.Count != 0) { data.Append($"MISSILES:\n"); data2.Append($"\n"); }
                foreach (MyTuple<string, string, string, string, string> log in missilesLog) {//toTarget=Item1,speed=Item2,command=command,status=status,type=type\n
                    data.Append($"{log.Item5}\n");
                    data.Append($"Speed: {log.Item2}\n");
                    data.Append($"Status: {log.Item4}\n");
                    data.Append($"\n");

                    data2.Append($"To Target: {log.Item1}\n");
                    data2.Append($"Command: {log.Item3}\n");
                    data2.Append($"\n\n");
                }

                WriteText(surface, new Vector2(Left.X, Left.Y), data.ToString(), "Default", 1f, new Color(0, 100, 0));
                WriteText(surface, new Vector2(Right.X, Right.Y), data2.ToString(), "Default", 1f, new Color(0, 100, 0));
            }
        }

        void LogPower() {
            foreach (IMyTextSurface surface in POWERSURFACES) {
                RectangleF Left = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, new Vector2(surface.SurfaceSize.X / 2, surface.SurfaceSize.Y));
                RectangleF Right = new RectangleF(Left.X + (surface.SurfaceSize.X / 2), Left.Y, Left.Width, Left.Height);

                string data = $"Status: {powerStatus}\n"
                    + $"Curr In: {terminalCurrentInput:0.0}\n"
                    + $"Batt. In: {battsCurrentInput}\n"
                    + $"Batt. Pow: {battsCurrentStoredPower:0.0}\n"
                    + $"Reactors Out: {reactorsCurrentOutput:0.0}\n"
                    + $"H2 Out: {hEngCurrentOutput:0.0}\n"
                    + $"Solar: {solarMaxOutput:0.0}\n"
                    + $"H2 Tank %: {tankCapacityPercent:0.0}\n";
                WriteText(surface, new Vector2(Left.X, Left.Y), data, "Default", 1f, new Color(0, 100, 0));

                data = $"\n"
                    + $"Max In: {terminalMaxInput:0.0}\n"
                    + $"Batt. Out: {battsCurrentOutput:0.0}\n"
                    + $"\n"
                    + $"Max Out: {reactorsMaxOutput:0.0}\n"
                    + $"Max Out: X:{hEngMaxOutput:0.0}\n"
                    + $"Turbines: X:{turbineMaxOutput:0.0}\n";
                WriteText(surface, new Vector2(Right.X, Right.Y), data, "Default", 1f, new Color(0, 100, 0));
            }
        }

        void LogInventory() {
            foreach (IMyTextSurface surface in INVENTORYCOMPONENTSAMMOSURFACES) {
                RectangleF Left = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, new Vector2(surface.SurfaceSize.X / 2, surface.SurfaceSize.Y));
                RectangleF Right = new RectangleF(Left.X + (surface.SurfaceSize.X / 2), Left.Y, Left.Width, Left.Height);

                StringBuilder data = new StringBuilder("");
                StringBuilder data2 = new StringBuilder("");
                data.Append($"COMPONENTS:\n");
                data2.Append($"\n");

                bool alternate = true;
                foreach (var log in componentsLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: {log.Value}\n");
                        alternate = false;
                    } else {
                        data2.Append($"{log.Key}: {log.Value}\n");
                        alternate = true;
                    }
                }

                data.Append($"\nAMMO:\n");
                data2.Append($"\n\n");

                alternate = true;
                foreach (var log in ammoLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: {log.Value}\n");
                        alternate = false;
                    } else {
                        data2.Append($"{log.Key}: {log.Value}\n");
                        alternate = true;
                    }
                }

                WriteText(surface, new Vector2(Left.X, Left.Y), data.ToString(), "Default", 1f, new Color(0, 100, 0));
                WriteText(surface, new Vector2(Right.X, Right.Y), data2.ToString(), "Default", 1f, new Color(0, 100, 0));
            }

            foreach (IMyTextSurface surface in INVENTORYOREINGOTSSURFACES) {
                RectangleF Left = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, new Vector2(surface.SurfaceSize.X / 2, surface.SurfaceSize.Y));
                RectangleF Right = new RectangleF(Left.X + (surface.SurfaceSize.X / 2), Left.Y, Left.Width, Left.Height);

                StringBuilder data = new StringBuilder("");
                StringBuilder data2 = new StringBuilder("");

                data.Append($"Inventory Fill: {cargoPercentage:0.0}%\n\n");
                data2.Append($"\n\n");

                data.Append($"ORE:\n");
                data2.Append($"\n");

                bool alternate = true;
                foreach (var log in oreLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: {log.Value}\n");
                        alternate = false;
                    } else {
                        data2.Append($"{log.Key}: {log.Value}\n");
                        alternate = true;
                    }
                }

                data.Append($"\nINGOTS:\n");
                data2.Append($"\n\n");

                alternate = true;
                foreach (var log in ingotsLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: {log.Value}\n");
                        alternate = false;
                    } else {
                        data2.Append($"{log.Key}: {log.Value}\n");
                        alternate = true;
                    }
                }

                WriteText(surface, new Vector2(Left.X, Left.Y), data.ToString(), "Default", 1f, new Color(0, 100, 0));
                WriteText(surface, new Vector2(Right.X, Right.Y), data2.ToString(), "Default", 1f, new Color(0, 100, 0));
            }
        }

        void WriteText(IMyTextSurface surface, Vector2 pos, string data, string font, float scale, Color? color = null, TextAlignment alignment = TextAlignment.LEFT) {
            surface.DrawFrame().Add(new MySprite() {
                Type = SpriteType.TEXT,
                Data = data,
                RotationOrScale = scale,
                Position = pos,
                FontId = font,
                Color = color,
                Alignment = alignment
            });
        }

        void GetBlocks() {
            COCKPITS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(COCKPITS, block => block.CustomName.Contains("[CRX] Controller Cockpit"));
            NAVIGATORSURFACES.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Navigator"));
            foreach (IMyTextPanel panel in panels) { NAVIGATORSURFACES.Add(panel as IMyTextSurface); }
            PAINTERSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Painter"));
            foreach (IMyTextPanel panel in panels) { PAINTERSURFACES.Add(panel as IMyTextSurface); }
            POWERSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Power"));
            foreach (IMyTextPanel panel in panels) { POWERSURFACES.Add(panel as IMyTextSurface); }
            INVENTORYOREINGOTSSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Ore Ingots"));
            foreach (IMyTextPanel panel in panels) { INVENTORYOREINGOTSSURFACES.Add(panel as IMyTextSurface); }
            INVENTORYCOMPONENTSAMMOSURFACES.Clear();
            panels.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Components Ammo"));
            foreach (IMyTextPanel panel in panels) { INVENTORYCOMPONENTSAMMOSURFACES.Add(panel as IMyTextSurface); }
        }

        //WriteText(lcd, new Vector2(Right.X + Right.Width, Right.Y), "Write on the Right side", "Default", 1f, Color.Red, TextAlignment.RIGHT);
        //WriteText(lcd, new Vector2(Left.X + Left.Width, Left.Y), "Write on the Center Left", "Default", 1f, Color.Red, TextAlignment.RIGHT);

    }
}
