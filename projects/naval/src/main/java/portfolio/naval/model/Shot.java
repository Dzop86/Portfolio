package portfolio.naval.model;

/** Outcome of a shot; `sunk` names the ship when the shot sank it. */
public record Shot(Result result, ShipType sunk) {
    public enum Result { MISS, HIT, SUNK, ALREADY_SHOT }

    static final Shot MISS = new Shot(Result.MISS, null);
    static final Shot HIT = new Shot(Result.HIT, null);
    static final Shot ALREADY_SHOT = new Shot(Result.ALREADY_SHOT, null);

    static Shot sunk(ShipType type) {
        return new Shot(Result.SUNK, type);
    }

    /** True for HIT and SUNK. */
    public boolean touched() {
        return result == Result.HIT || result == Result.SUNK;
    }
}
