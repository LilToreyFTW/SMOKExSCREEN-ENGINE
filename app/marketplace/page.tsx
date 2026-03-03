'use client';

import { useState } from 'react';
import Link from 'next/link';

interface Plan {
  id: string;
  name: string;
  price: number;
  duration: string;
  durationType: string;
  features: string[];
}

const plans: Plan[] = [
  {
    id: 'starter_1_month',
    name: 'STARTER',
    price: 0,
    duration: '1',
    durationType: '1_MONTH',
    features: [
      '3 active projects',
      '50K events/month',
      '1 team member',
      'Community support',
      'Basic analytics'
    ],
  },
  {
    id: 'starter_1_year',
    name: 'STARTER',
    price: 0,
    duration: '12',
    durationType: '1_YEAR',
    features: [
      '3 active projects',
      '50K events/month',
      '1 team member',
      'Community support',
      'Basic analytics'
    ],
  },
  {
    id: 'pro_1_month',
    name: 'PRO',
    price: 63,
    duration: '1',
    durationType: '1_MONTH',
    features: [
      'Unlimited projects',
      '5M events/month',
      'Up to 10 members',
      'Priority email support',
      'Full analytics suite',
      'Custom domains',
      'AI inference layer'
    ],
  },
  {
    id: 'pro_1_year',
    name: 'PRO',
    price: 564,
    duration: '12',
    durationType: '1_YEAR',
    features: [
      'Unlimited projects',
      '5M events/month',
      'Up to 10 members',
      'Priority email support',
      'Full analytics suite',
      'Custom domains',
      'AI inference layer'
    ],
  },
  {
    id: 'lifetime',
    name: 'LIFETIME',
    price: 999,
    duration: '0',
    durationType: 'LIFETIME',
    features: [
      'Unlimited everything',
      'Unlimited events',
      'Unlimited members',
      '24/7 dedicated support',
      'Advanced analytics',
      'Custom domains + SSL',
      'AI inference + training',
      '99.99% SLA guarantee'
    ],
  },
];

export default function MarketplacePage() {
  const [selectedPlan, setSelectedPlan] = useState<Plan | null>(null);
  const [generatedKey, setGeneratedKey] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [purchased, setPurchased] = useState(false);

  const handlePurchase = async (plan: Plan) => {
    if (plan.price > 0) {
      // For paid plans, show checkout (placeholder)
      alert(`Checkout for $${plan.price} - Payment integration coming soon!`);
      return;
    }

    // For free plans, generate key directly
    setLoading(true);
    setSelectedPlan(plan);

    try {
      // Generate a key locally for demo (in production, this would go through payment)
      const key = 'SS-' + Math.random().toString(16).slice(2, 18).toUpperCase();
      setGeneratedKey(key);
      setPurchased(true);
    } catch (error) {
      console.error('Error generating key:', error);
    } finally {
      setLoading(false);
    }
  };

  if (purchased && selectedPlan && generatedKey) {
    return (
      <main className="min-h-screen bg-[#0b0f14] text-white flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-[#11161c] rounded-2xl border border-gray-800 p-8 text-center">
          <div className="w-16 h-16 bg-green-500 rounded-full flex items-center justify-center mx-auto mb-4">
            <span className="text-3xl">✓</span>
          </div>
          <h2 className="text-2xl font-bold mb-2">Purchase Successful!</h2>
          <p className="text-gray-400 mb-6">Your {selectedPlan.name} license is ready.</p>
          
          <div className="bg-[#0d1117] border border-gray-700 rounded-lg p-4 mb-6">
            <div className="text-sm text-gray-400 mb-2">Your License Key</div>
            <div className="font-mono text-xl text-green-400">{generatedKey}</div>
          </div>
          
          <p className="text-sm text-gray-400 mb-6">
            Download SmokeScreenEngine.exe and redeem this key in Settings → License
          </p>
          
          <div className="space-y-3">
            <Link href="/download" className="block w-full bg-[#FF3D00] hover:bg-[#e03600] py-3 rounded-lg font-bold transition">
              DOWNLOAD EXE
            </Link>
            <button onClick={() => { setPurchased(false); setGeneratedKey(null); }} className="block w-full bg-[#1f2937] hover:bg-[#374151] py-3 rounded-lg font-bold transition">
              GET ANOTHER KEY
            </button>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#0b0f14] text-white">
      <nav className="flex justify-between items-center p-4 border-b border-gray-800">
        <Link href="/" className="text-xl font-bold text-[#FF3D00]">
          SmokeScreen ENGINE
        </Link>
        <div className="space-x-4">
          <Link href="/pricing" className="hover:text-[#FF3D00]">Pricing</Link>
          <Link href="/demo" className="hover:text-[#FF3D00]">Demo</Link>
          <Link href="/download" className="hover:text-[#FF3D00]">Download</Link>
          <Link href="/login" className="hover:text-[#FF3D00]">Login</Link>
        </div>
      </nav>

      <div className="max-w-6xl mx-auto px-4 py-16">
        <div className="text-center mb-16">
          <h1 className="text-4xl font-bold mb-4">Choose Your Plan</h1>
          <p className="text-gray-400 text-lg">Select a subscription tier and get your license key instantly</p>
        </div>

        {/* STARTER TIERS */}
        <div className="mb-12">
          <h2 className="text-2xl font-bold mb-6 text-[#FF3D00]">STARTER TIER OPTIONS</h2>
          <div className="grid md:grid-cols-2 gap-6">
            {plans.filter(p => p.name === 'STARTER').map((plan) => (
              <div key={plan.id} className="bg-[#11161c] rounded-2xl border border-gray-800 p-6">
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-xl font-bold">{plan.name}</h3>
                    <p className="text-gray-400 text-sm">{plan.duration === '12' ? '1 Year' : '1 Month'}</p>
                  </div>
                  <div className="text-right">
                    <span className="text-3xl font-bold">${plan.price}</span>
                    <span className="text-gray-400">/mo</span>
                  </div>
                </div>
                <ul className="space-y-2 mb-6">
                  {plan.features.map((feature, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm">
                      <span className="text-green-400">✓</span> {feature}
                  </li>
                  ))}
                </ul>
                <button 
                  onClick={() => handlePurchase(plan)}
                  disabled={loading}
                  className="w-full bg-[#FF3D00] hover:bg-[#e03600] disabled:opacity-50 py-3 rounded-lg font-bold transition"
                >
                  {loading ? 'Generating...' : plan.price === 0 ? 'START FREE' : `GET FOR $${plan.price}`}
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* PRO TIERS */}
        <div className="mb-12">
          <h2 className="text-2xl font-bold mb-6 text-[#FF3D00]">PRO TIER OPTIONS</h2>
          <div className="grid md:grid-cols-2 gap-6">
            {plans.filter(p => p.name === 'PRO').map((plan) => (
              <div key={plan.id} className="bg-[#11161c] rounded-2xl border-2 border-[#FF3D00] p-6 relative">
                <div className="absolute -top-3 left-1/2 -translate-x-1/2 bg-[#FF3D00] px-3 py-1 rounded-full text-xs font-bold">
                  POPULAR
                </div>
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-xl font-bold">{plan.name}</h3>
                    <p className="text-gray-400 text-sm">{plan.duration === '12' ? '1 Year (Save 25%)' : '1 Month'}</p>
                  </div>
                  <div className="text-right">
                    <span className="text-3xl font-bold">${plan.price}</span>
                    <span className="text-gray-400">{plan.duration === '12' ? '/yr' : '/mo'}</span>
                  </div>
                </div>
                <ul className="space-y-2 mb-6">
                  {plan.features.map((feature, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm">
                      <span className="text-green-400">✓</span> {feature}
                    </li>
                  ))}
                </ul>
                <button 
                  onClick={() => handlePurchase(plan)}
                  disabled={loading}
                  className="w-full bg-[#FF3D00] hover:bg-[#e03600] disabled:opacity-50 py-3 rounded-lg font-bold transition"
                >
                  {loading ? 'Processing...' : `GET ${plan.name} - $${plan.price}`}
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* ENTERPRISE / LIFETIME */}
        <div>
          <h2 className="text-2xl font-bold mb-6 text-[#FF3D00]">ENTERPRISE TIER OPTIONS</h2>
          <div className="grid md:grid-cols-2 gap-6">
            {plans.filter(p => p.name === 'LIFETIME').map((plan) => (
              <div key={plan.id} className="bg-[#11161c] rounded-2xl border border-gray-800 p-6">
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-xl font-bold">{plan.name}</h3>
                    <p className="text-gray-400 text-sm">One-time payment, yours forever</p>
                  </div>
                  <div className="text-right">
                    <span className="text-3xl font-bold">${plan.price}</span>
                  </div>
                </div>
                <ul className="space-y-2 mb-6">
                  {plan.features.map((feature, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm">
                      <span className="text-green-400">✓</span> {feature}
                    </li>
                  ))}
                </ul>
                <button 
                  onClick={() => handlePurchase(plan)}
                  disabled={loading}
                  className="w-full bg-[#FF3D00] hover:bg-[#e03600] disabled:opacity-50 py-3 rounded-lg font-bold transition"
                >
                  {loading ? 'Processing...' : `GET LIFETIME - $${plan.price}`}
                </button>
              </div>
            ))}
            <div className="bg-[#11161c] rounded-2xl border border-gray-800 p-6">
              <h3 className="text-xl font-bold mb-2">Custom Enterprise</h3>
              <p className="text-gray-400 text-sm mb-4">Need a custom solution? We got you.</p>
              <ul className="space-y-2 mb-6 text-sm text-gray-400">
                <li>• Volume discounts</li>
                <li>• Custom SLAs</li>
                <li>• Dedicated infrastructure</li>
                <li>• On-premise deployment</li>
              </ul>
              <a href="mailto:sales@smokescreen.engine" className="block w-full bg-[#1f2937] hover:bg-[#374151] text-center py-3 rounded-lg font-bold transition">
                CONTACT SALES
              </a>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
