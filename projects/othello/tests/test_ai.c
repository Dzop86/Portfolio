/* Unit tests of the AI: alpha-beta gives the value of the plain minimax, the evaluation is symmetric,
 * the chosen move is legal and deterministic, a free corner is taken, and the AI beats a random player. */
#include "othello/othello.h"
#include "unity.h"

void setUp(void) {}
void tearDown(void) {}

/* Small deterministic generator (LCG), so the tests are reproducible on every system. */
static uint32_t seed = 12345;
static uint32_t next_random(void) {
    seed = seed * 1664525u + 1013904223u;
    return seed >> 8;
}

static int random_move(const oth_board *b) {
    uint64_t moves = oth_moves(b);
    int n = oth_popcount(moves), k = (int)(next_random() % (uint32_t)n);
    for (int sq = 0; sq < 64; sq++) {
        if ((moves >> sq) & 1) {
            if (k-- == 0) return sq;
        }
    }
    return -1;
}

/* A position reached by `plies` random moves from the start (passes played when forced). */
static oth_board random_position(int plies) {
    oth_board b;
    oth_init(&b);
    for (int i = 0; i < plies && !oth_game_over(&b); i++) {
        if (!oth_moves(&b)) oth_pass(&b);
        else oth_play(&b, random_move(&b));
    }
    return b;
}

static void test_alpha_beta_gives_the_minimax_value_and_visits_fewer_nodes(void) {
    uint64_t pruned_total = 0, full_total = 0;
    for (int i = 0; i < 40; i++) {
        oth_board b = random_position(4 + i);
        for (int depth = 1; depth <= 4; depth++) {
            uint64_t pruned = 0, full = 0;
            int with = oth_search(&b, depth, 1, &pruned), without = oth_search(&b, depth, 0, &full);
            TEST_ASSERT_EQUAL_INT_MESSAGE(without, with, "alpha-beta changed the value");
            pruned_total += pruned;
            full_total += full;
        }
    }
    TEST_ASSERT_TRUE_MESSAGE(pruned_total < full_total / 2, "alpha-beta should at least halve the search");
}

static void test_the_best_move_is_legal_and_deterministic(void) {
    for (int i = 0; i < 20; i++) {
        oth_board b = random_position(i * 2);
        if (oth_game_over(&b) || !oth_moves(&b)) continue;
        int a = oth_best_move(&b, 3, NULL), c = oth_best_move(&b, 3, NULL);
        TEST_ASSERT_EQUAL_INT(a, c);
        TEST_ASSERT_TRUE((oth_moves(&b) >> a) & 1);
    }
}

static void test_one_ply_ahead_a_free_corner_is_taken(void) {
    /* Black can play a1 (enclosing b2 against c3) or e3 (enclosing e4 against e5). One ply ahead, the
     * evaluation alone decides and the corner wins. Deeper, the AI may rightly play e3 first: white must
     * then pass and cannot take a1, which black still gets on the next move. */
    oth_board b = { { 0, 0 }, OTH_BLACK };
    b.discs[OTH_BLACK] = (1ULL << oth_square_from_name("c3")) | (1ULL << oth_square_from_name("e5"));
    b.discs[OTH_WHITE] = (1ULL << oth_square_from_name("b2")) | (1ULL << oth_square_from_name("e4"));
    TEST_ASSERT_TRUE((oth_moves(&b) >> oth_square_from_name("a1")) & 1);
    TEST_ASSERT_EQUAL_INT(oth_square_from_name("a1"), oth_best_move(&b, 1, NULL));
}

static void test_the_evaluation_is_symmetric(void) {
    for (int i = 0; i < 20; i++) {
        oth_board b = random_position(i + 3), swapped = b;
        swapped.discs[OTH_BLACK] = b.discs[OTH_WHITE];
        swapped.discs[OTH_WHITE] = b.discs[OTH_BLACK];
        swapped.to_move = 1 - b.to_move;
        TEST_ASSERT_EQUAL_INT(oth_evaluate(&b), oth_evaluate(&swapped));
    }
}

static void test_must_pass_returns_no_move(void) {
    oth_board b = { { 0, 0 }, OTH_WHITE };
    b.discs[OTH_BLACK] = 1ULL << oth_square_from_name("a1");
    b.discs[OTH_WHITE] = 1ULL << oth_square_from_name("b1");
    TEST_ASSERT_EQUAL_INT(-1, oth_best_move(&b, 3, NULL));
}

static int play_game(int ai_colour, int depth) {
    oth_board b;
    oth_init(&b);
    while (!oth_game_over(&b)) {
        if (!oth_moves(&b)) {
            oth_pass(&b);
            continue;
        }
        oth_play(&b, b.to_move == ai_colour ? oth_best_move(&b, depth, NULL) : random_move(&b));
    }
    return oth_count(&b, ai_colour) - oth_count(&b, 1 - ai_colour);
}

static void test_the_ai_beats_a_random_player(void) {
    int wins = 0;
    for (int game = 0; game < 20; game++) {
        if (play_game(game % 2, 3) > 0) wins++;
    }
    TEST_ASSERT_GREATER_OR_EQUAL_INT_MESSAGE(18, wins, "depth 3 should win at least 18 games out of 20");
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_alpha_beta_gives_the_minimax_value_and_visits_fewer_nodes);
    RUN_TEST(test_the_best_move_is_legal_and_deterministic);
    RUN_TEST(test_one_ply_ahead_a_free_corner_is_taken);
    RUN_TEST(test_the_evaluation_is_symmetric);
    RUN_TEST(test_must_pass_returns_no_move);
    RUN_TEST(test_the_ai_beats_a_random_player);
    return UNITY_END();
}
