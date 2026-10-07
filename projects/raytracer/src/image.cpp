#include "rt/image.hpp"

#include <cmath>
#include <fstream>
#include <limits>
#include <stdexcept>

namespace rt {

Image from_rgba(uint32_t width, uint32_t height, const std::vector<uint8_t>& rgba) {
    Image img{width, height, {}};
    img.rgb.reserve(static_cast<std::size_t>(width) * height * 3);
    for (std::size_t i = 0; i + 3 < rgba.size(); i += 4) img.rgb.insert(img.rgb.end(), {rgba[i], rgba[i + 1], rgba[i + 2]});
    return img;
}

void write_ppm(const std::string& path, const Image& image) {
    std::ofstream out(path, std::ios::binary);
    out << "P6\n" << image.width << ' ' << image.height << "\n255\n";
    out.write(reinterpret_cast<const char*>(image.rgb.data()), static_cast<std::streamsize>(image.rgb.size()));
    if (!out) throw std::runtime_error("cannot write " + path);
}

Image read_ppm(const std::string& path) {
    std::ifstream in(path, std::ios::binary);
    std::string magic;
    Image img;
    unsigned max = 0;
    if (!(in >> magic >> img.width >> img.height >> max) || magic != "P6" || max != 255 || img.width == 0 ||
        img.height == 0 || img.width > 16384 || img.height > 16384) {
        throw std::runtime_error("not a binary 8-bit PPM: " + path);
    }
    in.get();  // the single whitespace after the header
    img.rgb.resize(static_cast<std::size_t>(img.width) * img.height * 3);
    in.read(reinterpret_cast<char*>(img.rgb.data()), static_cast<std::streamsize>(img.rgb.size()));
    if (!in) throw std::runtime_error("truncated PPM: " + path);
    return img;
}

double psnr(const Image& a, const Image& b) {
    if (a.width != b.width || a.height != b.height) throw std::invalid_argument("images of different sizes");
    double se = 0;
    for (std::size_t i = 0; i < a.rgb.size(); ++i) {
        const double d = static_cast<double>(a.rgb[i]) - b.rgb[i];
        se += d * d;
    }
    if (se == 0) return std::numeric_limits<double>::infinity();
    const double mse = se / static_cast<double>(a.rgb.size());
    return 10 * std::log10(255.0 * 255.0 / mse);
}

}  // namespace rt
