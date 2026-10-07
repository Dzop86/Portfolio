// Orbit camera: turns around a target point, at a distance, looking at it. No OpenGL here.
#pragma once

#include "meshmodel.h"

#include <QMatrix4x4>
#include <QVector3D>

class Camera {
public:
    static constexpr float kFieldOfView = 45.0f; // vertical, in degrees
    static constexpr float kMaxPitch = 89.0f;

    // Looks at the whole bounding sphere from a three-quarter view.
    void fit(const Bounds& bounds);
    // Turns around the target, in degrees; the pitch stays within +-89 so the view never flips.
    void orbit(float yawDegrees, float pitchDegrees);
    // Multiplies the distance (below 1 comes closer), kept between 1/20 and 50 times the radius.
    void zoom(float factor);
    // Moves the target in the view plane; dx and dy are fractions of the visible height.
    void pan(float dx, float dy);

    [[nodiscard]] QVector3D eye() const;
    [[nodiscard]] QVector3D target() const { return target_; }
    [[nodiscard]] float distance() const { return distance_; }
    [[nodiscard]] float yaw() const { return yaw_; }
    [[nodiscard]] float pitch() const { return pitch_; }
    [[nodiscard]] QMatrix4x4 view() const;
    [[nodiscard]] QMatrix4x4 projection(float aspect) const;

private:
    QVector3D target_;
    float radius_ = 1.0f;
    float distance_ = 3.0f;
    float yaw_ = 35.0f;
    float pitch_ = 25.0f;
};
