
const express = require('express');
const app = express();
app.get('/status', (req, res) => {
    res.json({ status: "API Running" });
});
app.listen(4000, () => console.log('API running on http://localhost:4000'));
