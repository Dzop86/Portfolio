#include "meshmodel.h"

#include <QFile>

#include <algorithm>
#include <cmath>
#include <numbers>
#include <stdexcept>
#include <string_view>

MeshModel MeshModel::fromFile(const QString& path)
{
    // Read through Qt so that any path works on every system (lib-c's fopen takes narrow strings).
    QFile file(path);
    if (!file.open(QIODevice::ReadOnly))
        throw topo::LoadError("cannot open " + path.toStdString() + ": " + file.errorString().toStdString(), 1, 0);
    return fromData(file.readAll());
}

MeshModel MeshModel::fromData(const QByteArray& data)
{
    return MeshModel(topo::Mesh::parse(std::string_view(data.constData(), static_cast<std::size_t>(data.size()))));
}

MeshModel::MeshModel(topo::Mesh mesh)
    : mesh_(std::move(mesh)), invariants_(topo::analyze(mesh_)), curvature_(topo::gaussian_curvature(mesh_))
{
    const auto& points = mesh_.positions();
    positions_.reserve(points.size());
    for (const topo::Vec3& p : points)
        positions_.emplace_back(static_cast<float>(p.x), static_cast<float>(p.y), static_cast<float>(p.z));

    if (!positions_.empty()) {
        bounds_ = {positions_.front(), positions_.front()};
        for (const QVector3D& p : positions_) {
            bounds_.min = QVector3D(std::min(bounds_.min.x(), p.x()), std::min(bounds_.min.y(), p.y()), std::min(bounds_.min.z(), p.z()));
            bounds_.max = QVector3D(std::max(bounds_.max.x(), p.x()), std::max(bounds_.max.y(), p.y()), std::max(bounds_.max.z(), p.z()));
        }
    }

    // The cross product of two sides is twice the face's area along its normal: summing it weights
    // each face by its area. Each face normal is first turned towards the vertex's running sum: on a
    // non-orientable or badly oriented mesh, opposite normals would otherwise cancel out (a dark seam
    // across a Möbius strip). The lighting is two-sided, so the side does not matter.
    normals_.assign(positions_.size(), QVector3D());
    std::vector<Edge> all;
    all.reserve(mesh_.triangles().size() * 3);
    for (const topo::Triangle& t : mesh_.triangles()) {
        const QVector3D n = QVector3D::crossProduct(positions_[t[1]] - positions_[t[0]], positions_[t[2]] - positions_[t[0]]);
        for (int i = 0; i < 3; ++i) {
            QVector3D& sum = normals_[t[static_cast<std::size_t>(i)]];
            sum += QVector3D::dotProduct(sum, n) < 0 ? -n : n;
            const uint32_t a = t[static_cast<std::size_t>(i)];
            const uint32_t b = t[static_cast<std::size_t>((i + 1) % 3)];
            all.push_back({std::min(a, b), std::max(a, b)});
        }
    }
    for (QVector3D& n : normals_)
        n.normalize();

    // Sorted copies of each edge: a run of one is a boundary edge.
    std::sort(all.begin(), all.end());
    for (std::size_t i = 0; i < all.size();) {
        std::size_t j = i;
        while (j < all.size() && all[j] == all[i])
            ++j;
        edges_.push_back(all[i]);
        if (j - i == 1)
            boundary_edges_.push_back(all[i]);
        i = j;
    }

    valence_.assign(positions_.size(), 0);
    for (const Edge& e : edges_) {
        ++valence_[e[0]];
        ++valence_[e[1]];
    }

    std::vector<double> magnitudes;
    for (std::size_t v = 0; v < curvature_.gaussian.size(); ++v)
        if (!curvature_.boundary[v] && curvature_.area[v] > 0)
            magnitudes.push_back(std::abs(curvature_.gaussian[v]));
    if (!magnitudes.empty()) {
        const auto k = static_cast<std::ptrdiff_t>(static_cast<double>(magnitudes.size() - 1) * 0.95);
        std::nth_element(magnitudes.begin(), magnitudes.begin() + k, magnitudes.end());
        if (magnitudes[static_cast<std::size_t>(k)] > 0)
            curvature_scale_ = magnitudes[static_cast<std::size_t>(k)];
    }
}

QColor MeshModel::curvatureColor(double k, double scale)
{
    // ColorBrewer RdBu ends and its light middle: readable by most colour-blind viewers.
    const QColor negative(33, 102, 172);
    const QColor flat(240, 240, 240);
    const QColor positive(178, 24, 43);
    const double t = scale > 0 ? std::clamp(k / scale, -1.0, 1.0) : 0.0;
    const QColor& end = t < 0 ? negative : positive;
    const double a = std::abs(t);
    auto mix = [a](int from, int to) { return static_cast<int>(std::lround(from + (to - from) * a)); };
    return {mix(flat.red(), end.red()), mix(flat.green(), end.green()), mix(flat.blue(), end.blue())};
}

double MeshModel::gaussBonnetRatio() const
{
    return curvature_.total / (2 * std::numbers::pi);
}

double MeshModel::faceArea(uint32_t face) const
{
    const topo::Triangle& t = mesh_.triangles().at(face);
    const auto& p = mesh_.positions();
    const double ux = p[t[1]].x - p[t[0]].x, uy = p[t[1]].y - p[t[0]].y, uz = p[t[1]].z - p[t[0]].z;
    const double vx = p[t[2]].x - p[t[0]].x, vy = p[t[2]].y - p[t[0]].y, vz = p[t[2]].z - p[t[0]].z;
    return std::hypot(uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx) / 2;
}

QVector3D MeshModel::faceNormal(uint32_t face) const
{
    const topo::Triangle& t = mesh_.triangles().at(face);
    return QVector3D::crossProduct(positions_[t[1]] - positions_[t[0]], positions_[t[2]] - positions_[t[0]]).normalized();
}
