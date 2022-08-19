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
        bool beautifyLog = true;

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
        float battsCurrentStoredPower;
        float battsMaxStoredPower;
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

        Color transparentBlue = new Color(0, 0, 255, 20);
        Color transparentDarkBlue = new Color(0, 0, 128, 20);
        Color transparentNeonAzure = new Color(0, 255, 255, 20);
        Color neonAzure = new Color(0, 255, 255, 255);
        Color transparentMagenta = new Color(64, 0, 64, 20);
        Color transparentNeonMagenta = new Color(128, 0, 128, 20);
        Color magenta = new Color(100, 0, 100);
        Color purple = new Color(25, 0, 100);
        Color azure = new Color(0, 100, 100);

        Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[LOGGER]");
            //BROADCASTLISTENER.SetMessageCallback();
            Me.GetSurface(0).BackgroundColor = logger ? purple : Color.Black;
            stateMachine = RunOverTime();
        }

        public void Main(string arg, UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (!logger) {
                        Me.GetSurface(0).BackgroundColor = Color.Black;
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    } else {
                        Me.GetSurface(0).BackgroundColor = purple;
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    }
                } else {
                    if ((updateType & UpdateType.Update10) == UpdateType.Update10) {
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
                foreach (MyPanel myPanel in NAVIGATOR) {
                    LogNavigator(myPanel);
                    yield return true;
                }
                navigator = false;
            }

            foreach (MyPanel myPanel in PAINTER) {
                LogPainter(myPanel);
                yield return true;
            }

            if (power) {
                foreach (MyPanel myPanel in POWER) {
                    LogPower(myPanel);
                    yield return true;
                }
                power = false;
            }

            if (inventory) {
                foreach (MyPanel myPanel in COMPONENTSAMMO) {
                    LogComponentsAmmo(myPanel);
                    yield return true;
                }
                foreach (MyPanel myPanel in OREINGOTS) {
                    LogOreIngots(myPanel);
                    yield return true;
                }
                inventory = false;
            }
        }

        public void RunStateMachine() {
            if (stateMachine != null) {
                bool hasMoreSteps = stateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
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
                case "ToggleBeautify":
                    beautifyLog = !beautifyLog;
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
                    MyTuple<float, float, float, int, float, float>,
                    MyTuple<float, float, int>,
                    MyTuple<float, float, int>,
                    MyTuple<float, int, float, int>,
                    double
                >) {
                    var data = (MyTuple<
                        MyTuple<string, float, float>,
                        MyTuple<float, float, float, int, float, float>,
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
                    battsMaxStoredPower = data.Item2.Item6;
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

        void LogNavigator(MyPanel myPanel) {
            Echo($"LogNavigator");

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
                + $"Safe Pos XYZ: ");

            data2.Append($"\n"
                + $"{timeRemaining}s\n"
                + $"{currentStoredPower:0.#}/{maxStoredPower:0.#}\n"
                + $"{currentJump:###,###,###} ({totJumpPercent:0.#}%)\n"
                + $"{maxJump:###,###,###}\n");

            if (!Vector3D.IsZero(rangeFinderPosition)) {
                data2.Append($"\n"
                + $"{rangeFinderName}\n"
                + $"{(int)rangeFinderDistance}\n"
                + $"{(int)rangeFinderDiameter}\n"
                + $"{rangeFinderPosition.X:0.#},{rangeFinderPosition.Y:0.#},{rangeFinderPosition.Z:0.#}");
            } else {
                data2.Append($"\n\n\n\n");
            }

            data3.Append($"JUMP DRIVE\n\n\n\n\nRANGE FINDER");

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + 20f, myPanel.col2_3.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data3.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

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

        void LogPainter(MyPanel myPanel) {
            Echo($"LogPainter");

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
                + $"{targetVelocity.Length():0.#}\n"
                + $"X:{targetPosition.X:0.#}, Y:{targetPosition.Y:0.#}, Z:{targetPosition.Z:0.#}\n");

                data4.Append($"\n"
                + $"\n"
                + $"{targetDistance:0.#}\n"
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

            data5.Append($"\n\n\n\nMISSILES\n");
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

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X, myPanel.col2_4.Y + 10f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width, myPanel.col3_4.Y + 10f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X, myPanel.col4_4.Y + 10f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data5.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

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

        void LogPower(MyPanel myPanel) {
            Echo($"LogPower");

            data.Append($"Status: \n"
                + $"Pow.: \n"
                + $"Batt. Out: \n"
                + $"Batt. Pow: \n"
                + $"Reactors: \n"
                + $"H2: \n"
                + $"Solar: \n"
                + $"H2 Tank: \n");

            data2.Append($"{powerStatus}\n"
                + $"{terminalCurrentInput:0.#}/{terminalMaxRequiredInput:0.#}\n"
                + $"{battsCurrentOutput:0.#}/{battsMaxOutput:0.#}\n"
                + $"{battsCurrentStoredPower:0.#}/{battsMaxStoredPower:0.#}\n"
                + $"{reactorsCurrentOutput:0.#}/{reactorsMaxOutput:0.#}\n"
                + $"{hEngCurrentOutput:0.#}/{hEngMaxOutput:0.#}\n"
                + $"{solarMaxOutput:0.#}\n"
                + $"{tankCapacityPercent:0.#}%\n");

            data3.Append($"\n"
                + $"\n"
                + $"In: \n"
                + $"\n"
                + $"\n"
                + $"\n"
                + $"Turbines: \n");

            data4.Append($"\n"
                + $"\n"
                + $"{battsCurrentInput:0.###}\n"
                + $"\n"
                + $"\n"
                + $"\n"
                + $"{turbineMaxOutput:0.#}\n");

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 20f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 20f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 20f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 20f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsPower(frame, myPanel.viewport.Center, myPanel.minScale);
            }
            foreach (var sprite in sprites) {
                frame.Add(sprite);
            }
            frame.Dispose();
            sprites.Clear();
        }

        void LogComponentsAmmo(MyPanel myPanel) {
            Echo($"LogComponentsAmmo");

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

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 75f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale - 0.1f, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
            data5.Clear();

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsComponentsAmmo(frame, myPanel.viewport.Center, myPanel.minScale);
            }
            foreach (var sprite in sprites) {
                frame.Add(sprite);
            }
            frame.Dispose();
            sprites.Clear();
        }

        void LogOreIngots(MyPanel myPanel) {
            Echo($"LogOreIngots");

            data5.Append($"\nORE\n\n\n\n\n\n\n\nINGOTS");

            data.Append($"Cargo:\n\n");
            data2.Append($"{cargoPercentage:0.#}%\n\n");
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

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 50f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            sprites.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            sprites.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
            data5.Clear();

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();
            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsOreIngots(frame, myPanel.viewport.Center, myPanel.animationCount, myPanel.minScale);
                myPanel.animationCount++;
                if (myPanel.animationCount > 5) {
                    myPanel.animationCount = 0;
                }
            }
            foreach (var sprite in sprites) {
                frame.Add(sprite);
            }
            frame.Dispose();
            sprites.Clear();
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

        public void DrawSpritesTabsOreIngots(MySpriteDrawFrame frame, Vector2 centerPos, int animationCount, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -90f) * scale + centerPos, new Vector2(500f, 179f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 12f) * scale + centerPos, new Vector2(350f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(112f, 12f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 1.5708f)); // purple bottom triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -192f) * scale + centerPos, new Vector2(275f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -192f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 4.7124f)); // purple top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -77f) * scale + centerPos, new Vector2(2f, 206f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -101f) * scale + centerPos, new Vector2(2f, 204f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 25f) * scale + centerPos, new Vector2(351f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 0f) * scale + centerPos, new Vector2(126f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 12f) * scale + centerPos, new Vector2(35f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 2.3562f)); // purple bottom line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -179f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -204f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -192f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 2.3736f)); // purple  top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -225f) * scale + centerPos, new Vector2(501f, 40f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // inv bar

            DrawStatusBar(frame, new Vector2(90f, -223f) * scale + centerPos, new Vector2(200f, 25f) * scale, (float)cargoPercentage / 100f, transparentBlue, neonAzure, TextAlignment.LEFT);

            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(235f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 0 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle2
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(215f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 1 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle3
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(195f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 2 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle4
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(175f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 3 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle5
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(155f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 4 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle6
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(135f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 5 ? neonAzure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle1

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 144f) * scale + centerPos, new Vector2(500f, 184f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // dark blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 238f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 51f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
        }

        public void DrawSpritesTabsComponentsAmmo(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -64f) * scale + centerPos, new Vector2(500f, 286f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 91f) * scale + centerPos, new Vector2(350f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(112f, 91f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 1.5708f)); // blue bottom triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -219f) * scale + centerPos, new Vector2(275f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -219f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 4.7124f)); // blue top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -52f) * scale + centerPos, new Vector2(2f, 311f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -77f) * scale + centerPos, new Vector2(2f, 310f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 103f) * scale + centerPos, new Vector2(351f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 79f) * scale + centerPos, new Vector2(126f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 90f) * scale + centerPos, new Vector2(35f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 2.3562f)); // blue bottom line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -207f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -232f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -220f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 2.3736f)); // blue top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 181f) * scale + centerPos, new Vector2(500f, 112f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 113f) * scale + centerPos, new Vector2(123f, 24f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta top corne
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(114f, 113f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 4.7124f)); // magenta top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(187f, 225f) * scale + centerPos, new Vector2(123f, 24f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta bottom corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(113f, 225f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 4.7124f)); // magenta top triangleCopy
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 238f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta bottom line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-75f, 125f) * scale + centerPos, new Vector2(351f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta top line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(188f, 101f) * scale + centerPos, new Vector2(126f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 170f) * scale + centerPos, new Vector2(2f, 137f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, 113f) * scale + centerPos, new Vector2(35f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 2.3562f)); // magenta top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 181f) * scale + centerPos, new Vector2(2f, 113f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // magenta left line
        }

        public void DrawSpritesTabsPower(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -120f) * scale + centerPos, new Vector2(500f, 240f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -120f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -120f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -240f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // top line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 0f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // bottom line

            DrawStatusBar(frame, new Vector2(-15f, -18f) * scale + centerPos, new Vector2(200f, 25f) * scale, (float)tankCapacityPercent / 100f, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);

            if (hEngMaxOutput != 0f) {
                DrawStatusBar(frame, new Vector2(-15f, -76f) * scale + centerPos, new Vector2(200f, 25f) * scale, hEngCurrentOutput / hEngMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (reactorsMaxOutput != 0f) {
                DrawStatusBar(frame, new Vector2(-15f, -105f) * scale + centerPos, new Vector2(200f, 25f) * scale, reactorsCurrentOutput / reactorsMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (battsMaxStoredPower != 0f) {
                DrawStatusBar(frame, new Vector2(-15f, -134f) * scale + centerPos, new Vector2(200f, 25f) * scale, battsCurrentStoredPower / battsMaxStoredPower, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (battsMaxOutput != 0f) {
                DrawStatusBar(frame, new Vector2(-15f, -163f) * scale + centerPos, new Vector2(200f, 25f) * scale, battsCurrentOutput / battsMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (terminalMaxRequiredInput != 0f) {
                DrawStatusBar(frame, new Vector2(-15f, -192f) * scale + centerPos, new Vector2(200f, 25f) * scale, terminalCurrentInput / terminalMaxRequiredInput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
        }

        void DrawStatusBar(MySpriteDrawFrame frame, Vector2 position, Vector2 size, float proportion, Color backgroundColor, Color barColor, TextAlignment barAlignment) {
            proportion = MathHelper.Clamp(proportion, 0, 1);

            var barBackground = MySprite.CreateSprite("SquareSimple", position, size);
            barBackground.Color = backgroundColor;
            frame.Add(barBackground);

            Vector2 barSize = size * new Vector2(proportion, 1f);

            Vector2 barPosition;
            switch (barAlignment) {
                default:
                case TextAlignment.CENTER:
                    barPosition = position;
                    break;
                case TextAlignment.LEFT:
                    barPosition = position + new Vector2(-0.5f * (size.X - barSize.X), 0);
                    break;
                case TextAlignment.RIGHT:
                    barPosition = position + new Vector2(0.5f * (size.X - barSize.X), 0);
                    break;
            }
            var barSprite = MySprite.CreateSprite("SquareSimple", barPosition, barSize);
            barSprite.Color = barColor;
            frame.Add(barSprite);
        }

        void GetBlocks() {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits, block => block.CustomName.Contains("[CRX] Controller Cockpit"));

            NAVIGATOR.Clear();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Navigator"));
            foreach (IMyTextPanel panel in panels) {
                NAVIGATOR.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "RangeFinderSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToString())) {
                    int cockpitRangeFinderSurface = myIni.Get("RangeFinderSettings", "cockpitRangeFinderSurface").ToInt32();
                    NAVIGATOR.Add(new MyPanel(cockpit.GetSurface(cockpitRangeFinderSurface), "SmallLCDPanel"));//TODO 1
                }
            }
            panels.Clear();

            PAINTER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Painter"));
            foreach (IMyTextPanel panel in panels) {
                PAINTER.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "MissilesSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("MissilesSettings", "cockpitTargetSurface").ToString())) {
                    int cockpitTargetSurface = myIni.Get("MissilesSettings", "cockpitTargetSurface").ToInt32();
                    PAINTER.Add(new MyPanel(cockpit.GetSurface(cockpitTargetSurface), "SmallLCDPanel"));//TODO 0
                }
            }
            panels.Clear();

            POWER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Power"));
            foreach (IMyTextPanel panel in panels) {
                POWER.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "ManagerSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("ManagerSettings", "cockpitPowerSurface").ToString())) {
                    int cockpitPowerSurface = myIni.Get("ManagerSettings", "cockpitPowerSurface").ToInt32();
                    POWER.Add(new MyPanel(cockpit.GetSurface(cockpitPowerSurface), "SmallLCDPanel"));//TODO 2
                }
            }
            panels.Clear();

            OREINGOTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Ore Ingots"));
            foreach (IMyTextPanel panel in panels) {
                OREINGOTS.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            panels.Clear();

            COMPONENTSAMMO.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Components Ammo"));
            foreach (IMyTextPanel panel in panels) {
                COMPONENTSAMMO.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            panels.Clear();
        }

        public class MyPanel {
            public readonly string subTypeId;
            public readonly IMyTextSurface surface;
            public readonly float minScale;
            public readonly RectangleF col1_4;
            public readonly RectangleF col2_4;
            public readonly RectangleF col3_4;
            public readonly RectangleF col4_4;
            public readonly RectangleF col1_3;
            public readonly RectangleF col2_3;
            public readonly RectangleF viewport;
            public int animationCount = 0;

            public MyPanel(IMyTextSurface _surface, string _subTypeId) {
                subTypeId = _subTypeId;
                surface = _surface;
                Vector2 scale = _surface.SurfaceSize / 512f;
                minScale = Math.Min(scale.X, scale.Y);
                if (_subTypeId == "SmallLCDPanel") {//TODO cockpit panel
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
                surface.FontColor = Color.Magenta;
            }
        }

    }
}
