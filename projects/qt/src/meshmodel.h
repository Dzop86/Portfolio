// A mesh ready to be shown: read by lib-c, analysed by topologie, plus what the display needs
// (single-precision positions, vertex normals, edge lists, curvature colours). No OpenGL here.
#pragma once

#include "topo/curvature.hpp"
#include "topo/invariants.hpp"
#include "topo/mesh.hpp"

#include <QColor>
#include <QString>
#include <QVector3D>

#include <array>
#include <cstdint>
#include <vector>

struct Bounds {
    QVector3D min;
    QVector3D max;
    [[nodiscard]] QVector3D center() const { return (min + max) / 2.0f; }
    // Radius of the sphere around the box: the camera frames this sphere.
    [[nodiscard]] float radius() const { return (max - min).length() / 2.0f; }
};

using Edge = std::array<uint32_t, 2>;

class MeshModel {
public:
    // Throws topo::LoadError when lib-c cannot read the file, std::invalid_argument on a bad mesh.
    [[nodiscard]] static MeshModel fromFile(const QString& path);
    // Same from bytes (the samples built into the application). Throws like fromFile.
    [[nodiscard]] static MeshModel fromData(const QByteArray& data);

    explicit MeshModel(topo::Mesh mesh);

    [[nodiscard]] const topo::Mesh& mesh() const { return mesh_; }
    [[nodiscard]] const topo::Invariants& invariants() const { return invariants_; }
    [[nodiscard]] const topo::GaussianCurvature& curvature() const { return curvature_; }

    [[nodiscard]] const std::vector<QVector3D>& positions() const { return positions_; }
    // Per vertex, the area-weighted mean of the face normals, of unit length (zero for an isolated vertex).
    [[nodiscard]] const std::vector<QVector3D>& normals() const { return normals_; }
    // Each edge once, as a sorted vertex pair.
    [[nodiscard]] const std::vector<Edge>& edges() const { return edges_; }
    // Edges used by a single face.
    [[nodiscard]] const std::vector<Edge>& boundaryEdges() const { return boundary_edges_; }
    // Edges shared by three faces or more.
    [[nodiscard]] const std::vector<Edge>& nonManifoldEdges() const { return mesh_.non_manifold_edges(); }
    [[nodiscard]] const Bounds& bounds() const { return bounds_; }
    // Number of edges at each vertex.
    [[nodiscard]] const std::vector<uint32_t>& valence() const { return valence_; }
    // Area and unit normal of a face (its vertices in the file's order).
    [[nodiscard]] double faceArea(uint32_t face) const;
    [[nodiscard]] QVector3D faceNormal(uint32_t face) const;

    // The colour scale runs from -scale to +scale: the 95th percentile of |K| over the interior
    // vertices, so that a few sharp vertices do not wash out the rest.
    [[nodiscard]] double curvatureScale() const { return curvature_scale_; }
    // Diverging scale: blue for negative K (saddle), near-white for flat, red for positive (dome).
    [[nodiscard]] static QColor curvatureColor(double k, double scale);
    // Sum of the angle defects over 2 pi: equal to the Euler characteristic (discrete Gauss-Bonnet).
    [[nodiscard]] double gaussBonnetRatio() const;

private:
    topo::Mesh mesh_;
    topo::Invariants invariants_;
    topo::GaussianCurvature curvature_;
    std::vector<QVector3D> positions_;
    std::vector<QVector3D> normals_;
    std::vector<Edge> edges_;
    std::vector<Edge> boundary_edges_;
    std::vector<uint32_t> valence_;
    Bounds bounds_;
    double curvature_scale_ = 1.0;
};
