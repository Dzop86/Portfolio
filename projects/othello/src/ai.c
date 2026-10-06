/* Negamax search with alpha-beta pruning. A finished game is worth the disc difference times WIN, more
 * than any heuristic value; otherwise the evaluation weighs squares (corners good, the squares next to
 * them bad while the corner is empty), mobility and corners. */
#include "othello/othello.h"

#include <stddef.h>

#define WIN 10000
#define INF 1000000
#define CORNERS 0x8100000000000081ULL

/* Value of each square, the same seen from any side of the board. */
static const int WEIGHTS[64] = {
    100, -20, 10,  5,  5, 10, -20, 100,
    -20, -50, -2, -2, -2, -2, -50, -20,
     10,  -2,  1,  1,  1,  1,  -2,  10,
      5,  -2,  1,  0,  0,  1,  -2,   5,
      5,  -2,  1,  0,  0,  1,  -2,   5,
     10,  -2,  1,  1,  1,  1,  -2,  10,
    -20, -50, -2, -2, -2, -2, -50, -20,
    100, -20, 10,  5,  5, 10, -20, 100,
};

static int lowest_square(uint64_t m) {
    int sq = 0;
    while (!((m >> sq) & 1)) sq++;
    return sq;
}

static int positional(uint64_t discs) {
    int score = 0;
    while (discs) {
        int sq = lowest_square(discs);
        discs &= discs - 1;
        score += WEIGHTS[sq];
    }
    return score;
}

int oth_evaluate(const oth_board *b) {
    int me = b->to_move, them = 1 - me;
    oth_board other = *b;
    other.to_move = them;
    int my_moves = oth_popcount(oth_moves(b)), their_moves = oth_popcount(oth_moves(&other));
    int corners = oth_popcount(b->discs[me] & CORNERS) - oth_popcount(b->discs[them] & CORNERS);
    return positional(b->discs[me]) - positional(b->discs[them]) + 5 * (my_moves - their_moves) + 50 * corners;
}

static int final_value(const oth_board *b) {
    return WIN * (oth_count(b, b->to_move) - oth_count(b, 1 - b->to_move));
}

static int negamax(const oth_board *b, int depth, int alpha, int beta, int prune, uint64_t *nodes) {
    if (nodes) (*nodes)++;
    uint64_t moves = oth_moves(b);
    if (!moves) {
        oth_board next = *b;
        next.to_move = 1 - b->to_move;
        if (!oth_moves(&next)) return final_value(b);
        if (depth == 0) return oth_evaluate(b);
        return -negamax(&next, depth - 1, -beta, -alpha, prune, nodes); /* forced pass */
    }
    if (depth == 0) return oth_evaluate(b);
    int best = -INF;
    /* Corners first: good moves early make the pruning cut more. */
    uint64_t order[2] = { moves & CORNERS, moves & ~CORNERS };
    for (int k = 0; k < 2; k++) {
        uint64_t m = order[k];
        while (m) {
            int sq = lowest_square(m);
            m &= m - 1;
            oth_board next = *b;
            oth_play(&next, sq);
            int v = -negamax(&next, depth - 1, -beta, -alpha, prune, nodes);
            if (v > best) best = v;
            if (prune) {
                if (v > alpha) alpha = v;
                if (alpha >= beta) return best;
            }
        }
    }
    return best;
}

int oth_search(const oth_board *b, int depth, int alphabeta, uint64_t *nodes) {
    return negamax(b, depth, -INF, INF, alphabeta, nodes);
}

int oth_best_move(const oth_board *b, int depth, int *value) {
    uint64_t moves = oth_moves(b);
    if (!moves) {
        if (value) *value = oth_search(b, depth, 1, NULL);
        return -1;
    }
    if (depth < 1) depth = 1;
    int best = -1, best_value = -INF;
    /* Every root move is searched with a full window, so ties are broken by square, not by order. */
    for (uint64_t m = moves; m; m &= m - 1) {
        int sq = lowest_square(m);
        oth_board next = *b;
        oth_play(&next, sq);
        int v = -negamax(&next, depth - 1, -INF, INF, 1, NULL);
        if (v > best_value) {
            best_value = v;
            best = sq;
        }
    }
    if (value) *value = best_value;
    return best;
}
