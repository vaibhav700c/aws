/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  // Amplify deployment configuration
  output: 'export',
  trailingSlash: true,
  // Disable server-side features for static export
  experimental: {
    // Required for Amplify static hosting
  }
}

export default nextConfig
