package portfolio.naval.model;

/** The classic fleet: one of each, 17 squares in all. */
public enum ShipType {
    CARRIER(5), BATTLESHIP(4), CRUISER(3), SUBMARINE(3), DESTROYER(2);

    private final int length;

    ShipType(int length) {
        this.length = length;
    }

    public int length() {
        return length;
    }
}
