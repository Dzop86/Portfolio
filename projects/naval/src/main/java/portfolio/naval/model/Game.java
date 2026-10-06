package portfolio.naval.model;

import java.util.Random;

/** A game between a person and the computer, each on a random fleet; the person fires first. */
public final class Game {
    private final Board person;
    private final Board computer;
    private final Computer ai;
    private int personShots;
    private int computerShots;

    public Game(long seed) {
        Random random = new Random(seed);
        this.person = Board.random(random);
        this.computer = Board.random(random);
        this.ai = new Computer(random);
    }

    public Board personBoard() { return person; }
    public Board computerBoard() { return computer; }
    public int personShots() { return personShots; }
    public int computerShots() { return computerShots; }

    public boolean over() {
        return person.allSunk() || computer.allSunk();
    }

    /** True when the person has sunk the whole enemy fleet. */
    public boolean personWon() {
        return computer.allSunk();
    }

    /** The person fires; ALREADY_SHOT costs no turn. Throws IllegalStateException once the game is over. */
    public Shot personFires(Coord at) {
        if (over()) throw new IllegalStateException("the game is over");
        Shot s = computer.shoot(at);
        if (s.result() != Shot.Result.ALREADY_SHOT) personShots++;
        return s;
    }

    /** The computer fires back; returns the square and the outcome. */
    public Turn computerFires() {
        if (over()) throw new IllegalStateException("the game is over");
        Coord at = ai.next();
        Shot s = person.shoot(at);
        ai.learn(at, s);
        computerShots++;
        return new Turn(at, s);
    }

    public record Turn(Coord at, Shot shot) { }
}
