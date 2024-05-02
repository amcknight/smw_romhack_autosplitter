state("snes9x"){}
state("snes9x-x64"){}
state("bsnes"){}
state("retroarch"){}
state("higan"){}
state("snes9x-rr"){}
state("emuhawk"){}

startup {
    vars.ready = false;
    vars.running = false;
    vars.startMs = vars.endMs = -1; // junk value
    int maxLagMs = 100;
    int minStartDurationMs = 1000;

    byte[] snesBytes = File.ReadAllBytes("Components/SNES.dll");
    Assembly snesAsm = Assembly.Load(snesBytes);
    vars.e = Activator.CreateInstance(snesAsm.GetType("SNES.Emu"));

    byte[] smwBytes = File.ReadAllBytes("Components/SMW.dll");
    Assembly smwAsm = Assembly.Load(smwBytes);
    vars.t =  Activator.CreateInstance(smwAsm.GetType("SMW.Tracker"));
    vars.ws = Activator.CreateInstance(smwAsm.GetType("SMW.Watchers"));
    vars.ss = Activator.CreateInstance(smwAsm.GetType("SMW.Settings"));
    vars.ss.Init(maxLagMs, minStartDurationMs);
    
    foreach (var entry in vars.ss.entries) {
        string k = entry.Key;
        var v = entry.Value;
        bool on =        v.Item1;
        string name =    v.Item2;
        string tooltip = v.Item3;
        string parent =  v.Item4;
        settings.Add(k, on, name, parent);
        settings.SetToolTip(k, tooltip);
    }
}

init {
    vars.e.Init(game);
}

update {
    var t = vars.t; var e = vars.e; var w = vars.ws; var s = vars.ss;
    
    if (t.HasLines()) print(t.ClearLines());

    vars.startMs = vars.endMs;

    try {
        e.Ready();
    } catch (Exception ex) {
        t.DbgOnce(ex);
        vars.ready = false;
        return vars.running; // Return vars.running for opposite behaviour in Start vs Reset
    }
    
    t.DbgOnce("SMC: " + e.Smc(), "smc");
    if (vars.ready) {
        // The order here matters (for Spawn recording)
        w.UpdateAll(game);
        var settingsDict = new Dictionary<string, bool>();
        foreach (string k in s.keys) {
            settingsDict[k] = settings[k];
        }
        s.Update(settingsDict, w);
        t.Update(w);
        w.UpdateState();
        
        // MONITOR HERE for monitoring even while not in a run
        
        //t.Monitor(w.roomNum, w);
        //t.Monitor(w.submap, w);

    } else {
        var ranges = new Dictionary<int, int>() {};
        try {
            var offset = e.GetOffset();
            w.SetMemoryOffset(offset, ranges);
            vars.memFoundTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            vars.ready = true;
        } catch (Exception ex) {
            t.DbgOnce(ex);
            return false;
        }
    }
}

start {
    var t = vars.t; var s = vars.ss;
    var startDuration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - vars.memFoundTime;
    if (s.StartStatus(startDuration)) {
        t.Dbg("Start: " + s.StartReasons());
        return true;
    }
}

reset {
    var t = vars.t; var s = vars.ss; var e = vars.e;
    bool smcChanged = e.SmcChanged();
    if (s.ResetStatus(vars.ready, smcChanged)) {
        var reasons = s.ResetReasons(vars.ready, smcChanged);
        t.Dbg("Reset: " + reasons);
        vars.ready = false;
        return true;
    }
}

split {
    var t = vars.t; var w = vars.ws; var s = vars.ss;

    string runName = string.Join(" - ", timer.Run.GameName, timer.Run.CategoryName);
    t.DbgOnce("Run: '"+runName+"'", "run");

    // Override Default split variables for individual runs. Customize Splits Tutorial: https://github.com/amcknight/kaizosplits?tab=readme-ov-file#custom-splits
    switch (runName) {
        case "Bunbun World - 100%":
            s.other =
                w.RoomShiftsInLevel(80) || // Six-Screen Suites
                w.RoomShiftInLevel(45, 9, 11) || // Mt. Ninji Secret. This should split on 1-up triggering the pipe instead
                w.RoomShiftInLevel(45, 9, 10) || // Mt. Ninji Ending
                w.RoomShiftInLevel(48, 12, 254) || // Slippery Spirits to Boss
                w.RoomShiftsInLevel(37) || // Cotton Candy Castle
                w.RoomShiftInLevel(78, 42, 74) || // Dizzy Drifting Secret pipe
                w.RoomShiftInLevel(51, 15, 198) || // Dolphin Dreams
                w.RoomShiftsInLevel(68) || // Breathtaking
                w.RoomShiftsInLevel(61) || // Night Sky Scamper
                w.RoomShiftInLevel(52, 16, 225) || // Bunbun Bastion
                w.ShiftIn(w.levelNum, 52, w.io, 3, 20) || // any% ending
                w.RoomShiftsInLevel(62) || // Culmination Castle
                w.RoomShiftInLevel(53, 17, 198) // Bowser's Tower
                ;
            s.credits = w.ShiftTo(w.io, 33) && w.Curr(w.levelNum) == 53; // Final Bowser hit (little late) (create a ShiftsToIn?)
        break;
        case "Cute Kaizo World - 100%":
            s.credits = w.ShiftTo(w.io, 21);
        break;
        case "Easyland - Beat the Game":
            s.credits = w.Curr(w.submap) == 6 && w.GmFadeToLevel && w.Curr(w.marioOverworldX) == 456 && w.Curr(w.marioOverworldY) == 392;
        break;
        case "Love Yourself - Welcome Home%":
            s.credits = w.EnterDoor && w.Curr(w.roomNum) == 66 && w.Curr(w.levelNum) == 85;
        break;
        case "Nonsense - 24 Exit":
            s.block = w.CPEntrance && w.Curr(w.roomNum) == 101; // Extra CP at beginning of Angry Parachutes when icy
            s.credits = w.ShiftIn(w.levelNum, 94, w.io, 255, 37);
        break;
    }

    if (s.UndoStatus()) {
        t.Dbg("Undo: " + s.UndoReasons());
        new TimerModel { CurrentState = timer }.UndoSplit();
    }

    vars.endMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    if (s.SplitStatus()) {
        t.Dbg("Split: " + s.SplitReasons());
        long lag = vars.endMs - vars.startMs;
        if (!s.SkipStatus(lag)) {
            return true;
        }
        t.Dbg("Skip: " + s.SkipReasons(lag));
        new TimerModel { CurrentState = timer }.SkipSplit();
    }
}

onStart {
    vars.running = true;
}

onReset {
    vars.running = false;
}