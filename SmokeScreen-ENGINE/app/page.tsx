export default function Home() {
  return (
    <main className="min-h-screen bg-[#0b0f14] text-white p-12">
      <div className="max-w-6xl mx-auto">
        <nav className="flex justify-between items-center mb-16">
          <h1 className="text-2xl font-bold text-[#1f6feb]">SmokeScreen ENGINE</h1>
          <div className="space-x-6">
            <a href="#" className="hover:text-[#1f6feb]">Docs</a>
            <a href="#" className="bg-[#1f6feb] px-4 py-2 rounded">Download EXE</a>
          </div>
        </nav>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-12 items-center">
          <div>
            <h2 className="text-6xl font-extrabold mb-6">Control your <span className="text-[#1f6feb]">SaaS</span> from the Desktop.</h2>
            <p className="text-gray-400 text-xl mb-8">Elite infrastructure management, hardware-locked security, and real-time cloud analytics.</p>
          </div>
          <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800 shadow-2xl">
             <div className="flex items-center space-x-2 mb-4">
                <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
                <span className="text-sm font-mono text-green-500">SYSTEMS ONLINE</span>
             </div>
             <div className="space-y-4">
                <div className="h-4 bg-gray-800 rounded w-3/4"></div>
                <div className="h-4 bg-gray-800 rounded w-1/2"></div>
                <div className="h-24 bg-[#0b0f14] rounded border border-gray-800"></div>
             </div>
          </div>
        </div>
      </div>
    </main>
  );
}