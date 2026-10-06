package portfolio.naval.ui;

import java.util.Locale;

import javafx.application.Application;
import javafx.scene.Scene;
import javafx.stage.Stage;

/** The window: one NavalView, in the system's language (French or English). */
public final class NavalApp extends Application {
    @Override
    public void start(Stage stage) {
        NavalView view = new NavalView(Locale.getDefault(), System.nanoTime());
        stage.setTitle(view.root().lookup(".title") instanceof javafx.scene.control.Label l ? l.getText() : "Naval");
        stage.setScene(new Scene(view.root()));
        stage.show();
    }
}
