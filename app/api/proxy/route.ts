import { NextRequest, NextResponse } from 'next/server';

export async function GET(req: NextRequest) {
  // Data the ENGINE EXE displays in the dashboard
  return NextResponse.json({
    cloudStatus: 'Active',
    activeUsers: 1240,
    revenue: '$12,450.00',
    serverLoad: '24%',
  });
}
