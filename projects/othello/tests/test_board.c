/* Unit tests of the board: starting position, legal moves, flips, passes, end of game, notation, and
 * perft counts checked against the published values. */
#include <string.h>

#include "othello/othello.h"
#include "unity.h"

void setUp(void) {}
void tearDown(void) {}

static uint64_t sq(const char *name) { return 1ULL << oth_square_from_name(name); }

static void test_starting_position(void) {
    oth_board b;
    oth_init(&b);
    TEST_ASSERT_EQUAL_INT(OTH_BLACK, b.to_move);
    TEST_ASSERT_EQUAL_INT(2, oth_count(&b, OTH_BLACK));
    TEST_ASSERT_EQUAL_INT(2, oth_count(&b, OTH_WHITE));
    TEST_ASSERT_TRUE(b.discs[OTH_WHITE] & sq("d4"));
    TEST_ASSERT_TRUE(b.discs[OTH_BLACK] & sq("e4"));
    /* Black's four opening moves. */
    TEST_ASSERT_EQUAL_UINT64(sq("d3") | sq("c4") | sq("f5") | sq("e6"), oth_moves(&b));
}

static void test_a_move_flips_the_enclosed_discs(void) {
    oth_board b;
    oth_init(&b);
    TEST_ASSERT_EQUAL_UINT64(sq("d4"), oth_flips(&b, oth_square_from_name("d3")));
    TEST_ASSERT_EQUAL_INT(0, oth_play(&b, oth_square_from_name("d3")));
    TEST_ASSERT_EQUAL_INT(4, oth_count(&b, OTH_BLACK));
    TEST_ASSERT_EQUAL_INT(1, oth_count(&b, OTH_WHITE));
    TEST_ASSERT_EQUAL_INT(OTH_WHITE, b.to_move);
    /* White's answers to d3: c3, e3, c5. */
    TEST_ASSERT_EQUAL_UINT64(sq("c3") | sq("e3") | sq("c5"), oth_moves(&b));
}

static void test_flips_follow_every_enclosing_line(void) {
    /* Black d4, e4, d5; white c3, f4, d6; white to move. */
    oth_board b = { { 0, 0 }, OTH_WHITE };
    b.discs[OTH_BLACK] = sq("d4") | sq("e4") | sq("d5");
    b.discs[OTH_WHITE] = sq("c3") | sq("f4") | sq("d6");
    /* e5: the diagonal e5-d4-c3 encloses d4; e4 and d5 lead to empty squares. */
    TEST_ASSERT_EQUAL_UINT64(sq("d4"), oth_flips(&b, oth_square_from_name("e5")));
    /* c4: the row c4-d4-e4-f4 encloses two discs. */
    TEST_ASSERT_EQUAL_UINT64(sq("d4") | sq("e4"), oth_flips(&b, oth_square_from_name("c4")));
    /* d3: the column d3-d4-d5-d6 encloses two discs. */
    TEST_ASSERT_EQUAL_UINT64(sq("d4") | sq("d5"), oth_flips(&b, oth_square_from_name("d3")));
    /* g4 touches only its own f4: nothing to flip, so it is not a move. */
    TEST_ASSERT_EQUAL_UINT64(0, oth_flips(&b, oth_square_from_name("g4")));
    TEST_ASSERT_EQUAL_UINT64(0, oth_moves(&b) & sq("g4"));
}

static void test_illegal_moves_are_refused_and_change_nothing(void) {
    oth_board b, before;
    oth_init(&b);
    before = b;
    TEST_ASSERT_EQUAL_INT(-1, oth_play(&b, oth_square_from_name("a1")));
    TEST_ASSERT_EQUAL_INT(-1, oth_play(&b, oth_square_from_name("d4"))); /* occupied */
    TEST_ASSERT_EQUAL_INT(-1, oth_play(&b, 64));
    TEST_ASSERT_EQUAL_INT(-1, oth_play(&b, -1));
    TEST_ASSERT_EQUAL_MEMORY(&before, &b, sizeof b);
    TEST_ASSERT_EQUAL_INT(-1, oth_pass(&b)); /* black has moves: no pass */
}

static void test_moves_do_not_wrap_around_the_edges(void) {
    /* Black h1, white a2: in square numbers they are neighbours (7 and 8), on the board they are not. */
    oth_board b = { { 0, 0 }, OTH_BLACK };
    b.discs[OTH_BLACK] = sq("h1");
    b.discs[OTH_WHITE] = sq("a2");
    TEST_ASSERT_EQUAL_UINT64(0, oth_moves(&b));
    TEST_ASSERT_EQUAL_UINT64(0, oth_flips(&b, oth_square_from_name("b2")));
}

static void test_pass_and_end_of_game(void) {
    /* Black a1, white b1: black can play c1; white has no move and must pass. */
    oth_board b = { { 0, 0 }, OTH_WHITE };
    b.discs[OTH_BLACK] = sq("a1");
    b.discs[OTH_WHITE] = sq("b1");
    TEST_ASSERT_EQUAL_UINT64(0, oth_moves(&b));
    TEST_ASSERT_FALSE(oth_game_over(&b));
    TEST_ASSERT_EQUAL_INT(0, oth_pass(&b));
    TEST_ASSERT_EQUAL_INT(OTH_BLACK, b.to_move);
    TEST_ASSERT_EQUAL_INT(0, oth_play(&b, oth_square_from_name("c1")));
    /* Only black discs left: nobody can move. */
    TEST_ASSERT_TRUE(oth_game_over(&b));
    TEST_ASSERT_EQUAL_INT(3, oth_count(&b, OTH_BLACK));
}

static void test_square_names(void) {
    char buf[3];
    TEST_ASSERT_EQUAL_INT(0, oth_square_from_name("a1"));
    TEST_ASSERT_EQUAL_INT(19, oth_square_from_name("d3"));
    TEST_ASSERT_EQUAL_INT(63, oth_square_from_name("h8"));
    TEST_ASSERT_EQUAL_INT(-1, oth_square_from_name("i1"));
    TEST_ASSERT_EQUAL_INT(-1, oth_square_from_name("a9"));
    TEST_ASSERT_EQUAL_INT(-1, oth_square_from_name("a10"));
    TEST_ASSERT_EQUAL_INT(-1, oth_square_from_name(""));
    TEST_ASSERT_EQUAL_INT(-1, oth_square_from_name(NULL));
    for (int s = 0; s < 64; s++) {
        oth_square_name(s, buf);
        TEST_ASSERT_EQUAL_INT(s, oth_square_from_name(buf));
    }
}

static void test_perft_matches_the_published_counts(void) {
    /* Leaf counts from the starting position (passes count as plies); published values. */
    static const uint64_t expected[] = { 1, 4, 12, 56, 244, 1396, 8200, 55092, 390216 };
    oth_board b;
    oth_init(&b);
    for (int depth = 0; depth <= 8; depth++) TEST_ASSERT_EQUAL_UINT64(expected[depth], oth_perft(&b, depth));
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_starting_position);
    RUN_TEST(test_a_move_flips_the_enclosed_discs);
    RUN_TEST(test_flips_follow_every_enclosing_line);
    RUN_TEST(test_illegal_moves_are_refused_and_change_nothing);
    RUN_TEST(test_moves_do_not_wrap_around_the_edges);
    RUN_TEST(test_pass_and_end_of_game);
    RUN_TEST(test_square_names);
    RUN_TEST(test_perft_matches_the_published_counts);
    return UNITY_END();
}
