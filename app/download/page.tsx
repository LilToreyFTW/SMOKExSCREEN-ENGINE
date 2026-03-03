import Link from "next/link";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Download - SmokeScreen ENGINE",
  description: "Download SmokeScreen ENGINE Desktop Application",
};

export default function DownloadPage() {
  return (
    <main className="min-h-screen bg-[#0b0f14] text-white p-8">
      <div className="max-w-4xl mx-auto">
        <nav className="flex justify-between items-center mb-12">
          <Link href="/" className="text-2xl font-bold text-[#FF3D00]">
            SmokeScreen ENGINE
          </Link>
          <div className="space-x-6">
            <Link href="/pricing" className="hover:text-[#FF3D00]">Pricing</Link>
            <Link href="/marketplace" className="hover:text-[#FF3D00]">Marketplace</Link>
            <Link href="/demo" className="hover:text-[#FF3D00]">Demo</Link>
            <Link href="/login" className="hover:text-[#FF3D00]">Login</Link>
          </div>
        </nav>

        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold mb-4">Download SmokeScreen ENGINE</h1>
          <p className="text-gray-400 text-lg">
            Get the latest version of SmokeScreen ENGINE Desktop Application
          </p>
        </div>

        {/* Main App Download */}
        <div className="bg-[#11161c] p-8 rounded-2xl border-2 border-[#FF3D00] mb-8">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-2xl font-bold mb-2">🎮 SmokeScreenEngine.exe</h2>
              <p className="text-gray-400">Main Application • Windows x64</p>
            </div>
            <a
              href="/SmokeScreenEngine.exe"
              download
              className="bg-[#FF3D00] hover:bg-[#e03600] px-6 py-3 rounded-lg font-bold text-lg transition-colors"
            >
              Download Now
            </a>
          </div>
        </div>

        {/* Key Generator Download */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 mb-8">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-2xl font-bold mb-2">🔑 ENGINE.exe</h2>
              <p className="text-gray-400">License Key Generator • For Admins</p>
            </div>
            <a
              href="/ENGINE.exe"
              download
              className="bg-[#1f2937] hover:bg-[#374151] px-6 py-3 rounded-lg font-bold text-lg transition-colors"
            >
              Download Now
            </a>
          </div>
        </div>

        {/* System Requirements */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 mb-8">
          <h3 className="font-bold mb-4 text-xl">⚠️ Required: .NET 8.0 Runtime</h3>
          <p className="text-gray-400 mb-4">
            SmokeScreen ENGINE requires Microsoft .NET 8.0 Desktop Runtime to run.
          </p>
          <a
            href="https://dotnet.microsoft.com/download/dotnet/8.0"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-block bg-[#512BD4] hover:bgurple-600 px-6 py-3 rounded-lg font-bold text-lg transition-colors"
          >
            Download .NET 8.0 Runtime
          </a>
          <p className="text-gray-500 text-sm mt-3">
            Select "Desktop Runtime 8.0.x" for Windows x64
          </p>
        </div>

        {/* Quick Start Guide */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 mb-8">
          <h3 className="font-bold mb-4 text-xl">🚀 Quick Start Guide</h3>
          <ol className="text-gray-300 space-y-3 list-decimal list-inside">
            <li>Download and install <strong>.NET 8.0 Desktop Runtime</strong></li>
            <li>Download <strong>SmokeScreenEngine.exe</strong></li>
            <li>Run SmokeScreenEngine.exe</li>
            <li>Sign in with Discord or redeem a license key</li>
            <li>Enjoy your features!</li>
          </ol>
        </div>

        {/* Features */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 mb-8">
          <h3 className="font-bold mb-4 text-xl">✨ Features</h3>
          <div className="grid md:grid-cols-2 gap-4 text-gray-300">
            <ul className="space-y-2">
              <li>✓ Hardware-locked security</li>
              <li>✓ Real-time cloud analytics</li>
              <li>✓ License key management</li>
              <li>✓ Discord integration</li>
              <li>✓ Marketplace access</li>
            </ul>
            <ul className="space-y-2">
              <li>✓ AI Inference Layer</li>
              <li>✓ Global CDN (42 nodes)</li>
              <li>✓ API Gateway</li>
              <li>✓ DevOps deployments</li>
              <li>✓ Workflow automation</li>
            </ul>
          </div>
        </div>

        <div className="text-center text-gray-500 text-sm">
          <p>Version 4.2.0 • Built with .NET 8.0</p>
        </div>
      </div>
    </main>
  );
}
