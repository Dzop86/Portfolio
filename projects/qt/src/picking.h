// Which vertex or face lies under a pixel: a ray from the eye through the pixel, intersected with
// every triangle on the CPU (Möller–Trumbore). No OpenGL here.
#pragma once

#include "camera.h"
#include "meshmodel.h"

#include <QPointF>
#include <QSize>
#include <QVector3D>

#include <cstdint>
#include <optional>

struct Ray {
    QVector3D origin;
    QVector3D direction; // unit length
};

struct Hit {
    uint32_t face = 0;
    float distance = 0; // along the ray
    QVector3D point;
};

struct Selection {
    enum class Kind { None, Vertex, Face };
    Kind kind = Kind::None;
    uint32_t index = 0;
    friend bool operator==(const Selection&, const Selection&) = default;
};

namespace picking {

// The ray through a pixel of a viewport (origin at the top left, as in Qt's widgets).
[[nodiscard]] Ray rayThrough(const Camera& camera, QPointF pixel, QSize viewport);

// The nearest triangle the ray crosses, either side facing; nothing when it misses the mesh.
[[nodiscard]] std::optional<Hit> intersect(const MeshModel& model, const Ray& ray);

// Where a world point falls on the viewport, in pixels.
[[nodiscard]] QPointF project(const Camera& camera, const QVector3D& point, QSize viewport);

// The vertex of the hit face nearest to the pixel when it lies within `radius` pixels of it,
// otherwise the face itself; nothing when the ray misses.
[[nodiscard]] Selection select(const MeshModel& model, const Camera& camera, QPointF pixel, QSize viewport, double radius = 8);

}  // namespace picking
