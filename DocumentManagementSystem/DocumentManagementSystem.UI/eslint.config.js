export default [
    // dein bisheriges Setup …
    {
        files: ['tailwind.config.cjs', 'postcss.config.cjs', 'vite.config.js'],
        languageOptions: {
            sourceType: 'script', // CJS
            globals: { module: 'readonly', require: 'readonly', __dirname: 'readonly' }
        },
    },
];
