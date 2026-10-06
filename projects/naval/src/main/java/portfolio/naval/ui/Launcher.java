package portfolio.naval.ui;

import javafx.application.Application;

/**
 * Entry point. A main class that does not extend Application lets the game start from a plain class
 * path (mvn javafx:run, or java -cp ...), without the JavaFX module path.
 */
public final class Launcher {
    private Launcher() { }

    public static void main(String[] args) {
        Application.launch(NavalApp.class, args);
    }
}
