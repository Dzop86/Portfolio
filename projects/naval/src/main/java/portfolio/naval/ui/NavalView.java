package portfolio.naval.ui;

import java.util.Locale;
import java.util.ResourceBundle;

import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Parent;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.layout.GridPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.VBox;
import portfolio.naval.model.Coord;
import portfolio.naval.model.Game;
import portfolio.naval.model.ShipType;
import portfolio.naval.model.Shot;

/**
 * The game window's content, built from a Game and a language; no Stage needed, so tests and the
 * screenshot tool can drive it headless. Cells are buttons with ids "enemy-A1" and "own-A1".
 */
public final class NavalView {
    public static final int CELL = 30;

    private final ResourceBundle text;
    private final VBox root = new VBox(12);
    private final Label status = new Label();
    private final Label score = new Label();
    private final Button[][] own = new Button[Coord.SIZE][Coord.SIZE];
    private final Button[][] enemy = new Button[Coord.SIZE][Coord.SIZE];
    private Game game;
    private long seed;

    public NavalView(Locale locale, long seed) {
        this.text = ResourceBundle.getBundle("portfolio.naval.ui.messages", locale);
        Label title = new Label(text.getString("title"));
        title.getStyleClass().add("title");
        status.setId("status");
        status.getStyleClass().add("status");
        status.setWrapText(true);
        score.setId("score");
        Button newGame = new Button(text.getString("new"));
        newGame.setId("new-game");
        newGame.setOnAction(e -> start(this.seed + 1));
        HBox grids = new HBox(24, grid(text.getString("yours"), own, "own", false), grid(text.getString("enemy"), enemy, "enemy", true));
        grids.setAlignment(Pos.CENTER);
        root.getChildren().addAll(title, status, grids, score, newGame);
        root.setPadding(new Insets(16));
        root.setAlignment(Pos.TOP_CENTER);
        root.getStyleClass().add("naval");
        root.getStylesheets().add(NavalView.class.getResource("naval.css").toExternalForm());
        start(seed);
    }

    public Parent root() { return root; }
    public Game game() { return game; }
    public String status() { return status.getText(); }

    /** A new game from a seed (fleets and the computer's choices follow from it). */
    public void start(long newSeed) {
        this.seed = newSeed;
        this.game = new Game(newSeed);
        status.setText(text.getString("start"));
        refresh();
    }

    private VBox grid(String caption, Button[][] cells, String prefix, boolean clickable) {
        GridPane g = new GridPane();
        g.getStyleClass().add("grid");
        for (int c = 0; c < Coord.SIZE; c++) g.add(header(Integer.toString(c + 1)), c + 1, 0);
        for (int r = 0; r < Coord.SIZE; r++) {
            g.add(header(String.valueOf((char) ('A' + r))), 0, r + 1);
            for (int c = 0; c < Coord.SIZE; c++) {
                Coord at = new Coord(r, c);
                Button b = new Button();
                b.setId(prefix + "-" + at);
                b.setMinSize(CELL, CELL);
                b.setPrefSize(CELL, CELL);
                b.setMaxSize(CELL, CELL);
                b.setFocusTraversable(clickable);
                b.setAccessibleText(at.toString());
                if (clickable) b.setOnAction(e -> fireAt(at));
                cells[r][c] = b;
                g.add(b, c + 1, r + 1);
            }
        }
        Label l = new Label(caption);
        l.getStyleClass().add("caption");
        return new VBox(6, l, g);
    }

    private static Label header(String s) {
        Label l = new Label(s);
        l.setMinSize(CELL, CELL);
        l.setAlignment(Pos.CENTER);
        l.getStyleClass().add("header");
        return l;
    }

    /** The person fires at a square of the enemy grid, then the computer answers (unless the game ended). */
    public void fireAt(Coord at) {
        if (game.over()) return;
        Shot s = game.personFires(at);
        if (s.result() == Shot.Result.ALREADY_SHOT) {
            status.setText(String.format(text.getString("already"), at));
            return;
        }
        String mine = describe(s, at, "you");
        if (game.over()) {
            status.setText(mine + " " + String.format(text.getString("won"), game.personShots()));
        } else {
            Game.Turn t = game.computerFires();
            String theirs = describe(t.shot(), t.at(), "computer");
            status.setText(mine + " " + theirs + (game.over() ? " " + text.getString("lost") : ""));
        }
        refresh();
    }

    private String describe(Shot s, Coord at, String who) {
        String outcome = switch (s.result()) {
            case MISS -> text.getString("miss");
            case HIT -> text.getString("hit");
            case SUNK -> String.format(text.getString("sunk"), shipName(s.sunk()));
            case ALREADY_SHOT -> "";
        };
        return String.format(text.getString(who + ".fired"), at, outcome);
    }

    private String shipName(ShipType t) {
        return text.getString("ship." + t.name().toLowerCase(Locale.ROOT));
    }

    private void refresh() {
        for (int r = 0; r < Coord.SIZE; r++) {
            for (int c = 0; c < Coord.SIZE; c++) {
                Coord at = new Coord(r, c);
                style(own[r][c], game.personBoard().isShot(at), game.personBoard().shipAt(at), true, game.personBoard());
                style(enemy[r][c], game.computerBoard().isShot(at), game.computerBoard().shipAt(at), game.over(), game.computerBoard());
                enemy[r][c].setDisable(game.over());
            }
        }
        score.setText(String.format(text.getString("score"), game.personShots(), game.computerShots()));
    }

    /** One class per state: water, ship (shown on one's own grid and at the end), miss, hit, sunk. */
    private static void style(Button b, boolean shot, ShipType ship, boolean reveal, portfolio.naval.model.Board board) {
        b.getStyleClass().removeAll("water", "ship", "miss", "hit", "sunk");
        String state;
        if (shot && ship == null) state = "miss";
        else if (shot) state = board.isSunk(ship) ? "sunk" : "hit";
        else state = ship != null && reveal ? "ship" : "water";
        b.getStyleClass().add(state);
        b.setText(state.equals("miss") ? "•" : state.equals("hit") || state.equals("sunk") ? "✕" : "");
    }
}
