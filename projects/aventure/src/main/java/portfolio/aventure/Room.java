package portfolio.aventure;

import java.util.ArrayList;
import java.util.EnumMap;
import java.util.List;
import java.util.Map;

/** A place: its exits (some locked behind an item), the items lying there, and whether the guard is here. */
public final class Room {
    public final String id;
    final Map<Direction, Room> exits = new EnumMap<>(Direction.class);
    final Map<Direction, Item> locks = new EnumMap<>(Direction.class);
    final List<Item> items = new ArrayList<>();
    boolean guard;

    Room(String id) {
        this.id = id;
    }

    void connect(Direction d, Room other, Direction back) {
        exits.put(d, other);
        other.exits.put(back, this);
    }
}
