/* C API of the parser for the WebAssembly build (scripts/build-web.sh): one parse at a time, the
 * tree kept as an S-expression string until the next parse. */
#include <stdlib.h>

#include "maille.h"

static maille_result result;
static char *tree;

/* Parses `len` bytes at `source`; returns a maille_status (0 on success). */
int maillejs_parse(const char *source, int len) {
    free(tree);
    tree = NULL;
    maille_status status = maille_parse(source, (size_t)len, &result);
    if (status == MAILLE_OK) {
        tree = node_to_string(result.tree);
        node_free(result.tree);
        result.tree = NULL;
        if (!tree) return MAILLE_OUT_OF_MEMORY;
    }
    return status;
}

const char *maillejs_tree(void) { return tree ? tree : ""; }
int maillejs_line(void) { return result.line; }
int maillejs_column(void) { return result.column; }
const char *maillejs_message(void) { return result.message; }
