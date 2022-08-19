﻿using Sandbox.Game.EntityComponents;
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
            Me.GetSurface(0).BackgroundColor = logger ? new Color(25, 0, 100) : new Color(0, 0, 0);
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
                        Me.GetSurface(0).BackgroundColor = new Color(25, 0, 100);
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
                DrawSpritesTabsComponentsAmmo(frame, myPanel.viewport.Center);//TODO
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
                DrawSpritesTabsOreIngots(frame, myPanel.viewport.Center);//TODO
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

        public void DrawSpritesTabsOreIngots(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            Color transparentBlue = new Color(0, 0, 255, 20);
            Color tranparentMagenta = new Color(64, 0, 64, 20);
            Color tranparentBrightMagenta = new Color(128, 0, 128, 20);
            Color neonAzure = new Color(0, 255, 255, 255);
            Color tranparentNeonAzure = new Color(0, 100, 100, 20);

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -90f) * scale + centerPos, new Vector2(500f, 179f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 0f)); // purple base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 12f) * scale + centerPos, new Vector2(350f, 25f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(112f, 12f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 1.5708f)); // purple bottom triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -192f) * scale + centerPos, new Vector2(275f, 25f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 0f)); // purple top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -192f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 4.7124f)); // purple top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -77f) * scale + centerPos, new Vector2(2f, 206f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -101f) * scale + centerPos, new Vector2(2f, 204f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 25f) * scale + centerPos, new Vector2(351f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 0f) * scale + centerPos, new Vector2(126f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 12f) * scale + centerPos, new Vector2(35f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 2.3562f)); // purple bottom line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -179f) * scale + centerPos, new Vector2(200f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -204f) * scale + centerPos, new Vector2(277f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // purple top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -192f) * scale + centerPos, new Vector2(36f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 2.3736f)); // purple  top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -225f) * scale + centerPos, new Vector2(501f, 40f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // inv bar
            if (cargoPercentage > 0d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(5f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 10
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(5f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 10
            }
            if (cargoPercentage > 10d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(20f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 20
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(20f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 20
            }
            if (cargoPercentage > 20d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(35f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 30
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(35f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 30
            }
            if (cargoPercentage > 30d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(50f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 40
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(50f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 40
            }
            if (cargoPercentage > 40d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(65f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 50
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(65f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 50
            }
            if (cargoPercentage > 50d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(80f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 60
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(80f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 60
            }
            if (cargoPercentage > 60d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(95f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 70
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(95f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 70
            }
            if (cargoPercentage > 70d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(110f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 80
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(110f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 80
            }
            if (cargoPercentage > 80d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(125f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 90
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(125f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 90
            }
            if (cargoPercentage > 90d) {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(140f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, neonAzure, null, TextAlignment.CENTER, 0f)); // inv 100
            } else {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(140f, -223f) * scale + centerPos, new Vector2(10f, 25f) * scale, tranparentNeonAzure, null, TextAlignment.CENTER, 0f)); // inv 100
            }
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(155f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle6
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(175f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle5
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(195f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle4
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(215f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle3
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(235f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle2
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(135f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle1
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 144f) * scale + centerPos, new Vector2(500f, 184f) * scale, new Color(0, 0, 128, 20), null, TextAlignment.CENTER, 0f)); // dark blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 238f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 51f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
        }

        public void DrawSpritesTabsComponentsAmmo(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            Color tranparentBrightMagenta = new Color(128, 0, 128, 20);
            Color tranparentDarkBlue = new Color(0, 0, 128, 20);
            Color tranparentBlue = new Color(0, 0, 255, 20);
            Color tranparentMagenta = new Color(64, 0, 64, 20);

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -64f) * scale + centerPos, new Vector2(500f, 286f) * scale, tranparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 91f) * scale + centerPos, new Vector2(350f, 25f) * scale, tranparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(112f, 91f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentDarkBlue, null, TextAlignment.CENTER, 1.5708f)); // blue bottom triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -219f) * scale + centerPos, new Vector2(275f, 25f) * scale, tranparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -219f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentDarkBlue, null, TextAlignment.CENTER, 4.7124f)); // blue top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -52f) * scale + centerPos, new Vector2(2f, 311f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -77f) * scale + centerPos, new Vector2(2f, 310f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 103f) * scale + centerPos, new Vector2(351f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 79f) * scale + centerPos, new Vector2(126f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 90f) * scale + centerPos, new Vector2(35f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 2.3562f)); // blue bottom line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -207f) * scale + centerPos, new Vector2(200f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -232f) * scale + centerPos, new Vector2(277f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -220f) * scale + centerPos, new Vector2(36f, 2f) * scale, tranparentBlue, null, TextAlignment.CENTER, 2.3736f)); // blue top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 181f) * scale + centerPos, new Vector2(500f, 112f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 113f) * scale + centerPos, new Vector2(123f, 24f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta top corne
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(114f, 113f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentMagenta, null, TextAlignment.CENTER, 4.7124f)); // magenta top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(187f, 225f) * scale + centerPos, new Vector2(123f, 24f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(113f, 225f) * scale + centerPos, new Vector2(25f, 25f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 4.7124f)); // magenta top triangleCopy
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 238f) * scale + centerPos, new Vector2(500f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta bottom line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 125f) * scale + centerPos, new Vector2(351f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta top line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 101f) * scale + centerPos, new Vector2(126f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 170f) * scale + centerPos, new Vector2(2f, 137f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 113f) * scale + centerPos, new Vector2(35f, 2f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 2.3562f)); // magenta top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 181f) * scale + centerPos, new Vector2(2f, 113f) * scale, tranparentBrightMagenta, null, TextAlignment.CENTER, 0f)); // magenta left line
        }

        void GetBlocks() {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits, block => block.CustomName.Contains("[CRX] Controller Cockpit"));

            NAVIGATOR.Clear();
            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Navigator"));
            foreach (IMyTextPanel panel in panels) { surfaces.Add(panel as IMyTextSurface); }//TODO panel.BlockDefinition.SubtypeId
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

            public readonly RectangleF viewport;

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

                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.BackgroundColor = Color.Black;
                surface.FontColor = new Color(0, 100, 0);
            }
        }

    }
}
