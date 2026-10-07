// Draws a MeshModel with OpenGL 3.3 core: shaded faces, then edges on top. Used by the window's
// widget and, on an off-screen surface, by the tests and the --screenshot option.
#pragma once

#include "camera.h"
#include "meshmodel.h"

#include <QColor>
#include <QOpenGLBuffer>
#include <QOpenGLExtraFunctions>
#include <QOpenGLShaderProgram>
#include <QOpenGLVertexArrayObject>
#include <QSurfaceFormat>

#include <memory>

struct RenderOptions {
    bool wireframe = true;   // every edge, thin and dark
    bool curvature = false;  // faces coloured by Gaussian curvature instead of a flat grey
    bool highlight = true;   // boundary edges in pistachio, non-manifold edges in red
};

class Renderer : protected QOpenGLExtraFunctions {
public:
    static const QColor kBackground;
    static const QColor kSurface;
    static const QColor kWire;
    static const QColor kBoundary;
    static const QColor kNonManifold;

    // OpenGL 3.3 core with a depth buffer and 4x multisampling: to set before creating the application.
    [[nodiscard]] static QSurfaceFormat surfaceFormat();

    Renderer() = default;
    Renderer(const Renderer&) = delete;
    Renderer& operator=(const Renderer&) = delete;
    ~Renderer();

    // With the context current. False when the shaders do not compile (see error()).
    bool initialize();
    // With the context current. Replaces the buffers with those of the model (nullptr clears them).
    void upload(const MeshModel* model);
    // With the context current, into the bound framebuffer of the given size in pixels.
    void render(const Camera& camera, const RenderOptions& options, int width, int height);

    [[nodiscard]] QString error() const { return error_; }
    [[nodiscard]] QString glVersion() const { return version_; }

private:
    struct Lines {
        QOpenGLVertexArrayObject vao;
        QOpenGLBuffer indices{QOpenGLBuffer::IndexBuffer};
        int count = 0;
    };
    void uploadLines(Lines& lines, const std::vector<Edge>& edges);
    void drawLines(Lines& lines, const QColor& colour);

    std::unique_ptr<QOpenGLShaderProgram> surface_;
    std::unique_ptr<QOpenGLShaderProgram> lines_;
    QOpenGLVertexArrayObject faces_vao_;
    QOpenGLBuffer vertices_{QOpenGLBuffer::VertexBuffer};
    QOpenGLBuffer faces_{QOpenGLBuffer::IndexBuffer};
    int face_indices_ = 0;
    Lines wire_;
    Lines boundary_;
    Lines non_manifold_;
    QMatrix4x4 mvp_;
    QString error_;
    QString version_;
    bool ready_ = false;
};
