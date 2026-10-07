#include "camera.h"

#include <QtMath>

#include <algorithm>
#include <cmath>

void Camera::fit(const Bounds& bounds)
{
    target_ = bounds.center();
    radius_ = std::max(bounds.radius(), 1e-6f);
    // The sphere fits the vertical field of view, with a margin.
    distance_ = 1.15f * radius_ / std::sin(qDegreesToRadians(kFieldOfView / 2));
    yaw_ = 35.0f;
    pitch_ = 25.0f;
}

void Camera::orbit(float yawDegrees, float pitchDegrees)
{
    yaw_ = std::fmod(yaw_ + yawDegrees, 360.0f);
    pitch_ = std::clamp(pitch_ + pitchDegrees, -kMaxPitch, kMaxPitch);
}

void Camera::zoom(float factor)
{
    distance_ = std::clamp(distance_ * factor, radius_ / 20, radius_ * 50);
}

void Camera::pan(float dx, float dy)
{
    const QMatrix4x4 v = view();
    // Rows of the view matrix: the camera's right and up axes in world space.
    const QVector3D right(v(0, 0), v(0, 1), v(0, 2));
    const QVector3D up(v(1, 0), v(1, 1), v(1, 2));
    const float height = 2 * distance_ * std::tan(qDegreesToRadians(kFieldOfView / 2));
    target_ += (-dx * right + dy * up) * height;
}

QVector3D Camera::eye() const
{
    const float yaw = qDegreesToRadians(yaw_);
    const float pitch = qDegreesToRadians(pitch_);
    const QVector3D direction(std::cos(pitch) * std::sin(yaw), std::sin(pitch), std::cos(pitch) * std::cos(yaw));
    return target_ + distance_ * direction;
}

QMatrix4x4 Camera::view() const
{
    QMatrix4x4 m;
    m.lookAt(eye(), target_, QVector3D(0, 1, 0));
    return m;
}

QMatrix4x4 Camera::projection(float aspect) const
{
    QMatrix4x4 m;
    // Near and far planes follow the distance: depth precision stays good at every zoom.
    m.perspective(kFieldOfView, aspect, std::max(distance_ - 2 * radius_, distance_ / 1000), distance_ + 2 * radius_);
    return m;
}
