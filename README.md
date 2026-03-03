# 🎮 SmokeScreen ENGINE - Complete System Documentation

## 📋 Table of Contents

1. [🔑 Overview](#-overview)
2. [🏗️ Architecture](#️-architecture)
3. [📦 Components](#-components)
4. [🎯 Recoil Key System](#-recoil-key-system)
5. [🤖 Discord Bot Integration](#-discord-bot-integration)
6. [💾 Database & Storage](#-database--storage)
7. [🌐 Website Integration](#-website-integration)
8. [🔧 Installation & Setup](#-installation--setup)
9. [📚 API Documentation](#-api-documentation)
10. [💰 Payment System](#-payment-system)
11. [🚀 Deployment](#-deployment)

---

## 🔑 Overview

SmokeScreen ENGINE is a comprehensive gaming enhancement platform featuring **Recoil V2** technology for multiple supported games. The system consists of two main applications:

### **🎯 Core Features**
- **Recoil V2 Control** - Advanced anti-recoil patterns for competitive advantage
- **Key-Based Access** - Subscription system for individual game modules
- **Discord Integration** - Automated key management and user validation
- **Admin Dashboard** - Complete key generation and analytics
- **Multi-Game Support** - Rainbow Six Siege, COD Warzone, Arc Raiders, Fortnite

### **📱 Applications**
- **ENGINE.exe** - Admin tool for key generation and system management
- **SmokeScreen-Engine.exe** - User client with game-specific recoil controls

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                   🌐 Website Store                      │
│                     │    │                              │
│                     ▼    ▼                              │
│              ┌───────────────┐    ┌───────────────┐   │
│              │   Neon DB     │    │   Discord Bot │   │
│              │               │    │               │   │
│              ▼               ▼    ▼               ▼   │
│    ┌────────────────┐   ┌─────────────────┐   ┌───────────────┐│
│    │   ENGINE.exe  │   │  SmokeScreen-  │   │   Discord     ││
│    │   (Admin)    │   │  Engine.exe   │   │   Server     ││
│    │               │   │  (User)      │   │               ││
│    ▼               ▼    ▼               ▼   │
│  Key Generation   │  Tab Access     │  Key Redemption   │
│  & Management    │  Control         │  & Validation    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📦 Components

### **🔧 ENGINE.exe (Admin Tool)**
- **📊 Analytics Dashboard** - Real-time system monitoring
- **🎮 RECOIL KEY SUBSCRIPTIONS GENERATOR** - Individual game key generation
- **🔑 LICENSE Management** - Main license key operations
- **👤 ACCOUNT Management** - User authentication
- **⚙️ ENGINE Controls** - System configuration
- **🛠️ TOOLS** - Administrative utilities

### **🎮 SmokeScreen-Engine.exe (User Client)**
- **🔑 LICENSE Tab** - Key redemption and validation
- **🎮 Game Tabs** - Individual recoil controls per game
- **🔄 AUTO UPDATER** - Automatic system updates
- **📊 API SERVICE STATUS** - System health monitoring
- **👤 ACCOUNT Tab** - User profile and settings

### **🎮 PS5 Controller System (JoyShockLibrary)**
- **RT trigger** – Recoil activation on aim
- **6 advanced recoil patterns** – Game-specific compensation
- **Gyro aiming** – Motion sensor assistance
- **Adaptive triggers & haptics** – Tactile feedback
- **PS5ControllerManager.cs** – DualSense session management
- **PS5RecoilIntegration.cs** – Recoil compensation; **PS5RecoilConfig.cs** – Configuration UI

### **📡 Wireless Receiver & Live User Dashboard**
- **WirelessReceiver.cs** – Session management and tracking
- **LiveUserDashboard.cs** – Real-time updates (IP, computer name, online status)
- **Heartbeat** – Active session monitoring

---

## 🎯 Recoil Key System

### **🔑 Key Types & Formats**

| Game | Key Format | Duration Options | Pricing |
|------|-------------|----------------|---------|
| 🎮 WARZONE | `CODW-XXXXXXXXX` | 1/6/12/Lifetime months | $9.99/$35.99/$65.99/$149.99 |
| 🔫 R6S | `R6S-XXXXXXXXX` | 1/6/12/Lifetime months | $9.99/$35.99/$65.99/$149.99 |
| 👾 AR | `AR-XXXXXXXXX` | 1/6/12/Lifetime months | $9.99/$35.99/$65.99/$149.99 |
| 🏝️ FN | `FN-XXXXXXXXX` | 1/6/12/Lifetime months | $9.99/$35.99/$65.99/$149.99 |

### **💳 Payment Methods**
- **Bitcoin (BTC)** - Primary cryptocurrency
- **Ethereum (ETH)** - Smart contract support
- **Solana (SOL)** - Fast transactions

### **🔄 Key Lifecycle**

1. **Generation** - ENGINE.exe creates keys with specific format
2. **Storage** - Keys saved to Neon DB + uploaded to website
3. **Purchase** - User buys key via website (crypto only)
4. **Redemption** - User redeems key via Discord bot
5. **Validation** - Bot validates format, role, expiration
6. **Access** - SmokeScreen-Engine.exe grants tab access
7. **Expiration** - Access automatically ends after duration

---

## 🤖 Discord Bot Integration

### **🔧 Bot Configuration**
```javascript
BOT_TOKEN: "MTQ3NzQyOTgzMDYxMzA3ODI0Nw.G0ZPlX..."
GUILD_ID: "1455221314653786207"
ANNOUNCEMENT_CHANNEL_ID: "1455221314653786207"
PORT: 9877
```

### **📋 Commands**

#### **!redeem <key>**
Redeem a key for game access
```
!redeem R6S-ABCD1234  // Rainbow Six Siege access
!redeem CODW-EFGH5678  // Warzone access  
!redeem AR-IJKL9012     // Arc Raiders access
!redeem FN-MNOP3456     // Fortnite access
```

#### **!help**
Display comprehensive help information
- Key examples for all games
- Pricing information
- Requirements and restrictions

### **🔐 Role-Based Access Control**
- **OWNER** (1455256056312631376) - Full access
- **COMMUNITY MANAGER** (1455256063153803304) - Key management
- **BASIC ACCESS** (1477448046873935872) - Key redemption

### **🔄 Auto-Reconnection System**
- **Exponential backoff** - Smart reconnection delays
- **Heartbeat monitoring** - Connection health checks
- **Status reporting** - Real-time bot status

---

## 💾 Database & Storage

### **🗄️ Neon Database Integration**
```sql
-- Keys Table Structure
CREATE TABLE recoil_keys (
    id INTEGER PRIMARY KEY,
    key TEXT UNIQUE NOT NULL,
    game_type TEXT NOT NULL,
    duration TEXT NOT NULL,
    generated_at INTEGER NOT NULL,
    generated_by TEXT DEFAULT 'ENGINE.exe',
    redeemed BOOLEAN DEFAULT FALSE,
    redeemed_by TEXT,
    redeemed_at INTEGER,
    expires_at INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### **📁 Local Storage**
- **generated_keys.json** - Bot local key cache
- **user_preferences.json** - Client settings
- **analytics.db** - Usage statistics
- **ai_notifications.db** - AI system logs

### **🔄 Synchronization**
- **Real-time updates** - Instant key propagation
- **Conflict resolution** - Handle duplicate keys
- **Backup systems** - Prevent data loss

---

## 🌐 Website Integration

### **🛒 Key Store Features**
- **Invisible uploads** - Keys auto-added from ENGINE.exe
- **Crypto payments** - BTC/ETH/SOL processing
- **Instant delivery** - Automatic key after payment
- **Purchase history** - User transaction records
- **Stock management** - Real-time availability

### **🔗 API Endpoints**
```javascript
// Key Generation (ENGINE.exe only)
POST /generate-key
{
  "game": "R6S|CODW|AR|FN",
  "duration": "1_MONTH|6_MONTHS|12_MONTHS|LIFETIME", 
  "keys": 1-1000,
  "source": "ENGINE.exe"
}

// Bot Status
GET /status
{
  "status": "online|offline",
  "uptime": 12345,
  "totalKeys": 150,
  "redeemedKeys": 75,
  "ping": 42
}
```

---

## 🔧 Installation & Setup

### **📋 Prerequisites**
- **Windows 10/11** - Primary platform
- **.NET 8.0** - Runtime framework
- **Discord Account** - For bot interaction
- **Crypto Wallet** - For key purchases
- **Node.js 18+** - For Discord bot

### **🚀 Quick Start**

1. **Download Applications**
   ```
   ENGINE.exe (Admin Tool)
   SmokeScreen-Engine.exe (User Client)
   ```

2. **Start Discord Bot**
   ```bash
   cd bot-directory
   npm install
   node bot-full-auth.js
   ```

3. **Launch ENGINE.exe**
   - Configure admin settings
   - Generate recoil keys
   - Monitor analytics

4. **Run SmokeScreen-Engine.exe**
   - Redeem game keys
   - Access recoil controls
   - Enjoy enhanced gaming

### **⚙️ Configuration Files**
- **bot-full-auth.js** - Discord bot settings
- **generated_keys.json** - Local key storage
- **Neon DB** - Cloud database
- **website-config.json** - Store integration

---

## 📚 API Documentation

### **🔑 Key Generation API**

#### **Endpoint**: `POST http://localhost:9877/generate-key`

#### **Request Body**:
```json
{
  "game": "R6S|CODW|AR|FN|MAIN",
  "duration": "1_MONTH|6_MONTHS|12_MONTHS|LIFETIME",
  "keys": 1-1000,
  "source": "ENGINE.exe"
}
```

#### **Response**:
```json
{
  "success": true,
  "keys": ["R6S-ABCD1234", "R6S-EFGH5678"],
  "message": "Generated 2 keys for R6S (1_MONTH)"
}
```

### **📊 Status API**

#### **Endpoint**: `GET http://localhost:9877/status`

#### **Response**:
```json
{
  "status": "online",
  "uptime": 86400,
  "guild": "SmokeScreen ENGINE",
  "reconnectAttempts": 0,
  "lastHeartbeat": "2024-03-02T10:30:00Z",
  "wsStatus": 6,
  "ping": 45,
  "totalKeys": 250,
  "redeemedKeys": 125
}
```

### **🤖 Discord Bot Commands**

#### **Key Redemption Flow**:
1. User types `!redeem KEY-XXXXXXX`
2. Bot validates key format
3. Bot checks user Discord roles
4. Bot verifies key not redeemed/expired
5. Bot grants access and confirms redemption
6. SmokeScreen-Engine.exe detects redeemed key
7. User gains access to specific game tab

---

## 💰 Payment System

### **🪙 Cryptocurrency Integration**

#### **Supported Coins**:
- **Bitcoin (BTC)** - Primary payment method
- **Ethereum (ETH)** - Smart contract compatibility  
- **Solana (SOL)** - Fast, low-fee transactions

#### **Payment Flow**:
1. **User selects game** → Chooses duration
2. **Crypto payment** → Sends BTC/ETH/SOL
3. **Payment confirmation** → Blockchain verification
4. **Key generation** → Automatic key creation
5. **Instant delivery** → Key appears in Discord
6. **Access granted** → Tab unlocked in client

#### **Pricing Tiers**:
```
🎮 GAME ACCESS
├── 📅 1 Month:  $9.99
├── 📅 6 Months: $35.99 (Save 40%)
├── 📅 12 Months: $65.99 (Save 45%)
└── 📅 Lifetime: $149.99 (Best Value)
```

---

## 🚀 Deployment

### **🖥️ Production Setup**

#### **Server Requirements**:
- **Node.js 18+** - Discord bot runtime
- **PM2 Process Manager** - Bot persistence
- **Reverse Proxy** - Nginx/Apache
- **SSL Certificate** - HTTPS termination
- **Firewall Rules** - Port 9877 access

#### **Database Deployment**:
- **Neon PostgreSQL** - Primary database
- **Connection Pooling** - Handle concurrent users
- **Backup Strategy** - Automated daily backups
- **Migration Scripts** - Schema updates

#### **Website Integration**:
- **Vercel Hosting** - Frontend deployment
- **API Gateway** - Backend connectivity
- **Crypto Processors** - Payment handling
- **CDN Distribution** - Global content delivery

### **📈 Monitoring & Analytics**

#### **System Metrics**:
- **Bot uptime** - Real-time availability
- **Key generation** - Generation statistics
- **Redemption rates** - Conversion tracking
- **User activity** - Engagement metrics
- **Revenue tracking** - Payment analytics

#### **Alert System**:
- **Bot disconnection** - Immediate notifications
- **Database errors** - Error reporting
- **Payment failures** - Transaction monitoring
- **System overload** - Performance alerts

---

## 🔒 Security Features

### **🛡️ Key Security**
- **Format validation** - Prevents fake keys
- **Source verification** - ENGINE.exe only generation
- **Role-based access** - Discord role validation
- **One-time use** - Keys expire after redemption
- **Expiration enforcement** - Automatic access removal

### **🔐 Authentication**
- **Discord OAuth2** - Secure user verification
- **Role hierarchy** - Granular permission control
- **API rate limiting** - Prevent abuse
- **CORS protection** - Secure API access
- **Input validation** - Sanitize all data

---

## 📞 Support & Troubleshooting

### **🔧 Common Issues**

#### **Bot Connection Problems**:
```bash
# Check bot token
echo $DISCORD_BOT_TOKEN

# Verify guild ID
# Check Discord server settings

# Restart bot
pm2 restart bot-app
```

#### **Key Redemption Issues**:
- **Invalid format** - Check key matches game pattern
- **Already redeemed** - Each key works once
- **Role missing** - Verify Discord role assignment
- **Expired key** - Purchase new subscription

#### **Client Access Problems**:
- **Restart client** - Refresh key cache
- **Check internet** - Verify Discord connectivity
- **Update client** - Ensure latest version
- **Clear cache** - Remove corrupted data

### **📚 Additional Resources**

- **Discord Developer Portal** - Bot configuration
- **Neon Documentation** - Database guides
- **GitHub Repository** - Source code reference
- **Community Discord** - User support
- **Video Tutorials** - Setup guides

---

## 📈 Version History

### **v2.0.0** (Current Release)
- ✅ Complete recoil key system
- ✅ Discord bot integration
- ✅ Neon database support
- ✅ Website payment integration
- ✅ Multi-game support
- ✅ Admin dashboard

### **v1.x.x** (Previous Versions)
- 📦 Basic key generation
- 🔧 Simple admin controls
- 📊 Limited analytics
- 🎮 Single game support

---

## 📄 License Information

### **📜 Software License**
- **Proprietary** - Commercial license required
- **Per-seat** - Each user requires license
- **Subscription-based** - Monthly/annual options
- **Source protection** - Encrypted components

### **🔑 Usage Rights**
- ✅ **Permitted**: Key generation and redemption
- ✅ **Permitted**: Client access and usage
- ❌ **Restricted**: Reverse engineering
- ❌ **Restricted**: Redistribution
- ❌ **Restricted**: Resale of access

---

## 🎯 Future Roadmap

### **🚀 Upcoming Features**
- **🎮 More Games** - Expand to popular titles
- **📱 Mobile App** - iOS/Android clients
- **☁️ Cloud Saves** - Cross-device sync
- **🤖 AI Assistant** - In-game help system
- **🏆 Ranking System** - Competitive leaderboards
- **🎨 Custom Themes** - UI personalization

### **🔧 Technical Improvements**
- **🔌 Plugin System** - Third-party extensions
- **📊 Advanced Analytics** - Detailed insights
- **🔄 Auto-Updates** - Seamless upgrades
- **🌍 Multi-language** - Global accessibility
- **⚡ Performance** - Optimization updates

---

## 👥 Development Team

### **🔧 Core Contributors**
- **Lead Developer** - System architecture & recoil algorithms
- **Backend Developer** - Discord bot & API development
- **Frontend Developer** - Website & user interface
- **Database Engineer** - Neon integration & optimization
- **Security Specialist** - Authentication & encryption

### **🧪 Quality Assurance**
- **Test Engineers** - Multi-game compatibility
- **Security Auditors** - Penetration testing
- **Performance Analysts** - Optimization testing
- **User Experience** - Interface design

---

## 📞 Contact Information

### **💬 Support Channels**
- **Discord Community** - 24/7 user support
- **Email Support** - technical@smokex-screen.com
- **Documentation Wiki** - guides.smokex-screen.com
- **Bug Reports** - github.com/smokex-screen/issues
- **Feature Requests** - feedback.smokex-screen.com

### **🌐 Official Links**
- **Website**: https://smokex-screen.com
- **Discord**: https://discord.gg/smokex-screen
- **GitHub**: https://github.com/smokex-screen
- **Twitter**: @SmokeScreenENG
- **YouTube**: youtube.com/@SmokeScreenENG

---

*📋 Last Updated: March 2, 2026*
*🔖 Version: 2.0.0*
*👥 Documentation: Complete System Guide*
