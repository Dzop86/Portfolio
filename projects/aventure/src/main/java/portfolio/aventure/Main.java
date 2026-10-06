package portfolio.aventure;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Locale;

/** The game in the terminal: java -jar aventure.jar [--lang fr|en] [--seed N]. Reads commands line by line. */
public final class Main {
    private Main() { }

    public static void main(String[] args) throws IOException {
        Lang lang = Locale.getDefault().getLanguage().equals("fr") ? Lang.FR : Lang.EN;
        long seed = System.nanoTime();
        for (int i = 0; i + 1 < args.length; i += 2) {
            switch (args[i]) {
                case "--lang" -> lang = args[i + 1].equalsIgnoreCase("fr") ? Lang.FR : Lang.EN;
                case "--seed" -> seed = Long.parseLong(args[i + 1]);
                default -> {
                    System.err.println("usage: aventure [--lang fr|en] [--seed N]");
                    System.exit(2);
                }
            }
        }
        Game game = new Game(lang, seed);
        System.out.println(game.intro());
        BufferedReader in = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
        String line;
        while (!game.over()) {
            System.out.print("\n> ");
            System.out.flush();
            if ((line = in.readLine()) == null) break;
            System.out.println(game.respond(line));
        }
    }
}
