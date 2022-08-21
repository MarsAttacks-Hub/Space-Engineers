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

        int frameWidth = 178;
        int frameHeight = 178;

        string[] lcdCommercialsNames = new string[] { "[MGNT] LCD Living Room 1", "[MGNT] LCD Living Room 2", "[MGNT] LCD Store Room 1" };
        string[] lcdNeonNames = new string[] { "[MGNT] LCD Store Room 4", "[MGNT] LCD Store Room 2", "[MGNT] LCD Store Room 3", "[MGNT] LCD Store Room 5" };
        string[] lcdWarnsNames = new string[] { "[MGNT] LCD Dock BL 1", "[MGNT] LCD Dock BR 1", "[MGNT] LCD Dock FL 1", "[MGNT] LCD Dock FR 1", "[MGNT] LCD Front Hangar 1", "[MGNT] LCD Back Hangar 1" };
        string[] lcdCorpsNames = new string[] { "[MGNT] LCD Dock BL 2", "[MGNT] LCD Dock BR 2", "[MGNT] LCD Dock FL 2", "[MGNT] LCD Dock FR 2" };
        string[] lcdHubsNames = new string[] { "[MGNT] LCD Med Room 1", "[MGNT] LCD Med Room 2", "[MGNT] LCD Med Room 3", "[MGNT] LCD Med Room 4", "[MGNT] LCD Front Bridge 5", "[MGNT] LCD Front Bridge 6" };
        string[] lcdStaticNames = new string[] { "[MGNT] LCD Magneto 1", "[MGNT] LCD Magneto 2", "[MGNT] LCD Magneto 3", "[MGNT] LCD Magneto 4", "[MGNT] LCD Engine Room 1", "[MGNT] LCD Front Hangar 2", "[MGNT] LCD Back Hangar 2" };

        bool loop = true;

        List<int> frames = new List<int> { }; // customize which frames play.  Ex: List<int> frames = new List<int>{1,3,5,7};
        int frameSkip = 0; // skips every frameSkip frames to increase performance.
        int throttle = 1000; // Set this lower to prevent complexity errors.  Set it higher to decrease loading times. 

        int wait = 20;
        int ticker = 0;
        public int frame = 0;
        public bool running = true;
        public long lastFrame = 0;
        public int delay = 0;
        public bool first = true;
        public bool isStatic = true;

        int bl1Frame = 18;
        int br1Frame = 16;
        int fl1Frame = 14;
        int fr1Frame = 12;
        int backHangarFrame = 8;
        int frontHangarFrame = 8;
        int beerFrame = 72;
        int engineRoomFrame = 10;

        public Gif gif;
        Random random = new Random();

        public List<IMyTextPanel> screensCommercials = new List<IMyTextPanel>();
        public List<IMyTextPanel> screensNeon = new List<IMyTextPanel>();
        public List<IMyTextPanel> screensWarns = new List<IMyTextPanel>();
        public List<IMyTextPanel> screensCorps = new List<IMyTextPanel>();
        public List<IMyTextPanel> screensHubs = new List<IMyTextPanel>();
        public List<IMyTextPanel> screensStatic = new List<IMyTextPanel>();

        public Program() {
            //Me.CustomData = Storage;
            //--------------------------------------
            Runtime.UpdateFrequency = UpdateFrequency.Update10;//Update100 doesn't work
            //--------------------------------------
            foreach (string lcdName in lcdCommercialsNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensCommercials.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            foreach (string lcdName in lcdWarnsNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensWarns.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            foreach (string lcdName in lcdCorpsNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensCorps.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            foreach (string lcdName in lcdHubsNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensHubs.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            foreach (string lcdName in lcdNeonNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensNeon.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            foreach (string lcdName in lcdStaticNames) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
                if (blocks.Count > 0) {
                    foreach (IMyTextPanel screen in blocks) {
                        if (screen.CustomName != lcdName) { continue; }
                        float fontSize = 0.1f;
                        screen.SetValue("FontSize", fontSize);
                        screen.SetValue<long>("Font", 1147350002);
                        screen.SetValue<Single>("TextPaddingSlider", 0);
                        screensStatic.Add(screen);
                    }
                } else {
                    throw new Exception("LCD named \"" + lcdName + "\" not found.");
                }
            }
            //--------------------------------------
            gif = new Gif(frameWidth, frameHeight, Storage);
        }

        void Main(string args) {
            Echo($"LastRunTimeMs:{Runtime.LastRunTimeMs}");
            if (args.IndexOf("frames") == 0) {
                loop = false;
                frame = 0;
                frames.Clear();
                string[] frameNumbers = args.Split(' ');
                foreach (string number in frameNumbers) {
                    if (number != "frames") {
                        if (number == "loop") {
                            frames.Add(-1);
                        } else {
                            frames.Add(Int32.Parse(number));
                        }
                    }
                }
            }

            if (running) {
                int count = 0;
                while (count++ < throttle && !first) {
                    try {
                        if (!gif.step()) {
                            if (Storage[0] != (char)'|') {
                                Storage = gif.serialize();
                                Echo(gif.frames.Count + " Frames loaded from GIF.");
                            } else {
                                Echo(gif.frames.Count + " Frames loaded from cache.");
                            }
                            running = false;
                            break;
                        }
                    } catch (Exception e) {
                        Echo($"Exception: {e}\n---");
                        throw;
                    }
                }
                first = false;
                return;
            }
            int currentFrame = frame;
            if (frames.Count > 0) {
                if (frame >= frames.Count) {
                    if (loop) frame = 0;
                    else frame = frames.Count - 1;
                }
                if (frames[frame] == -1) // loop
                {
                    frames.RemoveRange(0, frame + 1);
                    frame = 0;
                    loop = true;
                }
                currentFrame = frames[frame];
            }

            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - lastFrame >= delay * 10) {
                bool draw = false;
                if (lastFrame > 0 && ((now - lastFrame) - (delay * 10) < 100)) {
                    lastFrame = now - ((now - lastFrame) - (delay * 10));
                    draw = true;
                } else {
                    if (lastFrame == 0) {
                        draw = true;
                    }
                    lastFrame = now;
                }
                if (draw && (frameSkip == 0 || (frame % frameSkip != 0))) {
                    //-------------------------------
                    Echo(ticker + "\n");
                    Echo(gif.frames.Count + "\n");

                    if (ticker == 0) {
                        foreach (IMyTextPanel giscreen in screensWarns) {
                            if (giscreen.CustomName.Equals("[MGNT] LCD Dock BL 1")) {
                                giscreen.WriteText(new String(gif.frames[bl1Frame]), false);
                                if (bl1Frame >= 19) {
                                    bl1Frame = 18;
                                } else {
                                    bl1Frame++;
                                }
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Dock BR 1")) {
                                giscreen.WriteText(new String(gif.frames[br1Frame]), false);
                                if (br1Frame >= 17) {
                                    br1Frame = 16;
                                } else {
                                    br1Frame++;
                                }
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Dock FL 1")) {
                                giscreen.WriteText(new String(gif.frames[fl1Frame]), false);
                                if (fl1Frame >= 15) {
                                    fl1Frame = 14;
                                } else {
                                    fl1Frame++;
                                }
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Dock FR 1")) {
                                giscreen.WriteText(new String(gif.frames[fr1Frame]), false);
                                if (fr1Frame >= 13) {
                                    fr1Frame = 12;
                                } else {
                                    fr1Frame++;
                                }
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Front Hangar 1")) {
                                giscreen.WriteText(new String(gif.frames[frontHangarFrame]), false);
                                if (frontHangarFrame >= 9) {
                                    frontHangarFrame = 8;
                                } else {
                                    frontHangarFrame++;
                                }
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Back Hangar 1")) {
                                giscreen.WriteText(new String(gif.frames[backHangarFrame]), false);
                                if (backHangarFrame >= 9) {
                                    backHangarFrame = 8;
                                } else {
                                    backHangarFrame++;
                                }
                            }
                            /*else if (giscreen.CustomName.Equals("[MGNT] LCD Engine Room 2"))
                            {
                                giscreen.WriteText(new String(gif.frames[4]), false);
                                if (engineRoomFrame >= 11)
                                {
                                    engineRoomFrame = 10;
                                }
                                else
                                {
                                    engineRoomFrame++;
                                }
                            }*/
                        }
                        foreach (IMyTextPanel giscreen in screensCorps) {
                            int rndmFrame = random.Next(35, 47);
                            giscreen.WriteText(new String(gif.frames[rndmFrame]), false);
                        }
                        foreach (IMyTextPanel giscreen in screensCommercials) {
                            int rndmFrame = random.Next(48, 66);
                            giscreen.WriteText(new String(gif.frames[rndmFrame]), false);
                        }
                        foreach (IMyTextPanel giscreen in screensNeon) {
                            if (giscreen.CustomName.Equals("[MGNT] LCD Store Room 4")) {
                                int rndmFrame = random.Next(68, 71);
                                giscreen.WriteText(new String(gif.frames[rndmFrame]), false);
                            } else if (giscreen.CustomName.Equals("[MGNT] LCD Store Room 5")) {
                                giscreen.WriteText(new String(gif.frames[beerFrame]), false);
                                if (beerFrame >= 73) {
                                    beerFrame = 72;
                                } else {
                                    beerFrame++;
                                }
                            } else {
                                int rndmFrame = random.Next(74, gif.frames.Count);
                                giscreen.WriteText(new String(gif.frames[rndmFrame]), false);
                            }

                        }
                        foreach (IMyTextPanel giscreen in screensHubs) {
                            int rndmFrame = random.Next(20, 34);
                            giscreen.WriteText(new String(gif.frames[rndmFrame]), false);
                        }
                        if (isStatic) {
                            foreach (IMyTextPanel giscreen in screensStatic) {

                                if (giscreen.CustomName.Equals("[MGNT] LCD Front Hangar 2")) {
                                    giscreen.WriteText(new String(gif.frames[7]), false);
                                } else if (giscreen.CustomName.Equals("[MGNT] LCD Back Hangar 2")) {
                                    giscreen.WriteText(new String(gif.frames[7]), false);
                                }

                                /* if (giscreen.CustomName.Equals("[MGNT] LCD Magneto 1"))
                                 {
                                     giscreen.WriteText(new String(gif.frames[0]), false);
                                 }
                                 else if (giscreen.CustomName.Equals("[MGNT] LCD Magneto 2"))
                                 {
                                     giscreen.WriteText(new String(gif.frames[1]), false);
                                 }
                                 else if (giscreen.CustomName.Equals("[MGNT] LCD Magneto 3"))
                                 {
                                     giscreen.WriteText(new String(gif.frames[2]), false);
                                 }
                                 else if (giscreen.CustomName.Equals("[MGNT] LCD Magneto 4"))
                                 {
                                     giscreen.WriteText(new String(gif.frames[3]), false);
                                 }
                                 else if (giscreen.CustomName.Equals("[MGNT] LCD Engine Room 1"))
                                 {
                                     giscreen.WriteText(new String(gif.frames[4]), false);
                                 }*/
                            }
                            isStatic = false;
                        }
                    }
                    ticker++;
                    if (ticker >= wait) {
                        ticker = 0;
                    }
                    //-------------------------------
                }
                delay = gif.delays[currentFrame % gif.frames.Count];
                if (delay < 3) delay = 10; // this is how most browsers handle 0
                frame++;
            }
        }

        public class Decoder {
            int MaxStackSize = 8192;
            int width = 0;
            int height = 0;

            int dataSize = 0;

            int NullCode = -1;
            int pixelCount = 0;
            public byte[] pixels;
            int codeSize;
            int clearFlag;
            int endFlag;
            int available;

            int code;
            int old_code;
            int code_mask;
            int bits;

            int[] prefix;
            int[] suffix;
            int[] pixelStack;

            int top;
            int count;
            int bi;
            int i;

            int data;
            int first;
            int inCode;

            byte[] buffer;

            public Decoder(int width, int height, byte[] buffer, int minCodeSize) {
                this.buffer = buffer;
                this.width = width;
                this.height = height;
                this.dataSize = minCodeSize;

                this.NullCode = -1;
                this.pixelCount = width * height;
                this.pixels = new byte[pixelCount];
                this.codeSize = dataSize + 1;
                this.clearFlag = 1 << dataSize;
                this.endFlag = clearFlag + 1;
                this.available = endFlag + 1;

                this.code = NullCode;
                this.old_code = NullCode;
                this.code_mask = (1 << codeSize) - 1;
                this.bits = 0;

                this.prefix = new int[MaxStackSize];
                this.suffix = new int[MaxStackSize];
                this.pixelStack = new int[MaxStackSize + 1];

                this.top = 0;
                this.count = buffer.Length;
                this.bi = 0;
                this.i = 0;

                this.data = 0;
                this.first = 0;
                this.inCode = NullCode;

                for (code = 0; code < clearFlag; code++) {
                    prefix[code] = 0;
                    suffix[code] = (byte)code;
                }
            }

            public bool decode() {
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
                            count--;
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
                            old_code = NullCode;
                            return true;
                        }
                        if (old_code == NullCode) {
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
                        if (available > MaxStackSize) {
                            return false;
                        }
                        pixelStack[top++] = suffix[code];
                        prefix[available] = old_code;
                        suffix[available] = first;
                        available++;
                        if (available == code_mask + 1 && available < MaxStackSize) {
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

            public int LCDwidth = 175;
            public int LCDheight = 175;

            int globalColorTableSize;
            byte[][] globalColorTable;
            int localColorTableSize;
            byte[][] localColorTable;
            byte[] data;
            long counter = 0;
            byte backgroundColor;
            //    bool gce = false; 
            int lzwMinimumCodeSize;
            byte[] lzwData;
            int lzwDataIndex = 0;
            public Func<bool> step;
            //    long decodeBit = 0; 
            byte[] output;
            int top, left, w, h;
            bool interlaceFlag;
            int x, y;
            int transparent = 0;
            bool is_transparent = false;
            bool restore_background = false;
            bool do_not_dispose = false;
            bool last_do_not_dispose = false;

            char[] frame, last;
            public List<char[]> frames = new List<char[]>();
            public List<int> delays = new List<int>();

            bool createFrame() {
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
                        step = mainLoop;
                        y = 0;
                        return true;
                    }
                }

                return true;
            }

            bool decode() {
                if (this.decoder.decode()) {
                    return true;
                } else {
                    this.output = this.decoder.pixels;
                    x = 0;
                    y = 0;


                    if (last == null) last = new char[(LCDwidth + 1) * LCDheight];
                    if (frame != null) last = frame;
                    frame = new char[(LCDwidth + 1) * LCDheight];
                    Array.Copy(last, frame, frame.Length);

                    step = createFrame;
                    return true;
                }
            }

            bool decodeStart() {
                this.decoder = new Decoder(w, h, lzwData, lzwMinimumCodeSize);
                step = decode;
                return true;
            }

            bool getLzwData() {
                int len = data[counter++];
                for (int i = 0; i < len; i++) {
                    lzwData[lzwDataIndex++] = data[counter++];
                }

                if (data[counter] == 00) {
                    counter++;
                    step = decodeStart;
                }
                return true;
            }

            bool extensionLoop() {
                counter += data[counter++];
                if (data[counter++] == 0x00)
                    step = mainLoop;
                return true;
            }

            bool mainLoop() {
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
                                step = extensionLoop;
                                return true;
                        }
                        break;
                    case 0x2c: // image descriptor    
                        left = data[counter++] | (data[counter++] << 8);
                        top = data[counter++] | (data[counter++] << 8);

                        w = data[counter++] | (data[counter++] << 8);
                        h = data[counter++] | (data[counter++] << 8);
                        bool localColorTableFlag = (data[counter] & 0x80) > 0;
                        interlaceFlag = (data[counter] & 0x40) > 0;
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
                        step = getLzwData;
                        break;
                    case 0x3b: // trailer    
                               //        Console.WriteLine ("trailer found!");    
                        return false;
                }
                return true;
            }

            public string serialize() {
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

            void unserialize(string s) {
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
                    this.unserialize(base64);
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
                step = mainLoop;
            }
        }


    }
}
