const path = require("path");

module.exports = {
    entry: './pdf-generator.ts',
    module: {
        rules: [
            {
                test: /\.ts?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    optimization: {
        usedExports: true,
    },
    experiments: {
        outputModule: true,
    },
    output: {
        path: path.resolve(__dirname, '../wwwroot/dist'),
        filename: "pdf-generator.js",
        clean: true,
        library: {
            type: "module",
        },
    },
    cache: {
        type: 'filesystem',
    },
    mode: 'production',
};