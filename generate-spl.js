const crypto = require('crypto');
const fs = require('fs');

const chineseChars = "赵钱孙李周吴郑王冯陈褚卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜戚谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳酆鲍史";

function encryptChinese(plaintext) {
    const key = "SmokeScreenENGINE2026";
    const keyBytes = Buffer.from(key, 'utf8');
    const plaintextBytes = Buffer.from(plaintext, 'utf8');
    
    let result = '';
    for (let i = 0; i < plaintextBytes.length; i++) {
        const b = plaintextBytes[i] ^ keyBytes[i % keyBytes.length];
        const idx1 = (b >> 4) % chineseChars.length;
        const idx2 = (b & 0x0F) % chineseChars.length;
        result += chineseChars[idx1] + chineseChars[idx2];
    }
    return Buffer.from(result, 'utf8').toString('base64');
}

const config = {
    ConfigVersion: "1.0.0",
    EnableIPTracking: true,
    EnableAnalytics: true,
    PingIntervalMs: 3000,
    MaxCachedUsers: 1000,
    AdminKey: crypto.randomBytes(18).toString('base64').replace(/=/g, '').substring(0, 24),
    AllowedIPRanges: [],
    CreatedAt: new Date().toISOString()
};

const json = JSON.stringify(config, null, 2);
const encrypted = encryptChinese(json);

fs.writeFileSync('SmokeScreen-ENGINE/public/Engine.keyusergrid.SPL', encrypted);
console.log('Engine.keyusergrid.SPL created!');
console.log('Admin Key:', config.AdminKey);
