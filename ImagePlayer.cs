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

        readonly int frameWidth = 178;
        readonly int frameHeight = 178;

        public Gif gif;
        public Random random = new Random();
        IEnumerator<bool> stateMachine;

        public List<IMyTextPanel> NEON = new List<IMyTextPanel>();
        //public List<IMyTextPanel> COMMERCIAL = new List<IMyTextPanel>();
        //public List<IMyTextPanel> WARNS = new List<IMyTextPanel>();
        //public List<IMyTextPanel> CORPS = new List<IMyTextPanel>();
        //public List<IMyTextPanel> HUBS = new List<IMyTextPanel>();
        //public List<IMyTextPanel> STATIC = new List<IMyTextPanel>();

        public Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100; //Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GetBlocks();
            Setup();
        }

        void Setup() {
            GetBlocks();
            gif = new Gif(frameWidth, frameHeight, Storage);
            if (Storage[0] != (char)'|') {
                Storage = gif.Serialize();
            }
            stateMachine = RunOverTime();
        }

        public void Main(UpdateType updateType) {
            try {
                Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");

                if ((updateType & UpdateType.Update100) == UpdateType.Update100) {
                    RunStateMachine();
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
            foreach (IMyTextPanel panel in NEON) {
                PlayNeon(panel);
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

        void PlayNeon(IMyTextPanel myPanel) {
            int rndmFrame = random.Next(68, 71);
            myPanel.WriteText(new String(gif.frames[rndmFrame]), false);
        }

        void GetBlocks() {
            NEON.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(NEON, block => block.CustomName.Contains("[CRX] LCD Image Player Neon"));
            foreach (IMyTextPanel panel in NEON) {
                panel.FontSize = 0.1f;
                panel.Font = "Monospace";//myPanel.SetValue<long>("Font", 1147350002);
                panel.TextPadding = 0f;
            }

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