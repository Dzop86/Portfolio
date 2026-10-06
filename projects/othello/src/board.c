#include "othello/othello.h"

#include <stddef.h>

/* Masks that stop a shift from wrapping from one edge of the board to the other. */
#define NOT_A 0xfefefefefefefefeULL /* every column but a (cleared after a shift towards h) */
#define NOT_H 0x7f7f7f7f7f7f7f7fULL /* every column but h (cleared after a shift towards a) */

/* One step in each of the eight directions: down (+8), up (-8), right (+1), left (-1) and diagonals. */
static uint64_t step(uint64_t m, int dir) {
    switch (dir) {
    case 0: return m << 8;
    case 1: return m >> 8;
    case 2: return (m << 1) & NOT_A;
    case 3: return (m >> 1) & NOT_H;
    case 4: return (m << 9) & NOT_A;
    case 5: return (m << 7) & NOT_H;
    case 6: return (m >> 7) & NOT_A;
    default: return (m >> 9) & NOT_H;
    }
}

void oth_init(oth_board *b) {
    b->discs[OTH_WHITE] = (1ULL << 27) | (1ULL << 36); /* d4, e5 */
    b->discs[OTH_BLACK] = (1ULL << 28) | (1ULL << 35); /* e4, d5 */
    b->to_move = OTH_BLACK;
}

uint64_t oth_moves(const oth_board *b) {
    uint64_t own = b->discs[b->to_move], other = b->discs[1 - b->to_move];
    uint64_t empty = ~(own | other), moves = 0;
    for (int dir = 0; dir < 8; dir++) {
        /* Runs of opponent discs starting next to one of ours: at most 6 long on an 8x8 board. */
        uint64_t run = step(own, dir) & other;
        for (int k = 0; k < 5; k++) run |= step(run, dir) & other;
        moves |= step(run, dir) & empty;
    }
    return moves;
}

uint64_t oth_flips(const oth_board *b, int square) {
    if (square < 0 || square > 63) return 0;
    uint64_t own = b->discs[b->to_move], other = b->discs[1 - b->to_move];
    uint64_t at = 1ULL << square, flips = 0;
    if ((own | other) & at) return 0;
    for (int dir = 0; dir < 8; dir++) {
        uint64_t line = 0, x = step(at, dir);
        while (x & other) {
            line |= x;
            x = step(x, dir);
        }
        if (x & own) flips |= line;
    }
    return flips;
}

int oth_play(oth_board *b, int square) {
    uint64_t flips = oth_flips(b, square);
    if (!flips) return -1;
    int me = b->to_move;
    b->discs[me] |= flips | (1ULL << square);
    b->discs[1 - me] &= ~flips;
    b->to_move = 1 - me;
    return 0;
}

int oth_pass(oth_board *b) {
    if (oth_moves(b)) return -1;
    b->to_move = 1 - b->to_move;
    return 0;
}

int oth_game_over(const oth_board *b) {
    if (oth_moves(b)) return 0;
    oth_board other = *b;
    other.to_move = 1 - b->to_move;
    return oth_moves(&other) == 0;
}

int oth_popcount(uint64_t m) {
    int n = 0;
    while (m) {
        m &= m - 1;
        n++;
    }
    return n;
}

int oth_count(const oth_board *b, int colour) {
    return oth_popcount(b->discs[colour]);
}

int oth_square_from_name(const char *name) {
    if (!name || name[0] < 'a' || name[0] > 'h' || name[1] < '1' || name[1] > '8' || name[2] != '\0') return -1;
    return (name[1] - '1') * 8 + (name[0] - 'a');
}

void oth_square_name(int square, char *buf) {
    buf[0] = (char)('a' + square % 8);
    buf[1] = (char)('1' + square / 8);
    buf[2] = '\0';
}

uint64_t oth_perft(const oth_board *b, int depth) {
    if (depth == 0) return 1;
    uint64_t moves = oth_moves(b);
    if (!moves) {
        if (oth_game_over(b)) return 1;
        oth_board next = *b;
        oth_pass(&next);
        return oth_perft(&next, depth - 1);
    }
    uint64_t total = 0;
    while (moves) {
        uint64_t bit = moves & (~moves + 1);
        moves ^= bit;
        int sq = 0;
        while (!((bit >> sq) & 1)) sq++;
        oth_board next = *b;
        oth_play(&next, sq);
        total += oth_perft(&next, depth - 1);
    }
    return total;
}
