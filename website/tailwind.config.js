/** Built by the Tailwind target in website.csproj into wwwroot/css/tailwind.css */
module.exports = {
    content: [
        './Pages/**/*.{razor,cs}',
        './Shared/**/*.{razor,cs}',
        './wwwroot/js/site.js',
        '../website.api/Components/**/*.razor',
    ],
    darkMode: 'class',
    theme: {
        extend: {
            fontFamily: {
                'mono': ['"Source Code Pro"', 'monospace'],
            }
        }
    }
}
