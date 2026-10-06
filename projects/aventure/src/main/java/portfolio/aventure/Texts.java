package portfolio.aventure;

import java.util.HashMap;
import java.util.Map;

/**
 * Every sentence of the game, in French and English. Placeholders {0}, {1} are replaced by hand:
 * the engine avoids String.format so that it compiles to JavaScript with TeaVM.
 */
public final class Texts {
    private static final Map<String, String> FR = new HashMap<>();
    private static final Map<String, String> EN = new HashMap<>();

    private Texts() { }

    private static void put(String key, String fr, String en) {
        FR.put(key, fr);
        EN.put(key, en);
    }

    /** The text of `key` in `lang`, with {0}, {1}... replaced by `args`. */
    public static String get(Lang lang, String key, Object... args) {
        String s = (lang == Lang.FR ? FR : EN).get(key);
        if (s == null) throw new IllegalArgumentException("no text for " + key);
        for (int i = 0; i < args.length; i++) s = s.replace("{" + i + "}", String.valueOf(args[i]));
        return s;
    }

    /** Every key, for the tests (each must exist in both languages). */
    public static java.util.Set<String> keys(Lang lang) {
        return (lang == Lang.FR ? FR : EN).keySet();
    }

    static {
        put("title", "Le laboratoire de nuit", "The lab at night");
        put("intro",
            "Il est 23 h. Vous vous êtes endormi sur votre thèse et le laboratoire est fermé. La clé USB qui contient votre manuscrit est restée quelque part dans le bâtiment : impossible de partir sans elle. Tapez « aide » pour voir les commandes.",
            "It is 11 pm. You fell asleep over your thesis and the lab is closed. The USB key holding your manuscript is somewhere in the building: you cannot leave without it. Type \"help\" to see the commands.");
        put("help",
            "Commandes : aller <nord|sud|est|ouest|haut|bas> (ou juste la direction), regarder, prendre <objet>, poser <objet>, utiliser <objet>, sac, parler, attaquer, aide, quitter.",
            "Commands: go <north|south|east|west|up|down> (or just the direction), look, take <item>, drop <item>, use <item>, bag, talk, attack, help, quit.");
        put("unknown", "Je ne comprends pas « {0} ». Tapez « aide ».", "I do not understand \"{0}\". Type \"help\".");
        put("empty", "Que voulez-vous faire ?", "What do you want to do?");
        put("no.exit", "Vous ne pouvez pas aller par là.", "You cannot go that way.");
        put("which.way", "Aller où ? (nord, sud, est, ouest, haut, bas)", "Go where? (north, south, east, west, up, down)");
        put("locked.badge", "La porte de la salle des serveurs est fermée par un lecteur de badge.", "The server room door is locked behind a badge reader.");
        put("badge.opens", "Vous passez votre badge : bip, la porte s'ouvre.", "You swipe your badge: beep, the door opens.");
        put("exits", "Sorties : {0}.", "Exits: {0}.");
        put("items.here", "Vous voyez : {0}.", "You see: {0}.");
        put("bag.empty", "Votre sac est vide. Énergie : {0}/{1}.", "Your bag is empty. Energy: {0}/{1}.");
        put("bag", "Dans votre sac : {0}. Énergie : {1}/{2}.", "In your bag: {0}. Energy: {1}/{2}.");
        put("take.what", "Prendre quoi ?", "Take what?");
        put("drop.what", "Poser quoi ?", "Drop what?");
        put("use.what", "Utiliser quoi ?", "Use what?");
        put("not.here", "{0} n'est pas ici.", "{0} is not here.");
        put("not.carried", "Vous n'avez pas {0}.", "You do not have {0}.");
        put("taken", "Vous prenez {0}.", "You take {0}.");
        put("dropped", "Vous posez {0}.", "You drop {0}.");
        put("unknown.item", "Vous ne connaissez pas d'objet nommé « {0} ».", "You know no item called \"{0}\".");
        put("guarded", "Le robot vous barre le chemin : impossible d'approcher de la clé.", "The robot blocks your way: you cannot get near the key.");
        put("ate", "Vous prenez {0} : énergie {1}/{2}.", "You have {0}: energy {1}/{2}.");
        put("use.useless", "{0} ne sert à rien ici.", "{0} is of no use here.");
        put("use.weapon", "Vous serrez {0} : vous frapperez plus fort.", "You grip {0}: you will hit harder.");
        put("nobody", "Il n'y a personne à qui parler.", "There is nobody to talk to.");
        put("no.enemy", "Il n'y a rien à attaquer ici.", "There is nothing to attack here.");
        put("hit", "Vous frappez le robot ({0} dégâts, il lui reste {1}). Il riposte : {2} dégâts, il vous reste {3} d'énergie.",
            "You hit the robot ({0} damage, {1} left). It strikes back: {2} damage, you have {3} energy left.");
        put("robot.down", "Vous frappez le robot ({0} dégâts) : il s'éteint dans un dernier bip. La voie est libre.",
            "You hit the robot ({0} damage): it shuts down with a last beep. The way is clear.");
        put("lost", "Le robot vous repousse jusqu'au couloir et vous tombez de fatigue. Partie perdue.",
            "The robot pushes you back to the corridor and you collapse. Game over.");
        put("need.thesis", "Le gardien secoue la tête : « Pas sans votre clé USB, vous le regretteriez. »",
            "The guard shakes his head: \"Not without your USB key, you would regret it.\"");
        put("won", "Le gardien vous ouvre la porte. Vous sortez dans la nuit, votre thèse en poche. Gagné !",
            "The guard opens the door. You walk out into the night, your thesis in your pocket. You win!");
        put("over", "La partie est finie. Tapez « recommencer » pour rejouer.", "The game is over. Type \"restart\" to play again.");
        put("bye", "À bientôt.", "Goodbye.");
        put("talk.guard",
            "Le gardien : « Votre clé ? Un robot de ménage déréglé garde la salle des serveurs, à l'étage. Il faut un badge pour y entrer, il y en a toujours un qui traîne au bureau des doctorants. Et un bon parapluie, croyez-moi. »",
            "The guard: \"Your key? A broken cleaning robot guards the server room upstairs. You need a badge to get in, there is always one lying around in the PhD students' office. And a good umbrella, believe me.\"");

        put("dir.north", "nord", "north");
        put("dir.south", "sud", "south");
        put("dir.east", "est", "east");
        put("dir.west", "ouest", "west");
        put("dir.up", "haut", "up");
        put("dir.down", "bas", "down");

        put("item.badge", "le badge", "the badge");
        put("item.umbrella", "le parapluie", "the umbrella");
        put("item.coffee", "le café", "the coffee");
        put("item.sandwich", "le sandwich", "the sandwich");
        put("item.usb", "la clé USB", "the USB key");

        put("room.hall", "Le hall d'entrée. Le gardien de nuit somnole derrière son comptoir ; la porte de sortie est au sud.",
            "The entrance hall. The night guard dozes behind his desk; the way out is to the south.");
        put("room.corridor", "Le couloir du laboratoire, éclairé par les néons de secours. Un escalier monte vers la salle des serveurs.",
            "The lab corridor, lit by the emergency lights. A staircase leads up to the server room.");
        put("room.office", "Le bureau des doctorants : des piles d'articles, des écrans éteints, des tasses vides.",
            "The PhD students' office: piles of papers, dark screens, empty mugs.");
        put("room.library", "La bibliothèque. Entre deux rayonnages, quelqu'un a oublié ses affaires.",
            "The library. Between two shelves, someone left their things.");
        put("room.cafeteria", "La cafétéria. La machine à café ronronne encore.", "The cafeteria. The coffee machine is still humming.");
        put("room.servers", "La salle des serveurs, bruyante et froide.", "The server room, loud and cold.");
        put("robot.here", "Un robot de ménage déréglé tourne en rond en clignotant ({0} points de vie).",
            "A broken cleaning robot spins around, blinking ({0} hit points).");
        put("robot.off", "Le robot de ménage gît, éteint.", "The cleaning robot lies switched off.");
    }
}
