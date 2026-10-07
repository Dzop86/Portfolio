// Reading a mesh file with lib-c, the portfolio's C library (OBJ, PLY, STL).
#pragma once

#include "par/mesh.hpp"

#include <stdexcept>
#include <string>

namespace par {

class LoadError : public std::runtime_error {
public:
    using std::runtime_error::runtime_error;
};

// Throws LoadError with lib-c's message and, when it has one, the line.
[[nodiscard]] Mesh load(const std::string& path);

}  // namespace par
