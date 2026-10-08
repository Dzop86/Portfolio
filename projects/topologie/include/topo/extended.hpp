// Extended persistence of a height function on a mesh (Cohen-Steiner, Edelsbrunner and Harer 2009): the
// sublevel sets {f <= a} going up, then the mesh relative to its superlevel sets {f >= a} going down. Every
// class is then paired, the essential ones of ordinary persistence too: on a standing torus, a loop is born at
// the lower saddle and closed at the upper one, as the Reeb graph's loop (D50).
#pragma once

#include "topo/mesh.hpp"

#include <cstdint>
#include <vector>

namespace topo {

struct ExtendedPair {
    int dimension;          // of the class: 0 a component, 1 a loop, 2 a cavity
    uint32_t birth_vertex;  // the vertex where the class is born, going up then down
    uint32_t death_vertex;  // the vertex where it dies
    double birth;           // their heights
    double death;
};

struct ExtendedPersistence {
    // Born and dead going up: the finite pairs of topo::persistence.
    std::vector<ExtendedPair> ordinary;
    // Born going up, dead going down: the classes that never die in ordinary persistence, as many per dimension
    // as the Betti numbers. Above the diagonal (death higher than birth) or below it.
    std::vector<ExtendedPair> extended;
    // Born and dead going down (relative classes): born higher than they die.
    std::vector<ExtendedPair> relative;
};

// Lower-star filtration of `height` (ties broken by index, as topo::elevation), then the cone over the mesh, from
// an apex, built going down: the cone over a simplex enters at the height of its lowest vertex. The whole is
// reduced over Z/2 with clearing. A pair whose two simplices come from the same vertex is left out of the ordinary
// and relative ones. Each list is sorted by dimension, birth, death. Throws std::invalid_argument if `height` does
// not have one value per vertex.
[[nodiscard]] ExtendedPersistence extended_persistence(const Mesh& mesh, const std::vector<double>& height);

}  // namespace topo
