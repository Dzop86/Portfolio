package portfolio.naval.model;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
import java.util.List;
import java.util.Random;

/**
 * The computer's shots: it hunts on a checkerboard (the smallest ship covers two squares, so it cannot
 * hide between them), then targets around a hit, following the line once two hits are aligned. When a
 * ship sinks, the squares around it are crossed out: by the no-contact rule nothing can be there.
 */
public final class Computer {
    private final Random random;
    /** What the computer knows of the opponent's grid: 0 unknown, 1 miss or impossible, 2 hit. */
    private final int[][] known = new int[Coord.SIZE][Coord.SIZE];
    private final List<Coord> hitsOnCurrentShip = new ArrayList<>();
    private final Deque<Coord> targets = new ArrayDeque<>();

    public Computer(Random random) {
        this.random = random;
    }

    /** The next square to fire at; never one already known. */
    public Coord next() {
        while (!targets.isEmpty()) {
            Coord c = targets.pollFirst();
            if (known[c.row()][c.col()] == 0) return c;
        }
        List<Coord> hunt = new ArrayList<>(), other = new ArrayList<>();
        for (int r = 0; r < Coord.SIZE; r++) {
            for (int c = 0; c < Coord.SIZE; c++) {
                if (known[r][c] == 0) ((r + c) % 2 == 0 ? hunt : other).add(new Coord(r, c));
            }
        }
        List<Coord> pool = hunt.isEmpty() ? other : hunt;
        if (pool.isEmpty()) throw new IllegalStateException("every square is known");
        return pool.get(random.nextInt(pool.size()));
    }

    /** Tells the computer what its shot at `at` did, so it can aim the next ones. */
    public void learn(Coord at, Shot shot) {
        switch (shot.result()) {
            case MISS -> known[at.row()][at.col()] = 1;
            case HIT -> {
                known[at.row()][at.col()] = 2;
                hitsOnCurrentShip.add(at);
                retarget();
            }
            case SUNK -> {
                known[at.row()][at.col()] = 2;
                hitsOnCurrentShip.add(at);
                for (Coord c : hitsOnCurrentShip) crossOutAround(c);
                hitsOnCurrentShip.clear();
                targets.clear();
            }
            case ALREADY_SHOT -> { }
        }
    }

    private void crossOutAround(Coord c) {
        for (int dr = -1; dr <= 1; dr++) {
            for (int dc = -1; dc <= 1; dc++) {
                int r = c.row() + dr, col = c.col() + dc;
                if (Coord.inside(r, col) && known[r][col] == 0) known[r][col] = 1;
            }
        }
    }

    /** Neighbours of the hits; once two hits are aligned, only the two ends of that line. */
    private void retarget() {
        targets.clear();
        boolean aligned = hitsOnCurrentShip.size() >= 2;
        boolean horizontal = aligned && hitsOnCurrentShip.get(0).row() == hitsOnCurrentShip.get(1).row();
        for (Coord h : hitsOnCurrentShip) {
            int[][] steps = !aligned ? new int[][] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }
                    : horizontal ? new int[][] { { 0, 1 }, { 0, -1 } } : new int[][] { { 1, 0 }, { -1, 0 } };
            for (int[] s : steps) {
                int r = h.row() + s[0], c = h.col() + s[1];
                if (Coord.inside(r, c) && known[r][c] == 0) targets.add(new Coord(r, c));
            }
        }
    }
}
