package portfolio.aventure;

/** The objects of the game: what they are called, and what using them does. */
public enum Item {
    BADGE(0, 0, "badge"),
    UMBRELLA(3, 0, "parapluie", "umbrella"),
    COFFEE(0, 6, "cafe", "coffee"),
    SANDWICH(0, 10, "sandwich"),
    USB(0, 0, "cle", "usb", "key");

    /** Extra damage when carried. */
    public final int power;
    /** Energy given back when used (and then gone). */
    public final int heal;
    private final String[] words;

    Item(int power, int heal, String... words) {
        this.power = power;
        this.heal = heal;
        this.words = words;
    }

    /** The item a word names, in either language (accents ignored); null if none. */
    public static Item parse(String word) {
        for (Item i : values()) {
            for (String w : i.words) if (w.equals(word)) return i;
        }
        return null;
    }

    public String label(Lang lang) {
        return Texts.get(lang, "item." + name().toLowerCase());
    }
}
