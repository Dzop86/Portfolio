package portfolio.naval.ui;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Locale;
import java.util.Random;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.function.Supplier;

import javafx.application.Platform;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.image.WritableImage;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import portfolio.naval.model.Coord;

/**
 * The JavaFX interface, headless (Monocle, set in the pom): clicks fire, the computer answers, the end
 * of game disables the grid. With -Dnaval.screenshots=DIR it also writes the project page's screenshots.
 */
class ViewTest {

    @BeforeAll
    static void startJavaFx() throws Exception {
        CompletableFuture<Void> started = new CompletableFuture<>();
        try {
            Platform.startup(() -> started.complete(null));
        } catch (IllegalStateException alreadyStarted) {
            started.complete(null);
        }
        started.get(10, TimeUnit.SECONDS);
    }

    private static <T> T fx(Supplier<T> work) throws Exception {
        CompletableFuture<T> result = new CompletableFuture<>();
        Platform.runLater(() -> {
            try {
                result.complete(work.get());
            } catch (Throwable t) {
                result.completeExceptionally(t);
            }
        });
        return result.get(20, TimeUnit.SECONDS);
    }

    @Test
    void aClickFiresAndTheComputerAnswers() throws Exception {
        String[] texts = fx(() -> {
            NavalView v = new NavalView(Locale.ENGLISH, 7);
            new Scene(v.root());
            String before = v.status();
            ((Button) v.root().lookup("#enemy-A1")).fire();
            return new String[] { before, v.status(), Integer.toString(v.game().personShots()), Integer.toString(v.game().computerShots()) };
        });
        assertEquals("Both fleets are in place. Fire at the enemy grid.", texts[0]);
        assertTrue(texts[1].startsWith("You fire at A1: "), texts[1]);
        assertTrue(texts[1].contains("The computer fires at "), texts[1]);
        assertEquals("1", texts[2]);
        assertEquals("1", texts[3]);
    }

    @Test
    void firingTwiceAtTheSameSquareCostsNothing() throws Exception {
        String[] r = fx(() -> {
            NavalView v = new NavalView(Locale.FRENCH, 7);
            new Scene(v.root());
            Button a1 = (Button) v.root().lookup("#enemy-A1");
            a1.fire();
            a1.fire();
            return new String[] { v.status(), Integer.toString(v.game().personShots()) };
        });
        assertEquals("Vous avez déjà tiré en A1.", r[0]);
        assertEquals("1", r[1]);
    }

    @Test
    void ownShipsAreShownAndTheEnemyGridIsDisabledAtTheEnd() throws Exception {
        boolean[] r = fx(() -> {
            NavalView v = new NavalView(Locale.ENGLISH, 11);
            new Scene(v.root());
            long ships = v.root().lookupAll(".ship").stream().filter(n -> n.getId() != null && n.getId().startsWith("own-")).count();
            for (int i = 0; i < 100 && !v.game().over(); i++) v.fireAt(new Coord(i / 10, i % 10));
            return new boolean[] { ships == 17, v.game().over(), v.root().lookup("#enemy-J10").isDisabled(),
                v.status().contains("won") || v.status().contains("sank your whole fleet") };
        });
        assertTrue(r[0], "17 squares of own ships shown");
        assertTrue(r[1]);
        assertTrue(r[2], "grid disabled at the end");
        assertTrue(r[3], "end of game announced");
    }

    @Test
    void screenshotsForTheProjectPage() throws Exception {
        String dir = System.getProperty("naval.screenshots");
        // Only on request (mvn test -Dnaval.screenshots=DIR); Maven passes "" when the property is not given.
        if (dir == null || dir.isBlank() || dir.startsWith("${")) return;
        for (Locale locale : new Locale[] { Locale.FRENCH, Locale.ENGLISH }) {
            WritableImage image = fx(() -> {
                NavalView v = new NavalView(locale, 2026);
                Scene scene = new Scene(v.root());
                Random person = new Random(5);
                for (int i = 0; i < 24; i++) v.fireAt(new Coord(person.nextInt(10), person.nextInt(10)));
                return scene.snapshot(null);
            });
            Path out = Path.of(dir, "naval-" + locale.getLanguage() + ".png");
            Files.createDirectories(out.getParent());
            try (OutputStream o = Files.newOutputStream(out)) {
                Png.write(image, o);
            }
        }
    }
}
