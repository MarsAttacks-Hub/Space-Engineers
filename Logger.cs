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
        double targetDistance;
        public List<MyTuple<string, string, string, string, string>> missilesLog = new List<MyTuple<string, string, string, string, string>>();

        string powerStatus;
        float terminalCurrentInput;
        float terminalMaxRequiredInput;
        float battsCurrentInput;
        float battsCurrentOutput;
        float battsMaxOutput;
        //int batteriesCount;
        string battsCurrentStoredPower;
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

        public List<MyPanel> POWER = new List<MyPanel>();
        public List<MyPanel> NAVIGATOR = new List<MyPanel>();
        public List<MyPanel> PAINTER = new List<MyPanel>();
        public List<MyPanel> OREINGOTS = new List<MyPanel>();
        public List<MyPanel> COMPONENTSAMMO = new List<MyPanel>();

        readonly MyIni myIni = new MyIni();
        public IMyBroadcastListener BROADCASTLISTENER;
        IEnumerator<bool> stateMachine;

        public List<MySprite> sprites = new List<MySprite>();

        public StringBuilder data = new StringBuilder("");
        public StringBuilder data2 = new StringBuilder("");
        public StringBuilder data3 = new StringBuilder("");
        public StringBuilder data4 = new StringBuilder("");
        public StringBuilder data5 = new StringBuilder("");

        Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[LOGGER]");
            //BROADCASTLISTENER.SetMessageCallback();
            Me.GetSurface(0).BackgroundColor = logger ? new Color(25, 0, 50) : new Color(0, 0, 0);
            stateMachine = RunOverTime();
        }

        public void Main(string arg, UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (!logger) {
                        Me.GetSurface(0).BackgroundColor = new Color(0, 0, 0);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    } else {
                        Me.GetSurface(0).BackgroundColor = new Color(25, 0, 50);
                        Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    }
                } else {
                    if ((updateType & UpdateType.Update100) == UpdateType.Update100) {
                        RunStateMachine();
                    }
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
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        public IEnumerator<bool> RunOverTime() {
            GetBroadcastMessages();
            yield return true;

            if (navigator) {
                LogNavigator();
                navigator = false;
                yield return true;
            }

            LogPainter();
            yield return true;

            if (power) {
                LogPower();
                power = false;
                yield return true;
            }
            if (inventory) {
                LogInventory();
                inventory = false;
                yield return true;
            }
        }

        public void RunStateMachine() {
            if (stateMachine != null) {
                bool hasMoreSteps = stateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                } else {
                    Echo($"Dispose");

                    stateMachine.Dispose();
                    stateMachine = RunOverTime();//stateMachine = null;
                }
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
            Echo($"GetBroadcastMessages");

            while (BROADCASTLISTENER.HasPendingMessage) {
                MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();
                //NAVIGATOR
                if (igcMessage.Data is MyTuple<
                    MyTuple<string, int, int, double, double, double>,
                    MyTuple<Vector3D, string, double, double>
                >) {
                    var data = (MyTuple<
                        MyTuple<string, int, int, double, double, double>,
                        MyTuple<Vector3D, string, double, double>
                    >)igcMessage.Data;

                    timeRemaining = data.Item1.Item1;
                    maxJump = data.Item1.Item2;
                    currentJump = data.Item1.Item3;
                    totJumpPercent = data.Item1.Item4;
                    currentStoredPower = data.Item1.Item5;
                    maxStoredPower = data.Item1.Item6;

                    rangeFinderPosition = data.Item2.Item1;
                    rangeFinderName = data.Item2.Item2;
                    rangeFinderDistance = data.Item2.Item3;
                    rangeFinderDiameter = data.Item2.Item4;

                    navigator = true;
                }
                //PAINTER
                else if (igcMessage.Data is MyTuple<
                    MyTuple<string, Vector3D, Vector3D, Vector3D>,
                    string
                >) {
                    var data = (MyTuple<
                        MyTuple<string, Vector3D, Vector3D, Vector3D>,
                        string
                    >)igcMessage.Data;

                    targetName = data.Item1.Item1;
                    //targetHitPosition = data.Item1.Item2;
                    targetPosition = data.Item1.Item3;
                    targetVelocity = data.Item1.Item4;
                    targetDistance = Vector3D.Distance(targetPosition, Me.CubeGrid.WorldVolume.Center);

                    missilesLog.Clear();
                    if (!string.IsNullOrEmpty(data.Item2)) {
                        char[] c = new char[] { '\n' };
                        string[] missilesLogArray = data.Item2.Split(c);
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
                }
                //POWERMANAGER
                else if (igcMessage.Data is MyTuple<
                    MyTuple<string, float, float>,
                    MyTuple<float, float, float, int, string>,
                    MyTuple<float, float, int>,
                    MyTuple<float, float, int>,
                    MyTuple<float, int, float, int>,
                    double
                >) {
                    var data = (MyTuple<
                        MyTuple<string, float, float>,
                        MyTuple<float, float, float, int, string>,
                        MyTuple<float, float, int>,
                        MyTuple<float, float, int>,
                        MyTuple<float, int, float, int>,
                        double
                    >)igcMessage.Data;

                    powerStatus = data.Item1.Item1;
                    terminalCurrentInput = data.Item1.Item2;
                    terminalMaxRequiredInput = data.Item1.Item3;
                    battsCurrentInput = data.Item2.Item1;
                    battsCurrentOutput = data.Item2.Item2;
                    battsMaxOutput = data.Item2.Item3;
                    //batteriesCount = data.Item2.Item4;
                    battsCurrentStoredPower = data.Item2.Item5;
                    reactorsCurrentOutput = data.Item3.Item1;
                    reactorsMaxOutput = data.Item3.Item2;
                    //reactorsCount = data.Item3.Item3;
                    hEngCurrentOutput = data.Item4.Item1;
                    hEngMaxOutput = data.Item4.Item2;
                    //hEnginesCount = data.Item4.Item3;
                    solarMaxOutput = data.Item5.Item1;
                    //solarsCount = data.Item5.Item2;
                    turbineMaxOutput = data.Item5.Item3;
                    //turbinesCount = data.Item5.Item4;
                    tankCapacityPercent = data.Item6;

                    power = true;
                }
                //INVENTORYMANAGER
                else if (igcMessage.Data is MyTuple<double, string, string, string, string>) {
                    var data = (MyTuple<double, string, string, string, string>)igcMessage.Data;

                    cargoPercentage = data.Item1;

                    ammoLogDict.Clear();
                    if (!string.IsNullOrEmpty(data.Item2)) {
                        char[] c = new char[] { ',' };
                        string[] ammoLogArray = data.Item2.Split(c);
                        ParseLog(ref ammoLogDict, ammoLogArray);
                    }

                    oreLogDict.Clear();
                    if (!string.IsNullOrEmpty(data.Item3)) {
                        char[] c = new char[] { ',' };
                        string[] oreLogArray = data.Item3.Split(c);
                        ParseLog(ref oreLogDict, oreLogArray);
                    }

                    ingotsLogDict.Clear();
                    if (!string.IsNullOrEmpty(data.Item4)) {
                        char[] c = new char[] { ',' };
                        string[] ingotsLogArray = data.Item4.Split(c);
                        ParseLog(ref ingotsLogDict, ingotsLogArray);
                    }

                    componentsLogDict.Clear();
                    if (!string.IsNullOrEmpty(data.Item5)) {
                        char[] c = new char[] { ',' };
                        string[] componentsLogArray = data.Item5.Split(c);
                        ParseLog(ref componentsLogDict, componentsLogArray);
                    }

                    inventory = true;
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

        void LogNavigator() {
            Echo($"LogNavigator");

            foreach (MyPanel myPanel in NAVIGATOR) {
                timeRemaining = timeRemaining == "" ? "0" : timeRemaining;
                data.Append($"\n"
                    + $"Reload Time: \n"
                    + $"Power: \n"
                    + $"Jump: \n"
                    + $"Max Jump: \n");

                data.Append($"\n"
                    + $"Name: \n"
                    + $"Distance: \n"
                    + $"Diameter: \n"
                    + $"Position ");

                data2.Append($"\n"
                    + $"{timeRemaining}s\n"
                    + $"{currentStoredPower:0.0}/{maxStoredPower:0.0}\n"
                    + $"{currentJump:000,000,000} ({totJumpPercent:0.0}%)\n"
                    + $"{maxJump:000,000,000}\n");

                if (!Vector3D.IsZero(rangeFinderPosition)) {
                    data2.Append($"\n"
                   + $"{rangeFinderName}\n"
                   + $"{(int)rangeFinderDistance}\n"
                   + $"{(int)rangeFinderDiameter}\n"
                   + $"X:{rangeFinderPosition.X:0.0}, Y:{rangeFinderPosition.Y:0.0}, Z:{rangeFinderPosition.Z:0.0}");
                } else {
                    data2.Append($"\n\n\n\n");
                }

                data3.Append($"JUMP DRIVE\n\n\n\n\nRANGE FINDER");

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + 20f, myPanel.col2_3.Y + 20f), data2.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data3.ToString(), "Default", myPanel.minScale, new Color(25, 0, 100), TextAlignment.RIGHT));

                data.Clear();
                data2.Clear();
                data3.Clear();

                MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
                foreach (var sprite in sprites) {
                    frame.Add(sprite);
                }
                frame.Dispose();
                sprites.Clear();
            }
        }

        void LogPainter() {
            Echo($"LogPainter");

            foreach (MyPanel myPanel in PAINTER) {
                data5.Append($"PAINTER");

                data.Append($"\n"
                    + $"Name:\n"
                    + $"Velocity:\n"
                    + $"Position:\n");

                data3.Append($"\n"
                    + $"\n"
                    + $"Distance:\n"
                    + $"\n");

                if (!Vector3D.IsZero(targetPosition)) {
                    data2.Append($"\n"
                    + $"{targetName}\n"
                    + $"{targetVelocity.Length():0.0}\n"
                    + $"X:{targetPosition.X:0.0}, Y:{targetPosition.Y:0.0}, Z:{targetPosition.Z:0.0}\n");

                    data4.Append($"\n"
                    + $"\n"
                    + $"{targetDistance:0.0}\n"
                    + $"\n");
                } else {
                    data2.Append($"\n"
                    + $"\n"
                    + $"\n"
                    + $"\n");

                    data4.Append($"\n"
                    + $"\n"
                    + $"\n"
                    + $"\n");
                }

                if (missilesLog.Count != 0) { data5.Append($"\n\n\nMISSILES\n"); }
                foreach (MyTuple<string, string, string, string, string> log in missilesLog) {//toTarget=Item1,speed=Item2,command=command,status=status,type=type\n
                    data.Append($"\n");
                    data.Append($"Speed:\n");
                    data.Append($"Status:\n");

                    data2.Append($"{log.Item5}\n");
                    data2.Append($"{log.Item2}\n");
                    data2.Append($"{log.Item4}\n");

                    data3.Append($"To Target:\n");
                    data3.Append($"Command:\n");
                    data3.Append($"\n");

                    data4.Append($"{log.Item1}\n");
                    data4.Append($"{log.Item3}\n");
                    data4.Append($"\n");
                }

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X, myPanel.col2_4.Y + 10f), data2.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width, myPanel.col3_4.Y + 10f), data3.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X, myPanel.col4_4.Y + 10f), data4.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data5.ToString(), "Default", myPanel.minScale, new Color(25, 0, 100), TextAlignment.RIGHT));

                data.Clear();
                data2.Clear();
                data3.Clear();
                data4.Clear();
                data5.Clear();

                MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
                foreach (var sprite in sprites) {
                    frame.Add(sprite);
                }
                frame.Dispose();
                sprites.Clear();
            }
        }

        void LogPower() {
            Echo($"LogPower");

            foreach (MyPanel myPanel in POWER) {
                data.Append($"Status: \n"
                    + $"Pow.: \n"
                    + $"Batt. Out: \n"
                    + $"Batt. Pow: \n"
                    + $"Reactors: \n"
                    + $"H2: \n"
                    + $"Solar: \n"
                    + $"H2 Tank: \n");

                data2.Append($"{powerStatus}\n"
                    + $"{terminalCurrentInput:0.0}/{terminalMaxRequiredInput:0.0}\n"
                    + $"{battsCurrentOutput:0.0}/{battsMaxOutput:0.0}\n"
                    + $"{battsCurrentStoredPower}\n"
                    + $"{reactorsCurrentOutput:0.0}/{reactorsMaxOutput:0.0}\n"
                    + $"{hEngCurrentOutput:0.0}/{hEngMaxOutput:0.0}\n"
                    + $"{solarMaxOutput:0.0}\n"
                    + $"{tankCapacityPercent:0.0}%\n");

                data3.Append($"\n"
                    + $"\n"
                    + $"In: \n"
                    + $"\n"
                    + $"\n"
                    + $"\n"
                    + $"Turbines: \n");

                data4.Append($"\n"
                    + $"\n"
                    + $"{battsCurrentInput:0.0}\n"
                    + $"\n"
                    + $"\n"
                    + $"\n"
                    + $"{turbineMaxOutput:0.0}\n");

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 20f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 20f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 20f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 20f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                data.Clear();
                data2.Clear();
                data3.Clear();
                data4.Clear();

                MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
                foreach (var sprite in sprites) {
                    frame.Add(sprite);
                }
                frame.Dispose();
                sprites.Clear();
            }
        }

        void LogInventory() {
            Echo($"LogInventory");

            foreach (MyPanel myPanel in COMPONENTSAMMO) {
                data5.Append($"COMPONENTS\n\n\n\n\n\n\n\n\n\n\n\n\nAMMO");

                data.Append($"\n");
                data2.Append($"\n");
                data3.Append($"\n");
                data4.Append($"\n");

                bool alternate = true;
                foreach (var log in componentsLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: \n");
                        alternate = false;
                    } else {
                        data3.Append($"{log.Key.Replace("RadioCommunication", "RadioComm.")}: \n");
                        alternate = true;
                    }
                }
                alternate = true;
                foreach (var log in componentsLogDict) {
                    if (alternate) {
                        data2.Append($"{log.Value}\n");
                        alternate = false;
                    } else {
                        data4.Append($"{log.Value}\n");
                        alternate = true;
                    }
                }

                data.Append($"\n");
                data2.Append($"\n");
                data3.Append($"\n\n");
                data4.Append($"\n\n");

                alternate = true;
                foreach (var log in ammoLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key.Replace("Ammo", "").Replace("Clip", "")}: \n");
                        alternate = false;
                    } else {
                        data3.Append($"{log.Key.Replace("Ammo", "").Replace("Clip", "")}: \n");
                        alternate = true;
                    }
                }
                alternate = true;
                foreach (var log in ammoLogDict) {
                    if (alternate) {
                        data2.Append($"{log.Value}\n");
                        alternate = false;
                    } else {
                        data4.Append($"{log.Value}\n");
                        alternate = true;
                    }
                }

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale - 0.1f, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 75f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale - 0.1f, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale - 0.1f, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale - 0.1f, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale - 0.1f, new Color(25, 0, 100), TextAlignment.RIGHT));

                data.Clear();
                data2.Clear();
                data3.Clear();
                data4.Clear();
                data5.Clear();

                MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
                foreach (var sprite in sprites) {
                    frame.Add(sprite);
                }
                frame.Dispose();
                sprites.Clear();
            }

            foreach (MyPanel myPanel in OREINGOTS) {
                data5.Append($"\nORE\n\n\n\n\n\n\n\nINGOTS");

                data.Append($"Cargo:\n\n");
                data2.Append($"{cargoPercentage:0.0}%\n\n");
                data3.Append($"\n\n");
                data4.Append($"\n\n");

                bool alternate = true;
                foreach (var log in oreLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: \n");
                        alternate = false;
                    } else {
                        data3.Append($"{log.Key}: \n");
                        alternate = true;
                    }
                }
                alternate = true;
                foreach (var log in oreLogDict) {
                    if (alternate) {
                        data2.Append($"{log.Value}\n");
                        alternate = false;
                    } else {
                        data4.Append($"{log.Value}\n");
                        alternate = true;
                    }
                }

                data.Append($"\n");
                data2.Append($"\n");
                data3.Append($"\n\n");
                data4.Append($"\n\n");

                alternate = true;
                foreach (var log in ingotsLogDict) {
                    if (alternate) {
                        data.Append($"{log.Key}: \n");
                        alternate = false;
                    } else {
                        data3.Append($"{log.Key}: \n");
                        alternate = true;
                    }
                }
                alternate = true;
                foreach (var log in ingotsLogDict) {
                    if (alternate) {
                        data2.Append($"{log.Value}\n");
                        alternate = false;
                    } else {
                        data4.Append($"{log.Value}\n");
                        alternate = true;
                    }
                }

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 50f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, new Color(0, 100, 100), TextAlignment.RIGHT));
                sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, new Color(100, 0, 100), TextAlignment.LEFT));

                sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale, new Color(25, 0, 100), TextAlignment.RIGHT));

                data.Clear();
                data2.Clear();
                data3.Clear();
                data4.Clear();
                data5.Clear();

                MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
                foreach (var sprite in sprites) {
                    frame.Add(sprite);
                }
                frame.Dispose();
                sprites.Clear();
            }
        }

        MySprite DrawSpriteText(Vector2 pos, string data, string font, float scale, Color? color = null, TextAlignment alignment = TextAlignment.LEFT) {
            return new MySprite() {
                Type = SpriteType.TEXT,
                Data = data,
                RotationOrScale = scale,
                Position = pos,
                FontId = font,
                Color = color,
                Alignment = alignment
            };
        }

        void GetBlocks() {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits, block => block.CustomName.Contains("[CRX] Controller Cockpit"));

            NAVIGATOR.Clear();
            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Navigator"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }
            foreach (var surface in surfaces) {
                NAVIGATOR.Add(new MyPanel(surface, false));
            }
            surfaces.Clear();
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "RangeFinderSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToString())) {
                    int cockpitRangeFinderSurface = myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToInt32();
                    surfaces.Add(cockpit.GetSurface(cockpitRangeFinderSurface));//4
                }
            }
            foreach (var surface in surfaces) {
                NAVIGATOR.Add(new MyPanel(surface, true));
            }
            surfaces.Clear();
            panels.Clear();

            PAINTER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Painter"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }
            foreach (var surface in surfaces) {
                PAINTER.Add(new MyPanel(surface, false));
            }
            surfaces.Clear();
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "MissilesSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("MissilesSettings", "cockpitTargetSurface").ToString())) {
                    int cockpitTargetSurface = myIni.Get("MissilesSettings", "cockpitTargetSurface").ToInt32();
                    surfaces.Add(cockpit.GetSurface(cockpitTargetSurface));//0
                }
            }
            foreach (var surface in surfaces) {
                PAINTER.Add(new MyPanel(surface, true));
            }
            surfaces.Clear();
            panels.Clear();

            POWER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Power"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }
            foreach (var surface in surfaces) {
                POWER.Add(new MyPanel(surface, false));
            }
            surfaces.Clear();
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "ManagerSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("ManagerSettings", "cockpitPowerSurface").ToString())) {
                    int cockpitPowerSurface = myIni.Get("ManagerSettings", "cockpitPowerSurface").ToInt32();
                    surfaces.Add(cockpit.GetSurface(cockpitPowerSurface));//2
                }
            }
            foreach (var surface in surfaces) {
                POWER.Add(new MyPanel(surface, true));
            }
            surfaces.Clear();
            panels.Clear();

            OREINGOTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Ore Ingots"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }
            foreach (var surface in surfaces) {
                OREINGOTS.Add(new MyPanel(surface, false));
            }
            surfaces.Clear();
            panels.Clear();

            COMPONENTSAMMO.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Components Ammo"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }
            foreach (var surface in surfaces) {
                COMPONENTSAMMO.Add(new MyPanel(surface, false));
            }
            surfaces.Clear();
            panels.Clear();
        }

        public class MyPanel {
            public readonly IMyTextSurface surface;
            public readonly float minScale;
            public readonly RectangleF col1_4;
            public readonly RectangleF col2_4;
            public readonly RectangleF col3_4;
            public readonly RectangleF col4_4;
            public readonly RectangleF col1_3;
            public readonly RectangleF col2_3;

            public MyPanel(IMyTextSurface _surface, bool _isWide) {
                surface = _surface;
                Vector2 scale = _surface.SurfaceSize / 512f;
                minScale = Math.Min(scale.X, scale.Y);
                if (_isWide) {
                    minScale += 0.2f;
                }
                col1_4 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 4f, new Vector2(surface.SurfaceSize.X / 4f, surface.SurfaceSize.Y));
                col2_4 = new RectangleF(col1_4.X + (surface.SurfaceSize.X / 4f), col1_4.Y, col1_4.Width, col1_4.Height);
                col3_4 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 4f, new Vector2(surface.SurfaceSize.X / 4f * 3f, surface.SurfaceSize.Y));
                col4_4 = new RectangleF(col3_4.X + (surface.SurfaceSize.X / 4f * 3f), col3_4.Y, col3_4.Width, col3_4.Height);

                col1_3 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 3f, new Vector2(surface.SurfaceSize.X / 3f, surface.SurfaceSize.Y));
                col2_3 = new RectangleF(col1_3.X + (surface.SurfaceSize.X / 3f), col1_3.Y, col1_3.Width, col1_3.Height);

                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.BackgroundColor = Color.Black;
                surface.FontColor = new Color(0, 100, 0);
            }
        }

    }
}
