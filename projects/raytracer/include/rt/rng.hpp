// Deterministic random numbers: SplitMix64, seeded per pixel and per sample so that an image does not
// depend on the order in which pixels are rendered, nor on how many threads or workers render them.
#pragma once

#include <cstdint>

namespace rt {

class Rng {
public:
    explicit constexpr Rng(uint64_t seed) : state_(seed) {}

    // The stream of pixel (x, y) for sample number `sample` of an image `width` pixels wide.
    static constexpr Rng for_sample(uint32_t x, uint32_t y, uint32_t width, uint32_t sample) {
        const uint64_t pixel = static_cast<uint64_t>(y) * width + x;
        Rng r(pixel * 0x9E3779B97F4A7C15ULL ^ (static_cast<uint64_t>(sample) + 1) * 0xD1B54A32D192ED03ULL);
        r.next();  // decorrelates neighbouring seeds
        return r;
    }

    constexpr uint64_t next() {
        uint64_t z = (state_ += 0x9E3779B97F4A7C15ULL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ULL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBULL;
        return z ^ (z >> 31);
    }

    // Uniform in [0, 1), 53 bits.
    constexpr double uniform() { return static_cast<double>(next() >> 11) * 0x1.0p-53; }

private:
    uint64_t state_;
};

}  // namespace rt
