package portfolio.naval.model;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.HashSet;
import java.util.List;
import java.util.Random;
import java.util.Set;

import org.junit.jupiter.api.Test;

/** Unit tests of the model: coordinates, placement rules, shots, the computer and a whole game. */
class ModelTest {

    @Test
    void coordinatesReadAndWriteTheUsualNotation() {
        assertEquals(new Coord(0, 0), Coord.parse("A1"));
        assertEquals(new Coord(9, 9), Coord.parse("j10"));
        assertEquals("C5", new Coord(2, 4).toString());
        for (String bad : new String[] { "K1", "A0", "A11", "", "A", "1A", "Ax" }) {
            assertThrows(IllegalArgumentException.class, () -> Coord.parse(bad), bad);
        }
        assertThrows(IllegalArgumentException.class, () -> Coord.parse(null));
        assertThrows(IllegalArgumentException.class, () -> new Coord(10, 0));
    }

    @Test
    void shipsMustStayInsideAndNeitherOverlapNorTouch() {
        Board b = new Board();
        b.place(ShipType.CARRIER, Coord.parse("A1"), true); // A1-A5
        assertEquals("outside the grid", b.whyNot(ShipType.BATTLESHIP, Coord.parse("A8"), true));
        assertEquals("overlaps another ship", b.whyNot(ShipType.BATTLESHIP, Coord.parse("A3"), false));
        assertEquals("touches another ship", b.whyNot(ShipType.BATTLESHIP, Coord.parse("B6"), true)); // corner of A5
        assertEquals("touches another ship", b.whyNot(ShipType.DESTROYER, Coord.parse("A6"), true));
        assertNull(b.whyNot(ShipType.BATTLESHIP, Coord.parse("C1"), true));
        assertEquals("already placed", b.whyNot(ShipType.CARRIER, Coord.parse("J1"), true));
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
            () -> b.place(ShipType.CRUISER, Coord.parse("B1"), true));
        assertTrue(e.getMessage().contains("touches"));
    }

    @Test
    void randomFleetsAreCompleteValidAndReproducible() {
        for (long seed = 0; seed < 2000; seed++) {
            Board b = Board.random(new Random(seed));
            assertTrue(b.fleetPlaced());
            Set<Coord> cells = new HashSet<>();
            for (ShipType t : ShipType.values()) {
                List<Coord> c = b.cellsOf(t);
                assertEquals(t.length(), c.size());
                cells.addAll(c);
            }
            assertEquals(17, cells.size(), "no overlap");
            // No two ships touch: every neighbour of a ship square is empty or the same ship.
            for (Coord c : cells) {
                for (int dr = -1; dr <= 1; dr++) {
                    for (int dc = -1; dc <= 1; dc++) {
                        int r = c.row() + dr, col = c.col() + dc;
                        if (!Coord.inside(r, col)) continue;
                        ShipType other = b.shipAt(new Coord(r, col));
                        assertTrue(other == null || other == b.shipAt(c), "seed " + seed + " at " + c);
                    }
                }
            }
        }
        assertEquals(Board.random(new Random(7)).cellsOf(ShipType.CARRIER), Board.random(new Random(7)).cellsOf(ShipType.CARRIER));
    }

    @Test
    void shotsMissHitSinkAndAreNotCountedTwice() {
        Board b = new Board();
        b.place(ShipType.DESTROYER, Coord.parse("C3"), false); // C3, D3
        assertEquals(Shot.Result.MISS, b.shoot(Coord.parse("A1")).result());
        assertEquals(Shot.Result.ALREADY_SHOT, b.shoot(Coord.parse("A1")).result());
        assertEquals(Shot.Result.HIT, b.shoot(Coord.parse("C3")).result());
        assertFalse(b.isSunk(ShipType.DESTROYER));
        Shot s = b.shoot(Coord.parse("D3"));
        assertEquals(Shot.Result.SUNK, s.result());
        assertEquals(ShipType.DESTROYER, s.sunk());
        assertTrue(s.touched());
        assertFalse(b.allSunk(), "the rest of the fleet is not placed");
    }

    @Test
    void theComputerNeverFiresTwiceAtTheSameSquareAndFinishesEveryGame() {
        for (long seed = 0; seed < 200; seed++) {
            Board target = Board.random(new Random(seed));
            Computer ai = new Computer(new Random(seed + 1));
            Set<Coord> fired = new HashSet<>();
            while (!target.allSunk()) {
                Coord c = ai.next();
                assertTrue(fired.add(c), "seed " + seed + " fired twice at " + c);
                ai.learn(c, target.shoot(c));
            }
            assertTrue(fired.size() <= 100);
        }
    }

    @Test
    void afterAHitTheComputerFiresNextToIt() {
        Board target = new Board();
        target.place(ShipType.CARRIER, Coord.parse("E3"), true); // E3-E7
        Computer ai = new Computer(new Random(1));
        ai.learn(Coord.parse("E5"), target.shoot(Coord.parse("E5")));
        Coord next = ai.next();
        assertEquals(1, Math.abs(next.row() - 4) + Math.abs(next.col() - 4), "a neighbour of E5, got " + next);
    }

    @Test
    void theComputerIsMuchBetterThanRandomShots() {
        // Random shots need about 96 of the 100 squares on average to sink a fleet of 17 squares.
        int total = 0, games = 300;
        for (long seed = 0; seed < games; seed++) {
            Board target = Board.random(new Random(seed));
            Computer ai = new Computer(new Random(seed + 1000));
            int shots = 0;
            while (!target.allSunk()) {
                Coord c = ai.next();
                ai.learn(c, target.shoot(c));
                shots++;
            }
            total += shots;
        }
        double mean = (double) total / games;
        assertTrue(mean < 60, "mean shots " + mean);
    }

    @Test
    void aWholeGameEndsWithAWinner() {
        Game g = new Game(42);
        Random person = new Random(3);
        while (!g.over()) {
            Coord at = new Coord(person.nextInt(10), person.nextInt(10));
            if (g.personFires(at).result() == Shot.Result.ALREADY_SHOT) continue;
            if (!g.over()) g.computerFires();
        }
        assertTrue(g.personBoard().allSunk() != g.computerBoard().allSunk(), "exactly one fleet sunk");
        assertNotEquals(0, g.computerShots());
        assertThrows(IllegalStateException.class, () -> g.personFires(Coord.parse("A1")));
    }
}
