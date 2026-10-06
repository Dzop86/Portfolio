package portfolio.aventure;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

/**
 * "The lab at night": the whole game behind one method, respond(line) -> text. No input or output here,
 * so the terminal, the browser (TeaVM) and the tests drive the same engine.
 */
public final class Game {
    public static final int MAX_ENERGY = 20;
    /*
     * The fight is settled by the umbrella, whatever the dice. Bare hands hit 1 to 3: the robot (18 points)
     * needs at least 6 blows, and its 5 answers of 4 take the player's 20 points first. With the umbrella
     * (+3) blows hit 4 to 6: 5 blows always do, and 4 answers take only 16.
     */
    static final int ROBOT_LIFE = 18;
    static final int ROBOT_BLOW = 4;

    private final Lang lang;
    private final long seed;
    private Random random;
    private Room hall, corridor, office, library, cafeteria, servers;
    private Room here;
    private final List<Item> bag = new ArrayList<>();
    private int energy;
    private int robot;
    private boolean won, lost, quit;

    public Game(Lang lang, long seed) {
        this.lang = lang;
        this.seed = seed;
        reset();
    }

    private void reset() {
        random = new Random(seed);
        hall = new Room("hall");
        corridor = new Room("corridor");
        office = new Room("office");
        library = new Room("library");
        cafeteria = new Room("cafeteria");
        servers = new Room("servers");
        hall.connect(Direction.NORTH, corridor, Direction.SOUTH);
        corridor.connect(Direction.NORTH, library, Direction.SOUTH);
        corridor.connect(Direction.EAST, office, Direction.WEST);
        corridor.connect(Direction.WEST, cafeteria, Direction.EAST);
        corridor.connect(Direction.UP, servers, Direction.DOWN);
        corridor.locks.put(Direction.UP, Item.BADGE);
        hall.guard = true;
        office.items.add(Item.BADGE);
        library.items.add(Item.UMBRELLA);
        cafeteria.items.add(Item.COFFEE);
        cafeteria.items.add(Item.SANDWICH);
        servers.items.add(Item.USB);
        here = hall;
        bag.clear();
        energy = MAX_ENERGY;
        robot = ROBOT_LIFE;
        won = lost = quit = false;
    }

    public boolean over() { return won || lost || quit; }
    public boolean won() { return won; }
    public boolean lost() { return lost; }
    public String place() { return here.id; }
    public int energy() { return energy; }
    public List<Item> bag() { return List.copyOf(bag); }

    private String t(String key, Object... args) {
        return Texts.get(lang, key, args);
    }

    /** Title, story and the first room. */
    public String intro() {
        return t("title") + "\n\n" + t("intro") + "\n\n" + look();
    }

    /** Lower case, accents removed, so "Prendre la Clé" and "prendre la cle" are the same command. */
    static String normalise(String s) {
        String lower = s.toLowerCase().trim();
        StringBuilder b = new StringBuilder();
        for (char c : lower.toCharArray()) {
            switch (c) {
                case 'à', 'â', 'ä' -> b.append('a');
                case 'é', 'è', 'ê', 'ë' -> b.append('e');
                case 'î', 'ï' -> b.append('i');
                case 'ô', 'ö' -> b.append('o');
                case 'ù', 'û', 'ü' -> b.append('u');
                case 'ç' -> b.append('c');
                case '\u2019' -> b.append('\''); // typographic apostrophe, as typed on phones
                default -> b.append(c);
            }
        }
        return b.toString();
    }

    /** Words of the command, without the articles that carry no meaning ("prendre la cle" -> prendre, cle). */
    static List<String> words(String line) {
        List<String> out = new ArrayList<>();
        for (String w : normalise(line).split(" ")) {
            if (w.isEmpty()) continue;
            switch (w) {
                case "le", "la", "les", "l'", "un", "une", "du", "de", "the", "a", "an", "to", "vers", "au" -> { }
                default -> out.add(w.startsWith("l'") ? w.substring(2) : w);
            }
        }
        return out;
    }

    /** Plays one command and returns what the player sees. */
    public String respond(String line) {
        List<String> w = words(line == null ? "" : line);
        if (w.isEmpty()) return t("empty");
        String verb = w.get(0);
        String arg = w.size() > 1 ? w.get(1) : null;
        if (verb.equals("recommencer") || verb.equals("restart")) {
            reset();
            return intro();
        }
        if (over()) return t("over");
        Direction alone = Direction.parse(verb);
        if (alone != null) return go(alone);
        switch (verb) {
            case "aller", "go", "va", "vas" -> {
                Direction d = arg == null ? null : Direction.parse(arg);
                return d == null ? t("which.way") : go(d);
            }
            case "regarder", "look", "l" -> { return look(); }
            case "prendre", "take", "get", "ramasser" -> { return take(arg); }
            case "poser", "drop", "lacher" -> { return drop(arg); }
            case "utiliser", "use", "manger", "boire", "eat", "drink" -> { return use(arg); }
            case "sac", "bag", "inventaire", "inventory", "i" -> { return describeBag(); }
            case "parler", "talk" -> { return here.guard ? t("talk.guard") : t("nobody"); }
            case "attaquer", "attack", "fight", "frapper", "combattre" -> { return attack(); }
            case "aide", "help", "?" -> { return t("help"); }
            case "quitter", "quit", "exit" -> {
                quit = true;
                return t("bye");
            }
            default -> { return t("unknown", line.trim()); }
        }
    }

    private String go(Direction d) {
        if (here == hall && d == Direction.SOUTH) {
            if (!bag.contains(Item.USB)) return t("need.thesis");
            won = true;
            return t("won");
        }
        Room next = here.exits.get(d);
        if (next == null) return t("no.exit");
        Item lock = here.locks.get(d);
        String prefix = "";
        if (lock == Item.BADGE) {
            if (!bag.contains(Item.BADGE)) return t("locked.badge");
            prefix = t("badge.opens") + "\n";
        }
        here = next;
        return prefix + look();
    }

    private String look() {
        StringBuilder s = new StringBuilder(t("room." + here.id));
        if (here == servers) s.append(' ').append(robot > 0 ? t("robot.here", robot) : t("robot.off"));
        if (!here.items.isEmpty()) s.append('\n').append(t("items.here", list(here.items)));
        List<String> exits = new ArrayList<>();
        for (Direction d : Direction.values()) if (here.exits.containsKey(d)) exits.add(d.label(lang));
        if (here == hall) exits.add(Direction.SOUTH.label(lang));
        s.append('\n').append(t("exits", String.join(", ", exits)));
        return s.toString();
    }

    private String list(List<Item> items) {
        List<String> names = new ArrayList<>();
        for (Item i : items) names.add(i.label(lang));
        return String.join(", ", names);
    }

    private Item item(String arg) {
        return arg == null ? null : Item.parse(arg);
    }

    private String take(String arg) {
        if (arg == null) return t("take.what");
        Item i = item(arg);
        if (i == null) return t("unknown.item", arg);
        if (!here.items.contains(i)) return t("not.here", capitalise(i.label(lang)));
        if (i == Item.USB && robot > 0) return t("guarded");
        here.items.remove(i);
        bag.add(i);
        return t("taken", i.label(lang));
    }

    private String drop(String arg) {
        if (arg == null) return t("drop.what");
        Item i = item(arg);
        if (i == null) return t("unknown.item", arg);
        if (!bag.remove(i)) return t("not.carried", i.label(lang));
        here.items.add(i);
        return t("dropped", i.label(lang));
    }

    private String use(String arg) {
        if (arg == null) return t("use.what");
        Item i = item(arg);
        if (i == null) return t("unknown.item", arg);
        if (!bag.contains(i)) return t("not.carried", i.label(lang));
        if (i.heal > 0) {
            bag.remove(i);
            energy = Math.min(MAX_ENERGY, energy + i.heal);
            return t("ate", i.label(lang), energy, MAX_ENERGY);
        }
        if (i.power > 0) return t("use.weapon", i.label(lang));
        return t("use.useless", capitalise(i.label(lang)));
    }

    private static String capitalise(String s) {
        return s.isEmpty() ? s : Character.toUpperCase(s.charAt(0)) + s.substring(1);
    }

    private String describeBag() {
        if (bag.isEmpty()) return t("bag.empty", energy, MAX_ENERGY);
        return t("bag", list(bag), energy, MAX_ENERGY);
    }

    /** One exchange: the player hits (1 to 3, plus the umbrella's 3), then the robot answers (always 4). */
    private String attack() {
        if (here != servers || robot <= 0) return t("no.enemy");
        int power = 0;
        for (Item i : bag) power += i.power;
        int dealt = 1 + random.nextInt(3) + power;
        robot = Math.max(0, robot - dealt);
        if (robot == 0) return t("robot.down", dealt);
        int taken = ROBOT_BLOW;
        energy = Math.max(0, energy - taken);
        if (energy == 0) {
            lost = true;
            return t("hit", dealt, robot, taken, energy) + "\n" + t("lost");
        }
        return t("hit", dealt, robot, taken, energy);
    }
}
