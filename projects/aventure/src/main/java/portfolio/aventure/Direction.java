package portfolio.aventure;

/** The six directions, recognised in French and English. */
public enum Direction {
    NORTH("nord", "north", "n"), SOUTH("sud", "south", "s"), EAST("est", "east", "e"), WEST("ouest", "west", "o", "w"),
    UP("haut", "up", "monter"), DOWN("bas", "down", "descendre");

    private final String[] words;

    Direction(String... words) {
        this.words = words;
    }

    /** The direction a word names, in either language; null if none. */
    public static Direction parse(String word) {
        for (Direction d : values()) {
            for (String w : d.words) if (w.equals(word)) return d;
        }
        return null;
    }

    public String label(Lang lang) {
        return Texts.get(lang, "dir." + name().toLowerCase());
    }
}
