package portfolio.naval.model;

/** A square of the 10 x 10 grid: row 0 to 9 (A to J), column 0 to 9 (1 to 10); written "A1" to "J10". */
public record Coord(int row, int col) {
    public static final int SIZE = 10;

    public Coord {
        if (row < 0 || row >= SIZE || col < 0 || col >= SIZE) {
            throw new IllegalArgumentException("outside the grid: " + row + ", " + col);
        }
    }

    /** "C5" or "c5" -> row 2, column 4. */
    public static Coord parse(String name) {
        String s = name == null ? "" : name.trim().toUpperCase();
        if (s.length() < 2 || s.length() > 3 || s.charAt(0) < 'A' || s.charAt(0) > 'J') {
            throw new IllegalArgumentException("not a square: " + name);
        }
        int col;
        try {
            col = Integer.parseInt(s.substring(1));
        } catch (NumberFormatException e) {
            throw new IllegalArgumentException("not a square: " + name, e);
        }
        if (col < 1 || col > SIZE) throw new IllegalArgumentException("not a square: " + name);
        return new Coord(s.charAt(0) - 'A', col - 1);
    }

    public static boolean inside(int row, int col) {
        return row >= 0 && row < SIZE && col >= 0 && col < SIZE;
    }

    @Override
    public String toString() {
        return (char) ('A' + row) + Integer.toString(col + 1);
    }
}
