package portfolio.aventure;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.List;

import org.junit.jupiter.api.Test;

/** Tests of the adventure: commands in both languages, rules, fights, and whole games played by script. */
class GameTest {

    private static Game play(Game g, String... commands) {
        for (String c : commands) g.respond(c);
        return g;
    }

    /** Attacks until the robot is down, 10 blows at most: a broken fight fails the test instead of looping. */
    private static void fightRobot(Game g, String verb, String down) {
        for (int i = 0; i < 10; i++) {
            if (g.respond(verb).contains(down)) return;
        }
        throw new AssertionError("the robot is still up after 10 blows");
    }

    @Test
    void everyTextExistsInBothLanguages() {
        assertEquals(Texts.keys(Lang.FR), Texts.keys(Lang.EN));
        for (String key : Texts.keys(Lang.FR)) {
            assertFalse(Texts.get(Lang.FR, key).isBlank(), key);
            assertFalse(Texts.get(Lang.EN, key).isBlank(), key);
        }
    }

    @Test
    void commandsIgnoreCaseAccentsAndArticles() {
        assertEquals(List.of("prendre", "cle"), Game.words("  Prendre la Clé "));
        assertEquals(List.of("prendre", "eau"), Game.words("prendre l’eau"));
        assertEquals(List.of("take", "umbrella"), Game.words("take the umbrella"));
        assertEquals(List.of("aller", "nord"), Game.words("aller vers le nord"));
    }

    @Test
    void movingAroundAndTheWayOut() {
        Game g = new Game(Lang.FR, 1);
        assertEquals("hall", g.place());
        assertTrue(g.respond("ouest").startsWith("Vous ne pouvez pas"));
        g.respond("nord");
        assertEquals("corridor", g.place());
        g.respond("aller est");
        assertEquals("office", g.place());
        assertTrue(g.respond("aller").startsWith("Aller où"));
        g.respond("o");
        g.respond("s");
        assertTrue(g.respond("sud").contains("Pas sans votre clé USB"), "no way out without the thesis");
        assertFalse(g.over());
    }

    @Test
    void theServerRoomNeedsTheBadge() {
        Game g = new Game(Lang.EN, 1);
        g.respond("north");
        assertEquals("The server room door is locked behind a badge reader.", g.respond("up"));
        play(g, "east", "take badge", "west");
        String r = g.respond("up");
        assertTrue(r.startsWith("You swipe your badge"), r);
        assertEquals("servers", g.place());
    }

    @Test
    void itemsAreTakenDroppedAndUsed() {
        Game g = new Game(Lang.FR, 1);
        assertEquals("Le badge n'est pas ici.", g.respond("prendre badge"));
        assertEquals("Vous ne connaissez pas d'objet nommé « licorne ».", g.respond("prendre licorne"));
        assertEquals("Prendre quoi ?", g.respond("prendre"));
        play(g, "nord", "ouest");
        assertEquals("Vous prenez le café.", g.respond("prendre le café"));
        assertEquals("Vous posez le café.", g.respond("poser cafe"));
        assertEquals("Vous n'avez pas le café.", g.respond("utiliser café"));
        play(g, "prendre café", "prendre sandwich");
        assertTrue(g.respond("sac").startsWith("Dans votre sac : le café, le sandwich."));
        assertEquals("Vous prenez le café : énergie 20/20.", g.respond("boire café"), "energy never goes above the maximum");
        assertEquals(List.of(Item.SANDWICH), g.bag());
    }

    @Test
    void theGuardGivesTheHints() {
        Game g = new Game(Lang.EN, 1);
        String hint = g.respond("talk");
        assertTrue(hint.contains("badge") && hint.contains("umbrella"), hint);
        g.respond("north");
        assertEquals("There is nobody to talk to.", g.respond("talk"));
    }

    @Test
    void theKeyIsGuardedUntilTheRobotIsDown() {
        Game g = play(new Game(Lang.EN, 1), "north", "east", "take badge", "west", "north", "take umbrella", "south", "up");
        assertEquals("The robot blocks your way: you cannot get near the key.", g.respond("take key"));
        String last = "";
        for (int i = 0; i < 10 && !last.contains("shuts down"); i++) last = g.respond("attack");
        assertTrue(last.contains("shuts down"), last);
        assertEquals("You take the USB key.", g.respond("take usb"));
        assertEquals("There is nothing to attack here.", g.respond("attack"));
    }

    @Test
    void withTheUmbrellaTheRobotIsBeatenWithoutItThePlayerLoses() {
        // The umbrella adds 3 to every blow: over 200 seeds it always wins, bare hands always lose.
        for (long seed = 0; seed < 200; seed++) {
            Game armed = play(new Game(Lang.FR, seed), "nord", "est", "prendre badge", "ouest", "nord", "prendre parapluie", "sud", "haut");
            for (int i = 0; i < 20 && !armed.over() && armed.bag().size() == 2; i++) {
                if (armed.respond("attaquer").contains("s'éteint")) break;
            }
            assertFalse(armed.lost(), "armed, seed " + seed);
            Game bare = play(new Game(Lang.FR, seed), "nord", "est", "prendre badge", "ouest", "haut");
            for (int i = 0; i < 30 && !bare.over(); i++) bare.respond("attaquer");
            assertTrue(bare.lost(), "bare hands, seed " + seed);
            assertEquals("La partie est finie. Tapez « recommencer » pour rejouer.", bare.respond("regarder"));
        }
    }

    @Test
    void aWholeGameIsWonInFrenchAndInEnglish() {
        Game fr = play(new Game(Lang.FR, 7), "parler", "nord", "est", "prendre le badge", "ouest", "nord", "prendre le parapluie",
            "sud", "ouest", "prendre le sandwich", "est", "haut");
        fightRobot(fr, "attaquer", "s'éteint");
        play(fr, "prendre la clé", "bas", "sud");
        assertTrue(fr.respond("sud").contains("Gagné"));
        assertTrue(fr.won() && fr.over());

        Game en = play(new Game(Lang.EN, 7), "n", "e", "take badge", "w", "n", "take umbrella", "s", "up");
        fightRobot(en, "fight", "shuts down");
        play(en, "take the key", "down", "s");
        assertTrue(en.respond("s").contains("You win"));
        assertTrue(en.won());
        assertTrue(en.respond("restart").startsWith("The lab at night"));
        assertEquals("hall", en.place());
        assertFalse(en.over());
    }

    @Test
    void unknownAndEmptyCommands() {
        Game g = new Game(Lang.FR, 1);
        assertEquals("Je ne comprends pas « danser ». Tapez « aide ».", g.respond("danser"));
        assertEquals("Que voulez-vous faire ?", g.respond("   "));
        assertEquals("Que voulez-vous faire ?", g.respond(null));
        assertTrue(g.respond("aide").startsWith("Commandes"));
        assertEquals("À bientôt.", g.respond("quitter"));
        assertTrue(g.over());
    }
}
