/** Built by the Tailwind target in website.csproj into wwwroot/css/tailwind.css */
module.exports = {
    content: [
        './Pages/**/*.{razor,cs}',
        './Shared/**/*.{razor,cs}',
        './Data/**/*.cs',
        './wwwroot/js/site.js',
        '../website.api/Components/**/*.razor',
    ],
    darkMode: 'class',
    theme: {
        extend: {
            colors: {
                'ink': '#0b1020',     // page background
                'ink-2': '#111a2e',   // raised surfaces
                'line': '#1f2a44',    // borders
                'cy': '#22d3ee',      // primary accent
                'lamp': '#f5a35c',    // warm accent
            },
            fontFamily: {
                'sans': ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
                'mono': ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
            }
        }
    }
}
