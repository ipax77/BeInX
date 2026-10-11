const path = require("path");
const VersionedModulePlugin = require('../../../scripts/versioned-module-plugin.cjs');
const { version } = require('./package.json');
const filename = `pdf-generator.v${version}.js`;

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
        filename,
        clean: true,
        library: {
            type: "module",
        },
    },
    plugins: [new VersionedModulePlugin(
        path.resolve(__dirname, '../JsModule.g.cs'),
        'BlazorInvoice.Pdf',
        `_content/BlazorInvoice.Pdf/dist/${filename}`,
    )],
    cache: {
        type: 'filesystem',
    },
    mode: 'production',
};
