#include "renderer.h"

#include <array>

namespace {

// Two-sided headlight: the light comes from the eye, so both sides of a face are lit (a Möbius
// strip has only one side, and some meshes are inconsistently oriented).
constexpr const char* kSurfaceVertex = R"(#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec3 curvature;
uniform mat4 mvp;
uniform mat4 view;
uniform bool useCurvature;
uniform vec3 baseColour;
out vec3 vNormal;
out vec3 vColour;
void main() {
    gl_Position = mvp * vec4(position, 1.0);
    vNormal = mat3(view) * normal;
    vColour = useCurvature ? curvature : baseColour;
}
)";

constexpr const char* kSurfaceFragment = R"(#version 330 core
in vec3 vNormal;
in vec3 vColour;
out vec4 colour;
void main() {
    float light = length(vNormal) > 0.0 ? abs(normalize(vNormal).z) : 1.0;
    colour = vec4(vColour * (0.35 + 0.65 * light), 1.0);
}
)";

constexpr const char* kLinesVertex = R"(#version 330 core
layout(location = 0) in vec3 position;
uniform mat4 mvp;
void main() { gl_Position = mvp * vec4(position, 1.0); }
)";

constexpr const char* kLinesFragment = R"(#version 330 core
uniform vec3 lineColour;
out vec4 colour;
void main() { colour = vec4(lineColour, 1.0); }
)";

QVector3D rgb(const QColor& c) { return {c.redF(), c.greenF(), c.blueF()}; }

}  // namespace

// The portfolio's colours: VS Code grey background, pistachio for the boundary.
const QColor Renderer::kBackground(0x1f, 0x1f, 0x1f);
const QColor Renderer::kSurface(0xb8, 0xb8, 0xb8);
const QColor Renderer::kWire(0x2a, 0x2a, 0x2a);
const QColor Renderer::kBoundary(0xbe, 0xf3, 0x74);
const QColor Renderer::kNonManifold(0xff, 0x5c, 0x5c);

QSurfaceFormat Renderer::surfaceFormat()
{
    QSurfaceFormat format;
    format.setRenderableType(QSurfaceFormat::OpenGL);
    format.setVersion(3, 3);
    format.setProfile(QSurfaceFormat::CoreProfile);
    format.setDepthBufferSize(24);
    format.setSamples(4);
    return format;
}

Renderer::~Renderer() = default;

bool Renderer::initialize()
{
    initializeOpenGLFunctions();
    version_ = QString::fromLatin1(reinterpret_cast<const char*>(glGetString(GL_VERSION)));
    surface_ = std::make_unique<QOpenGLShaderProgram>();
    lines_ = std::make_unique<QOpenGLShaderProgram>();
    const bool ok = surface_->addShaderFromSourceCode(QOpenGLShader::Vertex, kSurfaceVertex)
        && surface_->addShaderFromSourceCode(QOpenGLShader::Fragment, kSurfaceFragment) && surface_->link()
        && lines_->addShaderFromSourceCode(QOpenGLShader::Vertex, kLinesVertex)
        && lines_->addShaderFromSourceCode(QOpenGLShader::Fragment, kLinesFragment) && lines_->link();
    if (!ok) {
        error_ = surface_->log() + lines_->log();
        return false;
    }
    faces_vao_.create();
    vertices_.create();
    faces_.create();
    for (Lines* l : {&wire_, &boundary_, &non_manifold_}) {
        l->vao.create();
        l->indices.create();
    }
    ready_ = true;
    return true;
}

void Renderer::upload(const MeshModel* model)
{
    if (!ready_)
        return;
    face_indices_ = 0;
    wire_.count = boundary_.count = non_manifold_.count = 0;
    if (model == nullptr)
        return;

    // Interleaved: position, normal, curvature colour (9 floats per vertex).
    const auto& positions = model->positions();
    const auto& normals = model->normals();
    const auto& k = model->curvature().gaussian;
    std::vector<float> data;
    data.reserve(positions.size() * 9);
    for (std::size_t v = 0; v < positions.size(); ++v) {
        const QColor c = MeshModel::curvatureColor(k[v], model->curvatureScale());
        for (float f : {positions[v].x(), positions[v].y(), positions[v].z(), normals[v].x(), normals[v].y(), normals[v].z(),
                        c.redF(), c.greenF(), c.blueF()})
            data.push_back(f);
    }
    std::vector<uint32_t> indices;
    indices.reserve(model->mesh().triangles().size() * 3);
    for (const topo::Triangle& t : model->mesh().triangles())
        indices.insert(indices.end(), t.begin(), t.end());
    face_indices_ = static_cast<int>(indices.size());

    faces_vao_.bind();
    vertices_.bind();
    vertices_.allocate(data.data(), static_cast<int>(data.size() * sizeof(float)));
    faces_.bind();
    faces_.allocate(indices.data(), static_cast<int>(indices.size() * sizeof(uint32_t)));
    constexpr int stride = 9 * sizeof(float);
    for (GLuint attribute = 0; attribute < 3; ++attribute) {
        glEnableVertexAttribArray(attribute);
        glVertexAttribPointer(attribute, 3, GL_FLOAT, GL_FALSE, stride, reinterpret_cast<const void*>(static_cast<std::uintptr_t>(attribute * 3 * sizeof(float))));
    }
    faces_vao_.release();

    uploadLines(wire_, model->edges());
    uploadLines(boundary_, model->boundaryEdges());
    uploadLines(non_manifold_, model->nonManifoldEdges());
}

void Renderer::uploadLines(Lines& lines, const std::vector<Edge>& edges)
{
    std::vector<uint32_t> indices;
    indices.reserve(edges.size() * 2);
    for (const Edge& e : edges)
        indices.insert(indices.end(), e.begin(), e.end());
    lines.count = static_cast<int>(indices.size());
    lines.vao.bind();
    vertices_.bind();
    lines.indices.bind();
    lines.indices.allocate(indices.data(), static_cast<int>(indices.size() * sizeof(uint32_t)));
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 9 * sizeof(float), nullptr);
    lines.vao.release();
}

void Renderer::render(const Camera& camera, const RenderOptions& options, int width, int height)
{
    glViewport(0, 0, width, height);
    glClearColor(kBackground.redF(), kBackground.greenF(), kBackground.blueF(), 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    if (!ready_ || face_indices_ == 0)
        return;
    glEnable(GL_DEPTH_TEST);
    glDepthFunc(GL_LEQUAL);
    mvp_ = camera.projection(static_cast<float>(width) / static_cast<float>(std::max(height, 1))) * camera.view();

    // Faces pushed slightly back so that the edges drawn on them are not hidden by them.
    glEnable(GL_POLYGON_OFFSET_FILL);
    glPolygonOffset(1.0f, 1.0f);
    surface_->bind();
    surface_->setUniformValue("mvp", mvp_);
    surface_->setUniformValue("view", camera.view());
    surface_->setUniformValue("useCurvature", options.curvature);
    surface_->setUniformValue("baseColour", rgb(kSurface));
    faces_vao_.bind();
    glDrawElements(GL_TRIANGLES, face_indices_, GL_UNSIGNED_INT, nullptr);
    faces_vao_.release();
    surface_->release();
    glDisable(GL_POLYGON_OFFSET_FILL);

    if (options.wireframe)
        drawLines(wire_, kWire);
    if (options.highlight) {
        drawLines(boundary_, kBoundary);
        drawLines(non_manifold_, kNonManifold);
    }
}

void Renderer::drawLines(Lines& lines, const QColor& colour)
{
    if (lines.count == 0)
        return;
    lines_->bind();
    lines_->setUniformValue("mvp", mvp_);
    lines_->setUniformValue("lineColour", rgb(colour));
    lines.vao.bind();
    glDrawElements(GL_LINES, lines.count, GL_UNSIGNED_INT, nullptr);
    lines.vao.release();
    lines_->release();
}
