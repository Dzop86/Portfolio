/* Othello in the terminal.
 *   othello                      you (black) against the AI (white, depth 4)
 *   othello --black ai:3 --white ai:5    two AIs; --black human / --white human for people
 * Moves are typed as a1 to h8; a forced pass is played automatically. Reads moves from standard input,
 * so a game can be scripted: printf 'd3\nc5\n' | othello --white human. Exit code 0 at the end of a game,
 * 1 if the input ends first, 2 on a bad argument. */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "othello/othello.h"

typedef struct {
    int ai;    /* 0 for a person */
    int depth; /* search depth of an AI */
} player;

static int parse_player(const char *arg, player *p) {
    if (strcmp(arg, "human") == 0) {
        p->ai = 0;
        return 0;
    }
    if (strncmp(arg, "ai:", 3) == 0) {
        int d = atoi(arg + 3);
        if (d < 1 || d > 10) return -1;
        p->ai = 1;
        p->depth = d;
        return 0;
    }
    return -1;
}

static void print_board(const oth_board *b) {
    uint64_t moves = oth_moves(b);
    printf("  a b c d e f g h\n");
    for (int row = 0; row < 8; row++) {
        printf("%d", row + 1);
        for (int col = 0; col < 8; col++) {
            int sq = row * 8 + col;
            uint64_t bit = 1ULL << sq;
            char c = (b->discs[OTH_BLACK] & bit) ? 'X' : (b->discs[OTH_WHITE] & bit) ? 'O' : (moves & bit) ? '.' : ' ';
            printf(" %c", c == ' ' ? '-' : c);
        }
        printf("\n");
    }
    printf("X %d  O %d\n", oth_count(b, OTH_BLACK), oth_count(b, OTH_WHITE));
}

int main(int argc, char **argv) {
    player players[2] = { { 0, 0 }, { 1, 4 } };
    for (int i = 1; i < argc; i++) {
        int side = strcmp(argv[i], "--black") == 0 ? OTH_BLACK : strcmp(argv[i], "--white") == 0 ? OTH_WHITE : -1;
        if (side < 0 || i + 1 >= argc || parse_player(argv[++i], &players[side]) != 0) {
            fprintf(stderr, "usage: othello [--black human|ai:N] [--white human|ai:N]  (N from 1 to 10)\n");
            return 2;
        }
    }
    oth_board b;
    oth_init(&b);
    char name[3], line[64];
    while (!oth_game_over(&b)) {
        const char *who = b.to_move == OTH_BLACK ? "X" : "O";
        if (!oth_moves(&b)) {
            printf("%s passes\n", who);
            oth_pass(&b);
            continue;
        }
        player p = players[b.to_move];
        int sq;
        if (p.ai) {
            sq = oth_best_move(&b, p.depth, NULL);
        } else {
            print_board(&b);
            printf("%s to play: ", who);
            fflush(stdout);
            if (!fgets(line, sizeof line, stdin)) {
                printf("\ninput ended before the end of the game\n");
                return 1;
            }
            line[strcspn(line, "\r\n")] = '\0';
            sq = oth_square_from_name(line);
            if (sq < 0 || !oth_flips(&b, sq)) {
                printf("illegal move: %s\n", line);
                continue;
            }
        }
        oth_square_name(sq, name);
        printf("%s plays %s\n", who, name);
        oth_play(&b, sq);
    }
    print_board(&b);
    int x = oth_count(&b, OTH_BLACK), o = oth_count(&b, OTH_WHITE);
    printf("game over: X %d, O %d, %s\n", x, o, x > o ? "X wins" : o > x ? "O wins" : "draw");
    return 0;
}
