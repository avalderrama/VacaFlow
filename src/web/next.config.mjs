/** @type {import('next').NextConfig} */
const nextConfig = {
  // Single origin, first-party cookie, zero CORS (ADR-009). The API's real
  // dev port, from src/BigSolutions.VacaFlow.Api/Properties/launchSettings.json.
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5217/api/:path*',
      },
    ];
  },
};

export default nextConfig;
