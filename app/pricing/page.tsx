import { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Pricing - SmokeScreen ENGINE",
  description: "Choose the perfect plan for your needs",
};

export default function PricingPage() {
  return (
    <main className="min-h-screen bg-[#0b0f14] text-white">
      <nav className="flex justify-between items-center p-4 border-b border-gray-800">
        <Link href="/" className="text-xl font-bold text-[#FF3D00]">
          SmokeScreen ENGINE
        </Link>
        <div className="space-x-4">
          <Link href="/demo" className="hover:text-[#FF3D00]">Demo</Link>
          <Link href="/download" className="hover:text-[#FF3D00]">Download</Link>
          <Link href="/login" className="hover:text-[#FF3D00]">Login</Link>
        </div>
      </nav>

      <div className="max-w-6xl mx-auto px-4 py-16">
        <div className="text-center mb-16">
          <h1 className="text-4xl font-bold mb-4">Simple, Transparent Pricing</h1>
          <p className="text-gray-400 text-lg">Choose the perfect plan for your needs</p>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          {/* STARTER */}
          <div className="bg-[#11161c] rounded-2xl border border-gray-800 p-8">
            <h2 className="text-2xl font-bold mb-2">STARTER</h2>
            <p className="text-gray-400 mb-4">For builders and solo developers moving fast.</p>
            <div className="mb-6">
              <span className="text-4xl font-bold">$0</span>
              <span className="text-gray-400">/mo</span>
            </div>
            <p className="text-sm text-gray-500 mb-6">Free forever. No card required.</p>
            <Link href="/login" className="block w-full bg-[#FF3D00] hover:bg-[#e03600] text-center py-3 rounded-lg font-bold transition mb-8">
              START FREE →
            </Link>
            <ul className="space-y-3 text-sm">
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 3 active projects
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 50K events/month
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 1 team member
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Community support
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Basic analytics
              </li>
              <li className="flex items-center gap-2 text-gray-600">
                <span>✕</span> Custom domains
              </li>
              <li className="flex items-center gap-2 text-gray-600">
                <span>✕</span> AI inference layer
              </li>
              <li className="flex items-center gap-2 text-gray-600">
                <span>✕</span> SLA guarantee
              </li>
            </ul>
          </div>

          {/* PRO - Most Popular */}
          <div className="bg-[#11161c] rounded-2xl border-2 border-[#FF3D00] p-8 relative">
            <div className="absolute -top-4 left-1/2 -translate-x-1/2 bg-[#FF3D00] px-4 py-1 rounded-full text-sm font-bold">
              MOST POPULAR
            </div>
            <h2 className="text-2xl font-bold mb-2">PRO</h2>
            <p className="text-gray-400 mb-4">For serious teams shipping production workloads.</p>
            <div className="mb-6">
              <span className="text-4xl font-bold">$63</span>
              <span className="text-gray-400">/mo</span>
            </div>
            <p className="text-sm text-gray-500 mb-6">Billed annually — save $192/yr</p>
            <Link href="/login" className="block w-full bg-[#FF3D00] hover:bg-[#e03600] text-center py-3 rounded-lg font-bold transition mb-8">
              GET PRO →
            </Link>
            <p className="text-xs text-gray-500 mb-4">14-day free trial. Cancel anytime.</p>
            <ul className="space-y-3 text-sm">
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Unlimited projects
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 5M events/month
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Up to 10 members
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Priority email support
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Full analytics suite
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Custom domains
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> AI inference layer
              </li>
              <li className="flex items-center gap-2 text-gray-600">
                <span>✕</span> SLA guarantee
              </li>
            </ul>
          </div>

          {/* ENTERPRISE */}
          <div className="bg-[#11161c] rounded-2xl border border-gray-800 p-8">
            <h2 className="text-2xl font-bold mb-2">ENTERPRISE</h2>
            <p className="text-gray-400 mb-4">For organizations that demand maximum control and scale.</p>
            <div className="mb-6">
              <span className="text-4xl font-bold">CUSTOM</span>
            </div>
            <p className="text-sm text-gray-500 mb-6">Volume discounts. Custom SLAs. Dedicated infra.</p>
            <a href="mailto:sales@smokescreen.engine" className="block w-full bg-[#1f2937] hover:bg-[#374151] text-center py-3 rounded-lg font-bold transition mb-8">
              CONTACT SALES →
            </a>
            <ul className="space-y-3 text-sm">
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Unlimited everything
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Unlimited events
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Unlimited members
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 24/7 dedicated support
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Advanced analytics
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> Custom domains + SSL
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> AI inference + training
              </li>
              <li className="flex items-center gap-2">
                <span className="text-green-400">✓</span> 99.99% SLA guarantee
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-12 text-center text-gray-400 text-sm">
          <p>SSO, SAML, SCIM, custom data residency, and on-prem available.</p>
        </div>
      </div>
    </main>
  );
}
