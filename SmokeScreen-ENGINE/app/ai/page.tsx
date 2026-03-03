'use client';

import { useState, useEffect } from 'react';
import Link from "next/link";

export default function AIControlPage() {
  const [notifications, setNotifications] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 10000);
    return () => clearInterval(interval);
  }, []);

  const fetchNotifications = async () => {
    try {
      const res = await fetch('/api/ai/broadcast');
      if (res.ok) {
        const data = await res.json();
        setNotifications(data);
      }
    } catch (error) {
      console.error('Failed to fetch notifications:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-[#0b0f14] text-white p-8">
      <div className="max-w-6xl mx-auto">
        <nav className="flex justify-between items-center mb-12">
          <Link href="/" className="text-2xl font-bold text-[#FF3D00]">
            SmokeScreen ENGINE
          </Link>
          <div className="space-x-6">
            <Link href="/pricing" className="hover:text-[#FF3D00]">Pricing</Link>
            <Link href="/marketplace" className="hover:text-[#FF3D00]">Marketplace</Link>
            <Link href="/demo" className="hover:text-[#FF3D00]">Demo</Link>
            <Link href="/download" className="hover:text-[#FF3D00]">Download</Link>
          </div>
        </nav>

        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold mb-4">🧠 SmokeScreen ENGINE AI</h1>
          <p className="text-gray-400 text-lg">
            Unified AI Control Center - Connecting all systems
          </p>
        </div>

        {/* System Status Cards */}
        <div className="grid md:grid-cols-3 gap-6 mb-12">
          <div className="bg-[#11161c] p-6 rounded-2xl border border-gray-800">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
              <h3 className="text-xl font-bold">SmokeScreenEngine.exe</h3>
            </div>
            <p className="text-gray-400 text-sm mb-4">Desktop Application</p>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Status</span>
                <span className="text-green-400">● Online</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">AI Sync</span>
                <span>Active</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Discord Bot</span>
                <span className="text-green-400">Connected</span>
              </div>
            </div>
          </div>

          <div className="bg-[#11161c] p-6 rounded-2xl border border-gray-800">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
              <h3 className="text-xl font-bold">ENGINE.exe</h3>
            </div>
            <p className="text-gray-400 text-sm mb-4">Key Generator</p>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Status</span>
                <span className="text-green-400">● Online</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Keys Generated</span>
                <span>4,001</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Discord Bot</span>
                <span className="text-green-400">Connected</span>
              </div>
            </div>
          </div>

          <div className="bg-[#11161c] p-6 rounded-2xl border border-gray-800">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
              <h3 className="text-xl font-bold">Website</h3>
            </div>
            <p className="text-gray-400 text-sm mb-4">Vercel Deployment</p>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Status</span>
                <span className="text-green-400">● Online</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Users Online</span>
                <span>1</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">API</span>
                <span className="text-green-400">Active</span>
              </div>
            </div>
          </div>
        </div>

        {/* Discord Integration */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 mb-12">
          <h2 className="text-2xl font-bold mb-6">📡 Discord Integration</h2>
          <div className="grid md:grid-cols-2 gap-6">
            <div>
              <h3 className="font-bold mb-3">Connected Channels</h3>
              <ul className="space-y-2 text-gray-400">
                <li>🔔 #general-logs</li>
                <li>📢 #announcements</li>
                <li>🔑 #keys-in-stock</li>
                <li>💬 #chat</li>
              </ul>
            </div>
            <div>
              <h3 className="font-bold mb-3">Notification Types</h3>
              <ul className="space-y-2 text-gray-400">
                <li>🎫 Key Redemptions</li>
                <li>👤 User Logins</li>
                <li>💰 Purchases</li>
                <li>🔑 Key Generation</li>
                <li>❌ Errors</li>
              </ul>
            </div>
          </div>
        </div>

        {/* Activity Feed */}
        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-2xl font-bold">📊 Recent Activity</h2>
            <button 
              onClick={fetchNotifications}
              className="bg-[#FF3D00] hover:bg-[#e03600] px-4 py-2 rounded font-bold text-sm"
            >
              🔄 Refresh
            </button>
          </div>
          
          {loading ? (
            <p className="text-gray-400">Loading...</p>
          ) : notifications.length > 0 ? (
            <div className="space-y-3 max-h-96 overflow-y-auto">
              {notifications.map((notif, i) => (
                <div key={i} className="flex items-center gap-3 p-3 bg-[#0d1117] rounded">
                  <span className={notif.type === 'error' ? 'text-red-400' : notif.type === 'success' ? 'text-green-400' : 'text-blue-400'}>
                    {notif.type === 'error' ? '❌' : notif.type === 'success' ? '✓' : 'ℹ️'}
                  </span>
                  <span className="text-gray-400">
                    <strong className="text-white">{notif.title}</strong> - {notif.message}
                  </span>
                  <span className="text-gray-600 text-sm ml-auto">
                    {notif.source?.toUpperCase()}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="space-y-3">
              <div className="flex items-center gap-3 p-3 bg-[#0d1117] rounded">
                <span className="text-green-400">✓</span>
                <span className="text-gray-400">SmokeScreenEngine.exe connected to AI</span>
                <span className="text-gray-600 text-sm ml-auto">Just now</span>
              </div>
              <div className="flex items-center gap-3 p-3 bg-[#0d1117] rounded">
                <span className="text-green-400">✓</span>
                <span className="text-gray-400">ENGINE.exe synced keys (4,001 available)</span>
                <span className="text-gray-600 text-sm ml-auto">1 min ago</span>
              </div>
              <div className="flex items-center gap-3 p-3 bg-[#0d1117] rounded">
                <span className="text-green-400">✓</span>
                <span className="text-gray-400">Website deployed and online</span>
                <span className="text-gray-600 text-sm ml-auto">5 min ago</span>
              </div>
              <div className="flex items-center gap-3 p-3 bg-[#0d1117] rounded">
                <span className="text-green-400">✓</span>
                <span className="text-gray-400">Discord bot authenticated</span>
                <span className="text-gray-600 text-sm ml-auto">10 min ago</span>
              </div>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
