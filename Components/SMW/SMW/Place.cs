﻿public class Place {
    readonly ushort submap;
    readonly ushort level;
    readonly ushort room;
    readonly ushort x;
    readonly ushort y;

    public Place(ushort submap, ushort level, ushort room, ushort x, ushort y) {
        this.submap = submap;
        this.level = level;
        this.room = room;
        this.x = x;
        this.y = y;
    }

    public override string ToString() {
        return "Map " + submap + ", Level " + level + ", Room " + room + ", Pos (" + x + ", " + y + ")";
    }

    public override bool Equals(object obj) {
        if ((obj == null) || !GetType().Equals(obj.GetType())) {
            return false;
        } else {
            Place p = (Place)obj;
            return submap.Equals(p.submap) && level.Equals(p.level) && room.Equals(p.room) && x.Equals(p.x) && y.Equals(p.y);
        }
    }
}
