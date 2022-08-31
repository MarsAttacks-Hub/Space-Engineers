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

        //PLAYER
        readonly int frameWidth = 178;
        readonly int frameHeight = 178;

        readonly int debugFrame = 0;
        readonly bool debug = false;

        readonly double neonShopTimeDelay = 3d;
        readonly double neonBeerTimeDelay = 2d;
        readonly double neonChinaTimeDelay = 3d;
        readonly double corpsTimeDelay = 5d;
        readonly double commercialTimeDelay = 3d;
        readonly double hubsTimeDelay = 5d;
        readonly double hangarTimeDelay = 3d;
        readonly double warnTimeDelay = 5d;
        readonly double dangerTimeDelay = 3d;
        readonly double colorMorphTimeDelay = 1d;

        bool player = true;
        bool runningLights = true;

        int beerFrame = 72;
        int warn1Frame = 18;
        int warn2Frame = 16;
        int warn3Frame = 14;
        int warn4Frame = 12;
        int hangar1Frame = 8;
        int colorIndex = 0;

        double neonShopTime = 0d;
        double neonBeerTime = 0d;
        double neonChinaTime = 0d;
        double corpsTime = 0d;
        double commercialTime = 0d;
        double colorMorphTime = 0d;
        double hubsTime = 0d;
        double hangarTime = 0d;
        double warn1Time = 0d;
        double warn2Time = 0d;
        double warn3Time = 0d;
        double warn4Time = 0d;
        double dangerTime = 0d;

        public Gif gif;
        public Random random = new Random();
        IEnumerator<bool> stateMachine;

        public List<IMyTextPanel> NEONSHOP = new List<IMyTextPanel>();
        public List<IMyTextPanel> NEONBEER = new List<IMyTextPanel>();
        public List<IMyTextPanel> NEONCHINA = new List<IMyTextPanel>();
        public List<IMyTextPanel> CORPS = new List<IMyTextPanel>();
        public List<IMyTextPanel> COMMERCIAL = new List<IMyTextPanel>();
        public List<IMyTextPanel> HUBS = new List<IMyTextPanel>();
        public List<IMyTextPanel> HANGARS = new List<IMyTextPanel>();
        public List<IMyTextPanel> WARN1 = new List<IMyTextPanel>();
        public List<IMyTextPanel> WARN2 = new List<IMyTextPanel>();
        public List<IMyTextPanel> WARN3 = new List<IMyTextPanel>();
        public List<IMyTextPanel> WARN4 = new List<IMyTextPanel>();
        public List<IMyTextPanel> DANGER = new List<IMyTextPanel>();
        public List<IMyTextPanel> STATIC = new List<IMyTextPanel>();

        public List<IMyLightingBlock> RUNNINGLIGHTS = new List<IMyLightingBlock>();

        IMyTextPanel LCDRUNNINGLIGHTS;

        public List<Color> colors = new List<Color>() {
            new Color(0, 250, 250), new Color(10, 240, 250), new Color(20, 230, 250), new Color(30, 220, 250), new Color(40, 210, 250), new Color(50, 200, 250),
            new Color(60, 190, 250), new Color(70, 180, 250), new Color(80, 170, 250), new Color(90, 160, 250), new Color(100, 150, 250),
            new Color(110, 140, 250), new Color(120, 130, 250), new Color(130, 120, 250), new Color(140, 110, 250), new Color(150, 100, 250),
            new Color(160, 90, 250), new Color(170, 80, 250), new Color(180, 70, 250), new Color(190, 60, 250), new Color(200, 50, 250),
            new Color(210, 40, 250), new Color(220, 30, 250), new Color(230, 20, 250), new Color(240, 10, 250), new Color(250, 0, 250)
        };

        public Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            GetBlocks();
            Setup();
        }

        void Setup() {
            GetBlocks();
            gif = new Gif(frameWidth, frameHeight, Storage);
            if (Storage[0] != (char)'|') {
                Storage = gif.Serialize();
            }
            foreach (IMyTextPanel panel in STATIC) {
                panel.WriteText(new String(gif.frames[7]), false);
            }
            if (debug) {
                IMyTextPanel DEBUG = GridTerminalSystem.GetBlockWithName("[CRX] Debug") as IMyTextPanel;
                if (DEBUG != null) {
                    DEBUG.ContentType = ContentType.TEXT_AND_IMAGE;
                    DEBUG.FontSize = 0.1f;
                    DEBUG.Font = "Monospace";
                    DEBUG.TextPadding = 0f;
                    DEBUG.WriteText(new String(gif.frames[debugFrame]), false);
                }
            }
            foreach (IMyLightingBlock light in RUNNINGLIGHTS) {
                light.Color = new Color(0, 250, 250);
            }
            if (LCDRUNNINGLIGHTS != null) { LCDRUNNINGLIGHTS.BackgroundColor = runningLights ? new Color(25, 0, 100) : Color.Black; }
            stateMachine = RunOverTime();
        }

        public void Main(string arg, UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");
                Echo($"frames:{gif.frames.Count}");

                if (!string.IsNullOrEmpty(arg)) {
                    ProcessArgument(arg);
                    if (!player) {
                        if (LCDRUNNINGLIGHTS != null) { LCDRUNNINGLIGHTS.BackgroundColor = Color.Black; }
                        Me.GetSurface(0).BackgroundColor = Color.Black;
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    } else {
                        if (LCDRUNNINGLIGHTS != null) { LCDRUNNINGLIGHTS.BackgroundColor = runningLights ? new Color(25, 0, 100) : Color.Black; }
                        Me.GetSurface(0).BackgroundColor = new Color(25, 0, 100);
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
                    debugLog.Append("\n" + e.Message + "\n").Append(e.Source + "\n").Append(e.TargetSite + "\n").Append(e.StackTrace + "\n");
                    DEBUG.WriteText(debugLog, false);
                }
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        void ProcessArgument(string argument) {
            switch (argument) {
                case "ToggleRunningLights":
                    runningLights = !runningLights;
                    if (LCDRUNNINGLIGHTS != null) { LCDRUNNINGLIGHTS.BackgroundColor = runningLights ? new Color(25, 0, 100) : Color.Black; }
                    if (!runningLights) {
                        foreach (IMyLightingBlock light in RUNNINGLIGHTS) {
                            light.Color = new Color(0, 250, 250);
                        }
                    }
                    break;
                case "TogglePlayer":
                    player = !player;
                    break;
                case "PlayerOn":
                    player = true;
                    break;
                case "PlayerOff":
                    player = false;
                    break;
            }
        }

        public IEnumerator<bool> RunOverTime() {
            double lastRun = Runtime.TimeSinceLastRun.TotalSeconds;

            if (runningLights) {
                if (RUNNINGLIGHTS.Count > 0) {
                    if (colorMorphTime > colorMorphTimeDelay) {
                        if (colorIndex >= colors.Count - 1) {
                            colorIndex = 0;
                            colors.Reverse();
                        }
                        foreach (IMyLightingBlock light in RUNNINGLIGHTS) {
                            light.Color = colors.ElementAt(colorIndex);
                        }
                        yield return true;

                        colorMorphTime = 0d;
                        colorIndex++;
                    } else {
                        colorMorphTime += lastRun;
                    }
                }
            }

            if (NEONSHOP.Count > 0) {
                if (neonShopTime > neonShopTimeDelay) {
                    neonShopTime = 0;
                    foreach (IMyTextPanel panel in NEONSHOP) {
                        PlayNeonShop(panel);
                    }
                    yield return true;
                } else {
                    neonShopTime += lastRun;
                }
            }

            if (NEONBEER.Count > 0) {
                if (neonBeerTime > neonBeerTimeDelay) {
                    neonBeerTime = 0;
                    foreach (IMyTextPanel panel in NEONBEER) {
                        PlayNeonBeer(panel);
                    }
                    yield return true;
                } else {
                    neonBeerTime += lastRun;
                }
            }

            if (NEONCHINA.Count > 0) {
                if (neonChinaTime > neonChinaTimeDelay) {
                    neonChinaTime = 0;
                    foreach (IMyTextPanel panel in NEONCHINA) {
                        PlayNeonChina(panel);
                    }
                    yield return true;
                } else {
                    neonChinaTime += lastRun;
                }
            }

            if (COMMERCIAL.Count > 0) {
                if (commercialTime > commercialTimeDelay) {
                    commercialTime = 0;
                    foreach (IMyTextPanel panel in COMMERCIAL) {
                        PlayCommercials(panel);
                    }
                    yield return true;
                } else {
                    commercialTime += lastRun;
                }
            }

            if (CORPS.Count > 0) {
                if (corpsTime > corpsTimeDelay) {
                    corpsTime = 0;
                    foreach (IMyTextPanel panel in CORPS) {
                        PlayCorps(panel);
                    }
                    yield return true;
                } else {
                    corpsTime += lastRun;
                }
            }

            if (HUBS.Count > 0) {
                if (hubsTime > hubsTimeDelay) {
                    hubsTime = 0;
                    foreach (IMyTextPanel panel in HUBS) {
                        PlayHubs(panel);
                    }
                    yield return true;
                } else {
                    hubsTime += lastRun;
                }
            }

            if (HANGARS.Count > 0) {
                if (hangarTime > hangarTimeDelay) {
                    hangarTime = 0;
                    foreach (IMyTextPanel panel in HANGARS) {
                        PlayHangar(panel);
                    }
                    yield return true;
                } else {
                    hangarTime += lastRun;
                }
            }

            if (WARN1.Count > 0) {
                if (warn1Time > warnTimeDelay) {
                    warn1Time = 0;
                    foreach (IMyTextPanel panel in WARN1) {
                        PlayWarn1(panel);
                    }
                    yield return true;
                } else {
                    warn1Time += lastRun;
                }
            }

            if (WARN2.Count > 0) {
                if (warn2Time > warnTimeDelay) {
                    warn2Time = 0;
                    foreach (IMyTextPanel panel in WARN2) {
                        PlayWarn2(panel);
                    }
                    yield return true;
                } else {
                    warn2Time += lastRun;
                }
            }

            if (WARN3.Count > 0) {
                if (warn3Time > warnTimeDelay) {
                    warn3Time = 0;
                    foreach (IMyTextPanel panel in WARN3) {
                        PlayWarn3(panel);
                    }
                    yield return true;
                } else {
                    warn3Time += lastRun;
                }
            }

            if (WARN4.Count > 0) {
                if (warn4Time > warnTimeDelay) {
                    warn4Time = 0;
                    foreach (IMyTextPanel panel in WARN4) {
                        PlayWarn4(panel);
                    }
                    yield return true;
                } else {
                    warn4Time += lastRun;
                }
            }

            if (DANGER.Count > 0) {
                if (dangerTime > dangerTimeDelay) {
                    dangerTime = 0;
                    foreach (IMyTextPanel panel in DANGER) {
                        PlayDanger(panel);
                    }
                    yield return true;
                } else {
                    dangerTime += lastRun;
                }
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

        void PlayNeonShop(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(68, 72);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void PlayNeonBeer(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[beerFrame]), false);
            beerFrame = beerFrame >= 73 ? 72 : beerFrame + 1;
        }

        void PlayNeonChina(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(74, gif.frames.Count);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void PlayCorps(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(35, 48);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void PlayCommercials(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(48, 67);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void PlayHubs(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(20, 35);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void PlayWarn1(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[warn1Frame]), false);
            warn1Frame = warn1Frame >= 19 ? 18 : warn1Frame + 1;
        }

        void PlayWarn2(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[warn2Frame]), false);
            warn2Frame = warn2Frame >= 17 ? 16 : warn2Frame + 1;
        }

        void PlayWarn3(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[warn3Frame]), false);
            warn3Frame = warn3Frame >= 15 ? 14 : warn3Frame + 1;
        }

        void PlayWarn4(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[warn4Frame]), false);
            warn4Frame = warn4Frame >= 13 ? 12 : warn4Frame + 1;
        }

        void PlayHangar(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[hangar1Frame]), false);
            hangar1Frame = hangar1Frame >= 9 ? 8 : hangar1Frame + 1;
        }

        void PlayDanger(IMyTextPanel myPanel) {
            myPanel.WriteText(new String(gif.frames[hangar1Frame]), false);
            hangar1Frame = hangar1Frame >= 11 ? 10 : hangar1Frame + 1;
        }

        void GetBlocks() {
            NEONSHOP.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(NEONSHOP, block => block.CustomName.Contains("[CRX] LCD Player Neon Shop"));
            foreach (IMyTextPanel panel in NEONSHOP) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            NEONBEER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(NEONBEER, block => block.CustomName.Contains("[CRX] LCD Player Neon Beer"));
            foreach (IMyTextPanel panel in NEONBEER) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            NEONCHINA.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(NEONCHINA, block => block.CustomName.Contains("[CRX] LCD Player Neon China"));
            foreach (IMyTextPanel panel in NEONCHINA) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            CORPS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(CORPS, block => block.CustomName.Contains("[CRX] LCD Player Corp"));
            foreach (IMyTextPanel panel in CORPS) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            COMMERCIAL.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(COMMERCIAL, block => block.CustomName.Contains("[CRX] LCD Player Commercial"));
            foreach (IMyTextPanel panel in COMMERCIAL) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            HUBS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(HUBS, block => block.CustomName.Contains("[CRX] LCD Player Hubs"));
            foreach (IMyTextPanel panel in HUBS) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            HANGARS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(HANGARS, block => block.CustomName.Contains("[CRX] LCD Player Hangars"));
            foreach (IMyTextPanel panel in HANGARS) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            WARN1.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(WARN1, block => block.CustomName.Contains("[CRX] LCD Player Warn A"));
            foreach (IMyTextPanel panel in WARN1) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            WARN2.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(WARN2, block => block.CustomName.Contains("[CRX] LCD Player Warn B"));
            foreach (IMyTextPanel panel in WARN2) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            WARN3.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(WARN3, block => block.CustomName.Contains("[CRX] LCD Player Warn C"));
            foreach (IMyTextPanel panel in WARN3) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            WARN4.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(WARN4, block => block.CustomName.Contains("[CRX] LCD Player Warn D"));
            foreach (IMyTextPanel panel in WARN4) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            DANGER.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(DANGER, block => block.CustomName.Contains("[CRX] LCD Player Danger"));
            foreach (IMyTextPanel panel in DANGER) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            STATIC.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(STATIC, block => block.CustomName.Contains("[CRX] LCD Player Static"));
            foreach (IMyTextPanel panel in STATIC) {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";
                panel.TextPadding = 0f;
            }

            RUNNINGLIGHTS.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(RUNNINGLIGHTS, block => block.CustomName.Contains("[CRX] Running Lights"));

            LCDRUNNINGLIGHTS = GridTerminalSystem.GetBlockWithName("[CRX] LCD Toggle Running Lights") as IMyTextPanel;
        }

        public class Decoder {
            readonly int maxStackSize = 8192;
            //readonly int width = 0;
            //readonly int height = 0;

            readonly int dataSize = 0;

            readonly int nullCode = -1;
            readonly int pixelCount = 0;
            public byte[] pixels;
            int codeSize;
            readonly int clearFlag;
            readonly int endFlag;
            int available;

            int code;
            int old_code;
            int code_mask;
            int bits;

            readonly int[] prefix;
            readonly int[] suffix;
            readonly int[] pixelStack;

            int top;
            //int count;
            int bi;
            int i;

            int data;
            int first;
            int inCode;

            readonly byte[] buffer;

            public Decoder(int width, int height, byte[] buffer, int minCodeSize) {
                this.buffer = buffer;
                //this.width = width;
                //this.height = height;
                this.dataSize = minCodeSize;

                this.nullCode = -1;
                this.pixelCount = width * height;
                this.pixels = new byte[pixelCount];
                this.codeSize = dataSize + 1;
                this.clearFlag = 1 << dataSize;
                this.endFlag = clearFlag + 1;
                this.available = endFlag + 1;

                this.code = nullCode;
                this.old_code = nullCode;
                this.code_mask = (1 << codeSize) - 1;
                this.bits = 0;

                this.prefix = new int[maxStackSize];
                this.suffix = new int[maxStackSize];
                this.pixelStack = new int[maxStackSize + 1];

                this.top = 0;
                //this.count = buffer.Length;
                this.bi = 0;
                this.i = 0;

                this.data = 0;
                this.first = 0;
                this.inCode = nullCode;

                for (code = 0; code < clearFlag; code++) {
                    prefix[code] = 0;
                    suffix[code] = (byte)code;
                }
            }

            public bool Decode() {
                if (i < pixelCount) {
                    if (top == 0) {
                        if (bits < codeSize) {
                            /*if (count == 0) 
                            { 
                                //    buffer = ReadData(); 
                                //    count = buffer.Length;                           
                                if (count == 0) 
                                { 
                                    //throw new Exception("got here"); 
                                    return false; 
                                } 
                                bi = 0; 
                            }*/
                            data += buffer[bi] << bits;
                            bits += 8;
                            bi++;
                            //count--;
                            return true;
                        }
                        code = data & code_mask;
                        data >>= codeSize;
                        bits -= codeSize;

                        if (code > available || code == endFlag) {
                            return false;
                        }
                        if (code == clearFlag) {
                            codeSize = dataSize + 1;
                            code_mask = (1 << codeSize) - 1;
                            available = clearFlag + 2;
                            old_code = nullCode;
                            return true;
                        }
                        if (old_code == nullCode) {
                            pixelStack[top++] = suffix[code];
                            old_code = code;
                            first = code;
                            return true;
                        }
                        inCode = code;
                        if (code == available) {
                            pixelStack[top++] = (byte)first;
                            code = old_code;
                        }
                        while (code > clearFlag) {
                            pixelStack[top++] = suffix[code];
                            code = prefix[code];
                        }
                        first = suffix[code];
                        if (available > maxStackSize) {
                            return false;
                        }
                        pixelStack[top++] = suffix[code];
                        prefix[available] = old_code;
                        suffix[available] = first;
                        available++;
                        if (available == code_mask + 1 && available < maxStackSize) {
                            codeSize++;
                            code_mask = (1 << codeSize) - 1;
                        }
                        old_code = inCode;
                    }
                    top--;
                    pixels[i++] = (byte)pixelStack[top];
                    return true;
                } else {
                    return false;
                }
            }
        }

        public class Gif {
            Decoder decoder;

            // lSD    
            public int width;
            public int height;

            int LCDwidth = 175;
            int LCDheight = 175;

            readonly int globalColorTableSize;
            readonly byte[][] globalColorTable;
            int localColorTableSize;
            byte[][] localColorTable;
            readonly byte[] data;
            long counter = 0;
            readonly byte backgroundColor;
            //    bool gce = false; 
            int lzwMinimumCodeSize;
            byte[] lzwData;
            int lzwDataIndex = 0;
            public Func<bool> step;
            //    long decodeBit = 0; 
            byte[] output;
            int top, left, w, h;
            //bool interlaceFlag;
            int x, y;
            int transparent = 0;
            bool is_transparent = false;
            bool restore_background = false;
            bool do_not_dispose = false;
            bool last_do_not_dispose = false;

            char[] frame, last;
            public List<char[]> frames = new List<char[]>();
            public List<int> delays = new List<int>();

            bool CreateFrame() {
                byte[] color = localColorTable[backgroundColor];

                float scale = 1;
                if (width > height) {
                    scale = (float)((float)width / (float)LCDwidth);
                } else {
                    scale = (float)((float)height / (float)LCDheight);
                }

                int sx = (int)(x * scale);
                int sy = (int)(y * scale);


                bool draw = false;
                bool transparentPixel = false;
                if (sx >= left && sx < left + w && sy >= top && sy < top + h) {
                    int spot = ((sy - top) * (w)) + (sx - left);
                    if (spot < output.Length) {
                        draw = true;
                        byte index = output[spot];
                        //    if(index < localColorTable.Length) 
                        color = localColorTable[index];
                        if (!this.is_transparent && index == transparent) {
                            transparentPixel = true;
                            draw = false;
                        }
                    }
                }

                if (this.restore_background && this.last_do_not_dispose && sx < width && sy < height && !draw && !transparentPixel) {
                    draw = true;
                }

                if (draw || frame[x + ((LCDwidth + 1) * y)] < '\uE100')
                    frame[x + ((LCDwidth + 1) * y)] = (char)('\uE100' + (color[2] * 8 / 256) + ((color[1] * 8 / 256) * 8) + ((color[0] * 8 / 256) * 64));

                x += 1;

                if (x > LCDwidth - 1) {
                    frame[x + ((LCDwidth + 1) * y)] = "\n"[0];
                    x = 0;
                    y++;
                    if (y > (LCDheight - 1)) {
                        frames.Add(frame);
                        step = MainLoop;
                        y = 0;
                        return true;
                    }
                }

                return true;
            }

            bool Decode() {
                if (this.decoder.Decode()) {
                    return true;
                } else {
                    this.output = this.decoder.pixels;
                    x = 0;
                    y = 0;


                    if (last == null) last = new char[(LCDwidth + 1) * LCDheight];
                    if (frame != null) last = frame;
                    frame = new char[(LCDwidth + 1) * LCDheight];
                    Array.Copy(last, frame, frame.Length);

                    step = CreateFrame;
                    return true;
                }
            }

            bool DecodeStart() {
                this.decoder = new Decoder(w, h, lzwData, lzwMinimumCodeSize);
                step = Decode;
                return true;
            }

            bool GetLzwData() {
                int len = data[counter++];
                for (int i = 0; i < len; i++) {
                    lzwData[lzwDataIndex++] = data[counter++];
                }

                if (data[counter] == 00) {
                    counter++;
                    step = DecodeStart;
                }
                return true;
            }

            bool ExtensionLoop() {
                counter += data[counter++];
                if (data[counter++] == 0x00)
                    step = MainLoop;
                return true;
            }

            bool MainLoop() {
                if (counter > data.Length)
                    return false;

                //gce = false; // had graphics control extension    
                switch (data[counter++]) {
                    case 0x21: // extension    
                        switch (data[counter++]) {
                            case 0xF9: // graphic control extension    
                                       //gce = true; 
                                counter++; // Block size 0x04 
                                int flags = data[counter++]; // Flags 
                                                             // 0 -   No disposal specified. The decoder is 
                                                             //       not required to take any action. 
                                                             // 1 -   Do not dispose. The graphic is to be left 
                                                             //       in place. 
                                this.last_do_not_dispose = this.do_not_dispose;
                                this.do_not_dispose = (flags & 0x2) > 0;

                                // 2 -   Restore to background color. The area used by the 
                                //       graphic must be restored to the background color. 
                                this.restore_background = (flags & 0x4) > 0;

                                // 3 -   Restore to previous. The decoder is required to 
                                //       restore the area overwritten by the graphic with 
                                //       what was there prior to rendering the graphic. 
                                this.is_transparent = (flags & 0x8) > 0;

                                // 4-7 -    To be defined. 
                                this.delays.Add(data[counter++] | (data[counter++] << 8)); // Delay Time 
                                transparent = data[counter++]; // Transparent Color Index 
                                counter++; // Block Terminator (0x00) 
                                break;
                            default:
                                step = ExtensionLoop;
                                return true;
                        }
                        break;
                    case 0x2c: // image descriptor    
                        left = data[counter++] | (data[counter++] << 8);
                        top = data[counter++] | (data[counter++] << 8);

                        w = data[counter++] | (data[counter++] << 8);
                        h = data[counter++] | (data[counter++] << 8);
                        bool localColorTableFlag = (data[counter] & 0x80) > 0;
                        //interlaceFlag = (data[counter] & 0x40) > 0;
                        bool sortFlag = (data[counter] & 0x20) > 0;
                        localColorTableSize = (int)Math.Pow(2, (((data[counter] & 0x07)) + 1));
                        counter++; // skip packed field used above    

                        if (localColorTableFlag) {
                            localColorTable = new byte[localColorTableSize][];
                            for (int i = 0; i < localColorTableSize; i++) {
                                localColorTable[i] = new byte[3];
                            }
                            for (int i = 0; i < localColorTableSize; i++) {
                                localColorTable[i][0] = data[counter++];
                                localColorTable[i][1] = data[counter++];
                                localColorTable[i][2] = data[counter++];
                            }
                        } else {
                            localColorTableSize = globalColorTableSize;
                            localColorTable = globalColorTable;
                        }

                        lzwMinimumCodeSize = data[counter++];
                        lzwData = new byte[w * h];
                        lzwDataIndex = 0;
                        step = GetLzwData;
                        break;
                    case 0x3b: // trailer    
                               //        Console.WriteLine ("trailer found!");    
                        return false;
                }
                return true;
            }

            public string Serialize() {
                string o = "";

                o += (char)'|';

                o += LCDwidth;

                o += (char)'|';

                o += LCDheight;

                o += (char)'|';

                o += this.delays.Count;

                o += (char)'|';

                o += this.frames.Count;

                o += (char)'|';

                foreach (var delay in this.delays) {
                    o += delay;
                    o += (char)'|';
                }

                foreach (var frm in this.frames) {
                    o += new string(frm);
                    o += (char)'|';
                }

                return o;
            }

            void Unserialize(string s) {
                var parts = s.Split((char)'|');

                this.LCDwidth = Int32.Parse(parts[1]);
                this.LCDheight = Int32.Parse(parts[2]);
                var delays = Int32.Parse(parts[3]);
                var frames = Int32.Parse(parts[4]);

                for (var n = 0; n < delays; n++)
                    this.delays.Add(Int32.Parse(parts[5 + n]));

                for (var o = 0; o < frames; o++)
                    this.frames.Add(parts[5 + delays + o].ToCharArray());

                return;
            }

            public Gif(int fwidth, int fheight, string base64) {
                if (base64[0] == '|') {
                    this.Unserialize(base64);
                    this.step = delegate () { return false; };
                    return;
                }

                this.LCDwidth = fwidth;
                this.LCDheight = fheight;

                data = Convert.FromBase64String(base64);

                string signature = "" + ((char)data[counter++]) + ((char)data[counter++]) + ((char)data[counter++]);
                string version = "" + (char)data[counter++] + (char)data[counter++] + (char)data[counter++];

                if (signature != "GIF" || (version != "87a" && version != "89a")) {
                    throw new Exception("Invalid gif file!");
                }

                width = data[counter++] | (data[counter++] << 8);
                height = data[counter++] | (data[counter++] << 8);

                globalColorTableSize = (int)Math.Pow(2, (((data[counter] & 0x07)) + 1));
                bool globalColorTableSortFlag = (data[counter] & 0x08) > 0;
                int colorResolution = ((data[counter] & 0x70) >> 4) + 1;
                bool globalColorTableFlag = (data[counter] & 0x80) > 0;
                counter++;

                backgroundColor = data[counter++];
                byte aspectRatio = data[counter++];

                globalColorTable = new byte[globalColorTableSize][];

                for (int i = 0; i < globalColorTableSize; i++) {
                    globalColorTable[i] = new byte[3];
                }

                if (!globalColorTableFlag) {
                    globalColorTable[0][0] = 0x00;
                    globalColorTable[0][1] = 0x00;
                    globalColorTable[0][2] = 0x00;

                    globalColorTable[1][0] = 0xFF;
                    globalColorTable[1][1] = 0xFF;
                    globalColorTable[1][2] = 0xFF;
                } else {
                    for (int i = 0; i < globalColorTableSize; i++) {
                        globalColorTable[i][0] = data[counter++];
                        globalColorTable[i][1] = data[counter++];
                        globalColorTable[i][2] = data[counter++];
                    }
                }
                step = MainLoop;
            }
        }



    }
}
