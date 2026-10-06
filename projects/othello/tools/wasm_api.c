/* C API of the engine for the WebAssembly build (scripts/build-wasm.sh): one game kept here, with the
 * positions before each move so the page can undo. Squares are 0 (a1) to 63 (h8). */
#include <stdint.h>

#include "othello/othello.h"

#define HISTORY 128

static oth_board game, history[HISTORY];
static int plies;

void othjs_reset(void) {
    oth_init(&game);
    plies = 0;
}

/* 0 empty, 1 black, 2 white. */
int othjs_cell(int square) {
    if (square < 0 || square > 63) return 0;
    uint64_t bit = 1ULL << square;
    return (game.discs[OTH_BLACK] & bit) ? 1 : (game.discs[OTH_WHITE] & bit) ? 2 : 0;
}

int othjs_is_legal(int square) {
    return square >= 0 && square < 64 && ((oth_moves(&game) >> square) & 1);
}

int othjs_to_move(void) { return game.to_move; }
int othjs_must_pass(void) { return oth_moves(&game) == 0 && !oth_game_over(&game); }
int othjs_game_over(void) { return oth_game_over(&game); }
int othjs_count(int colour) { return oth_count(&game, colour); }
int othjs_plies(void) { return plies; }

static int remember(void) {
    if (plies >= HISTORY) return -1;
    history[plies++] = game;
    return 0;
}

/* Plays a square for the side to move; 0, or -1 if illegal. */
int othjs_play(int square) {
    if (!othjs_is_legal(square) || remember() != 0) return -1;
    return oth_play(&game, square);
}

/* Passes when forced; 0, or -1 if a move exists. */
int othjs_pass(void) {
    if (oth_moves(&game) || remember() != 0) return -1;
    return oth_pass(&game);
}

/* Takes back the last ply; 0, or -1 at the start. */
int othjs_undo(void) {
    if (plies == 0) return -1;
    game = history[--plies];
    return 0;
}

/* The AI's choice for the side to move, without playing it; -1 if it must pass. */
int othjs_ai_move(int depth) { return oth_best_move(&game, depth, 0); }

/* Perft from the starting position, as a double (JavaScript numbers): for the tests. */
double othjs_perft(int depth) {
    oth_board start;
    oth_init(&start);
    return (double)oth_perft(&start, depth);
}
