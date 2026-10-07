// Binary PPM (P6) files, for the command line and the reference image of the tests, and their comparison.
#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace rt {

struct Image {
    uint32_t width = 0, height = 0;
    std::vector<uint8_t> rgb;  // 3 bytes per pixel, rows from top to bottom
};

// From the renderer's RGBA buffer.
Image from_rgba(uint32_t width, uint32_t height, const std::vector<uint8_t>& rgba);
void write_ppm(const std::string& path, const Image& image);
// Throws std::runtime_error on a missing or malformed file.
Image read_ppm(const std::string& path);
// Peak signal-to-noise ratio in dB (infinite for identical images); the sizes must match.
double psnr(const Image& a, const Image& b);

}  // namespace rt
