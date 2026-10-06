package portfolio.naval.model;

import java.util.ArrayList;
import java.util.Collections;
import java.util.EnumMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

/**
 * One player's grid: its ships and the shots received. Ships may not overlap nor touch, not even by a
 * corner (the rule of the game this one rewrites), which also tells a player where nothing can be.
 */
public final class Board {
    private final ShipType[][] ships = new ShipType[Coord.SIZE][Coord.SIZE];
    private final boolean[][] shot = new boolean[Coord.SIZE][Coord.SIZE];
    private final Map<ShipType, List<Coord>> placed = new EnumMap<>(ShipType.class);

    /** Squares a ship of `type` would cover from `start`, horizontally (to the right) or vertically (down). */
    public static List<Coord> footprint(ShipType type, Coord start, boolean horizontal) {
        List<Coord> cells = new ArrayList<>();
        for (int i = 0; i < type.length(); i++) {
            int r = start.row() + (horizontal ? 0 : i), c = start.col() + (horizontal ? i : 0);
            if (!Coord.inside(r, c)) return null;
            cells.add(new Coord(r, c));
        }
        return cells;
    }

    /** Why the ship cannot go there, or null if it can. */
    public String whyNot(ShipType type, Coord start, boolean horizontal) {
        if (placed.containsKey(type)) return "already placed";
        List<Coord> cells = footprint(type, start, horizontal);
        if (cells == null) return "outside the grid";
        // Overlap first, over every square: it is the more serious fault and must not be reported as a touch.
        for (Coord c : cells) {
            if (ships[c.row()][c.col()] != null) return "overlaps another ship";
        }
        for (Coord c : cells) {
            for (int dr = -1; dr <= 1; dr++) {
                for (int dc = -1; dc <= 1; dc++) {
                    int r = c.row() + dr, col = c.col() + dc;
                    if (Coord.inside(r, col) && ships[r][col] != null) return "touches another ship";
                }
            }
        }
        return null;
    }

    /** Places a ship; throws IllegalArgumentException with the reason if it cannot go there. */
    public void place(ShipType type, Coord start, boolean horizontal) {
        String reason = whyNot(type, start, horizontal);
        if (reason != null) throw new IllegalArgumentException(type + " at " + start + ": " + reason);
        List<Coord> cells = footprint(type, start, horizontal);
        for (Coord c : cells) ships[c.row()][c.col()] = type;
        placed.put(type, Collections.unmodifiableList(cells));
    }

    /** A board with the whole fleet placed at random (largest ships first), reproducible from the seed. */
    public static Board random(Random random) {
        while (true) {
            Board b = new Board();
            if (b.tryPlaceFleet(random)) return b;
        }
    }

    private boolean tryPlaceFleet(Random random) {
        for (ShipType type : ShipType.values()) {
            boolean done = false;
            for (int attempt = 0; attempt < 200 && !done; attempt++) {
                Coord start = new Coord(random.nextInt(Coord.SIZE), random.nextInt(Coord.SIZE));
                boolean horizontal = random.nextBoolean();
                if (whyNot(type, start, horizontal) == null) {
                    place(type, start, horizontal);
                    done = true;
                }
            }
            if (!done) return false;
        }
        return true;
    }

    /** Fires at a square. */
    public Shot shoot(Coord at) {
        if (shot[at.row()][at.col()]) return Shot.ALREADY_SHOT;
        shot[at.row()][at.col()] = true;
        ShipType type = ships[at.row()][at.col()];
        if (type == null) return Shot.MISS;
        for (Coord c : placed.get(type)) {
            if (!shot[c.row()][c.col()]) return Shot.HIT;
        }
        return Shot.sunk(type);
    }

    public boolean isShot(Coord at) {
        return shot[at.row()][at.col()];
    }

    /** The ship on a square, or null (what the owner sees). */
    public ShipType shipAt(Coord at) {
        return ships[at.row()][at.col()];
    }

    public List<Coord> cellsOf(ShipType type) {
        return placed.getOrDefault(type, List.of());
    }

    public boolean isSunk(ShipType type) {
        List<Coord> cells = placed.get(type);
        return cells != null && cells.stream().allMatch(this::isShot);
    }

    public boolean fleetPlaced() {
        return placed.size() == ShipType.values().length;
    }

    public boolean allSunk() {
        return fleetPlaced() && placed.keySet().stream().allMatch(this::isSunk);
    }
}
