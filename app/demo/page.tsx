import { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Demo - SmokeScreen ENGINE",
  description: "Interactive demo of SmokeScreen ENGINE Desktop Application",
};

export default function DemoPage() {
  return (
    <main className="min-h-screen bg-[#0b0f14] text-white">
      <nav className="flex justify-between items-center p-4 border-b border-gray-800">
        <Link href="/" className="text-xl font-bold text-[#FF3D00]">
          SmokeScreen ENGINE
        </Link>
        <div className="space-x-4">
          <Link href="/download" className="hover:text-[#FF3D00]">Download</Link>
          <Link href="/login" className="hover:text-[#FF3D00]">Login</Link>
        </div>
      </nav>

      <div className="flex justify-center p-6">
        <div className="w-full max-w-5xl">
          <div className="bg-[#1a1a1a] rounded-t-lg border border-gray-700 p-2 flex items-center gap-2">
            <div className="flex gap-1.5">
              <div className="w-3 h-3 rounded-full bg-red-500"></div>
              <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
              <div className="w-3 h-3 rounded-full bg-green-500"></div>
            </div>
            <div className="flex-1 text-center text-gray-400 text-sm">SmokeScreenEngine.exe</div>
          </div>
          
          <div className="bg-[#0d1117] border-x border-b border-gray-700 min-h-[600px] p-6">
            <div className="grid grid-cols-4 gap-4 h-full">
              {/* Sidebar */}
              <div className="col-span-1 space-y-2">
                <div className="bg-[#161b22] p-3 rounded-lg border border-gray-800">
                  <div className="flex items-center gap-2 mb-3">
                    <div className="w-8 h-8 bg-[#FF3D00] rounded flex items-center justify-center font-bold">S</div>
                    <div>
                      <div className="text-sm font-bold">SmokeScreen</div>
                      <div className="text-xs text-green-400">● Online</div>
                    </div>
                  </div>
                  <div className="text-xs text-gray-400">License: LIFETIME</div>
                  <div className="text-xs text-gray-400">Expires: Never</div>
                </div>
                
                <div className="space-y-1">
                  {['Dashboard', 'Keys', 'Settings', 'Marketplace', 'Cloud'].map((item, i) => (
                    <div key={item} className={`p-2 rounded cursor-pointer text-sm ${i === 0 ? 'bg-[#FF3D00] text-white' : 'hover:bg-[#161b22] text-gray-300'}`}>
                      {item}
                    </div>
                  ))}
                </div>
              </div>
              
              {/* Main Content */}
              <div className="col-span-3 space-y-4">
                {/* Status Cards */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                    <div className="text-2xl font-bold text-[#FF3D00]">4,001</div>
                    <div className="text-xs text-gray-400">Keys Available</div>
                  </div>
                  <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                    <div className="text-2xl font-bold text-green-400">0</div>
                    <div className="text-xs text-gray-400">Keys Redeemed</div>
                  </div>
                  <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                    <div className="text-2xl font-bold text-blue-400">Connected</div>
                    <div className="text-xs text-gray-400">Cloud Status</div>
                  </div>
                </div>
                
                {/* Generate Keys Section */}
                <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                  <h3 className="font-bold mb-3">Generate Keys</h3>
                  <div className="grid grid-cols-4 gap-2 mb-3">
                    {['1 Day', '7 Days', '1 Month', 'Lifetime'].map((dur) => (
                      <button key={dur} className="bg-[#0d1117] hover:bg-[#1f2937] border border-gray-700 py-2 px-3 rounded text-sm transition">
                        {dur}
                      </button>
                    ))}
                  </div>
                  <div className="flex gap-2">
                    <input type="number" placeholder="Quantity" defaultValue="100" className="bg-[#0d1117] border border-gray-700 rounded px-3 py-2 text-sm w-24" />
                    <button className="bg-[#FF3D00] hover:bg-[#e03600] px-4 py-2 rounded text-sm font-bold transition">
                      Generate
                    </button>
                  </div>
                </div>
                
                {/* Keys Table */}
                <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                  <h3 className="font-bold mb-3">Recent Keys</h3>
                  <div className="space-y-2">
                    {[1,2,3].map((i) => (
                      <div key={i} className="flex justify-between items-center bg-[#0d1117] p-2 rounded border border-gray-800 text-sm">
                        <span className="font-mono text-green-400">SS-{Math.random().toString(16).slice(2,18).toUpperCase()}</span>
                        <span className="text-gray-400">1 Month</span>
                        <span className="text-gray-500 text-xs">Just now</span>
                      </div>
                    ))}
                  </div>
                </div>
                
                {/* Activity */}
                <div className="bg-[#161b22] p-4 rounded-lg border border-gray-800">
                  <h3 className="font-bold mb-3">System Activity</h3>
                  <div className="space-y-2 text-sm text-gray-400">
                    <div className="flex gap-2">
                      <span className="text-green-400">✓</span>
                      <span>Connected to license server</span>
                    </div>
                    <div className="flex gap-2">
                      <span className="text-green-400">✓</span>
                      <span>Database synced (4,001 keys)</span>
                    </div>
                    <div className="flex gap-2">
                      <span className="text-green-400">✓</span>
                      <span>Discord authentication ready</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <div className="text-center pb-6">
        <Link href="/download" className="inline-block bg-[#FF3D00] hover:bg-[#e03600] px-6 py-3 rounded-lg font-bold transition">
          Download Now
        </Link>
      </div>
    </main>
  );
}
