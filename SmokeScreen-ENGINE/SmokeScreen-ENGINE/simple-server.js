const express = require('express');
const cors = require('cors');

const app = express();
const PORT = 3001;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static(__dirname));

// Sync endpoint for bot
app.get('/api/sync', (req, res) => {
  res.json({
    success: true,
    message: 'Bot synchronization successful',
    timestamp: new Date().toISOString(),
    keys: []
  });
});

// POST endpoint for sync (Engine.exe communication)
app.post('/api/sync', (req, res) => {
  console.log('Received POST request to /api/sync:', req.body);
  res.json({
    success: true,
    message: 'POST sync successful',
    action: req.body.action || 'unknown',
    timestamp: new Date().toISOString(),
    received: req.body
  });
});

// Status endpoint
app.get('/api/status', (req, res) => {
  res.json({
    status: 'online',
    version: '1.0.0',
    bot: 'connected',
    timestamp: new Date().toISOString()
  });
});

// Keys endpoint
app.get('/api/keys', (req, res) => {
  res.json({
    success: true,
    keys: [
      {
        key: 'SS-MASTER-99X-QM22-L091-OWNER-PRIME',
        game: 'MASTER',
        duration: 'LIFETIME',
        redeemedBy: '1368087024401252393'
      }
    ]
  });
});

app.listen(PORT, () => {
  console.log(`Simple website/API running on http://localhost:${PORT}`);
  console.log(`Sync endpoint: http://localhost:${PORT}/api/sync`);
});
