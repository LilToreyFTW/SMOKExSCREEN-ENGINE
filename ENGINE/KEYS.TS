// KEYS.TS - The Master Key Registry
export interface LicenseKey {
    key: string;
    duration: '1_MONTH' | '6_MONTHS' | '1_YEAR' | 'LIFETIME';
    isOwner: boolean;
}

const registry: LicenseKey[] = [
    // --- OWNER KEY ---
    { 
        key: "SS-MASTER-99X-QM22-L091-OWNER-PRIME", 
        duration: "LIFETIME", 
        isOwner: true 
    }
];

// Helper to generate mass keys
const generateType = (prefix: string, duration: any, count: number) => {
    for (let i = 0; i < count; i++) {
        const id = Math.random().toString(36).substring(2, 10).toUpperCase();
        registry.push({ key: `SS-${prefix}-${id}`, duration, isOwner: false });
    }
};

// Generate your 1000 keys for each type
generateType("1MO", "1_MONTH", 1000);
generateType("6MO", "6_MONTHS", 1000);
generateType("1YR", "1_YEAR", 1000);
generateType("LIFE", "LIFETIME", 1000);

export default registry;