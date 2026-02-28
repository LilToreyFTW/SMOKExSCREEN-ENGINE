// SmokeScreen ENGINE — main.js
// Tab logic, billing toggle, FAQ, and animations are in index.html.
// This file is reserved for future module imports and app-level logic.

console.log('SmokeScreen ENGINE — v4.2.1 Loaded');

// Expose switchTab globally for use in external scripts or console
window.SSE = {
  version: '4.2.1',
  goTo: function(tab) {
    if (typeof switchTab === 'function') switchTab(tab);
  }
};
