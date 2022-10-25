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
        bool painter = false;

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
        float battsCurrentStoredPower;
        float battsMaxStoredPower;
        float reactorsCurrentOutput;
        float reactorsMaxOutput;
        float hEngCurrentOutput;
        float hEngMaxOutput;
        float solarMaxOutput;
        float turbineMaxOutput;
        double tankCapacityPercent;

        double cargoPercentage;
        Dictionary<string, string> ammoLogDict = new Dictionary<string, string>();//SubtypeId=value
        Dictionary<string, string> oreLogDict = new Dictionary<string, string>();
        Dictionary<string, string> ingotsLogDict = new Dictionary<string, string>();
        Dictionary<string, string> componentsLogDict = new Dictionary<string, string>();

        int weaponType = 0;
        bool readyToFire = true;

        int selectedPayLoad = 0;
        bool missilesLoaded = true;

        int selectedDrop = 0;
        bool toggleDecoy = false;
        bool readyDecoy = true;
        bool readyJolt = true;
        bool toggleJolt = false;

        bool autoMissiles = false;
        bool autoSwitchGuns = false;
        bool sequenceWeapons = false;
        bool creative = false;
        bool autoFire = false;

        bool magneticDrive = false;
        bool idleThrusters = false;
        bool sunAlign = false;
        bool safetyDampeners = false;
        bool useGyrosToStabilize = false;
        bool autoCombat = false;
        bool obstaclesAvoidance = false;
        bool enemyEvasion = false;
        bool keepAltitude = false;
        bool moddedSensor = false;
        bool closeRangeCombat = false;

        double commercialTime = 0d;
        readonly double commercialTimeDelay = 1d;
        int brightnessIndex = 0;
        readonly int brightnessCount = 3;

        public List<MyPanel> POWER = new List<MyPanel>();
        public List<MyPanel> NAVIGATOR = new List<MyPanel>();
        public List<MyPanel> PAINTER = new List<MyPanel>();
        public List<MyPanel> OREINGOTS = new List<MyPanel>();
        public List<MyPanel> COMPONENTSAMMO = new List<MyPanel>();
        public List<MyPanel> OVERVIEW = new List<MyPanel>();
        public List<MyPanel> COMMERCIALS = new List<MyPanel>();

        IMyTextPanel LCDBEAUTIFY;
        IMyTextPanel LCDBRIGHTNESS;

        readonly MyIni myIni = new MyIni();
        public IMyBroadcastListener BROADCASTLISTENER;
        IEnumerator<bool> stateMachine;
        IEnumerator<bool> powerStateMachine;

        public StringBuilder data = new StringBuilder("");
        public StringBuilder data2 = new StringBuilder("");
        public StringBuilder data3 = new StringBuilder("");
        public StringBuilder data4 = new StringBuilder("");
        public StringBuilder data5 = new StringBuilder("");

        public List<Vector2> reactorsOutputs = new List<Vector2>();
        public List<Vector2> hEngOutputs = new List<Vector2>();
        public List<Vector2> tankCapacityOutputs = new List<Vector2>();
        public List<Vector2> battsCurrentStoredPowers = new List<Vector2>();
        public List<Vector2> batteriesOutputs = new List<Vector2>();
        public List<Vector2> terminalOutputs = new List<Vector2>();
        public List<Vector2> randomPositions1 = new List<Vector2>();
        public List<Vector2> randomPositions2 = new List<Vector2>();

        Color transparentBlue = new Color(0, 0, 255, 20);
        Color transparentDarkBlue = new Color(0, 0, 128, 20);
        Color transparentNeonAzure = new Color(0, 255, 255, 20);
        Color transparentMagenta = new Color(64, 0, 64, 20);
        Color transparentNeonMagenta = new Color(128, 0, 128, 20);
        Color deepPurple = new Color(64, 0, 128, 20);
        Color deepBlue = new Color(0, 0, 64, 20);
        Color transparentGreen = new Color(0, 255, 0, 20);
        Color magenta = new Color(100, 0, 100);
        Color purple = new Color(25, 0, 100);
        Color azure = new Color(0, 100, 100);

        public Random random = new Random();

        public List<int> alphaList = new List<int>() { 20, 30, 40, 50 };

        public List<Color> magentaList = new List<Color>() {
            new Color(100, 0, 100),
            new Color(150, 0, 150),
            new Color(200, 0, 200),
            new Color(255, 0, 255)
        };

        public List<Color> purpleList = new List<Color>() {
            new Color(25, 0, 100),
            new Color(75, 0, 150),
            new Color(125, 0, 200),
            new Color(180, 0, 255)
        };

        public List<Color> azureList = new List<Color>() {
            new Color(0, 100, 100),
            new Color(0, 150, 150),
            new Color(0, 200, 200),
            new Color(0, 255, 255)
        };

        public Dictionary<int, string> commercialsDict = new Dictionary<int, string> {
            {0,"LCD_Frozen_Poster01"},
            {1,"LCD_Frozen_Poster02"},
            {2,"LCD_Frozen_Poster03"},
            {3,"LCD_Frozen_Poster04"},
            {4,"LCD_Frozen_Poster05"},
            {5,"LCD_Frozen_Poster06"},
            {6,"LCD_Frozen_Poster07"},
            {7,"LCD_HI_Poster1_Square"},
            {8,"LCD_HI_Poster2_Square"},
            {9,"LCD_HI_Poster3_Square"},
            {10,"LCD_SoF_BrightFuture_Square"},
            {11,"LCD_SoF_CosmicTeam_Square"},
            {12,"LCD_SoF_Exploration_Square"},
            {13,"LCD_SoF_SpaceTravel_Square"},
            {14,"LCD_SoF_ThunderFleet_Square"}
        };

        Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            Setup();
        }

        void Setup() {
            GetBlocks();
            BROADCASTLISTENER = IGC.RegisterBroadcastListener("[LOGGER]");
            //BROADCASTLISTENER.SetMessageCallback();
            Me.GetSurface(0).BackgroundColor = logger ? new Color(25, 0, 100) : Color.Black;
            if (LCDBEAUTIFY != null) { LCDBEAUTIFY.BackgroundColor = beautifyLog ? new Color(25, 0, 100) : Color.Black; }
            if (LCDBRIGHTNESS != null) { LCDBRIGHTNESS.BackgroundColor = new Color(25, 0, 100); }

            if (beautifyLog) {
                randomPositions1.Add(new Vector2(-240f, 225f));
                randomPositions1.Add(new Vector2(-120f, 225f));
                randomPositions1.Add(new Vector2(0f, 225f));
                randomPositions1.Add(new Vector2(120f, 225f));
                randomPositions1.Add(new Vector2(240f, 225f));

                randomPositions2.Add(new Vector2(-240f, 225f));
                randomPositions2.Add(new Vector2(-180f, 225f));
                randomPositions2.Add(new Vector2(-60f, 225f));
                randomPositions2.Add(new Vector2(60f, 225f));
                randomPositions2.Add(new Vector2(180f, 225f));
                randomPositions2.Add(new Vector2(240f, 225f));
            }
            stateMachine = RunOverTime();
            powerStateMachine = RunPowerOverTime();
        }

        public void Main(string arg, UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (!logger) {
                        if (LCDBEAUTIFY != null) { LCDBEAUTIFY.BackgroundColor = Color.Black; }
                        if (LCDBRIGHTNESS != null) { LCDBRIGHTNESS.BackgroundColor = Color.Black; }
                        Me.GetSurface(0).BackgroundColor = Color.Black;
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    } else {
                        if (LCDBEAUTIFY != null) { LCDBEAUTIFY.BackgroundColor = new Color(25, 0, 100); }
                        if (LCDBRIGHTNESS != null) { LCDBRIGHTNESS.BackgroundColor = new Color(25, 0, 100); }
                        Me.GetSurface(0).BackgroundColor = new Color(25, 0, 100);
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    }
                } else {
                    if ((updateType & UpdateType.Update10) == UpdateType.Update10) {
                        RunStateMachine();

                        RunPowerStateMachine();
                    }
                }
            } catch (Exception e) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    StringBuilder debugLog = new StringBuilder("");
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog, false);
                }
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        public IEnumerator<bool> RunOverTime() {
            double lastRun = Runtime.TimeSinceLastRun.TotalSeconds;

            if (COMMERCIALS.Count > 0) {
                if (commercialTime > commercialTimeDelay) {
                    commercialTime = 0;
                    foreach (MyPanel panel in COMMERCIALS) {
                        PlayCommercials(panel);
                    }
                    yield return true;
                } else {
                    commercialTime += lastRun;
                }
            }

            GetBroadcastMessages();
            yield return true;

            if (navigator) {
                foreach (MyPanel myPanel in NAVIGATOR) {
                    LogNavigator(myPanel);
                    yield return true;
                }
                navigator = false;
            }

            if (painter) {
                foreach (MyPanel myPanel in PAINTER) {
                    LogPainter(myPanel);
                    yield return true;
                }
                painter = false;
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

            foreach (MyPanel myPanel in OVERVIEW) {
                LogOverview(myPanel);
                yield return true;
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

        public IEnumerator<bool> RunPowerOverTime() {
            GetBroadcastPowerMessages();
            yield return true;

            if (power) {
                foreach (MyPanel myPanel in POWER) {

                    MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

                    LogPower(myPanel, ref frame);

                    if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {

                        DrawSpritesTabsPower(ref frame, myPanel.viewport.Center, myPanel.minScale);
                        yield return true;

                        Vector2 removePos = new Vector2(-240f, -230f) * myPanel.minScale + myPanel.viewport.Center;
                        Vector2 startPos = new Vector2(240f, 230f) * myPanel.minScale + myPanel.viewport.Center;
                        Vector2 movementPos = new Vector2(-5f, 0f) * myPanel.minScale;
                        float columnSizeMultiplier = 200f * myPanel.minScale / 100f;
                        float width = 2f * myPanel.minScale;

                        tankCapacityOutputs = DrawMovingGraph(ref frame, tankCapacityOutputs,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            (float)tankCapacityPercent,
                            width, transparentGreen);
                        yield return true;

                        hEngOutputs = DrawMovingGraph(ref frame, hEngOutputs,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            hEngCurrentOutput / hEngMaxOutput * 100f,
                            width, deepPurple);
                        yield return true;

                        reactorsOutputs = DrawMovingGraph(ref frame, reactorsOutputs,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            reactorsCurrentOutput / reactorsMaxOutput * 100f,
                            width, transparentBlue);
                        yield return true;

                        battsCurrentStoredPowers = DrawMovingGraph(ref frame, battsCurrentStoredPowers,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            battsCurrentStoredPower / battsMaxStoredPower * 100f,
                            width, transparentMagenta);
                        yield return true;

                        batteriesOutputs = DrawMovingGraph(ref frame, batteriesOutputs,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            battsCurrentOutput / battsMaxOutput * 100f,
                            width, transparentNeonMagenta);
                        yield return true;

                        terminalOutputs = DrawMovingGraph(ref frame, terminalOutputs,
                            movementPos,
                            removePos.X,
                            startPos,
                            columnSizeMultiplier,
                            terminalCurrentInput / terminalMaxRequiredInput * 100f,
                            width, transparentNeonAzure);

                    } else if (myPanel.subTypeId == "SmallCockpitPanel") {

                        string uranium;
                        ingotsLogDict.TryGetValue("Uranium", out uranium);

                        string ice;
                        oreLogDict.TryGetValue("Ice", out ice);

                        data.Append("\n\n\n\n\n\n\n\nUranium: ");
                        data2.Append($"\n\n\n\n\n\n\n\n{uranium:0.#}");
                        data3.Append("\n\n\n\n\n\n\n\nIce: ");
                        data4.Append($"\n\n\n\n\n\n\n\n{ice:0.#}");

                        frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 20f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
                        frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 20f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

                        frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 20f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
                        frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 20f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

                        data.Clear();
                        data2.Clear();
                        data3.Clear();
                        data4.Clear();
                    }

                    frame.Dispose();
                    yield return true;
                }
                power = false;
            }
        }

        public void RunPowerStateMachine() {
            if (powerStateMachine != null) {
                bool hasMoreSteps = powerStateMachine.MoveNext();
                if (hasMoreSteps) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                } else {
                    Echo($"PowerDispose");

                    powerStateMachine.Dispose();
                    powerStateMachine = RunPowerOverTime();
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
                    if (LCDBEAUTIFY != null) { LCDBEAUTIFY.BackgroundColor = beautifyLog ? purple : Color.Black; }
                    break;
                case "CycleBrightness":
                    brightnessIndex++;
                    if (brightnessIndex > brightnessCount) {
                        brightnessIndex = 0;
                    }
                    LCDBRIGHTNESS.WriteText($"\nCycle Log\nBrightness\nlvl: {brightnessIndex + 1}");
                    transparentBlue = new Color(0, 0, 255, alphaList[brightnessIndex]);
                    transparentDarkBlue = new Color(0, 0, 128, alphaList[brightnessIndex]);
                    transparentNeonAzure = new Color(0, 255, 255, alphaList[brightnessIndex]);
                    transparentMagenta = new Color(64, 0, 64, alphaList[brightnessIndex]);
                    transparentNeonMagenta = new Color(128, 0, 128, alphaList[brightnessIndex]);
                    deepPurple = new Color(64, 0, 128, alphaList[brightnessIndex]);
                    deepBlue = new Color(0, 0, 64, alphaList[brightnessIndex]);
                    transparentGreen = new Color(0, 255, 0, alphaList[brightnessIndex]);
                    magenta = magentaList[brightnessIndex];
                    purple = purpleList[brightnessIndex];
                    azure = azureList[brightnessIndex];
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
                    MyTuple<Vector3D, string, double, double>,
                    MyTuple<bool, bool, bool, bool, bool, bool>,
                    MyTuple<bool, bool, bool, bool, bool, bool>
                >) {
                    var data = (MyTuple<
                        MyTuple<string, int, int, double, double, double>,
                        MyTuple<Vector3D, string, double, double>,
                        MyTuple<bool, bool, bool, bool, bool, bool>,
                        MyTuple<bool, bool, bool, bool, bool>
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

                    magneticDrive = data.Item3.Item1;
                    idleThrusters = data.Item3.Item2;
                    sunAlign = data.Item3.Item3;
                    safetyDampeners = data.Item3.Item4;
                    useGyrosToStabilize = data.Item3.Item5;
                    autoCombat = data.Item3.Item6;

                    obstaclesAvoidance = data.Item4.Item1;
                    enemyEvasion = data.Item4.Item2;
                    keepAltitude = data.Item4.Item3;
                    moddedSensor = data.Item4.Item4;
                    closeRangeCombat = data.Item4.Item5;

                    navigator = true;
                }
                //PAINTER
                else if (igcMessage.Data is MyTuple<
                    MyTuple<string, Vector3D, Vector3D>,
                    string,
                    MyTuple<int, bool, bool, bool>,
                    MyTuple<int, bool, bool, bool, bool>
                >) {
                    var data = (MyTuple<
                        MyTuple<string, Vector3D, Vector3D>,
                        string,
                        MyTuple<int, bool, bool, bool>,
                        MyTuple<int, bool, bool, bool, bool>
                    >)igcMessage.Data;

                    targetName = data.Item1.Item1;
                    targetPosition = data.Item1.Item2;
                    targetVelocity = data.Item1.Item3;
                    targetDistance = Vector3D.Distance(targetPosition, Me.CubeGrid.WorldVolume.Center);

                    weaponType = data.Item3.Item1;
                    readyToFire = data.Item3.Item2;
                    creative = data.Item3.Item3;
                    autoFire = data.Item3.Item4;

                    selectedPayLoad = data.Item4.Item1;
                    autoMissiles = data.Item4.Item2;
                    autoSwitchGuns = data.Item4.Item3;
                    sequenceWeapons = data.Item4.Item4;
                    missilesLoaded = data.Item4.Item5;

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
                            var tuple = MyTuple.Create(values[0], values[1], values[2], values[3], values[4]);
                            missilesLog.Add(tuple);
                        }
                    }

                    painter = true;
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
                //SHOOTER
                else if (igcMessage.Data is MyTuple<int, bool, bool, bool, bool>) {
                    var data = (MyTuple<int, bool, bool, bool, bool>)igcMessage.Data;

                    selectedDrop = data.Item1;
                    readyJolt = data.Item2;
                    toggleJolt = data.Item3;
                    readyDecoy = data.Item4;
                    toggleDecoy = data.Item5;
                }
            }
        }

        void GetBroadcastPowerMessages() {
            Echo($"GetBroadcastPowerMessages");

            while (BROADCASTLISTENER.HasPendingMessage) {
                MyIGCMessage igcMessage = BROADCASTLISTENER.AcceptMessage();

                if (igcMessage.Data is MyTuple<
                    MyTuple<string, float, float>,
                    MyTuple<float, float, float, float, float>,
                    MyTuple<float, float>,
                    MyTuple<float, float>,
                    MyTuple<float, float>,
                    double
                >) {
                    var data = (MyTuple<
                        MyTuple<string, float, float>,
                        MyTuple<float, float, float, float, float>,
                        MyTuple<float, float>,
                        MyTuple<float, float>,
                        MyTuple<float, float>,
                        double
                    >)igcMessage.Data;

                    powerStatus = data.Item1.Item1;
                    terminalCurrentInput = data.Item1.Item2;
                    terminalMaxRequiredInput = data.Item1.Item3;

                    battsCurrentInput = data.Item2.Item1;
                    battsCurrentOutput = data.Item2.Item2;
                    battsMaxOutput = data.Item2.Item3;
                    battsCurrentStoredPower = data.Item2.Item4;
                    battsMaxStoredPower = data.Item2.Item5;

                    reactorsCurrentOutput = data.Item3.Item1;
                    reactorsMaxOutput = data.Item3.Item2;

                    hEngCurrentOutput = data.Item4.Item1;
                    hEngMaxOutput = data.Item4.Item2;

                    solarMaxOutput = data.Item5.Item1;
                    turbineMaxOutput = data.Item5.Item2;

                    tankCapacityPercent = data.Item6;

                    power = true;
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
            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

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

            data2.Append($"\n"
            + $"{rangeFinderName}\n");
            if (!Vector3D.IsZero(rangeFinderPosition)) {
                data2.Append($"{(int)rangeFinderDistance}\n"
                + $"{(int)rangeFinderDiameter}\n"
                + $"{rangeFinderPosition.X:0.#},{rangeFinderPosition.Y:0.#},{rangeFinderPosition.Z:0.#}");
            } else {
                data2.Append($"\n\n");
            }

            data3.Append($"JUMP DRIVE\n\n\n\n\nRANGE FINDER");

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + 20f, myPanel.col2_3.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + myPanel.col1_3.Width + 20f, myPanel.col1_3.Y + 20f), data3.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();

            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsJumpRangeFinder(ref frame, myPanel.viewport.Center, myPanel.minScale);

                float columnSizeMultiplier = 150f * myPanel.minScale / 100f;
                float width = 2f * myPanel.minScale;

                DrawRandomGraph(ref frame, randomPositions1,
                    columnSizeMultiplier,
                    width, transparentNeonAzure, myPanel.minScale, myPanel.viewport.Center);

                DrawRandomGraph(ref frame, randomPositions2,
                    columnSizeMultiplier,
                    width, transparentNeonMagenta, myPanel.minScale, myPanel.viewport.Center);

                randomPositions1.Clear();
                randomPositions1.Add(new Vector2(-240f, 225f));
                randomPositions1.Add(new Vector2(-120f, 225f));
                randomPositions1.Add(new Vector2(0f, 225f));
                randomPositions1.Add(new Vector2(120f, 225f));
                randomPositions1.Add(new Vector2(240f, 225f));

                randomPositions2.Clear();
                randomPositions2.Add(new Vector2(-240f, 225f));
                randomPositions2.Add(new Vector2(-180f, 225f));
                randomPositions2.Add(new Vector2(-60f, 225f));
                randomPositions2.Add(new Vector2(60f, 225f));
                randomPositions2.Add(new Vector2(180f, 225f));
                randomPositions2.Add(new Vector2(240f, 225f));
            }

            frame.Dispose();
        }

        void LogPainter(MyPanel myPanel) {
            Echo($"LogPainter");

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

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

            data.Append($"\n");
            data2.Append($"\n");
            data3.Append($"\n");
            data4.Append($"\n");
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

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X, myPanel.col2_4.Y + 10f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width, myPanel.col3_4.Y + 10f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X, myPanel.col4_4.Y + 10f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width, myPanel.col1_4.Y + 10f), data5.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
            data5.Clear();

            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsPainter(ref frame, myPanel.viewport.Center, myPanel.minScale);
            }

            frame.Dispose();
        }

        void LogPower(MyPanel myPanel, ref MySpriteDrawFrame frame) {
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

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 20f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 20f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 20f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 20f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
        }

        void LogComponentsAmmo(MyPanel myPanel) {
            Echo($"LogComponentsAmmo");

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

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

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 75f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 75f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale - 0.1f, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
            data5.Clear();

            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsComponentsAmmo(ref frame, myPanel.viewport.Center, myPanel.minScale);
            }

            frame.Dispose();
        }

        void LogOreIngots(MyPanel myPanel) {
            Echo($"LogOreIngots");

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

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

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + 50f, myPanel.col2_4.Y + 20f), data2.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + 50f, myPanel.col3_4.Y + 20f), data3.ToString(), "Default", myPanel.minScale, azure, TextAlignment.RIGHT));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + 50f, myPanel.col4_4.Y + 20f), data4.ToString(), "Default", myPanel.minScale, magenta, TextAlignment.LEFT));

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + 50f, myPanel.col1_4.Y + 20f), data5.ToString(), "Default", myPanel.minScale, purple, TextAlignment.RIGHT));

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();
            data5.Clear();

            if (beautifyLog && myPanel.subTypeId == "LargeLCDPanel") {
                DrawSpritesTabsOreIngots(ref frame, myPanel.viewport.Center, myPanel.animationCount, myPanel.minScale);
                myPanel.animationCount++;
                if (myPanel.animationCount > 5) {
                    myPanel.animationCount = 0;
                }
            }

            frame.Dispose();
        }

        void LogOverview(MyPanel myPanel) {
            Echo($"LogOverview");

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

            float orizontalMargin = 20f * myPanel.minScale;
            float verticalMargin = 30f * myPanel.minScale;

            string gun;
            switch (weaponType) {
                case 0: gun = "Jolt"; break;
                case 1: gun = "Rockets"; break;
                case 2: gun = "Gatlings"; break;
                case 3: gun = "Autocannon"; break;
                case 4: gun = "Assault"; break;
                case 5: gun = "Artillery"; break;
                case 6: gun = "Railgun"; break;
                case 7: gun = "Small Rail"; break;
                default: gun = "None"; break;
            }
            if (readyToFire) {
                data.Append($"{gun}\n"); data2.Append("\n");
            } else {
                data2.Append($"{gun}\n"); data.Append("\n");
            }

            string drop;
            switch (selectedDrop) {
                case 0: drop = "Decoy"; break;
                case 1: drop = "Bomb"; break;
                default: drop = "None"; break;
            }
            if (readyDecoy) {
                if (toggleDecoy) {
                    data.Append($"{drop} TOG\n"); data2.Append("\n");
                } else {
                    data.Append($"{drop}\n"); data2.Append("\n");
                }
            } else {
                if (toggleDecoy) {
                    data2.Append($"{drop} TOG\n"); data.Append("\n");
                } else {
                    data2.Append($"{drop}\n"); data.Append("\n");
                }
            }

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + orizontalMargin, myPanel.col1_3.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale, magenta));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + orizontalMargin, myPanel.col1_3.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale, purple));

            data.Clear();
            data2.Clear();

            data.Append($"\n\n");
            data2.Append("\n\n");

            if (autoFire) {
                data.Append("A-Fire\n"); data2.Append("\n");
            } else {
                data2.Append("A-Fire\n"); data.Append("\n");
            }
            if (autoSwitchGuns) {
                data.Append("A-Switch\n"); data2.Append("\n");
            } else {
                data2.Append("A-Switch\n"); data.Append("\n");
            }
            if (sequenceWeapons) {
                data.Append("Sequencer\n"); data2.Append("\n");
            } else {
                data2.Append("Sequencer\n"); data.Append("\n");
            }
            if (autoMissiles) {
                data.Append("A-Missiles\n"); data2.Append("\n");
            } else {
                data2.Append("A-Missiles\n"); data.Append("\n");
            }
            if (creative) {
                data.Append("Creative\n"); data2.Append("\n");
            } else {
                data2.Append("Creative\n"); data.Append("\n");
            }

            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + orizontalMargin, myPanel.col1_3.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale, azure));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_3.X + orizontalMargin, myPanel.col1_3.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale, purple));

            data.Clear();
            data2.Clear();

            string payLoad;
            switch (selectedPayLoad) {
                case 0: payLoad = "Missiles"; break;
                case 1: payLoad = "Drones"; break;
                default: payLoad = "None"; break;
            }
            if (missilesLoaded) {
                data.Append($"{payLoad}\n"); data2.Append("\n");
            } else {
                data2.Append($"{payLoad}\n"); data.Append("\n");
            }

            if (readyJolt) {
                if (toggleJolt) {
                    data.Append("Jolt TOG\n"); data2.Append("\n");
                } else {
                    data.Append("Jolt\n"); data2.Append("\n");
                }
            } else {
                if (toggleJolt) {
                    data2.Append("Jolt TOG\n"); data.Append("\n");
                } else {
                    data2.Append("Jolt\n"); data.Append("\n");
                }
            }

            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + orizontalMargin, myPanel.col2_3.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale, magenta));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + orizontalMargin, myPanel.col2_3.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale, purple));

            data.Clear();
            data2.Clear();

            data.Append($"\n\n");
            data2.Append("\n\n");

            if (autoCombat) {
                data.Append("A-Combat\n"); data2.Append("\n");
            } else {
                data2.Append("A-Combat\n"); data.Append("\n");
            }
            if (enemyEvasion) {
                data.Append("Evasion\n"); data2.Append("\n");
            } else {
                data2.Append("Evasion\n"); data.Append("\n");
            }
            if (obstaclesAvoidance) {
                data.Append("Obstacles\n"); data2.Append("\n");
            } else {
                data2.Append("Obstacles\n"); data.Append("\n");
            }
            if (closeRangeCombat) {
                data.Append("CloseRange\n"); data2.Append("\n");
            } else {
                data2.Append("CloseRange\n"); data.Append("\n");
            }

            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + orizontalMargin, myPanel.col2_3.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale, azure));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_3.X + orizontalMargin, myPanel.col2_3.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale, purple));

            data.Clear();
            data2.Clear();

            if (magneticDrive) {
                data.Append("Magnetic\n"); data2.Append("\n");
            } else {
                data2.Append("Magnetic\n"); data.Append("\n");
            }
            if (useGyrosToStabilize) {
                data.Append("Stabilizer\n"); data2.Append("\n");
            } else {
                data2.Append("Stabilizer\n"); data.Append("\n");
            }
            if (idleThrusters) {
                data.Append("Thrusters\n"); data2.Append("\n");
            } else {
                data2.Append("Thrusters\n"); data.Append("\n");
            }
            if (safetyDampeners) {
                data.Append("Dampeners\n"); data2.Append("\n");
            } else {
                data2.Append("Dampeners\n"); data.Append("\n");
            }
            if (keepAltitude) {
                data.Append("Altitude\n"); data2.Append("\n");
            } else {
                data2.Append("Altitude\n"); data.Append("\n");
            }
            if (moddedSensor) {
                data.Append("Sensors\n"); data2.Append("\n");
            } else {
                data2.Append("Sensors\n"); data.Append("\n");
            }
            if (sunAlign) {
                data.Append("SunAlign\n"); data2.Append("\n");
            } else {
                data2.Append("SunAlign\n"); data.Append("\n");
            }

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_3.X + myPanel.col3_3.Width + orizontalMargin, myPanel.col3_3.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale, azure));
            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_3.X + myPanel.col3_3.Width + orizontalMargin, myPanel.col3_3.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale, purple));

            data.Clear();
            data2.Clear();

            data.Append("\n\n\n\n\n\n\n");
            data2.Append("\n\n\n\n\n\n\n");
            data3.Append("\n\n\n\n\n\n\n");
            data4.Append("\n\n\n\n\n\n\n");

            bool alternate = true;
            foreach (var log in ammoLogDict) {
                if (alternate) {
                    data.Append($"{log.Key.Replace("Ammo", "").Replace("Clip", "").Replace("mm", "")}: \n");
                    alternate = false;
                } else {
                    data3.Append($"{log.Key.Replace("Ammo", "").Replace("Clip", "").Replace("mm", "")}: \n");
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

            orizontalMargin = 75f * myPanel.minScale;
            verticalMargin = 50f * myPanel.minScale;
            frame.Add(DrawSpriteText(new Vector2(myPanel.col1_4.X + myPanel.col1_4.Width + orizontalMargin, myPanel.col1_4.Y + verticalMargin),
                data.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));//TODO
            frame.Add(DrawSpriteText(new Vector2(myPanel.col2_4.X + orizontalMargin, myPanel.col2_4.Y + verticalMargin),
                data2.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));//TODO

            frame.Add(DrawSpriteText(new Vector2(myPanel.col3_4.X + myPanel.col3_4.Width + orizontalMargin, myPanel.col3_4.Y + verticalMargin),
                data3.ToString(), "Default", myPanel.minScale - 0.1f, azure, TextAlignment.RIGHT));//TODO
            frame.Add(DrawSpriteText(new Vector2(myPanel.col4_4.X + orizontalMargin, myPanel.col4_4.Y + verticalMargin),
                data4.ToString(), "Default", myPanel.minScale - 0.1f, magenta, TextAlignment.LEFT));//TODO

            data.Clear();
            data2.Clear();
            data3.Clear();
            data4.Clear();

            frame.Dispose();
        }

        void PlayCommercials(MyPanel myPanel) {
            Echo("PlayCommercials");

            MySpriteDrawFrame frame = myPanel.surface.DrawFrame();

            int randomId = random.Next(0, 15);
            string pictureId;
            commercialsDict.TryGetValue(randomId, out pictureId);

            frame.Add(DrawSpritePicture(new Vector2(myPanel.viewport.Center.X - myPanel.viewport.Width / 2, myPanel.viewport.Y + myPanel.viewport.Center.Y), pictureId, "Default"));

            frame.Dispose();
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

        MySprite DrawSpritePicture(Vector2 pos, string data, string font, Color? color = null, TextAlignment alignment = TextAlignment.LEFT) {
            return new MySprite() {
                Type = SpriteType.TEXTURE,
                Data = data,
                //RotationOrScale = scale,
                Position = pos,
                FontId = font,
                Color = color,
                Alignment = alignment
            };
        }

        public void DrawSpritesTabsOreIngots(ref MySpriteDrawFrame frame, Vector2 centerPos, int animationCount, float scale = 1f) {
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

            DrawStatusBar(ref frame, new Vector2(90f, -223f) * scale + centerPos, new Vector2(200f, 25f) * scale, (float)cargoPercentage / 100f, transparentBlue, azure, TextAlignment.LEFT);

            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(235f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 0 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle2
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(215f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 1 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle3
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(195f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 2 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle4
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(175f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 3 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle5
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(155f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 4 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle6
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(135f, 25f) * scale + centerPos, new Vector2(40f, 23f) * scale, animationCount == 5 ? azure : transparentBlue, null, TextAlignment.CENTER, 4.7124f)); // triangle1

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 144f) * scale + centerPos, new Vector2(500f, 184f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // dark blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 238f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 51f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 144f) * scale + centerPos, new Vector2(2f, 187f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue line
        }

        public void DrawSpritesTabsComponentsAmmo(ref MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
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

        public void DrawSpritesTabsPower(ref MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -120f) * scale + centerPos, new Vector2(500f, 240f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // magenta base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -120f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -120f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -240f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // top line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 0f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // bottom line

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 130f) * scale + centerPos, new Vector2(500f, 240f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 130f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 130f) * scale + centerPos, new Vector2(2f, 240f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 10f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // top line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 250f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // bottom line

            DrawStatusBar(ref frame, new Vector2(-15f, -18f) * scale + centerPos, new Vector2(200f, 25f) * scale, (float)tankCapacityPercent / 100f, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);

            if (hEngMaxOutput != 0f) {
                DrawStatusBar(ref frame, new Vector2(-15f, -76f) * scale + centerPos, new Vector2(200f, 25f) * scale, hEngCurrentOutput / hEngMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (reactorsMaxOutput != 0f) {
                DrawStatusBar(ref frame, new Vector2(-15f, -105f) * scale + centerPos, new Vector2(200f, 25f) * scale, reactorsCurrentOutput / reactorsMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (battsMaxStoredPower != 0f) {
                DrawStatusBar(ref frame, new Vector2(-15f, -134f) * scale + centerPos, new Vector2(200f, 25f) * scale, battsCurrentStoredPower / battsMaxStoredPower, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (battsMaxOutput != 0f) {
                DrawStatusBar(ref frame, new Vector2(-15f, -163f) * scale + centerPos, new Vector2(200f, 25f) * scale, battsCurrentOutput / battsMaxOutput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
            if (terminalMaxRequiredInput != 0f) {
                DrawStatusBar(ref frame, new Vector2(-15f, -192f) * scale + centerPos, new Vector2(200f, 25f) * scale, terminalCurrentInput / terminalMaxRequiredInput, transparentMagenta, transparentNeonAzure, TextAlignment.RIGHT);
            }
        }

        public void DrawSpritesTabsJumpRangeFinder(ref MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -151f) * scale + centerPos, new Vector2(500f, 118f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -222f) * scale + centerPos, new Vector2(275f, 25f) * scale, deepBlue, null, TextAlignment.CENTER, 0f)); // blue top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -222f) * scale + centerPos, new Vector2(25f, 25f) * scale, deepBlue, null, TextAlignment.CENTER, 4.7124f)); // blue top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -150f) * scale + centerPos, new Vector2(2f, 120f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -163f) * scale + centerPos, new Vector2(2f, 143f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -91f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -209f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -233f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -222f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 2.3736f)); // blue  top line c

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -2f) * scale + centerPos, new Vector2(500f, 120f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -74f) * scale + centerPos, new Vector2(275f, 25f) * scale, deepPurple, null, TextAlignment.CENTER, 0f)); // purple top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -74f) * scale + centerPos, new Vector2(25f, 25f) * scale, deepPurple, null, TextAlignment.CENTER, 4.7124f)); // purple top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -87f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -3f) * scale + centerPos, new Vector2(2f, 121f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 57f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -63f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -15f) * scale + centerPos, new Vector2(2f, 145f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -75f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 2.3736f)); // purple  top line c

            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 148f) * scale + centerPos, new Vector2(500f, 170f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 148f) * scale + centerPos, new Vector2(2f, 170f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 148f) * scale + centerPos, new Vector2(2f, 170f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 63f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 233f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
        }

        public void DrawSpritesTabsPainter(ref MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -172f) * scale + centerPos, new Vector2(500f, 88f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -228f) * scale + centerPos, new Vector2(275f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 0f)); // purple top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -228f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentMagenta, null, TextAlignment.CENTER, 4.7124f)); // purple top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, -173f) * scale + centerPos, new Vector2(2f, 89f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, -185f) * scale + centerPos, new Vector2(2f, 113f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -129f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -216f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -240f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 0f)); // purple top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -229f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentNeonMagenta, null, TextAlignment.CENTER, 2.3736f)); // purple  top line c
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 75f) * scale + centerPos, new Vector2(500f, 349f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue base
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(111f, -112f) * scale + centerPos, new Vector2(275f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 0f)); // blue top corner
            frame.Add(new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(-39f, -112f) * scale + centerPos, new Vector2(25f, 25f) * scale, transparentDarkBlue, null, TextAlignment.CENTER, 4.7124f)); // blue top triangle
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(112f, -124f) * scale + centerPos, new Vector2(277f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line b
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-250f, 75f) * scale + centerPos, new Vector2(2f, 352f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue left line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 250f) * scale + centerPos, new Vector2(500f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue bottom line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-150f, -100f) * scale + centerPos, new Vector2(200f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue top line a
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250f, 63f) * scale + centerPos, new Vector2(2f, 375f) * scale, transparentBlue, null, TextAlignment.CENTER, 0f)); // blue right line
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-39f, -112f) * scale + centerPos, new Vector2(36f, 2f) * scale, transparentBlue, null, TextAlignment.CENTER, 2.3736f)); // blue  top line c
        }

        void DrawStatusBar(ref MySpriteDrawFrame frame, Vector2 position, Vector2 size, float proportion, Color backgroundColor, Color barColor, TextAlignment barAlignment) {
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

        List<Vector2> DrawMovingGraph(ref MySpriteDrawFrame frame, List<Vector2> positions, Vector2 movementValue, float removeValue, Vector2 startPosition, float multiplier, float inputValue, float width, Color color) {
            for (int i = 0; i < positions.Count; i++) {
                positions[i] = positions[i] + movementValue;
            }

            List<Vector2> positionsCopy = new List<Vector2>();
            foreach (Vector2 pos in positions) {
                if (pos.X > removeValue) {
                    positionsCopy.Add(pos);
                }
            }
            positions = positionsCopy;

            float columnSize = inputValue * multiplier;
            Vector2 position = new Vector2(startPosition.X, startPosition.Y - columnSize);
            positions.Add(position);

            for (int i = 0; i < positions.Count - 1; i++) {
                DrawLine(ref frame, positions[i], positions[i + 1], width, color);
            }
            return positions;
        }

        void DrawRandomGraph(ref MySpriteDrawFrame frame, List<Vector2> positions, float multiplier, float width, Color color, float scale, Vector2 center) {
            for (int i = 0; i < positions.Count; i++) {
                float rand = random.Next(0, 101);
                float columnSize = rand * multiplier;
                Vector2 pos = new Vector2(positions[i].X * scale, (positions[i].Y - columnSize) * scale) + center;
                positions[i] = pos;
            }

            for (int i = 0; i < positions.Count - 1; i++) {
                DrawLine(ref frame, positions[i], positions[i + 1], width, color);
            }
        }

        void DrawLine(ref MySpriteDrawFrame frame, Vector2 point1, Vector2 point2, float width, Color color) {
            Vector2 position = 0.5f * (point1 + point2);
            Vector2 diff = point1 - point2;
            float length = diff.Length();
            if (length > 0)
                diff /= length;

            Vector2 size = new Vector2(length, width);
            float angle = (float)Math.Acos(Vector2.Dot(diff, Vector2.UnitX));
            angle *= Math.Sign(Vector2.Dot(diff, Vector2.UnitY));

            MySprite sprite = MySprite.CreateSprite("SquareSimple", position, size);
            sprite.RotationOrScale = angle;
            sprite.Color = color;
            frame.Add(sprite);
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
                    NAVIGATOR.Add(new MyPanel(cockpit.GetSurface(cockpitRangeFinderSurface), "SmallCockpitPanel"));// 1
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
                    PAINTER.Add(new MyPanel(cockpit.GetSurface(cockpitTargetSurface), "SmallCockpitPanel"));// 0
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
                    POWER.Add(new MyPanel(cockpit.GetSurface(cockpitPowerSurface), "SmallCockpitPanel"));// 2
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

            OVERVIEW.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD Overview"));
            foreach (IMyTextPanel panel in panels) {
                OVERVIEW.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            foreach (IMyCockpit cockpit in cockpits) {
                MyIniParseResult result;
                myIni.TryParse(cockpit.CustomData, "OverviewSettings", out result);
                if (!string.IsNullOrEmpty(myIni.Get("OverviewSettings", "cockpitOverviewSurface").ToString())) {
                    int cockpitOverviewSurface = myIni.Get("OverviewSettings", "cockpitOverviewSurface").ToInt32();
                    OVERVIEW.Add(new MyPanel(cockpit.GetSurface(cockpitOverviewSurface), "SmallCockpitPanel"));// 3
                }
            }
            panels.Clear();

            COMMERCIALS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, block => block.CustomName.Contains("[CRX] LCD SE Commercials"));
            foreach (IMyTextPanel panel in panels) {
                COMMERCIALS.Add(new MyPanel(panel as IMyTextSurface, panel.BlockDefinition.SubtypeId));
            }
            panels.Clear();

            LCDBEAUTIFY = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Beautify Logs") as IMyTextPanel;
            LCDBRIGHTNESS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Cycle Brightness") as IMyTextPanel;
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
            public readonly RectangleF col3_3;
            public readonly RectangleF viewport;
            //public readonly RectangleF topHalf;
            //public readonly RectangleF bottomHalf;
            public int animationCount = 0;

            public MyPanel(IMyTextSurface _surface, string _subTypeId) {
                subTypeId = _subTypeId;
                surface = _surface;
                Vector2 scale = _surface.SurfaceSize / 512f;
                minScale = Math.Min(scale.X, scale.Y);
                if (_subTypeId == "SmallCockpitPanel") {//cockpit panel
                    minScale += 0.2f;
                }
                col1_4 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 4f, new Vector2(surface.SurfaceSize.X / 4f, surface.SurfaceSize.Y));
                col2_4 = new RectangleF(col1_4.X + (surface.SurfaceSize.X / 4f), col1_4.Y, col1_4.Width, col1_4.Height);
                col3_4 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 4f, new Vector2(surface.SurfaceSize.X / 4f * 3f, surface.SurfaceSize.Y));
                col4_4 = new RectangleF(col3_4.X + (surface.SurfaceSize.X / 4f * 3f), col3_4.Y, col3_4.Width, col3_4.Height);

                col1_3 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 3f, new Vector2(surface.SurfaceSize.X / 3f, surface.SurfaceSize.Y));
                col2_3 = new RectangleF(col1_3.X + (surface.SurfaceSize.X / 3f), col1_3.Y, col1_3.Width, col1_3.Height);
                col3_3 = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 3f, new Vector2(surface.SurfaceSize.X / 3f * 2f, surface.SurfaceSize.Y));

                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

                //Vector2 posHalf = new Vector2(0, viewport.Size.Y / 2);
                //Vector2 sizeHalf = new Vector2(viewport.Size.X, viewport.Size.Y / 2);
                //topHalf = new RectangleF(viewport.Center - posHalf, sizeHalf);
                //bottomHalf = new RectangleF(viewport.Center + posHalf, sizeHalf);

                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.BackgroundColor = Color.Black;
                surface.FontColor = Color.Magenta;
            }
        }


    }
}
