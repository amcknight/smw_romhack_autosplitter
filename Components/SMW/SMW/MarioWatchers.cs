﻿using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;

namespace SMW {
    public class MarioWatchers : MemoryWatcherList {
        public bool died;
        public bool roomStep;
        public ushort prevIO;

        public MarioWatchers() {
            died = false;
            roomStep = false;
            prevIO = 256; // junk default value
        }

        public void SetMemoryOffset(long memoryOffset) {
            foreach (KeyValuePair<int, string> entry in Memory.shortMap) {
                Add(new MemoryWatcher<short>((IntPtr)memoryOffset + entry.Key) { Name = entry.Value });
            }
            foreach (KeyValuePair<int, string> entry in Memory.byteMap) {
                Add(new MemoryWatcher<byte>((IntPtr)memoryOffset + entry.Key) { Name = entry.Value });
            }
        }

        public MemoryWatcher fileSelect => this["fileSelect"];
        public MemoryWatcher submap => this["submap"];
        public MemoryWatcher fanfare => this["fanfare"];
        public MemoryWatcher victory => this["victory"];
        public MemoryWatcher bossDefeat => this["bossDefeat"];
        public MemoryWatcher io => this["io"];
        public MemoryWatcher yellowSwitch => this["yellowSwitch"];
        public MemoryWatcher greenSwitch => this["greenSwitch"];
        public MemoryWatcher blueSwitch => this["blueSwitch"];
        public MemoryWatcher redSwitch => this["redSwitch"];
        public MemoryWatcher roomCounter => this["roomCounter"];
        public MemoryWatcher peach => this["peach"];
        public MemoryWatcher checkpointTape => this["checkpointTape"];
        public MemoryWatcher pipe => this["pipe"];
        public MemoryWatcher playerAnimation => this["playerAnimation"];
        public MemoryWatcher yoshiCoin => this["yoshiCoin"];
        public MemoryWatcher levelStart => this["levelStart"];
        public MemoryWatcher weirdLevVal => this["weirdLevVal"];
        public MemoryWatcher eventsTriggered => this["eventsTriggered"];
        public MemoryWatcher overworldPortal => this["overworldPortal"];
        public MemoryWatcher levelNum => this["levelNum"];
        public MemoryWatcher roomNum => this["roomNum"];
        public MemoryWatcher overworldExitEvent => this["overworldExitEvent"];
        public MemoryWatcher exitMode => this["exitMode"];
        public MemoryWatcher playerX => this["playerX"];
        public MemoryWatcher playerY => this["playerY"];

        // Temporary Test Watchers
        public MemoryWatcher gameMode => this["gameMode"];
        public MemoryWatcher levelMode => this["levelMode"];
        public MemoryWatcher player => this["player"];
        public MemoryWatcher cp1up => this["cp1up"];

        public bool ToOrb => ShiftTo(io, 3);
        public bool ToGoal => ShiftTo(io, 4);
        public bool ToKey => ShiftTo(io, 7);
        public bool GotOrb => Curr(io) == 3;
        public bool GotGoal => Curr(io) == 4;
        public bool GotKey => Curr(io) == 7;
        public bool GotFadeout => Curr(io) == 8;
        public bool BossUndead => Curr(bossDefeat) == 0;
        public bool GmFadeToLevel => ShiftTo(gameMode, 15);
        public bool GmFadeToLevelBlack => ShiftTo(gameMode, 16);
        public bool GmLoadLevel => ShiftTo(gameMode, 17);
        public bool GmPrepareLevel => ShiftTo(gameMode, 18);
        public bool GmLevelFadeIn => ShiftTo(gameMode, 19);
        public bool GmLevel => ShiftTo(gameMode, 20);
        public bool DiedNow => ShiftTo(playerAnimation, 9);
        public bool NewEvent => Stepped(eventsTriggered);
        public bool ToExit => ShiftFrom(exitMode, 0) && !ShiftTo(exitMode, 128);
        public bool EnteredPipe => Shifted(pipe) && Curr(pipe) < 4 && (Curr(playerAnimation) == 5 || Curr(playerAnimation) == 6);
        public bool Put => GmPrepareLevel && !died;
        public bool Spawn => GmPrepareLevel && died;
        public bool ToOverworldPortal => Shift(overworldPortal, 1, 0);
        public bool SubmapShift => Shifted(submap);
        public bool ToFanfare => StepTo(fanfare, 1);
        public bool IntroExit => Shift(weirdLevVal, 233, 0);
        public bool ToYellowSwitch => StepTo(yellowSwitch, 1);
        public bool ToGreenSwitch => StepTo(greenSwitch, 1);
        public bool ToBlueSwitch => StepTo(blueSwitch, 1);
        public bool ToRedSwitch => StepTo(redSwitch, 1);
        public bool ToLevelStart => StepTo(levelStart, 1);
        public bool ToPeachRelease => StepTo(peach, 1);
        public bool ToCheckpointTape => StepTo(checkpointTape, 1);

        public void UpdateState() {

            // Only roomStep if didn't just die. Assumes every death sets the roomCount to 1.
            died = died || DiedNow;
            roomStep = false;
            if (Stepped(roomCounter)) {
                roomStep = Curr(roomCounter) != 1 || !died;
            }
            // PrevIO is basically Current IO except when a P-Switch or Star shifts the io to 0
            if (Curr(io) != 0) {
                prevIO = Curr(io);
            }

            if (Spawn) died = false;
        }

        public ushort Prev(MemoryWatcher w) {
            return Convert.ToUInt16(w.Old);
        }

        public ushort Curr(MemoryWatcher w) {
            return Convert.ToUInt16(w.Current);
        }
        public bool Shift(MemoryWatcher w, ushort o, ushort c) {
            return Prev(w) == o && Curr(w) == c;
        }

        public bool ShiftTo(MemoryWatcher w, ushort c) {
            return Prev(w) != c && Curr(w) == c;
        }

        public bool ShiftFrom(MemoryWatcher w, ushort o) {
            return Prev(w) == o && Curr(w) != o;
        }

        public bool Shifted(MemoryWatcher w) {
            return Prev(w) != Curr(w);
        }

        public bool StepTo(MemoryWatcher w, ushort c) {
            return Curr(w) == c && Prev(w) + 1 == Curr(w);
        }

        public bool Stepped(MemoryWatcher w) {
            return Prev(w) + 1 == Curr(w);
        }

        public Event BuildEvent(string name) {
            return new Event(name, new Place(Curr(submap), Curr(levelNum), Curr(roomNum), Curr(playerX), Curr(playerY)));
        }
    }
}
