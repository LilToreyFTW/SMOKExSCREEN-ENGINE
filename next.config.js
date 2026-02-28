/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  experimental: {
    serverComponentsExternalPackages: ['@clerk/nextjs'],
  },
  // Disable src directory for build
  ...(process.env.NODE_ENV === 'production' ? {} : { srcDir: 'src' }),
};

module.exports = nextConfig;
