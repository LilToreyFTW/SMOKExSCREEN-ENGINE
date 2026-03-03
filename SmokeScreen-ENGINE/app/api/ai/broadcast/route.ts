import { NextRequest, NextResponse } from "next/server";

const notifications: Array<{
  title: string;
  message: string;
  source: string;
  type: string;
  timestamp: string;
  userId: string;
}> = [];

export async function POST(req: NextRequest) {
  try {
    const notification = await req.json();
    
    notifications.unshift({
      ...notification,
      timestamp: notification.timestamp || new Date().toISOString()
    });
    
    if (notifications.length > 100) {
      notifications.pop();
    }
    
    return NextResponse.json({ success: true });
  } catch (error) {
    return NextResponse.json({ error: "Failed to process notification" }, { status: 500 });
  }
}

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url);
  const source = searchParams.get("source");
  const limit = parseInt(searchParams.get("limit") || "50");
  
  let filtered = notifications;
  if (source) {
    filtered = notifications.filter(n => n.source === source);
  }
  
  return NextResponse.json(filtered.slice(0, limit));
}
