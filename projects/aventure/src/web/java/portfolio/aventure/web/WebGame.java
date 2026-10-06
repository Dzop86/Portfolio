package portfolio.aventure.web;

import org.teavm.jso.JSExport;

import portfolio.aventure.Game;
import portfolio.aventure.Lang;

/**
 * Browser entry point, compiled to an ES module by TeaVM (profile "web"): start(lang, seed) returns the
 * introduction, respond(line) the answer to a command, over() whether the game is finished. One game at a
 * time, as in the terminal.
 */
public final class WebGame {
    private static Game game;

    private WebGame() { }

    @JSExport
    public static String start(String lang, int seed) {
        game = new Game("fr".equals(lang) ? Lang.FR : Lang.EN, seed);
        return game.intro();
    }

    @JSExport
    public static String respond(String line) {
        if (game == null) start("en", 0);
        return game.respond(line);
    }

    @JSExport
    public static boolean over() {
        return game != null && game.over();
    }

    public static void main(String[] args) {
        // Nothing to do on load: the page calls start().
    }
}
