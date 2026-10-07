#include "picking.h"

#include <cmath>
#include <limits>

namespace picking {

Ray rayThrough(const Camera& camera, QPointF pixel, QSize viewport)
{
    const float aspect = static_cast<float>(viewport.width()) / static_cast<float>(std::max(viewport.height(), 1));
    const QMatrix4x4 inverse = (camera.projection(aspect) * camera.view()).inverted();
    // Normalised device coordinates: y grows upwards there, downwards in the widget.
    const auto x = static_cast<float>(2 * pixel.x() / viewport.width() - 1);
    const auto y = static_cast<float>(1 - 2 * pixel.y() / viewport.height());
    const QVector3D near = inverse.map(QVector3D(x, y, -1));
    const QVector3D far = inverse.map(QVector3D(x, y, 1));
    return {near, (far - near).normalized()};
}

std::optional<Hit> intersect(const MeshModel& model, const Ray& ray)
{
    const auto& p = model.positions();
    const auto& triangles = model.mesh().triangles();
    std::optional<Hit> best;
    constexpr float epsilon = 1e-9f;
    for (std::size_t f = 0; f < triangles.size(); ++f) {
        const QVector3D a = p[triangles[f][0]];
        const QVector3D e1 = p[triangles[f][1]] - a;
        const QVector3D e2 = p[triangles[f][2]] - a;
        const QVector3D h = QVector3D::crossProduct(ray.direction, e2);
        const float det = QVector3D::dotProduct(e1, h);
        if (std::abs(det) < epsilon)
            continue; // the ray runs along the triangle's plane
        const float inv = 1.0f / det;
        const QVector3D s = ray.origin - a;
        const float u = inv * QVector3D::dotProduct(s, h);
        if (u < 0 || u > 1)
            continue;
        const QVector3D q = QVector3D::crossProduct(s, e1);
        const float v = inv * QVector3D::dotProduct(ray.direction, q);
        if (v < 0 || u + v > 1)
            continue;
        const float t = inv * QVector3D::dotProduct(e2, q);
        if (t > 0 && (!best || t < best->distance))
            best = Hit{static_cast<uint32_t>(f), t, ray.origin + t * ray.direction};
    }
    return best;
}

QPointF project(const Camera& camera, const QVector3D& point, QSize viewport)
{
    const float aspect = static_cast<float>(viewport.width()) / static_cast<float>(std::max(viewport.height(), 1));
    const QVector3D ndc = (camera.projection(aspect) * camera.view()).map(point);
    return {(ndc.x() + 1.0) / 2 * viewport.width(), (1.0 - ndc.y()) / 2 * viewport.height()};
}

Selection select(const MeshModel& model, const Camera& camera, QPointF pixel, QSize viewport, double radius)
{
    const std::optional<Hit> hit = intersect(model, rayThrough(camera, pixel, viewport));
    if (!hit)
        return {};
    Selection selection{Selection::Kind::Face, hit->face};
    double nearest = radius;
    for (uint32_t v : model.mesh().triangles()[hit->face]) {
        const QPointF d = project(camera, model.positions()[v], viewport) - pixel;
        const double distance = std::hypot(d.x(), d.y());
        if (distance <= nearest) {
            nearest = distance;
            selection = {Selection::Kind::Vertex, v};
        }
    }
    return selection;
}

}  // namespace picking
